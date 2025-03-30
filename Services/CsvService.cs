using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using ExcelReplacement.Models;

namespace ExcelReplacement.Services
{
    public class CsvService
    {
        public List<ExcelRecord> LoadCsvData(string csvFilePath)
        {
            var records = new List<ExcelRecord>();

            using (var reader = new StreamReader(csvFilePath))
            {
                reader.BaseStream.Seek(0, SeekOrigin.Begin);
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
                        var record = new ExcelRecord();
                        foreach (var header in headers)
                        {
                            record.Values[header] = csv.GetField(header);
                        }
                        records.Add(record);
                    }
                }
            }

            return records;
        }
    }
} 