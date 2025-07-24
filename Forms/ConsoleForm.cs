using MultiSerialMonitor.Models;
using MultiSerialMonitor.Services;

namespace MultiSerialMonitor.Forms
{
    public partial class ConsoleForm : Form
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
            LoadHistory();
            
            _connection.DataReceived += OnDataReceived;
            _connection.StatusChanged += OnStatusChanged;
            _connection.DataCleared += OnDataCleared;
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
                BackColor = Color.Black,
                ForeColor = Color.LightGray,
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
            
            Color color = data.StartsWith(">") ? Color.Cyan : 
                         data.StartsWith("Error") ? Color.Red : 
                         Color.LightGray;
            
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
                _consoleOutput.SelectionColor = Color.DarkGray;
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
                MessageBox.Show("Command is too long. Maximum 1000 characters allowed.", 
                    "Invalid Command", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                MessageBox.Show($"Cannot send command: {ex.Message}", "Connection Error", 
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
                Filter = "Text files (*.txt)|*.txt|CSV files (*.csv)|*.csv|Log files (*.log)|*.log|Rich Text Format (*.rtf)|*.rtf|All files (*.*)|*.*",
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
                        var csvLines = new List<string> { "Timestamp,Line Number,Data" };
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
                            
                            csvLines.Add($"{timestamp},{lineNumber},{data}");
                            lineNumber++;
                        }
                        File.WriteAllLines(saveDialog.FileName, csvLines);
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
                    
                    MessageBox.Show($"Data exported successfully to:\n{saveDialog.FileName}\n\nTotal lines: {_connection.OutputHistory.Count}", 
                        "Export Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error exporting data: {ex.Message}", 
                        "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
    }
}