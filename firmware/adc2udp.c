/*
 * adc2udp -- ESP32 speed-sensor firmware for the VCE bicycle interface.
 *
 * Derived from the Virtual Cycling Environment (VCE),
 *   https://github.com/tkn-tub/virtual-cycling-environment
 *   path: bike-interface/esp32/adc2udp/main/adc2udp.c
 *   upstream commit: 4c9e70cde0fe2015e6377b83bc2d354f4d660b91 (main branch)
 * which is itself based on Espressif ESP-IDF example code
 * (Public Domain / CC0 -- original notice retained below).
 *
 * Modifications by Ali Obali, 2025-2026, for the Bachelor thesis
 * "Real-Time Integration of a Physical Bicycle into a Virtual Cycling
 * Environment and Its Effect on Users" (TU Berlin). Changes are mainly marked with
 * "//ali" (markers audited against the pinned commit on 2026-07-05;
 * deletions cannot carry inline markers, so the authoritative delta
 * remains a diff against the pinned upstream commit above). 
 *
 * The transmitted value is the wheel tick rate in ticks per second
 * (TPS), lightly smoothed on-device as described above; conversion to
 * m/s happens in the Unity receiver (BikeUdpReceiver.cs).
 */

/* adc2udp VCE
 *
 * read values of a hall sensor with the help of the adc and send them via udp

   This example code is in the Public Domain (or CC0 licensed, at your option.)

   Unless required by applicable law or agreed to in writing, this
   software is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR
   CONDITIONS OF ANY KIND, either express or implied.
*/
// #include <time.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <sys/param.h>
#include "freertos/FreeRTOS.h"
#include "freertos/task.h"
#include "freertos/event_groups.h"
#include "esp_system.h"
#include "esp_wifi.h"
#include "esp_event.h"
#include "esp_log.h"
#include "esp_err.h" //ali
#include "nvs_flash.h" //ali
#include "driver/gpio.h"
// #include "driver/adc.h"
// #include "esp_adc_cal.h"

#include "lwip/err.h"
#include "lwip/sockets.h"
#include "lwip/sys.h"
#include <lwip/netdb.h>

#include "nvs.h" //ali

//-------- WIFI declarations --------------------------------------
/* The examples use simple WiFi configuration that you can set via
   'make menuconfig'.
   If you'd rather not, just change the below entries to strings with
   the config you want - ie #define EXAMPLE_WIFI_SSID "mywifissid"
*/
#define EXAMPLE_WIFI_SSID CONFIG_WIFI_SSID
#define EXAMPLE_WIFI_PASS CONFIG_WIFI_PASSWORD

#define BICYCLE_MODEL_HOST CONFIG_BICYCLE_MODEL_HOST
#define PORT CONFIG_BICYCLE_MODEL_PORT

static const char *TAG = "adc2udp";

//-------- ADC declarations ---------------------------------------
// #define DEFAULT_VREF    1100                                    //Use adc2_vref_to_gpio() to obtain a better estimate
// #define NO_OF_SAMPLES   CONFIG_NUMBER_OF_SAMPLES_PER_VALUE      //Multisampling
//
// static esp_adc_cal_characteristics_t *adc_chars;
// static const adc_channel_t channel = ADC_CHANNEL_6;     //GPIO34 if ADC1, GPIO14 if ADC2
// static const adc_atten_t atten = ADC_ATTEN_DB_11;       //Use ADC_ATTEN_DB_11 for a maxium voltage of 3.9V
// static const adc_unit_t unit = ADC_UNIT_1;

//-------- GPIO declarations --------------------------------------
//GPIO setup based on this example:
//https://github.com/espressif/esp-idf/blob/09f7589ef23a6b78339224efd372257a57e1be4b/examples/peripherals/gpio/generic_gpio/main/gpio_example_main.c

