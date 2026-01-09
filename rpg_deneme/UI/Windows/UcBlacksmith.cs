using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using rpg_deneme.Business;
using rpg_deneme.Core;
using rpg_deneme.Data;
using rpg_deneme.Models;
using rpg_deneme.UI.Controls;

namespace rpg_deneme.UI.Windows;

public class UcBlacksmith : GameWindow
{
    private UcItemSlot slotMain;

    private UcItemSlot slotMaterial;

    private UcItemSlot slotLucky;

    private UcItemSlot slotResult;

    private Button btnUpgrade;

    private Label lblInfo;

    private Label lblChance;

    private UpgradeManager _upgManager = new UpgradeManager();

    private InventoryManager _invManager = new InventoryManager();

    private InventoryRepository _repo = new InventoryRepository();

    private readonly List<UcItemSlot> _slots = new List<UcItemSlot>();

    private Form _attachedParentForm;

    private IContainer components = null;

    public UcBlacksmith()
    {
        base.Title = "DEMİRCİ";
        base.Size = new Size(360, 320);
        SetupUI();
    }

    protected override void OnParentChanged(EventArgs e)
    {
        base.OnParentChanged(e);
        if (_attachedParentForm != null)
        {
            _attachedParentForm.FormClosing -= ParentForm_FormClosing;
            _attachedParentForm = null;
        }
        if (base.ParentForm != null)
        {
            _attachedParentForm = base.ParentForm;
            _attachedParentForm.FormClosing += ParentForm_FormClosing;
        }
    }

    protected override bool OnClosing()
    {
        try
        {
            FlushSlotsToInventory();
        }
        catch
        {
        }
        return true;
    }

    private void ParentForm_FormClosing(object sender, FormClosingEventArgs e)
    {
        try
        {
            FlushSlotsToInventory();
        }
        catch
        {
        }
    }

    public void RefreshFromDb()
    {
        ReloadSlotItem(slotMain);
        ReloadSlotItem(slotMaterial);
        ReloadSlotItem(slotLucky);
        ReloadSlotItem(slotResult);
        UpdateInfo();
    }

    private void ReloadSlotItem(UcItemSlot slot)
    {
        if (slot == null || slot.Item == null)
        {
            return;
        }
        CharacterModel hero = SessionManager.CurrentCharacter;
        if (hero != null)
        {
            ItemInstance freshItem = _repo.GetItemAt(hero.CharacterID, Enums.ItemLocation.Storage, slot.Item.SlotIndex);
            if (freshItem != null && freshItem.InstanceID == slot.Item.InstanceID)
            {
                slot.SetItem(freshItem);
            }
            else
            {
                slot.SetItem(null);
            }
        }
    }

    private void SetupUI()
    {
        int startX = 30;
        int startY = 60;
        int gap = 80;
        AllowDrop = true;
        base.DragEnter += UcBlacksmith_DragEnter;
        base.DragDrop += UcBlacksmith_DragDrop;
        slotMain = CreateSlot(0, startX, startY, "EKİPMAN");
        slotMaterial = CreateSlot(1, startX + gap, startY, "MALZEME");
        slotLucky = CreateSlot(2, startX + gap * 2, startY, "ŞANS");
        Label lblArrow = new Label
        {
            Text = "▼",
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 16f, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(startX + gap - 10, startY + 60)
        };
        base.Controls.Add(lblArrow);
        slotResult = CreateSlot(3, startX + gap - 10, startY + 100, "SONUÇ");
        slotResult.BackColor = Color.FromArgb(60, 70, 60);
        lblInfo = new Label
        {
            Text = "Gerekli: -",
            ForeColor = Color.Silver,
            Location = new Point(20, 230),
            AutoSize = true
        };
        base.Controls.Add(lblInfo);
        lblChance = new Label
        {
            Text = "Şans: -",
            ForeColor = Color.Gold,
            Location = new Point(200, 230),
            AutoSize = true,
            Font = new Font("Segoe UI", 10f, FontStyle.Bold)
        };
        base.Controls.Add(lblChance);
        btnUpgrade = new Button
        {
            Text = "YÜKSELT",
            Size = new Size(100, 35),
            Location = new Point(120, 260),
            BackColor = Color.DarkRed,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnUpgrade.Click += BtnUpgrade_Click;
        base.Controls.Add(btnUpgrade);
    }

    private UcItemSlot CreateSlot(int index, int x, int y, string placeholder)
    {
        Label lbl = new Label
        {
            Text = placeholder,
            ForeColor = Color.Gray,
            Font = new Font("Segoe UI", 7f),
            Location = new Point(x, y - 15),
            AutoSize = true
        };
        base.Controls.Add(lbl);
        UcItemSlot slot = new UcItemSlot(index);
        slot.Location = new Point(x, y);
        slot.AllowDrop = true;
        slot.OnItemDropped += Slot_OnItemDropped;
        slot.OnItemChanged += delegate
        {
            UpdateInfo();
        };
        slot.MouseUp += delegate (object? s, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                if (index == 3)
                {
                    ResultSlot_Click(s, e);
                }
                else
                {
                    ReturnSlotItem((UcItemSlot)s);
                }
            }
        };
        base.Controls.Add(slot);
        _slots.Add(slot);
        return slot;
    }

