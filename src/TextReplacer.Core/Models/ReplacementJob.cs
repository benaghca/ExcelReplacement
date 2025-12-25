namespace TextReplacer.Core.Models;

/// <summary>
/// Represents a complete replacement job with all required inputs and configuration.
/// </summary>
public class ReplacementJob
{
    /// <summary>
    /// Path to the template document (.docx or .xlsx).
    /// </summary>
    public required string TemplatePath { get; set; }

    /// <summary>
    /// Path to the CSV file containing replacement data.
    /// </summary>
    public required string CsvPath { get; set; }

    /// <summary>
    /// Directory where output files will be written.
    /// </summary>
    public required string OutputDirectory { get; set; }

    /// <summary>
    /// Configuration for placeholder syntax.
    /// </summary>
    public PlaceholderConfig PlaceholderConfig { get; set; } = new();

    /// <summary>
    /// Configuration for output file naming.
    /// </summary>
    public OutputNamingConfig OutputNamingConfig { get; set; } = new();

    /// <summary>
    /// Gets the file extension of the template (e.g., ".docx", ".xlsx").
    /// </summary>
    public string TemplateExtension => Path.GetExtension(TemplatePath).ToLowerInvariant();
}
