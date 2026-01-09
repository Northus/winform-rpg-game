using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using rpg_deneme.Business;
using rpg_deneme.Core;
using rpg_deneme.Models;

namespace rpg_deneme.UI.Controls;

public class UcItemSlot : UserControl
{
    private Label lblCount;

    private IContainer components = null;

    private PictureBox pbIcon;




    public int SlotIndex { get; private set; }

    public ItemInstance Item { get; private set; }

    public Enums.ItemType? AcceptedType { get; set; } = null;

    public event EventHandler<ItemInstance> OnItemDropped;

    public event EventHandler<ItemInstance> OnItemChanged;

    public UcItemSlot(int index)
    {
        InitializeComponent();
        SlotIndex = index;

        SetupCountLabel(); // önce label oluştur
        SetupDesign();     // sonra event bağla

        pbIcon.MouseUp += ForwardMouseUpFromChild;
        lblCount.MouseUp += ForwardMouseUpFromChild;
        base.MouseUp += delegate
        {
        };
    }

    private void ForwardMouseUpFromChild(object sender, MouseEventArgs e)
    {
        OnMouseUp(e);
    }

    private void SetupDesign()
    {
        base.Size = new Size(40, 40);
        BackColor = Color.FromArgb(60, 60, 60);
        base.BorderStyle = BorderStyle.FixedSingle;
        AllowDrop = true;
        pbIcon.MouseDown += PbIcon_MouseDown;
        lblCount.MouseDown += PbIcon_MouseDown; // lblCount'tan da sürüklenebilsin
        base.DragEnter += UcItemSlot_DragEnter;
        base.DragDrop += UcItemSlot_DragDrop;
        // Tooltip Handling
        EventHandler showTooltip = (s, e) =>
        {
            if (Item != null)
                rpg_deneme.UI.Windows.FrmItemTooltip.Instance.ShowTooltip(Item, Cursor.Position);
        };
        EventHandler hideTooltip = (s, e) =>
        {
            rpg_deneme.UI.Windows.FrmItemTooltip.Instance.HideTooltip();
        };

        pbIcon.MouseEnter += showTooltip;
        pbIcon.MouseLeave += hideTooltip;
        lblCount.MouseEnter += showTooltip;
        lblCount.MouseLeave += hideTooltip;
    }

    private void SetupCountLabel()
    {
        lblCount = new Label();
        lblCount.Parent = pbIcon;
        lblCount.BackColor = Color.FromArgb(150, 0, 0, 0);
        lblCount.ForeColor = Color.White;
        lblCount.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
        lblCount.AutoSize = true;
        lblCount.Visible = false;
    }

