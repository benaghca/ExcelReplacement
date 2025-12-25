using System.Text;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using TextReplacer.Core.Models;

namespace TextReplacer.Core.Services;

/// <summary>
/// Processes Word documents (.docx) for placeholder extraction and replacement.
/// Handles placeholders that may be split across multiple runs.
/// </summary>
public class WordProcessor : IDocumentProcessor
{
    public IReadOnlySet<string> ExtractPlaceholders(string templatePath, PlaceholderConfig config)
    {
        var placeholders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var pattern = config.BuildPattern();

        using var doc = WordprocessingDocument.Open(templatePath, false);
        var mainPart = doc.MainDocumentPart;
        if (mainPart == null) return placeholders;

        // Extract from main body
        if (mainPart.Document?.Body != null)
        {
            ExtractFromElement(mainPart.Document.Body, pattern, placeholders);
        }

        // Extract from headers
        foreach (var headerPart in mainPart.HeaderParts)
        {
            ExtractFromElement(headerPart.Header, pattern, placeholders);
        }

        // Extract from footers
        foreach (var footerPart in mainPart.FooterParts)
        {
            ExtractFromElement(footerPart.Footer, pattern, placeholders);
        }

        return placeholders;
    }

    public void Process(string templatePath, string outputPath, IDictionary<string, string> replacements, PlaceholderConfig config)
    {
        // Copy template to output location
        File.Copy(templatePath, outputPath, overwrite: true);

        using var doc = WordprocessingDocument.Open(outputPath, true);
        var mainPart = doc.MainDocumentPart;
        if (mainPart == null) return;

        var pattern = config.BuildPattern();

        // Process main body
        if (mainPart.Document?.Body != null)
        {
            ProcessElement(mainPart.Document.Body, pattern, replacements, config);
        }

        // Process headers
        foreach (var headerPart in mainPart.HeaderParts)
        {
            ProcessElement(headerPart.Header, pattern, replacements, config);
        }

        // Process footers
        foreach (var footerPart in mainPart.FooterParts)
        {
            ProcessElement(footerPart.Footer, pattern, replacements, config);
        }

        mainPart.Document?.Save();
    }

    private void ExtractFromElement(OpenXmlElement element, Regex pattern, HashSet<string> placeholders)
    {
        // Get all paragraphs including those in tables
        foreach (var para in element.Descendants<Paragraph>())
        {
            var text = GetParagraphText(para);
            var matches = pattern.Matches(text);
            foreach (Match match in matches)
            {
                placeholders.Add(match.Groups[1].Value);
            }
        }
    }

    private void ProcessElement(OpenXmlElement element, Regex pattern, IDictionary<string, string> replacements, PlaceholderConfig config)
    {
        // Process all paragraphs including those in tables
        foreach (var para in element.Descendants<Paragraph>().ToList())
        {
            ProcessParagraph(para, pattern, replacements, config);
        }
    }

    private string GetParagraphText(Paragraph para)
    {
        var sb = new StringBuilder();
        foreach (var run in para.Elements<Run>())
        {
            foreach (var text in run.Elements<Text>())
            {
                sb.Append(text.Text);
            }
        }
        return sb.ToString();
    }

