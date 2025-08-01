using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

[RequireComponent(typeof(NavMeshAgent))]
public class GhostRunnerAgent : MonoBehaviour
{
    [SerializeField] Transform player;
    [SerializeField] float lookAhead = 300f;
    [SerializeField] float extraSpeed = 4f;
    [SerializeField] float stuckSeconds = 0.3f;
    [SerializeField] int zTeleportStep = 2;
    [SerializeField] float teleportCooldown = 2f;

    [SerializeField] LayerMask obstacleMask;
    [SerializeField] float tileLength = 10f;
    [SerializeField] float tileWidth = 2.15f;
    [SerializeField] float minClearanceWidth = 1.5f;
    [SerializeField] float raycastSpacing = 0.5f;
    [SerializeField] float rayHeightOffset = 1f;

    float cooldownRemaining;
    float stuckTimer;
    float lastTrackedZ;
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
            obstacleMask = LayerMask.GetMask("Obstacles");
    }

    void Update()
    {
        if (!player) return;

        if (!agent.isOnNavMesh && NavMesh.SamplePosition(transform.position, out var snap, 3f, NavMesh.AllAreas))
        {
            agent.Warp(snap.position);
            agent.ResetPath();
        }

        Vector3 want = player.position + Vector3.forward * lookAhead;
        if (NavMesh.SamplePosition(want, out var hit, 50f, NavMesh.AllAreas))
        {
            if ((hit.position - lastDest).sqrMagnitude > 1f)
            {
                agent.SetDestination(hit.position);
                lastDest = hit.position;
            }
        }

        float playerSpeedZ = (player.position.z - lastPlayerZ) / Time.deltaTime;
        lastPlayerZ = player.position.z;
        agent.speed = Mathf.Max(10f, Mathf.Abs(playerSpeedZ)) + extraSpeed;

        if (player.position.z > transform.position.z)
        {
            agent.Warp(new Vector3(player.position.x, transform.position.y, player.position.z + 100f));
            agent.ResetPath();
        }

        float tileZ = Mathf.Floor(transform.position.z / tileLength) * tileLength;
        if (tileZ > _lastScannedTileZ)
        {
            _lastScannedTileZ = tileZ;
            ScanCorridor(tileZ);
        }

        if (cooldownRemaining > 0f)
        {
            cooldownRemaining -= Time.deltaTime;
            stuckTimer = 0f;
            return;
        }

        float currentZ = transform.position.z;
        bool noSpeed = agent.velocity.sqrMagnitude < 0.1f;

        if (Mathf.Abs(currentZ - lastTrackedZ) < 0.1f && noSpeed)
            stuckTimer += Time.deltaTime;
        else
        {
            stuckTimer = 0f;
            lastTrackedZ = currentZ;
        }

        if (stuckTimer >= stuckSeconds)
        {
            int targetZInt = Mathf.RoundToInt(currentZ) + zTeleportStep;
            Vector3 target = new Vector3(transform.position.x, transform.position.y, targetZInt);

            if (NavMesh.SamplePosition(target, out var snap2, 6f, NavMesh.AllAreas))
                agent.Warp(snap2.position);

            agent.ResetPath();
            stuckTimer = 0f;
            cooldownRemaining = teleportCooldown;
        }
    }

    void ScanCorridor(float tileZ)
    {
        float rayY = transform.position.y - rayHeightOffset;
        int rows = Mathf.CeilToInt(tileLength / raycastSpacing);

        var culpritsSet = new HashSet<Collider>();
        var culpritsDTO = new List<BlockageDetailDTO.CulpritInfo>();
        float firstBlockedZ = -1f;

        for (int i = 0; i <= rows; i++)
        {
            float z = tileZ + i * raycastSpacing;

            if (!CheckHorizontalClearance(z, rayY, culpritsSet, culpritsDTO))
            {
                if (firstBlockedZ < 0f)
                    firstBlockedZ = z;
            }
        }

        Collider[] hits = Physics.OverlapBox(new(transform.position.x, rayY + 2f, tileZ + tileLength * 0.5f), new(tileWidth * 0.5f, 2f, tileLength * 0.5f), Quaternion.identity, obstacleMask);

        foreach (var collider in hits)
        {
            if (culpritsSet.Add(collider))
                culpritsDTO.Add(ToCulpritInfo(collider));
        }

        if (culpritsDTO.Count > 0)
            BlockageReporter.ReportBlockage(tileZ, firstBlockedZ < 0 ? tileZ : firstBlockedZ, culpritsDTO);
    }

    bool CheckHorizontalClearance(float z, float rayY, HashSet<Collider> culpritsSet, List<BlockageDetailDTO.CulpritInfo> culpritsDTO)
    {
        float halfWidth = tileWidth * 0.5f;
        float gapWidth = 0f;

        for (float x = -halfWidth; x <= halfWidth; x += 0.05f)
        {
            if (Physics.Raycast(new(x, rayY, z), Vector3.down, out RaycastHit hit, 5f, obstacleMask))
            {
                gapWidth = 0f;
                if (culpritsSet.Add(hit.collider))
                    culpritsDTO.Add(ToCulpritInfo(hit.collider));
            }
            else if ((gapWidth += 0.05f) >= minClearanceWidth)
                return true;
        }

        return false;
    }

    BlockageDetailDTO.CulpritInfo ToCulpritInfo(Collider col) => new()
    {
        name = col.transform.root.name.Replace("(Clone)", ""),
        position = col.bounds.center,
        size = col.bounds.size,
        layer = LayerMask.LayerToName(col.gameObject.layer)
    };
}