    private void Slot_OnItemDropped(object sender, ItemInstance droppedItem)
    {
        UcItemSlot targetSlot = (UcItemSlot)sender;
        CharacterModel hero = SessionManager.CurrentCharacter;
        if (hero == null)
        {
            return;
        }
        if (targetSlot.Item != null && (targetSlot != slotMaterial || !droppedItem.IsStackable || targetSlot.Item.TemplateID != droppedItem.TemplateID) && droppedItem.Location == Enums.ItemLocation.Inventory)
        {
            if (!ValidateForTarget(droppedItem, targetSlot, out var tmpMsg))
            {
                ShowStatus(tmpMsg ?? "Uygun olmayan eşya.", Color.OrangeRed);
                return;
            }
            ItemInstance stored = targetSlot.Item;
            int heroId = SessionManager.CurrentCharacter.CharacterID;
            bool movedBack = false;
            if (stored.IsStackable)
            {
                ItemInstance existing = _repo.FindStackableItemWithCapacity(heroId, stored.TemplateID, stored.MaxStack);
                if (existing != null)
                {
                    movedBack = _invManager.TransferItem(SessionManager.CurrentCharacter, stored, Enums.ItemLocation.Inventory, existing.SlotIndex);
                }
            }
            if (!movedBack)
            {
                int empty = _repo.FindFirstEmptyInventorySlot(heroId);
                if (empty != -1)
                {
                    movedBack = _invManager.TransferItem(SessionManager.CurrentCharacter, stored, Enums.ItemLocation.Inventory, empty);
                }
            }
            if (!movedBack)
            {
                ShowStatus("Envanterde yer yok - takas yapılamadı.", Color.OrangeRed);
                return;
            }
            if (!_repo.MoveItemLocation(droppedItem.InstanceID, Enums.ItemLocation.Storage, targetSlot.SlotIndex))
            {
                ShowStatus("Eşya depoya taşınamadı.", Color.OrangeRed);
                return;
            }
            ItemInstance storedDef = _repo.GetItemAt(heroId, Enums.ItemLocation.Storage, targetSlot.SlotIndex);
            targetSlot.SetItem(storedDef);
            if (base.ParentForm is FormMain fmInv)
            {
                fmInv.RefreshInventoryOnly();
            }
            UpdateInfo();
        }
        else if (droppedItem.Location == Enums.ItemLocation.Inventory)
        {
            if (!ValidateForTarget(droppedItem, targetSlot, out var invalidMsg))
            {
                ShowStatus(invalidMsg ?? "Uygun olmayan eşya.", Color.OrangeRed);
                return;
            }
            if (targetSlot == slotMaterial && droppedItem.IsStackable)
            {
                if (targetSlot.Item != null && targetSlot.Item.TemplateID == droppedItem.TemplateID)
                {
                    int current = targetSlot.Item.Count;
                    int max = ((targetSlot.Item.MaxStack > 0) ? targetSlot.Item.MaxStack : 9999);
                    int space = Math.Max(0, max - current);
                    if (space > 0)
                    {
                        int toMove = Math.Min(space, droppedItem.Count);
                        _repo.IncrementItemCount(targetSlot.Item.InstanceID, toMove);
                        _repo.ConsumeItem(droppedItem.InstanceID, toMove);
                        ItemInstance updatedTarget = _repo.GetItemAt(hero.CharacterID, Enums.ItemLocation.Storage, targetSlot.SlotIndex);
                        targetSlot.SetItem(updatedTarget);
                        if (droppedItem.Count <= toMove)
                        {
                            if (base.ParentForm is FormMain fmx)
                            {
                                fmx.RefreshInventoryOnly();
                            }
                        }
                        else if (base.ParentForm is FormMain fmy)
                        {
                            fmy.RefreshInventoryOnly();
                        }
                        UpdateInfo();
                    }
                    else
                    {
                        ShowStatus("Malzeme slotu dolu.", Color.Orange);
                    }
                    return;
                }
                if (targetSlot.Item == null)
                {
                    int max2 = ((droppedItem.MaxStack > 0) ? droppedItem.MaxStack : 9999);
                    int toStore = Math.Min(max2, droppedItem.Count);
                    ItemInstance newItem = new ItemInstance
                    {
                        TemplateID = droppedItem.TemplateID,
                        OwnerID = hero.CharacterID,
                        SlotIndex = targetSlot.SlotIndex,
                        Grade = droppedItem.Grade,
                        Location = Enums.ItemLocation.Storage,
                        Count = toStore,
                        Name = droppedItem.Name,
                        ItemType = droppedItem.ItemType,
                        IsStackable = droppedItem.IsStackable,
                        MaxStack = droppedItem.MaxStack,
                        UpgradeLevel = droppedItem.UpgradeLevel
                    };
                    if (!_repo.AddItemDirectly(newItem))
                    {
                        ShowStatus("Malzeme depoya eklenemedi.", Color.OrangeRed);
                        return;
                    }
                    _repo.ConsumeItem(droppedItem.InstanceID, toStore);
                    ItemInstance stored2 = _repo.GetItemAt(hero.CharacterID, Enums.ItemLocation.Storage, targetSlot.SlotIndex);
                    targetSlot.SetItem(stored2);
                    if (base.ParentForm is FormMain fmx2)
                    {
                        fmx2.RefreshInventoryOnly();
                    }
                    UpdateInfo();
                    return;
                }
            }
            if (!_repo.MoveItemLocation(droppedItem.InstanceID, Enums.ItemLocation.Storage, targetSlot.SlotIndex))
            {
                ShowStatus("Eşya depoya taşınamadı.", Color.OrangeRed);
                return;
            }
            ItemInstance storedDef2 = _repo.GetItemAt(hero.CharacterID, Enums.ItemLocation.Storage, targetSlot.SlotIndex);
            targetSlot.SetItem(storedDef2);
            UpdateInfo();
            if (base.ParentForm is FormMain fm)
            {
                fm.RefreshInventoryOnly();
            }
        }
        else
        {
            if (droppedItem.Location != Enums.ItemLocation.Storage)
            {
                return;
            }
            UcItemSlot sourceSlot = new UcItemSlot[4] { slotMain, slotMaterial, slotLucky, slotResult }.FirstOrDefault((UcItemSlot s) => s != null && s.Item != null && s.Item.InstanceID == droppedItem.InstanceID);
            if (sourceSlot == null)
            {
                if (!ValidateForTarget(droppedItem, targetSlot, out var invalidMsg2))
                {
                    ShowStatus(invalidMsg2 ?? "Uygun olmayan eşya.", Color.OrangeRed);
                    return;
                }
                if (!_repo.MoveItemLocation(droppedItem.InstanceID, Enums.ItemLocation.Storage, targetSlot.SlotIndex))
                {
                    ShowStatus("Depodaki eşya taşınamadı.", Color.OrangeRed);
                    return;
                }
                ItemInstance storedExt = _repo.GetItemAt(hero.CharacterID, Enums.ItemLocation.Storage, targetSlot.SlotIndex);
                targetSlot.SetItem(storedExt);
                UpdateInfo();
            }
            else
            {
                if (sourceSlot == targetSlot)
                {
                    return;
                }
                if (!ValidateForTarget(droppedItem, targetSlot, out var invalidMsg3))
                {
                    ShowStatus(invalidMsg3 ?? "Uygun olmayan eşya.", Color.OrangeRed);
                }
                else if (targetSlot == slotMaterial && sourceSlot.Item != null && sourceSlot.Item.IsStackable && targetSlot.Item != null && targetSlot.Item.TemplateID == sourceSlot.Item.TemplateID)
                {
                    int current2 = targetSlot.Item.Count;
                    int max3 = ((targetSlot.Item.MaxStack > 0) ? targetSlot.Item.MaxStack : 9999);
                    int space2 = Math.Max(0, max3 - current2);
                    if (space2 > 0)
                    {
                        int toMove2 = Math.Min(space2, sourceSlot.Item.Count);
                        _repo.IncrementItemCount(targetSlot.Item.InstanceID, toMove2);
                        _repo.ConsumeItem(sourceSlot.Item.InstanceID, toMove2);
                        ReloadSlotItem(sourceSlot);
                        ReloadSlotItem(targetSlot);
                        UpdateInfo();
                        if (base.ParentForm is FormMain fm3)
                        {
                            fm3.RefreshInventoryOnly();
                        }
                    }
                    else
                    {
                        ShowStatus("Malzeme slotu dolu.", Color.Orange);
                    }
                }
                else if (sourceSlot != null && targetSlot.Item != null)
                {
                    int srcIndex = sourceSlot.Item.SlotIndex;
                    int tgtIndex = targetSlot.Item.SlotIndex;
                    bool movedTarget = _repo.MoveItemLocation(targetSlot.Item.InstanceID, Enums.ItemLocation.Storage, srcIndex);
                    bool movedSource = _repo.MoveItemLocation(sourceSlot.Item.InstanceID, Enums.ItemLocation.Storage, tgtIndex);
                    if (!movedTarget || !movedSource)
                    {
                        ShowStatus("Eşya takası başarısız.", Color.OrangeRed);
                        return;
                    }
                    ReloadSlotItem(sourceSlot);
                    ReloadSlotItem(targetSlot);
                    UpdateInfo();
                    if (base.ParentForm is FormMain fmSwap)
                    {
                        fmSwap.RefreshInventoryOnly();
                    }
                }
                else if (!_repo.MoveItemLocation(droppedItem.InstanceID, Enums.ItemLocation.Storage, targetSlot.SlotIndex))
                {
                    ShowStatus("Depo içinde taşınamadı.", Color.OrangeRed);
                }
                else
                {
                    sourceSlot.SetItem(null);
                    ItemInstance newStored = _repo.GetItemAt(hero.CharacterID, Enums.ItemLocation.Storage, targetSlot.SlotIndex);
                    targetSlot.SetItem(newStored);
                    UpdateInfo();
                    if (base.ParentForm is FormMain fm4)
                    {
                        fm4.RefreshInventoryOnly();
                    }
                }
            }
        }
        bool ValidateForTarget(ItemInstance item, UcItemSlot target, out string message)
        {
            message = null;
            if (target == slotMain)
            {
                if (!_upgManager.IsUpgradeable(item))
                {
                    message = "Sadece ekipman (silah/zırh) koyabilirsiniz.";
                    return false;
                }
            }
            else if (target == slotMaterial)
            {
                if (item.TemplateID != 7)
                {
                    message = "Bu slot yalnızca yükseltme malzemesi kabul eder.";
                    return false;
                }
            }
            else if (target == slotLucky)
            {
                bool isLuckyByList = _upgManager.IsLuckyItem(item.TemplateID);
                bool hasEffectValue = item.EffectValue > 0;
                if (!isLuckyByList && !hasEffectValue)
                {
                    message = "Bu slot yalnızca şans eşyası kabul eder.";
                    return false;
                }
            }
            else if (target == slotResult)
            {
                message = "Sonuç slotuna doğrudan koyamazsınız.";
                return false;
            }
            return true;
        }
    }

