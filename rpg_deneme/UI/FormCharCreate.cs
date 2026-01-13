using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using rpg_deneme.Business;
using rpg_deneme.Core;
using rpg_deneme.Models;

namespace rpg_deneme.UI;

public class FormCharCreate : Form
{
    private readonly CharacterManager _manager = new CharacterManager();

    private IContainer components = null;

    private TextBox textBoxCharName;

    private ComboBox comboBoxClass;

    private Button buttonCreate;

    private int _slotIndex;

    public FormCharCreate(int slotIndex = 0)
    {
        InitializeComponent();
        _slotIndex = slotIndex;
    }

    private void FormCharCreate_Load(object sender, EventArgs e)
    {
        comboBoxClass.DataSource = Enum.GetValues(typeof(Enums.CharacterClass));
    }

    private void buttonCreate_Click(object sender, EventArgs e)
    {
        CharacterModel newHero = new CharacterModel();
        newHero.SlotIndex = _slotIndex;
        newHero.Name = textBoxCharName.Text.Trim();
        if (comboBoxClass.SelectedItem is Enums.CharacterClass selectedClass)
        {
            newHero.Class = (byte)selectedClass;
        }
        if (newHero.Class == 1)
        {
            newHero.STR = 10;
            newHero.VIT = 10;
            newHero.DEX = 5;
            newHero.INT = 1;
        }
        else if (newHero.Class == 3)
        {
            newHero.STR = 2;
            newHero.VIT = 5;
            newHero.DEX = 5;
            newHero.INT = 12;
        }
        else
        {
            newHero.STR = 5;
            newHero.VIT = 8;
            newHero.DEX = 12;
            newHero.INT = 3;
        }
        if (_manager.CreateCharacter(newHero).Success)
        {
            MessageBox.Show("Character created! Ready for battle!");
            base.DialogResult = DialogResult.OK;
            Close();
        }
        else
        {
            MessageBox.Show("Character could not be created.");
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
        this.textBoxCharName = new System.Windows.Forms.TextBox();
        this.comboBoxClass = new System.Windows.Forms.ComboBox();
        this.buttonCreate = new System.Windows.Forms.Button();
        base.SuspendLayout();
        this.textBoxCharName.Location = new System.Drawing.Point(44, 64);
        this.textBoxCharName.Name = "textBoxCharName";
        this.textBoxCharName.Size = new System.Drawing.Size(139, 23);
        this.textBoxCharName.TabIndex = 0;
        this.comboBoxClass.FormattingEnabled = true;
        this.comboBoxClass.Location = new System.Drawing.Point(44, 106);
        this.comboBoxClass.Name = "comboBoxClass";
        this.comboBoxClass.Size = new System.Drawing.Size(139, 23);
        this.comboBoxClass.TabIndex = 1;
        this.buttonCreate.Location = new System.Drawing.Point(44, 153);
        this.buttonCreate.Name = "buttonCreate";
        this.buttonCreate.Size = new System.Drawing.Size(75, 23);
        this.buttonCreate.TabIndex = 2;
        this.buttonCreate.Text = "Create";
        this.buttonCreate.UseVisualStyleBackColor = true;
        this.buttonCreate.Click += new System.EventHandler(buttonCreate_Click);
        base.AutoScaleDimensions = new System.Drawing.SizeF(7f, 15f);
        base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        base.ClientSize = new System.Drawing.Size(241, 241);
        base.Controls.Add(this.buttonCreate);
        base.Controls.Add(this.comboBoxClass);
        base.Controls.Add(this.textBoxCharName);
        base.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
        base.MaximizeBox = false;
        base.Name = "FormCharCreate";
        base.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
        this.Text = "Character Creation";
        base.Load += new System.EventHandler(FormCharCreate_Load);
        base.ResumeLayout(false);
        base.PerformLayout();
    }
}
