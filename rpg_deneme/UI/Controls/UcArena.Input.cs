using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using rpg_deneme.Core;
using rpg_deneme.Models;
using rpg_deneme.Business;
using rpg_deneme.UI.Controls.GameEntities;
using rpg_deneme.Data;
using SkillProjectile = rpg_deneme.UI.Controls.GameEntities.SkillProjectile;

namespace rpg_deneme.UI.Controls;

public partial class UcArena
{
    private void Arena_MouseDown(object sender, MouseEventArgs e)
    {
        if (_showResults)
        {
            if (_primaryBtnRect.Contains(e.Location))
            {
                _showResults = false;
                _onPrimary?.Invoke();
            }
            else if (_onSecondary != null && _secondaryBtnRect.Contains(e.Location))
            {
                _showResults = false;
                _onSecondary?.Invoke();
            }
            return;
        }

        if (_hero == null || _player == null || _isBattleEnding)
        {
            return;
        }
        for (int i = 0; i < _hotbar.Count; i++)
        {
            if (GetHotbarSlotRect(i).Contains(e.Location))
            {
                if (e.Button == MouseButtons.Left)
                {
                    _hotbar[i].Type = 0;
                    _hotbar[i].ReferenceID = null;
                    _hotbar[i].Item = null;
                    _hotbar[i].Skill = null;
                    _hotbar[i].CachedImage = null;
                    SaveHotbarToSession();
                }
                else if (e.Button == MouseButtons.Right)
                {
                    UseHotbarSlot(i);
                }
                return;
            }
        }
        if (e.Button == MouseButtons.Left)
        {
            if ((DateTime.Now - _lastAttackTime).TotalMilliseconds < (double)_attackDelayMs)
            {
                return;
            }
            if (!_isTownMode)
            {
                if (_hero.Mana < _manaCostPerHit)
                {
                    return;
                }
                _hero.Mana -= _manaCostPerHit;
            }
            _lastAttackTime = DateTime.Now;
            _player.VisualAttackTimer = 10f;
            this.OnStatsUpdated?.Invoke(this, EventArgs.Empty);

            var equipment = GetEquipment();
            int critChance = StatManager.GetTotalAttributeValue(equipment, Enums.ItemAttributeType.CriticalChance);

            if (_hero.Class == 3)
            {
                int rawDmg = _rnd.Next(_player.MinDamage, _player.MaxDamage + 1);
                bool isCrit = _rnd.Next(100) < critChance;
                if (isCrit) rawDmg = (int)(rawDmg * 2);

                _projectiles.Add(new SkillProjectile(_player.Center.X, _player.Center.Y, e.X, e.Y, rawDmg, enemy: false, isCrit: isCrit));
            }
            else
            {
                _effects.Add(new VisualEffect
                {
                    X = e.X - 10,
                    Y = e.Y - 10,
                    Color = Color.WhiteSmoke,
                    Size = 20,
                    LifeTime = 10
                });
                if (!_isTownMode)
                {
                    BattleEntity target = null;
                    foreach (BattleEntity en in _enemies)
                    {
                        if (en.CurrentHP > 0 && en.Bounds.Contains(e.Location))
                        {
                            target = en;
                            break;
                        }
                    }
                    if (target == null)
                    {
                        foreach (BattleEntity en2 in _enemies)
                        {
                            if (en2.CurrentHP <= 0 || !(Distance(_player.Center.X, _player.Center.Y, en2.Center.X, en2.Center.Y) < 80f))
                            {
                                continue;
                            }
                            target = en2;
                            break;
                        }
                    }
                    if (target != null && Distance(_player.Center.X, _player.Center.Y, target.Center.X, target.Center.Y) <= 80f)
                    {
                        int dmg = _rnd.Next(_player.MinDamage, _player.MaxDamage + 1);
                        bool isCrit = _rnd.Next(100) < critChance;
                        if (isCrit) dmg = (int)(dmg * 2);
                        ApplyDamageToEnemy(target, dmg, isCrit);
                    }
                }
            }
        }
        if (!_isTownMode || e.Button != MouseButtons.Right)
        {
            return;
        }
        foreach (NpcEntity npc in _npcs)
        {
            if (Distance(e.X, e.Y, npc.Center.X, npc.Center.Y) < 60f && Distance(_player.Center.X, _player.Center.Y, npc.Center.X, npc.Center.Y) < 150f)
            {
                this.OnNpcInteraction?.Invoke(this, npc);
                break;
            }
        }
    }