    private void UpdateInfo()
    {
        if (slotMain.Item == null)
        {
            lblInfo.Text = "Lütfen bir ekipman koyun.";
            lblInfo.ForeColor = Color.Silver;
            lblChance.Text = "";
            return;
        }
        int reqCount = _upgManager.GetRequiredMaterialCount(slotMain.Item.UpgradeLevel);
        int baseChance = _upgManager.GetBaseSuccessRate(slotMain.Item.UpgradeLevel);
        int bonusChance = 0;
        if (slotLucky.Item != null)
        {
            bonusChance = slotLucky.Item.EffectValue;
        }
        int rawTotal = baseChance + bonusChance;
        int totalChance = Math.Min(100, rawTotal);
        if (rawTotal > 100)
        {
            lblChance.ForeColor = Color.OrangeRed;
            ShowStatus("Dikkat: Başarı şansı %100'ü aşıyor!", Color.Orange);
        }
        lblInfo.Text = $"Gerekli Malzeme: {reqCount} adet";
        lblInfo.ForeColor = Color.Silver;
        lblChance.Text = $"Başarı Şansı: %{totalChance}";
        if (totalChance < 50)
        {
            lblChance.ForeColor = Color.Red;
        }
        else if (totalChance < 80)
        {
            lblChance.ForeColor = Color.Orange;
        }
        else
        {
            lblChance.ForeColor = Color.LimeGreen;
        }
    }

