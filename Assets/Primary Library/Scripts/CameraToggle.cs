using UnityEngine;

public class CameraToggle : MonoBehaviour
{
    
    // Assign the player's transform in the Inspector
    public Transform player;

    // Offsets for 3D and 2D modes
    public Vector3 offset3D = new Vector3(0f, 1.68f, -5.01f);
    public Vector3 offset2D = new Vector3(-0.39f, 16f, 6.06f);

    // Store the camera's original rotation
    private Quaternion originalRotation;

    // Whether we are currently in 2D mode
    private bool is2D = false;

    // Reference to the camera component
    private Camera cam;

    void Start()
    {
        // Save the initial camera rotation
        originalRotation = transform.rotation;
        // Grab the camera component
        cam = GetComponent<Camera>();
        if (cam == null)
        {
            Debug.LogError("No Camera component found on this GameObject.");
        }
    }

    void Update()
    {
        // Toggle between 2D and 3D when "C" is pressed
        if (Input.GetKeyDown(KeyCode.C))
        {
            is2D = !is2D;
        }
    }

    void LateUpdate()
    {
        // Ensure the player is assigned
        if (player == null)
        {
            Debug.LogWarning("No player assigned to the camera. Please assign a player transform.");
            return;
        }

        // If we have a Camera component, switch projection
        if (cam != null)
        {
            cam.orthographic = is2D;  // Orthographic for 2D, Perspective for 3D
        }

        if (is2D)
        {
            // 2D top-down view
            cam.orthographicSize = 12f;
            transform.position = player.position + offset2D;
            transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }
        else
        {
            // 3D view
            transform.position = player.position + offset3D;
            transform.rotation = originalRotation;
        }
    }
    
}
