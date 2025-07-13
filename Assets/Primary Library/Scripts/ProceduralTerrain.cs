using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

public class ProceduralTerrain : MonoBehaviour
{
    [Header("Terrain Settings")] [SerializeField]
    private GameObject _ground;

    [SerializeField] private GameObject _previousGround;
    [SerializeField] private GameObject _groundContainer;
    [SerializeField] private GameObject _treePrefab;
    public bool _stopSpawningTerrain = false;

    [Header("Dynamic Spawning Settings")]
    [SerializeField] private float spawnDistanceAhead = 500f; // Always keep 500 units ahead
    [SerializeField] private float minSpawnRate = 0.1f; // Minimum time between spawns (fast player)
    [SerializeField] private float maxSpawnRate = 1f; // Maximum time between spawns (slow player)
    [SerializeField] private float groundTileLength = 96f; // Length of each ground tile

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

    // Private variables for terrain spawning logic
    private float _newgroundZAxis;
    private float _newgroundXAxis;
    private Vector3 _previousGroundLoc;
    private Vector3 _positionOfGround;
    private float lastPlayerZ; // Track player's last position for speed calculation

    private void Awake()
    {
        // Null checks and Initialize Reference Block
        {
            if (_player == null)
            {
                _player = GameObject.FindGameObjectWithTag("Player");
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
        _positionOfGround = _previousGround.transform.position;

        if (_positionOfGround.z > _player.transform.position.z + 1000)
        {
            _stopSpawningTerrain = true;
        }
        else
        {
            _stopSpawningTerrain = false;
        }

        if (Time.time > _canFire && _stopSpawningTerrain == false) // restricts spawning
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
        //// Debug.Log("Time Check ===== " + Time.time);
        _previousGroundLoc = new Vector3(_positionOfGround.x, _positionOfGround.y, (_positionOfGround.z + groundTileLength));

        if (skyboxChanger == null)
            skyboxChanger = FindObjectOfType<SkyboxChanger>();

        int variantIdx = skyboxChanger ? skyboxChanger.CurrentSkyboxIdx : 0;

        /* -------- choose ground that matches the current skybox -------- */
        int idx = skyboxChanger ? skyboxChanger.CurrentSkyboxIdx : 0; // 0-6
        string path = $"Prefabs/Shuffled Prefabs/Variant {variantIdx}/Ground_{variantIdx}";
        // Debug.Log($"Loading ground prefab from path: {path}");
        GameObject groundPrefab = Resources.Load<GameObject>(path);
        if (groundPrefab == null)
        {
            // Debug.LogError($"Ground prefab not found at {path}");
            groundPrefab = _ground; // fall back to whatever was set
        }


        //Spawning New Ground
        GameObject _newGround = Instantiate(groundPrefab, _previousGroundLoc, Quaternion.identity);

        var mgr = FindObjectOfType<EnvironmentObjectSpawnManager>();
        if (mgr) mgr.SpawnObjectsOnGround(_newGround);

        _newgroundZAxis = _newGround.transform.position.z;
        _newgroundXAxis = _newGround.transform.position.x;

        _newGround.transform.parent = _groundContainer.transform; //Putting spawned grounds into an empty container
        _previousGround = _newGround; //Swap Logic

        if (_previousGround != null)
        {
            // SpawnATree();
        }

        // IMPORTANT: update ghost agent's target to the newly spawned terrain
        if (ghostRunnerAgent != null)
        {
            ghostRunnerAgent.SetTarget(_newGround.transform);
        }

        tilesSinceLastBake++;
        if (tilesSinceLastBake >= bakeInterval)
        {
            tilesSinceLastBake = 0;
            StartCoroutine(RebuildNavmeshAsync()); // this calls the coroutine below
        }

        AdjustSpawnRate();
    }

    private void AdjustSpawnRate()
    {
        // Calculate the player's speed based on their movement since the last frame
        float playerSpeed = Mathf.Abs(_player.transform.position.z - lastPlayerZ) / Time.deltaTime;

        // Remap the player's speed to a spawn rate between minSpawnRate and maxSpawnRate
        _timeRate = Mathf.Clamp(maxSpawnRate - playerSpeed * 0.01f, minSpawnRate, maxSpawnRate);

        lastPlayerZ = _player.transform.position.z;
    }

    // public void SpawnATree()
    // {
    //     float treeX = Random.Range(-7.1f, 10.55f);
    //     float treeX2 = Random.Range(-7.1f, 10.55f);
    //     float treeX3 = Random.Range(-7.1f, 10.55f);
    //
    //     //(_treePrefab, new Vector3(treeX, 0.4326f, Random.Range(_newgroundZAxis-5.0f,_newgroundZAxis+5.0f)), Quaternion.identity);
    //     // Instantiate(_treePrefab, new Vector3(treeX2, 0.4326f, Random.Range(_newgroundZAxis - 5.0f, _newgroundZAxis + 5.0f)), Quaternion.identity);
    //     // Instantiate(_treePrefab, new Vector3(treeX3, 0.4326f, Random.Range(_newgroundZAxis - 5.0f, _newgroundZAxis + 5.0f)), Quaternion.identity);
    // }


    private void UpdateNavMeshSurface()
    {
        if (navMeshSurface == null)
        {
            GameObject navMeshObj = GameObject.FindGameObjectWithTag("Navigational Mesh");
            if (navMeshObj != null)
            {
                navMeshSurface = navMeshObj.GetComponent<NavMeshSurface>();
            }
        }

        // Calculate midpoint of spawned terrains based on player's forward position
        float navMeshLength = 1500f; // large enough to cover several spawned grounds ahead and behind
        float forwardOffset = navMeshLength / 2f - 100f; // offset forward so it's ahead of the player

        Vector3 navMeshPosition = new Vector3(
            0,
            0,
            _player.transform.position.z + forwardOffset
        );

        navMeshSurface.transform.position = navMeshPosition;
        navMeshSurface.size = new Vector3(600, 20, navMeshLength); // significantly increased size

        // navMeshSurface.BuildNavMesh();
    }

    IEnumerator RebuildNavmeshAsync()
    {
        if (bakeJob != null && !bakeJob.isDone) yield break; // still baking

        var sources = new List<NavMeshBuildSource>();
        NavMeshBuilder.CollectSources(
            navMeshSurface.transform, navMeshSurface.layerMask,
            navMeshSurface.useGeometry, navMeshSurface.defaultArea,
            new List<NavMeshBuildMarkup>(), sources);

        var bounds = new Bounds(navMeshSurface.transform.position, navMeshSurface.size);

        bakeJob = NavMeshBuilder.UpdateNavMeshDataAsync(
            bakedNavMesh,
            navMeshSurface.GetBuildSettings(),
            sources,
            bounds); // name arg → picks Bounds overload

        while (!bakeJob.isDone) yield return null; // main thread stays free
    }
}