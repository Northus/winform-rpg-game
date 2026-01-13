using Microsoft.Data.Sqlite;
using System;

namespace rpg_deneme.Data;

/// <summary>
/// Veritabanı şemasını (tabloları) başlatan sınıf.
/// </summary>
public static class SQLiteSchemaInitializer
{
    public static void EnsureCreated()
    {
        DatabaseHelper.EnsureDatabaseFileExists();
        using SqliteConnection conn = DatabaseHelper.GetConnection();
        conn.Open();

        // Enable foreign keys
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "PRAGMA foreign_keys = ON;";
            cmd.ExecuteNonQuery();
        }

        CreateTables(conn);
        SeedData(conn);
    }

    private static void ExecuteNonQuery(SqliteConnection conn, string sql)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }

    private static void CreateTables(SqliteConnection conn)
    {
        // Characters
        ExecuteNonQuery(conn, @"
            CREATE TABLE IF NOT EXISTS ""Characters"" (
                ""CharacterID"" INTEGER PRIMARY KEY AUTOINCREMENT,
                ""Name"" TEXT NOT NULL,
                ""Class"" INTEGER NOT NULL,
                ""Level"" INTEGER DEFAULT 1,
                ""Experience"" INTEGER DEFAULT 0,
                ""StatPoints"" INTEGER DEFAULT 0,
                ""STR"" INTEGER DEFAULT 5,
                ""DEX"" INTEGER DEFAULT 5,
                ""INT"" INTEGER DEFAULT 5,
                ""VIT"" INTEGER DEFAULT 5,
                ""CurrentHP"" INTEGER,
                ""CurrentMana"" INTEGER,
                ""Gold"" INTEGER DEFAULT 0,
                ""CreatedAt"" TEXT DEFAULT (CURRENT_TIMESTAMP),
                ""CurrentZoneID"" INTEGER DEFAULT 1,
                ""MaxUnlockedZoneID"" INTEGER DEFAULT 1,
                ""MaxSurvivalWave"" INTEGER DEFAULT 1,
                ""SlotIndex"" INTEGER DEFAULT 0,
                ""SkillPoints"" INTEGER DEFAULT 0
            );
        ");
        ExecuteNonQuery(conn, "CREATE UNIQUE INDEX IF NOT EXISTS \"IX_Characters_Name\" ON \"Characters\" (\"Name\");");

        // Zones
        ExecuteNonQuery(conn, @"
            CREATE TABLE IF NOT EXISTS ""Zones"" (
                ""ZoneID"" INTEGER PRIMARY KEY AUTOINCREMENT,
                ""Name"" TEXT,
                ""Description"" TEXT,
                ""MinLevel"" INTEGER DEFAULT 1,
                ""OrderIndex"" INTEGER
            );
        ");

        // Enemies
        ExecuteNonQuery(conn, @"
            CREATE TABLE IF NOT EXISTS ""Enemies"" (
                ""EnemyID"" INTEGER PRIMARY KEY AUTOINCREMENT,
                ""Name"" TEXT,
                ""Level"" INTEGER,
                ""MaxHP"" INTEGER,
                ""Damage"" INTEGER,
                ""ExpReward"" INTEGER,
                ""GoldReward"" INTEGER,
                ""SpritePath"" TEXT,
                ""IsBoss"" INTEGER DEFAULT 0
            );
        ");

        // ZoneEnemies
        ExecuteNonQuery(conn, @"
            CREATE TABLE IF NOT EXISTS ""ZoneEnemies"" (
                ""ZoneID"" INTEGER,
                ""EnemyID"" INTEGER,
                ""SpawnRate"" INTEGER,
                FOREIGN KEY(""EnemyID"") REFERENCES ""Enemies""(""EnemyID""),
                FOREIGN KEY(""ZoneID"") REFERENCES ""Zones""(""ZoneID"")
            );
        ");

        // ItemTemplates
        ExecuteNonQuery(conn, @"
            CREATE TABLE IF NOT EXISTS ""ItemTemplates"" (
                ""TemplateID"" INTEGER PRIMARY KEY AUTOINCREMENT,
                ""Name"" TEXT NOT NULL,
                ""ItemType"" INTEGER NOT NULL,
                ""BaseMinDamage"" INTEGER DEFAULT 0,
                ""BaseMaxDamage"" INTEGER DEFAULT 0,
                ""BaseDefense"" INTEGER DEFAULT 0,
                ""BaseAttackSpeed"" REAL DEFAULT 1.0,
                ""ReqLevel"" INTEGER DEFAULT 1,
                ""ReqClass"" INTEGER DEFAULT 0,
                ""MaxStack"" INTEGER DEFAULT 1,
                ""EffectType"" INTEGER DEFAULT 0,
                ""EffectValue"" INTEGER DEFAULT 0,
                ""Cooldown"" INTEGER DEFAULT 0,
                ""BaseMinMagicDamage"" INTEGER NOT NULL DEFAULT 0,
                ""BaseMaxMagicDamage"" INTEGER NOT NULL DEFAULT 0,
                ""Price"" INTEGER DEFAULT 0,
                ""SellPrice"" INTEGER DEFAULT 0,
                ""IsStackable"" INTEGER DEFAULT 0
            );
        ");

        // ItemInstances
        ExecuteNonQuery(conn, @"
            CREATE TABLE IF NOT EXISTS ""ItemInstances"" (
                ""InstanceID"" INTEGER PRIMARY KEY AUTOINCREMENT,
                ""TemplateID"" INTEGER NOT NULL,
                ""OwnerID"" INTEGER NOT NULL,
                ""Location"" INTEGER NOT NULL,
                ""SlotIndex"" INTEGER NOT NULL,
                ""Grade"" INTEGER NOT NULL,
                ""UpgradeLevel"" INTEGER DEFAULT 0,
                ""Durability"" INTEGER DEFAULT 100,
                ""Count"" INTEGER NOT NULL DEFAULT 1,
                ""LastUsed"" TEXT,
                FOREIGN KEY(""OwnerID"") REFERENCES ""Characters""(""CharacterID""),
                FOREIGN KEY(""TemplateID"") REFERENCES ""ItemTemplates""(""TemplateID"")
            );
        ");

        // ItemAttributes
        ExecuteNonQuery(conn, @"
            CREATE TABLE IF NOT EXISTS ""ItemAttributes"" (
                ""AttributeID"" INTEGER PRIMARY KEY AUTOINCREMENT,
                ""InstanceID"" INTEGER NOT NULL,
                ""AttrType"" INTEGER NOT NULL,
                ""AttrValue"" INTEGER NOT NULL,
                FOREIGN KEY(""InstanceID"") REFERENCES ""ItemInstances""(""InstanceID"") ON DELETE CASCADE
            );
        ");

        // LootTables
        ExecuteNonQuery(conn, @"
            CREATE TABLE IF NOT EXISTS ""LootTables"" (
                ""LootID"" INTEGER PRIMARY KEY AUTOINCREMENT,
                ""EnemyID"" INTEGER,
                ""TemplateID"" INTEGER,
                ""DropRate"" REAL,
                ""MinLevel"" INTEGER DEFAULT 0,
                FOREIGN KEY(""EnemyID"") REFERENCES ""Enemies""(""EnemyID""),
                FOREIGN KEY(""TemplateID"") REFERENCES ""ItemTemplates""(""TemplateID"")
            );
        ");

        // Shops
        ExecuteNonQuery(conn, @"
            CREATE TABLE IF NOT EXISTS ""Shops"" (
                ""ShopID"" INTEGER PRIMARY KEY AUTOINCREMENT,
                ""Name"" TEXT NOT NULL,
                ""NpcType"" INTEGER NOT NULL
            );
        ");

        // ShopItems
        ExecuteNonQuery(conn, @"
            CREATE TABLE IF NOT EXISTS ""ShopItems"" (
                ""ShopItemID"" INTEGER PRIMARY KEY AUTOINCREMENT,
                ""ShopID"" INTEGER NOT NULL DEFAULT 1,
                ""TemplateID"" INTEGER NOT NULL,
                ""Price"" INTEGER NOT NULL,
                FOREIGN KEY(""ShopID"") REFERENCES ""Shops""(""ShopID""),
                FOREIGN KEY(""TemplateID"" ) REFERENCES ""ItemTemplates""(""TemplateID"")
            );
        ");

        // CharacterZoneData
        ExecuteNonQuery(conn, @"
            CREATE TABLE IF NOT EXISTS ""CharacterZoneData"" (
                ""ID"" INTEGER PRIMARY KEY AUTOINCREMENT,
                ""CharacterID"" INTEGER NOT NULL,
                ""ZoneID"" INTEGER NOT NULL,
                ""ProgressEasy"" INTEGER NOT NULL DEFAULT 0,
                ""ProgressNormal"" INTEGER NOT NULL DEFAULT 0,
                ""ProgressHard"" INTEGER NOT NULL DEFAULT 0,
                ""BossKilledEasy"" INTEGER NOT NULL DEFAULT 0,
                ""BossKilledNormal"" INTEGER NOT NULL DEFAULT 0,
                ""BossKilledHard"" INTEGER NOT NULL DEFAULT 0,
                FOREIGN KEY(""CharacterID"") REFERENCES ""Characters""(""CharacterID""),
                FOREIGN KEY(""ZoneID"") REFERENCES ""Zones""(""ZoneID"")
            );
        ");

        // Skills
        ExecuteNonQuery(conn, @"
            CREATE TABLE IF NOT EXISTS ""Skills"" (
                ""SkillID"" INTEGER PRIMARY KEY AUTOINCREMENT,
                ""Name"" TEXT NOT NULL,
                ""Description"" TEXT,
                ""Class"" INTEGER NOT NULL,
                ""Type"" INTEGER NOT NULL,
                ""MaxLevel"" INTEGER DEFAULT 5,
                ""RequiredLevel"" INTEGER DEFAULT 1,
                ""Cooldown"" REAL DEFAULT 0,
                ""ManaCost"" INTEGER DEFAULT 0,
                ""EffectType"" INTEGER DEFAULT 0,
                ""BaseEffectValue"" REAL DEFAULT 0,
                ""EffectScaling"" REAL DEFAULT 0,
                ""Duration"" REAL DEFAULT 0,
                ""IconPath"" TEXT,
                ""RowIndex"" INTEGER DEFAULT 0,
                ""ColIndex"" INTEGER DEFAULT 0,
                ""PassiveStatType"" INTEGER DEFAULT 0,
                ""SecondaryEffect"" INTEGER DEFAULT 0,
                ""SecondaryEffectValue"" REAL DEFAULT 0,
                ""SecondaryEffectDuration"" REAL DEFAULT 0
            );
        ");

        // SkillDependencies
        ExecuteNonQuery(conn, @"
            CREATE TABLE IF NOT EXISTS ""SkillDependencies"" (
                ""SkillID"" INTEGER NOT NULL,
                ""PrerequisiteSkillID"" INTEGER NOT NULL,
                PRIMARY KEY(""SkillID"",""PrerequisiteSkillID""),
                FOREIGN KEY(""PrerequisiteSkillID"") REFERENCES ""Skills""(""SkillID""),
                FOREIGN KEY(""SkillID"") REFERENCES ""Skills""(""SkillID"")
            );
        ");

        // CharacterSkills
        ExecuteNonQuery(conn, @"
            CREATE TABLE IF NOT EXISTS ""CharacterSkills"" (
                ""CharacterID"" INTEGER NOT NULL,
                ""SkillID"" INTEGER NOT NULL,
                ""CurrentLevel"" INTEGER DEFAULT 0,
                PRIMARY KEY(""CharacterID"",""SkillID""),
                FOREIGN KEY(""CharacterID"") REFERENCES ""Characters""(""CharacterID""),
                FOREIGN KEY(""SkillID"") REFERENCES ""Skills""(""SkillID"")
            );
        ");

        // HotbarSettings
        ExecuteNonQuery(conn, @"
            CREATE TABLE IF NOT EXISTS ""HotbarSettings"" (
                ""CharacterID"" INTEGER NOT NULL,
                ""SlotIndex"" INTEGER NOT NULL,
                ""ItemInstanceID"" INTEGER,
                ""Type"" INTEGER DEFAULT 0,
                ""ReferenceID"" INTEGER,
                PRIMARY KEY(""CharacterID"",""SlotIndex""),
                FOREIGN KEY(""CharacterID"") REFERENCES ""Characters""(""CharacterID"") ON DELETE CASCADE
            );
        ");
    }

    private static void SeedData(SqliteConnection conn)
    {
        // Only seed if Enemies table is empty to avoid duplication
        long count = 0;
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT COUNT(*) FROM Enemies";
            count = (long)cmd.ExecuteScalar();
        }

        if (count > 0) return;

        // Use a transaction for fast bulk insert
        using (var transaction = conn.BeginTransaction())
        {
            var cmd = conn.CreateCommand();
            cmd.Transaction = transaction;

            try
            {
                // Enemies
                cmd.CommandText = @"
                    INSERT INTO ""Enemies"" (""EnemyID"",""Name"",""Level"",""MaxHP"",""Damage"",""ExpReward"",""GoldReward"",""SpritePath"",""IsBoss"") VALUES (1,'Spider',1,500,50,50,500,'',0);
                    INSERT INTO ""Enemies"" (""EnemyID"",""Name"",""Level"",""MaxHP"",""Damage"",""ExpReward"",""GoldReward"",""SpritePath"",""IsBoss"") VALUES (2,'Wild Wolf',5,750,75,100,1000,'',0);
                    INSERT INTO ""Enemies"" (""EnemyID"",""Name"",""Level"",""MaxHP"",""Damage"",""ExpReward"",""GoldReward"",""SpritePath"",""IsBoss"") VALUES (3,'Dark Archer',8,350,120,120,1000,'RANGED',0);
                    INSERT INTO ""Enemies"" (""EnemyID"",""Name"",""Level"",""MaxHP"",""Damage"",""ExpReward"",""GoldReward"",""SpritePath"",""IsBoss"") VALUES (4,'Bera',10,2000,200,1500,10000,'',1);
                    INSERT INTO ""Enemies"" (""EnemyID"",""Name"",""Level"",""MaxHP"",""Damage"",""ExpReward"",""GoldReward"",""SpritePath"",""IsBoss"") VALUES (5,'Mystic Soldier',12,1500,150,150,1500,'',0);
                    INSERT INTO ""Enemies"" (""EnemyID"",""Name"",""Level"",""MaxHP"",""Damage"",""ExpReward"",""GoldReward"",""SpritePath"",""IsBoss"") VALUES (6,'Evil Eye',15,750,200,200,2000,'RANGED',0);
                    INSERT INTO ""Enemies"" (""EnemyID"",""Name"",""Level"",""MaxHP"",""Damage"",""ExpReward"",""GoldReward"",""SpritePath"",""IsBoss"") VALUES (7,'Stone Golem',18,1500,200,250,2000,'',0);
                    INSERT INTO ""Enemies"" (""EnemyID"",""Name"",""Level"",""MaxHP"",""Damage"",""ExpReward"",""GoldReward"",""SpritePath"",""IsBoss"") VALUES (8,'Flame King',20,5000,500,5000,50000,'',1);
                ";
                cmd.ExecuteNonQuery();

                // ItemTemplates
                cmd.CommandText = @"
                    INSERT INTO ""ItemTemplates"" (""TemplateID"",""Name"",""ItemType"",""BaseMinDamage"",""BaseMaxDamage"",""BaseDefense"",""BaseAttackSpeed"",""ReqLevel"",""ReqClass"",""MaxStack"",""EffectType"",""EffectValue"",""Cooldown"",""BaseMinMagicDamage"",""BaseMaxMagicDamage"",""Price"",""SellPrice"",""IsStackable"") VALUES (1,'Rusty Sword',1,50,75,0,1.0,1,1,1,0,0,0,0,0,100,50,0);
                    INSERT INTO ""ItemTemplates"" (""TemplateID"",""Name"",""ItemType"",""BaseMinDamage"",""BaseMaxDamage"",""BaseDefense"",""BaseAttackSpeed"",""ReqLevel"",""ReqClass"",""MaxStack"",""EffectType"",""EffectValue"",""Cooldown"",""BaseMinMagicDamage"",""BaseMaxMagicDamage"",""Price"",""SellPrice"",""IsStackable"") VALUES (2,'Leather Armor',2,0,0,30,0.0,1,0,1,0,0,0,0,0,250,125,0);
                    INSERT INTO ""ItemTemplates"" (""TemplateID"",""Name"",""ItemType"",""BaseMinDamage"",""BaseMaxDamage"",""BaseDefense"",""BaseAttackSpeed"",""ReqLevel"",""ReqClass"",""MaxStack"",""EffectType"",""EffectValue"",""Cooldown"",""BaseMinMagicDamage"",""BaseMaxMagicDamage"",""Price"",""SellPrice"",""IsStackable"") VALUES (3,'Wooden Staff',1,0,0,0,1.0,1,3,1,0,0,0,75,110,100,50,0);
                    INSERT INTO ""ItemTemplates"" (""TemplateID"",""Name"",""ItemType"",""BaseMinDamage"",""BaseMaxDamage"",""BaseDefense"",""BaseAttackSpeed"",""ReqLevel"",""ReqClass"",""MaxStack"",""EffectType"",""EffectValue"",""Cooldown"",""BaseMinMagicDamage"",""BaseMaxMagicDamage"",""Price"",""SellPrice"",""IsStackable"") VALUES (4,'Broken Dagger',1,20,25,0,0.5,1,2,1,0,0,0,20,25,100,50,0);
                    INSERT INTO ""ItemTemplates"" (""TemplateID"",""Name"",""ItemType"",""BaseMinDamage"",""BaseMaxDamage"",""BaseDefense"",""BaseAttackSpeed"",""ReqLevel"",""ReqClass"",""MaxStack"",""EffectType"",""EffectValue"",""Cooldown"",""BaseMinMagicDamage"",""BaseMaxMagicDamage"",""Price"",""SellPrice"",""IsStackable"") VALUES (5,'Small Health Potion',3,0,0,0,0.0,1,0,100,1,100,5,0,0,10,5,1);
                    INSERT INTO ""ItemTemplates"" (""TemplateID"",""Name"",""ItemType"",""BaseMinDamage"",""BaseMaxDamage"",""BaseDefense"",""BaseAttackSpeed"",""ReqLevel"",""ReqClass"",""MaxStack"",""EffectType"",""EffectValue"",""Cooldown"",""BaseMinMagicDamage"",""BaseMaxMagicDamage"",""Price"",""SellPrice"",""IsStackable"") VALUES (6,'Small Mana Potion',3,0,0,0,0.0,1,0,100,2,50,5,0,0,10,5,1);
                    INSERT INTO ""ItemTemplates"" (""TemplateID"",""Name"",""ItemType"",""BaseMinDamage"",""BaseMaxDamage"",""BaseDefense"",""BaseAttackSpeed"",""ReqLevel"",""ReqClass"",""MaxStack"",""EffectType"",""EffectValue"",""Cooldown"",""BaseMinMagicDamage"",""BaseMaxMagicDamage"",""Price"",""SellPrice"",""IsStackable"") VALUES (7,'Blacksmith Scroll',5,0,0,0,0.0,1,0,500,0,0,0,0,0,10,5,1);
                    INSERT INTO ""ItemTemplates"" (""TemplateID"",""Name"",""ItemType"",""BaseMinDamage"",""BaseMaxDamage"",""BaseDefense"",""BaseAttackSpeed"",""ReqLevel"",""ReqClass"",""MaxStack"",""EffectType"",""EffectValue"",""Cooldown"",""BaseMinMagicDamage"",""BaseMaxMagicDamage"",""Price"",""SellPrice"",""IsStackable"") VALUES (8,'Clover Leaf',5,0,0,0,0.0,1,0,100,0,10,0,0,0,10,5,1);
                    INSERT INTO ""ItemTemplates"" (""TemplateID"",""Name"",""ItemType"",""BaseMinDamage"",""BaseMaxDamage"",""BaseDefense"",""BaseAttackSpeed"",""ReqLevel"",""ReqClass"",""MaxStack"",""EffectType"",""EffectValue"",""Cooldown"",""BaseMinMagicDamage"",""BaseMaxMagicDamage"",""Price"",""SellPrice"",""IsStackable"") VALUES (9,'Blessing Scroll',5,0,0,0,0.0,1,0,100,0,30,0,0,0,10,5,1);
                    INSERT INTO ""ItemTemplates"" (""TemplateID"",""Name"",""ItemType"",""BaseMinDamage"",""BaseMaxDamage"",""BaseDefense"",""BaseAttackSpeed"",""ReqLevel"",""ReqClass"",""MaxStack"",""EffectType"",""EffectValue"",""Cooldown"",""BaseMinMagicDamage"",""BaseMaxMagicDamage"",""Price"",""SellPrice"",""IsStackable"") VALUES (10,'Dragon God Scroll',5,0,0,0,0.0,1,0,100,0,50,0,0,0,10,5,1);
                    INSERT INTO ""ItemTemplates"" (""TemplateID"",""Name"",""ItemType"",""BaseMinDamage"",""BaseMaxDamage"",""BaseDefense"",""BaseAttackSpeed"",""ReqLevel"",""ReqClass"",""MaxStack"",""EffectType"",""EffectValue"",""Cooldown"",""BaseMinMagicDamage"",""BaseMaxMagicDamage"",""Price"",""SellPrice"",""IsStackable"") VALUES (11,'Magic Stone',5,0,0,0,0.0,1,0,100,0,100,0,0,0,10,5,1);
                    INSERT INTO ""ItemTemplates"" (""TemplateID"",""Name"",""ItemType"",""BaseMinDamage"",""BaseMaxDamage"",""BaseDefense"",""BaseAttackSpeed"",""ReqLevel"",""ReqClass"",""MaxStack"",""EffectType"",""EffectValue"",""Cooldown"",""BaseMinMagicDamage"",""BaseMaxMagicDamage"",""Price"",""SellPrice"",""IsStackable"") VALUES (12,'Blessing Marble',6,0,0,0,0.0,1,0,100,0,0,0,0,0,10,5,1);
                    INSERT INTO ""ItemTemplates"" (""TemplateID"",""Name"",""ItemType"",""BaseMinDamage"",""BaseMaxDamage"",""BaseDefense"",""BaseAttackSpeed"",""ReqLevel"",""ReqClass"",""MaxStack"",""EffectType"",""EffectValue"",""Cooldown"",""BaseMinMagicDamage"",""BaseMaxMagicDamage"",""Price"",""SellPrice"",""IsStackable"") VALUES (13,'Enchant Item',7,0,0,0,0.0,1,0,100,0,0,0,0,0,10,5,1);
                ";
                cmd.ExecuteNonQuery();

                // LootTables (References Enemies and ItemTemplates)
                cmd.CommandText = @"
                    INSERT INTO ""LootTables"" (""LootID"",""EnemyID"",""TemplateID"",""DropRate"",""MinLevel"") VALUES (1,1,1,30.0,1);
                    INSERT INTO ""LootTables"" (""LootID"",""EnemyID"",""TemplateID"",""DropRate"",""MinLevel"") VALUES (2,1,2,30.0,1);
                    INSERT INTO ""LootTables"" (""LootID"",""EnemyID"",""TemplateID"",""DropRate"",""MinLevel"") VALUES (3,1,3,30.0,1);
                    INSERT INTO ""LootTables"" (""LootID"",""EnemyID"",""TemplateID"",""DropRate"",""MinLevel"") VALUES (4,1,4,30.0,1);
                    INSERT INTO ""LootTables"" (""LootID"",""EnemyID"",""TemplateID"",""DropRate"",""MinLevel"") VALUES (5,2,1,30.0,1);
                    INSERT INTO ""LootTables"" (""LootID"",""EnemyID"",""TemplateID"",""DropRate"",""MinLevel"") VALUES (6,2,2,30.0,1);
                    INSERT INTO ""LootTables"" (""LootID"",""EnemyID"",""TemplateID"",""DropRate"",""MinLevel"") VALUES (7,2,3,30.0,1);
                    INSERT INTO ""LootTables"" (""LootID"",""EnemyID"",""TemplateID"",""DropRate"",""MinLevel"") VALUES (8,2,4,30.0,1);
                    INSERT INTO ""LootTables"" (""LootID"",""EnemyID"",""TemplateID"",""DropRate"",""MinLevel"") VALUES (9,3,1,30.0,1);
                    INSERT INTO ""LootTables"" (""LootID"",""EnemyID"",""TemplateID"",""DropRate"",""MinLevel"") VALUES (10,3,2,30.0,1);
                    INSERT INTO ""LootTables"" (""LootID"",""EnemyID"",""TemplateID"",""DropRate"",""MinLevel"") VALUES (11,3,3,30.0,1);
                    INSERT INTO ""LootTables"" (""LootID"",""EnemyID"",""TemplateID"",""DropRate"",""MinLevel"") VALUES (12,3,4,30.0,1);
                    INSERT INTO ""LootTables"" (""LootID"",""EnemyID"",""TemplateID"",""DropRate"",""MinLevel"") VALUES (13,4,1,50.0,1);
                    INSERT INTO ""LootTables"" (""LootID"",""EnemyID"",""TemplateID"",""DropRate"",""MinLevel"") VALUES (14,4,2,50.0,1);
                    INSERT INTO ""LootTables"" (""LootID"",""EnemyID"",""TemplateID"",""DropRate"",""MinLevel"") VALUES (15,4,3,50.0,1);
                    INSERT INTO ""LootTables"" (""LootID"",""EnemyID"",""TemplateID"",""DropRate"",""MinLevel"") VALUES (16,4,4,50.0,1);
                ";
                cmd.ExecuteNonQuery();

                // Shops (Moved UP to exist before ShopItems)
                cmd.CommandText = @"
                    INSERT INTO ""Shops"" (""ShopID"",""Name"",""NpcType"") VALUES (1,'Merchant',1);
                ";
                cmd.ExecuteNonQuery();

                // ShopItems (References Shops)
                cmd.CommandText = @"
                    INSERT INTO ""ShopItems"" (""ShopItemID"",""ShopID"",""TemplateID"",""Price"") VALUES (1,1,1,500);
                    INSERT INTO ""ShopItems"" (""ShopItemID"",""ShopID"",""TemplateID"",""Price"") VALUES (2,1,2,500);
                    INSERT INTO ""ShopItems"" (""ShopItemID"",""ShopID"",""TemplateID"",""Price"") VALUES (3,1,3,500);
                    INSERT INTO ""ShopItems"" (""ShopItemID"",""ShopID"",""TemplateID"",""Price"") VALUES (4,1,4,500);
                    INSERT INTO ""ShopItems"" (""ShopItemID"",""ShopID"",""TemplateID"",""Price"") VALUES (5,1,5,10);
                    INSERT INTO ""ShopItems"" (""ShopItemID"",""ShopID"",""TemplateID"",""Price"") VALUES (6,1,6,10);
                    INSERT INTO ""ShopItems"" (""ShopItemID"",""ShopID"",""TemplateID"",""Price"") VALUES (7,1,7,10);
                    INSERT INTO ""ShopItems"" (""ShopItemID"",""ShopID"",""TemplateID"",""Price"") VALUES (12,1,8,10);
                    INSERT INTO ""ShopItems"" (""ShopItemID"",""ShopID"",""TemplateID"",""Price"") VALUES (13,1,12,10);
                    INSERT INTO ""ShopItems"" (""ShopItemID"",""ShopID"",""TemplateID"",""Price"") VALUES (14,1,13,10);
                ";
                cmd.ExecuteNonQuery();

                // Skills
                cmd.CommandText = @"
                    INSERT INTO ""Skills"" (""SkillID"",""Name"",""Description"",""Class"",""Type"",""MaxLevel"",""RequiredLevel"",""Cooldown"",""ManaCost"",""EffectType"",""BaseEffectValue"",""EffectScaling"",""Duration"",""IconPath"",""RowIndex"",""ColIndex"",""PassiveStatType"",""SecondaryEffect"",""SecondaryEffectValue"",""SecondaryEffectDuration"") VALUES (1,'Heavy Slash','A powerful sword strike. Deals heavy physical damage.',1,0,5,1,3.0,10,0,150.0,80.0,0.0,'slash_icon',0,0,0,0,0.0,0.0);
                    INSERT INTO ""Skills"" (""SkillID"",""Name"",""Description"",""Class"",""Type"",""MaxLevel"",""RequiredLevel"",""Cooldown"",""ManaCost"",""EffectType"",""BaseEffectValue"",""EffectScaling"",""Duration"",""IconPath"",""RowIndex"",""ColIndex"",""PassiveStatType"",""SecondaryEffect"",""SecondaryEffectValue"",""SecondaryEffectDuration"") VALUES (2,'Iron Skin','Hardens body to increase defense. +DEF',1,1,5,1,0.0,0,4,30.0,20.0,0.0,'shield_icon',0,-1,1,0,0.0,0.0);
                    INSERT INTO ""Skills"" (""SkillID"",""Name"",""Description"",""Class"",""Type"",""MaxLevel"",""RequiredLevel"",""Cooldown"",""ManaCost"",""EffectType"",""BaseEffectValue"",""EffectScaling"",""Duration"",""IconPath"",""RowIndex"",""ColIndex"",""PassiveStatType"",""SecondaryEffect"",""SecondaryEffectValue"",""SecondaryEffectDuration"") VALUES (3,'Rage','Channels inner rage into attack. +ATK',1,1,5,1,0.0,0,4,30.0,30.0,0.0,'rage_icon',0,1,2,0,0.0,0.0);
                    INSERT INTO ""Skills"" (""SkillID"",""Name"",""Description"",""Class"",""Type"",""MaxLevel"",""RequiredLevel"",""Cooldown"",""ManaCost"",""EffectType"",""BaseEffectValue"",""EffectScaling"",""Duration"",""IconPath"",""RowIndex"",""ColIndex"",""PassiveStatType"",""SecondaryEffect"",""SecondaryEffectValue"",""SecondaryEffectDuration"") VALUES (4,'Whirlwind','Spins to attack all surrounding enemies.',1,0,5,5,8.0,25,0,200.0,120.0,0.0,'whirlwind_icon',1,0,0,0,0.0,0.0);
                    INSERT INTO ""Skills"" (""SkillID"",""Name"",""Description"",""Class"",""Type"",""MaxLevel"",""RequiredLevel"",""Cooldown"",""ManaCost"",""EffectType"",""BaseEffectValue"",""EffectScaling"",""Duration"",""IconPath"",""RowIndex"",""ColIndex"",""PassiveStatType"",""SecondaryEffect"",""SecondaryEffectValue"",""SecondaryEffectDuration"") VALUES (5,'Shield Wall','Increases block chance. +BLOCK%',1,1,5,5,0.0,0,4,5.0,3.0,0.0,'block_icon',1,-1,1,0,0.0,0.0);
                    INSERT INTO ""Skills"" (""SkillID"",""Name"",""Description"",""Class"",""Type"",""MaxLevel"",""RequiredLevel"",""Cooldown"",""ManaCost"",""EffectType"",""BaseEffectValue"",""EffectScaling"",""Duration"",""IconPath"",""RowIndex"",""ColIndex"",""PassiveStatType"",""SecondaryEffect"",""SecondaryEffectValue"",""SecondaryEffectDuration"") VALUES (6,'Battle Frenzy','Increases attack speed. +ATK SPD',1,1,5,5,0.0,0,4,5.0,3.0,0.0,'fury_icon',1,1,7,0,0.0,0.0);
                    INSERT INTO ""Skills"" (""SkillID"",""Name"",""Description"",""Class"",""Type"",""MaxLevel"",""RequiredLevel"",""Cooldown"",""ManaCost"",""EffectType"",""BaseEffectValue"",""EffectScaling"",""Duration"",""IconPath"",""RowIndex"",""ColIndex"",""PassiveStatType"",""SecondaryEffect"",""SecondaryEffectValue"",""SecondaryEffectDuration"") VALUES (7,'Crushing Blow','Crushes the enemy causing bleeding. DoT.',1,0,5,10,6.0,30,0,200.0,100.0,0.0,'crush_icon',2,0,0,7,5.0,3.0);
                    INSERT INTO ""Skills"" (""SkillID"",""Name"",""Description"",""Class"",""Type"",""MaxLevel"",""RequiredLevel"",""Cooldown"",""ManaCost"",""EffectType"",""BaseEffectValue"",""EffectScaling"",""Duration"",""IconPath"",""RowIndex"",""ColIndex"",""PassiveStatType"",""SecondaryEffect"",""SecondaryEffectValue"",""SecondaryEffectDuration"") VALUES (8,'Endurance','Increases maximum health. +MAX HP',1,1,5,10,0.0,0,4,150.0,150.0,0.0,'hp_icon',2,-1,4,0,0.0,0.0);
                    INSERT INTO ""Skills"" (""SkillID"",""Name"",""Description"",""Class"",""Type"",""MaxLevel"",""RequiredLevel"",""Cooldown"",""ManaCost"",""EffectType"",""BaseEffectValue"",""EffectScaling"",""Duration"",""IconPath"",""RowIndex"",""ColIndex"",""PassiveStatType"",""SecondaryEffect"",""SecondaryEffectValue"",""SecondaryEffectDuration"") VALUES (9,'Critical Mastery','Increases critical strike chance. +CRIT%',1,1,5,10,0.0,0,4,5.0,5.0,0.0,'crit_icon',2,1,6,0,0.0,0.0);
                    INSERT INTO ""Skills"" (""SkillID"",""Name"",""Description"",""Class"",""Type"",""MaxLevel"",""RequiredLevel"",""Cooldown"",""ManaCost"",""EffectType"",""BaseEffectValue"",""EffectScaling"",""Duration"",""IconPath"",""RowIndex"",""ColIndex"",""PassiveStatType"",""SecondaryEffect"",""SecondaryEffectValue"",""SecondaryEffectDuration"") VALUES (10,'Fireball','Hurls a fireball. Causes burning effect.',3,0,5,1,2.0,15,0,250.0,250.0,0.0,'fireball_icon',0,-1,0,1,3.0,3.0);
                    INSERT INTO ""Skills"" (""SkillID"",""Name"",""Description"",""Class"",""Type"",""MaxLevel"",""RequiredLevel"",""Cooldown"",""ManaCost"",""EffectType"",""BaseEffectValue"",""EffectScaling"",""Duration"",""IconPath"",""RowIndex"",""ColIndex"",""PassiveStatType"",""SecondaryEffect"",""SecondaryEffectValue"",""SecondaryEffectDuration"") VALUES (11,'Ice Bolt','Fires a freezing ice bolt. Causes slowing.',3,0,5,1,3.0,15,0,250.0,250.0,0.0,'icebolt_icon',0,0,0,2,30.0,2.0);
                    INSERT INTO ""Skills"" (""SkillID"",""Name"",""Description"",""Class"",""Type"",""MaxLevel"",""RequiredLevel"",""Cooldown"",""ManaCost"",""EffectType"",""BaseEffectValue"",""EffectScaling"",""Duration"",""IconPath"",""RowIndex"",""ColIndex"",""PassiveStatType"",""SecondaryEffect"",""SecondaryEffectValue"",""SecondaryEffectDuration"") VALUES (12,'Lightning','Strikes with fast lightning. Causes shock.',3,0,5,1,1.5,12,0,250.0,250.0,0.0,'lightning_icon',0,1,0,6,20.0,2.0);
                    INSERT INTO ""Skills"" (""SkillID"",""Name"",""Description"",""Class"",""Type"",""MaxLevel"",""RequiredLevel"",""Cooldown"",""ManaCost"",""EffectType"",""BaseEffectValue"",""EffectScaling"",""Duration"",""IconPath"",""RowIndex"",""ColIndex"",""PassiveStatType"",""SecondaryEffect"",""SecondaryEffectValue"",""SecondaryEffectDuration"") VALUES (13,'Fire Mastery','Increases fire damage. +FIRE DMG',3,1,5,5,0.0,0,4,50.0,30.0,0.0,'fire_mastery_icon',1,-1,11,0,0.0,0.0);
                    INSERT INTO ""Skills"" (""SkillID"",""Name"",""Description"",""Class"",""Type"",""MaxLevel"",""RequiredLevel"",""Cooldown"",""ManaCost"",""EffectType"",""BaseEffectValue"",""EffectScaling"",""Duration"",""IconPath"",""RowIndex"",""ColIndex"",""PassiveStatType"",""SecondaryEffect"",""SecondaryEffectValue"",""SecondaryEffectDuration"") VALUES (14,'Ice Mastery','Increases ice damage. +ICE DMG',3,1,5,5,0.0,0,4,50.0,30.0,0.0,'ice_mastery_icon',1,0,12,0,0.0,0.0);
                    INSERT INTO ""Skills"" (""SkillID"",""Name"",""Description"",""Class"",""Type"",""MaxLevel"",""RequiredLevel"",""Cooldown"",""ManaCost"",""EffectType"",""BaseEffectValue"",""EffectScaling"",""Duration"",""IconPath"",""RowIndex"",""ColIndex"",""PassiveStatType"",""SecondaryEffect"",""SecondaryEffectValue"",""SecondaryEffectDuration"") VALUES (15,'Lightning Mastery','Increases lightning damage. +LIGHTNING DMG',3,1,5,5,0.0,0,4,50.0,30.0,0.0,'lightning_mastery_icon',1,1,13,0,0.0,0.0);
                    INSERT INTO ""Skills"" (""SkillID"",""Name"",""Description"",""Class"",""Type"",""MaxLevel"",""RequiredLevel"",""Cooldown"",""ManaCost"",""EffectType"",""BaseEffectValue"",""EffectScaling"",""Duration"",""IconPath"",""RowIndex"",""ColIndex"",""PassiveStatType"",""SecondaryEffect"",""SecondaryEffectValue"",""SecondaryEffectDuration"") VALUES (16,'Meteor','Drop a meteor. Devastating damage and burning.',3,0,5,10,15.0,50,0,200.0,120.0,0.0,'meteor_icon',2,-1,0,1,8.0,5.0);
                    INSERT INTO ""Skills"" (""SkillID"",""Name"",""Description"",""Class"",""Type"",""MaxLevel"",""RequiredLevel"",""Cooldown"",""ManaCost"",""EffectType"",""BaseEffectValue"",""EffectScaling"",""Duration"",""IconPath"",""RowIndex"",""ColIndex"",""PassiveStatType"",""SecondaryEffect"",""SecondaryEffectValue"",""SecondaryEffectDuration"") VALUES (17,'Blizzard','Widely spread ice storm. Freezing effect.',3,0,5,10,12.0,45,0,200.0,120.0,0.0,'blizzard_icon',2,0,0,5,50.0,3.0);
                    INSERT INTO ""Skills"" (""SkillID"",""Name"",""Description"",""Class"",""Type"",""MaxLevel"",""RequiredLevel"",""Cooldown"",""ManaCost"",""EffectType"",""BaseEffectValue"",""EffectScaling"",""Duration"",""IconPath"",""RowIndex"",""ColIndex"",""PassiveStatType"",""SecondaryEffect"",""SecondaryEffectValue"",""SecondaryEffectDuration"") VALUES (18,'Chain Lightning','Jump lightning. Causes shock.',3,0,5,10,8.0,40,0,150.0,100.0,0.0,'chain_lightning_icon',2,1,0,6,30.0,2.0);
                    INSERT INTO ""Skills"" (""SkillID"",""Name"",""Description"",""Class"",""Type"",""MaxLevel"",""RequiredLevel"",""Cooldown"",""ManaCost"",""EffectType"",""BaseEffectValue"",""EffectScaling"",""Duration"",""IconPath"",""RowIndex"",""ColIndex"",""PassiveStatType"",""SecondaryEffect"",""SecondaryEffectValue"",""SecondaryEffectDuration"") VALUES (19,'Mana Pool','Increases maximum mana. +MAX MANA',3,1,5,1,0.0,0,4,150.0,150.0,0.0,'mana_icon',3,0,5,0,0.0,0.0);
                    INSERT INTO ""Skills"" (""SkillID"",""Name"",""Description"",""Class"",""Type"",""MaxLevel"",""RequiredLevel"",""Cooldown"",""ManaCost"",""EffectType"",""BaseEffectValue"",""EffectScaling"",""Duration"",""IconPath"",""RowIndex"",""ColIndex"",""PassiveStatType"",""SecondaryEffect"",""SecondaryEffectValue"",""SecondaryEffectDuration"") VALUES (20,'Arcane Power','Increases magic attack. +MAGIC ATK',3,1,5,5,0.0,0,4,50.0,50.0,0.0,'arcane_icon',3,1,3,0,0.0,0.0);
                    INSERT INTO ""Skills"" (""SkillID"",""Name"",""Description"",""Class"",""Type"",""MaxLevel"",""RequiredLevel"",""Cooldown"",""ManaCost"",""EffectType"",""BaseEffectValue"",""EffectScaling"",""Duration"",""IconPath"",""RowIndex"",""ColIndex"",""PassiveStatType"",""SecondaryEffect"",""SecondaryEffectValue"",""SecondaryEffectDuration"") VALUES (21,'Backstab','Powerful strike from behind.',2,0,5,1,4.0,15,0,200.0,150.0,0.0,'backstab_icon',0,0,0,0,0.0,0.0);
                    INSERT INTO ""Skills"" (""SkillID"",""Name"",""Description"",""Class"",""Type"",""MaxLevel"",""RequiredLevel"",""Cooldown"",""ManaCost"",""EffectType"",""BaseEffectValue"",""EffectScaling"",""Duration"",""IconPath"",""RowIndex"",""ColIndex"",""PassiveStatType"",""SecondaryEffect"",""SecondaryEffectValue"",""SecondaryEffectDuration"") VALUES (22,'Poisoned Blade','Increases poison damage. +POISON DMG',2,1,5,1,0.0,0,4,6.0,3.0,0.0,'poison_icon',0,-1,14,0,0.0,0.0);
                    INSERT INTO ""Skills"" (""SkillID"",""Name"",""Description"",""Class"",""Type"",""MaxLevel"",""RequiredLevel"",""Cooldown"",""ManaCost"",""EffectType"",""BaseEffectValue"",""EffectScaling"",""Duration"",""IconPath"",""RowIndex"",""ColIndex"",""PassiveStatType"",""SecondaryEffect"",""SecondaryEffectValue"",""SecondaryEffectDuration"") VALUES (23,'Agility','Increases attack speed. +ATK SPD',2,1,5,1,0.0,0,4,8.0,4.0,0.0,'agility_icon',0,1,7,0,0.0,0.0);
                    INSERT INTO ""Skills"" (""SkillID"",""Name"",""Description"",""Class"",""Type"",""MaxLevel"",""RequiredLevel"",""Cooldown"",""ManaCost"",""EffectType"",""BaseEffectValue"",""EffectScaling"",""Duration"",""IconPath"",""RowIndex"",""ColIndex"",""PassiveStatType"",""SecondaryEffect"",""SecondaryEffectValue"",""SecondaryEffectDuration"") VALUES (24,'Envenom','Poisons the enemy. Damage over time.',2,0,5,5,5.0,20,0,50.0,25.0,0.0,'envenom_icon',1,-1,0,4,8.0,5.0);
                    INSERT INTO ""Skills"" (""SkillID"",""Name"",""Description"",""Class"",""Type"",""MaxLevel"",""RequiredLevel"",""Cooldown"",""ManaCost"",""EffectType"",""BaseEffectValue"",""EffectScaling"",""Duration"",""IconPath"",""RowIndex"",""ColIndex"",""PassiveStatType"",""SecondaryEffect"",""SecondaryEffectValue"",""SecondaryEffectDuration"") VALUES (25,'Double Strike','Two consecutive quick attacks.',2,0,5,5,3.0,18,0,120.0,80.0,0.0,'dual_strike_icon',1,0,0,0,0.0,0.0);
                    INSERT INTO ""Skills"" (""SkillID"",""Name"",""Description"",""Class"",""Type"",""MaxLevel"",""RequiredLevel"",""Cooldown"",""ManaCost"",""EffectType"",""BaseEffectValue"",""EffectScaling"",""Duration"",""IconPath"",""RowIndex"",""ColIndex"",""PassiveStatType"",""SecondaryEffect"",""SecondaryEffectValue"",""SecondaryEffectDuration"") VALUES (26,'Evasion','Increases movement speed. +MOVE SPD',2,1,5,5,0.0,0,4,5.0,3.0,0.0,'evasion_icon',1,1,10,0,0.0,0.0);
                    INSERT INTO ""Skills"" (""SkillID"",""Name"",""Description"",""Class"",""Type"",""MaxLevel"",""RequiredLevel"",""Cooldown"",""ManaCost"",""EffectType"",""BaseEffectValue"",""EffectScaling"",""Duration"",""IconPath"",""RowIndex"",""ColIndex"",""PassiveStatType"",""SecondaryEffect"",""SecondaryEffectValue"",""SecondaryEffectDuration"") VALUES (27,'Mark of Death','Marks the enemy, reducing defense.',2,0,5,10,10.0,35,0,40.0,20.0,0.0,'death_mark_icon',2,-1,0,8,25.0,4.0);
                    INSERT INTO ""Skills"" (""SkillID"",""Name"",""Description"",""Class"",""Type"",""MaxLevel"",""RequiredLevel"",""Cooldown"",""ManaCost"",""EffectType"",""BaseEffectValue"",""EffectScaling"",""Duration"",""IconPath"",""RowIndex"",""ColIndex"",""PassiveStatType"",""SecondaryEffect"",""SecondaryEffectValue"",""SecondaryEffectDuration"") VALUES (28,'Critical Strike','Increases critical chance. +CRIT%',2,1,5,10,0.0,0,4,8.0,5.0,0.0,'crit_strike_icon',2,0,6,0,0.0,0.0);
                    INSERT INTO ""Skills"" (""SkillID"",""Name"",""Description"",""Class"",""Type"",""MaxLevel"",""RequiredLevel"",""Cooldown"",""ManaCost"",""EffectType"",""BaseEffectValue"",""EffectScaling"",""Duration"",""IconPath"",""RowIndex"",""ColIndex"",""PassiveStatType"",""SecondaryEffect"",""SecondaryEffectValue"",""SecondaryEffectDuration"") VALUES (29,'Life Steal','Recovers a portion of damage dealt as health.',2,1,5,10,0.0,0,4,10.0,10.0,0.0,'lifesteal_icon',2,1,15,0,0.0,0.0);
                ";
                cmd.ExecuteNonQuery();

                // SkillDependencies
                cmd.CommandText = @"
                    INSERT INTO ""SkillDependencies"" (""SkillID"",""PrerequisiteSkillID"") VALUES (4,1);
                    INSERT INTO ""SkillDependencies"" (""SkillID"",""PrerequisiteSkillID"") VALUES (5,2);
                    INSERT INTO ""SkillDependencies"" (""SkillID"",""PrerequisiteSkillID"") VALUES (6,3);
                    INSERT INTO ""SkillDependencies"" (""SkillID"",""PrerequisiteSkillID"") VALUES (7,4);
                    INSERT INTO ""SkillDependencies"" (""SkillID"",""PrerequisiteSkillID"") VALUES (8,5);
                    INSERT INTO ""SkillDependencies"" (""SkillID"",""PrerequisiteSkillID"") VALUES (9,6);
                    INSERT INTO ""SkillDependencies"" (""SkillID"",""PrerequisiteSkillID"") VALUES (13,10);
                    INSERT INTO ""SkillDependencies"" (""SkillID"",""PrerequisiteSkillID"") VALUES (14,11);
                    INSERT INTO ""SkillDependencies"" (""SkillID"",""PrerequisiteSkillID"") VALUES (15,12);
                    INSERT INTO ""SkillDependencies"" (""SkillID"",""PrerequisiteSkillID"") VALUES (16,13);
                    INSERT INTO ""SkillDependencies"" (""SkillID"",""PrerequisiteSkillID"") VALUES (17,14);
                    INSERT INTO ""SkillDependencies"" (""SkillID"",""PrerequisiteSkillID"") VALUES (18,15);
                    INSERT INTO ""SkillDependencies"" (""SkillID"",""PrerequisiteSkillID"") VALUES (24,22);
                    INSERT INTO ""SkillDependencies"" (""SkillID"",""PrerequisiteSkillID"") VALUES (25,21);
                    INSERT INTO ""SkillDependencies"" (""SkillID"",""PrerequisiteSkillID"") VALUES (26,23);
                    INSERT INTO ""SkillDependencies"" (""SkillID"",""PrerequisiteSkillID"") VALUES (27,24);
                    INSERT INTO ""SkillDependencies"" (""SkillID"",""PrerequisiteSkillID"") VALUES (28,25);
                    INSERT INTO ""SkillDependencies"" (""SkillID"",""PrerequisiteSkillID"") VALUES (29,26);
                ";
                cmd.ExecuteNonQuery();

                // Zones
                cmd.CommandText = @"
                    INSERT INTO ""Zones"" (""ZoneID"",""Name"",""Description"",""MinLevel"",""OrderIndex"") VALUES (1,'Dark Forest','Home of spiders and wolves.',1,1);
                    INSERT INTO ""Zones"" (""ZoneID"",""Name"",""Description"",""MinLevel"",""OrderIndex"") VALUES (2,'Crystal Cave','Magical creatures and golems.',10,2);
                ";
                cmd.ExecuteNonQuery();

                // ZoneEnemies
                cmd.CommandText = @"
                    INSERT INTO ""ZoneEnemies"" (""ZoneID"",""EnemyID"",""SpawnRate"") VALUES (1,1,50);
                    INSERT INTO ""ZoneEnemies"" (""ZoneID"",""EnemyID"",""SpawnRate"") VALUES (1,2,50);
                    INSERT INTO ""ZoneEnemies"" (""ZoneID"",""EnemyID"",""SpawnRate"") VALUES (1,3,50);
                    INSERT INTO ""ZoneEnemies"" (""ZoneID"",""EnemyID"",""SpawnRate"") VALUES (1,4,100);
                    INSERT INTO ""ZoneEnemies"" (""ZoneID"",""EnemyID"",""SpawnRate"") VALUES (2,5,50);
                    INSERT INTO ""ZoneEnemies"" (""ZoneID"",""EnemyID"",""SpawnRate"") VALUES (2,6,50);
                    INSERT INTO ""ZoneEnemies"" (""ZoneID"",""EnemyID"",""SpawnRate"") VALUES (2,7,50);
                    INSERT INTO ""ZoneEnemies"" (""ZoneID"",""EnemyID"",""SpawnRate"") VALUES (2,8,100);
                ";
                cmd.ExecuteNonQuery();

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }
}
