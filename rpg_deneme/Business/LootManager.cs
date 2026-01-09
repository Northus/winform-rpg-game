using System;
using System.Collections.Generic;
using rpg_deneme.Core;
using rpg_deneme.Data;
using rpg_deneme.Models;

namespace rpg_deneme.Business;

public class LootManager
{
	private LootRepository _repo = new LootRepository();

	private InventoryManager _invManager = new InventoryManager();

	private Random _rnd = new Random();

	public List<string> ProcessLoot(int characterId, int enemyId)
	{
		List<string> droppedItemsLog = new List<string>();
		List<LootTableModel> lootTable = _repo.GetLootTableForEnemy(enemyId);
		foreach (LootTableModel entry in lootTable)
		{
			double roll = _rnd.NextDouble() * 100.0;
			if (roll <= entry.DropRate)
			{
				Enums.ItemGrade grade = Enums.ItemGrade.Others;
				int upgradeLevel = 0;
				if (entry.ItemType != Enums.ItemType.Consumable)
				{
					grade = CalculateGrade();
					upgradeLevel = CalculateUpgradeLevel();
				}
				ItemInstance newItem = new ItemInstance
				{
					TemplateID = entry.TemplateID,
					OwnerID = characterId,
					Count = 1,
					Grade = grade,
					UpgradeLevel = upgradeLevel,
					Location = Enums.ItemLocation.Inventory,
					ItemType = entry.ItemType,
					IsStackable = entry.IsStackable,
					MaxStack = entry.MaxStack
				};
				if (_invManager.AddItemToInventory(newItem))
				{
					string qualityText = ((grade == Enums.ItemGrade.Others || grade == Enums.ItemGrade.Common) ? "" : $"[{grade}] ");
					string upgradeText = ((upgradeLevel > 0) ? $"+{upgradeLevel} " : "");
					droppedItemsLog.Add($"{qualityText}{upgradeText}Eşya (ID:{entry.TemplateID})");
				}
				else
				{
					droppedItemsLog.Add("Çanta dolu! Eşya kayboldu.");
				}
			}
		}
		return droppedItemsLog;
	}

	private Enums.ItemGrade CalculateGrade()
	{
		int roll = _rnd.Next(1, 101);
		if (roll <= 10)
		{
			return Enums.ItemGrade.Legendary;
		}
		if (roll <= 20)
		{
			return Enums.ItemGrade.Epic;
		}
		if (roll <= 40)
		{
			return Enums.ItemGrade.Rare;
		}
		return Enums.ItemGrade.Common;
	}

	private int CalculateUpgradeLevel()
	{
		int roll = _rnd.Next(1, 101);
		if (roll <= 5)
		{
			return 4;
		}
		if (roll <= 10)
		{
			return 3;
		}
		if (roll <= 20)
		{
			return 2;
		}
		if (roll <= 40)
		{
			return 1;
		}
		return 0;
	}
}
