using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using ExcelReplacement.Models;

namespace ExcelReplacement.Services
{
    public enum ValidationStatus
    {
        Success, // Placeholder found in template and has a matching, non-empty value in CSV
        Warning, // Placeholder found in template but has an empty value in CSV, or CSV column not found in template (old)
        Error,   // Placeholder found in template but no matching column in CSV
        CsvColumnUnused // CSV column exists but no matching placeholder in template
    }

    public class ValidationResult
    {
        public required string Placeholder { get; set; }
        public ValidationStatus Status { get; set; }
        public required string Message { get; set; }
        public required string SampleValue { get; set; }
        public required string Location { get; set; }
    }

    public class TemplateValidator
    {
        private readonly string _templatePath;
        private readonly bool _isExcel;
        private readonly Regex _placeholderRegex = new(@"\[([^\]]+)\]");

        public TemplateValidator(string templatePath, bool isExcel)
        {
            _templatePath = templatePath;
            _isExcel = isExcel;
        }

        public List<ValidationResult> ValidateTemplate(ExcelRecord sampleRecord)
        {
            var results = new List<ValidationResult>();
            var templatePlaceholders = ExtractPlaceholdersFromTemplate();
            var usedCsvColumns = new HashSet<string>();

            // Validate template placeholders against CSV record
            foreach (var placeholder in templatePlaceholders)
            {
                var key = placeholder.Trim('[', ']');
                var sampleValue = sampleRecord.GetValue(key);

                if (sampleRecord.Values.ContainsKey(key))
                {
                    usedCsvColumns.Add(key);
                    if (!string.IsNullOrEmpty(sampleValue))
                    {
                        results.Add(new ValidationResult
                        {
                            Placeholder = placeholder,
                            Status = ValidationStatus.Success,
                            Message = "Placeholder found in CSV with value",
                            SampleValue = sampleValue,
                            Location = "Template"
                        });
                    }
                    else
                    {
                        results.Add(new ValidationResult
                        {
                            Placeholder = placeholder,
                            Status = ValidationStatus.Warning,
                            Message = "Placeholder found in CSV, but value is empty",
                            SampleValue = "(empty)",
                            Location = "Template"
                        });
                    }
                }
                else
                {
                    results.Add(new ValidationResult
                    {
                        Placeholder = placeholder,
                        Status = ValidationStatus.Error,
                        Message = "Placeholder not found as a column in CSV",
                        SampleValue = "N/A",
                        Location = "Template"
                    });
                }
            }

            // Check for unused CSV columns
            foreach (var key in sampleRecord.Values.Keys)
            {
                if (!usedCsvColumns.Contains(key))
                {
                    results.Add(new ValidationResult
                    {
                        Placeholder = $"[{key}]",
                        Status = ValidationStatus.CsvColumnUnused,
                        Message = "This CSV column is not used in the template",
                        SampleValue = sampleRecord.GetValue(key),
                        Location = "CSV"
                    });
                }
            }

            return results.OrderBy(r => r.Status).ThenBy(r => r.Placeholder).ToList();
        }

        private HashSet<string> ExtractPlaceholdersFromTemplate()
        {
            var placeholders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (_isExcel)
            {
                ExtractPlaceholdersFromExcel(placeholders);
            }
            else
            {
                ExtractPlaceholdersFromWord(placeholders);
            }

            return placeholders;
        }

