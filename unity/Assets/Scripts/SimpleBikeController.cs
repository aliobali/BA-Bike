using UnityEngine;
/// <summary>
/// Controls the virtual bicycle from calibrated UDP bicycle input.
/// Maps speed and steering values to forward motion,
/// yaw rotation, rider-view synchronization, and visual bicycle feedback.
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

    [Tooltip("Handlebar transform to rotate (optional)")]
    public Transform handlebar;

    [Tooltip("Front wheel transform for rotation (optional)")]
    public Transform frontWheel;

    [Header("Visual Steering Fix")]
    public float handlebarVisualSign = -1f;

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
    public float accelerationRate = 2f;

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
    [Tooltip("Quadratic air resistance coefficient - higher values plateau speed sooner.")]
    [Range(0.0f, 0.1f)]
    public float resistCoefficient = 0.01f;

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
    public bool allowSteeringWOMvm = false;

    [Tooltip("Scale incoming speed (m/s) to tame motion")]
    public float speedScale = 1.5f;

    [Tooltip("Absolute cap on movement speed (m/s)")]
    public float maxSpeed = 10f;

    [Tooltip("Desired eye height above ground")]
    public float desiredEyeHeight = 0f;

    [Header("Stop Drift Fix")]
    [Tooltip("If target speed is below this, it is treated as zero input")]
    public float rawStopThreshold = 0.03f;

    [Tooltip("If current speed is below this while input is also near zero, the bike is hard-stopped")]
    public float currentStopThreshold = 0.05f;

    private Rigidbody rb;
    private float currentSpeed = 0f;
    private float currentRotation = 0f;
    private float wheelRotation = 0f;
    private Quaternion handlebarInitialRotation;

    private Vector3 initialBikePosition;
    private Quaternion initialBikeRotation;
    private Vector3 initialXRPosition;
    private Quaternion initialXRRotation;

    private Vector3 xrOffsetFromBike;

    private Vector3 frontWheelInitialEuler;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }
        else
        {
            Debug.LogWarning("SimpleBikeController: No Rigidbody found. Falling back to transform movement.");
        }

        if (udpReceiver == null)
        {
            udpReceiver = FindFirstObjectByType<BikeUdpReceiver>();
            if (udpReceiver == null)
            {
                Debug.LogError("SimpleBikeController: BikeUdpReceiver not found!");
            }
        }

        if (handlebar != null)
        {
            handlebarInitialRotation = handlebar.localRotation;
        }

        if (frontWheel != null)
            frontWheelInitialEuler = frontWheel.localEulerAngles;

        if (xrOrigin != null)
        {
            xrOrigin.position = new Vector3(xrOrigin.position.x, desiredEyeHeight, xrOrigin.position.z);
        }

        initialBikePosition = rb != null ? rb.position : transform.position;
        initialBikeRotation = rb != null ? rb.rotation : transform.rotation;
        initialXRPosition = xrOrigin != null ? xrOrigin.position : Vector3.zero;
        initialXRRotation = xrOrigin != null ? xrOrigin.rotation : Quaternion.identity;

        if (xrOrigin != null)
        {
            xrOffsetFromBike = initialXRPosition - initialBikePosition;
        }

        Transform movementTarget = cameraOffset != null ? cameraOffset : xrOrigin;
        if (movementTarget != null)
        {
            initialXRPosition = movementTarget.position;
            initialXRRotation = movementTarget.rotation;
            xrOffsetFromBike = movementTarget.position - initialBikePosition;
        }
    }

    void FixedUpdate()
    {
        if (udpReceiver == null) return;

        float dt = Time.fixedDeltaTime;

        float rawSpeed = udpReceiver.Speed;
        float steeringNormalized = udpReceiver.SteeringNormalizedDeadzoned;
        float targetSpeed = rawSpeed * speedScale;

        float resistance = resistCoefficient * targetSpeed * targetSpeed;
        targetSpeed = Mathf.Max(0, targetSpeed - resistance);
        targetSpeed = Mathf.Min(targetSpeed, maxSpeed);

        float accelRate;

        if (targetSpeed > currentSpeed)
        {
            accelRate = accelerationRate;
        }
        else if (targetSpeed < currentSpeed)
        {
            float speedDrop = currentSpeed - targetSpeed;

            if (speedDrop > brakingThreshold)
            {
                accelRate = activeBrakeRate;
            }
            else
            {
                accelRate = decelerationRate;
            }
        }
        else
        {
            accelRate = decelerationRate;
        }

        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, accelRate * dt);

        //hard stop tiny speed/rotation values so smoothed sensor noise cannot drift the bike while standing still.
        if (targetSpeed < rawStopThreshold && currentSpeed < currentStopThreshold)
        {
            targetSpeed = 0f;
            currentSpeed = 0f;
            currentRotation = 0f;

            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        float steeringAngleRad = steeringNormalized * (maxHandlebarAngle * Mathf.Deg2Rad);

        float turningRadius = float.MaxValue;

        if (Mathf.Abs(steeringAngleRad) > 0.001f)
        {
            turningRadius = (wheelbase + currentSpeed * 0.3f) / Mathf.Sin(Mathf.Abs(steeringAngleRad));
        }

        float targetRotationRate = 0f;

        if (turningRadius < float.MaxValue && currentSpeed > 0.05f)
        {
            targetRotationRate = (currentSpeed / turningRadius) * Mathf.Rad2Deg * Mathf.Sign(steeringNormalized);
        }

        targetRotationRate = Mathf.Clamp(targetRotationRate, -maxRotationRate, maxRotationRate);

        bool canSteer = (currentSpeed > 0.1f) || allowSteeringWOMvm;

        if (!canSteer)
        {
            targetRotationRate = 0f;
        }

        currentRotation = Mathf.Lerp(
            currentRotation,
            targetRotationRate,
            dt / Mathf.Max(0.01f, rotationSmoothTime)
        );

        if (rb != null)
        {
            Quaternion newRotation =
                rb.rotation * Quaternion.Euler(0f, currentRotation * dt, 0f);

            rb.MoveRotation(newRotation);

            if (currentSpeed > currentStopThreshold)
            {
                Vector3 newPosition =
                    rb.position + (newRotation * Vector3.forward) * currentSpeed * dt;

                rb.MovePosition(newPosition);
            }
        }
        else
        {
            transform.Rotate(0f, currentRotation * dt, 0f);

            if (currentSpeed > currentStopThreshold)
            {
                transform.position += transform.forward * currentSpeed * dt;
            }
        }

        Transform movementTarget = cameraOffset != null ? cameraOffset : xrOrigin;

        if (movementTarget != null)
        {
            Vector3 bikePosition = rb != null ? rb.position : transform.position;
            Quaternion bikeRotation = rb != null ? rb.rotation : transform.rotation;

            Vector3 rotatedOffset = bikeRotation * xrOffsetFromBike;
            movementTarget.position = bikePosition + rotatedOffset;
            movementTarget.rotation = Quaternion.Euler(0f, bikeRotation.eulerAngles.y, 0f);
        }

        if (handlebar != null)
        {
            float handlebarAngle = steeringNormalized * maxHandlebarAngle * handlebarVisualSign;
            handlebar.localRotation =
                handlebarInitialRotation * Quaternion.Euler(0f, handlebarAngle, 0f);
        }

        if (enableWheelRotation)
        {
            UpdateWheels(dt);
        }
    }

    void UpdateWheels(float dt)
    {
        if (currentSpeed < currentStopThreshold)
        {
            return;
        }

        float circumference = 2f * Mathf.PI * wheelRadius;
        float degreesPerSecond = (currentSpeed * 360f) / circumference;
        degreesPerSecond *= wheelSpinMultiplier;

        wheelRotation += degreesPerSecond * dt;

        if (wheelRotation >= 360f) wheelRotation -= 360f;
        if (wheelRotation < 0f) wheelRotation += 360f;

        if (frontWheel != null)
        {
            frontWheel.localRotation = Quaternion.Euler(
                wheelRotation,
                frontWheelInitialEuler.y,
                frontWheelInitialEuler.z
            );
        }
    }
    public float CurrentSpeed => currentSpeed;
}