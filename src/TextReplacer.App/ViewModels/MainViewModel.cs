using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using TextReplacer.Core.Models;
using TextReplacer.Core.Processing;
using TextReplacer.Core.Services;

namespace TextReplacer.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly JobRunner _jobRunner = new();
    private readonly PlaceholderFinder _placeholderFinder = new();
    private CancellationTokenSource? _cancellationTokenSource;

    // File paths
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ValidateCommand))]
    [NotifyCanExecuteChangedFor(nameof(ProcessCommand))]
    private string _templatePath = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ValidateCommand))]
    [NotifyCanExecuteChangedFor(nameof(ProcessCommand))]
    private string _csvPath = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ProcessCommand))]
    private string _outputDirectory = string.Empty;

    // Placeholder settings
    [ObservableProperty]
    private string _placeholderPrefix = "[";

    [ObservableProperty]
    private string _placeholderSuffix = "]";

    // Output naming
    [ObservableProperty]
    private bool _usePatternNaming = false;

    [ObservableProperty]
    private string _namingPattern = "{Location}_{Equipment}";

    [ObservableProperty]
    private string _sequentialPrefix = "output";

    // Validation results
    [ObservableProperty]
    private bool _isValidated = false;

    [ObservableProperty]
    private int _placeholderCount = 0;

    [ObservableProperty]
    private int _matchedCount = 0;

    [ObservableProperty]
    private int _missingCount = 0;

    [ObservableProperty]
    private int _unusedCount = 0;

    public ObservableCollection<string> MissingPlaceholders { get; } = new();
    public ObservableCollection<string> UnusedColumns { get; } = new();

    // Available CSV columns for output naming pattern
    public ObservableCollection<string> CsvColumns { get; } = new();

    // Progress
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ValidateCommand))]
    [NotifyCanExecuteChangedFor(nameof(ProcessCommand))]
    [NotifyCanExecuteChangedFor(nameof(CancelCommand))]
    private bool _isProcessing = false;

    [ObservableProperty]
    private double _progressValue = 0;

    [ObservableProperty]
    private string _progressText = string.Empty;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    // Commands
    [RelayCommand]
    private void BrowseTemplate()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Office Documents|*.docx;*.xlsx|Word Documents|*.docx|Excel Workbooks|*.xlsx",
            Title = "Select Template Document"
        };

        if (dialog.ShowDialog() == true)
        {
            TemplatePath = dialog.FileName;
            IsValidated = false;
            StatusMessage = "Template selected. Click Validate to check placeholders.";
        }
    }

    [RelayCommand]
    private void BrowseCsv()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "CSV Files|*.csv|All Files|*.*",
            Title = "Select CSV Data File"
        };

        if (dialog.ShowDialog() == true)
        {
            CsvPath = dialog.FileName;
            IsValidated = false;
            LoadCsvColumns();
            StatusMessage = "CSV selected. Click Validate to check placeholders.";
        }
    }

    private void LoadCsvColumns()
    {
        CsvColumns.Clear();

        if (string.IsNullOrWhiteSpace(CsvPath) || !File.Exists(CsvPath))
            return;

        try
        {
            using var csvReader = new CsvReaderService(CsvPath);
            foreach (var header in csvReader.GetHeaders())
            {
                CsvColumns.Add(header);
            }
        }
        catch
        {
            // Silently fail - columns just won't be shown
        }
    }

    [RelayCommand]
    private void BrowseOutput()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Select Output Directory"
        };

        if (dialog.ShowDialog() == true)
        {
            OutputDirectory = dialog.FolderName;
        }
    }

    private bool CanValidate() =>
        !IsProcessing &&
        !string.IsNullOrWhiteSpace(TemplatePath) &&
        !string.IsNullOrWhiteSpace(CsvPath) &&
        File.Exists(TemplatePath) &&
        File.Exists(CsvPath);

    [RelayCommand(CanExecute = nameof(CanValidate))]
    private async Task ValidateAsync()
    {
        try
        {
            IsProcessing = true;
            StatusMessage = "Validating...";

            var config = new PlaceholderConfig
            {
                Prefix = PlaceholderPrefix,
                Suffix = PlaceholderSuffix
            };

            var result = await Task.Run(() =>
                _placeholderFinder.ValidateTemplate(TemplatePath, CsvPath, config));

            PlaceholderCount = result.PlaceholdersInTemplate.Count;
            MatchedCount = result.MatchedPlaceholders.Count;
            MissingCount = result.MissingColumns.Count;
            UnusedCount = result.UnusedColumns.Count;

            MissingPlaceholders.Clear();
            foreach (var p in result.MissingColumns)
                MissingPlaceholders.Add(p);

            UnusedColumns.Clear();
            foreach (var c in result.UnusedColumns)
                UnusedColumns.Add(c);

            IsValidated = true;

            if (result.IsPerfectMatch)
            {
                StatusMessage = "Validation passed! All placeholders matched.";
            }
            else if (result.IsFullyMatched)
            {
                StatusMessage = $"Validation passed with {UnusedCount} unused CSV columns.";
            }
            else
            {
                StatusMessage = $"Warning: {MissingCount} placeholders have no matching CSV column.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Validation failed: {ex.Message}";
            IsValidated = false;
        }
        finally
        {
            IsProcessing = false;
        }
    }

    private bool CanProcess() =>
        !IsProcessing &&
        !string.IsNullOrWhiteSpace(TemplatePath) &&
        !string.IsNullOrWhiteSpace(CsvPath) &&
        !string.IsNullOrWhiteSpace(OutputDirectory) &&
        File.Exists(TemplatePath) &&
        File.Exists(CsvPath);

    [RelayCommand(CanExecute = nameof(CanProcess))]
    private async Task ProcessAsync()
    {
        try
        {
            IsProcessing = true;
            ProgressValue = 0;
            ProgressText = "Starting...";
            StatusMessage = "Processing documents...";

            _cancellationTokenSource = new CancellationTokenSource();

            var job = new ReplacementJob
            {
                TemplatePath = TemplatePath,
                CsvPath = CsvPath,
                OutputDirectory = OutputDirectory,
                PlaceholderConfig = new PlaceholderConfig
                {
                    Prefix = PlaceholderPrefix,
                    Suffix = PlaceholderSuffix
                },
                OutputNamingConfig = new OutputNamingConfig
                {
                    UsePattern = UsePatternNaming,
                    Pattern = NamingPattern,
                    SequentialPrefix = SequentialPrefix
                }
            };

            var progress = new Progress<ProgressInfo>(info =>
            {
                ProgressValue = info.PercentComplete;
                ProgressText = $"{info.ProcessedCount} / {info.TotalCount}";
                if (!string.IsNullOrEmpty(info.CurrentFileName))
                {
                    StatusMessage = $"Processing: {info.CurrentFileName}";
                }
            });

            var outputFiles = await _jobRunner.RunAsync(job, progress, _cancellationTokenSource.Token);

            ProgressValue = 100;
            ProgressText = $"{outputFiles.Count} / {outputFiles.Count}";
            StatusMessage = $"Complete! Generated {outputFiles.Count} files in {OutputDirectory}";

            // Offer to open output folder
            var result = MessageBox.Show(
                $"Successfully generated {outputFiles.Count} documents.\n\nOpen output folder?",
                "Processing Complete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);

            if (result == MessageBoxResult.Yes)
            {
                System.Diagnostics.Process.Start("explorer.exe", OutputDirectory);
            }
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Processing cancelled.";
            ProgressText = "Cancelled";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            MessageBox.Show($"Processing failed:\n\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsProcessing = false;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }

    private bool CanCancel() => IsProcessing;

    [RelayCommand(CanExecute = nameof(CanCancel))]
    private void Cancel()
    {
        _cancellationTokenSource?.Cancel();
        StatusMessage = "Cancelling...";
    }
}
