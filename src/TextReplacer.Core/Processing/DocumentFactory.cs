using TextReplacer.Core.Services;

namespace TextReplacer.Core.Processing;

/// <summary>
/// Factory for creating the appropriate document processor based on file extension.
/// </summary>
public static class DocumentFactory
{
    /// <summary>
    /// Creates a document processor for the given file extension.
    /// </summary>
    /// <param name="extension">File extension including the dot (e.g., ".docx").</param>
    /// <returns>The appropriate document processor.</returns>
    public static IDocumentProcessor Create(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".docx" => new WordProcessor(),
            ".xlsx" => new ExcelProcessor(),
            _ => throw new NotSupportedException($"File type '{extension}' is not supported. Use .docx or .xlsx files.")
        };
    }

    /// <summary>
    /// Checks if a file extension is supported.
    /// </summary>
    public static bool IsSupported(string extension)
    {
        return extension.ToLowerInvariant() is ".docx" or ".xlsx";
    }
}
