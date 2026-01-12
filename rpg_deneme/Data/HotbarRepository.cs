using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using rpg_deneme.Core; // For HotbarInfo
using rpg_deneme.Models;

namespace rpg_deneme.Data;

public class HotbarRepository
{
    public HotbarRepository()
    {
    }

    public void SaveHotbar(int characterId, int slotIndex, HotbarInfo info)
    {
        using (var conn = DatabaseHelper.GetConnection())
        {
            conn.Open();
            // We use ReferenceID and Type now. 
            // We can ignore ItemInstanceID column or set it same as ReferenceID if Type=0 for legacy support, 
            // but let's just use ReferenceID if logic allows.
            // Updated Schema has Type and ReferenceID.

            string sql = @"
                INSERT OR REPLACE INTO HotbarSettings (CharacterID, SlotIndex, Type, ReferenceID)
                VALUES (@CharID, @Slot, @Type, @RefID); 
            ";
            // Updated to match current schema (Type + ReferenceID)

            using (var cmd = new SqliteCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@CharID", characterId);
                cmd.Parameters.AddWithValue("@Slot", slotIndex);
                if (info != null)
                {
                    cmd.Parameters.AddWithValue("@Type", info.Type);
                    cmd.Parameters.AddWithValue("@RefID", info.ReferenceId);
                }
                else
                {
                    cmd.Parameters.AddWithValue("@Type", 0);
                    cmd.Parameters.AddWithValue("@RefID", DBNull.Value);
                }
                cmd.ExecuteNonQuery();
            }
        }
    }

    public Dictionary<int, HotbarInfo> LoadHotbar(int characterId)
    {
        var result = new Dictionary<int, HotbarInfo>();
        using (var conn = DatabaseHelper.GetConnection())
        {
            conn.Open();
            // Select Type, ReferenceID. If ReferenceID is null, maybe fallback to ItemInstanceID?
            string sql = "SELECT SlotIndex, Type, ReferenceID FROM HotbarSettings WHERE CharacterID = @CharID";
            using (var cmd = new SqliteCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@CharID", characterId);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int slot = reader.GetInt32(0);
                        int type = 0;
                        if (!reader.IsDBNull(1)) type = reader.GetInt32(1);

                        long refId = 0;
                        if (!reader.IsDBNull(2)) refId = reader.GetInt64(2);

                        if (refId > 0)
                        {
                            result[slot] = new HotbarInfo { Type = type, ReferenceId = refId };
                        }
                    }
                }
            }
        }
        return result;
    }
    public void RemoveSkillSlots(int characterId)
    {
        using (var conn = DatabaseHelper.GetConnection())
        {
            conn.Open();
            string sql = "DELETE FROM HotbarSettings WHERE CharacterID = @CharID AND Type = 1"; // Type 1 = Skill
            using (var cmd = new SqliteCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@CharID", characterId);
                cmd.ExecuteNonQuery();
            }
        }
    }
}
