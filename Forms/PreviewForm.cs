using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ExcelReplacement.Models;
using ExcelReplacement.Services;

namespace ExcelReplacement.Forms
{
    public class PreviewForm : Form
    {
        private readonly TabControl _tabControl;
        private readonly TabPage _validationTab;
        private readonly TabPage _previewTab;
        private readonly DataGridView _placeholdersGrid;
        private readonly RichTextBox _previewTextBox;
        private readonly Button _closeButton;
        private readonly Label _statusLabel;

        public PreviewForm()
        {
            this.Text = "Template Preview and Validation";
            this.Size = new Size(800, 600);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MaximizeBox = false;

            // Initialize components
            _tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Padding = new Point(10, 10)
            };

            _validationTab = new TabPage("Validation");
            _previewTab = new TabPage("Preview");

            _placeholdersGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            _previewTextBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                Font = new Font("Consolas", 10)
            };

            _closeButton = new Button
            {
                Text = "Close",
                DialogResult = DialogResult.OK,
                Dock = DockStyle.Bottom,
                Height = 40
            };

            _statusLabel = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 30,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0)
            };

            // Setup grid columns
            _placeholdersGrid.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn { Name = "Placeholder", HeaderText = "Placeholder" },
                new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Status" },
                new DataGridViewTextBoxColumn { Name = "Message", HeaderText = "Message" },
                new DataGridViewTextBoxColumn { Name = "SampleValue", HeaderText = "Sample Value" },
                new DataGridViewTextBoxColumn { Name = "Location", HeaderText = "Location" }
            });

            // Add controls to form
            _validationTab.Controls.Add(_placeholdersGrid);
            _previewTab.Controls.Add(_previewTextBox);
            _tabControl.TabPages.AddRange(new TabPage[] { _validationTab, _previewTab });

            this.Controls.AddRange(new Control[] { _tabControl, _statusLabel, _closeButton });
        }

        public void LoadPreview(string templatePath, string csvPath, bool isExcel)
        {
            try
            {
                var csvService = new Services.CsvService();
                var records = csvService.LoadCsvData(csvPath);
                if (records.Count == 0)
                {
                    throw new Exception("No records found in CSV file.");
                }

                var firstRecord = records[0];
                var templateValidator = new Services.TemplateValidator(templatePath, isExcel);
                var validationResults = templateValidator.ValidateTemplate(firstRecord);

                // Update validation grid
                _placeholdersGrid.Rows.Clear();
                foreach (var result in validationResults)
                {
                    var row = _placeholdersGrid.Rows[_placeholdersGrid.Rows.Add(
                        result.Placeholder,
                        result.Status.ToString(),
                        result.Message,
                        result.SampleValue,
                        result.Location
                    )];

                    // Apply color based on status
                    switch (result.Status)
                    {
                        case Services.ValidationStatus.Success:
                            row.DefaultCellStyle.BackColor = Color.LightGreen;
                            break;
                        case Services.ValidationStatus.Warning:
                            row.DefaultCellStyle.BackColor = Color.LightYellow;
                            break;
                        case Services.ValidationStatus.Error:
                            row.DefaultCellStyle.BackColor = Color.Salmon;
                            break;
                        case Services.ValidationStatus.CsvColumnUnused:
                            row.DefaultCellStyle.BackColor = Color.LightBlue;
                            break;
                    }
                }

                // Update preview
                var previewService = new Services.PreviewService();
                var preview = previewService.GeneratePreview(templatePath, firstRecord, isExcel);
                _previewTextBox.Text = preview;

                // Update status
                var errorCount = validationResults.Count(r => r.Status.ToString() == "Error");
                var warningCount = validationResults.Count(r => r.Status.ToString() == "Warning");
                _statusLabel.Text = $"Found {errorCount} errors and {warningCount} warnings.";
                _statusLabel.ForeColor = errorCount > 0 ? Color.Red : warningCount > 0 ? Color.Orange : Color.Green;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading preview: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            }
        }
    }
} 