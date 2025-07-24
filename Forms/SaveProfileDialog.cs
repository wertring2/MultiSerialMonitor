namespace MultiSerialMonitor.Forms
{
    public class SaveProfileDialog : Form
    {
        private TextBox _nameTextBox;
        private Button _okButton;
        private Button _cancelButton;

        public string ProfileName => _nameTextBox.Text.Trim();

        public SaveProfileDialog()
        {
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            Text = "Save Profile";
            Size = new Size(400, 150);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            var label = new Label
            {
                Text = "Profile Name:",
                Location = new Point(12, 20),
                Size = new Size(80, 23)
            };

            _nameTextBox = new TextBox
            {
                Location = new Point(100, 17),
                Size = new Size(270, 23),
                TabIndex = 0
            };

            _okButton = new Button
            {
                Text = "Save",
                Location = new Point(214, 70),
                Size = new Size(75, 30),
                DialogResult = DialogResult.OK,
                TabIndex = 1
            };
            _okButton.Click += OnOkClick;

            _cancelButton = new Button
            {
                Text = "Cancel",
                Location = new Point(295, 70),
                Size = new Size(75, 30),
                DialogResult = DialogResult.Cancel,
                TabIndex = 2
            };

            Controls.AddRange(new Control[] {
                label,
                _nameTextBox,
                _okButton,
                _cancelButton
            });

            AcceptButton = _okButton;
            CancelButton = _cancelButton;
        }

        private void InitializeComponent()
        {

        }

        private void OnOkClick(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_nameTextBox.Text))
            {
                MessageBox.Show("Please enter a profile name.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.None;
                return;
            }

            // Validate filename
            var invalidChars = Path.GetInvalidFileNameChars();
            if (_nameTextBox.Text.Any(c => invalidChars.Contains(c)))
            {
                MessageBox.Show("Profile name contains invalid characters.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.None;
                return;
            }
        }
    }
}