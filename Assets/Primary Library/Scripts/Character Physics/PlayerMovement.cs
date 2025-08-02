using System.Collections;
using UnityEngine;

/// <summary>
/// This script is responsible for handling player movement, including running, jumping and handles
/// animation states for the player which is switched depending upon the actions performed.
/// </summary>
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")] [SerializeField]
    public float runSpeed = 76f; // Base running speed (forward force)

    [SerializeField] public float sideSpeed = 5f; // Speed for side movement (sideways force)
    [SerializeField] private float _jumpForce = 15f; // Force that is applied while jumping
    [SerializeField] private float _jumpForwardBoost = 4f; // Additional boost while jumping forward during running state

    [Header("Visual Settings")] [SerializeField]
    private float _angleOfLean = 15f; // Angle direction while applying sideways movement (rotates player direction)

    [SerializeField] private float _speedOfLean = 8f; // The speed at which the player is rotated to lean either left or right

    [Header("Object References")] [SerializeField]
    private Transform _playerModel; // Reference to the player object's positional components

    [SerializeField] private Rigidbody _rigidBodyComponent; // Variable to hold the Rigidbody component for the player (physics component)
    [SerializeField] private Animator _animatorComponent; // Variable to hold the component that handles animation states for the player (visual component)
    [SerializeField] private Transform _feetOfPlayer; // The reference to the _feetOfPlayer of the character for checking grounded state
    [SerializeField] private LayerMask _groundMask = ~0; // Mask to determine the ground (using bitwise operator here to avoid compile time dependency because of multiple layers being present)
    [SerializeField] private float _groundCheckDistance = 0.3f; // Distance between the _feetOfPlayer and the ground to check if the player is grounded

    [Header("Debug")] [SerializeField] private bool _movementDebugger = false; // Field to check test and debug movement

    [Header("Respawn Settings")] [SerializeField]
    private Animator _respawnAnimator; 
    private Vector3 _lastSafePosition; 
    [SerializeField] private float _respawnAnimationLength = 3f; // Length of respawn animation
    private bool _isRespawning = false;
    private float _respawnTimer = 0f;
    private int respawnCount = 0;
    [SerializeField] private const int maxRespawns = 3;
    [SerializeField] private GameManager gameManager; // Assign your GameManager in the inspector
    [SerializeField] private Transform spawnPoint;
    
    // Tracker variables to detemine the movement state
    private bool _isGrounded = true;
    private bool _isRunning;
    private bool _isJumping;
    private float _horizontalInput; // Special variable to check if there is horizontal input

    
    [SerializeField] private float fallingThreshold = 50f;
    private float groundYAtThreshold = 0f;
    private bool hasPassedThreshold = false;


    /// <summary>
    /// Gets the current speed of the player based on whether they are running or not.
    /// </summary>
    public float CurrentSpeed => _isRunning ? runSpeed : 0;
    
    /// <summary>
    /// Checks if player is grounded or not.
    /// </summary>
    public bool IsGrounded => _isGrounded;
    
    /// <summary>
    /// Checks if player is running or not.
    /// </summary>
    public bool IsRunning => _isRunning;
    
    /// <summary>
    /// Gets the forward running speed of the player.
    /// </summary>
    public float RunSpeed
    {
        get => runSpeed;
        set => runSpeed = value;
    }
    
    /// <summary>
    /// Gets or sets the sideways speed of the player.
    /// </summary>
    public float SideSpeed
    {
        get => sideSpeed;
        set => sideSpeed = value;
    }

    // Enumeration to hold animation states
    private enum AnimationState
    {
        Idle,
        Running,
        Left,
        Right,
        Jumping,
        Falling
    }
    private AnimationState _currentAnimatorState;
    
    /// <summary>
    /// Start is called before the first frame update.
    /// </summary>
    void Start()
    {
        InitializeComponents();
        _currentAnimatorState = AnimationState.Idle; // Player always starts in idle state (1st animation state triggered after scene loads)
        respawnCount = 0;
        if (_respawnAnimator) _respawnAnimator.gameObject.SetActive(false);
        
        if (spawnPoint) {
            transform.position = spawnPoint.position;
            _lastSafePosition = spawnPoint.position;
        }
    }

    /// <summary>
    /// Function to initialise the basic components needed for the player movement.
    /// </summary>
    private void InitializeComponents()
    {
        if (!_rigidBodyComponent) _rigidBodyComponent = GetComponent<Rigidbody>();
        if (!_animatorComponent) _animatorComponent = GetComponent<Animator>();

        _animatorComponent.applyRootMotion = false; // This prevents the animation from overriding the physics and creating unwanted movement
        _rigidBodyComponent.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        _rigidBodyComponent.interpolation = RigidbodyInterpolation.Interpolate; // Used for smooth movement
        _rigidBodyComponent.linearDamping = 2f; // Slows down rigidbody movement over time to give a more realistic approach

        // Starting with a safe position on ground
        _lastSafePosition = transform.position;
        _lastSafePosition.x = 1.075f; // Set initial X to center (since the width of the ground is 2.15f)

        // Set position using _lastSafePosition
        transform.position = new Vector3(
            _lastSafePosition.x,
            Mathf.Max(1f, _lastSafePosition.y),
            _lastSafePosition.z
        );

        // Setting flags to initial state for player.
        _isGrounded = true;
        _isJumping = false;
    }

    /// <summary>
    /// Function called once per frame.
    /// </summary>
    void Update()
    {
        HandleInput();
        SimpleGroundCheck();
        HandleAnimations();
        
        if (transform.position.y < -8f && !_isRespawning)
        {
            _isRespawning = true;
            if (respawnCount < maxRespawns)
            {
                respawnCount++;
                _isRespawning = true;
                transform.position = spawnPoint ? spawnPoint.position : Vector3.zero;

                _rigidBodyComponent.linearVelocity = Vector3.zero;
                _isGrounded = true;
                _isJumping = false;
                _currentAnimatorState = AnimationState.Idle;
                _animatorComponent.Play("Idle 2 - Crypto", 0, 0f);

                if (_respawnAnimator)
                {
                    _respawnAnimator.gameObject.SetActive(true);
                    _respawnAnimator.Play("Respawning", 0, 0f);
                }
                StartCoroutine(EndRespawn(_respawnAnimationLength));
            }
            else
            {
                _isRespawning = true;
                if (_respawnAnimator) _respawnAnimator.gameObject.SetActive(false);
                if (gameManager) gameManager.GameEnds();
            }
        }
        
        if (!hasPassedThreshold && transform.position.z > fallingThreshold)
        {
            groundYAtThreshold = transform.position.y;
            hasPassedThreshold = true;
        }

    }

    /// <summary>
    /// Function is usually called at certain fixed intervals.
    /// </summary>
    void FixedUpdate()
    {
        HandleMovement();
        HandleVisualLean();
    }

    
    /// <summary>
    /// Function for movement. W key is for running forward, A and D are for left and right movement.
    /// Space is used for jumping.
    /// </summary>
    private void HandleInput()
    {
        _isRunning = Input.GetKey(KeyCode.W);
        _horizontalInput = Input.GetAxisRaw("Horizontal");

        if (Input.GetKeyDown(KeyCode.Space) && _isGrounded && !_isJumping)
        {
            Jump();
        }
    }

    /// <summary>
    /// Checks if the position of the player _feetOfPlayer is touching the ground object.
    /// </summary>
    private void SimpleGroundCheck()
    {
        Vector3 rayStart = _feetOfPlayer ? _feetOfPlayer.position : transform.position;
        bool wasGrounded = _isGrounded;
        _isGrounded = Physics.Raycast(rayStart, Vector3.down, _groundCheckDistance, _groundMask);

        if (!wasGrounded && _isGrounded && _isJumping)
        {
            _isJumping = false;
            if (_movementDebugger) Debug.Log("LANDED - Jump cleared", this);
        }

        // Used to reset the position.
        if (_isGrounded && transform.position.y >= 0f)
        {
            _lastSafePosition = transform.position;
            _lastSafePosition.x = 1.075f; 
        }
    }

    /// <summary>
    /// Function used to handle the velocity and movement with physical force.
    /// </summary>
    private void HandleMovement()
    {
        if (_isRespawning) return; 
        
        Vector3 velocity = _rigidBodyComponent.linearVelocity;

        // Forward movement
        if (_isRunning && _isGrounded)
        {
            velocity.z = runSpeed;
        }
        else if (_isGrounded && !_isJumping)
        {
            velocity.z = Mathf.Lerp(velocity.z, 0f, 10f * Time.fixedDeltaTime); // Used to slow down the player when the W key is not pressed.
        }

        // Checking ground state and then applying force sideways to generate velocity.
        if (_isGrounded || _isJumping)
        {
            velocity.x = _horizontalInput * sideSpeed;
        }
        _rigidBodyComponent.linearVelocity = velocity;
    }

    /// <summary>
    /// Handles the animation states of the player.
    /// </summary>
    private void HandleAnimations()
    {
        if (!_animatorComponent || _isRespawning) return;
        

        // Handle death and respawn sequence - If player falls below a certain height, then trigger a series of events.
        // if (transform.position.y < -5f)
        // {
        //     if (_currentAnimatorState != AnimationState.Falling)
        //     {
        //         _currentAnimatorState = AnimationState.Falling;
        //         _animatorComponent.Play("Falling - Crypto");
        //     }
        //
        //     if (transform.position.y < -8f && !_isRespawning)
        //     {
        //         // Teleport the player to the last safe position immediately.
        //         transform.position = _lastSafePosition;
        //         _rigidBodyComponent.linearVelocity = Vector3.zero; // The velocity is reset to zero
        //         _isGrounded = true;
        //         _isJumping = false;
        //
        //         // Play the 2nd idle animation (Default Idle as Idle 1 is only triggered once)
        //         _currentAnimatorState = AnimationState.Idle;
        //         _animatorComponent.Play("Idle 2 - Crypto", 0, 0f);
        //
        //         // Respawning sequence triggered
        //         _isRespawning = true;
        //
        //         // Trigger the respawn animation screen
        //         if (_respawnAnimator != null)
        //         {
        //             _respawnAnimator.Play("Respawning", 0, 0f);
        //         }
        //
        //         // Start the timer to reset respawn state back to normal
        //         StartCoroutine(EndRespawn(_respawnAnimationLength));
        //         return;
        //     }
        //     return;
        // }


        // This part of the code is responsible for checking and updating the animation states
        AnimationState targetState = _currentAnimatorState;

        if (
            hasPassedThreshold &&
            !_isGrounded &&
            _rigidBodyComponent.linearVelocity.y < -0.1f &&
            transform.position.y < groundYAtThreshold - 0.1f // you can make this 0.2 or 0.5 for safety if your ground is uneven
        )
        {
            targetState = AnimationState.Falling;
        }
        else if (_isJumping)
        {
            targetState = AnimationState.Jumping;
        }
        else if (_isGrounded)
        {
            if (_isRunning)
            {
                targetState = AnimationState.Running;
            }
            else if (_horizontalInput < -0.1f)
            {
                targetState = AnimationState.Left;
            }
            else if (_horizontalInput > 0.1f)
            {
                targetState = AnimationState.Right;
            }
            else
            {
                targetState = AnimationState.Idle;
            }
        }
        // Change the animation to the target state if the current animation is not the target one
        if (targetState != _currentAnimatorState)
        {
            _currentAnimatorState = targetState;
            PlayAnimationForState(_currentAnimatorState);
        }
    }
    
    /// <summary>
    /// This function is used to finish the respawn sequence with a delay to give time for the animation to trigger.
    /// </summary>
    /// <param name="delay">Amount of time</param>
    /// <returns>Returns the respawning flag</returns>
    private IEnumerator EndRespawn(float delay)
    {
        yield return new WaitForSeconds(delay);
        _isRespawning = false;
        HandleAnimations();
    }

    /// <summary>
    /// Animation based on the current state. 
    /// </summary>
    /// <param name="state">Animation States</param>
    private void PlayAnimationForState(AnimationState state)
    {
        switch (state)
        {
            case AnimationState.Idle:
                _animatorComponent.Play("Idle 2 - Crypto");
                break;
            case AnimationState.Running:
                _animatorComponent.Play("Running - Crypto");
                break;
            case AnimationState.Left:
                _animatorComponent.Play("Left - Crypto");
                break;
            case AnimationState.Right:
                _animatorComponent.Play("Right - Crypto");
                break;
            case AnimationState.Jumping:
                _animatorComponent.Play("Jump - Crypto");
                break;
            case AnimationState.Falling:
                _animatorComponent.Play("Falling - Crypto");
                break;
        }
    }

    /// <summary>
    /// Function to handle the visual lean and rotation of the player model based on the horizontal side input.
    /// </summary>
    private void HandleVisualLean()
    {
        if (_playerModel == null) return;

        float targetLean = 0f;
        if (_isRunning && _isGrounded && Mathf.Abs(_horizontalInput) > 0.1f)
        {
            targetLean = -_horizontalInput * _angleOfLean;
        }

        Quaternion targetRotation = Quaternion.Euler(0, 0, targetLean);
        
        // Spherically linear interpolation to transition the rotation smoothly
        _playerModel.localRotation =
            Quaternion.Slerp(_playerModel.localRotation, targetRotation, _speedOfLean * Time.fixedDeltaTime);
    }

    /// <summary>
    /// The function to handle the jump action of the player.
    /// </summary>
    private void Jump()
    {
        _isJumping = true;
        _currentAnimatorState = AnimationState.Jumping;

        Vector3 jumpVelocity = _rigidBodyComponent.linearVelocity;
        jumpVelocity.y = _jumpForce;

        if (_isRunning)
        {
            jumpVelocity.z += _jumpForwardBoost;
        }

        _rigidBodyComponent.linearVelocity = jumpVelocity;

        if (_movementDebugger)
        {
            Debug.Log($"JUMPED: Running={_isRunning}", this);
        }
    }

    /// <summary>
    /// Getter function to get the Z-Axis
    /// </summary>
    /// <returns>Position of Z Axis</returns>
    public float ReturnZAxis() => transform.position.z;
}