using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using rpg_deneme.Core;
using rpg_deneme.Models;

namespace rpg_deneme.Data;

public class InventoryRepository
{
    public List<ItemInstance> GetCharacterInventory(int characterId)
    {
        List<ItemInstance> items = new List<ItemInstance>();
        using (SqliteConnection conn = DatabaseHelper.GetConnection())
        {
            string sql = "\r\n                    SELECT \r\n                        I.InstanceID, I.TemplateID, I.SlotIndex, I.Grade, I.Location, I.Count, I.LastUsed, \r\n                        I.UpgradeLevel, I.OwnerID,\r\n                        T.Name, T.BaseMinDamage, T.BaseMaxDamage, \r\n                        T.EffectType, T.EffectValue, T.Cooldown, \r\n                        T.ItemType, T.BaseMinMagicDamage, T.BaseMaxMagicDamage, T.BaseDefense,\r\n                        T.ReqClass, T.IsStackable, T.MaxStack \r\n                    FROM ItemInstances I\r\n                    INNER JOIN ItemTemplates T ON I.TemplateID = T.TemplateID\r\n                    WHERE I.OwnerID = @charId";
            using SqliteCommand cmd = new SqliteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@charId", characterId);
            conn.Open();
            using SqliteDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                items.Add(new ItemInstance
                {
                    InstanceID = Convert.ToInt64(dr["InstanceID"]),
                    TemplateID = Convert.ToInt32(dr["TemplateID"]),
                    OwnerID = Convert.ToInt32(dr["OwnerID"]),
                    SlotIndex = Convert.ToInt32(dr["SlotIndex"]),
                    Grade = (Enums.ItemGrade)Convert.ToByte(dr["Grade"]),
                    Location = (Enums.ItemLocation)Convert.ToByte(dr["Location"]),
                    Name = dr["Name"]?.ToString(),
                    MinDamage = ((dr["BaseMinDamage"] != DBNull.Value) ? Convert.ToInt32(dr["BaseMinDamage"]) : 0),
                    MaxDamage = ((dr["BaseMaxDamage"] != DBNull.Value) ? Convert.ToInt32(dr["BaseMaxDamage"]) : 0),
                    BaseDefense = ((dr["BaseDefense"] != DBNull.Value) ? Convert.ToInt32(dr["BaseDefense"]) : 0),
                    MinMagicDamage = ((dr["BaseMinMagicDamage"] != DBNull.Value) ? Convert.ToInt32(dr["BaseMinMagicDamage"]) : 0),
                    MaxMagicDamage = ((dr["BaseMaxMagicDamage"] != DBNull.Value) ? Convert.ToInt32(dr["BaseMaxMagicDamage"]) : 0),
                    Count = ((dr["Count"] == DBNull.Value) ? 1 : Convert.ToInt32(dr["Count"])),
                    EffectType = (Enums.ItemEffectType)((dr["EffectType"] != DBNull.Value) ? Convert.ToByte(dr["EffectType"]) : 0),
                    EffectValue = ((dr["EffectValue"] != DBNull.Value) ? Convert.ToInt32(dr["EffectValue"]) : 0),
                    Cooldown = ((dr["Cooldown"] != DBNull.Value) ? Convert.ToInt32(dr["Cooldown"]) : 0),
                    LastUsed = ((dr["LastUsed"] == DBNull.Value) ? ((DateTime?)null) : new DateTime?(Convert.ToDateTime(dr["LastUsed"]))),
                    ItemType = (Enums.ItemType)Convert.ToByte(dr["ItemType"]),
                    UpgradeLevel = ((dr["UpgradeLevel"] != DBNull.Value) ? Convert.ToInt32(dr["UpgradeLevel"]) : 0),
                    AllowedClass = ((dr["ReqClass"] == DBNull.Value) ? ((byte?)null) : new byte?(Convert.ToByte(dr["ReqClass"]))),
                    IsStackable = (dr["IsStackable"] != DBNull.Value && Convert.ToBoolean(dr["IsStackable"])),
                    MaxStack = ((dr["MaxStack"] == DBNull.Value) ? 1 : Convert.ToInt32(dr["MaxStack"]))
                });
            }
        }

