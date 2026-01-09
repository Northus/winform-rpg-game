namespace rpg_deneme.Core;

public class Enums
{
    public enum CharacterClass : byte
    {
        Warrior = 1,
        Rogue,
        Mage
    }

    public enum ItemGrade : byte
    {
        Others,
        Common,
        Rare,
        Epic,
        Legendary
    }

    public enum ItemLocation : byte
    {
        Inventory = 1,
        Equipment,
        Storage,
        Ground
    }

    public enum ItemType : byte
    {
        Weapon = 1,
        Armor = 2,
        Consumable = 3,
        Material = 5,
        BlessingMarble = 6,
        EnchantItem = 7
    }

    public enum ItemEffectType : byte
    {
        None,
        RestoreHP,
        RestoreMana
    }

    public enum EquipmentSlot
    {
        Weapon,
        Armor,
        Helmet,
        Boots
    }

    public enum NpcType
    {
        None,
        Merchant,
        Teleporter,
        StorageKeeper,
        BlackSmith,
        ArenaMaster
    }

    public enum ZoneDifficulty
    {
        Easy,
        Normal,
        Hard
    }

    public enum EnemyType
    {
        Melee,
        Ranged,
        Boss
    }

    public enum ItemAttributeType : byte
    {
        None,
        CriticalChance,
        AttackSpeed,
        ManaRegen,
        MaxHP,
        MaxHPPercent,
        MaxMana,
        MaxManaPercent,
        BlockChance,
        AttackValue,
        DefenseValue,
        MagicAttackValue
    }

    public enum SkillType
    {
        Active = 0,
        Passive = 1
    }

    public enum SkilEffectType
    {
        Damage = 0,      // Hasar veren skill
        Heal = 1,        // İyileştirme
        Buff = 2,        // Olumlu etki
        Debuff = 3,      // Olumsuz etki
        PassiveStat = 4  // Pasif stat artışı
    }

    // Pasif skill'lerin hangi stat'ı etkilediği
    public enum PassiveStatType
    {
        None = 0,
        Defense = 1,          // Savunma
        Attack = 2,           // Saldırı
        MagicAttack = 3,      // Büyü Saldırısı
        MaxHP = 4,            // Maksimum Can
        MaxMana = 5,          // Maksimum Mana
        CriticalChance = 6,   // Kritik Şansı
        AttackSpeed = 7,      // Saldırı Hızı
        ManaRegen = 8,        // Mana Yenilenme
        HPRegen = 9,          // Can Yenilenme
        MovementSpeed = 10,   // Hareket Hızı
        FireDamage = 11,      // Ateş Hasarı Bonusu
        IceDamage = 12,       // Buz Hasarı Bonusu
        LightningDamage = 13, // Yıldırım Hasarı Bonusu
        PoisonDamage = 14,    // Zehir Hasarı Bonusu
        LifeSteal = 15        // Can Çalma
    }

    // Aktif skill'lerin özel efektleri
    public enum SkillSecondaryEffect
    {
        None = 0,
        Burn = 1,       // Yanma - saniyede hasar
        Slow = 2,       // Yavaşlatma - hareket hızı azaltma
        Stun = 3,       // Sersemletme - hareket edemez
        Poison = 4,     // Zehir - saniyede hasar
        Freeze = 5,     // Dondurma - hareket edemez
        Shock = 6,      // Elektrik şoku - sonraki hasarı artırır
        Bleed = 7,      // Kanama - saniyede hasar
        Weakness = 8,   // Zayıflık - savunma azaltma
        Knockback = 9   // Geri itme
    }
}
