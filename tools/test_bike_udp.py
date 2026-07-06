#!/usr/bin/env python3
"""
UDP test sender for the VCE bicycle Unity prototype.

Simulates the two live input streams of the physical bicycle setup:

  speed    -> UDP :4022  ticks per second (TPS), as sent by the ESP32
                         adc2udp firmware 
  steering -> UDP :5005  normalized handlebar angle in [-1.0, +1.0],
                         as sent by the BicycleTelemetry phone app

The ESP32 transmits the wheel tick rate (TPS), not m/s. Unity converts:

    v [m/s] = TPS * (pi * d_wheel) / n_ticks
    with d_wheel = 0.6 m and n_ticks = 18  ->  1 m/s = ~9.55 TPS

The tests below are written in m/s for readability and converted to TPS
on send, so the wire format matches the real firmware exactly.

Note: simulated packets are clean and regularly timed; they do not
reproduce the noise or irregular timing of the real sensors (see thesis
Sec. 4.6). Final parameter tuning must be done with the physical bicycle.

Usage:
    python3 test_bike_udp.py
"""

import math
import socket
import time

# UDP receiver (Unity) address
UNITY_HOST = "127.0.0.1"  # set to the receiver's LAN IP for remote/HMD tests
SPEED_PORT = 4022
STEERING_PORT = 5005

# Speed-sensor geometry. Must match the Unity receiver calibration
# (BikeUdpReceiver: ticksPerRotation, sensorWheelDiameter).
TICKS_PER_ROTATION = 18
SENSOR_WHEEL_DIAMETER = 0.6  # m
TPS_PER_MS = TICKS_PER_ROTATION / (math.pi * SENSOR_WHEEL_DIAMETER)  # ~9.55 TPS per m/s


def send_udp(message, port):
    """Send a single UDP datagram to the Unity receiver."""
    try:
        sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        sock.sendto(str(message).encode(), (UNITY_HOST, port))
        sock.close()
    except Exception as e:
        print(f"Error sending UDP: {e}")


def send_speed_ms(v_ms):
    """Send a speed given in m/s as the TPS value the ESP32 would emit."""
    send_udp(f"{v_ms * TPS_PER_MS:.2f}", SPEED_PORT)


def send_steering(norm):
    """Send a normalized steering value in [-1.0, +1.0]."""
    send_udp(f"{norm:.2f}", STEERING_PORT)


def fmt_speed(v_ms):
    """Format a speed for console output, showing both m/s and TPS."""
    return f"{v_ms:4.1f} m/s ({v_ms * TPS_PER_MS:5.1f} TPS)"


def test_simple_acceleration():
    """Test 1: Simple acceleration and braking"""
    print("\n=== Test 1: Simple Acceleration ===")
    speeds = [0, 1, 2, 3, 4, 5, 5, 5, 4, 3, 2, 1, 0]

    for speed in speeds:
        send_speed_ms(speed)
        print(f"Speed: {fmt_speed(speed)}")
        time.sleep(0.2)


def test_steering():
    """Test 2: Steering input"""
    print("\n=== Test 2: Steering ===")
    angles = [0, 0.2, 0.4, 0.6, 0.4, 0.2, 0, -0.2, -0.4, -0.6, -0.4, -0.2, 0]

    for angle in angles:
        send_steering(angle)
        print(f"Steering: {angle:+.1f}")
        time.sleep(0.2)


def test_realistic_ride():
    """Test 3: Simulate a realistic bike ride"""
    print("\n=== Test 3: Realistic Bike Ride ===")
    print("Simulating: acceleration -> cruise -> curves -> deceleration\n")

    sequence = [
        # Acceleration phase
        (1.0, 0.0, "Accelerating..."),
        (2.0, 0.0, ""),
        (3.0, 0.0, ""),
        (4.0, 0.0, ""),
        (5.0, 0.0, "Cruising speed reached"),

        # Cruising with curves
        (5.0, 0.3, "Slight left turn"),
        (5.0, 0.5, "Left turn"),
        (5.0, 0.3, "Straightening"),
        (5.0, -0.3, "Slight right turn"),
        (5.0, -0.5, "Right turn"),
        (5.0, -0.3, "Straightening"),
        (5.0, 0.0, "Straight ahead"),

        # Deceleration
        (4.0, 0.0, "Slowing down..."),
        (3.0, 0.0, ""),
        (2.0, 0.0, ""),
        (1.0, 0.0, ""),
        (0.0, 0.0, "Stopped"),
    ]

    for speed, steering, description in sequence:
        send_speed_ms(speed)
        send_steering(steering)

        desc_str = f" - {description}" if description else ""
        print(f"Speed: {fmt_speed(speed)}, Steering: {steering:+.1f}{desc_str}")
        time.sleep(0.3)


