using System.Collections.Generic;
using rpg_deneme.Core;
using rpg_deneme.Data;
using rpg_deneme.Models;

namespace rpg_deneme.Business;

public class ShopManager
{
    private ShopRepository _shopRepo = new ShopRepository();

    private InventoryManager _invManager = new InventoryManager();

    private InventoryRepository _invRepo = new InventoryRepository();

    public List<ItemInstance> GetShopList(int shopId)
    {
        return _shopRepo.GetShopItems(shopId);
    }

    public (bool Success, string Message) BuyItem(CharacterModel player, int shopId, ItemInstance shopItem, int quantity)
    {
        int unitPrice = _shopRepo.GetShopItemPrice(shopId, shopItem.TemplateID);
        if (unitPrice < 0)
        {
            return (Success: false, Message: "Product price changed or removed.");
        }
        long totalCost = (long)unitPrice * (long)quantity;
        if (player.Gold < totalCost)
        {
            return (Success: false, Message: $"Insufficient Gold! (Required: {totalCost}, Current: {player.Gold})");
        }
        ItemInstance newItem = new ItemInstance
        {
            TemplateID = shopItem.TemplateID,
            OwnerID = player.CharacterID,
            Count = quantity,
            Name = shopItem.Name,
            ItemType = shopItem.ItemType,
            IsStackable = shopItem.IsStackable,
            MaxStack = shopItem.MaxStack,
            Grade = shopItem.Grade,
            UpgradeLevel = 0,
            Location = Enums.ItemLocation.Inventory,
            EffectType = shopItem.EffectType,
            EffectValue = shopItem.EffectValue,
            MinDamage = shopItem.MinDamage,
            MaxDamage = shopItem.MaxDamage,
            BaseDefense = shopItem.BaseDefense
        };
        if (_invManager.AddItemToInventory(newItem))
        {
            player.Gold -= totalCost;
            _shopRepo.UpdateGold(player.CharacterID, player.Gold);
            return (Success: true, Message: "Purchase successful!");
        }
        return (Success: false, Message: "Not enough space in inventory!");
    }

    public (bool Success, string Message) SellItem(CharacterModel player, ItemInstance itemToSell, int quantity)
    {
        int unitSellPrice = _shopRepo.GetGlobalSellPrice(itemToSell.TemplateID);
        if (unitSellPrice <= 0)
        {
            unitSellPrice = 1;
        }
        long totalEarned = (long)unitSellPrice * (long)quantity;
        _invRepo.ConsumeItem(itemToSell.InstanceID, quantity);
        player.Gold += totalEarned;
        _shopRepo.UpdateGold(player.CharacterID, player.Gold);
        return (Success: true, Message: $"{quantity} items sold. (+{totalEarned} Gold)");
    }

    public int GetSellPrice(int templateId)
    {
        return _shopRepo.GetGlobalSellPrice(templateId);
    }
}