//1ULL = 1 'unsigned long long' = 1 as u64
#define GPIO_PIN_IR_SENSOR CONFIG_GPIO_IR_SENSOR
#define GPIO_IR_SENSOR_PIN_BITMASK (1ULL<<CONFIG_GPIO_IR_SENSOR)
#define GPIO_PIN_IR_SENSOR_EN CONFIG_GPIO_IR_SENSOR_EN
#define ESP_INTR_FLAG_DEFAULT 0
#define IR_SENSOR_HAS_EN_PIN CONFIG_IR_SENSOR_HAS_EN_PIN

//-------- Speed sensor variables ---------------------------------
static uint32_t ir_sensor_tick_counter = 0;
static int64_t ir_sensor_last_tick_time_us = 0;
#define SPEED_SENSOR_WINDOW_SIZE CONFIG_SPEED_SENSOR_WINDOW_SIZE
#define SPEED_SENSOR_TIMEOUT_US 500000
// Assuming a maximum of 48 spokes per wheel, a diameter of 0.6 m, and a max speed vmax of
// 45 km/h = 12.5 m/s
// -> distance per rotation dpr = pi*d = 1.88 m
// -> max rotations per second rps = vmax / dpr = 6.65
// -> max spokes per second = rps * 48 = 319.2
// -> min time between spokes = 1 / 319.2 = 3.1 ms
#define SPEED_SENSOR_MIN_TICK_TIME_MS 3
static int64_t speed_sensor_intervals[SPEED_SENSOR_WINDOW_SIZE];
static float speed_sensor_ticks_per_second = 0;
//static const float speed_sensor_filter_factor = 0.75; //ali - upstream version
static const float speed_sensor_filter_factor __attribute__((unused)) = 0.75f; //ali
QueueHandle_t speed_sensor_gpio_queue = NULL;

//----------- Forward declarations ----------------------------------- //ali
static int64_t get_time_us(); //ali

