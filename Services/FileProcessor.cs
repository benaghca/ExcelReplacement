using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using OfficeOpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Linq;

namespace ExcelReplacement.Services
{
    public class FileProcessor : IDisposable
    {
        public FileProcessor()
        {
            // Set EPPlus license context
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        public async Task ProcessExcelTemplate(string templatePath, string outputPath, Dictionary<string, string> replacements)
        {
            // EPPlus operations can be synchronous, run in a Task to match signature
            await Task.Run(() =>
            {
                FileInfo templateFile = new FileInfo(templatePath);
                using (ExcelPackage workbook = new ExcelPackage(templateFile))
                {
                    ExcelWorksheet? worksheet = workbook.Workbook.Worksheets.FirstOrDefault();
                    if (worksheet == null)
                    {
                        throw new InvalidOperationException("Worksheet not found in template.");
                    }

                    // Replace placeholders in the worksheet
                    foreach (var cell in worksheet.Cells)
                    {
                        if (cell.Value != null && cell.Value is string cellValue)
                        {
                            string newValue = cellValue;
                            foreach (var replacement in replacements)
                            {
                                newValue = newValue.Replace($"[{replacement.Key}]", replacement.Value ?? "");
                            }
                            cell.Value = newValue;
                        }
                    }

                    FileInfo outputFile = new FileInfo(outputPath);
                    workbook.SaveAs(outputFile);
                }
            });
        }

        public async Task ProcessWordTemplate(string templatePath, string outputPath, Dictionary<string, string> replacements)
        {
            await Task.Run(() =>
            {
                // Copy the template to the output path to avoid modifying the original
                File.Copy(templatePath, outputPath, true);

                using (WordprocessingDocument wordDocument = WordprocessingDocument.Open(outputPath, true))
                {
                    string? docText = null;
                    if (wordDocument.MainDocumentPart != null)
                    {
                        using (StreamReader sr = new StreamReader(wordDocument.MainDocumentPart.GetStream()))
                        {
                            docText = sr.ReadToEnd();
                        }
                    }

                    if (docText != null)
                    {
                        foreach (var replacement in replacements)
                        {
                             docText = docText.Replace($"[{replacement.Key}]", replacement.Value ?? "");
                        }

                        if (wordDocument.MainDocumentPart != null)
                        {
                            using (StreamWriter sw = new StreamWriter(wordDocument.MainDocumentPart.GetStream(FileMode.Create)))
                            {
                                sw.Write(docText);
                            }
                        }
                    }
                }
            });
        }

        public void Dispose()
        {
            // No COM objects to release with EPPlus and OpenXml SDK
        }
    }
} 