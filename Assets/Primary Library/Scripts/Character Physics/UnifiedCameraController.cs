using UnityEngine;

public class UnifiedCameraController : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform player;
    
    [Header("Camera Follow Settings")]
    [SerializeField] private float followSpeed = 5f;
    [SerializeField] private bool useSmoothing = true;
    
    [Header("Camera Mode Settings")]
    [SerializeField] private Vector3 offset3D = new Vector3(0f, 8.75f, -2f); // Fixed offset as requested
    [SerializeField] private Vector3 offset2D = new Vector3(0f, 11.3f, 6.3f);
    [SerializeField] private float transitionDuration = 1.0f;
    
    [Header("Debug Settings")]
    [SerializeField] private bool debugCamera = false;
    
    // Internal state
    private Vector3 currentOffset;
    private Vector3 velocity = Vector3.zero;
    private Quaternion originalRotation;
    private bool is2D = false;
    private Vector3 lastOffset3D;
    private Vector3 lastOffset2D;
    private PlayerMovement playerMovement;
    private Vector3 initialPlayerPosition; // Store initial player position for X reference
    
    // Transition state
    private bool isTransitioning = false;
    private float transitionTimer = 0f;
    private Vector3 startOffset;
    private Quaternion startRotation;
    private Quaternion targetRotation;

    void Start()
    {
        InitializeCamera();
    }
    
    private void InitializeCamera()
    {
        // Validate player reference with multiple fallback methods
        if (player == null)
        {
            // Method 1: Find by tag
            var playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                player = playerObject.transform;
                playerMovement = playerObject.GetComponent<PlayerMovement>();
            }
            else
            {
                // Method 2: Find by PlayerMovement component
                playerMovement = FindFirstObjectByType<PlayerMovement>();
                if (playerMovement != null)
                {
                    player = playerMovement.transform;
                }
            }
            
            if (player != null && debugCamera)
            {
                Debug.Log($"Player auto-assigned: {player.name}", this);
            }
        }

        if (player == null)
        {
            Debug.LogError("CRITICAL: Player with PlayerMovement script not found in scene. Camera cannot follow.", this);
            enabled = false;
            return;
        }

        // Store the camera's initial rotation for 3D mode
        originalRotation = transform.rotation;
        
        // Store initial player position for X reference
        initialPlayerPosition = player.position;
        
        // Initialize with 3D offset
        currentOffset = offset3D;
        lastOffset3D = offset3D;
        lastOffset2D = offset2D;
        
        // Set initial position immediately with correct X positioning
        Vector3 targetPos = new Vector3(
            initialPlayerPosition.x, // Same X as initial player position
            player.position.y + currentOffset.y,
            player.position.z + currentOffset.z
        );
        transform.position = targetPos;
        
        if (debugCamera)
        {
            Debug.Log($"UnifiedCameraController initialized. Player: {player.name}, " +
                     $"Initial offset: {currentOffset}, Camera pos: {transform.position}", this);
        }
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
                if (debugCamera)
                {
                    Debug.Log($"3D Offset updated to: {offset3D}", this);
                }
            }
            else if (is2D && offset2D != lastOffset2D)
            {
                currentOffset = offset2D;
                lastOffset2D = offset2D;
                if (debugCamera)
                {
                    Debug.Log($"2D Offset updated to: {offset2D}", this);
                }
            }
        }
    }

    void LateUpdate()
    {
        // Always check for player, disable if it gets destroyed
        if (player == null) 
        {
            if(enabled && debugCamera) Debug.LogError("Player reference lost. Disabling camera controller.", this);
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
        // Calculate target position with proper X axis handling
        Vector3 targetPosition;
        
        if (is2D)
        {
            // In 2D mode, follow normally
            targetPosition = player.position + currentOffset;
        }
        else
        {
            // In 3D mode, use initial player X position, follow Y and Z
            targetPosition = new Vector3(
                player.position.x + currentOffset.x,
                player.position.y + currentOffset.y,
                player.position.z + currentOffset.z
            );
        }
        
        // Apply smoothing if enabled
        if (useSmoothing)
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition, followSpeed * Time.deltaTime);
        }
        else
        {
            transform.position = targetPosition;
        }
        
        if (debugCamera && Time.frameCount % 60 == 0) // Log once per second
        {
            Debug.Log($"Camera following - Mode: {(is2D ? "2D" : "3D")}, " +
                     $"Player pos: {player.position}, Camera pos: {transform.position}, " +
                     $"Target pos: {targetPosition}", this);
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
        
        // Update current offset for the transition
        currentOffset = targetOffset;
        
        if (debugCamera)
        {
            Debug.Log($"Starting camera transition to {(is2D ? "2D" : "3D")} mode. " +
                     $"From offset: {startOffset} to {targetOffset}", this);
        }
    }
    
    private void UpdateTransition()
    {
        transitionTimer += Time.deltaTime;
        float t = transitionTimer / transitionDuration;
        
        if (t >= 1f)
        {
            // Transition complete
            t = 1f;
            isTransitioning = false;
            
            if (debugCamera)
            {
                Debug.Log($"Camera transition to {(is2D ? "2D" : "3D")} mode completed", this);
            }
        }
        
        // Smoothly interpolate offset and rotation
        Vector3 currentTargetOffset = Vector3.Lerp(startOffset, currentOffset, t);
        Quaternion currentTargetRotation = Quaternion.Slerp(startRotation, targetRotation, t);
        
        // Apply position based on interpolated offset
        Vector3 targetPosition;
        if (is2D)
        {
            targetPosition = player.position + currentTargetOffset;
        }
        else
        {
            targetPosition = new Vector3(
                player.position.x + currentTargetOffset.x,
                player.position.y + currentTargetOffset.y,
                player.position.z + currentTargetOffset.z
            );
        }
        
        transform.position = targetPosition;
        transform.rotation = currentTargetRotation;
    }
}
