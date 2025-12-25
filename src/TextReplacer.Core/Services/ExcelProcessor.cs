using System.Text;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using TextReplacer.Core.Models;

namespace TextReplacer.Core.Services;

/// <summary>
/// Processes Excel documents (.xlsx) for placeholder extraction and replacement.
/// Handles shared string table by converting to inline strings to avoid side effects.
/// Preserves rich text formatting (bold, italic, etc.) for both replaced and non-replaced text.
/// </summary>
public class ExcelProcessor : IDocumentProcessor
{
    public IReadOnlySet<string> ExtractPlaceholders(string templatePath, PlaceholderConfig config)
    {
        var placeholders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var pattern = config.BuildPattern();

        using var doc = SpreadsheetDocument.Open(templatePath, false);
        var workbookPart = doc.WorkbookPart;
        if (workbookPart == null) return placeholders;

        var sharedStringTable = workbookPart.SharedStringTablePart?.SharedStringTable;

        foreach (var worksheetPart in workbookPart.WorksheetParts)
        {
            ExtractFromWorksheet(worksheetPart, sharedStringTable, pattern, placeholders);
        }

        foreach (var chartPart in workbookPart.GetPartsOfType<ChartPart>())
        {
            ExtractFromChartPart(chartPart, pattern, placeholders);
        }

        return placeholders;
    }

    public void Process(string templatePath, string outputPath, IDictionary<string, string> replacements, PlaceholderConfig config)
    {
        File.Copy(templatePath, outputPath, overwrite: true);

        using var doc = SpreadsheetDocument.Open(outputPath, true);
        var workbookPart = doc.WorkbookPart;
        if (workbookPart == null) return;

        var pattern = config.BuildPattern();
        var sharedStringTable = workbookPart.SharedStringTablePart?.SharedStringTable;

        foreach (var worksheetPart in workbookPart.WorksheetParts)
        {
            ProcessWorksheet(worksheetPart, sharedStringTable, pattern, replacements);
            worksheetPart.Worksheet.Save();
        }

        foreach (var worksheetPart in workbookPart.WorksheetParts)
        {
            foreach (var drawingsPart in worksheetPart.GetPartsOfType<DrawingsPart>())
            {
                foreach (var chartPart in drawingsPart.GetPartsOfType<ChartPart>())
                {
                    ProcessChartPart(chartPart, pattern, replacements);
                }
            }
        }
    }

    #region Data Structures

    /// <summary>
    /// Represents a text run with its formatting properties.
    /// </summary>
    private record RichTextRun(string Text, RunProperties? Properties);

    /// <summary>
    /// Maps a character position to its source run and formatting.
    /// </summary>
    private record CharacterInfo(int RunIndex, int CharIndex, char Character, RunProperties? Properties);

    #endregion

    #region Extraction

    private void ExtractFromWorksheet(WorksheetPart worksheetPart, SharedStringTable? sharedStringTable, Regex pattern, HashSet<string> placeholders)
    {
        var worksheet = worksheetPart.Worksheet;
        if (worksheet == null) return;

        var sheetData = worksheet.Elements<SheetData>().FirstOrDefault();
        if (sheetData == null) return;

        foreach (var row in sheetData.Elements<Row>())
        {
            foreach (var cell in row.Elements<Cell>())
            {
                var text = GetCellText(cell, sharedStringTable);
                foreach (Match match in pattern.Matches(text))
                {
                    placeholders.Add(match.Groups[1].Value);
                }
            }
        }

        var headerFooter = worksheet.Elements<HeaderFooter>().FirstOrDefault();
        if (headerFooter != null)
        {
            ExtractFromHeaderFooter(headerFooter, pattern, placeholders);
        }
    }

    private void ExtractFromHeaderFooter(HeaderFooter headerFooter, Regex pattern, HashSet<string> placeholders)
    {
        var texts = new[]
        {
            headerFooter.OddHeader?.Text, headerFooter.OddFooter?.Text,
            headerFooter.EvenHeader?.Text, headerFooter.EvenFooter?.Text,
            headerFooter.FirstHeader?.Text, headerFooter.FirstFooter?.Text
        };

        foreach (var text in texts.Where(t => !string.IsNullOrEmpty(t)))
        {
            foreach (Match match in pattern.Matches(text!))
            {
                placeholders.Add(match.Groups[1].Value);
            }
        }
    }

