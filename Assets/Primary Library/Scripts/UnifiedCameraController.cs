using UnityEngine;

public class UnifiedCameraController : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform player;
    
    [Header("Camera Follow Settings")]
    [SerializeField] private float followSpeed = 5f; // Using 1/followSpeed for SmoothDamp
    [SerializeField] private bool useSmoothing = true;
    
    [Header("Camera Mode Settings")]
    [SerializeField] private Vector3 offset3D = new Vector3(0f, 4.62f, -3.18f);
    [SerializeField] private Vector3 offset2D = new Vector3(0f, 11.3f, 6.3f);
    [SerializeField] private float transitionDuration = 1.0f;
    
    // Internal state
    private Vector3 currentOffset;
    private Vector3 velocity = Vector3.zero;
    private Quaternion originalRotation;
    private bool is2D = false;
    private Vector3 lastOffset3D;
    private Vector3 lastOffset2D;
    private PlayerMovement playerMovement; // Reference to player script
    
    // Transition state
    private bool isTransitioning = false;
    private float transitionTimer = 0f;
    private Vector3 startOffset;
    private Quaternion startRotation;
    private Quaternion targetRotation;

    void Start()
    {
        // Validate player reference
        if (player == null)
        {
            // Try to find player automatically
            var playerObject = FindFirstObjectByType<PlayerMovement>();
            if (playerObject == null)
            {
                Debug.LogError("CRITICAL: Player with PlayerMovement script not found in scene. Camera cannot follow.", this);
                enabled = false; // Disable this component if no player is found
                return;
            }
            player = playerObject.transform;
            Debug.Log($"Player auto-assigned: {player.name}", this);
        }

        // Store the camera's initial rotation for 3D mode
        originalRotation = transform.rotation;
        
        // Initialize with 3D offset
        currentOffset = offset3D;
        lastOffset3D = offset3D;
        lastOffset2D = offset2D;
        
        // Set initial position immediately to prevent first-frame jitter
        transform.position = player.position + currentOffset;
        
        Debug.Log($"UnifiedCameraController initialized. Player: {player.name}, Initial offset: {currentOffset}, Camera pos: {transform.position}", this);
    }

    void OnValidate()
    {
        // This triggers whenever inspector values change in play mode
        if (Application.isPlaying && player != null)
        {
            if (!is2D && offset3D != lastOffset3D)
            {
                currentOffset = offset3D;
                lastOffset3D = offset3D;
                transform.position = player.position + currentOffset; // Apply immediately
                Debug.Log($"3D Offset updated to: {offset3D}", this);
            }
            else if (is2D && offset2D != lastOffset2D)
            {
                currentOffset = offset2D;
                lastOffset2D = offset2D;
                transform.position = player.position + currentOffset; // Apply immediately
                Debug.Log($"2D Offset updated to: {offset2D}", this);
            }
        }
    }

    void LateUpdate()
    {
        // Always check for player, disable if it gets destroyed
        if (player == null) 
        {
            if(enabled) Debug.LogError("Player reference lost. Disabling camera controller.", this);
            enabled = false;
            return;
        }
        
        // Handle input for mode switching
        HandleInput();
        
        // If a transition is active, it handles position and rotation updates.
        // Otherwise, the standard follow logic runs.
        if (isTransitioning)
        {
            UpdateTransition();
        }
        else
        {
            FollowPlayer();
        }
    }
    
    private void HandleInput()
    {
        // Toggle between 2D and 3D when "C" is pressed
        if (Input.GetKeyDown(KeyCode.C) && !isTransitioning)
        {
            is2D = !is2D;
            BeginTransition();
        }
    }
    
    private void FollowPlayer()
    {
        Vector3 targetPosition = player.position + currentOffset;
        
        // Use Lerp for smoothing if enabled. It's generally more stable than SmoothDamp for camera follow.
        if (useSmoothing)
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition, followSpeed * Time.deltaTime);
        }
        else
        {
            transform.position = targetPosition;
        }
    }
    
    private void BeginTransition()
    {
        isTransitioning = true;
        transitionTimer = 0f;
        
        // Store starting values
        startOffset = currentOffset;
        startRotation = transform.rotation;
        
        // Set target values based on mode
        Vector3 targetOffset = is2D ? offset2D : offset3D;
        targetRotation = is2D ? Quaternion.Euler(90f, 0f, 0f) : originalRotation;
        
        Debug.Log($"Starting camera transition to {(is2D ? "2D" : "3D")} mode");
    }
    
    private void UpdateTransition()
    {
        transitionTimer += Time.deltaTime;
        float t = Mathf.Clamp01(transitionTimer / transitionDuration);
        
        // Smoothly interpolate offset and rotation
        Vector3 targetOffset = is2D ? offset2D : offset3D;
        currentOffset = Vector3.Lerp(startOffset, targetOffset, t);
        transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
        
        // CRITICAL: During transition, we must also update the position continuously
        transform.position = player.position + currentOffset;
        
        // End transition when complete
        if (t >= 1f)
        {
            EndTransition();
        }
    }
    
    private void EndTransition()
    {
        isTransitioning = false;
        
        // Ensure final values are set
        currentOffset = is2D ? offset2D : offset3D;
        transform.rotation = targetRotation;
        
        // Reset velocity for smooth resumption of following
        velocity = Vector3.zero;
        
        Debug.Log($"Camera transition completed. Mode: {(is2D ? "2D" : "3D")}");
    }
    
    private void ApplyImmediatePosition()
    {
        if (player == null) return;
        transform.position = player.position + currentOffset;
        velocity = Vector3.zero;
    }
    
    // Public methods for external control
    public void SetOffset(Vector3 newOffset)
    {
        if (isTransitioning) return; // Don't allow offset changes during transitions
        
        currentOffset = newOffset;
        ApplyImmediatePosition();
        Debug.Log($"Camera offset set manually to: {newOffset}");
    }
    
    public void SetMode2D(bool enable2D)
    {
        if (is2D == enable2D || isTransitioning) return;
        
        is2D = enable2D;
        BeginTransition();
    }
    
    public bool Is2DMode()
    {
        return is2D;
    }
    
    public bool IsTransitioning()
    {
        return isTransitioning;
    }
    
    // Force enable method for debugging
    [ContextMenu("Reset to 3D Mode")]
    public void ResetTo3D()
    {
        is2D = false;
        isTransitioning = false;
        currentOffset = offset3D;
        transform.rotation = originalRotation;
        ApplyImmediatePosition();
        Debug.Log("Camera reset to 3D mode");
    }
    
    [ContextMenu("Switch to 2D Mode")]
    public void SwitchTo2D()
    {
        if (!is2D && !isTransitioning)
        {
            is2D = true;
            BeginTransition();
        }
    }
    
    [ContextMenu("Force Update Camera Position")]
    public void ForceUpdatePosition()
    {
        if (player != null)
        {
            ApplyImmediatePosition();
            Debug.Log("Camera position force updated", this);
        }
        else
        {
            Debug.LogWarning("Cannot force update position, player reference is missing.", this);
        }
    }
}
