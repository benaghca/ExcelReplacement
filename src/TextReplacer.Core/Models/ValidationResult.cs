namespace TextReplacer.Core.Models;

/// <summary>
/// Results of validating a template against CSV columns.
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// All placeholders found in the template document.
    /// </summary>
    public IReadOnlyList<string> PlaceholdersInTemplate { get; }

    /// <summary>
    /// All column headers found in the CSV file.
    /// </summary>
    public IReadOnlyList<string> ColumnsInCsv { get; }

    /// <summary>
    /// CSV columns that are not used by any placeholder in the template.
    /// </summary>
    public IReadOnlyList<string> UnusedColumns { get; }

    /// <summary>
    /// Placeholders in the template that have no matching CSV column.
    /// </summary>
    public IReadOnlyList<string> MissingColumns { get; }

    /// <summary>
    /// Placeholders that have matching CSV columns.
    /// </summary>
    public IReadOnlyList<string> MatchedPlaceholders { get; }

    /// <summary>
    /// Whether all placeholders have matching CSV columns.
    /// </summary>
    public bool IsFullyMatched => MissingColumns.Count == 0;

    /// <summary>
    /// Whether validation passed with no issues (all matched, no unused).
    /// </summary>
    public bool IsPerfectMatch => MissingColumns.Count == 0 && UnusedColumns.Count == 0;

    public ValidationResult(
        IEnumerable<string> placeholders,
        IEnumerable<string> csvColumns)
    {
        var placeholderSet = placeholders.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var columnSet = csvColumns.ToHashSet(StringComparer.OrdinalIgnoreCase);

        PlaceholdersInTemplate = placeholderSet.ToList().AsReadOnly();
        ColumnsInCsv = columnSet.ToList().AsReadOnly();

        MatchedPlaceholders = placeholderSet
            .Where(p => columnSet.Contains(p))
            .ToList()
            .AsReadOnly();

        MissingColumns = placeholderSet
            .Where(p => !columnSet.Contains(p))
            .ToList()
            .AsReadOnly();

        UnusedColumns = columnSet
            .Where(c => !placeholderSet.Contains(c))
            .ToList()
            .AsReadOnly();
    }
}
