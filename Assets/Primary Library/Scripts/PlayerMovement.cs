using System.Collections;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Physics Related Fields")] private float baseSpeed = 50f;
    [SerializeField] private float maxSpeed = 300f;
    [SerializeField] private float acceleration = 2f;
    [SerializeField] private float forwardForceMultiplier = 0.5f; // Adjust this multiplier to reduce forward force
    [SerializeField] private float sideSpeed = 80f;
    [SerializeField] private float jumpForce = 20f;
    [SerializeField] private float jumpForwardBoost = 200f;
    [SerializeField] private float jumpCooldown = 2f;


    [Header("Object References")] [SerializeField]
    private Rigidbody rb;


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
        if (!anim) TryGetComponent(out anim);

        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private void Update()
    {
        HandleInput();
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
        // Forward movement
        if (isRunning)
        {
            currentSpeed = Mathf.Min(currentSpeed + acceleration, maxSpeed);
            // Apply reduced forward force using the multiplier
            rb.AddForce(0, 0, currentSpeed * forwardForceMultiplier * Time.deltaTime, ForceMode.VelocityChange);
        }
        else
        {
            currentSpeed = baseSpeed;
        }

        // Lateral movement
        if (horizontalInput != 0)
        {
            rb.AddForce(horizontalInput * sideSpeed * Time.deltaTime, 0, 0, ForceMode.VelocityChange);
        }

        // Update animations
        UpdateAnimations();
    }

    private void TryJump()
    {
        if (!isJumping)
        {
            StartCoroutine(PerformJump());
        }
    }

    private IEnumerator PerformJump()
    {
        isJumping = true;

        // Initial jump force
        rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
        rb.AddForce(Vector3.forward * jumpForwardBoost, ForceMode.Impulse);

        // Trigger jump animation
        if (anim) anim.SetTrigger("isJumping");

        yield return new WaitForSeconds(jumpCooldown);
        isJumping = false;
    }

    public float ReturnZAxis() => transform.position.z;

    private void UpdateAnimations()
    {
        if (!anim) return;

        anim.SetBool("isRunning", isRunning);

        // Falling animation
        float yPos = transform.position.y;
        anim.SetBool("isFalling", yPos < -1f && yPos >= -170f);
    }


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
        if (transform.position.y < -10f && transform.position.y > -10.5f )
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
}