using UnityEngine;

/// <summary>
/// Flyer Agent is the controller class for the Flying Agent.
/// The controller class is responsible for moving the agent across the map and ahead of the player.
/// This allows complete evaluation to trigger before the player even reaches a certain point.
/// </summary>
public class FlyerAgent : MonoBehaviour
{
    [Header("Object and Script References")]
    [SerializeField] private Transform          _player;      // The Player Game Object's position, rotation and scale
    [SerializeField] private ProceduralTerrain  _terrain;     // The Procedural Terrain Script used to spawn grounds

    [Header("Offsets Variables")]
    [SerializeField] private float _flyerHeight        = 12f;      // Y altitude of the flying agent
    [SerializeField] private float _lateralOffset = 0f;       // X offset from player object
    [SerializeField] private float _forwardOffset = 200f;     // Z gap - Difference between the player and the agent that needs to be maintained

    [Header("Variables for Smoothing Effect")]
    [SerializeField] private float _smoothEffectTime    = 0.6f;
    [Tooltip("Extra Space above player speed")]
    [SerializeField] private float _agentSpeedBuffer   = 5f;       
    
    Vector3 agentVelocity;
    float   lastPlayerZAxis;

    /// <summary>
    /// Triggered when the game starts.
    /// </summary>
    void Awake()
    {
        if (!_terrain)
            _terrain = FindFirstObjectByType<ProceduralTerrain>();

        if (TryGetComponent(out Rigidbody rigidBody))
            rigidBody.useGravity = false;   // Do not allow gravity to drag the agent down
    }

    /// <summary>
    /// Called once per frame.
    /// </summary>
    void Update()
    {
        // Null check for player
        if (!_player)   return;
        
        float targetZAxis = _player.position.z + _forwardOffset; // Ensures target location is position of player on Z-Axis + offset value (to stay ahead of the player)

        
        if (_terrain) targetZAxis = Mathf.Min(targetZAxis, _terrain.LatestGroundZ);  // Ensures that target is always set to the last spawned ground so that the agent does not fly into empty space
        
        Vector3 targetPosition = new(
            _player.position.x + _lateralOffset,
            _flyerHeight,
            targetZAxis);

        // Adapts the speed according to the speed of the player
        float playerSpeedZAxis = (_player.position.z - lastPlayerZAxis) / Time.deltaTime;
        float    maximumAgentSpeed  = Mathf.Abs(playerSpeedZAxis) + _agentSpeedBuffer;
        lastPlayerZAxis        = _player.position.z;

        // Using Smooth Damp for smooth movement
        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref agentVelocity,
            _smoothEffectTime,
            maximumAgentSpeed);
    }
}