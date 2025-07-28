using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

[RequireComponent(typeof(NavMeshAgent))]
public class GhostRunnerAgent : MonoBehaviour
{
    [SerializeField] Transform player;
    [SerializeField] float lookAhead = 300f;
    [SerializeField] float extraSpeed = 4f;
    [SerializeField] float stuckSeconds = 0.3f; // time to consider “stuck”
    [SerializeField] int zTeleportStep = 2; // jump +2 on Z (e.g., 108 → 110)
    [SerializeField] float teleportCooldown = 2f; // seconds

    [SerializeField] LayerMask obstacleMask;
    [SerializeField] float tileLength = 10f; // must match your ground
    [SerializeField] float tileWidth = 2.15f; // full lane width
    [SerializeField] float minClearanceWidth = 1.5f;
    [SerializeField] float raycastSpacing = 0.5f;
    [SerializeField] float rayHeightOffset = 1f; // cast from Y + this, down

    float cooldownRemaining;
    float stuckTimer;
    float lastTrackedZ; // Changed from lastIntZ
    NavMeshAgent agent;
    Vector3 lastDest;
    float _lastScannedTileZ = float.NegativeInfinity;
    FlyerCorridorScanner _flyer;
    float lastPlayerZ;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        player ??= GameObject.FindWithTag("Player")?.transform;
        lastTrackedZ = transform.position.z;
        lastPlayerZ = player ? player.position.z : 0f;

        _flyer = FindFirstObjectByType<FlyerCorridorScanner>();
        if (_flyer)
        {
            tileLength = _flyer.TileLength;
            if (obstacleMask.value == 0)
                obstacleMask = _flyer.ObstacleMask;
        }

        if (obstacleMask.value == 0)
        {
            obstacleMask = LayerMask.GetMask("Obstacles");
        }
    }

    void Update()
    {
        if (!player) return;

        // 1. Off-mesh recovery
        if (!agent.isOnNavMesh &&
            NavMesh.SamplePosition(transform.position, out var snap, 3f, NavMesh.AllAreas))
        {
            agent.Warp(snap.position);
            agent.ResetPath();
            Vector3 wantA = player.position + Vector3.forward * lookAhead;
            if (NavMesh.SamplePosition(wantA, out var hitA, 50f, NavMesh.AllAreas))
            {
                agent.SetDestination(hitA.position);
                lastDest = hitA.position;
            }
        }

        // 2. Update destination FIRST
        Vector3 want = player.position + Vector3.forward * lookAhead;
        if (NavMesh.SamplePosition(want, out var hit, 50f, NavMesh.AllAreas))
        {
            if ((hit.position - lastDest).sqrMagnitude > 1f)
            {
                agent.SetDestination(hit.position);
                lastDest = hit.position;
            }
        }

        // 3. Speed management
        float playerSpeedZ = (player.position.z - lastPlayerZ) / Time.deltaTime;
        lastPlayerZ = player.position.z;
        agent.speed = Mathf.Max(10f, Mathf.Abs(playerSpeedZ)) + extraSpeed;

        // 4. Corridor scanning
        float tileZ = Mathf.Floor(transform.position.z / tileLength) * tileLength;
        if (tileZ > _lastScannedTileZ)
        {
            _lastScannedTileZ = tileZ;
            ScanCorridor(tileZ);
        }

        // 5. Stuck detection
        if (cooldownRemaining > 0f)
        {
            cooldownRemaining -= Time.deltaTime;
            stuckTimer = 0f;
            return;
        }

        float currentZ = transform.position.z;
        bool noSpeed = agent.velocity.sqrMagnitude < 0.1f;

        if (Mathf.Abs(currentZ - lastTrackedZ) < 0.1f && noSpeed)
        {
            stuckTimer += Time.deltaTime;
        }
        else
        {
            stuckTimer = 0f;
            lastTrackedZ = currentZ;
        }

        // 6. Teleport if stuck
        if (stuckTimer >= stuckSeconds)
        {
            int targetZInt = Mathf.RoundToInt(currentZ) + zTeleportStep;
            Vector3 target = new Vector3(transform.position.x, transform.position.y, targetZInt);

            Vector3 warpPos = target;
            if (NavMesh.SamplePosition(target, out var snap2, 6f, NavMesh.AllAreas))
                warpPos = snap2.position;

            agent.Warp(warpPos);
            agent.ResetPath();
            Vector3 wantB = player.position + Vector3.forward * lookAhead;
            if (NavMesh.SamplePosition(wantB, out var hitB, 50f, NavMesh.AllAreas))
            {
                agent.SetDestination(hitB.position);
                lastDest = hitB.position;
            }

            stuckTimer = 0f;
            cooldownRemaining = teleportCooldown;
            lastTrackedZ = warpPos.z;
        }
    }

    // Replace the entire ScanCorridor method with this:
    void ScanCorridor(float tileZ)
    {
        float rayY = transform.position.y - rayHeightOffset;
        int rows = Mathf.CeilToInt(tileLength / raycastSpacing);
        var blockers = new List<Collider>();
        bool tilePassable = false;

        // Check every row in this tile
        for (int i = 0; i <= rows; i++)
        {
            float z = tileZ + i * raycastSpacing;
            bool rowHasGap = false;
            var rowBlockers = new List<Collider>();

            // Check gap for this specific row
            if (CheckRowForGap(z, rayY, rowBlockers))
            {
                rowHasGap = true;
                tilePassable = true; // At least one row is passable
            }

            // Collect all blockers from this row
            blockers.AddRange(rowBlockers);
        }

        // Report if entire tile is blocked
        if (!tilePassable && blockers.Count > 0)
        {
            var dtoList = new List<BlockageDetailDTO.CulpritInfo>();
            foreach (var col in blockers)
            {
                dtoList.Add(new BlockageDetailDTO.CulpritInfo
                {
                    name = col.transform.root.name.Replace("(Clone)", ""),
                    position = col.bounds.center,
                    size = col.bounds.size,
                    layer = LayerMask.LayerToName(col.gameObject.layer)
                });
            }

            BlockageReporter.ReportBlockage(tileZ, tileZ, dtoList);
            Debug.LogError($"[GHOST] Tile completely blocked at Z={tileZ}");
        }
    }

