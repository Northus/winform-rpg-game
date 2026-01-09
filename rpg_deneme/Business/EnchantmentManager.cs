using System;
using System.Collections.Generic;
using System.Linq;
using rpg_deneme.Core;
using rpg_deneme.Data;
using rpg_deneme.Models;
using System.Windows.Forms; // For MessageBox if needed, or return bool/string result

namespace rpg_deneme.Business;

public class EnchantmentManager
{
    private readonly InventoryRepository _inventoryRepo;
    private readonly Random _random;

    public EnchantmentManager()
    {
        _inventoryRepo = new InventoryRepository();
        _random = new Random();
    }

    /// <summary>
    /// Kutsama Küresi uygular: %60 şansla yeni bir efsun ekler.
    /// </summary>
    public string ApplyBlessingMarble(ItemInstance equipment)
    {
        if (equipment.ItemType != Enums.ItemType.Weapon && equipment.ItemType != Enums.ItemType.Armor)
        {
            return "Kutsama Küresi sadece ekipmanlara uygulanabilir.";
        }

        if (equipment.Attributes.Count >= 5)
        {
            return "Bu eşyada zaten maksimum sayıda (5) efsun var.";
        }

        // %60 Şans
        if (_random.NextDouble() > 0.60)
        {
            return "Kutsama Küresi başarısız oldu.";
        }

        // Yeni efsun ekle
        var newAttr = GenerateRandomAttribute(equipment.Attributes);
        _inventoryRepo.AddAttribute(equipment.InstanceID, newAttr);
        equipment.Attributes.Add(newAttr);

        return "Kutsama Küresi başarıyla uygulandı! Yeni özellik eklendi.";
    }

    /// <summary>
    /// Efsun Nesnesi uygular: Mevcut efsunları değiştirir (sayı aynı kalır).
    /// </summary>
    public string ApplyEnchantItem(ItemInstance equipment)
    {
        if (equipment.ItemType != Enums.ItemType.Weapon && equipment.ItemType != Enums.ItemType.Armor)
        {
            return "Efsun Nesnesi sadece ekipmanlara uygulanabilir.";
        }

        if (equipment.Attributes.Count == 0)
        {
            return "Bu eşyada değiştirilecek efsun yok.";
        }

        int count = equipment.Attributes.Count;

        // Eski efsunları sil
        _inventoryRepo.ClearAttributes(equipment.InstanceID);
        equipment.Attributes.Clear();

        // Yeni efsunları ekle
        for (int i = 0; i < count; i++)
        {
            var newAttr = GenerateRandomAttribute(equipment.Attributes);
            _inventoryRepo.AddAttribute(equipment.InstanceID, newAttr);
            equipment.Attributes.Add(newAttr);
        }

        return "Efsunlar başarıyla değiştirildi.";
    }

    private ItemAttribute GenerateRandomAttribute(List<ItemAttribute> existingAttributes)
    {
        // Mevcut olmayan tipleri bul
        var allTypes = Enum.GetValues(typeof(Enums.ItemAttributeType))
                           .Cast<Enums.ItemAttributeType>()
                           .Where(t => t != Enums.ItemAttributeType.None)
                           .ToList();

        var existingTypes = existingAttributes.Select(a => a.AttributeType).ToList();
        var availableTypes = allTypes.Except(existingTypes).ToList();

        if (availableTypes.Count == 0)
        {
            // Teorik olarak 5 limit var, AttributeType sayısı > 5 olduğu sürece buraya düşmez.
            // Ama düşerse rastgele birini seçsin (duplicates allowed logic would be here, but we said unique types per item)
            availableTypes = allTypes;
        }

        var selectedType = availableTypes[_random.Next(availableTypes.Count)];
        int value = 0;

        switch (selectedType)
        {
            case Enums.ItemAttributeType.CriticalChance:
                value = _random.Next(1, 11); // %1 - %10
                break;
            case Enums.ItemAttributeType.AttackSpeed:
                value = _random.Next(1, 16); // %1 - %15
                break;
            case Enums.ItemAttributeType.ManaRegen:
                value = _random.Next(1, 11); // 1 - 10
                break;
            case Enums.ItemAttributeType.MaxHP:
                value = _random.Next(100, 1001); // +100 - +1000
                break;
            case Enums.ItemAttributeType.MaxHPPercent:
                value = _random.Next(1, 21); // %1 - %20
                break;
            case Enums.ItemAttributeType.MaxMana:
                value = _random.Next(50, 501); // +50 - +500
                break;
            case Enums.ItemAttributeType.MaxManaPercent:
                value = _random.Next(1, 21); // %1 - %20
                break;
            case Enums.ItemAttributeType.BlockChance:
                value = _random.Next(1, 11); // %1 - %10
                break;
            case Enums.ItemAttributeType.AttackValue:
                value = _random.Next(10, 51); // +10 - +50
                break;
            case Enums.ItemAttributeType.DefenseValue:
                value = _random.Next(10, 51); // +10 - +50
                break;
            case Enums.ItemAttributeType.MagicAttackValue:
                value = _random.Next(5, 31); // +5 - +30
                break;
        }

        return new ItemAttribute
        {
            AttributeType = selectedType,
            Value = value
        };
    }
}
