using MultiSerialMonitor.Models;

namespace MultiSerialMonitor.Controls
{
    public class PortPanel : Panel
    {
        private Label _nameLabel;
        private Label _statusLabel;
        private Label _lastLineLabel;
        private Button _expandButton;
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
        }
        
        private void InitializeComponents()
        {
            Height = 80;
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
                Font = new Font("Consolas", 9),
                Location = new Point(10, 50),
                Size = new Size(Width - 100, 20),
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
            };
            
            // Expand button
            _expandButton = new Button
            {
                Text = "Expand",
                Size = new Size(70, 25),
                Location = new Point(Width - 85, 25),
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                Cursor = Cursors.Hand
            };
            _expandButton.Click += (s, e) => ExpandRequested?.Invoke(this, EventArgs.Empty);
            
            Controls.AddRange(new Control[] { 
                _statusIndicator, 
                _nameLabel, 
                _statusLabel, 
                _lastLineLabel, 
                _expandButton 
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
            
            _lastLineLabel.Text = data.Length > 100 ? data.Substring(0, 97) + "..." : data;
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
            }
            base.Dispose(disposing);
        }
    }
}