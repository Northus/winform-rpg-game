using rpg_deneme.Models;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace rpg_deneme.UI.Controls;

public class UcSkillIcon : UserControl
{
    public SkillModel Skill { get; private set; }
    private bool _isSelected;
    private bool _canLearn;

    public event EventHandler IconClicked;
    public event MouseEventHandler StartDragData;

    public UcSkillIcon(SkillModel skill)
    {
        Skill = skill;
        this.DoubleBuffered = true;
        this.Size = new Size(40, 40); // Küçültüldü
        this.Cursor = Cursors.Hand;

        // Tooltip logic
        ToolTip tt = new ToolTip();
        tt.SetToolTip(this, GetTooltipText());
    }

    private string GetTooltipText()
    {
        string txt = $"{Skill.Name} (Lv. {Skill.CurrentLevel}/{Skill.MaxLevel})\n";
        txt += $"{Skill.Description}\n";
        if (Skill.Type == Core.Enums.SkillType.Active)
            txt += $"Mana: {Skill.ManaCost} | CD: {Skill.Cooldown}s\n";
        else
            txt += "PASİF\n";

        txt += $"Etki: {Skill.GetCurrentEffectValue():F0} (Sonraki: {Skill.GetCurrentEffectValue() + Skill.EffectScaling:F0})";
        return txt;
    }

    public void UpdateStatus(bool canLearn, bool isSelected)
    {
        _canLearn = canLearn;
        _isSelected = isSelected;
        this.Invalidate();
    }

    private Point _mouseDownLocation;
    private bool _isDragging = false;

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button == MouseButtons.Left)
        {
            _mouseDownLocation = e.Location;
            _isDragging = false;
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (e.Button == MouseButtons.Left && !_isDragging)
        {
            if (Math.Abs(e.X - _mouseDownLocation.X) > 5 || Math.Abs(e.Y - _mouseDownLocation.Y) > 5)
            {
                if (Skill.CurrentLevel > 0 && Skill.Type == Core.Enums.SkillType.Active)
                {
                    _isDragging = true;
                    StartDragData?.Invoke(this, e);
                }
            }
        }
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        _isDragging = false;
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        IconClicked?.Invoke(this, EventArgs.Empty);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        Graphics g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        // Background
        Color bgColor = Color.Gray;

        if (Skill.CurrentLevel >= Skill.MaxLevel) bgColor = Color.Gold;
        else if (Skill.CurrentLevel > 0) bgColor = Color.Orange;
        else if (_canLearn) bgColor = Color.LimeGreen;
        else bgColor = Color.FromArgb(70, 70, 70);

        if (Skill.Type == Core.Enums.SkillType.Active)
        {
            using (SolidBrush b = new SolidBrush(bgColor))
            {
                g.FillRectangle(b, 2, 2, 36, 36);
            }
        }
        else
        {
            using (SolidBrush b = new SolidBrush(bgColor))
            {
                g.FillEllipse(b, 2, 2, 36, 36);
            }
        }

        // Draw Icon Image
        if (!string.IsNullOrEmpty(Skill.IconPath))
        {
            string path = System.IO.Path.Combine(Application.StartupPath, "Assets", "Skills", Skill.IconPath + ".png");
            if (System.IO.File.Exists(path))
            {
                try
                {
                    using (Image img = Image.FromFile(path))
                    {
                        g.DrawImage(img, 4, 4, 32, 32);
                    }
                }
                catch { }
            }
            else
            {
                DrawLetterPlaceholder(g);
            }
        }
        else
        {
            DrawLetterPlaceholder(g);
        }

        // Border
        Color borderColor = Color.Black;
        if (_isSelected) borderColor = Color.White;

        using (Pen p = new Pen(borderColor, 2))
        {
            if (Skill.Type == Core.Enums.SkillType.Active)
            {
                g.DrawRectangle(p, 2, 2, 36, 36);
            }
            else
            {
                g.DrawEllipse(p, 2, 2, 36, 36);
            }
        }

        // Level Indicator
        string lvl = $"{Skill.CurrentLevel}";
        using (Font fArgs = new Font("Arial", 7, FontStyle.Bold))
        {
            g.DrawString(lvl, fArgs, Brushes.Black, 27, 27);
            g.DrawString(lvl, fArgs, Brushes.White, 26, 26);
        }
    }

    private void DrawLetterPlaceholder(Graphics g)
    {
        using (Font f = new Font("Arial", 12, FontStyle.Bold))
        {
            string letter = Skill.Name.Substring(0, Math.Min(2, Skill.Name.Length));
            SizeF size = g.MeasureString(letter, f);
            g.DrawString(letter, f, Brushes.Black, (Width - size.Width) / 2, (Height - size.Height) / 2);
        }
    }
}
