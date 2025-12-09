#!/usr/bin/env python3
"""
UDP fake sender for testing BikeUdpReceiver on Quest or Editor.

Usage:
  python3 tools/udp_fake_sender.py <HEADSET_IP> [PORT]

Examples:
  python3 tools/udp_fake_sender.py 192.168.1.42 5005
  python3 tools/udp_fake_sender.py 127.0.0.1 5005  # test in Editor

Sends lines of the form: "<speed>,<angle>" e.g. "3.50,-10.2" at ~10 Hz.
"""

import sys
import socket
import time
import random


def main():
    if len(sys.argv) < 2:
        print("Usage: udp_fake_sender.py <HEADSET_IP> [PORT]")
        return

    host = sys.argv[1]
    port = int(sys.argv[2]) if len(sys.argv) > 2 else 5005

    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    print(f"Sending UDP packets to {host}:{port} (CTRL-C to stop)")

    try:
        while True:
            speed = random.uniform(0.0, 5.0)  # m/s
            angle = random.uniform(-50.0, 50.0)  # degrees
            msg = f"{speed:.2f},{angle:.1f}"
            sock.sendto(msg.encode('utf-8'), (host, port))
            print('sent', msg)
            time.sleep(0.1)
    except KeyboardInterrupt:
        print('\nstopped')
    finally:
        sock.close()


if __name__ == '__main__':
    main()
