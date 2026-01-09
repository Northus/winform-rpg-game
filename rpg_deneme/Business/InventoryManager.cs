using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;
using rpg_deneme.Core;
using rpg_deneme.Data;
using rpg_deneme.Models;

namespace rpg_deneme.Business;

/// <summary>
/// Envanter işlemlerini yöneten iş mantığı sınıfı.
/// </summary>
public class InventoryManager
{
    private readonly InventoryRepository _repo = new InventoryRepository();

    /// <summary>
    /// Karakterin envanterini getirir.
    /// </summary>
    public List<ItemInstance> GetInventory(int charId)
    {
        return _repo.GetCharacterInventory(charId);
    }

    /// <summary>
    /// Eşyayı envanter içinde belirtilen slota taşır.
    /// </summary>
    public bool MoveItemToSlot(long instanceId, int targetSlotIndex)
    {
        return _repo.MoveItem(instanceId, targetSlotIndex);
    }

    /// <summary>
    /// Eşyayı yeni bir konuma ve slota taşır.
    /// </summary>
    public bool MoveItemToSlotAndLocation(long instanceId, Enums.ItemLocation newLocation, int targetSlotIndex)
    {
        return _repo.MoveItemLocation(instanceId, newLocation, targetSlotIndex);
    }

    /// <summary>
    /// Envantere yeni bir eşya ekler, üst üste binebilir eşyaları kontrol eder.
    /// </summary>
    public bool AddItemToInventory(ItemInstance item)
    {
        (Enums.ItemType, bool, int, string) templateBasic = _repo.GetTemplateBasicInfo(item.TemplateID);
        bool isStackable = item.IsStackable || templateBasic.Item2;
        int maxStackLimit = 1;
        if (isStackable)
        {
            maxStackLimit = ((item.MaxStack > 1) ? item.MaxStack : ((templateBasic.Item3 <= 1) ? 20 : templateBasic.Item3));
        }

        while (item.Count > 0)
        {
            ItemInstance existingStack = null;
            if (isStackable)
            {
                existingStack = _repo.FindStackableItemWithCapacity(item.OwnerID, item.TemplateID, maxStackLimit);
            }

            if (existingStack != null)
            {
                int spaceAvailable = maxStackLimit - existingStack.Count;
                int amountToAdd = Math.Min(spaceAvailable, item.Count);
                if (!_repo.IncrementItemCount(existingStack.InstanceID, amountToAdd))
                {
                    return false;
                }
                item.Count -= amountToAdd;
                continue;
            }

            int emptySlot = _repo.FindFirstEmptyInventorySlot(item.OwnerID);
            if (emptySlot == -1)
            {
                return false;
            }

            int amountToInsert = Math.Min(maxStackLimit, item.Count);
            item.SlotIndex = emptySlot;
            int originalCount = item.Count;
            item.Count = amountToInsert;
            bool success = InsertNewItemToDB(item);
            item.Count = originalCount;
            if (!success)
            {
                return false;
            }
            item.Count -= amountToInsert;
        }
        return true;
    }

    /// <summary>
    /// Veritabanına yeni bir eşya örneği ekler.
    /// </summary>
    private bool InsertNewItemToDB(ItemInstance item)
    {
        using SqliteConnection conn = DatabaseHelper.GetConnection();
        conn.Open();
        string sql = @"
                INSERT INTO ItemInstances 
                (TemplateID, OwnerID, SlotIndex, Grade, Location, Count, UpgradeLevel) 
                VALUES 
                (@tid, @oid, @slot, @grade, @loc, @count, @upg)";
        using SqliteCommand cmd = new SqliteCommand(sql, conn);
        cmd.Parameters.AddWithValue("@tid", item.TemplateID);
        cmd.Parameters.AddWithValue("@oid", item.OwnerID);
        cmd.Parameters.AddWithValue("@slot", item.SlotIndex);
        cmd.Parameters.AddWithValue("@grade", (byte)item.Grade);
        cmd.Parameters.AddWithValue("@loc", (byte)item.Location);
        cmd.Parameters.AddWithValue("@count", item.Count);
        cmd.Parameters.AddWithValue("@upg", item.UpgradeLevel);
        return cmd.ExecuteNonQuery() > 0;
    }