// New helper method for row checking
    bool CheckRowForGap(float z, float rayY, List<Collider> blockers)
    {
        float halfW = tileWidth * 0.5f;
        const float step = 0.05f;
        float currentGap = 0f;
        bool gapFound = false;

        for (float x = -halfW; x <= halfW; x += step)
        {
            Vector3 origin = new Vector3(x, rayY, z);

            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 5f, obstacleMask))
            {
                // Obstacle hit - reset gap counter
                currentGap = 0f;

                // Record blocker if new
                if (!blockers.Contains(hit.collider))
                    blockers.Add(hit.collider);
            }
            else
            {
                // Clear space - accumulate gap
                currentGap += step;

                // Check if we have sufficient clearance
                if (currentGap >= minClearanceWidth)
                {
                    gapFound = true;
                    // Don't break - continue to collect all blockers
                }
            }
        }

        return gapFound;
    }

    BlockageDetailDTO.CulpritInfo ToCulpritInfo(Collider col)
    {
        return new BlockageDetailDTO.CulpritInfo
        {
            name = col.transform.root.name.Replace("(Clone)", ""),
            position = col.bounds.center,
            size = col.bounds.size,
            layer = LayerMask.LayerToName(col.gameObject.layer)
        };
    }

    bool RowHasGap(float z, float rayY, List<Collider> culprits)
    {
        float halfW = tileWidth * 0.5f;
        const float stepX = 0.05f;
        float gap = 0f;
        bool rowHasGap = false;

        for (float x = -halfW; x <= halfW; x += stepX)
        {
            Vector3 origin = new Vector3(x, rayY, z);

            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 5f, obstacleMask))
            {
                gap = 0f;
                if (!culprits.Contains(hit.collider))
                    culprits.Add(hit.collider);
            }
            else
            {
                gap += stepX;
                if (gap >= minClearanceWidth)
                {
                    rowHasGap = true;
                    break;
                }
            }
        }

        return rowHasGap;
    }
}