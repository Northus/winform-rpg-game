using System;
using System.Collections.Generic;
using System.Linq;
using rpg_deneme.Core;
using rpg_deneme.Models;

namespace rpg_deneme.Business;

/// <summary>
/// Karakter istatistiklerini (HP, Mana, Defans, Hasar vb.) hesaplayan yardımcı sınıf.
/// </summary>
public static class StatManager
{
    /// <summary>
    /// Pasif skill bonuslarını hesaplar.
    /// </summary>
    public static PassiveBonus CalculatePassiveBonuses(List<SkillModel> skills)
    {
        if (skills == null) return new PassiveBonus();

        var skillManager = new SkillManager();
        return skillManager.CalculatePassiveBonuses(skills);
    }

    /// <summary>
    /// Maksimum can puanını hesaplar.
    /// </summary>
    public static int CalculateMaxHP(int vit, int level, List<SkillModel> skills = null)
    {
        int val = 100 + vit * 20 + level * 10;
        if (skills != null)
        {
            var bonus = CalculatePassiveBonuses(skills);
            val += bonus.MaxHPBonus;
        }
        return val;
    }

    /// <summary>
    /// Maksimum mana puanını hesaplar.
    /// </summary>
    public static int CalculateMaxMana(int intel, int level, List<SkillModel> skills = null)
    {
        int val = 50 + intel * 15 + level * 5;
        if (skills != null)
        {
            var bonus = CalculatePassiveBonuses(skills);
            val += bonus.MaxManaBonus;
        }
        return val;
    }

    /// <summary>
    /// Karakterin toplam defansını hesaplar (Statlar + Ekipman + Skill Bonusu).
    /// </summary>
    public static int CalculateTotalDefense(CharacterModel hero, List<ItemInstance> equipment, List<SkillModel> skills = null)
    {
        int naturalDef = hero.VIT / 2 + hero.Level;
        float totalArmorDef = 0f;

        if (equipment != null)
        {
            var armors = equipment.Where(x => x.ItemType == Enums.ItemType.Armor);
            foreach (ItemInstance item in armors)
            {
                float baseDef = item.BaseDefense;
                float gradeMult = GetGradeMultiplier(item.Grade);
                float upgradeMult = GetUpgradeMultiplier(item.UpgradeLevel);
                totalArmorDef += baseDef * gradeMult * upgradeMult;

                if (item.Attributes != null)
                {
                    var defAttr = item.Attributes.FirstOrDefault(a => a.AttributeType == Enums.ItemAttributeType.DefenseValue);
                    if (defAttr != null) totalArmorDef += defAttr.Value;
                }
            }
        }

        int total = naturalDef + (int)totalArmorDef;

        // Pasif skill bonusları
        if (skills != null)
        {
            var bonus = CalculatePassiveBonuses(skills);
            total += bonus.DefenseBonus;
        }

        return total;
    }

    /// <summary>
    /// Fiziksel hasar miktarını hesaplar (Min, Max).
    /// </summary>
    public static (int Min, int Max) CalculatePhysicalDamage(CharacterModel hero, ItemInstance weapon = null, List<SkillModel> skills = null)
    {
        float baseMin = 5f;
        float baseMax = 5f;
        float combinedMult = 1f;
        int attrDamage = 0;

        if (weapon != null)
        {
            baseMin = (float)weapon.MinDamage;
            baseMax = (float)weapon.MaxDamage;
            float gradeMult = GetGradeMultiplier(weapon.Grade);
            float upgradeMult = GetUpgradeMultiplier(weapon.UpgradeLevel);
            combinedMult = gradeMult * upgradeMult;

            if (weapon.Attributes != null)
            {
                var attAttr = weapon.Attributes.FirstOrDefault(a => a.AttributeType == Enums.ItemAttributeType.AttackValue);
                if (attAttr != null) attrDamage = attAttr.Value;
            }
        }

        float statBonus = hero.Class switch
        {
            1 => hero.STR * 4f,                     // Warrior
            2 => hero.STR * 2f + hero.DEX * 2.5f,   // Rogue
            3 => hero.STR * 0.5f,                   // Mage
            _ => 0f,
        };

        int totalMin = (int)(baseMin * combinedMult + statBonus) + attrDamage;
        int totalMax = (int)(baseMax * combinedMult + statBonus) + attrDamage;

        // Pasif skill bonusları
        if (skills != null)
        {
            var bonus = CalculatePassiveBonuses(skills);
            totalMin += bonus.AttackBonus;
            totalMax += bonus.AttackBonus;
        }

        return (totalMin, totalMax);
    }

