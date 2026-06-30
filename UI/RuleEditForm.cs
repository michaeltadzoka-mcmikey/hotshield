using System;
using System.Drawing;
using System.Windows.Forms;
using Hotshield.Models;

namespace Hotshield.UI
{
    public class RuleEditForm : Form
    {
        private TextBox _txtName, _txtPath, _txtService, _txtNotes;
        private ComboBox _cmbAction, _cmbGroup;
        public AppRule Rule { get; private set; }

        public RuleEditForm(AppRule? existing = null)
        {
            Rule = existing ?? new AppRule { Action = "block", Direction = "outbound", RuleGroup = "Custom" };
            Text = existing == null ? "Add Firewall Rule" : "Edit Firewall Rule";
            Size = new Size(450, 320);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            int y = 15;
            AddLabeledControl("App Name:", out _txtName, ref y, Rule.AppName);
            AddLabeledControl("Executable Path:", out _txtPath, ref y, Rule.ExePath);
            AddLabeledControl("Service Name (optional):", out _txtService, ref y, Rule.ServiceName ?? "");

            // Action combo
            var lblAction = new Label { Text = "Action:", Location = new Point(15, y), AutoSize = true };
            Controls.Add(lblAction);
            _cmbAction = new ComboBox { Location = new Point(130, y - 2), Width = 120, DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbAction.Items.AddRange(new[] { "block", "allow" });
            _cmbAction.SelectedItem = Rule.Action;
            Controls.Add(_cmbAction);
            y += 30;

            // Group combo
            var lblGroup = new Label { Text = "Group:", Location = new Point(15, y), AutoSize = true };
            Controls.Add(lblGroup);
            _cmbGroup = new ComboBox { Location = new Point(130, y - 2), Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbGroup.Items.AddRange(new[] { "Windows System", "Microsoft Apps", "Browser", "Gaming", "Communication", "Custom" });
            _cmbGroup.SelectedItem = Rule.RuleGroup;
            Controls.Add(_cmbGroup);
            y += 30;

            AddLabeledControl("Notes:", out _txtNotes, ref y, Rule.Notes);

            var btnOk = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new Point(120, y + 10), Size = new Size(90, 30) };
            btnOk.Click += (s, e) => SaveRule();
            Controls.Add(btnOk);
            var btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(220, y + 10), Size = new Size(90, 30) };
            Controls.Add(btnCancel);
        }

        private void AddLabeledControl(string label, out TextBox textBox, ref int y, string initialValue)
        {
            var lbl = new Label { Text = label, Location = new Point(15, y), AutoSize = true };
            Controls.Add(lbl);
            textBox = new TextBox { Location = new Point(130, y - 2), Width = 280 };
            textBox.Text = initialValue;
            Controls.Add(textBox);
            y += 28;
        }

        private void SaveRule()
        {
            Rule.AppName = _txtName.Text.Trim();
            Rule.ExePath = _txtPath.Text.Trim();
            Rule.ServiceName = string.IsNullOrWhiteSpace(_txtService.Text) ? null : _txtService.Text.Trim();
            Rule.Action = _cmbAction.SelectedItem.ToString();
            Rule.RuleGroup = _cmbGroup.SelectedItem.ToString();
            Rule.Notes = _txtNotes.Text.Trim();
        }
    }
}