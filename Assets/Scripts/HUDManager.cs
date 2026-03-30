using TMPro;
using UnityEngine;

/// <summary>
/// Displays real-time bike telemetry on screen.
/// Shows speed, steering angle, and sensor input status.
/// </summary>
public class HUDManager : MonoBehaviour
{
    [Tooltip("TextMeshPro text component to display HUD information")]
    public TMP_Text hudText;

    [Header("References")]
    [Tooltip("UDP receiver to get sensor data from stationary bike")]
    public BikeUdpReceiver udpReceiver;
    
    [Tooltip("Simple bike controller (non-physics)")]
    public SimpleBikeController bikeController;

    [Tooltip("Show debug information on HUD")]
    public bool showDebugInfo = true;

    void Start()
    {
        if (hudText == null)
        {
            Debug.LogWarning("HUDManager: hudText is not assigned");
        }
        if (udpReceiver == null)
        {
            Debug.LogWarning("HUDManager: udpReceiver is not assigned");
        }
        if (bikeController == null)
        {
            bikeController = FindFirstObjectByType<SimpleBikeController>();
        }
    }

    void Update()
    {
        if (hudText == null) return;

        if (udpReceiver == null)
        {
            hudText.text = "ERROR: UDP Receiver not assigned!";
            return;
        }

        // Get data from UDP sensors
        float speed = udpReceiver.Speed;
        float speedKmh = speed * 3.6f;
        float steeringAngle = udpReceiver.SteeringAngleDegrees;

        // Get data from bike controller
        float bikeSpeed = bikeController != null ? bikeController.CurrentSpeed : 0f;
        float bikeSpeedKmh = bikeSpeed * 3.6f;

        // Display telemetry
        string info = $"<b>BIKE TELEMETRY</b>\n\n";
        info += $"<b>Sensor Input (from stationary bike):</b>\n";
        info += $"  Speed: {speed:F2} m/s ({speedKmh:F1} km/h)\n";
        info += $"  Steering: {steeringAngle:F1}°\n\n";
        info += $"<b>Virtual Bike Response:</b>\n";
        info += $"  Speed: {bikeSpeed:F2} m/s ({bikeSpeedKmh:F1} km/h)\n";


        if (showDebugInfo && udpReceiver != null)
        {
            info += $"\n<b>Debug Info:</b>\n";
            info += $"  Speed Raw: {udpReceiver.Speed:F2}\n";
            info += $"  Steering Normalized: {udpReceiver.SteeringNormalized:F2}\n";
            info += $"  Steering w/ Deadzone: {udpReceiver.SteeringNormalizedDeadzoned:F2}";
        }

        hudText.text = info;
    }
}
