using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;
using rpg_deneme.Core;
using rpg_deneme.Models;

namespace rpg_deneme.Data;

public class ZoneRepository
{
    public List<ZoneModel> GetZones()
    {
        List<ZoneModel> list = new List<ZoneModel>();
        using (SqliteConnection conn = DatabaseHelper.GetConnection())
        {
            conn.Open();
            string sql = "SELECT ZoneID, Name, Description, MinLevel, OrderIndex FROM Zones ORDER BY OrderIndex";
            using SqliteCommand cmd = new SqliteCommand(sql, conn);
            using SqliteDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                list.Add(new ZoneModel
                {
                    ZoneID = Convert.ToInt32(dr["ZoneID"]),
                    Name = dr["Name"]?.ToString(),
                    Description = dr["Description"]?.ToString(),
                    MinLevel = ((dr["MinLevel"] == DBNull.Value) ? 1 : Convert.ToInt32(dr["MinLevel"])),
                    OrderIndex = ((dr["OrderIndex"] != DBNull.Value) ? Convert.ToInt32(dr["OrderIndex"]) : 0)
                });
            }
        }
        return list;
    }

    public EnemyModel GetRandomEnemy(int zoneId)
    {
        List<(int, int)> candidates = new List<(int, int)>();
        using (SqliteConnection conn = DatabaseHelper.GetConnection())
        {
            conn.Open();
            string sql = "\r\n SELECT Z.EnemyID, Z.SpawnRate\r\n FROM ZoneEnemies Z\r\n INNER JOIN Enemies E ON Z.EnemyID = E.EnemyID\r\n WHERE Z.ZoneID = @zid AND (E.IsBoss =0 OR E.IsBoss IS NULL)";
            using SqliteCommand cmd = new SqliteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@zid", zoneId);
            using SqliteDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                candidates.Add((Convert.ToInt32(dr["EnemyID"]), (dr["SpawnRate"] == DBNull.Value) ? 1 : Convert.ToInt32(dr["SpawnRate"])));
            }
        }
        if (candidates.Count == 0)
        {
            return GetAnyNonBossEnemy();
        }
        int total = candidates.Sum<(int, int)>(((int EnemyID, int SpawnRate) tuple2) => tuple2.SpawnRate);
        Random rnd = new Random();
        int pick = rnd.Next(0, Math.Max(1, total));
        int acc = 0;
        int chosenId = candidates[0].Item1;
        foreach (var c in candidates)
        {
            acc += c.Item2;
            if (pick < acc)
            {
                (chosenId, _) = c;
                break;
            }
        }
        return LoadEnemyById(chosenId);
    }

    public EnemyModel GetBossForZone(int zoneId)
    {
        using (SqliteConnection conn = DatabaseHelper.GetConnection())
        {
            conn.Open();
            string sql = "\r\n SELECT e.* FROM Enemies e\r\n INNER JOIN ZoneEnemies ze ON e.EnemyID = ze.EnemyID\r\n WHERE ze.ZoneID = @zid AND e.IsBoss =1\r\n ORDER BY ze.SpawnRate DESC\r\n LIMIT 1";
            using SqliteCommand cmd = new SqliteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@zid", zoneId);
            using SqliteDataReader dr = cmd.ExecuteReader();
            if (dr.Read())
            {
                string path = ((dr["SpritePath"] != DBNull.Value) ? dr["SpritePath"].ToString().Trim() : "");
                Enums.EnemyType type = Enums.EnemyType.Boss;
                if (path.ToUpperInvariant().Contains("RANGED"))
                {
                    type = Enums.EnemyType.Ranged;
                }
                return new EnemyModel
                {
                    EnemyID = Convert.ToInt32(dr["EnemyID"]),
                    Name = dr["Name"]?.ToString(),
                    Level = ((dr["Level"] == DBNull.Value) ? 1 : Convert.ToInt32(dr["Level"])),
                    MaxHP = ((dr["MaxHP"] != DBNull.Value) ? Convert.ToInt32(dr["MaxHP"]) : 100),
                    CurrentHP = ((dr["MaxHP"] != DBNull.Value) ? Convert.ToInt32(dr["MaxHP"]) : 100),
                    MinDamage = ((dr["Damage"] != DBNull.Value) ? Convert.ToInt32(dr["Damage"]) : 10),
                    MaxDamage = ((dr["Damage"] != DBNull.Value) ? Convert.ToInt32(dr["Damage"]) : 10),
                    ExpReward = ((dr["ExpReward"] != DBNull.Value) ? Convert.ToInt32(dr["ExpReward"]) : 0),
                    GoldReward = ((dr["GoldReward"] != DBNull.Value) ? Convert.ToInt32(dr["GoldReward"]) : 0),
                    IsBoss = true,
                    Type = type,
                    SpritePath = path,
                    Speed = 3.5f,
                    Width = 60,
                    Height = 60
                };
            }
        }
        return null;
    }

    public List<EnemyModel> GetEnemiesByLevelRange(int minLvl, int maxLvl)
    {
        List<EnemyModel> list = new List<EnemyModel>();
        using (SqliteConnection conn = DatabaseHelper.GetConnection())
        {
            conn.Open();
            string sql = "SELECT EnemyID FROM Enemies WHERE Level BETWEEN @min AND @max AND (IsBoss =0 OR IsBoss IS NULL)";
            using SqliteCommand cmd = new SqliteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@min", minLvl);
            cmd.Parameters.AddWithValue("@max", maxLvl);
            using SqliteDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                list.Add(LoadEnemyById(Convert.ToInt32(dr["EnemyID"])));
            }
        }
        return list;
    }

    private EnemyModel LoadEnemyById(int enemyId)
    {
        using (SqliteConnection conn = DatabaseHelper.GetConnection())
        {
            conn.Open();
            string sql = "SELECT EnemyID, Name, Level, MaxHP, Damage, ExpReward, GoldReward, SpritePath, IsBoss FROM Enemies WHERE EnemyID = @eid";
            using SqliteCommand cmd = new SqliteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@eid", enemyId);
            using SqliteDataReader dr = cmd.ExecuteReader();
            if (dr.Read())
            {
                EnemyModel em = new EnemyModel();
                em.EnemyID = Convert.ToInt32(dr["EnemyID"]);
                em.Name = dr["Name"]?.ToString();
                em.Level = ((dr["Level"] == DBNull.Value) ? 1 : Convert.ToInt32(dr["Level"]));
                em.MaxHP = ((dr["MaxHP"] != DBNull.Value) ? Convert.ToInt32(dr["MaxHP"]) : 50);
                em.CurrentHP = em.MaxHP;
                em.MinDamage = ((dr["Damage"] != DBNull.Value) ? Convert.ToInt32(dr["Damage"]) : 5);
                em.MaxDamage = ((dr["Damage"] != DBNull.Value) ? Convert.ToInt32(dr["Damage"]) : 5);
                em.ExpReward = ((dr["ExpReward"] != DBNull.Value) ? Convert.ToInt32(dr["ExpReward"]) : 0);
                em.GoldReward = ((dr["GoldReward"] != DBNull.Value) ? Convert.ToInt32(dr["GoldReward"]) : 0);
                em.SpritePath = dr["SpritePath"]?.ToString();
                em.IsBoss = dr["IsBoss"] != DBNull.Value && Convert.ToBoolean(dr["IsBoss"]);
                em.Type = (em.IsBoss ? Enums.EnemyType.Boss : Enums.EnemyType.Melee);
                em.Speed = 1f;
                em.Width = 32;
                em.Height = 32;
                em.IsRanged = false;
                em.AttackRange = 50;
                if (!string.IsNullOrEmpty(em.Name))
                {
                    string lower = em.Name.ToLowerInvariant();
                    if (lower.Contains("arch") || lower.Contains("bow") || lower.Contains("ranged") || lower.Contains("shooter"))
                    {
                        em.Type = Enums.EnemyType.Ranged;
                        em.IsRanged = true;
                        em.AttackRange = 220;
                    }
                }
                return em;
            }
        }
        return null;
    }

    private EnemyModel GetAnyNonBossEnemy()
    {
        using (SqliteConnection conn = DatabaseHelper.GetConnection())
        {
            conn.Open();
            string sql = "SELECT EnemyID FROM Enemies WHERE IsBoss =0 OR IsBoss IS NULL LIMIT1";
            using SqliteCommand cmd = new SqliteCommand(sql, conn);
            object obj = cmd.ExecuteScalar();
            if (obj != null && obj != DBNull.Value)
            {
                return LoadEnemyById(Convert.ToInt32(obj));
            }
        }
        return null;
    }

    private EnemyModel GetAnyBossEnemy()
    {
        using (SqliteConnection conn = DatabaseHelper.GetConnection())
        {
            conn.Open();
            string sql = "SELECT EnemyID FROM Enemies WHERE IsBoss =1 LIMIT1";
            using SqliteCommand cmd = new SqliteCommand(sql, conn);
            object obj = cmd.ExecuteScalar();
            if (obj != null && obj != DBNull.Value)
            {
                return LoadEnemyById(Convert.ToInt32(obj));
            }
        }
        return null;
    }

    private static bool ColumnExists(SqliteConnection conn, string tableName, string columnName)
    {
        using SqliteCommand cmd = new SqliteCommand("PRAGMA table_info(" + tableName + ");", conn);
        using SqliteDataReader dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            string name = dr["name"]?.ToString();
            if (string.Equals(name, columnName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    public ZoneProgressDTO GetZoneProgressData(int charId, int zoneId)
    {
        ZoneProgressDTO result = new ZoneProgressDTO();
        using (SqliteConnection conn = DatabaseHelper.GetConnection())
        {
            conn.Open();
            bool hasBossEasy = ColumnExists(conn, "CharacterZoneData", "BossKilledEasy");
            bool hasBossNormal = ColumnExists(conn, "CharacterZoneData", "BossKilledNormal");
            bool hasBossHard = ColumnExists(conn, "CharacterZoneData", "BossKilledHard");
            string sql = $"SELECT ProgressEasy, ProgressNormal, ProgressHard,\r\nIFNULL({(hasBossEasy ? "BossKilledEasy" : "0")},0) AS BossKilledEasy,\r\nIFNULL({(hasBossNormal ? "BossKilledNormal" : "0")},0) AS BossKilledNormal,\r\nIFNULL({(hasBossHard ? "BossKilledHard" : "0")},0) AS BossKilledHard\r\nFROM CharacterZoneData WHERE CharacterID=@cid AND ZoneID=@zid";
            using SqliteCommand cmd = new SqliteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@cid", charId);
            cmd.Parameters.AddWithValue("@zid", zoneId);
            using SqliteDataReader dr = cmd.ExecuteReader();
            if (dr.Read())
            {
                result.ProgressEasy = ((dr["ProgressEasy"] != DBNull.Value) ? Convert.ToInt32(dr["ProgressEasy"]) : 0);
                result.ProgressNormal = ((dr["ProgressNormal"] != DBNull.Value) ? Convert.ToInt32(dr["ProgressNormal"]) : 0);
                result.ProgressHard = ((dr["ProgressHard"] != DBNull.Value) ? Convert.ToInt32(dr["ProgressHard"]) : 0);
                result.BossKilledEasy = dr["BossKilledEasy"] != DBNull.Value && Convert.ToInt32(dr["BossKilledEasy"]) != 0;
                result.BossKilledNormal = dr["BossKilledNormal"] != DBNull.Value && Convert.ToInt32(dr["BossKilledNormal"]) != 0;
                result.BossKilledHard = dr["BossKilledHard"] != DBNull.Value && Convert.ToInt32(dr["BossKilledHard"]) != 0;
            }
        }
        return result;
    }

    public void SaveZoneProgress(int charId, int zoneId, Enums.ZoneDifficulty diff, int newVal)
    {
        if (newVal > 100)
        {
            newVal = 100;
        }
        using SqliteConnection conn = DatabaseHelper.GetConnection();
        conn.Open();
        string checkSql = "SELECT COUNT(*) FROM CharacterZoneData WHERE CharacterID=@cid AND ZoneID=@zid";
        using SqliteCommand cmdCheck = new SqliteCommand(checkSql, conn);
        cmdCheck.Parameters.AddWithValue("@cid", charId);
        cmdCheck.Parameters.AddWithValue("@zid", zoneId);
        long count = (long)cmdCheck.ExecuteScalar();
        string colName = "ProgressEasy";
        if (diff == Enums.ZoneDifficulty.Normal)
        {
            colName = "ProgressNormal";
        }
        if (diff == Enums.ZoneDifficulty.Hard)
        {
            colName = "ProgressHard";
        }
        if (count == 0)
        {
            string insertSql = "INSERT INTO CharacterZoneData (CharacterID, ZoneID, " + colName + ") VALUES (@cid, @zid, @val)";
            using SqliteCommand cmd = new SqliteCommand(insertSql, conn);
            cmd.Parameters.AddWithValue("@cid", charId);
            cmd.Parameters.AddWithValue("@zid", zoneId);
            cmd.Parameters.AddWithValue("@val", newVal);
            cmd.ExecuteNonQuery();
            return;
        }
        string updateSql = "UPDATE CharacterZoneData SET " + colName + "=@val WHERE CharacterID=@cid AND ZoneID=@zid";
        using SqliteCommand cmd2 = new SqliteCommand(updateSql, conn);
        cmd2.Parameters.AddWithValue("@cid", charId);
        cmd2.Parameters.AddWithValue("@zid", zoneId);
        cmd2.Parameters.AddWithValue("@val", newVal);
        cmd2.ExecuteNonQuery();
    }

    public bool GetBossKilled(int charId, int zoneId, Enums.ZoneDifficulty diff)
    {
        ZoneProgressDTO dto = GetZoneProgressData(charId, zoneId);
        if (diff switch
        {
            Enums.ZoneDifficulty.Normal => dto.BossKilledNormal,
            Enums.ZoneDifficulty.Easy => dto.BossKilledEasy,
            _ => dto.BossKilledHard,
        })
        {
            return true;
        }
        return diff switch
        {
            Enums.ZoneDifficulty.Normal => dto.ProgressNormal,
            Enums.ZoneDifficulty.Easy => dto.ProgressEasy,
            _ => dto.ProgressHard,
        } == 99;
    }

    public void SetBossKilled(int charId, int zoneId, Enums.ZoneDifficulty diff, bool killed)
    {
        try
        {
            using SqliteConnection conn = DatabaseHelper.GetConnection();
            conn.Open();
            string colName = diff switch
            {
                Enums.ZoneDifficulty.Normal => "BossKilledNormal",
                Enums.ZoneDifficulty.Easy => "BossKilledEasy",
                _ => "BossKilledHard",
            };
            if (!ColumnExists(conn, "CharacterZoneData", colName))
            {
                using SqliteCommand cmdEnsure = new SqliteCommand("ALTER TABLE CharacterZoneData ADD COLUMN " + colName + " INTEGER NOT NULL DEFAULT0;", conn);
                cmdEnsure.ExecuteNonQuery();
            }
            string checkSql = "SELECT COUNT(*) FROM CharacterZoneData WHERE CharacterID=@cid AND ZoneID=@zid";
            using SqliteCommand cmdCheck = new SqliteCommand(checkSql, conn);
            cmdCheck.Parameters.AddWithValue("@cid", charId);
            cmdCheck.Parameters.AddWithValue("@zid", zoneId);
            long count = (long)cmdCheck.ExecuteScalar();
            if (count == 0)
            {
                string insertSql = "INSERT INTO CharacterZoneData (CharacterID, ZoneID, " + colName + ") VALUES (@cid, @zid, @val)";
                using SqliteCommand cmd = new SqliteCommand(insertSql, conn);
                cmd.Parameters.AddWithValue("@cid", charId);
                cmd.Parameters.AddWithValue("@zid", zoneId);
                cmd.Parameters.AddWithValue("@val", killed ? 1 : 0);
                cmd.ExecuteNonQuery();
                return;
            }
            string updateSql = "UPDATE CharacterZoneData SET " + colName + "=@val WHERE CharacterID=@cid AND ZoneID=@zid";
            using SqliteCommand cmd2 = new SqliteCommand(updateSql, conn);
            cmd2.Parameters.AddWithValue("@cid", charId);
            cmd2.Parameters.AddWithValue("@zid", zoneId);
            cmd2.Parameters.AddWithValue("@val", killed ? 1 : 0);
            cmd2.ExecuteNonQuery();
        }
        catch
        {
            if (killed)
            {
                SaveZoneProgress(charId, zoneId, diff, 99);
            }
        }
    }
}
