using Microsoft.Data.Sqlite;

namespace rpg_deneme.Data;

/// <summary>
/// Veritabanı şemasını (tabloları) başlatan sınıf.
/// </summary>
public static class SQLiteSchemaInitializer
{
    /// <summary>
    /// Gerekli tabloların veritabanında oluşturulmasını sağlar.
    /// </summary>
    public static void EnsureCreated()
    {
        DatabaseHelper.EnsureDatabaseFileExists();
        using SqliteConnection conn = DatabaseHelper.GetConnection();
        conn.Open();

        using SqliteCommand cmd = conn.CreateCommand();
        cmd.CommandText = "PRAGMA foreign_keys = ON;";
        cmd.ExecuteNonQuery();

        // Karakterler Tablosu
        ExecuteNonQuery(conn, @"
            CREATE TABLE IF NOT EXISTS Characters (
                CharacterID INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Class INTEGER NOT NULL,
                Level INTEGER NULL DEFAULT 1,
                Experience INTEGER NULL DEFAULT 0,
                StatPoints INTEGER NULL DEFAULT 0,
                STR INTEGER NULL DEFAULT 5,
                DEX INTEGER NULL DEFAULT 5,
                INT INTEGER NULL DEFAULT 5,
                VIT INTEGER NULL DEFAULT 5,
                CurrentHP INTEGER NULL,
                CurrentMana INTEGER NULL,
                Gold INTEGER NULL DEFAULT 0,
                CreatedAt TEXT NULL DEFAULT (CURRENT_TIMESTAMP),
                CurrentZoneID INTEGER NULL DEFAULT 1,
                MaxUnlockedZoneID INTEGER NULL DEFAULT 1,
                MaxSurvivalWave INTEGER NULL DEFAULT 1,
                MaxSurvivalWave INTEGER NULL DEFAULT 1,
                SlotIndex INTEGER NULL DEFAULT 0,
                SkillPoints INTEGER NULL DEFAULT 0
            );
        ");

        try
        {
            ExecuteNonQuery(conn, "ALTER TABLE Characters ADD COLUMN SkillPoints INTEGER DEFAULT 0;");
        }
        catch { }

        try
        {
            ExecuteNonQuery(conn, "ALTER TABLE Characters ADD COLUMN SlotIndex INTEGER DEFAULT 0;");
        }
        catch
        {
            // Column likely exists
        }

        ExecuteNonQuery(conn, "CREATE UNIQUE INDEX IF NOT EXISTS IX_Characters_Name ON Characters(Name);");

        // Bölgeler Tablosu
        ExecuteNonQuery(conn, @"
            CREATE TABLE IF NOT EXISTS Zones (
                ZoneID INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NULL,
                Description TEXT NULL,
                MinLevel INTEGER NULL DEFAULT 1,
                OrderIndex INTEGER NULL
            );
        ");

        // Düşmanlar Tablosu
        ExecuteNonQuery(conn, @"
            CREATE TABLE IF NOT EXISTS Enemies (
                EnemyID INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NULL,
                Level INTEGER NULL,
                MaxHP INTEGER NULL,
                Damage INTEGER NULL,
                ExpReward INTEGER NULL,
                GoldReward INTEGER NULL,
                SpritePath TEXT NULL,
                IsBoss INTEGER NULL DEFAULT 0
            );
        ");

        // Bölge Düşmanları Eşleşme Tablosu
        ExecuteNonQuery(conn, @"
            CREATE TABLE IF NOT EXISTS ZoneEnemies (
                ZoneID INTEGER NULL,
                EnemyID INTEGER NULL,
                SpawnRate INTEGER NULL,
                FOREIGN KEY(ZoneID) REFERENCES Zones(ZoneID),
                FOREIGN KEY(EnemyID) REFERENCES Enemies(EnemyID)
            );
        ");

        // Eşya Şablonları Tablosu
        ExecuteNonQuery(conn, @"
            CREATE TABLE IF NOT EXISTS ItemTemplates (
                TemplateID INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                ItemType INTEGER NOT NULL,
                BaseMinDamage INTEGER NULL DEFAULT 0,
                BaseMaxDamage INTEGER NULL DEFAULT 0,
                BaseDefense INTEGER NULL DEFAULT 0,
                BaseAttackSpeed REAL NULL DEFAULT 1.0,
                ReqLevel INTEGER NULL DEFAULT 1,
                ReqClass INTEGER NULL DEFAULT 0,
                MaxStack INTEGER NULL DEFAULT 1,
                EffectType INTEGER NULL DEFAULT 0,
                EffectValue INTEGER NULL DEFAULT 0,
                Cooldown INTEGER NULL DEFAULT 0,
                BaseMinMagicDamage INTEGER NOT NULL DEFAULT 0,
                BaseMaxMagicDamage INTEGER NOT NULL DEFAULT 0,
                Price INTEGER NULL DEFAULT 0,
                SellPrice INTEGER NULL DEFAULT 0,
                IsStackable INTEGER NULL DEFAULT 0
            );
        ");

        // Eşya Örnekleri (Envanterdeki Eşyalar) Tablosu
        ExecuteNonQuery(conn, @"
            CREATE TABLE IF NOT EXISTS ItemInstances (
                InstanceID INTEGER PRIMARY KEY AUTOINCREMENT,
                TemplateID INTEGER NOT NULL,
                OwnerID INTEGER NOT NULL,
                Location INTEGER NOT NULL,
                SlotIndex INTEGER NOT NULL,
                Grade INTEGER NOT NULL,
                UpgradeLevel INTEGER NULL DEFAULT 0,
                Durability INTEGER NULL DEFAULT 100,
                Count INTEGER NOT NULL DEFAULT 1,
                LastUsed TEXT NULL,
                FOREIGN KEY(OwnerID) REFERENCES Characters(CharacterID),
                FOREIGN KEY(TemplateID) REFERENCES ItemTemplates(TemplateID)
            );
        ");

        // Eşya Özellikleri Tablosu
        ExecuteNonQuery(conn, @"
            CREATE TABLE IF NOT EXISTS ItemAttributes (
                AttributeID INTEGER PRIMARY KEY AUTOINCREMENT,
                InstanceID INTEGER NOT NULL,
                AttrType INTEGER NOT NULL,
                AttrValue INTEGER NOT NULL,
                FOREIGN KEY(InstanceID) REFERENCES ItemInstances(InstanceID) ON DELETE CASCADE
            );
        ");

        // Ganimet (Drop) Tabloları
        ExecuteNonQuery(conn, @"
            CREATE TABLE IF NOT EXISTS LootTables (
                LootID INTEGER PRIMARY KEY AUTOINCREMENT,
                EnemyID INTEGER NULL,
                TemplateID INTEGER NULL,
                DropRate REAL NULL,
                MinLevel INTEGER NULL DEFAULT 0,
                FOREIGN KEY(EnemyID) REFERENCES Enemies(EnemyID),
                FOREIGN KEY(TemplateID) REFERENCES ItemTemplates(TemplateID)
            );
        ");

        // Mağazalar Tablosu
        ExecuteNonQuery(conn, @"
            CREATE TABLE IF NOT EXISTS Shops (
                ShopID INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                NpcType INTEGER NOT NULL
            );
        ");

        // Mağaza Eşyaları Tablosu
        ExecuteNonQuery(conn, @"
            CREATE TABLE IF NOT EXISTS ShopItems (
                ShopItemID INTEGER PRIMARY KEY AUTOINCREMENT,
                ShopID INTEGER NOT NULL,
                TemplateID INTEGER NOT NULL,
                Price INTEGER NOT NULL,
                FOREIGN KEY(ShopID) REFERENCES Shops(ShopID),
                FOREIGN KEY(TemplateID) REFERENCES ItemTemplates(TemplateID)
            );
        ");

        // Karakter Bölge İlerleme Verileri
        ExecuteNonQuery(conn, @"
            CREATE TABLE IF NOT EXISTS CharacterZoneData (
                ID INTEGER PRIMARY KEY AUTOINCREMENT,
                CharacterID INTEGER NOT NULL,
                ZoneID INTEGER NOT NULL,
                ProgressEasy INTEGER NOT NULL DEFAULT 0,
                ProgressNormal INTEGER NOT NULL DEFAULT 0,
                ProgressHard INTEGER NOT NULL DEFAULT 0,
                BossKilledEasy INTEGER NOT NULL DEFAULT 0,
                BossKilledNormal INTEGER NOT NULL DEFAULT 0,
                BossKilledHard INTEGER NOT NULL DEFAULT 0,
                FOREIGN KEY(CharacterID) REFERENCES Characters(CharacterID),
                FOREIGN KEY(ZoneID) REFERENCES Zones(ZoneID)
            );
        ");

        // Yetenekler Tablosu
        ExecuteNonQuery(conn, @"
            CREATE TABLE IF NOT EXISTS Skills (
                SkillID INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Description TEXT NULL,
                Class INTEGER NOT NULL,
                Type INTEGER NOT NULL,
                MaxLevel INTEGER DEFAULT 5,
                RequiredLevel INTEGER DEFAULT 1,
                Cooldown REAL DEFAULT 0,
                ManaCost INTEGER DEFAULT 0,
                EffectType INTEGER DEFAULT 0,
                BaseEffectValue REAL DEFAULT 0,
                EffectScaling REAL DEFAULT 0,
                Duration REAL DEFAULT 0,
                PassiveStatType INTEGER DEFAULT 0,
                SecondaryEffect INTEGER DEFAULT 0,
                SecondaryEffectValue REAL DEFAULT 0,
                SecondaryEffectDuration REAL DEFAULT 0,
                IconPath TEXT NULL,
                RowIndex INTEGER DEFAULT 0,
                ColIndex INTEGER DEFAULT 0
            );
        ");

        // Yeni sütunları mevcut tabloya ekle (migration)
        try { ExecuteNonQuery(conn, "ALTER TABLE Skills ADD COLUMN PassiveStatType INTEGER DEFAULT 0;"); } catch { }
        try { ExecuteNonQuery(conn, "ALTER TABLE Skills ADD COLUMN SecondaryEffect INTEGER DEFAULT 0;"); } catch { }
        try { ExecuteNonQuery(conn, "ALTER TABLE Skills ADD COLUMN SecondaryEffectValue REAL DEFAULT 0;"); } catch { }
        try { ExecuteNonQuery(conn, "ALTER TABLE Skills ADD COLUMN SecondaryEffectDuration REAL DEFAULT 0;"); } catch { }


        // Yetenek Bağımlılıkları Tablosu
        ExecuteNonQuery(conn, @"
            CREATE TABLE IF NOT EXISTS SkillDependencies (
                SkillID INTEGER NOT NULL,
                PrerequisiteSkillID INTEGER NOT NULL,
                PRIMARY KEY (SkillID, PrerequisiteSkillID),
                FOREIGN KEY(SkillID) REFERENCES Skills(SkillID),
                FOREIGN KEY(PrerequisiteSkillID) REFERENCES Skills(SkillID)
            );
        ");

        // Karakter Yetenekleri Tablosu
        ExecuteNonQuery(conn, @"
            CREATE TABLE IF NOT EXISTS CharacterSkills (
                CharacterID INTEGER NOT NULL,
                SkillID INTEGER NOT NULL,
                CurrentLevel INTEGER DEFAULT 0,
                PRIMARY KEY (CharacterID, SkillID),
                FOREIGN KEY(CharacterID) REFERENCES Characters(CharacterID),
                FOREIGN KEY(SkillID) REFERENCES Skills(SkillID)
            );
        ");

        // Hotbar Ayarları Tablosu
        ExecuteNonQuery(conn, @"
            CREATE TABLE IF NOT EXISTS HotbarSettings (
                CharacterID INTEGER NOT NULL,
                SlotIndex INTEGER NOT NULL,
                Type INTEGER NOT NULL DEFAULT 0, -- 0: Item, 1: Skill
                ReferenceID INTEGER NULL, -- ItemInstanceID or SkillID
                PRIMARY KEY (CharacterID, SlotIndex),
                FOREIGN KEY(CharacterID) REFERENCES Characters(CharacterID) ON DELETE CASCADE
            );
        ");

        try
        {
            // Update existing HotbarSettings if strictly Items.
            // This is tricky if data exists. For now, we assume simple migration or recreate.
            // Adding columns is safer.
            ExecuteNonQuery(conn, "ALTER TABLE HotbarSettings ADD COLUMN Type INTEGER DEFAULT 0;");
            ExecuteNonQuery(conn, "ALTER TABLE HotbarSettings ADD COLUMN ReferenceID INTEGER NULL;");
            // If ItemInstanceID exists, migrate it to ReferenceID for Type=0?
            // SQLite ALTER TABLE is limited.
            // For dev environment simplicity, users might lose hotbar. 
            // Better strategy: Use new columns, ignore old if needed, or let user re-drag.
        }
        catch { }

        // Seed Skills if empty - Her zaman kontrol et ve doldur
        SeedSkillsIfEmpty(conn);
    }



    private static void SeedSkillsIfEmpty(SqliteConnection conn, bool force = false)
    {
        // Mevcut skill sayısı 20'den azsa tabloları temizle ve yeniden seed'le
        long skillCount = GetTableCount(conn, "Skills");
        bool needsReseed = skillCount < 20;

        if (force || skillCount == 0 || needsReseed)
        {
            // Önce eski verileri temizle
            try { ExecuteNonQuery(conn, "DELETE FROM CharacterSkills;"); } catch { }
            try { ExecuteNonQuery(conn, "DELETE FROM SkillDependencies;"); } catch { }
            try { ExecuteNonQuery(conn, "DELETE FROM Skills;"); } catch { }

            // Skill ID'lerini sıfırla
            try { ExecuteNonQuery(conn, "DELETE FROM sqlite_sequence WHERE name='Skills';"); } catch { }

            // ========== WARRIOR SKILLS ==========
            // Row 0: Başlangıç - Sol: Tank Path, Sağ: DPS Path
            // ID 1: Heavy Slash (Active) - Temel saldırı
            ExecuteNonQuery(conn, @"INSERT INTO Skills (Name, Description, Class, Type, MaxLevel, RequiredLevel, Cooldown, ManaCost, EffectType, BaseEffectValue, EffectScaling, SecondaryEffect, SecondaryEffectValue, SecondaryEffectDuration, IconPath, RowIndex, ColIndex) 
                VALUES ('Heavy Slash', 'Güçlü bir kılıç darbesi. Hedefe yoğun fiziksel hasar verir.', 1, 0, 5, 1, 3.0, 10, 0, 15, 8, 0, 0, 0, 'slash_icon', 0, 0);");
            // ID 2: Iron Skin (Passive - Defense)
            ExecuteNonQuery(conn, @"INSERT INTO Skills (Name, Description, Class, Type, MaxLevel, RequiredLevel, EffectType, BaseEffectValue, EffectScaling, PassiveStatType, IconPath, RowIndex, ColIndex) 
                VALUES ('Demir Deri', 'Vücudunu sertleştirerek savunmayı artırır. +DEF', 1, 1, 5, 1, 4, 5, 3, 1, 'shield_icon', 0, -1);");
            // ID 3: Rage (Passive - Attack)
            ExecuteNonQuery(conn, @"INSERT INTO Skills (Name, Description, Class, Type, MaxLevel, RequiredLevel, EffectType, BaseEffectValue, EffectScaling, PassiveStatType, IconPath, RowIndex, ColIndex) 
                VALUES ('Öfke', 'İçindeki öfkeyi saldırıya dönüştürür. +ATK', 1, 1, 5, 1, 4, 3, 2, 2, 'rage_icon', 0, 1);");

            // Row 1: Orta seviye
            // ID 4: Whirlwind (Active AoE) - Req Heavy Slash
            ExecuteNonQuery(conn, @"INSERT INTO Skills (Name, Description, Class, Type, MaxLevel, RequiredLevel, Cooldown, ManaCost, EffectType, BaseEffectValue, EffectScaling, SecondaryEffect, SecondaryEffectValue, SecondaryEffectDuration, IconPath, RowIndex, ColIndex) 
                VALUES ('Dönme Dolap', 'Etrafındaki tüm düşmanlara dönerek saldırır.', 1, 0, 5, 5, 8.0, 25, 0, 20, 12, 0, 0, 0, 'whirlwind_icon', 1, 0);");
            ExecuteNonQuery(conn, "INSERT INTO SkillDependencies (SkillID, PrerequisiteSkillID) VALUES (4, 1);");
            // ID 5: Shield Wall (Passive - Block) - Req Iron Skin
            ExecuteNonQuery(conn, @"INSERT INTO Skills (Name, Description, Class, Type, MaxLevel, RequiredLevel, EffectType, BaseEffectValue, EffectScaling, PassiveStatType, IconPath, RowIndex, ColIndex) 
                VALUES ('Kalkan Duvarı', 'Bloklama şansını artırır. +BLOCK%', 1, 1, 5, 5, 4, 5, 2, 1, 'block_icon', 1, -1);");
            ExecuteNonQuery(conn, "INSERT INTO SkillDependencies (SkillID, PrerequisiteSkillID) VALUES (5, 2);");
            // ID 6: Battle Fury (Passive - Attack Speed) - Req Rage
            ExecuteNonQuery(conn, @"INSERT INTO Skills (Name, Description, Class, Type, MaxLevel, RequiredLevel, EffectType, BaseEffectValue, EffectScaling, PassiveStatType, IconPath, RowIndex, ColIndex) 
                VALUES ('Savaş Çılgınlığı', 'Saldırı hızını artırır. +ATK SPD', 1, 1, 5, 5, 4, 5, 3, 7, 'fury_icon', 1, 1);");
            ExecuteNonQuery(conn, "INSERT INTO SkillDependencies (SkillID, PrerequisiteSkillID) VALUES (6, 3);");

            // Row 2: İleri seviye
            // ID 7: Crushing Blow (Active - Bleed) - Req Whirlwind
            ExecuteNonQuery(conn, @"INSERT INTO Skills (Name, Description, Class, Type, MaxLevel, RequiredLevel, Cooldown, ManaCost, EffectType, BaseEffectValue, EffectScaling, SecondaryEffect, SecondaryEffectValue, SecondaryEffectDuration, IconPath, RowIndex, ColIndex) 
                VALUES ('Ezici Darbe', 'Düşmanı ezer ve kanamaya neden olur. Saniyede hasar.', 1, 0, 5, 10, 6.0, 30, 0, 30, 15, 7, 5, 3, 'crush_icon', 2, 0);");
            ExecuteNonQuery(conn, "INSERT INTO SkillDependencies (SkillID, PrerequisiteSkillID) VALUES (7, 4);");
            // ID 8: Fortitude (Passive - MaxHP) - Req Shield Wall
            ExecuteNonQuery(conn, @"INSERT INTO Skills (Name, Description, Class, Type, MaxLevel, RequiredLevel, EffectType, BaseEffectValue, EffectScaling, PassiveStatType, IconPath, RowIndex, ColIndex) 
                VALUES ('Dayanıklılık', 'Maksimum canı artırır. +MAX HP', 1, 1, 5, 10, 4, 30, 20, 4, 'hp_icon', 2, -1);");
            ExecuteNonQuery(conn, "INSERT INTO SkillDependencies (SkillID, PrerequisiteSkillID) VALUES (8, 5);");
            // ID 9: Critical Mastery (Passive - Crit) - Req Battle Fury
            ExecuteNonQuery(conn, @"INSERT INTO Skills (Name, Description, Class, Type, MaxLevel, RequiredLevel, EffectType, BaseEffectValue, EffectScaling, PassiveStatType, IconPath, RowIndex, ColIndex) 
                VALUES ('Kritik Ustalığı', 'Kritik vuruş şansını artırır. +CRIT%', 1, 1, 5, 10, 4, 3, 2, 6, 'crit_icon', 2, 1);");
            ExecuteNonQuery(conn, "INSERT INTO SkillDependencies (SkillID, PrerequisiteSkillID) VALUES (9, 6);");

            // ========== MAGE SKILLS ==========
            // Row 0: Başlangıç - Sol: Ateş, Orta: Buz, Sağ: Yıldırım
            // ID 10: Fireball (Active - Burn)
            ExecuteNonQuery(conn, @"INSERT INTO Skills (Name, Description, Class, Type, MaxLevel, RequiredLevel, Cooldown, ManaCost, EffectType, BaseEffectValue, EffectScaling, SecondaryEffect, SecondaryEffectValue, SecondaryEffectDuration, IconPath, RowIndex, ColIndex) 
                VALUES ('Ateş Topu', 'Düşmana ateş topu fırlatır. Yanma etkisi verir.', 3, 0, 5, 1, 2.0, 15, 0, 18, 10, 1, 3, 3, 'fireball_icon', 0, -1);");
            // ID 11: Ice Bolt (Active - Slow)
            ExecuteNonQuery(conn, @"INSERT INTO Skills (Name, Description, Class, Type, MaxLevel, RequiredLevel, Cooldown, ManaCost, EffectType, BaseEffectValue, EffectScaling, SecondaryEffect, SecondaryEffectValue, SecondaryEffectDuration, IconPath, RowIndex, ColIndex) 
                VALUES ('Buz Oku', 'Dondurucu buz oku fırlatır. Yavaşlatma etkisi.', 3, 0, 5, 1, 3.0, 15, 0, 12, 7, 2, 30, 2, 'icebolt_icon', 0, 0);");
            // ID 12: Lightning Bolt (Active - Shock)
            ExecuteNonQuery(conn, @"INSERT INTO Skills (Name, Description, Class, Type, MaxLevel, RequiredLevel, Cooldown, ManaCost, EffectType, BaseEffectValue, EffectScaling, SecondaryEffect, SecondaryEffectValue, SecondaryEffectDuration, IconPath, RowIndex, ColIndex) 
                VALUES ('Yıldırım', 'Hızlı bir yıldırım çarpar. Şok etkisi verir.', 3, 0, 5, 1, 1.5, 12, 0, 10, 6, 6, 20, 2, 'lightning_icon', 0, 1);");

            // Row 1: Orta seviye - Pasifler
            // ID 13: Fire Mastery (Passive - Fire Damage)
            ExecuteNonQuery(conn, @"INSERT INTO Skills (Name, Description, Class, Type, MaxLevel, RequiredLevel, EffectType, BaseEffectValue, EffectScaling, PassiveStatType, IconPath, RowIndex, ColIndex) 
                VALUES ('Ateş Ustalığı', 'Ateş hasarını artırır. +FIRE DMG', 3, 1, 5, 5, 4, 5, 3, 11, 'fire_mastery_icon', 1, -1);");
            ExecuteNonQuery(conn, "INSERT INTO SkillDependencies (SkillID, PrerequisiteSkillID) VALUES (13, 10);");
            // ID 14: Ice Mastery (Passive - Ice Damage)
            ExecuteNonQuery(conn, @"INSERT INTO Skills (Name, Description, Class, Type, MaxLevel, RequiredLevel, EffectType, BaseEffectValue, EffectScaling, PassiveStatType, IconPath, RowIndex, ColIndex) 
                VALUES ('Buz Ustalığı', 'Buz hasarını artırır. +ICE DMG', 3, 1, 5, 5, 4, 5, 3, 12, 'ice_mastery_icon', 1, 0);");
            ExecuteNonQuery(conn, "INSERT INTO SkillDependencies (SkillID, PrerequisiteSkillID) VALUES (14, 11);");
            // ID 15: Lightning Mastery (Passive - Lightning Damage)
            ExecuteNonQuery(conn, @"INSERT INTO Skills (Name, Description, Class, Type, MaxLevel, RequiredLevel, EffectType, BaseEffectValue, EffectScaling, PassiveStatType, IconPath, RowIndex, ColIndex) 
                VALUES ('Yıldırım Ustalığı', 'Yıldırım hasarını artırır. +LIGHTNING DMG', 3, 1, 5, 5, 4, 5, 3, 13, 'lightning_mastery_icon', 1, 1);");
            ExecuteNonQuery(conn, "INSERT INTO SkillDependencies (SkillID, PrerequisiteSkillID) VALUES (15, 12);");

            // Row 2: Güçlü aktifler
            // ID 16: Meteor (Active AoE - Burn) - Req Fire Mastery
            ExecuteNonQuery(conn, @"INSERT INTO Skills (Name, Description, Class, Type, MaxLevel, RequiredLevel, Cooldown, ManaCost, EffectType, BaseEffectValue, EffectScaling, SecondaryEffect, SecondaryEffectValue, SecondaryEffectDuration, IconPath, RowIndex, ColIndex) 
                VALUES ('Meteor', 'Gökten meteor düşürür. Geniş alanda yıkıcı hasar ve yanma.', 3, 0, 5, 10, 15.0, 50, 0, 50, 25, 1, 8, 5, 'meteor_icon', 2, -1);");
            ExecuteNonQuery(conn, "INSERT INTO SkillDependencies (SkillID, PrerequisiteSkillID) VALUES (16, 13);");
            // ID 17: Blizzard (Active AoE - Freeze) - Req Ice Mastery
            ExecuteNonQuery(conn, @"INSERT INTO Skills (Name, Description, Class, Type, MaxLevel, RequiredLevel, Cooldown, ManaCost, EffectType, BaseEffectValue, EffectScaling, SecondaryEffect, SecondaryEffectValue, SecondaryEffectDuration, IconPath, RowIndex, ColIndex) 
                VALUES ('Kar Fırtınası', 'Geniş alanda buz fırtınası. Dondurma etkisi.', 3, 0, 5, 10, 12.0, 45, 0, 35, 18, 5, 50, 3, 'blizzard_icon', 2, 0);");
            ExecuteNonQuery(conn, "INSERT INTO SkillDependencies (SkillID, PrerequisiteSkillID) VALUES (17, 14);");
            // ID 18: Chain Lightning (Active Multi-target) - Req Lightning Mastery
            ExecuteNonQuery(conn, @"INSERT INTO Skills (Name, Description, Class, Type, MaxLevel, RequiredLevel, Cooldown, ManaCost, EffectType, BaseEffectValue, EffectScaling, SecondaryEffect, SecondaryEffectValue, SecondaryEffectDuration, IconPath, RowIndex, ColIndex) 
                VALUES ('Zincirleme Yıldırım', 'Düşmanlar arasında zıplayan yıldırım. Şoka sokma.', 3, 0, 5, 10, 8.0, 40, 0, 25, 15, 6, 30, 2, 'chain_lightning_icon', 2, 1);");
            ExecuteNonQuery(conn, "INSERT INTO SkillDependencies (SkillID, PrerequisiteSkillID) VALUES (18, 15);");

            // Row 3: Mana ve Büyü güçlendirme (tüm yollar için)
            // ID 19: Mana Pool (Passive - MaxMana)
            ExecuteNonQuery(conn, @"INSERT INTO Skills (Name, Description, Class, Type, MaxLevel, RequiredLevel, EffectType, BaseEffectValue, EffectScaling, PassiveStatType, IconPath, RowIndex, ColIndex) 
                VALUES ('Mana Havuzu', 'Maksimum mana miktarını artırır. +MAX MANA', 3, 1, 5, 1, 4, 20, 15, 5, 'mana_icon', 3, 0);");
            // ID 20: Arcane Power (Passive - MagicAttack)
            ExecuteNonQuery(conn, @"INSERT INTO Skills (Name, Description, Class, Type, MaxLevel, RequiredLevel, EffectType, BaseEffectValue, EffectScaling, PassiveStatType, IconPath, RowIndex, ColIndex) 
                VALUES ('Gizemli Güç', 'Büyü saldırısını artırır. +MAGIC ATK', 3, 1, 5, 5, 4, 5, 4, 3, 'arcane_icon', 3, 1);");

            // ========== ROGUE SKILLS ==========
            // Row 0: Başlangıç - Sol: Zehir, Sağ: Hız
            // ID 21: Backstab (Active)
            ExecuteNonQuery(conn, @"INSERT INTO Skills (Name, Description, Class, Type, MaxLevel, RequiredLevel, Cooldown, ManaCost, EffectType, BaseEffectValue, EffectScaling, SecondaryEffect, SecondaryEffectValue, SecondaryEffectDuration, IconPath, RowIndex, ColIndex) 
                VALUES ('Sırttan Bıçaklama', 'Düşmanın arkasından güçlü darbe.', 2, 0, 5, 1, 4.0, 15, 0, 20, 10, 0, 0, 0, 'backstab_icon', 0, 0);");
            // ID 22: Poison Blade (Passive - Poison on hit)
            ExecuteNonQuery(conn, @"INSERT INTO Skills (Name, Description, Class, Type, MaxLevel, RequiredLevel, EffectType, BaseEffectValue, EffectScaling, PassiveStatType, IconPath, RowIndex, ColIndex) 
                VALUES ('Zehirli Bıçak', 'Zehir hasarını artırır. +POISON DMG', 2, 1, 5, 1, 4, 3, 2, 14, 'poison_icon', 0, -1);");
            // ID 23: Agility (Passive - Attack Speed)
            ExecuteNonQuery(conn, @"INSERT INTO Skills (Name, Description, Class, Type, MaxLevel, RequiredLevel, EffectType, BaseEffectValue, EffectScaling, PassiveStatType, IconPath, RowIndex, ColIndex) 
                VALUES ('Çeviklik', 'Saldırı hızını artırır. +ATK SPD', 2, 1, 5, 1, 4, 8, 4, 7, 'agility_icon', 0, 1);");

            // Row 1: Orta seviye
            // ID 24: Envenom (Active - Poison DoT)
            ExecuteNonQuery(conn, @"INSERT INTO Skills (Name, Description, Class, Type, MaxLevel, RequiredLevel, Cooldown, ManaCost, EffectType, BaseEffectValue, EffectScaling, SecondaryEffect, SecondaryEffectValue, SecondaryEffectDuration, IconPath, RowIndex, ColIndex) 
                VALUES ('Zehirleme', 'Düşmanı zehirler. Saniyede hasar verir.', 2, 0, 5, 5, 5.0, 20, 0, 10, 5, 4, 8, 5, 'envenom_icon', 1, -1);");
            ExecuteNonQuery(conn, "INSERT INTO SkillDependencies (SkillID, PrerequisiteSkillID) VALUES (24, 22);");
            // ID 25: Dual Strike (Active - Fast double hit)
            ExecuteNonQuery(conn, @"INSERT INTO Skills (Name, Description, Class, Type, MaxLevel, RequiredLevel, Cooldown, ManaCost, EffectType, BaseEffectValue, EffectScaling, SecondaryEffect, SecondaryEffectValue, SecondaryEffectDuration, IconPath, RowIndex, ColIndex) 
                VALUES ('Çift Darbe', 'Hızlı iki ardışık saldırı yapar.', 2, 0, 5, 5, 3.0, 18, 0, 12, 6, 0, 0, 0, 'dual_strike_icon', 1, 0);");
            ExecuteNonQuery(conn, "INSERT INTO SkillDependencies (SkillID, PrerequisiteSkillID) VALUES (25, 21);");
            // ID 26: Evasion (Passive - Movement Speed)
            ExecuteNonQuery(conn, @"INSERT INTO Skills (Name, Description, Class, Type, MaxLevel, RequiredLevel, EffectType, BaseEffectValue, EffectScaling, PassiveStatType, IconPath, RowIndex, ColIndex) 
                VALUES ('Kaçınma', 'Hareket hızını artırır. +MOVE SPD', 2, 1, 5, 5, 4, 5, 3, 10, 'evasion_icon', 1, 1);");
            ExecuteNonQuery(conn, "INSERT INTO SkillDependencies (SkillID, PrerequisiteSkillID) VALUES (26, 23);");

            // Row 2: İleri seviye
            // ID 27: Death Mark (Active - Weakness then big damage)
            ExecuteNonQuery(conn, @"INSERT INTO Skills (Name, Description, Class, Type, MaxLevel, RequiredLevel, Cooldown, ManaCost, EffectType, BaseEffectValue, EffectScaling, SecondaryEffect, SecondaryEffectValue, SecondaryEffectDuration, IconPath, RowIndex, ColIndex) 
                VALUES ('Ölüm İşareti', 'Düşmanı işaretler, savunmasını düşürür.', 2, 0, 5, 10, 10.0, 35, 0, 40, 20, 8, 25, 4, 'death_mark_icon', 2, -1);");
            ExecuteNonQuery(conn, "INSERT INTO SkillDependencies (SkillID, PrerequisiteSkillID) VALUES (27, 24);");
            // ID 28: Critical Strike (Passive - Crit Chance)
            ExecuteNonQuery(conn, @"INSERT INTO Skills (Name, Description, Class, Type, MaxLevel, RequiredLevel, EffectType, BaseEffectValue, EffectScaling, PassiveStatType, IconPath, RowIndex, ColIndex) 
                VALUES ('Kritik Vuruş', 'Kritik şansını artırır. +CRIT%', 2, 1, 5, 10, 4, 5, 3, 6, 'crit_strike_icon', 2, 0);");
            ExecuteNonQuery(conn, "INSERT INTO SkillDependencies (SkillID, PrerequisiteSkillID) VALUES (28, 25);");
            // ID 29: Life Steal (Passive - Life Steal)
            ExecuteNonQuery(conn, @"INSERT INTO Skills (Name, Description, Class, Type, MaxLevel, RequiredLevel, EffectType, BaseEffectValue, EffectScaling, PassiveStatType, IconPath, RowIndex, ColIndex) 
                VALUES ('Can Çalma', 'Verilen hasarın bir kısmını can olarak geri alır.', 2, 1, 5, 10, 4, 2, 1, 15, 'lifesteal_icon', 2, 1);");
            ExecuteNonQuery(conn, "INSERT INTO SkillDependencies (SkillID, PrerequisiteSkillID) VALUES (29, 26);");
        }

        // Mevcut veritabanı için güncelleme
        UpdateSkillEffects(conn);
    }

    private static void UpdateSkillEffects(SqliteConnection conn)
    {
        try
        {
            // Pasif stat tiplerini güncelle
            ExecuteNonQuery(conn, "UPDATE Skills SET PassiveStatType=1 WHERE Name='Demir Deri'"); // Defense
            ExecuteNonQuery(conn, "UPDATE Skills SET PassiveStatType=2 WHERE Name='Öfke'"); // Attack
            ExecuteNonQuery(conn, "UPDATE Skills SET PassiveStatType=7 WHERE Name='Savaş Çılgınlığı'"); // AttackSpeed
            ExecuteNonQuery(conn, "UPDATE Skills SET PassiveStatType=4 WHERE Name='Dayanıklılık'"); // MaxHP
            ExecuteNonQuery(conn, "UPDATE Skills SET PassiveStatType=6 WHERE Name='Kritik Ustalığı'"); // CritChance

            // Mage pasifler
            ExecuteNonQuery(conn, "UPDATE Skills SET PassiveStatType=11 WHERE Name='Ateş Ustalığı'"); // FireDamage
            ExecuteNonQuery(conn, "UPDATE Skills SET PassiveStatType=12 WHERE Name='Buz Ustalığı'"); // IceDamage
            ExecuteNonQuery(conn, "UPDATE Skills SET PassiveStatType=13 WHERE Name='Yıldırım Ustalığı'"); // LightningDamage
            ExecuteNonQuery(conn, "UPDATE Skills SET PassiveStatType=5 WHERE Name='Mana Havuzu'"); // MaxMana
            ExecuteNonQuery(conn, "UPDATE Skills SET PassiveStatType=3 WHERE Name='Gizemli Güç'"); // MagicAttack

            // Rogue pasifler
            ExecuteNonQuery(conn, "UPDATE Skills SET PassiveStatType=14 WHERE Name='Zehirli Bıçak'"); // PoisonDamage
            ExecuteNonQuery(conn, "UPDATE Skills SET PassiveStatType=7 WHERE Name='Çeviklik'"); // AttackSpeed
            ExecuteNonQuery(conn, "UPDATE Skills SET PassiveStatType=10 WHERE Name='Kaçınma'"); // MovementSpeed
            ExecuteNonQuery(conn, "UPDATE Skills SET PassiveStatType=6 WHERE Name='Kritik Vuruş'"); // CritChance
            ExecuteNonQuery(conn, "UPDATE Skills SET PassiveStatType=15 WHERE Name='Can Çalma'"); // LifeSteal

            // Aktif skill secondary efektleri
            ExecuteNonQuery(conn, "UPDATE Skills SET SecondaryEffect=1, SecondaryEffectValue=3, SecondaryEffectDuration=3 WHERE Name='Ateş Topu'"); // Burn
            ExecuteNonQuery(conn, "UPDATE Skills SET SecondaryEffect=2, SecondaryEffectValue=30, SecondaryEffectDuration=2 WHERE Name='Buz Oku'"); // Slow
            ExecuteNonQuery(conn, "UPDATE Skills SET SecondaryEffect=6, SecondaryEffectValue=20, SecondaryEffectDuration=2 WHERE Name='Yıldırım'"); // Shock
            ExecuteNonQuery(conn, "UPDATE Skills SET SecondaryEffect=1, SecondaryEffectValue=8, SecondaryEffectDuration=5 WHERE Name='Meteor'"); // Burn
            ExecuteNonQuery(conn, "UPDATE Skills SET SecondaryEffect=5, SecondaryEffectValue=50, SecondaryEffectDuration=3 WHERE Name='Kar Fırtınası'"); // Freeze
            ExecuteNonQuery(conn, "UPDATE Skills SET SecondaryEffect=4, SecondaryEffectValue=8, SecondaryEffectDuration=5 WHERE Name='Zehirleme'"); // Poison
            ExecuteNonQuery(conn, "UPDATE Skills SET SecondaryEffect=7, SecondaryEffectValue=5, SecondaryEffectDuration=3 WHERE Name='Ezici Darbe'"); // Bleed
            ExecuteNonQuery(conn, "UPDATE Skills SET SecondaryEffect=8, SecondaryEffectValue=25, SecondaryEffectDuration=4 WHERE Name='Ölüm İşareti'"); // Weakness
        }
        catch { }
    }

    private static long GetTableCount(SqliteConnection conn, string tableName)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT COUNT(*) FROM {tableName}";
        return (long)cmd.ExecuteScalar();
    }


    /// <summary>
    /// Sql sorgusu çalıştıran yardımcı metod.
    /// </summary>
    private static void ExecuteNonQuery(SqliteConnection conn, string sql)
    {
        using SqliteCommand cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }
}
