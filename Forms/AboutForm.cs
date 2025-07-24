using MultiSerialMonitor.Services;

namespace MultiSerialMonitor.Forms
{
    public partial class AboutForm : Form
    {
        private Label _titleLabel;
        private Label _versionLabel;
        private Label _copyrightLabel;
        private Label _companyLabel;
        private Label _descriptionLabel;
        private Button _closeButton;
        private PictureBox _logoBox;

        public AboutForm()
        {
            InitializeComponents();
            ApplyTheme();
        }

        private void InitializeComponents()
        {
            Text = "About Multi Serial Monitor";
            Size = new Size(550, 450);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            // Main panel
            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 7,
                Padding = new Padding(20)
            };

            // Q WAVE Logo
            _logoBox = new PictureBox
            {
                Size = new Size(200, 64),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.None
            };

            // Load the Q WAVE logo
            LoadLogo();

            // Title
            _titleLabel = new Label
            {
                Text = "Multi Serial Monitor",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                AutoSize = false
            };

            // Version
            _versionLabel = new Label
            {
                Text = "Version 1.0.0",
                Font = new Font("Segoe UI", 10),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                AutoSize = false
            };

            // Description
            _descriptionLabel = new Label
            {
                Text = "A professional serial port and Telnet monitoring application\nwith advanced terminal features and pattern detection.",
                Font = new Font("Segoe UI", 9),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                AutoSize = false
            };

            // Company Info
            _companyLabel = new Label
            {
                Text = "Developed by Q WAVE COMPANY LIMITED",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                AutoSize = false,
                ForeColor = Color.Blue
            };

            // Copyright
            _copyrightLabel = new Label
            {
                Text = $"© {DateTime.Now.Year} Q WAVE COMPANY LIMITED. All rights reserved.\n\nFor professional industrial communication solutions\ncontact Q WAVE COMPANY LIMITED",
                Font = new Font("Segoe UI", 9),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                AutoSize = false
            };

            // Close button
            _closeButton = new Button
            {
                Text = "Close",
                DialogResult = DialogResult.OK,
                Size = new Size(100, 35),
                Anchor = AnchorStyles.None
            };

            // Add controls to layout
            mainPanel.Controls.Add(_logoBox, 0, 0);
            mainPanel.Controls.Add(_titleLabel, 0, 1);
            mainPanel.Controls.Add(_versionLabel, 0, 2);
            mainPanel.Controls.Add(_descriptionLabel, 0, 3);
            mainPanel.Controls.Add(_companyLabel, 0, 4);
            mainPanel.Controls.Add(_copyrightLabel, 0, 5);
            mainPanel.Controls.Add(_closeButton, 0, 6);

            // Configure row styles
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 100)); // Logo
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40)); // Title
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30)); // Version
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50)); // Description
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40)); // Company
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // Copyright
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50)); // Button

            Controls.Add(mainPanel);
            AcceptButton = _closeButton;
        }

        private void LoadLogo()
        {
            try
            {
                // Get the logo file path (relative to executable)
                var logoPath = Path.Combine(Application.StartupPath, "QW LOGO Qwave.png");
                
                if (File.Exists(logoPath))
                {
                    // Load the actual Q WAVE logo
                    _logoBox.Image = Image.FromFile(logoPath);
                }
                else
                {
                    // Fallback: Try relative to current directory
                    var alternatePath = "QW LOGO Qwave.png";
                    if (File.Exists(alternatePath))
                    {
                        _logoBox.Image = Image.FromFile(alternatePath);
                    }
                    else
                    {
                        // Create a placeholder if logo file is not found
                        CreatePlaceholderLogo();
                    }
                }
            }
            catch (Exception)
            {
                // If any error occurs, create a placeholder
                CreatePlaceholderLogo();
            }
        }

        private void CreatePlaceholderLogo()
        {
            // Create a simple placeholder if the actual logo cannot be loaded
            var logo = new Bitmap(200, 64);
            using (var g = Graphics.FromImage(logo))
            {
                g.Clear(Color.White);
                
                // Draw Q WAVE text placeholder
                using (var brush = new SolidBrush(Color.DarkBlue))
                {
                    var font = new Font("Arial", 16, FontStyle.Bold);
                    g.DrawString("Q WAVE", font, brush, 10, 20);
                }
            }
            
            _logoBox.Image = logo;
        }

        public void ApplyTheme()
        {
            ThemeManager.ApplyTheme(this);
            
            // Apply theme to all labels
            ThemeManager.ApplyTheme(_titleLabel);
            ThemeManager.ApplyTheme(_versionLabel);
            ThemeManager.ApplyTheme(_copyrightLabel);
            ThemeManager.ApplyTheme(_descriptionLabel);
            ThemeManager.ApplyTheme(_closeButton);
            
            // Keep company label blue in both themes
            _companyLabel.ForeColor = ThemeManager.CurrentTheme == Theme.Dark ? Color.LightBlue : Color.Blue;
            
            // Apply theme-appropriate background for logo
            if (ThemeManager.CurrentTheme == Theme.Dark)
            {
                _logoBox.BackColor = ThemeManager.Colors.ControlBackground;
            }
            else
            {
                _logoBox.BackColor = Color.White;
            }
        }

        private void InitializeComponent()
        {
            // Required for Windows Forms Designer support
        }
    }
}