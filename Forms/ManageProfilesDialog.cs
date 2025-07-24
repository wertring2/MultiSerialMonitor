using MultiSerialMonitor.Services;

namespace MultiSerialMonitor.Forms
{
    public class ManageProfilesDialog : Form
    {
        private ListBox _profilesList;
        private Button _deleteButton;
        private Button _closeButton;
        private readonly ConfigurationManager _configManager;

        public ManageProfilesDialog(ConfigurationManager configManager)
        {
            _configManager = configManager;
            InitializeComponents();
            LoadProfiles();
        }

        private void InitializeComponents()
        {
            Text = "Manage Profiles";
            Size = new Size(400, 350);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            var label = new Label
            {
                Text = "Saved Profiles:",
                Location = new Point(12, 12),
                Size = new Size(100, 23)
            };

            _profilesList = new ListBox
            {
                Location = new Point(12, 40),
                Size = new Size(360, 220),
                SelectionMode = SelectionMode.One
            };
            _profilesList.SelectedIndexChanged += OnSelectionChanged;

            _deleteButton = new Button
            {
                Text = "Delete",
                Location = new Point(12, 270),
                Size = new Size(75, 30),
                Enabled = false
            };
            _deleteButton.Click += OnDeleteClick;

            _closeButton = new Button
            {
                Text = "Close",
                Location = new Point(297, 270),
                Size = new Size(75, 30),
                DialogResult = DialogResult.OK
            };

            Controls.AddRange(new Control[] {
                label,
                _profilesList,
                _deleteButton,
                _closeButton
            });

            CancelButton = _closeButton;
        }

        private void LoadProfiles()
        {
            _profilesList.Items.Clear();
            var profiles = _configManager.GetAvailableProfiles();

            if (profiles.Length == 0)
            {
                _profilesList.Items.Add("(No saved profiles)");
                _profilesList.Enabled = false;
            }
            else
            {
                _profilesList.Enabled = true;
                foreach (var profile in profiles)
                {
                    _profilesList.Items.Add(profile);
                }
            }
        }

        private void OnSelectionChanged(object? sender, EventArgs e)
        {
            _deleteButton.Enabled = _profilesList.Enabled &&
                                  _profilesList.SelectedItem != null &&
                                  _profilesList.SelectedItem.ToString() != "(No saved profiles)";
        }

        private void InitializeComponent()
        {

        }

        private void OnDeleteClick(object? sender, EventArgs e)
        {
            if (_profilesList.SelectedItem == null) return;

            var profileName = _profilesList.SelectedItem.ToString();
            if (string.IsNullOrEmpty(profileName)) return;

            var result = MessageBox.Show($"Are you sure you want to delete the profile '{profileName}'?",
                "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                try
                {
                    _configManager.DeleteProfile(profileName);
                    LoadProfiles();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting profile: {ex.Message}",
                        "Delete Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}