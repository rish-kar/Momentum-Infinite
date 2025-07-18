using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Scans the ground corridor ahead of the player and reports any set of colliders that
/// block the minimum required clearance width (lane).
/// 
/// A full 10‑metre ground tile is swept with evenly‑spaced horizontal probe rows.
/// For every probe row we cast a rapid series of short downward rays. If *any* ray
/// finds ground clearance ≥ <see cref="minClearanceWidth"/>, that row is considered
/// passable; otherwise the colliders hit in that row are collected as potential
/// culprits.  All failing rows inside the tile are merged and sent to
/// <see cref="BlockageReporter"/> in a single report so that **every** prefab that
/// contributes to the blockage is listed.
/// </summary>
[AddComponentMenu("Momentum/Procedural Safety/Flyer Corridor Raycast Scanner")]
public class FlyerCorridorRaycastScanner : MonoBehaviour
{
    #region Inspector

    [Header("Ground Settings")]
    [SerializeField] private float tileLength = 10f;       // Length of a ground tile (Z)
    [SerializeField] private float tileWidth  = 2.15f;      // Total road width (X)
    [SerializeField] private LayerMask obstacleMask;        // Layers considered obstacles

    [Header("Player Clearance Requirements")]
    [Tooltip("Minimum free horizontal space the player needs to pass (metres)")]
    [SerializeField] private float minClearanceWidth = 1.5f;
    [Tooltip("How far below the player to originate the clearance rays")]
    [SerializeField] private float rayHeightOffset = 1f;
    [Tooltip("Distance between parallel probe rows (metres)")]
    [SerializeField] protected float raycastSpacing = 0.5f;

    #endregion

    private float _lastScannedTileZ = float.MinValue;

    /***********************************************************************/
    //                              Unity                                   
    /***********************************************************************/

    private void FixedUpdate()
    {
        // Work in tile increments: do not scan the same tile twice.
        float tileZ = Mathf.Floor(transform.position.z / tileLength) * tileLength;
        if (tileZ <= _lastScannedTileZ) return;
        _lastScannedTileZ = tileZ;

        ScanCorridor(tileZ);
    }

    /***********************************************************************/
    //                           Core logic                                 
    /***********************************************************************/

    /// <summary>
    /// Casts rapid downward rays across the width of the road at the given Z‑slice
    /// and determines whether the required lane width is clear.
    /// </summary>
    private bool CheckHorizontalClearance(float currentZ, float raycastY,
                                          out List<BlockageDetailDTO.CulpritInfo> culpritsList)
    {
        culpritsList = new List<BlockageDetailDTO.CulpritInfo>();
        var uniqueColliders = new HashSet<Collider>();

        float halfWidth      = tileWidth * 0.5f;
        const float step     = 0.05f;           // horizontal resolution of the scan
        float continuousFree = 0f;               // running counter of free space

        for (float x = -halfWidth; x <= halfWidth; x += step)
        {
            Vector3 origin = new Vector3(x, raycastY, currentZ);

            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 5f, obstacleMask))
            {
                // Blocked at this X → reset the free‑space counter
                continuousFree = 0f;

                // Record culprit (once per collider)
                Collider col = hit.collider;
                if (uniqueColliders.Add(col))
                {
                    string rootName  = col.transform.root.name.Replace("(Clone)", string.Empty);
                    string childName = col.transform == col.transform.root
                                        ? rootName
                                        : $"{rootName}/{col.gameObject.name.Replace("(Clone)", string.Empty)}";

                    culpritsList.Add(new BlockageDetailDTO.CulpritInfo
                    {
                        name     = childName,
                        position = col.bounds.center,
                        size     = col.bounds.size,
                        layer    = LayerMask.LayerToName(col.gameObject.layer)
                    });
                }
            }
            else
            {
                // Free point – accumulate
                continuousFree += step;
                if (continuousFree >= minClearanceWidth)
                    return true; // a clear lane found for this row
            }
        }

        return false; // insufficient clearance in this row
    }

    /// <summary>
    /// Sweeps an entire ground tile (length wise) with multiple probe rows and
    /// reports every collider that makes the lane impassable in *any* row.
    /// </summary>
    private void ScanCorridor(float tileZ)
    {
        float raycastY           = transform.position.y - rayHeightOffset;
        int   horizontalRowCount = Mathf.CeilToInt(tileLength / raycastSpacing);

        var   allCulprits   = new List<BlockageDetailDTO.CulpritInfo>();
        float firstBlockedZ = -1f;

        for (int i = 0; i <= horizontalRowCount; i++)
        {
            float currentZ = tileZ + (i * raycastSpacing);

            if (!CheckHorizontalClearance(currentZ, raycastY, out var rowCulprits))
            {
                if (firstBlockedZ < 0f) firstBlockedZ = currentZ;

                // Merge rows, avoid duplicates based on name + position
                foreach (var c in rowCulprits)
                {
                    bool already = allCulprits.Exists(k => k.name == c.name &&
                                                         (k.position - c.position).sqrMagnitude < 1e-4f);
                    if (!already) allCulprits.Add(c);
                }
            }
        }

        if (allCulprits.Count > 0)
        {
            Debug.LogError($"[RUNWAY] Corridor blocked at tile Z={tileZ}");
            BlockageReporter.ReportBlockage(tileZ, firstBlockedZ, allCulprits);
        }
    }

    /***********************************************************************/
    //                       Gizmos (editor‑only)                           
    /***********************************************************************/
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;

        float tileZ = Mathf.Floor(transform.position.z / tileLength) * tileLength;
        float raycastY = transform.position.y - rayHeightOffset;
        int rows = Mathf.CeilToInt(tileLength / raycastSpacing);
        float halfWidth = tileWidth * 0.5f;

        for (int i = 0; i <= rows; i++)
        {
            float currentZ = tileZ + (i * raycastSpacing);
            Vector3 left  = new Vector3(-halfWidth, raycastY, currentZ);
            Vector3 right = new Vector3( halfWidth, raycastY, currentZ);
            Gizmos.DrawLine(left, right);
        }
    }
#endif
}
