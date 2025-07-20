using UnityEngine;

[RequireComponent(typeof(PlayerMovement))]
public class FXController : MonoBehaviour
{
   
    [Header("References")]
    [Tooltip("Your PlayerMovement component")]
    public PlayerMovement player;

    [Tooltip("Left‐foot dust particle system")]
    public ParticleSystem leftDust;

    [Tooltip("Right‐foot dust particle system")]
    public ParticleSystem rightDust;

    [Header("Tuning")]
    [Tooltip("Speed at which dust emission just begins")]
    public float minSpeedForDust = 1f;

    [Tooltip("Speed at which dust emission is at its maximum rate")]
    public float maxSpeedForDust = 20f;

    [Tooltip("Maximum particles per unit moved (distance) at top speed")]
    public float maxDustRateOverDistance = 30f;

    void Start()
    {
        if (player == null)
            player = GetComponent<PlayerMovement>();

        if (leftDust  != null) leftDust .Stop();
        if (rightDust != null) rightDust.Stop();
    }

    void Update()
    {
        if (player == null) return;

        // only when both grounded AND running
        if (player.IsGrounded && player.IsRunning)
        {
            // normalize your speed into 0–1
            float t = Mathf.InverseLerp(minSpeedForDust, maxSpeedForDust, player.CurrentSpeed);
            float rate = t * maxDustRateOverDistance;

            // helper to set each emitter
            UpdateEmitter(leftDust, rate);
            UpdateEmitter(rightDust, rate);
        }
        else
        {
            // stop both if not grounded or not running
            if (leftDust  != null && leftDust .isEmitting) leftDust .Stop();
            if (rightDust != null && rightDust.isEmitting) rightDust.Stop();
        }
    }

    private void UpdateEmitter(ParticleSystem ps, float rateOverDistance)
    {
        if (ps == null) return;

        var emission = ps.emission;
        // ensure we're driving rate over distance, not time
        emission.rateOverTime      = 0f;                           // zero out time‐driven
        emission.rateOverDistance  = rateOverDistance;             // now dust follows movement
        emission.enabled           = true;

        if (!ps.isEmitting && rateOverDistance > 0f)
            ps.Play();
    }
}