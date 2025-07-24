using MultiSerialMonitor.Models;
using MultiSerialMonitor.Services;
using MultiSerialMonitor.Localization;

namespace MultiSerialMonitor.Forms
{
    public partial class ConsoleForm : Form, ILocalizable
    {
        private RichTextBox _consoleOutput;
        private TextBox _commandInput;
        private Button _sendButton;
        private ToolStripButton _clearButton;
        private ToolStripButton _disconnectButton;
        private ToolStripButton _connectButton;
        private StatusStrip _statusStrip;
        private ToolStripStatusLabel _statusLabel;
        private ToolStripButton _toggleLineNumbersButton;
        
        private readonly PortConnection _connection;
        private readonly IPortMonitor _monitor;
        private int _lineNumber = 1;
        private bool _showLineNumbers = true;
        
        public ConsoleForm(PortConnection connection, IPortMonitor monitor)
        {
            _connection = connection;
            _monitor = monitor;
            
            InitializeComponents();
            ApplyTheme();
            ApplyLocalization();
            LoadHistory();
            
            _connection.DataReceived += OnDataReceived;
            _connection.StatusChanged += OnStatusChanged;
            _connection.DataCleared += OnDataCleared;
            
            LocalizationManager.LanguageChanged += (s, e) => ApplyLocalization();
            ThemeManager.ThemeChanged += (s, e) => ApplyTheme();
        }
        
        private void InitializeComponents()
        {
            Text = $"Console - {_connection.Name}";
            Size = new Size(900, 700);
            MinimumSize = new Size(600, 400);
            StartPosition = FormStartPosition.CenterScreen;
            
            // Enable double buffering
            SetStyle(ControlStyles.AllPaintingInWmPaint | 
                    ControlStyles.UserPaint | 
                    ControlStyles.DoubleBuffer, true);
            
            // Console output
            _consoleOutput = new RichTextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 10),
                ReadOnly = true,
                WordWrap = false
            };
            
