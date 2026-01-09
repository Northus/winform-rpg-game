using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using rpg_deneme.Models;

namespace rpg_deneme.Data;

/// <summary>
/// Karakter veritabanı işlemlerini yöneten depo sınıfı.
/// </summary>
public class CharacterRepository
{
    /// <summary>
    /// Veritabanındaki tüm karakterleri liste olarak döner.
    /// </summary>
    public List<CharacterModel> GetCharacters()
    {
        List<CharacterModel> list = new List<CharacterModel>();
        using (SqliteConnection conn = DatabaseHelper.GetConnection())
        {
            conn.Open();
            using SqliteCommand cmd = new SqliteCommand("SELECT * FROM Characters", conn);
            using SqliteDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                list.Add(new CharacterModel
                {
                    CharacterID = Convert.ToInt32(dr["CharacterID"]),
                    Name = dr["Name"]?.ToString(),
                    Class = Convert.ToByte(dr["Class"]),
                    Level = ((dr["Level"] == DBNull.Value) ? 1 : Convert.ToInt32(dr["Level"])),
                    Experience = ((dr["Experience"] != DBNull.Value) ? Convert.ToInt64(dr["Experience"]) : 0),
                    STR = ((dr["STR"] != DBNull.Value) ? Convert.ToInt32(dr["STR"]) : 0),
                    DEX = ((dr["DEX"] != DBNull.Value) ? Convert.ToInt32(dr["DEX"]) : 0),
                    INT = ((dr["INT"] != DBNull.Value) ? Convert.ToInt32(dr["INT"]) : 0),
                    VIT = ((dr["VIT"] != DBNull.Value) ? Convert.ToInt32(dr["VIT"]) : 0),
                    StatPoints = ((dr["StatPoints"] != DBNull.Value) ? Convert.ToInt32(dr["StatPoints"]) : 0),
                    SkillPoints = ((dr["SkillPoints"] != DBNull.Value) ? Convert.ToInt32(dr["SkillPoints"]) : 0),
                    HP = ((dr["CurrentHP"] != DBNull.Value) ? Convert.ToInt32(dr["CurrentHP"]) : 100),
                    Mana = ((dr["CurrentMana"] != DBNull.Value) ? Convert.ToInt32(dr["CurrentMana"]) : 50),
                    Gold = ((dr["Gold"] != DBNull.Value) ? Convert.ToInt64(dr["Gold"]) : 0),
                    MaxSurvivalWave = ((dr["MaxSurvivalWave"] == DBNull.Value) ? 1 : Convert.ToInt32(dr["MaxSurvivalWave"])),
                    CurrentZoneID = ((dr["CurrentZoneID"] == DBNull.Value) ? 1 : Convert.ToInt32(dr["CurrentZoneID"])),
                    MaxUnlockedZoneID = ((dr["MaxUnlockedZoneID"] == DBNull.Value) ? 1 : Convert.ToInt32(dr["MaxUnlockedZoneID"])),
                    SlotIndex = ((dr["SlotIndex"] == DBNull.Value) ? 0 : Convert.ToInt32(dr["SlotIndex"]))
                });
            }
        }
        return list;
    }

    /// <summary>
    /// Yeni bir karakter oluşturur ve veritabanına kaydeder.
    /// </summary>
    public bool CreateCharacter(CharacterModel hero)
    {
        using SqliteConnection conn = DatabaseHelper.GetConnection();
        conn.Open();
        int str = ((hero.STR > 0) ? hero.STR : 5);
        int dex = ((hero.DEX > 0) ? hero.DEX : 5);
        int intel = ((hero.INT > 0) ? hero.INT : 5);
        int vit = ((hero.VIT > 0) ? hero.VIT : 10);
        int startHP = 100;
        int startMana = 50;
        string sql = @"
                INSERT INTO Characters 
                (
                    Name, Class, Level, Experience, 
                    STR, DEX, INT, VIT, StatPoints, SkillPoints, 
                    CurrentHP, CurrentMana, 
                    MaxUnlockedZoneID, CurrentZoneID, MaxSurvivalWave,
                    Gold, SlotIndex
                ) 
                VALUES 
                (
                    @name, @class, 1, 0, 
                    @str, @dex, @int, @vit, 0, 6,
                    @hp, @mana,   
                    1, 1, 1,
                    10000,
                    @slot
                )";
        using SqliteCommand cmd = new SqliteCommand(sql, conn);
        cmd.Parameters.AddWithValue("@name", hero.Name ?? "Adsız Kahraman");
        cmd.Parameters.AddWithValue("@class", hero.Class);
        cmd.Parameters.AddWithValue("@str", str);
        cmd.Parameters.AddWithValue("@dex", dex);
        cmd.Parameters.AddWithValue("@int", intel);
        cmd.Parameters.AddWithValue("@vit", vit);
        cmd.Parameters.AddWithValue("@hp", startHP);
        cmd.Parameters.AddWithValue("@mana", startMana);
        cmd.Parameters.AddWithValue("@slot", hero.SlotIndex);
        return cmd.ExecuteNonQuery() > 0;
    }

    /// <summary>
    /// İsmin kullanılabilir olup olmadığını kontrol eder.
    /// </summary>
    public bool IsNameAvailable(string name)
    {
        using SqliteConnection conn = DatabaseHelper.GetConnection();
        string sql = "SELECT COUNT(*) FROM Characters WHERE Name = @name";
        using SqliteCommand cmd = new SqliteCommand(sql, conn);
        cmd.Parameters.AddWithValue("@name", name);
        conn.Open();
        long count = (long)cmd.ExecuteScalar();
        return count == 0;
    }

    /// <summary>
    /// Belirtilen ID'ye sahip karakteri siler.
    /// </summary>
    public bool DeleteCharacter(int characterId)
    {
        using SqliteConnection conn = DatabaseHelper.GetConnection();
        string sql = "DELETE FROM Characters WHERE CharacterID = @id";
        using SqliteCommand cmd = new SqliteCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", characterId);
        conn.Open();
        return cmd.ExecuteNonQuery() > 0;
    }

    /// <summary>
    /// Karakterin istatistiklerini günceller.
    /// </summary>
    public bool UpdateStats(CharacterModel hero)
    {
        using SqliteConnection conn = DatabaseHelper.GetConnection();
        string sql = @"
            UPDATE Characters 
            SET STR = @str, DEX = @dex, INT = @int, VIT = @vit, StatPoints = @pts 
            WHERE CharacterID = @id";
        using SqliteCommand cmd = new SqliteCommand(sql, conn);
        cmd.Parameters.AddWithValue("@str", hero.STR);
        cmd.Parameters.AddWithValue("@dex", hero.DEX);
        cmd.Parameters.AddWithValue("@int", hero.INT);
        cmd.Parameters.AddWithValue("@vit", hero.VIT);
        cmd.Parameters.AddWithValue("@pts", hero.StatPoints);
        cmd.Parameters.AddWithValue("@id", hero.CharacterID);
        conn.Open();
        return cmd.ExecuteNonQuery() > 0;
    }

    /// <summary>
    /// Karakterin seviye, tecrübe, can gibi genel ilerlemesini günceller.
    /// </summary>
    public bool UpdateProgress(CharacterModel hero)
    {
        using SqliteConnection conn = DatabaseHelper.GetConnection();
        string sql = @"
                UPDATE Characters 
                SET Level = @lvl, 
                    Experience = @exp, 
                    StatPoints = @statPts,
                    SkillPoints = @skillPts,
                    CurrentHP = @hp, 
                    CurrentMana = @mana, 
                    MaxSurvivalWave = @maxWave,
                    MaxUnlockedZoneID = @maxZone,
                    CurrentZoneID = @currentZone
                WHERE CharacterID = @id";
        using SqliteCommand cmd = new SqliteCommand(sql, conn);
        cmd.Parameters.AddWithValue("@lvl", hero.Level);
        cmd.Parameters.AddWithValue("@exp", hero.Experience);
        cmd.Parameters.AddWithValue("@statPts", hero.StatPoints);
        cmd.Parameters.AddWithValue("@skillPts", hero.SkillPoints);
        cmd.Parameters.AddWithValue("@hp", hero.HP);
        cmd.Parameters.AddWithValue("@mana", hero.Mana);
        cmd.Parameters.AddWithValue("@id", hero.CharacterID);
        cmd.Parameters.AddWithValue("@maxWave", hero.MaxSurvivalWave);
        cmd.Parameters.AddWithValue("@maxZone", hero.MaxUnlockedZoneID);
        cmd.Parameters.AddWithValue("@currentZone", hero.CurrentZoneID);
        conn.Open();
        return cmd.ExecuteNonQuery() > 0;
    }

    /// <summary>
    /// Karakterin statlarını (seviye, xp, hp, mana vb.) günceller.
    /// </summary>
    public void UpdateCharacterStats(CharacterModel hero)
    {
        using SqliteConnection conn = DatabaseHelper.GetConnection();
        conn.Open();
        string sql = @"
                UPDATE Characters 
                SET 
                    Level = @lvl,
                    Experience = @xp,
                    StatPoints = @sp,
                    SkillPoints = @skillSp,
                    CurrentHP = @hp,
                    CurrentMana = @mana
                WHERE CharacterID = @id";
        using SqliteCommand cmd = new SqliteCommand(sql, conn);
        cmd.Parameters.AddWithValue("@lvl", hero.Level);
        cmd.Parameters.AddWithValue("@xp", hero.Experience);
        cmd.Parameters.AddWithValue("@sp", hero.StatPoints);
        cmd.Parameters.AddWithValue("@skillSp", hero.SkillPoints);
        cmd.Parameters.AddWithValue("@hp", hero.HP);
        cmd.Parameters.AddWithValue("@mana", hero.Mana);
        cmd.Parameters.AddWithValue("@id", hero.CharacterID);
        cmd.ExecuteNonQuery();
    }

    public void UpdateMaxSurvivalWave(int characterId, int newMaxWave)
    {
        using SqliteConnection conn = DatabaseHelper.GetConnection();
        string sql = "UPDATE Characters SET MaxSurvivalWave = @wave WHERE CharacterID = @id";
        using SqliteCommand cmd = new SqliteCommand(sql, conn);
        cmd.Parameters.AddWithValue("@wave", newMaxWave);
        cmd.Parameters.AddWithValue("@id", characterId);
        conn.Open();
        cmd.ExecuteNonQuery();
    }

    public void UpdateSlotIndex(int characterId, int newSlotIndex)
    {
        using SqliteConnection conn = DatabaseHelper.GetConnection();
        string sql = "UPDATE Characters SET SlotIndex = @slot WHERE CharacterID = @id";
        using SqliteCommand cmd = new SqliteCommand(sql, conn);
        cmd.Parameters.AddWithValue("@slot", newSlotIndex);
        cmd.Parameters.AddWithValue("@id", characterId);
        conn.Open();
        cmd.ExecuteNonQuery();
    }
}
