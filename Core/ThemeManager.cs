using System.Drawing;
using System.Windows.Forms;

namespace Hotshield.Core
{
    public static class ThemeManager
    {
        public static bool IsDarkMode { get; private set; }

        public static Color BackColor => IsDarkMode ? Color.FromArgb(30, 30, 30) : Color.FromArgb(245, 245, 245);
        public static Color ForeColor => IsDarkMode ? Color.FromArgb(224, 224, 224) : Color.Black;
        public static Color AccentColor => Color.FromArgb(74, 144, 217);
        public static Color StatusGreen => Color.FromArgb(16, 185, 129);
        public static Color StatusAmber => Color.FromArgb(245, 158, 11);
        public static Color StatusRed => Color.FromArgb(239, 68, 68);

        public static void SetDarkMode(bool dark)
        {
            IsDarkMode = dark;
            Data.SettingsRepo.Set("dark_mode", dark ? "true" : "false");
        }

        public static void ApplyToForm(Form form)
        {
            if (form == null) return;
            form.BackColor = BackColor;
            form.ForeColor = ForeColor;
            foreach (Control ctrl in form.Controls)
                ApplyToControl(ctrl);
        }

        private static void ApplyToControl(Control ctrl)
        {
            if (ctrl == null) return;
            if (ctrl is Button btn)
            {
                btn.BackColor = IsDarkMode ? Color.FromArgb(45, 45, 45) : SystemColors.Control;
                btn.ForeColor = ForeColor;
            }
            else if (ctrl is TextBox || ctrl is ComboBox || ctrl is ListBox)
            {
                ctrl.BackColor = IsDarkMode ? Color.FromArgb(45, 45, 45) : Color.White;
                ctrl.ForeColor = ForeColor;
            }
            else if (ctrl is DataGridView dgv)
            {
                dgv.BackgroundColor = BackColor;
                dgv.DefaultCellStyle.BackColor = IsDarkMode ? Color.FromArgb(45, 45, 45) : Color.White;
                dgv.DefaultCellStyle.ForeColor = ForeColor;
                dgv.ColumnHeadersDefaultCellStyle.BackColor = IsDarkMode ? Color.FromArgb(60, 60, 60) : SystemColors.Control;
                dgv.ColumnHeadersDefaultCellStyle.ForeColor = ForeColor;
            }
            else if (ctrl is GroupBox gb)
            {
                gb.ForeColor = AccentColor;
            }
            foreach (Control child in ctrl.Controls)
                ApplyToControl(child);
        }
    }
}
