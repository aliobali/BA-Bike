using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Optional Origin-Shifting system to eliminate floating-point precision jitter on distant objects.
/// 
/// How it works:
/// - Keeps the player camera near (0,0,0) where float precision is highest
/// - When player moves far, the environment shifts backwards by the same amount
/// - Invisible to player - feels like normal movement
/// - Result: Distant buildings render crisply without jitter
/// 
/// To DISABLE: Uncheck "enabled" checkbox in Inspector, or delete this script
/// To REVERT: Disable component or delete file - original code is completely unchanged
/// </summary>
public class OriginShiftingManager : MonoBehaviour
{
    [Header("Origin Shifting Settings")]
    [Tooltip("Enable/disable origin shifting. Uncheck to disable without removing script.")]
    public bool enableShifting = true;

    [Tooltip("How far from origin (0,0,0) before shifting occurs. Lower = more frequent shifts but better precision")]
    [Range(100f, 500f)]
    public float shiftThreshold = 100f;

    [Tooltip("The bike controller - gets notified when shifts occur")]
    public Transform playerBike;

    [Tooltip("The environment root - all buildings/terrain as children get shifted")]
    public Transform environmentRoot;

    // Cumulative shift applied to the world
    private Vector3 cumulativeShift = Vector3.zero;

    private void Start()
    {
        if (!enableShifting)
        {
            Debug.Log("[OriginShifting] DISABLED - Set enableShifting to true to activate.");
            return;
        }

        Debug.Log("[OriginShifting] Starting origin shifting system...");

        // Auto-find environment if not assigned
        if (environmentRoot == null)
        {
            GameObject networkGen = GameObject.Find("NetworkGenerator");
            if (networkGen != null)
            {
                environmentRoot = networkGen.transform.parent ?? networkGen.transform;
                Debug.Log("[OriginShifting] Auto-found environment root: " + environmentRoot.name);
            }
        }

        if (playerBike == null)
        {
            playerBike = GameObject.Find("Bicycle (1)")?.transform;
            if (playerBike == null)
            {
                playerBike = FindFirstObjectByType<SimpleBikeController>()?.transform;
            }
            if (playerBike != null)
            {
                Debug.Log("[OriginShifting] Auto-found player bike: " + playerBike.name);
            }
        }

        if (environmentRoot == null)
        {
            Debug.LogError("[OriginShifting] FAILED: Environment Root not found! Assign manually in Inspector.");
        }
        else
        {
            Debug.Log("[OriginShifting] Environment Root assigned: " + environmentRoot.name);
        }

        if (playerBike == null)
        {
            Debug.LogError("[OriginShifting] FAILED: Player Bike not found! Assign manually in Inspector.");
        }
        else
        {
            Debug.Log("[OriginShifting] Player Bike assigned: " + playerBike.name);
        }

        Debug.Log("[OriginShifting] System ready. Waiting for player movement...");
    }

    /// <summary>
    /// Call this after player moves to check if origin shift is needed.
    /// This is the OPTIONAL LINE called from SimpleBikeController.
    /// </summary>
    public void OnPlayerMoved()
    {
        if (!enableShifting)
        {
            return;
        }

        if (playerBike == null || environmentRoot == null)
        {
            Debug.LogError("[OriginShifting] OnPlayerMoved called but references are NULL! Check assignments in Inspector.");
            return;
        }

        // Check if player is too far from origin
        Vector3 playerPos = playerBike.position;
        float distanceFromOrigin = new Vector3(playerPos.x, 0, playerPos.z).magnitude;

        // Debug: Log position every 100 frames to track player movement
        if (Time.frameCount % 100 == 0)
        {
            Debug.Log($"[OriginShifting] Player at distance {distanceFromOrigin:F1}m from origin (threshold: {shiftThreshold}m)");
        }

        if (distanceFromOrigin > shiftThreshold)
        {
            PerformOriginShift(playerPos);
        }
    }

    private void PerformOriginShift(Vector3 playerPosition)
    {
        // Shift amount = current player position
        Vector3 shiftAmount = new Vector3(playerPosition.x, 0, playerPosition.z);

        // Apply shift to entire environment (all buildings, terrain, etc.)
        environmentRoot.position -= shiftAmount;

        // Reset player/bike to origin (approximately)
        playerBike.position -= shiftAmount;

        // Track cumulative shift for debugging
        cumulativeShift += shiftAmount;

        Debug.Log($"[OriginShifting] Performed shift. Total displacement: {cumulativeShift.magnitude:F1}m. Player now at: {playerBike.position}");
    }

    /// <summary>
    /// Get total distance world has shifted (for debugging)
    /// </summary>
    public float GetTotalShiftDistance()
    {
        return cumulativeShift.magnitude;
    }

    /// <summary>
    /// Reset world to original position (rarely needed)
    /// </summary>
    public void ResetOrigin()
    {
        if (environmentRoot == null) return;

        environmentRoot.position += cumulativeShift;
        if (playerBike != null)
        {
            playerBike.position += cumulativeShift;
        }

        cumulativeShift = Vector3.zero;
        Debug.Log("[OriginShifting] Reset to original origin.");
    }
}
