using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using rpg_deneme.Business;
using rpg_deneme.Core;
using rpg_deneme.Models;
using rpg_deneme.Data;
using rpg_deneme.UI.Controls.GameEntities;
using System.Runtime.InteropServices;
using SkillProjectile = rpg_deneme.UI.Controls.GameEntities.SkillProjectile;

namespace rpg_deneme.UI.Controls;

public partial class UcArena
{
    private bool _statsDirty = false;
    private int _screenShakeTimer = 0;

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    private void UpdateInputState()
    {
        var form = this.FindForm();
        if (form == null) return;

        IntPtr fg = GetForegroundWindow();
        bool isGameFocused = (fg == form.Handle);

        if (!isGameFocused && rpg_deneme.UI.Windows.FrmItemTooltip.Instance.Visible && fg == rpg_deneme.UI.Windows.FrmItemTooltip.Instance.Handle)
        {
            isGameFocused = true;
        }

        if (isGameFocused)
        {
            _w = (GetAsyncKeyState(0x57) & 0x8000) != 0; // W
            _a = (GetAsyncKeyState(0x41) & 0x8000) != 0; // A
            _s = (GetAsyncKeyState(0x53) & 0x8000) != 0; // S
            _d = (GetAsyncKeyState(0x44) & 0x8000) != 0; // D
        }
        else
        {
            _w = false; _a = false; _s = false; _d = false;
            if (_player != null) _player.IsMoving = false;
        }
    }

    public void StartBattle(CharacterModel hero, BattleEntity enemyTemplate)
    {
        PrepareBattleCommon(hero);
        (int, int) arenaSize = GetArenaSize();
        int w = arenaSize.Item1;
        int h = arenaSize.Item2;
        if (w <= 0) w = 1000;
        if (h <= 0) h = 700;

        BattleEntity singleEnemy = new BattleEntity
        {
            Width = enemyTemplate.Width,
            Height = enemyTemplate.Height,
            Speed = enemyTemplate.Speed * 0.65f,
            MaxHP = enemyTemplate.MaxHP,
            CurrentHP = enemyTemplate.MaxHP,
            MinDamage = enemyTemplate.MinDamage,
            MaxDamage = enemyTemplate.MaxDamage,
            Name = enemyTemplate.Name,
            IsRanged = enemyTemplate.IsRanged,
            AttackRange = enemyTemplate.AttackRange
        };

        Random rnd = new Random();
        int edge = rnd.Next(4); // 0: Top, 1: Bottom, 2: Left, 3: Right
        int margin = 60;

        switch (edge)
        {
            case 0: // Top
                singleEnemy.X = rnd.Next(margin, w - margin);
                singleEnemy.Y = margin;
                break;
            case 1: // Bottom
                singleEnemy.X = rnd.Next(margin, w - margin);
                singleEnemy.Y = h - margin - singleEnemy.Height;
                break;
            case 2: // Left
                singleEnemy.X = margin;
                singleEnemy.Y = rnd.Next(margin, h - margin);
                break;
            case 3: // Right
                singleEnemy.X = w - margin - singleEnemy.Width;
                singleEnemy.Y = rnd.Next(margin, h - margin);
                break;
        }

        if (_player != null)
        {
            // Ensure not spawning on top of player (though edge vs center is usually safe)
            float dist = (float)Math.Sqrt(Math.Pow(singleEnemy.X - _player.X, 2) + Math.Pow(singleEnemy.Y - _player.Y, 2));
            if (dist < 200)
            {
                // Fallback to far corner
                singleEnemy.X = w - 100;
                singleEnemy.Y = 100;
            }
        }
        _enemies.Add(singleEnemy);
        StartGameLoop();
    }

    /// <summary>
    /// Hayatta kalma modu (Survival/Wave) savaşını başlatır.
    /// Düşmanlar oyuncudan uzakta rastgele konumlarda doğar.
    /// </summary>
    public void StartSurvivalBattle(CharacterModel hero, List<EnemyModel> enemyTemplates)
    {
        PrepareBattleCommon(hero);
        (int, int) arenaSize = GetArenaSize();
        int w = arenaSize.Item1;
        int h = arenaSize.Item2;
        if (w <= 0) w = 1000;
        if (h <= 0) h = 700;
        Random rnd = new Random();
        foreach (EnemyModel tmpl in enemyTemplates)
        {
            bool isRangedUnit = tmpl.IsRanged || tmpl.Type == Enums.EnemyType.Ranged;
            int attackRange = ((tmpl.AttackRange > 0) ? tmpl.AttackRange : (isRangedUnit ? 250 : 50));
            BattleEntity newEnemy = new BattleEntity
            {
                Width = tmpl.Width,
                Height = tmpl.Height,
                Speed = tmpl.Speed * 0.65f,
                MaxHP = tmpl.MaxHP,
                CurrentHP = tmpl.MaxHP,
                MinDamage = tmpl.MinDamage,
                MaxDamage = tmpl.MaxDamage,
                Name = tmpl.Name,
                IsRanged = isRangedUnit,
                AttackRange = attackRange
            };

            int edge = rnd.Next(4);
            int margin = 50;

            switch (edge)
            {
                case 0: // Top
                    newEnemy.X = rnd.Next(margin, w - margin);
                    newEnemy.Y = margin;
                    break;
                case 1: // Bottom
                    newEnemy.X = rnd.Next(margin, w - margin);
                    newEnemy.Y = h - margin - newEnemy.Height;
                    break;
                case 2: // Left
                    newEnemy.X = margin;
                    newEnemy.Y = rnd.Next(margin, h - margin);
                    break;
                case 3: // Right
                    newEnemy.X = w - margin - newEnemy.Width;
                    newEnemy.Y = rnd.Next(margin, h - margin);
                    break;
            }

            if (_player != null)
            {
                // Safety check to push away if somehow too close
                float dist = (float)Math.Sqrt(Math.Pow(newEnemy.X - _player.X, 2) + Math.Pow(newEnemy.Y - _player.Y, 2));
                if (dist < 150)
                {
                    newEnemy.X += 200; // Just push it somewhere
                    if (newEnemy.X > w - 50) newEnemy.X = 50;
                }
            }
            _enemies.Add(newEnemy);
        }
        StartGameLoop();
    }

    /// <summary>
    /// Şehir modunu (güvenli bölge) başlatır.
    /// Düşmanlar temizlenir, NPC'ler yüklenir.
    /// </summary>
    public void StartTown(CharacterModel hero)
    {
        _isBattleEnding = false;
        _hero = hero;
        LoadHotbarFromSession();
        _isTownMode = true;

        // Fix: Clear ALL lists to prevent lingering lag/stutters
        _projectiles.Clear();
        _enemies.Clear();
        _effects.Clear();

        _w = (_a = (_s = (_d = false)));
        InitTownEntities();
        UpdateTownLayout();
        (int, int) arenaSize = GetArenaSize();
        int w = arenaSize.Item1;
        int h = arenaSize.Item2;

        List<ItemInstance> inventory = _invManager.GetInventory(_hero.CharacterID);
        List<ItemInstance> equippedItems = inventory.Where(x => x.Location == Enums.ItemLocation.Equipment).ToList();
        int maxHp = StatManager.CalculateTotalMaxHP(_hero, equippedItems);

        int currentHP = _hero.HP;
        if (currentHP > maxHp) currentHP = maxHp;
        if (currentHP < 0) currentHP = 0;

        _player = new BattleEntity
        {
            X = w / 2 - 16,
            Y = h / 2 - 16,
            Width = 32,
            Height = 32,
            Speed = 3.5f,
            MaxHP = maxHp,
            CurrentHP = currentHP,
            MinDamage = 0,
            MaxDamage = 0,
            Defense = 0
        };

        _hero.HP = currentHP;
        StartGameLoop();
    }

