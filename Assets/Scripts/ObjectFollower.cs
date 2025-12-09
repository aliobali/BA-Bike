using UnityEngine;
using UnityEngine.XR;

public class CameraFollow : MonoBehaviour
{
    public Transform bike; // Reference to the bike object
    public Vector3 offset = new Vector3(0, 2, -5);
    [Tooltip("When true, the camera follow will be disabled while a VR device is active.")]
    public bool disableWhenVRActive = true;

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
        if (disableWhenVRActive && XRSettings.isDeviceActive)
        {
            // In VR the headset controls the camera, so don't override it.
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
