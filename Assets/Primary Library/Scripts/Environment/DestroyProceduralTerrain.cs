using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The task of this script is to primarily delete the procedurally generated ground element
/// once the player crosses it towards the positive direction of Z-Axis.
/// This keeps the game from creating multiple objects and allows Unity's garbage collector
/// to occassionally clear out deleted objects to keep consistent performance without any lag.
/// </summary>
public class DestroyProceduralTerrain : MonoBehaviour
{
    [SerializeField] private GameObject _previousGround;

    private PlayerMovement _playerMovement; // PlayerMovement script to track the location of player

    private float _previousGroundLocation; // Z-Axis exact location of the game object

    private float _playerPosition; // Position of the player on Z-Axis

    /// <summary>
    /// Called before the first frame of the game.
    /// </summary>
    void Start()
    {
        _playerMovement = GameObject.Find("Player").GetComponent<PlayerMovement>();
    }

    /// <summary>
    /// Update is called once per frame
    /// </summary>
    void Update()
    {
        _playerPosition = _playerMovement.ReturnZAxis();
        _previousGroundLocation = _previousGround.transform.position.z + 100;

        // Conditional check to make sure that player is always 400 units ahead before deleting the ground
        if (_playerPosition > _previousGroundLocation + 400)
        {
            DestroyGround();
        }
    }

    /// <summary>
    /// Ground deletion function.
    /// </summary>
    public void DestroyGround()
    {
        Destroy(this.gameObject, 5.0f);
    }
}