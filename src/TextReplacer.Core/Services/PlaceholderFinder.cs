using TextReplacer.Core.Models;

namespace TextReplacer.Core.Services;

/// <summary>
/// Unified service for extracting placeholders from any supported document type.
/// </summary>
public class PlaceholderFinder
{
    private readonly WordProcessor _wordProcessor = new();
    private readonly ExcelProcessor _excelProcessor = new();

    /// <summary>
    /// Extracts placeholders from a document, automatically detecting the document type.
    /// </summary>
    /// <param name="filePath">Path to the document file.</param>
    /// <param name="config">Placeholder configuration.</param>
    /// <returns>Set of unique placeholder names found in the document.</returns>
    public IReadOnlySet<string> ExtractPlaceholders(string filePath, PlaceholderConfig config)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();

        return extension switch
        {
            ".docx" => _wordProcessor.ExtractPlaceholders(filePath, config),
            ".xlsx" => _excelProcessor.ExtractPlaceholders(filePath, config),
            _ => throw new NotSupportedException($"File type '{extension}' is not supported. Use .docx or .xlsx files.")
        };
    }

    /// <summary>
    /// Validates a template against CSV headers and returns the validation result.
    /// </summary>
    /// <param name="templatePath">Path to the template document.</param>
    /// <param name="csvPath">Path to the CSV file.</param>
    /// <param name="config">Placeholder configuration.</param>
    /// <returns>Validation result with matched, missing, and unused columns.</returns>
    public ValidationResult ValidateTemplate(string templatePath, string csvPath, PlaceholderConfig config)
    {
        var placeholders = ExtractPlaceholders(templatePath, config);

        using var csvReader = new CsvReaderService(csvPath);
        var headers = csvReader.GetHeaders();

        return new ValidationResult(placeholders, headers);
    }
}
