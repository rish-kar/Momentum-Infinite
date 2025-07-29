using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// Spawning script (controlling the despawning mechanism as well) using Wave Function Collapse PCG Mechanism
/// to arrange and spawn objects on the ground. Attached to an empty game object (acting as the parent object)
/// under which the child prefab objects are being spawned.
/// </summary>
[DisallowMultipleComponent]
public class EnvironmentObjectSpawnManager : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("Percentage of horizontal cells that will actually receive a prop (0 to 100).")]
    [Range(0f, 100f)]
    public float spawnPercentage = 25f; // Basic parameter for controlling the number of objects being spawned randomly

    [Tooltip("X-Axis range within which objects will spawn.")]
    public Vector2 xAxisRange = new(-7.1f, 10.55f); // Setting range so that the objects do not spawn out of the ground

    [Tooltip("Half width to keep runner's lane clear of clusters.")]
    public float clearHalfWidth = 2f;

    [Tooltip("Random Z Axis offset applied per object.")]
    public float zAxisJitter = 5f; // Jitter is applied so that objects do not form in straight lines

    public float yAxisOffset = 0.0f; // Offset to control spawning above the ground level

    [SerializeField] private LayerMask
        groundMask =
            ~0; // This means all layers by default, Bitwise operator used here to make sure that multiple layers can be selected

    [Header("Debug Settings")] [SerializeField]
    private bool debugSpawning = false;

    [SerializeField] private bool forceSpawn = false; // Variable for force spawning for testing purposes


    // Cache References along with environment change
    SkyboxChanger skyboxChanger; // Detects the current skybox variant to spawn objects accordingly

    static readonly Regex
        pfRegex = new("^PF_(\\d+)_",
            RegexOptions
                .Compiled); // Prefab objects are named in the following order: PF_<skybox-variant>_<object-number>

    static readonly Dictionary<int, List<GameObject>>
        PrefabCache = new(); // Caching all objects to prevent memory overload


    // These objects have mesh in the bottom which are not suitable for dropping down from height and sticking to the ground.
    // In this case, we force the objects to be embedded in the ground at specific Y Axis position.
    // Used to solve the flying trees problem and the falling rock problem.
    // This is a workaround and it will generate mesh collider related warnings in the logs.
    static readonly HashSet<string> ForceYNames = new(new[]
    {
        "pf_1_1", "pf_1_2", "pf_1_3", "pf_3_2"
    });

    const float ForcedY = -0.5673f; // Exact fixed position tried and tested while the game runs

    /// <summary>
    /// The method checks if the Game object prefab's name matches the list of names mentioned
    /// in the static set called 'ForceYNames' to snap them to the ground.
    /// </summary>
    /// <param name="gameObject"></param>
    /// <returns></returns>
    static bool ShouldForceY(GameObject gameObject)
    {
        string prefabName = gameObject.name;
        const string clone = "(Clone)";
        if (prefabName.EndsWith(clone)) prefabName = prefabName.Substring(0, prefabName.Length - clone.Length);
        return ForceYNames.Contains(prefabName.ToLowerInvariant());
    }

    /// <summary>
    /// Triggered when the game starts.
    /// </summary>
    void Awake()
    {
        skyboxChanger =
            FindObjectOfType<SkyboxChanger>(); // Assigned to an object named 'Background Graphics' in the inspector
        if (!skyboxChanger && debugSpawning)
            // Fallback condition if Skybox changer object is null
            Debug.LogWarning("SkyboxChanger not found : defaulting to variant 0.", this);

        if (PrefabCache.Count == 0)
        {
            BuildPrefabCache();
            if (debugSpawning)
            {
                Debug.Log($"Built prefab cache with {PrefabCache.Count} variants", this);
                foreach (var prefab in PrefabCache)
                {
                    Debug.Log($"Variant {prefab.Key}: {prefab.Value.Count} prefabs", this);
                }
            }
        }
    }

    /// <summary>
    /// Loading all prefabs from Resources/Prefabs/Shuffled Prefabs folder and storing them
    /// in a dictionary based on their variant number.
    /// </summary>
    static void BuildPrefabCache()
    {
        var all = Resources.LoadAll<GameObject>("Prefabs/Shuffled Prefabs");
        if (all.Length == 0)
        {
            // Error condition if prefabs get deleted from the resources folder
            Debug.LogError("No prefabs found in Resources/Prefabs/Shuffled Prefabs! Objects will not spawn.");
            return;
        }

        foreach (var gameObject in all)
        {
            // Null check
            if (!gameObject) continue;

            var match = pfRegex.Match(gameObject.name);
            if (!match.Success)
            {
                // If direct match does not work in this case, trying to match using fallback technique
                if (gameObject.name.Contains("PF_"))
                {
                    Debug.LogWarning(
                        $"Prefab {gameObject.name} doesn't match expected pattern PF_X_Y, falling back by adding variant 0");

                    if (!PrefabCache.TryGetValue(0, out var resetList))
                    {
                        resetList = new List<GameObject>();
                        PrefabCache[0] = resetList;
                    }

                    resetList.Add(gameObject);
                }

                continue;
            }

            // If match is successful, organise by variant number in a list
            int variant = int.Parse(match.Groups[1].Value);
            if (!PrefabCache.TryGetValue(variant, out var list))
            {
                list = new List<GameObject>();
                PrefabCache[variant] = list;
            }

            list.Add(gameObject);
        }

        Debug.Log($"Prefab cache built: {PrefabCache.Count} variants, {all.Length} total prefabs processed");
    }

    /// <summary>
    /// The original Wave Function Collapse algorithm which is called from the Procedural Terrain script after the ground
    /// is insantiated and ready for object spawning.
    /// </summary>
    /// <param name="ground">Ground Object</param>
    public void SpawnObjectsOnGround(GameObject ground)
    {
        // Null check for ground passed from the procedural terrain script
        if (!ground)
        {
            if (debugSpawning) Debug.LogWarning("SpawnObjectsOnGround called with null ground!", this);
            return;
        }

        // Getting the index of the current skybox variant
        int variant = skyboxChanger ? skyboxChanger.CurrentSkyboxIdx : 0;

        // Tries to get the values from the PrefabCache built using the built function (fallback condition).
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
                return;
            }
        }

        // Check for ground collider, otherwise objects might fall through the ground if gravity is enabled in the prefabs.
        if (!ground.TryGetComponent(out Collider groundCollider))
        {
            if (debugSpawning) Debug.LogWarning("Ground passed to SpawnObjectsOnGround has no collider!", this);
            return;
        }

        // Wave Function Collapse Grid Building Section
        float
            width = xAxisRange.y -
                    xAxisRange.x; // Determine width by subrtracting the values mentioned in the range on top of this file
        int cells = Mathf.Max(Mathf.CeilToInt(width), 12);
        float actualSpawnPercentage =
            forceSpawn ? 100f : spawnPercentage; // Force spawn will always generate 100% of objects for testing
        int desired = Mathf.Clamp(Mathf.FloorToInt(cells * (actualSpawnPercentage * 0.01f)), 0, cells);

        // Using debugSpawning flag everything in the project as it gets harder to track objects using direct Debugger since framerate is involved.
        if (debugSpawning)
        {
            Debug.Log($"Spawning objects on ground {ground.name}: {cells} cells, {desired} desired spawns", this);
        }


        HashSet<int> collapsed = new(); // Responsible for finding the cells that are already filled with objects
        int attempts = 0; // Random selection can trigger infinite loops, this is a safety counter
        int actuallySpawnedObjects = 0; // Counts objects placed

        while (collapsed.Count < desired && attempts++ < cells * 5)
        {
            int i = Random.Range(0, cells); // Randomly selecting cells
            if (collapsed.Contains(i)) continue; // Check if already spawned, then skip iteration

            float localXAxis = xAxisRange.x + (i + 0.5f) * width / cells;
            if (Mathf.Abs(localXAxis) < clearHalfWidth) continue;

            // Perform raycast above the ground to get a valid point for spawning the object
            Vector3 rayOrigin = new(localXAxis, groundCollider.bounds.max.y + 10f,
                groundCollider.bounds.center.z + Random.Range(-zAxisJitter, zAxisJitter));
            if (!Physics.Raycast(rayOrigin, Vector3.down, out var hit, 30f, groundMask)) continue;

            GameObject
                prefab = prefabs[
                    Random.Range(0,
                        prefabs.Count)]; // Randomly choose a prefab from a list of prefabs for a particular vairant
            GameObject instantiatedGameObject =
                Instantiate(prefab, hit.point, prefab.transform.rotation,
                    transform); // Instantiate at the position of ground hit


            // Only valid for the UFO object, rest all objects are grounded
            bool shouldFloat = IsFloatingObject(instantiatedGameObject);

            if (shouldFloat)
            {
                // For floating objects like UFOs, setting a Y-Axis offset position
                if (instantiatedGameObject.TryGetComponent(out Collider c))
                {
                    float floatHeight = GetFloatingHeight(instantiatedGameObject);
                    instantiatedGameObject.transform.position = hit.point + new Vector3(0f, floatHeight, 0f);
                }
            }
            else
            {
                SnapToGround(instantiatedGameObject.transform);
            }

            // Triggered only for Trees and a mountain rock object for variant 3
            if (ShouldForceY(instantiatedGameObject))
            {
                var position = instantiatedGameObject.transform.position;
                instantiatedGameObject.transform.position =
                    new Vector3(position.x, ForcedY,
                        position.z); // Only change the Y axis to force the object into the ground
            }

            // Automatically despawns after the player crosses the object in the positive direction of Z-Axis
            if (!instantiatedGameObject.TryGetComponent(out DespawnAfterPlayer _))
            {
                var despawnComponent = instantiatedGameObject.AddComponent<DespawnAfterPlayer>();
                // Try multiple methods to find player, fallback approach used so that the game does not crash
                despawnComponent.playerTransform = FindPlayerTransform();
            }

            // WFC Constraints applied: Mark current cell occupied, mark adjacent cells occupied as well to prevent clustering
            collapsed.Add(i);
            if (i > 0) collapsed.Add(i - 1);
            if (i < cells - 1) collapsed.Add(i + 1);

            actuallySpawnedObjects++; // Once game object is added, increment the counter
        }

        if (debugSpawning)
        {
            Debug.Log($"Spawned {actuallySpawnedObjects} objects on ground {ground.name}", this);
        }
    }

    /// <summary>
    /// Find the transform of the main player.
    /// </summary>
    /// <returns>Transform Values: Position, Rotation, Scale</returns>
    private Transform FindPlayerTransform()
    {
        Transform playerTransform = null;

        // Find by object name (we cannot use tags here as Player object is tagged as 'Character' and in future multiple characters can exist)
        if (!playerTransform)
        {
            var playerGameObject = GameObject.Find("Player");
            if (playerGameObject) playerTransform = playerGameObject.transform;
        }

        // Find by Script attached to the object
        if (!playerTransform)
        {
            var playerMovement = FindFirstObjectByType<PlayerMovement>();
            if (playerMovement) playerTransform = playerMovement.transform;
        }

        if (!playerTransform && debugSpawning)
        {
            Debug.LogWarning("Could not find player transform for DespawnAfterPlayer component!", this);
            Debug.LogError("Despawning Mechanism will not trigger. Game might crash soon.");
        }
        return playerTransform;
    }

    /// <summary>
    /// Function used to snap the objects to the ground by using raycasting mechanism.
    /// </summary>
    /// <param name="transformDetails">Transform of the object</param>
    void SnapToGround(Transform transformDetails)
    {
        // Shoot a ray straight down to find the terrain height
        if (Physics.Raycast(transformDetails.position + Vector3.up * 10f,
                Vector3.down,
                out RaycastHit hit, 50f, groundMask))
        {
            float bottomOffset = 0f;
            
            // Used to calculate the distance from the centre of the object to the lowest point in edge
            if (transformDetails.TryGetComponent(out Collider collider))
            {
                bottomOffset = transformDetails.position.y - collider.bounds.min.y;
            }
            else if (transformDetails.TryGetComponent(out Renderer rend))
            {
                bottomOffset = transformDetails.position.y - rend.bounds.min.y;
            }

            // Re-position this so the lowest point touches the hit position (on ground)
            transformDetails.position = hit.point + Vector3.up * bottomOffset;
        }
    }

    /// <summary>
    /// Check if the object is UFO.
    /// </summary>
    /// <param name="gameObject">Object Details</param>
    /// <returns></returns>
    bool IsFloatingObject(GameObject gameObject)
    {
        string objectName = gameObject.name.ToLower();
        if (objectName.Contains("ufo"))
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// Returns a random range for the height of the UFO.
    /// IsKinematic is disabled on UFO prefab (which means gravity and physics will not effect the object).
    /// </summary>
    /// <param name="gameObject"></param>
    /// <returns></returns>
    float GetFloatingHeight(GameObject gameObject)
    {
        string objectName = gameObject.name.ToLower();
        
        if (objectName.Contains("ufo"))
        {
            return 3.5f + Random.Range(-0.5f, 0.5f); // 3-4 units above ground with some variation
        }

        return 2f; // Random height for future scope of objects
    }
}

