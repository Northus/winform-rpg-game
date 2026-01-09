using rpg_deneme.Business;
using rpg_deneme.Core;
using rpg_deneme.Models;
using rpg_deneme.Data;
using rpg_deneme.UI.Controls;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace rpg_deneme.UI.Windows;

public class FormSkills : GameWindow
{
    private CharacterModel _character;
    private SkillManager _skillManager;
    private List<SkillModel> _skills;
    private Panel _pnlTree;
    private Label _lblPoints;
    private Label _lblDescription;
    private Button _btnLearn;
    private Button _btnReset;
    private SkillModel _selectedSkill;

    public FormSkills()
    {
        this.Title = "YETENEKLER";
        this.Size = new Size(600, 450); // Daha kompakt
        _skillManager = new SkillManager();
        _character = SessionManager.CurrentCharacter;

        InitializeControls();
        LoadSkills();
    }

    private void InitializeControls()
    {
        // Skill Points Info
        _lblPoints = new Label
        {
            ForeColor = Color.Gold,
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            Location = new Point(20, 40),
            AutoSize = true
        };
        this.Controls.Add(_lblPoints);

        // Learn Button
        _btnLearn = new Button
        {
            Text = "ÖĞREN (+)",
            Location = new Point(480, 35),
            Size = new Size(120, 30),
            BackColor = Color.ForestGreen,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        _btnLearn.Click += BtnLearn_Click;
        this.Controls.Add(_btnLearn);

        // Reset Button
        _btnReset = new Button
        {
            Text = "SIFIRLA (5k)",
            Location = new Point(370, 35),
            Size = new Size(100, 30),
            BackColor = Color.IndianRed,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        _btnReset.Click += BtnReset_Click;
        this.Controls.Add(_btnReset);

        // Tree Panel
        _pnlTree = new Panel
        {
            Location = new Point(10, 70),
            Size = new Size(580, 280), // Kompakt
            BackColor = Color.FromArgb(40, 40, 40),
            AutoScroll = true
        };
        _pnlTree.Paint += PnlTree_Paint;
        this.Controls.Add(_pnlTree);

        // Validating Description
        _lblDescription = new Label
        {
            ForeColor = Color.WhiteSmoke,
            Location = new Point(10, 360),
            Size = new Size(580, 60),
            Text = "Bir yetenek seçin...",
            Font = new Font("Segoe UI", 9),
            BorderStyle = BorderStyle.FixedSingle
        };
        this.Controls.Add(_lblDescription);
    }

    private void LoadSkills()
    {
        if (_character == null) return;

        // TODO: Map byte Class to Enum
        Enums.CharacterClass cClass = (Enums.CharacterClass)_character.Class;
        _skills = _skillManager.LoadSkillsForClass(cClass, _character.CharacterID);

        UpdateUI();
    }

    private void UpdateUI()
    {
        _lblPoints.Text = $"Yetenek Puanı: {_character.SkillPoints}";

        // Preserve Scroll Position
        Point scrollPos = _pnlTree.AutoScrollPosition;

        _pnlTree.Controls.Clear();

        if (_skills == null || !_skills.Any()) return;

        // Calculate layout properties
        int maxRow = _skills.Max(s => s.Row);
        int minCol = _skills.Min(s => s.Col);

        int iconSize = 40; // Küçültüldü
        int gapX = 50; // Daha sıkı
        int gapY = 50;
        int startX = 100 - (minCol * (iconSize + gapX)); // Negatif col'ları göster
        int topPadding = 30;

        foreach (var skill in _skills)
        {
            UcSkillIcon icon = new UcSkillIcon(skill);

            // Calculate Position
            // Row 0 is bottom (Basic Skills), Row Max is top (Advanced)? 
            // Usually Tree grows Upwards (Root at bottom).
            // So Row 0 (Root) should be at Bottom. 
            // MaxRow (Leaf) should be at Top.
            // Screen Y 0 is Top.
            // So Row Max should be near Y 0.
            // Row 0 should be near Y Max.

            int rowFromTop = maxRow - skill.Row;

            int x = startX + (skill.Col * (iconSize + gapX));
            int y = topPadding + (rowFromTop * (iconSize + gapY));

            icon.Location = new Point(x, y);
            icon.IconClicked += Icon_Clicked;
            icon.StartDragData += Icon_StartDrag;

            var result = _skillManager.CanLearnSkill(_character, skill, _skills);
            bool isSelected = _selectedSkill == skill;
            icon.UpdateStatus(result.Success, isSelected);

            _pnlTree.Controls.Add(icon);
        }

        // Restore Scroll Position
        _pnlTree.AutoScrollPosition = new Point(Math.Abs(scrollPos.X), Math.Abs(scrollPos.Y));
        _pnlTree.Invalidate(); // Redraw lines
    }

    private void Icon_Clicked(object sender, EventArgs e)
    {
        if (sender is UcSkillIcon icon)
        {
            _selectedSkill = icon.Skill;
            _lblDescription.Text = $"{icon.Skill.Name}\n{icon.Skill.Description}\nEffect: {icon.Skill.GetCurrentEffectValue()}";
            UpdateUI();
        }
    }

    private void Icon_StartDrag(object sender, MouseEventArgs e)
    {
        if (sender is UcSkillIcon icon)
        {
            // Drag Drop Logic
            // We need a specific data format for Hotbar to accept
            DoDragDrop(icon.Skill, DragDropEffects.Copy);
        }
    }

    private void PnlTree_Paint(object sender, PaintEventArgs e)
    {
        if (_skills == null) return;

        e.Graphics.TranslateTransform(_pnlTree.AutoScrollPosition.X, _pnlTree.AutoScrollPosition.Y);
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using (Pen p = new Pen(Color.Gray, 2))
        {
            foreach (var skill in _skills)
            {
                // Find control for this skill
                Control c1 = GetIconControl(skill.SkillID);
                if (c1 == null) continue;

                // Draw line to prerequisites
                foreach (var reqId in skill.PrerequisiteSkillIDs)
                {
                    Control c2 = GetIconControl(reqId);
                    if (c2 != null)
                    {
                        // Draw from center to center
                        Point p1 = new Point(c1.Left + c1.Width / 2, c1.Top + c1.Height / 2);
                        Point p2 = new Point(c2.Left + c2.Width / 2, c2.Top + c2.Height / 2);
                        e.Graphics.DrawLine(p, p1, p2);
                    }
                }
            }
        }
    }

    private Control GetIconControl(int skillId)
    {
        foreach (Control c in _pnlTree.Controls)
        {
            if (c is UcSkillIcon icon && icon.Skill.SkillID == skillId)
                return c;
        }
        return null;
    }

    public event EventHandler OnSkillChanged;

    private void BtnLearn_Click(object sender, EventArgs e)
    {
        if (_selectedSkill == null) return;

        var result = _skillManager.CanLearnSkill(_character, _selectedSkill, _skills);
        if (result.Success)
        {
            _skillManager.LearnSkill(_character, _selectedSkill);
            // Deduct Points
            _character.SkillPoints--;

            // Update Character Stats (including Skill Points)
            CharacterRepository charRepo = new CharacterRepository();
            charRepo.UpdateCharacterStats(_character);

            UpdateUI();

            // Notify listeners (e.g. Main Form to update stats/hotbar)
            OnSkillChanged?.Invoke(this, EventArgs.Empty);

            MessageBox.Show($"Yetenek öğrenildi! ({_selectedSkill.CurrentLevel}. Seviye)", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        else
        {
            MessageBox.Show(result.Message, "Öğrenilemedi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
    private void BtnReset_Click(object sender, EventArgs e)
    {
        int resetCost = 5000;
        if (_character.Gold < resetCost)
        {
            MessageBox.Show($"Yeterli altının yok! Gereken: {resetCost} Gold", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var result = MessageBox.Show($"Yetenekleri sıfırlamak istiyor musun?\nBedel: {resetCost} Gold\nBütün yetenek puanların iade edilecek.", "Sıfırla", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (result == DialogResult.Yes)
        {
            // Reset Logic
            _character.Gold -= resetCost;

            // Calculate Total Points: 6 (Start) + (Level - 1) * 3
            int totalPoints = 6 + ((_character.Level - 1) * 3);
            _character.SkillPoints = totalPoints;

            // Reset Skills in DB
            _skillManager.ResetSkills(_character);

            // Reset Hotbar Skills in DB
            HotbarRepository hbRepo = new HotbarRepository();
            hbRepo.RemoveSkillSlots(_character.CharacterID);

            // Reset Local Skills List
            foreach (var s in _skills)
            {
                s.CurrentLevel = 0;
            }
            _selectedSkill = null; // Clear selection

            // Save Character Update
            CharacterRepository charRepo = new CharacterRepository();
            charRepo.UpdateCharacterStats(_character); // Updates Gold & SkillPoints

            UpdateUI();
            OnSkillChanged?.Invoke(this, EventArgs.Empty);

            MessageBox.Show("Yetenekler sıfırlandı!", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
