namespace TextReplacer.Core.Models;

/// <summary>
/// Progress information for reporting job status.
/// </summary>
public class ProgressInfo
{
    /// <summary>
    /// Number of documents processed so far.
    /// </summary>
    public int ProcessedCount { get; }

    /// <summary>
    /// Total number of documents to process.
    /// </summary>
    public int TotalCount { get; }

    /// <summary>
    /// Name of the current file being processed.
    /// </summary>
    public string CurrentFileName { get; }

    /// <summary>
    /// Progress as a percentage (0-100).
    /// </summary>
    public double PercentComplete => TotalCount > 0 ? (double)ProcessedCount / TotalCount * 100 : 0;

    /// <summary>
    /// Whether the job is complete.
    /// </summary>
    public bool IsComplete => ProcessedCount >= TotalCount;

    public ProgressInfo(int processed, int total, string currentFileName = "")
    {
        ProcessedCount = processed;
        TotalCount = total;
        CurrentFileName = currentFileName;
    }
}
