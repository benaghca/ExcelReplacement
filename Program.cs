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
using System.Threading.Tasks;

namespace ExcelReplacement
{
    public static class ThemeManager
    {
        // Classic terminal colors with improved readability
        public static System.Drawing.Color BackgroundColor = System.Drawing.Color.FromArgb(0, 0, 0); // Pure black background
        public static System.Drawing.Color ForegroundColor = System.Drawing.Color.FromArgb(0, 255, 128); // Brighter green for better readability
        public static System.Drawing.Color AccentColor = System.Drawing.Color.FromArgb(0, 255, 192); // Even brighter green for accents
        public static System.Drawing.Color ControlBackgroundColor = System.Drawing.Color.FromArgb(0, 0, 0); // Keep controls black
        public static System.Drawing.Color BorderColor = System.Drawing.Color.FromArgb(0, 192, 0); // Brighter green for borders
        public static System.Drawing.Color GridBackgroundColor = System.Drawing.Color.FromArgb(0, 0, 0); // Black grid background
        public static System.Drawing.Color GridHeaderColor = System.Drawing.Color.FromArgb(0, 96, 0); // Darker green for headers
        public static System.Drawing.Color SelectionColor = System.Drawing.Color.FromArgb(0, 192, 0); // Brighter green for selection

        // Status colors in terminal style with improved contrast
        public static System.Drawing.Color SuccessColor = System.Drawing.Color.FromArgb(0, 255, 128); // Bright green
        public static System.Drawing.Color WarningColor = System.Drawing.Color.FromArgb(255, 255, 0); // Yellow
        public static System.Drawing.Color ErrorColor = System.Drawing.Color.FromArgb(255, 64, 64); // Softer red
        public static System.Drawing.Color InfoColor = System.Drawing.Color.FromArgb(64, 192, 255); // Softer blue for info

        // Hover and active states with better visibility
        public static System.Drawing.Color HoverColor = System.Drawing.Color.FromArgb(0, 96, 0); // Dark green
        public static System.Drawing.Color ActiveColor = System.Drawing.Color.FromArgb(0, 128, 0); // Medium green

        public static void ApplyTheme(System.Windows.Forms.Form form)
        {
            form.BackColor = BackgroundColor;
            form.ForeColor = ForegroundColor;

            foreach (System.Windows.Forms.Control control in form.Controls)
            {
                ApplyThemeToControl(control);
            }
        }

