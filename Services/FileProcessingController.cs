using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ExcelReplacement.Services
{
    [ApiController]
    [Route("api/[controller]")]
    public class FileProcessingController : ControllerBase
    {
        private readonly ILogger<FileProcessingController> _logger;
        private readonly ExcelService _excelService;
        private readonly WordService _wordService;
        private readonly CsvService _csvService;
        private readonly PreviewService _previewService;

        public FileProcessingController(
            ILogger<FileProcessingController> logger,
            ExcelService excelService,
            WordService wordService,
            CsvService csvService,
            PreviewService previewService)
        {
            _logger = logger;
            _excelService = excelService;
            _wordService = wordService;
            _csvService = csvService;
            _previewService = previewService;
        }

        [HttpPost("process")]
        public IActionResult ProcessFiles([FromBody] ProcessRequest request)
        {
            try
            {
                // Ensure output directory exists
                if (!Directory.Exists(request.OutputPath))
                {
                    Directory.CreateDirectory(request.OutputPath);
                }

                var records = _csvService.LoadCsvData(request.CsvFilePath);
                var outputFiles = new List<string>();
                int processedFiles = 0;

                foreach (var record in records)
                {
                    var fileName = record.GenerateFileName(request.FileNamePattern, request.TemplateType.ToLower() == "excel" ? ".xlsx" : ".docx");
                    var outputPath = Path.Combine(request.OutputPath, fileName);
                    if (request.TemplateType.ToLower() == "excel")
                    {
                        _excelService.ProcessRecord(record, request.TemplatePath, outputPath);
                    }
                    else if (request.TemplateType.ToLower() == "word")
                    {
                        _wordService.ProcessRecord(record, request.TemplatePath, outputPath);
                    }
                    else
                    {
                        return BadRequest(new { error = "Unsupported file type. Only 'excel' and 'word' are supported." });
                    }
                    outputFiles.Add(outputPath);
                    processedFiles++;
                }

                return Ok(new ProcessResponse 
                { 
                    success = true, 
                    processedFiles = processedFiles, 
                    outputFiles = outputFiles 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing files: {Message}", ex.Message);
                return StatusCode(500, new { 
                    error = "An error occurred while processing files", 
                    details = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        [HttpPost("preview")]
        public IActionResult PreviewFileName([FromBody] PreviewRequest request)
        {
            try
            {
                var records = _csvService.LoadCsvData(request.CsvFilePath);
                var sampleRecord = records.FirstOrDefault();
                if (sampleRecord == null)
                {
                    return BadRequest(new { error = "CSV file is empty or invalid." });
                }

                var previewName = sampleRecord.GenerateFileName(request.FileNamePattern);
                
                // Extract placeholders from template content
                var templatePlaceholders = ExtractPlaceholdersFromTemplate(request.TemplatePath);
                
                return Ok(new PreviewResponse 
                { 
                    previewName = previewName,
                    fields = sampleRecord.Values,
                    headers = sampleRecord.Values.Keys.ToList(),
                    templatePlaceholders = templatePlaceholders
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating preview");
                return StatusCode(500, new { error = ex.Message });
            }
        }
        
        private List<string> ExtractPlaceholdersFromTemplate(string templatePath)
        {
            var placeholders = new HashSet<string>();
            var placeholderRegex = new System.Text.RegularExpressions.Regex(@"\[([^\]]+)\]");
            
            try
            {
                if (templatePath.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                {
                    ExtractPlaceholdersFromExcel(templatePath, placeholderRegex, placeholders);
                }
                else if (templatePath.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
                {
                    ExtractPlaceholdersFromWord(templatePath, placeholderRegex, placeholders);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error extracting placeholders from template");
            }
            
            return placeholders.ToList();
        }
        
        private string ExtractTemplateContent(string templatePath)
        {
            var content = new StringBuilder();
            
            try
            {
                if (templatePath.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                {
                    ExtractExcelContent(templatePath, content);
                }
                else if (templatePath.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
                {
                    ExtractWordContent(templatePath, content);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error extracting template content");
            }
            
            return content.ToString();
        }
        
        private void ExtractPlaceholdersFromExcel(string templatePath, System.Text.RegularExpressions.Regex regex, HashSet<string> placeholders)
        {
            using (var document = DocumentFormat.OpenXml.Packaging.SpreadsheetDocument.Open(templatePath, false))
            {
                var workbookPart = document.WorkbookPart;
                if (workbookPart?.Workbook?.Sheets == null) return;

                foreach (var sheet in workbookPart.Workbook.Sheets.OfType<DocumentFormat.OpenXml.Spreadsheet.Sheet>())
                {
                    if (sheet?.Id?.Value == null) continue;
                    
                    var worksheetPart = workbookPart.GetPartById(sheet.Id.Value) as DocumentFormat.OpenXml.Packaging.WorksheetPart;
                    if (worksheetPart?.Worksheet == null) continue;

                    var sheetData = worksheetPart.Worksheet.Elements<DocumentFormat.OpenXml.Spreadsheet.SheetData>().FirstOrDefault();
                    if (sheetData == null) continue;

                    foreach (var row in sheetData.Elements<DocumentFormat.OpenXml.Spreadsheet.Row>().Where(r => r != null))
                    {
                        foreach (var cell in row.Elements<DocumentFormat.OpenXml.Spreadsheet.Cell>().Where(c => c != null))
                        {
                            string cellText = GetCellText(cell, workbookPart);
                            if (!string.IsNullOrEmpty(cellText))
                            {
                                var matches = regex.Matches(cellText);
                                foreach (System.Text.RegularExpressions.Match match in matches)
                                {
                                    placeholders.Add(match.Groups[1].Value);
                                }
                            }
                        }
                    }
                }
            }
        }
        
        private void ExtractPlaceholdersFromWord(string templatePath, System.Text.RegularExpressions.Regex regex, HashSet<string> placeholders)
        {
            using (var document = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Open(templatePath, false))
            {
                var mainPart = document.MainDocumentPart;
                if (mainPart?.Document?.Body == null) return;

                ExtractPlaceholdersFromWordElement(mainPart.Document.Body, regex, placeholders);

                // Check headers and footers
                if (mainPart.HeaderParts != null)
                {
                    foreach (var headerPart in mainPart.HeaderParts)
                    {
                        if (headerPart?.Header != null)
                        {
                            ExtractPlaceholdersFromWordElement(headerPart.Header, regex, placeholders);
                        }
                    }
                }

                if (mainPart.FooterParts != null)
                {
                    foreach (var footerPart in mainPart.FooterParts)
                    {
                        if (footerPart?.Footer != null)
                        {
                            ExtractPlaceholdersFromWordElement(footerPart.Footer, regex, placeholders);
                        }
                    }
                }
            }
        }
        
        private void ExtractPlaceholdersFromWordElement(DocumentFormat.OpenXml.OpenXmlElement element, System.Text.RegularExpressions.Regex regex, HashSet<string> placeholders)
        {
            foreach (var textElement in element.Descendants<DocumentFormat.OpenXml.Wordprocessing.Text>().Where(t => t?.Text != null))
            {
                var matches = regex.Matches(textElement.Text);
                foreach (System.Text.RegularExpressions.Match match in matches)
                {
                    placeholders.Add(match.Groups[1].Value);
                }
            }
        }
        
        private string GetCellText(DocumentFormat.OpenXml.Spreadsheet.Cell cell, DocumentFormat.OpenXml.Packaging.WorkbookPart workbookPart)
        {
            if (cell.DataType != null && cell.DataType == DocumentFormat.OpenXml.Spreadsheet.CellValues.SharedString)
            {
                if (cell.CellValue?.Text == null || workbookPart.SharedStringTablePart?.SharedStringTable == null) 
                    return "";

                if (int.TryParse(cell.CellValue.Text, out int ssIndex))
                {
                    var sharedStringItem = workbookPart.SharedStringTablePart.SharedStringTable
                        .Elements<DocumentFormat.OpenXml.Spreadsheet.SharedStringItem>()
                        .ElementAtOrDefault(ssIndex);

                    if (sharedStringItem?.Text?.Text != null)
                    {
                        return sharedStringItem.Text.Text;
                    }
                    else if (sharedStringItem?.Elements<DocumentFormat.OpenXml.Spreadsheet.Run>().Any() == true)
                    {
                        return string.Join("", sharedStringItem.Elements<DocumentFormat.OpenXml.Spreadsheet.Run>().Select(r => r.Text?.Text ?? ""));
                    }
                }
            }
            else
            {
                return cell.CellValue?.Text ?? "";
            }
            
            return "";
        }
        
        private void ExtractExcelContent(string templatePath, StringBuilder content)
        {
            using (var document = DocumentFormat.OpenXml.Packaging.SpreadsheetDocument.Open(templatePath, false))
            {
                var workbookPart = document.WorkbookPart;
                if (workbookPart?.Workbook?.Sheets == null) return;

                foreach (var sheet in workbookPart.Workbook.Sheets.OfType<DocumentFormat.OpenXml.Spreadsheet.Sheet>())
                {
                    if (sheet?.Id?.Value == null) continue;
                    
                    var worksheetPart = workbookPart.GetPartById(sheet.Id.Value) as DocumentFormat.OpenXml.Packaging.WorksheetPart;
                    if (worksheetPart?.Worksheet == null) continue;

                    var sheetData = worksheetPart.Worksheet.Elements<DocumentFormat.OpenXml.Spreadsheet.SheetData>().FirstOrDefault();
                    if (sheetData == null) continue;

                    foreach (var row in sheetData.Elements<DocumentFormat.OpenXml.Spreadsheet.Row>().Where(r => r != null))
                    {
                        foreach (var cell in row.Elements<DocumentFormat.OpenXml.Spreadsheet.Cell>().Where(c => c != null))
                        {
                            var cellText = GetCellText(cell, workbookPart);
                            if (!string.IsNullOrEmpty(cellText))
                            {
                                content.Append(cellText).Append(" ");
                            }
                        }
                    }
                }
            }
        }
        
        private void ExtractWordContent(string templatePath, StringBuilder content)
        {
            using (var document = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Open(templatePath, false))
            {
                var mainPart = document.MainDocumentPart;
                if (mainPart?.Document?.Body == null) return;

                // Extract from main body
                ExtractWordElementContent(mainPart.Document.Body, content);
                
                // Extract from headers
                foreach (var headerPart in mainPart.HeaderParts)
                {
                    ExtractWordElementContent(headerPart.Header, content);
                }
                
                // Extract from footers
                foreach (var footerPart in mainPart.FooterParts)
                {
                    ExtractWordElementContent(footerPart.Footer, content);
                }
            }
        }
        
        private void ExtractWordElementContent(DocumentFormat.OpenXml.OpenXmlElement element, StringBuilder content)
        {
            foreach (var textElement in element.Descendants<DocumentFormat.OpenXml.Wordprocessing.Text>())
            {
                if (!string.IsNullOrEmpty(textElement.Text))
                {
                    content.Append(textElement.Text).Append(" ");
                }
            }
        }
    }

    public class ProcessRequest
    {
        public required string CsvFilePath { get; set; }
        public required string TemplatePath { get; set; }
        public required string OutputPath { get; set; }
        public required string TemplateType { get; set; }
        public required string FileNamePattern { get; set; }
    }

    public class PreviewRequest
    {
        public required string CsvFilePath { get; set; }
        public required string FileNamePattern { get; set; }
        public required string TemplatePath { get; set; }
    }

    public class ProcessResponse
    {
        public bool success { get; set; }
        public int processedFiles { get; set; }
        public List<string>? outputFiles { get; set; }
        public List<string>? errors { get; set; }
    }

    public class PreviewResponse
    {
        public required string previewName { get; set; }
        public required Dictionary<string, string> fields { get; set; }
        public List<string>? headers { get; set; }
        public List<string>? templatePlaceholders { get; set; }
    }
} 