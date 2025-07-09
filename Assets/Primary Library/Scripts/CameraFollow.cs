using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform player; // Reference to the Player GameObject
    public Vector3 offset;   // Offset to maintain between the camera and the player

    void Start()
    {
        // If no offset is set in the Inspector, calculate the initial offset
        if (player != null)
        {
            offset = transform.position - player.position;
        }
    }

    void LateUpdate()
    {
        // Ensure the camera follows the player while maintaining the offset
        if (player != null)
        {
            transform.position = player.position + offset;
        }
    }
}