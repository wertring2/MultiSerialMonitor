using MultiSerialMonitor.Models;

namespace MultiSerialMonitor.Controls
{
    public class PortPanel : Panel
    {
        private Label _nameLabel;
        private Label _statusLabel;
        private Label _lastLineLabel;
        private Label _statsLabel;
        private Label _detectionLabel;
        private Button _expandButton;
        private Button _deleteButton;
        private Button _configDetectionButton;
        private Button _clearButton;
        private Panel _statusIndicator;
        private ContextMenuStrip _contextMenu;
        private int _lineCount = 0;
        private int _packageCount = 0;
        private DateTime? _lastTimestamp;
        
        public PortConnection Connection { get; }
        
        public event EventHandler? ExpandRequested;
        public event EventHandler? RemoveRequested;
        public event EventHandler? ConnectRequested;
        public event EventHandler? DisconnectRequested;
        public event EventHandler? ConfigureDetectionRequested;
        public event EventHandler? ViewDetectionsRequested;
        public event EventHandler? ClearDataRequested;
        
        public PortPanel(PortConnection connection)
        {
            Connection = connection;
            InitializeComponents();
            
            Connection.DataReceived += OnDataReceived;
            Connection.StatusChanged += OnStatusChanged;
            Connection.ErrorOccurred += OnErrorOccurred;
            Connection.PatternDetected += OnPatternDetected;
            Connection.DataCleared += OnDataCleared;
        }
        
        private void InitializeComponents()
        {
            Height = 160; // Increased height to accommodate detection info
            BorderStyle = BorderStyle.FixedSingle;
            Padding = new Padding(10);
            BackColor = Color.FromArgb(245, 245, 245);
            
            // Status indicator
            _statusIndicator = new Panel
            {
                Width = 12,
                Height = 12,
                Location = new Point(10, 10),
                BackColor = GetStatusColor(Connection.Status)
            };
            
            // Name label
            _nameLabel = new Label
            {
                Text = Connection.Name,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Location = new Point(30, 8),
                AutoSize = true
            };
            
            // Status label
            _statusLabel = new Label
            {
                Text = Connection.Status.ToString(),
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.Gray,
                Location = new Point(30, 28),
                AutoSize = true
            };
            
            // Last line label
            _lastLineLabel = new Label
            {
                Text = Connection.LastLine,
                Font = new Font("Consolas", 8),
                Location = new Point(10, 50),
                Size = new Size(Width - 100, 40),
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
                AutoEllipsis = false,
                UseMnemonic = false,
                MaximumSize = new Size(0, 40)
            };
            
            // Stats label
            _statsLabel = new Label
            {
                Text = "Lines: 0 | Packages: 0",
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.DarkGray,
                Location = new Point(10, 95),
                Size = new Size(Width - 100, 20),
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
            };
            
            // Detection label
            _detectionLabel = new Label
            {
                Text = "Detections: 0",
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                ForeColor = Color.DarkRed,
                Location = new Point(10, 115),
                Size = new Size(Width - 160, 20),
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
                Cursor = Cursors.Hand
            };
            _detectionLabel.Click += (s, e) => 
            {
                if (Connection.DetectionMatches.Count > 0)
                    ViewDetectionsRequested?.Invoke(this, EventArgs.Empty);
            };
            
            // Expand button
            _expandButton = new Button
            {
                Text = "Expand",
                Size = new Size(60, 23),
                Location = new Point(Width - 145, 10),
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                Cursor = Cursors.Hand
            };
            _expandButton.Click += (s, e) => ExpandRequested?.Invoke(this, EventArgs.Empty);
            
            // Clear button
            _clearButton = new Button
            {
                Text = "Clear",
                Size = new Size(50, 23),
                Location = new Point(Width - 205, 10),
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.LightBlue,
                Cursor = Cursors.Hand
            };
            _clearButton.Click += (s, e) => ClearDataRequested?.Invoke(this, EventArgs.Empty);
            
            // Configure detection button
            _configDetectionButton = new Button
            {
                Text = "⚙",
                Size = new Size(25, 23),
                Location = new Point(Width - 40, 115),
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.LightGray,
                Font = new Font("Arial", 10),
                Cursor = Cursors.Hand
            };
            _configDetectionButton.Click += (s, e) => ConfigureDetectionRequested?.Invoke(this, EventArgs.Empty);
            
            // Delete button
            _deleteButton = new Button
            {
                Text = "×",
                Size = new Size(25, 23),
                Location = new Point(Width - 40, 10),
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(255, 200, 200),
                ForeColor = Color.DarkRed,
                Font = new Font("Arial", 12, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _deleteButton.Click += (s, e) => RemoveRequested?.Invoke(this, EventArgs.Empty);
            
            Controls.AddRange(new Control[] { 
                _statusIndicator, 
                _nameLabel, 
                _statusLabel, 
                _lastLineLabel,
                _statsLabel,
                _detectionLabel,
                _configDetectionButton,
                _clearButton,
                _expandButton,
                _deleteButton 
            });
            
            // Context menu
            _contextMenu = new ContextMenuStrip();
            var connectItem = new ToolStripMenuItem("Connect");
            connectItem.Click += (s, e) => ConnectRequested?.Invoke(this, EventArgs.Empty);
            
            var disconnectItem = new ToolStripMenuItem("Disconnect");
            disconnectItem.Click += (s, e) => DisconnectRequested?.Invoke(this, EventArgs.Empty);
            
            var clearItem = new ToolStripMenuItem("Clear Data");
            clearItem.Click += (s, e) => ClearDataRequested?.Invoke(this, EventArgs.Empty);
            
            var removeItem = new ToolStripMenuItem("Remove");
            removeItem.Click += (s, e) => RemoveRequested?.Invoke(this, EventArgs.Empty);
            
            _contextMenu.Items.AddRange(new ToolStripItem[] {
                connectItem,
                disconnectItem,
                new ToolStripSeparator(),
                clearItem,
                new ToolStripSeparator(),
                removeItem
            });
            
            ContextMenuStrip = _contextMenu;
            UpdateContextMenu();
            
            // Enable keyboard support
            TabStop = true;
            KeyDown += OnKeyDown;
            
            // Add tooltips
            var toolTip = new ToolTip();
            toolTip.SetToolTip(_expandButton, "Open console view");
            toolTip.SetToolTip(_deleteButton, "Remove this port");
            toolTip.SetToolTip(_configDetectionButton, "Configure Detection Patterns");
            toolTip.SetToolTip(_clearButton, "Clear all data for this port");
            
            // Add hover effects
            MouseEnter += (s, e) => BackColor = Color.FromArgb(235, 235, 235);
            MouseLeave += (s, e) => BackColor = Color.FromArgb(245, 245, 245);
        }
        
        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                RemoveRequested?.Invoke(this, EventArgs.Empty);
            }
        }
        
