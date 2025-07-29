using System.Diagnostics;
using MultiSerialMonitor.Controls;
using MultiSerialMonitor.Forms;
using MultiSerialMonitor.Models;
using MultiSerialMonitor.Services;
using MultiSerialMonitor.Exceptions;
using MultiSerialMonitor.Localization;

namespace MultiSerialMonitor
{
    public enum ViewMode
    {
        List,
        Grid
    }
    
    public partial class Form1 : Form, ILocalizable
    {
        private FlowLayoutPanel _portsPanel;
        private ToolStrip _toolbar;
        private StatusStrip _statusStrip;
        private ToolStripStatusLabel _statusLabel;
        private ToolStripDropDownButton _languageDropDown;
        private ToolStripButton? _viewModeButton;
        private ViewMode _currentViewMode = ViewMode.List;
        
        private readonly Dictionary<string, IPortMonitor> _monitors = new();
        private readonly Dictionary<string, PortPanel> _portPanels = new();
        private readonly Dictionary<string, ConsoleForm> _consoleForms = new();
        private readonly ConfigurationManager _configManager = new();
        private System.Windows.Forms.Timer? _resizeTimer;
        private bool _isResizing = false;
        private ToolStripButton? _darkModeButton;
        private AppSettings _appSettings;
        
        public Form1()
        {
            InitializeComponent();
            LoadAppSettings();
            LoadLanguagePreference();
            InitializeCustomComponents();
            ApplyTheme();
            ApplyLocalization();
            LocalizationManager.LanguageChanged += (s, e) => ApplyLocalization();
            LoadViewModePreference();
            LoadSavedConfiguration();
        }
        
        private void InitializeCustomComponents()
        {
            Text = "Multi Serial Monitor - by Q WAVE COMPANY LIMITED";
            Size = new Size(1200, 800);
            MinimumSize = new Size(800, 600);
            StartPosition = FormStartPosition.CenterScreen;
            
            // Enable double buffering for smoother resizing
            SetStyle(ControlStyles.AllPaintingInWmPaint | 
                    ControlStyles.UserPaint | 
                    ControlStyles.DoubleBuffer, true);
            
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
            
            var exportProfileItem = new ToolStripMenuItem("Export Profile...");
            exportProfileItem.Click += OnExportProfileClick;
            
            var importProfileItem = new ToolStripMenuItem("Import Profile...");
            importProfileItem.Click += OnImportProfileClick;
            
            var manageProfilesItem = new ToolStripMenuItem("Manage Profiles...");
            manageProfilesItem.Click += OnManageProfilesClick;
            
            profileDropDown.DropDownItems.AddRange(new ToolStripItem[] {
                saveProfileItem,
                loadProfileItem,
                new ToolStripSeparator(),
                exportProfileItem,
                importProfileItem,
                new ToolStripSeparator(),
                manageProfilesItem
            });
            
            // Update load profile submenu dynamically
            profileDropDown.DropDownOpening += (s, e) => UpdateLoadProfileMenu(loadProfileItem);
            
            // Language dropdown
            _languageDropDown = new ToolStripDropDownButton
            {
                Text = "Language",
                DisplayStyle = ToolStripItemDisplayStyle.Text
            };
            
            foreach (var lang in LanguageInfo.AvailableLanguages)
            {
                var item = new ToolStripMenuItem(lang.DisplayName);
                item.Tag = lang.Language;
                item.Click += (s, e) =>
                {
                    if (s is ToolStripMenuItem menuItem && menuItem.Tag is Language language)
                    {
                        LocalizationManager.CurrentLanguage = language;
                        SaveLanguagePreference(language);
                    }
                };
                _languageDropDown.DropDownItems.Add(item);
            }
            
            // View mode button
            _viewModeButton = new ToolStripButton
            {
                Text = "Grid View",
                DisplayStyle = ToolStripItemDisplayStyle.Text,
                ToolTipText = "Switch between List and Grid view"
            };
            _viewModeButton.Click += OnViewModeClick;
            
            // Dark mode button
            _darkModeButton = new ToolStripButton
            {
                Text = "Dark Mode",
                DisplayStyle = ToolStripItemDisplayStyle.Text,
                ToolTipText = "Toggle Dark Mode"
            };
            _darkModeButton.Click += OnDarkModeClick;
            
            // About button
            var aboutButton = new ToolStripButton
            {
                Text = "About",
                DisplayStyle = ToolStripItemDisplayStyle.Text,
                ToolTipText = "About Multi Serial Monitor"
            };
            aboutButton.Click += OnAboutClick;
            
            _toolbar.Items.AddRange(new ToolStripItem[] { 
                addButton, 
                new ToolStripSeparator(), 
                refreshButton,
                new ToolStripSeparator(),
                _viewModeButton,
                new ToolStripSeparator(),
                profileDropDown,
                new ToolStripSeparator(),
                clearAllButton,
                removeAllButton,
                new ToolStripSeparator(),
                _darkModeButton,
                new ToolStripSeparator(),
                _languageDropDown,
                new ToolStripSeparator(),
                aboutButton
            });
            
            // Ports panel
            _portsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(10)
            };
            
