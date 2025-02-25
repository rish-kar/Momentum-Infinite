using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    //Stop Condition incase of player death
    public bool _stopSpawningTerrain = false;

    //Positions of Ground
    private Vector3 _previousGroundLoc;
    private Vector3 _positionOfGround;
    private float _playerPos;

    //Count Variable
    private int _count = 1;

    //Time Control Variables
    [SerializeField]
    private float _canFire = -1f;
    [SerializeField]
    private float _timeRate = 2f;

    //New Ground Data
    private float _newgroundZAxis;
    private float _newgroundXAxis;
    
    private void Awake()
    {
        // If _player is not assigned via Inspector, find it by tag.
        if (_player == null)
        {
            _player = GameObject.FindGameObjectWithTag("Player");
        }
    
        // Other initialization code...
    }

    public void StartSpawning()
    {
        //Debug.Log("Time Check ===== " + Time.time);
        _previousGroundLoc = new Vector3(_positionOfGround.x, _positionOfGround.y, (_positionOfGround.z + 100));
        //Spawning New Ground
        GameObject _newGround = Instantiate(_ground, _previousGroundLoc, Quaternion.identity);
        
        _newgroundZAxis = _newGround.transform.position.z;
        _newgroundXAxis = _newGround.transform.position.x;

        _newGround.transform.parent = _groundContainer.transform;   //Putting spawned grounds into an empty container
        _count++;
        _previousGround = _newGround; //Swap Logic
        
        if (_previousGround != null)
        {
            SpawnATree();
        }

    }

    public void SpawnATree()
    {
        float treeX = Random.Range(-7.1f, 10.55f);
       float treeX2 = Random.Range(-7.1f, 10.55f);
        float treeX3 = Random.Range(-7.1f, 10.55f);

      //  Debug.Log("Tree Position : " + treeX);
       // Debug.Log("Tree 2 Position : " + treeX2);
        //Debug.Log("Tree 3 Position : " + treeX3);
        //(_treePrefab, new Vector3(treeX, 0.4326f, Random.Range(_newgroundZAxis-5.0f,_newgroundZAxis+5.0f)), Quaternion.identity);
        Instantiate(_treePrefab, new Vector3(treeX2, 0.4326f, Random.Range(_newgroundZAxis - 5.0f, _newgroundZAxis + 5.0f)), Quaternion.identity);
        Instantiate(_treePrefab, new Vector3(treeX3, 0.4326f, Random.Range(_newgroundZAxis - 5.0f, _newgroundZAxis + 5.0f)), Quaternion.identity);

    }

    void Update()
    {
        _positionOfGround = _previousGround.transform.position;
        _playerPos = _player.transform.position.z;
                
        if (Time.time > _canFire && _stopSpawningTerrain == false)    // restricts spawning
        {
            _canFire = Time.time + _timeRate; //Time Control Formula
            StartSpawning(); //Spawn Function
        }
    }

    float GetXAxisVal()
    {
        return _newgroundXAxis;
    }

    float GetZAxisVal()
    {
        return _newgroundZAxis;
    }
         
}
