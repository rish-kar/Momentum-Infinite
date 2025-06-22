using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

public class ProceduralTerrain : MonoBehaviour
{
    //GameObjects Spawn
    [SerializeField]
    private GameObject _ground;
    [SerializeField]
    private GameObject _previousGround;
    [SerializeField]
    private GameObject _player;
    [SerializeField]
    private GameObject _groundContainer;
    [SerializeField]
    private GameObject _treePrefab;
    [SerializeField] 
    private NavMeshSurface navMeshSurface;
    [SerializeField] 
    private GhostRunnerAgent ghostRunnerAgent;
    
    //Stop Condition incase of player death
    public bool _stopSpawningTerrain = false;

    //Positions of Ground
    private Vector3 _previousGroundLoc;
    private Vector3 _positionOfGround;

    //Time Control Variables
    [SerializeField]
    private float _canFire = -1f;
    [SerializeField]
    private float _timeRate = 1f;

    //New Ground Data
    private float _newgroundZAxis;
    private float _newgroundXAxis;
    
    
    private float nextNavMeshUpdate;
    private float navMeshUpdateInterval = 1.0f; // rebuild every 1 second

    private void Awake()
    {
        // If values not assigned by inspector, find them by tag
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

    
    void Update()
    {
        _positionOfGround = _previousGround.transform.position;
        
        if(_positionOfGround.z > _player.transform.position.z + 1000)
        {
            _stopSpawningTerrain = true;
        }
        else
        {
            _stopSpawningTerrain = false;
        }
        
        if (Time.time > _canFire && _stopSpawningTerrain == false)    // restricts spawning
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
        //Debug.Log("Time Check ===== " + Time.time);
        _previousGroundLoc = new Vector3(_positionOfGround.x, _positionOfGround.y, (_positionOfGround.z + 96));
        //Spawning New Ground
        GameObject _newGround = Instantiate(_ground, _previousGroundLoc, Quaternion.identity);
        
        _newgroundZAxis = _newGround.transform.position.z;
        _newgroundXAxis = _newGround.transform.position.x;

        _newGround.transform.parent = _groundContainer.transform;   //Putting spawned grounds into an empty container
        _previousGround = _newGround; //Swap Logic
        
        if (_previousGround != null)
        {
            SpawnATree();
        }
        
        // IMPORTANT: update ghost agent's target to the newly spawned terrain
        if (ghostRunnerAgent != null)
        {
            ghostRunnerAgent.SetTarget(_newGround.transform);
        }
    }

    public void SpawnATree()
    {
        float treeX = Random.Range(-7.1f, 10.55f);
        float treeX2 = Random.Range(-7.1f, 10.55f);
        float treeX3 = Random.Range(-7.1f, 10.55f);
        
        //(_treePrefab, new Vector3(treeX, 0.4326f, Random.Range(_newgroundZAxis-5.0f,_newgroundZAxis+5.0f)), Quaternion.identity);
        Instantiate(_treePrefab, new Vector3(treeX2, 0.4326f, Random.Range(_newgroundZAxis - 5.0f, _newgroundZAxis + 5.0f)), Quaternion.identity);
        Instantiate(_treePrefab, new Vector3(treeX3, 0.4326f, Random.Range(_newgroundZAxis - 5.0f, _newgroundZAxis + 5.0f)), Quaternion.identity);

    }

    
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

        navMeshSurface.BuildNavMesh();
    }
}
