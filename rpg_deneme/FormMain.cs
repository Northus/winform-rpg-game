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
using rpg_deneme.UI;
using rpg_deneme.UI.Controls;
using rpg_deneme.UI.Windows;

namespace rpg_deneme;

/// <summary>
/// Oyunun ana arayüz ekranı.
/// </summary>
public class FormMain : Form
{
    private CharacterModel _hero;
    private InventoryManager _invManager = new InventoryManager();
    private UcArena _townArena;
    private UcMerchant _ucMerchant;
    private UcStorage _ucStorage;
    private UcBlacksmith _ucBlacksmith;
    private UcSurvivalLobby _ucSurvival;
    private FormSkills _formSkills; // Changed to Form (GameWindow) but logic similar
    private int _currentSurvivalWave;
    private UserControl _currentScreen;
    private IContainer components = null;



    private bool _isReturningToCharSelect = false;

    private Panel panel1;
    private Button btnInventory;
    private Button btnStats;
    private UcStats ucStats1;
    private Label lblCharName;
    private UcInventory ucInventory1;
    private Panel pnlMainContent;
    private Label lblLevel;
    private GameProgressBar pbMana;
    private GameProgressBar pbHP;
    private Button btnSkills;
    private Button btnMenu;
    private GameProgressBar pbExp;

    /// <summary>
    /// FormMain yapıcı metodu.
    /// </summary>
    public FormMain()
    {
        InitializeComponent();
        base.KeyPreview = true;
        base.FormClosing += FormMain_FormClosing;
    }

    /// <summary>
    /// Form yüklendiğinde karakter verilerini çeker ve arayüzü hazırlar.
    /// </summary>
    private void FormMain_Load(object sender, EventArgs e)
    {
        _hero = SessionManager.CurrentCharacter;
        if (_hero != null)
        {
            ucInventory1.Visible = false;
            ucStats1.Visible = false;
            ucInventory1.OnSellRequest += delegate (object? s, ItemInstance item)
            {
                SellItemProcess(item);
            };
            UpdateUI();

            // Load Hotbar from DB
            HotbarRepository hotbarRepo = new HotbarRepository();
            var savedSlots = hotbarRepo.LoadHotbar(_hero.CharacterID);
            for (int i = 0; i < 5; i++)
            {
                if (savedSlots.ContainsKey(i))
                    SessionManager.HotbarSlots[i] = savedSlots[i];
                else
                    SessionManager.HotbarSlots[i] = null;
            }

            ShowTown();
            btnMenu.Click += (s, args) => ShowMenu();
        }
    }

    /// <summary>
    /// Form kapanırken gerekli temizlik işlemlerini yapar.
    /// </summary>
    private void FormMain_FormClosing(object? sender, FormClosingEventArgs e)
    {
        try
        {
            if (_ucBlacksmith != null && !_ucBlacksmith.IsDisposed)
            {
                _ucBlacksmith.FlushSlotsToInventory();
            }
            if (_ucStorage != null && !_ucStorage.IsDisposed)
            {
                RefreshStats();
            }
            if (_hero != null)
            {
                CharacterRepository repo = new CharacterRepository();
                repo.UpdateProgress(_hero);
            }
        }
        catch { }

        if (_formSkills != null && !_formSkills.IsDisposed) _formSkills.Dispose();

        if (!_isReturningToCharSelect)
        {
            Application.Exit();
        }
    }

    /// <summary>
    /// Ana içerik panelindeki ekranı değiştirir.
    /// </summary>
    private void SwitchScreen(UserControl newScreen)
    {
        pnlMainContent.Controls.Clear();
        _currentScreen = newScreen;
        newScreen.Dock = DockStyle.Fill;
        pnlMainContent.Controls.Add(newScreen);
        ucInventory1.IsMerchantMode = false;
    }

    /// <summary>
    /// Bölge haritasını görüntüler.
    /// </summary>
    public void ShowZoneMap()
    {
        UcZoneMap mapScreen = new UcZoneMap();
        mapScreen.OnZoneSelected += delegate (object? s, ZoneModel zone)
        {
            ShowExploration(zone);
        };
        mapScreen.OnTownRequested += delegate
        {
            ShowTown();
        };
        SwitchScreen(mapScreen);
    }

