using System;
using System.Collections.Generic;
using rpg_deneme.Core;

namespace rpg_deneme.Models;

/// <summary>
/// Envanterdeki veya yerdeki bir eşya örneğini temsil eden model sınıfı.
/// </summary>
public class ItemInstance
{
    // Veritabanı ve Sahiplik Bilgileri
    public long InstanceID { get; set; }
    public int TemplateID { get; set; }
    public int OwnerID { get; set; }
    public int Count { get; set; }
    public int SlotIndex { get; set; }

    // Temel Özellikler
    public string Name { get; set; }
    public Enums.ItemType ItemType { get; set; }
    public Enums.ItemGrade Grade { get; set; }
    public Enums.ItemLocation Location { get; set; }
    public int UpgradeLevel { get; set; }

    // Savaş İstatistikleri
    public int MinDamage { get; set; }
    public int MaxDamage { get; set; }
    public int MinMagicDamage { get; set; }
    public int MaxMagicDamage { get; set; }
    public int BaseDefense { get; set; }

    // Kullanım Özellikleri (Potlar vb.)
    public Enums.ItemEffectType EffectType { get; set; }
    public int EffectValue { get; set; }
    public int Cooldown { get; set; }
    public DateTime? LastUsed { get; set; }

    // Sınıf ve Stack Bilgileri
    public byte? AllowedClass { get; set; }
    public bool IsStackable { get; set; }
    public int MaxStack { get; set; } = 1;

    // Efsunlar
    public List<ItemAttribute> Attributes { get; set; } = new List<ItemAttribute>();

    // Fiyat
    public int BuyPrice { get; internal set; }

    /// <summary>
    /// Eşyanın kalan bekleme süresini (saniye cinsinden) hesaplar.
    /// </summary>
    public int RemainingCooldownSeconds
    {
        get
        {
            if (!LastUsed.HasValue || Cooldown == 0) return 0;
            double passed = (DateTime.Now - LastUsed.Value).TotalSeconds;
            double remaining = (double)Cooldown - passed;
            return (remaining > 0) ? (int)remaining : 0;
        }
    }

    /// <summary>
    /// Eşyanın kalan bekleme süresini (onlu sayı cinsinden) hesaplar.
    /// </summary>
    public double RemainingCooldownExact
    {
        get
        {
            if (!LastUsed.HasValue || Cooldown == 0) return 0.0;
            double passed = (DateTime.Now - LastUsed.Value).TotalSeconds;
            double remaining = (double)Cooldown - passed;
            return (remaining > 0) ? remaining : 0.0;
        }
    }
}