    /// <summary>
    /// Üst üste binmiş bir eşyayı bölerek başka bir slota koyar.
    /// </summary>
    public (bool Success, string Message) SplitItem(ItemInstance item, int amountToSplit)
    {
        if (amountToSplit >= item.Count) { return (Success: false, Message: "Hatalı miktar."); }
        if (amountToSplit <= 0) { return (Success: false, Message: "En az 1 adet bölmelisin."); }

        int emptySlot = _repo.FindFirstEmptyInventorySlot(item.OwnerID);
        if (emptySlot == -1) { return (Success: false, Message: "Bölmek için boş yer yok!"); }

        ItemInstance newItem = new ItemInstance
        {
            TemplateID = item.TemplateID,
            OwnerID = item.OwnerID,
            SlotIndex = emptySlot,
            Count = amountToSplit,
            Name = item.Name,
            Grade = item.Grade,
            ItemType = item.ItemType,
            IsStackable = item.IsStackable,
            MaxStack = item.MaxStack,
            Location = Enums.ItemLocation.Inventory
        };

        if (_repo.AddItemDirectly(newItem))
        {
            _repo.ConsumeItem(item.InstanceID, amountToSplit);
            return (Success: true, Message: "Eşya bölündü.");
        }
        return (Success: false, Message: "Veritabanı hatası.");
    }

