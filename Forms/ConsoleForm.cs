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
        
        private readonly PortConnection _connection;
        private readonly IPortMonitor _monitor;
        
        public ConsoleForm(PortConnection connection, IPortMonitor monitor)
        {
            _connection = connection;
            _monitor = monitor;
            
            InitializeComponents();
            LoadHistory();
            
            _connection.DataReceived += OnDataReceived;
            _connection.StatusChanged += OnStatusChanged;
        }
        
        private void InitializeComponents()
        {
            Text = $"Console - {_connection.Name}";
            Size = new Size(800, 600);
            StartPosition = FormStartPosition.CenterScreen;
            
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
            _clearButton.Click += (s, e) => _consoleOutput.Clear();
            
            toolbar.Items.AddRange(new ToolStripItem[] { 
                _connectButton, 
                _disconnectButton, 
                new ToolStripSeparator(), 
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
            
            try
            {
                await _monitor.SendCommandAsync(_commandInput.Text);
                _commandInput.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error sending command: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
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
        
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _connection.DataReceived -= OnDataReceived;
            _connection.StatusChanged -= OnStatusChanged;
            base.OnFormClosed(e);
        }
    }
}