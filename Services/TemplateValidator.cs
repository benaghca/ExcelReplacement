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
            var foundPlaceholders = new HashSet<string>();

            if (_isExcel)
            {
                ValidateExcelTemplate(sampleRecord, results, foundPlaceholders);
            }
            else
            {
                ValidateWordTemplate(sampleRecord, results, foundPlaceholders);
            }

            // Check for unused CSV columns
            foreach (var key in sampleRecord.Values.Keys)
            {
                if (!foundPlaceholders.Contains($"[{key}]"))
                {
                    results.Add(new ValidationResult
                    {
                        Placeholder = $"[{key}]",
                        Status = ValidationStatus.Warning,
                        Message = "This CSV column is not used in the template",
                        SampleValue = sampleRecord.GetValue(key),
                        Location = "CSV"
                    });
                }
            }

            return results;
        }

        private void ValidateExcelTemplate(ExcelRecord sampleRecord, List<ValidationResult> results, HashSet<string> foundPlaceholders)
        {
            using (var document = SpreadsheetDocument.Open(_templatePath, false))
            {
                var workbookPart = document.WorkbookPart;
                if (workbookPart == null) return;

                foreach (var sheet in workbookPart.Workbook.Sheets.Cast<Sheet>())
                {
                    var worksheetPart = (WorksheetPart)workbookPart.GetPartById(sheet.Id);
                    var sheetData = worksheetPart.Worksheet.Elements<SheetData>().FirstOrDefault();

                    if (sheetData != null)
                    {
                        foreach (var row in sheetData.Elements<Row>())
                        {
                            foreach (var cell in row.Elements<Cell>())
                            {
                                if (cell.DataType != null && cell.DataType == CellValues.SharedString)
                                {
                                    var sharedStringItem = workbookPart.SharedStringTablePart.SharedStringTable
                                        .Elements<SharedStringItem>()
                                        .ElementAt(int.Parse(cell.CellValue.Text));

                                    ValidateCellContent(sharedStringItem, sampleRecord, results, foundPlaceholders, 
                                        $"Sheet: {sheet.Name}, Cell: {cell.CellReference}");
                                }
                            }
                        }
                    }
                }
            }
        }

        private void ValidateWordTemplate(ExcelRecord sampleRecord, List<ValidationResult> results, HashSet<string> foundPlaceholders)
        {
            using (var document = WordprocessingDocument.Open(_templatePath, false))
            {
                var mainPart = document.MainDocumentPart;
                if (mainPart?.Document?.Body == null) return;

                // Process body
                ProcessWordElement(mainPart.Document.Body, sampleRecord, results, foundPlaceholders, "Body");

                // Process headers
                foreach (var headerPart in mainPart.HeaderParts)
                {
                    if (headerPart.Header != null)
                    {
                        ProcessWordElement(headerPart.Header, sampleRecord, results, foundPlaceholders, "Header");
                    }
                }

                // Process footers
                foreach (var footerPart in mainPart.FooterParts)
                {
                    if (footerPart.Footer != null)
                    {
                        ProcessWordElement(footerPart.Footer, sampleRecord, results, foundPlaceholders, "Footer");
                    }
                }
            }
        }

        private void ProcessWordElement(OpenXmlElement element, ExcelRecord sampleRecord, 
            List<ValidationResult> results, HashSet<string> foundPlaceholders, string location)
        {
            foreach (var paragraph in element.Descendants<Paragraph>())
            {
                var text = string.Join("", paragraph.Descendants<DocumentFormat.OpenXml.Wordprocessing.Text>().Select(t => t.Text));
                ValidateTextContent(text, sampleRecord, results, foundPlaceholders, location);
            }
        }

        private void ValidateCellContent(SharedStringItem sharedStringItem, ExcelRecord sampleRecord, 
            List<ValidationResult> results, HashSet<string> foundPlaceholders, string location)
        {
            string text;
            if (sharedStringItem.Text != null)
            {
                text = sharedStringItem.Text.Text;
            }
            else
            {
                text = string.Join("", sharedStringItem.Elements<DocumentFormat.OpenXml.Spreadsheet.Run>().Select(r => r.Text.Text));
            }

            ValidateTextContent(text, sampleRecord, results, foundPlaceholders, location);
        }

        private void ValidateTextContent(string text, ExcelRecord sampleRecord, 
            List<ValidationResult> results, HashSet<string> foundPlaceholders, string location)
        {
            var matches = _placeholderRegex.Matches(text);
            foreach (Match match in matches)
            {
                var placeholder = match.Value;
                var fieldName = match.Groups[1].Value;
                foundPlaceholders.Add(placeholder);

                var result = new ValidationResult
                {
                    Placeholder = placeholder,
                    Location = location,
                    SampleValue = sampleRecord.GetValue(fieldName)
                };

                if (!sampleRecord.Values.ContainsKey(fieldName))
                {
                    result.Status = ValidationStatus.Error;
                    result.Message = "This placeholder has no matching column in the CSV file";
                }
                else if (string.IsNullOrEmpty(sampleRecord.GetValue(fieldName)))
                {
                    result.Status = ValidationStatus.Warning;
                    result.Message = "Sample value is empty";
                }
                else
                {
                    result.Status = ValidationStatus.Success;
                    result.Message = "Valid";
                }

                results.Add(result);
            }
        }
    }

    public class ValidationResult
    {
        public string Placeholder { get; set; }
        public ValidationStatus Status { get; set; }
        public string Message { get; set; }
        public string SampleValue { get; set; }
        public string Location { get; set; }
    }

    public enum ValidationStatus
    {
        Success,
        Warning,
        Error
    }
} 