//----------- freertos task ------------------------------------------
static void udp_transmit_task(void *pvParameters)
{
    // TODO: integrate speed sensor by having ir sensor ticks add values to a queue that we process here in a loop…
    // cp. the queuing in the gpio example: https://github.com/espressif/esp-idf/blob/09f7589ef23a6b78339224efd372257a57e1be4b/examples/peripherals/gpio/generic_gpio/main/gpio_example_main.c
    // ^ upstream TODO, resolved: ticks are queued from gpio_isr_handler() //ali
    //   and processed in speed_sensor_task(). //ali

    char sendbuf[128];
    char addr_str[128];
    int addr_family;
    int ip_protocol;
    int delayInMs = 1000 / CONFIG_UDP_PACKET_FREQUENCY;
    int failures = 0;

    ESP_LOGI(TAG, "Running speed sensor task.");

#ifdef CONFIG_IPV4
    struct sockaddr_in destAddr;
    destAddr.sin_addr.s_addr = inet_addr(BICYCLE_MODEL_HOST);
    destAddr.sin_family = AF_INET;
    destAddr.sin_port = htons(PORT);
    addr_family = AF_INET;
    ip_protocol = IPPROTO_IP;
    inet_ntoa_r(destAddr.sin_addr, addr_str, sizeof(addr_str) - 1);
#else // IPV6
    struct sockaddr_in6 destAddr;
    bzero(&destAddr.sin6_addr.un, sizeof(destAddr.sin6_addr.un));
    destAddr.sin6_family = AF_INET6;
    destAddr.sin6_port = htons(PORT);
    addr_family = AF_INET6;
    ip_protocol = IPPROTO_IPV6;
    inet6_ntoa_r(destAddr.sin6_addr, addr_str, sizeof(addr_str) - 1);
#endif

    int sock = socket(addr_family, SOCK_DGRAM, ip_protocol);
    if (sock < 0) {
        ESP_LOGE(TAG, "Unable to create socket: errno %d", errno);
        return;
    }
    int err = connect(sock, (struct sockaddr *)&destAddr, sizeof(destAddr));
    if (err < 0) {
        ESP_LOGE(TAG, "Socket unable to connect: errno %d", errno);
        return;
    }
    ESP_LOGI(TAG, "Socket connected to %s:%d.", addr_str, PORT);

    while(1) {
        //Ignoring ADC values for now; only using the IR sensor ticks for the speed sensor
        // uint32_t adc_reading = 0;
        // failures = 0;
        // //Multisampling
        // for (int i = 0; i < NO_OF_SAMPLES; i++) {
        //     if (unit == ADC_UNIT_1) {
        //         adc_reading += adc1_get_raw((adc1_channel_t)channel);
        //     } else {
        //         int raw;
        //         adc2_get_raw((adc2_channel_t)channel, ADC_WIDTH_BIT_12, &raw);
        //         adc_reading += raw;
        //     }
        // }
        // adc_reading /= NO_OF_SAMPLES;

        //Convert adc_reading to voltage in mV
        // uint32_t voltage = esp_adc_cal_raw_to_voltage(adc_reading, adc_chars);
        // ESP_LOGI(TAG, "Raw: %d\tVoltage: %dmV", adc_reading, voltage);

        //ali
        // Apply speed decay if no new ticks for a while (realistic coast-down)
        int64_t time_since_last_tick = get_time_us() - ir_sensor_last_tick_time_us;
        const float decay_rate = 0.95f;  // speed decays to 95% per packet when coasting
        const int64_t decay_threshold_us = 200000;  // start decay after 200ms without tick
        
        if (time_since_last_tick > decay_threshold_us && speed_sensor_ticks_per_second > 0.5f) {
            speed_sensor_ticks_per_second *= decay_rate;
            ESP_LOGI(TAG, "Coasting: speed decaying to %.2f tps", speed_sensor_ticks_per_second);
        }
        //ali end

        //Send adc value
        // int len = sprintf(sendbuf, "%d", adc_reading);
        int len = sprintf(sendbuf, "%0.2f", speed_sensor_ticks_per_second);
        ESP_LOGI(TAG, "Sending speed value: %0.2f tps (tick_counter=%lu)", speed_sensor_ticks_per_second, (unsigned long)ir_sensor_tick_counter); //ali
        int err = sendto(sock, sendbuf, len, 0,(struct sockaddr *)&destAddr, sizeof(destAddr));  // returns -1 if something fails
        // ESP_LOGI(TAG, "send adc value: %d\t to %s:%d\n.", adc_reading, addr_str, PORT);
        while (err < 0) {
            ESP_LOGE(TAG, "Error occurred during sending: errno %d", errno);
            if (errno == 12) {  // ENOMEM
                failures++;
                //ali start
                if (failures > 10) { //limit retries
                    ESP_LOGE(TAG, "Too many ENOMEM failures, skipping packet");
                    break;
                }
                vTaskDelay(pdMS_TO_TICKS(100)); //delay before retry
                //ali end
                err = sendto(sock, sendbuf, len, 0,(struct sockaddr *)&destAddr, sizeof(destAddr));
            } else {
                break; //ali
            }
        }
        if (err >= 0) failures = 0; //ali //reset counter on success
        // ESP_LOGI(TAG, "Sent speed sensor value: %s with %d failures.", sendbuf, failures);
        vTaskDelay(pdMS_TO_TICKS(delayInMs));
     }

    if (sock != -1) {
        ESP_LOGE(TAG, "Shutting down socket and restarting...");
        shutdown(sock, 0);
        close(sock);
    }
    vTaskDelete(NULL);
}

