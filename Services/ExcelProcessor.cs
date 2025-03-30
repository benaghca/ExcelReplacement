using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using ExcelReplacement.Models;

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
            var sheets = _workbookPart.Workbook.Sheets.Cast<Sheet>();
            foreach (var sheet in sheets)
            {
                ProcessSheet(sheet);
            }
        }

        private void ProcessSheet(Sheet sheet)
        {
            var worksheetPart = (WorksheetPart)_workbookPart.GetPartById(sheet.Id);
            var sheetData = worksheetPart.Worksheet.Elements<SheetData>().FirstOrDefault();

            if (sheetData != null)
            {
                foreach (var row in sheetData.Elements<Row>())
                {
                    ProcessRow(row);
                }
            }
        }

        private void ProcessRow(Row row)
        {
            foreach (var cell in row.Elements<Cell>())
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
            int sharedStringIndex = int.Parse(cell.CellValue.Text);
            var sharedStringItem = _workbookPart.SharedStringTablePart.SharedStringTable
                .Elements<SharedStringItem>()
                .ElementAt(sharedStringIndex);

            if (sharedStringItem.Text != null)
            {
                ProcessSimpleText(sharedStringItem);
            }
            else if (sharedStringItem.Elements<Run>().Any())
            {
                ProcessFormattedText(sharedStringItem);
            }
        }

        private void ProcessSimpleText(SharedStringItem sharedStringItem)
        {
            string cellText = sharedStringItem.Text.Text;
            string processedText = ReplacePlaceholders(cellText);
            sharedStringItem.Text = new Text(processedText);
        }

        private void ProcessFormattedText(SharedStringItem sharedStringItem)
        {
            foreach (var run in sharedStringItem.Elements<Run>())
            {
                string runText = run.Text.Text;
                string processedText = ReplacePlaceholders(runText);
                run.Text = new Text(processedText) { Space = SpaceProcessingModeValues.Preserve };
            }
        }

        private string ReplacePlaceholders(string text)
        {
            string result = text;
            foreach (var key in _record.Values.Keys)
            {
                string placeholder = $"[[{key}]]";
                if (result.Contains(placeholder))
                {
                    result = result.Replace(placeholder, _record.GetValue(key));
                }
            }
            return result;
        }
    }
} 