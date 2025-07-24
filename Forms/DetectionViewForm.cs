using MultiSerialMonitor.Models;
using MultiSerialMonitor.Localization;
using MultiSerialMonitor.Services;
using System.Text;

namespace MultiSerialMonitor.Forms
{
    public partial class DetectionViewForm : Form, ILocalizable
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
            ApplyTheme();
            ApplyLocalization();
            LoadDetections();
            
            LocalizationManager.LanguageChanged += (s, e) => ApplyLocalization();
            ThemeManager.ThemeChanged += (s, e) => ApplyTheme();
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
            
            if (LocalizationManager.CurrentLanguage == Language.Thai)
            {
                _summaryLabel.Text = $"รายการตรวจพบทั้งหมด: {totalCount} | {string.Join(" | ", patternCounts)}";
            }
            else
            {
                _summaryLabel.Text = $"Total Detections: {totalCount} | {string.Join(" | ", patternCounts)}";
            }
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
            
            var headerText = LocalizationManager.CurrentLanguage == Language.Thai ? "รายละเอียดการตรวจพบ" : "Detection Details";
            _detailsTextBox.AppendText($"{headerText}\n");
            _detailsTextBox.AppendText(new string('-', 80) + "\n\n");
            
            // Details
            _detailsTextBox.SelectionColor = Color.Cyan;
            var patternLabel = LocalizationManager.CurrentLanguage == Language.Thai ? "รูปแบบ: " : "Pattern: ";
            _detailsTextBox.AppendText(patternLabel);
            _detailsTextBox.SelectionColor = Color.White;
            _detailsTextBox.AppendText($"{detection.PatternName}\n");
            
            _detailsTextBox.SelectionColor = Color.Cyan;
            var timestampLabel = LocalizationManager.CurrentLanguage == Language.Thai ? "เวลา: " : "Timestamp: ";
            _detailsTextBox.AppendText(timestampLabel);
            _detailsTextBox.SelectionColor = Color.White;
            _detailsTextBox.AppendText($"{detection.Timestamp:yyyy-MM-dd HH:mm:ss.fff}\n");
            
            _detailsTextBox.SelectionColor = Color.Cyan;
            var lineNumberLabel = LocalizationManager.CurrentLanguage == Language.Thai ? "บรรทัดที่: " : "Line Number: ";
            _detailsTextBox.AppendText(lineNumberLabel);
            _detailsTextBox.SelectionColor = Color.White;
            _detailsTextBox.AppendText($"{detection.LineNumber}\n");
            
            _detailsTextBox.SelectionColor = Color.Cyan;
            var matchedTextLabel = LocalizationManager.CurrentLanguage == Language.Thai ? "ข้อความที่ตรงกัน: " : "Matched Text: ";
            _detailsTextBox.AppendText(matchedTextLabel);
            _detailsTextBox.SelectionColor = Color.Orange;
            _detailsTextBox.SelectionFont = new Font(_detailsTextBox.Font, FontStyle.Bold);
            _detailsTextBox.AppendText($"{detection.MatchedText}\n\n");
            
            // Full line with highlighting
            _detailsTextBox.SelectionColor = Color.Cyan;
            _detailsTextBox.SelectionFont = new Font(_detailsTextBox.Font, FontStyle.Regular);
            var fullLineLabel = LocalizationManager.CurrentLanguage == Language.Thai ? "บรรทัดเต็ม:\n" : "Full Line:\n";
            _detailsTextBox.AppendText(fullLineLabel);
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
            var message = LocalizationManager.CurrentLanguage == Language.Thai
                ? "คุณแน่ใจหรือไม่ที่จะล้างรายการตรวจพบทั้งหมด?"
                : "Are you sure you want to clear all detection matches?";
            var title = LocalizationManager.CurrentLanguage == Language.Thai
                ? "ยืนยันการล้าง"
                : "Confirm Clear";
                
