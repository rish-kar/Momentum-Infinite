// Assets/Scripts/Diagnostics/BlockageReporter.cs

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// One static method the scanner calls; builds a BlockageDetailDTO,
/// fills it with live game data, stores it in CrashCache.reports.
/// </summary>
public static class BlockageReporter
{
    static ProceduralTerrain terrain;
    static EnvironmentObjectSpawnManager spawner;
    static SkyboxChanger skyboxChanger;
    static PlayerMovement playerMovement;
    static Rigidbody playerRb;

    public static void ReportBlockage(float tileStartZ, float probeZ, List<BlockageDetailDTO.CulpritInfo> culprits)
    {
        // Ensure CrashCache singleton exists
        EnsureCrashCacheExists();

        if (!terrain) terrain = Object.FindFirstObjectByType<ProceduralTerrain>();
        if (!spawner) spawner = Object.FindFirstObjectByType<EnvironmentObjectSpawnManager>();
        if (!skyboxChanger) skyboxChanger = Object.FindFirstObjectByType<SkyboxChanger>();
        if (!playerMovement) playerMovement = Object.FindFirstObjectByType<PlayerMovement>();
        if (playerMovement && !playerRb) playerRb = playerMovement.GetComponent<Rigidbody>();

        var dto = new BlockageDetailDTO
        {
            // Global game state
            sceneName = SceneManager.GetActiveScene().name,
            timestamp = System.DateTime.Now.ToString("o"),
            frame = Time.frameCount,
            timeSinceStart = Time.time,

            // Player position & speed
            playerPos = playerMovement ? playerMovement.transform.position : Vector3.zero,
            playerSpeed = playerMovement ? playerMovement.CurrentSpeed : 0f,

            // Ground-generation context
            skyboxVariant = skyboxChanger ? skyboxChanger.CurrentSkyboxIdx : -1,
            latestGroundZ = terrain ? terrain.LatestGroundZ : 0f,
            tileLength = terrain ? terrain.groundTileLength : 0f,

            // Environment object spawning details
            spawnPercentage = spawner ? spawner.spawnPercentage / 100f : 0f,
            xRange = spawner ? spawner.xAxisRange : Vector2.zero,
            clearHalfWidth = spawner ? spawner.clearHalfWidth : 0f,
            zJitter = spawner ? spawner.zAxisJitter : 0f,
            yOffset = spawner ? spawner.yAxisOffset : 0f,

            culprits = culprits,
            hitsDetected = culprits.Count,
            // Corridor-scan specifics (from FlyerCorridorRaycastScanner directly)
            tileStartZ = tileStartZ,
            probeZ = probeZ,
            laneHalfWidth = 1.075f, // from (tileWidth / 2), explicitly given as 2.15/2
            raySpacing = 0.5f, // directly from FlyerCorridorRaycastScanner raycastSpacing
            maxAllowedHits = 0, // implied from your logic (no hits allowed)
            // hitsDetected = 1, // implied from blockage logic (since blockage occurred)
        };

        CrashCache.reports.Add(dto);

        // Debug logging
        Debug.Log($"BlockageReporter: Added report #{CrashCache.reports.Count} for blockage at Z={tileStartZ:F2} in scene '{dto.sceneName}'");
    }

    private static void EnsureCrashCacheExists()
    {
        if (Object.FindFirstObjectByType<CrashCache>() == null)
        {
            var go = new GameObject("CrashCache");
            go.AddComponent<CrashCache>();
        }
    }
}