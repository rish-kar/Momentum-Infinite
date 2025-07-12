using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform player; // Reference to the Player GameObject
    public Vector3 offset = new Vector3(0, 5, -10);   // Fixed offset to prevent camera jumping

    void Awake()
    {
        // Use the fixed offset set in inspector or default offset
        // Do not calculate offset dynamically to prevent camera jumping
        if (player != null && offset == Vector3.zero)
        {
            offset = new Vector3(0, 5, -10); // Default camera offset
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