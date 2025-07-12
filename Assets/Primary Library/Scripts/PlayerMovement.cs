using System.Collections;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Physics Related Fields")]
    [SerializeField] private float baseSpeed = 6f;           // Realistic movement speed
    [SerializeField] private float maxSpeed = 12f;           // Realistic max speed
    [SerializeField] private float acceleration = 3f;        // Increased for better responsiveness
    [SerializeField] private float forwardForceMultiplier = 15f; // Increased for actual movement
    [SerializeField] private float sideSpeed = 8f;           // Reduced as requested
    [SerializeField] private float jumpForce = 7f;           // Realistic jump height
    [SerializeField] private float jumpForwardBoost = 4f;    // Realistic forward momentum
    [SerializeField] private float jumpCooldown = 0.1f;      // Quick jump response

    [Header("Object References")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Animator anim;

    [Header("Ground-check")]
    [SerializeField] Transform feet; // empty at sole level
    [SerializeField] float groundRadius = 0.25f;
    [SerializeField] LayerMask groundMask = ~0;
    [SerializeField] float groundedRay = 0.15f;

    bool isGrounded; // updated every frame

    [Header("Environment Tracking")]
    [SerializeField] private int environmentUpdateInterval = 150;
    private int nextEnvironmentUpdate;

    [Header("Respawn Settings")]
    [SerializeField] private int maxRespawns = 3;
    private int respawnCount;
    private GameObject lastGroundBeforeDeath;
    [SerializeField] private GameObject respawnScreen;
    private bool isDeadOrRespawning;
    private bool hasTriggeredDeathAnimation;

    [Header("Flags")]
    [SerializeField] private bool isJumping;
    private float currentSpeed;
    private bool isRunning;
    private float horizontalInput;
    private bool isFalling;
    private float fallingTimer;
    private bool isInRunToStopTransition;
    private bool justRespawned; // New flag to handle respawn state
    private bool actuallyFalling; // Track if player is actually falling off platform

    // Animation state hashes for performance
    private static readonly int GroundedHash = Animator.StringToHash("Grounded");
    private static readonly int RunHash = Animator.StringToHash("Run");
    private static readonly int JumpHash = Animator.StringToHash("Jump");

    // --------------------------- Unity Methods ---------------------------

    private void Awake()
    {
        InitializeComponents();
        currentSpeed = baseSpeed;
        nextEnvironmentUpdate = environmentUpdateInterval;
        Debug.Log("[PlayerMovement] Awake - Components initialized");
    }

    private void InitializeComponents()
    {
        if (!rb) rb = GetComponent<Rigidbody>();
        if (!anim) anim = GetComponent<Animator>();

        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.linearDamping = 1f; // Reduced drag for better movement
        
        // Ensure player starts above ground
        CorrectPlayerPosition();
        Debug.Log("[PlayerMovement] Components initialized successfully");
    }

    private void Update()
    {
        HandleInput();
        CheckGroundStatus();
        HandleAnimations();
        CorrectPlayerPosition();
        UpdateEnvironmentTracking();
        CheckDeathCondition();
        
        // Debug current state every few frames
        if (Time.frameCount % 60 == 0)
        {
            Debug.Log($"[PlayerMovement] State - Grounded: {isGrounded}, Running: {isRunning}, Jumping: {isJumping}, Falling: {isFalling}, Position: {transform.position}, Velocity: {rb.linearVelocity}");
        }
    }

    private void FixedUpdate()
    {
        HandleMovement();
    }

    // --------------------------- Core Methods ---------------------------

    private void HandleInput()
    {
        // COMPREHENSIVE INPUT DEBUGGING
        if (Input.GetKeyDown(KeyCode.W)) Debug.Log("[INPUT] W KEY DOWN - Start Running");
        if (Input.GetKeyUp(KeyCode.W)) Debug.Log("[INPUT] W KEY UP - Stop Running");
        if (Input.GetKeyDown(KeyCode.Space)) Debug.Log("[INPUT] SPACE KEY DOWN - Jump Attempt");
        if (Input.GetKeyDown(KeyCode.A)) Debug.Log("[INPUT] A KEY DOWN - Left Movement");
        if (Input.GetKeyDown(KeyCode.D)) Debug.Log("[INPUT] D KEY DOWN - Right Movement");
        if (Input.GetKeyDown(KeyCode.LeftArrow)) Debug.Log("[INPUT] LEFT ARROW DOWN");
        if (Input.GetKeyDown(KeyCode.RightArrow)) Debug.Log("[INPUT] RIGHT ARROW DOWN");
        if (Input.GetKeyDown(KeyCode.R)) Debug.Log("[INPUT] R KEY DOWN - Teleport Request");
        
        // R button teleport functionality
        if (Input.GetKeyDown(KeyCode.R))
        {
            TeleportToLastGround();
            return; // Exit early to prevent other input processing during teleport
        }

        // Forward movement input
        bool wasRunning = isRunning;
        isRunning = Input.GetKey(KeyCode.W);
        horizontalInput = Input.GetAxisRaw("Horizontal");

        // Log every change in running state
        if (wasRunning != isRunning)
        {
            Debug.Log($"[INPUT] Running state changed: {wasRunning} -> {isRunning}");
            Debug.Log($"[INPUT] W Key currently pressed: {Input.GetKey(KeyCode.W)}");
        }

        // Log horizontal input changes
        float previousHorizontal = horizontalInput;
        if (previousHorizontal != horizontalInput)
        {
            Debug.Log($"[INPUT] Horizontal input changed: {previousHorizontal} -> {horizontalInput}");
        }

        // Jump input - IMMEDIATE response
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded && !isJumping)
        {
            Debug.Log("[INPUT] Space pressed - attempting jump");
            TryJump();
        }
        else if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log($"[INPUT] Jump blocked - Grounded: {isGrounded}, Already Jumping: {isJumping}");
        }
        
        // Handle Left/Right movements ONLY during Idle states and when grounded
        if (isGrounded && !isJumping && !isRunning && !isInRunToStopTransition)
        {
            var currentState = anim.GetCurrentAnimatorStateInfo(0);
            bool isInIdleState = currentState.IsName("Idle 2 - Crypto") || currentState.IsName("Idle - Crypto");
            
            Debug.Log($"[INPUT] Side movement check - Grounded: {isGrounded}, Jumping: {isJumping}, Running: {isRunning}, InTransition: {isInRunToStopTransition}, InIdleState: {isInIdleState}");
            
            if (isInIdleState)
            {
                // Left movement
                if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
                {
                    Debug.Log("[INPUT] LEFT MOVEMENT TRIGGERED - Conditions met");
                    PerformSideMovement(true); // true for left
                }
                
                // Right movement
                if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
                {
                    Debug.Log("[INPUT] RIGHT MOVEMENT TRIGGERED - Conditions met");
                    PerformSideMovement(false); // false for right
                }
            }
            else
            {
                Debug.Log($"[INPUT] Side movement BLOCKED - Not in idle state. Current animation: {currentState.shortNameHash}");
            }
        }
        else
        {
            // Log why side movement is blocked
            if (!isGrounded) Debug.Log("[INPUT] Side movement BLOCKED - Not grounded");
            if (isJumping) Debug.Log("[INPUT] Side movement BLOCKED - Currently jumping");
            if (isRunning) Debug.Log("[INPUT] Side movement BLOCKED - Currently running");
            if (isInRunToStopTransition) Debug.Log("[INPUT] Side movement BLOCKED - In stop transition");
        }
    }

    private void PerformSideMovement(bool isLeft)
    {
        string direction = isLeft ? "Left" : "Right";
        float forceMultiplier = isLeft ? -1f : 1f;
        
        Debug.Log($"[PlayerMovement] Performing {direction} movement");
        
        // Check if animation exists, otherwise use Idle 2
        if (HasAnimationClip($"{direction} - Crypto"))
        {
            anim.Play($"{direction} - Crypto", 0, 0f);
            Debug.Log($"[PlayerMovement] Playing {direction} animation");
        }
        else
        {
            Debug.LogWarning($"[PlayerMovement] {direction} animation not found, staying in idle");
        }
        
        // Apply smooth side force with reduced speed
        rb.AddForce(forceMultiplier * sideSpeed, 0, 0, ForceMode.VelocityChange);
        Debug.Log($"[PlayerMovement] Applied {direction} force: {forceMultiplier * sideSpeed}");
    }

    private void CheckGroundStatus()
    {
        bool wasGrounded = isGrounded;

        // More robust ground detection with multiple raycasts
        isGrounded = Physics.SphereCast(
            feet.position + Vector3.up * 0.1f,
            groundRadius,
            Vector3.down,
            out _,
            groundedRay + 0.1f, // Slightly longer raycast
            groundMask,
            QueryTriggerInteraction.Ignore);

        // Additional raycast from center of player for side movement stability
        if (!isGrounded)
        {
            isGrounded = Physics.Raycast(
                transform.position + Vector3.up * 0.2f,
                Vector3.down,
                groundedRay + 0.3f,
                groundMask,
                QueryTriggerInteraction.Ignore);
        }

        if (wasGrounded != isGrounded)
        {
            Debug.Log($"[PlayerMovement] Ground status changed: {wasGrounded} -> {isGrounded}");
            
            // Only start tracking actual falling when leaving ground AND not jumping AND velocity is downward
            if (wasGrounded && !isGrounded && !isJumping && rb.linearVelocity.y < -1f)
            {
                actuallyFalling = true;
                fallingTimer = 0f;
                Debug.Log("[PlayerMovement] Started actually falling off platform");
            }
        }

        if (anim)
        {
            anim.SetBool(GroundedHash, isGrounded);
        }

        // Reset respawn state when grounded
        if (isGrounded && justRespawned)
        {
            Debug.Log("[PlayerMovement] Just landed after respawn - resetting to Idle 2");
            justRespawned = false;
            isFalling = false;
            fallingTimer = 0f;
            actuallyFalling = false;
            anim.speed = 1f;
            anim.Play("Idle 2 - Crypto", 0, 0f);
        }

        // CRITICAL FIX: Trigger respawn when falling below -4f Y position as requested
        if (transform.position.y < -4f && !isDeadOrRespawning)
        {
            Debug.Log("[PlayerMovement] Player fell below -4f Y position - triggering respawn");
            isDeadOrRespawning = true;

            if (respawnCount < maxRespawns)
            {
                StartCoroutine(RespawnSequence());
            }
            else
            {
                TriggerDeathAnimation();
            }
        }
    }

    private void HandleAnimations()
    {
        if (!anim) return;

        // Skip falling animation logic if just respawned
        if (justRespawned)
        {
            Debug.Log("[PlayerMovement] Skipping falling animation - just respawned");
            return;
        }

        // Handle falling animation ONLY when actually falling off platform (not during jumps)
        if (!isGrounded && actuallyFalling && !isJumping && rb.linearVelocity.y < -5f)
        {
            if (!isFalling)
            {
                fallingTimer += Time.deltaTime;
                
                // Trigger falling after 0.8 seconds of actual falling off platform
                if (fallingTimer >= 0.8f)
                {
                    isFalling = true;
                    Debug.Log("[PlayerMovement] Triggering falling animation - actually fell off platform");
                    
                    // Check if Falling animation exists, otherwise use a substitute
                    if (HasAnimationClip("Falling - Crypto"))
                    {
                        anim.Play("Falling - Crypto", 0, 0f);
                    }
                    else if (HasAnimationClip("Jump - Crypto"))
                    {
                        anim.Play("Jump - Crypto", 0, 0f);
                        anim.speed = 0.3f; // Slow down jump animation for falling
                    }
                    Debug.Log("[PlayerMovement] Started falling animation");
                }
            }
        }
        
        // Reset falling state when grounded or jumping
        if (isGrounded || isJumping)
        {
            if (isFalling || actuallyFalling)
            {
                Debug.Log("[PlayerMovement] Resetting falling state");
            }
            isFalling = false;
            fallingTimer = 0f;
            actuallyFalling = false;
            anim.speed = 1f; // Reset animation speed
        }
        
        // Handle ground-based animations ONLY when grounded and not jumping
        if (isGrounded && !isJumping && !isFalling && !justRespawned)
        {
            // IMMEDIATE W key response
            if (Input.GetKeyDown(KeyCode.W))
            {
                Debug.Log("[PlayerMovement] W Pressed - Starting Run Animation");
                isInRunToStopTransition = false;
                anim.Play("Running - Crypto", 0, 0f);
                anim.SetBool(RunHash, true);
            }
            // IMMEDIATE W key release response
            else if (Input.GetKeyUp(KeyCode.W))
            {
                Debug.Log("[PlayerMovement] W Released - Starting Stop Sequence");
                isInRunToStopTransition = true;
                
                // Check if Running to Stop animation exists
                if (HasAnimationClip("Running to Stop - Crypto"))
                {
                    anim.Play("Running to Stop - Crypto", 0, 0f);
                    StartCoroutine(WaitForStopAnimationToFinish());
                }
                else
                {
                    // Direct transition to Idle 2 if no stop animation
                    anim.Play("Idle 2 - Crypto", 0, 0f);
                    isInRunToStopTransition = false;
                }
                anim.SetBool(RunHash, false);
            }
            // Maintain running state
            else if (isRunning && !isInRunToStopTransition)
            {
                if (!anim.GetCurrentAnimatorStateInfo(0).IsName("Running - Crypto"))
                {
                    Debug.Log("[PlayerMovement] Ensuring running animation is playing");
                    anim.Play("Running - Crypto", 0, 0f);
                }
                anim.SetBool(RunHash, true);
            }
            // Default to Idle 2 when no input and not in transition
            else if (!isRunning && !isInRunToStopTransition)
            {
                var currentState = anim.GetCurrentAnimatorStateInfo(0);
                if (!currentState.IsName("Idle 2 - Crypto") && 
                    !currentState.IsName("Running to Stop - Crypto") &&
                    !currentState.IsName("Left - Crypto") &&
                    !currentState.IsName("Right - Crypto"))
                {
                    Debug.Log("[PlayerMovement] Returning to Idle 2");
                    anim.Play("Idle 2 - Crypto", 0, 0f);
                }
                anim.SetBool(RunHash, false);
            }
        }
    }

    private void HandleMovement()
    {
        // Only apply forward force when running animation is playing AND grounded
        bool canApplyForwardForce = false;
        
        if (anim != null && isGrounded && !justRespawned)
        {
            bool isRunningAnimationPlaying = anim.GetCurrentAnimatorStateInfo(0).IsName("Running - Crypto");
            bool isJumpingWithForwardMomentum = isJumping && isRunning;
            
            canApplyForwardForce = isRunningAnimationPlaying || isJumpingWithForwardMomentum;
            
            if (Time.frameCount % 120 == 0)
            {
                Debug.Log($"[PlayerMovement] Movement Check - CanApplyForce: {canApplyForwardForce}, RunningAnim: {isRunningAnimationPlaying}, IsRunning: {isRunning}");
            }
        }
        
        // Forward movement - ONLY when conditions are met
        if (isRunning && canApplyForwardForce)
        {
            currentSpeed = Mathf.Lerp(currentSpeed, maxSpeed, acceleration * Time.fixedDeltaTime);
            // Apply constant forward force for movement
            rb.AddForce(Vector3.forward * forwardForceMultiplier, ForceMode.Force);
            
            if (Time.frameCount % 120 == 0)
            {
                Debug.Log($"[PlayerMovement] Applying forward force: {forwardForceMultiplier}, Current Speed: {currentSpeed}");
            }
        }
        else if (!isRunning && !isInRunToStopTransition && !justRespawned)
        {
            // Smooth deceleration when not running
            Vector3 velocity = rb.linearVelocity;
            velocity.z = Mathf.Lerp(velocity.z, 0f, 8f * Time.fixedDeltaTime);
            rb.linearVelocity = velocity;
            currentSpeed = Mathf.Lerp(currentSpeed, baseSpeed, 4f * Time.fixedDeltaTime);
        }
        else if (isInRunToStopTransition)
        {
            // Gradual deceleration during stop animation
            Vector3 currentVelocity = rb.linearVelocity;
            if (currentVelocity.z > 0.5f)
            {
                currentVelocity.z = Mathf.Lerp(currentVelocity.z, 0f, 6f * Time.fixedDeltaTime);
                rb.linearVelocity = currentVelocity;
            }
            else
            {
                Vector3 velocity = rb.linearVelocity;
                velocity.z = 0f;
                rb.linearVelocity = velocity;
            }
        }

        // Smooth lateral movement
        if (horizontalInput != 0 && isGrounded && !justRespawned)
        {
            rb.AddForce(Vector3.right * horizontalInput * sideSpeed, ForceMode.Force);
        }
    }

    private void TryJump()
    {
        if (!isGrounded || isJumping) 
        {
            Debug.Log($"[PlayerMovement] Jump blocked - Grounded: {isGrounded}, Jumping: {isJumping}");
            return;
        }

        bool wasRunning = isRunning;
        
        // IMMEDIATE jump animation
        isJumping = true;
        Debug.Log("[PlayerMovement] Starting jump sequence");
        
        // Check if Jump animation exists
        if (HasAnimationClip("Jump - Crypto"))
        {
            anim.Play("Jump - Crypto", 0, 0f);
            anim.speed = 0.6f; // Slower jump animation
            Debug.Log("[PlayerMovement] Playing jump animation");
        }
        else
        {
            Debug.LogWarning("[PlayerMovement] Jump animation not found, using Idle animation");
            anim.Play("Idle 2 - Crypto", 0, 0f);
        }
        
        anim.SetBool(JumpHash, true);
        
        // Apply jump physics
        rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
        
        // Add forward momentum only if running
        if (wasRunning)
        {
            rb.AddForce(Vector3.forward * jumpForwardBoost, ForceMode.VelocityChange);
            Debug.Log("[PlayerMovement] Added forward momentum to jump");
        }
        
        StartCoroutine(WaitForLanding());
    }

    private void CorrectPlayerPosition()
    {
        // Keep player above ground at all times
        if (isGrounded && !justRespawned)
        {
            RaycastHit hit;
            if (Physics.Raycast(feet.position + Vector3.up * 2f, Vector3.down, out hit, 5f, groundMask))
            {
                float desiredY = hit.point.y;
                float currentY = transform.position.y;
                
                // Lift player if clipping into ground
                if (currentY < desiredY + 0.2f)
                {
                    Vector3 correctedPosition = transform.position;
                    correctedPosition.y = desiredY + 0.2f;
                    transform.position = correctedPosition;
                    
                    // Prevent falling back down
                    Vector3 velocity = rb.linearVelocity;
                    if (velocity.y < 0) velocity.y = 0;
                    rb.linearVelocity = velocity;
                }
            }
        }
    }

    // --------------------------- Helper Methods ---------------------------

    private bool HasAnimationClip(string clipName)
    {
        if (anim == null) return false;
        
        foreach (AnimationClip clip in anim.runtimeAnimatorController.animationClips)
        {
            if (clip.name == clipName)
                return true;
        }
        return false;
    }

    // --------------------------- Coroutines ---------------------------

    IEnumerator WaitForLanding()
    {
        yield return new WaitUntil(() => isGrounded);
        
        Debug.Log("[PlayerMovement] Landed from jump");
        anim.speed = 1f;
        anim.SetBool(JumpHash, false);
        
        // Return to appropriate animation based on input state
        if (isRunning)
        {
            Debug.Log("[PlayerMovement] Landing -> Running");
            anim.Play("Running - Crypto", 0, 0f);
        }
        else
        {
            Debug.Log("[PlayerMovement] Landing -> Idle 2");
            anim.Play("Idle 2 - Crypto", 0, 0f);
        }
        
        yield return new WaitForSeconds(jumpCooldown);
        isJumping = false;
    }

    IEnumerator WaitForStopAnimationToFinish()
    {
        yield return new WaitForSeconds(0.4f);
        isInRunToStopTransition = false;
        
        if (!isRunning)
        {
            Debug.Log("[PlayerMovement] Stop animation finished -> Idle 2");
            anim.Play("Idle 2 - Crypto", 0, 0f);
        }
    }

    // --------------------------- Public Methods ---------------------------

    public float ReturnZAxis() => transform.position.z;

    // --------------------------- Environment Updates ---------------------------

    private void UpdateEnvironmentTracking()
    {
        int currentZ = Mathf.FloorToInt(transform.position.z);
        if (currentZ >= nextEnvironmentUpdate)
        {
            nextEnvironmentUpdate += environmentUpdateInterval;
        }
    }

    // --------------------------- Respawn and Death Logic ---------------------------

    private void OnCollisionEnter(Collision collision)
    {
        // Track safe ground
        if (collision.gameObject.CompareTag("Procedural Ground"))
        {
            lastGroundBeforeDeath = collision.gameObject;
        }
        
        // Only trigger respawn for Environment Objects (hazards)
        if (collision.gameObject.CompareTag("Environment Objects"))
        {
            Debug.Log($"Collision with hazard: {collision.gameObject.name}");
            
            if (!isDeadOrRespawning)
            {
                isDeadOrRespawning = true;
                
                if (respawnCount < maxRespawns)
                {
                    StartCoroutine(RespawnSequence());
                }
                else
                {
                    TriggerDeathAnimation();
                }
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Only trigger for specific death zones if they exist
        if (other.CompareTag("DeathZone"))
        {
            Debug.Log($"Entered death zone: {other.gameObject.name}");
            
            if (!isDeadOrRespawning)
            {
                isDeadOrRespawning = true;
                
                if (respawnCount < maxRespawns)
                {
                    StartCoroutine(RespawnSequence());
                }
                else
                {
                    TriggerDeathAnimation();
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Procedural Ground"))
        {
            lastGroundBeforeDeath = other.gameObject;
        }
    }

    public void CheckDeathCondition()
    {
        if (isDeadOrRespawning) return;

        // Additional safety check - this method is kept for compatibility but main check is in CheckGroundStatus
        // Only trigger if somehow the main check in CheckGroundStatus failed
        if (transform.position.y < -10f)
        {
            Debug.Log("[PlayerMovement] Secondary safety check triggered - player fell very far");
            isDeadOrRespawning = true;

            if (respawnCount < maxRespawns)
            {
                StartCoroutine(RespawnSequence());
            }
            else
            {
                TriggerDeathAnimation();
            }
        }
    }

    private IEnumerator RespawnSequence()
    {
        Debug.Log("[PlayerMovement] Starting respawn sequence");
        
        if (isDeadOrRespawning)
        {
            if (respawnScreen) respawnScreen.SetActive(true);
            yield return new WaitForSeconds(0.5f);

            if (lastGroundBeforeDeath != null)
            {
                Vector3 p = lastGroundBeforeDeath.transform.position;
                transform.position = new Vector3(p.x, p.y + 3f, p.z);
                Debug.Log($"[PlayerMovement] Respawned at ground position: {transform.position}");
            }
            else
            {
                // Fallback spawn position
                transform.position = new Vector3(0, 5f, 0);
                Debug.Log("[PlayerMovement] Respawned at fallback position");
            }

            // Reset all physics and states
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            
            // Reset all animation states
            isFalling = false;
            fallingTimer = 0f;
            isJumping = false;
            isInRunToStopTransition = false;
            justRespawned = true; // Mark as just respawned
            
            // Force to Idle 2 animation
            anim.speed = 1f;
            anim.Play("Idle 2 - Crypto", 0, 0f);
            anim.SetBool(RunHash, false);
            anim.SetBool(JumpHash, false);

            isDeadOrRespawning = false;
            respawnCount++;

            if (respawnScreen) respawnScreen.SetActive(false);

            Debug.Log($"[PlayerMovement] Respawn complete - Position: {transform.position}, State: Idle 2");
        }
    }

    private void TriggerDeathAnimation()
    {
        if (hasTriggeredDeathAnimation) return;
        hasTriggeredDeathAnimation = true;

        if (respawnScreen) respawnScreen.SetActive(false);

        var gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager) gameManager.GameEnds();
    }

    private void TeleportToLastGround()
    {
        Debug.Log("[TELEPORT] R button pressed - initiating teleport");
        
        if (lastGroundBeforeDeath != null)
        {
            Vector3 teleportPosition = new Vector3(
                lastGroundBeforeDeath.transform.position.x, 
                lastGroundBeforeDeath.transform.position.y + 2f, 
                lastGroundBeforeDeath.transform.position.z
            );
            
            Debug.Log($"[TELEPORT] Teleporting to last ground: {lastGroundBeforeDeath.name} at position {teleportPosition}");
            
            // Reset all states
            isRunning = false;
            isJumping = false;
            isFalling = false;
            actuallyFalling = false;
            isInRunToStopTransition = false;
            justRespawned = false;
            fallingTimer = 0f;
            
            // Reset physics
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            
            // Teleport player
            transform.position = teleportPosition;
            
            // Force Idle 2 animation
            anim.speed = 1f;
            anim.Play("Idle 2 - Crypto", 0, 0f);
            anim.SetBool(RunHash, false);
            anim.SetBool(JumpHash, false);
            anim.SetBool(GroundedHash, true);
            
            Debug.Log($"[TELEPORT] Teleport complete - New position: {transform.position}, State: Idle 2");
        }
        else
        {
            // Fallback teleport to origin
            Vector3 fallbackPosition = new Vector3(0, 5f, 0);
            Debug.LogWarning($"[TELEPORT] No last ground found, teleporting to fallback position: {fallbackPosition}");
            
            // Reset all states
            isRunning = false;
            isJumping = false;
            isFalling = false;
            actuallyFalling = false;
            isInRunToStopTransition = false;
            justRespawned = false;
            fallingTimer = 0f;
            
            // Reset physics
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            
            // Teleport player
            transform.position = fallbackPosition;
            
            // Force Idle 2 animation
            anim.speed = 1f;
            anim.Play("Idle 2 - Crypto", 0, 0f);
            anim.SetBool(RunHash, false);
            anim.SetBool(JumpHash, false);
            anim.SetBool(GroundedHash, true);
            
            Debug.Log($"[TELEPORT] Fallback teleport complete - Position: {transform.position}");
        }
    }
}
