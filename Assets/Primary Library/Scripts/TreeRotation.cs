using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreeRotation : MonoBehaviour
{
    [SerializeField]
    private float rotationSpeed = 50.0f;
    private PlayerMovement playerMovement;
    private float _playerPos;
    private float _previousTreeLoc;

    // Start is called before the first frame update
    void Start()
    {
        playerMovement = GameObject.Find("Player").GetComponent<PlayerMovement>();
    }

    // Update is called once per frame
    void Update()
    {
        _playerPos = playerMovement.ReturnZAxis();
        _previousTreeLoc = this.gameObject.transform.position.z + 50;

        transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime);
        if (_playerPos > _previousTreeLoc)
        {
            Destroy(this.gameObject);
        }
    }
}