    /// <summary>
    /// Büyü hasar miktarını hesaplar (Min, Max).
    /// </summary>
    public static (int Min, int Max) CalculateMagicalDamage(CharacterModel hero, ItemInstance weapon = null, List<SkillModel> skills = null)
    {
        float baseMinMagic = 0f;
        float baseMaxMagic = 0f;
        float combinedMult = 1f;

        if (weapon != null)
        {
            baseMinMagic = (float)weapon.MinMagicDamage;
            baseMaxMagic = (float)weapon.MaxMagicDamage;

            if (baseMinMagic > 0f || baseMaxMagic > 0f)
            {
                float gradeMult = GetGradeMultiplier(weapon.Grade);
                float upgradeMult = GetUpgradeMultiplier(weapon.UpgradeLevel);
                combinedMult = gradeMult * upgradeMult;
            }

            if (weapon.Attributes != null)
            {
                var attAttr = weapon.Attributes.FirstOrDefault(a => a.AttributeType == Enums.ItemAttributeType.MagicAttackValue);
                if (attAttr != null)
                {
                    baseMinMagic += attAttr.Value;
                    baseMaxMagic += attAttr.Value;
                }
            }
        }

        float statBonus = (hero.Class == 3) ? (hero.INT * 4f) : (hero.INT * 0.5f);

        int totalMin = (int)(baseMinMagic * combinedMult + statBonus);
        int totalMax = (int)(baseMaxMagic * combinedMult + statBonus);

        // Pasif skill bonusları
        if (skills != null)
        {
            var bonus = CalculatePassiveBonuses(skills);
            totalMin += bonus.MagicAttackBonus;
            totalMax += bonus.MagicAttackBonus;
        }

        return (totalMin, totalMax);
    }

    /// <summary>
    /// Saldırı hızını hesaplar (ms cinsinden delay, düşük = hızlı).
    /// </summary>
    public static int CalculateAttackDelay(CharacterModel hero, List<ItemInstance> equipment = null, List<SkillModel> skills = null)
    {
        int baseDelay = hero.Class switch
        {
            1 => 700,  // Warrior - was 500
            2 => 480,  // Rogue - was 350
            3 => 850,  // Mage - was 600
            _ => 700
        };

        // Calculate Base Duration in ms
        float baseMs = (float)baseDelay;
        if (baseMs < 100) baseMs = 100;

        // Calculate Speed Multiplier (100% is normal)
        // DEX adds ~0.5% per point? Or stick to flat reduction for DEX?
        // Let's keep DEX flat reduction for now as "Optimization" is tricky if we change too much logic at once.
        // Actually, user specifically asked for percentage from passive.

        float dexBonusSpeed = hero.DEX * 0.5f; // % speed increase from DEX
        float equipBonusSpeed = 0f;
        float passiveBonusSpeed = 0f;

        // Equipment bonus
        if (equipment != null)
        {
            equipBonusSpeed = (float)GetTotalAttributeValue(equipment, Enums.ItemAttributeType.AttackSpeed);
        }

        // Passive skill bonus
        if (skills != null)
        {
            var bonus = CalculatePassiveBonuses(skills);
            passiveBonusSpeed = (float)bonus.AttackSpeedBonus;
        }

        float totalSpeedBonusPercent = dexBonusSpeed + equipBonusSpeed + passiveBonusSpeed;

        // Formula: NewDelay = BaseDelay / (1 + Bonus/100)
        // Example: 500ms base, +100% speed => 500 / 2 = 250ms

        float finalDelay = baseMs / (1f + (totalSpeedBonusPercent / 100f));

        return Math.Max(200, (int)finalDelay);
    }

    /// <summary>
    /// Kritik şansını hesaplar.
    /// </summary>
    public static int CalculateCritChance(CharacterModel hero, List<ItemInstance> equipment = null, List<SkillModel> skills = null)
    {
        int baseCrit = hero.DEX / 5 + (hero.Class == 2 ? 5 : 0); // Rogue +5 base

        if (equipment != null)
        {
            baseCrit += GetTotalAttributeValue(equipment, Enums.ItemAttributeType.CriticalChance);
        }

        if (skills != null)
        {
            var bonus = CalculatePassiveBonuses(skills);
            baseCrit += bonus.CritChanceBonus;
        }

        return Math.Min(100, baseCrit);
    }

    /// <summary>
    /// Element hasarı bonusunu hesaplar.
    /// </summary>
    public static int CalculateElementBonus(List<SkillModel> skills, SkillElement element)
    {
        if (skills == null) return 0;
        var bonus = CalculatePassiveBonuses(skills);

        return element switch
        {
            SkillElement.Fire => bonus.FireDamageBonus,
            SkillElement.Ice => bonus.IceDamageBonus,
            SkillElement.Lightning => bonus.LightningDamageBonus,
            SkillElement.Poison => bonus.PoisonDamageBonus,
            _ => 0
        };
    }

