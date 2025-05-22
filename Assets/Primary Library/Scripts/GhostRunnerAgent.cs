using UnityEngine;
using UnityEngine.AI;

public class GhostRunnerAgent : MonoBehaviour
{
    private NavMeshAgent agent;
    private Transform target;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        if (agent != null && target != null)
        {
            agent.SetDestination(target.position);
        }
    }

    void Update()
    {
        if (agent != null && target != null && agent.isOnNavMesh)
        {
            agent.SetDestination(target.position);
        }
    }
}
