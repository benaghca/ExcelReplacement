using GemBox.Document;
using GemBox.Spreadsheet;

namespace TextReplacer.Core.Services;

/// <summary>
/// Converts processed .xlsx and .docx files to PDF using GemBox libraries.
/// Call with the free-tier key — no Office installation required.
/// </summary>
public static class PdfExporter
{
    private static bool _initialized = false;

    private static void EnsureInitialized()
    {
        if (_initialized) return;
        SpreadsheetInfo.SetLicense("FREE-LIMITED-KEY");
        ComponentInfo.SetLicense("FREE-LIMITED-KEY");
        _initialized = true;
    }

    /// <summary>
    /// Converts a .xlsx or .docx file to PDF and returns the PDF path.
    /// The original file is deleted after successful conversion.
    /// </summary>
    public static string ConvertToPdf(string filePath)
    {
        EnsureInitialized();

        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        var pdfPath = Path.ChangeExtension(filePath, ".pdf");

        if (extension == ".xlsx")
            ConvertExcel(filePath, pdfPath);
        else if (extension == ".docx")
            ConvertWord(filePath, pdfPath);
        else
            throw new NotSupportedException($"PDF conversion is not supported for '{extension}' files.");

        File.Delete(filePath);
        return pdfPath;
    }

    private static void ConvertExcel(string inputPath, string pdfPath)
    {
        var workbook = ExcelFile.Load(inputPath);
        workbook.Save(pdfPath);
    }

    private static void ConvertWord(string inputPath, string pdfPath)
    {
        var document = DocumentModel.Load(inputPath);
        document.Save(pdfPath);
    }
}
