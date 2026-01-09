namespace rpg_deneme.Models;

public class ZoneModel
{
	public int ZoneID { get; set; }

	public string Name { get; set; }

	public string Description { get; set; }

	public int MinLevel { get; set; }

	public int OrderIndex { get; set; }

	public bool IsUnlocked { get; set; }
}
