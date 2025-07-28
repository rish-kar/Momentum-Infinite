using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Scans the ground corridor ahead of the player and reports *every prefab* whose
/// colliders intrude into the tile‑long safety lane the runner must traverse.
/// 
/// How it works in three passes:
/// 1. **Horizontal probe rows** ‑ identical to the previous algorithm – decide whether
///    the tile is *truly impassable* (no 1.5 m gap anywhere). Rows that fail add their
///    hit colliders to a working set.
/// 2. **Overlap sweep** – a Physics.OverlapBox grabs *all* colliders that fall inside
///    the tile volume. Anything that belongs to the obstacle layer and crosses the
///    lane is appended to the working set.  This catches objects that didn’t sit exactly
///    under a downward ray or that appeared in a "clear" row but still help block the
///    corridor when combined with others.
/// 3. The merged, de‑duplicated list is forwarded to <see cref="BlockageReporter"/>.
///    Names are always root‑prefab/child and have their “(Clone)” suffix removed for
///    readability.
/// </summary>
[AddComponentMenu("Momentum/Procedural Safety/Flyer Corridor Raycast Scanner")]
public class FlyerCorridorScanner : MonoBehaviour
{
    #region Inspector Fields ----------------------------------------------------

    [Header("Ground Settings")]
    [SerializeField] private float tileLength = 10f;          // Z‑size of each ground tile
    [SerializeField] private float tileWidth  = 2.15f;        // total road width we scan
    [SerializeField] private LayerMask obstacleMask;

    [Header("Player Clearance Requirements")]
    [SerializeField] private float minClearanceWidth = 1.5f;  // needed gap across X
    [SerializeField] private float rayHeightOffset   = 1f;    // cast rays from this Y
    [SerializeField] private float raycastSpacing    = 0.5f;  // spacing of probe rows

    [Header("Overlap Sweep")]
    [Tooltip("Extra margin (in metres) above ground when doing the OverlapBox sweep")]
    [SerializeField] private float sweepHeight = 4f;

    #endregion ----------------------------------------------------------------

    private float _lastScannedTileZ = float.MinValue;
    
    EnvironmentObjectSpawnManager _spawnMgr;
    
    [SerializeField] private bool autoRemoveBlockers = true;   // show in Inspector

    public void SetAutoRemove(bool on) => autoRemoveBlockers = on;



    // -------------------------------------------------------------------------
    // FixedUpdate
    // -------------------------------------------------------------------------
    private void FixedUpdate()
    {
        float tileZ = Mathf.Floor(transform.position.z / tileLength) * tileLength;
        if (tileZ <= _lastScannedTileZ) return;    // already processed this tile

        _lastScannedTileZ = tileZ;
        ScanCorridor(tileZ);
    }
    
    void Awake()
    {
        _spawnMgr = FindFirstObjectByType<EnvironmentObjectSpawnManager>();
    }

    Transform GetPrefabRoot(Transform t)
    {
        if (!_spawnMgr) return t;                          // fallback
        var parent = _spawnMgr.transform;
        while (t.parent && t.parent != parent) t = t.parent;
        return t;
    }


