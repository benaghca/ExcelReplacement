using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ExcelReplacement.Models;

namespace ExcelReplacement.Forms
{
    public class FileNameConfigForm : Form
    {
        private ListBox availablePlaceholdersListBox;
        private TextBox patternTextBox;
        private Button addPlaceholderButton;
        private Button addTextButton;
        private Button previewButton;
        private Label previewLabel;
        private Button saveButton;
        private Button cancelButton;
        private List<string> availablePlaceholders;
        private TextBox customTextTextBox;

        public string FileNamePattern { get; private set; }

        public FileNameConfigForm(List<string> placeholders, string currentPattern = "")
        {
            availablePlaceholders = placeholders;
            FileNamePattern = currentPattern;
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            this.Text = "Configure File Name Pattern";
            this.Size = new Size(600, 500);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // Available Placeholders Label
            var placeholdersLabel = new Label
            {
                Text = "Available Placeholders:",
                Location = new Point(20, 20),
                AutoSize = true
            };

            // Available Placeholders ListBox
            availablePlaceholdersListBox = new ListBox
            {
                Location = new Point(20, 50),
                Size = new Size(200, 200),
                SelectionMode = SelectionMode.One
            };
            availablePlaceholdersListBox.Items.AddRange(availablePlaceholders.ToArray());

            // Pattern Label
            var patternLabel = new Label
            {
                Text = "File Name Pattern:",
                Location = new Point(20, 270),
                AutoSize = true
            };

            // Pattern TextBox
            patternTextBox = new TextBox
            {
                Location = new Point(20, 300),
                Size = new Size(400, 20),
                Text = FileNamePattern
            };

            // Add Placeholder Button
            addPlaceholderButton = new Button
            {
                Text = "Add Placeholder",
                Location = new Point(240, 50),
                Size = new Size(120, 30)
            };
            addPlaceholderButton.Click += AddPlaceholderButton_Click;

            // Custom Text Label
            var customTextLabel = new Label
            {
                Text = "Custom Text:",
                Location = new Point(240, 100),
                AutoSize = true
            };

            // Custom Text TextBox
            customTextTextBox = new TextBox
            {
                Location = new Point(240, 130),
                Size = new Size(120, 20)
            };

            // Add Text Button
            addTextButton = new Button
            {
                Text = "Add Text",
                Location = new Point(240, 160),
                Size = new Size(120, 30)
            };
            addTextButton.Click += AddTextButton_Click;

            // Preview Button
            previewButton = new Button
            {
                Text = "Preview",
                Location = new Point(240, 200),
                Size = new Size(120, 30)
            };
            previewButton.Click += PreviewButton_Click;

            // Preview Label
            previewLabel = new Label
            {
                Location = new Point(20, 340),
                Size = new Size(400, 40),
                AutoSize = true
            };

            // Save Button
            saveButton = new Button
            {
                Text = "Save",
                Location = new Point(240, 400),
                Size = new Size(80, 30),
                DialogResult = DialogResult.OK
            };
            saveButton.Click += SaveButton_Click;

            // Cancel Button
            cancelButton = new Button
            {
                Text = "Cancel",
                Location = new Point(340, 400),
                Size = new Size(80, 30),
                DialogResult = DialogResult.Cancel
            };

            // Add controls to form
            this.Controls.AddRange(new Control[] {
                placeholdersLabel,
                availablePlaceholdersListBox,
                patternLabel,
                patternTextBox,
                addPlaceholderButton,
                customTextLabel,
                customTextTextBox,
                addTextButton,
                previewButton,
                previewLabel,
                saveButton,
                cancelButton
            });
        }

        private void AddPlaceholderButton_Click(object sender, EventArgs e)
        {
            if (availablePlaceholdersListBox.SelectedItem != null)
            {
                var placeholder = $"[{availablePlaceholdersListBox.SelectedItem}]";
                InsertTextAtCursor(placeholder);
            }
        }

        private void AddTextButton_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(customTextTextBox.Text))
            {
                InsertTextAtCursor(customTextTextBox.Text);
                customTextTextBox.Clear();
            }
        }

        private void InsertTextAtCursor(string text)
        {
            var selectionStart = patternTextBox.SelectionStart;
            patternTextBox.Text = patternTextBox.Text.Insert(selectionStart, text);
            patternTextBox.SelectionStart = selectionStart + text.Length;
            patternTextBox.Focus();
        }

        private void PreviewButton_Click(object sender, EventArgs e)
        {
            try
            {
                var record = new ExcelRecord();
                foreach (var placeholder in availablePlaceholders)
                {
                    record.Values[placeholder] = $"Sample{placeholder}";
                }

                var preview = GeneratePreviewFileName(record);
                previewLabel.Text = $"Preview: {preview}";
            }
            catch (Exception ex)
            {
                previewLabel.Text = $"Error: {ex.Message}";
            }
        }

        private string GeneratePreviewFileName(ExcelRecord record)
        {
            var pattern = patternTextBox.Text;
            foreach (var placeholder in availablePlaceholders)
            {
                pattern = pattern.Replace($"[{placeholder}]", record.GetValue(placeholder));
            }
            return pattern + ".xlsx";
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(patternTextBox.Text))
            {
                MessageBox.Show("Please enter a file name pattern.", "Validation Error",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.DialogResult = DialogResult.None;
                return;
            }

            FileNamePattern = patternTextBox.Text;
        }
    }
} 