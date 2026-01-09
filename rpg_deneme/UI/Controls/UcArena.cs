using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using rpg_deneme.Business;
using rpg_deneme.Core;
using rpg_deneme.Models;
using rpg_deneme.UI.Controls.GameEntities;
using SkillProjectile = rpg_deneme.UI.Controls.GameEntities.SkillProjectile;
using SkiaSharp.Views.Desktop;

namespace rpg_deneme.UI.Controls;

public partial class UcArena : UserControl
{
    // Windows multimedia timer for high-resolution timing
    [DllImport("winmm.dll")]
    private static extern uint timeBeginPeriod(uint uPeriod);
    [DllImport("winmm.dll")]
    private static extern uint timeEndPeriod(uint uPeriod);

    private ArenaPanel _arena;

    // OpenGL-backed renderer
    private SKGLControl _skgl;



    // Game Loop
    private long _lastTick = 0;
    private double _accumulator = 0;
    private const double FixedTimeStep = 1.0 / 60.0; // 60 updates per second physics
    private Stopwatch _gameTimer;
    private bool _isIdleBound = false;

    // VSync P/Invoke
    [DllImport("opengl32.dll", EntryPoint = "wglGetProcAddress", ExactSpelling = true)]
    private static extern IntPtr wglGetProcAddress(string name);

    private delegate bool wglSwapIntervalEXT(int interval);
    private wglSwapIntervalEXT _wglSwapIntervalEXT;

    private Random _rnd = new Random();

    private List<HotbarSlot> _hotbar = new List<HotbarSlot>();

    private BattleEntity _player;

    private List<BattleEntity> _enemies = new List<BattleEntity>();

    private List<SkillProjectile> _projectiles = new List<SkillProjectile>();

    private List<VisualEffect> _effects = new List<VisualEffect>();

    private CharacterModel _hero;

    private List<SkillModel> _learnedSkills = new List<SkillModel>();

    private InventoryManager _invManager = new InventoryManager();

    private Button _btnMap;

    private bool _isTownMode = false;

    private List<NpcEntity> _npcs = new List<NpcEntity>();

    private bool _w;
    private bool _a;
    private bool _s;
    private bool _d;

    private Point _mousePos = Point.Empty;

    private int _attackDelayMs = 1000;

    private DateTime _lastAttackTime = DateTime.MinValue;

    private int _manaCostPerHit = 0;

    private bool _isBattleEnding = false;

    private int _enemyAttackCooldown = 0;

    private IContainer components = null;

    // Optimization & Visuals
    private Dictionary<Color, SolidBrush> _brushCache;
    private Dictionary<Color, Pen> _penCache;

    // Zero-allocation brushes/pens
    private readonly SolidBrush _brushWhite = new(Color.White);
    private readonly SolidBrush _brushBlack = new(Color.Black);
    private readonly SolidBrush _brushGold = new(Color.Gold);
    private readonly SolidBrush _brushDimGray = new(Color.DimGray);
    private readonly SolidBrush _brushDarkGray = new(Color.DarkGray);
    private readonly SolidBrush _brushOrangeRed = new(Color.OrangeRed);
    private readonly SolidBrush _brushLightCyan = new(Color.LightCyan);
    private readonly SolidBrush _brushMediumPurple = new(Color.MediumPurple);
    private readonly SolidBrush _brushDodgerBlue = new(Color.DodgerBlue);
    private readonly SolidBrush _brushLimeGreen = new(Color.LimeGreen);

    private readonly Pen _penBlack1 = new(Color.Black, 1f);
    private readonly Pen _penDimGray1 = new(Color.DimGray, 1f);
    private readonly Pen _penGray1 = new(Color.Gray, 1f);
    private readonly Pen _penWhite1 = new(Color.White, 1f);
    private readonly Pen _penGold2 = new(Color.Gold, 2f);

    private int _currentWeaponUpgradeLevel = 0;
    private Enums.ItemGrade _currentWeaponGrade = Enums.ItemGrade.Common;

    // Animation Counter
    private int _animCounter = 0;

    // Cached Fonts
    private readonly Font _damageFont = new Font("Segoe UI", 11f, FontStyle.Bold);
    private readonly Font _critDamageFont = new Font("Segoe UI", 14f, FontStyle.Bold);
    private readonly Font _enemyNameFont = new Font("Segoe UI", 7f, FontStyle.Regular);
    private readonly StringFormat _centerFormat = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

    // Mana Regen
    private int _manaRegenAmount = 0;
    private int _manaRegenTimer = 0;

    // Equipment Cache
    private List<ItemInstance> _cachedEquipment;
    private long _lastEquipFetchTicks;
    private readonly Stopwatch _cacheWatch = Stopwatch.StartNew();

    // Results UI
    private bool _showResults = false;
    private string _resultMessage = "";

    private Action _onPrimary = null;
    private string _primaryBtnText = "DEVAM ET";
    private Rectangle _primaryBtnRect;

    private Action _onSecondary = null;
    private string _secondaryBtnText = "";
    private Rectangle _secondaryBtnRect;

    // Tooltip State
    private int _lastHoveredBuffIndex = -1;
    private int _lastHoveredHotbarIndex = -1;

    public event EventHandler OnMapRequested;
    public event EventHandler<bool> OnBattleEnded;
    public event EventHandler OnStatsUpdated;
    public event EventHandler<NpcEntity> OnNpcInteraction;

    // Enemy neighbor acceleration
    private readonly EnemySpatialGrid _enemyGrid = new EnemySpatialGrid();

    // Game loop state
    private bool _loopRunning;
    private bool _timerResolutionSet;

