using System;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class BikeUdpReceiver : MonoBehaviour
{
    [Tooltip("UDP port to listen on for bike data (speed,angle).")]
    public int udpPort = 5005;

    [Tooltip("Log every packet to the console for debugging.")]
    public bool logPackets = false;

    private UdpClient client;
    private IPEndPoint endpoint;

    // Latest parsed values (speed in m/s, steering angle in degrees).
    public float Speed => latestSpeed;
    public float SteeringAngle => latestSteeringAngle;

    private volatile float latestSpeed;
    private volatile float latestSteeringAngle;

    void Start()
    {
        try
        {
            client = new UdpClient(udpPort);
            endpoint = new IPEndPoint(IPAddress.Any, udpPort);
            client.BeginReceive(OnReceive, null);
            Debug.Log($"BikeUdpReceiver listening on UDP {udpPort}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"BikeUdpReceiver failed to bind UDP {udpPort}: {ex.Message}");
        }
    }

    void OnDestroy()
    {
        client?.Close();
        client = null;
    }

    private void OnReceive(IAsyncResult ar)
    {
        if (client == null) return;

        try
        {
            byte[] data = client.EndReceive(ar, ref endpoint);
            string msg = Encoding.UTF8.GetString(data).Trim();

            // Expect: "speed,angle" e.g. "3.5,-10.2"
            string[] parts = msg.Split(',');
            if (parts.Length >= 2)
            {
                if (float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var s))
                {
                    latestSpeed = s;
                }
                if (float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var a))
                {
                    latestSteeringAngle = a;
                }

                if (logPackets)
                {
                    Debug.Log($"BikeUdpReceiver {endpoint} -> speed {latestSpeed:F2} m/s, angle {latestSteeringAngle:F1}°");
                }
            }
            else if (logPackets)
            {
                Debug.LogWarning($"BikeUdpReceiver received malformed packet: '{msg}'");
            }
        }
        catch (ObjectDisposedException)
        {
            return; // socket closed on destroy
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"BikeUdpReceiver receive error: {ex.Message}");
        }

        try
        {
            client?.BeginReceive(OnReceive, null);
        }
        catch { }
    }
}
