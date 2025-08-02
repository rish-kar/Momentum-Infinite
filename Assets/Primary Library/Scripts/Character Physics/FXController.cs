using UnityEngine;

/// <summary>
/// Class to control the dust particles emitted by the feet of the player.
/// Acts as a visual effects controller.
/// </summary>
[RequireComponent(typeof(PlayerMovement))]
public class FXController : MonoBehaviour
{
    [Header("References Object")] 
    public PlayerMovement player; // Reference to the PlayerMovement script

    [Tooltip("Left Foot dust particle system")]
    public ParticleSystem leftFootDust; // Particle system attached to the left foot of the player

    [Tooltip("Right Foot dust particle system")]
    public ParticleSystem rightFootDust; // Particle system attached to the right foot of the player

    [Header("Tuning")] 
    [Tooltip("Speed at which dust emission just begins as the player starts moving")]
    public float minSpeedForDust = 1f;  // Minimum Emission Rate - Starting

    [Tooltip("Speed at which dust emission is at its maximum")]
    public float maxSpeedForDust = 20f; // Maximum Emission Rate - Ending

    [Tooltip("Maximum particles per unit moved (distance) at highest speed")]
    public float maxDustRateOverDistance = 30f;

    /// <summary>
    /// Function to initialize the FXController called before the first frame.
    /// </summary>
    void Start()
    {
        if (player == null)
            player = GetComponent<PlayerMovement>();

        if (leftFootDust != null) leftFootDust.Stop();
        if (rightFootDust != null) rightFootDust.Stop();
    }

    /// <summary>
    /// Function called once per frame to update visual effects.
    /// </summary>
    void Update()
    {
        // Basic Null Checks
        if (player == null) return;

        // Only emit dust when the player is grounded and in continuous motion state
        if (player.IsGrounded && player.IsRunning && player.CurrentAnimationStateName == "Running")

        {
            float t = Mathf.InverseLerp(minSpeedForDust, maxSpeedForDust, player.CurrentSpeed);
            float rate = t * maxDustRateOverDistance;

            // Set the values for dust emission
            UpdateEmitter(leftFootDust, rate);
            UpdateEmitter(rightFootDust, rate);
        }
        else
        {
            // Stop originating particles when the player is not in moving state
            if (leftFootDust != null && leftFootDust.isEmitting) leftFootDust.Stop();
            if (rightFootDust != null && rightFootDust.isEmitting) rightFootDust.Stop();
        }
    }

    /// <summary>
    /// Get emission module, disable then and enable time-based emission.
    /// Activate emission module and trigger play.
    /// </summary>
    /// <param name="particleSystem">Particle System Object</param>
    /// <param name="rateOverDistance">Variable to cover rate over distance with particles</param>
    private void UpdateEmitter(ParticleSystem particleSystem, float rateOverDistance)
    {
        if (particleSystem == null) return;

        var emission = particleSystem.emission;
        emission.rateOverTime = 0f; 
        emission.rateOverDistance = rateOverDistance; // Dust emission will follow movement
        emission.enabled = true;

        if (!particleSystem.isEmitting && rateOverDistance > 0f)
            particleSystem.Play();
    }
}