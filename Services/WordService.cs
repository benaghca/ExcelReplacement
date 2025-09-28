using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ExcelReplacement.Models;
using System.IO;

namespace ExcelReplacement.Services
{
    public class WordService
    {
        public void ProcessRecord(ExcelRecord record, string templatePath, string outputPath)
        {
            // Delete existing file if it exists and is not locked
            if (File.Exists(outputPath))
            {
                try
                {
                    File.Delete(outputPath);
                }
                catch (IOException ex) when (ex.Message.Contains("being used by another process"))
                {
                    // Generate a unique filename if the file is locked
                    var directory = Path.GetDirectoryName(outputPath);
                    var fileName = Path.GetFileNameWithoutExtension(outputPath);
                    var extension = Path.GetExtension(outputPath);
                    var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    outputPath = Path.Combine(directory, $"{fileName}_{timestamp}{extension}");
                }
            }

            // Copy template to output path
            File.Copy(templatePath, outputPath, true);

            using (var document = WordprocessingDocument.Open(outputPath, true))
            {
                var mainDocumentPart = document.MainDocumentPart;
                if (mainDocumentPart == null)
                {
                    throw new InvalidOperationException($"Main document part not found in Word template: {templatePath}");
                }
                var processor = new WordProcessor(mainDocumentPart, record);
                processor.ProcessDocument();
            }
        }
    }
} 