//--------- WIFI functions -----------------------------------------
static void event_handler(void *arg, esp_event_base_t event_base, int32_t event_id, void* event_data)
{
    if (event_base == WIFI_EVENT) {
        switch (event_id) {
            case WIFI_EVENT_STA_START:
                //ali start
                //ESP_ERROR_CHECK(esp_wifi_connect()); //upstream version
                esp_err_t err = esp_wifi_connect(); //moved here from initialise_wifi
                if (err != ESP_OK && err != ESP_ERR_WIFI_CONN) { //ignore if already connecting
                    ESP_LOGE(TAG, "connect failed: %s", esp_err_to_name(err));
                } //ali end
                ESP_LOGI(TAG, "SYSTEM_EVENT_STA_START");
                break;
            case WIFI_EVENT_STA_CONNECTED:
                ESP_LOGI(TAG, "SYSTEM_EVENT_STA_CONNECTED");
                break;
            case WIFI_EVENT_STA_DISCONNECTED:
                {
                    wifi_event_sta_disconnected_t *disconnected = (wifi_event_sta_disconnected_t *)event_data; //ali
                    ESP_LOGW(TAG, "WIFI_EVENT_STA_DISCONNECTED, reason: %d", disconnected->reason); //ali
                    ESP_LOGI(TAG, "SYSTEM_EVENT_STA_DISCONNECTED");
                    esp_wifi_connect();
                }
                break;
            default:
                //ESP_LOGI(TAG, "Unknown event_id: %d.", event_id); //ali - upstream version
                ESP_LOGI(TAG, "Unknown event_id: %ld.", (long)event_id); //ali
                break;
        }
    } else if (event_base == IP_EVENT) {
        switch (event_id) {
            case IP_EVENT_STA_GOT_IP:
                ESP_LOGI(TAG, "SYSTEM_EVENT_STA_GOT_IP");
                xTaskCreate(udp_transmit_task, "udp_transmit_task", 16384, NULL, 5, NULL);
                break;
            default:
                //ESP_LOGI(TAG, "Unknown event_id: %d.", event_id); //ali - upstream version
                ESP_LOGI(TAG, "Unknown event_id: %lu.", (unsigned long)event_id); //ali
                break;
        }
    } else {
        ESP_LOGI(TAG, "Unknown event base.");
    }
}

static void initialise_wifi(void)
{
    //ali start
    ESP_LOGI(TAG, "Initialising WiFi"); 

    // NVS init
    esp_err_t ret = nvs_flash_init();
    if (ret == ESP_ERR_NVS_NO_FREE_PAGES || ret == ESP_ERR_NVS_NEW_VERSION_FOUND) {
        ESP_ERROR_CHECK(nvs_flash_erase());
        ESP_ERROR_CHECK(nvs_flash_init());
    }
    //ali end

    ESP_ERROR_CHECK(esp_netif_init());
    ESP_ERROR_CHECK(esp_event_loop_create_default());
    esp_netif_create_default_wifi_sta();

    wifi_init_config_t cfg = WIFI_INIT_CONFIG_DEFAULT();
    ESP_ERROR_CHECK(esp_wifi_init(&cfg));

    //upstream version
    // esp_event_handler_instance_t instance_got_ip;
    // esp_event_handler_instance_t instance_any_id;
    // ESP_ERROR_CHECK(esp_event_handler_instance_register(IP_EVENT,
    //                                                     IP_EVENT_STA_GOT_IP,
    //                                                     &event_handler,
    //                                                     NULL,
    //                                                     &instance_got_ip));

    ESP_ERROR_CHECK(esp_event_handler_instance_register(
        WIFI_EVENT,
        ESP_EVENT_ANY_ID,
        &event_handler,
        NULL,
        NULL //upstream replaced &instance_any_id
    ));
    ESP_ERROR_CHECK(esp_event_handler_instance_register(
        IP_EVENT,
        IP_EVENT_STA_GOT_IP,
        &event_handler,
        NULL,
        NULL
    ));

    wifi_config_t wifi_config = {
        .sta = {
            .ssid = EXAMPLE_WIFI_SSID,
            .password = EXAMPLE_WIFI_PASS,
            .threshold.authmode = WIFI_AUTH_WPA_WPA2_PSK, //ali - flexible auth for iPhone hotspot
            .pmf_cfg = { //ali
                .capable = true,
                .required = false,
                // (upstream wifi_config also set .scan_method, .bssid_set, .channel,
                //  .threshold.rssi and .threshold.authmode = WIFI_AUTH_OPEN)
            },
        },
    };

    //upstream
    // ESP_ERROR_CHECK(esp_wifi_set_ps(WIFI_PS_NONE));
    // ESP_LOGI(TAG, "Setting WiFi configuration SSID %s...", wifi_config.sta.ssid);

    ESP_ERROR_CHECK(esp_wifi_set_mode(WIFI_MODE_STA));
    ESP_ERROR_CHECK(esp_wifi_set_config(WIFI_IF_STA, &wifi_config));
    ESP_ERROR_CHECK(esp_wifi_start());
    //ESP_ERROR_CHECK(esp_wifi_connect()); //ali - removed bc event handler will trigger connection
}


