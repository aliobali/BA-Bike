// using UnityEngine;
// using rayzngames;

// /// <summary>
// /// Simplified adapter that connects UDP sensor input from the stationary bike to the RayzNGames BicycleVehicle.
// /// The person riding the stationary bike sends speed (m/s) and steering angle data via UDP.
// /// This adapter converts that data into motion for the virtual bike.
// /// </summary>
// [RequireComponent(typeof(BicycleVehicle))]
// public class RayzNGamesInputAdapter : MonoBehaviour
// {
//     private BicycleVehicle bicycle;
//     public BikeUdpReceiver udpReceiver;

//     [Header("UDP Input Settings")]
//     [Tooltip("Maximum speed expected from UDP (m/s) - used for normalization")]
//     public float maxExpectedSpeed = 10f;

//     [Tooltip("How quickly the bike accelerates from 0 to target speed (m/s²)")]
//     [Range(0.5f, 5f)]
//     public float accelerationRate = 2f;

//     [Tooltip("How quickly the bike decelerates when speed drops (m/s²)")]
//     [Range(1f, 10f)]
//     public float decelerationRate = 4f;

//     [Tooltip("Simulate acceleration from 0 when speed jumps from idle (prevents instant speed jump at startup)")]
//     public bool simulateStartupAcceleration = true;

//     [Tooltip("Speed threshold to detect 'idle' state (m/s)")]
//     [Range(0f, 2f)]
//     public float idleThreshold = 0.5f;

//     [Tooltip("Speed threshold to detect 'active' pedaling (m/s)")]
//     [Range(0.5f, 5f)]
//     public float activeThreshold = 1.0f;

//     [Tooltip("Automatically brake when speed drops below threshold")]
//     public bool autoBrakeOnSlowdown = true;
//     [Range(0f, 0.5f)]
//     public float autoBrakeThreshold = 0.1f;

//     // Properties for HUD and diagnostics
//     public float CurrentSpeed => bicycle != null ? bicycle.currentSpeed : 0f;
//     public float CurrentSteeringAngle => bicycle != null ? bicycle.currentSteeringAngle : 0f;
//     public float MoveInput { get; private set; }
//     public float TurnInput { get; private set; }

//     private float lastVerticalInput = 0f;

//     void Awake()
//     {
//         bicycle = GetComponent<BicycleVehicle>();
//         if (bicycle == null)
//         {
//             Debug.LogError("RayzNGamesInputAdapter requires a BicycleVehicle component!");
//         }
//         if (udpReceiver == null)
//         {
//             Debug.LogError("RayzNGamesInputAdapter requires a BikeUdpReceiver reference!");
//         }
//     }

//     void Start()
//     {
//         if (bicycle != null)
//         {
//             bicycle.InControl(true);
//         }
//     }

//     void Update()
//     {
//         if (bicycle == null || udpReceiver == null) return;

//         // Read speed and steering from UDP receiver (sensor data from stationary bike)
//         float speed = udpReceiver.Speed; // m/s
//         float steeringAngleDeg = udpReceiver.SteeringAngleDegrees; // degrees

//         // Convert speed (m/s) to vertical input (0-1 range for forward motion)
//         float targetVerticalInput = Mathf.Clamp01(speed / maxExpectedSpeed);
        
//         // Simulate startup acceleration: if speed jumps from idle to active, reset to 0
//         // This makes the bike feel like it's responding to your pedaling effort naturally
//         if (simulateStartupAcceleration && lastVerticalInput < idleThreshold && targetVerticalInput > activeThreshold)
//         {
//             lastVerticalInput = 0f;  // Reset to 0 and accelerate naturally toward target
//         }
        
//         // Apply realistic acceleration/deceleration physics (time-based, not frame-based)
//         // This simulates inertia - gradual speedup instead of instant jump
//         float accelRate = (targetVerticalInput > lastVerticalInput) ? accelerationRate : decelerationRate;
//         float verticalInput = Mathf.MoveTowards(lastVerticalInput, targetVerticalInput, accelRate * Time.deltaTime);
//         lastVerticalInput = verticalInput;

//         // Convert steering angle to horizontal input (-1 to 1)
//         float maxSteeringAngle = 50f; // Match BicycleVehicle's default max steering
//         float horizontalInput = Mathf.Clamp(steeringAngleDeg / maxSteeringAngle, -1f, 1f);

//         // Apply inputs to bicycle physics
//         bicycle.verticalInput = verticalInput;
//         bicycle.horizontalInput = horizontalInput;

//         // Auto-braking: engage brakes when speed drops below threshold
//         if (autoBrakeOnSlowdown && verticalInput < autoBrakeThreshold)
//         {
//             bicycle.braking = true;
//         }
//         else
//         {
//             bicycle.braking = false;
//         }

//         // Store for HUD and diagnostics
//         MoveInput = verticalInput;
//         TurnInput = horizontalInput;
//     }
// }
