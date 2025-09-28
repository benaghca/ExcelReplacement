using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using ExcelReplacement.Models;
using System.Linq;

namespace ExcelReplacement.Services
{
    public class ExcelProcessor
    {
        private readonly WorkbookPart _workbookPart;
        private readonly ExcelRecord _record;

        public ExcelProcessor(WorkbookPart workbookPart, ExcelRecord record)
        {
            _workbookPart = workbookPart;
            _record = record;
        }

        public void ProcessAllSheets()
        {
            if (_workbookPart.Workbook?.Sheets == null) return;
            var sheets = _workbookPart.Workbook.Sheets.OfType<Sheet>();
            foreach (var sheet in sheets)
            {
                ProcessSheet(sheet);
            }
        }

        private void ProcessSheet(Sheet sheet)
        {
            if (sheet?.Id?.Value == null) return;
            var worksheetPart = _workbookPart.GetPartById(sheet.Id.Value) as WorksheetPart;
            if (worksheetPart?.Worksheet == null) return;

            var sheetData = worksheetPart.Worksheet.Elements<SheetData>().FirstOrDefault();

            if (sheetData != null)
            {
                foreach (var row in sheetData.Elements<Row>().Where(r => r != null))
                {
                    ProcessRow(row);
                }
            }
        }

        private void ProcessRow(Row row)
        {
            foreach (var cell in row.Elements<Cell>().Where(c => c != null))
            {
                ProcessCell(cell);
            }
        }

        private void ProcessCell(Cell cell)
        {
            if (IsSharedStringCell(cell))
            {
                ProcessSharedStringCell(cell);
            }
        }

        private bool IsSharedStringCell(Cell cell)
        {
            return cell.DataType != null && cell.DataType == CellValues.SharedString;
        }

        private void ProcessSharedStringCell(Cell cell)
        {
            if (cell.CellValue?.Text == null || _workbookPart.SharedStringTablePart?.SharedStringTable == null) return;

            if (int.TryParse(cell.CellValue.Text, out int sharedStringIndex))
            {
                var sharedStringItem = _workbookPart.SharedStringTablePart.SharedStringTable
                    .Elements<SharedStringItem>()
                    .ElementAtOrDefault(sharedStringIndex);

                if (sharedStringItem?.Text != null)
                {
                    ProcessSimpleText(sharedStringItem);
                }
                else if (sharedStringItem?.Elements<Run>().Any() == true)
                {
                    ProcessFormattedText(sharedStringItem);
                }
            }
        }

        private void ProcessSimpleText(SharedStringItem sharedStringItem)
        {
            if (sharedStringItem?.Text?.Text == null) return;
            string cellText = sharedStringItem.Text.Text;
            string processedText = ReplacePlaceholders(cellText);
            sharedStringItem.Text.Text = processedText;
        }

        private void ProcessFormattedText(SharedStringItem sharedStringItem)
        {
            if (sharedStringItem == null) return;
            foreach (var run in sharedStringItem.Elements<Run>().Where(r => r != null))
            {
                if (run?.Text?.Text == null) continue;
                string runText = run.Text.Text;
                string processedText = ReplacePlaceholders(runText);
                run.Text.Text = processedText;
                if (run.Text.Space?.Value == SpaceProcessingModeValues.Preserve)
                {
                    run.Text.Space = new EnumValue<SpaceProcessingModeValues>(SpaceProcessingModeValues.Preserve);
                } else {
                     // If no original space setting, ensure it's default
                    run.Text.Space = null;
                }
            }
        }

        private string ReplacePlaceholders(string text)
        {
            string result = text;
            // Use null conditional access for _record.Values
            if (_record?.Values != null)
            {
                foreach (var key in _record.Values.Keys)
                {
                    string placeholder = $"[{key}]";
                    if (result.Contains(placeholder))
                    {
                         // Use null conditional access for _record.GetValue(key)
                        result = result.Replace(placeholder, _record.GetValue(key) ?? ""); 
                    }
                }
            }
            return result;
        }
    }
} 