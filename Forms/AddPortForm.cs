using System.IO.Ports;
using MultiSerialMonitor.Models;

namespace MultiSerialMonitor.Forms
{
    public partial class AddPortForm : Form
    {
        private TabControl _tabControl;
        private TextBox _nameTextBox;
        
        // Serial Port controls
        private ComboBox _portNameCombo;
        private ComboBox _baudRateCombo;
        private ComboBox _parityCombo;
        private ComboBox _dataBitsCombo;
        private ComboBox _stopBitsCombo;
        
        // Telnet controls
        private TextBox _hostTextBox;
        private NumericUpDown _portNumeric;
        
        public PortConnection? Connection { get; private set; }
        
        public AddPortForm()
        {
            InitializeComponents();
        }
        
        private void InitializeComponents()
        {
            Text = "Add Port Connection";
            Size = new Size(400, 350);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            
            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 3,
                ColumnCount = 2,
                Padding = new Padding(10)
            };
            
            // Name input
            mainPanel.Controls.Add(new Label { Text = "Name:", AutoSize = true }, 0, 0);
            _nameTextBox = new TextBox { Dock = DockStyle.Fill };
            mainPanel.Controls.Add(_nameTextBox, 1, 0);
            
            // Tab control for connection types
            _tabControl = new TabControl
            {
                Dock = DockStyle.Fill
            };
            mainPanel.SetColumnSpan(_tabControl, 2);
            mainPanel.Controls.Add(_tabControl, 0, 1);
            
            // Serial Port tab
            var serialTab = new TabPage("Serial Port");
            _tabControl.TabPages.Add(serialTab);
            InitializeSerialPortTab(serialTab);
            
            // Telnet tab
            var telnetTab = new TabPage("Telnet");
            _tabControl.TabPages.Add(telnetTab);
            InitializeTelnetTab(telnetTab);
            
            // Buttons
            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft
            };
            mainPanel.SetColumnSpan(buttonPanel, 2);
            mainPanel.Controls.Add(buttonPanel, 0, 2);
            
            var cancelButton = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel
            };
            
            var okButton = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK
            };
            okButton.Click += OnOkClick;
            
            buttonPanel.Controls.AddRange(new Control[] { cancelButton, okButton });
            
            mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            
            Controls.Add(mainPanel);
            AcceptButton = okButton;
            CancelButton = cancelButton;
        }
        
        private void InitializeSerialPortTab(TabPage tab)
        {
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                Padding = new Padding(10),
                AutoSize = true
            };
            
            // Port name
            panel.Controls.Add(new Label { Text = "Port:", AutoSize = true }, 0, 0);
            _portNameCombo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Dock = DockStyle.Fill
            };
            _portNameCombo.Items.AddRange(SerialPort.GetPortNames());
            if (_portNameCombo.Items.Count > 0) _portNameCombo.SelectedIndex = 0;
            panel.Controls.Add(_portNameCombo, 1, 0);
            
            // Baud rate
            panel.Controls.Add(new Label { Text = "Baud Rate:", AutoSize = true }, 0, 1);
            _baudRateCombo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Dock = DockStyle.Fill
            };
            _baudRateCombo.Items.AddRange(new object[] { 
                "300", "600", "1200", "2400", "4800", "9600", 
                "14400", "19200", "38400", "57600", "115200" 
            });
            _baudRateCombo.SelectedItem = "9600";
            panel.Controls.Add(_baudRateCombo, 1, 1);
            
            // Parity
            panel.Controls.Add(new Label { Text = "Parity:", AutoSize = true }, 0, 2);
            _parityCombo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Dock = DockStyle.Fill
            };
            _parityCombo.Items.AddRange(Enum.GetNames(typeof(Parity)));
            _parityCombo.SelectedItem = "None";
            panel.Controls.Add(_parityCombo, 1, 2);
            
            // Data bits
            panel.Controls.Add(new Label { Text = "Data Bits:", AutoSize = true }, 0, 3);
            _dataBitsCombo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Dock = DockStyle.Fill
            };
            _dataBitsCombo.Items.AddRange(new object[] { "5", "6", "7", "8" });
            _dataBitsCombo.SelectedItem = "8";
            panel.Controls.Add(_dataBitsCombo, 1, 3);
            
            // Stop bits
            panel.Controls.Add(new Label { Text = "Stop Bits:", AutoSize = true }, 0, 4);
            _stopBitsCombo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Dock = DockStyle.Fill
            };
            _stopBitsCombo.Items.AddRange(Enum.GetNames(typeof(StopBits)));
            _stopBitsCombo.SelectedItem = "One";
            panel.Controls.Add(_stopBitsCombo, 1, 4);
            
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            
            tab.Controls.Add(panel);
        }
        
        private void InitializeTelnetTab(TabPage tab)
        {
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                Padding = new Padding(10),
                AutoSize = true
            };
            
            // Host
            panel.Controls.Add(new Label { Text = "Host:", AutoSize = true }, 0, 0);
            _hostTextBox = new TextBox { Dock = DockStyle.Fill };
            panel.Controls.Add(_hostTextBox, 1, 0);
            
            // Port
            panel.Controls.Add(new Label { Text = "Port:", AutoSize = true }, 0, 1);
            _portNumeric = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 65535,
                Value = 23,
                Dock = DockStyle.Fill
            };
            panel.Controls.Add(_portNumeric, 1, 1);
            
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            
            tab.Controls.Add(panel);
        }
        
        private void OnOkClick(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_nameTextBox.Text))
            {
                MessageBox.Show("Please enter a name for this connection.", "Validation Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.None;
                return;
            }
            
            if (_tabControl.SelectedIndex == 0) // Serial Port
            {
                if (_portNameCombo.SelectedItem == null)
                {
                    MessageBox.Show("Please select a serial port.", "Validation Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    DialogResult = DialogResult.None;
                    return;
                }
                
                Connection = new PortConnection
                {
                    Name = _nameTextBox.Text,
                    Type = ConnectionType.SerialPort,
                    PortName = _portNameCombo.SelectedItem.ToString()!,
                    BaudRate = int.Parse(_baudRateCombo.SelectedItem.ToString()!),
                    Parity = (Parity)Enum.Parse(typeof(Parity), _parityCombo.SelectedItem.ToString()!),
                    DataBits = int.Parse(_dataBitsCombo.SelectedItem.ToString()!),
                    StopBits = (StopBits)Enum.Parse(typeof(StopBits), _stopBitsCombo.SelectedItem.ToString()!)
                };
            }
            else // Telnet
            {
                if (string.IsNullOrWhiteSpace(_hostTextBox.Text))
                {
                    MessageBox.Show("Please enter a host name or IP address.", "Validation Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    DialogResult = DialogResult.None;
                    return;
                }
                
                Connection = new PortConnection
                {
                    Name = _nameTextBox.Text,
                    Type = ConnectionType.Telnet,
                    HostName = _hostTextBox.Text,
                    Port = (int)_portNumeric.Value
                };
            }
        }
    }
}