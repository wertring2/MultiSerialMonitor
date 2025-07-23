using MultiSerialMonitor.Models;

namespace MultiSerialMonitor.Controls
{
    public class PortPanel : Panel
    {
        private Label _nameLabel;
        private Label _statusLabel;
        private Label _lastLineLabel;
        private Button _expandButton;
        private Button _deleteButton;
        private Panel _statusIndicator;
        private ContextMenuStrip _contextMenu;
        
        public PortConnection Connection { get; }
        
        public event EventHandler? ExpandRequested;
        public event EventHandler? RemoveRequested;
        public event EventHandler? ConnectRequested;
        public event EventHandler? DisconnectRequested;
        
        public PortPanel(PortConnection connection)
        {
            Connection = connection;
            InitializeComponents();
            
            Connection.DataReceived += OnDataReceived;
            Connection.StatusChanged += OnStatusChanged;
            Connection.ErrorOccurred += OnErrorOccurred;
        }
        
        private void InitializeComponents()
        {
            Height = 120; // Increased height to accommodate error messages
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
                _expandButton,
                _deleteButton 
            });
            
            // Context menu
            _contextMenu = new ContextMenuStrip();
            var connectItem = new ToolStripMenuItem("Connect");
            connectItem.Click += (s, e) => ConnectRequested?.Invoke(this, EventArgs.Empty);
            
            var disconnectItem = new ToolStripMenuItem("Disconnect");
            disconnectItem.Click += (s, e) => DisconnectRequested?.Invoke(this, EventArgs.Empty);
            
            var removeItem = new ToolStripMenuItem("Remove");
            removeItem.Click += (s, e) => RemoveRequested?.Invoke(this, EventArgs.Empty);
            
            _contextMenu.Items.AddRange(new ToolStripItem[] {
                connectItem,
                disconnectItem,
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
        
        private void OnDataReceived(object? sender, string data)
        {
            if (InvokeRequired)
            {
                Invoke(() => OnDataReceived(sender, data));
                return;
            }
            
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
        
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Connection.DataReceived -= OnDataReceived;
                Connection.StatusChanged -= OnStatusChanged;
                Connection.ErrorOccurred -= OnErrorOccurred;
            }
            base.Dispose(disposing);
        }
    }
}