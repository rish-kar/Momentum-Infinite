using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform player; // Reference to the Player GameObject
    public Vector3 offset;   // Offset to maintain between the camera and the player

    void Awake()
    {
        // Only calculate the initial offset if no offset is manually set in the Inspector
        // This preserves the camera position you set in the Unity Editor
        if (player != null && offset == Vector3.zero)
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