    private void PrepareBattleCommon(CharacterModel hero)
    {
        _hero = hero;
        LoadHotbarFromSession();
        _projectiles.Clear();
        _enemies.Clear();
        _w = (_a = (_s = (_d = false)));
        _isBattleEnding = false;
        _isTownMode = false;

        List<ItemInstance> inventory = _invManager.GetInventory(_hero.CharacterID);
        List<ItemInstance> equippedItems = inventory.Where(x => x.Location == Enums.ItemLocation.Equipment).ToList();
        int maxHp = StatManager.CalculateTotalMaxHP(_hero, equippedItems);

        int currentHP = _hero.HP;

        if (currentHP > maxHp)
        {
            currentHP = maxHp;
        }
        if (currentHP < 0)
        {
            currentHP = 0;
        }

        (int, int) arenaSize = GetArenaSize();
        int arenaW = arenaSize.Item1;
        int arenaH = arenaSize.Item2;
        int startX = Math.Max(0, arenaW / 2 - 16);
        int startY = Math.Max(0, arenaH / 2 - 16);
        _player = new BattleEntity
        {
            X = startX,
            Y = startY,
            Width = 32,
            Height = 32,
            Speed = 3.5f,
            MaxHP = maxHp,
            CurrentHP = currentHP,
            MinDamage = 0,
            MaxDamage = 0,
            Defense = 0
        };

        _hero.HP = currentHP;
        RecalculatePlayerStats();

        if (_player.CurrentHP > _player.MaxHP)
        {
            _player.CurrentHP = _player.MaxHP;
            _hero.HP = _player.MaxHP;
        }
    }

    // Stats refresh (equipment changes during battle)
    private long _lastStatsRefreshTicks;
    private static readonly long StatsRefreshIntervalTicks = Stopwatch.Frequency * 2; // 2 seconds

    // Cached skills - avoid DB query every frame
    private List<SkillModel> _cachedLearnedSkills;
    private long _lastSkillFetchTicks;

    public void RefreshGameState(CharacterModel hero)
    {
        _hero = hero;
        LoadHotbarFromSession();
        RecalculatePlayerStats();
    }

