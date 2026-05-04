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

    [Tooltip("Simulate acceleration from 0 when speed jumps from idle (prevents instant speed jump at startup)")]
    public bool simulateStartupAcceleration = true;

    [Tooltip("Speed threshold to detect 'idle' state (m/s)")]
    [Range(0f, 2f)]
    public float idleThreshold = 0.5f;

    [Tooltip("Speed threshold to detect 'active' pedaling (m/s)")]
    [Range(0.5f, 5f)]
    public float activeThreshold = 1.0f;

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
    
    [Tooltip("Automatically sync bike rotation with XR Origin when device recenters")]
    public bool autoSyncWithXRRecenter = true;

    [Header("Ground Detection")]
    [Tooltip("Auto-adjust XR Origin height based on ground at startup")]
    public bool autoAdjustHeightToGround = true;
    [Tooltip("Layer mask for ground detection")]
    public LayerMask groundLayer = -1;
    [Tooltip("Distance to raycast down for ground")]
    public float groundDetectionDistance = 5f;
    [Tooltip("Desired eye height above ground")]
    public float desiredEyeHeight = 0.3f;
    private float currentSpeed = 0f;
    private float currentRotation = 0f;
    private float wheelRotation = 0f;
    private Vector3 lastPosition;
    private Quaternion handlebarInitialRotation;
    
    // Store initial positions for recentering
    private Vector3 initialBikePosition;
    private Quaternion initialBikeRotation;
    private Vector3 initialXRPosition;
    private Quaternion initialXRRotation;
    
    // Track XR rotation to detect recenter
    private float lastXRYaw = 0f;
    
    // Store the offset between bike and camera (for saddle locking)
    private Vector3 xrOffsetFromBike;

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

        // Auto-adjust XR Origin height based on ground at startup
        // if (autoAdjustHeightToGround && xrOrigin != null)
        // {
        //     AdjustXRHeightToGround();
        // }
        
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
            lastXRYaw = xrOrigin.transform.eulerAngles.y;
        }
    }

    private void AdjustXRHeightToGround()
    {
        if (xrOrigin == null) return;

        // Raycast down from XR Origin to find ground
        // RaycastHit hit;
        // if (Physics.Raycast(xrOrigin.position, Vector3.down, out hit, groundDetectionDistance, groundLayer))
        // {
        //     // Ground found - adjust XR Origin height so eyes are at desired height above ground
        //     float groundY = hit.point.y;
        //     float targetXROriginY = groundY + desiredEyeHeight;
        //     
        //     xrOrigin.position = new Vector3(xrOrigin.position.x, targetXROriginY, xrOrigin.position.z);
        //     Debug.Log($"[SimpleBikeController] Ground at {groundY:F2}m. Adjusted XR Origin to Y={targetXROriginY:F2}m for {desiredEyeHeight}m eye height.");
        // }
        // else
        // {
        //     Debug.LogWarning($"[SimpleBikeController] No ground detected within {groundDetectionDistance}m. Using current height.");
        // }
    }

    void Update()
    {
        // Check if XR device has been recentered (sudden yaw change indicates recenter)
        if (autoSyncWithXRRecenter && xrOrigin != null)
        {
            float currentXRYaw = xrOrigin.transform.eulerAngles.y;
            
            // Detect recenter: significant yaw change that looks like a reset (not gradual turning)
            float yawDelta = Mathf.Abs(Mathf.DeltaAngle(lastXRYaw, currentXRYaw));
            if (yawDelta > 30f && currentXRYaw < 5f)  // Large jump to near 0 = recenter detected
            {
                Debug.Log($"[SimpleBikeController] Detected device recenter. Syncing bike...");
                RecenterBikeAndCamera();
            }
            lastXRYaw = currentXRYaw;
        }
        
        if (udpReceiver == null) return;

        // Get sensor data
        float rawSpeed = udpReceiver.Speed;
        float steeringNormalized = udpReceiver.SteeringNormalizedDeadzoned;

        // Scale and cap speed for comfort
        float targetSpeed = Mathf.Min(rawSpeed * speedScale, maxSpeed);
        
        // Simulate startup acceleration: if speed jumps from idle to active, reset to 0
        // This makes the bike feel like it's responding to your pedaling effort naturally
        if (false && simulateStartupAcceleration && currentSpeed < idleThreshold && targetSpeed > activeThreshold)
        {
            currentSpeed = 0f;  // Reset to 0 and accelerate naturally toward target
        }
        
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
                Debug.Log($"[ACTIVE BRAKE] Current: {currentSpeed:F2}, Target: {targetSpeed:F2}, Drop: {speedDrop:F2}, Rate: 15");
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

        // Lock XR Origin to bike saddle (maintains fixed offset even when steering)
        if (xrOrigin != null)
        {
            // Calculate rotated offset based on bike's current rotation
            Vector3 rotatedOffset = transform.rotation * xrOffsetFromBike;
            
            // Position camera at bike position + rotated offset (locked to saddle)
            xrOrigin.position = transform.position + rotatedOffset;
            
            // Rotate camera with bike (Y-axis only, keeps head level)
            xrOrigin.rotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);
        }
        
        // COMMENTED OUT - Old version that drifts when steering
        // if (xrOrigin != null)
        // {
        //     // Update position
        //     Vector3 deltaPosition = transform.position - lastPosition;
        //     xrOrigin.position += deltaPosition;
        //     
        //     // Update rotation to match bike yaw (keep XR Origin facing bike's forward)
        //     xrOrigin.rotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);
        // }
        
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
        
        // Reset XR Origin (camera)
        if (xrOrigin != null)
        {
            xrOrigin.position = initialXRPosition;
            xrOrigin.rotation = initialXRRotation;
        }
        
        // Reset movement state
        lastPosition = initialBikePosition;
        currentSpeed = 0f;
        currentRotation = 0f;
        wheelRotation = 0f;
        
        Debug.Log("[SimpleBikeController] Recenter complete!");
    }

    // Public properties for HUD
    public float CurrentSpeed => currentSpeed;
}
