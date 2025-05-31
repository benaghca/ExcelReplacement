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
        private readonly Button _closeButton;
        private readonly Label _statusLabel;
        private readonly PreviewService _previewService;
        private readonly List<ExcelRecord> _records;
        private readonly ListView _recordsListView;
        private readonly RichTextBox _previewTextBox;
        private readonly SplitContainer _previewSplitContainer;
        private readonly TextBox _searchBox;
        private readonly Button _clearSearchButton;
        private readonly Button _showMarkedButton;
        private readonly List<int> _markedRecords;
        private readonly ImageList _listViewImageList;
        private bool _showOnlyMarked;

        public PreviewForm()
        {
            this.Text = "Template Preview and Validation";
            this.Size = new Size(1200, 800);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.MaximizeBox = false;
            this.MinimumSize = new Size(1000, 700);

            _previewService = new PreviewService();
            _records = new List<ExcelRecord>();
            _markedRecords = new List<int>();
            _showOnlyMarked = false;

            // Initialize components
            _tabControl = new System.Windows.Forms.TabControl
            {
                Dock = System.Windows.Forms.DockStyle.Fill,
                Padding = new System.Drawing.Point(10, 10),
                Margin = new System.Windows.Forms.Padding(10)
            };

            _validationTab = new System.Windows.Forms.TabPage("Validation");
            _previewTab = new System.Windows.Forms.TabPage("Preview");

            _placeholdersGrid = new System.Windows.Forms.DataGridView
            {
                Dock = System.Windows.Forms.DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill,
                Margin = new System.Windows.Forms.Padding(5),
                // Revert DataGridView styling to default, let ThemeManager handle it
                EnableHeadersVisualStyles = true, 
                RowHeadersVisible = true, 
                RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.EnableResizing,
            };

            // Initialize preview components
            _previewSplitContainer = new System.Windows.Forms.SplitContainer
            {
                Dock = System.Windows.Forms.DockStyle.Fill,
                Orientation = System.Windows.Forms.Orientation.Vertical,
                SplitterDistance = 300,
                Margin = new System.Windows.Forms.Padding(5),
                Panel1 = { Padding = new System.Windows.Forms.Padding(5) },
                Panel2 = { Padding = new System.Windows.Forms.Padding(5) }
            };

            // Create search panel
            var searchPanel = new System.Windows.Forms.Panel
            {
                Dock = System.Windows.Forms.DockStyle.Top,
                Height = 45,
                Padding = new System.Windows.Forms.Padding(5),
                Margin = new System.Windows.Forms.Padding(0, 0, 0, 5)
            };

            _searchBox = new System.Windows.Forms.TextBox
            {
                Dock = System.Windows.Forms.DockStyle.Fill,
                PlaceholderText = "Search records...",
                Font = new System.Drawing.Font("Consolas", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0))),
                Margin = new System.Windows.Forms.Padding(0, 0, 5, 0)
            };
            _searchBox.TextChanged += SearchBox_TextChanged;

            _clearSearchButton = new System.Windows.Forms.Button
            {
                Dock = System.Windows.Forms.DockStyle.Right,
                Width = 80,
                Text = "Clear",
                Enabled = false,
                Margin = new System.Windows.Forms.Padding(5, 0, 0, 0)
            };
            _clearSearchButton.Click += ClearSearchButton_Click;

            _showMarkedButton = new System.Windows.Forms.Button
            {
                Dock = System.Windows.Forms.DockStyle.Right,
                Width = 100,
                Text = "Show Marked",
                Margin = new System.Windows.Forms.Padding(5, 0, 0, 0)
            };
            _showMarkedButton.Click += ShowMarkedButton_Click;

            searchPanel.Controls.AddRange(new System.Windows.Forms.Control[] { _searchBox, _clearSearchButton, _showMarkedButton });

            // Initialize ListView with images
            _listViewImageList = new System.Windows.Forms.ImageList();
            _listViewImageList.Images.Add("normal", CreateCircleImage(System.Drawing.Color.Transparent));
            _listViewImageList.Images.Add("marked", CreateCircleImage(System.Drawing.Color.Yellow));

            _recordsListView = new System.Windows.Forms.ListView
            {
                Dock = System.Windows.Forms.DockStyle.Fill,
                View = System.Windows.Forms.View.Details,
                FullRowSelect = true,
                GridLines = false,
                MultiSelect = false,
                SmallImageList = _listViewImageList,
                Margin = new System.Windows.Forms.Padding(0)
            };
            _recordsListView.Columns.Add("Record", 280);
            _recordsListView.SelectedIndexChanged += RecordsListView_SelectedIndexChanged;
            _recordsListView.KeyDown += RecordsListView_KeyDown;
            _recordsListView.MouseClick += RecordsListView_MouseClick;

            // Create a container panel for the left side using TableLayoutPanel for better control
            var leftPanel = new System.Windows.Forms.TableLayoutPanel
            {
                Dock = System.Windows.Forms.DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new System.Windows.Forms.Padding(0),
                Margin = new System.Windows.Forms.Padding(0),
                // Define row styles: first row for search panel (auto-size), second for list view (fills remaining space)
                RowStyles =
                {
                    new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize),
                    new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F)
                }
            };

            // Add controls to the TableLayoutPanel
            leftPanel.Controls.Add(searchPanel, 0, 0); // Add searchPanel to cell (0, 0)
            leftPanel.Controls.Add(_recordsListView, 0, 1); // Add _recordsListView to cell (0, 1)

            _previewTextBox = new System.Windows.Forms.RichTextBox
            {
                Dock = System.Windows.Forms.DockStyle.Fill,
                ReadOnly = true,
                Font = new System.Drawing.Font("Consolas", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0))),
                Margin = new System.Windows.Forms.Padding(0, 5, 0, 0)
            };

            _previewSplitContainer.Panel1.Controls.Add(leftPanel);
            _previewSplitContainer.Panel2.Controls.Add(_previewTextBox);

            // Create bottom panel for status and close button
            var bottomPanel = new System.Windows.Forms.Panel
            {
                Dock = System.Windows.Forms.DockStyle.Bottom,
                Height = 80,
                Padding = new System.Windows.Forms.Padding(10)
            };

            _closeButton = new System.Windows.Forms.Button
            {
                Text = "Close",
                DialogResult = System.Windows.Forms.DialogResult.OK,
                Dock = System.Windows.Forms.DockStyle.Right,
                Width = 100,
                Height = 35
            };

            _statusLabel = new System.Windows.Forms.Label
            {
                Dock = System.Windows.Forms.DockStyle.Fill,
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
                Padding = new System.Windows.Forms.Padding(10, 0, 0, 0),
                AutoSize = false
            };

            bottomPanel.Controls.AddRange(new System.Windows.Forms.Control[] { _statusLabel, _closeButton });

            // Setup grid columns
            _placeholdersGrid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[]
            {
                new System.Windows.Forms.DataGridViewTextBoxColumn { Name = "Placeholder", HeaderText = "Placeholder" },
                new System.Windows.Forms.DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Status" },
                new System.Windows.Forms.DataGridViewTextBoxColumn { Name = "Message", HeaderText = "Message" },
                new System.Windows.Forms.DataGridViewTextBoxColumn { Name = "SampleValue", HeaderText = "Sample Value" },
                new System.Windows.Forms.DataGridViewTextBoxColumn { Name = "Location", HeaderText = "Location" }
            });

            // Add controls to form
            _validationTab.Controls.Add(_placeholdersGrid);
            _previewTab.Controls.Add(_previewSplitContainer);
            _tabControl.TabPages.AddRange(new System.Windows.Forms.TabPage[] { _validationTab, _previewTab });

            this.Controls.AddRange(new System.Windows.Forms.Control[] { _tabControl, bottomPanel });

            // Apply theme - this should now correctly style controls without conflicting custom code
            ThemeManager.ApplyTheme(this);
        }

        private void ShowMarkedButton_Click(object? sender, EventArgs e)
        {
            _showOnlyMarked = !_showOnlyMarked;
            _showMarkedButton.Text = _showOnlyMarked ? "Show All" : "Show Marked";
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            var searchText = _searchBox.Text.ToLower();
            _clearSearchButton.Enabled = !string.IsNullOrWhiteSpace(searchText);

            // Create a temporary list of ListViewItems that should be visible
            var itemsToShow = new List<System.Windows.Forms.ListViewItem>();

            for (int i = 0; i < _records.Count; i++)
            {
                var record = _records[i];
                // Create a descriptive label for each record using key fields
                var label = string.Join(" - ", record.Values.Take(3).Select(kv => $"{kv.Key}: {kv.Value}"));

                bool matchesSearch = string.IsNullOrWhiteSpace(searchText) ||
                                     label.ToLower().Contains(searchText);
                bool matchesMarked = !_showOnlyMarked || _markedRecords.Contains(i);

                if (matchesSearch && matchesMarked)
                {
                    // Check if an item for this record already exists in the current view
                    // This requires a way to link ListViewItem back to the original record index
                    // Let's store the original index in the ListViewItem's Tag property
                    var existingItem = _recordsListView.Items
                        .Cast<System.Windows.Forms.ListViewItem>()
                        .FirstOrDefault(item => item.Tag is int originalIndex && originalIndex == i);

                    if (existingItem != null)
                    {
                        // Update existing item's appearance
                        existingItem.ImageKey = _markedRecords.Contains(i) ? "marked" : "normal";
                        itemsToShow.Add(existingItem);
                    }
                    else
                    {
                        // Create a new item for this record
                        var newItem = new System.Windows.Forms.ListViewItem(label)
                        {
                            ImageKey = _markedRecords.Contains(i) ? "marked" : "normal",
                            Tag = i // Store original index in Tag
                        };
                        itemsToShow.Add(newItem);
                    }
                }
            }

            // Replace the current items in the ListView with the filtered and updated items
            // This will prevent duplication and ensure correct items are shown
            _recordsListView.BeginUpdate(); // Optimize for bulk updates
            _recordsListView.Items.Clear();
            _recordsListView.Items.AddRange(itemsToShow.ToArray());
            _recordsListView.EndUpdate();

            // Re-select the first item if available, otherwise clear selection
            if (_recordsListView.Items.Count > 0)
            {
                _recordsListView.Items[0].Selected = true;
            }

            UpdateStatusLabel();
        }

        private System.Drawing.Image CreateCircleImage(System.Drawing.Color color)
        {
            var image = new System.Drawing.Bitmap(16, 16);
            using (var g = System.Drawing.Graphics.FromImage(image))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.Clear(System.Drawing.Color.Transparent);
                using (System.Drawing.Brush brush = new System.Drawing.SolidBrush(color))
                {
                    g.FillEllipse(brush, 2, 2, 12, 12);
                }
            }
            return image;
        }

        private void RecordsListView_MouseClick(object? sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                var hit = _recordsListView.GetItemAt(e.X, e.Y);
                if (hit != null && hit.Tag is int originalIndex)
                {
                    ToggleRecordMark(originalIndex); // Use the original index stored in Tag
                }
            }
        }

        private void ToggleRecordMark(int index)
        {
            if (_markedRecords.Contains(index))
            {
                _markedRecords.Remove(index);
            }
            else
            {
                _markedRecords.Add(index);
            }
            ApplyFilters(); // Re-apply filters to update the list view appearance
        }

        private void RecordsListView_KeyDown(object? sender, System.Windows.Forms.KeyEventArgs e)
        {
            if (e.KeyCode == System.Windows.Forms.Keys.Space && _recordsListView.SelectedItems.Count > 0)
            {
                var selectedItem = _recordsListView.SelectedItems[0];
                if (selectedItem.Tag is int originalIndex)
                {
                    ToggleRecordMark(originalIndex); // Use the original index stored in Tag
                    e.Handled = true;
                }
            }
            else if (e.KeyCode == System.Windows.Forms.Keys.Up || e.KeyCode == System.Windows.Forms.Keys.Down)
            {
                // Let the default ListView navigation handle it
                return;
            }
        }

        private void SearchBox_TextChanged(object? sender, EventArgs e)
        {
            ApplyFilters();
        }

        private void ClearSearchButton_Click(object? sender, EventArgs e)
        {
            _searchBox.Clear();
            ApplyFilters();
        }

        private void UpdateStatusLabel()
        {
            var baseText = _statusLabel.Text.Split(new[] { " | " }, System.StringSplitOptions.None)[0];
            var markedCount = _markedRecords.Count;
            var visibleCount = _recordsListView.Items.Count;
            _statusLabel.Text = $"{baseText} | {markedCount} record(s) marked for review | {visibleCount} record(s) visible";
        }

        private void RecordsListView_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_recordsListView.SelectedItems.Count > 0)
            {
                var selectedItem = _recordsListView.SelectedItems[0];
                if (selectedItem.Tag is int originalIndex)
                {
                     var record = _records[originalIndex]; // Get the record using the original index
                    UpdatePreview(record);
                }
            }
        }

        private void UpdatePreview(ExcelRecord record)
        {
            if (_tabControl.SelectedTab == _previewTab)
            {
                var preview = _previewService.GeneratePreview(_currentTemplatePath, record, _isExcel);
                _previewTextBox.Text = preview;
            }
        }

        private string _currentTemplatePath = string.Empty;
        private bool _isExcel;

        public void LoadPreview(string templatePath, string csvPath, bool isExcel)
        {
            try
            {
                _currentTemplatePath = templatePath;
                _isExcel = isExcel;

                var csvService = new CsvService();
                _records.Clear();
                _records.AddRange(csvService.LoadCsvData(csvPath));
                
                if (_records.Count == 0)
                {
                    throw new Exception("No records found in CSV file.");
                }

                // Populate the list view with all records initially
                _recordsListView.Items.Clear();
                _markedRecords.Clear();
                for (int i = 0; i < _records.Count; i++)
                {
                    var record = _records[i];
                    // Create a descriptive label for each record using key fields
                    var label = string.Join(" - ", record.Values.Take(3).Select(kv => $"{kv.Key}: {kv.Value}"));
                    var item = new System.Windows.Forms.ListViewItem(label) { ImageKey = "normal", Tag = i }; // Store original index in Tag
                    _recordsListView.Items.Add(item);
                }

                // Manually resize the column to fit the content after items are added
                int maxColumnWidth = 0;
                using (System.Drawing.Graphics g = _recordsListView.CreateGraphics())
                {
                    foreach (System.Windows.Forms.ListViewItem item in _recordsListView.Items)
                    {
                        int itemWidth = (int)g.MeasureString(item.Text, _recordsListView.Font).Width;
                        if (itemWidth > maxColumnWidth)
                        {
                            maxColumnWidth = itemWidth;
                        }
                    }
                }
                // Add a small buffer for padding and the image
                _recordsListView.Columns[0].Width = maxColumnWidth + 30; // Add buffer for icon and padding

                var firstRecord = _records[0];
                var templateValidator = new TemplateValidator(templatePath, isExcel);
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

                    // Apply color based on status - set text color, background is handled by theme
                    switch (result.Status)
                    {
                        case ValidationStatus.Success:
                            row.DefaultCellStyle.ForeColor = ThemeManager.SuccessColor; // Green text
                            break;
                        case ValidationStatus.Warning:
                            row.DefaultCellStyle.ForeColor = ThemeManager.WarningColor; // Yellow text
                            break;
                        case ValidationStatus.Error:
                            row.DefaultCellStyle.ForeColor = ThemeManager.ErrorColor; // Red text
                            break;
                        case ValidationStatus.CsvColumnUnused:
                            row.DefaultCellStyle.ForeColor = ThemeManager.InfoColor; // Blue text
                            break;
                    }
                }

                // Apply filters after loading data (will also handle selection of the first visible item)
                ApplyFilters();

                // Update status
                var errorCount = validationResults.Count(r => r.Status.ToString() == "Error");
                var warningCount = validationResults.Count(r => r.Status.ToString() == "Warning");
                _statusLabel.Text = $"Found {errorCount} errors and {warningCount} warnings. {_records.Count} records available for preview.";
                _statusLabel.ForeColor = errorCount > 0 ? ThemeManager.ErrorColor : warningCount > 0 ? ThemeManager.WarningColor : ThemeManager.SuccessColor;
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show($"Error loading preview: {ex.Message}", "Error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
                this.Close();
            }
        }
    }
} 