using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;

namespace TextReplacer.Core.Services;

/// <summary>
/// Reads CSV files and provides access to headers and row data.
/// </summary>
public class CsvReaderService : IDisposable
{
    private readonly string _filePath;
    private readonly CsvConfiguration _config;
    private string[]? _headers;
    private List<Dictionary<string, string>>? _cachedRows;

    public CsvReaderService(string filePath)
    {
        _filePath = filePath;
        _config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.Trim,
            MissingFieldFound = null, // Don't throw on missing fields
            BadDataFound = null // Handle malformed data gracefully
        };
    }

    /// <summary>
    /// Gets the column headers from the CSV file.
    /// </summary>
    public IReadOnlyList<string> GetHeaders()
    {
        EnsureHeadersLoaded();
        return _headers!;
    }

    /// <summary>
    /// Gets the total number of data rows (excluding header).
    /// </summary>
    public int RowCount
    {
        get
        {
            EnsureRowsCached();
            return _cachedRows!.Count;
        }
    }

    /// <summary>
    /// Reads all rows from the CSV file as dictionaries mapping column name to value.
    /// </summary>
    public IEnumerable<IDictionary<string, string>> ReadRows()
    {
        EnsureRowsCached();
        return _cachedRows!;
    }

    /// <summary>
    /// Gets a specific row by index (0-based).
    /// </summary>
    public IDictionary<string, string>? GetRow(int index)
    {
        EnsureRowsCached();
        if (index < 0 || index >= _cachedRows!.Count)
            return null;
        return _cachedRows[index];
    }

    private void EnsureHeadersLoaded()
    {
        if (_headers != null) return;

        using var reader = new StreamReader(_filePath);
        using var csv = new CsvReader(reader, _config);

        if (csv.Read() && csv.ReadHeader())
        {
            _headers = csv.HeaderRecord ?? Array.Empty<string>();
        }
        else
        {
            _headers = Array.Empty<string>();
        }
    }

    private void EnsureRowsCached()
    {
        if (_cachedRows != null) return;

        _cachedRows = new List<Dictionary<string, string>>();
        EnsureHeadersLoaded();

        using var reader = new StreamReader(_filePath);
        using var csv = new CsvReader(reader, _config);

        csv.Read();
        csv.ReadHeader();

        while (csv.Read())
        {
            var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var header in _headers!)
            {
                var value = csv.GetField(header) ?? string.Empty;
                row[header] = value;
            }
            _cachedRows.Add(row);
        }
    }

    public void Dispose()
    {
        // Nothing to dispose currently, but keeping for future streaming support
    }
}
