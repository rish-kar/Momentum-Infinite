using UnityEngine;

/// <summary>
/// This script is a unified camera controller that is responsible for the following tasks:
/// - Following the player in 2D and 3D modes.
/// - Switching between 2D and 3D camera modes with seamless transitions.
/// - Maintaining offsets (distance) for both modes.
/// - Ensuring camera reaching player with adjustable parameters.
/// </summary>
public class UnifiedCameraController : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform player; // Transform (position, rotation and scale values) of the player

    [Header("Camera Follow Parameters")] [SerializeField]
    private float _followPlayerSpeed = 5f; // The speed at which the camera reaches the player's position

    [SerializeField] private bool _useSmoothing = true;

    // Fixed Values for Camera Positioning behind/above the player
    // These offsets are used to position the camera correctly in both 2D and 3D modes.
    [Header("Camera Mode Parameters")] [SerializeField]
    private Vector3 _offset3D = new Vector3(0f, 5.34f, -3.52f);

    [SerializeField] private Vector3 _offset2D = new Vector3(0f, 11.3f, 6.3f);
    [SerializeField] private float _transitionDuration = 1.0f;

    [Header("Debug Triggers")] [SerializeField]
    private bool _debugCamera = false; // Minor debug option to check camera movements in inspector

    // Basic Variables to track the internal state of the camera
    private Vector3 _currentPlayerOffset;
    private Vector3 _cameraVelocity = Vector3.zero;
    private Quaternion _originalCameraRotation;
    private bool _is2DEnabled = false;
    private Vector3 _previousOffset3D;
    private Vector3 _previousOffset2D;
    private PlayerMovement _playerMovement; // Reference to the PlayerMovement script
    private Vector3 _initialPlayerPos; // Stores the initial transform position of the player

    // Variables to handle transitions between 2D and 3D modes
    private bool _isTransitioningNow = false;
    private float _transitioningTimer = 0f;
    private Vector3 _initialOffset;
    private Quaternion _initialiseRotation; // Quaternion is useed to handle rotations in Unity
    private Quaternion _targetRotation;

    private readonly Vector3 ROTATION_OFFSET_2D = new Vector3(90f, 0f, 0f);

    /// <summary>
    /// Start is called before the first frame update.
    /// </summary>
    void Start()
    {
        InitializeCamera();
    }

    /// <summary>
    /// Method to begin initialisation of the camera on the start of the game.
    /// Used for setting up the the player object reference, offsets and rotation.
    /// </summary>
    private void InitializeCamera()
    {
        // Validate player reference with multiple fallback methods
        if (player == null)
        {
            // Attempt to find the player script reference by type PlayerMovement
            // First attempt was to find the player by tag, but it was later switched since multiple character prefabs 
            // in future can have the same tag.
            _playerMovement = FindFirstObjectByType<PlayerMovement>();
            if (_playerMovement != null)
            {
                player = _playerMovement.transform;
            }

            if (player != null && _debugCamera)
            {
                Debug.Log($"Player auto-assigned: {player.name}", this);
            }
        }

        if (player == null)
        {
            Debug.LogError("CRITICAL: Player with PlayerMovement script not found in scene. Camera cannot follow.",
                this);
            enabled = false;
            return;
        }

        // Store the initial rotation of the camera before any modifications
        _originalCameraRotation = transform.rotation;

        // Store the initial position of the player
        _initialPlayerPos = player.position;

        // Initialising the offsets depending upon the mode
        _currentPlayerOffset = _offset3D;
        _previousOffset3D = _offset3D;
        _previousOffset2D = _offset2D;

        // Setting the target position according to the initial position + player offsets in X and Y Axis
        Vector3 targetPos = new Vector3(
            _initialPlayerPos
                .x, // Same X as initial player position as the camera is in 3rd Person and is positioned right behind the player in 3D view
            player.position.y +
            _currentPlayerOffset
                .y, // Y offset indicates that the height of the camera will be above the player so that view of the environment is clear while movement
            player.position.z +
            _currentPlayerOffset.z // Z offset indicates that the camera will be positioned behind the player in 3D mode
        );
        transform.position = targetPos;

        if (_debugCamera)
        {
            Debug.Log($"UnifiedCameraController initialised. Player: {player.name}, " +
                      $"Initial offset values: {_currentPlayerOffset}, Camera position: {transform.position}", this);
        }
    }

    /// <summary>
    /// This function is called whenever the inspector has offset values changed in play mode.
    /// It is useful for real-time dynamic adjustments without needing the game to be stopped.
    /// </summary>
    void OnValidate()
    {
        if (Application.isPlaying && player != null)
        {
            if (!_is2DEnabled && _offset3D != _previousOffset3D)
            {
                _currentPlayerOffset = _offset3D;
                _previousOffset3D = _offset3D;
                if (_debugCamera)
                {
                    Debug.Log($"3D Offset updated to: {_offset3D}", this);
                }
            }
            else if (_is2DEnabled && _offset2D != _previousOffset2D)
            {
                _currentPlayerOffset = _offset2D;
                _previousOffset2D = _offset2D;
                if (_debugCamera)
                {
                    Debug.Log($"2D Offset updated to: {_offset2D}", this);
                }
            }
        }
    }

    /// <summary>
    /// LateUpdate is called once per frame after all Update methods have been called.
    /// This is where we handle camera following the player and mode switching.
    /// It ensures that the camera updates after the player has moved, providing a smooth following effect and transitioning effect.
    /// </summary>
    void LateUpdate()
    {
        // Always check for player, disable if it gets destroyed
        if (player == null)
        {
            if (enabled && _debugCamera) Debug.LogError("Player reference lost. Disabling camera controller.", this);
            enabled = false; // Disable script if player is null
            return;
        }

        HandleInput();

        // If a transition is in progress, it handles position and rotation updates.
        // If a transition is not in progress, simple follow mechanism continues.
        if (_isTransitioningNow)
        {
            UpdateTransition();
        }
        else
        {
            FollowPlayer();
        }
    }

    /// <summary>
    /// Function used to switch between 2D and 3D camera modes.
    /// Triggers camera movement from 3rd person view to top-down view and vice versa.
    /// </summary>
    private void HandleInput()
    {
        // Toggle between 2D and 3D when "C" is pressed, "C" stands for Camera
        if (Input.GetKeyDown(KeyCode.C) && !_isTransitioningNow)
        {
            _is2DEnabled = !_is2DEnabled;
            BeginTransition();
        }
    }

    /// <summary>
    /// The function responsible for the primary movement to follow the player.
    /// Uses target position based on player's position and offset.
    /// </summary>
    private void FollowPlayer()
    {
        Vector3 targetPosition;

        if (_is2DEnabled)
        {
            // In 2D mode, follow normally
            targetPosition = player.position + _currentPlayerOffset;
        }
        else
        {
            // In 3D mode, follow Y and Z
            targetPosition = new Vector3(
                player.position.x + _currentPlayerOffset.x,
                player.position.y + _currentPlayerOffset.y,
                player.position.z + _currentPlayerOffset.z
            );
        }

        if (_useSmoothing)
        {
            // Using Lerp functionality for smooth transition
            transform.position = Vector3.Lerp(transform.position, targetPosition, _followPlayerSpeed * Time.deltaTime);
        }
        else
        {
            // Transition without smoothing (direct assignment)
            transform.position = targetPosition;
        }

        if (_debugCamera && Time.frameCount % 60 == 0) // Log once per second
        {
            Debug.Log($"Camera following - Mode: {(_is2DEnabled ? "2D" : "3D")}, " +
                      $"Player position: {player.position}, Camera position: {transform.position}, " +
                      $"Target position: {targetPosition}", this);
        }
    }

    /// <summary>
    /// Camera movement triggered by the mode switch from input function.
    /// </summary>
    private void BeginTransition()
    {
        _isTransitioningNow = true;
        _transitioningTimer = 0f;
        _initialOffset = _currentPlayerOffset;
        _initialiseRotation = transform.rotation;

        // Depending upon the mode that is selected on the basis of the flag, set offset and rotation
        Vector3 targetOffset = _is2DEnabled ? _offset2D : _offset3D;
        _targetRotation = _is2DEnabled ? Quaternion.Euler(ROTATION_OFFSET_2D) : _originalCameraRotation;
        _currentPlayerOffset = targetOffset;
    }

    /// <summary>
    /// Function to update the transition progress and increment the timer.
    /// Calculates normalised progress and ends the transition when it reaches 100%.
    /// </summary>
    private void UpdateTransition()
    {
        _transitioningTimer += Time.deltaTime;
        float transitionProgress = _transitioningTimer / _transitionDuration;

        if (transitionProgress >= 1f)
        {
            // Transition complete
            transitionProgress = 1f;
            _isTransitioningNow = false;
        }

        // Using Lerp and Slerp to interpolate the position and rotation smoothly
        Vector3 currentTargetOffset = Vector3.Lerp(_initialOffset, _currentPlayerOffset, transitionProgress);
        Quaternion currentTargetRotation = Quaternion.Slerp(_initialiseRotation, _targetRotation, transitionProgress);

        // Switch position based on the mode
        Vector3 targetPosition;
        if (_is2DEnabled)
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