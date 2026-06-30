using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Hotshield.Data;

namespace Hotshield.UI
{
    public partial class WhitelistForm : Form
    {
        private ListBox _listBox;
        public WhitelistForm()
        {
            Text = "Kill Switch Whitelist";
            Size = new Size(450, 350);
            StartPosition = FormStartPosition.CenterParent;
            Font = new Font("Segoe UI", 9F);

            var lbl = new Label
            {
                Text = "Apps allowed when Kill Switch is active:",
                Location = new Point(12, 12),
                AutoSize = true,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            Controls.Add(lbl);

            _listBox = new ListBox
            {
                Location = new Point(12, 35),
                Size = new Size(300, 200)
            };
            Controls.Add(_listBox);

            var btnAdd = new Button { Text = "➕ Add", Location = new Point(320, 35), Size = new Size(90, 30) };
            btnAdd.Click += (s, e) => AddApp();
            Controls.Add(btnAdd);

            var btnRemove = new Button { Text = "🗑 Remove", Location = new Point(320, 75), Size = new Size(90, 30) };
            btnRemove.Click += (s, e) =>
            {
                if (_listBox.SelectedItem != null)
                {
                    WhitelistRepo.Remove(_listBox.SelectedItem.ToString()!);
                    RefreshList();
                }
            };
            Controls.Add(btnRemove);

            var btnClose = new Button { Text = "Close", Location = new Point(160, 250), Size = new Size(80, 30) };
            btnClose.Click += (s, e) => Close();
            Controls.Add(btnClose);

            RefreshList();
        }

        private void AddApp()
        {
            using var ofd = new OpenFileDialog { Filter = "Executable (*.exe)|*.exe" };
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                WhitelistRepo.Add(ofd.FileName);
                RefreshList();
            }
        }

        private void RefreshList()
        {
            _listBox.Items.Clear();
            foreach (var path in WhitelistRepo.GetAllowedApps())
                _listBox.Items.Add(path);
        }
    }
}
