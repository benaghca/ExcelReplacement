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
                var workbookPart = document.WorkbookPart;
                if (workbookPart == null)
                {
                    throw new InvalidOperationException($"Workbook part not found in Excel template: {templatePath}");
                }
                var processor = new ExcelProcessor(workbookPart, record);
                processor.ProcessAllSheets();
            }
        }
    }
} 