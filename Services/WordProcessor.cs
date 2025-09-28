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

            // Debug: Log what replacements we have
            Console.WriteLine($"WordProcessor: Processing document with {_replacements.Count} replacements");
            foreach (var kvp in _replacements)
            {
                Console.WriteLine($"  [{kvp.Key}] -> {kvp.Value}");
            }

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

            // Create a mapping for common placeholder variations
            var placeholderMapping = new Dictionary<string, string>
            {
                { "[Panel]", "[Panel-1]" },
                { "[Breaker]", "[Breaker-1]" }
            };

            // Process each run individually to preserve formatting
            foreach (var run in runs)
            {
                var textElement = run.GetFirstChild<Text>();
                if (textElement?.Text == null) continue;

                string originalText = textElement.Text;
                string processedText = originalText;

                // Check if this run contains any placeholders
                var matches = _placeholderRegex.Matches(originalText);
                if (matches.Count == 0) continue;

                Console.WriteLine($"WordProcessor: Processing run with text: '{originalText}'");

                // Process matches in reverse order to maintain indices
                var sortedMatches = matches.Cast<Match>().OrderByDescending(m => m.Index).ToList();
                
                foreach (var match in sortedMatches)
                {
                    var placeholder = match.Value;
                    Console.WriteLine($"  Found placeholder: {placeholder}");
                    
                    // Try to find a replacement using the mapping first
                    string mappedPlaceholder = placeholderMapping.TryGetValue(placeholder, out var mapped) ? mapped : placeholder;
                    
                    // Extract the key from the placeholder (remove brackets)
                    string key = mappedPlaceholder.Trim('[', ']');
                    string replacement = _replacements.TryGetValue(key, out var val) ? (val ?? "") : placeholder;
                    
                    Console.WriteLine($"  Replacing {placeholder} -> {mappedPlaceholder} -> {replacement}");
                    
                    // Replace in the processed text
                    processedText = processedText.Substring(0, match.Index) + replacement + processedText.Substring(match.Index + match.Length);
                }

                Console.WriteLine($"WordProcessor: Final processed text: '{processedText}'");

                // Update the text in this run only
                textElement.Text = processedText;
            }
        }
    }
} 