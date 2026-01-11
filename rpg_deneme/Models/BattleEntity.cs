using System.Drawing;

namespace rpg_deneme.Models;

public class BattleEntity
{
    private float _x;
    private float _y;

    public float X
    {
        get => _x;
        set
        {
            if (_x != value)
            {
                _x = value;
                InvalidateCache();
            }
        }
    }

    public float Y
    {
        get => _y;
        set
        {
            if (_y != value)
            {
                _y = value;
                InvalidateCache();
            }
        }
    }

    public int Width { get; set; }

    public int Height { get; set; }

    public float Speed { get; set; }

    public int CurrentHP { get; set; }

    public int MaxHP { get; set; }

    public int CurrentMana { get; set; }
    public int MaxMana { get; set; }

    public string Name { get; set; }

    public int MinDamage { get; set; }
    public int MaxDamage { get; set; }

    public int Defense { get; set; }

    public float VX { get; set; }

    public float VY { get; set; }

    public int AIState { get; set; }

    public int DecisionCooldown { get; set; }

    public int StrafeSign { get; set; } = 1;

    // Cached bounds and center to avoid allocation every frame
    private Rectangle _cachedBounds;
    private Point _cachedCenter;
    private bool _cacheValid = false;

    private void InvalidateCache()
    {
        _cacheValid = false;
    }

    public Rectangle Bounds
    {
        get
        {
            if (!_cacheValid)
            {
                RecalculateCache();
            }
            return _cachedBounds;
        }
    }

    public Point Center
    {
        get
        {
            if (!_cacheValid)
            {
                RecalculateCache();
            }
            return _cachedCenter;
        }
    }

    private void RecalculateCache()
    {
        _cachedBounds = new Rectangle((int)_x, (int)_y, Width, Height);
        _cachedCenter = new Point((int)(_x + Width / 2f), (int)(_y + Height / 2f));
        _cacheValid = true;
    }

    public bool IsRanged { get; set; } = false;

    public int AttackRange { get; set; } = 50;

    // Animation Properties
    public float AnimTimer { get; set; }
    public bool IsMoving { get; set; }
    public bool FacingRight { get; set; } = true;
    public float VisualAttackTimer { get; set; } // 0 = no attack, >0 = swinging

    // Per-entity attack cooldown
    public int AttackCooldown { get; set; }

    // Status Effects (Debuffs) Timers (in frames)
    public int BurnTimer { get; set; }
    public int SlowTimer { get; set; }
    public int StunTimer { get; set; }
    public int PoisonTimer { get; set; }
    public int FreezeTimer { get; set; }
    public int ShockTimer { get; set; }
    public int BleedTimer { get; set; }
    public int WeaknessTimer { get; set; }

    public bool IsStunned => StunTimer > 0;
    public bool IsFrozen => FreezeTimer > 0;
    public bool IsSlowed => SlowTimer > 0;
    public bool IsShocked => ShockTimer > 0;
    public bool IsWeak => WeaknessTimer > 0;

    // AI Behaviors
    public int SprintCooldown { get; set; }
    public int SprintTimer { get; set; }
    public bool IsSprinting => SprintTimer > 0;
}