    private void ExtractFromChartPart(ChartPart chartPart, Regex pattern, HashSet<string> placeholders)
    {
        var chartXml = chartPart.ChartSpace?.OuterXml ?? string.Empty;
        foreach (Match match in pattern.Matches(chartXml))
        {
            placeholders.Add(match.Groups[1].Value);
        }
    }

    private string GetCellText(Cell cell, SharedStringTable? sharedStringTable)
    {
        if (cell.DataType?.Value == CellValues.SharedString && cell.CellValue != null)
        {
            if (int.TryParse(cell.CellValue.Text, out int index))
            {
                var item = sharedStringTable?.Elements<SharedStringItem>().ElementAtOrDefault(index);
                return item != null ? GetSharedStringItemText(item) : string.Empty;
            }
        }
        else if (cell.DataType?.Value == CellValues.InlineString)
        {
            return GetInlineStringText(cell.InlineString);
        }
        else if (cell.CellValue != null)
        {
            return cell.CellValue.Text ?? string.Empty;
        }

        return string.Empty;
    }

    private string GetSharedStringItemText(SharedStringItem item)
    {
        if (item.Text != null) return item.Text.Text ?? string.Empty;

        var sb = new StringBuilder();
        foreach (var run in item.Elements<Run>())
        {
            sb.Append(run.Text?.Text ?? string.Empty);
        }
        return sb.ToString();
    }

    private string GetInlineStringText(InlineString? inlineString)
    {
        if (inlineString == null) return string.Empty;
        if (inlineString.Text != null) return inlineString.Text.Text ?? string.Empty;

        var sb = new StringBuilder();
        foreach (var run in inlineString.Elements<Run>())
        {
            sb.Append(run.Text?.Text ?? string.Empty);
        }
        return sb.ToString();
    }

    #endregion

    #region Processing

    private void ProcessWorksheet(WorksheetPart worksheetPart, SharedStringTable? sharedStringTable, Regex pattern, IDictionary<string, string> replacements)
    {
        var worksheet = worksheetPart.Worksheet;
        if (worksheet == null) return;

        var sheetData = worksheet.Elements<SheetData>().FirstOrDefault();
        if (sheetData == null) return;

        foreach (var row in sheetData.Elements<Row>())
        {
            foreach (var cell in row.Elements<Cell>())
            {
                ProcessCell(cell, sharedStringTable, pattern, replacements);
            }
        }

        var headerFooter = worksheet.Elements<HeaderFooter>().FirstOrDefault();
        if (headerFooter != null)
        {
            ProcessHeaderFooter(headerFooter, pattern, replacements);
        }
    }

    private void ProcessCell(Cell cell, SharedStringTable? sharedStringTable, Regex pattern, IDictionary<string, string> replacements)
    {
        var fullText = GetCellText(cell, sharedStringTable);
        if (string.IsNullOrEmpty(fullText) || !pattern.IsMatch(fullText))
            return;

        // Extract original runs with formatting
        var originalRuns = GetOriginalRuns(cell, sharedStringTable);

        // Process runs with placeholder replacement while preserving formatting
        var newRuns = ProcessRunsWithReplacement(originalRuns, pattern, replacements);

        // Build new InlineString with preserved formatting
        var inlineString = BuildInlineString(newRuns);

        // Update cell
        cell.DataType = CellValues.InlineString;
        cell.CellValue = null;
        cell.RemoveAllChildren<InlineString>();
        cell.AppendChild(inlineString);
    }

    private List<RichTextRun> GetOriginalRuns(Cell cell, SharedStringTable? sharedStringTable)
    {
        if (cell.DataType?.Value == CellValues.SharedString && cell.CellValue != null)
        {
            if (int.TryParse(cell.CellValue.Text, out int index))
            {
                var item = sharedStringTable?.Elements<SharedStringItem>().ElementAtOrDefault(index);
                return ExtractRunsFromSharedString(item);
            }
        }
        else if (cell.DataType?.Value == CellValues.InlineString)
        {
            return ExtractRunsFromInlineString(cell.InlineString);
        }

        // Plain value - no formatting
        var text = cell.CellValue?.Text ?? string.Empty;
        return new List<RichTextRun> { new(text, null) };
    }

