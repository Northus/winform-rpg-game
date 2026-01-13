using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using rpg_deneme.Business;
using rpg_deneme.Core;
using rpg_deneme.Models;

namespace rpg_deneme.UI;

/// <summary>
/// Karakter seçme ekranı.
/// </summary>
public class FormCharSelect : Form
{
    private readonly CharacterManager _manager = new CharacterManager();
    private List<Panel> _slots;
    private IContainer components = null;

    private Panel panel1;
    private Panel panel2;
    private Panel panel3;
    private Panel panel4;
    private Panel panel5;
    private Panel panel6;
    private Panel panel7;
    private Panel panel8;

    /// <summary>
    /// FormCharSelect yapıcı metodu.
    /// </summary>
    public FormCharSelect()
    {
        InitializeComponent();
        _slots = new List<Panel> { panel1, panel2, panel3, panel4, panel5, panel6, panel7, panel8 };
    }

    /// <summary>
    /// Form yüklendiğinde mevcut karakterleri slotlara yükler.
    /// </summary>
    private void FormCharSelect_Load(object sender, EventArgs e)
    {
        LoadCharactersToSlots();
    }

    /// <summary>
    /// Karakterleri veritabanından çeker ve slotları doldurur.
    /// </summary>
    public void LoadCharactersToSlots()
    {
        List<CharacterModel> charList = _manager.GetCharacters();
        // Legacy Data Fix: If multiple characters are at slot 0, distribute them.
        var charsAtZero = charList.FindAll(c => c.SlotIndex == 0);
        if (charsAtZero.Count > 1)
        {
            for (int k = 0; k < charsAtZero.Count && k < 8; k++)
            {
                charsAtZero[k].SlotIndex = k;
                _manager.UpdateSlotIndex(charsAtZero[k].CharacterID, k);
            }
            // Refresh list
            charList = _manager.GetCharacters();
        }

        for (int i = 0; i < 8; i++)
        {
            _slots[i].Controls.Clear();
            CharacterModel charInSlot = charList.Find(c => c.SlotIndex == i);

            if (charInSlot != null)
            {
                DisplayFullSlot(_slots[i], charInSlot);
            }
            else
            {
                DisplayEmptySlot(_slots[i], i);
            }
        }
    }

    /// <summary>
    /// İçinde karakter olan bir slotu görüntüler.
    /// </summary>
    private void DisplayFullSlot(Panel pnl, CharacterModel model)
    {
        pnl.BackColor = Color.DarkSlateGray;
        string className = ((Enums.CharacterClass)model.Class).ToString();
        Label lbl = new Label
        {
            Text = $"{model.Name}\n{className}\nLv.{model.Level}",
            ForeColor = Color.White,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Cursor = Cursors.Hand
        };
        lbl.Click += delegate
        {
            SessionManager.CurrentCharacter = model;
            MessageBox.Show(model.Name + " selected! Entering game...");
            GoToMainGame();
        };
        pnl.Controls.Add(lbl);
    }

    /// <summary>
    /// Boş bir slotu görüntüler ve karakter oluşturma butonu ekler.
    /// </summary>
    private void DisplayEmptySlot(Panel pnl, int slotIndex)
    {
        pnl.BackColor = Color.FromArgb(60, 60, 60);
        Button btn = new Button
        {
            Text = "NEW CHARACTER",
            Dock = DockStyle.Fill,
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.LightGray
        };
        btn.Click += delegate
        {
            using FormCharCreate formCharCreate = new FormCharCreate(slotIndex);
            if (formCharCreate.ShowDialog() == DialogResult.OK)
            {
                LoadCharactersToSlots();
            }
        };
        pnl.Controls.Add(btn);
    }

    /// <summary>
    /// Ana oyun formuna geçiş yapar.
    /// </summary>
    private void GoToMainGame()
    {
        FormMain game = new FormMain();
        game.Show();
        Hide();
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
        this.panel1 = new Panel();
        this.panel2 = new Panel();
        this.panel3 = new Panel();
        this.panel4 = new Panel();
        this.panel5 = new Panel();
        this.panel6 = new Panel();
        this.panel7 = new Panel();
        this.panel8 = new Panel();
        base.SuspendLayout();
        this.panel1.BackColor = Color.DimGray;
        this.panel1.Location = new Point(25, 59);
        this.panel1.Name = "panel1";
        this.panel1.Size = new Size(125, 125);
        this.panel1.TabIndex = 0;
        this.panel2.BackColor = Color.DimGray;
        this.panel2.Location = new Point(175, 59);
        this.panel2.Name = "panel2";
        this.panel2.Size = new Size(125, 125);
        this.panel2.TabIndex = 1;
        this.panel3.BackColor = Color.DimGray;
        this.panel3.Location = new Point(473, 59);
        this.panel3.Name = "panel3";
        this.panel3.Size = new Size(125, 125);
        this.panel3.TabIndex = 3;
        this.panel4.BackColor = Color.DimGray;
        this.panel4.Location = new Point(323, 59);
        this.panel4.Name = "panel4";
        this.panel4.Size = new Size(125, 125);
        this.panel4.TabIndex = 2;
        this.panel5.BackColor = Color.DimGray;
        this.panel5.Location = new Point(473, 204);
        this.panel5.Name = "panel5";
        this.panel5.Size = new Size(125, 125);
        this.panel5.TabIndex = 7;
        this.panel6.BackColor = Color.DimGray;
        this.panel6.Location = new Point(323, 204);
        this.panel6.Name = "panel6";
        this.panel6.Size = new Size(125, 125);
        this.panel6.TabIndex = 6;
        this.panel7.BackColor = Color.DimGray;
        this.panel7.Location = new Point(175, 204);
        this.panel7.Name = "panel7";
        this.panel7.Size = new Size(125, 125);
        this.panel7.TabIndex = 5;
        this.panel8.BackColor = Color.DimGray;
        this.panel8.Location = new Point(25, 204);
        this.panel8.Name = "panel8";
        this.panel8.Size = new Size(125, 125);
        this.panel8.TabIndex = 4;
        base.AutoScaleDimensions = new SizeF(7f, 15f);
        base.AutoScaleMode = AutoScaleMode.Font;
        base.ClientSize = new Size(635, 398);
        base.Controls.Add(this.panel5);
        base.Controls.Add(this.panel6);
        base.Controls.Add(this.panel7);
        base.Controls.Add(this.panel8);
        base.Controls.Add(this.panel3);
        base.Controls.Add(this.panel4);
        base.Controls.Add(this.panel2);
        base.Controls.Add(this.panel1);
        base.FormBorderStyle = FormBorderStyle.FixedSingle;
        base.MaximizeBox = false;
        base.Name = "FormCharSelect";
        base.StartPosition = FormStartPosition.CenterScreen;
        this.Text = "Character Selection";
        base.Load += new EventHandler(FormCharSelect_Load);
        base.ResumeLayout(false);
    }
}