    /// <summary>
    /// İki aynı tür eşyayı üst üste birleştirir.
    /// </summary>
    public bool MergeItems(ItemInstance sourceItem, ItemInstance targetItem)
    {
        if (sourceItem.TemplateID != targetItem.TemplateID) return false;
        if (!targetItem.IsStackable) return false;
        if (targetItem.Count >= targetItem.MaxStack) return false;

        int spaceAvailable = targetItem.MaxStack - targetItem.Count;
        int amountToMove = Math.Min(spaceAvailable, sourceItem.Count);
        if (amountToMove <= 0) return false;

        if (_repo.IncrementItemCount(targetItem.InstanceID, amountToMove))
        {
            _repo.ConsumeItem(sourceItem.InstanceID, amountToMove);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Depoda (Storage) ilk boş slotu bulur.
    /// </summary>
    public int FindFirstEmptyStorageSlot(int characterId)
    {
        List<ItemInstance> allItems = _repo.GetCharacterInventory(characterId);
        List<ItemInstance> storageItems = allItems.Where(x => x.Location == Enums.ItemLocation.Storage).ToList();
        for (int i = 0; i < 42; i++)
        {
            if (!storageItems.Any(x => x.SlotIndex == i)) return i;
        }
        return -1;
    }

    /// <summary>
    /// Envanterde (Inventory) ilk boş slotu bulur.
    /// </summary>
    public int FindFirstEmptyInventorySlot(int characterId)
    {
        return _repo.FindFirstEmptyInventorySlot(characterId);
    }

    /// <summary>
    /// Ortak depodaki eşyaları getirir.
    /// </summary>
    public List<ItemInstance> GetSharedStorage()
    {
        return _repo.GetSharedStorageItems();
    }

    /// <summary>
    /// Eşyayı bir konumdan diğerine (envanter <-> depo) transfer eder.
    /// </summary>
    public bool TransferItem(CharacterModel hero, ItemInstance sourceItem, Enums.ItemLocation targetLoc, int targetSlot)
    {
        ItemInstance targetItem = null;
        if (targetLoc == Enums.ItemLocation.Inventory)
        {
            targetItem = _repo.GetItemAt(hero.CharacterID, targetLoc, targetSlot);
        }
        else if (targetLoc == Enums.ItemLocation.Storage)
        {
            List<ItemInstance> allStorage = GetSharedStorage();
            targetItem = allStorage.FirstOrDefault(x => x.SlotIndex == targetSlot);
        }

        if (targetItem != null)
        {
            if (sourceItem.TemplateID != targetItem.TemplateID || !targetItem.IsStackable || targetItem.Count >= targetItem.MaxStack)
            {
                return false;
            }

            int spaceAvailable = targetItem.MaxStack - targetItem.Count;
            int amountToMove = Math.Min(spaceAvailable, sourceItem.Count);
            if (amountToMove <= 0) return false;

            _repo.IncrementItemCount(targetItem.InstanceID, amountToMove);
            _repo.ConsumeItem(sourceItem.InstanceID, amountToMove);
            return true;
        }

        _repo.MoveItemLocation(sourceItem.InstanceID, targetLoc, targetSlot);
        if (targetLoc == Enums.ItemLocation.Inventory)
        {
            _repo.UpdateItemOwner(sourceItem.InstanceID, hero.CharacterID);
        }
        return true;
    }

    /// <summary>
    /// Depodan envantere "akıllı" çekme işlemi yapar (mevcut stackleri önceliklendirir).
    /// </summary>
    public void SmartWithdraw(CharacterModel hero, ItemInstance sourceItem)
    {
        if (hero == null || sourceItem == null) return;

        ItemInstance refreshed = GetSharedStorage().FirstOrDefault(x => x.InstanceID == sourceItem.InstanceID);
        if (refreshed == null) return;

        // Önce mevcut stackleri doldur
        while (true)
        {
            refreshed = GetSharedStorage().FirstOrDefault(x => x.InstanceID == sourceItem.InstanceID);
            if (refreshed == null) return;

            ItemInstance existingStack = _repo.FindStackableItemWithCapacity(hero.CharacterID, refreshed.TemplateID, refreshed.MaxStack);
            if (existingStack == null) break;

            int spaceAvailable = existingStack.MaxStack - existingStack.Count;
            if (spaceAvailable <= 0) break;

            int amountToMove = Math.Min(spaceAvailable, refreshed.Count);
            if (amountToMove <= 0 || !_repo.IncrementItemCount(existingStack.InstanceID, amountToMove)) break;

            _repo.ConsumeItem(refreshed.InstanceID, amountToMove);
        }

        // Kalanları boş slotlara yerleştir
        while (true)
        {
            refreshed = GetSharedStorage().FirstOrDefault(x => x.InstanceID == sourceItem.InstanceID);
            if (refreshed == null || refreshed.Count <= 0) break;

            int emptySlot = FindFirstEmptyInventorySlot(hero.CharacterID);
            if (emptySlot == -1) break;

            int amountToMove2 = Math.Min(refreshed.Count, refreshed.MaxStack);
            if (amountToMove2 == refreshed.Count)
            {
                if (_repo.MoveItemLocation(refreshed.InstanceID, Enums.ItemLocation.Inventory, emptySlot))
                {
                    _repo.UpdateItemOwner(refreshed.InstanceID, hero.CharacterID);
                }
                break;
            }

            ItemInstance newItem = new ItemInstance
            {
                TemplateID = refreshed.TemplateID,
                OwnerID = hero.CharacterID,
                SlotIndex = emptySlot,
                Count = amountToMove2,
                Grade = refreshed.Grade,
                Location = Enums.ItemLocation.Inventory,
                ItemType = refreshed.ItemType,
                IsStackable = refreshed.IsStackable,
                MaxStack = refreshed.MaxStack,
                Name = refreshed.Name
            };

            if (!_repo.AddItemDirectly(newItem)) break;
            _repo.ConsumeItem(refreshed.InstanceID, amountToMove2);
        }
    }

    /// <summary>
    /// Eşyayı tüketir/kullanır (adet azaltır veya siler).
    /// </summary>
    public void ConsumeItem(long instanceId, int amount)
    {
        _repo.ConsumeItem(instanceId, amount);
    }
}
