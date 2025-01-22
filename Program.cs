using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using CsvHelper;
using CsvHelper.Configuration;

namespace ExcelReplacement
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("Usage: ExcelReplacement <csvFilePath> <templateDirectory> <outputDirectory>");
                return;
            }

            Console.WriteLine("Arguments received:");
            foreach (var arg in args)
            {
                Console.WriteLine(arg);
            }

            string csvFilePath = args[0];
            string templateDirectory = args[1];
            string outputDirectory = args[2];
            var nameColumns = new List<string> { "Site", "Building", "Lineup", "Equipment", "Procedure" };

            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            var records = LoadCsvData(csvFilePath);

            foreach (var record in records)
            {
                ProcessRecord(record, nameColumns, templateDirectory, outputDirectory);
            }

            Console.WriteLine("Completed processing.");
        }

        private static List<Dictionary<string, string>> LoadCsvData(string csvFilePath)
        {
            var records = new List<Dictionary<string, string>>();

            using (var reader = new StreamReader(csvFilePath))
            {
                /*
                // Print the first few lines of the CSV file for debugging
                Console.WriteLine("First few lines of the CSV file:");
                for (int i = 0; i < 5; i++)
                {
                    if (reader.EndOfStream) break;
                    Console.WriteLine(reader.ReadLine());
                }
                */

                reader.BaseStream.Seek(0, SeekOrigin.Begin); // Reset the reader to the beginning
                reader.DiscardBufferedData();

                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true
                };

                using (var csv = new CsvReader(reader, config))
                {
                    csv.Read();
                    csv.ReadHeader();
                    var headers = csv.Context.Reader.HeaderRecord;
                    if (headers == null)
                    {
                        throw new Exception("CSV file does not contain headers.");
                    }

                    // Print headers for debugging
                    Console.WriteLine("Headers found in CSV:");
                    foreach (var header in headers)
                    {
                        Console.WriteLine(header);
                    }

                    while (csv.Read())
                    {
                        var record = new Dictionary<string, string>();
                        foreach (var header in headers)
                        {
                            record[header] = csv.GetField(header);
                        }
                        records.Add(record);
                    }
                }
            }

            return records;
        }

        private static void ProcessRecord(Dictionary<string, string> record, List<string> nameColumns, string templateDirectory, string outputDirectory)
        {
            // Load the template file
            string templatePath = Path.Combine(templateDirectory, "default_template.xlsx");
            string outputFilePath = Path.Combine(outputDirectory, GenerateOutputFileName(record));
            File.Copy(templatePath, outputFilePath, true);

            using (var document = SpreadsheetDocument.Open(outputFilePath, true))
            {
                var workbookPart = document.WorkbookPart;
                var sheets = workbookPart.Workbook.Sheets.Cast<Sheet>();

                foreach (var sheet in sheets)
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
                                    int sharedStringIndex = int.Parse(cell.CellValue.Text);
                                    var sharedStringItem = workbookPart.SharedStringTablePart.SharedStringTable.Elements<SharedStringItem>().ElementAt(sharedStringIndex);

                                    if (sharedStringItem.Text != null)
                                    {
                                        // Simple text replacement
                                        string cellText = sharedStringItem.Text.Text;
                                        foreach (var key in record.Keys)
                                        {
                                            string placeholder = $"[[{key}]]";
                                            if (cellText.Contains(placeholder))
                                            {
                                                cellText = cellText.Replace(placeholder, record[key]);
                                            }
                                        }
                                        sharedStringItem.Text = new Text(cellText);
                                    }
                                    else if (sharedStringItem.Elements<Run>().Any())
                                    {
                                        // Handle text runs for preserving formatting
                                        foreach (var run in sharedStringItem.Elements<Run>())
                                        {
                                            string runText = run.Text.Text;
                                            foreach (var key in record.Keys)
                                            {
                                                string placeholder = $"[[{key}]]";
                                                if (runText.Contains(placeholder))
                                                {
                                                    runText = runText.Replace(placeholder, record[key]);
                                                }
                                            }
                                            run.Text = new Text(runText) { Space = SpaceProcessingModeValues.Preserve };
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private static string GetCellValue(Cell cell, WorkbookPart workbookPart)
        {
            if (cell == null || cell.CellValue == null)
            {
                return null;
            }

            string value = cell.CellValue.InnerText;
            if (cell.DataType != null && cell.DataType.Value == CellValues.SharedString)
            {
                return workbookPart.SharedStringTablePart.SharedStringTable.Elements<SharedStringItem>().ElementAt(int.Parse(value)).InnerText;
            }

            return value;
        }

        private static void SetCellValue(Cell cell, string value)
        {
            if (cell == null || value == null)
            {
                return;
            }

            cell.CellValue = new CellValue(value);
            cell.DataType = CellValues.String;
        }

        private static List<string> ExtractPlaceholders(string text)
        {
            var placeholders = new List<string>();
            var matches = Regex.Matches(text, @"\[\[(.*?)\]\]");
            foreach (Match match in matches)
            {
                placeholders.Add(match.Groups[1].Value);
            }
            return placeholders;
        }

        private static string GenerateOutputFileName(Dictionary<string, string> record)
        {
            return $"{record["Site"]} {record["Building"]} {record["Equipment"]} {record["Lineup"]} {record["Procedure"]}.xlsx";
        }
    }
}