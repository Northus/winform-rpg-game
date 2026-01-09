using System;
using rpg_deneme.Core;
using rpg_deneme.Data;
using rpg_deneme.Models;

namespace rpg_deneme.Business;

public class ConsumableManager
{
    private InventoryRepository _repo = new InventoryRepository();

    private CharacterRepository _charRepo = new CharacterRepository();

    public (bool Success, string Message) UseItem(CharacterModel hero, ItemInstance item)
    {
        if (item.RemainingCooldownSeconds > 0)
        {
            return (Success: false, Message: $"Henüz hazır değil! ({item.RemainingCooldownSeconds} sn)");
        }
        if (ApplyEffect(hero, item))
        {
            if (item.Cooldown > 0)
            {
                _repo.UpdateLastUsed(item.InstanceID);
                item.LastUsed = DateTime.Now; // Update memory for UI
            }
            //_charRepo.UpdateProgress(hero); // Removed to prevent stutter during combat
            _repo.ConsumeItem(item.InstanceID, 1);
            if (item.Count > 0) item.Count--; // Update memory for UI
            return (Success: true, Message: "Eşya kullanıldı.");
        }
        return (Success: false, Message: "Bu eşya şu an kullanılamaz.");
    }

    private bool ApplyEffect(CharacterModel hero, ItemInstance item)
    {
        // Calculate Total Max Stats using Equipment
        InventoryManager invManager = new InventoryManager();
        var inventory = invManager.GetInventory(hero.CharacterID);
        var equipment = inventory.FindAll(x => x.Location == Enums.ItemLocation.Equipment);

        switch (item.EffectType)
        {
            case Enums.ItemEffectType.RestoreHP:
                {
                    int maxHP = StatManager.CalculateTotalMaxHP(hero, equipment);
                    if (hero.HP >= maxHP)
                    {
                        return false;
                    }
                    hero.HP += item.EffectValue;
                    if (hero.HP > maxHP)
                    {
                        hero.HP = maxHP;
                    }
                    return true;
                }
            case Enums.ItemEffectType.RestoreMana:
                {
                    int maxMana = StatManager.CalculateTotalMaxMana(hero, equipment);
                    if (hero.Mana >= maxMana)
                    {
                        return false;
                    }
                    hero.Mana += item.EffectValue;
                    if (hero.Mana > maxMana)
                    {
                        hero.Mana = maxMana;
                    }
                    return true;
                }
            default:
                return false;
        }
    }
}
