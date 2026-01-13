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

public class UcInventory : GameWindow
{
    private const int SLOT_COUNT = 40;

    private ConsumableManager _consumableManager = new ConsumableManager();

    private UcItemSlot slotWeapon;

    private UcItemSlot slotArmor;

    private EquipmentManager _equipManager = new EquipmentManager();

    private Label lblGold;

    private InventoryManager _manager = new InventoryManager();

    private IContainer components = null;

    private FlowLayoutPanel pnlGrid;

    public List<UcItemSlot> Slots { get; private set; } = new List<UcItemSlot>();

    public bool IsMerchantMode { get; set; } = false;

    public event EventHandler<ItemInstance> OnSellRequest;

    public UcInventory()
    {
        InitializeComponent();
        base.Title = "INVENTORY";
        GenerateSlots();
        SetupGoldLabel();
    }

    private void SetupGoldLabel()
    {
        lblGold = new Label
        {
            Text = "0 G",
            ForeColor = Color.Gold,
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            AutoSize = true,
            Anchor = (AnchorStyles.Top | AnchorStyles.Right)
        };
        base.Controls.Add(lblGold);
        lblGold.BringToFront();
        UpdateGoldLabel();
    }

    public void UpdateGoldLabel()
    {
        CharacterModel player = SessionManager.CurrentCharacter;
        if (player != null && lblGold != null)
        {
            lblGold.Text = $"{player.Gold:N0} G";
            int x = base.Width - lblGold.Width - 50;
            lblGold.Location = new Point(x, 8);
        }
    }

    private void GenerateSlots()
    {
        Panel pnlEquip = new Panel
        {
            Dock = DockStyle.Top,
            Height = 100,
            BackColor = Color.FromArgb(40, 40, 40)
        };
        slotWeapon = new UcItemSlot(0);
        slotWeapon.AcceptedType = Enums.ItemType.Weapon;
        slotWeapon.Location = new Point(20, 30);
        slotWeapon.OnItemDropped += EquipmentSlot_OnItemDropped;
        slotWeapon.MouseUp += Slot_MouseUp;
        slotArmor = new UcItemSlot(1);
        slotArmor.AcceptedType = Enums.ItemType.Armor;
        slotArmor.Location = new Point(80, 30);
        slotArmor.OnItemDropped += EquipmentSlot_OnItemDropped;
        slotArmor.MouseUp += Slot_MouseUp;
        pnlEquip.Controls.Add(slotWeapon);
        pnlEquip.Controls.Add(slotArmor);
        base.Controls.Add(pnlEquip);
        pnlGrid.Controls.Clear();
        Slots.Clear();
        for (int i = 0; i < 40; i++)
        {
            UcItemSlot slot = new UcItemSlot(i);
            slot.OnItemDropped += Slot_OnItemDropped;
            slot.MouseUp += Slot_MouseUp;
            pnlGrid.Controls.Add(slot);
            Slots.Add(slot);
        }
    }

    public void LoadItems(List<ItemInstance> items)
    {
        foreach (UcItemSlot slot in Slots)
        {
            slot.SetItem(null);
        }
        slotWeapon.SetItem(null);
        slotArmor.SetItem(null);
        foreach (ItemInstance item in items)
        {
            if (item.Location == Enums.ItemLocation.Inventory)
            {
                if (item.SlotIndex < 40)
                {
                    Slots[item.SlotIndex].SetItem(item);
                }
            }
            else if (item.Location == Enums.ItemLocation.Equipment)
            {
                if (item.SlotIndex == 0)
                {
                    slotWeapon.SetItem(item);
                }
                else if (item.SlotIndex == 1)
                {
                    slotArmor.SetItem(item);
                }
            }
        }
        UpdateGoldLabel();
    }

    private void Slot_OnItemDropped(object sender, ItemInstance droppedItem)
    {
        UcItemSlot targetSlot = (UcItemSlot)sender;
        CharacterModel hero = SessionManager.CurrentCharacter;
        if (droppedItem.Location == Enums.ItemLocation.Storage)
        {
            if (_manager.TransferItem(hero, droppedItem, Enums.ItemLocation.Inventory, targetSlot.SlotIndex))
            {
                ReloadInventory();
                NotifyStatUpdate();
            }
            else
            {
                MessageBox.Show("Move failed (Slot full or item mismatch).");
            }
        }
        else
        {
            if (targetSlot.SlotIndex == droppedItem.SlotIndex && droppedItem.Location == Enums.ItemLocation.Inventory)
            {
                return;
            }
            if (droppedItem.Location == Enums.ItemLocation.Equipment)
            {
                _manager.MoveItemToSlotAndLocation(droppedItem.InstanceID, Enums.ItemLocation.Inventory, targetSlot.SlotIndex);
                ReloadInventory();
                NotifyStatUpdate();
                return;
            }
            if (targetSlot.Item == null)
            {
                _manager.MoveItemToSlot(droppedItem.InstanceID, targetSlot.SlotIndex);
                ReloadInventory();
                return;
            }
            ItemInstance targetItem = targetSlot.Item;
            if (droppedItem.TemplateID == targetItem.TemplateID && targetItem.IsStackable && targetItem.Count < targetItem.MaxStack && _manager.MergeItems(droppedItem, targetItem))
            {
                ReloadInventory();
                return;
            }
            _manager.MoveItemToSlot(targetItem.InstanceID, droppedItem.SlotIndex);
            _manager.MoveItemToSlot(droppedItem.InstanceID, targetSlot.SlotIndex);
            ReloadInventory();
        }
    }

