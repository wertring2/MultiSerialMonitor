using System.Diagnostics;
using MultiSerialMonitor.Controls;
using MultiSerialMonitor.Forms;
using MultiSerialMonitor.Models;
using MultiSerialMonitor.Services;
using MultiSerialMonitor.Exceptions;

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
        private readonly ConfigurationManager _configManager = new();
        
        public Form1()
        {
            InitializeComponent();
            InitializeCustomComponents();
            LoadSavedConfiguration();
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
            
            var removeAllButton = new ToolStripButton
            {
                Text = "Remove All",
                DisplayStyle = ToolStripItemDisplayStyle.Text
            };
            removeAllButton.Click += OnRemoveAllClick;
            
            var clearAllButton = new ToolStripButton
            {
                Text = "Clear All Data",
                DisplayStyle = ToolStripItemDisplayStyle.Text
            };
            clearAllButton.Click += OnClearAllClick;
            
            // Profile management dropdown
            var profileDropDown = new ToolStripDropDownButton
            {
                Text = "Profiles",
                DisplayStyle = ToolStripItemDisplayStyle.Text
            };
            
            var saveProfileItem = new ToolStripMenuItem("Save Profile...");
            saveProfileItem.Click += OnSaveProfileClick;
            
            var loadProfileItem = new ToolStripMenuItem("Load Profile");
            
            var manageProfilesItem = new ToolStripMenuItem("Manage Profiles...");
            manageProfilesItem.Click += OnManageProfilesClick;
            
            profileDropDown.DropDownItems.AddRange(new ToolStripItem[] {
                saveProfileItem,
                loadProfileItem,
                new ToolStripSeparator(),
                manageProfilesItem
            });
            
            // Update load profile submenu dynamically
            profileDropDown.DropDownOpening += (s, e) => UpdateLoadProfileMenu(loadProfileItem);
            
            _toolbar.Items.AddRange(new ToolStripItem[] { 
                addButton, 
                new ToolStripSeparator(), 
                refreshButton,
                new ToolStripSeparator(),
                profileDropDown,
                new ToolStripSeparator(),
                clearAllButton,
                removeAllButton 
            });
            
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
        
        private bool IsPortInUse(string portName, ConnectionType type, out string existingConnectionName)
        {
            existingConnectionName = "";
            if (type != ConnectionType.SerialPort)
                return false;
                
            var existingConnection = _portPanels.Values.FirstOrDefault(panel => 
                panel.Connection.Type == ConnectionType.SerialPort && 
                panel.Connection.PortName.Equals(portName, StringComparison.OrdinalIgnoreCase));
                
            if (existingConnection != null)
            {
                existingConnectionName = existingConnection.Connection.Name;
                return true;
            }
            
            return false;
        }
        
        private async Task<bool> AddPortConnectionAsync(PortConnection connection, bool isFromProfile = false)
        {
            // Check if port is already in use by another connection in this application
            if (connection.Type == ConnectionType.SerialPort && IsPortInUse(connection.PortName, connection.Type, out string existingName))
            {
                if (!isFromProfile)
                {
                    MessageBox.Show(
                        $"Port {connection.PortName} is already configured for connection '{existingName}'.\n\n" +
                        "Each serial port can only be used by one connection at a time.\n" +
                        "Please choose a different port or remove the existing connection first.",
                        "Port Already In Use",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
                return false;
            }
            
            // Check if serial port exists before creating monitor (for profiles only)
            if (isFromProfile && connection.Type == ConnectionType.SerialPort)
            {
                var availablePorts = System.IO.Ports.SerialPort.GetPortNames();
                if (!availablePorts.Contains(connection.PortName))
                {
                    // Don't create the connection if port doesn't exist
                    return false;
                }
            }
            
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
            panel.ConfigureDetectionRequested += (s, e) => OnPortConfigureDetectionRequested(connection);
            panel.ViewDetectionsRequested += (s, e) => OnPortViewDetectionsRequested(connection);
            panel.ClearDataRequested += (s, e) => OnPortClearDataRequested(connection);
            
            _portPanels[connection.Id] = panel;
            _portsPanel.Controls.Add(panel);
            
            // Auto-save configuration
            SaveConfiguration();
            
            // Auto-connect
            try
            {
                await monitor.ConnectAsync();
                _statusLabel.Text = $"Connected to {connection.Name}";
                return true;
            }
            catch (PortNotFoundException pnfEx)
            {
                // For profiles, we should have already checked this above
                // For manual additions, re-throw to show the dialog
                if (!isFromProfile)
                {
                    throw;
                }
                
                // This shouldn't happen as we check above, but just in case
                await RemovePortAsync(connection);
                return false;
            }
            catch (Exception ex)
            {
                _statusLabel.Text = $"Failed to connect to {connection.Name}";
                
                if (isFromProfile)
                {
                    // For profiles, just skip failed connections
                    await RemovePortAsync(connection);
                    return false;
                }
                
                // Show options to user for manual additions
                var result = MessageBox.Show(
                    $"Failed to connect to {connection.Name}.\n\n" +
                    $"Error: {ex.Message}\n\n" +
                    "Would you like to:\n" +
                    "• Retry - Try connecting again\n" +
                    "• Ignore - Keep the port but don't connect\n" +
                    "• Abort - Remove the port",
                    "Connection Failed",
                    MessageBoxButtons.AbortRetryIgnore,
                    MessageBoxIcon.Warning);
                
                switch (result)
                {
                    case DialogResult.Retry:
                        // Try again
                        try
                        {
                            await monitor.ConnectAsync();
                            _statusLabel.Text = $"Connected to {connection.Name}";
                            return true;
                        }
                        catch
                        {
                            _statusLabel.Text = $"Still failed to connect to {connection.Name}";
                            return true; // Keep the port even if connection failed
                        }
                    
                    case DialogResult.Abort:
                        // Remove the port
                        await RemovePortAsync(connection);
                        _statusLabel.Text = $"Removed {connection.Name}";
                        return false;
                    
                    case DialogResult.Ignore:
                        // Keep port in disconnected state
                        _statusLabel.Text = $"Port {connection.Name} added but not connected";
                        return true;
                }
            }
            
            return true;
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
        
        private async void RemovePort(PortConnection connection)
        {
            var result = MessageBox.Show($"Remove {connection.Name}?", "Confirm Remove", 
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            
            if (result == DialogResult.Yes)
            {
                try
                {
                    Cursor = Cursors.WaitCursor;
                    _statusLabel.Text = $"Removing {connection.Name}...";
                    Application.DoEvents(); // Force UI update
                    
                    await RemovePortAsync(connection);
                    
                    _statusLabel.Text = $"Removed {connection.Name}";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error removing port: {ex.Message}", "Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    _statusLabel.Text = "Error removing port";
                }
                finally
                {
                    Cursor = Cursors.Default;
                }
            }
        }
        
        private async Task RemovePortAsync(PortConnection connection)
        {
            // Close console form if open
            if (_consoleForms.TryGetValue(connection.Id, out var consoleForm))
            {
                if (InvokeRequired)
                {
                    Invoke(() => consoleForm.Close());
                }
                else
                {
                    consoleForm.Close();
                }
                _consoleForms.Remove(connection.Id);
            }
            
            // Disconnect and dispose monitor in background
            if (_monitors.TryGetValue(connection.Id, out var monitor))
            {
                await Task.Run(async () =>
                {
                    try
                    {
                        if (monitor.IsConnected)
                        {
                            await monitor.DisconnectAsync();
                        }
                        monitor.Dispose();
                    }
                    catch (Exception ex)
                    {
                        // Log error but continue with removal
                        Debug.WriteLine($"Error disposing monitor: {ex.Message}");
                    }
                });
                _monitors.Remove(connection.Id);
            }
            
            // Remove panel on UI thread
            if (_portPanels.TryGetValue(connection.Id, out var panel))
            {
                if (InvokeRequired)
                {
                    Invoke(() =>
                    {
                        _portsPanel.Controls.Remove(panel);
                        panel.Dispose();
                    });
                }
                else
                {
                    _portsPanel.Controls.Remove(panel);
                    panel.Dispose();
                }
                _portPanels.Remove(connection.Id);
            }
            
            // Auto-save configuration after removal
            SaveConfiguration();
        }
        
        private void OnRefreshClick(object? sender, EventArgs e)
        {
            // Refresh UI
            foreach (var panel in _portPanels.Values)
            {
                panel.Invalidate();
            }
        }
        
        private void OnPortConfigureDetectionRequested(PortConnection connection)
        {
            using var configForm = new DetectionConfigForm(connection);
            if (configForm.ShowDialog(this) == DialogResult.OK)
            {
                // Save configuration after detection patterns are modified
                SaveConfiguration();
            }
        }
        
        private void OnPortViewDetectionsRequested(PortConnection connection)
        {
            using var viewForm = new DetectionViewForm(connection);
            viewForm.ShowDialog(this);
        }
        
        private void OnPortClearDataRequested(PortConnection connection)
        {
            var result = MessageBox.Show($"Are you sure you want to clear all data for {connection.Name}?", 
                "Confirm Clear", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            
            if (result == DialogResult.Yes)
            {
                connection.ClearData();
                _statusLabel.Text = $"Cleared data for {connection.Name}";
            }
        }
        
        private void OnClearAllClick(object? sender, EventArgs e)
        {
            if (_portPanels.Count == 0)
            {
                MessageBox.Show("No ports to clear.", "Information", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            
            var result = MessageBox.Show("Are you sure you want to clear all data for all ports?", 
                "Confirm Clear All", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            
            if (result == DialogResult.Yes)
            {
                foreach (var connection in _monitors.Keys.Select(id => _portPanels[id].Connection))
                {
                    connection.ClearData();
                }
                _statusLabel.Text = "Cleared all data";
            }
        }
        
        private async void OnRemoveAllClick(object? sender, EventArgs e)
        {
            if (_portPanels.Count == 0)
            {
                MessageBox.Show("No ports to remove.", "Information", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            
            var result = MessageBox.Show($"Remove all {_portPanels.Count} port(s)?", "Confirm Remove All", 
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            
            if (result == DialogResult.Yes)
            {
                try
                {
                    Cursor = Cursors.WaitCursor;
                    _statusLabel.Text = "Removing all ports...";
                    Application.DoEvents();
                    
                    // Get all connections to remove
                    var connectionsToRemove = _portPanels.Values
                        .Select(p => p.Connection)
                        .ToList();
                    
                    // Remove each connection
                    foreach (var connection in connectionsToRemove)
                    {
                        await RemovePortAsync(connection);
                    }
                    
                    _statusLabel.Text = $"Removed all ports";
                }
                finally
                {
                    Cursor = Cursors.Default;
                }
            }
        }
        
        private async Task RemoveAllConnectionsAsync()
        {
            try
            {
                Cursor = Cursors.WaitCursor;
                _statusLabel.Text = "Removing all ports...";
                Application.DoEvents();
                
                // Get all connections to remove
                var connectionsToRemove = _portPanels.Values
                    .Select(p => p.Connection)
                    .ToList();
                
                // Remove each connection
                foreach (var connection in connectionsToRemove)
                {
                    await RemovePortAsync(connection);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error removing ports: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }
        
        private async void LoadSavedConfiguration()
        {
            try
            {
                var savedConnections = _configManager.LoadConfiguration();
                var skippedPorts = new List<string>();
                var loadedCount = 0;
                
                foreach (var connection in savedConnections)
                {
                    if (await AddPortConnectionAsync(connection, isFromProfile: true))
                    {
                        loadedCount++;
                    }
                    else if (connection.Type == ConnectionType.SerialPort)
                    {
                        skippedPorts.Add($"{connection.Name} ({connection.PortName})");
                    }
                }
                
                if (loadedCount > 0)
                {
                    _statusLabel.Text = $"Loaded {loadedCount} saved connection(s)";
                }
                
                // Show warning if some ports were skipped during startup
                if (skippedPorts.Count > 0)
                {
                    MessageBox.Show(
                        $"The following saved ports could not be loaded:\n\n" +
                        string.Join("\n", skippedPorts.Select(p => $"• {p}")) +
                        "\n\nThese ports are not currently available. " +
                        "They may be unplugged or in use by another application.",
                        "Some Ports Not Available",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading saved configuration: {ex.Message}", 
                    "Load Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        
        private void SaveConfiguration()
        {
            var connections = _portPanels.Values.Select(p => p.Connection).ToList();
            _configManager.SaveConfiguration(connections);
        }
        
        private void UpdateLoadProfileMenu(ToolStripMenuItem loadProfileItem)
        {
            loadProfileItem.DropDownItems.Clear();
            
            var profiles = _configManager.GetAvailableProfiles();
            if (profiles.Length == 0)
            {
                var noProfilesItem = new ToolStripMenuItem("(No profiles)") { Enabled = false };
                loadProfileItem.DropDownItems.Add(noProfilesItem);
            }
            else
            {
                foreach (var profile in profiles)
                {
                    var profileItem = new ToolStripMenuItem(profile);
                    profileItem.Click += (s, e) => LoadProfile(profile);
                    loadProfileItem.DropDownItems.Add(profileItem);
                }
            }
        }
        
        private async void LoadProfile(string profileName)
        {
            try
            {
                // Ask user if they want to replace or append
                DialogResult result = DialogResult.Yes;
                if (_portPanels.Count > 0)
                {
                    result = MessageBox.Show(
                        "Do you want to replace existing connections?\n\n" +
                        "Yes - Remove existing connections and load profile\n" +
                        "No - Add profile connections to existing ones\n" +
                        "Cancel - Do nothing",
                        "Load Profile",
                        MessageBoxButtons.YesNoCancel,
                        MessageBoxIcon.Question);
                    
                    if (result == DialogResult.Cancel)
                        return;
                    
                    if (result == DialogResult.Yes)
                    {
                        // Remove all existing connections first
                        await RemoveAllConnectionsAsync();
                    }
                }
                
                var connections = _configManager.LoadProfile(profileName);
                var skippedPorts = new List<string>();
                var loadedCount = 0;
                
                foreach (var connection in connections)
                {
                    if (await AddPortConnectionAsync(connection, isFromProfile: true))
                    {
                        loadedCount++;
                    }
                    else if (connection.Type == ConnectionType.SerialPort)
                    {
                        skippedPorts.Add($"{connection.Name} ({connection.PortName})");
                    }
                }
                
                _statusLabel.Text = $"Loaded profile '{profileName}' with {loadedCount} connection(s)";
                
                // Show warning if some ports were skipped
                if (skippedPorts.Count > 0)
                {
                    MessageBox.Show(
                        $"The following ports from the profile could not be loaded:\n\n" +
                        string.Join("\n", skippedPorts.Select(p => $"• {p}")) +
                        "\n\nThese ports are not currently available. " +
                        "They may be unplugged or in use by another application.",
                        "Some Ports Not Available",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading profile: {ex.Message}", 
                    "Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void OnSaveProfileClick(object? sender, EventArgs e)
        {
            if (_portPanels.Count == 0)
            {
                MessageBox.Show("No connections to save.", "Save Profile", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            
            using var dialog = new SaveProfileDialog();
            if (dialog.ShowDialog(this) == DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.ProfileName))
            {
                try
                {
                    var connections = _portPanels.Values.Select(p => p.Connection).ToList();
                    _configManager.SaveProfile(dialog.ProfileName, connections);
                    _statusLabel.Text = $"Saved profile '{dialog.ProfileName}'";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving profile: {ex.Message}", 
                        "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        
        private void OnManageProfilesClick(object? sender, EventArgs e)
        {
            using var dialog = new ManageProfilesDialog(_configManager);
            dialog.ShowDialog(this);
        }
        
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Save configuration before closing
            SaveConfiguration();
            
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
