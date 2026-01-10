using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using rpg_deneme.Business;
using rpg_deneme.Core;
using rpg_deneme.Data;
using rpg_deneme.Models;
using rpg_deneme.UI.Controls;

namespace rpg_deneme.UI.Windows;

public class UcStats : GameWindow
{
    private CharacterRepository _charRepo = new CharacterRepository();

    private InventoryManager _invManager = new InventoryManager();

    private CharacterModel _hero;

    private IContainer components = null;

    private Label label1;

    private Label label2;

    private Label label3;

    private Label label4;

    private Label lblVitVal;

    private Label lblIntVal;

    private Label lblDexVal;

    private Label lblStrVal;

    private Button btnIncStr;

    private Button btnIncDex;

    private Button btnIncInt;

    private Button btnIncVit;

    private Label lblAttack;

    private Label lblMagic;

    private Label lblHP;

    private Label lblDefense;

    private Label lblSpeed;

    private Label lblMP;

    private Label lblPoints;

    public UcStats()
    {
        InitializeComponent();
        base.Title = "KARAKTER DETAYLARI";
        base.Size = new Size(400, 350);
    }

    public void LoadData()
    {
        _hero = SessionManager.CurrentCharacter;
        if (_hero != null)
        {
            UpdateVisuals();
        }
    }

    private void UpdateVisuals()
    {
        lblStrVal.Text = _hero.STR.ToString();
        lblDexVal.Text = _hero.DEX.ToString();
        lblIntVal.Text = _hero.INT.ToString();
        lblVitVal.Text = _hero.VIT.ToString();
        lblPoints.Text = $"Kalan Puan: {_hero.StatPoints}";
        lblPoints.ForeColor = ((_hero.StatPoints > 0) ? Color.Gold : Color.White);
        bool canSpend = _hero.StatPoints > 0;
        btnIncStr.Enabled = canSpend;
        btnIncDex.Enabled = canSpend;
        btnIncInt.Enabled = canSpend;
        btnIncVit.Enabled = canSpend;
        List<ItemInstance> allItems = _invManager.GetInventory(_hero.CharacterID);
        List<ItemInstance> equipment = allItems.Where((ItemInstance x) => x.Location == Enums.ItemLocation.Equipment).ToList();
        ItemInstance weapon = equipment.FirstOrDefault((ItemInstance x) => x.ItemType == Enums.ItemType.Weapon);

        // Load Skills for Passives
        SkillManager skillMgr = new SkillManager();
        var skills = skillMgr.LoadSkillsForClass((Enums.CharacterClass)_hero.Class, _hero.CharacterID);

        (int, int) phyDmg = StatManager.CalculatePhysicalDamage(_hero, weapon, skills);
        (int, int) magDmg = StatManager.CalculateMagicalDamage(_hero, weapon, skills);
        int def = StatManager.CalculateTotalDefense(_hero, equipment, skills);
        int hp = StatManager.CalculateTotalMaxHP(_hero, equipment, skills);
        int mp = StatManager.CalculateTotalMaxMana(_hero, equipment, skills);
        float speed = StatManager.CalculateAttackSpeed(_hero, weapon, equipment, skills);
        lblAttack.Text = $"Fiziksel Hasar: {((phyDmg.Item1 == phyDmg.Item2) ? $"{phyDmg.Item1}" : $"{phyDmg.Item1}-{phyDmg.Item2}")}";
        lblMagic.Text = $"Büyü Hasarı: {((magDmg.Item1 == magDmg.Item2) ? $"{magDmg.Item1}" : $"{magDmg.Item1}-{magDmg.Item2}")}";
        lblDefense.Text = $"Toplam Defans: {def}";
        lblHP.Text = $"Can Kapasitesi: {hp}";
        lblMP.Text = $"Mana Kapasitesi: {mp}";
        lblSpeed.Text = $"Saldırı Hızı: {speed:F2} / sn";

        // Optional: Show Skill Points here too?
        // lblPoints.Text += $" | Yetenek Puanı: {_hero.SkillPoints}";
    }

