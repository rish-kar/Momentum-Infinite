using UnityEngine;

public class ProjectorSpinner : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 30f; // degrees per second

    void Update()
    {
        transform.Rotate(Vector3.forward, rotationSpeed * Time.deltaTime, Space.Self);
    }
}