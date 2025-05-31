using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using ExcelReplacement.Models;

namespace ExcelReplacement.Services
{
    public class PreviewService
    {
        private readonly Regex _placeholderRegex = new(@"\[([^\]]+)\]");

        public string GeneratePreview(string templatePath, ExcelRecord sampleRecord, bool isExcel)
        {
            if (isExcel)
            {
                return GenerateExcelPreview(templatePath, sampleRecord);
            }
            else
            {
                return GenerateWordPreview(templatePath, sampleRecord);
            }
        }

        private string GenerateExcelPreview(string templatePath, ExcelRecord sampleRecord)
        {
            var preview = new StringBuilder();
            preview.AppendLine("Excel Template Preview");
            preview.AppendLine("=====================");

            using (var document = SpreadsheetDocument.Open(templatePath, false))
            {
                var workbookPart = document.WorkbookPart;
                if (workbookPart == null) return "Error: Invalid Excel file";

                foreach (var sheet in workbookPart.Workbook.Sheets.Cast<Sheet>())
                {
                    preview.AppendLine($"\nSheet: {sheet.Name}");
                    preview.AppendLine("-------------------");

                    var worksheetPart = (WorksheetPart)workbookPart.GetPartById(sheet.Id);
                    var sheetData = worksheetPart.Worksheet.Elements<SheetData>().FirstOrDefault();

                    if (sheetData != null)
                    {
                        foreach (var row in sheetData.Elements<Row>())
                        {
                            var rowContent = new List<string>();
                            foreach (var cell in row.Elements<Cell>())
                            {
                                if (cell.DataType != null && cell.DataType == CellValues.SharedString)
                                {
                                    var sharedStringItem = workbookPart.SharedStringTablePart.SharedStringTable
                                        .Elements<SharedStringItem>()
                                        .ElementAt(int.Parse(cell.CellValue.Text));

                                    string cellText;
                                    if (sharedStringItem.Text != null)
                                    {
                                        cellText = sharedStringItem.Text.Text;
                                    }
                                    else
                                    {
                                        cellText = string.Join("", sharedStringItem.Elements<DocumentFormat.OpenXml.Spreadsheet.Run>().Select(r => r.Text.Text));
                                    }

                                    cellText = ReplacePlaceholders(cellText, sampleRecord);
                                    rowContent.Add(cellText);
                                }
                                else
                                {
                                    rowContent.Add(cell.CellValue?.Text ?? "");
                                }
                            }

                            if (rowContent.Any(c => !string.IsNullOrWhiteSpace(c)))
                            {
                                preview.AppendLine(string.Join(" | ", rowContent));
                            }
                        }
                    }
                }
            }

            return preview.ToString();
        }

        private string GenerateWordPreview(string templatePath, ExcelRecord sampleRecord)
        {
            var preview = new StringBuilder();
            preview.AppendLine("Word Template Preview");
            preview.AppendLine("====================");

            using (var document = WordprocessingDocument.Open(templatePath, false))
            {
                var mainPart = document.MainDocumentPart;
                if (mainPart?.Document?.Body == null) return "Error: Invalid Word file";

                // Process body
                preview.AppendLine("\nDocument Body");
                preview.AppendLine("-------------");
                ProcessWordElement(mainPart.Document.Body, preview, sampleRecord);

                // Process headers
                foreach (var headerPart in mainPart.HeaderParts)
                {
                    if (headerPart.Header != null)
                    {
                        preview.AppendLine("\nHeader");
                        preview.AppendLine("------");
                        ProcessWordElement(headerPart.Header, preview, sampleRecord);
                    }
                }

                // Process footers
                foreach (var footerPart in mainPart.FooterParts)
                {
                    if (footerPart.Footer != null)
                    {
                        preview.AppendLine("\nFooter");
                        preview.AppendLine("------");
                        ProcessWordElement(footerPart.Footer, preview, sampleRecord);
                    }
                }
            }

            return preview.ToString();
        }

        private void ProcessWordElement(OpenXmlElement element, StringBuilder preview, ExcelRecord sampleRecord)
        {
            foreach (var paragraph in element.Descendants<Paragraph>())
            {
                var text = string.Join("", paragraph.Descendants<DocumentFormat.OpenXml.Wordprocessing.Text>().Select(t => t.Text));
                if (!string.IsNullOrWhiteSpace(text))
                {
                    text = ReplacePlaceholders(text, sampleRecord);
                    preview.AppendLine(text);
                }
            }
        }

        private string ReplacePlaceholders(string text, ExcelRecord sampleRecord)
        {
            return _placeholderRegex.Replace(text, match =>
            {
                var fieldName = match.Groups[1].Value;
                return sampleRecord.GetValue(fieldName) ?? match.Value;
            });
        }
    }
} 