    public UcArena()
    {
        DoubleBuffered = true;
        _brushCache = new Dictionary<Color, SolidBrush>();
        _penCache = new Dictionary<Color, Pen>();
        BackColor = Color.Black;

        // Set high-resolution timer (1ms instead of 15.6ms default)
        try
        {
            timeBeginPeriod(1);
            _timerResolutionSet = true;
        }
        catch { _timerResolutionSet = false; }

        SetupArena();
    }

    private void SetupArena()
    {
        _arena = new ArenaPanel();
        _arena.Dock = DockStyle.Fill;
        _arena.BackColor = Color.FromArgb(45, 45, 48);

        _skgl = new SKGLControl
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(45, 45, 48),
            // Disable VSync for higher FPS
            VSync = false
        };
        _skgl.PaintSurface += Arena_SKPaintSurface;

        _skgl.MouseDown += Arena_MouseDown;
        _skgl.MouseMove += delegate (object? s, MouseEventArgs e)
        {
            _mousePos = e.Location;
            UpdateTooltips(e.Location);
        };
        _skgl.AllowDrop = true;
        _skgl.DragEnter += Arena_DragEnter;
        _skgl.DragDrop += Arena_DragDrop;

        // Attempt to force SwapInterval(0)
        _skgl.HandleCreated += (s, e) =>
        {
            _skgl.MakeCurrent();
            SetVSync(false);
        };

        _arena.Controls.Add(_skgl);
        base.Controls.Add(_arena);

        base.Click += delegate { Focus(); };

        for (int i = 0; i < 5; i++)
        {
            _hotbar.Add(new HotbarSlot { Index = i });
        }
    }

    public (int, int) GetArenaSize()
    {
        if (_skgl == null) return (0, 0);
        return (_skgl.Width, _skgl.Height);
    }

    private Rectangle GetHotbarSlotRect(int index)
    {
        var (w, h) = GetArenaSize();
        int slotSize = 50;
        int gap = 10;
        int totalW = (5 * slotSize) + (4 * gap);
        int startX = (w - totalW) / 2;
        int y = h - slotSize - 20;
        return new Rectangle(startX + (index * (slotSize + gap)), y, slotSize, slotSize);
    }

    private void SetVSync(bool enable)
    {
        try
        {
            IntPtr addr = wglGetProcAddress("wglSwapIntervalEXT");
            if (addr != IntPtr.Zero)
            {
                _wglSwapIntervalEXT = Marshal.GetDelegateForFunctionPointer<wglSwapIntervalEXT>(addr);
                _wglSwapIntervalEXT(enable ? 1 : 0);
            }
        }
        catch { }
    }

    private static float Lerp(float a, float b, float t) => a + (b - a) * t;

    private float Distance(float x1, float y1, float x2, float y2)
    {
        float dx = x2 - x1;
        float dy = y2 - y1;
        return MathF.Sqrt(dx * dx + dy * dy);
    }

    private float DistanceSquared(float x1, float y1, float x2, float y2)
    {
        float dx = x2 - x1;
        float dy = y2 - y1;
        return dx * dx + dy * dy;
    }

    private void ClampEntityPosition(BattleEntity entity)
    {
        if (entity == null) return;
        var (w, h) = GetArenaSize();
        if (entity.X < 0f) entity.X = 0f;
        if (entity.X > w - entity.Width) entity.X = w - entity.Width;
        if (entity.Y < 0f) entity.Y = 0f;
        if (entity.Y > h - entity.Height) entity.Y = h - entity.Height;
    }

    private List<ItemInstance> GetEquipment()
    {
        if (_hero == null) return _cachedEquipment ?? new List<ItemInstance>();

        long nowTicks = _cacheWatch.ElapsedTicks;
        // Cache for 2 seconds
        if (_cachedEquipment == null || (nowTicks - _lastEquipFetchTicks) > Stopwatch.Frequency * 2)
        {
            var inv = _invManager.GetInventory(_hero.CharacterID);
            _cachedEquipment = inv.FindAll(x => x.Location == Enums.ItemLocation.Equipment);
            _lastEquipFetchTicks = nowTicks;
        }
        return _cachedEquipment;
    }

    private void InvalidateEquipmentCache()
    {
        _cachedEquipment = null;
        _lastEquipFetchTicks = 0;
    }

    protected override void OnParentChanged(EventArgs e)
    {
        base.OnParentChanged(e);
        if (Parent == null)
        {
            StopGameLoop();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            StopGameLoop();

            // Restore default timer resolution
            if (_timerResolutionSet)
            {
                try { timeEndPeriod(1); } catch { }
            }

            if (components != null) components.Dispose();

            foreach (var b in _brushCache.Values) b.Dispose();
            foreach (var p in _penCache.Values) p.Dispose();
            _brushCache.Clear();
            _penCache.Clear();

            _brushWhite.Dispose();
            _brushBlack.Dispose();
            _brushGold.Dispose();
            _brushDimGray.Dispose();
            _brushDarkGray.Dispose();
            _brushOrangeRed.Dispose();
            _brushLightCyan.Dispose();
            _brushMediumPurple.Dispose();
            _brushDodgerBlue.Dispose();
            _brushLimeGreen.Dispose();

            _penBlack1.Dispose();
            _penDimGray1.Dispose();
            _penGray1.Dispose();
            _penWhite1.Dispose();
            _penGold2.Dispose();

            _enemyNameFont?.Dispose();
            _centerFormat?.Dispose();
            _damageFont?.Dispose();
            _critDamageFont?.Dispose();

            DisposeSkiaCaches();



            if (_skgl != null)
            {
                _skgl.PaintSurface -= Arena_SKPaintSurface;
                _skgl.Dispose();
                _skgl = null;
            }
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        this.components = new System.ComponentModel.Container();
        base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
    }
}
