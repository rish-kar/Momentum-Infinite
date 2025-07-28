using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;


public class ProceduralTerrain : MonoBehaviour
{
    [Header("Terrain Settings")] 
    [SerializeField] private GameObject _ground;

    [SerializeField] private GameObject _previousGround;
    [SerializeField] private GameObject _groundContainer;
    [SerializeField] private GameObject _treePrefab;
    public bool stopSpawningTerrain = false;

    [Header("Dynamic Spawning Settings")] 
    [SerializeField] private float minSpawnRate = 0.1f; // Minimum time between spawns (fast player)
    [SerializeField] private float maxSpawnRate = 1f; // Maximum time between spawns (slow player)
    [SerializeField] public float groundTileLength = 96f; // Length of each ground tile

    [Header("Agents and Player")] [SerializeField]
    private GhostRunnerAgent ghostRunnerAgent;

    [SerializeField] private GameObject _player;


    [Header("Time Control")] [SerializeField]
    private float _canFire = -1f;

    [SerializeField] private float _timeRate = 1f;


    [Header("NavMesh Settings")] [SerializeField]
    private NavMeshSurface navMeshSurface;

    private float nextNavMeshUpdate;
    private NavMeshData bakedNavMesh;
    private NavMeshDataInstance meshInstance;
    private AsyncOperation bakeJob;
    private int tilesSinceLastBake;
    private const int bakeInterval = 5; // bake every 5 tiles


    private float navMeshUpdateInterval = 1.0f; // rebuild every 1 second


    [Header("Script References")] [SerializeField]
    private SkyboxChanger skyboxChanger;

    [Header("Debug Settings")] [SerializeField]
    private bool debugTerrain = false;

    [SerializeField] private bool debugObjectSpawning = false;

    // Private variables for terrain spawning logic
    private float _newgroundZAxis;
    private float _newgroundXAxis;
    private Vector3 _previousGroundLoc;
    private Vector3 _positionOfGround;
    private float lastPlayerZ; // Track player's last position for speed calculation
    private EnvironmentObjectSpawnManager objectSpawnManager; // Cache the spawn manager

    private const float NAVMESH_LOOKAHEAD = 600f; // 300m ahead + 300m buffer

    private readonly List<NavMeshDataInstance> liveMeshes = new();


    private void Awake()
    {
        // Null checks and Initialize Reference Block
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

            if (_treePrefab == null)
            {
                _treePrefab = GameObject.FindGameObjectWithTag("Environment Objects");
            }

            // Cache the EnvironmentObjectSpawnManager
            objectSpawnManager = FindFirstObjectByType<EnvironmentObjectSpawnManager>();
            if (objectSpawnManager == null && debugObjectSpawning)
            {
                Debug.LogWarning("EnvironmentObjectSpawnManager not found in scene! Objects will not spawn.", this);
            }

            // Log initialization status
            if (debugTerrain)
            {
                Debug.Log($"ProceduralTerrain initialized - Player: {(_player ? _player.name : "NULL")}, " +
                          $"Ground Container: {(_groundContainer ? _groundContainer.name : "NULL")}, " +
                          $"Object Spawn Manager: {(objectSpawnManager ? "Found" : "NULL")}", this);
            }
        }

