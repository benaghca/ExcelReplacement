using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
                return Ok(new PreviewResponse 
                { 
                    previewName = previewName,
                    fields = sampleRecord.Values
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating preview");
                return StatusCode(500, new { error = ex.Message });
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
    }
} 