namespace TextReplacer.Core.Models;

/// <summary>
/// Configuration for how output files should be named.
/// </summary>
public class OutputNamingConfig
{
    /// <summary>
    /// Whether to use a pattern-based naming (true) or sequential numbering (false).
    /// </summary>
    public bool UsePattern { get; set; } = false;

    /// <summary>
    /// Pattern for naming output files when UsePattern is true.
    /// Placeholders like {ColumnName} will be replaced with values from the CSV row.
    /// Example: "{Location}_{Equipment}" -> "Building1_Pump3.docx"
    /// </summary>
    public string Pattern { get; set; } = "output";

    /// <summary>
    /// Prefix for sequential naming when UsePattern is false.
    /// Example: "output" -> "output_001.docx", "output_002.docx"
    /// </summary>
    public string SequentialPrefix { get; set; } = "output";

    /// <summary>
    /// Number of digits for sequential numbering (pads with zeros).
    /// </summary>
    public int SequentialDigits { get; set; } = 3;

    /// <summary>
    /// Generates the output filename (without extension) for a given row.
    /// </summary>
    public string GenerateFileName(IDictionary<string, string> rowData, int rowIndex)
    {
        if (!UsePattern)
        {
            return $"{SequentialPrefix}_{(rowIndex + 1).ToString().PadLeft(SequentialDigits, '0')}";
        }

        var result = Pattern;
        foreach (var kvp in rowData)
        {
            result = result.Replace($"{{{kvp.Key}}}", SanitizeFileName(kvp.Value));
        }
        return result;
    }

    private static string SanitizeFileName(string input)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Join("_", input.Split(invalid, StringSplitOptions.RemoveEmptyEntries));
    }
}
