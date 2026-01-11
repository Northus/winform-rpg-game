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
        this.Size = new Size(64, 64); // Larger size for better visibility
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
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        int margin = 3;
        int size = Width - (margin * 2);
        Rectangle rect = new Rectangle(margin, margin, size, size);

        // Background
        Color bgColor = Color.Gray;

        if (Skill.CurrentLevel >= Skill.MaxLevel) bgColor = Color.Gold;
        else if (Skill.CurrentLevel > 0) bgColor = Color.Orange;
        else if (_canLearn) bgColor = Color.LimeGreen;
        else bgColor = Color.FromArgb(60, 60, 60);

        if (Skill.Type == Core.Enums.SkillType.Active)
        {
            using (SolidBrush b = new SolidBrush(bgColor))
            {
                g.FillRectangle(b, rect);
            }
        }
        else
        {
            using (SolidBrush b = new SolidBrush(bgColor))
            {
                g.FillEllipse(b, rect);
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
                        Rectangle imgRect = new Rectangle(margin + 2, margin + 2, size - 4, size - 4);

                        if (Skill.Type != Core.Enums.SkillType.Active)
                        {
                            using (var gp = new System.Drawing.Drawing2D.GraphicsPath())
                            {
                                gp.AddEllipse(imgRect);
                                g.SetClip(gp);
                                g.DrawImage(img, imgRect);
                                g.ResetClip();
                            }
                        }
                        else
                        {
                            g.DrawImage(img, imgRect);
                        }
                    }
                }
                catch { }
            }
            else
            {
                DrawLetterPlaceholder(g, rect);
            }
        }
        else
        {
            DrawLetterPlaceholder(g, rect);
        }

        // Border
        Color borderColor = Color.Black;
        int borderWidth = 2;
        if (_isSelected)
        {
            borderColor = Color.White;
            borderWidth = 3;
        }

        using (Pen p = new Pen(borderColor, borderWidth))
        {
            if (Skill.Type == Core.Enums.SkillType.Active)
            {
                g.DrawRectangle(p, rect);
            }
            else
            {
                g.DrawEllipse(p, rect);
            }
        }

        // Level Indicator Badge
        string lvl = $"{Skill.CurrentLevel}";
        using (Font f = new Font("Segoe UI", 10, FontStyle.Bold))
        {
            SizeF txtSize = g.MeasureString(lvl, f);

            // Badge position (Bottom Right)
            int badgeSize = (int)Math.Max(txtSize.Width, txtSize.Height) + 6;
            Rectangle badgeRect = new Rectangle(Width - badgeSize - 2, Height - badgeSize - 2, badgeSize, badgeSize);

            // Badge Background
            using (SolidBrush b = new SolidBrush(Color.FromArgb(200, 0, 0, 0)))
            {
                g.FillEllipse(b, badgeRect);
            }
            using (Pen p = new Pen(Color.Gold, 1.5f))
            {
                g.DrawEllipse(p, badgeRect);
            }

            // Text
            float tx = badgeRect.X + (badgeRect.Width - txtSize.Width) / 2;
            float ty = badgeRect.Y + (badgeRect.Height - txtSize.Height) / 2;

            g.DrawString(lvl, f, Brushes.White, tx, ty);
        }
    }

    private void DrawLetterPlaceholder(Graphics g, Rectangle r)
    {
        using (Font f = new Font("Arial", 16, FontStyle.Bold))
        {
            string letter = Skill.Name.Substring(0, Math.Min(2, Skill.Name.Length)).ToUpper();
            SizeF size = g.MeasureString(letter, f);
            g.DrawString(letter, f, Brushes.Black, r.X + (r.Width - size.Width) / 2, r.Y + (r.Height - size.Height) / 2);
        }
    }
}
