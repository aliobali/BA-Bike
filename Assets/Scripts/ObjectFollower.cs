using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform bike; // Reference to the bike object
    public Vector3 offset = new Vector3(0, 2, -5);

    void Start()
    {
        if (bike == null)
        {
            Debug.LogError("Bike GameObject with tag 'Bike' not found. Please assign the bike reference.");
            return;
        }
    }

    void LateUpdate()
    {
        if (bike == null)
        {
            Debug.LogWarning("Bike reference is missing in CameraFollow script.");
            return;
        }

        // Calculate the desired position behind the bike
        Vector3 desiredPosition = bike.position + bike.TransformDirection(offset);

        // Smoothly move the camera to the desired position
        transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * 5f);

        // Make the camera look at the bike
        transform.LookAt(bike);
    }
}