        private static void ApplyThemeToControl(System.Windows.Forms.Control control)
        {
            if (control is System.Windows.Forms.Button button)
            {
                button.BackColor = ControlBackgroundColor;
                button.ForeColor = ForegroundColor;
                button.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
                button.FlatAppearance.BorderColor = BorderColor;
                button.FlatAppearance.MouseOverBackColor = HoverColor;
                button.FlatAppearance.MouseDownBackColor = ActiveColor;
                button.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            }
            else if (control is System.Windows.Forms.TextBox textBox)
            {
                textBox.BackColor = ControlBackgroundColor;
                textBox.ForeColor = ForegroundColor;
                textBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
                textBox.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            }
            else if (control is System.Windows.Forms.ComboBox comboBox)
            {
                comboBox.BackColor = ControlBackgroundColor;
                comboBox.ForeColor = ForegroundColor;
                comboBox.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
                comboBox.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            }
            else if (control is System.Windows.Forms.DataGridView grid)
            {
                grid.BackgroundColor = GridBackgroundColor;
                grid.ForeColor = ForegroundColor;
                grid.BorderStyle = System.Windows.Forms.BorderStyle.None;
                grid.ColumnHeadersDefaultCellStyle.BackColor = GridHeaderColor;
                grid.ColumnHeadersDefaultCellStyle.ForeColor = ForegroundColor;
                grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = ActiveColor;
                grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = ForegroundColor;
                grid.DefaultCellStyle.BackColor = GridBackgroundColor;
                grid.DefaultCellStyle.ForeColor = ForegroundColor;
                grid.DefaultCellStyle.SelectionBackColor = GridBackgroundColor;
                grid.DefaultCellStyle.SelectionForeColor = ForegroundColor;
                grid.DefaultCellStyle.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
                grid.ColumnHeadersDefaultCellStyle.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
                grid.EnableHeadersVisualStyles = false;
                
                grid.CellBorderStyle = DataGridViewCellBorderStyle.Single;
                grid.DefaultCellStyle.SelectionBackColor = GridBackgroundColor;
                grid.DefaultCellStyle.SelectionForeColor = ForegroundColor;
                
                grid.CellPainting += (sender, e) =>
                {
                    // Paint row header
                    if (e.RowIndex >= 0 && e.ColumnIndex == -1)
                    {
                        // Manually paint the background with the desired color
                        using (var brush = new System.Drawing.SolidBrush(GridBackgroundColor))
                        {
                            e.Graphics.FillRectangle(brush, e.CellBounds);
                        }

                        // Draw the triangle/arrow for the selected row header
                        if (grid.Rows[e.RowIndex].Selected)
                        {
                            int triangleSize = 6;
                            int x = e.CellBounds.Left + (e.CellBounds.Width - triangleSize) / 2;
                            int y = e.CellBounds.Top + (e.CellBounds.Height - triangleSize) / 2;
                            Point[] trianglePoints = new Point[]
                            {
                                new Point(x, y),
                                new Point(x + triangleSize, y + triangleSize / 2),
                                new Point(x, y + triangleSize)
                            };
                            using (var brush = new System.Drawing.SolidBrush(SelectionColor))
                            {
                                e.Graphics.FillPolygon(brush, trianglePoints);
                            }
                        }

                        // Optionally draw row number if RowHeadersVisible is true
                        // if (grid.RowHeadersVisible)
                        // {
                        //     using (var sf = new StringFormat() { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                        //     using (var brush = new SolidBrush(ForegroundColor))
                        //     {
                        //         e.Graphics.DrawString((e.RowIndex + 1).ToString(), e.CellStyle.Font, brush, e.CellBounds, sf);
                        //     }
                        // }

                        // Draw border for selected row header
                         if (grid.Rows[e.RowIndex].Selected)
                         {
                            using (var pen = new System.Drawing.Pen(SelectionColor, 2))
                            {
                                e.Graphics.DrawRectangle(pen, new System.Drawing.Rectangle(
                                    e.CellBounds.X,
                                    e.CellBounds.Y,
                                    e.CellBounds.Width - 1,
                                    e.CellBounds.Height - 1));
                            }
                         }

                        e.Handled = true;
                    }
                    // Paint data cells
                    else if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
                    {
                        var cell = grid.Rows[e.RowIndex].Cells[e.ColumnIndex];
                        if (cell.Selected)
                        {
                            e.Paint(e.CellBounds, DataGridViewPaintParts.All);
                            using (var pen = new System.Drawing.Pen(SelectionColor, 2))
                            {
                                e.Graphics.DrawRectangle(pen, new System.Drawing.Rectangle(
                                    e.CellBounds.X,
                                    e.CellBounds.Y,
                                    e.CellBounds.Width - 1,
                                    e.CellBounds.Height - 1));
                            }
                            e.Handled = true;
                        }
                    }
                };
            }
            else if (control is System.Windows.Forms.ListView listView)
            {
                listView.BackColor = ControlBackgroundColor;
                listView.ForeColor = ForegroundColor;
                listView.BorderStyle = System.Windows.Forms.BorderStyle.None;
                listView.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            }
            else if (control is System.Windows.Forms.RichTextBox richTextBox)
            {
                richTextBox.BackColor = ControlBackgroundColor;
                richTextBox.ForeColor = ForegroundColor;
                richTextBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
                richTextBox.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            }
            else if (control is System.Windows.Forms.Label label)
            {
                label.ForeColor = ForegroundColor;
                label.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            }
            else if (control is System.Windows.Forms.ProgressBar progressBar)
            {
                progressBar.BackColor = ControlBackgroundColor;
                progressBar.ForeColor = AccentColor;
            }
            else if (control is System.Windows.Forms.TabControl tabControl)
            {
                tabControl.BackColor = BackgroundColor;
                tabControl.ForeColor = ForegroundColor;
                tabControl.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
                
                // Custom drawing for TabControl tabs
                tabControl.DrawMode = TabDrawMode.OwnerDrawFixed;
                tabControl.Padding = new Point(6, 4);
                tabControl.ItemSize = new Size(tabControl.ItemSize.Width, 20);
                tabControl.Appearance = TabAppearance.FlatButtons;
                tabControl.Region = new Region(new RectangleF(0, 0, tabControl.Width, tabControl.Height));

                tabControl.DrawItem += (sender, e) =>
                {
                    var tabControl = (System.Windows.Forms.TabControl)sender;
                    var tabPage = tabControl.TabPages[e.Index];
                    var tabBounds = e.Bounds;

                    // Determine colors based on selection state and theme
                    var backColor = tabControl.SelectedIndex == e.Index ? ActiveColor : BackgroundColor;
                    var foreColor = ForegroundColor;

                    // Draw background
                    using (var brush = new System.Drawing.SolidBrush(backColor))
                    {
                        e.Graphics.FillRectangle(brush, tabBounds);
                    }

                    // Draw tab text
                    System.Windows.Forms.TextRenderer.DrawText(
                        e.Graphics,
                        tabPage.Text,
                        tabControl.Font,
                        tabBounds,
                        foreColor,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter
                    );

                    // Draw border for selected tab
                    if (tabControl.SelectedIndex == e.Index)
                    {
                        using (var pen = new System.Drawing.Pen(SelectionColor, 2))
                        {
                            e.Graphics.DrawRectangle(pen, new System.Drawing.Rectangle(
                                tabBounds.X,
                                tabBounds.Y,
                                tabBounds.Width - 1,
                                tabBounds.Height - 1));
                        }
                    }
                };

                foreach (System.Windows.Forms.TabPage page in tabControl.TabPages)
                {
                    page.BackColor = BackgroundColor;
                    page.ForeColor = ForegroundColor;
                    page.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
                }
            }
            else if (control is System.Windows.Forms.SplitContainer splitContainer)
            {
                splitContainer.BackColor = BackgroundColor;
                splitContainer.Panel1.BackColor = BackgroundColor;
                splitContainer.Panel2.BackColor = BackgroundColor;
            }
            else if (control is System.Windows.Forms.TableLayoutPanel tableLayout)
            {
                tableLayout.BackColor = BackgroundColor;
            }
            else if (control is System.Windows.Forms.FlowLayoutPanel flowLayout)
            {
                flowLayout.BackColor = BackgroundColor;
            }
            else if (control is System.Windows.Forms.Panel panel)
            {
                panel.BackColor = BackgroundColor;
            }

            // Recursively apply theme to child controls
            foreach (System.Windows.Forms.Control childControl in control.Controls)
            {
                ApplyThemeToControl(childControl);
            }
        }
    }

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
                