    private async void ShowStatus(string text, Color color)
    {
        lblInfo.Text = text;
        lblInfo.ForeColor = color;
        try
        {
            await Task.Delay(3000);
        }
        catch
        {
        }
        UpdateInfo();
    }

    public void FlushSlotsToInventory()
    {
        CharacterModel hero = SessionManager.CurrentCharacter;
        if (hero == null)
        {
            return;
        }
        UcItemSlot[] slots = new UcItemSlot[4] { slotMain, slotMaterial, slotLucky, slotResult };
        UcItemSlot[] array = slots;
        foreach (UcItemSlot s in array)
        {
            if (s == null || s.Item == null)
            {
                continue;
            }
            try
            {
                if (s.Item.Location == Enums.ItemLocation.Storage)
                {
                    _invManager.SmartWithdraw(hero, s.Item);
                }
                else if (s.Item.InstanceID > 0 && SessionManager.IsReserved(s.Item.InstanceID))
                {
                    SessionManager.UnreserveItem(s.Item.InstanceID);
                }
                else
                {
                    if (s.Item.Location == Enums.ItemLocation.Inventory)
                    {
                        continue;
                    }
                    bool moved = false;
                    if (s.Item.IsStackable)
                    {
                        ItemInstance existing = _repo.FindStackableItemWithCapacity(hero.CharacterID, s.Item.TemplateID, s.Item.MaxStack);
                        if (existing != null)
                        {
                            moved = _invManager.TransferItem(hero, s.Item, Enums.ItemLocation.Inventory, existing.SlotIndex);
                            if (moved)
                            {
                                ShowStatus("Eşya envantere taşındı ve birleştirildi.", Color.LimeGreen);
                            }
                        }
                    }
                    if (moved)
                    {
                        continue;
                    }
                    int empty = _repo.FindFirstEmptyInventorySlot(hero.CharacterID);
                    if (empty != -1)
                    {
                        if (_invManager.TransferItem(hero, s.Item, Enums.ItemLocation.Inventory, empty))
                        {
                            ShowStatus("Eşya envantere taşındı.", Color.LimeGreen);
                        }
                        else
                        {
                            ShowStatus("Eşya envantere taşınamadı.", Color.OrangeRed);
                        }
                    }
                    else
                    {
                        ShowStatus("Envanterde yer yok!", Color.OrangeRed);
                    }
                    continue;
                }
            }
            catch
            {
            }
            finally
            {
                s.SetItem(null);
            }
        }
        NotifyMainForm();
        ShowStatus("Demircide kalan eşyalar envantere taşındı.", Color.LimeGreen);
    }