    /// <summary>
    /// Kasaba (Town) ekranını görüntüler.
    /// </summary>
    public void ShowTown()
    {
        if (_townArena == null)
        {
            _townArena = new UcArena();
            _townArena.Dock = DockStyle.Fill;
            _townArena.OnStatsUpdated += delegate { RefreshStats(); };
            _townArena.OnNpcInteraction += delegate (object? s, NpcEntity npc)
            {
                if (npc.Name == "DEPOCU")
                {
                    ShowOnlyInventoryAnd(_ucStorage);
                    if (_ucStorage == null || _ucStorage.IsDisposed)
                    {
                        _ucStorage = new UcStorage();
                        _ucStorage.OnWithdrawRequest += delegate (object? sender, ItemInstance item)
                        {
                            int num = _invManager.FindFirstEmptyInventorySlot(_hero.CharacterID);
                            if (num != -1)
                            {
                                _invManager.TransferItem(_hero, item, Enums.ItemLocation.Inventory, num);
                                RefreshStats();
                            }
                            else { MessageBox.Show("Çantan dolu!"); }
                        };
                        pnlMainContent.Controls.Add(_ucStorage);
                    }
                    else if (!pnlMainContent.Controls.Contains(_ucStorage))
                    {
                        pnlMainContent.Controls.Add(_ucStorage);
                    }
                    ucInventory1.Visible = true;
                    _ucStorage.LoadStorage();
                    _ucStorage.Height = ucInventory1.Height;
                    _ucStorage.Location = new Point(ucInventory1.Right + 5, ucInventory1.Top);
                    _ucStorage.Visible = true;
                    _ucStorage.BringToFront();
                    ucInventory1.BringToFront();
                }
                else if (npc.Type == Enums.NpcType.Teleporter)
                {
                    ShowZoneMap();
                }
                else if (npc.Name == "DEMİRCİ")
                {
                    ShowOnlyInventoryAnd(_ucBlacksmith);
                    if (_ucBlacksmith == null || _ucBlacksmith.IsDisposed)
                    {
                        _ucBlacksmith = new UcBlacksmith();
                        pnlMainContent.Controls.Add(_ucBlacksmith);
                    }
                    else if (!pnlMainContent.Controls.Contains(_ucBlacksmith))
                    {
                        pnlMainContent.Controls.Add(_ucBlacksmith);
                    }
                    ucInventory1.Visible = true;
                    _ucBlacksmith.Height = ucInventory1.Height;
                    _ucBlacksmith.Location = new Point(ucInventory1.Right + 5, ucInventory1.Top);
                    _ucBlacksmith.Visible = true;
                    _ucBlacksmith.BringToFront();
                    ucInventory1.BringToFront();
                }
                else if (npc.Type == Enums.NpcType.Merchant)
                {
                    ShowOnlyInventoryAnd(_ucMerchant);
                    if (_ucMerchant == null || _ucMerchant.IsDisposed)
                    {
                        int shopId = 1;
                        _ucMerchant = new UcMerchant(shopId);
                        _ucMerchant.OnInventoryUpdateNeeded += delegate { RefreshStats(); };
                        _ucMerchant.OnSellRequested += delegate (object? sender, ItemInstance item) { SellItemProcess(item); };
                        _ucMerchant.VisibleChanged += delegate { ucInventory1.IsMerchantMode = _ucMerchant.Visible; };
                        pnlMainContent.Controls.Add(_ucMerchant);
                    }
                    else if (!pnlMainContent.Controls.Contains(_ucMerchant))
                    {
                        pnlMainContent.Controls.Add(_ucMerchant);
                    }
                    ucInventory1.Visible = true;
                    _ucMerchant.Height = ucInventory1.Height;
                    _ucMerchant.Location = new Point(ucInventory1.Right + 5, ucInventory1.Top);
                    _ucMerchant.Visible = true;
                    _ucMerchant.BringToFront();
                    ucInventory1.BringToFront();
                }
                else if (npc.Name == "ARENA GUARD")
                {
                    if (_ucSurvival != null && _ucSurvival.Visible) { _ucSurvival.Visible = false; }
                    else
                    {
                        ShowOnlyInventoryAnd(_ucSurvival);
                        if (_ucSurvival == null || _ucSurvival.IsDisposed)
                        {
                            _ucSurvival = new UcSurvivalLobby();
                            _ucSurvival.OnStartRequested += StartSurvivalMode;
                            pnlMainContent.Controls.Add(_ucSurvival);
                        }
                        else if (!pnlMainContent.Controls.Contains(_ucSurvival))
                        {
                            pnlMainContent.Controls.Add(_ucSurvival);
                        }
                        ucInventory1.Visible = true;
                        _ucSurvival.Location = new Point(ucInventory1.Right + 5, ucInventory1.Top);
                        _ucSurvival.LoadData();
                        _ucSurvival.Visible = true;
                        _ucSurvival.BringToFront();
                        ucInventory1.BringToFront();
                    }
                }
            };
        }
        SwitchScreen(_townArena);
        _townArena.StartTown(_hero);
        RefreshStats();
    }

