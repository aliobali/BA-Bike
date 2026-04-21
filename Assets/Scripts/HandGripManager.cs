using UnityEngine;
using UnityEngine.XR.Hands;
using TMPro;

/// <summary>
/// Manages hand visualization and snapping to handlebar grip points.
/// Hands snap when making a FIST gesture near the handlebar.
/// Uses XR Hands for hand tracking (Meta Quest 3 / HTC Vive compatible).
/// </summary>
public class HandGripManager : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Left hand root transform")]
    public Transform leftHandRoot;
    
    [Tooltip("Right hand root transform")]
    public Transform rightHandRoot;
    
    [Tooltip("Left grip point on handlebar")]
    public Transform leftGripPoint;
    
    [Tooltip("Right grip point on handlebar")]
    public Transform rightGripPoint;

    [Header("Grip Settings")]
    [Tooltip("Distance at which hands start snapping to grip point")]
    public float gripActivationDistance = 0.8f;
    
    [Tooltip("Smooth speed for hand snapping")]
    public float snapSmoothTime = 0.08f;
    
    [Tooltip("Distance offset to bring hands closer/further from grip points (negative = closer)")]
    public float gripDistanceOffset = 0f;

    [Header("Fist Detection")]
    [Tooltip("How closed the hand must be (0-1, where 1 = fully closed fist)")]
    public float fistThreshold = 0.6f;

    [Header("Debug")]
    [Tooltip("Show debug info on screen")]
    public bool showDebugInfo = false;

    private XRHand leftHand;
    private XRHand rightHand;
    private Vector3 leftHandVelocity = Vector3.zero;
    private Vector3 rightHandVelocity = Vector3.zero;
    private TextMeshProUGUI debugText;

    void Start()
    {
        Debug.Log("[HandGripManager] Hand tracking disabled - using system-level hand visuals only");
        
        // Auto-find references if not assigned
        if (leftHandRoot == null || rightHandRoot == null)
        {
            Debug.Log("[HandGripManager] Auto-finding hand references...");
            AutoFindHandReferences();
        }

        // Setup debug UI
        if (showDebugInfo)
        {
            Debug.Log("[HandGripManager] Creating debug UI...");
            CreateDebugUI();
        }

        Debug.Log($"[HandGripManager] Ready. Use system hand menu for hand visibility.");
    }

    void Update()
    {
        // Hand tracking is disabled due to XR system initialization issues
        // The system-level hand visuals work fine for interaction with the OS menu
        // For now, just update debug display if enabled
        
        if (showDebugInfo && debugText != null)
        {
            UpdateDebugDisplay();
        }
    }

    private void UpdateHand(XRHand hand, Transform handRoot, Transform gripPoint, ref Vector3 velocity, string handName)
    {
        if (!hand.isTracked) 
        {
            Debug.Log($"[{handName}] Hand not tracked");
            return;
        }

        float fistClosedness = GetFistClosedness(hand);
        bool isFist = fistClosedness > fistThreshold;

        Debug.Log($"[{handName}] Fist={fistClosedness:F2}, Threshold={fistThreshold}, IsFist={isFist}");

        // Snap to grip when making a fist (no distance check needed)
        if (isFist)
        {
            // Calculate target position with distance offset (negative = closer)
            Vector3 targetPos = gripPoint.position;
            Vector3 directionFromHand = (targetPos - handRoot.position).normalized;
            targetPos -= directionFromHand * gripDistanceOffset;
            
            // Smooth damp hand position towards grip point
            handRoot.position = Vector3.SmoothDamp(handRoot.position, targetPos, ref velocity, snapSmoothTime);
            Debug.Log($"[{handName}] GRABBING! Fist={fistClosedness:F2}, SnapPos={handRoot.position}");
        }
    }

    private void UpdateHandTest(Transform handRoot, Transform gripPoint, ref Vector3 velocity, string handName, float fistClosedness)
    {
        bool isFist = fistClosedness > fistThreshold;

        Debug.Log($"[TEST {handName}] Fist={fistClosedness:F2}, Threshold={fistThreshold}, IsFist={isFist}");

        // Snap to grip when making a fist
        if (isFist)
        {
            // Calculate target position with distance offset (negative = closer)
            Vector3 targetPos = gripPoint.position;
            Vector3 directionFromHand = (targetPos - handRoot.position).normalized;
            targetPos -= directionFromHand * gripDistanceOffset;
            
            // Smooth damp hand position towards grip point
            handRoot.position = Vector3.SmoothDamp(handRoot.position, targetPos, ref velocity, snapSmoothTime);
            Debug.Log($"[TEST {handName}] GRABBING! Fist={fistClosedness:F2}, SnapPos={handRoot.position}");
        }
    }

    /// <summary>
    /// Detects how closed the hand is (0 = open, 1 = fully closed fist)
    /// Checks if fingers are curled by measuring joint distances
    /// </summary>
    private float GetFistClosedness(XRHand hand)
    {
        if (!hand.isTracked) 
        {
            return 0f;
        }

        try
        {
            // Get finger index positions - simpler approach
            var indexTip = hand.GetJoint(XRHandJointID.IndexTip);
            var palmPos = hand.GetJoint(XRHandJointID.Palm);

            if (indexTip.TryGetPose(out var tipPose) && palmPos.TryGetPose(out var palmPose))
            {
                // Distance from fingertip to palm indicates how closed the hand is
                float distToTip = Vector3.Distance(tipPose.position, palmPose.position);
                
                // When fist is closed: ~0.04m, open hand: ~0.10m
                float closedness = Mathf.Clamp01(1f - (distToTip / 0.12f));
                
                //Debug.Log($"[Fist] Hand tracked, distToTip={distToTip:F3}, closedness={closedness:F2}");
                return closedness;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[HandGripManager] Fist detection error: {e.Message}");
        }
        
        return 0f;
    }

    private void CreateDebugUI()
    {
        // Find or create a Canvas for debug display
#pragma warning disable CS0618
        Canvas canvas = FindObjectOfType<Canvas>();
#pragma warning restore CS0618
        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("DebugCanvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }

        // Create debug text
        GameObject textGO = new GameObject("HandGripDebugText");
        textGO.transform.SetParent(canvas.transform, false);
        
        debugText = textGO.AddComponent<TextMeshProUGUI>();
        debugText.text = "HandGripManager Debug";
        debugText.fontSize = 20;
        debugText.color = Color.white;
        
        RectTransform rectTransform = textGO.GetComponent<RectTransform>();
        rectTransform.anchoredPosition = new Vector2(10, -10);
        rectTransform.sizeDelta = new Vector2(600, 400);
    }

    private void UpdateDebugDisplay()
    {
        string text = $"<b>Hand Tracking Status</b>\n";
        text += $"System-level hands: <color=green>ACTIVE</color>\n";
        text += $"Use OS menu to hide/show hands\n\n";
        text += $"<b>Features Working:</b>\n";
        text += $"✓ Camera centered on seat\n";
        text += $"✓ No rotation on steering\n";
        text += $"✓ Hand offset adjustment";

        debugText.text = text;
    }

    private void AutoFindHandReferences()
    {
#pragma warning disable CS0618
        Transform[] allTransforms = FindObjectsOfType<Transform>();
#pragma warning restore CS0618
        
        Debug.Log($"[HandGripManager] Searching through {allTransforms.Length} transforms for hand tracking objects...");
        
        // Log all objects with "Hand" in the name to see what's available
        foreach (Transform t in allTransforms)
        {
            if (t.name.ToLower().Contains("hand"))
            {
                Debug.Log($"  Found hand object: {t.name} | Children: {t.childCount}");
                
                // Check if it has XRHand
                XRHand xrHand = t.GetComponent<XRHand>();
                Debug.Log($"    └─ Has XRHand? {(xrHand != null ? "YES" : "NO")}");
                
                // Check for mesh renderers
                SkinnedMeshRenderer[] meshes = t.GetComponentsInChildren<SkinnedMeshRenderer>();
                Debug.Log($"    └─ SkinnedMeshRenderers: {meshes.Length}");
            }
        }
        
        // First pass: look for explicit Left/Right Hand Tracking objects
        foreach (Transform t in allTransforms)
        {
            string nameLower = t.name.ToLower();
            
            if (nameLower.Contains("left") && nameLower.Contains("hand") && nameLower.Contains("tracking"))
            {
                leftHandRoot = t;
                Debug.Log($"[HandGripManager] Found LEFT Hand Tracking: {t.name}");
            }
            else if (nameLower.Contains("right") && nameLower.Contains("hand") && nameLower.Contains("tracking"))
            {
                rightHandRoot = t;
                Debug.Log($"[HandGripManager] Found RIGHT Hand Tracking: {t.name}");
            }
            
            if (leftHandRoot != null && rightHandRoot != null)
                break;
        }

        // If not found, search for "Hand" objects and use for both
        if (leftHandRoot == null && rightHandRoot == null)
        {
            Debug.Log("[HandGripManager] Fallback: looking for Hand Visualizer...");
#pragma warning disable CS0618
            foreach (Transform t in FindObjectsOfType<Transform>())
#pragma warning restore CS0618
            {
                if (t.name.Contains("Hand") && !t.name.Contains("Tracking"))
                {
                    leftHandRoot = t;
                    rightHandRoot = t;
                    Debug.Log($"[HandGripManager] Fallback: Using {t.name} for both hands");
                    break;
                }
            }
        }

        if (leftHandRoot == null || rightHandRoot == null)
        {
            Debug.LogError("[HandGripManager] Could not auto-find hand transforms!");
        }
        else
        {
            Debug.Log($"[HandGripManager] Hand references: Left={leftHandRoot.name}, Right={rightHandRoot.name}");
        }
    }

    private void AutoFindGripPoints()
    {
#pragma warning disable CS0618
        Transform[] allTransforms = FindObjectsOfType<Transform>();
#pragma warning restore CS0618
        
        foreach (Transform t in allTransforms)
        {
            string nameLower = t.name.ToLower();
            
            if (nameLower.Contains("left") && nameLower.Contains("grip"))
            {
                leftGripPoint = t;
            }
            else if (nameLower.Contains("right") && nameLower.Contains("grip"))
            {
                rightGripPoint = t;
            }
            
            if (leftGripPoint != null && rightGripPoint != null)
                break;
        }

        if (leftGripPoint == null || rightGripPoint == null)
        {
            Debug.LogWarning("HandGripManager: Could not find grip points. Create LeftGrip and RightGrip as children of Handle.");
        }
    }
}
