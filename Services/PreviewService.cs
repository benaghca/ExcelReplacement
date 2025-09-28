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
                if (workbookPart?.Workbook?.Sheets == null) return "Error: Invalid Excel file or missing sheets";

                foreach (var sheet in workbookPart.Workbook.Sheets.OfType<Sheet>())
                {
                    if (sheet?.Name == null || sheet.Id?.Value == null) continue;
                    preview.AppendLine($"\nSheet: {sheet.Name.Value}");
                    preview.AppendLine("-------------------");

                    var worksheetPart = workbookPart.GetPartById(sheet.Id.Value) as WorksheetPart;
                    if (worksheetPart?.Worksheet == null) continue;

                    var sheetData = worksheetPart.Worksheet.Elements<SheetData>().FirstOrDefault();

                    if (sheetData != null)
                    {
                        foreach (var row in sheetData.Elements<Row>().Where(r => r != null))
                        {
                            var rowContent = new List<string>();
                            foreach (var cell in row.Elements<Cell>().Where(c => c != null))
                            {
                                if (cell.DataType != null && cell.DataType == CellValues.SharedString)
                                {
                                    if (cell.CellValue?.Text == null || workbookPart.SharedStringTablePart?.SharedStringTable == null) 
                                    {
                                        rowContent.Add("");
                                        continue;
                                    }
                                     if (int.TryParse(cell.CellValue.Text, out int ssIndex))
                                    {
                                        var sharedStringItem = workbookPart.SharedStringTablePart.SharedStringTable
                                            .Elements<SharedStringItem>()
                                            .ElementAtOrDefault(ssIndex);

                                        string cellText;
                                        if (sharedStringItem?.Text?.Text != null)
                                        {
                                            cellText = sharedStringItem.Text.Text;
                                        }
                                        else if (sharedStringItem?.Elements<DocumentFormat.OpenXml.Spreadsheet.Run>().Any() == true)
                                        {
                                            cellText = string.Join("", sharedStringItem.Elements<DocumentFormat.OpenXml.Spreadsheet.Run>().Select(r => r.Text?.Text ?? ""));
                                        }
                                        else
                                        {
                                            cellText = "";
                                        }

                                        cellText = ReplacePlaceholders(cellText, sampleRecord);
                                        rowContent.Add(cellText);
                                    }
                                    else
                                    {
                                        rowContent.Add("Error: Invalid shared string index");
                                    }
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
                if (mainPart?.Document?.Body == null) return "Error: Invalid Word file or empty body";

                // Process body
                preview.AppendLine("\nDocument Body");
                preview.AppendLine("-------------");
                ProcessWordElement(mainPart.Document.Body, preview, sampleRecord);

                // Process headers
                if (mainPart.HeaderParts != null)
                {
                    foreach (var headerPart in mainPart.HeaderParts)
                    {
                        if (headerPart?.Header != null)
                        {
                            preview.AppendLine("\nHeader");
                            preview.AppendLine("------");
                            ProcessWordElement(headerPart.Header, preview, sampleRecord);
                        }
                    }
                }

                // Process footers
                if (mainPart.FooterParts != null)
                {
                    foreach (var footerPart in mainPart.FooterParts)
                    {
                        if (footerPart?.Footer != null)
                        {
                            preview.AppendLine("\nFooter");
                            preview.AppendLine("------");
                            ProcessWordElement(footerPart.Footer, preview, sampleRecord);
                        }
                    }
                }
            }

            return preview.ToString();
        }

        private void ProcessWordElement(OpenXmlElement element, StringBuilder preview, ExcelRecord sampleRecord)
        {
            foreach (var paragraph in element.Descendants<Paragraph>().Where(p => p != null))
            {
                var text = string.Join("", paragraph.Descendants<DocumentFormat.OpenXml.Wordprocessing.Text>().Select(t => t.Text ?? ""));
                if (!string.IsNullOrWhiteSpace(text))
                {
                    text = ReplacePlaceholders(text, sampleRecord);
                    preview.AppendLine(text);
                }
            }
            
            foreach (var table in element.Descendants<DocumentFormat.OpenXml.Wordprocessing.Table>().Where(t => t != null))
            {
                foreach(var cell in table.Descendants<DocumentFormat.OpenXml.Wordprocessing.TableCell>().Where(c => c != null))
                {
                    var cellText = string.Join("", cell.Descendants<DocumentFormat.OpenXml.Wordprocessing.Text>().Select(t => t.Text ?? ""));
                    if (!string.IsNullOrWhiteSpace(cellText))
                    {
                        cellText = ReplacePlaceholders(cellText, sampleRecord);
                        preview.AppendLine($"Table Cell: {cellText}");
                    }
                }
            }
        }

        private string ReplacePlaceholders(string text, ExcelRecord sampleRecord)
        {
            return _placeholderRegex.Replace(text, match =>
            {
                var fieldName = match.Groups[1].Value;
                return sampleRecord.GetValue(fieldName) ?? match.Value ?? "";
            });
        }
    }
} 