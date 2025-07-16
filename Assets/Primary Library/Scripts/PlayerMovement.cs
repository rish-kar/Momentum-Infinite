using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")] [SerializeField]
    private float runSpeed = 8f;

    [SerializeField] private float sideSpeed = 12f;
    [SerializeField] private float jumpForce = 7f;
    [SerializeField] private float jumpForwardBoost = 4f;

    [Header("Visual Settings")] [SerializeField]
    private float leanAngle = 15f;

    [SerializeField] private float leanSpeed = 8f;

    [Header("Object References")] [SerializeField]
    private Transform playerModel;

    [SerializeField] private Rigidbody rb;
    [SerializeField] private Animator anim;
    [SerializeField] private Transform feet;
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private float groundCheckDistance = 0.3f;

    [Header("Debug")] [SerializeField] private bool debugMovement = false;
    private Vector3 lastSafePosition;

    [Header("Respawn Settings")] [SerializeField]
    private Animator respawnAnimator; // Assign the Canvas animator in inspector

    [SerializeField] private float respawnAnimationLength = 3f; // Length of your respawn animation
    private bool isRespawning = false;
    private float respawnTimer = 0f;
    
    public float CurrentSpeed => isRunning ? runSpeed : 0;


    // Animation states
    private enum AnimationState
    {
        Idle,
        Running,
        Left,
        Right,
        Jumping,
        Falling
    }

    private AnimationState currentAnimState;

    // Movement state
    private bool isGrounded = true;
    private bool isRunning;
    private bool isJumping;
    private float horizontalInput;

    void Start()
    {
        InitializeComponents();
        currentAnimState = AnimationState.Idle; // Start in idle state
    }

    private void InitializeComponents()
    {
        if (!rb) rb = GetComponent<Rigidbody>();
        if (!anim) anim = GetComponent<Animator>();

        anim.applyRootMotion = false;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.linearDamping = 2f;

        // Initialize safe position FIRST
        lastSafePosition = transform.position;
        lastSafePosition.x = 1.075f; // Set initial X to center

        // Set position using lastSafePosition
        transform.position = new Vector3(
            lastSafePosition.x,
            Mathf.Max(1f, lastSafePosition.y),
            lastSafePosition.z
        );

        isGrounded = true;
        isJumping = false;
    }

    void Update()
    {
        HandleInput();
        SimpleGroundCheck();
        HandleAnimations();
    }

    void FixedUpdate()
    {
        HandleMovement();
        HandleVisualLean();
    }

    private void HandleInput()
    {
        isRunning = Input.GetKey(KeyCode.W);
        horizontalInput = Input.GetAxisRaw("Horizontal");

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded && !isJumping)
        {
            Jump();
        }
    }

    private void SimpleGroundCheck()
    {
        Vector3 rayStart = feet ? feet.position : transform.position;
        bool wasGrounded = isGrounded;
        isGrounded = Physics.Raycast(rayStart, Vector3.down, groundCheckDistance, groundMask);

        if (!wasGrounded && isGrounded && isJumping)
        {
            isJumping = false;
            if (debugMovement) Debug.Log("LANDED - Jump cleared", this);
        }

        if (isGrounded && transform.position.y >= 0f)
        {
            lastSafePosition = transform.position;
            lastSafePosition.x = 1.075f; // Center X position
        }
    }

    private void HandleMovement()
    {
        Vector3 velocity = rb.linearVelocity;

        // Forward movement
        if (isRunning && isGrounded)
        {
            velocity.z = runSpeed;
        }
        else if (isGrounded && !isJumping)
        {
            velocity.z = Mathf.Lerp(velocity.z, 0f, 10f * Time.fixedDeltaTime);
        }

        // Side movement
        if (isGrounded || isJumping)
        {
            velocity.x = horizontalInput * sideSpeed;
        }

        rb.linearVelocity = velocity;
    }

    private void HandleAnimations()
    {
        if (!anim) return;

        // Handle respawning sequence
        if (isRespawning)
        {
            // Player is already teleported and ready - just waiting for animation to finish
            return;
        }

        // Handle death/respawn first
        if (transform.position.y < -5f)
        {
            if (currentAnimState != AnimationState.Falling)
            {
                currentAnimState = AnimationState.Falling;
                anim.Play("Falling - Crypto");
            }

            if (transform.position.y < -8f && !isRespawning)
            {
                // Immediately teleport player to safe position
                transform.position = lastSafePosition;
                rb.linearVelocity = Vector3.zero; // Reset velocity
                isGrounded = true;
                isJumping = false;

                // Set to idle state and animation
                currentAnimState = AnimationState.Idle;
                anim.Play("Idle 2 - Crypto", 0, 0f);

                // Start respawning sequence
                isRespawning = true;

                // Trigger respawning animation
                if (respawnAnimator != null)
                {
                    respawnAnimator.Play("Respawning", 0, 0f);
                }

                // Start timer to reset respawn state
                StartCoroutine(EndRespawn(respawnAnimationLength));
                return;
            }

            return;
        }


        // Handle normal animation states
        AnimationState targetState = currentAnimState;

        if (isJumping)
        {
            targetState = AnimationState.Jumping;
        }
        else if (isGrounded)
        {
            if (isRunning)
            {
                targetState = AnimationState.Running;
            }
            else if (horizontalInput < -0.1f)
            {
                targetState = AnimationState.Left;
            }
            else if (horizontalInput > 0.1f)
            {
                targetState = AnimationState.Right;
            }
            else
            {
                targetState = AnimationState.Idle;
            }
        }
        // Maintain current state if in air but not jumping (shouldn't happen normally)

        // Only change animation if state changed
        if (targetState != currentAnimState)
        {
            currentAnimState = targetState;
            PlayAnimationForState(currentAnimState);
        }
    }

    private IEnumerator EndRespawn(float delay)
    {
        yield return new WaitForSeconds(delay);
        isRespawning = false;
    }

    private void PlayAnimationForState(AnimationState state)
    {
        switch (state)
        {
            case AnimationState.Idle:
                anim.Play("Idle 2 - Crypto");
                break;
            case AnimationState.Running:
                anim.Play("Running - Crypto");
                break;
            case AnimationState.Left:
                anim.Play("Left - Crypto");
                break;
            case AnimationState.Right:
                anim.Play("Right - Crypto");
                break;
            case AnimationState.Jumping:
                anim.Play("Jump - Crypto");
                break;
            case AnimationState.Falling:
                anim.Play("Falling - Crypto");
                break;
        }
    }

    private void HandleVisualLean()
    {
        if (playerModel == null) return;

        float targetLean = 0f;
        if (isRunning && isGrounded && Mathf.Abs(horizontalInput) > 0.1f)
        {
            targetLean = -horizontalInput * leanAngle;
        }

        Quaternion targetRotation = Quaternion.Euler(0, 0, targetLean);
        playerModel.localRotation =
            Quaternion.Slerp(playerModel.localRotation, targetRotation, leanSpeed * Time.fixedDeltaTime);
    }

    private void Jump()
    {
        isJumping = true;
        currentAnimState = AnimationState.Jumping;

        Vector3 jumpVelocity = rb.linearVelocity;
        jumpVelocity.y = jumpForce;

        if (isRunning)
        {
            jumpVelocity.z += jumpForwardBoost;
        }

        rb.linearVelocity = jumpVelocity;

        if (debugMovement)
        {
            Debug.Log($"JUMPED: Running={isRunning}", this);
        }
    }

    public float ReturnZAxis() => transform.position.z;
}