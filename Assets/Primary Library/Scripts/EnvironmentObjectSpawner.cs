using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// Global manager that spawns (and later despawns) decorative props on every
/// newly‑generated ground tile.  Attach this to **one** empty GameObject named
/// "Environment Object Spawn Manager" and leave it in the root of the scene.
/// </summary>
[DisallowMultipleComponent]
public class EnvironmentObjectSpawnManager : MonoBehaviour
{
    /*──────────── Inspector ───────────────────────────────────────────*/
    [Header("Spawn Settings")]
    [Tooltip("Percentage of horizontal cells that will actually receive a prop (0‑100).")]
    [Range(0f, 100f)] public float spawnPercentage = 25f;

    [Tooltip("World‑space X range within which props may appear.")]
    public Vector2 xRange = new(-7.1f, 10.55f);

    [Tooltip("Half‑width of the clear corridor – keeps the runner’s lane free of clutter.")]
    public float clearHalfWidth = 2f;

    [Tooltip("Random ±Z offset applied per prop so they don’t sit on a perfect line.")]
    public float zJitter = 5f;

    [Tooltip("Extra Y lift so colliders sit flush on the ground (tweak per art‑style).")]
    public float yOffset = 0.0f;

    [SerializeField] private LayerMask groundMask = ~0;   // default = everything

    /*──────────── Private / cached ───────────────────────────────────*/
    SkyboxChanger skyboxChanger;
    static readonly Regex pfRegex = new("^PF_(\\d+)_", RegexOptions.Compiled);
    static readonly Dictionary<int, List<GameObject>> PrefabCache = new();

    /*──────────────────────────────────────────────────────────────────*/
    
    
    void Awake()
    {
        skyboxChanger = FindObjectOfType<SkyboxChanger>();
        if (!skyboxChanger)
            // Debug.LogWarning("SkyboxChanger not found – defaulting to variant 0.");

        if (PrefabCache.Count == 0) BuildPrefabCache();
    }

    static void BuildPrefabCache()
    {
        var all = Resources.LoadAll<GameObject>("Prefabs/Shuffled Prefabs");
        foreach (var go in all)
        {
            if (!go) continue;
            var m = pfRegex.Match(go.name);
            if (!m.Success) continue;
            int variant = int.Parse(m.Groups[1].Value);
            if (!PrefabCache.TryGetValue(variant, out var list))
            {
                list = new List<GameObject>();
                PrefabCache[variant] = list;
            }
            list.Add(go);
        }
    }

    /*──────────── Public API ─────────────────────────────────────────*/
    /// <summary>
    /// Called from <c>ProceduralTerrain</c> immediately after it instantiates a
    /// new ground tile.
    /// </summary>
    public void SpawnObjectsOnGround(GameObject ground)
    {
        if (!ground) return;

        int variant = skyboxChanger ? skyboxChanger.CurrentSkyboxIdx : 0;
        if (!PrefabCache.TryGetValue(variant, out var prefabs) || prefabs.Count == 0)
            return; // nothing for current environment

        if (!ground.TryGetComponent(out Collider groundCol))
        {
            // Debug.LogWarning("Ground passed to SpawnObjectsOnGround has no collider!");
            return;
        }

        // Build a simple 1‑D WFC grid along the X‑axis of the tile.
        float width = xRange.y - xRange.x;
        int cells = Mathf.Max(Mathf.CeilToInt(width), 12); // at least 1m resolution
        int desired = Mathf.Clamp(Mathf.FloorToInt(cells * (spawnPercentage * 0.01f)), 0, cells);

        HashSet<int> collapsed = new();
        int attempts = 0;
        while (collapsed.Count < desired && attempts++ < cells * 5)
        {
            int i = Random.Range(0, cells);
            if (collapsed.Contains(i)) continue;

            float localX = xRange.x + (i + 0.5f) * width / cells;
            if (Mathf.Abs(localX) < clearHalfWidth) continue; // keep centre lane clear

            Vector3 rayOrigin = new(localX, groundCol.bounds.max.y + 10f,
                                    groundCol.bounds.center.z + Random.Range(-zJitter, zJitter));
            if (!Physics.Raycast(rayOrigin, Vector3.down, out var hit, 30f, groundMask)) continue;

            GameObject prefab = prefabs[Random.Range(0, prefabs.Count)];
            GameObject inst   = Instantiate(prefab, hit.point, prefab.transform.rotation, transform);
            // Nudge vertically so it sits exactly on the floor (without scaling!)
            if (inst.TryGetComponent(out Collider c))
                inst.transform.position += new Vector3(0f, c.bounds.extents.y + yOffset, 0f);

            SnapToGround(inst.transform);

            
            // Attach helper so it self‑despawns when the player passes.
            if (!inst.TryGetComponent(out DespawnAfterPlayer _))
            {
                var d = inst.AddComponent<DespawnAfterPlayer>();
                d.player = Camera.main ? Camera.main.transform : null;
            }
                
            // Lightweight WFC – block current cell and immediate neighbours.
            collapsed.Add(i);
            if (i > 0) collapsed.Add(i - 1);
            if (i < cells - 1) collapsed.Add(i + 1);
        }
    }
    
    void SnapToGround(Transform t)
    {
        // Cast straight down so we know the exact terrain height
        if (Physics.Raycast(t.position + Vector3.up * 10f, Vector3.down,
                out RaycastHit hit, 50f, groundMask))
        {
            // If a mesh pivot is mid-trunk, offset so the bottom touches the hit point
            if (t.TryGetComponent(out Renderer r))
            {
                float pivotToBottom = r.bounds.min.y - t.position.y;
                t.position = hit.point - Vector3.up * pivotToBottom;
            }
            else
            {
                t.position = hit.point;
            }
        }
    }
    
}

/*─────────────────────────────────────────────────────────────────────────*/
/// <summary>
/// Tiny component that deletes the GameObject once the player’s Z‑position is
/// far enough ahead.  Keeps memory & physics tidy even if a tile is kept alive
/// for longer than the prop.
/// </summary>
public class DespawnAfterPlayer : MonoBehaviour
{
    public Transform player;
    public float despawnOffset = 50f; // metres behind the player before delete

    void Awake()
    {
        if (!player)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }
    }

    void Update()
    {
        if (!player) return;
        if (player.position.z - transform.position.z > despawnOffset)
            Destroy(gameObject);
    }
}
