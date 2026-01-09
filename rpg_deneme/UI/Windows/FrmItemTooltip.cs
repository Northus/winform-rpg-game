using System;
using System.Drawing;
using System.Windows.Forms;
using rpg_deneme.Business;
using rpg_deneme.Core;
using rpg_deneme.Models;

namespace rpg_deneme.UI.Windows;

public class FrmItemTooltip : Form
{
    private static FrmItemTooltip _instance;
    public static FrmItemTooltip Instance
    {
        get
        {
            if (_instance == null || _instance.IsDisposed)
            {
                _instance = new FrmItemTooltip();
            }
            return _instance;
        }
    }

    private Panel pnlBorder;
    private Panel pnlContent;
    private Label lblName;
    private Label lblType;
    private Label lblStats; // Base stats
    private Label lblEnchants; // Future use
    private ItemInstance _lastItem;

    private FrmItemTooltip()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        this.FormBorderStyle = FormBorderStyle.None;
        this.ShowInTaskbar = false;
        this.TopMost = true;
        this.StartPosition = FormStartPosition.Manual;
        this.AutoSize = true;
        this.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        this.DoubleBuffered = true;

        // Border Panel (Outer)
        pnlBorder = new Panel();
        pnlBorder.AutoSize = true;
        pnlBorder.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        pnlBorder.Padding = new Padding(2); // Border thickness
        pnlBorder.Dock = DockStyle.Fill;
        pnlBorder.BackColor = Color.Gray; // Default border color

        // Content Panel (Inner)
        pnlContent = new Panel();
        pnlContent.AutoSize = true;
        pnlContent.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        pnlContent.BackColor = Color.FromArgb(20, 20, 20); // Dark background
        pnlContent.Dock = DockStyle.Fill;
        pnlContent.Padding = new Padding(10);

        // Layout inside Content
        FlowLayoutPanel flowLayout = new FlowLayoutPanel();
        flowLayout.AutoSize = true;
        flowLayout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        flowLayout.FlowDirection = FlowDirection.TopDown;
        flowLayout.Dock = DockStyle.Fill;
        flowLayout.BackColor = Color.Transparent;
        // flowLayout.WrapContents = false; // Important so it grows vertically

        // Header (Name + Upgrade)
        lblName = new Label();
        lblName.AutoSize = true;
        lblName.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        lblName.ForeColor = Color.White;
        lblName.Margin = new Padding(0, 0, 0, 5);

        // Subheader (Type | Class)
        lblType = new Label();
        lblType.AutoSize = true;
        lblType.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
        lblType.ForeColor = Color.LightGray;
        lblType.Margin = new Padding(0, 0, 0, 10);

        // Stats
        lblStats = new Label();
        lblStats.AutoSize = true;
        lblStats.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
        lblStats.ForeColor = Color.White;
        lblStats.Margin = new Padding(0, 0, 0, 10);

        // Enchants (Placeholder)
        lblEnchants = new Label();
        lblEnchants.AutoSize = true;
        lblEnchants.Font = new Font("Segoe UI", 9F, FontStyle.Italic);
        lblEnchants.ForeColor = Color.CornflowerBlue; // Different color for stats
        lblEnchants.Margin = new Padding(0);

        flowLayout.Controls.Add(lblName);
        flowLayout.Controls.Add(lblType);
        flowLayout.Controls.Add(lblStats);
        flowLayout.Controls.Add(lblEnchants);