    private List<RichTextRun> ExtractRunsFromSharedString(SharedStringItem? item)
    {
        var runs = new List<RichTextRun>();
        if (item == null) return runs;

        // Simple text without runs
        if (item.Text != null)
        {
            runs.Add(new RichTextRun(item.Text.Text ?? string.Empty, null));
            return runs;
        }

        // Rich text with runs
        foreach (var run in item.Elements<Run>())
        {
            var text = run.Text?.Text ?? string.Empty;
            var props = run.RunProperties?.CloneNode(true) as RunProperties;
            runs.Add(new RichTextRun(text, props));
        }

        return runs;
    }

    private List<RichTextRun> ExtractRunsFromInlineString(InlineString? inlineString)
    {
        var runs = new List<RichTextRun>();
        if (inlineString == null) return runs;

        if (inlineString.Text != null)
        {
            runs.Add(new RichTextRun(inlineString.Text.Text ?? string.Empty, null));
            return runs;
        }

        foreach (var run in inlineString.Elements<Run>())
        {
            var text = run.Text?.Text ?? string.Empty;
            var props = run.RunProperties?.CloneNode(true) as RunProperties;
            runs.Add(new RichTextRun(text, props));
        }

        return runs;
    }

    /// <summary>
    /// Processes text runs, replacing placeholders while preserving formatting.
    /// Uses a character-by-character mapping to handle placeholders that may span multiple runs.
    /// </summary>
    private List<RichTextRun> ProcessRunsWithReplacement(List<RichTextRun> originalRuns, Regex pattern, IDictionary<string, string> replacements)
    {
        // Build character map with position and formatting info
        var charMap = new List<CharacterInfo>();
        for (int r = 0; r < originalRuns.Count; r++)
        {
            var run = originalRuns[r];
            for (int c = 0; c < run.Text.Length; c++)
            {
                charMap.Add(new CharacterInfo(r, c, run.Text[c], run.Properties));
            }
        }

        if (charMap.Count == 0)
            return originalRuns;

        var fullText = new string(charMap.Select(c => c.Character).ToArray());
        var matches = pattern.Matches(fullText).Cast<Match>().ToList();

        if (matches.Count == 0)
            return originalRuns;

        // Build result by walking through the text
        var result = new List<RichTextRun>();
        int currentPos = 0;

        foreach (var match in matches)
        {
            // Add text before this match (preserving original formatting per character)
            if (match.Index > currentPos)
            {
                AddPreservingFormatting(result, charMap, currentPos, match.Index - 1);
            }

            var placeholderName = match.Groups[1].Value;

            // Check if we have a replacement value
            if (replacements.TryGetValue(placeholderName, out var value))
            {
                // Add replacement text with formatting from the first character of the placeholder
                if (!string.IsNullOrEmpty(value))
                {
                    var startProps = charMap[match.Index].Properties;
                    result.Add(new RichTextRun(value, CloneProperties(startProps)));
                }
                currentPos = match.Index + match.Length;
            }
            else
            {
                // No matching column - leave placeholder unchanged (preserving its formatting)
                AddPreservingFormatting(result, charMap, match.Index, match.Index + match.Length - 1);
                currentPos = match.Index + match.Length;
            }
        }

        // Add any remaining text after the last match
        if (currentPos < charMap.Count)
        {
            AddPreservingFormatting(result, charMap, currentPos, charMap.Count - 1);
        }

        return result;
    }

    /// <summary>
    /// Adds characters from startPos to endPos, grouping consecutive characters with the same formatting.
    /// </summary>
    private void AddPreservingFormatting(List<RichTextRun> result, List<CharacterInfo> charMap, int startPos, int endPos)
    {
        if (startPos > endPos || startPos >= charMap.Count)
            return;

        var currentText = new StringBuilder();
        var currentProps = charMap[startPos].Properties;
        string? currentPropsXml = currentProps?.OuterXml;

        for (int pos = startPos; pos <= endPos && pos < charMap.Count; pos++)
        {
            var info = charMap[pos];
            string? infoPropsXml = info.Properties?.OuterXml;

            // Check if formatting changed
            if (infoPropsXml != currentPropsXml)
            {
                // Flush current segment
                if (currentText.Length > 0)
                {
                    result.Add(new RichTextRun(currentText.ToString(), CloneProperties(currentProps)));
                    currentText.Clear();
                }
                currentProps = info.Properties;
                currentPropsXml = infoPropsXml;
            }

            currentText.Append(info.Character);
        }

        // Flush final segment
        if (currentText.Length > 0)
        {
            result.Add(new RichTextRun(currentText.ToString(), CloneProperties(currentProps)));
        }
    }

