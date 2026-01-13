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
        this.Title = "SKILLS";
        this.Size = new Size(650, 520); // Increased height to fit content properly
        _skillManager = new SkillManager();
        _character = SessionManager.CurrentCharacter;

        InitializeControls();
        LoadSkills();
    }

    private void InitializeControls()
    {
        // Skill Points Info - Moved down to avoid potential title bar overlap
        _lblPoints = new Label
        {
            ForeColor = Color.Gold,
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            Location = new Point(20, 50),
            AutoSize = true
        };
        this.Controls.Add(_lblPoints);

        // Learn Button
        _btnLearn = new Button
        {
            Text = "LEARN (+)",
            Location = new Point(480, 45),
            Size = new Size(120, 30),
            BackColor = Color.ForestGreen,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        _btnLearn.Click += BtnLearn_Click;
        this.Controls.Add(_btnLearn);

        // Reset Button
        _btnReset = new Button
        {
            Text = "RESET (5k)",
            Location = new Point(370, 45),
            Size = new Size(100, 30),
            BackColor = Color.IndianRed,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        _btnReset.Click += BtnReset_Click;
        this.Controls.Add(_btnReset);

        // Tree Panel - Expanded to fill vertical space
        _pnlTree = new Panel
        {
            Location = new Point(10, 90),
            Size = new Size(615, 330),
            BackColor = Color.FromArgb(40, 40, 40),
            AutoScroll = true
        };
        _pnlTree.Paint += PnlTree_Paint;
        this.Controls.Add(_pnlTree);

        // Validating Description - Pushed to bottom
        Panel pnlDesc = new Panel
        {
            Location = new Point(10, 430),
            Size = new Size(615, 60),
            BackColor = Color.FromArgb(30, 30, 30),
            BorderStyle = BorderStyle.FixedSingle
        };
        this.Controls.Add(pnlDesc);

        _lblDescription = new Label
        {
            ForeColor = Color.WhiteSmoke,
            Dock = DockStyle.Fill,
            Text = "Select a skill...",
            Font = new Font("Segoe UI", 9),
            TextAlign = ContentAlignment.MiddleCenter,
            Padding = new Padding(5)
        };
        pnlDesc.Controls.Add(_lblDescription);
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
        _lblPoints.Text = $"Skill Points: {_character.SkillPoints}";

        // Preserve Scroll Position
        Point scrollPos = _pnlTree.AutoScrollPosition;

        _pnlTree.Controls.Clear();

        if (_skills == null || !_skills.Any()) return;

        // Calculate layout properties
        int maxRow = _skills.Max(s => s.Row);
        int minCol = _skills.Min(s => s.Col);

        int iconSize = 64; // Increased from 40
        int gapX = 40; // Adjusted gap
        int gapY = 40;

        // Center the tree horizontally based on minCol (usually negative)
        // Assume tree roughly centers around col 0. 
        // We want Col 0 to be at center of panel.
        int panelCenter = _pnlTree.Width / 2;
        int startX = panelCenter - (iconSize / 2); // Center of Col 0

        int topPadding = 20;

        foreach (var skill in _skills)
        {
            UcSkillIcon icon = new UcSkillIcon(skill);

            // Calculate Position
            // Row 0 is bottom
            // MaxRow is top

            int rowFromTop = maxRow - skill.Row;

            // X = Center + (Col * (Size + Gap))
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

            MessageBox.Show($"Skill learned! (Level {_selectedSkill.CurrentLevel})", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        else
        {
            MessageBox.Show(result.Message, "Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
    private void BtnReset_Click(object sender, EventArgs e)
    {
        int resetCost = 5000;
        if (_character.Gold < resetCost)
        {
            MessageBox.Show($"Not enough gold! Required: {resetCost} Gold", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        // Calculate Total Points: 6 (Start) + (Level - 1) * 3
        int totalPoints = 6 + ((_character.Level - 1) * 3);

        if (_character.SkillPoints == totalPoints)
        {
            MessageBox.Show("No distributed points to reset!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var result = MessageBox.Show($"Do you want to reset skills?\nCost: {resetCost} Gold\nAll skill points will be refunded.", "Reset", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (result == DialogResult.Yes)
        {
            // Reset Logic
            _character.Gold -= resetCost;
            _character.SkillPoints = totalPoints;

            // Reset Skills in DB
            _skillManager.ResetSkills(_character);

            // Reset Hotbar Skills in DB
            HotbarRepository hbRepo = new HotbarRepository();
            hbRepo.RemoveSkillSlots(_character.CharacterID);

            // Clear Session Hotbar Slots immediately
            for (int i = 0; i < 5; i++)
            {
                if (SessionManager.HotbarSlots[i] != null && SessionManager.HotbarSlots[i].Type == 1)
                {
                    SessionManager.HotbarSlots[i] = null;
                }
            }

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

            MessageBox.Show("Skills reset!", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
