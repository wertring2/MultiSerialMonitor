using MultiSerialMonitor.Models;

namespace MultiSerialMonitor.Forms
{
    public partial class ConnectionSettingsForm : Form
    {
        private NumericUpDown _maxRetriesNumeric;
        private NumericUpDown _retryDelayNumeric;
        private NumericUpDown _timeoutNumeric;
        private CheckBox _autoReconnectCheckBox;
        private NumericUpDown _reconnectIntervalNumeric;

        public ConnectionConfig Config { get; private set; }

        public ConnectionSettingsForm(ConnectionConfig config)
        {
            Config = config.Clone();
            InitializeComponents();
            LoadSettings();
        }

        private void InitializeComponents()
        {
            Text = "Connection Settings";
            Size = new Size(400, 300);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                Padding = new Padding(10),
                AutoSize = true
            };

            // Max retry attempts
            mainPanel.Controls.Add(new Label { Text = "Max Retry Attempts:", AutoSize = true }, 0, 0);
            _maxRetriesNumeric = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 10,
                Value = Config.MaxRetryAttempts,
                Dock = DockStyle.Fill
            };
            mainPanel.Controls.Add(_maxRetriesNumeric, 1, 0);

            // Retry delay
            mainPanel.Controls.Add(new Label { Text = "Retry Delay (ms):", AutoSize = true }, 0, 1);
            _retryDelayNumeric = new NumericUpDown
            {
                Minimum = 100,
                Maximum = 10000,
                Increment = 100,
                Value = Config.RetryDelayMs,
                Dock = DockStyle.Fill
            };
            mainPanel.Controls.Add(_retryDelayNumeric, 1, 1);

            // Connection timeout
            mainPanel.Controls.Add(new Label { Text = "Connection Timeout (ms):", AutoSize = true }, 0, 2);
            _timeoutNumeric = new NumericUpDown
            {
                Minimum = 1000,
                Maximum = 30000,
                Increment = 1000,
                Value = Config.ConnectionTimeoutMs,
                Dock = DockStyle.Fill
            };
            mainPanel.Controls.Add(_timeoutNumeric, 1, 2);

            // Auto-reconnect
            mainPanel.Controls.Add(new Label { Text = "Auto-Reconnect:", AutoSize = true }, 0, 3);
            _autoReconnectCheckBox = new CheckBox
            {
                Checked = Config.AutoReconnect,
                Text = "Enable",
                AutoSize = true
            };
            _autoReconnectCheckBox.CheckedChanged += OnAutoReconnectChanged;
            mainPanel.Controls.Add(_autoReconnectCheckBox, 1, 3);

            // Reconnect interval
            mainPanel.Controls.Add(new Label { Text = "Reconnect Interval (ms):", AutoSize = true }, 0, 4);
            _reconnectIntervalNumeric = new NumericUpDown
            {
                Minimum = 1000,
                Maximum = 60000,
                Increment = 1000,
                Value = Config.ReconnectIntervalMs,
                Enabled = Config.AutoReconnect,
                Dock = DockStyle.Fill
            };
            mainPanel.Controls.Add(_reconnectIntervalNumeric, 1, 4);

            // Buttons
            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                Height = 40,
                Padding = new Padding(10, 0, 10, 10)
            };

            var cancelButton = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Size = new Size(80, 25)
            };

            var okButton = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Size = new Size(80, 25)
            };
            okButton.Click += OnOkClick;

            buttonPanel.Controls.AddRange(new Control[] { cancelButton, okButton });

            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            Controls.Add(mainPanel);
            Controls.Add(buttonPanel);

            AcceptButton = okButton;
            CancelButton = cancelButton;
        }

        private void LoadSettings()
        {
            _maxRetriesNumeric.Value = Config.MaxRetryAttempts;
            _retryDelayNumeric.Value = Config.RetryDelayMs;
            _timeoutNumeric.Value = Config.ConnectionTimeoutMs;
            _autoReconnectCheckBox.Checked = Config.AutoReconnect;
            _reconnectIntervalNumeric.Value = Config.ReconnectIntervalMs;
        }

        private void OnAutoReconnectChanged(object? sender, EventArgs e)
        {
            _reconnectIntervalNumeric.Enabled = _autoReconnectCheckBox.Checked;
        }

        private void InitializeComponent()
        {

        }

        private void OnOkClick(object? sender, EventArgs e)
        {
            Config.MaxRetryAttempts = (int)_maxRetriesNumeric.Value;
            Config.RetryDelayMs = (int)_retryDelayNumeric.Value;
            Config.ConnectionTimeoutMs = (int)_timeoutNumeric.Value;
            Config.AutoReconnect = _autoReconnectCheckBox.Checked;
            Config.ReconnectIntervalMs = (int)_reconnectIntervalNumeric.Value;
        }
    }

    public static class ConnectionConfigExtensions
    {
        public static ConnectionConfig Clone(this ConnectionConfig config)
        {
            return new ConnectionConfig
            {
                MaxRetryAttempts = config.MaxRetryAttempts,
                RetryDelayMs = config.RetryDelayMs,
                ConnectionTimeoutMs = config.ConnectionTimeoutMs,
                AutoReconnect = config.AutoReconnect,
                ReconnectIntervalMs = config.ReconnectIntervalMs
            };
        }
    }
}