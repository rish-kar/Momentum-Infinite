using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class GhostRunnerAgent : MonoBehaviour
{
    [SerializeField] Transform player;
    [SerializeField] float   lookAhead = 300f;
    [SerializeField] float   extraSpeed = 4f;

    NavMeshAgent agent;            // cache
    Vector3      lastDest;         // avoid spam

    void Awake()
    {
        agent  = GetComponent<NavMeshAgent>();
        player ??= GameObject.FindWithTag("Player")?.transform;
    }

    void Update()
    {
        if (!player) return;

        /* 1 ─ hop onto the mesh if carving deleted the old poly */
        if (!agent.isOnNavMesh &&
            NavMesh.SamplePosition(transform.position, out var snap, 3f, NavMesh.AllAreas))
            agent.Warp(snap.position);

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
    }

    /* 4 ─ diagnostics */
    // void OnCollisionEnter(Collision c) =>
        // Debug.Log($"Ghost hit {c.gameObject.name} @ {Time.time:F2}s");
}