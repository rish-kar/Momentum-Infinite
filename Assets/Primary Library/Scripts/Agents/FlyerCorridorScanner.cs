using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Script attached to the flyer agent scans the ground procedurally created terrain ahead of the player
/// and reports every object where the colliders intrude into the safety lane the player has to traverse.
///
/// Basic Working Technique:
/// - Horizontal Probe: Scan the tile and determine if the tile is passable. Rows that fail are stored for record.
/// - Overlapping Sweeps: Physics overlapping box collects colliders that fall inside the tile volume.
/// - Obstacle Layering: Objects belong to the obstacle layer added in the working set.
/// - Publish Results: Details of object as forwarded to blockage reporter.
/// </summary>
[AddComponentMenu("Momentum/Procedural Safety/Flyer Corridor Raycast Scanner")]
public class FlyerCorridorScanner : MonoBehaviour
{
    [Header("Ground Settings for Procedrual Ground Detection")] [SerializeField]
    private float _lengthOfTile = 10f; // Size (scale) of each tile in Z-Axis units

    [SerializeField] private float _widthOfTile = 2.15f; // The total width of every procedurally spawned ground

    [SerializeField]
    public LayerMask obstacleMask; // Mask that detects all the objects to a special layer called 'Obstacle'

    [Header("Player Clearance Requirements")] [SerializeField]
    private float
        _minimumClearanceWidth =
            1.5f; // Minimum distance needed in the X-Axis to cross the tile and mark it as passable (this number is chosen as the scale of the player character is 1 and 0.5 is margin)

    [SerializeField] private float _heightForRayCasting = 1f; // Height for the project of rays to scan the ground
    [SerializeField] private float _raycastingGaps = 0.5f; // Gap or spacing of probe rows

    [Header("Sweeping")]
    [Tooltip("Extra margin (in metres) above ground when doing the OverlapBox sweep")]
    [SerializeField]
    private float _heightOfSweep = 4f;

    private float _lastScannedTileZAxis = float.MinValue;

    [Header("Object Deletion Feature")] [SerializeField]
    private bool
        _autoRemoveBlockersTrigger =
            true; // Trigger when enabled will delete the objects blocking the ground before the player reaches it

    private EnvironmentObjectSpawnManager _spawnManager;

    private const float HALF_MULTIPLIER = 0.5f;

    // Setter functions for native variables
    public void SetAutoRemove(bool on)
    {
        _autoRemoveBlockersTrigger = on;
    }

    public LayerMask ObstacleMask => obstacleMask;
    public float TileLength => _lengthOfTile;


    /// <summary>
    /// Function triggered at fixed intervals.
    /// </summary>
    private void FixedUpdate()
    {
        float tileZAxis = Mathf.Floor(transform.position.z / _lengthOfTile) * _lengthOfTile;
        if (tileZAxis <= _lastScannedTileZAxis) return; // If tile is already processed once, then return

        _lastScannedTileZAxis = tileZAxis;
        ScanCorridor(tileZAxis);
    }

    /// <summary>
    /// Function triggered after the game starts.
    /// </summary>
    void Awake()
    {
        // Parent object under which all the objects are spawned
        _spawnManager = FindFirstObjectByType<EnvironmentObjectSpawnManager>();
    }

    /// <summary>
    /// Get the parent name (original prefab name) of the prefab part on which the raycast hits.
    /// </summary>
    /// <param name="transform"></param>
    /// <returns></returns>
    Transform GetPrefabRoot(Transform transform)
    {
        // Basic Null Check
        if (!_spawnManager) return transform; // fallback
        var parent = _spawnManager.transform;
        while (transform.parent && transform.parent != parent)
            transform = transform
                .parent; // Parsing back till the root object is found (details of this object to be included in the report)
        return transform;
    }


