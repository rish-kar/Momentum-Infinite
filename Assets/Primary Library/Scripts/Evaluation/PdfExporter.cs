using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Text.RegularExpressions;

#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

/* =====================================================================
 *  PDF EXPORTER (v3.2) – Momentum‑Infinite
 *  -------------------------------------------------------------------
 *  ▸ Attach to the DownloadPdfButton GameObject.
 *  ▸ Drag the **Report Text** (TextMeshProUGUI) from the Exit‑Report scene into
 *    <reportText>.
 *  ▸ Link the button’s OnClick() → PdfExporter.Download().
 *
 *  Changes in 3.2
 *  ----------------
 *  ✓ File names now include an ISO‑like timestamp (YYYYMMDD‑HHMMSS‑fff) so every
 *    click produces a **unique** report on disk / browser downloads folder.
 *  ✓ Early‑exit guard if the script isn’t wired → a yellow Warning instead of a
 *    silent fail so you can spot missing references.
 *  ✓ Minor refactor: string building moved to single method for clarity.
 * ===================================================================== */

public class PdfExporter : MonoBehaviour
{
    [Header("Links")] [SerializeField] private TextMeshProUGUI reportText;

    [Header("Settings")] [SerializeField] private int linesPerPage = 45; // tweak if you change font size
    private const float PAGE_W = 595f; // A4 portrait @ 72 dpi
    private const float PAGE_H = 842f;

    const float TopMargin = 40f, BottomMargin = 40f, LineH = 12f;
    int maxLinesPerPage = Mathf.FloorToInt((PAGE_H - TopMargin - BottomMargin) / LineH); // ≈ 63

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void DownloadPdfFile(string name, string dataBase64);
#endif

    // Add next to other statics in PdfExporter
    const int MaxCols = 85; // ≈ (595 - 40 - 40) / (10pt * 0.6) → page width / char width

    static void WrapLine(string line, List<string> outLines, int width)
    {
        if (string.IsNullOrEmpty(line))
        {
            outLines.Add("");
            return;
        }

        int i = 0;
        while (i < line.Length)
        {
            int len = Mathf.Min(width, line.Length - i);
            int end = i + len;

            // try word boundary if we are mid‑word
            if (end < line.Length && !char.IsWhiteSpace(line[end]))
            {
                int space = line.LastIndexOf(' ', end - 1, len);
                if (space > i) len = space - i; // wrap at last space
            }

            outLines.Add(line.Substring(i, len));
            i += len;

            // skip any extra spaces at the start of the next segment
            while (i < line.Length && line[i] == ' ') i++;
        }
    }


    private void Awake() // auto-hook if designer forgot
    {
        if (TryGetComponent(out Button btn) &&
            btn.onClick.GetPersistentEventCount() == 0)
        {
            btn.onClick.AddListener(Download);
        }
    }

    /* ------------------------------------------------------------ */
    public void Download()
    {
        if (reportText == null)
        {
            Debug.LogWarning("PdfExporter: <reportText> reference missing on " + name +
                             ". Attach the component and set it in the Inspector.");
            return;
        }

        // 1. Grab the on-screen string, remove TMP tags / unsupported bullets → plain ASCII
        string plain = Regex.Replace(reportText.text, "<.*?>", string.Empty);
        plain = plain.Replace("•", "*").Replace('─', '-');
        plain = plain.Replace("\r", ""); // avoid CR artifacts

        string[] allLines = plain.Split('\n'); // base lines

// wrap to fit page width (your WrapLine helper)
        var wrapped = new List<string>(allLines.Length);
        foreach (var ln in allLines) WrapLine(ln, wrapped, MaxCols);

// paginate using dynamic capacity
        var pages = new List<string[]>();
        for (int i = 0; i < wrapped.Count; i += maxLinesPerPage)
        {
            int cnt = Mathf.Min(maxLinesPerPage, wrapped.Count - i);
            var slice = new string[cnt];
            wrapped.CopyTo(i, slice, 0, cnt);
            pages.Add(slice);
        }

        // 3. Build PDF
        byte[] pdfBytes = BuildPdf(pages);

        // 4. Unique file name using precise timestamp
        string stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss-fff");
        string fileName = $"CrashReport-{stamp}.pdf";

#if UNITY_WEBGL && !UNITY_EDITOR
        DownloadPdfFile(fileName, Convert.ToBase64String(pdfBytes));
#else
        string path = Path.Combine(Application.persistentDataPath, fileName);
        File.WriteAllBytes(path, pdfBytes);
        Debug.Log("Crash report written: " + path);

#if UNITY_EDITOR
        UnityEditor.EditorUtility.RevealInFinder(path);
#else
        Application.OpenURL($"file://{path}");
#endif
#endif
    }

    /* ------------------------------------------------------------ */
    private static byte[] BuildPdf(List<string[]> pages)
    {
        var pdf = new StringBuilder(4096);
        var offs = new List<int>();

        void AddObj(string body)
        {
            offs.Add(pdf.Length);
            pdf.Append(body);
        }

        // — Catalog —
        AddObj("1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");

        // Placeholder for Pages.
        int pagesOffset = pdf.Length;
        const string pagesStub = "2 0 obj\n<< /Type /Pages /Count 0 /Kids[] >>\nendobj\n";
        AddObj(pagesStub);

        int fontId = 3 + pages.Count * 2; // after all pages & streams

        // — Pages & Streams —
        for (int i = 0; i < pages.Count; i++)
        {
            int pageId = 3 + i * 2;
            int streamId = pageId + 1;

            AddObj($"{pageId} 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox[0 0 {PAGE_W} {PAGE_H}] " +
                   $"/Resources<< /Font<< /F1 {fontId} 0 R >> >> /Contents {streamId} 0 R >>\nendobj\n");

            // Build stream for this page
            var sb = new StringBuilder(1024);
            sb.Append("BT /F1 10 Tf 40 ").Append(PAGE_H - 40).Append(" Td 12 TL\n");
            foreach (var line in pages[i])
            {
                sb.Append('(').Append(Escape(line)).Append(") Tj\n0 -12 Td\n");
            }

            sb.Append("ET");
            string stream = sb.ToString();

            AddObj($"{streamId} 0 obj\n<< /Length {stream.Length} >>\nstream\n{stream}\nendstream\nendobj\n");
        }

        // — Font —
        AddObj($"{fontId} 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Courier >>\nendobj\n");

        // — Replace stub Pages with real one —
        var kidsList = new StringBuilder();
        for (int i = 0; i < pages.Count; i++)
            kidsList.Append(' ').Append(3 + i * 2).Append(" 0 R");
        string pagesObj = $"2 0 obj\n<< /Type /Pages /Count {pages.Count} /Kids[{kidsList}] >>\nendobj\n";
        pdf.Remove(pagesOffset, pagesStub.Length).Insert(pagesOffset, pagesObj);

        // — Xref & trailer —
        int xref = pdf.Length;
        pdf.Append("xref\n0 ").Append(offs.Count + 1).Append("\n")
            .Append("0000000000 65535 f \n");
        foreach (int o in offs) pdf.Append(o.ToString("D10")).Append(" 00000 n \n");

        pdf.Append("trailer\n<< /Size ").Append(offs.Count + 1).Append(" /Root 1 0 R >>\n")
            .Append("startxref\n").Append(xref).Append("\n%%EOF");

        return Encoding.ASCII.GetBytes("%PDF-1.4\n" + pdf);
    }

    private static string Escape(string s) =>
        s.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)")
            .Replace("\r", "").Replace("\n", "");
}