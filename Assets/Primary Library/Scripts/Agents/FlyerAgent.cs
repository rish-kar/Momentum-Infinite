using UnityEngine;

public class FlyerAgent : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform          player;      // runner
    [SerializeField] private ProceduralTerrain  terrain;     // terrain script that has LatestGroundZ

    [Header("Offsets")]
    [SerializeField] private float height        = 12f;      // Y altitude
    [SerializeField] private float lateralOffset = 0f;       // X offset from runner
    [SerializeField] private float forwardOffset = 200f;     // Z gap ahead of runner

    [Header("Smoothing")]
    [Tooltip("≈ time to remove 95 % of the gap (sec)")]
    [SerializeField] private float smoothTime    = 0.6f;
    [Tooltip("extra head-room above player speed")]
    [SerializeField] private float speedBuffer   = 5f;       // units/s

    /* ─────  internal  ───── */
    Vector3 velocity;
    float   lastPlayerZ;

    void Awake()
    {
        /* ---------- auto-wire ---------- */
        if (!player)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (!terrain)
            terrain = FindFirstObjectByType<ProceduralTerrain>();

        /* ---------- ensure physics won’t drag us down ---------- */
        if (TryGetComponent(out Rigidbody rb))
            rb.useGravity = false;
    }

    void Update()
    {
        if (!player)   return;

        /* ---------- target position ---------- */
        float targetZ = player.position.z + forwardOffset;

        // Clamp to LAST spawned tile so we don’t fly into empty space
        if (terrain) targetZ = Mathf.Min(targetZ, terrain.LatestGroundZ);  // LatestGroundZ added earlier

        Vector3 targetPos = new(
            player.position.x + lateralOffset,
            height,
            targetZ);

        /* ---------- adapt maxSpeed to player’s instant speed ---------- */
        float playerSpeedZ = (player.position.z - lastPlayerZ) / Time.deltaTime;
        float maxSpeed     = Mathf.Abs(playerSpeedZ) + speedBuffer;        // follow faster when runner speeds up
        lastPlayerZ        = player.position.z;

        /* ---------- smooth-damp move ---------- */
        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPos,
            ref velocity,
            smoothTime,
            maxSpeed);
    }
}