            // Status strip
            _statusStrip = new StatusStrip();
            _statusLabel = new ToolStripStatusLabel
            {
                Text = "Ready"
            };
            
            // Company credit in status bar
            var companyLabel = new ToolStripStatusLabel
            {
                Text = "© Q WAVE COMPANY LIMITED",
                Spring = true,
                TextAlign = ContentAlignment.MiddleRight
            };
            
            _statusStrip.Items.Add(_statusLabel);
            _statusStrip.Items.Add(companyLabel);
            
            Controls.Add(_portsPanel);
            Controls.Add(_toolbar);
            Controls.Add(_statusStrip);
            
            // Handle resize events
            Resize += OnFormResize;
            _portsPanel.Resize += OnPortsPanelResize;
        }
        
        private void OnFormResize(object? sender, EventArgs e)
        {
            // Adjust port panels width when form is resized
            if (WindowState != FormWindowState.Minimized)
            {
                // Use debouncing to prevent excessive updates during resize
                if (_resizeTimer == null)
                {
                    _resizeTimer = new System.Windows.Forms.Timer();
                    _resizeTimer.Interval = 100; // 100ms debounce
                    _resizeTimer.Tick += (s, args) =>
                    {
                        _resizeTimer.Stop();
                        _isResizing = false;
                        UpdatePortPanelWidths();
                    };
                }
                
                _isResizing = true;
                _resizeTimer.Stop();
                _resizeTimer.Start();
            }
        }
        
        private void OnPortsPanelResize(object? sender, EventArgs e)
        {
            if (!_isResizing)
            {
                UpdatePortPanelWidths();
            }
        }
        
        private void UpdatePortPanelWidths()
        {
            // Use the same logic as ApplyViewMode but without changing layout direction
            _portsPanel.SuspendLayout();
            
            try
            {
                if (_currentViewMode == ViewMode.List)
                {
                    // Calculate optimal width for list view
                    int availableWidth = _portsPanel.ClientSize.Width;
                    int scrollBarWidth = SystemInformation.VerticalScrollBarWidth;
                    int padding = 40;
                    int optimalWidth = availableWidth - padding;
                    
                    // Account for scroll bar if needed
                    if (_portPanels.Count * 170 > _portsPanel.ClientSize.Height)
                    {
                        optimalWidth -= scrollBarWidth;
                    }
                    
                    foreach (var panel in _portPanels.Values)
                    {
                        panel.Width = Math.Max(300, optimalWidth);
                    }
                }
                else
                {
                    // Calculate optimal grid layout
                    int availableWidth = _portsPanel.ClientSize.Width;
                    int scrollBarWidth = SystemInformation.VerticalScrollBarWidth;
                    int minPanelWidth = 300;
                    int maxPanelWidth = 450;
                    int spacing = 10;
                    
                    // Try different column counts to find optimal layout
                    int bestColumns = 1;
                    int bestPanelWidth = minPanelWidth;
                    
                    for (int cols = 1; cols <= 10; cols++)
                    {
                        int totalSpacing = spacing * (cols + 1);
                        int usableWidth = availableWidth - totalSpacing - scrollBarWidth;
                        int panelWidth = usableWidth / cols;
                        
                        if (panelWidth >= minPanelWidth && panelWidth <= maxPanelWidth)
                        {
                            bestColumns = cols;
                            bestPanelWidth = panelWidth;
                        }
                        else if (panelWidth < minPanelWidth)
                        {
                            break;
                        }
                    }
                    
                    foreach (var panel in _portPanels.Values)
                    {
                        panel.Width = bestPanelWidth;
                    }
                }
            }
            finally
            {
                _portsPanel.ResumeLayout(true);
            }
        }
        
