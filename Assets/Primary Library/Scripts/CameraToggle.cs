using UnityEngine;

public class CameraToggle : MonoBehaviour
{
    // Assign the player's transform in the Inspector
    public Transform player;

    // Offsets for 3D and 2D modes
    public Vector3 offset3D = new Vector3(0f, 10f, -10f);
    public Vector3 offset2D = new Vector3(0f, 16f, 0f);

    // The camera's initial rotation for 3D mode
    private Quaternion originalRotation;

    // Whether we are currently in 2D mode
    private bool is2D = false;

    // Smooth transition parameters
    public float transitionDuration = 1.0f;
    private bool isTransitioning = false;
    private float transitionTimer = 0f;

    // Cache the start rotation for lerping, but we will dynamically track the player's position
    private Vector3 startPos;
    private Quaternion startRot;
    private Quaternion targetRot;

    void Start()
    {
        // Store the camera's initial rotation for 3D mode
        originalRotation = transform.rotation;

        // Place the camera at the 3D offset initially (if player is assigned)
        if (player != null)
        {
            transform.position = player.position + offset3D;
            transform.rotation = originalRotation;
        }
        else
        {
            Debug.LogWarning("No player assigned to the camera. Please assign one in the Inspector.");
        }
    }

    void Update()
    {
        // Toggle between 2D and 3D when "C" is pressed
        if (Input.GetKeyDown(KeyCode.C) && !isTransitioning)
        {
            is2D = !is2D;
            BeginTransition(is2D);
        }

        // If in the middle of a transition, animate position & rotation
        if (isTransitioning)
        {
            transitionTimer += Time.deltaTime;
            float t = Mathf.Clamp01(transitionTimer / transitionDuration);

            // Recalculate the current target position so the camera follows the player's movement
            Vector3 currentTargetPos = is2D
                ? player.position + offset2D
                : player.position + offset3D;

            // Lerp from the startPos to the (constantly updated) current target
            transform.position = Vector3.Lerp(startPos, currentTargetPos, t);

            // Slerp rotation from startRot to the final 2D or 3D rotation
            transform.rotation = Quaternion.Slerp(startRot, targetRot, t);

            // End transition if time has elapsed
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
            Debug.LogWarning("No player assigned to the camera. Transition canceled.");
            return;
        }

        isTransitioning = true;
        transitionTimer = 0f;

        // Capture the camera's position & rotation at the start
        startPos = transform.position;
        startRot = transform.rotation;

        // Decide target rotation (top-down for 2D, original for 3D)
        if (to2D)
        {
            targetRot = Quaternion.Euler(90f, 0f, 0f);
        }
        else
        {
            targetRot = originalRotation;
        }
    }

    void LateUpdate()
    {
        // If we're NOT in transition, directly follow the player.
        // This ensures the camera stays locked at the correct offset.
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


