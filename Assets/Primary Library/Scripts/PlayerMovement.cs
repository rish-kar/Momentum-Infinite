using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Physics Related Fields")] [SerializeField]
    private float baseSpeed = 6f; // Realistic movement speed

    [SerializeField] private float maxSpeed = 12f; // Realistic max speed
    [SerializeField] private float acceleration = 3f; // Increased for better responsiveness
    [SerializeField] private float forwardForceMultiplier = 15f; // Increased for actual movement
    [SerializeField] private float constantForwardSpeed = 8f; // CONSTANT FORWARD MOVEMENT for endless runner
    [SerializeField] private float sideSpeed = 8f; // Reduced as requested
    [SerializeField] private float jumpForce = 7f; // Realistic jump height
    [SerializeField] private float jumpForwardBoost = 4f; // Realistic forward momentum
    [SerializeField] private float jumpCooldown = 0.1f; // Quick jump response

    [Header("Object References")] [SerializeField]
    private Rigidbody rb;

    [SerializeField] private Animator anim;

    [Header("Ground-check")] [SerializeField]
    Transform feet; // empty at sole level

    [SerializeField] float groundRadius = 0.25f;
    [SerializeField] LayerMask groundMask = ~0;
    [SerializeField] float groundedRay = 0.15f;

    bool isGrounded; // updated every frame

    [Header("Environment Tracking")] [SerializeField]
    private int environmentUpdateInterval = 150;

    private int nextEnvironmentUpdate;

    [Header("Respawn Settings")] [SerializeField]
    private int maxRespawns = 3;

    private int respawnCount;
    private GameObject lastGroundBeforeDeath;
    [SerializeField] private GameObject respawnScreen;
    private bool isDeadOrRespawning;
    private bool hasTriggeredDeathAnimation;

    [Header("Flags")] [SerializeField] private bool isJumping;
    private float currentSpeed;
    private bool isRunning;
    private float horizontalInput;
    private bool isFalling;
    private float fallingTimer;
    private bool isInRunToStopTransition;
    private bool justRespawned; // New flag to handle respawn state
    private bool actuallyFalling; // Track if player is actually falling off platform
    private bool hasPlayedInitialIdle; // Track if initial idle animation has been played
    private bool isPlayingInitialIdle; // Track if currently playing initial idle to prevent multiple starts
    private Vector3 lockedPosition; // Lock position during initial idle animation

    // Animation state hashes for performance
    private static readonly int GroundedHash = Animator.StringToHash("Grounded");
    private static readonly int RunHash = Animator.StringToHash("Run");
    private static readonly int JumpHash = Animator.StringToHash("Jump");
    private static readonly int LeftTriggerHash = Animator.StringToHash("LeftTrigger");
    private static readonly int RightTriggerHash = Animator.StringToHash("RightTrigger");
    private static readonly int FallingTriggerHash = Animator.StringToHash("FallingTrigger");
    private static readonly int InitialIdleHash = Animator.StringToHash("InitialIdle"); // For initial idle trigger

    
    private Dictionary<string, bool> _animationClipCache = new();

    // --------------------------- Unity Methods ---------------------------

    private void Awake()
    {
        InitializeComponents();
        currentSpeed = baseSpeed;
        nextEnvironmentUpdate = environmentUpdateInterval;
        
        foreach (AnimationClip clip in anim.runtimeAnimatorController.animationClips)
        {
            _animationClipCache[clip.name] = true;
        }
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
    }

    private void Update()
    {
        HandleInput();
        CheckGroundStatus();
        HandleAnimations();

        // Call position correction more frequently during initial idle animation
        // if (isPlayingInitialIdle)
        // {
        //     CorrectPlayerPositionForInitialIdle();
        // }

        CorrectPlayerPosition();
        UpdateEnvironmentTracking();
        CheckDeathCondition();
    }

    private void FixedUpdate()
    {
        HandleMovement();
    }

    // --------------------------- Core Methods ---------------------------

    private void HandleInput()
    {
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

        // Jump input - IMMEDIATE response
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded && !isJumping)
        {
            TryJump();
        }

        // Handle Left/Right movements - SIMPLIFIED for better functionality
        if (isGrounded && !isJumping && !isRunning)
        {
            // Left movement
            if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            {
                PerformSideMovement(true); // true for left
            }

            // Right movement
            if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
            {
                PerformSideMovement(false); // false for right
            }
        }
    }

    private void PerformSideMovement(bool isLeft)
    {
        string direction = isLeft ? "Left" : "Right";
        float forceMultiplier = isLeft ? -1f : 1f;

        // DIRECT ANIMATION PLAY - bypassing triggers for immediate response
        string animationName = $"{direction} - Crypto";

        if (HasAnimationClip(animationName))
        {
            // Play animation directly with immediate transition (no crossfade time)
            anim.Play(animationName, 0, 0f);
        }
        else
        {
            // Fallback to triggers if direct play fails
            if (isLeft)
            {
                anim.SetTrigger(LeftTriggerHash);
            }
            else
            {
                anim.SetTrigger(RightTriggerHash);
            }
        }

        // Apply smooth side force with reduced speed
        rb.AddForce(forceMultiplier * sideSpeed, 0, 0, ForceMode.VelocityChange);
    }

    private void CheckGroundStatus()
    {
        bool wasGrounded = isGrounded;

        // More robust ground detection with multiple raycasts
        isGrounded = Physics.SphereCast(
            feet.position, // Start at feet position
            groundRadius,
            Vector3.down,
            out _,
            groundedRay, // Total distance
            groundMask
        );

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

        if (wasGrounded != isGrounded && !isPlayingInitialIdle)
        {
            // Only start tracking actual falling when leaving ground AND not jumping AND velocity is downward
            if (wasGrounded && !isGrounded && rb.linearVelocity.y < -1f)
            {
                actuallyFalling = true;
                fallingTimer = 0f;
            }
        }

        if (anim)
        {
            anim.SetBool(GroundedHash, isGrounded);
        }

        // Reset respawn state when grounded
        if (isGrounded && justRespawned)
        {
            justRespawned = false;
            isFalling = false;
            fallingTimer = 0f;
            actuallyFalling = false;
            anim.speed = 1f;
            anim.Play("Idle 2 - Crypto", 0, 0f);
        }

        // CRITICAL FIX: Trigger falling animation at -5f, respawn at -10f as requested
        // Add safeguards to prevent double triggering
        if (transform.position.y < -5f && !isFalling && actuallyFalling && !isDeadOrRespawning)
        {
            isFalling = true;
            anim.SetTrigger(FallingTriggerHash);
        }

        // Trigger respawn when falling below -10f Y position - ONLY ONCE
        if (transform.position.y < -10f && !isDeadOrRespawning)
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

    private void HandleAnimations()
    {
        if (!anim) return;

        // Skip all animations if just respawned
        if (justRespawned) return;

        // Handle falling animation ONLY when actually falling off platform (not during jumps)
        if (!isGrounded && actuallyFalling && !isJumping && rb.linearVelocity.y < -2f)
        {
            if (!isFalling)
            {
                fallingTimer += Time.deltaTime;

                // Trigger falling after 0.5 seconds of actual falling off platform for slow transition
                if (fallingTimer >= 0.5f)
                {
                    isFalling = true;
                    anim.SetTrigger(FallingTriggerHash);
                }
            }
        }

        // Reset falling state when grounded or jumping
        if (isGrounded || isJumping)
        {
            isFalling = false;
            fallingTimer = 0f;
            actuallyFalling = false;
            anim.speed = 1f; // Reset animation speed
        }

        // Handle ground-based animations ONLY when grounded and not jumping
        if (isGrounded && !isJumping && !isFalling && !justRespawned)
        {
            // Play initial idle animation once at the start, then transition to Idle 2
            if (!hasPlayedInitialIdle && !isPlayingInitialIdle)
            {
                isPlayingInitialIdle = true;

                // Lock the player position at a safe height above ground
                RaycastHit hit;
                if (Physics.Raycast(feet.position + Vector3.up * 2f, Vector3.down, out hit, 5f, groundMask))
                {
                    lockedPosition = new Vector3(transform.position.x, hit.point.y + 1f, transform.position.z);
                }
                else
                {
                    lockedPosition = new Vector3(transform.position.x, transform.position.y + 1f, transform.position.z);
                }

                // Set position and freeze all physics
                transform.position = lockedPosition;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true; // Completely freeze physics during animation

                if (HasAnimationClip("Idle - Crypto"))
                {
                    anim.Play("Idle - Crypto", 0, 0f);
                    StartCoroutine(WaitForInitialIdleToFinish());
                }
                else
                {
                    anim.Play("Idle 2 - Crypto", 0, 0f);
                    hasPlayedInitialIdle = true;
                    isPlayingInitialIdle = false;
                    rb.isKinematic = false; // Re-enable physics
                }

                return; // Exit early to let initial idle play
            }

            // Skip other animations while initial idle is playing
            if (isPlayingInitialIdle)
            {
                // Maintain locked position during animation
                transform.position = lockedPosition;
                return;
            }

            // IMMEDIATE W key response
            if (Input.GetKeyDown(KeyCode.W))
            {
                isInRunToStopTransition = false;
                anim.Play("Running - Crypto", 0, 0f);
                anim.SetBool(RunHash, true);
            }
            // IMMEDIATE W key release response
            else if (Input.GetKeyUp(KeyCode.W))
            {
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
                    anim.Play("Running - Crypto", 0, 0f);
                }

                anim.SetBool(RunHash, true);
            }
            // Default to Idle 2 when no input and not in transition (only after initial idle has played)
            else if (!isRunning && !isInRunToStopTransition && hasPlayedInitialIdle)
            {
                var currentState = anim.GetCurrentAnimatorStateInfo(0);
                if (!currentState.IsName("Idle 2 - Crypto") &&
                    !currentState.IsName("Running to Stop - Crypto") &&
                    !currentState.IsName("Left - Crypto") &&
                    !currentState.IsName("Right - Crypto") &&
                    !currentState.IsName("Idle - Crypto"))
                {
                    anim.Play("Idle 2 - Crypto", 0, 0f);
                }

                anim.SetBool(RunHash, false);
            }
        }
    }

    private void HandleMovement()
    {
        // Player should ONLY move forward when W is pressed (running)
        if (!isDeadOrRespawning && !justRespawned && isGrounded && isRunning)
        {
            // Apply forward movement only when W is pressed
            Vector3 currentVelocity = rb.linearVelocity;
            currentVelocity.z = constantForwardSpeed;
            rb.linearVelocity = currentVelocity;

            // Extra speed boost when running
            rb.AddForce(Vector3.forward * forwardForceMultiplier * 0.2f, ForceMode.Force);
        }
        else if (!isDeadOrRespawning && !justRespawned && !isGrounded && isRunning)
        {
            // When in air and running, maintain forward momentum
            Vector3 currentVelocity = rb.linearVelocity;
            if (currentVelocity.z < constantForwardSpeed * 0.8f)
            {
                rb.AddForce(Vector3.forward * forwardForceMultiplier * 0.5f, ForceMode.Force);
            }
        }
        else if (!isRunning && isGrounded)
        {
            // When not running, stop forward movement
            Vector3 currentVelocity = rb.linearVelocity;
            currentVelocity.z = Mathf.Lerp(currentVelocity.z, 0f, 8f * Time.fixedDeltaTime);
            rb.linearVelocity = currentVelocity;
        }

        // Smooth lateral movement
        if (horizontalInput != 0 && isGrounded && !justRespawned)
        {
            rb.AddForce(Vector3.right * horizontalInput * sideSpeed, ForceMode.Force);
        }
    }

    private void TryJump()
    {
        if (!isGrounded || isJumping) return;

        bool wasRunning = isRunning;

        // IMMEDIATE jump animation
        isJumping = true;

        // Check if Jump animation exists
        if (HasAnimationClip("Jump - Crypto"))
        {
            anim.Play("Jump - Crypto", 0, 0f);
            anim.speed = Mathf.Clamp(jumpForce / 7f, 0.5f, 1.2f);        }
        else
        {
            anim.Play("Idle 2 - Crypto", 0, 0f);
        }

        anim.SetBool(JumpHash, true);

        // Apply jump physics
        rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);

        // Add forward momentum only if running
        if (wasRunning)
        {
            rb.AddForce(Vector3.forward * jumpForwardBoost, ForceMode.VelocityChange);
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

    private void CorrectPlayerPositionForInitialIdle()
    {
        // Special correction to prevent ground clipping during initial idle animation
        // This animation involves lying down and standing up, so we need constant correction
        if (isGrounded && !justRespawned && isPlayingInitialIdle)
        {
            RaycastHit hit;
            if (Physics.Raycast(feet.position + Vector3.up * 2f, Vector3.down, out hit, 5f, groundMask))
            {
                float desiredY = hit.point.y;
                float currentY = transform.position.y;

                // During initial idle, maintain minimum height above ground at all times
                // This prevents the lying down portion from clipping through ground
                float minHeightAboveGround = 0.8f; // Higher clearance for lying down animation

                if (currentY < desiredY + minHeightAboveGround)
                {
                    Vector3 correctedPosition = transform.position;
                    correctedPosition.y = desiredY + minHeightAboveGround;
                    transform.position = correctedPosition;

                    // Reset vertical velocity to prevent downward movement
                    Vector3 velocity = rb.linearVelocity;
                    velocity.y = Mathf.Max(velocity.y, 0); // Prevent negative Y velocity
                    rb.linearVelocity = velocity;
                }
            }
        }
    }

    // --------------------------- Helper Methods ---------------------------

    private bool HasAnimationClip(string clipName) => 
        _animationClipCache.ContainsKey(clipName);


    // --------------------------- Coroutines ---------------------------

    IEnumerator WaitForLanding()
    {
        yield return new WaitUntil(() => isGrounded);

        anim.speed = 1f;
        anim.SetBool(JumpHash, false);

        // Return to appropriate animation based on input state
        if (isRunning)
        {
            anim.Play("Running - Crypto", 0, 0f);
        }
        else
        {
            anim.Play("Idle 2 - Crypto", 0, 0f);
        }

        yield return new WaitForSeconds(jumpCooldown);
        isJumping = false;
    }

    IEnumerator WaitForStopAnimationToFinish()
    {
        AnimationClip clip = anim.GetCurrentAnimatorClipInfo(0)[0].clip;
        yield return new WaitForSeconds(clip.length);
        isInRunToStopTransition = false;

        if (!isRunning)
        {
            anim.Play("Idle 2 - Crypto", 0, 0f);
        }
    }

    IEnumerator WaitForInitialIdleToFinish()
    {
        // var state = anim.GetCurrentAnimatorStateInfo(0);
        yield return new WaitUntil(() => 
            anim.GetCurrentAnimatorStateInfo(0).normalizedTime >= 0.99f
        );
        hasPlayedInitialIdle = true;
        isPlayingInitialIdle = false;
        rb.isKinematic = false;
        anim.Play("Idle 2 - Crypto", 0, 0);
    }

    void OnAnimatorMove()
    {
        if (!anim) return;

        if (isPlayingInitialIdle)
        {
            // Let the animation root motion handle positioning
            transform.position += anim.deltaPosition;
        }
        else
        {
            rb.MovePosition(rb.position + anim.deltaPosition);
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
        if (isDeadOrRespawning)
        {
            // Increment respawn count FIRST to properly track lives
            respawnCount++;

            if (respawnScreen) respawnScreen.SetActive(true);
            yield return new WaitForSeconds(0.5f);

            if (lastGroundBeforeDeath != null)
            {
                Vector3 p = lastGroundBeforeDeath.transform.position;
                transform.position = new Vector3(p.x, p.y + 3f, p.z);
            }
            else
            {
                // Fallback spawn position
                transform.position = new Vector3(0, 5f, 0);
            }

            // Reset all physics and states
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            // Reset all animation states
            isFalling = false;
            fallingTimer = 0f;
            isJumping = false;
            isInRunToStopTransition = false;
            actuallyFalling = false;
            justRespawned = true; // Mark as just respawned

            // Force to Idle 2 animation
            anim.speed = 1f;
            anim.Play("Idle 2 - Crypto", 0, 0f);
            anim.SetBool(RunHash, false);
            anim.SetBool(JumpHash, false);
            anim.SetBool(GroundedHash, true);

            isDeadOrRespawning = false;

            if (respawnScreen) respawnScreen.SetActive(false);
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
        if (lastGroundBeforeDeath != null)
        {
            Vector3 teleportPosition = new Vector3(
                lastGroundBeforeDeath.transform.position.x,
                lastGroundBeforeDeath.transform.position.y + 2f,
                lastGroundBeforeDeath.transform.position.z
            );

            // Reset all states
            isRunning = false;
            isJumping = false;
            isFalling = false;
            actuallyFalling = false;
            isInRunToStopTransition = false;
            justRespawned = false;
            fallingTimer = 0f;
            rb.isKinematic = false; // Ensure physics reactivates
            isPlayingInitialIdle = false;
            hasPlayedInitialIdle = true;

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
        }
        else
        {
            // Fallback teleport to origin
            Vector3 fallbackPosition = new Vector3(0, 5f, 0);

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
        }
    }
}