        private async void OnAddPortClick(object? sender, EventArgs e)
        {
            try
            {
                using var dialog = new AddPortForm();
                if (dialog.ShowDialog(this) == DialogResult.OK && dialog.Connection != null)
                {
                    await AddPortConnectionAsync(dialog.Connection);
                }
            }
            catch (Exception ex)
            {
                Utils.ErrorHandler.ShowError(this, ex, "Add Port Error");
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
            // Calculate initial panel width based on current view mode
            int panelWidth = 300; // Default minimum
            if (_currentViewMode == ViewMode.List)
            {
                int availableWidth = _portsPanel.ClientSize.Width;
                int scrollBarWidth = SystemInformation.VerticalScrollBarWidth;
                int padding = 40;
                panelWidth = Math.Max(300, availableWidth - padding - scrollBarWidth);
            }
            else
            {
                // Grid view - use same calculation as ApplyViewMode
                int availableWidth = _portsPanel.ClientSize.Width;
                int scrollBarWidth = SystemInformation.VerticalScrollBarWidth;
                int minPanelWidth = 300;
                int maxPanelWidth = 450;
                int spacing = 10;
                
                for (int cols = 1; cols <= 10; cols++)
                {
                    int totalSpacing = spacing * (cols + 1);
                    int usableWidth = availableWidth - totalSpacing - scrollBarWidth;
                    int testWidth = usableWidth / cols;
                    
                    if (testWidth >= minPanelWidth && testWidth <= maxPanelWidth)
                    {
                        panelWidth = testWidth;
                    }
                    else if (testWidth < minPanelWidth)
                    {
                        break;
                    }
                }
            }
            
            var panel = new PortPanel(connection)
            {
                Width = panelWidth,
                Margin = _currentViewMode == ViewMode.List 
                    ? new Padding(10, 5, 10, 5)
                    : new Padding(5)
            };
            panel.ExpandRequested += OnPortExpandRequested;
            panel.ConnectRequested += async (s, e) => await ConnectPortAsync(connection);
            panel.DisconnectRequested += async (s, e) => await DisconnectPortAsync(connection);
            panel.RemoveRequested += (s, e) => RemovePort(connection);
            panel.ConfigureDetectionRequested += (s, e) => OnPortConfigureDetectionRequested(connection);
            panel.ViewDetectionsRequested += (s, e) => OnPortViewDetectionsRequested(connection);
            panel.ClearDataRequested += (s, e) => OnPortClearDataRequested(connection);
            panel.ExportDataRequested += (s, e) => OnPortExportDataRequested(connection);
            
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
            var message = string.Format(LocalizationManager.GetString("RemoveQuestion"), connection.Name);
            var result = MessageBox.Show(message, LocalizationManager.GetString("ConfirmRemove"), 
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
        
        private void OnPortExportDataRequested(PortConnection connection)
        {
            if (connection.OutputHistory.Count == 0)
            {
                MessageBox.Show($"No data to export for {connection.Name}.", "Export Data", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            
            using var saveDialog = new SaveFileDialog
            {
                Filter = "Text files (*.txt)|*.txt|CSV files (*.csv)|*.csv|Log files (*.log)|*.log|All files (*.*)|*.*",
                FileName = $"{connection.Name}_{DateTime.Now:yyyyMMdd_HHmmss}",
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
                        foreach (var line in connection.OutputHistory)
                        {
                            // Extract timestamp if present in the line
                            var timestamp = "";
                            var data = line;
                            
                            var match = System.Text.RegularExpressions.Regex.Match(line, @"^\[(.*?)\](.*)");
                            if (match.Success)
                            {
                                timestamp = match.Groups[1].Value;
                                data = match.Groups[2].Value.Trim();
                            }
                            
                            // Escape CSV fields
                            timestamp = $"\"{timestamp}\"";
                            data = $"\"{data.Replace("\"", "\"\"")}\"";
                            
                            csvLines.Add($"{timestamp},{lineNumber},{data}");
                            lineNumber++;
                        }
                        File.WriteAllLines(saveDialog.FileName, csvLines);
                    }
                    else
                    {
                        // Export as plain text
                        File.WriteAllLines(saveDialog.FileName, connection.OutputHistory);
                    }
                    
                    MessageBox.Show($"Data exported successfully to:\n{saveDialog.FileName}\n\nTotal lines: {connection.OutputHistory.Count}", 
                        "Export Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    _statusLabel.Text = $"Exported data from {connection.Name}";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error exporting data: {ex.Message}", 
                        "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        
        private void OnClearAllClick(object? sender, EventArgs e)
        {
            if (_portPanels.Count == 0)
            {
                MessageBox.Show(LocalizationManager.GetString("NoPortsToClear"), 
                    LocalizationManager.GetString("Information"), 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            
            var result = MessageBox.Show(LocalizationManager.GetString("ClearAllQuestion"), 
                LocalizationManager.GetString("ConfirmClearAll"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            
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
                MessageBox.Show(LocalizationManager.GetString("NoPortsToRemove"), 
                    LocalizationManager.GetString("Information"), 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            
            var message = string.Format(LocalizationManager.GetString("RemoveAllQuestion"), _portPanels.Count);
            var result = MessageBox.Show(message, LocalizationManager.GetString("ConfirmRemoveAll"), 
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
        
        private void OnExportProfileClick(object? sender, EventArgs e)
        {
            var profiles = _configManager.GetAvailableProfiles();
            if (profiles.Length == 0)
            {
                MessageBox.Show("No profiles available to export.", "Export Profile", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            
            // Let user select which profile to export
            using var selectDialog = new SelectProfileDialog(profiles, "Select Profile to Export");
            if (selectDialog.ShowDialog(this) == DialogResult.OK && !string.IsNullOrEmpty(selectDialog.SelectedProfile))
            {
                using var saveDialog = new SaveFileDialog
                {
                    Filter = "Profile files (*.json)|*.json|All files (*.*)|*.*",
                    FileName = $"{selectDialog.SelectedProfile}.json",
                    DefaultExt = "json"
                };
                
                if (saveDialog.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        _configManager.ExportProfile(selectDialog.SelectedProfile, saveDialog.FileName);
                        MessageBox.Show($"Profile exported successfully to:\n{saveDialog.FileName}", 
                            "Export Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error exporting profile: {ex.Message}", 
                            "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
        
        private void OnImportProfileClick(object? sender, EventArgs e)
        {
            using var openDialog = new OpenFileDialog
            {
                Filter = "Profile files (*.json)|*.json|All files (*.*)|*.*",
                DefaultExt = "json"
            };
            
            if (openDialog.ShowDialog(this) == DialogResult.OK)
            {
                try
                {
                    string profileName = _configManager.ImportProfile(openDialog.FileName);
                    MessageBox.Show($"Profile '{profileName}' imported successfully!", 
                        "Import Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    
                    // Ask if they want to load it now
                    var result = MessageBox.Show("Would you like to load this profile now?", 
                        "Load Profile", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    
                    if (result == DialogResult.Yes)
                    {
                        LoadProfile(profileName);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error importing profile: {ex.Message}", 
                        "Import Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Save configuration before closing
            SaveConfiguration();
            
            // Dispose resize timer
            _resizeTimer?.Stop();
            _resizeTimer?.Dispose();
            
            // Close all console forms
            foreach (var form in _consoleForms.Values.ToList())
            {
                form.Close();
            }
            
            // Disconnect and dispose all monitors
            foreach (var monitor in _monitors.Values)
            {
                try
                {
                    if (monitor.IsConnected)
                    {
                        // Disconnect from all ports before closing
                        monitor.DisconnectAsync().Wait(1000); // Wait max 1 second per connection
                    }
                }
                catch (Exception ex)
                {
                    // Log error but continue with disposal
                    Debug.WriteLine($"Error disconnecting monitor during form close: {ex.Message}");
                }
                finally
                {
                    monitor.Dispose();
                }
            }
            
            base.OnFormClosing(e);
        }
        
        public void ApplyLocalization()
        {
            Text = LocalizationManager.GetString("AppTitle");
            
            // Update toolbar items
            if (_toolbar != null)
            {
                foreach (ToolStripItem item in _toolbar.Items)
                {
                    if (item.Text == "Add Port" || item.Text == "เพิ่มพอร์ต")
                        item.Text = LocalizationManager.GetString("AddPort");
                    else if (item.Text == "Refresh" || item.Text == "รีเฟรช")
                        item.Text = LocalizationManager.GetString("Refresh");
                    else if (item.Text == "Remove All" || item.Text == "ลบทั้งหมด")
                        item.Text = LocalizationManager.GetString("RemoveAll");
                    else if (item.Text == "Clear All Data" || item.Text == "ล้างข้อมูลทั้งหมด")
                        item.Text = LocalizationManager.GetString("ClearAllData");
                    else if (item.Text == "Profiles" || item.Text == "โปรไฟล์")
                        item.Text = LocalizationManager.GetString("Profiles");
                    else if (item.Text == "Language" || item.Text == "ภาษา")
                        item.Text = LocalizationManager.GetString("Language");
                }
                
                // Update view mode button
                if (_viewModeButton != null)
                {
                    _viewModeButton.Text = _currentViewMode == ViewMode.List 
                        ? LocalizationManager.GetString("GridView")
                        : LocalizationManager.GetString("ListView");
                    _viewModeButton.ToolTipText = LocalizationManager.CurrentLanguage == Language.Thai 
                        ? "สลับระหว่างมุมมองรายการและตาราง"
                        : "Switch between List and Grid view";
                }
                
                // Update profile dropdown items
                var profileDropDown = _toolbar.Items.OfType<ToolStripDropDownButton>().FirstOrDefault(b => b.Text == LocalizationManager.GetString("Profiles"));
                if (profileDropDown != null)
                {
                    foreach (ToolStripItem item in profileDropDown.DropDownItems)
                    {
                        if (item is ToolStripMenuItem menuItem)
                        {
                            if (menuItem.Text == "Save Profile..." || menuItem.Text == "บันทึกโปรไฟล์...")
                                menuItem.Text = LocalizationManager.GetString("SaveProfile");
                            else if (menuItem.Text == "Load Profile" || menuItem.Text == "โหลดโปรไฟล์")
                                menuItem.Text = LocalizationManager.GetString("LoadProfile");
                            else if (menuItem.Text == "Export Profile..." || menuItem.Text == "ส่งออกโปรไฟล์...")
                                menuItem.Text = LocalizationManager.GetString("ExportProfile");
                            else if (menuItem.Text == "Import Profile..." || menuItem.Text == "นำเข้าโปรไฟล์...")
                                menuItem.Text = LocalizationManager.GetString("ImportProfile");
                            else if (menuItem.Text == "Manage Profiles..." || menuItem.Text == "จัดการโปรไฟล์...")
                                menuItem.Text = LocalizationManager.GetString("ManageProfiles");
                        }
                    }
                }
            }
            
            // Update status label
            if (_statusLabel != null && (_statusLabel.Text == "Ready" || _statusLabel.Text == "พร้อม"))
            {
                _statusLabel.Text = LocalizationManager.GetString("Ready");
            }
            
            // Update all port panels
            foreach (var panel in _portPanels.Values)
            {
                if (panel is ILocalizable localizable)
                {
                    localizable.ApplyLocalization();
                }
            }
            
            // Update all console forms
            foreach (var form in _consoleForms.Values)
            {
                if (form is ILocalizable localizable)
                {
                    localizable.ApplyLocalization();
                }
            }
        }
        
        private void SaveLanguagePreference(Language language)
        {
            try
            {
                var appDataPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "MultiSerialMonitor"
                );
                Directory.CreateDirectory(appDataPath);
                
                var prefsPath = Path.Combine(appDataPath, "language.pref");
                File.WriteAllText(prefsPath, language.ToString());
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving language preference: {ex.Message}");
            }
        }
        
        private void LoadLanguagePreference()
        {
            try
            {
                var appDataPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "MultiSerialMonitor"
                );
                var prefsPath = Path.Combine(appDataPath, "language.pref");
                
                if (File.Exists(prefsPath))
                {
                    var langStr = File.ReadAllText(prefsPath).Trim();
                    if (Enum.TryParse<Language>(langStr, out var language))
                    {
                        LocalizationManager.CurrentLanguage = language;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading language preference: {ex.Message}");
            }
        }
        
        private void OnViewModeClick(object? sender, EventArgs e)
        {
            // Toggle view mode
            _currentViewMode = _currentViewMode == ViewMode.List ? ViewMode.Grid : ViewMode.List;
            
            // Update button text
            if (_viewModeButton != null)
            {
                _viewModeButton.Text = _currentViewMode == ViewMode.List 
                    ? LocalizationManager.GetString("GridView")
                    : LocalizationManager.GetString("ListView");
            }
            
            // Apply new layout
            ApplyViewMode();
            SaveViewModePreference();
        }
        
        private void ApplyViewMode()
        {
            _portsPanel.SuspendLayout();
            
            try
            {
                if (_currentViewMode == ViewMode.List)
                {
                    // List view settings
                    _portsPanel.FlowDirection = FlowDirection.TopDown;
                    _portsPanel.WrapContents = false;
                    _portsPanel.AutoScroll = true;
                    
                    // Calculate optimal width for list view
                    int availableWidth = _portsPanel.ClientSize.Width;
                    int scrollBarWidth = SystemInformation.VerticalScrollBarWidth;
                    int padding = 40;
                    int optimalWidth = availableWidth - padding;
                    
                    // Account for scroll bar if needed
                    if (_portPanels.Count * 170 > _portsPanel.ClientSize.Height)
                    {
                        optimalWidth -= scrollBarWidth;
                    }
                    
                    foreach (var panel in _portPanels.Values)
                    {
                        panel.Width = Math.Max(300, optimalWidth);
                        panel.Height = 160;
                        panel.Margin = new Padding(10, 5, 10, 5);
                    }
                }
                else
                {
                    // Grid view settings
                    _portsPanel.FlowDirection = FlowDirection.LeftToRight;
                    _portsPanel.WrapContents = true;
                    _portsPanel.AutoScroll = true;
                    
                    // Calculate optimal grid layout
                    int availableWidth = _portsPanel.ClientSize.Width;
                    int scrollBarWidth = SystemInformation.VerticalScrollBarWidth;
                    int minPanelWidth = 300;
                    int maxPanelWidth = 450;
                    int spacing = 10;
                    
                    // Try different column counts to find optimal layout
                    int bestColumns = 1;
                    int bestPanelWidth = minPanelWidth;
                    
                    for (int cols = 1; cols <= 10; cols++)
                    {
                        int totalSpacing = spacing * (cols + 1);
                        int usableWidth = availableWidth - totalSpacing - scrollBarWidth;
                        int panelWidth = usableWidth / cols;
                        
                        if (panelWidth >= minPanelWidth && panelWidth <= maxPanelWidth)
                        {
                            bestColumns = cols;
                            bestPanelWidth = panelWidth;
                        }
                        else if (panelWidth < minPanelWidth)
                        {
                            break;
                        }
                    }
                    
                    foreach (var panel in _portPanels.Values)
                    {
                        panel.Width = bestPanelWidth;
                        panel.Height = 160;
                        panel.Margin = new Padding(spacing / 2);
                    }
                }
                
                // Force immediate layout update
                _portsPanel.PerformLayout();
            }
            finally
            {
                _portsPanel.ResumeLayout(true);
            }
        }
        
        private void SaveViewModePreference()
        {
            try
            {
                var appDataPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "MultiSerialMonitor"
                );
                Directory.CreateDirectory(appDataPath);
                
                var prefsPath = Path.Combine(appDataPath, "viewmode.pref");
                File.WriteAllText(prefsPath, _currentViewMode.ToString());
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving view mode preference: {ex.Message}");
            }
        }
        
        private void LoadViewModePreference()
        {
            try
            {
                var appDataPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "MultiSerialMonitor"
                );
                var prefsPath = Path.Combine(appDataPath, "viewmode.pref");
                
                if (File.Exists(prefsPath))
                {
                    var viewModeStr = File.ReadAllText(prefsPath).Trim();
                    if (Enum.TryParse<ViewMode>(viewModeStr, out var viewMode))
                    {
                        _currentViewMode = viewMode;
                        if (_viewModeButton != null)
                        {
                            _viewModeButton.Text = _currentViewMode == ViewMode.List 
                                ? LocalizationManager.GetString("GridView")
                                : LocalizationManager.GetString("ListView");
                        }
                        ApplyViewMode();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading view mode preference: {ex.Message}");
            }
        }
        
        private void LoadAppSettings()
        {
            _appSettings = AppSettings.Load();
            ThemeManager.CurrentTheme = _appSettings.Theme;
            ThemeManager.ThemeChanged += OnThemeChanged;
        }
        
        private void SaveAppSettings()
        {
            _appSettings.Save();
        }
        
        private void ApplyTheme()
        {
            ThemeManager.ApplyTheme(this);
            
            // Update dark mode button text
            if (_darkModeButton != null)
            {
                _darkModeButton.Text = ThemeManager.CurrentTheme == Theme.Dark ? "Light Mode" : "Dark Mode";
            }
            
            // Apply theme to all existing port panels
            foreach (var panel in _portPanels.Values)
            {
                panel.ApplyTheme();
            }
            
            // Apply theme to all existing console forms
            foreach (var form in _consoleForms.Values)
            {
                form.ApplyTheme();
            }
        }
        
        private void OnThemeChanged(object? sender, EventArgs e)
        {
            ApplyTheme();
        }
        
        private void OnDarkModeClick(object? sender, EventArgs e)
        {
            // Toggle theme
            ThemeManager.CurrentTheme = ThemeManager.CurrentTheme == Theme.Dark ? Theme.Light : Theme.Dark;
            
            // Save preference
            _appSettings.Theme = ThemeManager.CurrentTheme;
            SaveAppSettings();
            
            _statusLabel.Text = $"Switched to {ThemeManager.CurrentTheme} mode";
        }
        
        private void OnAboutClick(object? sender, EventArgs e)
        {
            using var aboutForm = new AboutForm();
            aboutForm.ShowDialog(this);
        }
    }
}
