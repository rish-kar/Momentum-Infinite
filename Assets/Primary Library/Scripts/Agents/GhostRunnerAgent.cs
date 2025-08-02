using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

/// <summary>
/// This class controls the runner agent that moves across the NavMesh to identify the issues.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class GhostRunnerAgent : MonoBehaviour
{
    [SerializeField] Transform player; // Reference to the position, rotation, and scale of the player object.
    [SerializeField] float lookAhead = 300f; // Distance to look ahead in the destination.
    [SerializeField] float boosterSpeed = 4f; // Speed to stay ahead of the player.
    [SerializeField] float stuckSeconds = 0.3f; // Workaround variable to determine if the agent is stuck.
    [SerializeField] int zTeleportStepDistance = 2; // Teleportation steps when stuck.
    [SerializeField] float cooldownTimer = 2f; // Cooldown timer before teleporting again.

    [SerializeField] LayerMask obstacleMask; // Mask for holding obstacle objects.
    [SerializeField] float tileLength = 10f; // Length of tiles in the corridor.
    [SerializeField] float widthOfTile = 2.15f; // Width of the corridor, complete X-Axis width.
    [SerializeField] float minClearanceWidth = 1.5f; 
    [SerializeField] float raycastGap = 0.5f; // Gaps between raycasts.
    [SerializeField] float rayHeightOffset = 1f; // Offset for the raycast height.

    // Extra variables and reference variables.
    float cooldownRemaining;
    float stuckTimer;
    float lastTrackedZ;
    NavMeshAgent agent;
    Vector3 lastDestination;
    float _lastScannedTileZAxis = float.NegativeInfinity;
    FlyerCorridorScanner flyerAgent;
    float lastPlayerZ;

    /// <summary>
    /// Called before the first frame update.
    /// </summary>
    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        player ??= GameObject.Find("Player")?.transform;
        lastTrackedZ = transform.position.z;
        lastPlayerZ = player ? player.position.z : 0f;

        flyerAgent = FindFirstObjectByType<FlyerCorridorScanner>();
        if (flyerAgent)
        {
            tileLength = flyerAgent.TileLength;
            if (obstacleMask.value == 0)
                obstacleMask = flyerAgent.ObstacleMask;
        }

        if (obstacleMask.value == 0)
            obstacleMask = LayerMask.GetMask("Obstacles");
    }

    /// <summary>
    /// Called once per frame.
    /// </summary>
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
            if ((hit.position - lastDestination).sqrMagnitude > 1f)
            {
                agent.SetDestination(hit.position);
                lastDestination = hit.position;
            }
        }

        float playerSpeedZ = (player.position.z - lastPlayerZ) / Time.deltaTime;
        lastPlayerZ = player.position.z;
        agent.speed = Mathf.Max(10f, Mathf.Abs(playerSpeedZ)) + boosterSpeed;

        // Ensure that the player is ahead before resetting.
        if (player.position.z > transform.position.z)
        {
            agent.Warp(new Vector3(player.position.x, transform.position.y, player.position.z + 100f));
            agent.ResetPath();
        }

        float tileZ = Mathf.Floor(transform.position.z / tileLength) * tileLength;
        if (tileZ > _lastScannedTileZAxis)
        {
            _lastScannedTileZAxis = tileZ;
            ScanCorridor(tileZ);
        }

        if (cooldownRemaining > 0f)
        {
            cooldownRemaining -= Time.deltaTime;
            stuckTimer = 0f;
            return;
        }

        float currentZAxis = transform.position.z;
        bool noSpeed = agent.velocity.sqrMagnitude < 0.1f;

        if (Mathf.Abs(currentZAxis - lastTrackedZ) < 0.1f && noSpeed)
            stuckTimer += Time.deltaTime;
        else
        {
            stuckTimer = 0f;
            lastTrackedZ = currentZAxis;
        }

        // If the agent is stuck due to NavMesh overlap, teleport forward.
        if (stuckTimer >= stuckSeconds)
        {
            int targetZInt = Mathf.RoundToInt(currentZAxis) + zTeleportStepDistance;
            Vector3 target = new Vector3(transform.position.x, transform.position.y, targetZInt);

            if (NavMesh.SamplePosition(target, out var snap2, 6f, NavMesh.AllAreas))
                agent.Warp(snap2.position);

            agent.ResetPath();
            stuckTimer = 0f;
            cooldownRemaining = cooldownTimer;
        }
    }

    /// <summary>
    /// Scan the corridor for obstacles and blockages.
    /// </summary>
    /// <param name="tileZ">The ground tile</param>
    void ScanCorridor(float tileZ)
    {
        float raycastYAxis = transform.position.y - rayHeightOffset;
        int rows = Mathf.CeilToInt(tileLength / raycastGap);

        var culpritsSet = new HashSet<Collider>();
        var culpritsDTO = new List<BlockageDetailDTO.CulpritInfo>();
        float firstBlockedZ = -1f;

        for (int i = 0; i <= rows; i++)
        {
            float z = tileZ + i * raycastGap;

            if (!CheckHorizontalClearance(z, raycastYAxis, culpritsSet, culpritsDTO))
            {
                if (firstBlockedZ < 0f)
                    firstBlockedZ = z;
            }
        }

        // Creating an overlap box simiar to the Flyer Agent.
        Collider[] hits = Physics.OverlapBox(new(transform.position.x, raycastYAxis + 2f, tileZ + tileLength * 0.5f), new(widthOfTile * 0.5f, 2f, tileLength * 0.5f), Quaternion.identity, obstacleMask);

        foreach (var collider in hits)
        {
            if (culpritsSet.Add(collider))
                culpritsDTO.Add(ToCulpritInfo(collider));
        }

        if (culpritsDTO.Count > 0)
            BlockageReporter.ReportBlockage(tileZ, firstBlockedZ < 0 ? tileZ : firstBlockedZ, culpritsDTO);
    }

    bool CheckHorizontalClearance(float z, float raycastYAxis, HashSet<Collider> culpritsSet, List<BlockageDetailDTO.CulpritInfo> culpritsDTO)
    {
        float halfWidth = widthOfTile * 0.5f;
        float gapWidth = 0f;

        for (float x = -halfWidth; x <= halfWidth; x += 0.05f)
        {
            if (Physics.Raycast(new(x, raycastYAxis, z), Vector3.down, out RaycastHit hit, 5f, obstacleMask))
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

    /// <summary>
    /// Publish info towards the Blockage Data Transfer Object (DTO).
    /// </summary>
    /// <param name="collider">Collider of the object</param>
    /// <returns></returns>
    BlockageDetailDTO.CulpritInfo ToCulpritInfo(Collider collider) => new()
    {
        name = collider.transform.root.name.Replace("(Clone)", ""),
        position = collider.bounds.center,
        size = collider.bounds.size,
        layer = LayerMask.LayerToName(collider.gameObject.layer)
    };
}
