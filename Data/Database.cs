using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;

namespace Hotshield.Data
{
    public static class Database
    {
        private static string _connectionString =
            "Data Source=" +
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                         "Hotshield", "hotshield.db");

        public static SqliteConnection GetConnection()
        {
            return new SqliteConnection(_connectionString);
        }

        public static void Initialise()
        {
            var dir = Path.GetDirectoryName(_connectionString.Split('=')[1]);
            Directory.CreateDirectory(dir);

            using var conn = GetConnection();
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS metered_networks (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ssid TEXT NOT NULL,
                    label TEXT,
                    preset_id INTEGER,
                    is_active INTEGER NOT NULL DEFAULT 1,
                    created_at TEXT DEFAULT (datetime('now'))
                );
                CREATE TABLE IF NOT EXISTS app_rules (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    app_name TEXT NOT NULL,
                    exe_path TEXT NOT NULL,
                    service_name TEXT,
                    action TEXT NOT NULL CHECK(action IN ('block','allow')),
                    direction TEXT NOT NULL DEFAULT 'outbound',
                    rule_group TEXT DEFAULT 'Custom',
                    is_active INTEGER NOT NULL DEFAULT 1,
                    firewall_rule_name TEXT,
                    notes TEXT,
                    created_at TEXT DEFAULT (datetime('now'))
                );
                CREATE TABLE IF NOT EXISTS presets (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL,
                    description TEXT,
                    is_builtin INTEGER DEFAULT 0
                );
                CREATE TABLE IF NOT EXISTS preset_rules (
                    preset_id INTEGER NOT NULL,
                    rule_id INTEGER NOT NULL,
                    PRIMARY KEY (preset_id, rule_id),
                    FOREIGN KEY (preset_id) REFERENCES presets(id) ON DELETE CASCADE,
                    FOREIGN KEY (rule_id) REFERENCES app_rules(id) ON DELETE CASCADE
                );
                CREATE TABLE IF NOT EXISTS settings (
                    key TEXT PRIMARY KEY,
                    value TEXT
                );
                CREATE TABLE IF NOT EXISTS kill_switch_whitelist (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    exe_path TEXT NOT NULL UNIQUE
                );
            ";
            cmd.ExecuteNonQuery();

            // Migration: add is_active column if it doesn't exist (for existing databases)
            try
            {
                var migCmd = conn.CreateCommand();
                migCmd.CommandText = "ALTER TABLE metered_networks ADD COLUMN is_active INTEGER NOT NULL DEFAULT 1";
                migCmd.ExecuteNonQuery();
            }
            catch { /* Column already exists, ignore */ }

            SeedDefaultSettings(conn);
            PresetRepo.SeedBuiltinPresets(conn);
        }

        private static void SeedDefaultSettings(SqliteConnection conn)
        {
            var defaults = new Dictionary<string, string>
            {
                { "launch_on_startup", "true" },
                { "show_notifications", "true" },
                { "theme", "light" },
                { "kill_switch_active", "false" },
                { "dark_mode", "false" },
                { "first_run_complete", "false" },
                { "pause_active", "false" },
                { "pause_resume_time", "" }
            };
            foreach (var kv in defaults)
            {
                var c = conn.CreateCommand();
                c.CommandText = "INSERT OR IGNORE INTO settings (key, value) VALUES (@k, @v)";
                c.Parameters.AddWithValue("@k", kv.Key);
                c.Parameters.AddWithValue("@v", kv.Value);
                c.ExecuteNonQuery();
            }
        }
    }
}