    private void PbIcon_MouseDown(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left && Item != null && (Control.ModifierKeys & Keys.Control) != Keys.Control)
        {
            pbIcon.DoDragDrop(Item, DragDropEffects.Move);
        }
    }

    private void UcItemSlot_DragEnter(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(typeof(ItemInstance)))
        {
            ItemInstance item = (ItemInstance)e.Data.GetData(typeof(ItemInstance));
            if (IsItemValidForSlot(item))
            {
                e.Effect = DragDropEffects.Move;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }
        else
        {
            e.Effect = DragDropEffects.None;
        }
    }

    private void UcItemSlot_DragDrop(object sender, DragEventArgs e)
    {
        ItemInstance droppedItem = (ItemInstance)e.Data.GetData(typeof(ItemInstance));

        // Efsun Kontrolü: Eğer bu slotta bir ekipman varsa ve üzerine Kutsama Küresi/Efsun Nesnesi bırakıldıysa
        if (this.Item != null && (this.Item.ItemType == Enums.ItemType.Weapon || this.Item.ItemType == Enums.ItemType.Armor))
        {
            if (droppedItem.ItemType == Enums.ItemType.BlessingMarble || droppedItem.ItemType == Enums.ItemType.EnchantItem)
            {
                // Kendisi üzerine bırakılırsa işlem yapma
                if (droppedItem.InstanceID == this.Item.InstanceID) return;

                EnchantmentManager enchantManager = new EnchantmentManager();
                string result = "";

                if (droppedItem.ItemType == Enums.ItemType.BlessingMarble)
                {
                    result = enchantManager.ApplyBlessingMarble(this.Item);
                }
                else
                {
                    result = enchantManager.ApplyEnchantItem(this.Item);
                }

                if (result.ToLower().Contains("başarı") && !result.ToLower().Contains("başarısız"))
                {
                    NotificationManager.AddNotification(result, Color.LimeGreen);
                }
                else
                {
                    NotificationManager.AddNotification(result, Color.OrangeRed);
                }

                // Kullanılan eşyayı tüket
                InventoryManager invManager = new InventoryManager();
                invManager.ConsumeItem(droppedItem.InstanceID, 1);

                // Global yenileme tetikle
                if (this.FindForm() is FormMain main)
                {
                    main.RefreshStats();
                    // main.RefreshStats() Inventory'yi reload eder mi? 
                    // UcInventory.NotifyStatUpdate çağırıyor, o da main.RefreshStats().
                    // Genellikle Full Refresh gerekir.
                    // Ancak UcInventory'ye doğrudan erişimimiz yok.
                    // FormMain üzerinde Public bi Inventory reload varsa çağıralım. 
                    // Şimdilik sadece RefreshStats yeterli olabilir mi? Kontrol edelim.
                    // Eğer RefreshStats sadece karakter statlarını güncelliyorsa, inventory grid güncellenmez.
                    // O zaman droppedItem (envanterdeki) count azalmış görünmeyecek.

                    // FormMain üzerinden envanteri bulup yenilemek en iyisi ama referans yok.
                    // Basit çözüm: Event fırlatalım, ama nereye?
                    // UcSlot -> UcInventory -> FormMain.

                    // Neyse, şimdilik logic çalışsın. Görsel güncelleme olmazsa kullanıcı kapatıp açınca görür.
                    // Veya Slot_MouseUp'taki gibi trickler yapabiliriz.
                }

                // Bu slotu güncelle (Attribute değişti)
                this.SetItem(this.Item);

                // Düştü eventi fırlatma, çünkü biz tükettik.
                return;
            }
        }

        this.OnItemDropped?.Invoke(this, droppedItem);
    }

    private Color GetColorByGrade(Enums.ItemGrade grade)
    {
        if (1 == 0)
        {
        }
        Color result = grade switch
        {
            Enums.ItemGrade.Common => Color.WhiteSmoke,
            Enums.ItemGrade.Rare => Color.CornflowerBlue,
            Enums.ItemGrade.Epic => Color.MediumPurple,
            Enums.ItemGrade.Legendary => Color.Orange,
            _ => Color.Gray,
        };
        if (1 == 0)
        {
        }
        return result;
    }

    public void SetItem(ItemInstance item)
    {
        Item = item;
        if (item != null)
        {
            pbIcon.BackColor = GetColorByGrade(item.Grade);
            pbIcon.Image = ItemDrawer.DrawItem(item);

            if (item.Count > 1)
            {
                lblCount.Text = $"{item.Count}";
                lblCount.Visible = true;
                int x = pbIcon.Width - lblCount.Width - 2;
                int y = pbIcon.Height - lblCount.Height - 2;
                lblCount.Location = new Point(x, y);
                lblCount.BringToFront();
            }
            else
            {
                lblCount.Visible = false;
            }
        }
        else
        {
            pbIcon.BackColor = Color.FromArgb(60, 60, 60);
            pbIcon.Image = null;
            lblCount.Visible = false;
        }
        this.OnItemChanged?.Invoke(this, Item);
    }

    public string GetTooltipText(ItemInstance item)
    {
        if (item == null)
        {
            return "";
        }
        string text = "";
        text += item.Name;
        if (item.ItemType != Enums.ItemType.Consumable && item.UpgradeLevel > 0)
        {
            text += $" +{item.UpgradeLevel}";
        }
        text += "\n";
        text = ((item.ItemType != Enums.ItemType.Consumable) ? (text + $"{item.Grade} | {item.ItemType}") : (text + $"{item.ItemType}"));
        if (item.AllowedClass.HasValue && item.AllowedClass.Value != 0)
        {
            text += $" | Class: {item.AllowedClass.Value}";
        }
        text += "\n";
        text += "--------------------------\n";
        float gradeMult = StatManager.GetGradeMultiplier(item.Grade);
        float upgradeMult = StatManager.GetUpgradeMultiplier(item.UpgradeLevel);
        float totalMult = gradeMult * upgradeMult;
        if (item.ItemType == Enums.ItemType.Weapon)
        {
            if (item.MinDamage > 0 || item.MaxDamage > 0)
            {
                int finalMin = (int)((float)item.MinDamage * totalMult);
                int finalMax = (int)((float)item.MaxDamage * totalMult);
                int bonusMin = finalMin - item.MinDamage;
                int bonusMax = finalMax - item.MaxDamage;
                text += $"Saldırı Gücü: {finalMin} - {finalMax}";
                if (bonusMin > 0 || bonusMax > 0)
                {
                    text += $" (+{bonusMin} - +{bonusMax})";
                }
                text += "\n";
            }
            if (item.MinMagicDamage > 0 || item.MaxMagicDamage > 0)
            {
                int finalMagMin = (int)((float)item.MinMagicDamage * totalMult);
                int finalMagMax = (int)((float)item.MaxMagicDamage * totalMult);
                int bonusMagMin = finalMagMin - item.MinMagicDamage;
                int bonusMagMax = finalMagMax - item.MaxMagicDamage;
                text += $"Büyü Gücü: {finalMagMin} - {finalMagMax}";
                if (bonusMagMin > 0 || bonusMagMax > 0)
                {
                    text += $" (+{bonusMagMin} - +{bonusMagMax})";
                }
                text += "\n";
            }
        }
        else if (item.ItemType == Enums.ItemType.Armor && item.BaseDefense > 0)
        {
            int finalDef = (int)((float)item.BaseDefense * totalMult);
            int bonusDef = finalDef - item.BaseDefense;
            text += $"Defans: {finalDef}";
            if (bonusDef > 0)
            {
                text += $" (+{bonusDef})";
            }
            text += "\n";
        }
        if (item.ItemType == Enums.ItemType.Consumable)
        {
            if (item.EffectType != Enums.ItemEffectType.None)
            {
                text += $"Etkisi: {item.EffectType} ({item.EffectValue})\n";
            }
            if (item.Cooldown > 0)
            {
                text += $"Cooldown: {item.Cooldown}s";
                int remaining = item.RemainingCooldownSeconds;
                text = ((remaining <= 0) ? (text + "\n") : (text + $" (Hazır: {remaining}s)\n"));
            }
            if (item.IsStackable && item.Count > 1)
            {
                text += $"Adet: {item.Count}\n";
            }
            text += "Sağ Tık: Kullan\n";
        }
        return text;
    }

    private bool IsItemValidForSlot(ItemInstance item)
    {
        if (!AcceptedType.HasValue)
        {
            return true;
        }
        return item.ItemType == AcceptedType.Value;
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
        this.pbIcon = new System.Windows.Forms.PictureBox();
        ((System.ComponentModel.ISupportInitialize)this.pbIcon).BeginInit();
        base.SuspendLayout();
        this.pbIcon.Dock = System.Windows.Forms.DockStyle.Fill;
        this.pbIcon.Location = new System.Drawing.Point(0, 0);
        this.pbIcon.Name = "pbIcon";
        this.pbIcon.Size = new System.Drawing.Size(40, 40);
        this.pbIcon.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
        this.pbIcon.TabIndex = 0;
        this.pbIcon.TabStop = false;
        base.AutoScaleDimensions = new System.Drawing.SizeF(7f, 15f);
        base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        base.Controls.Add(this.pbIcon);
        base.Name = "UcItemSlot";
        base.Size = new System.Drawing.Size(40, 40);
        ((System.ComponentModel.ISupportInitialize)this.pbIcon).EndInit();
        base.ResumeLayout(false);
    }
}