//-------- ADC functions -----------------------------------------------
// static void check_efuse()
// {
//     //Check TP is burned into eFuse
//     if (esp_adc_cal_check_efuse(ESP_ADC_CAL_VAL_EFUSE_TP) == ESP_OK) {
//         printf("eFuse Two Point: Supported\n");
//     } else {
//         printf("eFuse Two Point: NOT supported\n");
//     }
//
//     //Check Vref is burned into eFuse
//     if (esp_adc_cal_check_efuse(ESP_ADC_CAL_VAL_EFUSE_VREF) == ESP_OK) {
//         printf("eFuse Vref: Supported\n");
//     } else {
//         printf("eFuse Vref: NOT supported\n");
//     }
// }

// static void print_char_val_type(esp_adc_cal_value_t val_type)
// {
//     if (val_type == ESP_ADC_CAL_VAL_EFUSE_TP) {
//         printf("Characterized using Two Point Value\n");
//     } else if (val_type == ESP_ADC_CAL_VAL_EFUSE_VREF) {
//         printf("Characterized using eFuse Vref\n");
//     } else {
//         printf("Characterized using Default Vref\n");
//     }
// }

// static void configure_adc(void)
// {
//     //Check if Two Point or Vref are burned into eFuse
//     check_efuse();
//
//     //Configure ADC
//     if (unit == ADC_UNIT_1) {
//         adc1_config_width(ADC_WIDTH_BIT_12);
//         adc1_config_channel_atten((adc1_channel_t)channel, atten);
//     } else {
//         adc2_config_channel_atten((adc2_channel_t)channel, atten);
//     }
//
//     //Characterize ADC
//     adc_chars = calloc(1, sizeof(esp_adc_cal_characteristics_t));
//     esp_adc_cal_value_t val_type = esp_adc_cal_characterize(unit, atten, ADC_WIDTH_BIT_12, DEFAULT_VREF, adc_chars);
//     print_char_val_type(val_type);
// }

static int64_t get_time_us()
{
    //Get time in us according to
    //https://docs.espressif.com/projects/esp-idf/en/latest/esp32/api-reference/system/system_time.html
    struct timeval tv_now;
    gettimeofday(&tv_now, NULL);
    return (int64_t)tv_now.tv_sec * 1000000L + (int64_t)tv_now.tv_usec;
}

static void reset_speed_sensor()
{
    for (size_t i=0; i < SPEED_SENSOR_WINDOW_SIZE; ++i)
    {
        speed_sensor_intervals[i] = 0;
    }
    speed_sensor_ticks_per_second = 0;
    ir_sensor_tick_counter = 0;
}