        // Object Instantiation Block 
        {
            bakedNavMesh = new NavMeshData();
            meshInstance = NavMesh.AddNavMeshData(bakedNavMesh);
            UpdateNavMeshSurface(); // position & size the surface once
            navMeshSurface.BuildNavMesh(); // synchronous, happens only here
        }
    }


    void Update()
    {
        if (_player == null)
        {
            if (debugTerrain && Time.frameCount % 60 == 0) // Log once per second
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

        if (Time.time >= nextNavMeshUpdate)
        {
            UpdateNavMeshSurface();
            nextNavMeshUpdate = Time.time + navMeshUpdateInterval;
        }
    }


    private void StartSpawning()
    {
        if (debugTerrain)
        {
            Debug.Log($"StartSpawning called - Time: {Time.time}, Player Z: {_player.transform.position.z}", this);
        }

        _previousGroundLoc =
            new Vector3(_positionOfGround.x, _positionOfGround.y, (_positionOfGround.z + groundTileLength));

        if (skyboxChanger == null)
            skyboxChanger = FindFirstObjectByType<SkyboxChanger>();

        int variantIdx = skyboxChanger ? skyboxChanger.CurrentSkyboxIdx : 0;

        /* -------- choose ground that matches the current skybox -------- */
        string path = $"Prefabs/Shuffled Prefabs/Variant {variantIdx}/Ground_{variantIdx}";
        if (debugTerrain)
        {
            Debug.Log($"Loading ground prefab from path: {path}", this);
        }

        GameObject groundPrefab = Resources.Load<GameObject>(path);
        if (groundPrefab == null)
        {
            if (debugTerrain)
            {
                Debug.LogWarning($"Ground prefab not found at {path}, using fallback", this);
            }

            groundPrefab = _ground; // fall back to whatever was set
        }


        //Spawning New Ground
        GameObject _newGround = Instantiate(groundPrefab, _previousGroundLoc, Quaternion.identity);

        // Spawn objects on the new ground
        if (objectSpawnManager != null)
        {
            objectSpawnManager.SpawnObjectsOnGround(_newGround);
            if (debugObjectSpawning)
            {
                Debug.Log($"Called SpawnObjectsOnGround for {_newGround.name}", this);
            }
        }
        else
        {
            // Try to find the spawn manager again (in case it was created after this script)
            objectSpawnManager = FindFirstObjectByType<EnvironmentObjectSpawnManager>();
            if (objectSpawnManager != null)
            {
                objectSpawnManager.SpawnObjectsOnGround(_newGround);
                if (debugObjectSpawning)
                {
                    Debug.Log($"Found spawn manager and called SpawnObjectsOnGround for {_newGround.name}", this);
                }
            }
            else if (debugObjectSpawning)
            {
                Debug.LogWarning($"No EnvironmentObjectSpawnManager found - objects not spawned on {_newGround.name}",
                    this);
            }
        }

        _newgroundZAxis = _newGround.transform.position.z;
        _newgroundXAxis = _newGround.transform.position.x;

        _newGround.transform.parent = _groundContainer.transform; //Putting spawned grounds into an empty container
        _previousGround = _newGround; //Swap Logic

        if (_previousGround != null)
        {
            // SpawnATree();
        }

        // IMPORTANT: update ghost agent's target to the newly spawned terrain
        // if (ghostRunnerAgent != null)
        // {
        //     ghostRunnerAgent.SetTarget(_newGround.transform);


        StartCoroutine(RebuildNavmeshAsync());

        AdjustSpawnRate();

        if (debugTerrain)
        {
            Debug.Log($"Spawned new ground: {_newGround.name} at position {_newGround.transform.position}", this);
        }

        // Modify StartSpawning method (add at end)
        tilesSinceLastBake++;
        if (tilesSinceLastBake >= bakeInterval ||
            transform.position.z - lastPlayerZ > groundTileLength * 2)
        {
            tilesSinceLastBake = 0;
            StartCoroutine(RebuildNavmeshAsync());
        }
    }

    private void AdjustSpawnRate()
    {
        if (_player == null) return;

        // Calculate the player's speed based on their movement since the last frame
        float playerSpeed = Mathf.Abs(_player.transform.position.z - lastPlayerZ) / Time.deltaTime;

        // Remap the player's speed to a spawn rate between minSpawnRate and maxSpawnRate
        _timeRate = Mathf.Clamp(maxSpawnRate - playerSpeed * 0.01f, minSpawnRate, maxSpawnRate);

        lastPlayerZ = _player.transform.position.z;

        if (debugTerrain && Time.frameCount % 120 == 0) // Log every 2 seconds
        {
            Debug.Log($"Player speed: {playerSpeed:F2}, Spawn rate: {_timeRate:F2}", this);
        }
    }

    private void UpdateNavMeshSurface()
    {
        if (navMeshSurface == null)
        {
            GameObject navMeshObj = GameObject.FindGameObjectWithTag("Navigational Mesh");
            if (navMeshObj != null) navMeshSurface = navMeshObj.GetComponent<NavMeshSurface>();
        }

        if (navMeshSurface == null || _player == null) return;

        // Center NavMesh 300m ahead of player
        Vector3 playerPos = _player.transform.position;
        Vector3 navMeshCenter = new Vector3(
            0f,
            0f,
            playerPos.z + NAVMESH_LOOKAHEAD / 2
        );

        navMeshSurface.transform.position = navMeshCenter;
        navMeshSurface.size = new Vector3(600f, 20f, NAVMESH_LOOKAHEAD);

        // Rebuild immediately when player moves significantly
        if (Mathf.Abs(playerPos.z - lastPlayerZ) > 50f)
        {
            StartCoroutine(RebuildNavmeshAsync());
        }
    }


    IEnumerator RebuildNavmeshAsync()
    {
        // avoid overlap: if a bake is running, wait for it
        while (bakeJob != null && !bakeJob.isDone) yield return null;

        // ► collect every collider/renderer under the surface volume ―
        var sources = new List<NavMeshBuildSource>();
        NavMeshBuilder.CollectSources(
            navMeshSurface.transform, // root
            navMeshSurface.layerMask,
            navMeshSurface.useGeometry,
            navMeshSurface.defaultArea,
            new List<NavMeshBuildMarkup>(),
            sources);

        var bounds = new Bounds(
            navMeshSurface.transform.position,
            navMeshSurface.size);

        bakeJob = NavMeshBuilder.UpdateNavMeshDataAsync(
            bakedNavMesh,
            navMeshSurface.GetBuildSettings(),
            sources,
            bounds);

        yield return bakeJob; // wait until finished
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