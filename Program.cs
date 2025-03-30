using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using CsvHelper;
using CsvHelper.Configuration;
using ExcelReplacement.Models;
using ExcelReplacement.Services;
using System.Windows.Forms;

namespace ExcelReplacement
{
    internal class Program
    {
        private static readonly CsvService _csvService = new();
        private static readonly ExcelService _excelService = new();

        [STAThread]
        static void Main()
        {
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                var (csvPath, templatePath, outputDir) = GetFilesFromUser();
                if (csvPath == null || templatePath == null || outputDir == null)
                {
                    return;
                }

                ProcessFiles(csvPath, templatePath, outputDir);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}\n\nPlease check your input files and try again.", 
                              "Error", 
                              MessageBoxButtons.OK, 
                              MessageBoxIcon.Error);
            }
        }

        private static (string csvPath, string templatePath, string outputDir) GetFilesFromUser()
        {
            string csvPath = null;
            string templatePath = null;
            string outputDir = null;

            // Select CSV file
            using (var dialog = new OpenFileDialog())
            {
                dialog.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*";
                dialog.Title = "Select CSV file with replacement data";
                if (dialog.ShowDialog() != DialogResult.OK)
                {
                    return (null, null, null);
                }
                csvPath = dialog.FileName;
            }

            // Select Excel template
            using (var dialog = new OpenFileDialog())
            {
                dialog.Filter = "Excel files (*.xlsx)|*.xlsx|All files (*.*)|*.*";
                dialog.Title = "Select Excel template file";
                if (dialog.ShowDialog() != DialogResult.OK)
                {
                    return (null, null, null);
                }
                templatePath = dialog.FileName;
            }

            // Select output directory
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select directory for output files";
                if (dialog.ShowDialog() != DialogResult.OK)
                {
                    return (null, null, null);
                }
                outputDir = dialog.SelectedPath;
            }

            return (csvPath, templatePath, outputDir);
        }

        private static void ProcessFiles(string csvPath, string templatePath, string outputDir)
        {
            var progressForm = new ProgressForm();
            progressForm.Show();

            EnsureOutputDirectoryExists(outputDir);
            var records = LoadCsvData(csvPath);
            ProcessRecords(records, templatePath, outputDir, progressForm);
            PrintSummary(records.Count, progressForm);
        }

        private static void EnsureOutputDirectoryExists(string outputDir)
        {
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }
        }

        private static List<ExcelRecord> LoadCsvData(string csvPath)
        {
            return _csvService.LoadCsvData(csvPath);
        }

        private static void ProcessRecords(List<ExcelRecord> records, string templatePath, string outputDir, ProgressForm progressForm)
        {
            for (int i = 0; i < records.Count; i++)
            {
                var record = records[i];
                var outputPath = Path.Combine(outputDir, record.GenerateFileName());
                
                progressForm.UpdateProgress(i + 1, records.Count, record.GenerateFileName());
                _excelService.ProcessRecord(record, templatePath, outputPath);
            }
        }

        private static void PrintSummary(int recordCount, ProgressForm progressForm)
        {
            progressForm.ShowSummary($"Successfully processed {recordCount} records");
        }
    }

    public class ProgressForm : Form
    {
        private Label statusLabel;
        private ProgressBar progressBar;
        private Label currentFileLabel;

        public ProgressForm()
        {
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            this.Text = "Excel Replacement Progress";
            this.Size = new System.Drawing.Size(400, 150);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.ControlBox = false;

            statusLabel = new Label
            {
                Text = "Processing files...",
                Location = new System.Drawing.Point(10, 10),
                AutoSize = true
            };

            progressBar = new ProgressBar
            {
                Location = new System.Drawing.Point(10, 40),
                Width = 360
            };

            currentFileLabel = new Label
            {
                Location = new System.Drawing.Point(10, 70),
                AutoSize = true
            };

            this.Controls.AddRange(new System.Windows.Forms.Control[] { statusLabel, progressBar, currentFileLabel });
        }

        public void UpdateProgress(int current, int total, string currentFile)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => UpdateProgress(current, total, currentFile)));
                return;
            }

            progressBar.Maximum = total;
            progressBar.Value = current;
            currentFileLabel.Text = $"Processing: {currentFile}";
        }

        public void ShowSummary(string message)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => ShowSummary(message)));
                return;
            }

            statusLabel.Text = "Complete!";
            currentFileLabel.Text = message;
            progressBar.Value = progressBar.Maximum;
        }
    }
}