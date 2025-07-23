using MultiSerialMonitor.Controls;
using MultiSerialMonitor.Forms;
using MultiSerialMonitor.Models;
using MultiSerialMonitor.Services;

namespace MultiSerialMonitor
{
    public partial class Form1 : Form
    {
        private FlowLayoutPanel _portsPanel;
        private ToolStrip _toolbar;
        private StatusStrip _statusStrip;
        private ToolStripStatusLabel _statusLabel;
        
        private readonly Dictionary<string, IPortMonitor> _monitors = new();
        private readonly Dictionary<string, PortPanel> _portPanels = new();
        private readonly Dictionary<string, ConsoleForm> _consoleForms = new();
        
        public Form1()
        {
            InitializeComponent();
            InitializeCustomComponents();
        }
        
        private void InitializeCustomComponents()
        {
            Text = "Multi Serial Monitor";
            Size = new Size(1000, 700);
            StartPosition = FormStartPosition.CenterScreen;
            
            // Toolbar
            _toolbar = new ToolStrip();
            var addButton = new ToolStripButton
            {
                Text = "Add Port",
                Image = SystemIcons.Shield.ToBitmap(),
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText
            };
            addButton.Click += OnAddPortClick;
            
            var refreshButton = new ToolStripButton
            {
                Text = "Refresh",
                DisplayStyle = ToolStripItemDisplayStyle.Text
            };
            refreshButton.Click += OnRefreshClick;
            
            _toolbar.Items.AddRange(new ToolStripItem[] { addButton, new ToolStripSeparator(), refreshButton });
            
            // Ports panel
            _portsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(10),
                BackColor = Color.White
            };
            
            // Status strip
            _statusStrip = new StatusStrip();
            _statusLabel = new ToolStripStatusLabel
            {
                Text = "Ready"
            };
            _statusStrip.Items.Add(_statusLabel);
            
            Controls.Add(_portsPanel);
            Controls.Add(_toolbar);
            Controls.Add(_statusStrip);
        }
        
        private async void OnAddPortClick(object? sender, EventArgs e)
        {
            using var dialog = new AddPortForm();
            if (dialog.ShowDialog(this) == DialogResult.OK && dialog.Connection != null)
            {
                await AddPortConnectionAsync(dialog.Connection);
            }
        }
        
        private async Task AddPortConnectionAsync(PortConnection connection)
        {
            // Create monitor
            IPortMonitor monitor = connection.Type == ConnectionType.SerialPort
                ? new SerialPortMonitor(connection)
                : new TelnetMonitor(connection);
            
            _monitors[connection.Id] = monitor;
            
            // Create panel
            var panel = new PortPanel(connection)
            {
                Width = _portsPanel.ClientSize.Width - 40
            };
            panel.ExpandRequested += OnPortExpandRequested;
            panel.ConnectRequested += async (s, e) => await ConnectPortAsync(connection);
            panel.DisconnectRequested += async (s, e) => await DisconnectPortAsync(connection);
            panel.RemoveRequested += (s, e) => RemovePort(connection);
            
            _portPanels[connection.Id] = panel;
            _portsPanel.Controls.Add(panel);
            
            // Auto-connect
            try
            {
                await monitor.ConnectAsync();
                _statusLabel.Text = $"Connected to {connection.Name}";
            }
            catch (Exception ex)
            {
                _statusLabel.Text = $"Failed to connect to {connection.Name}: {ex.Message}";
            }
        }
        
        private void OnPortExpandRequested(object? sender, EventArgs e)
        {
            if (sender is PortPanel panel)
            {
                ShowConsoleForm(panel.Connection);
            }
        }
        
        private void ShowConsoleForm(PortConnection connection)
        {
            if (_consoleForms.TryGetValue(connection.Id, out var existingForm))
            {
                existingForm.BringToFront();
                existingForm.WindowState = FormWindowState.Normal;
                return;
            }
            
            if (_monitors.TryGetValue(connection.Id, out var monitor))
            {
                var consoleForm = new ConsoleForm(connection, monitor);
                consoleForm.FormClosed += (s, e) => _consoleForms.Remove(connection.Id);
                _consoleForms[connection.Id] = consoleForm;
                consoleForm.Show();
            }
        }
        
        private async Task ConnectPortAsync(PortConnection connection)
        {
            if (_monitors.TryGetValue(connection.Id, out var monitor))
            {
                try
                {
                    await monitor.ConnectAsync();
                    _statusLabel.Text = $"Connected to {connection.Name}";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to connect: {ex.Message}", "Connection Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    _statusLabel.Text = $"Failed to connect to {connection.Name}";
                }
            }
        }
        
        private async Task DisconnectPortAsync(PortConnection connection)
        {
            if (_monitors.TryGetValue(connection.Id, out var monitor))
            {
                try
                {
                    await monitor.DisconnectAsync();
                    _statusLabel.Text = $"Disconnected from {connection.Name}";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to disconnect: {ex.Message}", "Disconnection Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        
        private void RemovePort(PortConnection connection)
        {
            var result = MessageBox.Show($"Remove {connection.Name}?", "Confirm Remove", 
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            
            if (result == DialogResult.Yes)
            {
                // Close console form if open
                if (_consoleForms.TryGetValue(connection.Id, out var consoleForm))
                {
                    consoleForm.Close();
                }
                
                // Dispose monitor
                if (_monitors.TryGetValue(connection.Id, out var monitor))
                {
                    monitor.Dispose();
                    _monitors.Remove(connection.Id);
                }
                
                // Remove panel
                if (_portPanels.TryGetValue(connection.Id, out var panel))
                {
                    _portsPanel.Controls.Remove(panel);
                    panel.Dispose();
                    _portPanels.Remove(connection.Id);
                }
                
                _statusLabel.Text = $"Removed {connection.Name}";
            }
        }
        
        private void OnRefreshClick(object? sender, EventArgs e)
        {
            // Refresh UI
            foreach (var panel in _portPanels.Values)
            {
                panel.Invalidate();
            }
        }
        
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Close all console forms
            foreach (var form in _consoleForms.Values.ToList())
            {
                form.Close();
            }
            
            // Dispose all monitors
            foreach (var monitor in _monitors.Values)
            {
                monitor.Dispose();
            }
            
            base.OnFormClosing(e);
        }
    }
}
