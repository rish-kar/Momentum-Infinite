using System.Collections;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float baseSpeed = 60f;
    [SerializeField] private float maxSpeed = 100f;
    [SerializeField] private float acceleration = 2f;
    [SerializeField] private float forwardForceMultiplier = 0.5f; // Adjust this multiplier to reduce forward force
    [SerializeField] private float sideSpeed = 100f;
    [SerializeField] private float jumpForce = 15f;
    [SerializeField] private float jumpForwardBoost = 1000f;
    [SerializeField] private float jumpCooldown = 1.19f;

    [Header("References")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Animator anim;

    [Header("Environment Tracking")]
    [SerializeField] private int environmentUpdateInterval = 150;
    private int nextEnvironmentUpdate;
    
    private bool isJumping;
    private float currentSpeed;
    private bool isRunning;
    private float horizontalInput;

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
            rb.AddForce(0, 0, currentSpeed * forwardForceMultiplier);
        }
        else
        {
            currentSpeed = baseSpeed;
        }

        // Lateral movement
        if (horizontalInput != 0)
        {
            rb.AddForce(horizontalInput * sideSpeed, 0, 0);
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

    private void UpdateAnimations()
    {
        if (!anim) return;
        
        anim.SetBool("isRunning", isRunning);
        
        // Falling animation
        float yPos = transform.position.y;
        anim.SetBool("isFalling", yPos < -1f && yPos >= -170f);
    }

    private void UpdateEnvironmentTracking()
    {
        int currentZ = Mathf.FloorToInt(transform.position.z);
        if (currentZ >= nextEnvironmentUpdate)
        {
            // Call environment change here
            nextEnvironmentUpdate += environmentUpdateInterval;
        }
    }

    public void CheckDeathCondition()
    {
        if (transform.position.y < -170f)
        {
            if (anim)
            {
                anim.SetBool("isFalling", false);
                anim.SetTrigger("Crash");
            }
        }
    }

    public float ReturnZAxis() => transform.position.z;
}
