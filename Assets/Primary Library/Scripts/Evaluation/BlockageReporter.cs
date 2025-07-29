using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Script for storing the data in the data transfer object and later using it to export and publish PDF.
/// </summary>
public static class BlockageReporter
{
    // Static references to all the other scripts needed
    static ProceduralTerrain proceduralTerrain;
    static EnvironmentObjectSpawnManager objectSpawner;
    static SkyboxChanger skyboxChanger;
    static PlayerMovement playerMovement;
    static Rigidbody playerRigidbody;

    /// <summary>
    /// Function triggered by agent reporting the problem.
    /// </summary>
    /// <param name="tileStartZ">Starting coordinates of Z Axis</param>
    /// <param name="probeZ">Probe Details on Z Axis</param>
    /// <param name="culprits">List of Culprits blocking the road</param>
    public static void ReportBlockage(float tileStartZ, float probeZ, List<BlockageDetailDTO.CulpritInfo> culprits)
    {
        // Ensure CrashCache (singleton because of static references)
        EnsureCrashCacheExists();

        // Check null values and assign using the scripts or components where the values are assigned
        if (!proceduralTerrain) proceduralTerrain = Object.FindFirstObjectByType<ProceduralTerrain>();
        if (!objectSpawner) objectSpawner = Object.FindFirstObjectByType<EnvironmentObjectSpawnManager>();
        if (!skyboxChanger) skyboxChanger = Object.FindFirstObjectByType<SkyboxChanger>();
        if (!playerMovement) playerMovement = Object.FindFirstObjectByType<PlayerMovement>();
        if (playerMovement && !playerRigidbody) playerRigidbody = playerMovement.GetComponent<Rigidbody>();

        var blockageDetailDTO = new BlockageDetailDTO
        {
            // Global states are usually pulled out through library functions operating the game
            sceneName = SceneManager.GetActiveScene().name,
            timestamp = System.DateTime.Now.ToString("o"),
            frame = Time.frameCount,
            timeSinceStart = Time.time,

            // Player movement related details
            playerPosition = playerMovement ? playerMovement.transform.position : Vector3.zero,
            playerSpeed = playerMovement ? playerMovement.CurrentSpeed : 0f,

            // Ground generated details
            skyboxVariant = skyboxChanger ? skyboxChanger.CurrentSkyboxIdx : -1,
            latestGroundZ = proceduralTerrain ? proceduralTerrain.LatestGroundZ : 0f,
            tileLength = proceduralTerrain ? proceduralTerrain.groundTileLength : 0f,

            // Parameters from the spawning script
            spawnPercentage = objectSpawner ? objectSpawner.spawnPercentage / 100f : 0f,
            xAxisRange = objectSpawner ? objectSpawner.xAxisRange : Vector2.zero,
            clearHalfWidth = objectSpawner ? objectSpawner.clearHalfWidth : 0f,
            zAxisJitter = objectSpawner ? objectSpawner.zAxisJitter : 0f,
            yAxisOffset = objectSpawner ? objectSpawner.yAxisOffset : 0f,

            culprits = culprits,
            hitsDetected = culprits.Count,

            // FlyerCorridor Scanner Details reported
            tileStartZ = tileStartZ,
            probeZ = probeZ,
            laneHalfWidth = 1.075f,
            raycastingGaps = 0.5f,
            maxAllowedHits = 0,
        };

        CrashCache.reports.Add(blockageDetailDTO);

        // Logs everytime a blockage is detected
        Debug.Log(
            $"BlockageReporter: Added report #{CrashCache.reports.Count} for blockage at Z={tileStartZ:F2} in scene '{blockageDetailDTO.sceneName}'");
    }

    /// <summary>
    /// Checking the crash cache exists (similar to BlockageReportDisplay Script)
    /// </summary>
    private static void EnsureCrashCacheExists()
    {
        if (Object.FindFirstObjectByType<CrashCache>() == null)
        {
            var gameObject = new GameObject("CrashCache");
            gameObject.AddComponent<CrashCache>();
        }
    }
}