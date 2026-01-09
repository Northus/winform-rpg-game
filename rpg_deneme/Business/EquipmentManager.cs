using rpg_deneme.Core;
using rpg_deneme.Data;
using rpg_deneme.Models;

namespace rpg_deneme.Business;

public class EquipmentManager
{
	private readonly InventoryRepository _repo = new InventoryRepository();

	public (bool Success, string Message) EquipItem(ItemInstance newItem, int targetSlotIndex)
	{
		if (!IsValidSlot(newItem.ItemType, targetSlotIndex))
		{
			return (Success: false, Message: "Bu slot için uygun bir eşya değil.");
		}
		CharacterModel currentChar = SessionManager.CurrentCharacter;
		if (newItem.AllowedClass.HasValue && newItem.AllowedClass.Value != 0 && currentChar.Class != newItem.AllowedClass.Value)
		{
			string className = ((Enums.CharacterClass)newItem.AllowedClass.Value/*cast due to .constrained prefix*/).ToString();
			return (Success: false, Message: "Bu eşyayı sadece " + className + " sınıfı kullanabilir!");
		}
		ItemInstance oldItem = _repo.GetItemAt(newItem.OwnerID, Enums.ItemLocation.Equipment, targetSlotIndex);
		if (oldItem != null)
		{
			_repo.MoveItemLocation(oldItem.InstanceID, Enums.ItemLocation.Inventory, newItem.SlotIndex);
			_repo.MoveItemLocation(newItem.InstanceID, Enums.ItemLocation.Equipment, targetSlotIndex);
			return (Success: true, Message: "Eşyalar değiştirildi.");
		}
		return _repo.MoveItemLocation(newItem.InstanceID, Enums.ItemLocation.Equipment, targetSlotIndex) ? (Success: true, Message: "Kuşanıldı") : (Success: false, Message: "Veritabanı hatası.");
	}

	private bool IsValidSlot(Enums.ItemType type, int slotIndex)
	{
		if (type == Enums.ItemType.Weapon && slotIndex == 0)
		{
			return true;
		}
		if (type == Enums.ItemType.Armor && slotIndex == 1)
		{
			return true;
		}
		return false;
	}

	public (bool Success, string Message) UnequipItem(ItemInstance item)
	{
		int emptySlot = _repo.FindFirstEmptyInventorySlot(item.OwnerID);
		if (emptySlot == -1)
		{
			return (Success: false, Message: "Çantan dolu! Eşyayı çıkaramazsın.");
		}
		return _repo.MoveItemLocation(item.InstanceID, Enums.ItemLocation.Inventory, emptySlot) ? (Success: true, Message: "Eşya çıkarıldı.") : (Success: false, Message: "Bir hata oluştu.");
	}

	public int GetTargetEquipmentSlot(Enums.ItemType type)
	{
		return type switch
		{
			Enums.ItemType.Weapon => 0, 
			Enums.ItemType.Armor => 1, 
			_ => -1, 
		};
	}
}
