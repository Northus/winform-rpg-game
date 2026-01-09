using System.Drawing;
using rpg_deneme.Core;

namespace rpg_deneme.Models;

public class NpcEntity : BattleEntity
{
	public new string Name { get; set; }

	public Enums.NpcType Type { get; set; }

	public Color Color { get; set; }

	public NpcEntity(string name, Enums.NpcType type, int x, int y)
	{
		Name = name;
		Type = type;
		base.X = x;
		base.Y = y;
		base.Width = 40;
		base.Height = 40;
		base.CurrentHP = 1;
		base.MaxHP = 1;
		switch (type)
		{
		case Enums.NpcType.Merchant:
			Color = Color.Gold;
			break;
		case Enums.NpcType.Teleporter:
			Color = Color.Cyan;
			break;
		default:
			Color = Color.Gray;
			break;
		}
	}
}
