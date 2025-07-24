using MultiSerialMonitor.Models;

namespace MultiSerialMonitor.Forms
{
    public partial class DetectionViewForm : Form
    {
        private PortConnection _connection;
        private DataGridView _detectionsGrid;
        private RichTextBox _detailsTextBox;
        private Button _clearButton;
        private Button _exportButton;
        private Button _closeButton;
        private Label _summaryLabel;
        private SplitContainer _splitContainer;
        
        public DetectionViewForm(PortConnection connection)
        {
            _connection = connection;
            InitializeComponents();
            LoadDetections();
        }
        
        private void InitializeComponents()
        {
            Text = $"Detection Matches - {_connection.Name}";
            Size = new Size(900, 600);
            StartPosition = FormStartPosition.CenterParent;
            
            // Summary label
            _summaryLabel = new Label
            {
                Location = new Point(12, 12),
                Size = new Size(600, 23),
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            
            // Split container
            _splitContainer = new SplitContainer
            {
                Location = new Point(12, 40),
                Size = new Size(864, 480),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 300
            };
            
            // Detections grid
            _detectionsGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize
            };
            
            // Configure columns
            _detectionsGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Timestamp",
                HeaderText = "Timestamp",
                Width = 150,
                DataPropertyName = "Timestamp",
                DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd HH:mm:ss" }
            });
            