    /// <summary>
    /// Primary Logic to scan the corridor.
    /// </summary>
    /// <param name="tileZAxis">Tile to be Scanned along Z-Axis</param>
    private void ScanCorridor(float tileZAxis)
    {
        float raycastYAxis = transform.position.y - _heightForRayCasting;
        int horizontalRows = Mathf.CeilToInt(_lengthOfTile / _raycastingGaps);
        float halfWidth = _widthOfTile * HALF_MULTIPLIER;

        // Collecting the collider details of the objects that are creating blockage (using HashSet to avoid duplicates)
        var culpritsSet = new HashSet<Collider>();
        var culpritsDTO =
            new List<BlockageDetailDTO.CulpritInfo>(); // Data Transfer object used here to store the internal values of the culprit objects for exact identification
        float firstBlockedZ = -1f;

        // Probing Rows Mechanism - Checks each horizontal row 
        for (int i = 0; i <= horizontalRows; i++)
        {
            float currentZAxis = tileZAxis + (i * _raycastingGaps);
            if (!CheckHorizontalClearance(currentZAxis, raycastYAxis, culpritsSet, culpritsDTO))
            {
                if (firstBlockedZ < 0f)
                {
                    firstBlockedZ = currentZAxis;
                }
            }
        }

        // Creates a 3D box covering the volume of the tile to catch if any obstacles are missed
        Vector3 centerOfOverlapBox = new(transform.position.x, raycastYAxis + _heightOfSweep * HALF_MULTIPLIER,
            tileZAxis + _lengthOfTile * HALF_MULTIPLIER);
        Vector3 halfOfOverlapBox = new(halfWidth, _heightOfSweep * HALF_MULTIPLIER, _lengthOfTile * HALF_MULTIPLIER);
        Collider[] hits = Physics.OverlapBox(centerOfOverlapBox, halfOfOverlapBox, Quaternion.identity, obstacleMask);

        foreach (var collider in hits)
        {
            if (culpritsSet.Add(collider))
                culpritsDTO.Add(ToCulpritInfo(collider));
        }

        // Block of code meant to test and report the exact position of blockage
        if (culpritsDTO.Count > 0)
        {
            Debug.LogError($"[RUNWAY] Corridor blocked at tile Z={tileZAxis}");

            // Blockage Reporter Called for Publishing Details on the Crash Scene
            BlockageReporter.ReportBlockage(tileZAxis, firstBlockedZ < 0 ? tileZAxis : firstBlockedZ, culpritsDTO);

            // Auto Remove Blocker Checkbox present in UI to trigger destruction of objects if pathway blocked
            if (_autoRemoveBlockersTrigger)
            {
                foreach (var collider in culpritsSet)
                {
                    // Null check: Skip Loop for a single iteration
                    if (!collider) continue;
                    var childObjectRoot = GetPrefabRoot(collider.transform);
                    if (childObjectRoot && childObjectRoot != _spawnManager.transform)
                        Destroy(childObjectRoot.gameObject);
                }
            }
        }
    }

    /// <summary>
    /// Function responsible for checking the horizontal ground clearance.
    /// </summary>
    /// <param name="currentZAxis">Z-Axis of Tile</param>
    /// <param name="raycastYAxis">Height from which they rays are casted</param>
    /// <param name="culpritsSet">Set of Culprits Colliders hit by ray casting</param>
    /// <param name="culpritsDTO">Class storing information about culprits</param>
    /// <returns></returns>
    private bool CheckHorizontalClearance(
        float currentZAxis,
        float raycastYAxis,
        HashSet<Collider> culpritsSet,
        List<BlockageDetailDTO.CulpritInfo> culpritsDTO)
    {
        float halfWidth = _widthOfTile * HALF_MULTIPLIER;
        float stepInXAxis = 0.05f;
        float currentGapWidth = 0f;

        for (float x = -halfWidth; x <= halfWidth; x += stepInXAxis)
        {
            Vector3 origin = new(x, raycastYAxis, currentZAxis);

            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 5f, obstacleMask))
            {
                currentGapWidth = 0f; // reset gap count

                if (culpritsSet.Add(hit.collider))
                    culpritsDTO.Add(ToCulpritInfo(hit.collider));
            }
            else
            {
                currentGapWidth += stepInXAxis;
                if (currentGapWidth >= _minimumClearanceWidth)
                    return true; // In this scenario, the row is passable, return value
            }
        }

        return false; // no sufficient gap found across the whole row, non-passable condition triggered
    }

    /// <summary>
    /// Stripping off the 'Clone' word from the name of the prefab and creating a DTO. 
    /// </summary>
    /// <param name="collider">Collider Object</param>
    /// <returns>Culprit Information</returns>
    private static BlockageDetailDTO.CulpritInfo ToCulpritInfo(Collider collider)
    {
        string rootName = collider.transform.root.name.Replace("(Clone)", "");
        string childName = collider.transform == collider.transform.root
            ? rootName
            : $"{rootName}/{collider.gameObject.name}";

        // Using a basic constructor to create a new object here
        return new BlockageDetailDTO.CulpritInfo
        {
            name = childName.Replace("(Clone)", ""),
            position = collider.bounds.center,
            size = collider.bounds.size,
            layer = LayerMask.LayerToName(collider.gameObject.layer)
        };
    }
}