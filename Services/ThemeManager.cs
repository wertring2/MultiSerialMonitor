using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace MultiSerialMonitor.Services
{
    public enum Theme
    {
        Light,
        Dark
    }

    public static class ThemeManager
    {
        private static Theme _currentTheme = Theme.Light;
        
        public static Theme CurrentTheme
        {
            get => _currentTheme;
            set
            {
                if (_currentTheme != value)
                {
                    _currentTheme = value;
                    ThemeChanged?.Invoke(null, EventArgs.Empty);
                }
            }
        }

        public static event EventHandler ThemeChanged;

        public static class Colors
        {
            public static Color FormBackground => CurrentTheme == Theme.Dark ? Color.FromArgb(30, 30, 30) : Color.White;
            public static Color FormForeground => CurrentTheme == Theme.Dark ? Color.FromArgb(240, 240, 240) : Color.Black;
            
            public static Color PanelBackground => CurrentTheme == Theme.Dark ? Color.FromArgb(45, 45, 45) : Color.FromArgb(245, 245, 245);
            public static Color PanelHoverBackground => CurrentTheme == Theme.Dark ? Color.FromArgb(60, 60, 60) : Color.FromArgb(235, 235, 235);
            public static Color PanelBorder => CurrentTheme == Theme.Dark ? Color.FromArgb(70, 70, 70) : Color.FromArgb(200, 200, 200);
            
            public static Color ControlBackground => CurrentTheme == Theme.Dark ? Color.FromArgb(40, 40, 40) : Color.White;
            public static Color ControlForeground => CurrentTheme == Theme.Dark ? Color.FromArgb(240, 240, 240) : Color.Black;
            public static Color ControlBorder => CurrentTheme == Theme.Dark ? Color.FromArgb(70, 70, 70) : Color.FromArgb(180, 180, 180);
            
            public static Color ButtonBackground => CurrentTheme == Theme.Dark ? Color.FromArgb(60, 60, 60) : Color.FromArgb(240, 240, 240);
            public static Color ButtonForeground => CurrentTheme == Theme.Dark ? Color.FromArgb(240, 240, 240) : Color.Black;
            public static Color ButtonHoverBackground => CurrentTheme == Theme.Dark ? Color.FromArgb(80, 80, 80) : Color.FromArgb(230, 230, 230);
            
            public static Color ToolStripBackground => CurrentTheme == Theme.Dark ? Color.FromArgb(45, 45, 45) : SystemColors.Control;
            public static Color ToolStripForeground => CurrentTheme == Theme.Dark ? Color.FromArgb(240, 240, 240) : Color.Black;
            
            public static Color MenuBackground => CurrentTheme == Theme.Dark ? Color.FromArgb(45, 45, 45) : SystemColors.Menu;
            public static Color MenuForeground => CurrentTheme == Theme.Dark ? Color.FromArgb(240, 240, 240) : SystemColors.MenuText;
            public static Color MenuHighlight => CurrentTheme == Theme.Dark ? Color.FromArgb(70, 70, 70) : SystemColors.MenuHighlight;
            
            public static Color StatusStripBackground => CurrentTheme == Theme.Dark ? Color.FromArgb(45, 45, 45) : SystemColors.Control;
            
            // Console colors remain the same for both themes as console is already dark
            public static Color ConsoleBackground => Color.FromArgb(30, 30, 30);
            public static Color ConsoleForeground => Color.LightGray;
            public static Color ConsoleCommand => Color.Cyan;
            public static Color ConsoleError => Color.Red;
            public static Color ConsoleLineNumber => Color.DarkGray;
            
            // Status indicator colors remain the same for visibility
            public static Color StatusConnected => Color.LimeGreen;
            public static Color StatusConnecting => Color.Orange;
            public static Color StatusError => Color.Red;
            public static Color StatusDisconnected => Color.Gray;
            
            // Port Panel specific
            public static Color PortNameColor => CurrentTheme == Theme.Dark ? Color.FromArgb(100, 149, 237) : Color.FromArgb(51, 122, 183);
            public static Color TimestampColor => CurrentTheme == Theme.Dark ? Color.FromArgb(150, 150, 150) : Color.Gray;
            public static Color DataTextColor => CurrentTheme == Theme.Dark ? Color.FromArgb(220, 220, 220) : Color.Black;
        }

        public static void ApplyTheme(Control control)
        {
            if (control is Form form)
            {
                form.BackColor = Colors.FormBackground;
                form.ForeColor = Colors.FormForeground;
            }
            else if (control is Panel panel)
            {
                panel.BackColor = Colors.PanelBackground;
                panel.ForeColor = Colors.FormForeground;
            }
            else if (control is Button button)
            {
                button.BackColor = Colors.ButtonBackground;
                button.ForeColor = Colors.ButtonForeground;
                button.FlatStyle = FlatStyle.Flat;
                button.FlatAppearance.BorderColor = Colors.ControlBorder;
                button.FlatAppearance.MouseOverBackColor = Colors.ButtonHoverBackground;
            }
            else if (control is TextBox textBox)
            {
                textBox.BackColor = Colors.ControlBackground;
                textBox.ForeColor = Colors.ControlForeground;
                textBox.BorderStyle = BorderStyle.FixedSingle;
            }
            else if (control is ComboBox comboBox)
            {
                comboBox.BackColor = Colors.ControlBackground;
                comboBox.ForeColor = Colors.ControlForeground;
                comboBox.FlatStyle = FlatStyle.Flat;
            }
            else if (control is ListBox listBox)
            {
                listBox.BackColor = Colors.ControlBackground;
                listBox.ForeColor = Colors.ControlForeground;
                listBox.BorderStyle = BorderStyle.FixedSingle;
            }
            else if (control is RichTextBox richTextBox)
            {
                // Special handling for console-style RichTextBoxes
                if (richTextBox.Name?.Contains("console", StringComparison.OrdinalIgnoreCase) == true ||
                    richTextBox.BackColor == Color.Black)
                {
                    richTextBox.BackColor = Colors.ConsoleBackground;
                    richTextBox.ForeColor = Colors.ConsoleForeground;
                }
                else
                {
                    richTextBox.BackColor = Colors.ControlBackground;
                    richTextBox.ForeColor = Colors.ControlForeground;
                }
            }
            else if (control is Label label)
            {
                label.ForeColor = Colors.FormForeground;
            }
            else if (control is GroupBox groupBox)
            {
                groupBox.ForeColor = Colors.FormForeground;
            }
            else if (control is TabControl tabControl)
            {
                tabControl.BackColor = Colors.PanelBackground;
                tabControl.ForeColor = Colors.FormForeground;
            }
            else if (control is TabPage tabPage)
            {
                tabPage.BackColor = Colors.FormBackground;
                tabPage.ForeColor = Colors.FormForeground;
            }
            else if (control is ToolStrip toolStrip)
            {
                toolStrip.BackColor = Colors.ToolStripBackground;
                toolStrip.ForeColor = Colors.ToolStripForeground;
                toolStrip.RenderMode = ToolStripRenderMode.Professional;
                toolStrip.Renderer = new DarkToolStripRenderer();
                
                foreach (ToolStripItem item in toolStrip.Items)
                {
                    item.ForeColor = Colors.ToolStripForeground;
                    if (item is ToolStripButton)
                    {
                        item.BackColor = Colors.ToolStripBackground;
                    }
                }
            }
            else if (control is MenuStrip menuStrip)
            {
                menuStrip.BackColor = Colors.MenuBackground;
                menuStrip.ForeColor = Colors.MenuForeground;
                menuStrip.RenderMode = ToolStripRenderMode.Professional;
                menuStrip.Renderer = new DarkToolStripRenderer();
                
                foreach (ToolStripItem item in menuStrip.Items)
                {
                    ApplyThemeToMenuItem(item);
                }
            }
            else if (control is StatusStrip statusStrip)
            {
                statusStrip.BackColor = Colors.StatusStripBackground;
                statusStrip.ForeColor = Colors.ToolStripForeground;
                statusStrip.RenderMode = ToolStripRenderMode.Professional;
                statusStrip.Renderer = new DarkToolStripRenderer();
            }

            // Recursively apply theme to child controls
            foreach (Control childControl in control.Controls)
            {
                ApplyTheme(childControl);
            }
        }

        private static void ApplyThemeToMenuItem(ToolStripItem item)
        {
            item.ForeColor = Colors.MenuForeground;
            item.BackColor = Colors.MenuBackground;
            
            if (item is ToolStripMenuItem menuItem)
            {
                foreach (ToolStripItem dropDownItem in menuItem.DropDownItems)
                {
                    ApplyThemeToMenuItem(dropDownItem);
                }
            }
        }

        private class DarkToolStripRenderer : ToolStripProfessionalRenderer
        {
            public DarkToolStripRenderer() : base(new DarkColorTable()) { }

            protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
            {
                using (var brush = new SolidBrush(Colors.ToolStripBackground))
                {
                    e.Graphics.FillRectangle(brush, e.AffectedBounds);
                }
            }

            protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
            {
                if (e.Item.Selected)
                {
                    using (var brush = new SolidBrush(Colors.MenuHighlight))
                    {
                        e.Graphics.FillRectangle(brush, new Rectangle(Point.Empty, e.Item.Size));
                    }
                }
                else
                {
                    using (var brush = new SolidBrush(Colors.MenuBackground))
                    {
                        e.Graphics.FillRectangle(brush, new Rectangle(Point.Empty, e.Item.Size));
                    }
                }
            }
        }

        private class DarkColorTable : ProfessionalColorTable
        {
            public override Color ToolStripDropDownBackground => Colors.MenuBackground;
            public override Color MenuItemSelected => Colors.MenuHighlight;
            public override Color MenuItemSelectedGradientBegin => Colors.MenuHighlight;
            public override Color MenuItemSelectedGradientEnd => Colors.MenuHighlight;
            public override Color MenuItemPressedGradientBegin => Colors.MenuHighlight;
            public override Color MenuItemPressedGradientEnd => Colors.MenuHighlight;
            public override Color MenuBorder => Colors.ControlBorder;
            public override Color ToolStripBorder => Colors.ControlBorder;
        }
    }
}