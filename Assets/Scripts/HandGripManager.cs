using UnityEngine;
using UnityEngine.XR.Hands;

/// <summary>
/// Manages hand visualization and snapping to handlebar grip.
/// Uses XR Hands for hand tracking (Meta Quest 3 / HTC Vive compatible).
/// Hands fade in when near handlebar and snap to grip position.
/// </summary>
public class HandGripManager : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Left hand joint hierarchy (XROrigin > Camera Offset > Left Controller)")]
    public Transform leftHandRoot;
    
    [Tooltip("Right hand joint hierarchy (XROrigin > Camera Offset > Right Controller)")]
    public Transform rightHandRoot;
    
    [Tooltip("Handlebar transform to snap hands to")]
    public Transform handlebar;

    [Header("Grip Settings")]
    [Tooltip("Distance at which hands start snapping to handlebar")]
    public float gripActivationDistance = 0.3f;
    
    [Tooltip("Smooth speed for hand snapping (higher = faster snap)")]
    public float snapSmoothTime = 0.1f;
    
    [Header("Appearance")]
    [Tooltip("Hand opacity when fully visible (0-1)")]
    public float visibleAlpha = 0.9f;
    
    [Tooltip("Hand opacity when far away (0-1)")]
    public float transluentAlpha = 0.4f;

    private XRHandTrackingEvents leftHandEvents;
    private XRHandTrackingEvents rightHandEvents;
    private Renderer[] leftHandRenderers;
    private Renderer[] rightHandRenderers;
    private Vector3 leftHandVelocity = Vector3.zero;
    private Vector3 rightHandVelocity = Vector3.zero;

    void Start()
    {
        // Find hand tracking events in children
        if (leftHandRoot != null)
        {
            leftHandEvents = leftHandRoot.GetComponentInChildren<XRHandTrackingEvents>();
            leftHandRenderers = leftHandRoot.GetComponentsInChildren<Renderer>();
        }

        if (rightHandRoot != null)
        {
            rightHandEvents = rightHandRoot.GetComponentInChildren<XRHandTrackingEvents>();
            rightHandRenderers = rightHandRoot.GetComponentsInChildren<Renderer>();
        }

        // Start with translucent hands
        SetHandAlpha(leftHandRenderers, transluentAlpha);
        SetHandAlpha(rightHandRenderers, transluentAlpha);
    }

    void Update()
    {
        if (handlebar == null) return;

        // Update left hand
        if (leftHandRoot != null)
        {
            UpdateHand(leftHandRoot, handlebar, leftHandRenderers, ref leftHandVelocity);
        }

        // Update right hand
        if (rightHandRoot != null)
        {
            UpdateHand(rightHandRoot, handlebar, rightHandRenderers, ref rightHandVelocity);
        }
    }

    void UpdateHand(Transform handRoot, Transform handlebar, Renderer[] renderers, ref Vector3 velocity)
    {
        float distToHandlebar = Vector3.Distance(handRoot.position, handlebar.position);
        bool isGripping = distToHandlebar < gripActivationDistance;

        // Snap hand to handlebar if close
        if (isGripping)
        {
            // Smooth damp hand position towards handlebar
            Vector3 targetPos = handlebar.position;
            handRoot.position = Vector3.SmoothDamp(handRoot.position, targetPos, ref velocity, snapSmoothTime);

            // Fade to opaque
            SetHandAlpha(renderers, visibleAlpha);
        }
        else
        {
            // Fade to translucent when far
            SetHandAlpha(renderers, transluentAlpha);
        }
    }

    void SetHandAlpha(Renderer[] renderers, float alpha)
    {
        if (renderers == null) return;

        foreach (Renderer renderer in renderers)
        {
            foreach (Material mat in renderer.materials)
            {
                Color color = mat.color;
                color.a = alpha;
                mat.color = color;
            }
        }
    }
}