    private void Arena_DragEnter(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(typeof(ItemInstance)))
        {
            e.Effect = DragDropEffects.Move;
        }
        else if (e.Data.GetDataPresent(typeof(SkillModel)))
        {
            e.Effect = DragDropEffects.Copy;
        }
    }

    private void Arena_DragDrop(object sender, DragEventArgs e)
    {
        Point pt = _arena.PointToClient(new Point(e.X, e.Y));
        int targetSlot = -1;
        for (int i = 0; i < 5; i++)
        {
            if (GetHotbarSlotRect(i).Contains(pt))
            {
                targetSlot = i;
                break;
            }
        }

        if (targetSlot == -1) return;

        if (e.Data.GetDataPresent(typeof(ItemInstance)))
        {
            ItemInstance item = (ItemInstance)e.Data.GetData(typeof(ItemInstance));
            _hotbar[targetSlot].Type = 0;
            _hotbar[targetSlot].ReferenceID = item.InstanceID;
            _hotbar[targetSlot].Item = item;
            _hotbar[targetSlot].Skill = null;
            _hotbar[targetSlot].CachedImage = rpg_deneme.UI.ItemDrawer.DrawItem(item);
            SaveHotbarToSession();
        }
        else if (e.Data.GetDataPresent(typeof(SkillModel)))
        {
            SkillModel skill = (SkillModel)e.Data.GetData(typeof(SkillModel));

            if (skill.Type == Enums.SkillType.Passive)
            {
                NotificationManager.AddNotification("Pasif yetenekler hotbara eklenemez.", Color.Yellow);
                return;
            }

            _hotbar[targetSlot].Type = 1;
            _hotbar[targetSlot].ReferenceID = skill.SkillID;
            _hotbar[targetSlot].Item = null;
            _hotbar[targetSlot].Skill = skill;

            Bitmap bmp = new Bitmap(40, 40);
            using (Graphics gw = Graphics.FromImage(bmp))
            {
                gw.Clear(Color.Transparent);
                bool drawn = false;

                if (!string.IsNullOrEmpty(skill.IconPath))
                {
                    string path = System.IO.Path.Combine(Application.StartupPath, "Assets", "Skills", skill.IconPath + ".png");
                    if (System.IO.File.Exists(path))
                    {
                        try
                        {
                            using (Image img = Image.FromFile(path))
                            {
                                gw.DrawImage(img, 0, 0, 40, 40);
                                drawn = true;
                            }
                        }
                        catch { }
                    }
                }

                if (!drawn)
                {
                    gw.Clear(Color.DarkSlateBlue);
                    string l = skill.Name.Substring(0, 1);
                    using (Font f = new Font("Segoe UI", 12, FontStyle.Bold))
                        gw.DrawString(l, f, Brushes.White, 10, 10);
                }
            }
            _hotbar[targetSlot].CachedImage = bmp;
            SaveHotbarToSession();
        }
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (_isTownMode)
        {
            UpdateTownLayout();
        }
    }

    public void HandleKeyDown(Keys key)
    {
        if (key == Keys.W) _w = true;
        if (key == Keys.S) _s = true;
        if (key == Keys.A) _a = true;
        if (key == Keys.D) _d = true;
        HandleHotbarKey(key);
    }

    public void HandleKeyUp(Keys key)
    {
        if (key == Keys.W) _w = false;
        if (key == Keys.S) _s = false;
        if (key == Keys.A) _a = false;
        if (key == Keys.D) _d = false;
    }

    public void HandleHotbarKey(Keys key)
    {
        int idx = -1;
        if (key >= Keys.D1 && key <= Keys.D5)
        {
            idx = (int)(key - 49);
        }
        else if (key >= Keys.NumPad1 && key <= Keys.NumPad5)
        {
            idx = (int)(key - 97);
        }
        if (idx >= 0)
        {
            UseHotbarSlot(idx);
        }
    }

    protected override void OnLeave(EventArgs e)
    {
        base.OnLeave(e);
        _w = false;
        _a = false;
        _s = false;
        _d = false;
        if (_player != null) _player.IsMoving = false;
    }

    private void UpdateTooltips(Point pt)
    {
        for (int i = 0; i < _hotbar.Count; i++)
        {
            Rectangle slotRect = GetHotbarSlotRect(i);
            if (slotRect.Contains(pt))
            {
                if (_hotbar[i].Type == 0 && _hotbar[i].Item != null)
                {
                    if (_lastHoveredHotbarIndex != i)
                    {
                        Point screenPt = _arena.PointToScreen(new Point(slotRect.Right, slotRect.Top));
                        rpg_deneme.UI.Windows.FrmItemTooltip.Instance.ShowTooltip(_hotbar[i].Item, screenPt);
                        _lastHoveredHotbarIndex = i;
                    }
                    _lastHoveredBuffIndex = -1;
                    return;
                }
                else if (_hotbar[i].Type == 1 && _hotbar[i].Skill != null)
                {
                    if (_lastHoveredHotbarIndex != i)
                    {
                        Point screenPt = _arena.PointToScreen(new Point(slotRect.Right, slotRect.Top));
                        string txt = $"{_hotbar[i].Skill.Name}\n{_hotbar[i].Skill.Description}\nMana: {_hotbar[i].Skill.ManaCost}\nDamage: {_hotbar[i].Skill.GetCurrentEffectValue()}";
                        rpg_deneme.UI.Windows.FrmItemTooltip.Instance.ShowSimpleTooltip(txt, screenPt);
                        _lastHoveredHotbarIndex = i;
                    }
                    _lastHoveredBuffIndex = -1;
                    return;
                }
            }
        }
        _lastHoveredHotbarIndex = -1;

        var buffs = NotificationManager.GetBuffs();
        int startX = 20;
        int startY = 20;

        for (int i = 0; i < buffs.Count; i++)
        {
            int boxSize = 32;
            Rectangle rect = new Rectangle(startX, startY + (i * (boxSize + 5)), boxSize, boxSize);
            if (rect.Contains(pt))
            {
                if (_lastHoveredBuffIndex != i)
                {
                    Point screenPt = _arena.PointToScreen(new Point(rect.Right + 5, rect.Top));
                    rpg_deneme.UI.Windows.FrmItemTooltip.Instance.ShowSimpleTooltip(buffs[i].Name, screenPt);
                    _lastHoveredBuffIndex = i;
                }
                return;
            }
        }
        _lastHoveredBuffIndex = -1;

        rpg_deneme.UI.Windows.FrmItemTooltip.Instance.HideTooltip();
    }
}
