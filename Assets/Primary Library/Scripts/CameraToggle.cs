using UnityEngine;

public class CameraToggle : MonoBehaviour
{
    // Assign the player's transform in the Inspector
    public Transform player;

    // Offsets for 3D and 2D modes
    public Vector3 offset3D = new Vector3(0f, 10f, -10f);
    public Vector3 offset2D = new Vector3(0f, 16f, 0f);

    // A reference rotation for 3D and a top-down rotation for 2D
    private Quaternion originalRotation;

    // Whether we are currently in 2D mode
    private bool is2D = false;

    // Parameters for smoothly animating between offsets
    public float transitionDuration = 1.0f;
    private bool isTransitioning = false;
    private float transitionTimer = 0f;

    // We’ll store the start and end positions/rotations for the animation
    private Vector3 startPos;
    private Quaternion startRot;
    private Vector3 targetPos;
    private Quaternion targetRot;

    void Start()
    {
        // Keep the camera's initial rotation for 3D mode
        originalRotation = transform.rotation;

        // Place the camera initially in 3D offset
        if (player != null)
        {
            transform.position = player.position + offset3D;
            transform.rotation = originalRotation;
        }
        else
        {
            Debug.LogWarning("Player is not assigned. Camera will remain at default position.");
        }
    }

    void Update()
    {
        // Check for toggle input
        if (Input.GetKeyDown(KeyCode.C) && !isTransitioning)
        {
            is2D = !is2D;
            BeginTransition(is2D);
        }

        // If we’re in the middle of a transition, animate
        if (isTransitioning)
        {
            transitionTimer += Time.deltaTime;
            float t = Mathf.Clamp01(transitionTimer / transitionDuration);

            // Lerp position and rotation
            transform.position = Vector3.Lerp(startPos, targetPos, t);
            transform.rotation = Quaternion.Slerp(startRot, targetRot, t);

            // If finished
            if (t >= 1f)
            {
                isTransitioning = false;
            }
        }
    }

    private void BeginTransition(bool to2D)
    {
        if (player == null)
        {
            Debug.LogWarning("No player assigned to the camera.");
            return;
        }

        isTransitioning = true;
        transitionTimer = 0f;

        // Save current position/rotation
        startPos = transform.position;
        startRot = transform.rotation;

        // Decide target position/rotation
        if (to2D)
        {
            targetPos = player.position + offset2D;
            targetRot = Quaternion.Euler(90f, 0f, 0f); // top-down
        }
        else
        {
            targetPos = player.position + offset3D;
            targetRot = originalRotation; // original 3D rotation
        }
    }

    // Keep following the player if we’re not transitioning
    void LateUpdate()
    {
        if (!isTransitioning && player != null)
        {
            if (is2D)
            {
                transform.position = player.position + offset2D;
                transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            }
            else
            {
                transform.position = player.position + offset3D;
                transform.rotation = originalRotation;
            }
        }
    }
}


