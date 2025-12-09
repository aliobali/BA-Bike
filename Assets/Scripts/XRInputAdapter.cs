using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

[RequireComponent(typeof(BikeController))]
public class XRInputAdapter : MonoBehaviour
{
    public BikeController bike;

    [Header("Mapping")]
    [Tooltip("If true use triggers for throttle/brake (common). If false, use the primary2DAxis.y (thumbstick) for throttle.)")]
    public bool useTriggersForThrottle = true;
    [Tooltip("If true, uses the right-hand thumbstick for steering when using thumbstick steering; otherwise uses left-hand.")]
    public bool useRightHandForSteering = false;

    [Header("Input")]
    public float deadzone = 0.15f;

    private InputDevice leftController;
    private InputDevice rightController;

    void Start()
    {
        if (bike == null) bike = GetComponent<BikeController>();
        TryInitializeDevices();

        if (bike != null)
            bike.useExternalInput = true;
    }

    void TryInitializeDevices()
    {
        var leftDevices = new List<InputDevice>();
        var rightDevices = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller, leftDevices);
        InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller, rightDevices);

        if (leftDevices.Count > 0) leftController = leftDevices[0];
        if (rightDevices.Count > 0) rightController = rightDevices[0];
    }

    void Update()
    {
        if (!leftController.isValid || !rightController.isValid)
        {
            TryInitializeDevices();
        }

        float move = 0f;
        float turn = 0f;

        // Read triggers
        float rightTrigger = 0f;
        float leftTrigger = 0f;
        if (rightController.isValid)
        {
            rightController.TryGetFeatureValue(CommonUsages.trigger, out rightTrigger);
        }
        if (leftController.isValid)
        {
            leftController.TryGetFeatureValue(CommonUsages.trigger, out leftTrigger);
        }

        // Read primary2DAxis for thumbstick
        Vector2 leftAxis = Vector2.zero;
        Vector2 rightAxis = Vector2.zero;
        if (leftController.isValid)
            leftController.TryGetFeatureValue(CommonUsages.primary2DAxis, out leftAxis);
        if (rightController.isValid)
            rightController.TryGetFeatureValue(CommonUsages.primary2DAxis, out rightAxis);

        // Compute move and turn depending on mapping choice
        if (useTriggersForThrottle)
        {
            // Right trigger accelerates, left trigger brakes
            move = rightTrigger - leftTrigger;
            // Steering from thumbstick x (prefer right stick unless configured otherwise)
            turn = useRightHandForSteering ? rightAxis.x : leftAxis.x;
        }
        else
        {
            // Thumbstick y for move, x for steering
            var steeringAxis = useRightHandForSteering ? rightAxis : leftAxis;
            var throttleAxis = useRightHandForSteering ? rightAxis : leftAxis;
            move = throttleAxis.y;
            turn = steeringAxis.x;
        }

        // Apply deadzone
        if (Mathf.Abs(move) < deadzone) move = 0f;
        if (Mathf.Abs(turn) < deadzone) turn = 0f;

        // Send to bike
        if (bike != null)
        {
            bike.SetExternalInput(move, turn);
        }
    }
}
