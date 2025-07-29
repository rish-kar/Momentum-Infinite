using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// The Procedural Terrain generation code that spawns ground tiles dynamically depending upon the position of the player.
/// This script has the responsibility of creating a pathway for the player to run on while maintaining the Navigational Mesh for AI agents.
/// </summary>
public class ProceduralTerrain : MonoBehaviour
{
    [Header("Terrain Settings")] [SerializeField]
    private GameObject _ground; // The object of prefab for the ground zero tile
    [SerializeField] private GameObject _previousGround; // Previous ground tile reference
    [SerializeField]
    private GameObject _groundContainer; // Parent container under which all the ground terrain spawned holds
    public bool
        stopSpawningTerrain =
            false; // Trigger flag to true depending upon the location of the player, if player stops, it will stop spawning the ground

    
    [Header("Dynamic Spawning Settings")] [SerializeField]
    private float
        _mininimumSpawnRate = 0.1f; // Minimum time that is needed between spawns (when player is fast in pace)
    [SerializeField]
    private float _maximumSpawnRate = 1f; // Maximum time that is needed between spawns (when player is slow in pace)
    [SerializeField] public float
        groundTileLength = 96f; // Length of each ground tile prefab (all prefabs have exactly same scale at Z Axis)

    
    [Header("Agents and Player Fields")] [SerializeField]
    private GhostRunnerAgent _ghostRunnerAgent; // Ground agent for surveillance 
    [SerializeField] private GameObject _player; // Referece to the player object


    [Header("Time Control Settings")] [SerializeField]
    private float _canFire = -1f; // Basic unit of time for restricting spawning of ground tiles
    [SerializeField] private float _timeRate = 1f;


    [Header("NavMesh Settings (Navigational Mesh for AI Agent)")] [SerializeField]
    private NavMeshSurface _navMeshSurface; // Navigational Mesh Surface component to build the navigational mesh upon
    private float _nextNavMeshUpdate;
    private NavMeshData _bakedNavMesh; // Data for the navigational mesh
    private NavMeshDataInstance _meshInstance; // Instance of navigational mesh data
    private AsyncOperation _bakeJob; // Navigational Mesh baking 
    private int _tilesSinceLastBake; // Counter of tiles spawned since the last bake
    private const int _bakeInterval = 5; // Interval to bake the tiles 


    private float _navMeshUpdateInterval = 1.0f; // rebuild every 1 second


    [Header("Other Script References")] [SerializeField]
    private SkyboxChanger _skyboxChanger;
    private EnvironmentObjectSpawnManager _objectSpawnManager; // Cache the spawn manager


    [Header("Debugger")] [SerializeField]
    private bool _debugTerrain = false;
    [SerializeField] private bool _debugObjectSpawning = false;

    // General Vairables for spawning
    private Vector3 _previousGroundLoc;
    private Vector3 _positionOfGround;
    private float _lastPlayerZ; // Track player's last position for speed calculation
    
    [SerializeField] float navMeshDistanceAhead = 600f; // Distance ahead of the player to include in the navigational mesh so that the runner agent is always ahead of the player
    [SerializeField] float navMeshDistanceBehind = 50f; // Including some extra distance behind the player for safety

    /// <summary>
    /// Triggered once the game starts.
    /// </summary>
    private void Awake()
    {
        // Null checks and basic initialisations
        {
            if (_player == null)
            {
                _player = GameObject.Find("Player");
                if (_player == null)
                {
                    // Fallback Mechanism: If Player not found directly, then find the object attached with the script
                    var playerMovement = FindFirstObjectByType<PlayerMovement>();
                    if (playerMovement != null)
                    {
                        _player = playerMovement.gameObject;
                    }
                }
            }

            _previousGround = GameObject.FindGameObjectWithTag("Initial Ground");

            // Ground container is the parent objec to store all spawned tiles
            if (_groundContainer == null)
            {
                _groundContainer = GameObject.FindGameObjectWithTag("Ground Container");
            }
            
            _objectSpawnManager = FindFirstObjectByType<EnvironmentObjectSpawnManager>();
            if (_objectSpawnManager == null && _debugObjectSpawning)
            {
                Debug.LogWarning("EnvironmentObjectSpawnManager not found in scene! Objects might not spawn. " +
                                 "Assign values in the inspector before proceeding.", this);
            }
        }

        // Baking for Navigational Mesh
        {
            _bakedNavMesh = new NavMeshData();
            _meshInstance = NavMesh.AddNavMeshData(_bakedNavMesh);
            UpdateNavMeshSurface(); // First update the position and size before baking
            _navMeshSurface.BuildNavMesh(); // synchronous, happens only here
        }
    }


    /// <summary>
    /// Update function is called once per frame.
    /// </summary>
    void Update()
    {
        if (_player == null)
        {
            if (_debugTerrain && Time.frameCount % 60 == 0) // Log once per second
            {
                Debug.LogError("Player reference lost in ProceduralTerrain!", this);
            }
            return;
        }

        _positionOfGround = _previousGround.transform.position;
        if (_positionOfGround.z > _player.transform.position.z + 1000) // If the player stops or falls down then in that case it stops spawning to save memory
        {
            stopSpawningTerrain = true;
        }
        else
        {
            stopSpawningTerrain = false;
        }

        if (Time.time > _canFire && stopSpawningTerrain == false) // restricts spawning mechanism
        {
            _canFire = Time.time + _timeRate; 
            StartSpawning();
        }

        if (Time.time >= _nextNavMeshUpdate)
        {
            UpdateNavMeshSurface(); 
            _nextNavMeshUpdate = Time.time + _navMeshUpdateInterval; // Updates the nav mesh with time
        }
    }


