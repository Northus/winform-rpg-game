using rpg_deneme.Core;

namespace rpg_deneme.Models;

/// <summary>
/// Düşman verilerini temsil eden model sınıfı.
/// </summary>
public class EnemyModel : BattleEntity
{
    public int EnemyID { get; set; }
    public new string Name { get; set; }
    public int Level { get; set; }
    public int ExpReward { get; set; }
    public int GoldReward { get; set; }
    public bool IsBoss { get; set; }
    public string SpritePath { get; internal set; }
    public Enums.EnemyType Type { get; set; } = Enums.EnemyType.Melee;
}
