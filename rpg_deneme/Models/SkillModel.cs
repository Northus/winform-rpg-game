using rpg_deneme.Core;
using System;
using System.Collections.Generic;

namespace rpg_deneme.Models;

[Serializable]
public class SkillModel
{
    public int SkillID { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public Enums.CharacterClass Class { get; set; }
    public Enums.SkillType Type { get; set; }

    // Limits
    public int MaxLevel { get; set; } = 5;
    public int RequiredLevel { get; set; } = 1;

    // Cooldown & Cost
    public double Cooldown { get; set; }
    public int ManaCost { get; set; }

    // Effect
    public Enums.SkilEffectType EffectType { get; set; }
    public double BaseEffectValue { get; set; }
    public double EffectScaling { get; set; }
    public double Duration { get; set; }

    // Pasif için hangi stat'ı etkiliyor
    public Enums.PassiveStatType PassiveStatType { get; set; } = Enums.PassiveStatType.None;

    // Aktif için özel efekt (yanma, yavaşlatma vb.)
    public Enums.SkillSecondaryEffect SecondaryEffect { get; set; } = Enums.SkillSecondaryEffect.None;
    public double SecondaryEffectValue { get; set; } // Efektin gücü (hasar/yavaşlatma yüzdesi)
    public double SecondaryEffectDuration { get; set; } // Efekt süresi (saniye)

    // Visual
    public string IconPath { get; set; }

    // Tree Layout
    public int Row { get; set; }
    public int Col { get; set; }

    // Runtime helpers
    public int CurrentLevel { get; set; } = 0;
    public DateTime LastCastTime { get; set; } = DateTime.MinValue;

    public double RemainingCooldown
    {
        get
        {
            var diff = (DateTime.Now - LastCastTime).TotalSeconds;
            if (diff >= Cooldown) return 0;
            return Cooldown - diff;
        }
    }

    public List<int> PrerequisiteSkillIDs { get; set; } = new List<int>();

    public double GetCurrentEffectValue()
    {
        return BaseEffectValue + (EffectScaling * CurrentLevel);
    }

    // Skill element tipi (projectile çizimi için)
    public SkillElement Element
    {
        get
        {
            string n = Name.ToLowerInvariant();
            if (n.Contains("ateş") || n.Contains("fire") || n.Contains("meteor") || n.Contains("alev") || n.Contains("yanma"))
                return SkillElement.Fire;
            if (n.Contains("buz") || n.Contains("ice") || n.Contains("frost") || n.Contains("don"))
                return SkillElement.Ice;
            if (n.Contains("yıldırım") || n.Contains("lightning") || n.Contains("şok") || n.Contains("elektrik") || n.Contains("thunder"))
                return SkillElement.Lightning;
            if (n.Contains("zehir") || n.Contains("poison") || n.Contains("toxic"))
                return SkillElement.Poison;
            if (n.Contains("gölge") || n.Contains("shadow") || n.Contains("karanlık") || n.Contains("dark"))
                return SkillElement.Dark;
            return SkillElement.Physical;
        }
    }
}

public enum SkillElement
{
    Physical,
    Fire,
    Ice,
    Lightning,
    Poison,
    Dark
}
