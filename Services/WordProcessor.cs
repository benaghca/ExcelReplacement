using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml.Packaging;
using ExcelReplacement.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ExcelReplacement.Services
{
    public class WordProcessor
    {
        private readonly MainDocumentPart _mainDocumentPart;
        private readonly ExcelRecord _record;
        private readonly Dictionary<string, string> _replacements;

        public WordProcessor(MainDocumentPart mainDocumentPart, ExcelRecord record)
        {
            _mainDocumentPart = mainDocumentPart;
            _record = record;
            _replacements = record.GetReplacements();
        }

        public void ProcessDocument()
        {
            var body = _mainDocumentPart.Document.Body;
            if (body == null) return;

            // Process all paragraphs in the body, including those in tables
            ProcessElementForParagraphs(body);

            // Process headers and footers
            foreach (var headerPart in _mainDocumentPart.HeaderParts)
            {
                if (headerPart.Header != null)
                    ProcessElementForParagraphs(headerPart.Header);
            }

            foreach (var footerPart in _mainDocumentPart.FooterParts)
            {
                if (footerPart.Footer != null)
                    ProcessElementForParagraphs(footerPart.Footer);
            }
        }

        // Recursively process all paragraphs in the given element (body, table, header, footer, etc.)
        private void ProcessElementForParagraphs(OpenXmlElement element)
        {
            foreach (var paragraph in element.Descendants<Paragraph>())
            {
                ProcessParagraphRuns(paragraph);
            }
        }

        // Handles placeholders that may span multiple runs and preserves formatting
        private void ProcessParagraphRuns(Paragraph paragraph)
        {
            var runs = paragraph.Elements<Run>().ToList();
            if (runs.Count == 0) return;

            // Gather all run texts and their references
            var runTexts = runs.Select(r => r.GetFirstChild<Text>()?.Text ?? "").ToList();
            var fullText = string.Concat(runTexts);
            if (string.IsNullOrEmpty(fullText)) return;

            // Find all placeholder matches in the concatenated text
            var matches = _replacements.Keys
                .SelectMany(placeholder => Regex.Matches(fullText, Regex.Escape(placeholder)).Cast<Match>())
                .OrderBy(m => m.Index)
                .ToList();

            if (matches.Count == 0) return;

            // Build a new list of runs, preserving formatting except for replaced placeholders
            var newRuns = new List<Run>();
            int textPos = 0; // Position in fullText
            int runIdx = 0;  // Index in runs
            int runCharIdx = 0; // Char index in current run

            foreach (var match in matches)
            {
                // Add all text before the match, preserving original runs
                while (textPos < match.Index)
                {
                    var run = runs[runIdx];
                    var runText = runTexts[runIdx];
                    int charsLeftInRun = runText.Length - runCharIdx;
                    int charsToCopy = Math.Min(charsLeftInRun, match.Index - textPos);
                    if (charsToCopy > 0)
                    {
                        var textToCopy = runText.Substring(runCharIdx, charsToCopy);
                        var newRun = new Run();
                        if (run.RunProperties != null)
                            newRun.RunProperties = run.RunProperties.CloneNode(true) as RunProperties;
                        newRun.AppendChild(new Text(textToCopy) { Space = SpaceProcessingModeValues.Preserve });
                        newRuns.Add(newRun);
                        runCharIdx += charsToCopy;
                        textPos += charsToCopy;
                    }
                    if (runCharIdx >= runText.Length)
                    {
                        runIdx++;
                        runCharIdx = 0;
                    }
                }
                // Add the replacement (use formatting from the first run in the match)
                var placeholder = match.Value;
                var replacement = _replacements[placeholder];
                var replacementRun = new Run();
                if (runs[runIdx].RunProperties != null)
                    replacementRun.RunProperties = runs[runIdx].RunProperties.CloneNode(true) as RunProperties;
                replacementRun.AppendChild(new Text(replacement) { Space = SpaceProcessingModeValues.Preserve });
                newRuns.Add(replacementRun);
                textPos += placeholder.Length;
                // Advance runIdx/runCharIdx to after the placeholder
                int charsToAdvance = placeholder.Length;
                while (charsToAdvance > 0 && runIdx < runs.Count)
                {
                    var runText = runTexts[runIdx];
                    int charsLeftInRun = runText.Length - runCharIdx;
                    if (charsToAdvance < charsLeftInRun)
                    {
                        runCharIdx += charsToAdvance;
                        charsToAdvance = 0;
                    }
                    else
                    {
                        charsToAdvance -= charsLeftInRun;
                        runIdx++;
                        runCharIdx = 0;
                    }
                }
            }
            // Add any remaining text after the last match
            while (runIdx < runs.Count)
            {
                var run = runs[runIdx];
                var runText = runTexts[runIdx];
                if (runCharIdx < runText.Length)
                {
                    var textToCopy = runText.Substring(runCharIdx);
                    var newRun = new Run();
                    if (run.RunProperties != null)
                        newRun.RunProperties = run.RunProperties.CloneNode(true) as RunProperties;
                    newRun.AppendChild(new Text(textToCopy) { Space = SpaceProcessingModeValues.Preserve });
                    newRuns.Add(newRun);
                }
                runIdx++;
                runCharIdx = 0;
            }
            // Replace paragraph runs
            paragraph.RemoveAllChildren<Run>();
            foreach (var run in newRuns)
                paragraph.AppendChild(run);
        }
    }
} 