    private RunProperties? CloneProperties(RunProperties? props)
    {
        return props?.CloneNode(true) as RunProperties;
    }

    private InlineString BuildInlineString(List<RichTextRun> runs)
    {
        var inlineString = new InlineString();

        // Optimize: if single run with no formatting, use simple text
        if (runs.Count == 1 && runs[0].Properties == null)
        {
            var text = new Text(runs[0].Text);
            if (runs[0].Text.StartsWith(' ') || runs[0].Text.EndsWith(' '))
            {
                text.Space = SpaceProcessingModeValues.Preserve;
            }
            inlineString.AppendChild(text);
            return inlineString;
        }

        // Multiple runs or runs with formatting - use Run elements
        foreach (var richRun in runs)
        {
            if (string.IsNullOrEmpty(richRun.Text))
                continue;

            var run = new Run();

            if (richRun.Properties != null)
            {
                run.AppendChild(richRun.Properties.CloneNode(true));
            }

            var text = new Text(richRun.Text);
            if (richRun.Text.StartsWith(' ') || richRun.Text.EndsWith(' '))
            {
                text.Space = SpaceProcessingModeValues.Preserve;
            }
            run.AppendChild(text);

            inlineString.AppendChild(run);
        }

        return inlineString;
    }

    private void ProcessHeaderFooter(HeaderFooter headerFooter, Regex pattern, IDictionary<string, string> replacements)
    {
        if (headerFooter.OddHeader != null)
            headerFooter.OddHeader.Text = ReplaceAllPlaceholders(headerFooter.OddHeader.Text ?? "", pattern, replacements);
        if (headerFooter.OddFooter != null)
            headerFooter.OddFooter.Text = ReplaceAllPlaceholders(headerFooter.OddFooter.Text ?? "", pattern, replacements);
        if (headerFooter.EvenHeader != null)
            headerFooter.EvenHeader.Text = ReplaceAllPlaceholders(headerFooter.EvenHeader.Text ?? "", pattern, replacements);
        if (headerFooter.EvenFooter != null)
            headerFooter.EvenFooter.Text = ReplaceAllPlaceholders(headerFooter.EvenFooter.Text ?? "", pattern, replacements);
        if (headerFooter.FirstHeader != null)
            headerFooter.FirstHeader.Text = ReplaceAllPlaceholders(headerFooter.FirstHeader.Text ?? "", pattern, replacements);
        if (headerFooter.FirstFooter != null)
            headerFooter.FirstFooter.Text = ReplaceAllPlaceholders(headerFooter.FirstFooter.Text ?? "", pattern, replacements);
    }

    private void ProcessChartPart(ChartPart chartPart, Regex pattern, IDictionary<string, string> replacements)
    {
        var chartSpace = chartPart.ChartSpace;
        if (chartSpace == null) return;

        foreach (var textElement in chartSpace.Descendants<DocumentFormat.OpenXml.Drawing.Charts.StringPoint>())
        {
            if (textElement.NumericValue != null)
            {
                var text = textElement.NumericValue.Text ?? "";
                if (pattern.IsMatch(text))
                {
                    textElement.NumericValue.Text = ReplaceAllPlaceholders(text, pattern, replacements);
                }
            }
        }

        foreach (var textElement in chartSpace.Descendants<DocumentFormat.OpenXml.Drawing.Text>())
        {
            var text = textElement.Text ?? "";
            if (pattern.IsMatch(text))
            {
                textElement.Text = ReplaceAllPlaceholders(text, pattern, replacements);
            }
        }
    }

    private string ReplaceAllPlaceholders(string text, Regex pattern, IDictionary<string, string> replacements)
    {
        return pattern.Replace(text, match =>
        {
            var placeholderName = match.Groups[1].Value;
            // Leave placeholder unchanged if no matching column in CSV
            return replacements.TryGetValue(placeholderName, out var value) ? value : match.Value;
        });
    }

    #endregion
}
