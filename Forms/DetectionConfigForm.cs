using MultiSerialMonitor.Models;

namespace MultiSerialMonitor.Forms
{
    public partial class DetectionConfigForm : Form
    {
        private PortConnection _connection;
        private DataGridView _patternsGrid;
        private Button _addButton;
        private Button _removeButton;
        private Button _saveButton;
        private Button _cancelButton;
        private List<DetectionPattern> _patterns;
        
        public DetectionConfigForm(PortConnection connection)
        {
            _connection = connection;
            _patterns = new List<DetectionPattern>(connection.Config.DetectionPatterns);
            InitializeComponents();
        }
        
        private void InitializeComponents()
        {
            Text = $"Configure Detection Patterns - {_connection.Name}";
            Size = new Size(700, 500);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            
            // Patterns grid
            _patternsGrid = new DataGridView
            {
                Location = new Point(12, 12),
                Size = new Size(660, 350),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize
            };
            
            // Configure columns
            _patternsGrid.Columns.Add(new DataGridViewCheckBoxColumn
            {
                Name = "Enabled",
                HeaderText = "Enabled",
                Width = 60,
                DataPropertyName = "IsEnabled"
            });
            
            _patternsGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Name",
                HeaderText = "Pattern Name",
                Width = 150,
                DataPropertyName = "Name"
            });
            