        private void UpdateContextMenu()
        {
            if (_contextMenu != null)
            {
                _contextMenu.Items[0].Enabled = Connection.Status != ConnectionStatus.Connected; // Connect
                _contextMenu.Items[1].Enabled = Connection.Status == ConnectionStatus.Connected; // Disconnect
            }
        }
        
        private void UpdateStatsDisplay()
        {
            string timestampText = _lastTimestamp.HasValue 
                ? _lastTimestamp.Value.ToString("HH:mm:ss") 
                : "No data";
            
            _statsLabel.Text = $"Time: {timestampText} | Lines: {_lineCount} | Packages: {_packageCount}";
        }
        
        private void OnDataReceived(object? sender, string data)
        {
            if (InvokeRequired)
            {
                Invoke(() => OnDataReceived(sender, data));
                return;
            }
            
            // Update counters
            _lineCount++;
            
            // Check if this is a new package (you can customize this logic)
            // For now, we'll count every non-empty line as a package
            if (!string.IsNullOrWhiteSpace(data))
            {
                _packageCount++;
            }
            
            // Update timestamp
            _lastTimestamp = DateTime.Now;
            
            // Update stats display
            UpdateStatsDisplay();
            
            // Display the complete line
            _lastLineLabel.Text = data;
            
            // Dynamically adjust font size if text is too long
            using (Graphics g = CreateGraphics())
            {
                SizeF textSize = g.MeasureString(data, _lastLineLabel.Font);
                
                // If text is wider than label, try smaller fonts
                if (textSize.Width > _lastLineLabel.Width)
                {
                    Font currentFont = _lastLineLabel.Font;
                    float fontSize = currentFont.Size;
                    
                    // Try progressively smaller fonts
                    while (fontSize > 6 && textSize.Width > _lastLineLabel.Width)
                    {
                        fontSize -= 0.5f;
                        using (Font testFont = new Font(currentFont.FontFamily, fontSize))
                        {
                            textSize = g.MeasureString(data, testFont);
                            if (textSize.Width <= _lastLineLabel.Width)
                            {
                                _lastLineLabel.Font = new Font(currentFont.FontFamily, fontSize);
                                break;
                            }
                        }
                    }
                    
                    // If still too long, wrap or truncate smartly
                    if (textSize.Width > _lastLineLabel.Width)
                    {
                        // Allow text wrapping for very long lines
                        _lastLineLabel.AutoSize = false;
                        _lastLineLabel.Height = 40;
                    }
                }
                else
                {
                    // Reset to default font if text fits
                    if (_lastLineLabel.Font.Size != 8)
                    {
                        _lastLineLabel.Font = new Font("Consolas", 8);
                    }
                }
            }
        }
        
