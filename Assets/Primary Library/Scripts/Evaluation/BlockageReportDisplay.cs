using UnityEngine;
using TMPro;
using System.Text;

public class BlockageReportDisplay : MonoBehaviour
{
  [Header("Optional extra margin (px)")]
    [SerializeField] int margin = 30;

    TextMeshProUGUI txt;
    void Awake()
    {
        txt = GetComponent<TextMeshProUGUI>();

        // give the Content object wide margins so nothing is clipped
        var rt = GetComponent<RectTransform>();
        rt.offsetMin = new Vector2(margin,  -margin); // left /  bottom
        rt.offsetMax = new Vector2(-margin, margin);  // right /  top
    }

    void Start()
{
    txt = GetComponent<TextMeshProUGUI>();

    // Ensure CrashCache singleton exists
    EnsureCrashCacheExists();

    var sb = new StringBuilder(2048);

    // Debug information
    Debug.Log($"BlockageReportDisplay: Found {CrashCache.reports.Count} reports");

    /* ── LEGEND ─────────────────────────────────────────────── */
    sb.AppendLine("<size=32><b>LEGEND</b></size>\n");
    sb.AppendLine("<b>Scene</b>: Level name at time of blockage.");
    sb.AppendLine("<b>Timestamp</b>: Local time in ISO format.");
    sb.AppendLine("<b>Frame</b>: Time.frameCount when blockage occurred.");
    sb.AppendLine("<b>Player Pos / Speed</b>: Position and forward velocity.");
    sb.AppendLine("<b>Skybox Variant</b>: Index used by SkyboxChanger.");
    sb.AppendLine("<b>Spawn %</b>: Probability of obstacle generation.");
    sb.AppendLine("<b>Tile Z</b>: Position of current ground tile.");
    sb.AppendLine("<b>Lane</b>: Width of safe corridor.");
    sb.AppendLine("<b>Ray Spacing</b>: Distance between horizontal probes.");
    sb.AppendLine("<b>Culprits</b>: Prefabs detected in blocked corridor.");
    sb.AppendLine("<line-height=60%>───────────────────────────────────────────────</line-height>\n");

    /* ── REPORT ENTRIES ─────────────────────────────────────── */
    if (CrashCache.reports.Count == 0)
    {
        sb.AppendLine("<size=24><color=yellow><b>No blockage reports found.</b></color></size>");
        sb.AppendLine("This could mean:");
        sb.AppendLine("• No blockages occurred during gameplay");
        sb.AppendLine("• Reports were not properly saved");
        sb.AppendLine("• Scene transition cleared the data");
    }
    else
    {
        foreach (var r in CrashCache.reports)
        {
            sb.AppendLine($"<size=28><b>✖ Blockage @ Z={r.tileStartZ:F2}</b></size>");
            sb.AppendLine($"<b>Scene</b>: {r.sceneName}    <b>Time</b>: {r.timestamp}");
            sb.AppendLine($"<b>Frame</b>: {r.frame}    <b>t</b>: {r.timeSinceStart:F2}s");
            sb.AppendLine($"<b>Player</b>: {r.playerPos:F2}");
            sb.AppendLine($"<b>Speed</b>: {r.playerSpeed:F2} m/s");
            sb.AppendLine($"<b>Skybox</b>: {r.skyboxVariant}");
            sb.AppendLine($"<b>Latest Ground Z</b>: {r.latestGroundZ:F1}");
            sb.AppendLine($"<b>TileLen</b>: {r.tileLength}");
            sb.AppendLine($"<b>Spawn Percentage</b>: {r.spawnPercentage:P0}");
            sb.AppendLine($"<b>Blocked Z</b>: {r.probeZ:F2}");
            sb.AppendLine($"<b>Lane </b>: {r.laneHalfWidth}");
            sb.AppendLine($"<b>Ray Spacing</b>: {r.raySpacing}");

            if (r.culprits.Count > 0)
            {
                sb.AppendLine("<b>Culprits:</b>");
                foreach (var c in r.culprits)
                    sb.AppendLine($"  • <b>{c.name}</b>  [{c.layer}]  size: {c.size:F2}  pos: {c.position:F2}");
            }

            sb.AppendLine("<line-height=50%>───────────────────────────────────────────────</line-height>\n");
        }
    }

    txt.text = sb.ToString();
}

private void EnsureCrashCacheExists()
{
    if (FindFirstObjectByType<CrashCache>() == null)
    {
        var go = new GameObject("CrashCache");
        go.AddComponent<CrashCache>();
        Debug.Log("BlockageReportDisplay: Created CrashCache singleton");
    }
}
}
