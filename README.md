# Real-Time Bicycle Input Pipeline for the Virtual Cycling Environment (VCE)

Bachelor thesis of Ali Obali, TU Berlin (HCI, Faculty IV), 2026.

Thesis: "Real-Time Integration of a Physical Bicycle into a Virtual Cycling Environment and Its Effect on Users"

This repository contains the Unity VR prototype and the supporting test tool for a real-time bicycle input pipeline. The setup maps wheel-sensor input and steering telemetry into virtual bicycle motion.

	IR sensor -> ESP32 -> UDP :4022 ┐
								    ├-> BikeUdpReceiver.cs -> SimpleBikeController.cs -> Rigidbody + XR rig
	Phone app ----------> UDP :5005 ┘

## Repository layout

| Path | Contents |
|---|---|
| `unity/` | Unity project (Unity 6000.2.6f2) |
| `unity/Assets/Scripts/` | Unity scripts for the UDP receiver and bike controller |
| `unity/Assets/Resources/Environment/Vehicles/` | Vehicle prefabs and mesh assets used in the scene |
| `tools/` | UDP test script for simulating the Unity-facing streams |
| `firmware/` | ESP32 bicycle-interface source and firmware setup notes |

## System Overview

The runtime pipeline is:

1. The IR wheel sensor produces tick pulses.
2. The ESP32 converts those ticks into UDP traffic on port `4022`.
3. The phone app sends steering telemetry on port `5005`.
4. Unity receives both streams through `BikeUdpReceiver.cs`.
5. `SimpleBikeController.cs` maps the data to the rigidbody and XR rig.

## Requirements

Hardware:

- Stationary bicycle with an IR wheel sensor.
- ESP32 development board.
- Android phone with the BicycleTelemetry app.
- 2.4 GHz Wi-Fi access point shared by all communicating devices.
- VR headset for the Unity prototype.
- PC with DisplayPort access to the graphics card for the headset connection.

Software:

- Unity 6000.x.
- Python 3.x.
- ESP-IDF / VCE Bicycle Interface build environment for reflashing the ESP32.
- Wireshark for UDP diagnostics.

## Quickstart

1. Open the Unity project in `unity/` and load the main scene.
2. Connect the ESP32 to power before starting the live system.
3. Put the phone in the handlebar holder.
4. Make sure the steering app can join the same Wi-Fi network as the ESP32 and the PC.
5. Start Unity and press Play.
6. If you want to test without hardware, run `python3 tools/test_bike_udp.py`.

## ESP32 Reflash and Configuration

Firmware setup, cloning instructions, and the `adc2udp.c` replacement workflow are documented in [firmware/README.md](firmware/README.md). That page also points back to the VCE Bicycle Interface guide.

## VR Setup

Before starting the headset flow:

1. Connect the ESP32 to a power source.
2. Put the phone in the holder on the handlebar.
3. Obtain the Wi-Fi and phone password from Esteli Garcia if you do not already have it.

To start the VR setup:

1. Connect the Vive headset to the PC with the DisplayPort cable to the graphics card.
2. Open Vive Hub, SteamVR, and Unity on the PC.
3. Sit on the bicycle and adjust the safety boundaries while facing forward. This sets the simulation center and helps with correct bike and hand-tracking placement.
4. Connect the cable to the headset while you are seated.
5. Activate DisplayPort mode so the SteamVR interface opens.
6. Start the car simulation SUMO script if traffic integration is needed.
7. Start Play mode in Unity.
8. If the position is slightly off, move the bike or the physical setup a little, but avoid recentering unless needed because recentering can break hand tracking.
9. If the alignment is too far off, close SteamVR, adjust the boundaries in Vive Hub settings, and repeat the setup.
10. Before pressing Play, make sure the required GameObjects and Inspector references are assigned in the Unity scene. Bike should have the SimpleBikeController script and Network/BikeUdpReceiver should have BikeUdpReceiver.

## Network and Packet Checks

All communicating devices must share the same 2.4 GHz Wi-Fi network.

To verify the UDP streams, use Wireshark with this display filter:

```text
udp.port == 5005 || udp.port == 4022
```

If packets appear with that filter applied, the network path is working and the system is running.

If packets do not appear, check the following first:

- Windows Defender Firewall or other local firewall rules.
- Whether the ESP32 is powered and connected to Wi-Fi.
- Whether the phone app is sending telemetry to UDP port `5005`.
- Whether Unity is listening on the expected port and host.

## Relationship to the original VCE

This repository focuses on the bicycle-input pipeline and the Unity VR prototype. The thesis documentation in this repo explains the integration and the surrounding evaluation context.

## Notes

- `tools/test_bike_udp.py` is the helper script for sending test UDP traffic into Unity.
- The firmware notes and exact ESP32 configuration live in [firmware/README.md](firmware/README.md).
- `unity/Scenes/PanelScene.unity` is the Unity scene version used as the expert-panel reference.

## Citation

Ali Obali, "Real-Time Integration of a Physical Bicycle into a Virtual Cycling Environment and Its Effect on Users," Bachelor thesis, TU Berlin, 2026.
