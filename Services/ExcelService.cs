using DocumentFormat.OpenXml.Packaging;
using ExcelReplacement.Models;

namespace ExcelReplacement.Services
{
    public class ExcelService
    {
        public void ProcessRecord(ExcelRecord record, string templatePath, string outputPath)
        {
            File.Copy(templatePath, outputPath, true);

            using (var document = SpreadsheetDocument.Open(outputPath, true))
            {
                var processor = new ExcelProcessor(document.WorkbookPart, record);
                processor.ProcessAllSheets();
            }
        }
    }
} 