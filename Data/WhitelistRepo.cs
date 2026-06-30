using System.Collections.Generic;
using Microsoft.Data.Sqlite;

namespace Hotshield.Data
{
    public static class WhitelistRepo
    {
        public static List<string> GetAllowedApps()
        {
            var list = new List<string>();
            using var conn = Database.GetConnection();
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT exe_path FROM kill_switch_whitelist";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                list.Add(reader.GetString(0));
            return list;
        }

        public static void Add(string exePath)
        {
            using var conn = Database.GetConnection();
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT OR IGNORE INTO kill_switch_whitelist (exe_path) VALUES (@p)";
            cmd.Parameters.AddWithValue("@p", exePath);
            cmd.ExecuteNonQuery();
        }

        public static void Remove(string exePath)
        {
            using var conn = Database.GetConnection();
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM kill_switch_whitelist WHERE exe_path = @p";
            cmd.Parameters.AddWithValue("@p", exePath);
            cmd.ExecuteNonQuery();
        }

        public static void CreateTable(SqliteConnection conn)
        {
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"CREATE TABLE IF NOT EXISTS kill_switch_whitelist (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                exe_path TEXT NOT NULL UNIQUE
            );";
            cmd.ExecuteNonQuery();
        }
    }
}
