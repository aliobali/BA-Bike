using UnityEngine;

public class Player : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Set frame rate to 90Hz for Meta Quest 3 to match native refresh rate
        // This reduces jitter and improves VR experience
        Application.targetFrameRate = 90;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
