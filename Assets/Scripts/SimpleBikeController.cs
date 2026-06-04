using UnityEngine;

/// <summary>
/// Simple bike controller that moves based on UDP sensor data.
/// </summary>
public class SimpleBikeController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("UDP receiver to get sensor data from stationary bike")]
    public BikeUdpReceiver udpReceiver;

    [Tooltip("XR Origin to move with the bike (position only, not rotation)")]
    public Transform xrOrigin;

   [Tooltip("Camera Offset child inside XR Origin - drag the 'Camera Offset' child object here")]
    public Transform cameraOffset;

    [Tooltip("Bike visual mesh/model to rotate for steering")]
    public Transform bikeVisuals;

    [Tooltip("Handlebar transform to rotate (optional)")]
    public Transform handlebar;

    [Tooltip("Front wheel transform for rotation (optional)")]
    public Transform frontWheel;

    [Tooltip("Rear wheel transform for rotation (optional)")]
    public Transform rearWheel;

    [Header("Movement Settings")]
    [Tooltip("Maximum rotation rate per second (degrees) to prevent VR sickness")]
    [Range(10f, 120f)]
    public float maxRotationRate = 35f;

    [Tooltip("Maximum handlebar rotation angle in degrees")]
    public float maxHandlebarAngle = 45f;

    [Tooltip("Distance between front and rear axle in meters - main steering tuning knob")]
    [Range(0.5f, 2.0f)]
    public float wheelbase = 1.0f;

    [Tooltip("How quickly the bike accelerates from 0 to target speed (m/s²)")]
    [Range(0.5f, 5f)]
    public float accelerationRate = 2f;  // Tuned for natural acceleration feel

    [Tooltip("How quickly the bike decelerates when speed drops (m/s²)")]
    [Range(1f, 10f)]
    public float decelerationRate = 8f;
    
    [Tooltip("Faster deceleration when user actively brakes (stops pedaling suddenly)")]
    [Range(5f, 40f)]
    public float activeBrakeRate = 30f;
    
    [Tooltip("Threshold for detecting active braking (m/s difference)")]
    [Range(0.1f, 5f)]
    public float brakingThreshold = 0.5f;

    [Tooltip("Smooth out rotation changes (0 = instant, higher = more lag)")]
    public float rotationSmoothTime = 0.15f;

    [Header("Resistance Settings")]
    [Tooltip("Quadratic air resistance coefficient - higher values plateau speed sooner. Start at 0.02 and tune up if bike gets too fast")]
    [Range(0.0f, 0.1f)]
    public float resistanceCoefficient = 0.01f;

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
    public float speedScale = 1.0f;

    [Tooltip("Absolute cap on movement speed (m/s)")]
    public float maxSpeed = 10f;

    [Header("Ground Detection")]
    [Tooltip("Desired eye height above ground")]
    public float desiredEyeHeight = 0f;
    private float currentSpeed = 0f;
    private float currentRotation = 0f;
    private float wheelRotation = 0f;
    // private Vector3 lastPosition;  // UNUSED - was for velocity calculations in older code
    private Quaternion handlebarInitialRotation;
    
    // Store initial positions for recentering
    private Vector3 initialBikePosition;
    private Quaternion initialBikeRotation;
    private Vector3 initialXRPosition;
    private Quaternion initialXRRotation;
    
    // Store the offset between bike and camera (for saddle locking)
    private Vector3 xrOffsetFromBike;

    // Cache wheel initial rotations to avoid Quaternion-to-Euler ambiguity
    private Vector3 frontWheelInitialEuler;
    private Vector3 rearWheelInitialEuler;

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
        
        // Cache wheel initial Euler angles to avoid Quaternion-to-Euler ambiguity when rotating
        if (frontWheel != null)
            frontWheelInitialEuler = frontWheel.localEulerAngles;
        if (rearWheel != null)
            rearWheelInitialEuler = rearWheel.localEulerAngles;
        
        // lastPosition = transform.position;  // UNUSED
        
        // Use desired eye height directly
       if (xrOrigin != null)
        {
            xrOrigin.position = new Vector3(xrOrigin.position.x, desiredEyeHeight, xrOrigin.position.z);
        }
        
        // Store initial positions for recentering
        initialBikePosition = transform.position;
        initialBikeRotation = transform.rotation;
        initialXRPosition = xrOrigin != null ? xrOrigin.position : Vector3.zero;
        initialXRRotation = xrOrigin != null ? xrOrigin.rotation : Quaternion.identity;
        
        // Calculate offset between bike and camera (for saddle locking)
        if (xrOrigin != null)
        {
            xrOffsetFromBike = initialXRPosition - initialBikePosition;
        }

        // Use cameraOffset if assigned, otherwise fall back to xrOrigin
        Transform movementTarget = cameraOffset != null ? cameraOffset : xrOrigin;
        if (movementTarget != null)
        {
            initialXRPosition = movementTarget.position;
            initialXRRotation = movementTarget.rotation;
            xrOffsetFromBike = movementTarget.position - initialBikePosition;
        }
    }

    void Update()
    {
        
        if (udpReceiver == null) return;

        // Get sensor data
        float rawSpeed = udpReceiver.Speed;
        float steeringNormalized = udpReceiver.SteeringNormalizedDeadzoned;

        // DEBUG: Verify TPS to m/s conversion
        //Debug.Log($"[SPEED] TPS: {rawSpeed / (Mathf.PI * 0.6f) * 18f:F2} → m/s: {rawSpeed:F2}");

        // Speed with air resistance
        float targetSpeed = rawSpeed * speedScale;
        
        // Apply air resistance (drag increases with speed squared)
        float resistance = resistanceCoefficient * targetSpeed * targetSpeed;
        targetSpeed = Mathf.Max(0, targetSpeed - resistance);
        targetSpeed = Mathf.Min(targetSpeed, maxSpeed);
        
        // Apply realistic acceleration/deceleration physics (time-based)
        // This simulates inertia - gradual speedup instead of instant jump
        float accelRate;
        if (targetSpeed > currentSpeed)
        {
            // Accelerating
            accelRate = accelerationRate;
        }
        else if (targetSpeed < currentSpeed)
        {
            // Decelerating - detect if this is active braking or coasting
            float speedDrop = currentSpeed - targetSpeed;
            if (speedDrop > brakingThreshold)
            {
                // Large speed drop = user actively braked, use faster deceleration
                Debug.Log($"[ACTIVE BRAKE] Current: {currentSpeed:F2}, Target: {targetSpeed:F2}, Drop: {speedDrop:F2}, Rate: {activeBrakeRate}");
                accelRate = activeBrakeRate;
            }
            else
            {
                // Small speed drop = coasting naturally, use slower deceleration
                accelRate = decelerationRate;
            }
        }
        else
        {
            // Speed matches target
            accelRate = decelerationRate;
        }
        
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, accelRate * Time.deltaTime);

        // Steering with radius based physics
        float steeringAngleRad = steeringNormalized * (maxHandlebarAngle * Mathf.Deg2Rad);
        
        float turningRadius = float.MaxValue;
        if (Mathf.Abs(steeringAngleRad) > 0.001f)
        {
            // Bicycle Ackermann steering geometry: turning radius depends on wheelbase and speed
            turningRadius = (wheelbase + currentSpeed * 0.3f) / Mathf.Sin(Mathf.Abs(steeringAngleRad));
        }
        
        float targetRotationRate = 0f;
        if (turningRadius < float.MaxValue && currentSpeed > 0.05f)
        {
            // Angular velocity = linear velocity / radius (in radians/sec, convert to degrees)
            targetRotationRate = (currentSpeed / turningRadius) * Mathf.Rad2Deg * Mathf.Sign(steeringNormalized);
        }
        
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

       // Move Camera Offset instead of XR Origin root - keeps hand tracking aligned
        Transform movementTarget = cameraOffset != null ? cameraOffset : xrOrigin;
        if (movementTarget != null)
        {
            Vector3 rotatedOffset = transform.rotation * xrOffsetFromBike;
            movementTarget.position = transform.position + rotatedOffset;
            movementTarget.rotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);
        }

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
            frontWheel.localRotation = Quaternion.Euler(
                wheelRotation,
                frontWheelInitialEuler.y,
                frontWheelInitialEuler.z);
        }
        if (rearWheel != null)
        {
            rearWheel.localRotation = Quaternion.Euler(
                wheelRotation,
                rearWheelInitialEuler.y,
                rearWheelInitialEuler.z);
        }
    }

    /// <summary>
    /// Reset bike and camera to starting position and rotation.
    /// Call this when user presses a button to recenter (e.g., pause menu, specific controller button).
    /// </summary>
    public void RecenterBikeAndCamera()
    {
        Debug.Log("[SimpleBikeController] Recentering bike and camera to start position...");
        
        // Reset bike transform
        transform.position = initialBikePosition;
        transform.rotation = initialBikeRotation;
        
        Transform movementTarget = cameraOffset != null ? cameraOffset : xrOrigin;
        if (movementTarget != null)
        {
            movementTarget.position = initialXRPosition;
            movementTarget.rotation = initialXRRotation;
        }
                
        // Reset movement state
        // lastPosition = initialBikePosition;  // UNUSED
        currentSpeed = 0f;
        currentRotation = 0f;
        wheelRotation = 0f;
        
        Debug.Log("[SimpleBikeController] Recenter complete!");
    }

    // Public properties for HUD
    public float CurrentSpeed => currentSpeed;
}