            _patternsGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Pattern",
                HeaderText = "Pattern",
                Width = 250,
                DataPropertyName = "Pattern"
            });
            
            _patternsGrid.Columns.Add(new DataGridViewCheckBoxColumn
            {
                Name = "IsRegex",
                HeaderText = "Regex",
                Width = 50,
                DataPropertyName = "IsRegex"
            });
            
            _patternsGrid.Columns.Add(new DataGridViewCheckBoxColumn
            {
                Name = "CaseSensitive",
                HeaderText = "Case Sensitive",
                Width = 100,
                DataPropertyName = "CaseSensitive"
            });
            
            // Buttons
            _addButton = new Button
            {
                Text = "Add Pattern",
                Location = new Point(12, 375),
                Size = new Size(100, 30),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            _addButton.Click += OnAddPattern;
            
            _removeButton = new Button
            {
                Text = "Remove",
                Location = new Point(118, 375),
                Size = new Size(100, 30),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
                Enabled = false
            };
            _removeButton.Click += OnRemovePattern;
            
            _saveButton = new Button
            {
                Text = "Save",
                Location = new Point(466, 420),
                Size = new Size(100, 30),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                DialogResult = DialogResult.OK
            };
            _saveButton.Click += OnSave;
            
            _cancelButton = new Button
            {
                Text = "Cancel",
                Location = new Point(572, 420),
                Size = new Size(100, 30),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                DialogResult = DialogResult.Cancel
            };
            
            // Add controls
            Controls.AddRange(new Control[] {
                _patternsGrid,
                _addButton,
                _removeButton,
                _saveButton,
                _cancelButton
            });
            
            // Load existing patterns
            LoadPatterns();
            
            // Wire up events
            _patternsGrid.SelectionChanged += OnSelectionChanged;
            _patternsGrid.CellValueChanged += OnCellValueChanged;
        }
        
        private void LoadPatterns()
        {
            _patternsGrid.DataSource = null;
            _patternsGrid.DataSource = _patterns;
        }
        
        private void OnAddPattern(object? sender, EventArgs e)
        {
            var editForm = new DetectionPatternEditForm(null);
            if (editForm.ShowDialog(this) == DialogResult.OK && editForm.Pattern != null)
            {
                _patterns.Add(editForm.Pattern);
                LoadPatterns();
            }
        }
        
        private void OnRemovePattern(object? sender, EventArgs e)
        {
            if (_patternsGrid.SelectedRows.Count > 0)
            {
                var pattern = _patternsGrid.SelectedRows[0].DataBoundItem as DetectionPattern;
                if (pattern != null)
                {
                    _patterns.Remove(pattern);
                    LoadPatterns();
                }
            }
        }
        
        private void OnSelectionChanged(object? sender, EventArgs e)
        {
            _removeButton.Enabled = _patternsGrid.SelectedRows.Count > 0;
        }
        
        private void OnCellValueChanged(object? sender, DataGridViewCellEventArgs e)
        {
            // Patterns are updated automatically through data binding
        }
        
        private void OnSave(object? sender, EventArgs e)
        {
            _connection.Config.DetectionPatterns = new List<DetectionPattern>(_patterns);
        }
    }
    
    public class DetectionPatternEditForm : Form
    {
        private TextBox _nameTextBox;
        private TextBox _patternTextBox;
        private CheckBox _regexCheckBox;
        private CheckBox _caseSensitiveCheckBox;
        private Button _okButton;
        private Button _cancelButton;
        
        public DetectionPattern? Pattern { get; private set; }
        
        public DetectionPatternEditForm(DetectionPattern? pattern)
        {
            Pattern = pattern;
            InitializeComponents();
            
            if (pattern != null)
            {
                _nameTextBox.Text = pattern.Name;
                _patternTextBox.Text = pattern.Pattern;
                _regexCheckBox.Checked = pattern.IsRegex;
                _caseSensitiveCheckBox.Checked = pattern.CaseSensitive;
            }
        }
        
        private void InitializeComponents()
        {
            Text = Pattern == null ? "Add Detection Pattern" : "Edit Detection Pattern";
            Size = new Size(400, 250);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            
            var nameLabel = new Label
            {
                Text = "Pattern Name:",
                Location = new Point(12, 15),
                Size = new Size(100, 23)
            };
            
            _nameTextBox = new TextBox
            {
                Location = new Point(120, 12),
                Size = new Size(250, 23),
                TabIndex = 0
            };
            
            var patternLabel = new Label
            {
                Text = "Pattern:",
                Location = new Point(12, 45),
                Size = new Size(100, 23)
            };
            
            _patternTextBox = new TextBox
            {
                Location = new Point(120, 42),
                Size = new Size(250, 23),
                TabIndex = 1
            };
            
            _regexCheckBox = new CheckBox
            {
                Text = "Use Regular Expression",
                Location = new Point(120, 75),
                Size = new Size(200, 23),
                TabIndex = 2
            };
            
            _caseSensitiveCheckBox = new CheckBox
            {
                Text = "Case Sensitive",
                Location = new Point(120, 105),
                Size = new Size(200, 23),
                TabIndex = 3
            };
            
            _okButton = new Button
            {
                Text = "OK",
                Location = new Point(214, 160),
                Size = new Size(75, 30),
                DialogResult = DialogResult.OK,
                TabIndex = 4
            };
            _okButton.Click += OnOk;
            
            _cancelButton = new Button
            {
                Text = "Cancel",
                Location = new Point(295, 160),
                Size = new Size(75, 30),
                DialogResult = DialogResult.Cancel,
                TabIndex = 5
            };
            
            Controls.AddRange(new Control[] {
                nameLabel,
                _nameTextBox,
                patternLabel,
                _patternTextBox,
                _regexCheckBox,
                _caseSensitiveCheckBox,
                _okButton,
                _cancelButton
            });
            
            AcceptButton = _okButton;
            CancelButton = _cancelButton;
        }
        
        private void OnOk(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_nameTextBox.Text))
            {
                MessageBox.Show("Please enter a pattern name.", "Validation Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.None;
                return;
            }
            
            if (string.IsNullOrWhiteSpace(_patternTextBox.Text))
            {
                MessageBox.Show("Please enter a pattern.", "Validation Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.None;
                return;
            }
            
            if (Pattern == null)
            {
                Pattern = new DetectionPattern();
            }
            
            Pattern.Name = _nameTextBox.Text.Trim();
            Pattern.Pattern = _patternTextBox.Text.Trim();
            Pattern.IsRegex = _regexCheckBox.Checked;
            Pattern.CaseSensitive = _caseSensitiveCheckBox.Checked;
            Pattern.IsEnabled = true;
        }
    }
}