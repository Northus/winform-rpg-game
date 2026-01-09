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

public class UcMerchant : GameWindow
{
    private FlowLayoutPanel pnlGrid;

    private ShopManager _manager = new ShopManager();

    private Label lblInfo;

    private int _currentShopId;



    private IContainer components = null;

    public event EventHandler OnInventoryUpdateNeeded;

    public event EventHandler<ItemInstance> OnSellRequested;

    public UcMerchant(int shopId)
    {
        InitializeComponent();
        base.Title = "MARKET";
        base.Size = new Size(300, 450);
        _currentShopId = shopId;

        SetupUI();
        LoadShop(shopId);
    }

    private void SetupUI()
    {
        lblInfo = new Label
        {
            Text = "",
            Dock = DockStyle.Top,
            Height = 10,
            BackColor = Color.Transparent,
            Visible = false
        };
        pnlGrid = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(45, 45, 48),
            AutoScroll = true,
            Padding = new Padding(20, 50, 10, 10),
            AllowDrop = true
        };
        pnlGrid.DragEnter += PnlGrid_DragEnter;
        pnlGrid.DragDrop += PnlGrid_DragDrop;
        base.Controls.Add(pnlGrid);
        base.Controls.Add(lblInfo);
        pnlGrid.SendToBack();
    }

    private void LoadShop(int shopId)
    {
        pnlGrid.Controls.Clear();
        List<ItemInstance> items = _manager.GetShopList(shopId);
        foreach (ItemInstance item in items)
        {
            Panel pnlCard = new Panel
            {
                Size = new Size(64, 84),
                Margin = new Padding(6),
                BackColor = Color.FromArgb(60, 60, 60),
                BorderStyle = BorderStyle.FixedSingle,
                Cursor = Cursors.Hand
            };
            UcItemSlot slot = new UcItemSlot(-1);
            slot.SetItem(item);
            slot.Size = new Size(50, 50);
            slot.Location = new Point(6, 4);
            slot.BorderStyle = BorderStyle.None;
            slot.BackColor = Color.Transparent;
            slot.Enabled = false;
            Label lblPrice = new Label
            {
                Text = ((item.BuyPrice > 0) ? $"{item.BuyPrice} G" : "Bedava"),
                Dock = DockStyle.Bottom,
                Height = 25,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.Gold,
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                BackColor = Color.FromArgb(50, 50, 50)
            };
            Label lblOverlay = new Label
            {
                Text = "AL",
                AutoSize = false,
                Size = new Size(50, 50),
                Location = new Point(6, 4),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.FromArgb(200, 0, 0, 0),
                ForeColor = Color.LimeGreen,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                Visible = false
            };
            pnlCard.Controls.Add(lblOverlay);
            pnlCard.Controls.Add(slot);
            pnlCard.Controls.Add(lblPrice);
            lblOverlay.BringToFront();
            EventHandler enterAction = delegate
            {
                lblOverlay.Visible = true;
                rpg_deneme.UI.Windows.FrmItemTooltip.Instance.ShowTooltip(item, Cursor.Position);
            };
            EventHandler leaveAction = delegate
            {
                Point pt = pnlCard.PointToClient(System.Windows.Forms.Cursor.Position);
                if (!pnlCard.ClientRectangle.Contains(pt))
                {
                    lblOverlay.Visible = false;
                    rpg_deneme.UI.Windows.FrmItemTooltip.Instance.HideTooltip();
                }
            };
            pnlCard.MouseEnter += enterAction;
            pnlCard.MouseLeave += leaveAction;
            lblOverlay.MouseEnter += enterAction;
            lblOverlay.MouseLeave += leaveAction;
            lblPrice.MouseEnter += enterAction;
            lblPrice.MouseLeave += leaveAction;
            EventHandler buyAction = delegate
            {
                BuyItemProcess(item);
            };
            pnlCard.Click += buyAction;
            lblOverlay.Click += buyAction;
            lblPrice.Click += buyAction;
            pnlGrid.Controls.Add(pnlCard);
        }
    }

    public void BuyItemProcess(ItemInstance item)
    {
        CharacterModel player = SessionManager.CurrentCharacter;
        int qty = 1;
        if (item.IsStackable)
        {
            int limit = ((item.MaxStack > 0) ? item.MaxStack : 20);
            int price = ((item.BuyPrice > 0) ? item.BuyPrice : 999999);
            int affordable = (int)(player.Gold / price);
            if (affordable < limit)
            {
                limit = affordable;
            }
            if (limit < 1)
            {
                limit = 1;
            }
            using QuantityDialog qd = new QuantityDialog(limit);
            if (qd.ShowDialog() != DialogResult.OK)
            {
                return;
            }
            qty = qd.SelectedQuantity;
        }
        (bool, string) result = _manager.BuyItem(player, _currentShopId, item, qty);
        MessageBox.Show(result.Item2);
        if (result.Item1)
        {
            this.OnInventoryUpdateNeeded?.Invoke(this, EventArgs.Empty);
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
            this.OnSellRequested?.Invoke(this, item);
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
