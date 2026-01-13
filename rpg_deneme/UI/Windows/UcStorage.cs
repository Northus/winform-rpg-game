using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using rpg_deneme.Business;
using rpg_deneme.Core;
using rpg_deneme.Models;
using rpg_deneme.UI.Controls;

namespace rpg_deneme.UI.Windows;

public class UcStorage : GameWindow
{
    private FlowLayoutPanel pnlGrid;

    private List<UcItemSlot> _slots = new List<UcItemSlot>();

    private const int STORAGE_CAPACITY = 42;

    private InventoryManager _manager = new InventoryManager();

    private IContainer components = null;

    public event EventHandler<ItemInstance> OnWithdrawRequest;

    public UcStorage()
    {
        base.Title = "STORAGE";
        base.Size = new Size(320, 400);
        SetupUI();
    }

    private void SetupUI()
    {
        pnlGrid = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(45, 45, 48),
            Padding = new Padding(10, 40, 10, 10),
            AutoScroll = true,
            AllowDrop = true
        };
        pnlGrid.DragEnter += PnlGrid_DragEnter;
        pnlGrid.DragDrop += PnlGrid_DragDrop;
        base.Controls.Add(pnlGrid);
        pnlGrid.SendToBack();
        GenerateSlots();
    }

    private void GenerateSlots()
    {
        _slots.Clear();
        pnlGrid.Controls.Clear();
        for (int i = 0; i < 42; i++)
        {
            UcItemSlot slot = new UcItemSlot(i);
            slot.OnItemDropped += Slot_OnItemDropped;
            slot.MouseUp += Slot_MouseUp;
            pnlGrid.Controls.Add(slot);
            _slots.Add(slot);
        }
    }

    public void LoadStorage()
    {
        List<ItemInstance> storageItems = _manager.GetSharedStorage();
        foreach (UcItemSlot slot in _slots)
        {
            slot.SetItem(null);
        }
        foreach (ItemInstance item in storageItems)
        {
            if (item.SlotIndex < _slots.Count)
            {
                _slots[item.SlotIndex].SetItem(item);
            }
        }
    }

    private void Slot_OnItemDropped(object sender, ItemInstance droppedItem)
    {
        UcItemSlot targetSlot = (UcItemSlot)sender;
        CharacterModel hero = SessionManager.CurrentCharacter;
        if (droppedItem.Location == Enums.ItemLocation.Inventory)
        {
            if (!_manager.TransferItem(hero, droppedItem, Enums.ItemLocation.Storage, targetSlot.SlotIndex))
            {
                MessageBox.Show("That slot is full and cannot be stacked.");
            }
            ReloadInventory();
            NotifyStatUpdate();
        }
        else
        {
            if (targetSlot.SlotIndex == droppedItem.SlotIndex)
            {
                return;
            }
            if (targetSlot.Item == null)
            {
                _manager.TransferItem(hero, droppedItem, Enums.ItemLocation.Storage, targetSlot.SlotIndex);
                ReloadInventory();
                return;
            }
            ItemInstance targetItem = targetSlot.Item;
            if (targetItem.TemplateID == droppedItem.TemplateID && targetItem.IsStackable)
            {
                _manager.MergeItems(droppedItem, targetItem);
            }
            else
            {
                _manager.MoveItemToSlot(targetItem.InstanceID, droppedItem.SlotIndex);
                _manager.MoveItemToSlot(droppedItem.InstanceID, targetSlot.SlotIndex);
            }
            RefreshStorage();
        }
    }

    private void Slot_MouseUp(object sender, MouseEventArgs e)
    {
        UcItemSlot slot = (UcItemSlot)sender;
        if (slot.Item != null && e.Button == MouseButtons.Right)
        {
            _manager.SmartWithdraw(SessionManager.CurrentCharacter, slot.Item);
            ReloadInventory();
            NotifyStatUpdate();
        }
    }

    private void PnlGrid_DragEnter(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(typeof(ItemInstance)))
        {
            e.Effect = DragDropEffects.Move;
        }
        else
        {
            e.Effect = DragDropEffects.None;
        }
    }

    private void PnlGrid_DragDrop(object sender, DragEventArgs e)
    {
        ItemInstance item = (ItemInstance)e.Data.GetData(typeof(ItemInstance));
        if (item.Location == Enums.ItemLocation.Inventory)
        {
            int emptySlot = _manager.FindFirstEmptyStorageSlot(item.OwnerID);
            if (emptySlot != -1)
            {
                MoveToStorageSlot(item, emptySlot);
            }
            else
            {
                MessageBox.Show("Storage full!");
            }
        }
    }

    private void MoveToStorageSlot(ItemInstance item, int slotIndex)
    {
        _manager.MoveItemToSlotAndLocation(item.InstanceID, Enums.ItemLocation.Storage, slotIndex);
        RefreshStorage();
        if (base.ParentForm is FormMain main)
        {
            main.RefreshStats();
        }
    }

    private void RefreshStorage()
    {
        if (base.ParentForm is FormMain)
        {
            List<ItemInstance> allItems = _manager.GetInventory(SessionManager.CurrentCharacter.CharacterID);
            LoadStorage();
        }
    }

    private void ReloadInventory()
    {
        RefreshStorage();
    }

    private void NotifyStatUpdate()
    {
        if (base.ParentForm is FormMain main)
        {
            main.RefreshStats();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && components != null)
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        this.components = new System.ComponentModel.Container();
        base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
    }
}
