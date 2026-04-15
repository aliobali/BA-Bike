using UnityEngine;

/// <summary>
/// Simple non-physics bike controller that moves based on UDP sensor data.
/// Moves the bike transform directly without using WheelColliders or Rigidbody physics.
/// </summary>
public class SimpleBikeController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("UDP receiver to get sensor data from stationary bike")]
    public BikeUdpReceiver udpReceiver;

    [Tooltip("XR Origin to move with the bike (position only, not rotation)")]
    public Transform xrOrigin;

    [Tooltip("Bike visual mesh/model to rotate for steering")]
    public Transform bikeVisuals;

    [Tooltip("Handlebar transform to rotate (optional)")]
    public Transform handlebar;

    [Tooltip("Front wheel transform for rotation (optional)")]
    public Transform frontWheel;

    [Tooltip("Rear wheel transform for rotation (optional)")]
    public Transform rearWheel;

    [Header("Movement Settings")]
    [Tooltip("How fast the bike rotates based on steering input")]
    public float rotationSpeed = 30f;

    [Tooltip("Maximum rotation rate per second (degrees) to prevent VR sickness")]
    [Range(10f, 120f)]
    public float maxRotationRate = 60f;

    [Tooltip("Maximum handlebar rotation angle in degrees")]
    public float maxHandlebarAngle = 45f;

    [Tooltip("Smooth out speed changes")]
    public float speedSmoothTime = 0.4f;

    [Tooltip("Smooth out rotation changes (0 = instant, higher = more lag)")]
    public float rotationSmoothTime = 0.2f;

    [Header("Wheel Settings")]
    [Tooltip("Enable wheel rotation animation")]
    public bool enableWheelRotation = true;

    [Tooltip("Radius of the bike wheels in meters (affects rotation speed)")]
    public float wheelRadius = 0.34f;
    [Tooltip("Visual spin multiplier for wheels (1 = realistic circumference-based spin)")]
    [Range(0.1f, 2f)]
    public float wheelSpinMultiplier = 0.7f;

    [Header("Comfort Settings")]
    [Tooltip("Allow steering to rotate bike even when not pedaling")]
    public bool allowSteeringWithoutMovement = false;

    [Tooltip("Scale incoming speed (m/s) to tame motion")]
    public float speedScale = 0.5f;

    [Tooltip("Absolute cap on movement speed (m/s)")]
    public float maxSpeed = 5f;

    // Internal state
    private float currentSpeed = 0f;
    private float currentRotation = 0f;
    private float speedVelocity = 0f;
    private float wheelRotation = 0f;
    private Vector3 lastPosition;
    private Quaternion handlebarInitialRotation;

    void Start()
    {
        if (udpReceiver == null)
        {
            udpReceiver = FindFirstObjectByType<BikeUdpReceiver>();
            if (udpReceiver == null)
            {
                Debug.LogError("SimpleBikeController: BikeUdpReceiver not found!");
            }
        }
        
        // Cache initial handlebar rotation to use as baseline
        if (handlebar != null)
        {
            handlebarInitialRotation = handlebar.localRotation;
        }
        
        lastPosition = transform.position;
    }

    void Update()
    {
        if (udpReceiver == null) return;

        // Get sensor data
        float rawSpeed = udpReceiver.Speed;
        float steeringNormalized = udpReceiver.SteeringNormalizedDeadzoned;

        // Scale and cap speed for comfort, then smooth
        float targetSpeed = Mathf.Min(rawSpeed * speedScale, maxSpeed);
        currentSpeed = Mathf.SmoothDamp(currentSpeed, targetSpeed, ref speedVelocity, speedSmoothTime);

        // Calculate target rotation based on steering
        float targetRotationRate = steeringNormalized * rotationSpeed;
        targetRotationRate = Mathf.Clamp(targetRotationRate, -maxRotationRate, maxRotationRate);
        
        // Only allow steering if: 1) Bike is moving, OR 2) Steering without movement is enabled
        bool canSteer = (currentSpeed > 0.1f) || allowSteeringWithoutMovement;
        if (!canSteer)
        {
            targetRotationRate = 0f;
        }
        
        // Smoothly interpolate current rotation rate
        currentRotation = Mathf.Lerp(currentRotation, targetRotationRate, Time.deltaTime / Mathf.Max(0.01f, rotationSmoothTime));
        
        // Apply rotation to this transform (the bike platform)
        transform.Rotate(0, currentRotation * Time.deltaTime, 0);

        // Move forward based on speed
        Vector3 newPosition = transform.position + transform.forward * currentSpeed * Time.deltaTime;
        transform.position = newPosition;

        // Move AND rotate XR Origin with the bike
        if (xrOrigin != null)
        {
            // Update position
            Vector3 deltaPosition = transform.position - lastPosition;
            xrOrigin.position += deltaPosition;
            
            // Update rotation to match bike yaw (keep XR Origin facing bike's forward)
            xrOrigin.rotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);
        }
        lastPosition = transform.position;

        // Rotate bike visuals for steering effect (optional, keeps rider stable)
        if (bikeVisuals != null)
        {
            float visualTiltAngle = steeringNormalized * 5f; // Subtle lean
            bikeVisuals.localRotation = Quaternion.Euler(0, 0, -visualTiltAngle);
        }

        // Rotate handlebar
        if (handlebar != null)
        {
            float handlebarAngle = steeringNormalized * maxHandlebarAngle;
            handlebar.localRotation = handlebarInitialRotation * Quaternion.Euler(0, handlebarAngle, 0);
        }

        // Update wheel rotations
        if (enableWheelRotation)
        {
            UpdateWheels();
        }
    }

    void UpdateWheels()
    {
        // Only rotate wheels if bike is actually moving (avoid jitter when speed approaches 0)
        if (currentSpeed < 0.05f)
        {
            return;
        }

        // Calculate rotation based on speed and wheel radius
        // Circumference = 2 * PI * radius
        // Rotations per second = speed / circumference
        // Degrees per second = (speed * 360) / (2 * PI * radius)
        float circumference = 2f * Mathf.PI * wheelRadius;
        float degreesPerSecond = (currentSpeed * 360f) / circumference;
        degreesPerSecond *= wheelSpinMultiplier;
        
        // Accumulate rotation
        wheelRotation += degreesPerSecond * Time.deltaTime;
        
        // Wrap rotation to 0-360
        if (wheelRotation >= 360f) wheelRotation -= 360f;
        if (wheelRotation < 0f) wheelRotation += 360f;
        
        // Apply rotation to wheels
        if (frontWheel != null)
        {
            frontWheel.localRotation = Quaternion.Euler(wheelRotation, frontWheel.localRotation.eulerAngles.y, frontWheel.localRotation.eulerAngles.z);
        }
        if (rearWheel != null)
        {
            rearWheel.localRotation = Quaternion.Euler(wheelRotation, rearWheel.localRotation.eulerAngles.y, rearWheel.localRotation.eulerAngles.z);
        }
    }

    // Public properties for HUD
    public float CurrentSpeed => currentSpeed;
}
