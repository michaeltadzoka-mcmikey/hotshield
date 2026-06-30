using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using Hotshield.Models;

namespace Hotshield.Data
{
    public class MeteredNetworkRepo
    {
        public List<MeteredNetwork> GetAll()
        {
            var list = new List<MeteredNetwork>();
            using var conn = Database.GetConnection();
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT id, ssid, label, preset_id, is_active, created_at FROM metered_networks ORDER BY ssid";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(ReadNetwork(reader));
            }
            return list;
        }

        public MeteredNetwork? FindBySsid(string ssid)
        {
            using var conn = Database.GetConnection();
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT id, ssid, label, preset_id, is_active, created_at FROM metered_networks WHERE ssid = @ssid";
            cmd.Parameters.AddWithValue("@ssid", ssid);
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return ReadNetwork(reader);
            }
            return null;
        }

        public MeteredNetwork? FindById(int id)
        {
            using var conn = Database.GetConnection();
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT id, ssid, label, preset_id, is_active, created_at FROM metered_networks WHERE id = @id";
            cmd.Parameters.AddWithValue("@id", id);
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return ReadNetwork(reader);
            }
            return null;
        }

        public void Add(MeteredNetwork net)
        {
            using var conn = Database.GetConnection();
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO metered_networks (ssid, label, preset_id, is_active) VALUES (@ssid, @label, @presetId, @active)";
            cmd.Parameters.AddWithValue("@ssid", net.Ssid);
            cmd.Parameters.AddWithValue("@label", (object?)net.Label ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@presetId", (object?)net.PresetId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@active", net.IsActive ? 1 : 0);
            cmd.ExecuteNonQuery();
        }

        public void Delete(int id)
        {
            using var conn = Database.GetConnection();
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM metered_networks WHERE id = @id";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        public void UpdatePreset(int id, int? presetId)
        {
            using var conn = Database.GetConnection();
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE metered_networks SET preset_id = @presetId WHERE id = @id";
            cmd.Parameters.AddWithValue("@presetId", (object?)presetId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        public void UpdateIsActive(int id, bool isActive)
        {
            using var conn = Database.GetConnection();
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE metered_networks SET is_active = @active WHERE id = @id";
            cmd.Parameters.AddWithValue("@active", isActive ? 1 : 0);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        private MeteredNetwork ReadNetwork(SqliteDataReader reader)
        {
            return new MeteredNetwork
            {
                Id = reader.GetInt32(0),
                Ssid = reader.GetString(1),
                Label = reader.IsDBNull(2) ? "" : reader.GetString(2),
                PresetId = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                IsActive = reader.GetInt32(4) == 1,
                CreatedAt = reader.GetString(5)
            };
        }
    }
}