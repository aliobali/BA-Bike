using UnityEngine;
using UnityEngine.InputSystem;

public class BikeController : MonoBehaviour
{
    [Header("Speed")]
    [Tooltip("Maximum forward speed (m/s)")]
    public float maxSpeed = 6f; // ~21.6 km/h
    [Tooltip("Acceleration when W is pressed (m/s^2)")]
    public float acceleration = 2f;
    [Tooltip("Braking deceleration when S is pressed (m/s^2)")]
    public float brakingAcceleration = 4f;
    [Tooltip("Natural drag when no input (m/s^2)")]
    public float naturalDrag = 1f;

    [Header("Steering")]
    [Tooltip("Base turning rate used together with steering and speed (deg/sec)")]
    public float turnSpeed = 80f;
    [Tooltip("Maximum steering wheel/bike angle in degrees")]
    public float maxSteeringAngle = 50f;
    [Tooltip("How fast the steering angle moves toward the target (deg/sec)")]
    public float steeringResponseSpeed = 120f;

    [Header("External input (optional)")]
    [Tooltip("When enabled, read speed/steering from a UDP receiver instead of keyboard/gamepad.")]
    public bool useUdpInput = false;
    public BikeUdpReceiver udpReceiver;
    [Tooltip("When enabled, accept external input (e.g. from VR adapter) via SetExternalInput()")]
    public bool useExternalInput = false;

    // External input values (set via SetExternalInput)
    private float externalMoveInput = 0f;
    private float externalTurnInput = 0f;

    // Call from external code (VR adapter) to provide inputs in range [-1,1]
    public void SetExternalInput(float move, float turn)
    {
        externalMoveInput = Mathf.Clamp(move, -1f, 1f);
        externalTurnInput = Mathf.Clamp(turn, -1f, 1f);
    }

    // Exposed read-only values for HUD or other scripts
    public float CurrentSpeed { get; private set; }      // m/s
    public float CurrentSteeringAngle { get; private set; }   // degrees
    public float MoveInput { get; private set; }         // raw input value (-1..1)
    public float TurnInput { get; private set; }         // raw input value (-1..1)
    public bool UsingUdpInput => useUdpInput && udpReceiver != null;

    private InputAction moveAction;
    private InputAction turnAction;

    void OnEnable()
    {
        moveAction = new InputAction(type: InputActionType.Value, binding: "<Gamepad>/leftStick/y");
        moveAction.AddBinding("<Keyboard>/w").WithProcessor("scale(factor=1)");
        moveAction.AddBinding("<Keyboard>/s").WithProcessor("scale(factor=-1)");
        moveAction.Enable();

        turnAction = new InputAction(type: InputActionType.Value, binding: "<Gamepad>/leftStick/x");
        turnAction.AddBinding("<Keyboard>/a").WithProcessor("scale(factor=-1)");
        turnAction.AddBinding("<Keyboard>/d").WithProcessor("scale(factor=1)");
        turnAction.Enable();
    }

    void OnDisable()
    {
        moveAction.Disable();
        turnAction.Disable();
    }

    void Update()
    {
        bool usingUdp = useUdpInput && udpReceiver != null;
        bool usingExternal = useExternalInput;

        if (usingExternal)
        {
            // Use inputs supplied externally (e.g., XR adapter)
            MoveInput = externalMoveInput;
            TurnInput = externalTurnInput;

            // Forward speed handling based on external MoveInput
            if (MoveInput > 0.01f)
            {
                CurrentSpeed += acceleration * MoveInput * Time.deltaTime;
            }
            else if (MoveInput < -0.01f)
            {
                CurrentSpeed -= brakingAcceleration * (-MoveInput) * Time.deltaTime;
            }
            else
            {
                CurrentSpeed -= naturalDrag * Time.deltaTime;
            }

            CurrentSpeed = Mathf.Clamp(CurrentSpeed, 0f, Mathf.Max(0.0001f, maxSpeed));

            float targetSteeringAngle = TurnInput * maxSteeringAngle;
            CurrentSteeringAngle = Mathf.MoveTowards(CurrentSteeringAngle, targetSteeringAngle, steeringResponseSpeed * Time.deltaTime);
        }
        else if (usingUdp)
        {
            // Use external values coming from UDP (already in m/s and degrees).
            MoveInput = 0f;
            TurnInput = 0f;

            CurrentSpeed = Mathf.Clamp(udpReceiver.Speed, 0f, Mathf.Max(0.0001f, maxSpeed));
            float targetSteeringAngle = Mathf.Clamp(udpReceiver.SteeringAngle, -maxSteeringAngle, maxSteeringAngle);
            CurrentSteeringAngle = Mathf.MoveTowards(CurrentSteeringAngle, targetSteeringAngle, steeringResponseSpeed * Time.deltaTime);
        }
        else
        {
            // read inputs
            MoveInput = moveAction.ReadValue<float>();    // W/S or gamepad
            TurnInput = turnAction.ReadValue<float>();    // A/D or gamepad

            // --- Forward speed handling ---
            // Accelerate when W pressed, brake when S pressed. Bike cannot go backwards.
            if (MoveInput > 0.01f)
            {
                CurrentSpeed += acceleration * MoveInput * Time.deltaTime;
            }
            else if (MoveInput < -0.01f)
            {
                // braking
                CurrentSpeed -= brakingAcceleration * (-MoveInput) * Time.deltaTime;
            }
            else
            {
                // natural slow down
                CurrentSpeed -= naturalDrag * Time.deltaTime;
            }

            // clamp speed
            CurrentSpeed = Mathf.Clamp(CurrentSpeed, 0f, Mathf.Max(0.0001f, maxSpeed));

            // --- Steering angle handling ---
            float targetSteeringAngle = TurnInput * maxSteeringAngle;
            CurrentSteeringAngle = Mathf.MoveTowards(CurrentSteeringAngle, targetSteeringAngle, steeringResponseSpeed * Time.deltaTime);
        }

        // --- Movement & rotation ---
        // move forward
        transform.Translate(Vector3.forward * CurrentSpeed * Time.deltaTime);

        // Only rotate when moving (bike should not turn in place)
        if (CurrentSpeed > 0.01f)
        {
            // rotation rate scaled by steering angle proportion and by current speed fraction
            float steeringFactor = (maxSteeringAngle > 0f) ? (CurrentSteeringAngle / maxSteeringAngle) : 0f;
            float speedFactor = maxSpeed > 0f ? (CurrentSpeed / maxSpeed) : 0f;
            float rotationDegPerSec = steeringFactor * turnSpeed * speedFactor;
            transform.Rotate(Vector3.up, rotationDegPerSec * Time.deltaTime);
        }
    }
}
