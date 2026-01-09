namespace rpg_deneme.Models;

/// <summary>
/// Karakter verilerini temsil eden model sınıfı.
/// </summary>
public class CharacterModel
{
    // Temel bilgiler
    public int CharacterID { get; set; }
    public int SlotIndex { get; set; }
    public string Name { get; set; }
    public byte Class { get; set; }
    public int Level { get; set; } = 1;

    // İstatistikler (Stats)
    public int STR { get; set; }
    public int DEX { get; set; }
    public int INT { get; set; }
    public int VIT { get; set; }
    public int StatPoints { get; set; }
    public int SkillPoints { get; set; }

    // Kaynaklar
    public long Gold { get; set; }
    public long Experience { get; set; }
    public int HP { get; internal set; }
    public int Mana { get; internal set; }

    // İlerleme bilgileri
    public int MaxSurvivalWave { get; set; } = 1;
    public int CurrentZoneID { get; set; } = 1;
    public int MaxUnlockedZoneID { get; set; } = 1;
    public int ZoneProgress { get; set; } = 0;
}
