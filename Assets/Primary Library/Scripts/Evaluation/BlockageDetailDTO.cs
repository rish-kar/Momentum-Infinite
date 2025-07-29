using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Data Transfer Object for detailed blockage reports.
/// Contains information about the objects, states and positions of objects causing a problem.
/// </summary>
[Serializable]
public class BlockageDetailDTO
{
    [Header("Scene References")]
    public string  sceneName;                   // Name of the game scene
    public string  timestamp;                   // local system time in ISO format 
    public int     frame;                       // Frame number when the blockage occurred
    public float   timeSinceStart;              // Time since that start of the scene

    [Header("Player Speed and Position")]
    public Vector3 playerPosition;              // Position of the player
    public float   playerSpeed;                 // speed of player

    [Header("Context of Ground Generation - Variables from EnvironmentObjectSpawnManager Script")]
    public int     skyboxVariant;              // Variant of Skybox index
    public float   latestGroundZ;              // The latest ground spawned
    public float   tileLength;                 // Length of tile spawned
    public float   spawnPercentage;            // Percentage of spawning
    public Vector2 xAxisRange;                 // Range of X Axis for spawning
    public float   clearHalfWidth;             // Half width of the clear corridor
    public float   zAxisJitter;                // Jitter is applied so that objects do not form in straight lines   
    public float   yAxisOffset;                // Offset to control spawning above the ground level

    [Header("Corridor Scanner Context")]
    public float   tileStartZ;                 // starting Z Axis coordinate of the tile tested
    public float   probeZ;                     // Z Axis of the failing probe
    public float   laneHalfWidth;              // FlyerCorridorScanner width of lane
    public float   raycastingGaps;             // Gap or spacing of probe rows
    public int     maxAllowedHits;
    public int     hitsDetected;

    [Header("Information Regarding Culprits Blocking the way")]
    public List<CulpritInfo> culprits = new(); // filled only when blocked

    [Serializable]
    public struct CulpritInfo
    {
        public string   name;                  // Game Object name / prefab
        public Vector3  position;              // Game Object Position
        public Vector3  size;                  // Size of the bounds
        public string   layer;                 // Layer - Obstacle always
    }
}
