using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using rpg_deneme.Core;
using rpg_deneme.Models;

namespace rpg_deneme.Data;

public class LootRepository
{
	public List<LootTableModel> GetLootTableForEnemy(int enemyId)
	{
		List<LootTableModel> list = new List<LootTableModel>();
		using (SqliteConnection conn = DatabaseHelper.GetConnection())
		{
			conn.Open();
			string sql = "\r\n                    SELECT L.LootID, L.EnemyID, L.TemplateID, L.DropRate, L.MinLevel,\r\n                           T.ItemType, T.IsStackable, T.MaxStack\r\n                    FROM LootTables L\r\n                    INNER JOIN ItemTemplates T ON L.TemplateID = T.TemplateID\r\n                    WHERE L.EnemyID = @eid";
			using SqliteCommand cmd = new SqliteCommand(sql, conn);
			cmd.Parameters.AddWithValue("@eid", enemyId);
			using SqliteDataReader dr = cmd.ExecuteReader();
			while (dr.Read())
			{
				list.Add(new LootTableModel
				{
					LootID = Convert.ToInt32(dr["LootID"]),
					EnemyID = Convert.ToInt32(dr["EnemyID"]),
					TemplateID = Convert.ToInt32(dr["TemplateID"]),
					DropRate = Convert.ToDouble(dr["DropRate"]),
					ItemType = (Enums.ItemType)Convert.ToByte(dr["ItemType"]),
					IsStackable = (dr["IsStackable"] != DBNull.Value && Convert.ToBoolean(dr["IsStackable"])),
					MaxStack = ((dr["MaxStack"] == DBNull.Value) ? 1 : Convert.ToInt32(dr["MaxStack"]))
				});
			}
		}
		return list;
	}
}