    // -------------------------------------------------------------------------
    // Corridor scan main routine
    // -------------------------------------------------------------------------
    private void ScanCorridor(float tileZ)
    {
        float raycastY         = transform.position.y - rayHeightOffset;
        int   horizontalRows   = Mathf.CeilToInt(tileLength / raycastSpacing);
        float halfWidth        = tileWidth * 0.5f;

        // running collection of blocking colliders (HashSet avoids duplicates fast)
        var culpritsSet = new HashSet<Collider>();
        var culpritsDTO = new List<BlockageDetailDTO.CulpritInfo>();
        float firstBlockedZ = -1f;

        // ── 1⃣  Probe rows ───────────────────────────────────────────────────
        for (int i = 0; i <= horizontalRows; i++)
        {
            float currentZ = tileZ + (i * raycastSpacing);
            if (!CheckHorizontalClearance(currentZ, raycastY, culpritsSet, culpritsDTO))
            {
                if (firstBlockedZ < 0f) firstBlockedZ = currentZ;
            }
        }

        // ── 2⃣  Overlap sweep across the entire tile volume ──────────────────
        Vector3 boxCenter = new(transform.position.x, raycastY + sweepHeight * 0.5f, tileZ + tileLength * 0.5f);
        Vector3 boxHalf   = new(halfWidth, sweepHeight * 0.5f, tileLength * 0.5f);
        Collider[] hits   = Physics.OverlapBox(boxCenter, boxHalf, Quaternion.identity, obstacleMask);

        foreach (var col in hits)
        {
            if (culpritsSet.Add(col))               // brand‑new collider → add DTO row
                culpritsDTO.Add(ToCulpritInfo(col));
        }

        // ── 3⃣  Report (only if something actually touches the lane) ─────────
        if (culpritsDTO.Count > 0)
        {
            Debug.LogError($"[RUNWAY] Corridor blocked at tile Z={tileZ}");
            BlockageReporter.ReportBlockage(tileZ, firstBlockedZ < 0 ? tileZ : firstBlockedZ, culpritsDTO);

            if (autoRemoveBlockers)                    // <<< guard with the switch
            {
                foreach (var col in culpritsSet)
                {
                    if (!col) continue;
                    var root = GetPrefabRoot(col.transform);
                    if (root && root != _spawnMgr.transform)
                        Destroy(root.gameObject);
                }
            }
        }

    }

    // -------------------------------------------------------------------------
    // CheckHorizontalClearance  – returns true if the *row* has an open gap.
    // Also gathers any colliders the downward rays hit.
    // -------------------------------------------------------------------------
    private bool CheckHorizontalClearance(
        float currentZ,
        float raycastY,
        HashSet<Collider> culpritsSet,
        List<BlockageDetailDTO.CulpritInfo> culpritsDTO)
    {
        float halfWidth       = tileWidth * 0.5f;
        float stepX           = 0.05f;   // resolution across X
        float currentGapWidth = 0f;

        for (float x = -halfWidth; x <= halfWidth; x += stepX)
        {
            Vector3 origin = new(x, raycastY, currentZ);

            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 5f, obstacleMask))
            {
                currentGapWidth = 0f;    // reset gap count

                if (culpritsSet.Add(hit.collider))
                    culpritsDTO.Add(ToCulpritInfo(hit.collider));
            }
            else
            {
                currentGapWidth += stepX;
                if (currentGapWidth >= minClearanceWidth)
                    return true;        // row is passable, no need to check further X
            }
        }

        return false; // no sufficient gap found across the whole row
    }

    // -------------------------------------------------------------------------
    // Helper – convert collider to DTO (root/child path, no "(Clone)")
    // -------------------------------------------------------------------------
    private static BlockageDetailDTO.CulpritInfo ToCulpritInfo(Collider col)
    {
        string rootName  = col.transform.root.name.Replace("(Clone)", "");
        string childName = col.transform == col.transform.root
                            ? rootName
                            : $"{rootName}/{col.gameObject.name}";

        return new BlockageDetailDTO.CulpritInfo
        {
            name     = childName.Replace("(Clone)", ""),
            position = col.bounds.center,
            size     = col.bounds.size,
            layer    = LayerMask.LayerToName(col.gameObject.layer)
        };
    }

    // -------------------------------------------------------------------------
    // Debug gizmos – shows probe rows in Scene view
    // -------------------------------------------------------------------------
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;

        float tileZ        = Mathf.Floor(transform.position.z / tileLength) * tileLength;
        float raycastY     = transform.position.y - rayHeightOffset;
        int   rows         = Mathf.CeilToInt(tileLength / raycastSpacing);
        float halfWidth    = tileWidth * 0.5f;

        for (int i = 0; i <= rows; i++)
        {
            float currentZ = tileZ + (i * raycastSpacing);
            Vector3 from   = new(-halfWidth, raycastY, currentZ);
            Vector3 to     = new(halfWidth,  raycastY, currentZ);
            Gizmos.DrawLine(from, to);
        }

        // Draw the overlap box (safety lane volume)
        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.25f);
        Vector3 boxCenter = new(transform.position.x, raycastY + sweepHeight * 0.5f, tileZ + tileLength * 0.5f);
        Vector3 boxSize   = new(tileWidth, sweepHeight, tileLength);
        Gizmos.DrawCube(boxCenter, boxSize);
    }
#endif
}