    private void BtnUpgrade_Click(object sender, EventArgs e)
    {
        if (slotMain.Item == null)
        {
            return;
        }
        if (slotResult.Item != null)
        {
            ShowStatus("Önce sonuç slotundaki eşyayı almalısın!", Color.OrangeRed);
            return;
        }
        string statusMsg = "";
        Color statusColor = Color.White;
        bool success = false;
        switch (_upgManager.PerformUpgrade(slotMain.Item, slotMaterial.Item, slotLucky.Item))
        {
            case -1:
                ShowStatus("Yeterli yükseltme malzemen yok!", Color.OrangeRed);
                return;
            case 1:
                {
                    statusMsg = "BAŞARILI! Eşya seviye atladı.";
                    statusColor = Color.LimeGreen;
                    success = true;
                    ItemInstance upgradedItem = slotMain.Item;
                    upgradedItem.UpgradeLevel++;
                    slotResult.SetItem(upgradedItem);
                    slotMain.SetItem(null);
                    break;
                }
            default:
                statusMsg = "BAŞARISIZ... Eşya yok oldu.";
                statusColor = Color.OrangeRed;
                slotMain.SetItem(null);
                break;
        }
        ReloadSlotItem(slotMaterial);
        ReloadSlotItem(slotLucky);
        NotifyMainForm();
        ShowStatus(statusMsg, statusColor);
    }

