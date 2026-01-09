using System;
using System.Linq;
using Microsoft.Data.Sqlite;
using rpg_deneme.Core;
using rpg_deneme.Data;
using rpg_deneme.Models;

namespace rpg_deneme.Business;

public class UpgradeManager
{
	private readonly InventoryRepository _repo = new InventoryRepository();

	private readonly Random _rnd = new Random();

	public const int UPGRADE_MATERIAL_ID = 7;

	private readonly int[] _luckyItemIds = new int[4] { 8, 9, 10, 11 };

	public int GetRequiredMaterialCount(int currentLevel)
	{
		return 5 * (int)Math.Pow(2.0, currentLevel);
	}

	public int GetBaseSuccessRate(int currentLevel)
	{
		int chance = 100 - currentLevel * 10;
		return Math.Max(10, chance);
	}

	public int GetLuckyItemBonus(int templateId)
	{
		if (1 == 0)
		{
		}
		int result = templateId switch
		{
			11 => 10, 
			12 => 30, 
			13 => 50, 
			14 => 100, 
			_ => 0, 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	public bool IsLuckyItem(int templateId)
	{
		return _luckyItemIds.Contains(templateId);
	}

	public bool IsUpgradeable(ItemInstance item)
	{
		return (item.ItemType == Enums.ItemType.Weapon || item.ItemType == Enums.ItemType.Armor) && item.UpgradeLevel < 9;
	}

	public int PerformUpgrade(ItemInstance mainItem, ItemInstance materialItem, ItemInstance luckyItem)
	{
		int reqCount = GetRequiredMaterialCount(mainItem.UpgradeLevel);
		if (materialItem == null || materialItem.TemplateID != 7 || materialItem.Count < reqCount)
		{
			return -1;
		}
		int baseChance = GetBaseSuccessRate(mainItem.UpgradeLevel);
		int bonusChance = 0;
		if (luckyItem != null)
		{
			bonusChance = ((luckyItem.EffectValue > 0) ? luckyItem.EffectValue : GetLuckyItemBonus(luckyItem.TemplateID));
		}
		int totalChance = baseChance + bonusChance;
		_repo.ConsumeItem(materialItem.InstanceID, reqCount);
		if (luckyItem != null)
		{
			_repo.ConsumeItem(luckyItem.InstanceID, 1);
		}
		int roll = _rnd.Next(1, 101);
		if (roll <= totalChance)
		{
			UpdateUpgradeLevel(mainItem.InstanceID, mainItem.UpgradeLevel + 1);
			return 1;
		}
		_repo.ConsumeItem(mainItem.InstanceID, mainItem.Count);
		return 0;
	}

	private void UpdateUpgradeLevel(long instanceId, int newLevel)
	{
		using SqliteConnection conn = DatabaseHelper.GetConnection();
		conn.Open();
		using SqliteCommand cmd = new SqliteCommand("UPDATE ItemInstances SET UpgradeLevel = @lvl WHERE InstanceID = @id", conn);
		cmd.Parameters.AddWithValue("@lvl", newLevel);
		cmd.Parameters.AddWithValue("@id", instanceId);
		cmd.ExecuteNonQuery();
	}
}