// Called in speed_sensor_task whenever a gpio event is detected from the IR sensor.
// This function is used to determine speed_sensor_ticks_per_second,
// which is to be transmitted in udp_transmit_task().
static void IRAM_ATTR ir_sensor_tick()
{
    int64_t dt_us = get_time_us() - ir_sensor_last_tick_time_us;
    ir_sensor_last_tick_time_us = get_time_us();

    //ESP_LOGI(TAG, "speed sensor tick %d in %d us!", ir_sensor_tick_counter + 1, (uint32_t) dt_us); //upstream version
    //ali
    ESP_LOGI(TAG, 
             "speed sensor tick %lu in %lu us!",
             (unsigned long)(ir_sensor_tick_counter + 1),
             (unsigned long)dt_us);

    ir_sensor_tick_counter++;

    //upstream version:
    // size_t window_end = (ir_sensor_tick_counter - 1) % SPEED_SENSOR_WINDOW_SIZE;
    // speed_sensor_intervals[window_end] = dt_us;
    //
    // int64_t intervals_sum_us = 0;
    // for (size_t i=0; i < SPEED_SENSOR_WINDOW_SIZE; ++i)
    // {
    //     intervals_sum_us += speed_sensor_intervals[i];
    // }
    // speed_sensor_ticks_per_second = 1e6 / (intervals_sum_us / SPEED_SENSOR_WINDOW_SIZE);

    //ali start
    //calculate instantaneous speed from this interval only (not moving average)
    //which makes speed responsive to actual pedaling changes
    float instantaneous_tps = 1e6 / (float)dt_us;
    
    //exponential smoothing to reduce noise while maintaining responsiveness
    //Lower alpha = more smoothing but slower response
    //Higher alpha = less smoothing but faster response
    const float alpha = 0.5f;  //balanced, responds in 2-3 ticks
    
    //limit to prevent unrealistic jumps
    const float max_delta_tps = 25.0f;
    
    if (speed_sensor_ticks_per_second == 0 || ir_sensor_tick_counter == 1) {
        //First measurement
        speed_sensor_ticks_per_second = instantaneous_tps;
    } else {
        //Limit instantaneous changes to realistic values
        float delta = instantaneous_tps - speed_sensor_ticks_per_second;
        if (delta > max_delta_tps) {
            instantaneous_tps = speed_sensor_ticks_per_second + max_delta_tps;
        } else if (delta < -max_delta_tps) {
            instantaneous_tps = speed_sensor_ticks_per_second - max_delta_tps;
        }
        
        //exponential smoothing
        speed_sensor_ticks_per_second = alpha * instantaneous_tps + 
                                       (1.0f - alpha) * speed_sensor_ticks_per_second;
    }
    
    ESP_LOGI(TAG, "Speed: raw=%.2f tps, smoothed=%.2f tps (interval: %lld us)", 
             instantaneous_tps, speed_sensor_ticks_per_second, dt_us);
    //ali end
}

static void speed_sensor_task(void *pvParameters)
{
    uint32_t io_num;
    //ali start
    ESP_LOGI(TAG, "speed_sensor_task start"); 
    //IR sensor already enabled and interrupts configured in configure_gpio() 
    if (IR_SENSOR_HAS_EN_PIN) { 
        // optional small settle delay
        vTaskDelay(pdMS_TO_TICKS(3)); 
    }
    vTaskDelay(pdMS_TO_TICKS(100)); 
    gpio_intr_disable(GPIO_PIN_IR_SENSOR); 
    gpio_set_intr_type(GPIO_PIN_IR_SENSOR, GPIO_INTR_POSEDGE); 
    gpio_intr_enable(GPIO_PIN_IR_SENSOR);
    ESP_LOGI(TAG, "IR sensor interrupts enabled after warmup"); 
    //ali end
    for (;;)
    {
        if (speed_sensor_gpio_queue == 0) continue;
        int64_t dt_us = get_time_us() - ir_sensor_last_tick_time_us;

        // Block for SPEED_SENSOR_QUEUE_INTV_TICKS if a message is not immediately available:
        if (xQueueReceive(speed_sensor_gpio_queue, &io_num, pdMS_TO_TICKS(50)))
        {
            ESP_LOGI(TAG, "Received GPIO interrupt event from queue"); //ali
            if (dt_us < SPEED_SENSOR_MIN_TICK_TIME_MS * 1000)
            {
                ESP_LOGI(TAG, "Ignoring tick - too soon after last (dt=%lld us)", dt_us); //ali
                //re-enable immediately
                gpio_intr_enable(GPIO_PIN_IR_SENSOR); //ali 
                continue;
            }
            if (IR_SENSOR_HAS_EN_PIN) {
                // Disabling IR sensor regularly b/c of automatic gain control:
                //  http://irsensor.wizecode.com/
                // Also to hopefully avoid some of the phantom ticks.
                gpio_set_level(GPIO_PIN_IR_SENSOR_EN, 0);
                gpio_intr_disable(GPIO_PIN_IR_SENSOR);
            }

            ir_sensor_tick();

            if (IR_SENSOR_HAS_EN_PIN) {
                vTaskDelay(pdMS_TO_TICKS(3));
                gpio_set_level(GPIO_PIN_IR_SENSOR_EN, 1);
                vTaskDelay(pdMS_TO_TICKS(1));
            }
            //always re-enable interrupts after processing
            gpio_intr_enable(GPIO_PIN_IR_SENSOR); //ali
            ESP_LOGI(TAG, "Interrupt re-enabled after tick"); //ali
        }
        else if (dt_us > SPEED_SENSOR_TIMEOUT_US)
        {
            reset_speed_sensor();
            continue;
        }
    }
}