    private void Slot_MouseUp(object sender, MouseEventArgs e)
    {
        UcItemSlot slot = (UcItemSlot)sender;
        if (slot.Item == null)
        {
            return;
        }
        if (e.Button == MouseButtons.Left && (Control.ModifierKeys & Keys.Control) == Keys.Control && slot.Item.IsStackable && slot.Item.Count > 1)
        {
            using (QuantityDialog qd = new QuantityDialog(slot.Item.Count - 1))
            {
                if (qd.ShowDialog() == DialogResult.OK)
                {
                    int amount = qd.SelectedQuantity;
                    (bool, string) result = _manager.SplitItem(slot.Item, amount);
                    if (result.Item1)
                    {
                        ReloadInventory();
                        NotifyStatUpdate();
                    }
                    else
                    {
                        MessageBox.Show(result.Item2);
                    }
                }
                return;
            }
        }
        if (e.Button != MouseButtons.Right)
        {
            return;
        }
        if (IsMerchantMode && slot.Item.Location == Enums.ItemLocation.Inventory)
        {
            this.OnSellRequest?.Invoke(this, slot.Item);
        }
        else if (slot.Item.Location == Enums.ItemLocation.Inventory)
        {
            if (slot.Item.ItemType == Enums.ItemType.Consumable)
            {
                (bool, string) result2 = _consumableManager.UseItem(SessionManager.CurrentCharacter, slot.Item);
                if (result2.Item1)
                {
                    ReloadInventory();
                    NotifyStatUpdate();
                }
                else
                {
                    MessageBox.Show(result2.Item2);
                }
            }
            else
            {
                if (slot.Item.ItemType != Enums.ItemType.Weapon && slot.Item.ItemType != Enums.ItemType.Armor)
                {
                    return;
                }
                int targetSlotIndex = _equipManager.GetTargetEquipmentSlot(slot.Item.ItemType);
                if (targetSlotIndex == -1)
                {
                    return;
                }
                (bool, string) result3 = _equipManager.EquipItem(slot.Item, targetSlotIndex);
                if (result3.Item1)
                {
                    ReloadInventory();
                    NotifyStatUpdate();
                    if (base.ParentForm is FormMain main)
                    {
                        main.RefreshStats();
                    }
                }
                else
                {
                    MessageBox.Show(result3.Item2);
                }
            }
        }
        else if (slot.Item.Location == Enums.ItemLocation.Equipment)
        {
            (bool, string) result4 = _equipManager.UnequipItem(slot.Item);
            if (result4.Item1)
            {
                ReloadInventory();
                NotifyStatUpdate();
            }
            else
            {
                MessageBox.Show(result4.Item2);
            }
        }
    }

    private void EquipmentSlot_OnItemDropped(object sender, ItemInstance droppedItem)
    {
        UcItemSlot targetSlot = (UcItemSlot)sender;
        if (droppedItem.Location == Enums.ItemLocation.Equipment && droppedItem.SlotIndex == targetSlot.SlotIndex)
        {
            return;
        }
        (bool, string) result = _equipManager.EquipItem(droppedItem, targetSlot.SlotIndex);
        if (result.Item1)
        {
            ReloadInventory();
            NotifyStatUpdate();
            if (base.ParentForm is FormMain main)
            {
                main.RefreshStats();
            }
        }
        else
        {
            MessageBox.Show(result.Item2);
        }
    }

    private void NotifyStatUpdate()
    {
        if (base.ParentForm is FormMain mainForm)
        {
            mainForm.RefreshStats();
        }
    }

    public void ReloadInventory()
    {
        List<ItemInstance> items = _manager.GetInventory(SessionManager.CurrentCharacter.CharacterID);
        LoadItems(items);
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
        this.pnlGrid = new System.Windows.Forms.FlowLayoutPanel();
        base.SuspendLayout();
        this.pnlGrid.AutoScroll = true;
        this.pnlGrid.Dock = System.Windows.Forms.DockStyle.Fill;
        this.pnlGrid.Location = new System.Drawing.Point(0, 30);
        this.pnlGrid.Name = "pnlGrid";
        this.pnlGrid.Size = new System.Drawing.Size(298, 368);
        this.pnlGrid.TabIndex = 1;
        base.AutoScaleDimensions = new System.Drawing.SizeF(7f, 15f);
        base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        base.Controls.Add(this.pnlGrid);
        base.Name = "UcInventory";
        base.Controls.SetChildIndex(this.pnlGrid, 0);
        base.ResumeLayout(false);
    }
}
