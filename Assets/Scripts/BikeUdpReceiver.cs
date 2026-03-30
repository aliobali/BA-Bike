using System;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

/// <summary>
/// Receives speed and steering angle data via UDP from the stationary bike's sensors.
/// Listens on two separate UDP ports:
/// - Port 5005: Steering angle data
/// - Port 4022: Speed data (m/s)
/// </summary>
public class BikeUdpReceiver : MonoBehaviour
{
    [Header("UDP Configuration")]
    [Tooltip("UDP port to listen on for steering data (angle in degrees, normalized [-1,1])")]
    public int steeringUdpPort = 5005;

    [Tooltip("UDP port to listen on for speed data (m/s)")]
    public int speedUdpPort = 4022;

    [Tooltip("Show debug logs for received packets")]
    public bool logPackets = false;

    [Header("Input Processing")]
    [Tooltip("Deadzone threshold for steering (values smaller than this become zero)")]
    [Range(0f, 0.2f)]
    public float steeringDeadzone = 0.02f;

    // UDP listeners
    private UdpClient steeringClient;
    private UdpClient speedClient;
    private IPEndPoint steeringEndpoint;
    private IPEndPoint speedEndpoint;

    // Latest sensor values from stationary bike
    private volatile float latestSpeed = 0f;
    private volatile float latestSteeringNormalized = 0f;

    // Public properties for the input adapter
    public float Speed => latestSpeed;
    public float SteeringNormalized => latestSteeringNormalized;
    public float SteeringNormalizedDeadzoned => ApplyDeadzone(latestSteeringNormalized, steeringDeadzone);
    public float SteeringAngleDegrees => Mathf.Asin(Mathf.Clamp(SteeringNormalizedDeadzoned, -1f, 1f)) * Mathf.Rad2Deg;

    void Start()
    {
        try
        {
            StartListener(ref steeringClient, ref steeringEndpoint, steeringUdpPort, OnSteeringReceive, "steering");
            StartListener(ref speedClient, ref speedEndpoint, speedUdpPort, OnSpeedReceive, "speed");
        }
        catch (Exception ex)
        {
            Debug.LogError($"BikeUdpReceiver failed to start: {ex.Message}");
        }
    }

    void OnDestroy()
    {
        steeringClient?.Close();
        speedClient?.Close();
    }

    private void OnSteeringReceive(IAsyncResult ar)
    {
        if (steeringClient == null) return;

        try
        {
            byte[] data = steeringClient.EndReceive(ar, ref steeringEndpoint);
            string msg = Encoding.UTF8.GetString(data).Trim();
            ProcessSteeringMessage(msg);
        }
        catch (ObjectDisposedException) { return; }
        catch (Exception ex) { Debug.LogWarning($"BikeUdpReceiver steering error: {ex.Message}"); }

        try { steeringClient?.BeginReceive(OnSteeringReceive, null); }
        catch { }
    }

    private void OnSpeedReceive(IAsyncResult ar)
    {
        if (speedClient == null) return;

        try
        {
            byte[] data = speedClient.EndReceive(ar, ref speedEndpoint);
            string msg = Encoding.UTF8.GetString(data).Trim();
            ProcessSpeedMessage(msg);
        }
        catch (ObjectDisposedException) { return; }
        catch (Exception ex) { Debug.LogWarning($"BikeUdpReceiver speed error: {ex.Message}"); }

        try { speedClient?.BeginReceive(OnSpeedReceive, null); }
        catch { }
    }

    private void ProcessSteeringMessage(string msg)
    {
        if (string.IsNullOrWhiteSpace(msg)) return;

        // Try labeled format first: "steering:0.5" or "steer:-0.2"
        if (TryParseLabeledFloat(msg, out var label, out var value))
        {
            if (IsSteeringLabel(label))
            {
                latestSteeringNormalized = value;
                if (logPackets) Debug.Log($"[UDP Steering] {value:F3}");
                return;
            }
        }

        // Fall back to plain number
        if (float.TryParse(msg, NumberStyles.Float, CultureInfo.InvariantCulture, out var plainValue))
        {
            latestSteeringNormalized = plainValue;
            if (logPackets) Debug.Log($"[UDP Steering] {plainValue:F3}");
        }
    }

    private void ProcessSpeedMessage(string msg)
    {
        if (string.IsNullOrWhiteSpace(msg)) return;

        // Try labeled format first: "speed:3.5" or "spd:2.0"
        if (TryParseLabeledFloat(msg, out var label, out var value))
        {
            if (IsSpeedLabel(label))
            {
                latestSpeed = value;
                if (logPackets) Debug.Log($"[UDP Speed] {value:F2} m/s");
                return;
            }
        }

        // Fall back to plain number
        if (float.TryParse(msg, NumberStyles.Float, CultureInfo.InvariantCulture, out var plainValue))
        {
            latestSpeed = plainValue;
            if (logPackets) Debug.Log($"[UDP Speed] {plainValue:F2} m/s");
        }
    }

    private static bool TryParseLabeledFloat(string msg, out string label, out float value)
    {
        label = null;
        value = 0f;
        int sep = msg.IndexOf(':');
        if (sep <= 0 || sep >= msg.Length - 1) return false;
        label = msg.Substring(0, sep).Trim().ToLowerInvariant();
        string number = msg.Substring(sep + 1).Trim();
        return float.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    private static bool IsSpeedLabel(string label)
    {
        return label == "speed" || label == "spd" || label == "velocity";
    }

    private static bool IsSteeringLabel(string label)
    {
        return label == "steer" || label == "steering" || label == "angle";
    }

    private static float ApplyDeadzone(float value, float deadzone)
    {
        return Mathf.Abs(value) < deadzone ? 0f : value;
    }

    private void StartListener(ref UdpClient client, ref IPEndPoint endpoint, int port, AsyncCallback callback, string label)
    {
        if (port <= 0)
        {
            Debug.LogWarning($"BikeUdpReceiver {label} port {port} is invalid (must be > 0)");
            return;
        }

        try
        {
            client = new UdpClient(port);
            endpoint = new IPEndPoint(IPAddress.Any, port);
            client.BeginReceive(callback, null);
            Debug.Log($"[BikeUdpReceiver] Listening for {label} on UDP port {port}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[BikeUdpReceiver] Failed to bind {label} to UDP port {port}: {ex.Message}");
        }
    }
}