    /// <summary>
    /// Eşya satma işlemini gerçekleştirir.
    /// </summary>
    public void SellItemProcess(ItemInstance item)
    {
        ShopManager shopManager = new ShopManager();
        int qty = item.Count;
        if (item.IsStackable && item.Count > 1)
        {
            using QuantityDialog qd = new QuantityDialog(item.Count);
            if (qd.ShowDialog() != DialogResult.OK) { return; }
            qty = qd.SelectedQuantity;
        }
        int unitSellPrice = shopManager.GetSellPrice(item.TemplateID);
        if (unitSellPrice <= 0) { unitSellPrice = 1; }
        long totalEarn = (long)unitSellPrice * (long)qty;
        DialogResult confirm = MessageBox.Show($"{qty} adet {item.Name} satılacak.\nKazanılacak: {totalEarn} Altın\nOnaylıyor musun?", "Satış", MessageBoxButtons.YesNo);
        if (confirm == DialogResult.Yes)
        {
            (bool, string) result = shopManager.SellItem(_hero, item, qty);
            if (result.Item1)
            {
                RefreshStats();
                MessageBox.Show(result.Item2);
            }
        }
    }

    /// <summary>
    /// Keşif ekranını görüntüler.
    /// </summary>
    public void ShowExploration(ZoneModel zone)
    {
        UcExploration exploreScreen = new UcExploration(zone);
        exploreScreen.OnReturnRequested += delegate { ShowZoneMap(); };
        SwitchScreen(exploreScreen);
    }