    private void btnIncrease_Click(object sender, EventArgs e)
    {
        if (_hero == null)
        {
            LoadData();
            if (_hero == null)
            {
                return;
            }
        }
        if (_hero.StatPoints > 0)
        {
            Button btn = (Button)sender;
            if (btn == btnIncStr)
            {
                _hero.STR++;
            }
            else if (btn == btnIncDex)
            {
                _hero.DEX++;
            }
            else if (btn == btnIncInt)
            {
                _hero.INT++;
            }
            else if (btn == btnIncVit)
            {
                _hero.VIT++;
            }
            _hero.StatPoints--;
            _charRepo.UpdateStats(_hero);
            UpdateVisuals();
            if (base.ParentForm is FormMain main)
            {
                main.RefreshStats();
            }
        }
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        LoadData();
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
        this.label1 = new System.Windows.Forms.Label();
        this.label2 = new System.Windows.Forms.Label();
        this.label3 = new System.Windows.Forms.Label();
        this.label4 = new System.Windows.Forms.Label();
        this.lblVitVal = new System.Windows.Forms.Label();
        this.lblIntVal = new System.Windows.Forms.Label();
        this.lblDexVal = new System.Windows.Forms.Label();
        this.lblStrVal = new System.Windows.Forms.Label();
        this.btnIncStr = new System.Windows.Forms.Button();
        this.btnIncDex = new System.Windows.Forms.Button();
        this.btnIncInt = new System.Windows.Forms.Button();
        this.btnIncVit = new System.Windows.Forms.Button();
        this.lblAttack = new System.Windows.Forms.Label();
        this.lblMagic = new System.Windows.Forms.Label();
        this.lblHP = new System.Windows.Forms.Label();
        this.lblDefense = new System.Windows.Forms.Label();
        this.lblSpeed = new System.Windows.Forms.Label();
        this.lblMP = new System.Windows.Forms.Label();
        this.lblPoints = new System.Windows.Forms.Label();
        base.SuspendLayout();
        this.label1.AutoSize = true;
        this.label1.ForeColor = System.Drawing.SystemColors.HighlightText;
        this.label1.Location = new System.Drawing.Point(35, 87);
        this.label1.Name = "label1";
        this.label1.Size = new System.Drawing.Size(27, 15);
        this.label1.TabIndex = 1;
        this.label1.Text = "STR";
        this.label2.AutoSize = true;
        this.label2.ForeColor = System.Drawing.SystemColors.HighlightText;
        this.label2.Location = new System.Drawing.Point(35, 116);
        this.label2.Name = "label2";
        this.label2.Size = new System.Drawing.Size(28, 15);
        this.label2.TabIndex = 2;
        this.label2.Text = "DEX";
        this.label3.AutoSize = true;
        this.label3.ForeColor = System.Drawing.SystemColors.HighlightText;
        this.label3.Location = new System.Drawing.Point(35, 145);
        this.label3.Name = "label3";
        this.label3.Size = new System.Drawing.Size(26, 15);
        this.label3.TabIndex = 3;
        this.label3.Text = "INT";
        this.label4.AutoSize = true;
        this.label4.ForeColor = System.Drawing.SystemColors.HighlightText;
        this.label4.Location = new System.Drawing.Point(35, 174);
        this.label4.Name = "label4";
        this.label4.Size = new System.Drawing.Size(24, 15);
        this.label4.TabIndex = 4;
        this.label4.Text = "VIT";
        this.lblVitVal.AutoSize = true;
        this.lblVitVal.ForeColor = System.Drawing.SystemColors.HighlightText;
        this.lblVitVal.Location = new System.Drawing.Point(77, 174);
        this.lblVitVal.Name = "lblVitVal";
        this.lblVitVal.Size = new System.Drawing.Size(19, 15);
        this.lblVitVal.TabIndex = 8;
        this.lblVitVal.Text = "10";
        this.lblIntVal.AutoSize = true;
        this.lblIntVal.ForeColor = System.Drawing.SystemColors.HighlightText;
        this.lblIntVal.Location = new System.Drawing.Point(77, 145);
        this.lblIntVal.Name = "lblIntVal";
        this.lblIntVal.Size = new System.Drawing.Size(19, 15);
        this.lblIntVal.TabIndex = 7;
        this.lblIntVal.Text = "10";
        this.lblDexVal.AutoSize = true;
        this.lblDexVal.ForeColor = System.Drawing.SystemColors.HighlightText;
        this.lblDexVal.Location = new System.Drawing.Point(77, 116);
        this.lblDexVal.Name = "lblDexVal";
        this.lblDexVal.Size = new System.Drawing.Size(19, 15);
        this.lblDexVal.TabIndex = 6;
        this.lblDexVal.Text = "10";
        this.lblStrVal.AutoSize = true;
        this.lblStrVal.ForeColor = System.Drawing.SystemColors.HighlightText;
        this.lblStrVal.Location = new System.Drawing.Point(77, 87);
        this.lblStrVal.Name = "lblStrVal";
        this.lblStrVal.Size = new System.Drawing.Size(19, 15);
        this.lblStrVal.TabIndex = 5;
        this.lblStrVal.Text = "10";
        this.btnIncStr.BackColor = System.Drawing.Color.WhiteSmoke;
        this.btnIncStr.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        this.btnIncStr.Location = new System.Drawing.Point(113, 83);
        this.btnIncStr.Name = "btnIncStr";
        this.btnIncStr.Size = new System.Drawing.Size(22, 23);
        this.btnIncStr.TabIndex = 9;
        this.btnIncStr.Text = "+";
        this.btnIncStr.UseVisualStyleBackColor = false;
        this.btnIncStr.Click += new System.EventHandler(btnIncrease_Click);
        this.btnIncDex.BackColor = System.Drawing.Color.WhiteSmoke;
        this.btnIncDex.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        this.btnIncDex.Location = new System.Drawing.Point(113, 112);
        this.btnIncDex.Name = "btnIncDex";
        this.btnIncDex.Size = new System.Drawing.Size(22, 23);
        this.btnIncDex.TabIndex = 10;
        this.btnIncDex.Text = "+";
        this.btnIncDex.UseVisualStyleBackColor = false;
        this.btnIncDex.Click += new System.EventHandler(btnIncrease_Click);
        this.btnIncInt.BackColor = System.Drawing.Color.WhiteSmoke;
        this.btnIncInt.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        this.btnIncInt.Location = new System.Drawing.Point(113, 141);
        this.btnIncInt.Name = "btnIncInt";
        this.btnIncInt.Size = new System.Drawing.Size(22, 23);
        this.btnIncInt.TabIndex = 11;
        this.btnIncInt.Text = "+";
        this.btnIncInt.UseVisualStyleBackColor = false;
        this.btnIncInt.Click += new System.EventHandler(btnIncrease_Click);
        this.btnIncVit.BackColor = System.Drawing.Color.WhiteSmoke;
        this.btnIncVit.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        this.btnIncVit.Location = new System.Drawing.Point(113, 170);
        this.btnIncVit.Name = "btnIncVit";
        this.btnIncVit.Size = new System.Drawing.Size(22, 23);
        this.btnIncVit.TabIndex = 12;
        this.btnIncVit.Text = "+";
        this.btnIncVit.UseVisualStyleBackColor = false;
        this.btnIncVit.Click += new System.EventHandler(btnIncrease_Click);
        this.lblAttack.AutoSize = true;
        this.lblAttack.ForeColor = System.Drawing.SystemColors.HighlightText;
        this.lblAttack.Location = new System.Drawing.Point(35, 231);
        this.lblAttack.Name = "lblAttack";
        this.lblAttack.Size = new System.Drawing.Size(38, 15);
        this.lblAttack.TabIndex = 13;
        this.lblAttack.Text = "label5";
        this.lblMagic.AutoSize = true;
        this.lblMagic.ForeColor = System.Drawing.SystemColors.HighlightText;
        this.lblMagic.Location = new System.Drawing.Point(35, 256);
        this.lblMagic.Name = "lblMagic";
        this.lblMagic.Size = new System.Drawing.Size(38, 15);
        this.lblMagic.TabIndex = 14;
        this.lblMagic.Text = "label6";
        this.lblHP.AutoSize = true;
        this.lblHP.ForeColor = System.Drawing.SystemColors.HighlightText;
        this.lblHP.Location = new System.Drawing.Point(35, 306);
        this.lblHP.Name = "lblHP";
        this.lblHP.Size = new System.Drawing.Size(38, 15);
        this.lblHP.TabIndex = 16;
        this.lblHP.Text = "label7";
        this.lblDefense.AutoSize = true;
        this.lblDefense.ForeColor = System.Drawing.SystemColors.HighlightText;
        this.lblDefense.Location = new System.Drawing.Point(35, 281);
        this.lblDefense.Name = "lblDefense";
        this.lblDefense.Size = new System.Drawing.Size(38, 15);
        this.lblDefense.TabIndex = 15;
        this.lblDefense.Text = "label8";
        this.lblSpeed.AutoSize = true;
        this.lblSpeed.ForeColor = System.Drawing.SystemColors.HighlightText;
        this.lblSpeed.Location = new System.Drawing.Point(35, 354);
        this.lblSpeed.Name = "lblSpeed";
        this.lblSpeed.Size = new System.Drawing.Size(38, 15);
        this.lblSpeed.TabIndex = 18;
        this.lblSpeed.Text = "label9";
        this.lblMP.AutoSize = true;
        this.lblMP.ForeColor = System.Drawing.SystemColors.HighlightText;
        this.lblMP.Location = new System.Drawing.Point(35, 329);
        this.lblMP.Name = "lblMP";
        this.lblMP.Size = new System.Drawing.Size(44, 15);
        this.lblMP.TabIndex = 17;
        this.lblMP.Text = "label10";
        this.lblPoints.AutoSize = true;
        this.lblPoints.ForeColor = System.Drawing.SystemColors.HighlightText;
        this.lblPoints.Location = new System.Drawing.Point(97, 49);
        this.lblPoints.Name = "lblPoints";
        this.lblPoints.Size = new System.Drawing.Size(38, 15);
        this.lblPoints.TabIndex = 19;
        this.lblPoints.Text = "label5";
        base.AutoScaleDimensions = new System.Drawing.SizeF(7f, 15f);
        base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        base.Controls.Add(this.lblPoints);
        base.Controls.Add(this.lblSpeed);
        base.Controls.Add(this.lblMP);
        base.Controls.Add(this.lblHP);
        base.Controls.Add(this.lblDefense);
        base.Controls.Add(this.lblMagic);
        base.Controls.Add(this.lblAttack);
        base.Controls.Add(this.btnIncVit);
        base.Controls.Add(this.btnIncInt);
        base.Controls.Add(this.btnIncDex);
        base.Controls.Add(this.btnIncStr);
        base.Controls.Add(this.lblVitVal);
        base.Controls.Add(this.lblIntVal);
        base.Controls.Add(this.lblDexVal);
        base.Controls.Add(this.lblStrVal);
        base.Controls.Add(this.label4);
        base.Controls.Add(this.label3);
        base.Controls.Add(this.label2);
        base.Controls.Add(this.label1);
        base.Name = "UcStats";
        base.Controls.SetChildIndex(this.label1, 0);
        base.Controls.SetChildIndex(this.label2, 0);
        base.Controls.SetChildIndex(this.label3, 0);
        base.Controls.SetChildIndex(this.label4, 0);
        base.Controls.SetChildIndex(this.lblStrVal, 0);
        base.Controls.SetChildIndex(this.lblDexVal, 0);
        base.Controls.SetChildIndex(this.lblIntVal, 0);
        base.Controls.SetChildIndex(this.lblVitVal, 0);
        base.Controls.SetChildIndex(this.btnIncStr, 0);
        base.Controls.SetChildIndex(this.btnIncDex, 0);
        base.Controls.SetChildIndex(this.btnIncInt, 0);
        base.Controls.SetChildIndex(this.btnIncVit, 0);
        base.Controls.SetChildIndex(this.lblAttack, 0);
        base.Controls.SetChildIndex(this.lblMagic, 0);
        base.Controls.SetChildIndex(this.lblDefense, 0);
        base.Controls.SetChildIndex(this.lblHP, 0);
        base.Controls.SetChildIndex(this.lblMP, 0);
        base.Controls.SetChildIndex(this.lblSpeed, 0);
        base.Controls.SetChildIndex(this.lblPoints, 0);
        base.ResumeLayout(false);
        base.PerformLayout();
    }
}
