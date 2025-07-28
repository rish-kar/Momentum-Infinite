using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;


public class ProceduralTerrain : MonoBehaviour
{
    [Header("Terrain Settings")] 
    [SerializeField] private GameObject _ground; // Prefab object for the ground tile
    [SerializeField] private GameObject _previousGround; // Holds the reference to the previous ground tile spawned
    [SerializeField] private GameObject _groundContainer; // Parent Container under which ground tiles will be spawned
    public bool stopSpawningTerrain = false;    // Trigger flag to true depending upon the location of the player, if player stops, it will stop spawning the ground

    [Header("Dynamic Spawning Settings")] 
    [SerializeField] private float _minSpawnRate = 0.1f; // Minimum time that is needed between spawns (when player is fast)
    [SerializeField] private float _maxSpawnRate = 1f; // Maximum time that is needed between spawns (when player is slow)
    [SerializeField] public float groundTileLength = 96f; // Length of each ground tile prefab

    [Header("Agents and Player")] [SerializeField]
    private GhostRunnerAgent _ghostRunnerAgent;
    [SerializeField] private GameObject _player;

    
    [Header("Time Control")] [SerializeField]
    private float _canFire = -1f;

    [SerializeField] private float _timeRate = 1f;


    [Header("NavMesh Settings")] [SerializeField]
    private NavMeshSurface _navMeshSurface;

    private float _nextNavMeshUpdate;
    private NavMeshData _bakedNavMesh;
    private NavMeshDataInstance _meshInstance;
    private AsyncOperation _bakeJob;
    private int _tilesSinceLastBake;
    private const int _bakeInterval = 5; // bake every 5 tiles


    private float _navMeshUpdateInterval = 1.0f; // rebuild every 1 second


    [Header("Script References")] [SerializeField]
    private SkyboxChanger _skyboxChanger;

    [Header("Debug Settings")] [SerializeField]
    private bool _debugTerrain = false;

    [SerializeField] private bool _debugObjectSpawning = false;

    // Private variables for terrain spawning logic
    private float _newgroundZAxis;
    private float _newgroundXAxis;
    private Vector3 _previousGroundLoc;
    private Vector3 _positionOfGround;
    private float _lastPlayerZ; // Track player's last position for speed calculation
    private EnvironmentObjectSpawnManager _objectSpawnManager; // Cache the spawn manager

    private const float NAVIGATIONAL_MESH_LOOKAHEAD_DISTANCE = 600f; // 300m ahead + 300m buffer

    // Called once when the game starts
    private void Awake()
    {
        // Null checks and basic initialisations
        {
            if (_player == null)
            {
                _player = GameObject.FindGameObjectWithTag("Player");
                if (_player == null)
                {
                    // Fallback: find by PlayerMovement component
                    var playerMovement = FindFirstObjectByType<PlayerMovement>();
                    if (playerMovement != null)
                    {
                        _player = playerMovement.gameObject;
                    }
                }
            }

            _previousGround = GameObject.FindGameObjectWithTag("Initial Ground");

            if (_groundContainer == null)
            {
                _groundContainer = GameObject.FindGameObjectWithTag("Ground Container");
            }

            // Cache the EnvironmentObjectSpawnManager
            _objectSpawnManager = FindFirstObjectByType<EnvironmentObjectSpawnManager>();
            if (_objectSpawnManager == null && _debugObjectSpawning)
            {
                Debug.LogWarning("EnvironmentObjectSpawnManager not found in scene! Objects will not spawn.", this);
            }

            // Log initialization status
            if (_debugTerrain)
            {
                Debug.Log($"ProceduralTerrain initialized - Player: {(_player ? _player.name : "NULL")}, " +
                          $"Ground Container: {(_groundContainer ? _groundContainer.name : "NULL")}, " +
                          $"Object Spawn Manager: {(_objectSpawnManager ? "Found" : "NULL")}", this);
            }
        }

        // Object Instantiation Block 
        {
            _bakedNavMesh = new NavMeshData();
            _meshInstance = NavMesh.AddNavMeshData(_bakedNavMesh);
            UpdateNavMeshSurface(); // position & size the surface once
            _navMeshSurface.BuildNavMesh(); // synchronous, happens only here
        }
    }


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

        if (_positionOfGround.z > _player.transform.position.z + 1000)
        {
            stopSpawningTerrain = true;
        }
        else
        {
            stopSpawningTerrain = false;
        }

        if (Time.time > _canFire && stopSpawningTerrain == false) // restricts spawning
        {
            _canFire = Time.time + _timeRate; //Time Control Formula
            StartSpawning(); //Spawn Function
        }

