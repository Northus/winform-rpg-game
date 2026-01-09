using rpg_deneme.Core;
using rpg_deneme.Data;
using rpg_deneme.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace rpg_deneme.Business;

public class SkillManager
{
    /// <summary>
    /// Loads all skills for a specific class from the database.
    /// Also populates CurrentLevel for the given character.
    /// </summary>
    public List<SkillModel> LoadSkillsForClass(Enums.CharacterClass charClass, int characterId)
    {
        SkillRepository repo = new SkillRepository();
        return repo.GetSkillsByClass(charClass, characterId);
    }

    public (bool Success, string Message) CanLearnSkill(CharacterModel character, SkillModel skill, List<SkillModel> allSkills)
    {
        if (character.SkillPoints <= 0) return (false, "Yetersiz Yetenek Puanı!");

        if (skill.CurrentLevel >= skill.MaxLevel) return (false, "Yetenek zaten maksimum seviyede!");

        if (character.Level < skill.RequiredLevel) return (false, $"Gereken Karakter Seviyesi: {skill.RequiredLevel}");

        foreach (var reqId in skill.PrerequisiteSkillIDs)
        {
            var reqSkill = allSkills.FirstOrDefault(s => s.SkillID == reqId);
            if (reqSkill == null || reqSkill.CurrentLevel < 1)
            {
                var reqName = reqSkill?.Name ?? "Bilinmeyen Yetenek";
                return (false, $"Öncelikle '{reqName}' yeteneğini öğrenmelisin!");
            }
        }

        return (true, "Öğrenilebilir");
    }

    public void LearnSkill(CharacterModel character, SkillModel skill)
    {
        skill.CurrentLevel++;
        SkillRepository repo = new SkillRepository();
        repo.SaveCharacterSkill(character.CharacterID, skill.SkillID, skill.CurrentLevel);
    }

    public void ResetSkills(CharacterModel character)
    {
        SkillRepository repo = new SkillRepository();
        repo.ResetCharacterSkills(character.CharacterID);
    }

    /// <summary>
    /// Tüm öğrenilmiş pasif skill'lerin etkilerini karaktere uygular.
    /// Bu metod karakter yüklendiğinde ve skill öğrenildiğinde çağrılmalı.
    /// </summary>
    public PassiveBonus CalculatePassiveBonuses(List<SkillModel> learnedSkills)
    {
        PassiveBonus bonus = new PassiveBonus();

        foreach (var skill in learnedSkills)
        {
            if (skill.CurrentLevel > 0 && skill.Type == Enums.SkillType.Passive)
            {
                double value = skill.GetCurrentEffectValue();
                ApplyPassiveToBonus(bonus, skill, value);
            }
        }

        return bonus;
    }

    private void ApplyPassiveToBonus(PassiveBonus bonus, SkillModel skill, double value)
    {
        switch (skill.PassiveStatType)
        {
            case Enums.PassiveStatType.Defense:
                bonus.DefenseBonus += (int)value;
                break;
            case Enums.PassiveStatType.Attack:
                bonus.AttackBonus += (int)value;
                break;
            case Enums.PassiveStatType.MagicAttack:
                bonus.MagicAttackBonus += (int)value;
                break;
            case Enums.PassiveStatType.MaxHP:
                bonus.MaxHPBonus += (int)value;
                break;
            case Enums.PassiveStatType.MaxMana:
                bonus.MaxManaBonus += (int)value;
                break;
            case Enums.PassiveStatType.CriticalChance:
                bonus.CritChanceBonus += (int)value;
                break;
            case Enums.PassiveStatType.AttackSpeed:
                bonus.AttackSpeedBonus += (int)value;
                break;
            case Enums.PassiveStatType.ManaRegen:
                bonus.ManaRegenBonus += value;
                break;
            case Enums.PassiveStatType.HPRegen:
                bonus.HPRegenBonus += value;
                break;
            case Enums.PassiveStatType.MovementSpeed:
                bonus.MovementSpeedBonus += (int)value;
                break;
            case Enums.PassiveStatType.FireDamage:
                bonus.FireDamageBonus += (int)value;
                break;
            case Enums.PassiveStatType.IceDamage:
                bonus.IceDamageBonus += (int)value;
                break;
            case Enums.PassiveStatType.LightningDamage:
                bonus.LightningDamageBonus += (int)value;
                break;
            case Enums.PassiveStatType.PoisonDamage:
                bonus.PoisonDamageBonus += (int)value;
                break;
            case Enums.PassiveStatType.LifeSteal:
                bonus.LifeStealPercent += (int)value;
                break;
            default:
                // Eski sistem uyumluluğu - isim bazlı
                ApplyLegacyPassive(bonus, skill, value);
                break;
        }
    }

    private void ApplyLegacyPassive(PassiveBonus bonus, SkillModel skill, double value)
    {
        string name = skill.Name.ToLowerInvariant();

        if (name.Contains("demir") || name.Contains("zırh") || name.Contains("kalkan") || name.Contains("deri"))
            bonus.DefenseBonus += (int)value;
        else if (name.Contains("güç") || name.Contains("strength") || name.Contains("saldırı"))
            bonus.AttackBonus += (int)value;
        else if (name.Contains("sihir") || name.Contains("magic") || name.Contains("büyü"))
            bonus.MagicAttackBonus += (int)value;
        else if (name.Contains("kritik") || name.Contains("critical"))
            bonus.CritChanceBonus += (int)value;
        else if (name.Contains("hız") || name.Contains("speed"))
            bonus.AttackSpeedBonus += (int)value;
    }

    // Eski uyumluluk için - kullanılmayabilir
    public void ApplyPassiveEffects(CharacterModel character, List<SkillModel> learnedSkills)
    {
        var bonus = CalculatePassiveBonuses(learnedSkills);
        // Bu değerler artık StatManager üzerinden hesaplanmalı
    }
}

/// <summary>
/// Pasif skill'lerden gelen toplam bonusları tutar.
/// StatManager bu değerleri kullanarak toplam stat hesaplar.
/// </summary>
public class PassiveBonus
{
    public int DefenseBonus { get; set; }
    public int AttackBonus { get; set; }
    public int MagicAttackBonus { get; set; }
    public int MaxHPBonus { get; set; }
    public int MaxManaBonus { get; set; }
    public int CritChanceBonus { get; set; }
    public int AttackSpeedBonus { get; set; }
    public double ManaRegenBonus { get; set; }
    public double HPRegenBonus { get; set; }
    public int MovementSpeedBonus { get; set; }
    public int FireDamageBonus { get; set; }
    public int IceDamageBonus { get; set; }
    public int LightningDamageBonus { get; set; }
    public int PoisonDamageBonus { get; set; }
    public int LifeStealPercent { get; set; }
}
