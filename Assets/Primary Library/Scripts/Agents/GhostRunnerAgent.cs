using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class GhostRunnerAgent : MonoBehaviour
{
    [SerializeField] Transform player;
    [SerializeField] float   lookAhead = 300f;
    [SerializeField] float   extraSpeed = 4f;
    [SerializeField] float stuckSeconds   = 1f;  // time to consider “stuck”
    [SerializeField] int   zTeleportStep  = 2;   // jump +2 on Z (e.g., 108 → 110)
    [SerializeField] float teleportCooldown = 2f;   // seconds
    float cooldownRemaining;

    float stuckTimer;
    int   lastIntZ;

    NavMeshAgent agent;            // cache
    Vector3      lastDest;         // avoid spam

    void Awake()
    {
        agent  = GetComponent<NavMeshAgent>();
        player ??= GameObject.FindWithTag("Player")?.transform;
        lastIntZ = Mathf.RoundToInt(transform.position.z);

    }

    void Update()
    {
        if (!player) return;

        /* 1 ─ hop onto the mesh if carving deleted the old poly */
        if (!agent.isOnNavMesh &&
            NavMesh.SamplePosition(transform.position, out var snap, 3f, NavMesh.AllAreas))
        {
            agent.Warp(snap.position);
            agent.ResetPath();                                        // <<< clear old path
            Vector3 wantA = player.position + Vector3.forward * lookAhead;
            if (NavMesh.SamplePosition(wantA, out var hitA, 10f, NavMesh.AllAreas))
            {
                agent.SetDestination(hitA.position);                  // <<< re‑set
                lastDest = hitA.position;
            }
        }

        /* 2 ─ pick a point lookAhead metres ahead of the player */
        Vector3 want = player.position + Vector3.forward * lookAhead;

        if (!NavMesh.SamplePosition(want, out var hit, 10f, NavMesh.AllAreas)) return;

        if ((hit.position - lastDest).sqrMagnitude > 1f)
        {
            agent.SetDestination(hit.position);
            lastDest = hit.position;
        }

        /* 3 ─ keep a speed margin over the player */
        var v = player.GetComponent<Rigidbody>()?.linearVelocity.z ?? 0f;
        agent.speed = Mathf.Max(v + extraSpeed, 1f);
        if (cooldownRemaining > 0f)
        {
            cooldownRemaining -= Time.deltaTime;
            stuckTimer = 0f;                         // prevent retrigger while cooling down
            return;                                  // skip stuck logic this frame
        }
        int curIntZ = Mathf.RoundToInt(transform.position.z);
        bool noSpeed = agent.velocity.sqrMagnitude < 0.0001f;   // agent not moving

        if (curIntZ == lastIntZ && noSpeed)
        {
            stuckTimer += Time.deltaTime;
        }
        else
        {
            stuckTimer = 0f;
            lastIntZ   = curIntZ;
        }

        // 3) stuck teleport block
        if (stuckTimer >= stuckSeconds)
        {
            int targetZInt = curIntZ + zTeleportStep;
            Vector3 target = new Vector3(transform.position.x, transform.position.y, targetZInt);

            Vector3 warpPos = target;
            if (NavMesh.SamplePosition(target, out var snap2, 6f, NavMesh.AllAreas))
                warpPos = snap2.position;

            agent.Warp(warpPos);
            agent.ResetPath();                                        // <<< clear
            Vector3 wantB = player.position + Vector3.forward * lookAhead;
            if (NavMesh.SamplePosition(wantB, out var hitB, 10f, NavMesh.AllAreas))
            {
                agent.SetDestination(hitB.position);                  // <<< re‑set
                lastDest = hitB.position;
            }

            stuckTimer = 0f;
            cooldownRemaining = teleportCooldown;
            lastIntZ = Mathf.RoundToInt(transform.position.z);
        }

    }

    /* 4 ─ diagnostics */
    // void OnCollisionEnter(Collision c) =>
        // Debug.Log($"Ghost hit {c.gameObject.name} @ {Time.time:F2}s");
}