        if (Time.time >= _nextNavMeshUpdate)
        {
            UpdateNavMeshSurface();
            _nextNavMeshUpdate = Time.time + _navMeshUpdateInterval;
        }
    }


    private void StartSpawning()
    {
        if (_debugTerrain)
        {
            Debug.Log($"StartSpawning called - Time: {Time.time}, Player Z: {_player.transform.position.z}", this);
        }

        _previousGroundLoc =
            new Vector3(_positionOfGround.x, _positionOfGround.y, (_positionOfGround.z + groundTileLength));

        if (_skyboxChanger == null)
            _skyboxChanger = FindFirstObjectByType<SkyboxChanger>();

        int variantIdx = _skyboxChanger ? _skyboxChanger.CurrentSkyboxIdx : 0;

        /* -------- choose ground that matches the current skybox -------- */
        string path = $"Prefabs/Shuffled Prefabs/Variant {variantIdx}/Ground_{variantIdx}";
        if (_debugTerrain)
        {
            Debug.Log($"Loading ground prefab from path: {path}", this);
        }

        GameObject groundPrefab = Resources.Load<GameObject>(path);
        if (groundPrefab == null)
        {
            if (_debugTerrain)
            {
                Debug.LogWarning($"Ground prefab not found at {path}, using fallback", this);
            }

            groundPrefab = _ground; // fall back to whatever was set
        }


        //Spawning New Ground
        GameObject _newGround = Instantiate(groundPrefab, _previousGroundLoc, Quaternion.identity);

        // Spawn objects on the new ground
        if (_objectSpawnManager != null)
        {
            _objectSpawnManager.SpawnObjectsOnGround(_newGround);
            if (_debugObjectSpawning)
            {
                Debug.Log($"Called SpawnObjectsOnGround for {_newGround.name}", this);
            }
        }
        else
        {
            // Try to find the spawn manager again (in case it was created after this script)
            _objectSpawnManager = FindFirstObjectByType<EnvironmentObjectSpawnManager>();
            if (_objectSpawnManager != null)
            {
                _objectSpawnManager.SpawnObjectsOnGround(_newGround);
                if (_debugObjectSpawning)
                {
                    Debug.Log($"Found spawn manager and called SpawnObjectsOnGround for {_newGround.name}", this);
                }
            }
            else if (_debugObjectSpawning)
            {
                Debug.LogWarning($"No EnvironmentObjectSpawnManager found - objects not spawned on {_newGround.name}",
                    this);
            }
        }

        _newgroundZAxis = _newGround.transform.position.z;
        _newgroundXAxis = _newGround.transform.position.x;

        _newGround.transform.parent = _groundContainer.transform; //Putting spawned grounds into an empty container
        _previousGround = _newGround; //Swap Logic

        StartCoroutine(RebuildNavmeshAsync());

        AdjustSpawnRate();

        if (_debugTerrain)
        {
            Debug.Log($"Spawned new ground: {_newGround.name} at position {_newGround.transform.position}", this);
        }

        // Modify StartSpawning method (add at end)
        _tilesSinceLastBake++;
        if (_tilesSinceLastBake >= _bakeInterval ||
            transform.position.z - _lastPlayerZ > groundTileLength * 2)
        {
            _tilesSinceLastBake = 0;
            StartCoroutine(RebuildNavmeshAsync());
        }
    }

    private void AdjustSpawnRate()
    {
        if (_player == null) return;

        // Calculate the player's speed based on their movement since the last frame
        float playerSpeed = Mathf.Abs(_player.transform.position.z - _lastPlayerZ) / Time.deltaTime;

        // Remap the player's speed to a spawn rate between _minSpawnRate and _maxSpawnRate
        _timeRate = Mathf.Clamp(_maxSpawnRate - playerSpeed * 0.01f, _minSpawnRate, _maxSpawnRate);

        _lastPlayerZ = _player.transform.position.z;

        if (_debugTerrain && Time.frameCount % 120 == 0) // Log every 2 seconds
        {
            Debug.Log($"Player speed: {playerSpeed:F2}, Spawn rate: {_timeRate:F2}", this);
        }
    }

    private void UpdateNavMeshSurface()
    {
        if (_navMeshSurface == null)
        {
            GameObject navMeshObj = GameObject.FindGameObjectWithTag("Navigational Mesh");
            if (navMeshObj != null) _navMeshSurface = navMeshObj.GetComponent<NavMeshSurface>();
        }

        if (_navMeshSurface == null || _player == null) return;

        // Center NavMesh 300m ahead of player
        Vector3 playerPos = _player.transform.position;
        Vector3 navMeshCenter = new Vector3(
            0f,
            0f,
            playerPos.z + NAVIGATIONAL_MESH_LOOKAHEAD_DISTANCE / 2
        );

        _navMeshSurface.transform.position = navMeshCenter;
        _navMeshSurface.size = new Vector3(600f, 20f, NAVIGATIONAL_MESH_LOOKAHEAD_DISTANCE);

        // Rebuild immediately when player moves significantly
        if (Mathf.Abs(playerPos.z - _lastPlayerZ) > 50f)
        {
            StartCoroutine(RebuildNavmeshAsync());
        }
    }


    IEnumerator RebuildNavmeshAsync()
    {
        // avoid overlap: if a bake is running, wait for it
        while (_bakeJob != null && !_bakeJob.isDone) yield return null;

        // ► collect every collider/renderer under the surface volume ―
        var sources = new List<NavMeshBuildSource>();
        NavMeshBuilder.CollectSources(
            _navMeshSurface.transform, // root
            _navMeshSurface.layerMask,
            _navMeshSurface.useGeometry,
            _navMeshSurface.defaultArea,
            new List<NavMeshBuildMarkup>(),
            sources);

        var bounds = new Bounds(
            _navMeshSurface.transform.position,
            _navMeshSurface.size);

        _bakeJob = NavMeshBuilder.UpdateNavMeshDataAsync(
            _bakedNavMesh,
            _navMeshSurface.GetBuildSettings(),
            sources,
            bounds);

        yield return _bakeJob; // wait until finished
    }

    // public float LatestGroundZ
    // {
    //     get
    //     {
    //         return _previousGround     != null
    //             ? _previousGround.transform.position.z
    //             : 0f;
    //     }
    // }

    public float LatestGroundZ => _previousGround ? _previousGround.transform.position.z : 0f;
}