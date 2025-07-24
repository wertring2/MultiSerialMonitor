namespace MultiSerialMonitor.Forms
{
    public class SelectProfileDialog : Form
    {
        private ListBox _profilesList = null!;
        private Button _okButton = null!;
        private Button _cancelButton = null!;

        public string SelectedProfile => _profilesList.SelectedItem?.ToString() ?? "";

        public SelectProfileDialog(string[] profiles, string title = "Select Profile")
        {
            InitializeComponents(title);
            LoadProfiles(profiles);
        }

        private void InitializeComponents(string title)
        {
            Text = title;
            Size = new Size(400, 300);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            var label = new Label
            {
                Text = "Available Profiles:",
                Location = new Point(12, 12),
                Size = new Size(100, 23)
            };

            _profilesList = new ListBox
            {
                Location = new Point(12, 40),
                Size = new Size(360, 180),
                SelectionMode = SelectionMode.One
            };
            _profilesList.DoubleClick += (s, e) =>
            {
                if (_profilesList.SelectedItem != null)
                {
                    DialogResult = DialogResult.OK;
                    Close();
                }
            };

            _okButton = new Button
            {
                Text = "OK",
                Location = new Point(216, 230),
                Size = new Size(75, 30),
                DialogResult = DialogResult.OK,
                Enabled = false
            };

            _cancelButton = new Button
            {
                Text = "Cancel",
                Location = new Point(297, 230),
                Size = new Size(75, 30),
                DialogResult = DialogResult.Cancel
            };

            _profilesList.SelectedIndexChanged += (s, e) =>
            {
                _okButton.Enabled = _profilesList.SelectedItem != null;
            };

            Controls.AddRange(new Control[] {
                label,
                _profilesList,
                _okButton,
                _cancelButton
            });

            AcceptButton = _okButton;
            CancelButton = _cancelButton;
        }

        private void InitializeComponent()
        {

        }

        private void LoadProfiles(string[] profiles)
        {
            _profilesList.Items.Clear();
            foreach (var profile in profiles)
            {
                _profilesList.Items.Add(profile);
            }

            if (_profilesList.Items.Count > 0)
            {
                _profilesList.SelectedIndex = 0;
            }
        }
    }
}