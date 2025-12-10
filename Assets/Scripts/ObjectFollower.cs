using UnityEngine;
using UnityEngine.XR;

public class CameraFollow : MonoBehaviour
{
    public Transform bike; // Reference to the bike object
    public Vector3 offset = new Vector3(0, 2, -5);
    [Tooltip("When true, the camera follow will be disabled while a VR device is active.")]
    public bool disableWhenVRActive = false;
    [Tooltip("When true and a VR device is active, the XR rig (camera parent) will follow the bike from behind while keeping HMD look rotation.")]
    public bool followInVR = true;

    void Start()
    {
        if (bike == null)
        {
            var bikeObj = GameObject.FindWithTag("Bike");
            if (bikeObj != null)
            {
                bike = bikeObj.transform;
            }
            else
            {
                Debug.LogWarning("Bike reference is not assigned in CameraFollow. Assign it in the Inspector or tag the bike GameObject with 'Bike'.");
            }
        }
    }

    void LateUpdate()
    {
        if (XRSettings.isDeviceActive)
        {
            if (disableWhenVRActive || !followInVR)
            {
                // VR active but follow disabled.
                return;
            }

            // In VR: move the XR rig (camera parent) so the HMD stays behind the bike,
            // but preserve the head's rotation so the user can look around freely.
            var cam = Camera.main;
            if (cam == null)
            {
                return;
            }

            Transform rig = cam.transform.parent;
            if (rig == null)
            {
                // No parent rig; move the camera itself as fallback.
                rig = cam.transform;
            }

            Vector3 desiredRigPosition = bike.position + bike.TransformDirection(offset);
            rig.position = Vector3.Lerp(rig.position, desiredRigPosition, Time.deltaTime * 5f);

            // Rotate the rig to face the bike on the Y-axis only (preserve head pitch/roll)
            Vector3 lookDir = bike.position - rig.position;
            lookDir.y = 0f;
            if (lookDir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(lookDir);
                rig.rotation = Quaternion.Slerp(rig.rotation, targetRot, Time.deltaTime * 5f);
            }

            return;
        }

        if (bike == null)
        {
            Debug.LogWarning("Bike reference is missing in CameraFollow script.");
            return;
        }

        // Calculate the desired position behind the bike (relative to bike rotation)
        Vector3 desiredPosition = bike.position + bike.TransformDirection(offset);

        // Smoothly move the camera to the desired position
        transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * 5f);

        // Make the camera look at the bike's position (slightly above)
        transform.LookAt(bike.position + Vector3.up * 1f);
    }
}