def test_continuous_send():
    """Test 4: Continuous data stream (like real sensors)"""
    print("\n=== Test 4: Continuous Data Stream (30 seconds) ===")
    print("Sending continuous sine wave motion...\n")

    start_time = time.time()
    while time.time() - start_time < 30:
        elapsed = time.time() - start_time

        # Sine wave for smooth motion
        speed = 3.0 + 2.0 * math.sin(elapsed)  # 1-5 m/s
        steering = 0.6 * math.sin(elapsed * 0.7)  # -0.6 to 0.6

        send_speed_ms(speed)
        send_steering(steering)

        print(f"[{elapsed:.1f}s] Speed: {fmt_speed(speed)}, Steering: {steering:+.2f}")
        time.sleep(0.1)

    print("\nContinuous test complete")


def test_out_and_back():
    """Test 5: Accelerate straight, turn, and drive back"""
    print("\n=== Test 5: Out-and-Back ===")
    print("Accelerate straight for 14s, then turn and head back\n")

    start_time = time.time()

    # Phase 1: accelerate straight for 14 seconds
    while True:
        elapsed = time.time() - start_time
        if elapsed >= 14:
            break
        # ramp speed from 0 to 6 m/s over 10 seconds, then hold
        speed = min(6.0, 0.6 * elapsed)
        steering = 0.0
        send_speed_ms(speed)
        send_steering(steering)
        print(f"[Phase 1 | {elapsed:4.1f}s] Speed: {fmt_speed(speed)}, Steering: {steering:+.2f}")
        time.sleep(0.1)

    # Phase 2: smooth turn for 6 seconds to point back (ease-in/out)
    turn_start = time.time()
    turn_duration = 6.0
    turn_strength = 1.0  # peak steering magnitude
    while True:
        elapsed = time.time() - turn_start
        if elapsed >= turn_duration:
            break
        norm = elapsed / turn_duration  # 0..1
        # Smooth bell curve: 0 -> 1 -> 0 using sin(pi * t)
        steering = turn_strength * math.sin(math.pi * norm)
        speed = 4.5  # maintain some speed while turning
        send_speed_ms(speed)
        send_steering(steering)
        print(f"[Phase 2 | {elapsed:4.1f}s] Speed: {fmt_speed(speed)}, Steering: {steering:+.2f}")
        time.sleep(0.1)

    # Phase 3: head back straight for 8 seconds, gently slowing
    back_start = time.time()
    while True:
        elapsed = time.time() - back_start
        if elapsed >= 8:
            break
        speed = max(1.5, 4.5 - 0.4 * elapsed)  # gradual slowdown
        steering = 0.0
        send_speed_ms(speed)
        send_steering(steering)
        print(f"[Phase 3 | {elapsed:4.1f}s] Speed: {fmt_speed(speed)}, Steering: {steering:+.2f}")
        time.sleep(0.1)

    print("\nOut-and-back test complete")


def print_menu():
    """Print test menu"""
    print(f"""
╔══════════════════════════════════════════════════════════════╗
║                      UDP Test Suite                          ║
║                                                              ║
║  Send test sensor data to the virtual bike system            ║
║  Make sure Unity is running with BikeUdpReceiver listening   ║
╚══════════════════════════════════════════════════════════════╝

Select a test:
  1. Simple Acceleration (test speed control)
  2. Steering Input (test steering control)
  3. Realistic Bike Ride (complete ride simulation)
  4. Continuous Data Stream (30-second sine wave)
  5. Out-and-Back (accelerate, turn, return)
  Q. Quit

Notes:
  - Speeds are shown in m/s and sent as TPS (m/s x ~{TPS_PER_MS:.2f}),
    matching the ESP32 adc2udp wire format on port {SPEED_PORT}
  - Steering range: -1.0 to +1.0 (normalized angle) on port {STEERING_PORT}
  - Messages are plain numbers; the receiver also accepts labeled
    forms (e.g., "tps:47.7", "steer:0.20")
  - Watch the debug HUD in Unity to see sensor data and bike response
    """)


def main():
    print("╔════════════════════════════════════════════════════════╗")
    print("║        UDP Test Script - Python Edition                ║")
    print("║                                                        ║")
    print(f"║  Target: {UNITY_HOST}:{SPEED_PORT}/{STEERING_PORT}                  ║")
    print("║  Make sure Unity is running!                          ║")
    print("╚════════════════════════════════════════════════════════╝\n")

    while True:
        print_menu()
        choice = input("\nEnter choice (1-5 or Q): ").strip().upper()

        if choice == "Q":
            print("Exiting...")
            break
        elif choice == "1":
            test_simple_acceleration()
        elif choice == "2":
            test_steering()
        elif choice == "3":
            test_realistic_ride()
        elif choice == "4":
            test_continuous_send()
        elif choice == "5":
            test_out_and_back()
        else:
            print("Invalid choice, please try again")

        input("\nPress Enter to continue...")


if __name__ == "__main__":
    try:
        main()
    except KeyboardInterrupt:
        print("\n\nInterrupted")