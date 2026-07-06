# ESP32 Bicycle Interface Firmware

This folder contains the thesis-specific ESP32 source used for the bicycle input pipeline. The updated `adc2udp.c` in this folder is the version that should replace the upstream VCE file when rebuilding the firmware. Reflash only when hardware wiring, Wi-Fi, IP, or port values change.

## Source handoff

The upstream reference project is the TKN Virtual Cycling Environment repository:

https://github.com/tkn-tub/virtual-cycling-environment

To update the upstream firmware tree, replace:

- `bike-interface/esp32/adc2udp/main/adc2udp.c`

with the updated thesis version from this repository:

- `firmware/adc2udp.c`

## Reflash workflow

1. Clone the upstream VCE repository from TKN.
2. Copy `firmware/adc2udp.c` into the upstream `bike-interface/esp32/adc2udp/main/` directory.
3. Set up the ESP-ID 5.2 environment for the ESP32 (https://github.com/espressif/esp-idf). `git clone -b v5.2 --recursive https://github.com/espressif/esp-idf.git`
4. Open the configuration menu with `idf.py menuconfig`.
5. Set the GPIO pins, Wi-Fi credentials, UDP ports, and target IP address.
6. Flash the ESP32 with `idf.py flash monitor` or `idf.py flash` followed by `idf.py monitor`.
7. Reflash only when hardware wiring, Wi-Fi, IP, or port values change.

## Reference guide

The original VCE Bicycle Interface guide is here:

https://vce.readthedocs.io/en/latest/extension_guide/bike-interface.html#bicycleinterface-extension

Use that guide for the full ESP-IDF installation and permission setup details.

## Notes

- The thesis version of the firmware is in [adc2udp.c].
- The repository root README provides the project-level overview and VR setup steps.
