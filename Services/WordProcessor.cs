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
        private readonly Regex _placeholderRegex = new(@"\[([^\]]+)\]", RegexOptions.Compiled);

        public WordProcessor(MainDocumentPart mainDocumentPart, ExcelRecord record)
        {
            _mainDocumentPart = mainDocumentPart;
            _record = record;
            _replacements = record.Values ?? new Dictionary<string, string>();
        }

        public void ProcessDocument()
        {
            var body = _mainDocumentPart?.Document?.Body;
            if (body == null) return;

            ProcessElementForParagraphs(body);

            if (_mainDocumentPart.HeaderParts != null)
            {
                foreach (var headerPart in _mainDocumentPart.HeaderParts.Where(h => h?.Header != null))
                {
                    ProcessElementForParagraphs(headerPart.Header!);
                }
            }

            if (_mainDocumentPart.FooterParts != null)
            {
                foreach (var footerPart in _mainDocumentPart.FooterParts.Where(f => f?.Footer != null))
                {
                    ProcessElementForParagraphs(footerPart.Footer!);
                }
            }
        }

        private void ProcessElementForParagraphs(OpenXmlElement element)
        {
            foreach (var paragraph in element.Descendants<Paragraph>().Where(p => p != null))
            {
                ProcessParagraphRuns(paragraph);
            }

            foreach (var table in element.Descendants<Table>().Where(t => t != null))
            {
                foreach(var row in table.Descendants<TableRow>().Where(r => r != null))
                {
                    foreach(var cell in row.Descendants<TableCell>().Where(c => c != null))
                    {
                        ProcessElementForParagraphs(cell);
                    }
                }
            }
        }

        private void ProcessParagraphRuns(Paragraph paragraph)
        {
            var runs = paragraph.Elements<Run>().ToList();
            if (runs.Count == 0) return;

            var runTexts = runs.Select(r => r.GetFirstChild<Text>()?.Text ?? "").ToList();
            var fullText = string.Concat(runTexts);
            if (string.IsNullOrEmpty(fullText)) return;

            var matches = _placeholderRegex.Matches(fullText);

            if (matches.Count == 0) return;

            var newRuns = new List<Run>();
            int textPos = 0;
            int runIdx = 0;
            int runCharIdx = 0;

            foreach (Match match in matches)
            {
                while (textPos < match.Index)
                {
                    if (runIdx >= runs.Count) break;

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

                if (runIdx >= runs.Count) break;

                var placeholder = match.Value;
                string replacement = _replacements.TryGetValue(placeholder, out var val) ? (val ?? "") : placeholder;
                
                var replacementRun = new Run();
                if (runs[runIdx].RunProperties != null)
                    replacementRun.RunProperties = runs[runIdx].RunProperties.CloneNode(true) as RunProperties;
                replacementRun.AppendChild(new Text(replacement) { Space = SpaceProcessingModeValues.Preserve });
                newRuns.Add(replacementRun);
                textPos += placeholder.Length;

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

            paragraph.RemoveAllChildren<Run>();
            foreach (var run in newRuns)
                paragraph.AppendChild(run);
        }
    }
} 