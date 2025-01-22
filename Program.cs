using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using CsvHelper;
using CsvHelper.Configuration;

namespace ExcelReplacementApp
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("Usage: ExcelReplacementApp <csvFilePath> <templateDirectory> <outputDirectory>");
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
                // Print the first few lines of the CSV file for debugging
                Console.WriteLine("First few lines of the CSV file:");
                for (int i = 0; i < 5; i++)
                {
                    if (reader.EndOfStream) break;
                    Console.WriteLine(reader.ReadLine());
                }

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

                foreach (var sheet in workbookPart.Workbook.Sheets.Elements<Sheet>())
                {
                    var worksheetPart = (WorksheetPart)workbookPart.GetPartById(sheet.Id);
                    var sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();

                    foreach (var row in sheetData.Elements<Row>())
                    {
                        foreach (var cell in row.Elements<Cell>())
                        {
                            string cellValue = GetCellValue(cell, workbookPart);
                            if (string.IsNullOrEmpty(cellValue))
                            {
                                Console.WriteLine($"Cell {cell.CellReference} is empty or has an unsupported data type.");
                                continue;
                            }

                            // Process the cell value
                            if (cellValue.Contains("[[") && cellValue.Contains("]]"))
                            {
                                var placeholders = ExtractPlaceholders(cellValue);
                                foreach (var placeholder in placeholders)
                                {
                                    if (record.ContainsKey(placeholder))
                                    {
                                        cellValue = cellValue.Replace($"[[{placeholder}]]", record[placeholder]);
                                    }
                                }
                                SetCellValue(cell, cellValue);
                            }
                        }
                    }
                }

                // Save the processed file
                workbookPart.Workbook.Save();
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
            return $"{record["Site"]}_{record["Building"]}_{record["Lineup"]}_{record["Equipment"]}_{record["Procedure"]}.xlsx";
        }
    }
}