            _detectionsGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "PatternName",
                HeaderText = "Pattern",
                Width = 150,
                DataPropertyName = "PatternName"
            });
            
            _detectionsGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "MatchedText",
                HeaderText = "Matched Text",
                Width = 200,
                DataPropertyName = "MatchedText"
            });
            
            _detectionsGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "LineNumber",
                HeaderText = "Line #",
                Width = 80,
                DataPropertyName = "LineNumber"
            });
            
            _detectionsGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "FullLine",
                HeaderText = "Full Line",
                Width = 300,
                DataPropertyName = "FullLine"
            });
            
            _splitContainer.Panel1.Controls.Add(_detectionsGrid);
            
            // Details text box
            _detailsTextBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                Font = new Font("Consolas", 9),
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.LightGray,
                BorderStyle = BorderStyle.None
            };
            
            _splitContainer.Panel2.Controls.Add(_detailsTextBox);
            
            // Buttons
            _clearButton = new Button
            {
                Text = "Clear All",
                Location = new Point(12, 530),
                Size = new Size(100, 30),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            _clearButton.Click += OnClearAll;
            
            _exportButton = new Button
            {
                Text = "Export",
                Location = new Point(118, 530),
                Size = new Size(100, 30),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            _exportButton.Click += OnExport;
            
            _closeButton = new Button
            {
                Text = "Close",
                Location = new Point(776, 530),
                Size = new Size(100, 30),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                DialogResult = DialogResult.OK
            };
            
            // Add controls
            Controls.AddRange(new Control[] {
                _summaryLabel,
                _splitContainer,
                _clearButton,
                _exportButton,
                _closeButton
            });
            
            // Wire up events
            _detectionsGrid.SelectionChanged += OnSelectionChanged;
            _connection.PatternDetected += OnPatternDetected;
        }
        
        private void LoadDetections()
        {
            var detections = _connection.DetectionMatches
                .OrderByDescending(d => d.Timestamp)
                .ToList();
            
            _detectionsGrid.DataSource = detections;
            UpdateSummary();
        }
        
        private void UpdateSummary()
        {
            var totalCount = _connection.DetectionMatches.Count;
            var patternCounts = _connection.DetectionMatches
                .GroupBy(d => d.PatternName)
                .Select(g => $"{g.Key}: {g.Count()}")
                .ToList();
            
            _summaryLabel.Text = $"Total Detections: {totalCount} | {string.Join(" | ", patternCounts)}";
        }
        
        private void OnSelectionChanged(object? sender, EventArgs e)
        {
            if (_detectionsGrid.SelectedRows.Count > 0)
            {
                var detection = _detectionsGrid.SelectedRows[0].DataBoundItem as DetectionMatch;
                if (detection != null)
                {
                    ShowDetectionDetails(detection);
                }
            }
        }
        
        private void ShowDetectionDetails(DetectionMatch detection)
        {
            _detailsTextBox.Clear();
            
            // Header
            _detailsTextBox.SelectionColor = Color.Yellow;
            _detailsTextBox.SelectionFont = new Font(_detailsTextBox.Font, FontStyle.Bold);
            _detailsTextBox.AppendText($"Detection Details\n");
            _detailsTextBox.AppendText(new string('-', 80) + "\n\n");
            
            // Details
            _detailsTextBox.SelectionColor = Color.Cyan;
            _detailsTextBox.AppendText($"Pattern: ");
            _detailsTextBox.SelectionColor = Color.White;
            _detailsTextBox.AppendText($"{detection.PatternName}\n");
            
            _detailsTextBox.SelectionColor = Color.Cyan;
            _detailsTextBox.AppendText($"Timestamp: ");
            _detailsTextBox.SelectionColor = Color.White;
            _detailsTextBox.AppendText($"{detection.Timestamp:yyyy-MM-dd HH:mm:ss.fff}\n");
            
            _detailsTextBox.SelectionColor = Color.Cyan;
            _detailsTextBox.AppendText($"Line Number: ");
            _detailsTextBox.SelectionColor = Color.White;
            _detailsTextBox.AppendText($"{detection.LineNumber}\n");
            
            _detailsTextBox.SelectionColor = Color.Cyan;
            _detailsTextBox.AppendText($"Matched Text: ");
            _detailsTextBox.SelectionColor = Color.Orange;
            _detailsTextBox.SelectionFont = new Font(_detailsTextBox.Font, FontStyle.Bold);
            _detailsTextBox.AppendText($"{detection.MatchedText}\n\n");
            
            // Full line with highlighting
            _detailsTextBox.SelectionColor = Color.Cyan;
            _detailsTextBox.SelectionFont = new Font(_detailsTextBox.Font, FontStyle.Regular);
            _detailsTextBox.AppendText($"Full Line:\n");
            _detailsTextBox.SelectionColor = Color.LightGray;
            
            // Highlight matched text in the full line
            var fullLine = detection.FullLine;
            var matchIndex = fullLine.IndexOf(detection.MatchedText, StringComparison.OrdinalIgnoreCase);
            
            if (matchIndex >= 0)
            {
                // Before match
                _detailsTextBox.AppendText(fullLine.Substring(0, matchIndex));
                
                // Matched text
                _detailsTextBox.SelectionColor = Color.Red;
                _detailsTextBox.SelectionBackColor = Color.Yellow;
                _detailsTextBox.SelectionFont = new Font(_detailsTextBox.Font, FontStyle.Bold);
                _detailsTextBox.AppendText(detection.MatchedText);
                
                // After match
                _detailsTextBox.SelectionColor = Color.LightGray;
                _detailsTextBox.SelectionBackColor = Color.FromArgb(30, 30, 30);
                _detailsTextBox.SelectionFont = new Font(_detailsTextBox.Font, FontStyle.Regular);
                _detailsTextBox.AppendText(fullLine.Substring(matchIndex + detection.MatchedText.Length));
            }
            else
            {
                _detailsTextBox.AppendText(fullLine);
            }
            
            _detailsTextBox.AppendText("\n");
        }
        
        private void OnClearAll(object? sender, EventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to clear all detection matches?", 
                "Confirm Clear", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            
            if (result == DialogResult.Yes)
            {
                _connection.DetectionMatches.Clear();
                LoadDetections();
                _detailsTextBox.Clear();
            }
        }
        
        private void OnExport(object? sender, EventArgs e)
        {
            using (var saveDialog = new SaveFileDialog())
            {
                saveDialog.Filter = "CSV files (*.csv)|*.csv|Text files (*.txt)|*.txt|All files (*.*)|*.*";
                saveDialog.FileName = $"{_connection.Name}_detections_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                
                if (saveDialog.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        using (var writer = new StreamWriter(saveDialog.FileName))
                        {
                            // Write header
                            writer.WriteLine("Timestamp,Pattern,Matched Text,Line Number,Full Line");
                            
                            // Write data
                            foreach (var detection in _connection.DetectionMatches.OrderBy(d => d.Timestamp))
                            {
                                writer.WriteLine($"\"{detection.Timestamp:yyyy-MM-dd HH:mm:ss}\",\"{detection.PatternName}\",\"{detection.MatchedText}\",{detection.LineNumber},\"{detection.FullLine.Replace("\"", "\"\"")}\"");
                            }
                        }
                        
                        MessageBox.Show($"Detections exported successfully to:\n{saveDialog.FileName}", 
                            "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error exporting detections:\n{ex.Message}", 
                            "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
        
        private void OnPatternDetected(object? sender, DetectionMatch match)
        {
            if (InvokeRequired)
            {
                Invoke(() => OnPatternDetected(sender, match));
                return;
            }
            
            // Refresh the grid to show new detection
            LoadDetections();
        }
        
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _connection.PatternDetected -= OnPatternDetected;
            }
            base.Dispose(disposing);
        }
    }
}