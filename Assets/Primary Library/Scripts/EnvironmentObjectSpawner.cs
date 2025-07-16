using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.AI;

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

    [Header("Debug Settings")]
    [SerializeField] private bool debugSpawning = false;
    [SerializeField] private bool forceSpawn = false; // Force spawning even if percentage is low

    /*──────────── Private / cached ───────────────────────────────────*/
    SkyboxChanger skyboxChanger;
    static readonly Regex pfRegex = new("^PF_(\\d+)_", RegexOptions.Compiled);
    static readonly Dictionary<int, List<GameObject>> PrefabCache = new();

    /*──────────────────────────────────────────────────────────────────*/
    
    
    void Awake()
    {
        skyboxChanger = FindObjectOfType<SkyboxChanger>();
        if (!skyboxChanger && debugSpawning)
            Debug.LogWarning("SkyboxChanger not found – defaulting to variant 0.", this);

        if (PrefabCache.Count == 0) 
        {
            BuildPrefabCache();
            if (debugSpawning)
            {
                Debug.Log($"Built prefab cache with {PrefabCache.Count} variants", this);
                foreach (var kvp in PrefabCache)
                {
                    Debug.Log($"Variant {kvp.Key}: {kvp.Value.Count} prefabs", this);
                }
            }
        }
    }

    static void BuildPrefabCache()
    {
        var all = Resources.LoadAll<GameObject>("Prefabs/Shuffled Prefabs");
        if (all.Length == 0)
        {
            Debug.LogError("No prefabs found in Resources/Prefabs/Shuffled Prefabs! Objects will not spawn.");
            return;
        }
        
        foreach (var go in all)
        {
            if (!go) continue;
            var m = pfRegex.Match(go.name);
            if (!m.Success) 
            {
                // Try fallback naming pattern
                if (go.name.Contains("PF_"))
                {
                    Debug.LogWarning($"Prefab {go.name} doesn't match expected pattern PF_X_, adding to variant 0");
                    if (!PrefabCache.TryGetValue(0, out var list0))
                    {
                        list0 = new List<GameObject>();
                        PrefabCache[0] = list0;
                    }
                    list0.Add(go);
                }
                continue;
            }
            int variant = int.Parse(m.Groups[1].Value);
            if (!PrefabCache.TryGetValue(variant, out var list))
            {
                list = new List<GameObject>();
                PrefabCache[variant] = list;
            }
            list.Add(go);
        }
        
        Debug.Log($"Prefab cache built: {PrefabCache.Count} variants, {all.Length} total prefabs processed");
    }

    /*──────────── Public API ─────────────────────────────────────────*/
    /// <summary>
    /// Called from <c>ProceduralTerrain</c> immediately after it instantiates a
    /// new ground tile.
    /// </summary>
    public void SpawnObjectsOnGround(GameObject ground)
    {
        if (!ground) 
        {
            if (debugSpawning) Debug.LogWarning("SpawnObjectsOnGround called with null ground!", this);
            return;
        }

        int variant = skyboxChanger ? skyboxChanger.CurrentSkyboxIdx : 0;
        if (!PrefabCache.TryGetValue(variant, out var prefabs) || prefabs.Count == 0)
        {
            if (debugSpawning) Debug.LogWarning($"No prefabs available for variant {variant}", this);
            // Try fallback to variant 0
            if (variant != 0 && PrefabCache.TryGetValue(0, out var fallbackPrefabs) && fallbackPrefabs.Count > 0)
            {
                prefabs = fallbackPrefabs;
                if (debugSpawning) Debug.Log($"Using fallback variant 0 prefabs ({prefabs.Count} available)", this);
            }
            else
            {
                return; // nothing for current environment
            }
        }

        if (!ground.TryGetComponent(out Collider groundCol))
        {
            if (debugSpawning) Debug.LogWarning("Ground passed to SpawnObjectsOnGround has no collider!", this);
            return;
        }

        // Build a simple 1‑D WFC grid along the X‑axis of the tile.
        float width = xRange.y - xRange.x;
        int cells = Mathf.Max(Mathf.CeilToInt(width), 12); // at least 1m resolution
        float actualSpawnPercentage = forceSpawn ? 100f : spawnPercentage;
        int desired = Mathf.Clamp(Mathf.FloorToInt(cells * (actualSpawnPercentage * 0.01f)), 0, cells);

        if (debugSpawning) 
        {
            Debug.Log($"Spawning objects on ground {ground.name}: {cells} cells, {desired} desired spawns", this);
        }

        HashSet<int> collapsed = new();
        int attempts = 0;
        int actualSpawned = 0;
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
            
            
            // Check if this object should float (like UFOs)
            bool shouldFloat = IsFloatingObject(inst);
            
            if (shouldFloat)
            {
                // For floating objects like UFOs, position them above ground with their intended offset
                if (inst.TryGetComponent(out Collider c))
                {
                    float floatHeight = GetFloatingHeight(inst);
                    inst.transform.position = hit.point + new Vector3(0f, floatHeight, 0f);
                }
            }
            else
            {
                // For ground objects, ensure they sit properly on the ground
                SnapToGround(inst.transform);
            }

            // Attach helper so it self‑despawns when the player passes.
            if (!inst.TryGetComponent(out DespawnAfterPlayer _))
            {
                var d = inst.AddComponent<DespawnAfterPlayer>();
                // Try multiple methods to find player
                d.player = FindPlayerTransform();
            }
                
            // Lightweight WFC – block current cell and immediate neighbours.
            collapsed.Add(i);
            if (i > 0) collapsed.Add(i - 1);
            if (i < cells - 1) collapsed.Add(i + 1);
            
            actualSpawned++;
        }
        
        if (debugSpawning)
        {
            Debug.Log($"Spawned {actualSpawned} objects on ground {ground.name} (desired: {desired})", this);
        }
    }
    
    private Transform FindPlayerTransform()
    {
        // Multiple fallback methods to find player
        Transform playerTransform = null;
        
        // Method 1: Camera.main
        if (Camera.main) playerTransform = Camera.main.transform;
        
        // Method 2: Player tag
        if (!playerTransform)
        {
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj) playerTransform = playerObj.transform;
        }
        
        // Method 3: PlayerMovement component
        if (!playerTransform)
        {
            var playerMovement = FindFirstObjectByType<PlayerMovement>();
            if (playerMovement) playerTransform = playerMovement.transform;
        }
        
        if (!playerTransform && debugSpawning)
        {
            Debug.LogWarning("Could not find player transform for DespawnAfterPlayer component!", this);
        }
        
        return playerTransform;
    }
    
    void SnapToGround(Transform t)
    {
        // Shoot a ray straight down to find the terrain height
        if (Physics.Raycast(t.position + Vector3.up * 10f,
                Vector3.down,
                out RaycastHit hit, 50f, groundMask))
        {
            float bottomOffset = 0f;

            // Prefer a collider (trunk) over renderer (includes leaves) if it exists
            if (t.TryGetComponent(out Collider col))
            {
                bottomOffset = t.position.y - col.bounds.min.y;
            }
            else if (t.TryGetComponent(out Renderer rend))
            {
                bottomOffset = t.position.y - rend.bounds.min.y;
            }

            // Re-position so the lowest point touches the hit position
            t.position = hit.point + Vector3.up * bottomOffset;
        }
    }
    
    bool IsFloatingObject(GameObject obj)
    {
        // Check if this is a UFO or other floating object by name
        string objName = obj.name.ToLower();
        if (objName.Contains("ufo"))
        {
            return true;
        }
        
        // Add other floating object patterns here as needed
        // For example: if (objName.Contains("balloon") || objName.Contains("drone"))
        
        return false;
    }

    float GetFloatingHeight(GameObject obj)
    {
        string objName = obj.name.ToLower();
        
        // UFOs should float higher
        if (objName.Contains("ufo"))
        {
            return 3.5f + Random.Range(-0.5f, 0.5f); // 3-4 units above ground with some variation
        }
        
        // Default floating height for other floating objects
        return 2f;
    }
}

/*─────────────────────────────────────────────────────────────────────────*/
/// <summary>
/// Tiny component that deletes the GameObject once the player's Z‑position is
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
            // Try multiple methods to find player
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) 
            {
                player = p.transform;
            }
            else
            {
                var playerMovement = FindFirstObjectByType<PlayerMovement>();
                if (playerMovement) player = playerMovement.transform;
            }
            
            if (!player)
            {
                Debug.LogWarning($"DespawnAfterPlayer on {gameObject.name} could not find player!", this);
            }
        }
    }

    void Update()
    {
        if (!player) return;
        if (player.position.z - transform.position.z > despawnOffset)
        {
            Destroy(gameObject);
        }
    }
}
