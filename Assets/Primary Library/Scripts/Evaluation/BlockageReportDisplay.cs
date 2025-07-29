using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Text;

/// <summary>
/// Class with functionality to publish the result in the Exit Report Scene
/// </summary>
public class BlockageReportDisplayBlockageReportDisplay : MonoBehaviour
{
    [SerializeField] int singleMarginValue = 30;

    [SerializeField] TextMeshProUGUI textMeshPro;

    /// <summary>
    /// Triggered when the game starts.
    /// </summary>
    void Awake()
    {
        // Basic Null Check and assignment
        if (!textMeshPro) textMeshPro = GetComponent<TextMeshProUGUI>();

        var transformForUIElement = (RectTransform)transform;
        transformForUIElement.offsetMin =
            new Vector2(singleMarginValue, -singleMarginValue); // For Left and Bottom Side
        transformForUIElement.offsetMax = new Vector2(-singleMarginValue, singleMarginValue); // For Right and Top Side
    }

    /// <summary>
    /// Start is triggered before the first frame of the game begins.
    /// </summary>
    void Start()
    {
        // Null check
        if (!textMeshPro) textMeshPro = GetComponent<TextMeshProUGUI>();
        EnsureCrashCacheExists();

        var stringBuilder = new StringBuilder(4096); // Mutatable Object to prevent multiple object creation

        // Legend : Understanding the parameters published
        stringBuilder.AppendLine("<size=32><b>LEGEND</b></size>\n");
        stringBuilder.AppendLine("<b>Scene</b>: Level name at time of blockage.");
        stringBuilder.AppendLine("<b>Timestamp</b>: Local time in ISO format.");
        stringBuilder.AppendLine("<b>Frame</b>: Time.frameCount when blockage occurred.");
        stringBuilder.AppendLine("<b>Player Pos / Speed</b>: Position and forward velocity.");
        stringBuilder.AppendLine("<b>Skybox Variant</b>: Index used by SkyboxChanger.");
        stringBuilder.AppendLine("<b>Spawn %</b>: Probability of obstacle generation.");
        stringBuilder.AppendLine("<b>Tile Z</b>: Position of current ground tile.");
        stringBuilder.AppendLine("<b>Lane</b>: Width of safe corridor.");
        stringBuilder.AppendLine("<b>Ray Spacing</b>: Spacing or distance between horizontal probes.");
        stringBuilder.AppendLine("<b>Culprits</b>: Details of Prefabs detected in blocked corridor.");
        stringBuilder.AppendLine("<line-height=60%>───────────────────────────────────────────────</line-height>\n");

        // This block triggered indicates that the blockages are not detected yet.
        if (CrashCache.reports.Count == 0)
        {
            stringBuilder.AppendLine("<size=24><color=yellow><b>No blockage reports found.</b></color></size>");
            stringBuilder.AppendLine("This could mean:");
            stringBuilder.AppendLine("1. No blockages occurred during gameplay");
            stringBuilder.AppendLine("2. The is a problem with the saving mechanism of crash details");
            stringBuilder.AppendLine("3. Scene transition cleared the data due to automatic cache wipe-out");
        }
        else
        {
            foreach (var singleReport in CrashCache.reports)
            {
                stringBuilder.AppendLine($"<size=28><b>• Blockage @ Z={singleReport.tileStartZ:F2}</b></size>");
                stringBuilder.AppendLine(
                    $"<b>Scene</b>: {singleReport.sceneName}    <b>Time</b>: {singleReport.timestamp}");
                stringBuilder.AppendLine(
                    $"<b>Frame</b>: {singleReport.frame}    <b>t</b>: {singleReport.timeSinceStart:F2}s");
                stringBuilder.AppendLine($"<b>Player</b>: {singleReport.playerPosition:F2}");
                stringBuilder.AppendLine($"<b>Speed</b>: {singleReport.playerSpeed:F2} m/s");
                stringBuilder.AppendLine($"<b>Skybox</b>: {singleReport.skyboxVariant}");
                stringBuilder.AppendLine($"<b>Latest Ground Z</b>: {singleReport.latestGroundZ:F1}");
                stringBuilder.AppendLine($"<b>TileLen</b>: {singleReport.tileLength}");
                stringBuilder.AppendLine($"<b>Spawn Percentage</b>: {singleReport.spawnPercentage:P0}");
                stringBuilder.AppendLine($"<b>Blocked Z</b>: {singleReport.probeZ:F2}");
                stringBuilder.AppendLine($"<b>Lane </b>: {singleReport.laneHalfWidth}");
                stringBuilder.AppendLine($"<b>Ray Spacing</b>: {singleReport.raycastingGaps}");

                if (singleReport.culprits.Count > 0)
                {
                    var printed = new HashSet<string>();
                    stringBuilder.AppendLine("<b>Culprits:</b>");
                    foreach (var c in singleReport.culprits)
                    {
                        string key = $"{c.name}@{c.position}";
                        if (printed.Add(key))
                            stringBuilder.AppendLine(
                                $"  • <b>{c.name}</b>  [{c.layer}]  size: {c.size:F2}  pos: {c.position:F2}");
                    }
                }
                stringBuilder.AppendLine(
                    "<line-height=50%>───────────────────────────────────────────────</line-height>\n");
            }
        }

        textMeshPro.text = stringBuilder.ToString();
    }

    /// <summary>
    /// Verify that the Crash Cache object exists and if not, then initialise it.
    /// </summary>
    private void EnsureCrashCacheExists()
    {
        if (FindFirstObjectByType<CrashCache>() == null)
        {
            var gameObject = new GameObject("CrashCache");
            gameObject.AddComponent<CrashCache>();
        }
    }
}