using TMPro;
using UnityEngine;

public class HUDManager : MonoBehaviour
{
    public TMP_Text hudText;
    public BikeController bike;

    void Update()
    {
        if (bike == null)
        {
            hudText.text = "Bike reference missing.";
            return;
        }

        float speed = bike.CurrentSpeed;             // m/s
        float speedKmh = speed * 3.6f;               // km/h
        float steeringAngle = bike.CurrentSteeringAngle; // degrees
        float moveInput = bike.MoveInput;
        float turnInput = bike.TurnInput;
        string inputSource = bike.UsingUdpInput ? "UDP (speed,angle)" : "Keyboard/Gamepad";

        hudText.text =
            $"Speed: {speed:F2} m/s ({speedKmh:F1} km/h)\n" +
            $"Steering Angle: {steeringAngle:F1}°\n" +
            $"Move Input: {moveInput:F2}\n" +
            $"Turn Input: {turnInput:F2}\n" +
            $"Input: {inputSource}\n" +
            $"(W/S: accelerate/brake, A/D: steer)";
    }
}