            // Bottom panel for input
            var bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 35,
                Padding = new Padding(5)
            };
            
            _commandInput = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 10),
                Enabled = _monitor.IsConnected
            };
            _commandInput.KeyPress += OnCommandInputKeyPress;
            
            _sendButton = new Button
            {
                Text = "Send",
                Dock = DockStyle.Right,
                Width = 60,
                Enabled = _monitor.IsConnected
            };
            _sendButton.Click += OnSendClick;
            
            bottomPanel.Controls.Add(_commandInput);
            bottomPanel.Controls.Add(_sendButton);
            
            // Top toolbar
            var toolbar = new ToolStrip();
            
            _connectButton = new ToolStripButton
            {
                Text = "Connect",
                Enabled = !_monitor.IsConnected
            };
            _connectButton.Click += async (s, e) => await ConnectAsync();
            
            _disconnectButton = new ToolStripButton
            {
                Text = "Disconnect",
                Enabled = _monitor.IsConnected
            };
            _disconnectButton.Click += async (s, e) => await DisconnectAsync();
            
            _clearButton = new ToolStripButton
            {
                Text = "Clear"
            };
            _clearButton.Click += (s, e) => 
            {
                _consoleOutput.Clear();
                _lineNumber = 1;
            };
            
            var exportButton = new ToolStripButton
            {
                Text = "Export Data"
            };
            exportButton.Click += OnExportClick;
            
            _toggleLineNumbersButton = new ToolStripButton
            {
                Text = "Line #",
                CheckOnClick = true,
                Checked = _showLineNumbers
            };
            _toggleLineNumbersButton.Click += (s, e) =>
            {
                _showLineNumbers = _toggleLineNumbersButton.Checked;
                // Refresh display by reloading history
                _consoleOutput.Clear();
                _lineNumber = 1;
                LoadHistory();
            };
            
            toolbar.Items.AddRange(new ToolStripItem[] { 
                _connectButton, 
                _disconnectButton, 
                new ToolStripSeparator(), 
                _toggleLineNumbersButton,
                exportButton,
                _clearButton 
            });
            
            // Status strip
            _statusStrip = new StatusStrip();
            _statusLabel = new ToolStripStatusLabel
            {
                Text = $"Status: {_connection.Status}"
            };
            _statusStrip.Items.Add(_statusLabel);
            
            Controls.Add(_consoleOutput);
            Controls.Add(bottomPanel);
            Controls.Add(toolbar);
            Controls.Add(_statusStrip);
        }
        
        private void LoadHistory()
        {
            _lineNumber = 1; // Reset line numbers when loading history
            foreach (var line in _connection.OutputHistory.TakeLast(1000))
            {
                AppendLine(line, Color.LightGray);
            }
            _consoleOutput.ScrollToCaret();
        }
        
        private void OnDataReceived(object? sender, string data)
        {
            if (InvokeRequired)
            {
                Invoke(() => OnDataReceived(sender, data));
                return;
            }
            
            Color color = data.StartsWith(">") ? ThemeManager.Colors.ConsoleCommand : 
                         data.StartsWith("Error") ? ThemeManager.Colors.ConsoleError : 
                         ThemeManager.Colors.ConsoleForeground;
            
            // Check if data already contains timestamp
            bool hasTimestamp = false;
            if (data.Length > 0 && data[0] == '[')
            {
                int closeBracket = data.IndexOf(']');
                if (closeBracket > 1 && closeBracket < 50)
                {
                    hasTimestamp = true;
                }
            }
            
            if (hasTimestamp)
            {
                AppendLine(data, color);
            }
            else
            {
                AppendLine($"[{DateTime.Now:HH:mm:ss}] {data}", color);
            }
        }
        
        private void OnStatusChanged(object? sender, ConnectionStatus status)
        {
            if (InvokeRequired)
            {
                Invoke(() => OnStatusChanged(sender, status));
                return;
            }
            
            _statusLabel.Text = $"Status: {status}";
            bool isConnected = status == ConnectionStatus.Connected;
            
            _connectButton.Enabled = !isConnected;
            _disconnectButton.Enabled = isConnected;
            _commandInput.Enabled = isConnected;
            _sendButton.Enabled = isConnected;
        }
        
        private void AppendLine(string text, Color color)
        {
            _consoleOutput.SelectionStart = _consoleOutput.TextLength;
            _consoleOutput.SelectionLength = 0;
            
            if (_showLineNumbers)
            {
                // Add line number in gray color
                _consoleOutput.SelectionColor = ThemeManager.Colors.ConsoleLineNumber;
                _consoleOutput.AppendText($"{_lineNumber,5}: ");
                _lineNumber++;
            }
            
            _consoleOutput.SelectionColor = color;
            _consoleOutput.AppendText(text + Environment.NewLine);
            _consoleOutput.ScrollToCaret();
        }
        
        private async void OnSendClick(object? sender, EventArgs e)
        {
            await SendCommandAsync();
        }
        
        private async void OnCommandInputKeyPress(object? sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                e.Handled = true;
                await SendCommandAsync();
            }
        }
        
        private async Task SendCommandAsync()
        {
            if (string.IsNullOrWhiteSpace(_commandInput.Text)) return;
            
            var command = _commandInput.Text.Trim();
            
            // Validate command length
            if (command.Length > 1000)
            {
                MessageBox.Show(LocalizationManager.GetString("CommandTooLong"), 
                    LocalizationManager.GetString("InvalidCommand"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            try
            {
                _commandInput.Enabled = false;
                _sendButton.Enabled = false;
                
                await _monitor.SendCommandAsync(command);
                _commandInput.Clear();
                _commandInput.Focus();
            }
            catch (InvalidOperationException ex)
            {
                var message = string.Format(LocalizationManager.GetString("CannotSendCommand"), ex.Message);
                MessageBox.Show(message, LocalizationManager.GetString("ConnectionError"), 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                Utils.ErrorHandler.ShowError(this, ex, "Send Command Error");
            }
            finally
            {
                _commandInput.Enabled = _monitor.IsConnected;
                _sendButton.Enabled = _monitor.IsConnected;
            }
        }
        
        private async Task ConnectAsync()
        {
            try
            {
                await _monitor.ConnectAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error connecting: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private async Task DisconnectAsync()
        {
            try
            {
                await _monitor.DisconnectAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error disconnecting: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void OnExportClick(object? sender, EventArgs e)
        {
            if (_connection.OutputHistory.Count == 0)
            {
                MessageBox.Show($"No data to export for {_connection.Name}.", "Export Data", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            
            using var saveDialog = new SaveFileDialog
            {
                Filter = LocalizationManager.CurrentLanguage == Language.Thai
                    ? "ไฟล์ข้อความ (*.txt)|*.txt|ไฟล์ CSV (*.csv)|*.csv|ไฟล์บันทึก (*.log)|*.log|Rich Text Format (*.rtf)|*.rtf|ไฟล์ทั้งหมด (*.*)|*.*"
                    : "Text files (*.txt)|*.txt|CSV files (*.csv)|*.csv|Log files (*.log)|*.log|Rich Text Format (*.rtf)|*.rtf|All files (*.*)|*.*",
                FileName = $"{_connection.Name}_{DateTime.Now:yyyyMMdd_HHmmss}",
                DefaultExt = "txt"
            };
            
            if (saveDialog.ShowDialog(this) == DialogResult.OK)
            {
                try
                {
                    var extension = Path.GetExtension(saveDialog.FileName).ToLower();
                    
                    if (extension == ".csv")
                    {
                        // Export as CSV with timestamp, line number, and data
                        // Use UTF-8 with BOM for proper Thai support
                        var encoding = new System.Text.UTF8Encoding(true);
                        using (var writer = new StreamWriter(saveDialog.FileName, false, encoding))
                        {
                            // Write header with localized column names
                            if (LocalizationManager.CurrentLanguage == Language.Thai)
                            {
                                writer.WriteLine("เวลา,บรรทัดที่,ข้อมูล");
                            }
                            else
                            {
                                writer.WriteLine("Timestamp,Line Number,Data");
                            }
                            
                            int lineNumber = 1;
                            foreach (var line in _connection.OutputHistory)
                            {
                                var timestamp = "";
                                var data = line;
                                
                                var match = System.Text.RegularExpressions.Regex.Match(line, @"^\[(.*?)\](.*)");
                                if (match.Success)
                                {
                                    timestamp = match.Groups[1].Value;
                                    data = match.Groups[2].Value.Trim();
                                }
                                
                                timestamp = $"\"{timestamp}\"";
                                data = $"\"{data.Replace("\"", "\"\"")}\"";
                                
                                writer.WriteLine($"{timestamp},{lineNumber},{data}");
                                lineNumber++;
                            }
                        }
                    }
                    else if (extension == ".rtf")
                    {
                        // Export as RTF with colors preserved
                        _consoleOutput.SaveFile(saveDialog.FileName, RichTextBoxStreamType.RichText);
                    }
                    else
                    {
                        // Export as plain text
                        File.WriteAllLines(saveDialog.FileName, _connection.OutputHistory);
                    }
                    
                    var successMessage = LocalizationManager.CurrentLanguage == Language.Thai
                        ? $"ส่งออกข้อมูลสำเร็จไปที่:\n{saveDialog.FileName}\n\nจำนวนบรรทัดทั้งหมด: {_connection.OutputHistory.Count}"
                        : $"Data exported successfully to:\n{saveDialog.FileName}\n\nTotal lines: {_connection.OutputHistory.Count}";
                    var successTitle = LocalizationManager.CurrentLanguage == Language.Thai
                        ? "ส่งออกสำเร็จ"
                        : "Export Success";
                    MessageBox.Show(successMessage, successTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    var errorMessage = LocalizationManager.CurrentLanguage == Language.Thai
                        ? $"เกิดข้อผิดพลาดในการส่งออกข้อมูล: {ex.Message}"
                        : $"Error exporting data: {ex.Message}";
                    var errorTitle = LocalizationManager.CurrentLanguage == Language.Thai
                        ? "ข้อผิดพลาดการส่งออก"
                        : "Export Error";
                    MessageBox.Show(errorMessage, errorTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        
        private void OnDataCleared(object? sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(() => OnDataCleared(sender, e));
                return;
            }
            
            _consoleOutput.Clear();
            _lineNumber = 1;
        }
        
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _connection.DataReceived -= OnDataReceived;
            _connection.StatusChanged -= OnStatusChanged;
            _connection.DataCleared -= OnDataCleared;
            base.OnFormClosed(e);
        }
        
        public void ApplyTheme()
        {
            // Apply theme to form
            ThemeManager.ApplyTheme(this);
            
            // Console output uses special console colors
            _consoleOutput.BackColor = ThemeManager.Colors.ConsoleBackground;
            _consoleOutput.ForeColor = ThemeManager.Colors.ConsoleForeground;
            
            // Apply theme to toolbar
            var toolbar = Controls.OfType<ToolStrip>().FirstOrDefault();
            if (toolbar != null)
            {
                ThemeManager.ApplyTheme(toolbar);
            }
            
            // Apply theme to status strip
            if (_statusStrip != null)
            {
                ThemeManager.ApplyTheme(_statusStrip);
            }
        }
        
        public void ApplyLocalization()
        {
            Text = $"{LocalizationManager.GetString("Console")} - {_connection.Name}";
            
            // Update button texts
            if (_connectButton != null)
                _connectButton.Text = LocalizationManager.GetString("Connect");
            if (_disconnectButton != null)
                _disconnectButton.Text = LocalizationManager.GetString("Disconnect");
            if (_clearButton != null)
                _clearButton.Text = LocalizationManager.GetString("Clear");
            if (_toggleLineNumbersButton != null)
                _toggleLineNumbersButton.Text = LocalizationManager.GetString("LineNumbers");
            if (_sendButton != null)
                _sendButton.Text = LocalizationManager.GetString("Send");
                
            // Update status label
            if (_statusLabel != null)
            {
                var statusText = LocalizationManager.GetString("Status");
                var statusValue = LocalizationManager.GetString(_connection.Status.ToString());
                _statusLabel.Text = $"{statusText}: {statusValue}";
            }
            
            // Update export button
            var toolbar = Controls.OfType<ToolStrip>().FirstOrDefault();
            if (toolbar != null)
            {
                var exportButton = toolbar.Items.OfType<ToolStripButton>()
                    .FirstOrDefault(b => b.Text == "Export Data" || b.Text == "ส่งออกข้อมูล");
                if (exportButton != null)
                    exportButton.Text = LocalizationManager.GetString("ExportData");
            }
        }
    }
}