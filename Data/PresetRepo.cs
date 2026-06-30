using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using Hotshield.Models;

namespace Hotshield.Data
{
    public static class PresetRepo
    {
        public static List<Preset> GetAll()
        {
            var list = new List<Preset>();
            using var conn = Database.GetConnection();
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT id, name, description, is_builtin FROM presets ORDER BY id";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new Preset
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Description = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    IsBuiltin = reader.GetInt32(3) == 1
                });
            }
            return list;
        }

        public static void SeedBuiltinPresets(SqliteConnection conn)
        {
            var builtins = new[]
            {
                ("Hotspot Mode", "Blocks most apps by default; you choose what's allowed."),
                ("Work", "Allows work apps; blocks background sync/updates.")
            };

            foreach (var (name, description) in builtins)
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO presets (name, description, is_builtin)
                    SELECT $name, $desc, 1
                    WHERE NOT EXISTS (SELECT 1 FROM presets WHERE name = $name);";
                cmd.Parameters.AddWithValue("$name", name);
                cmd.Parameters.AddWithValue("$desc", description);
                cmd.ExecuteNonQuery();
            }
        }
    }
}