            var result = MessageBox.Show(message, title, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            
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
                var csvFilter = LocalizationManager.CurrentLanguage == Language.Thai 
                    ? "ไฟล์ CSV (*.csv)|*.csv|ไฟล์ข้อความ (*.txt)|*.txt|ไฟล์ทั้งหมด (*.*)|*.*"
                    : "CSV files (*.csv)|*.csv|Text files (*.txt)|*.txt|All files (*.*)|*.*";
                    
                saveDialog.Filter = csvFilter;
                saveDialog.FileName = $"{_connection.Name}_detections_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                
                if (saveDialog.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        // Use UTF-8 with BOM for proper Thai support in Excel
                        var encoding = new UTF8Encoding(true);
                        using (var writer = new StreamWriter(saveDialog.FileName, false, encoding))
                        {
                            // Write header with localized column names
                            var headers = new List<string>();
                            if (LocalizationManager.CurrentLanguage == Language.Thai)
                            {
                                headers.Add("เวลา");
                                headers.Add("รูปแบบ");
                                headers.Add("ข้อความที่ตรงกัน");
                                headers.Add("บรรทัดที่");
                                headers.Add("บรรทัดเต็ม");
                            }
                            else
                            {
                                headers.Add("Timestamp");
                                headers.Add("Pattern");
                                headers.Add("Matched Text");
                                headers.Add("Line Number");
                                headers.Add("Full Line");
                            }
                            
                            writer.WriteLine(string.Join(",", headers));
                            
                            // Write data
                            foreach (var detection in _connection.DetectionMatches.OrderBy(d => d.Timestamp))
                            {
                                var fields = new List<string>
                                {
                                    $"\"{detection.Timestamp:yyyy-MM-dd HH:mm:ss}\"",
                                    $"\"{EscapeCsvField(detection.PatternName)}\"",
                                    $"\"{EscapeCsvField(detection.MatchedText)}\"",
                                    detection.LineNumber.ToString(),
                                    $"\"{EscapeCsvField(detection.FullLine)}\""
                                };
                                
                                writer.WriteLine(string.Join(",", fields));
                            }
                        }
                        
                        var successMessage = LocalizationManager.CurrentLanguage == Language.Thai
                            ? $"ส่งออกการตรวจพบสำเร็จไปที่:\n{saveDialog.FileName}"
                            : $"Detections exported successfully to:\n{saveDialog.FileName}";
                            
                        var successTitle = LocalizationManager.CurrentLanguage == Language.Thai
                            ? "ส่งออกสำเร็จ"
                            : "Export Complete";
                            
                        MessageBox.Show(successMessage, successTitle, 
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        var errorMessage = LocalizationManager.CurrentLanguage == Language.Thai
                            ? $"เกิดข้อผิดพลาดในการส่งออก:\n{ex.Message}"
                            : $"Error exporting detections:\n{ex.Message}";
                            
                        var errorTitle = LocalizationManager.CurrentLanguage == Language.Thai
                            ? "ข้อผิดพลาดการส่งออก"
                            : "Export Error";
                            
                        MessageBox.Show(errorMessage, errorTitle, 
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
        
        private string EscapeCsvField(string field)
        {
            if (field == null) return "";
            return field.Replace("\"", "\"\"");
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
        
        public void ApplyTheme()
        {
            ThemeManager.ApplyTheme(this);
            
            // Apply theme to DataGridView
            if (_detectionsGrid != null)
            {
                _detectionsGrid.BackgroundColor = ThemeManager.Colors.ControlBackground;
                _detectionsGrid.GridColor = ThemeManager.Colors.ControlBorder;
                _detectionsGrid.DefaultCellStyle.BackColor = ThemeManager.Colors.ControlBackground;
                _detectionsGrid.DefaultCellStyle.ForeColor = ThemeManager.Colors.ControlForeground;
                _detectionsGrid.DefaultCellStyle.SelectionBackColor = ThemeManager.CurrentTheme == Theme.Dark 
                    ? Color.FromArgb(70, 70, 70) 
                    : SystemColors.Highlight;
                _detectionsGrid.DefaultCellStyle.SelectionForeColor = ThemeManager.CurrentTheme == Theme.Dark 
                    ? Color.White 
                    : SystemColors.HighlightText;
                _detectionsGrid.ColumnHeadersDefaultCellStyle.BackColor = ThemeManager.Colors.PanelBackground;
                _detectionsGrid.ColumnHeadersDefaultCellStyle.ForeColor = ThemeManager.Colors.ControlForeground;
                _detectionsGrid.EnableHeadersVisualStyles = false;
            }
            
            // Details textbox already uses console colors
            if (_detailsTextBox != null)
            {
                _detailsTextBox.BackColor = ThemeManager.Colors.ConsoleBackground;
                _detailsTextBox.ForeColor = ThemeManager.Colors.ConsoleForeground;
            }
            
            // Apply theme to buttons
            var buttons = new[] { _clearButton, _exportButton, _closeButton };
            foreach (var button in buttons)
            {
                if (button != null)
                {
                    ThemeManager.ApplyTheme(button);
                }
            }
        }
        
        public void ApplyLocalization()
        {
            // Update form title
            Text = LocalizationManager.CurrentLanguage == Language.Thai
                ? $"รายการตรวจพบ - {_connection.Name}"
                : $"Detection Matches - {_connection.Name}";
            
            // Update column headers
            if (_detectionsGrid != null && _detectionsGrid.Columns.Count >= 5)
            {
                if (LocalizationManager.CurrentLanguage == Language.Thai)
                {
                    _detectionsGrid.Columns["Timestamp"].HeaderText = "เวลา";
                    _detectionsGrid.Columns["PatternName"].HeaderText = "รูปแบบ";
                    _detectionsGrid.Columns["MatchedText"].HeaderText = "ข้อความที่ตรงกัน";
                    _detectionsGrid.Columns["LineNumber"].HeaderText = "บรรทัดที่";
                    _detectionsGrid.Columns["FullLine"].HeaderText = "บรรทัดเต็ม";
                }
                else
                {
                    _detectionsGrid.Columns["Timestamp"].HeaderText = "Timestamp";
                    _detectionsGrid.Columns["PatternName"].HeaderText = "Pattern";
                    _detectionsGrid.Columns["MatchedText"].HeaderText = "Matched Text";
                    _detectionsGrid.Columns["LineNumber"].HeaderText = "Line #";
                    _detectionsGrid.Columns["FullLine"].HeaderText = "Full Line";
                }
            }
            
            // Update buttons
            if (_clearButton != null)
                _clearButton.Text = LocalizationManager.CurrentLanguage == Language.Thai ? "ล้างทั้งหมด" : "Clear All";
            if (_exportButton != null)
                _exportButton.Text = LocalizationManager.CurrentLanguage == Language.Thai ? "ส่งออก" : "Export";
            if (_closeButton != null)
                _closeButton.Text = LocalizationManager.CurrentLanguage == Language.Thai ? "ปิด" : "Close";
                
            // Update summary
            UpdateSummary();
        }
    }
}