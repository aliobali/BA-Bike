UDP tools
=========

This folder contains small helper scripts for testing the Bike project.

udp_fake_sender.py
------------------

Usage:

  python3 tools/udp_fake_sender.py <HEADSET_IP> [PORT]

Examples:

  python3 tools/udp_fake_sender.py 192.168.1.42 5005
  python3 tools/udp_fake_sender.py 127.0.0.1 5005  # test in Editor

The script sends messages in the form "speed,angle" (e.g. "3.50,-10.2") at ~10Hz.