        private void OnStatusChanged(object? sender, ConnectionStatus status)
        {
            if (InvokeRequired)
            {
                Invoke(() => OnStatusChanged(sender, status));
                return;
            }
            
            // Reset counters when connecting
            if (status == ConnectionStatus.Connected)
            {
                _lineCount = 0;
                _packageCount = 0;
                _lastTimestamp = null;
                Connection.DetectionMatches.Clear();
                UpdateStatsDisplay();
                UpdateDetectionDisplay();
            }
            
            _statusLabel.Text = status.ToString();
            _statusIndicator.BackColor = GetStatusColor(status);
            UpdateContextMenu();
            
            // Show error details if in error state
            if (status == ConnectionStatus.Error && !string.IsNullOrEmpty(Connection.LastError))
            {
                // For multi-line errors, show first line in status and full error in last line label
                var errorLines = Connection.LastError.Split('\n');
                var firstLine = errorLines[0].Replace("Error: ", "").Trim();
                
                _statusLabel.Text = $"Error: {firstLine}";
                _statusLabel.ForeColor = Color.Red;
                
                // If multi-line error, show full error in the last line label
                if (errorLines.Length > 1)
                {
                    _lastLineLabel.Text = Connection.LastError;
                    _lastLineLabel.ForeColor = Color.Red;
                    _lastLineLabel.Font = new Font("Consolas", 8);
                    _lastLineLabel.AutoSize = false;
                    _lastLineLabel.Height = 60; // More height for multi-line errors
                }
                
                // Update tooltip with full error
                var toolTip = new ToolTip();
                toolTip.SetToolTip(_statusLabel, Connection.LastError);
                toolTip.SetToolTip(_lastLineLabel, Connection.LastError);
            }
            else
            {
                _statusLabel.ForeColor = Color.Gray;
                _lastLineLabel.ForeColor = Color.Black;
            }
        }
        
        private void OnErrorOccurred(object? sender, string error)
        {
            if (InvokeRequired)
            {
                Invoke(() => OnErrorOccurred(sender, error));
                return;
            }
            
            // Flash the panel briefly to indicate error
            var originalColor = BackColor;
            BackColor = Color.FromArgb(255, 230, 230);
            Task.Delay(200).ContinueWith(_ => 
            {
                if (!IsDisposed)
                {
                    Invoke(() => BackColor = originalColor);
                }
            });
        }
        
        private Color GetStatusColor(ConnectionStatus status)
        {
            return status switch
            {
                ConnectionStatus.Connected => Color.LimeGreen,
                ConnectionStatus.Connecting => Color.Orange,
                ConnectionStatus.Error => Color.Red,
                _ => Color.Gray
            };
        }
        
        private void OnPatternDetected(object? sender, DetectionMatch match)
        {
            if (InvokeRequired)
            {
                Invoke(() => OnPatternDetected(sender, match));
                return;
            }
            
            UpdateDetectionDisplay();
            
            // Flash the detection label to indicate new detection
            var originalColor = _detectionLabel.ForeColor;
            _detectionLabel.ForeColor = Color.Red;
            Task.Delay(500).ContinueWith(_ => 
            {
                if (!IsDisposed)
                {
                    Invoke(() => _detectionLabel.ForeColor = originalColor);
                }
            });
        }
        
        private void UpdateDetectionDisplay()
        {
            int detectionCount = Connection.DetectionMatches.Count;
            _detectionLabel.Text = $"Detections: {detectionCount}";
            
            if (detectionCount > 0)
            {
                _detectionLabel.ForeColor = Color.DarkRed;
                _detectionLabel.Font = new Font("Segoe UI", 8, FontStyle.Bold);
                
                // Show tooltip with recent detections
                var recentDetections = Connection.DetectionMatches
                    .OrderByDescending(d => d.Timestamp)
                    .Take(3)
                    .Select(d => $"{d.PatternName}: {d.MatchedText}")
                    .ToList();
                
                var toolTip = new ToolTip();
                toolTip.SetToolTip(_detectionLabel, string.Join("\n", recentDetections) + "\nClick to view all");
            }
            else
            {
                _detectionLabel.ForeColor = Color.Gray;
                _detectionLabel.Font = new Font("Segoe UI", 8);
            }
        }
        
        private void OnDataCleared(object? sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(() => OnDataCleared(sender, e));
                return;
            }
            
            // Reset counters and display
            _lineCount = 0;
            _packageCount = 0;
            _lastTimestamp = null;
            _lastLineLabel.Text = "";
            UpdateStatsDisplay();
            UpdateDetectionDisplay();
        }
        
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Connection.DataReceived -= OnDataReceived;
                Connection.StatusChanged -= OnStatusChanged;
                Connection.ErrorOccurred -= OnErrorOccurred;
                Connection.PatternDetected -= OnPatternDetected;
                Connection.DataCleared -= OnDataCleared;
            }
            base.Dispose(disposing);
        }
    }
}