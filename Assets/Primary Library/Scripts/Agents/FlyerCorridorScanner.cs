using UnityEngine;

[AddComponentMenu("Momentum/Procedural Safety/Flyer Corridor Raycast Scanner")]
public class FlyerCorridorRaycastScanner : MonoBehaviour
{
    [Header("Ground Settings")]
    [SerializeField] private float tileLength = 10f; 
    [SerializeField] private float tileWidth = 2.15f; 
    [SerializeField] private LayerMask obstacleMask;

    [Header("Player Clearance Requirements")]
    [SerializeField] private float minClearanceWidth = 1.5f;
    [SerializeField] private float rayHeightOffset = 1f; 
    [SerializeField] private float raycastSpacing = 0.5f;

    private float _lastScannedTileZ = float.MinValue;

    void FixedUpdate()
    {
        float tileZ = Mathf.Floor(transform.position.z / tileLength) * tileLength;

        if (tileZ <= _lastScannedTileZ) return;
        _lastScannedTileZ = tileZ;

        ScanCorridor(tileZ);
    }

    private void ScanCorridor(float tileZ)
    {
        float raycastY = transform.position.y - rayHeightOffset;

        int horizontalRayCount = Mathf.CeilToInt(tileLength / raycastSpacing);

        for (int i = 0; i <= horizontalRayCount; i++)
        {
            float currentZ = tileZ + (i * raycastSpacing);

            if (!CheckHorizontalClearance(currentZ, raycastY))
            {
                Debug.LogError($"[RUNWAY] Corridor blocked at tile starting Z={tileZ}, point Z={currentZ}");
                return;
            }
        }
    }

    private bool CheckHorizontalClearance(float currentZ, float raycastY)
    {
        float halfWidth = tileWidth * 0.5f;
        float clearanceStep = 0.05f;
        float currentClearance = 0f;

        for (float x = -halfWidth; x <= halfWidth; x += clearanceStep)
        {
            Vector3 rayOrigin = new Vector3(x, raycastY, currentZ);

            if (!Physics.Raycast(rayOrigin, Vector3.forward, raycastSpacing, obstacleMask))
            {
                currentClearance += clearanceStep;

                if (currentClearance >= minClearanceWidth)
                    return true; // Sufficient clearance found
            }
            else
            {
                currentClearance = 0f; // Obstacle encountered, reset clearance tracking
            }
        }
        return false; // No sufficient clearance found
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;

        float tileZ = Mathf.Floor(transform.position.z / tileLength) * tileLength;
        float raycastY = transform.position.y - rayHeightOffset;
        int horizontalRayCount = Mathf.CeilToInt(tileLength / raycastSpacing);
        float halfWidth = tileWidth * 0.5f;

        for (int i = 0; i <= horizontalRayCount; i++)
        {
            float currentZ = tileZ + (i * raycastSpacing);
            Vector3 from = new Vector3(-halfWidth, raycastY, currentZ);
            Vector3 to = new Vector3(halfWidth, raycastY, currentZ);
            Gizmos.DrawLine(from, to);
        }
    }
#endif
}
