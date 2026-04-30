using TextReplacer.Core.Models;
using TextReplacer.Core.Services;

namespace TextReplacer.Core.Processing;

/// <summary>
/// Orchestrates the complete replacement job: reads CSV, processes each row, generates output files.
/// </summary>
public class JobRunner
{
    /// <summary>
    /// Runs the replacement job asynchronously with progress reporting.
    /// </summary>
    /// <param name="job">The replacement job configuration.</param>
    /// <param name="progress">Progress reporter for UI updates.</param>
    /// <param name="cancellationToken">Cancellation token to abort the job.</param>
    /// <returns>List of generated output file paths.</returns>
    public async Task<IReadOnlyList<string>> RunAsync(
        ReplacementJob job,
        IProgress<ProgressInfo>? progress = null,
        CancellationToken cancellationToken = default)
    {
        // Ensure output directory exists
        Directory.CreateDirectory(job.OutputDirectory);

        var processor = DocumentFactory.Create(job.TemplateExtension);
        using var csvReader = new CsvReaderService(job.CsvPath);

        var rows = csvReader.ReadRows().ToList();
        var totalCount = rows.Count;
        var outputFiles = new List<string>();

        for (int i = 0; i < rows.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var row = rows[i];

            // Generate output filename
            var baseName = job.OutputNamingConfig.GenerateFileName(row, i);
            var extension = job.TemplateExtension;
            var outputFileName = baseName + extension;
            var outputPath = Path.Combine(job.OutputDirectory, outputFileName);

            // Report progress before processing
            progress?.Report(new ProgressInfo(i, totalCount, outputFileName));

            // Process the document
            await Task.Run(() =>
            {
                processor.Process(job.TemplatePath, outputPath, row, job.PlaceholderConfig);

                if (job.ExportAsPdf)
                    outputPath = PdfExporter.ConvertToPdf(outputPath);
            }, cancellationToken);

            outputFiles.Add(outputPath);

            // Report progress after processing
            progress?.Report(new ProgressInfo(i + 1, totalCount, outputFileName));
        }

        return outputFiles.AsReadOnly();
    }

    /// <summary>
    /// Validates the job configuration before running.
    /// </summary>
    /// <returns>Validation result.</returns>
    public ValidationResult Validate(ReplacementJob job)
    {
        var finder = new PlaceholderFinder();
        return finder.ValidateTemplate(job.TemplatePath, job.CsvPath, job.PlaceholderConfig);
    }
}