    /// <summary>
    /// Oyuncunun mevcut ekipmanlarına, yeteneklerine ve seviyesine göre
    /// hasar, defans, can, mana vb. tüm istatistiklerini yeniden hesaplar.
    /// </summary>
    public void RecalculatePlayerStats()
    {
        if (_hero == null || _player == null) return;

        // Use cached skills - only refresh every 5 seconds
        long nowTicks = _cacheWatch.ElapsedTicks;
        if (_cachedLearnedSkills == null || (nowTicks - _lastSkillFetchTicks) > Stopwatch.Frequency * 5)
        {
            SkillManager skillMgr = new SkillManager();
            _cachedLearnedSkills = skillMgr.LoadSkillsForClass((Enums.CharacterClass)_hero.Class, _hero.CharacterID);
            _lastSkillFetchTicks = nowTicks;
        }
        _learnedSkills = _cachedLearnedSkills;

        // Update hotbar skill references (without DB query)
        foreach (var slot in _hotbar)
        {
            if (slot.Type == 1 && slot.Skill != null)
            {
                var freshSkill = _learnedSkills.FirstOrDefault(s => s.SkillID == slot.Skill.SkillID);
                if (freshSkill != null)
                {
                    freshSkill.LastCastTime = slot.Skill.LastCastTime;
                    slot.Skill = freshSkill;
                }
            }
        }

        // Use cached equipment
        var equipment = GetEquipment();
        ItemInstance equippedWeapon = null;
        for (int i = 0; i < equipment.Count; i++)
        {
            if (equipment[i].ItemType == Enums.ItemType.Weapon && equipment[i].Location == Enums.ItemLocation.Equipment)
            {
                equippedWeapon = equipment[i];
                break;
            }
        }

        _currentWeaponUpgradeLevel = equippedWeapon?.UpgradeLevel ?? 0;
        _currentWeaponGrade = equippedWeapon?.Grade ?? Enums.ItemGrade.Common;

        (int min, int max) calculatedDamage = (_hero.Class == 3)
          ? StatManager.CalculateMagicalDamage(_hero, equippedWeapon, _learnedSkills)
             : StatManager.CalculatePhysicalDamage(_hero, equippedWeapon, _learnedSkills);

        int calculatedDefense = StatManager.CalculateTotalDefense(_hero, equipment, _learnedSkills);

        _attackDelayMs = StatManager.CalculateAttackDelay(_hero, equipment, _learnedSkills);
        _manaCostPerHit = StatManager.CalculateAttackManaCost(_hero);
        _player.MinDamage = calculatedDamage.Item1;
        _player.MaxDamage = calculatedDamage.Item2;
        _player.Defense = calculatedDefense;

        int moveSpeedBonus = StatManager.CalculateMovementSpeedBonus(_learnedSkills);
        _player.Speed = 3.5f + moveSpeedBonus * 0.1f;

        _manaRegenAmount = StatManager.GetTotalAttributeValue(equipment, Enums.ItemAttributeType.ManaRegen);

        var passiveBonus = StatManager.CalculatePassiveBonuses(_learnedSkills);
        _manaRegenAmount += (int)passiveBonus.ManaRegenBonus;
        _hpRegenAmount = (float)passiveBonus.HPRegenBonus;
        _lifeStealPercent = passiveBonus.LifeStealPercent;

        NotificationManager.ClearBuffs();
        if (_manaRegenAmount > 0) NotificationManager.AddBuff($"Mana Regen: {_manaRegenAmount}/3sn", "ManaRegen", _manaRegenAmount);
        if (_hpRegenAmount > 0) NotificationManager.AddBuff($"HP Regen: {_hpRegenAmount:F1}/3sn", "HPRegen", (int)_hpRegenAmount);

        int newMaxHP = StatManager.CalculateTotalMaxHP(_hero, equipment, _learnedSkills);
        int oldMaxHP = _player.MaxHP;
        _player.MaxHP = newMaxHP;
        if (oldMaxHP > 0 && newMaxHP != oldMaxHP && _player.CurrentHP > newMaxHP)
        {
            _player.CurrentHP = newMaxHP;
            _hero.HP = newMaxHP;
        }
        if (_player.CurrentHP > _player.MaxHP)
        {
            _player.CurrentHP = _player.MaxHP;
            _hero.HP = _player.MaxHP;
        }

        int newMaxMana = StatManager.CalculateTotalMaxMana(_hero, equipment, _learnedSkills);
        _player.MaxMana = newMaxMana;
        if (_hero.Mana > newMaxMana) _hero.Mana = newMaxMana;
        _player.CurrentMana = _hero.Mana;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct NativeMessage
    {
        public IntPtr Handle;
        public uint Message;
        public IntPtr WParameter;
        public IntPtr LParameter;
        public uint Time;
        public Point Location;
    }

    [DllImport("user32.dll")]
    private static extern bool PeekMessage(out NativeMessage lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax, uint wRemoveMsg);

    private bool IsApplicationIdle()
    {
        NativeMessage result;
        return !PeekMessage(out result, IntPtr.Zero, 0, 0, 0);
    }

    /// <summary>
    /// Oyun döngüsünü (Game Loop) başlatır.
    /// Application.Idle olayını kullanarak mümkün olan en yüksek FPS'i hedefler.
    /// </summary>
    private void StartGameLoop()
    {
        if (_loopRunning) return;
        _loopRunning = true;

        if (!_isIdleBound)
        {
            Application.Idle += OnIdle;
            _isIdleBound = true;
        }

        _gameTimer = Stopwatch.StartNew();
        _lastTick = _gameTimer.ElapsedTicks;
        _accumulator = 0;

        Focus();
    }

    private void StopGameLoop()
    {
        if (!_loopRunning) return;
        _loopRunning = false;

        if (_isIdleBound)
        {
            Application.Idle -= OnIdle;
            _isIdleBound = false;
        }
    }

    private void OnIdle(object? sender, EventArgs e)
    {
        while (_loopRunning && IsApplicationIdle())
        {
            if (_gameTimer == null) return;

            long now = _gameTimer.ElapsedTicks;
            long deltaTicks = now - _lastTick;
            _lastTick = now;

            double dt = (double)deltaTicks / Stopwatch.Frequency;
            if (dt > 0.25) dt = 0.25;

            _accumulator += dt;

            const double TargetDt = 1.0 / 60.0;

            while (_accumulator >= TargetDt)
            {
                StepSimulation();
                _accumulator -= TargetDt;
            }

            _skgl?.Invalidate();
        }
    }



    /// <summary>
    /// Oyunun fizik ve mantık simülasyonunun bir adımı (Tick).
    /// Sabit 60Hz hızında çalışacak şekilde ayarlanmıştır.
    /// </summary>
    private void StepSimulation()
    {
        UpdateInputState();
        Fps_OnGameTick();

        if (_player == null) return;

        if (_isBattleEnding)
        {
            UpdateEffects();
            return;
        }

        // Keep hero<->player resources in sync
        if (_hero != null)
        {
            _animCounter++;

            // Periodically refresh stats - every 120 ticks (~2 seconds)
            if (_animCounter % 120 == 0)
            {
                InvalidateEquipmentCache();
                RecalculatePlayerStats();

                if (_hero.HP > _player.MaxHP) _hero.HP = _player.MaxHP;
                if (_hero.Mana > _player.MaxMana) _hero.Mana = _player.MaxMana;
            }

            // Deferred DB save - every 5 seconds if needed


            if (_hero.HP != _player.CurrentHP)
            {
                _player.CurrentHP = _hero.HP;
            }

            if (_player.CurrentHP > _player.MaxHP)
            {
                _player.CurrentHP = _player.MaxHP;
                _hero.HP = _player.MaxHP;
            }

            if (_hero.Mana != _player.CurrentMana)
            {
                _player.CurrentMana = _hero.Mana;
            }

            if (_player.CurrentMana > _player.MaxMana)
            {
                _player.CurrentMana = _player.MaxMana;
                _hero.Mana = _player.CurrentMana;
            }

            // Mana regen - every 3 seconds (180 ticks)
            _manaRegenTimer++;
            if (_manaRegenTimer >= 180)
            {
                _manaRegenTimer = 0;
                if (_manaRegenAmount > 0 && _player.CurrentMana < _player.MaxMana)
                {
                    _player.CurrentMana += _manaRegenAmount;
                    if (_player.CurrentMana > _player.MaxMana) _player.CurrentMana = _player.MaxMana;
                    _hero.Mana = _player.CurrentMana;
                    _statsDirty = true;
                }
            }

            // HP Regen wait same interval
            _hpRegenTimer++;
            if (_hpRegenTimer >= 180)
            {
                _hpRegenTimer = 0;
                if (_hpRegenAmount > 0 && _player.CurrentHP < _player.MaxHP)
                {
                    _player.CurrentHP += (int)_hpRegenAmount;
                    if (_player.CurrentHP > _player.MaxHP) _player.CurrentHP = _player.MaxHP;
                    _hero.HP = _player.CurrentHP;
                    _statsDirty = true;

                    // Visual feedback
                    if (_effects.Count < 30)
                    {
                        _effects.Add(new VisualEffect
                        {
                            X = _player.Center.X,
                            Y = _player.Center.Y,
                            IsText = true,
                            Text = $"+{(int)_hpRegenAmount}",
                            Color = Color.LimeGreen,
                            Size = 10,
                            LifeTime = 40
                        });
                    }
                }
            }

            // Sync stats to UI throttled
            if (_statsDirty && _animCounter % 10 == 0)
            {
                _statsDirty = false;
                this.OnStatsUpdated?.Invoke(this, EventArgs.Empty);
            }

            NotificationManager.Update();

            UpdatePlayer();
            UpdateProjectiles();
            UpdateEffects();
            if (_screenShakeTimer > 0) _screenShakeTimer--;
            if (!_isTownMode)
            {
                UpdateEnemiesOptimized();
            }
        }
    }



    private void UpdatePlayer()
    {
        float currentSpeed = _player.Speed;
        if (_player.IsSlowed)
        {
            currentSpeed *= 0.6f;
            _player.SlowTimer--;
        }
        if (_player.HitTimer > 0) _player.HitTimer--;

        float proposedX = _player.X;
        float proposedY = _player.Y;
        if (_w)
        {
            proposedY -= currentSpeed;
        }
        if (_s)
        {
            proposedY += currentSpeed;
        }
        if (_a)
        {
            proposedX -= currentSpeed;
        }
        if (_d)
        {
            proposedX += currentSpeed;
            _player.FacingRight = true;
        }
        else if (_a)
        {
            _player.FacingRight = false;
        }

        if (_w || _a || _s || _d)
        {
            _player.IsMoving = true;
            _player.AnimTimer += 0.04f * currentSpeed;
        }
        else
        {
            _player.IsMoving = false;
            _player.AnimTimer += 0.05f;
        }

        if (_player.VisualAttackTimer > 0) _player.VisualAttackTimer -= 1f;

        Rectangle proposedRect = new Rectangle((int)proposedX, (int)proposedY, _player.Width, _player.Height);
        bool collides = false;
        float pushX = 0f, pushY = 0f;

        if (_isTownMode)
        {
            foreach (NpcEntity npc in _npcs)
            {
                Rectangle npcRect = new Rectangle((int)npc.X, (int)npc.Y, npc.Width, npc.Height);
                if (proposedRect.IntersectsWith(npcRect))
                {
                    collides = true;
                    break;
                }
            }
        }
        else
        {
            float playerCenterX = proposedX + _player.Width / 2f;
            float playerCenterY = proposedY + _player.Height / 2f;
            float minDist = 20f;

            int enemyCount = _enemies.Count;
            float minDistSq = minDist * minDist;

            for (int i = 0; i < enemyCount; i++)
            {
                var en = _enemies[i];
                if (en.CurrentHP <= 0) continue;

                float enemyCenterX = en.X + en.Width / 2f;
                float enemyCenterY = en.Y + en.Height / 2f;
                float dx = playerCenterX - enemyCenterX;
                float dy = playerCenterY - enemyCenterY;
                float distSq = dx * dx + dy * dy;

                if (distSq < minDistSq && distSq > 0.001f)
                {
                    float dist = (float)Math.Sqrt(distSq);
                    float overlap = minDist - dist;
                    pushX += (dx / dist) * overlap * 0.5f;
                    pushY += (dy / dist) * overlap * 0.5f;
                }
            }
        }

        if (!collides)
        {
            _player.X = proposedX + pushX;
            _player.Y = proposedY + pushY;
            ClampEntityPosition(_player);
        }
    }

    private void UpdateProjectiles()
    {
        // Use forward iteration with index tracking for safe removal
        int writeIndex = 0;
        int projCount = _projectiles.Count;

        var (arenaW, arenaH) = GetArenaSize();

        for (int i = 0; i < projCount; i++)
        {
            SkillProjectile proj = _projectiles[i];
            proj.Move();

            bool shouldRemove = false;

            // Bounds check
            if (proj.X < -20f || proj.X > arenaW + 20f || proj.Y < -20f || proj.Y > arenaH + 20f)
            {
                shouldRemove = true;
            }
            else
            {
                bool hit = false;
                if (proj.IsEnemy)
                {
                    // Use pre-computed bounds
                    var projBounds = proj.Bounds;
                    if (_player.Bounds.IntersectsWith(projBounds))
                    {
                        ApplyDamageToPlayer(proj.Damage);
                        hit = true;
                    }
                }
                else
                {
                    var projBounds = proj.Bounds;
                    int enemyCount = _enemies.Count;

                    for (int e = 0; e < enemyCount; e++)
                    {
                        var en = _enemies[e];
                        if (en.CurrentHP <= 0) continue;

                        if (projBounds.IntersectsWith(en.Bounds))
                        {
                            if (proj.IsAoE)
                            {
                                float projCx = proj.X;
                                float projCy = proj.Y;
                                float aoeSq = proj.AoERadius * proj.AoERadius;

                                // Only check living enemies
                                for (int t = 0; t < enemyCount; t++)
                                {
                                    var target = _enemies[t];
                                    if (target.CurrentHP <= 0) continue;

                                    float dx = target.Center.X - projCx;
                                    float dy = target.Center.Y - projCy;
                                    float distSq = dx * dx + dy * dy;

                                    if (distSq <= aoeSq)
                                    {
                                        float dist = MathF.Sqrt(distSq);
                                        float dmgMult = 1f - (dist / proj.AoERadius) * 0.5f;
                                        int aoeDmg = (int)(proj.Damage * dmgMult);
                                        ApplyDamageToEnemy(target, aoeDmg, proj.IsCrit, proj.SecondaryEffect);
                                    }
                                }

                                // Only add effect if under limit
                                if (_effects.Count < 50)
                                {
                                    int effectType = proj.VisualType switch
                                    {
                                        1 => 12,
                                        2 => 13,
                                        _ => 14
                                    };
                                    Color effectColor = proj.VisualType switch
                                    {
                                        1 => Color.OrangeRed,
                                        2 => Color.LightCyan,
                                        _ => Color.MediumPurple
                                    };
                                    _effects.Add(new VisualEffect
                                    {
                                        X = proj.X,
                                        Y = proj.Y,
                                        EffectType = effectType,
                                        Size = proj.AoERadius,
                                        MaxRadius = proj.AoERadius,
                                        Color = effectColor,
                                        LifeTime = 20,
                                        InitialLifeTime = 20
                                    });
                                }
                            }
                            else
                            {
                                ApplyDamageToEnemy(en, proj.Damage, proj.IsCrit, proj.SecondaryEffect);
                            }
                            hit = true;
                            break;
                        }
                    }
                }

                if (hit)
                {
                    shouldRemove = true;
                }
            }

            // Keep alive projectiles
            if (!shouldRemove)
            {
                if (writeIndex != i)
                {
                    _projectiles[writeIndex] = proj;
                }
                writeIndex++;
            }
        }

        // Batch remove dead projectiles
        if (writeIndex < projCount)
        {
            _projectiles.RemoveRange(writeIndex, projCount - writeIndex);
        }
    }

    private void ApplyDamageToPlayer(int dmg)
    {
        if (_isBattleEnding)
        {
            return;
        }

        // DON'T recalculate stats on every hit - too expensive!
        // Stats are refreshed periodically in StepSimulation already.

        int reduced = Math.Max(1, dmg - _player.Defense);
        _player.CurrentHP -= reduced;
        _player.HitTimer = 8; // Player flashes when hit
        if (_player.CurrentHP < 0)
        {
            _player.CurrentHP = 0;
        }

        _hero.HP = _player.CurrentHP;

        if (SessionManager.CurrentCharacter != null && SessionManager.CurrentCharacter.CharacterID == _hero.CharacterID)
        {
            SessionManager.CurrentCharacter = _hero;
        }

        _statsDirty = true;
        if (_player.CurrentHP <= 0)
        {
            StartBattleEndSequence(victory: false);
        }
    }

    // Flag for deferred DB save
    private void ApplyDamageToEnemy(BattleEntity target, int dmg, bool isCrit = false, Enums.SkillSecondaryEffect secondaryEffect = Enums.SkillSecondaryEffect.None, float effectAmount = 0)
    {
        // Check invulnerability or status

        // Critical Bonus
        if (isCrit)
        {
            dmg = (int)(dmg * 1.5f);
        }

        // Apply Status Effects logic
        switch (secondaryEffect)
        {
            case Enums.SkillSecondaryEffect.Burn:
                if (target.BurnTimer <= 0)
                {
                    target.BurnTimer = 180; // 3 seconds
                    _effects.Add(new VisualEffect { X = target.X, Y = target.Y - 20, IsText = true, Text = "YANMA", Color = Color.OrangeRed, Size = 10, LifeTime = 40 });
                }
                break;
            case Enums.SkillSecondaryEffect.Freeze:
                target.FreezeTimer = 120; // 2 seconds
                _effects.Add(new VisualEffect { X = target.X, Y = target.Y - 20, IsText = true, Text = "DONDU", Color = Color.Cyan, Size = 10, LifeTime = 40 });
                break;
            case Enums.SkillSecondaryEffect.Stun:
                target.StunTimer = 60; // 1 second
                _effects.Add(new VisualEffect { X = target.X, Y = target.Y - 20, IsText = true, Text = "SERSEM", Color = Color.Yellow, Size = 10, LifeTime = 40 });
                break;
            case Enums.SkillSecondaryEffect.Poison:
                if (target.PoisonTimer <= 0)
                {
                    target.PoisonTimer = 300; // 5 seconds
                    _effects.Add(new VisualEffect { X = target.X, Y = target.Y - 20, IsText = true, Text = "ZEHİR", Color = Color.LimeGreen, Size = 10, LifeTime = 40 });
                }
                break;
            case Enums.SkillSecondaryEffect.Slow:
                target.SlowTimer = 180; // 3 seconds
                break;
            case Enums.SkillSecondaryEffect.Shock:
                target.ShockTimer = 120;
                // Shock increases damage taken
                dmg = (int)(dmg * 1.2f);
                break;
        }

        target.CurrentHP -= dmg;
        target.HitTimer = isCrit ? 10 : 5; // Enemy flashes white on hit
        if (isCrit) _screenShakeTimer = 6; // Simple screen shake on crits

        // Life Steal
        if (_lifeStealPercent > 0 && _player.CurrentHP < _player.MaxHP)
        {
            int heal = (int)(dmg * (_lifeStealPercent / 100f));
            if (heal > 0)
            {
                _player.CurrentHP += heal;
                if (_player.CurrentHP > _player.MaxHP) _player.CurrentHP = _player.MaxHP;
                _hero.HP = _player.CurrentHP;

                // Visual for LS
                if (_effects.Count < 50 && _rnd.NextDouble() < 0.3)
                {
                    _effects.Add(new VisualEffect
                    {
                        X = _player.Center.X,
                        Y = _player.Center.Y,
                        Text = $"+{heal}",
                        Color = Color.Lime,
                        IsText = true,
                        Size = 10,
                        LifeTime = 30
                    });
                }
            }
        }

        // Limit effects to reduce rendering load
        if (_effects.Count < 50)
        {
            Color txtColor = isCrit ? Color.Gold : Color.White;
            string txt = $"-{dmg}" + (isCrit ? "!" : "");
            int fontSize = isCrit ? 14 : 11;

            _effects.Add(new VisualEffect
            {
                X = target.X + 10f,
                Y = target.Y,
                Text = txt,
                Color = txtColor,
                IsText = true,
                Size = fontSize,
                LifeTime = isCrit ? 40 : 30
            });

            // Only add hit effect, skip the other two for performance
            _effects.Add(new VisualEffect
            {
                X = target.X + _rnd.Next(0, target.Width),
                Y = target.Y + _rnd.Next(0, target.Height),
                EffectType = 1,
                Size = isCrit ? 60 : 40,
                Color = isCrit ? Color.OrangeRed : Color.White,
                LifeTime = 10
            });
        }

        if (target.CurrentHP <= 0)
        {
            target.CurrentHP = 0;
            if (_effects.Count < 50)
            {
                _effects.Add(new VisualEffect
                {
                    X = target.Center.X,
                    Y = target.Center.Y,
                    EffectType = 2,
                    Size = 10,
                    Color = Color.DarkGray,
                    LifeTime = 25
                });
            }
        }
    }

    private void LoadHotbarFromSession()
    {
        if (_hero == null) return;
        List<ItemInstance> inv = _invManager.GetInventory(_hero.CharacterID);

        SkillManager sm = new SkillManager();
        var skills = sm.LoadSkillsForClass((Enums.CharacterClass)_hero.Class, _hero.CharacterID);

        for (int i = 0; i < 5; i++)
        {
            var oldSkill = _hotbar[i].Skill; // Preserve old skill to keep LastCastTime
            HotbarInfo info = SessionManager.HotbarSlots[i];

            _hotbar[i].Type = 0;
            _hotbar[i].ReferenceID = null;
            _hotbar[i].Item = null;
            _hotbar[i].Skill = null;
            _hotbar[i].CachedImage = null;

            if (info != null)
            {
                _hotbar[i].Type = info.Type;
                _hotbar[i].ReferenceID = info.ReferenceId;

                if (info.Type == 0)
                {
                    ItemInstance item = inv.FirstOrDefault((ItemInstance x) => x.InstanceID == info.ReferenceId);
                    _hotbar[i].Item = item;
                    _hotbar[i].CachedImage = (item != null) ? rpg_deneme.UI.ItemDrawer.DrawItem(item) : null;
                }
                else if (info.Type == 1)
                {
                    _hotbar[i].Skill = skills.FirstOrDefault(s => s.SkillID == info.ReferenceId);

                    if (_hotbar[i].Skill != null)
                    {
                        // Restore LastCastTime if it's the same skill
                        if (oldSkill != null && oldSkill.SkillID == _hotbar[i].Skill.SkillID)
                        {
                            _hotbar[i].Skill.LastCastTime = oldSkill.LastCastTime;
                        }

                        Bitmap bmp = new Bitmap(40, 40);
                        using (Graphics gw = Graphics.FromImage(bmp))
                        {
                            gw.Clear(Color.Transparent);
                            bool drawn = false;

                            if (!string.IsNullOrEmpty(_hotbar[i].Skill.IconPath))
                            {
                                string path = System.IO.Path.Combine(Application.StartupPath, "Assets", "Skills", _hotbar[i].Skill.IconPath + ".png");
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
                                string l = (!_hotbar[i].Skill.Name.StartsWith("?")) ? _hotbar[i].Skill.Name.Substring(0, 1) : "?";
                                using (Font f = new Font("Segoe UI", 12, FontStyle.Bold))
                                    gw.DrawString(l, f, Brushes.White, 10, 10);
                            }
                        }
                        _hotbar[i].CachedImage = bmp;
                    }
                }
            }
        }
    }


    private void SaveHotbarToSession()
    {
        HotbarRepository repo = new HotbarRepository();
        int charId = (_hero != null) ? _hero.CharacterID : 0;

        for (int i = 0; i < 5; i++)
        {
            if (_hotbar[i].ReferenceID.HasValue)
            {
                SessionManager.HotbarSlots[i] = new HotbarInfo
                {
                    Type = _hotbar[i].Type,
                    ReferenceId = _hotbar[i].ReferenceID.Value
                };
            }
            else
            {
                SessionManager.HotbarSlots[i] = null;
            }

            if (charId > 0)
            {
                repo.SaveHotbar(charId, i, SessionManager.HotbarSlots[i]);
            }
        }
    }

    private void UpdateHotbarState()
    {
        LoadHotbarFromSession();
    }

    private void UseHotbarSlot(int idx)
    {
        if (idx < 0 || idx >= _hotbar.Count) return;

        var slot = _hotbar[idx];

        if (slot.Type == 0)
        {
            if (slot.Item == null) return;
            ItemInstance item = slot.Item;
            if (item.RemainingCooldownSeconds > 0) return;

            ConsumableManager cm = new ConsumableManager();
            if (cm.UseItem(_hero, item, _learnedSkills).Success)
            {
                _effects.Add(new VisualEffect
                {
                    X = _player.Center.X,
                    Y = _player.Center.Y,
                    EffectType = 3,
                    Size = 40,
                    Color = (item.EffectType == Enums.ItemEffectType.RestoreHP ? Color.Lime : Color.DeepSkyBlue),
                    LifeTime = 60
                });

                if (SessionManager.CurrentCharacter != null && SessionManager.CurrentCharacter.CharacterID == _hero.CharacterID)
                {
                    SessionManager.CurrentCharacter = _hero;
                }

                this.OnStatsUpdated?.Invoke(this, EventArgs.Empty);
                SaveHotbarToSession();
                if (base.ParentForm is FormMain m)
                {
                    m.RefreshStats();
                }
            }
        }
        else if (slot.Type == 1)
        {
            if (slot.Skill == null) return;

            if (slot.Skill.RemainingCooldown > 0)
            {
                // NotificationManager.AddNotification($"Cooldown: {slot.Skill.RemainingCooldown:F1}s", Color.Yellow); // REMOVED as requested
                return;
            }

            if (_hero.Mana < slot.Skill.ManaCost)
            {
                NotificationManager.AddNotification($"Yetersiz Mana! ({slot.Skill.ManaCost})", Color.Red);
                return;
            }

            _hero.Mana -= slot.Skill.ManaCost;
            _player.CurrentMana = _hero.Mana;

            slot.Skill.LastCastTime = DateTime.Now;

            SkillManager skillMgr = new SkillManager();

            bool skillSuccess = false;
            double effectVal = slot.Skill.GetCurrentEffectValue();

            if (slot.Skill.Name.Contains("Area") || slot.Skill.Name.Contains("Whirlwind") || slot.Skill.Name.Contains("Meteor") || slot.Skill.Name.Contains("Dönme") || slot.Skill.Name.Contains("Nova"))
            {
                int range = 150;
                bool isMeteor = slot.Skill.Name.Contains("Meteor");
                bool isWhirlwind = slot.Skill.Name.Contains("Whirlwind") || slot.Skill.Name.Contains("Dönme");
                if (isMeteor) range = 250;

                float aoeX = isMeteor ? _mousePos.X : _player.Center.X;
                float aoeY = isMeteor ? _mousePos.Y : _player.Center.Y;

                var equipment = GetEquipment();
                var weapon = equipment.FirstOrDefault(x => x.ItemType == Enums.ItemType.Weapon);

                foreach (var entity in _enemies.ToList())
                {
                    if (entity.CurrentHP <= 0) continue;

                    double dist = Math.Sqrt(Math.Pow(entity.Center.X - aoeX, 2) + Math.Pow(entity.Center.Y - aoeY, 2));
                    if (dist <= range && entity.CurrentHP > 0)
                    {
                        (int min, int max) dmgRange = (0, 0);
                        if (_hero.Class == 3)
                            dmgRange = StatManager.CalculateMagicalDamage(_hero, weapon, _learnedSkills);
                        else
                            dmgRange = StatManager.CalculatePhysicalDamage(_hero, weapon, _learnedSkills);

                        int baseDmg = _rnd.Next(dmgRange.min, dmgRange.max + 1);
                        int skillDmg = (int)effectVal;
                        int elementBonus = StatManager.CalculateElementBonus(_learnedSkills, slot.Skill.Element);
                        int finalDmg = baseDmg + skillDmg + elementBonus;

                        int critChance = StatManager.CalculateCritChance(_hero, equipment, _learnedSkills);
                        bool isCrit = _rnd.Next(100) < critChance;
                        if (isCrit) finalDmg *= 2;

                        ApplyDamageToEnemy(entity, finalDmg, isCrit);
                        skillSuccess = true;
                    }
                }

                int effectType = 11;
                Color effectColor = Color.Goldenrod;

                if (isMeteor || slot.Skill.Name.Contains("Fire") || slot.Skill.Name.Contains("Ateş"))
                {
                    effectType = 12;
                    effectColor = Color.OrangeRed;
                }
                else if (slot.Skill.Name.Contains("Ice") || slot.Skill.Name.Contains("Buz") || slot.Skill.Name.Contains("Nova"))
                {
                    effectType = 13;
                    effectColor = Color.LightCyan;
                }
                else if (slot.Skill.Name.Contains("Arcane") || slot.Skill.Name.Contains("Karanlık"))
                {
                    effectType = 14;
                    effectColor = Color.MediumPurple;
                }

                _effects.Add(new VisualEffect
                {
                    X = aoeX,
                    Y = aoeY,
                    EffectType = effectType,
                    Size = range,
                    Color = effectColor,
                    LifeTime = 25,
                    InitialLifeTime = 25,
                    MaxRadius = range
                });
            }
            else
            {
                bool isMage = (_hero.Class == 3);

                var equipment = GetEquipment();
                var weapon = equipment.FirstOrDefault(x => x.ItemType == Enums.ItemType.Weapon);

                (int min, int max) dmgRange = (0, 0);
                if (isMage)
                    dmgRange = StatManager.CalculateMagicalDamage(_hero, weapon, _learnedSkills);
                else
                    dmgRange = StatManager.CalculatePhysicalDamage(_hero, weapon, _learnedSkills);

                int baseDmg = _rnd.Next(dmgRange.min, dmgRange.max + 1);
                int skillDmg = (int)effectVal;

                int elementBonus = StatManager.CalculateElementBonus(_learnedSkills, slot.Skill.Element);
                int finalDmg = baseDmg + skillDmg + elementBonus;

                int critChance = StatManager.CalculateCritChance(_hero, equipment, _learnedSkills);
                bool isCrit = _rnd.Next(100) < critChance;
                if (isCrit) finalDmg *= 2;

                Enums.SkillSecondaryEffect secEffect = slot.Skill.SecondaryEffect;

                if (isMage)
                {
                    int vType = 0;
                    switch (slot.Skill.Element)
                    {
                        case SkillElement.Fire: vType = 1; break;
                        case SkillElement.Ice: vType = 2; break;
                        case SkillElement.Dark: vType = 3; break;
                        case SkillElement.Lightning: vType = 4; break;
                        case SkillElement.Poison: vType = 5; break;
                    }

                    if (slot.Skill.Name.Contains("Zincirleme") || slot.Skill.Name.Contains("Chain"))
                    {
                        var targets = _enemies.Where(e => e.CurrentHP > 0)
                            .OrderBy(e => Math.Sqrt(Math.Pow(e.X - _mousePos.X, 2) + Math.Pow(e.Y - _mousePos.Y, 2)))
                            .Take(5)
                            .ToList();

                        float lastX = _player.Center.X, lastY = _player.Center.Y;
                        int i = 0;
                        foreach (var target in targets)
                        {
                            _effects.Add(new VisualEffect
                            {
                                X = lastX,
                                Y = lastY,
                                EffectType = 15,
                                TargetX = target.Center.X,
                                TargetY = target.Center.Y,
                                Color = Color.Yellow,
                                LifeTime = 20,
                                InitialLifeTime = 20,
                                StartDelay = i * 5 // Delay each bounce by 5 frames
                            });

                            ApplyDamageToEnemy(target, finalDmg, isCrit, secEffect);
                            lastX = target.Center.X;
                            lastY = target.Center.Y;
                            finalDmg = (int)(finalDmg * 0.8);
                            i++; // Increment counter for delay
                        }
                        skillSuccess = targets.Count > 0;
                    }
                    else if (slot.Skill.Element == SkillElement.Fire)
                    {
                        var proj = new SkillProjectile(_player.Center.X, _player.Center.Y, _mousePos.X, _mousePos.Y, finalDmg, enemy: false, isCrit: isCrit, visualType: vType);
                        proj.IsAoE = true;
                        proj.AoERadius = 60 + (slot.Skill.CurrentLevel * 10);
                        proj.SecondaryEffect = secEffect;
                        _projectiles.Add(proj);
                        skillSuccess = true;
                    }
                    else
                    {
                        _projectiles.Add(new SkillProjectile(_player.Center.X, _player.Center.Y, _mousePos.X, _mousePos.Y, finalDmg, enemy: false, isCrit: isCrit, visualType: vType) { SecondaryEffect = secEffect });
                        skillSuccess = true;
                    }
                }
                else
                {
                    bool isAoEMelee = slot.Skill.Name.Contains("Dönme") || slot.Skill.Name.Contains("Whirl") ||
                        slot.Skill.Name.Contains("Ezici") || slot.Skill.Name.Contains("Crush");

                    float range = isAoEMelee ? 120f : 100f;

                    float mdx = _mousePos.X - _player.Center.X;
                    float mdy = _mousePos.Y - _player.Center.Y;
                    float mDist = (float)Math.Sqrt(mdx * mdx + mdy * mdy);
                    if (mDist < 1f) mDist = 1f;
                    float dirX = mdx / mDist;
                    float dirY = mdy / mDist;
                    float facingAngle = (float)Math.Atan2(dirY, dirX);

                    int effectType = isAoEMelee ? 11 : 10;
                    Color slashColor = slot.Skill.Name.Contains("Ezici") ? Color.Red : Color.Silver;

                    _effects.Add(new VisualEffect
                    {
                        X = _player.Center.X,
                        Y = _player.Center.Y,
                        EffectType = effectType,
                        Size = (int)range,
                        Color = slashColor,
                        LifeTime = 30,
                        InitialLifeTime = 30,
                        Angle = facingAngle,
                        MaxRadius = (int)range
                    });

                    int hitCount = 0;
                    float rangeSq = range * range;
                    for (int k = 0; k < _enemies.Count; k++)
                    {
                        var entity = _enemies[k];
                        if (entity.CurrentHP <= 0) continue;

                        float edx = entity.Center.X - _player.Center.X;
                        float edy = entity.Center.Y - _player.Center.Y;
                        float distSq = edx * edx + edy * edy;

                        if (distSq <= rangeSq)
                        {
                            if (isAoEMelee)
                            {
                                ApplyDamageToEnemy(entity, finalDmg, isCrit, secEffect);
                                hitCount++;
                            }
                            else
                            {
                                float entityAngle = (float)Math.Atan2(edy, edx);
                                float angleDiff = Math.Abs(entityAngle - facingAngle);
                                if (angleDiff > Math.PI) angleDiff = (float)(2 * Math.PI - angleDiff);

                                if (angleDiff < Math.PI / 3)
                                {
                                    ApplyDamageToEnemy(entity, finalDmg, isCrit, secEffect);
                                    hitCount++;
                                }
                            }
                        }
                    }
                    skillSuccess = hitCount > 0 || isAoEMelee;
                }

                if (isMage)
                {
                    string n = slot.Skill.Name;
                    int castEffectType = 0;
                    Color castColor = Color.Cyan;

                    if (n.Contains("Fire") || n.Contains("Ateş") || n.Contains("Alev"))
                    {
                        castEffectType = 12;
                        castColor = Color.Orange;
                    }
                    else if (n.Contains("Ice") || n.Contains("Buz") || n.Contains("Frost"))
                    {
                        castEffectType = 13;
                        castColor = Color.LightCyan;
                    }
                    else if (n.Contains("Arcane") || n.Contains("Karanlık"))
                    {
                        castEffectType = 14;
                        castColor = Color.MediumPurple;
                    }

                    _effects.Add(new VisualEffect
                    {
                        X = _player.Center.X,
                        Y = _player.Center.Y,
                        EffectType = castEffectType,
                        Color = castColor,
                        LifeTime = 12,
                        InitialLifeTime = 12,
                        Size = 40
                    });
                }
            }

            CharacterRepository repo = new CharacterRepository();
            repo.UpdateProgress(_hero);
            if (base.ParentForm is FormMain m) m.UpdateBars(null);
            _statsDirty = true;
        }
    }

    private async void StartBattleEndSequence(bool victory)
    {
        _isBattleEnding = true;
        await Task.Delay(1000);
        StopGameLoop();

        this.OnBattleEnded?.Invoke(this, victory);
    }

    public void ShowBattleResults(string message, Action onPrimary, string primaryText = "DEVAM ET", Action onSecondary = null, string secondaryText = "")
    {
        _showResults = true;
        _resultMessage = message;
        _onPrimary = onPrimary;
        _primaryBtnText = primaryText;
        _onSecondary = onSecondary;
        _secondaryBtnText = secondaryText;
        _skgl?.Invalidate();
    }



    private void UpdateTownLayout()
    {
        if (!_isTownMode) return;

        var (w, h) = GetArenaSize();
        int cx = w / 2;
        int cy = h / 2;

        for (int i = 0; i < _npcs.Count; i++)
        {
            NpcEntity npc = _npcs[i];
            switch (npc.Type)
            {
                case Enums.NpcType.ArenaMaster: // Top Center
                    npc.X = cx - npc.Width / 2;
                    npc.Y = cy - 180;
                    break;
                case Enums.NpcType.Merchant: // Top Left
                    npc.X = cx - 150;
                    npc.Y = cy - 120;
                    break;
                case Enums.NpcType.BlackSmith: // Top Right
                    npc.X = cx + 120;
                    npc.Y = cy - 120;
                    break;
                case Enums.NpcType.StorageKeeper: // Bottom Left
                    npc.X = cx - 150;
                    npc.Y = cy + 100;
                    break;
                case Enums.NpcType.Teleporter: // Bottom Right
                    npc.X = cx + 120;
                    npc.Y = cy + 100;
                    break;
            }
        }

        if (_btnMap != null)
        {
            _btnMap.Location = new Point(w - 180, 20);
        }
    }

    private void InitTownEntities()
    {
        _npcs = new List<NpcEntity>
        {
            new NpcEntity("MARKET", Enums.NpcType.Merchant,0,0),
            new NpcEntity("IŞINLAYICI", Enums.NpcType.Teleporter,0,0),
            new NpcEntity("DEPOCU", Enums.NpcType.StorageKeeper,0,0)
            {
                Color = Color.Brown
            },
            new NpcEntity("DEMİRCİ", Enums.NpcType.BlackSmith,0,0)
            {
                Color = Color.DarkSlateGray
            },
            new NpcEntity("ARENA GUARD", Enums.NpcType.ArenaMaster,0,0)
            {
                Color = Color.Red
            }
        };
    }

    private void UpdateEffects()
    {
        // Use backwards iteration to safely remove - but batch removals
        int writeIndex = 0;
        int effectCount = _effects.Count;

        for (int i = 0; i < effectCount; i++)
        {
            var fx = _effects[i];
            if (fx.StartDelay > 0)
            {
                fx.StartDelay--;
                // Keep alive (in list) but don't process lifetime or movement yet
                if (writeIndex != i)
                {
                    _effects[writeIndex] = fx;
                }
                writeIndex++;
                continue;
            }

            fx.LifeTime--;

            if (fx.IsText)
            {
                fx.Y -= 1f;
            }

            // Keep alive effects - swap to front
            if (fx.LifeTime > 0)
            {
                if (writeIndex != i)
                {
                    _effects[writeIndex] = fx;
                }
                writeIndex++;
            }
        }

        // Trim dead effects in one operation
        if (writeIndex < effectCount)
        {
            _effects.RemoveRange(writeIndex, effectCount - writeIndex);
        }
    }
    private void UpdateEnemiesOptimized()
    {
        if (_enemyAttackCooldown > 0)
        {
            _enemyAttackCooldown--;
        }

        int aliveCount = 0;
        int enemyCount = _enemies.Count;

        // Infer player velocity for prediction (Intercept logic)
        float pVx = 0f, pVy = 0f;
        float pSpeed = 4f;
        if (_w) pVy -= pSpeed;
        if (_s) pVy += pSpeed;
        if (_a) pVx -= pSpeed;
        if (_d) pVx += pSpeed;

        float playerCenterX = _player.Center.X;
        float playerCenterY = _player.Center.Y;

        // 1. Build Spatial Grid (O(N))
        _enemyGrid.Build(_enemies, 80f);

        for (int i = 0; i < enemyCount; i++)
        {
            BattleEntity enemy = _enemies[i];

            // Status Effects Logic
            if (enemy.BurnTimer > 0)
            {
                enemy.BurnTimer--;
                if (enemy.BurnTimer % 60 == 0)
                {
                    enemy.CurrentHP -= 5;
                    _effects.Add(new VisualEffect { X = enemy.X, Y = enemy.Y - 10, IsText = true, Text = "5", Color = Color.OrangeRed, Size = 9, LifeTime = 30 });
                }
            }
            if (enemy.PoisonTimer > 0)
            {
                enemy.PoisonTimer--;
                if (enemy.PoisonTimer % 60 == 0)
                {
                    enemy.CurrentHP -= 3;
                    _effects.Add(new VisualEffect { X = enemy.X, Y = enemy.Y - 10, IsText = true, Text = "3", Color = Color.LimeGreen, Size = 9, LifeTime = 30 });
                }
            }
            if (enemy.StunTimer > 0) enemy.StunTimer--;
            if (enemy.FreezeTimer > 0) enemy.FreezeTimer--;
            if (enemy.SlowTimer > 0) enemy.SlowTimer--;
            if (enemy.ShockTimer > 0) enemy.ShockTimer--;
            if (enemy.WeaknessTimer > 0) enemy.WeaknessTimer--;
            if (enemy.HitTimer > 0) enemy.HitTimer--;

            // AI Cooldowns (Sprint)
            if (enemy.SprintCooldown > 0) enemy.SprintCooldown--;
            if (enemy.SprintTimer > 0) enemy.SprintTimer--;

            if (enemy.CurrentHP <= 0) continue;

            // Skip movement if stunned or frozen
            if (enemy.IsStunned || enemy.IsFrozen)
            {
                enemy.IsMoving = false;
                continue;
            }
            aliveCount++;

            float enemyCenterX = enemy.X + enemy.Width / 2f;
            float enemyCenterY = enemy.Y + enemy.Height / 2f;

            // Vector to Player
            float dx = playerCenterX - enemyCenterX;
            float dy = playerCenterY - enemyCenterY;
            float distSqToPlayer = dx * dx + dy * dy;
            float distToPlayer = MathF.Sqrt(distSqToPlayer);
            if (distToPlayer < 0.001f) distToPlayer = 0.001f;

            // --- PREDICTION (Intercept Logic) ---
            float lookAhead = Math.Min(distToPlayer / 10f, 20f);
            float targetX = playerCenterX + pVx * lookAhead;
            float targetY = playerCenterY + pVy * lookAhead;

            float tDx = targetX - enemyCenterX;
            float tDy = targetY - enemyCenterY;
            float distToTarget = MathF.Sqrt(tDx * tDx + tDy * tDy);

            float dirX = (distToTarget > 0.001f) ? tDx / distToTarget : 0;
            float dirY = (distToTarget > 0.001f) ? tDy / distToTarget : 0;

            // --- FLANKING (Melee) ---
            if (!enemy.IsRanged && distToPlayer < 200f)
            {
                // Add tangential component to circle around
                float flankDir = (i % 2 == 0) ? 1f : -1f;
                float tangentX = -dirY * flankDir;
                float tangentY = dirX * flankDir;

                // Blend approach and flank
                dirX = dirX * 0.6f + tangentX * 0.4f;
                dirY = dirY * 0.6f + tangentY * 0.4f;

                // Re-normalize
                float dMag = MathF.Sqrt(dirX * dirX + dirY * dirY);
                if (dMag > 0.01f) { dirX /= dMag; dirY /= dMag; }
            }

            // --- SPRINT / DASH ---
            if (!enemy.IsRanged && enemy.SprintCooldown <= 0 && enemy.SprintTimer <= 0)
            {
                if (distToPlayer > 80f && distToPlayer < 350f)
                {
                    if (_rnd.NextDouble() < 0.02)
                    {
                        enemy.SprintTimer = 40; // 0.6s
                        enemy.SprintCooldown = 240; // 4s
                        _effects.Add(new VisualEffect
                        {
                            X = enemy.X + enemy.Width / 2,
                            Y = enemy.Y,
                            IsText = true,
                            Text = "!",
                            Color = Color.Red,
                            Size = 16,
                            LifeTime = 20
                        });
                    }
                }
            }

            // --- Separation Logic using Grid ---
            float sepX = 0f;
            float sepY = 0f;
            int neighbors = 0;

            float separationRadius = 28f;
            float separationRadiusSq = separationRadius * separationRadius;

            _enemyGrid.ForEachNeighborIndex(enemyCenterX, enemyCenterY, (idx) =>
            {
                if (idx == i) return;

                BattleEntity other = _enemies[idx];
                if (other.CurrentHP <= 0) return;

                float odx = enemyCenterX - (other.X + other.Width / 2f);
                float ody = enemyCenterY - (other.Y + other.Height / 2f);
                float distSq = odx * odx + ody * ody;

                if (distSq < separationRadiusSq && distSq > 0.0001f)
                {
                    float dist = MathF.Sqrt(distSq);
                    float push = (separationRadius - dist) / separationRadius;
                    sepX += (odx / dist) * push;
                    sepY += (ody / dist) * push;
                    neighbors++;
                }
            });

            if (neighbors > 0)
            {
                sepX /= neighbors;
                sepY /= neighbors;
            }

            // --- Steering Synthesis ---
            float desiredRange = (enemy.AttackRange > 0) ? enemy.AttackRange : (enemy.IsRanged ? 250f : 45f);
            float rangeVariation = (i % 5) * 6.0f;
            desiredRange += rangeVariation;

            float moveX = 0f;
            float moveY = 0f;

            float separationWeight = 3.5f;
            float approachWeight = 1.0f;

            if (enemy.IsRanged)
            {
                if (distToPlayer > desiredRange)
                {
                    moveX = dirX * approachWeight + sepX * separationWeight;
                    moveY = dirY * approachWeight + sepY * separationWeight;
                }
                else if (distToPlayer < desiredRange * 0.6f)
                {
                    moveX = -dirX * 1.5f + sepX * separationWeight;
                    moveY = -dirY * 1.5f + sepY * separationWeight;
                }
                else
                {
                    moveX = sepX * separationWeight;
                    moveY = sepY * separationWeight;
                }
            }
            else
            {
                if (distToPlayer > desiredRange)
                {
                    moveX = dirX * approachWeight + sepX * separationWeight;
                    moveY = dirY * approachWeight + sepY * separationWeight;
                }
                else
                {
                    moveX = sepX * separationWeight;
                    moveY = sepY * separationWeight;
                }
            }

            float mag = moveX * moveX + moveY * moveY;
            if (mag > 0.001f)
            {
                float norm = MathF.Sqrt(mag);
                moveX /= norm;
                moveY /= norm;
            }
            else
            {
                moveX = 0f; moveY = 0f;
            }

            float currentSpeed = enemy.Speed;
            if (enemy.IsSlowed) currentSpeed *= 0.5f;
            if (enemy.IsSprinting) currentSpeed *= 1.8f; // Dash speed

            enemy.VX = Lerp(enemy.VX, moveX * currentSpeed, 0.2f);
            enemy.VY = Lerp(enemy.VY, moveY * currentSpeed, 0.2f);

            enemy.X += enemy.VX;
            enemy.Y += enemy.VY;

            bool isMoving = MathF.Abs(enemy.VX) > 0.1f || MathF.Abs(enemy.VY) > 0.1f;
            enemy.IsMoving = isMoving;
            enemy.AnimTimer += isMoving ? (currentSpeed * 0.03f) : 0.05f;
            if (dx != 0) enemy.FacingRight = dx > 0; // Face player

            if (enemy.VisualAttackTimer > 0) enemy.VisualAttackTimer--;

            if (enemy.IsRanged)
            {
                if (distToPlayer <= desiredRange + 150f)
                {
                    if (enemy.DecisionCooldown > 0) enemy.DecisionCooldown--;
                    if (enemy.DecisionCooldown <= 0 && _rnd.NextDouble() < 0.02)
                    {
                        _projectiles.Add(new SkillProjectile(enemyCenterX, enemyCenterY, playerCenterX, playerCenterY,
                            _rnd.Next(enemy.MinDamage, enemy.MaxDamage + 1), enemy: true));
                        enemy.VisualAttackTimer = 10f;
                        enemy.DecisionCooldown = 90;
                    }
                }
            }
            else
            {
                if (distToPlayer <= 65f && enemy.AttackCooldown <= 0)
                {
                    enemy.VisualAttackTimer = 10f;
                    enemy.AttackCooldown = 75;
                    ApplyDamageToPlayer(_rnd.Next(enemy.MinDamage, enemy.MaxDamage + 1));

                    // Slow Player Chance
                    if (_player.SlowTimer <= 0 && _rnd.NextDouble() < 0.35)
                    {
                        _player.SlowTimer = 45; // 0.75s slow
                        _effects.Add(new VisualEffect
                        {
                            X = _player.Center.X,
                            Y = _player.Center.Y - 20,
                            IsText = true,
                            Text = "Slowed!",
                            Color = Color.LightBlue,
                            Size = 12,
                            LifeTime = 40
                        });
                    }
                }
            }
            if (enemy.AttackCooldown > 0) enemy.AttackCooldown--;
            ClampEntityPosition(enemy);
        }

        if (_enemies.Count > 0)
        {
            _enemies.RemoveAll(x => x.CurrentHP <= 0);
        }

        if (aliveCount == 0 && !_isBattleEnding)
        {
            StartBattleEndSequence(victory: true);
        }
    }
}
