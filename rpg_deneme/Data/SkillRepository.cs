using rpg_deneme.Core;
using rpg_deneme.Models;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using System;

namespace rpg_deneme.Data;

public class SkillRepository
{
    public List<SkillModel> GetSkillsByClass(Enums.CharacterClass charClass, int characterId)
    {
        List<SkillModel> skills = new List<SkillModel>();
        using (var conn = DatabaseHelper.GetConnection())
        {
            conn.Open();

            // 1. Get Skills
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM Skills WHERE Class = @Class";
                cmd.Parameters.AddWithValue("@Class", (int)charClass);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var s = new SkillModel();
                        s.SkillID = Convert.ToInt32(reader["SkillID"]);
                        s.Name = reader["Name"].ToString();
                        s.Description = reader["Description"] != DBNull.Value ? reader["Description"].ToString() : "";
                        s.Class = (Enums.CharacterClass)Convert.ToInt32(reader["Class"]);
                        s.Type = (Enums.SkillType)Convert.ToInt32(reader["Type"]);
                        s.MaxLevel = Convert.ToInt32(reader["MaxLevel"]);
                        s.RequiredLevel = Convert.ToInt32(reader["RequiredLevel"]);
                        s.Cooldown = Convert.ToDouble(reader["Cooldown"]);
                        s.ManaCost = Convert.ToInt32(reader["ManaCost"]);
                        s.EffectType = (Enums.SkilEffectType)Convert.ToInt32(reader["EffectType"]);
                        s.BaseEffectValue = Convert.ToDouble(reader["BaseEffectValue"]);
                        s.EffectScaling = Convert.ToDouble(reader["EffectScaling"]);
                        s.Duration = Convert.ToDouble(reader["Duration"]);
                        s.IconPath = reader["IconPath"] != DBNull.Value ? reader["IconPath"].ToString() : "";
                        s.Row = Convert.ToInt32(reader["RowIndex"]);
                        s.Col = Convert.ToInt32(reader["ColIndex"]);

                        // Yeni alanlar - güvenli okuma
                        s.PassiveStatType = TryGetEnum<Enums.PassiveStatType>(reader, "PassiveStatType");
                        s.SecondaryEffect = TryGetEnum<Enums.SkillSecondaryEffect>(reader, "SecondaryEffect");
                        s.SecondaryEffectValue = TryGetDouble(reader, "SecondaryEffectValue");
                        s.SecondaryEffectDuration = TryGetDouble(reader, "SecondaryEffectDuration");

                        skills.Add(s);
                    }
                }
            }

            // 2. Get Dependencies
            List<(int, int)> deps = new List<(int, int)>();
            using (var cmdDeps = conn.CreateCommand())
            {
                cmdDeps.CommandText = "SELECT SkillID, PrerequisiteSkillID FROM SkillDependencies";
                using (var reader = cmdDeps.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        deps.Add((Convert.ToInt32(reader[0]), Convert.ToInt32(reader[1])));
                    }
                }
            }

            // 3. Get Character Progress
            Dictionary<int, int> progress = new Dictionary<int, int>();
            using (var cmdProg = conn.CreateCommand())
            {
                cmdProg.CommandText = "SELECT SkillID, CurrentLevel FROM CharacterSkills WHERE CharacterID = @CharID";
                cmdProg.Parameters.AddWithValue("@CharID", characterId);
                using (var reader = cmdProg.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        progress[Convert.ToInt32(reader[0])] = Convert.ToInt32(reader[1]);
                    }
                }
            }

            // Map everything
            foreach (var skill in skills)
            {
                foreach (var d in deps)
                {
                    if (d.Item1 == skill.SkillID)
                    {
                        skill.PrerequisiteSkillIDs.Add(d.Item2);
                    }
                }

                if (progress.ContainsKey(skill.SkillID))
                {
                    skill.CurrentLevel = progress[skill.SkillID];
                }
            }
        }
        return skills;
    }

    private T TryGetEnum<T>(SqliteDataReader reader, string column) where T : struct
    {
        try
        {
            int ordinal = reader.GetOrdinal(column);
            if (!reader.IsDBNull(ordinal))
            {
                return (T)Enum.ToObject(typeof(T), reader.GetInt32(ordinal));
            }
        }
        catch { }
        return default(T);
    }

    private double TryGetDouble(SqliteDataReader reader, string column)
    {
        try
        {
            int ordinal = reader.GetOrdinal(column);
            if (!reader.IsDBNull(ordinal))
            {
                return reader.GetDouble(ordinal);
            }
        }
        catch { }
        return 0;
    }

    public void SaveCharacterSkill(int characterId, int skillId, int level)
    {
        using (var conn = DatabaseHelper.GetConnection())
        {
            conn.Open();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    INSERT INTO CharacterSkills (CharacterID, SkillID, CurrentLevel)
                    VALUES (@CharID, @SkillID, @Level)
                    ON CONFLICT(CharacterID, SkillID) DO UPDATE SET CurrentLevel = @Level";
                cmd.Parameters.AddWithValue("@CharID", characterId);
                cmd.Parameters.AddWithValue("@SkillID", skillId);
                cmd.Parameters.AddWithValue("@Level", level);
                cmd.ExecuteNonQuery();
            }
        }
    }

    public void ResetCharacterSkills(int characterId)
    {
        using (var conn = DatabaseHelper.GetConnection())
        {
            conn.Open();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM CharacterSkills WHERE CharacterID = @CharID";
                cmd.Parameters.AddWithValue("@CharID", characterId);
                cmd.ExecuteNonQuery();
            }
        }
    }
}