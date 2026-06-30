using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using Hotshield.Data;
using Hotshield.Models;

namespace Hotshield.Helpers
{
    public static class ExportHelper
    {
        public static void ExportToJson()
        {
            var networks = new MeteredNetworkRepo().GetAll();
            var rules = new AppRuleRepo().GetAll();
            var presets = PresetRepo.GetAll();
            var data = new { networks, rules, presets };

            using var sfd = new SaveFileDialog
            {
                Filter = "JSON files (*.json)|*.json",
                FileName = "hotshield_backup.json"
            };
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                File.WriteAllText(sfd.FileName, JsonConvert.SerializeObject(data, Formatting.Indented));
                MessageBox.Show("Rules exported successfully.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        public static void ImportFromJson()
        {
            using var ofd = new OpenFileDialog
            {
                Filter = "JSON files (*.json)|*.json"
            };
            if (ofd.ShowDialog() != DialogResult.OK) return;

            string json = File.ReadAllText(ofd.FileName);
            var imported = JsonConvert.DeserializeAnonymousType(json, new { rules = new List<AppRule>() });
            var repo = new AppRuleRepo();
            foreach (var rule in imported.rules)
            {
                rule.FirewallRuleName = null;
                rule.Id = 0;
                repo.Insert(rule);
            }
            MessageBox.Show("Rules imported. You must re-apply your network preset to activate them.", "Import", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
