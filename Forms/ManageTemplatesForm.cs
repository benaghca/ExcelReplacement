using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using ExcelReplacement.Models;
using System.Text.Json;

namespace ExcelReplacement.Forms
{
    public class ManageTemplatesForm : Form
    {
        private DataGridView savedTemplatesGrid;
        private Button removeTemplateButton;
        private Label savedTemplatesLabel;
        private Button selectTemplateButton;
        private Button addTemplateButton;
        private ComboBox templateTypeComboBox;
        private Label templateTypeLabel;
        private TextBox searchTextBox;
        private Label searchLabel;

        private List<TemplateInfo> savedTemplates = new List<TemplateInfo>();
        private const string TemplatesFileName = "templates.json";

        public TemplateInfo SelectedTemplate { get; private set; }

        public ManageTemplatesForm()
        {
            InitializeComponents();
            LoadTemplates();
            this.FormClosing += ManageTemplatesForm_FormClosing;
        }

        private void InitializeComponents()
        {
            this.Text = "Manage Templates";
            this.Size = new Size(700, 500); // Increased size for DataGridView and search
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // Use a TableLayoutPanel for better layout management
            var tableLayoutPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                ColumnCount = 3,
                RowCount = 5,
                GrowStyle = TableLayoutPanelGrowStyle.AddRows
            };

