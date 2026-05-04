using UnityEngine;

/// <summary>
/// Adds arm visualization to XR hands by creating and animating arm bones
/// with shoulder anchored to XR Origin body position.
/// Uses simple IK to connect hand to shoulder through elbow.
/// </summary>
public class XRHandArmVisualizer : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Left hand tracking root")]
    public Transform leftHandRoot;
    
    [Tooltip("Right hand tracking root")]
    public Transform rightHandRoot;

    [Header("Arm Settings")]
    [Tooltip("Distance shoulder sits below XR Origin (body anchor)")]
    public float shoulderOffsetY = 0.2f;

    [Tooltip("Length of upper arm bone (shoulder to elbow)")]
    public float upperArmLength = 0.3f;
    
    [Tooltip("Length of forearm bone (elbow to wrist/hand)")]
    public float forearmLength = 0.25f;
    
    [Tooltip("Radius of arm cylinder mesh")]
    public float armRadius = 0.02f;
    
    [Tooltip("Create visual mesh for arms")]
    public bool createArmMeshes = true;
    
    [Tooltip("Material for arm rendering")]
    public Material armMaterial;

    private Transform xrOrigin;
    private Transform leftShoulder;
    private Transform leftElbow;
    private Transform rightShoulder;
    private Transform rightElbow;

    void Start()
    {
        // Find XR Origin from camera hierarchy
        var cam = Camera.main;
        if (cam != null && cam.transform.parent != null)
        {
            xrOrigin = cam.transform.parent;
        }
        
        if (xrOrigin == null)
        {
            xrOrigin = transform;
            Debug.LogWarning("[XRHandArmVisualizer] Could not find XR Origin, using script's transform");
        }

        InitializeArmBones();
    }

    void Update()
    {
        // Update shoulder position (anchored to XR Origin body)
        if (xrOrigin != null && leftShoulder != null)
        {
            leftShoulder.position = xrOrigin.position + Vector3.down * shoulderOffsetY;
        }
        if (xrOrigin != null && rightShoulder != null)
        {
            rightShoulder.position = xrOrigin.position + Vector3.down * shoulderOffsetY;
        }

        // Update arm IK
        UpdateArmIK(leftShoulder, leftElbow, leftHandRoot, "Left");
        UpdateArmIK(rightShoulder, rightElbow, rightHandRoot, "Right");
    }

    private void InitializeArmBones()
    {
        // Create left arm hierarchy
        leftShoulder = CreateArmBone("LeftShoulder", null);
        leftElbow = CreateArmBone("LeftElbow", leftShoulder);
        if (leftHandRoot != null)
        {
            leftHandRoot.SetParent(leftElbow);
        }

        // Create right arm hierarchy
        rightShoulder = CreateArmBone("RightShoulder", null);
        rightElbow = CreateArmBone("RightElbow", rightShoulder);
        if (rightHandRoot != null)
        {
            rightHandRoot.SetParent(rightElbow);
        }

        Debug.Log("[XRHandArmVisualizer] Arm bones initialized");
    }

    private Transform CreateArmBone(string boneName, Transform parent)
    {
        GameObject boneGO = new GameObject(boneName);
        Transform boneTransform = boneGO.transform;
        boneTransform.SetParent(parent ?? transform);
        boneTransform.localPosition = Vector3.zero;
        boneTransform.localRotation = Quaternion.identity;

        // Create visual mesh if enabled
        if (createArmMeshes)
        {
            CreateCylinderMesh(boneGO, boneName);
        }

        return boneTransform;
    }

    private void CreateCylinderMesh(GameObject boneGO, string boneName)
    {
        GameObject meshGO = new GameObject("Mesh");
        meshGO.transform.SetParent(boneGO.transform);
        meshGO.transform.localPosition = Vector3.zero;
        meshGO.transform.localRotation = Quaternion.identity;

        // Add mesh filter and renderer
        MeshFilter meshFilter = meshGO.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = meshGO.AddComponent<MeshRenderer>();

        // Create simple cylinder mesh
        meshFilter.mesh = CreateCylinderMesh(armRadius, boneName.Contains("Upper") ? upperArmLength : forearmLength);
        
        if (armMaterial != null)
        {
            meshRenderer.material = armMaterial;
        }
        else
        {
            meshRenderer.material = new Material(Shader.Find("Standard"));
            meshRenderer.material.color = boneName.Contains("Left") ? Color.blue : Color.red;
        }
    }

    private Mesh CreateCylinderMesh(float radius, float height)
    {
        Mesh mesh = new Mesh();
        mesh.name = "ArmCylinder";

        int segments = 8;
        Vector3[] vertices = new Vector3[(segments + 1) * 2 + 2];
        int[] triangles = new int[segments * 12];

        // Top and bottom cap centers
        vertices[0] = new Vector3(0, height / 2f, 0);
        vertices[1] = new Vector3(0, -height / 2f, 0);

        // Create cylinder sides
        for (int i = 0; i <= segments; i++)
        {
            float angle = (i % segments) * 2f * Mathf.PI / segments;
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;

            vertices[i + 2] = new Vector3(x, height / 2f, z);
            vertices[i + segments + 3] = new Vector3(x, -height / 2f, z);
        }

        // Create triangles (simplified - just sides)
        int triIndex = 0;
        for (int i = 0; i < segments; i++)
        {
            // Top cap
            triangles[triIndex++] = 0;
            triangles[triIndex++] = i + 2;
            triangles[triIndex++] = i + 3;

            // Bottom cap
            triangles[triIndex++] = 1;
            triangles[triIndex++] = i + segments + 4;
            triangles[triIndex++] = i + segments + 3;

            // Side
            triangles[triIndex++] = i + 2;
            triangles[triIndex++] = i + segments + 3;
            triangles[triIndex++] = i + 3;
            triangles[triIndex++] = i + 2;
            triangles[triIndex++] = i + segments + 3;
            triangles[triIndex++] = i + segments + 4;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    private void UpdateArmIK(Transform shoulder, Transform elbow, Transform handTarget, string handName)
    {
        if (shoulder == null || elbow == null || handTarget == null)
        {
            return;
        }

        try
        {
            // Get positions
            Vector3 shoulderPos = shoulder.position;
            Vector3 handPos = handTarget.position;
            
            // Calculate distances
            float totalDistance = Vector3.Distance(shoulderPos, handPos);
            float maxReach = upperArmLength + forearmLength;
            
            // If hand is out of reach, pull it closer
            if (totalDistance > maxReach)
            {
                Vector3 direction = (handPos - shoulderPos).normalized;
                handPos = shoulderPos + direction * maxReach;
            }
            
            // Calculate elbow position using IK
            // Elbow should bend naturally between shoulder and hand
            Vector3 elbowPos = CalculateElbowPosition(shoulderPos, handPos, upperArmLength, forearmLength);
            elbow.position = elbowPos;
            
            // Rotate upper arm (shoulder) to point at elbow
            Vector3 upperArmDir = (elbowPos - shoulderPos).normalized;
            if (upperArmDir.sqrMagnitude > 0.01f)
            {
                shoulder.rotation = Quaternion.LookRotation(upperArmDir, Vector3.up);
            }
            
            // Rotate forearm (elbow) to point at hand
            Vector3 forearmDir = (handPos - elbowPos).normalized;
            if (forearmDir.sqrMagnitude > 0.01f)
            {
                elbow.rotation = Quaternion.LookRotation(forearmDir, Vector3.up);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[XRHandArmVisualizer] Error updating {handName} arm IK: {e.Message}");
        }
    }

    private Vector3 CalculateElbowPosition(Vector3 shoulderPos, Vector3 handPos, float upperLen, float forearmLen)
    {
        // Simple IK: find point where upper arm + forearm segments reach the hand
        Vector3 toHand = handPos - shoulderPos;
        float distToHand = toHand.magnitude;
        
        if (distToHand < 0.01f)
            return shoulderPos + Vector3.forward * upperLen;
        
        // Calculate angle using law of cosines
        float a = upperLen;
        float b = forearmLen;
        float c = distToHand;
        
        // Clamp to valid triangle
        c = Mathf.Clamp(c, Mathf.Abs(a - b), a + b);
        
        float cosA = (a * a + c * c - b * b) / (2f * a * c);
        cosA = Mathf.Clamp01(cosA);
        float angleAtShoulder = Mathf.Acos(cosA);
        
        // Position elbow along the line to hand, bent by the angle
        Vector3 direction = toHand.normalized;
        Vector3 perpendicular = Vector3.Cross(direction, Vector3.up).normalized;
        if (perpendicular.sqrMagnitude < 0.01f)
            perpendicular = Vector3.Cross(direction, Vector3.right).normalized;
        
        // Elbow position: shoulder + elbow arm length in bent direction
        Vector3 elbowPos = shoulderPos + direction * (a * Mathf.Cos(angleAtShoulder));
        elbowPos += perpendicular * (a * Mathf.Sin(angleAtShoulder));
        
        return elbowPos;
    }
}