    /// <summary>
    /// Hareket hızı bonusunu hesaplar.
    /// </summary>
    public static int CalculateMovementSpeedBonus(List<SkillModel> skills)
    {
        if (skills == null) return 0;
        var bonus = CalculatePassiveBonuses(skills);
        return bonus.MovementSpeedBonus;
    }

    /// <summary>
    /// Can çalma yüzdesini hesaplar.
    /// </summary>
    public static int CalculateLifeStealPercent(List<SkillModel> skills)
    {
        if (skills == null) return 0;
        var bonus = CalculatePassiveBonuses(skills);
        return bonus.LifeStealPercent;
    }

    /// <summary>
    /// Her saldırının mana maliyetini hesaplar.
    /// </summary>
    public static int CalculateAttackManaCost(CharacterModel hero)
    {
        return hero.Class switch
        {
            3 => 8,
            2 => 3,
            1 => 2,
            _ => 1,
        };
    }

    /// <summary>
    /// Eşya kalitesine (Grade) göre çarpan döner.
    /// </summary>
    public static float GetGradeMultiplier(Enums.ItemGrade grade)
    {
        return grade switch
        {
            Enums.ItemGrade.Common => 1f,
            Enums.ItemGrade.Rare => 1.25f,
            Enums.ItemGrade.Epic => 1.5f,
            Enums.ItemGrade.Legendary => 2f,
            _ => 1f,
        };
    }

    /// <summary>
    /// Eşya artı basma seviyesine (+1, +2...) göre çarpan döner.
    /// </summary>
    public static float GetUpgradeMultiplier(int level)
    {
        return 1f + (float)level * 0.1f;
    }

    /// <summary>
    /// Toplam MaxHP hesaplar (Stat + Equipment + Bonus).
    /// </summary>
    public static int CalculateTotalMaxHP(CharacterModel hero, List<ItemInstance> equipment, List<SkillModel> skills = null)
    {
        int baseMax = CalculateMaxHP(hero.VIT, hero.Level, skills);
        if (equipment == null) return baseMax;

        int flatBonus = 0;
        int percentBonus = 0;

        foreach (var item in equipment)
        {
            if (item.Attributes == null) continue;
            foreach (var attr in item.Attributes)
            {
                if (attr.AttributeType == Enums.ItemAttributeType.MaxHP) flatBonus += attr.Value;
                else if (attr.AttributeType == Enums.ItemAttributeType.MaxHPPercent) percentBonus += attr.Value;
            }
        }

        int total = baseMax + flatBonus;
        if (percentBonus > 0)
        {
            total += (int)(baseMax * (percentBonus / 100f));
        }
        return total;
    }

    /// <summary>
    /// Toplam MaxMana hesaplar (Stat + Equipment + Bonus).
    /// </summary>
    public static int CalculateTotalMaxMana(CharacterModel hero, List<ItemInstance> equipment, List<SkillModel> skills = null)
    {
        int baseMax = CalculateMaxMana(hero.INT, hero.Level, skills);
        if (equipment == null) return baseMax;

        int flatBonus = 0;
        int percentBonus = 0;

        foreach (var item in equipment)
        {
            if (item.Attributes == null) continue;
            foreach (var attr in item.Attributes)
            {
                if (attr.AttributeType == Enums.ItemAttributeType.MaxMana) flatBonus += attr.Value;
                else if (attr.AttributeType == Enums.ItemAttributeType.MaxManaPercent) percentBonus += attr.Value;
            }
        }

        int total = baseMax + flatBonus;
        if (percentBonus > 0)
        {
            total += (int)(baseMax * (percentBonus / 100f));
        }
        return total;
    }

    /// <summary>
    /// Belirli bir özelliğin toplam değerini döner (örn. Kritik Şans).
    /// </summary>
    public static int GetTotalAttributeValue(List<ItemInstance> equipment, Enums.ItemAttributeType type)
    {
        if (equipment == null) return 0;
        int total = 0;
        foreach (var item in equipment)
        {
            if (item.Attributes == null) continue;
            var attr = item.Attributes.FirstOrDefault(a => a.AttributeType == type);
            if (attr != null) total += attr.Value;
        }
        return total;
    }

    // Eski uyumluluk için
    // Eski uyumluluk için
    public static float CalculateAttackSpeed(CharacterModel hero, ItemInstance weapon = null, List<ItemInstance> equipment = null, List<SkillModel> skills = null)
    {
        int delay = CalculateAttackDelay(hero, equipment, skills);
        return 1000f / delay; // Saniyede vuruş
    }
}
