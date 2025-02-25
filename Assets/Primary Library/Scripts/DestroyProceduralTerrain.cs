using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyProceduralTerrain : MonoBehaviour
{

    //Ground GameObjects
    [SerializeField]
    private GameObject _previousGround;
    

    private PlayerMovement playerMovement;

    //GameObject Positions
    private float _previousGroundLoc;
    //private Vector3 _positionOfGround;
    private float _playerPos;

    private float _liftime = 10f;
  
    // Start is called before the first frame update
    void Start()
    {
        playerMovement = GameObject.Find("Player").GetComponent<PlayerMovement>();
    }

    // Update is called once per frame
    void Update()
    {
        _playerPos = playerMovement.ReturnZAxis();
        _previousGroundLoc = _previousGround.transform.position.z + 100;
       
        if (_playerPos > _previousGroundLoc)
        {
            DestroyGround();
        }     

    }

    public void DestroyGround()
    {
        Destroy(this.gameObject, 5.0f);
    }
}