    /// <summary>
    /// Tuş basımlarını yakalar ve ilgili ekranlara iletir.
    /// </summary>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape)
        {
            if (_ucBlacksmith != null && _ucBlacksmith.Visible) { _ucBlacksmith.CloseWindow(); }
            else if (_ucMerchant != null && _ucMerchant.Visible) { _ucMerchant.CloseWindow(); }
            else if (_ucStorage != null && _ucStorage.Visible) { _ucStorage.CloseWindow(); }
            else if (_ucSurvival != null && _ucSurvival.Visible) { _ucSurvival.CloseWindow(); }
            else if (ucStats1 != null && ucStats1.Visible) { ucStats1.Visible = false; ucStats1.SendToBack(); }
            else if (ucInventory1 != null && ucInventory1.Visible) { ucInventory1.Visible = false; ucInventory1.SendToBack(); }
            e.Handled = true;
            return;
        }
        if (_currentScreen is UcExploration explore) { explore.RelayKeyDown(e.KeyCode); }
        else if (_currentScreen is UcArena arena)
        {
            arena.HandleKeyDown(e.KeyCode);
            if ((e.KeyCode >= Keys.D1 && e.KeyCode <= Keys.D5) || (e.KeyCode >= Keys.NumPad1 && e.KeyCode <= Keys.NumPad5))
            {
                arena.HandleHotbarKey(e.KeyCode);
                e.Handled = true;
            }
        }
        base.OnKeyDown(e);
    }

    /// <summary>
    /// Tuş bırakma olayını yakalar ve ilgili ekranlara iletir.
    /// </summary>
    protected override void OnKeyUp(KeyEventArgs e)
    {
        if (_currentScreen is UcExploration explore) { explore.RelayKeyUp(e.KeyCode); }
        else if (_currentScreen is UcArena arena) { arena.HandleKeyUp(e.KeyCode); }
        base.OnKeyUp(e);
    }

    /// <summary>
    /// Envanter butonu tıklama olayı.
    /// </summary>
    private void btnInventory_Click(object sender, EventArgs e)
    {
        ucInventory1.Visible = !ucInventory1.Visible;
        if (ucInventory1.Visible) { ucInventory1.BringToFront(); }
    }

    /// <summary>
    /// İstatistik butonu tıklama olayı.
    /// </summary>
    private void btnStats_Click(object sender, EventArgs e)
    {
        ucStats1.Visible = !ucStats1.Visible;
        if (ucStats1.Visible) { ucStats1.BringToFront(); ucStats1.LoadData(); }
    }

    /// <summary>
    /// Yetenekler butonu tıklama olayı.
    /// </summary>
    private void btnSkills_Click(object sender, EventArgs e)
    {
        if (_formSkills == null || _formSkills.IsDisposed)
        {
            _formSkills = new FormSkills();
            _formSkills.Location = new Point((this.Width - _formSkills.Width) / 2, 50); // Fixed Top Position
            _formSkills.OnSkillChanged += delegate { RefreshStats(); };
            this.Controls.Add(_formSkills);
            _formSkills.BringToFront();
            _formSkills.Show();
        }
        else
        {
            if (_formSkills.Visible)
            {
                _formSkills.Hide();
            }
            else
            {
                _formSkills.Show();
                _formSkills.BringToFront();
            }
        }
    }

    /// <summary>
    /// Tüm kullanıcı arayüzünü günceller.
    /// </summary>
    public void UpdateUI()
    {
        lblCharName.Text = _hero.Name;
        List<ItemInstance> myItems = _invManager.GetInventory(_hero.CharacterID);
        ucInventory1.LoadItems(myItems);
        if (ucStats1.Visible) { ucStats1.LoadData(); }
        UpdateBars();
    }

    /// <summary>
    /// Karakter verilerini yeniler ve arayüzü günceller.
    /// </summary>
    public void RefreshStats()
    {
        _hero = SessionManager.CurrentCharacter;
        if (_hero != null)
        {
            // Retroactive Skill Point Fix for existing characters
            if (_hero.Level > 1)
            {
                SkillManager sm = new SkillManager();
                var learnedSkills = sm.LoadSkillsForClass((Enums.CharacterClass)_hero.Class, _hero.CharacterID);
                int spentPoints = learnedSkills.Sum(s => s.CurrentLevel);
                // 6 Starting + 3 per Level (Level 1 has 6, Level 2 has 9...)
                // Formula: 6 + (Level - 1) * 3
                int expectedPoints = 6 + ((_hero.Level - 1) * 3);

                if (spentPoints + _hero.SkillPoints < expectedPoints)
                {
                    int diff = expectedPoints - (spentPoints + _hero.SkillPoints);
                    _hero.SkillPoints += diff;
                    CharacterRepository repo = new CharacterRepository();
                    repo.UpdateCharacterStats(_hero);
                    NotificationManager.AddNotification($"{diff} Yetenek Puanı telafi edildi.", Color.Green);
                }
            }

            List<ItemInstance> allItems = _invManager.GetInventory(_hero.CharacterID);
            if (ucStats1.Visible) { ucStats1.LoadData(); }
            ucInventory1.LoadItems(allItems);
            ucInventory1.UpdateGoldLabel();
            UpdateBars();
            if (_ucStorage != null && _ucStorage.Visible) { _ucStorage.LoadStorage(); }
            if (_ucBlacksmith != null && _ucBlacksmith.Visible) { _ucBlacksmith.RefreshFromDb(); }
            if (_currentScreen is UcExploration exploreScreen) { exploreScreen.RefreshBattleStats(); }
            else if (_currentScreen is UcArena townArena) { townArena.RefreshGameState(_hero); }
        }
    }

    /// <summary>
    /// HP, Mana ve Tecrübe çubuklarını günceller.
    /// </summary>
    public void UpdateBars(List<ItemInstance> equipment = null)
    {
        if (_hero != null)
        {
            if (equipment == null)
            {
                equipment = _invManager.GetInventory(_hero.CharacterID)
                                       .Where(x => x.Location == Enums.ItemLocation.Equipment)
                                       .ToList();
            }



            // Load Skills for Passives
            SkillManager skillMgr = new SkillManager();
            var skills = skillMgr.LoadSkillsForClass((Enums.CharacterClass)_hero.Class, _hero.CharacterID);

            int maxHP = StatManager.CalculateTotalMaxHP(_hero, equipment, skills);
            int maxMana = StatManager.CalculateTotalMaxMana(_hero, equipment, skills);
            int requiredExp = LevelManager.GetRequiredXp(_hero.Level);
            if (_hero.HP > maxHP) { _hero.HP = maxHP; }
            pbHP.Maximum = maxHP;
            pbHP.Value = Math.Max(0, _hero.HP);
            pbHP.BarColor = Color.Crimson;
            if (_hero.Mana > maxMana) { _hero.Mana = maxMana; }
            pbMana.Maximum = maxMana;
            pbMana.Value = Math.Max(0, _hero.Mana);
            pbMana.BarColor = Color.DodgerBlue;
            pbExp.Maximum = requiredExp;
            pbExp.Value = Math.Min((long)requiredExp, Math.Max(0L, _hero.Experience));
            pbExp.BarColor = Color.Gold;
            pbExp.ShowPercentage = true;
            if (lblLevel != null) { lblLevel.Text = $"Level {_hero.Level}"; }
        }
    }

    /// <summary>
    /// Sadece envanteri ve belirtilen yan pencereyi gösterir.
    /// </summary>
    private void ShowOnlyInventoryAnd(Control sideWindow)
    {
        ucInventory1.Visible = true;
        ucInventory1.BringToFront();
        Control[] sideWindows = new Control[] { _ucMerchant, _ucStorage, _ucBlacksmith, _ucSurvival };
        foreach (Control w in sideWindows)
        {
            if (w == null) continue;
            if (w == sideWindow)
            {
                if (!pnlMainContent.Controls.Contains(w)) { pnlMainContent.Controls.Add(w); }
                w.Visible = true;
                w.BringToFront();
            }
            else { w.Visible = false; }
        }
    }

    /// <summary>
    /// Sadece envanterdeki eşyaları ve altın bilgisini tazeler.
    /// </summary>
    public void RefreshInventoryOnly()
    {
        _hero = SessionManager.CurrentCharacter;
        if (_hero != null)
        {
            List<ItemInstance> myItems = _invManager.GetInventory(_hero.CharacterID);
            ucInventory1.LoadItems(myItems);
            ucInventory1.UpdateGoldLabel();
        }
    }

    /// <summary>
    /// Hayatta kalma modunu başlatır.
    /// </summary>
    public void StartSurvivalMode(object sender, int waveIndex)
    {
        _currentSurvivalWave = waveIndex;
        if (_ucSurvival != null) { _ucSurvival.Visible = false; }
        SurvivalManager logic = new SurvivalManager();
        List<EnemyModel> enemies = logic.GenerateWaveEnemies(waveIndex);
        if (_townArena == null)
        {
            _townArena = new UcArena();
            _townArena.Dock = DockStyle.Fill;
            _townArena.OnStatsUpdated += delegate { RefreshStats(); };
        }
        SwitchScreen(_townArena);
        _townArena.StartSurvivalBattle(_hero, enemies);
        _townArena.OnBattleEnded -= SurvivalBattleEnded;
        _townArena.OnBattleEnded += SurvivalBattleEnded;
    }

    /// <summary>
    /// Hayatta kalma savaşı bittiğinde tetiklenir.
    /// </summary>
    private void SurvivalBattleEnded(object sender, bool victory)
    {
        if (victory)
        {
            SurvivalManager logic = new SurvivalManager();
            this.Invoke((MethodInvoker)delegate
            {
                bool isFirstTime = _currentSurvivalWave == _hero.MaxSurvivalWave;
                int reward = logic.CalculateReward(_currentSurvivalWave, isFirstTime);
                _hero.Gold += reward;
                logic.CompleteWave(_hero, _currentSurvivalWave);
                RefreshStats();

                _townArena.ShowBattleResults(
                    $"TEBRİKLER! Dalga {_currentSurvivalWave} Tamamlandı!\nKazanılan: {reward} Gold.",
                    () => { StartSurvivalMode(this, _currentSurvivalWave + 1); },
                    "SONRAKİ DALGA",
                    () => { ShowTown(); },
                    "ŞEHRE DÖN"
                );
            });
        }
        else
        {
            // Revive - ekipman bonuslarını dahil ederek MaxHP hesapla
            InventoryManager invManager = new InventoryManager();
            var equipment = invManager.GetInventory(_hero.CharacterID)
                .Where(x => x.Location == Enums.ItemLocation.Equipment).ToList();
            int maxHP = StatManager.CalculateTotalMaxHP(_hero, equipment);
            _hero.HP = maxHP;
            CharacterRepository charRepo = new CharacterRepository();
            charRepo.UpdateProgress(_hero);
            SessionManager.CurrentCharacter.HP = _hero.HP;

            this.Invoke((MethodInvoker)delegate
            {
                _townArena.ShowBattleResults(
                   "Maalesef kaybettin...",
                   () => { StartSurvivalMode(this, _currentSurvivalWave); },
                   "TEKRAR DENE",
                   () => { ShowTown(); },
                   "ŞEHRE DÖN"
               );
            });
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && components != null) { components.Dispose(); }
        base.Dispose(disposing);
    }

    private void ShowMenu()
    {
        // 1. Create Dimmer Form
        Form dimmer = new Form();
        dimmer.FormBorderStyle = FormBorderStyle.None;
        dimmer.BackColor = Color.Black;
        dimmer.Opacity = 0.5; // Adjust darkness here
        dimmer.ShowInTaskbar = false;
        dimmer.StartPosition = FormStartPosition.Manual;
        dimmer.Location = this.PointToScreen(Point.Empty);
        dimmer.Size = this.Size;
        dimmer.Owner = this;
        dimmer.Show();

        // 2. Show Menu Dialog
        using (FormGameMenu menu = new FormGameMenu())
        {
            menu.StartPosition = FormStartPosition.CenterScreen; // Center on screen usually works best combined with parent context, but CenterParent is safer if parent set.
                                                                 // However, CenterParent centers on the 'dimmer' if passed to ShowDialog.

            menu.ShowDialog(dimmer); // Modal to dimmer

            // 3. Handle Result
            if (menu.SelectedAction == FormGameMenu.MenuAction.CharSelect)
            {
                ReturnToCharSelect();
            }
            else if (menu.SelectedAction == FormGameMenu.MenuAction.Exit)
            {
                Application.Exit();
            }
        }

        // 4. Cleanup
        dimmer.Close();
        dimmer.Dispose();
    }

    private void ReturnToCharSelect()
    {
        if (MessageBox.Show("Karakter seçim ekranına dönmek istediğine emin misin?", "Onay", MessageBoxButtons.YesNo) == DialogResult.Yes)
        {
            _isReturningToCharSelect = true;

            // Find existing hidden FormCharSelect if any
            FormCharSelect existing = null;
            foreach (Form f in Application.OpenForms)
            {
                if (f is FormCharSelect)
                {
                    existing = (FormCharSelect)f;
                    break;
                }
            }

            if (existing != null)
            {
                existing.Show();
                existing.LoadCharactersToSlots(); // Refresh slots
            }
            else
            {
                FormCharSelect frm = new FormCharSelect();
                frm.Show();
            }

            this.Close();
        }
    }

    private void InitializeComponent()
    {
        panel1 = new Panel();
        btnSkills = new Button();
        pbMana = new GameProgressBar();
        pbHP = new GameProgressBar();
        pbExp = new GameProgressBar();
        lblLevel = new Label();
        lblCharName = new Label();
        btnStats = new Button();
        btnInventory = new Button();
        ucStats1 = new UcStats();
        ucInventory1 = new UcInventory();
        pnlMainContent = new Panel();
        btnMenu = new Button();
        panel1.SuspendLayout();
        SuspendLayout();
        // 
        // panel1
        // 
        panel1.BackColor = Color.DimGray;
        panel1.Controls.Add(btnMenu);
        panel1.Controls.Add(btnSkills);
        panel1.Controls.Add(pbMana);
        panel1.Controls.Add(pbHP);
        panel1.Controls.Add(pbExp);
        panel1.Controls.Add(lblLevel);
        panel1.Controls.Add(lblCharName);
        panel1.Controls.Add(btnStats);
        panel1.Controls.Add(btnInventory);
        panel1.Dock = DockStyle.Bottom;
        panel1.Location = new Point(0, 611);
        panel1.Name = "panel1";
        panel1.Size = new Size(1008, 70);
        panel1.TabIndex = 0;
        // 
        // btnSkills
        // 
        btnSkills.Location = new Point(286, 26);
        btnSkills.Name = "btnSkills";
        btnSkills.Size = new Size(75, 23);
        btnSkills.TabIndex = 10;
        btnSkills.Text = "Skills";
        btnSkills.UseVisualStyleBackColor = true;
        btnSkills.Click += btnSkills_Click;
        // 
        // pbMana
        // 
        pbMana.BarColor = Color.Red;
        pbMana.Location = new Point(874, 26);
        pbMana.Maximum = 100;
        pbMana.Name = "pbMana";
        pbMana.ShowPercentage = false;
        pbMana.Size = new Size(125, 25);
        pbMana.TabIndex = 9;
        pbMana.Text = "gameProgressBar3";
        pbMana.Value = 0L;
        // 
        // pbHP
        // 
        pbHP.BarColor = Color.Red;
        pbHP.Location = new Point(743, 26);
        pbHP.Maximum = 100;
        pbHP.Name = "pbHP";
        pbHP.ShowPercentage = false;
        pbHP.Size = new Size(125, 25);
        pbHP.TabIndex = 8;
        pbHP.Text = "gameProgressBar2";
        pbHP.Value = 0L;
        // 
        // pbExp
        // 
        pbExp.BarColor = Color.Red;
        pbExp.Location = new Point(612, 26);
        pbExp.Maximum = 100;
        pbExp.Name = "pbExp";
        pbExp.ShowPercentage = false;
        pbExp.Size = new Size(125, 25);
        pbExp.TabIndex = 7;
        pbExp.Text = "gameProgressBar1";
        pbExp.Value = 0L;
        // 
        // lblLevel
        // 
        lblLevel.AutoSize = true;
        lblLevel.Location = new Point(560, 30);
        lblLevel.Name = "lblLevel";
        lblLevel.Size = new Size(34, 15);
        lblLevel.TabIndex = 6;
        lblLevel.Text = "Level";
        // 
        // lblCharName
        // 
        lblCharName.AutoSize = true;
        lblCharName.Location = new Point(43, 30);
        lblCharName.Name = "lblCharName";
        lblCharName.Size = new Size(38, 15);
        lblCharName.TabIndex = 2;
        lblCharName.Text = "label1";
        // 
        // btnStats
        // 
        btnStats.Location = new Point(205, 26);
        btnStats.Name = "btnStats";
        btnStats.Size = new Size(75, 23);
        btnStats.TabIndex = 1;
        btnStats.Text = "Stats";
        btnStats.UseVisualStyleBackColor = true;
        btnStats.Click += btnStats_Click;
        // 
        // btnInventory
        // 
        btnInventory.Location = new Point(124, 26);
        btnInventory.Name = "btnInventory";
        btnInventory.Size = new Size(75, 23);
        btnInventory.TabIndex = 0;
        btnInventory.Text = "Inventory";
        btnInventory.UseVisualStyleBackColor = true;
        btnInventory.Click += btnInventory_Click;
        // 
        // ucStats1
        // 
        ucStats1.BackColor = Color.FromArgb(45, 45, 48);
        ucStats1.BorderStyle = BorderStyle.FixedSingle;
        ucStats1.Location = new Point(733, 205);
        ucStats1.Name = "ucStats1";
        ucStats1.Size = new Size(250, 400);
        ucStats1.TabIndex = 2;
        ucStats1.Title = "STATS";
        ucStats1.Visible = false;
        // 
        // ucInventory1
        // 
        ucInventory1.BackColor = Color.FromArgb(45, 45, 48);
        ucInventory1.BorderStyle = BorderStyle.FixedSingle;
        ucInventory1.IsMerchantMode = false;
        ucInventory1.Location = new Point(30, 240);
        ucInventory1.Name = "ucInventory1";
        ucInventory1.Size = new Size(376, 365);
        ucInventory1.TabIndex = 1;
        ucInventory1.Title = "ENVANTER";
        ucInventory1.Visible = false;
        // 
        // pnlMainContent
        // 
        pnlMainContent.Dock = DockStyle.Fill;
        pnlMainContent.Location = new Point(0, 0);
        pnlMainContent.Name = "pnlMainContent";
        pnlMainContent.Size = new Size(1008, 611);
        pnlMainContent.TabIndex = 3;
        // 
        // btnMenu
        // 
        btnMenu.Location = new Point(12, 26);
        btnMenu.Name = "btnMenu";
        btnMenu.Size = new Size(23, 23);
        btnMenu.TabIndex = 11;
        btnMenu.Text = "*";
        btnMenu.UseVisualStyleBackColor = true;
        // 
        // FormMain
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1008, 681);
        Controls.Add(pnlMainContent);
        Controls.Add(ucStats1);
        Controls.Add(ucInventory1);
        Controls.Add(panel1);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        Name = "FormMain";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "RPG Deneme";
        Load += FormMain_Load;
        panel1.ResumeLayout(false);
        panel1.PerformLayout();
        ResumeLayout(false);
    }
}