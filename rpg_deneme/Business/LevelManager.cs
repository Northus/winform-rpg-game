using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using rpg_deneme.Core;
using rpg_deneme.Data;
using rpg_deneme.Models;

namespace rpg_deneme.Business;

/// <summary>
/// Karakterin seviye atlama ve tecrübe puanı işlemlerini yöneten sınıf.
/// </summary>
public class LevelManager
{
    private CharacterRepository _repo = new CharacterRepository();
    private InventoryManager _invManager = new InventoryManager();

    /// <summary>
    /// Belirli bir seviye için gereken tecrübe puanını hesaplar.
    /// </summary>
    public static int GetRequiredXp(int level)
    {
        return level * 100;
    }

    /// <summary>
    /// Karaktere tecrübe puanı ekler ve gerekirse seviye atlatır.
    /// </summary>
    public void AddExperience(CharacterModel hero, int amount)
    {
        hero.Experience += amount;
        bool leveledUp = false;

        // Gereken XP miktarını aşarsa seviye atlat
        while (hero.Experience >= GetRequiredXp(hero.Level))
        {
            hero.Experience -= GetRequiredXp(hero.Level);
            hero.Level++;
            hero.StatPoints += 3;
            hero.SkillPoints += 3;

            // Ekipman bonuslarını dahil ederek MaxHP/MaxMana hesapla
            var inventory = _invManager.GetInventory(hero.CharacterID);
            var equipment = inventory.Where(x => x.Location == Enums.ItemLocation.Equipment).ToList();

            // Seviye atlayınca can ve manayı doldur
            hero.HP = StatManager.CalculateTotalMaxHP(hero, equipment);
            hero.Mana = StatManager.CalculateTotalMaxMana(hero, equipment);
            leveledUp = true;
        }

        _repo.UpdateCharacterStats(hero);

        if (leveledUp)
        {
            MessageBox.Show($"TEBRİKLER! SEVİYE ATLADIN! \nYeni Seviye: {hero.Level}\nKazanılan Stat Puanı: +3\nKazanılan Yetenek Puanı: +3");
        }
    }

    public static void CheckLevelUp(CharacterModel hero)
    {
        LevelManager lm = new LevelManager();
        // Since we already added Exp directly in UcArena, we just call AddExperience with 0 to trigger check
        // Or better, UcArena should call AddExperience instead of modifying .Experience directly.
        // But for minimal change:
        lm.AddExperience(hero, 0);
    }
}
