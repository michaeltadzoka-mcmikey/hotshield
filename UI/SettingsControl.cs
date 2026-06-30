using System;
using System;
using System.Drawing;
using System.Windows.Forms;
using Hotshield.Core;
using Hotshield.Data;
using Hotshield.Helpers;

namespace Hotshield.UI
{
    public class SettingsControl : UserControl
    {
        private CheckBox _chkStartup, _chkNotifications, _chkDarkMode;

        public SettingsControl()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            var title = new Label
            {
                Text = "⚙️ Settings",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                Location = new Point(12, 10),
                AutoSize = true
            };
            Controls.Add(title);

            // Startup
            var grpStartup = new GroupBox
            {
                Text = "Startup",
                Location = new Point(12, 40),
                Size = new Size(500, 60),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            _chkStartup = new CheckBox
            {
                Text = "Launch when Windows starts (admin UAC at login)",
                Location = new Point(10, 20),
                AutoSize = true,
                Checked = SettingsRepo.Get("launch_on_startup") == "true"
            };
            _chkStartup.CheckedChanged += (s, e) =>
            {
                SettingsRepo.Set("launch_on_startup", _chkStartup.Checked ? "true" : "false");
                if (_chkStartup.Checked) StartupHelper.Enable(); else StartupHelper.Disable();
            };
            grpStartup.Controls.Add(_chkStartup);
            Controls.Add(grpStartup);

            // Notifications
            var grpNotif = new GroupBox
            {
                Text = "Notifications",
                Location = new Point(12, 110),
                Size = new Size(500, 60),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            _chkNotifications = new CheckBox
            {
                Text = "Show balloon tips when rules change",
                Location = new Point(10, 20),
                AutoSize = true,
                Checked = SettingsRepo.Get("show_notifications") == "true"
            };
            _chkNotifications.CheckedChanged += (s, e) =>
                SettingsRepo.Set("show_notifications", _chkNotifications.Checked ? "true" : "false");
            grpNotif.Controls.Add(_chkNotifications);
            Controls.Add(grpNotif);

            // Dark mode
            var grpTheme = new GroupBox
            {
                Text = "Appearance",
                Location = new Point(12, 180),
                Size = new Size(500, 60),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            _chkDarkMode = new CheckBox
            {
                Text = "Dark mode",
                Location = new Point(10, 20),
                AutoSize = true,
                Checked = SettingsRepo.Get("dark_mode") == "true"
            };
            _chkDarkMode.CheckedChanged += (s, e) =>
            {
                ThemeManager.SetDarkMode(_chkDarkMode.Checked);
                // Apply to parent form
                ThemeManager.ApplyToForm(this.FindForm()!);
            };
            grpTheme.Controls.Add(_chkDarkMode);
            Controls.Add(grpTheme);

            // Data management
            var grpData = new GroupBox
            {
                Text = "Data Management",
                Location = new Point(12, 250),
                Size = new Size(500, 80),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            var btnExport = new Button { Text = "Export Rules (JSON)", Location = new Point(10, 20), Size = new Size(150, 30) };
            btnExport.Click += (s, e) => ExportHelper.ExportToJson();
            grpData.Controls.Add(btnExport);
            var btnImport = new Button { Text = "Import Rules (JSON)", Location = new Point(170, 20), Size = new Size(150, 30) };
            btnImport.Click += (s, e) => ExportHelper.ImportFromJson();
            grpData.Controls.Add(btnImport);
            var btnReset = new Button { Text = "Reset All Data", Location = new Point(330, 20), Size = new Size(150, 30), ForeColor = Color.Red };
            btnReset.Click += (s, e) =>
            {
                if (MessageBox.Show("Delete all rules and networks? This cannot be undone.", "Confirm Reset",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    using var conn = Database.GetConnection();
                    conn.Open();
                    var cmd = conn.CreateCommand();
                    cmd.CommandText = "DELETE FROM app_rules; DELETE FROM metered_networks; DELETE FROM preset_rules; DELETE FROM presets; DELETE FROM kill_switch_whitelist;";
                    cmd.ExecuteNonQuery();
                    PresetRepo.SeedBuiltinPresets(conn);
                    MessageBox.Show("All data reset. Built-in presets restored.");
                }
            };
            grpData.Controls.Add(btnReset);
            Controls.Add(grpData);

            // About
            var btnAbout = new Button { Text = "About Hotshield", Location = new Point(12, 350), Size = new Size(150, 30) };
            btnAbout.Click += (s, e) =>
                MessageBox.Show("Hotshield v2.0\nWindows Data Guard\n\nAll features implemented.\nBy Michael Tadzoka\nBindura University of Science Education\n\n2026",
                    "About", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Controls.Add(btnAbout);
        }
    }
}