    /// <summary>
    /// Processes a paragraph, handling placeholders that may span multiple runs.
    /// Uses a "virtual text" approach where we:
    /// 1. Build a combined string from all runs
    /// 2. Map character positions back to specific runs
    /// 3. Find placeholders in the combined text
    /// 4. Modify the appropriate runs
    /// </summary>
    private void ProcessParagraph(Paragraph para, Regex pattern, IDictionary<string, string> replacements, PlaceholderConfig config)
    {
        var runs = para.Elements<Run>().ToList();
        if (runs.Count == 0) return;

        // Build position map: each character position maps to (runIndex, textElementIndex, charIndex)
        var positionMap = new List<(int RunIndex, int TextIndex, int CharIndex)>();
        var virtualText = new StringBuilder();

        for (int r = 0; r < runs.Count; r++)
        {
            var textElements = runs[r].Elements<Text>().ToList();
            for (int t = 0; t < textElements.Count; t++)
            {
                var text = textElements[t].Text ?? string.Empty;
                for (int c = 0; c < text.Length; c++)
                {
                    positionMap.Add((r, t, c));
                    virtualText.Append(text[c]);
                }
            }
        }

        var fullText = virtualText.ToString();
        var matches = pattern.Matches(fullText).Cast<Match>().ToList();
        if (matches.Count == 0) return;

        // Process matches in reverse order to preserve positions
        for (int m = matches.Count - 1; m >= 0; m--)
        {
            var match = matches[m];
            var placeholderName = match.Groups[1].Value;

            // Skip if no matching column - leave placeholder unchanged
            if (!replacements.TryGetValue(placeholderName, out var replacement))
                continue;

            replacement ??= string.Empty;

            // Find which runs contain this match
            var startPos = match.Index;
            var endPos = match.Index + match.Length - 1;

            if (startPos >= positionMap.Count || endPos >= positionMap.Count)
                continue;

            var startInfo = positionMap[startPos];
            var endInfo = positionMap[endPos];

            // If placeholder is entirely within one run's one text element, simple replace
            if (startInfo.RunIndex == endInfo.RunIndex && startInfo.TextIndex == endInfo.TextIndex)
            {
                var textElement = runs[startInfo.RunIndex].Elements<Text>().ElementAt(startInfo.TextIndex);
                var originalText = textElement.Text ?? string.Empty;
                var before = originalText.Substring(0, startInfo.CharIndex);
                var after = originalText.Substring(endInfo.CharIndex + 1);
                textElement.Text = before + replacement + after;
            }
            else
            {
                // Placeholder spans multiple runs or text elements - more complex handling
                ReplaceAcrossRuns(runs, positionMap, startPos, endPos, replacement);
            }
        }
    }

    /// <summary>
    /// Replaces text that spans multiple runs.
    /// Strategy: Put all replacement text in the first run, clear the rest.
    /// </summary>
    private void ReplaceAcrossRuns(
        List<Run> runs,
        List<(int RunIndex, int TextIndex, int CharIndex)> positionMap,
        int startPos,
        int endPos,
        string replacement)
    {
        var startInfo = positionMap[startPos];
        var endInfo = positionMap[endPos];

        // Collect all affected (run, text) pairs
        var affectedPairs = new HashSet<(int Run, int Text)>();
        for (int pos = startPos; pos <= endPos; pos++)
        {
            affectedPairs.Add((positionMap[pos].RunIndex, positionMap[pos].TextIndex));
        }

        bool isFirst = true;
        foreach (var pair in affectedPairs.OrderBy(p => p.Run).ThenBy(p => p.Text))
        {
            var textElement = runs[pair.Run].Elements<Text>().ElementAtOrDefault(pair.Text);
            if (textElement == null) continue;

            var originalText = textElement.Text ?? string.Empty;

            if (isFirst)
            {
                // First text element: keep text before start, add replacement
                var before = "";
                if (pair.Run == startInfo.RunIndex && pair.Text == startInfo.TextIndex)
                {
                    before = originalText.Substring(0, startInfo.CharIndex);
                }

                var after = "";
                if (pair.Run == endInfo.RunIndex && pair.Text == endInfo.TextIndex)
                {
                    after = originalText.Substring(endInfo.CharIndex + 1);
                }
                else
                {
                    // This is first but not last, so no after text here
                    after = "";
                }

                textElement.Text = before + replacement + after;
                isFirst = false;
            }
            else if (pair.Run == endInfo.RunIndex && pair.Text == endInfo.TextIndex)
            {
                // Last text element (but not first): keep text after end
                var after = originalText.Substring(endInfo.CharIndex + 1);
                textElement.Text = after;
            }
            else
            {
                // Middle text elements: clear completely
                textElement.Text = "";
            }
        }
    }
}
