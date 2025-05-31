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
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MaximizeBox = false;
            this.MinimumSize = new Size(1000, 700);

            _previewService = new PreviewService();
            _records = new List<ExcelRecord>();
            _markedRecords = new List<int>();
            _showOnlyMarked = false;

            // Initialize components
            _tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Padding = new Point(10, 10),
                Margin = new Padding(10)
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
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                Margin = new Padding(5)
            };

            // Initialize preview components
            _previewSplitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 300,
                Margin = new Padding(5),
                Panel1 = { Padding = new Padding(5) },
                Panel2 = { Padding = new Padding(5) }
            };

            // Create search panel
            var searchPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 45,
                Padding = new Padding(5),
                Margin = new Padding(0, 0, 0, 5)
            };

            _searchBox = new TextBox
            {
                Dock = DockStyle.Fill,
                PlaceholderText = "Search records...",
                Font = new Font("Segoe UI", 10),
                Margin = new Padding(0, 0, 5, 0)
            };
            _searchBox.TextChanged += SearchBox_TextChanged;

            _clearSearchButton = new Button
            {
                Dock = DockStyle.Right,
                Width = 80,
                Text = "Clear",
                Enabled = false,
                Margin = new Padding(5, 0, 0, 0)
            };
            _clearSearchButton.Click += ClearSearchButton_Click;

            _showMarkedButton = new Button
            {
                Dock = DockStyle.Right,
                Width = 100,
                Text = "Show Marked",
                Margin = new Padding(5, 0, 0, 0)
            };
            _showMarkedButton.Click += ShowMarkedButton_Click;

            searchPanel.Controls.AddRange(new Control[] { _searchBox, _clearSearchButton, _showMarkedButton });

            // Initialize ListView with images
            _listViewImageList = new ImageList();
            _listViewImageList.Images.Add("normal", CreateCircleImage(Color.Transparent));
            _listViewImageList.Images.Add("marked", CreateCircleImage(Color.Yellow));

            _recordsListView = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = false,
                MultiSelect = false,
                SmallImageList = _listViewImageList,
                Margin = new Padding(0)
            };
            _recordsListView.Columns.Add("Record", 280);
            _recordsListView.SelectedIndexChanged += RecordsListView_SelectedIndexChanged;
            _recordsListView.KeyDown += RecordsListView_KeyDown;
            _recordsListView.MouseClick += RecordsListView_MouseClick;

            // Create a container panel for the left side using TableLayoutPanel for better control
            var leftPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(0),
                Margin = new Padding(0),
                // Define row styles: first row for search panel (auto-size), second for list view (fills remaining space)
                RowStyles =
                {
                    new RowStyle(SizeType.AutoSize),
                    new RowStyle(SizeType.Percent, 100F)
                }
            };

            // Add controls to the TableLayoutPanel
            leftPanel.Controls.Add(searchPanel, 0, 0); // Add searchPanel to cell (0, 0)
            leftPanel.Controls.Add(_recordsListView, 0, 1); // Add _recordsListView to cell (0, 1)

            _previewTextBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                Font = new Font("Consolas", 10),
                Margin = new Padding(0, 5, 0, 0)
            };

            _previewSplitContainer.Panel1.Controls.Add(leftPanel);
            _previewSplitContainer.Panel2.Controls.Add(_previewTextBox);

            // Create bottom panel for status and close button
            var bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 80,
                Padding = new Padding(10)
            };

            _closeButton = new Button
            {
                Text = "Close",
                DialogResult = DialogResult.OK,
                Dock = DockStyle.Right,
                Width = 100,
                Height = 35
            };

            _statusLabel = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0),
                AutoSize = false
            };

            bottomPanel.Controls.AddRange(new Control[] { _statusLabel, _closeButton });

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
            _previewTab.Controls.Add(_previewSplitContainer);
            _tabControl.TabPages.AddRange(new TabPage[] { _validationTab, _previewTab });

            this.Controls.AddRange(new Control[] { _tabControl, bottomPanel });
        }

        private void ShowMarkedButton_Click(object sender, EventArgs e)
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
            var itemsToShow = new List<ListViewItem>();

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
                        .Cast<ListViewItem>()
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
                        var newItem = new ListViewItem(label)
                        {
                            ImageKey = _markedRecords.Contains(i) ? "marked" : "normal",
                            Tag = i // Store the original record index
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

        private Image CreateCircleImage(Color color)
        {
            var image = new Bitmap(16, 16);
            using (var g = Graphics.FromImage(image))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                using (var brush = new SolidBrush(color))
                {
                    g.FillEllipse(brush, 2, 2, 12, 12);
                }
            }
            return image;
        }

        private void RecordsListView_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
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

        private void RecordsListView_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space && _recordsListView.SelectedItems.Count > 0)
            {
                var selectedItem = _recordsListView.SelectedItems[0];
                if (selectedItem.Tag is int originalIndex)
                {
                    ToggleRecordMark(originalIndex); // Use the original index stored in Tag
                    e.Handled = true;
                }
            }
            else if (e.KeyCode == Keys.Up || e.KeyCode == Keys.Down)
            {
                // Let the default ListView navigation handle it
                return;
            }
        }

        private void SearchBox_TextChanged(object sender, EventArgs e)
        {
            ApplyFilters();
        }

        private void ClearSearchButton_Click(object sender, EventArgs e)
        {
            _searchBox.Clear();
            ApplyFilters();
        }

        private void UpdateStatusLabel()
        {
            var baseText = _statusLabel.Text.Split(new[] { " | " }, StringSplitOptions.None)[0];
            var markedCount = _markedRecords.Count;
            var visibleCount = _recordsListView.Items.Count;
            _statusLabel.Text = $"{baseText} | {markedCount} record(s) marked for review | {visibleCount} record(s) visible";
        }

        private void RecordsListView_SelectedIndexChanged(object sender, EventArgs e)
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
                    var item = new ListViewItem(label) { ImageKey = "normal", Tag = i }; // Store original index in Tag
                    _recordsListView.Items.Add(item);
                }

                // Manually resize the column to fit the content after items are added
                int maxColumnWidth = 0;
                using (Graphics g = _recordsListView.CreateGraphics())
                {
                    foreach (ListViewItem item in _recordsListView.Items)
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

                    // Apply color based on status
                    switch (result.Status)
                    {
                        case ValidationStatus.Success:
                            row.DefaultCellStyle.BackColor = Color.LightGreen;
                            break;
                        case ValidationStatus.Warning:
                            row.DefaultCellStyle.BackColor = Color.LightYellow;
                            break;
                        case ValidationStatus.Error:
                            row.DefaultCellStyle.BackColor = Color.Salmon;
                            break;
                        case ValidationStatus.CsvColumnUnused:
                            row.DefaultCellStyle.BackColor = Color.LightBlue;
                            break;
                    }
                }

                // Apply filters after loading data (will also handle selection of the first visible item)
                ApplyFilters();

                // Update status
                var errorCount = validationResults.Count(r => r.Status.ToString() == "Error");
                var warningCount = validationResults.Count(r => r.Status.ToString() == "Warning");
                _statusLabel.Text = $"Found {errorCount} errors and {warningCount} warnings. {_records.Count} records available for preview.";
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