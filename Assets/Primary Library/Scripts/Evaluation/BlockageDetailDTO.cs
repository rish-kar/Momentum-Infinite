// ─────────────────────────────────────────────────────────────────────────────
// Momentum – Infinite  |  Safety / Diagnostics
// A single report captures everything needed to reproduce *one* blockage
// ─────────────────────────────────────────────────────────────────────────────
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Immutable snapshot generated the moment FlyerCorridorScanner detects a blocked
/// lane.  Serialisable to JSON, text or ScriptableObject for later upload.
/// </summary>
[Serializable]
public class BlockageDetailDTO
{
    /* ──   1  :  Global game state   ────────────────────────────────────── */
    public string  sceneName;                  // e.g. “Ground Level”
    public string  timestamp;                  // ISO-8601, local time
    public int     frame;                      // Time.frameCount when captured
    public float   timeSinceStart;             // Time.time

    /* ──   2  :  Player position & speed   ─────────────────────────────── */
    public Vector3 playerPos;
    public float   playerSpeed;                // m/s on last frame

    /* ──   3  :  Ground-generation context   ───────────────────────────── */
    public int     skyboxVariant;              // SkyboxChanger.CurrentSkyboxIdx
    public float   latestGroundZ;              // ProceduralTerrain.LatestGroundZ
    public float   tileLength;                 // copy from ProceduralTerrain
    public float   spawnPercentage;            // EnvironmentObjectSpawner
    public Vector2 xRange;
    public float   clearHalfWidth;
    public float   zJitter;
    public float   yOffset;

    /* ──   4  :  Corridor-scan specifics   ─────────────────────────────── */
    public float   tileStartZ;                 // starting Z of the tile tested
    public float   probeZ;                     // Z of the failing probe
    public float   laneHalfWidth;              // FlyerCorridorScanner
    public float   raySpacing;                 // distance between probes
    public int     maxAllowedHits;
    public int     hitsDetected;

    /* ──   5  :  Culprits (every collider hit)   ───────────────────────── */
    public List<CulpritInfo> culprits = new(); // filled only when blocked

    [Serializable]
    public struct CulpritInfo
    {
        public string   name;                  // GameObject name / prefab
        public Vector3  position;              // world-space centre
        public Vector3  size;                  // bounds.size
        public string   layer;                 // LayerMask.NameToLayer
    }
}
