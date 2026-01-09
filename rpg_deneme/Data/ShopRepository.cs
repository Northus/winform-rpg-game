using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using rpg_deneme.Core;
using rpg_deneme.Models;

namespace rpg_deneme.Data;

public class ShopRepository
{
	public List<ItemInstance> GetShopItems(int shopId)
	{
		List<ItemInstance> list = new List<ItemInstance>();
		using (SqliteConnection conn = DatabaseHelper.GetConnection())
		{
			conn.Open();
			string sql = "\r\n                SELECT \r\n                    S.Price AS ShopPrice, -- marketteki fiyat\r\n                    T.TemplateID, T.Name, T.ItemType, T.IsStackable, T.MaxStack, \r\n                    T.BaseMinDamage, T.BaseDefense, T.EffectType, T.EffectValue,\r\n                    T.SellPrice\r\n                FROM ShopItems S\r\n                INNER JOIN ItemTemplates T ON S.TemplateID = T.TemplateID\r\n                WHERE S.ShopID = @sid";
			using SqliteCommand cmd = new SqliteCommand(sql, conn);
			cmd.Parameters.AddWithValue("@sid", shopId);
			using SqliteDataReader dr = cmd.ExecuteReader();
			while (dr.Read())
			{
				Enums.ItemType itemType = (Enums.ItemType)Convert.ToByte(dr["ItemType"]);
				list.Add(new ItemInstance
				{
					InstanceID = 0L,
					TemplateID = Convert.ToInt32(dr["TemplateID"]),
					Name = dr["Name"]?.ToString(),
					ItemType = itemType,
					IsStackable = (dr["IsStackable"] != DBNull.Value && Convert.ToBoolean(dr["IsStackable"])),
					MaxStack = ((dr["MaxStack"] == DBNull.Value) ? 1 : Convert.ToInt32(dr["MaxStack"])),
					BuyPrice = Convert.ToInt32(dr["ShopPrice"]),
					EffectValue = ((dr["EffectValue"] != DBNull.Value) ? Convert.ToInt32(dr["EffectValue"]) : 0),
					EffectType = (Enums.ItemEffectType)((dr["EffectType"] != DBNull.Value) ? Convert.ToByte(dr["EffectType"]) : 0),
					Grade = ((itemType == Enums.ItemType.Weapon || itemType == Enums.ItemType.Armor) ? Enums.ItemGrade.Common : Enums.ItemGrade.Others),
					UpgradeLevel = 0,
					Count = 1
				});
			}
		}
		return list;
	}

	public int GetShopItemPrice(int shopId, int templateId)
	{
		using SqliteConnection conn = DatabaseHelper.GetConnection();
		conn.Open();
		string sql = "SELECT Price FROM ShopItems WHERE ShopID = @sid AND TemplateID = @tid";
		using SqliteCommand cmd = new SqliteCommand(sql, conn);
		cmd.Parameters.AddWithValue("@sid", shopId);
		cmd.Parameters.AddWithValue("@tid", templateId);
		object result = cmd.ExecuteScalar();
		return (result != null) ? Convert.ToInt32(result) : (-1);
	}

	public int GetGlobalSellPrice(int templateId)
	{
		using SqliteConnection conn = DatabaseHelper.GetConnection();
		conn.Open();
		string sql = "SELECT SellPrice FROM ItemTemplates WHERE TemplateID = @tid";
		using SqliteCommand cmd = new SqliteCommand(sql, conn);
		cmd.Parameters.AddWithValue("@tid", templateId);
		object result = cmd.ExecuteScalar();
		return (result != null && result != DBNull.Value) ? Convert.ToInt32(result) : 0;
	}

	public bool UpdateGold(int charId, long newGoldAmount)
	{
		using SqliteConnection conn = DatabaseHelper.GetConnection();
		conn.Open();
		string sql = "UPDATE Characters SET Gold = @gold WHERE CharacterID = @id";
		using SqliteCommand cmd = new SqliteCommand(sql, conn);
		cmd.Parameters.AddWithValue("@gold", newGoldAmount);
		cmd.Parameters.AddWithValue("@id", charId);
		return cmd.ExecuteNonQuery() > 0;
	}
}