            // Define column styles: Labels, Controls, Buttons
            tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // Column 0: Labels
            tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F)); // Column 1: Controls (fills remaining space)
            tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // Column 2: Buttons

            // Template Type Selection
            templateTypeLabel = new Label { Text = "Template Type:", AutoSize = true };
            templateTypeComboBox = new ComboBox
            {
                Width = 150,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            templateTypeComboBox.Items.AddRange(new object[] { "Excel", "Word" });
            templateTypeComboBox.SelectedIndex = 0;

            // Add Template Button
            addTemplateButton = new Button
            {
                Text = "Add Template...",
                Size = new Size(120, 30)
            };
            addTemplateButton.Click += AddTemplateButton_Click;

            // Search Label and TextBox
            searchLabel = new Label { Text = "Search:", AutoSize = true };
            searchTextBox = new TextBox { Width = 300 };
            searchTextBox.TextChanged += SearchTextBox_TextChanged;

            // Saved Templates Label
            savedTemplatesLabel = new Label { Text = "Saved Templates:", AutoSize = true };

            // Saved Templates DataGridView
            savedTemplatesGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                AutoGenerateColumns = false, // We will define columns manually
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            // Define columns for the DataGridView
            savedTemplatesGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Name", HeaderText = "Template Name", DataPropertyName = "Name" });
            savedTemplatesGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Path", HeaderText = "Path", DataPropertyName = "Path" });

            savedTemplatesGrid.SelectionChanged += SavedTemplatesGrid_SelectionChanged;

            // Remove Template Button
            removeTemplateButton = new Button
            {
                Text = "Remove Selected",
                Size = new Size(120, 30),
                Enabled = false // Initially disabled
            };
            removeTemplateButton.Click += RemoveTemplateButton_Click;

            // Select Template Button
            selectTemplateButton = new Button
            {
                Text = "Select Template",
                Size = new Size(120, 30),
                DialogResult = DialogResult.OK,
                Enabled = false // Initially disabled
            };

            // Add controls to the TableLayoutPanel
            tableLayoutPanel.Controls.Add(templateTypeLabel, 0, 0);
            tableLayoutPanel.Controls.Add(templateTypeComboBox, 1, 0);
            tableLayoutPanel.Controls.Add(addTemplateButton, 2, 0);

            tableLayoutPanel.Controls.Add(searchLabel, 0, 1);
            tableLayoutPanel.Controls.Add(searchTextBox, 1, 1);
            tableLayoutPanel.SetColumnSpan(searchTextBox, 2); // Span search box across remaining columns

            tableLayoutPanel.Controls.Add(savedTemplatesLabel, 0, 2);
            tableLayoutPanel.SetColumnSpan(savedTemplatesLabel, 3); // Span label across all columns

            tableLayoutPanel.Controls.Add(savedTemplatesGrid, 0, 3);
            tableLayoutPanel.SetColumnSpan(savedTemplatesGrid, 3); // Span grid across all columns

            // Button Flow Panel for bottom buttons
            var buttonFlowPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true
            };
            buttonFlowPanel.Controls.Add(selectTemplateButton);
            buttonFlowPanel.Controls.Add(removeTemplateButton);

            tableLayoutPanel.Controls.Add(buttonFlowPanel, 0, 4);
            tableLayoutPanel.SetColumnSpan(buttonFlowPanel, 3);
            buttonFlowPanel.Anchor = AnchorStyles.None; // Center the flow panel

            // Add TableLayoutPanel to the form
            this.Controls.Add(tableLayoutPanel);
        }

        private void AddTemplateButton_Click(object sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Filter = templateTypeComboBox.SelectedItem.ToString() == "Excel"
                    ? "Excel files (*.xlsx)|*.xlsx|All files (*.*)|*.*"
                    : "Word files (*.docx)|*.docx|All files (*.*)|*.*";
                dialog.Title = "Select template file";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    var templatePath = dialog.FileName;
                    var templateName = Path.GetFileName(templatePath);
                    var templateInfo = new TemplateInfo { Name = templateName, Path = templatePath };
                    AddTemplate(templateInfo);
                }
            }
        }

        private void RemoveTemplateButton_Click(object sender, EventArgs e)
        {
            RemoveSelectedTemplate();
        }

        private void RemoveSelectedTemplate()
        {
            if (savedTemplatesGrid.SelectedRows.Count > 0)
            {
                var selectedRow = savedTemplatesGrid.SelectedRows[0];
                if (selectedRow.DataBoundItem is TemplateInfo selectedTemplate)
                {
                    savedTemplates.Remove(selectedTemplate);
                    savedTemplatesGrid.Rows.Remove(selectedRow);
                    SaveTemplates();
                }
            }
            else
            {
                MessageBox.Show("Please select a template from the list to remove.", "No Template Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void SavedTemplatesGrid_SelectionChanged(object sender, EventArgs e)
        {
            if (savedTemplatesGrid.SelectedRows.Count > 0)
            {
                if (savedTemplatesGrid.SelectedRows[0].DataBoundItem is TemplateInfo selectedTemplate)
                {
                    SelectedTemplate = selectedTemplate;
                    selectTemplateButton.Enabled = true;
                    removeTemplateButton.Enabled = true;
                }
                else
                {
                     SelectedTemplate = null;
                    selectTemplateButton.Enabled = false;
                    removeTemplateButton.Enabled = false;
                }
            }
            else
            {
                SelectedTemplate = null;
                selectTemplateButton.Enabled = false;
                removeTemplateButton.Enabled = false;
            }
        }

        private void LoadTemplates()
        {
            if (File.Exists(TemplatesFileName))
            {
                try
                {
                    var jsonString = File.ReadAllText(TemplatesFileName);
                    var loadedTemplates = JsonSerializer.Deserialize<List<TemplateInfo>>(jsonString);
                    if (loadedTemplates != null)
                    {
                        savedTemplates = loadedTemplates;
                        // Bind the list to the DataGridView
                        savedTemplatesGrid.DataSource = savedTemplates;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading templates: {ex.Message}", "Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void SaveTemplates()
        {
            try
            {
                // Ensure savedTemplates is in sync with the grid if filtering was applied (though filtering here just hides rows)
                // For true filtering, we might use a BindingSource. For now, rely on operations updating savedTemplates directly.
                var jsonString = JsonSerializer.Serialize(savedTemplates);
                File.WriteAllText(TemplatesFileName, jsonString);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving templates: {ex.Message}", "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ManageTemplatesForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveTemplates(); // Always save on closing
        }

        public void AddTemplate(TemplateInfo templateInfo)
        {
            if (savedTemplates.Any(t => t.Path.Equals(templateInfo.Path, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show("This template is already in the list.", "Duplicate Template", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            savedTemplates.Add(templateInfo);
            // Re-bind the DataSource to update the grid (simple approach)
            savedTemplatesGrid.DataSource = null;
            savedTemplatesGrid.DataSource = savedTemplates;
            SaveTemplates();
        }

        private void SearchTextBox_TextChanged(object sender, EventArgs e)
        {
            string searchText = searchTextBox.Text.ToLower();

            if (string.IsNullOrWhiteSpace(searchText))
            {
                // Show all rows if search text is empty
                foreach (DataGridViewRow row in savedTemplatesGrid.Rows)
                {
                    row.Visible = true;
                }
            }
            else
            {
                // Filter rows based on search text in Name or Path columns
                foreach (DataGridViewRow row in savedTemplatesGrid.Rows)
                {
                    bool nameMatch = row.Cells["Name"].Value?.ToString().ToLower().Contains(searchText) ?? false;
                    bool pathMatch = row.Cells["Path"].Value?.ToString().ToLower().Contains(searchText) ?? false;
                    row.Visible = nameMatch || pathMatch;
                }
            }
            // Clear selection when filtering
            savedTemplatesGrid.ClearSelection();
            SelectedTemplate = null;
            selectTemplateButton.Enabled = false;
            removeTemplateButton.Enabled = false;
        }
    }
} 