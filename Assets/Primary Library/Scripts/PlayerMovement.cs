using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float runSpeed = 8f;
    [SerializeField] private float sideSpeed = 12f;
    [SerializeField] private float jumpForce = 7f;
    [SerializeField] private float jumpForwardBoost = 4f;
    
    [Header("Visual Settings")]
    [SerializeField] private float leanAngle = 15f;
    [SerializeField] private float leanSpeed = 8f;
    
    [Header("Object References")]
    [SerializeField] private Transform playerModel;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Animator anim;
    [SerializeField] private Transform feet;
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private float groundCheckDistance = 0.3f;
    
    // Movement state
    private bool isGrounded;
    private bool isRunning;
    private bool isJumping;
    private bool wasRunningBeforeJump;
    private bool isFalling;
    private float horizontalInput;
    
    // Animation hashes
    private static readonly int JumpHash = Animator.StringToHash("Jump");
    
    void Start()
    {
        InitializeComponents();
    }
    
    private void InitializeComponents()
    {
        if (!rb) rb = GetComponent<Rigidbody>();
        if (!anim) anim = GetComponent<Animator>();
        
        // Disable root motion completely
        anim.applyRootMotion = false;
        
        // Proper rigidbody setup
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.linearDamping = 2f;
    }
    
    void Update()
    {
        HandleInput();
        CheckGroundStatus();
        CheckFallingState();
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
        
        // Jump
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded && !isJumping)
        {
            Jump();
        }
    }
    
    private void CheckGroundStatus()
    {
        bool wasGrounded = isGrounded;
        isGrounded = Physics.Raycast(feet.position, Vector3.down, groundCheckDistance, groundMask);
        
        // Handle landing from jump
        if (!wasGrounded && isGrounded && isJumping)
        {
            // Just landed from jump
            isJumping = false;
            isFalling = false;
            anim.SetBool(JumpHash, false);
            
            // Resume running if player was running before jump and W is still held
            if (wasRunningBeforeJump && Input.GetKey(KeyCode.W))
            {
                isRunning = true;
            }
        }
    }
    
    private void CheckFallingState()
    {
        // Check if player has fallen below ground level (death zone)
        if (transform.position.y < -5f && !isFalling)
        {
            isFalling = true;
            if (HasAnimationClip("Falling - Crypto"))
            {
                anim.Play("Falling - Crypto", 0, 0f);
            }
        }
        
        // Trigger respawn at -10f
        if (transform.position.y < -10f)
        {
            // Add your respawn logic here
            Debug.Log("Player fell too far - respawn needed");
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
        else if (isGrounded)
        {
            velocity.z = Mathf.Lerp(velocity.z, 0f, 10f * Time.fixedDeltaTime);
        }
        
        // Side movement - instant response
        if (isGrounded)
        {
            velocity.x = horizontalInput * sideSpeed;
        }
        
        rb.linearVelocity = velocity;
    }
    
    private void HandleAnimations()
    {
        if (!anim) return;
        
        // Priority: Falling > Jumping > Running/Movement > Idle
        if (isFalling)
        {
            // Keep falling animation
            return;
        }
        
        if (isJumping)
        {
            // Keep jump animation playing
            return;
        }
        
        if (isRunning && isGrounded)
        {
            // Running
            if (!anim.GetCurrentAnimatorStateInfo(0).IsName("Running - Crypto"))
            {
                anim.Play("Running - Crypto", 0, 0f);
            }
        }
        else if (isGrounded)
        {
            // Handle side step animations
            if (horizontalInput < -0.1f)
            {
                if (!anim.GetCurrentAnimatorStateInfo(0).IsName("Left - Crypto"))
                {
                    anim.Play("Left - Crypto", 0, 0f);
                }
            }
            else if (horizontalInput > 0.1f)
            {
                if (!anim.GetCurrentAnimatorStateInfo(0).IsName("Right - Crypto"))
                {
                    anim.Play("Right - Crypto", 0, 0f);
                }
            }
            else
            {
                // Idle
                if (!anim.GetCurrentAnimatorStateInfo(0).IsName("Idle 2 - Crypto"))
                {
                    anim.Play("Idle 2 - Crypto", 0, 0f);
                }
            }
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
        playerModel.localRotation = Quaternion.Slerp(playerModel.localRotation, targetRotation, leanSpeed * Time.fixedDeltaTime);
    }
    
    private void Jump()
    {
        // Remember if player was running before jump
        wasRunningBeforeJump = isRunning;
        
        isJumping = true;
        anim.SetBool(JumpHash, true);
        anim.Play("Jump - Crypto", 0, 0f);
        
        // Apply jump physics
        Vector3 jumpVelocity = rb.linearVelocity;
        jumpVelocity.y = jumpForce;
        
        // Add forward boost if running
        if (isRunning)
        {
            jumpVelocity.z += jumpForwardBoost;
        }
        
        rb.linearVelocity = jumpVelocity;
    }
    
    private bool HasAnimationClip(string clipName)
    {
        if (anim && anim.runtimeAnimatorController)
        {
            foreach (AnimationClip clip in anim.runtimeAnimatorController.animationClips)
            {
                if (clip.name == clipName)
                    return true;
            }
        }
        return false;
    }
    
    public float ReturnZAxis() => transform.position.z;
}
