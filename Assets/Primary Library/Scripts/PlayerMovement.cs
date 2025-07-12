using System.Collections;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Physics Related Fields")]
    [SerializeField] private float baseSpeed = 8f;           // Reduced from 50f for realistic movement
    [SerializeField] private float maxSpeed = 15f;           // Reduced from 300f for realistic max speed
    [SerializeField] private float acceleration = 1.5f;      // Slightly reduced for smoother acceleration
    [SerializeField] private float forwardForceMultiplier = 1f; 
    [SerializeField] private float sideSpeed = 12f;          // Reduced from 80f for realistic side movement
    [SerializeField] private float jumpForce = 5f;           // Reduced from 8f for realistic jump height
    [SerializeField] private float jumpForwardBoost = 8f;    // Reduced from 11f for realistic forward momentum
    [SerializeField] private float jumpCooldown = 0.2f;      // Reduced from 0.5f for more responsive jumping


    [Header("Object References")] [SerializeField]
    private Rigidbody rb;

    Animator playerAnimator;

    [Header("Ground-check")] [SerializeField]
    Transform feet; // empty at sole level

    [SerializeField] float groundRadius = 0.25f;
    [SerializeField] LayerMask groundMask = ~0;
    [SerializeField] float groundedRay = 0.10f; // ★ new: ray length

    bool isGrounded; // updated every frame

    [SerializeField] private Animator anim;


    [Header("Environment Tracking")] [SerializeField]
    private int environmentUpdateInterval = 150;

    private int nextEnvironmentUpdate;


    [Header("Respawn Settings")] [SerializeField]
    private int maxRespawns = 3; // Maximum number of respawns allowed

    private int respawnCount;
    private GameObject lastGroundBeforeDeath;
    [SerializeField] private GameObject respawnScreen;
    private bool isDeadOrRespawning = false;
    private bool hasTriggeredDeathAnimation = false;


    [Header("Flags")] [SerializeField] private bool isJumping;
    private float currentSpeed;
    private bool isRunning;
    private float horizontalInput;

    // --------------------------- Unity Methods ---------------------------

    private void Awake()
    {
        InitializeComponents();
        currentSpeed = baseSpeed;
        nextEnvironmentUpdate = environmentUpdateInterval;
    }

    private void InitializeComponents()
    {
        if (!rb) rb = GetComponent<Rigidbody>();
        playerAnimator = GetComponent<Animator>();
        if (!anim) TryGetComponent(out anim);

        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private void Update()
    {
        HandleInput();
        
        /* ---------- GROUND CHECK ---------- */
        /* only count as grounded while moving DOWN or resting */
        bool goingDown = rb.linearVelocity.y <= 0.0f;
        isGrounded = goingDown &&
                     Physics.SphereCast(feet.position + Vector3.up * 0.05f,
                         groundRadius,
                         Vector3.down,
                         out _,
                         groundedRay,
                         groundMask,
                         QueryTriggerInteraction.Ignore);

        /* ---------- ANIMATOR ---------- */
        // COMPLETELY SIMPLIFIED ANIMATION SYSTEM - DIRECT CONTROL
        if (anim)
        {
            anim.SetBool("Grounded", isGrounded);
            
            // ONLY handle animations when grounded and not jumping
            if (isGrounded && !isJumping)
            {
                // W KEY PRESSED - IMMEDIATE RUNNING
                if (Input.GetKeyDown(KeyCode.W))
                {
                    anim.Play("Running - Crypto", 0, 0f);
                    anim.SetBool("Run", true);
                }
                // W KEY RELEASED - IMMEDIATE STOP SEQUENCE
                else if (Input.GetKeyUp(KeyCode.W))
                {
                    anim.Play("Running to Stop - Crypto", 0, 0f);
                    anim.SetBool("Run", false);
                    StartCoroutine(WaitForStopAnimationToFinish());
                }
                // W KEY HELD - MAINTAIN RUNNING
                else if (isRunning)
                {
                    if (!anim.GetCurrentAnimatorStateInfo(0).IsName("Running - Crypto"))
                    {
                        anim.Play("Running - Crypto", 0, 0f);
                    }
                    anim.SetBool("Run", true);
                }
                // NO INPUT - GO TO IDLE 2
                else
                {
                    if (!anim.GetCurrentAnimatorStateInfo(0).IsName("Idle 2 - Crypto") && 
                        !anim.GetCurrentAnimatorStateInfo(0).IsName("Running to Stop - Crypto"))
                    {
                        anim.Play("Idle 2 - Crypto", 0, 0f);
                    }
                    anim.SetBool("Run", false);
                }
            }
        }

        // GROUND POSITION CORRECTION - Keep player above ground at all times
        if (isGrounded)
        {
            // Ensure the player's feet are always above the ground surface
            RaycastHit hit;
            if (Physics.Raycast(feet.position + Vector3.up * 2f, Vector3.down, out hit, 5f, groundMask))
            {
                float desiredY = hit.point.y;
                float currentY = transform.position.y;
                
                // If player is clipping into ground, lift them up
                if (currentY < desiredY + 0.1f) // 0.1f buffer to stay above ground
                {
                    Vector3 correctedPosition = transform.position;
                    correctedPosition.y = desiredY + 0.1f;
                    transform.position = correctedPosition;
                    
                    // Also adjust velocity to prevent falling back down
                    Vector3 velocity = rb.linearVelocity;
                    if (velocity.y < 0) velocity.y = 0;
                    rb.linearVelocity = velocity;
                }
            }
        }

        UpdateEnvironmentTracking();
        CheckDeathCondition();
    }

    private void FixedUpdate()
    {
        HandleMovement();
    }


    // --------------------------- Movement Related Methods ---------------------------


    private void HandleInput()
    {
        // Forward movement input
        isRunning = Input.GetKey(KeyCode.W);
        horizontalInput = Input.GetAxisRaw("Horizontal");

        // Jump input
        if (Input.GetKeyDown(KeyCode.Space)) TryJump();
    }

    private void HandleMovement()
    {
        // Only apply forward force if running animation is actually playing OR during jump
        bool canApplyForwardForce = false;
        
        if (anim != null)
        {
            // Check if running animation is playing or if jumping (which should maintain forward momentum)
            bool isRunningAnimationPlaying = anim.GetCurrentAnimatorStateInfo(0).IsName("Running - Crypto");
            bool isJumpingWithForwardMomentum = isJumping;
            bool isRunningToStop = anim.GetCurrentAnimatorStateInfo(0).IsName("Running to Stop - Crypto");
            
            canApplyForwardForce = isRunningAnimationPlaying || isJumpingWithForwardMomentum;
        }
        
        // Forward movement - only apply force when running animation is playing or jumping
        if (isRunning && canApplyForwardForce)
        {
            currentSpeed = Mathf.Min(currentSpeed + acceleration, maxSpeed);
            // Apply forward force using the multiplier
            rb.AddForce(0, 0, currentSpeed * forwardForceMultiplier * Time.deltaTime, ForceMode.VelocityChange);
        }
        else if (!isRunning)
        {
            // Don't immediately stop - let ground friction handle deceleration naturally
            // Only apply drag/friction to slow down the player gradually
            Vector3 currentVelocity = rb.linearVelocity;
            if (currentVelocity.z > 0.1f) // Only apply friction if moving forward
            {
                // Apply gradual deceleration through friction (adjust multiplier for feel)
                float frictionForce = currentVelocity.z * 8f; // Friction coefficient
                rb.AddForce(0, 0, -frictionForce * Time.deltaTime, ForceMode.VelocityChange);
            }
            else
            {
                // Stop completely when velocity is very low to prevent micro-movements
                rb.linearVelocity = new Vector3(currentVelocity.x, currentVelocity.y, 0f);
            }
            currentSpeed = baseSpeed;
        }

        // Lateral movement
        if (horizontalInput != 0)
        {
            rb.AddForce(horizontalInput * sideSpeed * Time.deltaTime, 0, 0, ForceMode.VelocityChange);
        }
    }

    private void TryJump()
    {
        if (!isGrounded || isJumping) return;

        // Store current state
        bool wasRunning = anim.GetCurrentAnimatorStateInfo(0).IsName("Running - Crypto");
        
        // FORCE JUMP ANIMATION IMMEDIATELY - NO DELAYS
        isJumping = true;
        anim.Play("Jump - Crypto", 0, 0f);
        anim.speed = 0.4f;
        
        // Apply jump physics RIGHT NOW
        rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
        
        if (wasRunning)
        {
            rb.AddForce(Vector3.forward * jumpForwardBoost, ForceMode.Impulse);
        }
        
        StartCoroutine(WaitForLanding());
    }

    IEnumerator WaitForLanding()
    {
        yield return new WaitUntil(() => isGrounded);
        
        anim.speed = 1f;
        
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
        yield return new WaitForSeconds(0.3f);
        if (!isRunning)
        {
            anim.Play("Idle 2 - Crypto", 0, 0f);
        }
    }


    public float ReturnZAxis() => transform.position.z;

    // private void UpdateAnimations()
    // {
    //     if (!anim) return;
    //
    //     anim.SetBool("isRunning", isRunning);
    //
    //     // Falling animation
    //     float yPos = transform.position.y;
    //     anim.SetBool("isFalling", yPos < -1f && yPos >= -170f);
    // }


    // --------------------------- Environment Updates ---------------------------

    private void UpdateEnvironmentTracking()
    {
        int currentZ = Mathf.FloorToInt(transform.position.z);
        if (currentZ >= nextEnvironmentUpdate)
        {
            // Call environment change here
            nextEnvironmentUpdate += environmentUpdateInterval;
        }
    }


    // --------------------------- Respawn and Death Logic ---------------------------

    private void OnCollisionEnter(Collision collision)
    {
        // Track the last ground prefab the player was on
        if (collision.gameObject.CompareTag("Procedural Ground"))
        {
            lastGroundBeforeDeath = collision.gameObject;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Track the last procedural ground when the player leaves it
        if (other.CompareTag("Procedural Ground"))
        {
            lastGroundBeforeDeath = other.gameObject;
        }
    }


    public void CheckDeathCondition()
    {
        if (isDeadOrRespawning) return;

        // It is extremely important to only respawn between a particular position in the Y-Axis
        // because this prevents the respawning logic from being continuously triggered
        // at every frame until reset triggering multiple respawns on a single death.
        // Debugging Time on this Minor Issue: >24 hours (still needs testing)
        if (transform.position.y < -10f && transform.position.y > -10.5f)
        {
            isDeadOrRespawning = true; // lock immediately

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
            // 1.  optional UI
            if (respawnScreen) respawnScreen.SetActive(true);
            yield return new WaitForSeconds(0f);

            // 2.  move to last safe ground
            if (lastGroundBeforeDeath != null)
            {
                Vector3 p = lastGroundBeforeDeath.transform.position;
                transform.position = new Vector3(p.x, p.y + 2f, p.z);
            }

            // 3.  ***reset physics***  (the real bug-fix)
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            // 4.  hide UI & unlock
            isDeadOrRespawning = false;
            respawnCount++;


            Debug.Log($"Respawned player at: {transform.position}");
        }
    }

    private void TriggerDeathAnimation()
    {
        if (hasTriggeredDeathAnimation) return;
        hasTriggeredDeathAnimation = true;

        // Hide respawn screen (if up)
        if (respawnScreen) respawnScreen.SetActive(false);

        FindObjectOfType<GameManager>().GameEnds();
        // Lock remains forever, no further respawns
    }

    private IEnumerator SmoothRunningToStopSequence()
    {
        // Play running to stop animation with proper duration
        anim.Play("Running to Stop - Crypto", 0, 0f);
        anim.SetBool("Run", false);
        
        // Wait longer to let the running to stop animation play visibly
        yield return new WaitForSeconds(0.4f); // Increased from 0.1f to show the animation properly
        
        // Then transition to idle
        anim.Play("Idle 2 - Crypto", 0, 0f);
    }
}