//on IRAM_ATTR: https://esp32.com/viewtopic.php?t=4978
//-> Apparently, ensure that this function is stored in RAM, not
//in slower flash memory…
static void IRAM_ATTR gpio_isr_handler(void* arg)
{
    // Don't run normal C code here!
    // Instead, we push an event to xQueue to be read in speed_sensor_task.

    uint32_t gpio_num = (uint32_t)arg;
    //Should be equal to CONFIG_GPIO_IR_SENSOR if no other pins are used for GPIO?
    //xQueueSendFromISR(speed_sensor_gpio_queue, &gpio_num, NULL);
    //ali start
    // Throttle potential interrupt storms by disabling until task processes
    gpio_intr_disable(GPIO_PIN_IR_SENSOR); 
    BaseType_t xHigherPriorityTaskWoken = pdFALSE;
    BaseType_t result = xQueueSendFromISR(speed_sensor_gpio_queue, &gpio_num, &xHigherPriorityTaskWoken); 
    if (result != pdTRUE) {
        //Queue full
    }
    if (xHigherPriorityTaskWoken) { 
        portYIELD_FROM_ISR(); 
    } //ali end
}

static void configure_gpio(void)
{
    //ali start
    ESP_LOGI(TAG, "configure_gpio start"); 
    ESP_LOGI(TAG, "before queue create"); 
    speed_sensor_gpio_queue = xQueueCreate(10, sizeof(uint32_t)); //upstream line, moved before ISR setup
    ESP_LOGI(TAG, "after queue create: %p", (void*)speed_sensor_gpio_queue); 
    if (speed_sensor_gpio_queue == NULL) { 
        ESP_LOGE(TAG, "Failed to create speed sensor GPIO queue"); 
        return; 
    }

#if CONFIG_IDF_TARGET_ESP32S2 
    // Guard against selecting flash/PSRAM pins on ESP32-S2 (26..33). Using these crashes at runtime. 
    const int reserved_pins[] = {26,27,28,29,30,31,32,33}; 
    for (size_t i = 0; i < sizeof(reserved_pins)/sizeof(reserved_pins[0]); ++i) { 
        if (GPIO_PIN_IR_SENSOR == reserved_pins[i]) { 
            ESP_LOGE(TAG, "GPIO %d is reserved for SPI flash/PSRAM on ESP32-S2. Change CONFIG_GPIO_IR_SENSOR to a free pin.", GPIO_PIN_IR_SENSOR);
            return; 
        } 
    } 
#endif // CONFIG_IDF_TARGET_ESP32S2

    // Install ISR service before configuring/enable interrupts 
    esp_err_t isr_svc = gpio_install_isr_service(ESP_INTR_FLAG_DEFAULT);
    if (isr_svc == ESP_ERR_INVALID_STATE) { 
        ESP_LOGW(TAG, "GPIO ISR service already installed"); 
    } else if (isr_svc != ESP_OK) { 
        ESP_LOGE(TAG, "Failed to install GPIO ISR service: %s", esp_err_to_name(isr_svc)); //ali
        return; 
    }
    ESP_LOGI(TAG, "Installed GPIO ISR service"); 
    //ali end
   //hook isr handler for specific gpio pin:
    int isr_result = gpio_isr_handler_add(GPIO_PIN_IR_SENSOR, gpio_isr_handler, (void*) GPIO_PIN_IR_SENSOR); //upstream line, moved before pin config
    switch (isr_result)
    {
        case ESP_OK:
            ESP_LOGI(TAG, "Set up gpio_isr_handler");
            break;
        case ESP_ERR_INVALID_STATE:
            ESP_LOGE(TAG, "Invalid state for gpio_isr_handler_add");
            break;
        case ESP_ERR_INVALID_ARG:
            ESP_LOGE(TAG, "Invalid argument for gpio_isr_handler_add");
            break;
    }
    //config for the IR sensor input pin 
    //ali start
    ESP_LOGI(TAG, "before gpio_reset_pin"); 
    esp_err_t err = gpio_reset_pin(GPIO_PIN_IR_SENSOR); 
    ESP_LOGI(TAG, "gpio_reset_pin result: %s", esp_err_to_name(err)); 
    ESP_LOGI(TAG, "before set_direction"); 
    err = gpio_set_direction(GPIO_PIN_IR_SENSOR, GPIO_MODE_INPUT); 
    ESP_LOGI(TAG, "gpio_set_direction result: %s", esp_err_to_name(err)); 
    ESP_LOGI(TAG, "before set_pull_mode"); 
    err = gpio_set_pull_mode(GPIO_PIN_IR_SENSOR, GPIO_PULLUP_ONLY); 
    ESP_LOGI(TAG, "gpio_set_pull_mode result: %s", esp_err_to_name(err)); 
    ESP_LOGI(TAG, "before set_intr_type"); 
    err = gpio_set_intr_type(GPIO_PIN_IR_SENSOR, GPIO_INTR_DISABLE); 
    ESP_LOGI(TAG, "gpio_set_intr_type result: %s", esp_err_to_name(err)); 
    ESP_LOGI(TAG, "Configured IR sensor pin %d (input, pullup, intr disabled)", GPIO_PIN_IR_SENSOR);
    //ali end

    //upstream configuration below
    // if (IR_SENSOR_HAS_EN_PIN) {
    //     // Configuration for the IR sensor output pin
    //     // (if it has an enable (EN) pin):
    //     gpio_config_t io_conf = {};
    //     io_conf.pin_bit_mask = 1ULL << GPIO_PIN_IR_SENSOR_EN;
    //     io_conf.mode = GPIO_MODE_OUTPUT;
    //     io_conf.pull_down_en = 1;
    //     io_conf.pull_up_en = 0;
    //     int gpioconf_result = gpio_config(&io_conf);
    //     if (gpioconf_result == ESP_ERR_INVALID_ARG) {
    //         ESP_LOGE(TAG, "Invalid argument in GPIO configuration for IR EN pin");
    //     }
    //     gpio_set_level(GPIO_PIN_IR_SENSOR_EN, 1); // Enable IR sensor on startup.
    //     ESP_LOGI(TAG, "Ran gpio_config, IR sensor EN on pin %d", GPIO_PIN_IR_SENSOR_EN);
    // }

    ESP_LOGI(TAG, "configure_gpio done (interrupts still disabled)"); //ali

    //speed sensor task
    //xTaskCreate(speed_sensor_task, "speed_sensor_task", 16384, NULL, 5, NULL);    //upstream version
    BaseType_t task_created = xTaskCreate(speed_sensor_task, "speed_sensor_task", 16384, NULL, 5, NULL); //ali
    ESP_LOGI(TAG, "speed_sensor_task create result: %ld", (long)task_created); //ali
}

void app_main()
{
    ESP_LOGI(TAG, "Starting adc2udp");
    reset_speed_sensor();
    ESP_LOGI(TAG, "Reset speed sensor for the first time");
    // ESP_ERROR_CHECK( nvs_flash_init() );
    // ESP_LOGI(TAG, "checked nvs flash.");
    // configure_adc(); // TODO: remove?
    configure_gpio();
    ESP_LOGI(TAG, "Configured GPIO");
    vTaskDelay(pdMS_TO_TICKS(500)); //give tasks time to start //ali
    vTaskDelay(pdMS_TO_TICKS(200)); //brief delay before WiFi init //ali
    initialise_wifi();
}
