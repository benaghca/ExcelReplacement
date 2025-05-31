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
using System.Drawing;
using ExcelReplacement.Forms;

namespace ExcelReplacement
{
    internal class Program
    {
        private static readonly CsvService _csvService = new();
        private static readonly ExcelService _excelService = new();
        private static readonly WordService _wordService = new();

        [STAThread]
        static void Main()
        {
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}\n\nPlease check your input files and try again.", 
                              "Error", 
                              MessageBoxButtons.OK, 
                              MessageBoxIcon.Error);
            }
        }
    }

    public class MainForm : Form
    {
        private readonly CsvService _csvService = new();
        private readonly ExcelService _excelService = new();
        private readonly WordService _wordService = new();
        private TextBox csvPathTextBox;
        private TextBox templatePathTextBox;
        private TextBox outputDirTextBox;
        private Button browseCsvButton;
        private Button browseTemplateButton;
        private Button browseOutputButton;
        private Button processButton;
        private ProgressForm progressForm;
        private ComboBox templateTypeComboBox;
        private Label statusLabel;
        private Button configureFileNameButton;
        private Button previewButton;
        private string fileNamePattern = "[Location] [Facility] [Equipment] [Procedure]";

        public MainForm()
        {
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            this.Text = "Document Template Processor";
            this.Size = new Size(650, 450);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MaximizeBox = false;

            // Template Type Selection
            var templateTypeLabel = new Label
            {
                Text = "Template Type:",
                Location = new Point(30, 30),
                AutoSize = true
            };

            templateTypeComboBox = new ComboBox
            {
                Location = new Point(130, 30),
                Width = 200,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            templateTypeComboBox.Items.AddRange(new object[] { "Excel", "Word" });
            templateTypeComboBox.SelectedIndex = 0;
            templateTypeComboBox.SelectedIndexChanged += TemplateTypeComboBox_SelectedIndexChanged;

            // CSV File Selection
            var csvLabel = new Label
            {
                Text = "CSV File:",
                Location = new Point(30, 70),
                AutoSize = true
            };

            csvPathTextBox = new TextBox
            {
                Location = new Point(130, 70),
                Width = 400,
                ReadOnly = true
            };

            browseCsvButton = new Button
            {
                Text = "Browse...",
                Location = new Point(540, 70),
                Width = 70
            };
            browseCsvButton.Click += BrowseCsvButton_Click;

            // Template File Selection
            var templateLabel = new Label
            {
                Text = "Template File:",
                Location = new Point(30, 110),
                AutoSize = true
            };

            templatePathTextBox = new TextBox
            {
                Location = new Point(130, 110),
                Width = 400,
                ReadOnly = true
            };

            browseTemplateButton = new Button
            {
                Text = "Browse...",
                Location = new Point(540, 110),
                Width = 70
            };
            browseTemplateButton.Click += BrowseTemplateButton_Click;

            // Output Directory Selection
            var outputLabel = new Label
            {
                Text = "Output Directory:",
                Location = new Point(30, 150),
                AutoSize = true
            };

            outputDirTextBox = new TextBox
            {
                Location = new Point(130, 150),
                Width = 400,
                ReadOnly = true
            };

            browseOutputButton = new Button
            {
                Text = "Browse...",
                Location = new Point(540, 150),
                Width = 70
            };
            browseOutputButton.Click += BrowseOutputButton_Click;

            // Configure File Name Button
            configureFileNameButton = new Button
            {
                Text = "Configure File Name Pattern",
                Location = new Point(30, 200),
                Size = new Size(200, 30)
            };
            configureFileNameButton.Click += ConfigureFileNameButton_Click;

            // Preview Button
            previewButton = new Button
            {
                Text = "Preview Template",
                Location = new Point(240, 200),
                Size = new Size(120, 30)
            };
            previewButton.Click += PreviewButton_Click;

            // Process Button
            processButton = new Button
            {
                Text = "Process Files",
                Location = new Point(260, 250),
                Size = new Size(100, 30)
            };
            processButton.Click += ProcessButton_Click;

            // Status Label
            statusLabel = new Label
            {
                Text = "Ready",
                Location = new Point(30, 300),
                AutoSize = true
            };

            // Add controls to form
            this.Controls.AddRange(new System.Windows.Forms.Control[] {
                templateTypeLabel,
                templateTypeComboBox,
                csvLabel,
                csvPathTextBox,
                browseCsvButton,
                templateLabel,
                templatePathTextBox,
                browseTemplateButton,
                outputLabel,
                outputDirTextBox,
                browseOutputButton,
                configureFileNameButton,
                previewButton,
                processButton,
                statusLabel
            });
        }

        private void TemplateTypeComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateTemplateFileFilter();
        }

        private void UpdateTemplateFileFilter()
        {
            string filter = templateTypeComboBox.SelectedItem.ToString() == "Excel"
                ? "Excel files (*.xlsx)|*.xlsx|All files (*.*)|*.*"
                : "Word files (*.docx)|*.docx|All files (*.*)|*.*";
            
            templatePathTextBox.Text = string.Empty;
        }

        private void BrowseCsvButton_Click(object sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*";
                dialog.Title = "Select CSV file with replacement data";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    csvPathTextBox.Text = dialog.FileName;
                }
            }
        }

        private void BrowseTemplateButton_Click(object sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Filter = templateTypeComboBox.SelectedItem.ToString() == "Excel"
                    ? "Excel files (*.xlsx)|*.xlsx|All files (*.*)|*.*"
                    : "Word files (*.docx)|*.docx|All files (*.*)|*.*";
                dialog.Title = "Select template file";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    templatePathTextBox.Text = dialog.FileName;
                }
            }
        }

        private void BrowseOutputButton_Click(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select directory for output files";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    outputDirTextBox.Text = dialog.SelectedPath;
                }
            }
        }

        private void ConfigureFileNameButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(csvPathTextBox.Text))
            {
                MessageBox.Show("Please select a CSV file first to see available placeholders.",
                              "No CSV File Selected",
                              MessageBoxButtons.OK,
                              MessageBoxIcon.Warning);
                return;
            }

            try
            {
                var records = _csvService.LoadCsvData(csvPathTextBox.Text);
                if (records.Count == 0)
                {
                    MessageBox.Show("The selected CSV file contains no records.",
                                  "Empty CSV File",
                                  MessageBoxButtons.OK,
                                  MessageBoxIcon.Warning);
                    return;
                }

                var placeholders = records[0].Values.Keys.ToList();
                using (var configForm = new FileNameConfigForm(placeholders, fileNamePattern))
                {
                    if (configForm.ShowDialog() == DialogResult.OK)
                    {
                        fileNamePattern = configForm.FileNamePattern;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading CSV file: {ex.Message}",
                              "Error",
                              MessageBoxButtons.OK,
                              MessageBoxIcon.Error);
            }
        }

        private void PreviewButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(csvPathTextBox.Text) ||
                string.IsNullOrEmpty(templatePathTextBox.Text))
            {
                MessageBox.Show("Please select both CSV and template files.", "Missing Information",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                var previewForm = new PreviewForm();
                previewForm.LoadPreview(
                    templatePathTextBox.Text,
                    csvPathTextBox.Text,
                    templateTypeComboBox.SelectedItem.ToString() == "Excel"
                );
                previewForm.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating preview: {ex.Message}", "Error",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void ProcessButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(csvPathTextBox.Text) ||
                string.IsNullOrEmpty(templatePathTextBox.Text) ||
                string.IsNullOrEmpty(outputDirTextBox.Text))
            {
                MessageBox.Show("Please select all required files and directories.", "Missing Information",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                processButton.Enabled = false;
                statusLabel.Text = "Processing...";

                var records = _csvService.LoadCsvData(csvPathTextBox.Text);
                progressForm = new ProgressForm();
                progressForm.Show();

                await System.Threading.Tasks.Task.Run(() =>
                {
                    ProcessFiles(records, templatePathTextBox.Text, outputDirTextBox.Text);
                });

                progressForm.ShowSummary($"Successfully processed {records.Count} records");
                statusLabel.Text = "Complete!";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                statusLabel.Text = "Error occurred";
            }
            finally
            {
                processButton.Enabled = true;
            }
        }

        private void ProcessFiles(List<ExcelRecord> records, string templatePath, string outputDir)
        {
            EnsureOutputDirectoryExists(outputDir);
            var isExcel = templateTypeComboBox.SelectedItem.ToString() == "Excel";
            var extension = isExcel ? ".xlsx" : ".docx";

            for (int i = 0; i < records.Count; i++)
            {
                var record = records[i];
                var outputPath = Path.Combine(outputDir, record.GenerateFileName(fileNamePattern, extension));
                
                progressForm.UpdateProgress(i + 1, records.Count, record.GenerateFileName(fileNamePattern, extension));

                if (isExcel)
                {
                    _excelService.ProcessRecord(record, templatePath, outputPath);
                }
                else
                {
                    _wordService.ProcessRecord(record, templatePath, outputPath);
                }
            }
        }

        private void EnsureOutputDirectoryExists(string outputDir)
        {
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }
        }
    }

    public class ProgressForm : Form
    {
        private Label statusLabel;
        private ProgressBar progressBar;
        private Label currentFileLabel;
        private Button doneButton;

        public ProgressForm()
        {
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            this.Text = "Processing Progress";
            this.Size = new Size(400, 180);  // Made slightly taller for the button
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.ControlBox = false;

            statusLabel = new Label
            {
                Text = "Processing files...",
                Location = new Point(10, 10),
                AutoSize = true
            };

            progressBar = new ProgressBar
            {
                Location = new Point(10, 40),
                Width = 360
            };

            currentFileLabel = new Label
            {
                Location = new Point(10, 70),
                AutoSize = true
            };

            doneButton = new Button
            {
                Text = "Done",
                Location = new Point(150, 110),
                Size = new Size(80, 30),
                Visible = false
            };
            doneButton.Click += (s, e) => this.Close();

            this.Controls.AddRange(new System.Windows.Forms.Control[] { 
                statusLabel, 
                progressBar, 
                currentFileLabel,
                doneButton 
            });
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
            doneButton.Visible = true;
        }
    }
}