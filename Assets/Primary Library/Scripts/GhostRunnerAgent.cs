using UnityEngine;

public class GhostRunnerAgent : MonoBehaviour
{
    private Transform _target;
    private UnityEngine.AI.NavMeshAgent _agent;

    void Start()
    {
        _agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
    }

    void Update()
    {
        if (_target != null)
        {
            _agent.destination = _target.position;
        }
    }

    // Public method to dynamically set the target
    public void SetTarget(Transform newTarget)
    {
        _target = newTarget;
    }
}
