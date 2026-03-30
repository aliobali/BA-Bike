import socket
import time
import math
import random

UDP_IP = "127.0.0.1"
UDP_PORT = 5005

# Simulation parameters
DT = 0.1              # time step in seconds (10 Hz)
MAX_SPEED = 6.0       # m/s ~ 21.6 km/h
RIDE_DURATION = 180   # seconds

def clamp(x, lo, hi):
    return max(lo, min(hi, x))

def simulate_speed(t):
    """
    Simple piecewise speed profile:
    - 0–10s: accelerate to MAX_SPEED
    - 10–70s: cruise with small variations
    - 70–85s: brake to near stop
    - 85–95s: slow roll
    - 95–140s: accelerate + second cruise
    - 140–160s: hills/variation
    - 160–end: gentle roll-out
    """
    if t < 10:
        # accelerate linearly
        speed = MAX_SPEED * (t / 10.0)
    elif t < 70:
        # cruise with slight breathing
        base = MAX_SPEED * 0.85
        osc = 0.5 * math.sin(0.1 * t)
        speed = base + osc
    elif t < 85:
        # braking
        f = (t - 70) / 15.0  # 0 → 1
        speed = MAX_SPEED * (1.0 - f)  # down to ~0
    elif t < 95:
        # slow roll at ~1 m/s
        speed = 1.0 + 0.2 * math.sin(0.5 * t)
    elif t < 140:
        # accelerate again and cruise a bit slower than before
        f = min(1.0, (t - 95) / 15.0)  # 0 → 1
        base = 4.5 * f
        osc = 0.4 * math.sin(0.08 * t)
        speed = base + osc
    elif t < 160:
        # "hill" – fluctuating speed
        base = 3.5 + 1.0 * math.sin(0.2 * t)
        speed = base
    else:
        # gentle roll to stop
        f = max(0.0, 1.0 - (t - 160) / 20.0)
        speed = 2.0 * f

    return max(0.0, speed)


def simulate_steering_angle(t):
    """
    Simulate steering in degrees:
    - slow general meandering
    - occasional stronger turns
    - small noise
    """
    # base gentle meander (like following a slightly curvy path)
    angle = 8.0 * math.sin(0.05 * t)

    # add a very slow drift
    angle += 4.0 * math.sin(0.015 * t + 1.0)

    # occasional stronger “turn sections”
    if 30 < t < 40 or 110 < t < 120:
        angle += 15.0 * math.sin(0.3 * t)

    # small random noise
    angle += random.gauss(0.0, 1.5)

    # clamp to reasonable steering range
    angle = clamp(angle, -30.0, 30.0)
    return angle


def main():
    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    print(f"Sending simulated bike data to {UDP_IP}:{UDP_PORT} ... (Ctrl+C to stop)")

    t = 0.0
    start = time.time()

    try:
        while t < RIDE_DURATION:
            speed = simulate_speed(t)
            angle = simulate_steering_angle(t)

            msg = f"{speed:.2f},{angle:.2f}"
            sock.sendto(msg.encode("utf-8"), (UDP_IP, UDP_PORT))

            # Also print to console so you see what's happening
            print(f"t={t:6.1f}s  speed={speed:5.2f} m/s  angle={angle:6.2f}°  -> '{msg}'")

            time.sleep(DT)
            t = time.time() - start

    except KeyboardInterrupt:
        print("\nStopped by user.")

    finally:
        sock.close()
        print("Socket closed.")


if __name__ == "__main__":
    main()

