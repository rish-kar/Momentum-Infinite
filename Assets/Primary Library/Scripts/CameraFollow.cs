using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform player; // Reference to the Player GameObject
    
    [Header("Camera Offset Settings")]
    [SerializeField] private Vector3 offset = new Vector3(0, 5, -10);   // Camera offset from player
    [SerializeField] private float followSpeed = 2f;                     // Slower follow speed for stability
    [SerializeField] private bool useSmoothing = true;                   // Enable/disable smooth following
    
    private Vector3 velocity = Vector3.zero;

    void Awake()
    {
        // Set initial position if player exists
        if (player != null)
        {
            transform.position = player.position + offset;
        }
    }

    void LateUpdate()
    {
        if (player == null) return;
        
        // Simple, stable camera following - NO COMPLEX CALCULATIONS
        Vector3 targetPosition = player.position + offset;
        
        // Apply camera movement with heavy smoothing for stability
        if (useSmoothing)
        {
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, followSpeed);
        }
        else
        {
            transform.position = targetPosition;
        }
    }
    
    // Public methods to adjust camera settings at runtime
    public void SetOffset(Vector3 newOffset)
    {
        offset = newOffset;
    }
    
    public void SetFollowSpeed(float newSpeed)
    {
        followSpeed = Mathf.Clamp(newSpeed, 0.1f, 10f);
    }
}