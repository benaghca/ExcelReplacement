using TextReplacer.Core.Models;

namespace TextReplacer.Core.Services;

/// <summary>
/// Interface for document processors that can extract placeholders and perform replacements.
/// </summary>
public interface IDocumentProcessor
{
    /// <summary>
    /// Extracts all placeholders from a template document.
    /// </summary>
    /// <param name="templatePath">Path to the template file.</param>
    /// <param name="config">Placeholder configuration.</param>
    /// <returns>Set of unique placeholder names (without prefix/suffix).</returns>
    IReadOnlySet<string> ExtractPlaceholders(string templatePath, PlaceholderConfig config);

    /// <summary>
    /// Processes a template document, replacing placeholders with values from the provided dictionary.
    /// </summary>
    /// <param name="templatePath">Path to the template file.</param>
    /// <param name="outputPath">Path where the processed file will be saved.</param>
    /// <param name="replacements">Dictionary mapping placeholder names to replacement values.</param>
    /// <param name="config">Placeholder configuration.</param>
    void Process(string templatePath, string outputPath, IDictionary<string, string> replacements, PlaceholderConfig config);
}