/// <summary>
/// Class responsible for automatically despawning the objects once the player crossed them in positive
/// direction of the Z-Axis. Used to keep game performance consistent.
/// The ground despawns after this trigger as the offset for ground is 400f whereas for objects is 50f.
/// </summary>
public class DespawnAfterPlayer : MonoBehaviour
{
    public Transform playerTransform;
    public float despawnOffset = 50f; // Distance behind the player after which object will get deleted
    // The number 50f is intentional as ground gets deleted after 400f. If ground is deleted first, the objects
    // will fall through the ground and rapid velocity and transform changes will cause performance issues.

    /// <summary>
    /// Triggered when the game starts.
    /// </summary>
    void Awake()
    {
        if (!playerTransform)
        {
            var playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject)
            {
                playerTransform = playerObject.transform;
            }
            else
            {
                var playerMovement = FindFirstObjectByType<PlayerMovement>();
                if (playerMovement) playerTransform = playerMovement.transform;
            }

            if (!playerTransform)
            {
                Debug.LogWarning($"DespawnAfterPlayer on {gameObject.name} could not find player!", this);
                Debug.LogError("Game might crash soon as despawning mechanism will not work.");
            }
        }
    }

    /// <summary>
    /// Called once per frame.
    /// </summary>
    void Update()
    {
        if (!playerTransform) return;
        if (playerTransform.position.z - transform.position.z > despawnOffset)
        {
            Destroy(gameObject);
        }
    }
}