    private void ReturnSlotItem(UcItemSlot slot)
    {
        if (slot == null || slot.Item == null)
        {
            return;
        }
        CharacterModel hero = SessionManager.CurrentCharacter;
        if (slot.Item.InstanceID > 0 && SessionManager.IsReserved(slot.Item.InstanceID))
        {
            SessionManager.UnreserveItem(slot.Item.InstanceID);
            slot.SetItem(null);
            NotifyMainForm();
            return;
        }
        if (slot.Item.IsStackable)
        {
            while (true)
            {
                ItemInstance existing = _repo.FindStackableItemWithCapacity(hero.CharacterID, slot.Item.TemplateID, slot.Item.MaxStack);
                if (existing == null)
                {
                    break;
                }
                bool moved = _invManager.TransferItem(hero, slot.Item, Enums.ItemLocation.Inventory, existing.SlotIndex);
                ItemInstance refreshedItem = _repo.GetItemAt(hero.CharacterID, Enums.ItemLocation.Storage, slot.Item.SlotIndex);
                if (refreshedItem == null)
                {
                    slot.SetItem(null);
                    NotifyMainForm();
                    UpdateInfo();
                    ShowStatus("Eşya envantere taşındı.", Color.LimeGreen);
                    return;
                }
                slot.SetItem(refreshedItem);
            }
        }
        if (slot.Item != null)
        {
            int emptySlot = _repo.FindFirstEmptyInventorySlot(hero.CharacterID);
            if (emptySlot == -1)
            {
                ShowStatus("Envanterde yer yok!", Color.OrangeRed);
                return;
            }
            if (_invManager.TransferItem(hero, slot.Item, Enums.ItemLocation.Inventory, emptySlot))
            {
                slot.SetItem(null);
                ShowStatus("Eşya envantere taşındı.", Color.LimeGreen);
            }
            else
            {
                ShowStatus("Taşıma hatası.", Color.OrangeRed);
            }
        }
        NotifyMainForm();
        UpdateInfo();
    }

    private void ResultSlot_Click(object sender, MouseEventArgs e)
    {
        if (slotResult.Item != null)
        {
            ReturnSlotItem(slotResult);
        }
    }

    private void NotifyMainForm()
    {
        if (base.ParentForm is FormMain main)
        {
            main.RefreshStats();
        }
    }

    private void UcBlacksmith_DragEnter(object sender, DragEventArgs e)
    {
        try
        {
            if (e.Data != null)
            {
                e.Effect = DragDropEffects.Move;
                return;
            }
        }
        catch
        {
        }
        e.Effect = DragDropEffects.None;
    }

    private void UcBlacksmith_DragDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(typeof(ItemInstance)))
        {
            ItemInstance item = (ItemInstance)e.Data.GetData(typeof(ItemInstance));
            Point screenPoint = new Point(e.X, e.Y);
            Point clientPoint = PointToClient(screenPoint);
            UcItemSlot target = _slots.FirstOrDefault((UcItemSlot s) => s.Bounds.Contains(clientPoint));
            if (target != null)
            {
                Slot_OnItemDropped(target, item);
            }
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
        base.SuspendLayout();
        base.AutoScaleDimensions = new System.Drawing.SizeF(7f, 15f);
        base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        base.Name = "UcBlacksmith";
        base.ResumeLayout(false);
    }
}
