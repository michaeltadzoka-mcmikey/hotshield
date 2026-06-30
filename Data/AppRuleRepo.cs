using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using Hotshield.Models;

namespace Hotshield.Data
{
    public class AppRuleRepo
    {
        public List<AppRule> GetAll()
        {
            var list = new List<AppRule>();
            using var conn = Database.GetConnection();
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT id, app_name, exe_path, service_name, action, direction, rule_group, is_active, firewall_rule_name, notes FROM app_rules ORDER BY id";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                list.Add(ReadRule(reader));
            return list;
        }

        public AppRule? GetById(int id)
        {
            using var conn = Database.GetConnection();
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT id, app_name, exe_path, service_name, action, direction, rule_group, is_active, firewall_rule_name, notes FROM app_rules WHERE id = @id";
            cmd.Parameters.AddWithValue("@id", id);
            using var reader = cmd.ExecuteReader();
            if (reader.Read()) return ReadRule(reader);
            return null;
        }

        public int Insert(AppRule rule)
        {
            using var conn = Database.GetConnection();
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"INSERT INTO app_rules (app_name, exe_path, service_name, action, direction, rule_group, is_active, firewall_rule_name, notes)
                               VALUES (@name, @path, @svc, @action, @dir, @grp, @active, @fwName, @notes);
                               SELECT last_insert_rowid();";
            cmd.Parameters.AddWithValue("@name", rule.AppName);
            cmd.Parameters.AddWithValue("@path", rule.ExePath);
            cmd.Parameters.AddWithValue("@svc", (object?)rule.ServiceName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@action", rule.Action);
            cmd.Parameters.AddWithValue("@dir", rule.Direction);
            cmd.Parameters.AddWithValue("@grp", rule.RuleGroup);
            cmd.Parameters.AddWithValue("@active", rule.IsActive ? 1 : 0);
            cmd.Parameters.AddWithValue("@fwName", (object?)rule.FirewallRuleName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@notes", (object?)rule.Notes ?? DBNull.Value);
            return (int)(long)cmd.ExecuteScalar()!;
        }

        public void Update(AppRule rule)
        {
            using var conn = Database.GetConnection();
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"UPDATE app_rules SET app_name=@name, exe_path=@path, service_name=@svc, action=@action,
                               direction=@dir, rule_group=@grp, is_active=@active, firewall_rule_name=@fwName, notes=@notes
                               WHERE id=@id";
            cmd.Parameters.AddWithValue("@name", rule.AppName);
            cmd.Parameters.AddWithValue("@path", rule.ExePath);
            cmd.Parameters.AddWithValue("@svc", (object?)rule.ServiceName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@action", rule.Action);
            cmd.Parameters.AddWithValue("@dir", rule.Direction);
            cmd.Parameters.AddWithValue("@grp", rule.RuleGroup);
            cmd.Parameters.AddWithValue("@active", rule.IsActive ? 1 : 0);
            cmd.Parameters.AddWithValue("@fwName", (object?)rule.FirewallRuleName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@notes", (object?)rule.Notes ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@id", rule.Id);
            cmd.ExecuteNonQuery();
        }

        public void Delete(int id)
        {
            using var conn = Database.GetConnection();
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM app_rules WHERE id = @id";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        public List<AppRule> GetActiveRulesForPreset(int? presetId)
        {
            if (presetId == null) return new List<AppRule>();
            var list = new List<AppRule>();
            using var conn = Database.GetConnection();
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT r.id, r.app_name, r.exe_path, r.service_name, r.action, r.direction, r.rule_group, r.is_active, r.firewall_rule_name, r.notes
                FROM app_rules r
                INNER JOIN preset_rules pr ON r.id = pr.rule_id
                WHERE pr.preset_id = @pid AND r.is_active = 1";
            cmd.Parameters.AddWithValue("@pid", presetId.Value);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                list.Add(ReadRule(reader));
            return list;
        }

        public List<string> GetAllFirewallRuleNames()
        {
            var names = new List<string>();
            using var conn = Database.GetConnection();
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT firewall_rule_name FROM app_rules WHERE firewall_rule_name IS NOT NULL AND is_active = 1";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                names.Add(reader.GetString(0));
            return names;
        }

        public void LinkRuleToPreset(int presetId, int ruleId)
        {
            using var conn = Database.GetConnection();
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT OR IGNORE INTO preset_rules (preset_id, rule_id) VALUES (@pid, @rid)";
            cmd.Parameters.AddWithValue("@pid", presetId);
            cmd.Parameters.AddWithValue("@rid", ruleId);
            cmd.ExecuteNonQuery();
        }

        public void UnlinkRuleFromPreset(int presetId, int ruleId)
        {
            using var conn = Database.GetConnection();
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM preset_rules WHERE preset_id = @pid AND rule_id = @rid";
            cmd.Parameters.AddWithValue("@pid", presetId);
            cmd.Parameters.AddWithValue("@rid", ruleId);
            cmd.ExecuteNonQuery();
        }

        private AppRule ReadRule(SqliteDataReader reader)
        {
            return new AppRule
            {
                Id = reader.GetInt32(0),
                AppName = reader.GetString(1),
                ExePath = reader.GetString(2),
                ServiceName = reader.IsDBNull(3) ? null : reader.GetString(3),
                Action = reader.GetString(4),
                Direction = reader.GetString(5),
                RuleGroup = reader.GetString(6),
                IsActive = reader.GetInt32(7) == 1,
                FirewallRuleName = reader.IsDBNull(8) ? null : reader.GetString(8),
                Notes = reader.IsDBNull(9) ? "" : reader.GetString(9)
            };
        }
    }
}
