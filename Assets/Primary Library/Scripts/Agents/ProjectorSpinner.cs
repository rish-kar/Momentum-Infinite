using UnityEngine;

/// <summary>
/// Minor script responsible for the projection on the ground by the flyer agent to rotate
/// and give a visually appealing look while it runs ahead of the player.
/// The sprites are set in the inspector window.
/// </summary>
public class ProjectorSpinner : MonoBehaviour
{
    [SerializeField] private float _rotationSpeed = 30f; // degree of rotation per second

    /// <summary>
    /// Called once per every frame.
    /// </summary>
    void Update()
    {
        transform.Rotate(Vector3.forward, _rotationSpeed * Time.deltaTime, Space.Self);
    }
}