        // Efsunları yükle
        foreach (var item in items)
        {
            item.Attributes = GetAttributes(item.InstanceID);
        }

        return items;
    }

    public bool MoveItem(long instanceId, int newSlotIndex)
    {
        using SqliteConnection conn = DatabaseHelper.GetConnection();
        string sql = "UPDATE ItemInstances SET SlotIndex = @newSlot WHERE InstanceID = @id";
        using SqliteCommand cmd = new SqliteCommand(sql, conn);
        cmd.Parameters.AddWithValue("@newSlot", newSlotIndex);
        cmd.Parameters.AddWithValue("@id", instanceId);
        conn.Open();
        return cmd.ExecuteNonQuery() > 0;
    }

    public void UpdateLastUsed(long instanceId)
    {
        using SqliteConnection conn = DatabaseHelper.GetConnection();
        string sql = "UPDATE ItemInstances SET LastUsed = datetime('now', 'localtime') WHERE InstanceID = @id";
        using SqliteCommand cmd = new SqliteCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", instanceId);
        conn.Open();
        cmd.ExecuteNonQuery();
    }

    public void ConsumeItem(long instanceId, int amountToConsume)
    {
        using SqliteConnection conn = DatabaseHelper.GetConnection();
        conn.Open();
        string sel = "SELECT Count FROM ItemInstances WHERE InstanceID = @id";
        using SqliteCommand selCmd = new SqliteCommand(sel, conn);
        selCmd.Parameters.AddWithValue("@id", instanceId);
        object result = selCmd.ExecuteScalar();
        if (result == null || result == DBNull.Value)
        {
            return;
        }
        int currentCount = Convert.ToInt32(result);
        if (currentCount > amountToConsume)
        {
            string upd = "UPDATE ItemInstances SET Count = Count - @amt WHERE InstanceID = @id";
            using SqliteCommand updCmd = new SqliteCommand(upd, conn);
            updCmd.Parameters.AddWithValue("@amt", amountToConsume);
            updCmd.Parameters.AddWithValue("@id", instanceId);
            updCmd.ExecuteNonQuery();
            return;
        }
        string del = "DELETE FROM ItemInstances WHERE InstanceID = @id";
        using SqliteCommand delCmd = new SqliteCommand(del, conn);
        delCmd.Parameters.AddWithValue("@id", instanceId);
        delCmd.ExecuteNonQuery();
    }

    public bool MoveItemLocation(long instanceId, Enums.ItemLocation newLoc, int newSlot)
    {
        using SqliteConnection conn = DatabaseHelper.GetConnection();
        string sql = "UPDATE ItemInstances SET Location = @loc, SlotIndex = @slot WHERE InstanceID = @id";
        using SqliteCommand cmd = new SqliteCommand(sql, conn);
        cmd.Parameters.AddWithValue("@loc", (byte)newLoc);
        cmd.Parameters.AddWithValue("@slot", newSlot);
        cmd.Parameters.AddWithValue("@id", instanceId);
        conn.Open();
        return cmd.ExecuteNonQuery() > 0;
    }

    public int FindFirstEmptyInventorySlot(int characterId)
    {
        HashSet<int> usedSlots = new HashSet<int>();
        using (SqliteConnection conn = DatabaseHelper.GetConnection())
        {
            string sql = "SELECT SlotIndex FROM ItemInstances WHERE OwnerID = @charId AND Location =1";
            using SqliteCommand cmd = new SqliteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@charId", characterId);
            conn.Open();
            using SqliteDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                if (dr["SlotIndex"] != DBNull.Value)
                {
                    usedSlots.Add(Convert.ToInt32(dr["SlotIndex"]));
                }
            }
        }
        for (int i = 0; i < 40; i++)
        {
            if (!usedSlots.Contains(i))
            {
                return i;
            }
        }
        return -1;
    }

    public ItemInstance GetItemAt(int characterId, Enums.ItemLocation loc, int slotIndex)
    {
        using (SqliteConnection conn = DatabaseHelper.GetConnection())
        {
            string sql = "SELECT I.InstanceID, I.TemplateID, I.SlotIndex, I.Grade, I.Location, I.Count, I.LastUsed, I.UpgradeLevel, I.OwnerID,\r\n                                 T.Name, T.BaseMinDamage, T.BaseMaxDamage, T.EffectType, T.EffectValue, T.Cooldown, T.ItemType, T.BaseMinMagicDamage, T.BaseMaxMagicDamage, T.BaseDefense, T.ReqClass, T.IsStackable, T.MaxStack\r\n                            FROM ItemInstances I\r\n                            INNER JOIN ItemTemplates T ON I.TemplateID = T.TemplateID\r\n                            WHERE I.OwnerID = @charId AND I.Location = @loc AND I.SlotIndex = @slot";
            using SqliteCommand cmd = new SqliteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@charId", characterId);
            cmd.Parameters.AddWithValue("@loc", (byte)loc);
            cmd.Parameters.AddWithValue("@slot", slotIndex);
            conn.Open();
            using SqliteDataReader dr = cmd.ExecuteReader();
            if (dr.Read())
            {
                return new ItemInstance
                {
                    InstanceID = Convert.ToInt64(dr["InstanceID"]),
                    TemplateID = Convert.ToInt32(dr["TemplateID"]),
                    OwnerID = Convert.ToInt32(dr["OwnerID"]),
                    SlotIndex = Convert.ToInt32(dr["SlotIndex"]),
                    Grade = (Enums.ItemGrade)Convert.ToByte(dr["Grade"]),
                    Location = (Enums.ItemLocation)Convert.ToByte(dr["Location"]),
                    Count = Convert.ToInt32(dr["Count"]),
                    Name = dr["Name"]?.ToString(),
                    EffectType = (Enums.ItemEffectType)((dr["EffectType"] != DBNull.Value) ? Convert.ToByte(dr["EffectType"]) : 0),
                    EffectValue = ((dr["EffectValue"] != DBNull.Value) ? Convert.ToInt32(dr["EffectValue"]) : 0),
                    Cooldown = ((dr["Cooldown"] != DBNull.Value) ? Convert.ToInt32(dr["Cooldown"]) : 0),
                    LastUsed = ((dr["LastUsed"] == DBNull.Value) ? ((DateTime?)null) : new DateTime?(Convert.ToDateTime(dr["LastUsed"]))),
                    ItemType = (Enums.ItemType)Convert.ToByte(dr["ItemType"]),
                    IsStackable = (dr["IsStackable"] != DBNull.Value && Convert.ToBoolean(dr["IsStackable"])),
                    MaxStack = ((dr["MaxStack"] == DBNull.Value) ? 1 : Convert.ToInt32(dr["MaxStack"])),
                    UpgradeLevel = ((dr["UpgradeLevel"] != DBNull.Value) ? Convert.ToInt32(dr["UpgradeLevel"]) : 0)
                };
            }
        }
        return null;
    }

    public (Enums.ItemType ItemType, byte? AllowedClass, bool IsStackable) GetTemplateInfo(int templateId)
    {
        using (SqliteConnection conn = DatabaseHelper.GetConnection())
        {
            string sql = "SELECT ItemType, ReqClass, IsStackable FROM ItemTemplates WHERE TemplateID = @tid";
            using SqliteCommand cmd = new SqliteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@tid", templateId);
            conn.Open();
            using SqliteDataReader dr = cmd.ExecuteReader();
            if (dr.Read())
            {
                Enums.ItemType it = (Enums.ItemType)Convert.ToByte(dr["ItemType"]);
                byte? allowed = ((dr["ReqClass"] == DBNull.Value) ? ((byte?)null) : new byte?(Convert.ToByte(dr["ReqClass"])));
                bool stk = dr["IsStackable"] != DBNull.Value && Convert.ToBoolean(dr["IsStackable"]);
                return (ItemType: it, AllowedClass: allowed, IsStackable: stk);
            }
        }
        return (ItemType: Enums.ItemType.Weapon, AllowedClass: null, IsStackable: false);
    }

    public ItemInstance FindStackableItem(int characterId, int templateId)
    {
        using (SqliteConnection conn = DatabaseHelper.GetConnection())
        {
            string sql = "SELECT InstanceID, Count, SlotIndex, TemplateID, OwnerID FROM ItemInstances WHERE OwnerID=@charId AND TemplateID=@tid AND Location=@loc";
            using SqliteCommand cmd = new SqliteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@charId", characterId);
            cmd.Parameters.AddWithValue("@tid", templateId);
            cmd.Parameters.AddWithValue("@loc", (byte)1);
            conn.Open();
            using SqliteDataReader dr = cmd.ExecuteReader();
            if (dr.Read())
            {
                return new ItemInstance
                {
                    InstanceID = Convert.ToInt64(dr["InstanceID"]),
                    Count = Convert.ToInt32(dr["Count"]),
                    SlotIndex = Convert.ToInt32(dr["SlotIndex"]),
                    OwnerID = Convert.ToInt32(dr["OwnerID"]),
                    TemplateID = Convert.ToInt32(dr["TemplateID"]),
                    Location = Enums.ItemLocation.Inventory
                };
            }
        }
        return null;
    }

    public bool IncrementItemCount(long instanceId, int add)
    {
        using SqliteConnection conn = DatabaseHelper.GetConnection();
        string sql = "UPDATE ItemInstances SET Count = Count + @add WHERE InstanceID = @id";
        using SqliteCommand cmd = new SqliteCommand(sql, conn);
        cmd.Parameters.AddWithValue("@add", add);
        cmd.Parameters.AddWithValue("@id", instanceId);
        conn.Open();
        return cmd.ExecuteNonQuery() > 0;
    }

    public ItemInstance FindStackableItemWithCapacity(int characterId, int templateId, int maxStack)
    {
        using (SqliteConnection conn = DatabaseHelper.GetConnection())
        {
            string sql = "\r\n                SELECT I.InstanceID, I.Count, I.SlotIndex, I.TemplateID, I.OwnerID, T.IsStackable, T.MaxStack, T.Name, T.ItemType\r\n                FROM ItemInstances I\r\n                INNER JOIN ItemTemplates T ON I.TemplateID = T.TemplateID\r\n                WHERE I.OwnerID = @charId\r\n                AND I.TemplateID = @tid\r\n                AND I.Location = @loc\r\n                AND I.Count < @maxStack\r\n                LIMIT 1";
            using SqliteCommand cmd = new SqliteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@charId", characterId);
            cmd.Parameters.AddWithValue("@tid", templateId);
            cmd.Parameters.AddWithValue("@loc", (byte)1);
            cmd.Parameters.AddWithValue("@maxStack", maxStack);
            conn.Open();
            using SqliteDataReader dr = cmd.ExecuteReader();
            if (dr.Read())
            {
                return new ItemInstance
                {
                    InstanceID = Convert.ToInt64(dr["InstanceID"]),
                    Count = Convert.ToInt32(dr["Count"]),
                    SlotIndex = Convert.ToInt32(dr["SlotIndex"]),
                    OwnerID = Convert.ToInt32(dr["OwnerID"]),
                    TemplateID = Convert.ToInt32(dr["TemplateID"]),
                    IsStackable = (dr["IsStackable"] != DBNull.Value && Convert.ToBoolean(dr["IsStackable"])),
                    MaxStack = ((dr["MaxStack"] == DBNull.Value) ? 1 : Convert.ToInt32(dr["MaxStack"])),
                    Name = ((dr["Name"] != DBNull.Value) ? dr["Name"].ToString() : string.Empty),
                    ItemType = (Enums.ItemType)((dr["ItemType"] == DBNull.Value) ? 1 : Convert.ToByte(dr["ItemType"])),
                    Location = Enums.ItemLocation.Inventory
                };
            }
        }
        return null;
    }

    public bool AddItemDirectly(ItemInstance item)
    {
        if (item.ItemType == Enums.ItemType.Weapon || item.ItemType == Enums.ItemType.Armor)
        {
            if (item.Grade == Enums.ItemGrade.Others)
            {
                item.Grade = Enums.ItemGrade.Common;
            }
        }
        else
        {
            item.Grade = Enums.ItemGrade.Others;
        }
        using SqliteConnection conn = DatabaseHelper.GetConnection();
        conn.Open();
        string sql = "INSERT INTO ItemInstances (TemplateID, OwnerID, SlotIndex, Grade, Location, Count) \r\n                       VALUES (@tid, @oid, @slot, @grade, @loc, @count)";
        using SqliteCommand cmd = new SqliteCommand(sql, conn);
        cmd.Parameters.AddWithValue("@tid", item.TemplateID);
        cmd.Parameters.AddWithValue("@oid", item.OwnerID);
        cmd.Parameters.AddWithValue("@slot", item.SlotIndex);
        cmd.Parameters.AddWithValue("@grade", (byte)item.Grade);
        cmd.Parameters.AddWithValue("@loc", (byte)item.Location);
        cmd.Parameters.AddWithValue("@count", item.Count);
        return cmd.ExecuteNonQuery() > 0;
    }

    public (Enums.ItemType ItemType, bool IsStackable, int MaxStack, string Name) GetTemplateBasicInfo(int templateId)
    {
        using (SqliteConnection conn = DatabaseHelper.GetConnection())
        {
            string sql = "SELECT ItemType, IsStackable, MaxStack, Name FROM ItemTemplates WHERE TemplateID = @tid";
            using SqliteCommand cmd = new SqliteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@tid", templateId);
            conn.Open();
            using SqliteDataReader dr = cmd.ExecuteReader();
            if (dr.Read())
            {
                Enums.ItemType it = (Enums.ItemType)Convert.ToByte(dr["ItemType"]);
                bool stk = dr["IsStackable"] != DBNull.Value && Convert.ToBoolean(dr["IsStackable"]);
                int max = ((dr["MaxStack"] == DBNull.Value) ? 1 : Convert.ToInt32(dr["MaxStack"]));
                string name = ((dr["Name"] != DBNull.Value) ? dr["Name"].ToString() : string.Empty);
                return (ItemType: it, IsStackable: stk, MaxStack: max, Name: name);
            }
        }
        return (ItemType: Enums.ItemType.Weapon, IsStackable: false, MaxStack: 1, Name: string.Empty);
    }

    public List<ItemInstance> GetSharedStorageItems()
    {
        List<ItemInstance> items = new List<ItemInstance>();
        using (SqliteConnection conn = DatabaseHelper.GetConnection())
        {
            string sql = "\r\n                SELECT \r\n                I.InstanceID, I.TemplateID, I.SlotIndex, I.Grade, I.Location, I.Count, I.LastUsed, I.UpgradeLevel,\r\n                I.OwnerID, T.Name, T.BaseMinDamage, T.BaseMaxDamage, T.EffectType, T.EffectValue, T.Cooldown, \r\n                T.ItemType, T.BaseMinMagicDamage, T.BaseMaxMagicDamage, T.BaseDefense, T.ReqClass, T.IsStackable, T.MaxStack\r\n                FROM ItemInstances I\r\n                INNER JOIN ItemTemplates T ON I.TemplateID = T.TemplateID\r\n                WHERE I.Location = @loc";
            using SqliteCommand cmd = new SqliteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@loc", (byte)3);
            conn.Open();
            using SqliteDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                items.Add(new ItemInstance
                {
                    InstanceID = Convert.ToInt64(dr["InstanceID"]),
                    TemplateID = Convert.ToInt32(dr["TemplateID"]),
                    OwnerID = Convert.ToInt32(dr["OwnerID"]),
                    SlotIndex = Convert.ToInt32(dr["SlotIndex"]),
                    Grade = (Enums.ItemGrade)Convert.ToByte(dr["Grade"]),
                    Location = (Enums.ItemLocation)Convert.ToByte(dr["Location"]),
                    Count = Convert.ToInt32(dr["Count"]),
                    Name = dr["Name"]?.ToString(),
                    MinDamage = ((dr["BaseMinDamage"] != DBNull.Value) ? Convert.ToInt32(dr["BaseMinDamage"]) : 0),
                    MaxDamage = ((dr["BaseMaxDamage"] != DBNull.Value) ? Convert.ToInt32(dr["BaseMaxDamage"]) : 0),
                    BaseDefense = ((dr["BaseDefense"] != DBNull.Value) ? Convert.ToInt32(dr["BaseDefense"]) : 0),
                    MinMagicDamage = ((dr["BaseMinMagicDamage"] != DBNull.Value) ? Convert.ToInt32(dr["BaseMinMagicDamage"]) : 0),
                    MaxMagicDamage = ((dr["BaseMaxMagicDamage"] != DBNull.Value) ? Convert.ToInt32(dr["BaseMaxMagicDamage"]) : 0),
                    ItemType = (Enums.ItemType)Convert.ToByte(dr["ItemType"]),
                    IsStackable = (dr["IsStackable"] != DBNull.Value && Convert.ToBoolean(dr["IsStackable"])),
                    MaxStack = ((dr["MaxStack"] == DBNull.Value) ? 1 : Convert.ToInt32(dr["MaxStack"])),
                    UpgradeLevel = ((dr["UpgradeLevel"] != DBNull.Value) ? Convert.ToInt32(dr["UpgradeLevel"]) : 0),
                    EffectType = (Enums.ItemEffectType)((dr["EffectType"] != DBNull.Value) ? Convert.ToByte(dr["EffectType"]) : 0),
                    EffectValue = ((dr["EffectValue"] != DBNull.Value) ? Convert.ToInt32(dr["EffectValue"]) : 0)
                });
            }
        }

        // Load Attributes
        foreach (var item in items)
        {
            item.Attributes = GetAttributes(item.InstanceID);
        }

        return items;
    }

    public void UpdateItemOwner(long instanceId, int newOwnerId)
    {
        using SqliteConnection conn = DatabaseHelper.GetConnection();
        string sql = "UPDATE ItemInstances SET OwnerID = @oid WHERE InstanceID = @id";
        using SqliteCommand cmd = new SqliteCommand(sql, conn);
        cmd.Parameters.AddWithValue("@oid", newOwnerId);
        cmd.Parameters.AddWithValue("@id", instanceId);
        conn.Open();
        cmd.ExecuteNonQuery();
    }

    public List<ItemAttribute> GetAttributes(long instanceId)
    {
        List<ItemAttribute> attrs = new List<ItemAttribute>();
        using (SqliteConnection conn = DatabaseHelper.GetConnection())
        {
            string sql = "SELECT AttributeID, InstanceID, AttrType, AttrValue FROM ItemAttributes WHERE InstanceID = @id";
            using SqliteCommand cmd = new SqliteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", instanceId);
            conn.Open();
            using SqliteDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                attrs.Add(new ItemAttribute
                {
                    AttributeID = Convert.ToInt64(dr["AttributeID"]),
                    InstanceID = Convert.ToInt64(dr["InstanceID"]),
                    AttributeType = (Enums.ItemAttributeType)Convert.ToByte(dr["AttrType"]),
                    Value = Convert.ToInt32(dr["AttrValue"])
                });
            }
        }
        return attrs;
    }

    public void AddAttribute(long instanceId, ItemAttribute attr)
    {
        using SqliteConnection conn = DatabaseHelper.GetConnection();
        string sql = "INSERT INTO ItemAttributes (InstanceID, AttrType, AttrValue) VALUES (@iid, @type, @val)";
        using SqliteCommand cmd = new SqliteCommand(sql, conn);
        cmd.Parameters.AddWithValue("@iid", instanceId);
        cmd.Parameters.AddWithValue("@type", (byte)attr.AttributeType);
        cmd.Parameters.AddWithValue("@val", attr.Value);
        conn.Open();
        cmd.ExecuteNonQuery();
    }

    public void ClearAttributes(long instanceId)
    {
        using SqliteConnection conn = DatabaseHelper.GetConnection();
        string sql = "DELETE FROM ItemAttributes WHERE InstanceID = @iid";
        using SqliteCommand cmd = new SqliteCommand(sql, conn);
        cmd.Parameters.AddWithValue("@iid", instanceId);
        conn.Open();
        cmd.ExecuteNonQuery();
    }
}
