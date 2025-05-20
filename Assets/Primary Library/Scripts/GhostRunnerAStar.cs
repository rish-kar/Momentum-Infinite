using UnityEngine;
// using Pathfinding;         // Namespace for A* components
//
// [RequireComponent(typeof(Rigidbody))]
// [RequireComponent(typeof(Seeker))]
public class GhostRunnerAStar : MonoBehaviour
{
    // [Header("A* Movement Settings")]
    // public float speed = 3f; 
    // public float nextWaypointDistance = 1f; 
    //
    // // Current path, set by A* after path calculation
    // private Path _path;
    // private int _currentWaypoint = 0;
    // private bool _reachedEndOfPath = false;
    //
    // // References
    // private Seeker _seeker;
    // private Rigidbody _rb;
    //
    // // Who or what we want to chase
    // private Transform _target;
    //
    // void Start()
    // {
    //     // Cache required components
    //     _seeker = GetComponent<Seeker>();
    //     _rb = GetComponent<Rigidbody>();
    //
    //     // Optionally disable gravity if you want a top-down runner
    //     //_rb.useGravity = false; 
    // }
    //
    // void Update()
    // {
    //     // Recalculate path occasionally (e.g. every second) or if you have a moving target
    //     // Only recalc if a path calculation isn't already in progress
    //     if (_target != null && _seeker.IsDone())
    //     {
    //         _seeker.StartPath(_rb.position, _target.position, OnPathComplete);
    //     }
    // }
    //
    // void FixedUpdate()
    // {
    //     // If no path, do nothing
    //     if (_path == null) return;
    //
    //     // Check if we've reached end of path
    //     if (_currentWaypoint >= _path.vectorPath.Count)
    //     {
    //         _reachedEndOfPath = true;
    //         return;
    //     }
    //     else
    //     {
    //         _reachedEndOfPath = false;
    //     }
    //
    //     // Compute direction from current position to next waypoint
    //     Vector3 direction = 
    //         ((Vector3)_path.vectorPath[_currentWaypoint] - _rb.position).normalized 
    //         * speed * Time.fixedDeltaTime;
    //
    //     // Move the Rigidbody in that direction
    //     // Using MovePosition maintains collision detection properly
    //     _rb.MovePosition(_rb.position + direction);
    //
    //     // Check distance to next waypoint
    //     float distance = Vector3.Distance(_rb.position, _path.vectorPath[_currentWaypoint]);
    //     if (distance < nextWaypointDistance)
    //     {
    //         _currentWaypoint++;
    //     }
    // }
    //
    // // Called by Seeker when a new path is computed
    // void OnPathComplete(Path p)
    // {
    //     if (!p.error)
    //     {
    //         _path = p;
    //         _currentWaypoint = 0;
    //     }
    // }
    //
    // // Public method for ProceduralTerrain to update the Ghost's target
    // public void SetTarget(Transform newTarget)
    // {
    //     _target = newTarget;
    // }
}
