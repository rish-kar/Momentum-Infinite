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

/// <summary>
/// Script responsible for publishing the results and writing it to a PDF.
/// Depending upon the platform, either the PDF is downloadable (WebGL Build) or in case of a
/// standalone build (PDF is stored in 'C:\Users\<username>\AppData\LocalLow\DefaultCompany\Momentum' location
/// This file has been built using the PDF Document Format by Adobe Open Source: https://opensource.adobe.com/dc-acrobat-sdk-docs/standards/pdfstandards/pdf/PDF32000_2008.pdf?from=20423&from_column=20423
/// </summary>
public class PdfExporter : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _reportData;

    [SerializeField] private int _linesAllowedPerPage = 45;

    // Default A4 size page
    private const float PAGE_W = 595f;
    private const float PAGE_H = 842f;
    const float TopMargin = 40f, BottomMargin = 40f, LineH = 12f;
    int maxLinesPerPage = Mathf.FloorToInt((PAGE_H - TopMargin - BottomMargin) / LineH);

    // Cross-platform Check
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void DownloadPdfFile(string name, string dataBase64);
#endif

    const int MaxCols = 85; // To prevent the lines from going out of the page

    /// <summary>
    /// WrapLine function is responsible for adding a next line incase the characters go beyond the page limit margin specified.
    /// </summary>
    /// <param name="line">Line</param>
    /// <param name="outLines">List of outlines</param>
    /// <param name="width">Width Amount</param>
    static void WrapLine(string line, List<string> outLines, int width)
    {
        if (string.IsNullOrEmpty(line))
        {
            outLines.Add("");
            return;
        }

        // Processes the line characters one by one
        int i = 0;
        while (i < line.Length)
        {
            int remainingLength =
                Mathf.Min(width, line.Length - i); // Either width limit or remaining length (whichever is smaller)
            int segmentEnd = i + remainingLength; // End of the current segment

            // Go back to the last space if cutting mid-word and wrap it before the that word ends
            if (segmentEnd < line.Length && !char.IsWhiteSpace(line[segmentEnd]))
            {
                int space = line.LastIndexOf(' ', segmentEnd - 1, remainingLength);
                if (space > i) remainingLength = space - i; // wrap at last space
            }

            outLines.Add(line.Substring(i, remainingLength));
            i += remainingLength;

            while (i < line.Length && line[i] == ' ') i++;
        }
    }


    /// <summary>
    /// Starts when the game starts.
    /// </summary>
    private void Awake() // auto-hook if designer forgot
    {
        if (TryGetComponent(out Button downloadButton) &&
            downloadButton.onClick.GetPersistentEventCount() == 0)
        {
            downloadButton.onClick.AddListener(Download);
        }
    }

    /// <summary>
    /// Method that is triggered when the button called 'Download' is clicked.
    /// </summary>
    public void Download()
    {
        if (_reportData == null)
        {
            Debug.LogWarning("PdfExporter: <_reportData> reference missing on " + name +
                             ". Problem with exporting PDF until proper references are assigned in Unity inspectoer.");
            return;
        }

        // Convert into plain ASCII by removing tags
        string plain = Regex.Replace(_reportData.text, "<.*?>", string.Empty);
        plain = plain.Replace("•", "*").Replace('─', '-').Replace("\r", "");
        string[] allLines = plain.Split('\n');

        // Once, split and replacement features are done, wrapping the lines takes place
        var wrapped = new List<string>(allLines.Length);
        foreach (var ln in allLines) WrapLine(ln, wrapped, MaxCols);

        // Pagination Segment
        var pages = new List<string[]>();
        for (int i = 0; i < wrapped.Count; i += maxLinesPerPage) // Loops only uptil the maximum lines per page allowed
        {
            int continuity = Mathf.Min(maxLinesPerPage, wrapped.Count - i);
            var slice = new string[continuity];
            wrapped.CopyTo(i, slice, 0, continuity); // Copies array segments
            pages.Add(slice);
        }

        // Trigger the PDF building function
        byte[] pdfBytes = BuildPdf(pages);

        // File named stored as CrashReport with Timestamp
        string exactTimeStamp = DateTime.Now.ToString("yyyyMMdd-HHmmss-fff");
        string pdfFileName = $"CrashReport-{exactTimeStamp}.pdf";

#if UNITY_WEBGL && !UNITY_EDITOR
        DownloadPdfFile(pdfFileName, Convert.ToBase64String(pdfBytes));
#else
        // Write the details to file
        string path = Path.Combine(Application.persistentDataPath, pdfFileName);
        File.WriteAllBytes(path, pdfBytes);
        Debug.Log("Crash report written at: " + path);

#if UNITY_EDITOR
        UnityEditor.EditorUtility.RevealInFinder(path);
#else
        Application.OpenURL($"file://{path}");
#endif
#endif
    }

    /// <summary>
    /// Builds PDF from scratch.
    /// </summary>
    /// <param name="pages">Details continaing PDF data</param>
    /// <returns>byte of array to be written in a file</returns>
    private static byte[] BuildPdf(List<string[]> pages)
    {
        var pdfDocument = new StringBuilder(4096);
        var offs = new List<int>();

        void AddObj(string body)
        {
            offs.Add(pdfDocument.Length);
            pdfDocument.Append(body);
        }

        AddObj("1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n"); // Root Object to reference the pages, document reference 7.3.10

        int pagesOffset = pdfDocument.Length;
        const string pagesStub = "2 0 obj\n<< /Type /Pages /Count 0 /Kids[] >>\nendobj\n"; // Pages Object
        AddObj(pagesStub);

        int fontId = 3 + pages.Count * 2; // Font Object Id: Starts after pages and content streams

        for (int i = 0; i < pages.Count; i++)
        {
            int pageId = 3 + i * 2;
            int streamId = pageId + 1;

            // Page with A4 dimensions, font reference and content stream with IDs assigned above
            AddObj($"{pageId} 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox[0 0 {PAGE_W} {PAGE_H}] " +
                   $"/Resources<< /Font<< /F1 {fontId} 0 R >> >> /Contents {streamId} 0 R >>\nendobj\n");

            // Build stream for this page
            var stringBuilder = new StringBuilder(1024);
            stringBuilder.Append("BT /F1 10 Tf 40 ").Append(PAGE_H - 40)
                .Append(" Td 12 TL\n"); // BT: Begins text, F1: Font, 10 Tf: Size, Td 12: Line spacing


            foreach (var line in pages[i])
            {
                stringBuilder.Append('(').Append(Escape(line))
                    .Append(") Tj\n0 -12 Td\n"); // Tj: Text, 0 to 12 means moving down 12 points
            }

            stringBuilder.Append("ET"); // ET: End text block
            string stream = stringBuilder.ToString();

            AddObj($"{streamId} 0 obj\n<< /Length {stream.Length} >>\nstream\n{stream}\nendstream\nendobj\n");
        }


        AddObj($"{fontId} 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Courier >>\nendobj\n");

        // Build the list of page references and replaces the placeholders with actual page
        var newList = new StringBuilder();
        for (int i = 0; i < pages.Count; i++)
            newList.Append(' ').Append(3 + i * 2).Append(" 0 R");
        string pagesObject = $"2 0 obj\n<< /Type /Pages /Count {pages.Count} /Kids[{newList}] >>\nendobj\n";
        pdfDocument.Remove(pagesOffset, pagesStub.Length).Insert(pagesOffset, pagesObject);

        // Body is always placed before xref, reference 7.5.1 of the reference document mentioned in the summary
        int xReference = pdfDocument.Length;
        pdfDocument.Append("xref\n0 ").Append(offs.Count + 1).Append("\n") // Reference to section 7.5.4 in the document as cross- reference table
            .Append("0000000000 65535 f \n"); // Free object entry with 0000000000 65535 f

        foreach (int o in offs) pdfDocument.Append(o.ToString("D10")).Append(" 00000 n \n");

        pdfDocument.Append("trailer\n<< /Size ").Append(offs.Count + 1).Append(" /Root 1 0 R >>\n") // Trailer reference to 7.5.5 of the reference document
            .Append("startxref\n").Append(xReference) // Startxref reference to 7.5.5 of the reference document
            .Append("\n%%EOF"); // startxref: where reference begins, EOF: End of File

        return
            Encoding.ASCII.GetBytes("%PDF-1.4\n" +
                                    pdfDocument); // Adding PDF Header and encoding the document into byte array format, mentioned in section 7.5.2 of the document mentioned in summary of this class 
    }

    /// <summary>
    /// Escapes the special characters, remove certain line breaks
    /// </summary>
    /// <param name="stringValue">Value of the string to trigger replacement of escape sequences</param>
    /// <returns></returns>
    private static string Escape(string stringValue) =>
        stringValue.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)")
            .Replace("\r", "").Replace("\n", "");
}