        private void ExtractPlaceholdersFromExcel(HashSet<string> placeholders)
        {
            using (var document = SpreadsheetDocument.Open(_templatePath, false))
            {
                var workbookPart = document.WorkbookPart;
                if (workbookPart == null) return;

                foreach (var sheet in workbookPart.Workbook.Sheets.Cast<Sheet>())
                {
                    if (sheet.Id?.Value == null) continue; // Skip if sheet ID is null
                    var worksheetPart = (WorksheetPart)workbookPart.GetPartById(sheet.Id);
                    var sheetData = worksheetPart.Worksheet.Elements<SheetData>().FirstOrDefault();

                    if (sheetData != null)
                    {
                        foreach (var row in sheetData.Elements<Row>())
                        {
                            foreach (var cell in row.Elements<Cell>())
                            {
                                string cellText = null;
                                if (cell.DataType != null && cell.DataType == CellValues.SharedString && cell.CellValue != null)
                                {
                                     if (int.TryParse(cell.CellValue.Text, out int ssIndex) && workbookPart.SharedStringTablePart?.SharedStringTable != null)
                                    {
                                        var sharedStringItem = workbookPart.SharedStringTablePart.SharedStringTable
                                            .Elements<SharedStringItem>()
                                            .ElementAtOrDefault(ssIndex);
                                        cellText = sharedStringItem?.InnerText;
                                    }
                                }
                                else if (cell.CellValue != null)
                                {
                                    cellText = cell.CellValue.Text;
                                }

                                if (!string.IsNullOrEmpty(cellText))
                                {
                                    foreach (Match match in _placeholderRegex.Matches(cellText))
                                    {
                                        placeholders.Add(match.Value);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private void ExtractPlaceholdersFromWord(HashSet<string> placeholders)
        {
            using (var document = WordprocessingDocument.Open(_templatePath, false))
            {
                var mainPart = document.MainDocumentPart;
                if (mainPart?.Document?.Body == null) return;

                // Process body
                ProcessWordElementForPlaceholders(mainPart.Document.Body, placeholders);

                // Process headers
                foreach (var headerPart in mainPart.HeaderParts)
                {
                    if (headerPart.Header != null)
                    {
                        ProcessWordElementForPlaceholders(headerPart.Header, placeholders);
                    }
                }

                // Process footers
                foreach (var footerPart in mainPart.FooterParts)
                {
                    if (footerPart.Footer != null)
                    {
                        ProcessWordElementForPlaceholders(footerPart.Footer, placeholders);
                    }
                }
            }
        }

        private void ProcessWordElementForPlaceholders(OpenXmlElement element, HashSet<string> placeholders)
        {
            foreach (var paragraph in element.Descendants<Paragraph>())
            {
                var text = string.Join("", paragraph.Descendants<DocumentFormat.OpenXml.Wordprocessing.Text>().Select(t => t.Text));
                 foreach (Match match in _placeholderRegex.Matches(text))
                {
                    placeholders.Add(match.Value);
                }
            }
             // Also check in Tables, Headers, Footers, etc. if needed
             foreach (var table in element.Descendants<DocumentFormat.OpenXml.Wordprocessing.Table>())
             {
                 foreach(var cell in table.Descendants<DocumentFormat.OpenXml.Wordprocessing.TableCell>())
                 {
                      var cellText = string.Join("", cell.Descendants<DocumentFormat.OpenXml.Wordprocessing.Text>().Select(t => t.Text));
                       foreach (Match match in _placeholderRegex.Matches(cellText))
                        {
                            placeholders.Add(match.Value);
                        }
                 }
             }
        }

        // Old validation methods (no longer needed)
        // private void ValidateCellContent(SharedStringItem sharedStringItem, ExcelRecord sampleRecord, List<ValidationResult> results, HashSet<string> foundPlaceholders, string location)
        // {
        //     var text = sharedStringItem.InnerText;
        //     ValidateTextContent(text, sampleRecord, results, foundPlaceholders, location);
        // }

        // private void ValidateTextContent(string text, ExcelRecord sampleRecord, List<ValidationResult> results, HashSet<string> foundPlaceholders, string location)
        // {
        //     foreach (Match match in _placeholderRegex.Matches(text))
        //     {
        //         var placeholder = match.Value;
        //         var key = placeholder.Trim('[', ']');
        //         foundPlaceholders.Add(placeholder);
        //         var sampleValue = sampleRecord.GetValue(key);

        //         if (!sampleRecord.Values.ContainsKey(key))
        //         {
        //             results.Add(new ValidationResult
        //             {
        //                 Placeholder = placeholder,
        //                 Status = ValidationStatus.Error,
        //                 Message = "Placeholder not found as a column in CSV",
        //                 SampleValue = "N/A",
        //                 Location = location
        //             });
        //         } else if (string.IsNullOrEmpty(sampleValue)) {
        //              results.Add(new ValidationResult
        //             {
        //                 Placeholder = placeholder,
        //                 Status = ValidationStatus.Warning,
        //                 Message = "Placeholder found in CSV, but value is empty",
        //                 SampleValue = "(empty)",
        //                 Location = location
        //             });
        //         } else {
        //              results.Add(new ValidationResult
        //             {
        //                 Placeholder = placeholder,
        //                 Status = ValidationStatus.Success,
        //                 Message = "Placeholder found in CSV with value",
        //                 SampleValue = sampleValue,
        //                 Location = location
        //             });
        //         }
        //     }
        // }

        // Old ValidationResult class (moved and updated)
        // public enum ValidationStatus
        // {
        //     Success, Warning, Error
        // }
        // public class ValidationResult
        // {
        //     public string Placeholder { get; set; }
        //     public ValidationStatus Status { get; set; }
        //     public string Message { get; set; }
        //     public string SampleValue { get; set; }
        //     public string Location { get; set; }
        // }
    }
} 