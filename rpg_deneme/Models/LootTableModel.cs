using rpg_deneme.Core;

namespace rpg_deneme.Models;

public class LootTableModel
{
	public int LootID { get; set; }

	public int EnemyID { get; set; }

	public int TemplateID { get; set; }

	public double DropRate { get; set; }

	public int MinLevel { get; set; }

	public Enums.ItemType ItemType { get; set; }

	public bool IsStackable { get; set; }

	public int MaxStack { get; set; }
}