        pnlContent.Controls.Add(flowLayout);
        pnlBorder.Controls.Add(pnlContent);
        this.Controls.Add(pnlBorder);
    }

    protected override bool ShowWithoutActivation => true;

    public void ShowTooltip(ItemInstance item, Point location)
    {
        if (item == null)
        {
            Hide();
            return;
        }

        if (_lastItem != item)
        {
            UpdateContent(item);
            _lastItem = item;
        }

        // Adjust location to not go off-screen
        Rectangle screenBounds = Screen.FromPoint(location).WorkingArea;
        int x = location.X + 15;
        int y = location.Y + 15;

        // If simple check:
        if (x + this.Width > screenBounds.Right)
            x = location.X - this.Width - 10;

        if (y + this.Height > screenBounds.Bottom)
            y = location.Y - this.Height - 10;

        this.Location = new Point(x, y);

        if (!this.Visible)
        {
            this.Show();
        }
        else
        {
            this.BringToFront();
            this.Invalidate();
            // this.Update();
        }
    }

    public void HideTooltip()
    {
        if (this.Visible)
        {
            this.Hide();
        }
    }

    public void ShowSimpleTooltip(string text, Point location)
    {
        // Simple mode: just show one label with text
        _lastItem = null; // Reset last item since this is custom text

        // Flicker Check
        if (this.Visible && lblName.Text == text)
        {
            Rectangle screenBounds = Screen.FromPoint(location).WorkingArea;
            int x = location.X + 15;
            int y = location.Y + 15;
            if (x + this.Width > screenBounds.Right) x = location.X - this.Width - 10;
            if (y + this.Height > screenBounds.Bottom) y = location.Y - this.Height - 10;
            this.Location = new Point(x, y);
            return;
        }

        pnlBorder.BackColor = Color.Black;
        lblName.ForeColor = Color.White;
        lblName.Text = text;
        lblName.Visible = true;

        lblType.Visible = false;
        lblStats.Visible = false;
        lblEnchants.Visible = false;

        // Adjust location
        Rectangle screenBounds2 = Screen.FromPoint(location).WorkingArea;
        int x2 = location.X + 15;
        int y2 = location.Y + 15;

        if (x2 + this.Width > screenBounds2.Right) x2 = location.X - this.Width - 10;
        if (y2 + this.Height > screenBounds2.Bottom) y2 = location.Y - this.Height - 10;

        this.Location = new Point(x2, y2);

        if (!this.Visible) this.Show();
        else { this.BringToFront(); this.Invalidate(); }
    }

    private void UpdateContent(ItemInstance item)
    {
        // Restore visibility for standard item view
        lblName.Visible = true;
        lblType.Visible = true;
        lblStats.Visible = true;
        lblEnchants.Visible = true;

        Color gradeColor = GetColorByGrade(item.Grade);
        pnlBorder.BackColor = gradeColor;
        lblName.ForeColor = gradeColor;

        // Header: [GradeName] [ItemName] +[UpgradeLevel]
        string gradePrefix = GetGradeName(item.Grade);
        string upgradeSuffix = (item.ItemType != Enums.ItemType.Consumable && item.UpgradeLevel > 0) ? $" +{item.UpgradeLevel}" : "";
        lblName.Text = $"{gradePrefix} {item.Name}{upgradeSuffix}".Trim();

        // SubHeader
        string typeStr = $"{item.ItemType}";
        string classStr = (item.AllowedClass.HasValue && item.AllowedClass.Value != 0)
            ? $" | Class: {(Enums.CharacterClass)item.AllowedClass.Value}"
            : "";
        lblType.Text = $"{typeStr}{classStr}";

        // Stats
        lblStats.Text = GenerateStatsText(item);

        // Enchants
        if (item.Attributes != null && item.Attributes.Count > 0)
        {
            string enchantText = "Efsunlar:\n";
            foreach (var attr in item.Attributes)
            {
                // Format attribute display nicely
                string attrName = attr.AttributeType.ToString();
                string valStr = $"+{attr.Value}";

                // Special formatting based on type
                switch (attr.AttributeType)
                {
                    case Enums.ItemAttributeType.CriticalChance:
                    case Enums.ItemAttributeType.MaxHPPercent:
                    case Enums.ItemAttributeType.MaxManaPercent:
                    case Enums.ItemAttributeType.AttackSpeed:
                    case Enums.ItemAttributeType.BlockChance:
                        valStr = $"+{attr.Value}%";
                        break;
                }

                enchantText += $"- {attrName}: {valStr}\n";
            }
            lblEnchants.Text = enchantText.Trim();
        }
        else
        {
            lblEnchants.Text = "";
        }
    }

    private string GenerateStatsText(ItemInstance item)
    {
        string text = "";
        float gradeMult = StatManager.GetGradeMultiplier(item.Grade);
        float upgradeMult = StatManager.GetUpgradeMultiplier(item.UpgradeLevel);
        float totalMult = gradeMult * upgradeMult;

        if (item.ItemType == Enums.ItemType.Weapon)
        {
            // Attack Speed (Currently static or calc in Manager, but let's show whatever relevant)
            // StatManager.CalculateAttackSpeed uses Hero, here we only show item stats.

            if (item.MinDamage > 0 || item.MaxDamage > 0)
            {
                int finalMin = (int)((float)item.MinDamage * totalMult);
                int finalMax = (int)((float)item.MaxDamage * totalMult);
                int bonusMin = finalMin - item.MinDamage;
                int bonusMax = finalMax - item.MaxDamage;
                text += $"Saldırı Gücü: {finalMin} - {finalMax}";
                if (bonusMin > 0 || bonusMax > 0) text += $" (+{bonusMin} - +{bonusMax})";
                text += "\n";
            }
            if (item.MinMagicDamage > 0 || item.MaxMagicDamage > 0)
            {
                int finalMagMin = (int)((float)item.MinMagicDamage * totalMult);
                int finalMagMax = (int)((float)item.MaxMagicDamage * totalMult);
                int bonusMagMin = finalMagMin - item.MinMagicDamage;
                int bonusMagMax = finalMagMax - item.MaxMagicDamage;
                text += $"Büyü Gücü: {finalMagMin} - {finalMagMax}";
                if (bonusMagMin > 0 || bonusMagMax > 0) text += $" (+{bonusMagMin} - +{bonusMagMax})";
                text += "\n";
            }
        }
        else if (item.ItemType == Enums.ItemType.Armor && item.BaseDefense > 0)
        {
            int finalDef = (int)((float)item.BaseDefense * totalMult);
            int bonusDef = finalDef - item.BaseDefense;
            text += $"Defans: {finalDef}";
            if (bonusDef > 0) text += $" (+{bonusDef})";
            text += "\n";
        }
        else if (item.ItemType == Enums.ItemType.Consumable)
        {
            if (item.EffectType != Enums.ItemEffectType.None)
                text += $"Etkisi: {item.EffectType} ({item.EffectValue})\n";
            if (item.Cooldown > 0)
                text += $"Cooldown: {item.Cooldown}s\n";
            if (item.IsStackable && item.Count > 1)
                text += $"Adet: {item.Count}\n";
            text += "Sağ Tık: Kullan\n";
        }

        return text.Trim();
    }

    private Color GetColorByGrade(Enums.ItemGrade grade)
    {
        return grade switch
        {
            Enums.ItemGrade.Common => Color.WhiteSmoke,
            Enums.ItemGrade.Rare => Color.CornflowerBlue,
            Enums.ItemGrade.Epic => Color.MediumPurple,
            Enums.ItemGrade.Legendary => Color.Orange,
            _ => Color.Gray,
        };
    }

    private string GetGradeName(Enums.ItemGrade grade)
    {
        return grade switch
        {
            Enums.ItemGrade.Common => "Common",
            Enums.ItemGrade.Rare => "Rare",
            Enums.ItemGrade.Epic => "Epic",
            Enums.ItemGrade.Legendary => "Legendary",
            _ => "",
        };
    }
}
