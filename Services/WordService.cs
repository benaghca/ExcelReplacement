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