    /// <summary>
    /// This function adjusts the spawn rate of the tiles depending upon the speed of the player.
    /// </summary>
    private void AdjustSpawnRate()
    {
        if (_player == null) return;

        float playerSpeed = Mathf.Abs(_player.transform.position.z - _lastPlayerZ) / Time.deltaTime; // Calculating the speed of the player

        _timeRate = Mathf.Clamp(_maximumSpawnRate - playerSpeed * 0.01f, _mininimumSpawnRate, _maximumSpawnRate); // Multiply the speed of the player by the factor and then using clamping mechanism
        _lastPlayerZ = _player.transform.position.z;

        if (_debugTerrain && Time.frameCount % 120 == 0) 
        {
            Debug.Log($"Player speed: {playerSpeed:F2}, Spawn rate: {_timeRate:F2}", this);
        }
    }

    /// <summary>
    /// Start the spawning mechanism for the ground tiles using this function.
    /// </summary>
    private void StartSpawning()
    {
        _previousGroundLoc =
            new Vector3(_positionOfGround.x, _positionOfGround.y, (_positionOfGround.z + groundTileLength)); // Track the location of the previous ground to spawn the new one

        if (_skyboxChanger == null)
            _skyboxChanger = FindFirstObjectByType<SkyboxChanger>(); // Pull out the skybox details to pick the right prefab object

        int skyboxVariantIndex = _skyboxChanger ? _skyboxChanger.CurrentSkyboxIdx : 0;

        // Path at which prefab objects are being stored
        string path = $"Prefabs/Shuffled Prefabs/Variant {skyboxVariantIndex}/Ground_{skyboxVariantIndex}";

        GameObject groundPrefab = Resources.Load<GameObject>(path);
        if (groundPrefab == null)
        {
            if (_debugTerrain)
            {
                Debug.LogWarning($"Ground prefab not found at {path}, using fallback", this);
            }
            groundPrefab = _ground; // Using the previous ground prefab as a fallback mechanism as
                                    // there is no explicit way to pull out the right ground
        }

        //Actual Ground Spawning Logic
        GameObject _newGround = Instantiate(groundPrefab, _previousGroundLoc, Quaternion.identity);
        
        // Spawning objects on the newly spawned ground
        if (_objectSpawnManager != null)
        {
            _objectSpawnManager.SpawnObjectsOnGround(_newGround);
            if (_debugObjectSpawning)
            {
                Debug.Log($"Called SpawnObjectsOnGround for {_newGround.name}", this);
            }
        }
        _newGround.transform.parent = _groundContainer.transform; // Setting the parent container
        _previousGround = _newGround; 

        StartCoroutine(RebuildNavmeshAsync());
        AdjustSpawnRate();

        // Increase bake counter since the previous rebuild navigational mesh was called
        _tilesSinceLastBake++;
        if (_tilesSinceLastBake >= _bakeInterval ||
            transform.position.z - _lastPlayerZ > groundTileLength * 2)
        {
            _tilesSinceLastBake = 0;
            StartCoroutine(RebuildNavmeshAsync());
        }
    }

    /// <summary>
    /// Function to update the Navigational Mesh based on position of the player
    /// </summary>
    private void UpdateNavMeshSurface()
    {
        // Pull out values from the scene
        if (_navMeshSurface == null)
        {
            GameObject navigationalMeshObject = GameObject.FindGameObjectWithTag("Navigational Mesh");
            if (navigationalMeshObject != null) _navMeshSurface = navigationalMeshObject.GetComponent<NavMeshSurface>();
        }

        // In this case, no mesh will be built since references will be empty
        if (_navMeshSurface == null || _player == null) return;

        // Create and center NavMesh 300m ahead of player
        Vector3 playerPosition = _player.transform.position;
        float sizeZAxis = navMeshDistanceBehind + navMeshDistanceAhead;
        float centerZAxis = playerPosition.z + (navMeshDistanceAhead - navMeshDistanceBehind) * 0.5f;

        _navMeshSurface.transform.position = new Vector3(0f, 0f, centerZAxis);
        _navMeshSurface.size = new Vector3(600f, 20f, sizeZAxis);

        if (Mathf.Abs(playerPosition.z - _lastPlayerZ) > 50f)
        {
            StartCoroutine(RebuildNavmeshAsync()); // Manually call when the player position moves
        }
    }

    /// <summary>
    /// Rebuilds the navigational mesh for the runner agent to move on.
    /// </summary>
    /// <returns>Bake job</returns>
    IEnumerator RebuildNavmeshAsync()
    {
        // Let the bake job run completely
        while (_bakeJob != null && !_bakeJob.isDone) yield return null;

        // Collect the resources for Navigational Mesh
        var sources = new List<NavMeshBuildSource>();
        NavMeshBuilder.CollectSources(
            _navMeshSurface.transform, // root
            _navMeshSurface.layerMask,
            _navMeshSurface.useGeometry,
            _navMeshSurface.defaultArea,
            new List<NavMeshBuildMarkup>(),
            sources);

        // Set the bounds for the NavMesh
        // The bounds are set to cover the area around the player
        // This ensures that the NavMesh is built around the player and includes the newly spawned ground
        var bounds = new Bounds(
            _navMeshSurface.transform.position,
            _navMeshSurface.size);

        // If there is no baked NavMesh, create a new one
        _bakeJob = NavMeshBuilder.UpdateNavMeshDataAsync(
            _bakedNavMesh,
            _navMeshSurface.GetBuildSettings(),
            sources,
            bounds);

        yield return _bakeJob; // let the job complete first and then only return
    }

    /// <summary>
    /// Constructor to get the latest ground Z position.
    /// </summary>
    public float LatestGroundZ => _previousGround ? _previousGround.transform.position.z : 0f;
}