                // Set console-style icon
                var icon = CreateConsoleIcon();
                
                var mainForm = new MainForm();
                mainForm.Icon = icon;
                ThemeManager.ApplyTheme(mainForm);
                Application.Run(mainForm);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}\n\nPlease check your input files and try again.", 
                              "Error", 
                              MessageBoxButtons.OK, 
                              MessageBoxIcon.Error);
            }
        }

        private static Icon CreateConsoleIcon()
        {
            // Create a 32x32 bitmap for the icon
            using var bitmap = new System.Drawing.Bitmap(32, 32);
            using var g = System.Drawing.Graphics.FromImage(bitmap);
            
            // Fill background
            g.Clear(System.Drawing.Color.Black);
            
            // Draw console-style text
            using var font = new System.Drawing.Font("Consolas", 16, System.Drawing.FontStyle.Bold);
            using var brush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(0, 255, 128)); // Terminal green
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
            g.DrawString(">", font, brush, 4, 4);
            
            // Convert to icon
            var handle = bitmap.GetHicon();
            return System.Drawing.Icon.FromHandle(handle);
        }
    }

    public class MainForm : Form
    {
        private readonly CsvService _csvService = new();
        private readonly ExcelService _excelService = new();
        private readonly WordService _wordService = new();
        private TextBox csvPathTextBox = new();
        private TextBox templatePathTextBox = new();
        private TextBox outputDirTextBox = new();
        private Button browseCsvButton = new();
        private Button browseTemplateButton = new();
        private Button browseOutputButton = new();
        private Button processButton = new();
        private ProgressForm progressForm = new();
        private ComboBox templateTypeComboBox = new();
        private Label statusLabel = new();
        private Button configureFileNameButton = new();
        private Button previewButton = new();
        private string fileNamePattern = "[Location] [Facility] [Equipment] [Procedure]";
        private Button manageTemplatesButton = new();

        public MainForm()
        {
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            this.Text = "Document Template Processor";
            this.Size = new Size(700, 450); // Increased width slightly for better layout
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MaximizeBox = false;

            // Create a TableLayoutPanel to manage the layout
            var tableLayoutPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                ColumnCount = 3,
                RowCount = 6,
                GrowStyle = TableLayoutPanelGrowStyle.AddRows
            };

            // Define column styles: Labels, TextBoxes, Buttons
            tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // Column 0: Labels
            tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F)); // Column 1: TextBoxes (fills remaining space)
            tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // Column 2: Buttons

            // Define row styles (auto-sized for each row of controls)
            for (int i = 0; i < tableLayoutPanel.RowCount; i++)
            {
                tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            }

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

            // New Manage Templates Button
            manageTemplatesButton = new Button
            {
                Text = "Manage Templates",
                Location = new Point(370, 200), // Adjusted location
                Size = new Size(120, 30)
            };
            manageTemplatesButton.Click += ManageTemplatesButton_Click;

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

            // Add controls to the TableLayoutPanel
            tableLayoutPanel.Controls.Add(templateTypeLabel, 0, 0);
            tableLayoutPanel.Controls.Add(templateTypeComboBox, 1, 0);
            // Skip column 2 in row 0 for template type

            tableLayoutPanel.Controls.Add(csvLabel, 0, 1);
            tableLayoutPanel.Controls.Add(csvPathTextBox, 1, 1);
            tableLayoutPanel.Controls.Add(browseCsvButton, 2, 1);

            tableLayoutPanel.Controls.Add(templateLabel, 0, 2);
            tableLayoutPanel.Controls.Add(templatePathTextBox, 1, 2);
            tableLayoutPanel.Controls.Add(browseTemplateButton, 2, 2);

            tableLayoutPanel.Controls.Add(outputLabel, 0, 3);
            tableLayoutPanel.Controls.Add(outputDirTextBox, 1, 3);
            tableLayoutPanel.Controls.Add(browseOutputButton, 2, 3);

            // Buttons row (spanning columns for better centering/grouping if needed, or individual cells)
            // Let's place them in a FlowLayoutPanel within a TableLayoutPanel cell for flexibility
            var buttonFlowPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true
            };

            buttonFlowPanel.Controls.Add(configureFileNameButton);
            buttonFlowPanel.Controls.Add(previewButton);
            buttonFlowPanel.Controls.Add(manageTemplatesButton);

            tableLayoutPanel.Controls.Add(buttonFlowPanel, 0, 4); // Span across all 3 columns
            tableLayoutPanel.SetColumnSpan(buttonFlowPanel, 3);

            // Process Files button centered below the others
             var processButtonFlowPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown, // Stack vertically within this panel
                WrapContents = false,
                AutoSize = true,
                Anchor = AnchorStyles.None // Center the flow panel in the cell
            };
            processButtonFlowPanel.Controls.Add(processButton);
             tableLayoutPanel.Controls.Add(processButtonFlowPanel, 0, 5); // Span across all 3 columns
            tableLayoutPanel.SetColumnSpan(processButtonFlowPanel, 3);


            // Status Label (spanning columns)
            tableLayoutPanel.Controls.Add(statusLabel, 0, 6); // Row 6
            tableLayoutPanel.SetColumnSpan(statusLabel, 3);
            statusLabel.Anchor = AnchorStyles.Left | AnchorStyles.Right; // Stretch across cell


            // Add the TableLayoutPanel to the form's controls
            this.Controls.Add(tableLayoutPanel);
        }

        private void TemplateTypeComboBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            UpdateTemplateFileFilter();
        }

        private void UpdateTemplateFileFilter()
        {
            string filter = templateTypeComboBox.SelectedItem?.ToString() == "Excel"
                ? "Excel files (*.xlsx)|*.xlsx|All files (*.*)|*.*"
                : "Word files (*.docx)|*.docx|All files (*.*)|*.*";
            
            templatePathTextBox.Text = string.Empty;
        }

        private void BrowseCsvButton_Click(object? sender, EventArgs e)
        {
            using var openFileDialog = new OpenFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
                Title = "Select CSV File"
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                csvPathTextBox.Text = openFileDialog.FileName;
            }
        }

        private void BrowseTemplateButton_Click(object? sender, EventArgs e)
        {
            using var openFileDialog = new OpenFileDialog
            {
                Filter = templateTypeComboBox.SelectedItem?.ToString() == "Excel" 
                    ? "Excel Files (*.xlsx)|*.xlsx|All Files (*.*)|*.*"
                    : "Word Files (*.docx)|*.docx|All Files (*.*)|*.*",
                Title = "Select Template File"
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                templatePathTextBox.Text = openFileDialog.FileName;
            }
        }

        private void BrowseOutputButton_Click(object? sender, EventArgs e)
        {
            using var folderBrowserDialog = new FolderBrowserDialog
            {
                Description = "Select Output Directory"
            };

            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                outputDirTextBox.Text = folderBrowserDialog.SelectedPath;
            }
        }

        private void ConfigureFileNameButton_Click(object? sender, EventArgs e)
        {
            using var configForm = new FileNameConfigForm(new List<string>(), fileNamePattern);
            if (configForm.ShowDialog() == DialogResult.OK)
            {
                fileNamePattern = configForm.FileNamePattern;
            }
        }

        private void PreviewButton_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(csvPathTextBox.Text) || string.IsNullOrEmpty(templatePathTextBox.Text))
            {
                MessageBox.Show("Please select both CSV and template files first.", "Missing Files", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                var records = _csvService.LoadCsvData(csvPathTextBox.Text);
                if (records.Count == 0)
                {
                    MessageBox.Show("No records found in the CSV file.", "Empty File", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                using var previewForm = new PreviewForm();
                previewForm.LoadPreview(templatePathTextBox.Text, csvPathTextBox.Text, templateTypeComboBox.SelectedItem?.ToString() == "Excel");
                previewForm.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error previewing files: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void ProcessButton_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(csvPathTextBox.Text) || string.IsNullOrEmpty(templatePathTextBox.Text) || string.IsNullOrEmpty(outputDirTextBox.Text))
            {
                MessageBox.Show("Please select all required files and directories.", "Missing Files", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                var records = _csvService.LoadCsvData(csvPathTextBox.Text);
                if (records.Count == 0)
                {
                    MessageBox.Show("No records found in the CSV file.", "Empty File", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                processButton.Enabled = false;
                progressForm = new ProgressForm();
                progressForm.Show();

                await Task.Run(() => ProcessFiles(records, templatePathTextBox.Text, outputDirTextBox.Text));

                progressForm.Close();
                MessageBox.Show("Processing completed successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error processing files: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                processButton.Enabled = true;
            }
        }

        private void ProcessFiles(List<ExcelRecord> records, string templatePath, string outputDir)
        {
            EnsureOutputDirectoryExists(outputDir);
            var isExcel = templateTypeComboBox.SelectedItem?.ToString() == "Excel";
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

        private void ManageTemplatesButton_Click(object? sender, EventArgs e)
        {
            using var manageTemplatesForm = new ManageTemplatesForm();
            if (manageTemplatesForm.ShowDialog() == DialogResult.OK && manageTemplatesForm.SelectedTemplate != null)
            {
                templatePathTextBox.Text = manageTemplatesForm.SelectedTemplate.Path;
                UpdateTemplateTypeComboBox(manageTemplatesForm.SelectedTemplate.Path);
            }
        }

        private void UpdateTemplateTypeComboBox(string filePath)
        {
            if (!string.IsNullOrEmpty(filePath))
            {
                string extension = Path.GetExtension(filePath).ToLower();
                if (extension == ".xlsx")
                {
                    templateTypeComboBox.SelectedItem = "Excel";
                }
                else if (extension == ".docx")
                {
                    templateTypeComboBox.SelectedItem = "Word";
                }
            }
        }
    }

    public class ProgressForm : Form
    {
        private Label statusLabel = new();
        private ProgressBar progressBar = new();
        private Label currentFileLabel = new();
        private Button doneButton = new();

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