using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using Unity.XR.CoreUtils;

/// <summary>
/// Locks the XR camera to a seat anchor so it stays stationary relative to the bike.
/// Use with XROrigin in "Device" tracking origin mode.
/// </summary>
[DefaultExecutionOrder(200)]
public class XRCameraSeatAnchor : MonoBehaviour
{
    [Tooltip("XR Origin that owns the Main Camera")] public XROrigin xrOrigin;
    [Tooltip("Seat or reference transform on the bike")] public Transform seatAnchor;

    [Header("Offsets")]
    [Tooltip("Local offset from the seat anchor to the rider's head position")] public Vector3 localOffset = new Vector3(0f, 1.1f, 0f);
    [Tooltip("Align XR Origin yaw with the bike/anchor yaw")] public bool alignYawWithAnchor = true;

    [Header("Behavior")]
    [Tooltip("Continuously cancel HMD positional drift each frame")] public bool lockContinuously = true;
    [Tooltip("Recenter XR on Start and force Device origin mode")] public bool recenterOnStart = true;

    private XRInputSubsystem inputSubsystem;

    [System.Obsolete]
    void Awake()
    {
        if (xrOrigin == null) xrOrigin = FindObjectOfType<XROrigin>();
    }

    void Start()
    {
        if (xrOrigin == null || seatAnchor == null)
        {
            Debug.LogWarning("XRCameraSeatAnchor: Assign xrOrigin and seatAnchor in the Inspector.");
            return;
        }

        // Try force Device tracking origin and recenter
        var subsystems = new List<XRInputSubsystem>();
        SubsystemManager.GetSubsystems(subsystems);
        if (subsystems.Count > 0)
        {
            inputSubsystem = subsystems[0];
            if (recenterOnStart)
            {
                inputSubsystem.TrySetTrackingOriginMode(TrackingOriginModeFlags.Device);
                inputSubsystem.TryRecenter();
            }
        }

        ApplyAnchor(true);
    }

    void LateUpdate()
    {
        if (lockContinuously) ApplyAnchor(false);
    }

    private void ApplyAnchor(bool snapRotation)
    {
        if (xrOrigin == null || seatAnchor == null) return;

        // Desired camera world position = seat anchor + local offset
        Vector3 targetWorldPos = seatAnchor.TransformPoint(localOffset);
        xrOrigin.MoveCameraToWorldLocation(targetWorldPos);

        if (alignYawWithAnchor)
        {
            // Keep XR Origin yaw aligned with the bike/anchor; preserve level (no roll/pitch)
            var yaw = seatAnchor.eulerAngles.y;
            xrOrigin.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        }
    }
}
