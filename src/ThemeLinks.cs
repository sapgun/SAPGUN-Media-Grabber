using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace SapgunMediaGrabber
{
    public class ThemedMainForm : MainForm
    {
        const string XProfileUrl = "https://x.com/caro7370";
        // Set this to the user's exact Ko-fi page before v0.2.2 is released.
        const string KoFiUrl = "";

        bool lightMode;
        Button themeButton;
        Button feedbackButton;
        Button supportButton;
        Panel footer;
        readonly string settingsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SAPGUN Media Grabber");
        string SettingsFile { get { return Path.Combine(settingsDir, "ui-settings.txt"); } }

        public ThemedMainForm()
            : base()
        {
            Size = new Size(740, 900);
            MinimumSize = new Size(690, 840);
            BuildFooter();
            lightMode = LoadLightMode();
            ApplyTheme();
        }

        void BuildFooter()
        {
            footer = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 58,
                Padding = new Padding(12, 9, 12, 9)
            };

            var row = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1
            };
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));

            themeButton = FooterButton("Light mode");
            feedbackButton = FooterButton("Feedback / DM on X");
            supportButton = FooterButton("Support me · Ko-fi");

            themeButton.Click += delegate
            {
                lightMode = !lightMode;
                SaveLightMode(lightMode);
                ApplyTheme();
            };

            feedbackButton.Click += delegate { OpenUrl(XProfileUrl); };

            supportButton.Enabled = !String.IsNullOrWhiteSpace(KoFiUrl);
            supportButton.Click += delegate
            {
                if (!String.IsNullOrWhiteSpace(KoFiUrl)) OpenUrl(KoFiUrl);
            };

            var tip = new ToolTip();
            tip.SetToolTip(feedbackButton, "Open @caro7370 on X to send feedback or a DM.");
            if (!supportButton.Enabled) tip.SetToolTip(supportButton, "Ko-fi URL will be enabled when the exact page URL is configured.");

            row.Controls.Add(themeButton, 0, 0);
            row.Controls.Add(feedbackButton, 1, 0);
            row.Controls.Add(supportButton, 2, 0);
            footer.Controls.Add(row);
            Controls.Add(footer);
            footer.BringToFront();
        }

        Button FooterButton(string text)
        {
            var button = new Button
            {
                Text = text,
                Dock = DockStyle.Fill,
                Margin = new Padding(4, 0, 4, 0),
                Height = 38,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            return button;
        }

        void ApplyTheme()
        {
            Color bg = lightMode ? Color.FromArgb(246, 247, 249) : Color.FromArgb(14, 16, 21);
            Color surface = lightMode ? Color.White : Color.FromArgb(24, 27, 34);
            Color buttonBg = lightMode ? Color.FromArgb(235, 238, 243) : Color.FromArgb(30, 34, 43);
            Color primary = lightMode ? Color.FromArgb(28, 31, 38) : Color.White;
            Color secondary = lightMode ? Color.FromArgb(91, 98, 112) : Color.FromArgb(169, 176, 190);
            Color border = lightMode ? Color.FromArgb(190, 196, 207) : Color.FromArgb(72, 79, 92);

            BackColor = bg;
            ForeColor = primary;
            ApplyThemeRecursive(this, bg, surface, buttonBg, primary, secondary, border);

            footer.BackColor = bg;
            themeButton.Text = lightMode ? "Dark mode" : "Light mode";

            // Keep primary action visually distinct.
            SetDownloadAccent(this);
        }

        void ApplyThemeRecursive(Control root, Color bg, Color surface, Color buttonBg, Color primary, Color secondary, Color border)
        {
            foreach (Control control in root.Controls)
            {
                if (control is Panel || control is TableLayoutPanel || control is FlowLayoutPanel)
                {
                    control.BackColor = bg;
                    control.ForeColor = primary;
                }
                else if (control is TextBox || control is ComboBox)
                {
                    control.BackColor = surface;
                    control.ForeColor = primary;
                }
                else if (control is Button)
                {
                    var button = (Button)control;
                    button.BackColor = buttonBg;
                    button.ForeColor = primary;
                    button.FlatStyle = FlatStyle.Flat;
                    button.FlatAppearance.BorderColor = border;
                }
                else if (control is RadioButton || control is CheckBox)
                {
                    control.BackColor = bg;
                    control.ForeColor = primary;
                }
                else if (control is Label)
                {
                    var label = (Label)control;
                    label.BackColor = bg;
                    label.ForeColor = LabelColor(label.Text, primary, secondary);
                }

                if (control.HasChildren)
                    ApplyThemeRecursive(control, bg, surface, buttonBg, primary, secondary, border);
            }
        }

        Color LabelColor(string text, Color primary, Color secondary)
        {
            string value = text ?? "";
            if (value.StartsWith("LOCAL", StringComparison.OrdinalIgnoreCase))
                return lightMode ? Color.FromArgb(55, 86, 210) : Color.FromArgb(126, 164, 255);
            if (value.StartsWith("Failed", StringComparison.OrdinalIgnoreCase))
                return lightMode ? Color.FromArgb(184, 47, 47) : Color.Salmon;
            if (value.StartsWith("Done", StringComparison.OrdinalIgnoreCase) || value.StartsWith("Ready", StringComparison.OrdinalIgnoreCase))
                return lightMode ? Color.FromArgb(34, 126, 76) : Color.LightGreen;
            if (value.StartsWith("Downloading", StringComparison.OrdinalIgnoreCase) || value.StartsWith("Optimizing", StringComparison.OrdinalIgnoreCase) || value.StartsWith("Updating", StringComparison.OrdinalIgnoreCase))
                return lightMode ? Color.FromArgb(55, 86, 210) : Color.FromArgb(126, 164, 255);
            if (value.StartsWith("Latest:", StringComparison.OrdinalIgnoreCase) || value.StartsWith("Waiting", StringComparison.OrdinalIgnoreCase) || value.StartsWith("Starting", StringComparison.OrdinalIgnoreCase) || value.StartsWith("Current version", StringComparison.OrdinalIgnoreCase))
                return secondary;
            if (value == value.ToUpperInvariant() && value.Length > 2)
                return secondary;
            return primary;
        }

        void SetDownloadAccent(Control root)
        {
            foreach (Control control in root.Controls)
            {
                var button = control as Button;
                if (button != null && button.Text == "DOWNLOAD")
                {
                    button.BackColor = Color.FromArgb(76, 111, 255);
                    button.ForeColor = Color.White;
                    button.FlatAppearance.BorderColor = Color.FromArgb(76, 111, 255);
                }
                if (control.HasChildren) SetDownloadAccent(control);
            }
        }

        bool LoadLightMode()
        {
            try
            {
                if (!File.Exists(SettingsFile)) return false;
                return String.Equals(File.ReadAllText(SettingsFile).Trim(), "light", StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        }

        void SaveLightMode(bool light)
        {
            try
            {
                Directory.CreateDirectory(settingsDir);
                File.WriteAllText(SettingsFile, light ? "light" : "dark");
            }
            catch { }
        }

        void OpenUrl(string target)
        {
            try
            {
                Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
            }
            catch
            {
                try { Process.Start("explorer.exe", target); } catch { }
            }
        }
    }

    public static class AppEntry
    {
        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new ThemedMainForm());
        }
    }
}
