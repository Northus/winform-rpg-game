using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using rpg_deneme.Business;
using rpg_deneme.Core;
using rpg_deneme.Data;
using rpg_deneme.Models;
using rpg_deneme.UI.Controls;

namespace rpg_deneme.UI.Windows;

public class UcExploration : UserControl
{
    private class ComboBoxItem
    {
        public string Text { get; set; }

        public Enums.ZoneDifficulty Value { get; set; }

        public override string ToString()
        {
            return Text;
        }
    }

    private ZoneModel _currentZone;

    private Label lblTitle;

    private Label lblInfo;

    private Button btnExplore;

    private UcArena ucArena;

    private Panel pnlInfo;

    private ComboBox cmbDifficulty;

    private Label lblDifficultyInfo;

    private ZoneManager _zoneManager = new ZoneManager();

    private LootManager _lootManager = new LootManager();

    private LevelManager _levelManager = new LevelManager();

    private CharacterRepository _charRepo = new CharacterRepository();

    private Button btnBack;

    private bool _isBossFight = false;

    private bool _bossReadyToSpawn = false;

    private Enums.ZoneDifficulty _selectedDifficulty = Enums.ZoneDifficulty.Easy;

    private List<int> _currentEnemyIDs = new List<int>();

    private IContainer components = null;

    public GameProgressBar pbZoneProgress { get; private set; }

    public event EventHandler OnReturnRequested;

    public UcExploration(ZoneModel zone)
    {
        InitializeComponent();
        _currentZone = zone;
        Dock = DockStyle.Fill;
        BackColor = Color.FromArgb(40, 40, 40);
        SetupUI();
    }

    private void SetupUI()
    {
        base.Controls.Clear();
        pnlInfo = new Panel
        {
            Dock = DockStyle.Top,
            Height = 350,
            BackColor = Color.Transparent
        };
        CharacterModel hero = SessionManager.CurrentCharacter;
        lblTitle = new Label
        {
            Text = _currentZone.Name,
            Font = new Font("Segoe UI", 24f, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(20, 20),
            Parent = pnlInfo
        };
        lblInfo = new Label
        {
            Text = _currentZone.Description,
            Font = new Font("Segoe UI", 12f),
            ForeColor = Color.LightGray,
            AutoSize = true,
            Location = new Point(20, 80),
            Parent = pnlInfo
        };
        Label lblDiff = new Label
        {
            Text = "Difficulty:",
            ForeColor = Color.White,
            Location = new Point(20, 130),
            AutoSize = true,
            Parent = pnlInfo
        };
        cmbDifficulty = new ComboBox
        {
            Location = new Point(80, 125),
            Width = 200,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Parent = pnlInfo
        };
        cmbDifficulty.SelectedIndexChanged += delegate
        {
            if (cmbDifficulty.SelectedItem is ComboBoxItem comboBoxItem)
            {
                _selectedDifficulty = comboBoxItem.Value;
            }
            UpdateProgressBar();
        };
        pbZoneProgress = new GameProgressBar
        {
            Maximum = 100,
            Value = 0L,
            Parent = pnlInfo,
            Location = new Point(20, 170),
            Size = new Size(200, 25),
            BarColor = Color.Gold,
            ShowPercentage = true
        };
        base.Controls.Add(pnlInfo);
        btnExplore = new Button
        {
            Text = "EXPLORE",
            Size = new Size(200, 60),
            Location = new Point(20, 210),
            BackColor = Color.DarkOrange,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 14f, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Parent = pnlInfo
        };
        btnExplore.Click += BtnExplore_Click;
        btnBack = new Button
        {
            Text = "X",
            Size = new Size(40, 40),
            Location = new Point(base.Width - 60, 20),
            Anchor = (AnchorStyles.Top | AnchorStyles.Right),
            BackColor = Color.IndianRed,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Parent = pnlInfo
        };
        btnBack.Click += delegate
        {
            this.OnReturnRequested?.Invoke(this, EventArgs.Empty);
        };
        ucArena = new UcArena();
        ucArena.Dock = DockStyle.Fill;
        ucArena.Visible = false;
        ucArena.OnBattleEnded += UcArena_OnBattleEnded;
        ucArena.OnStatsUpdated += delegate
        {
            if (base.ParentForm is FormMain formMain)
            {
                formMain.UpdateBars();
            }
        };
        base.Controls.Add(ucArena);
        ucArena.BringToFront();
        RefreshDifficultyCombo(hero);
        RecomputeBossState(hero);
    }

    private void RefreshDifficultyCombo(CharacterModel hero)
    {
        cmbDifficulty.Items.Clear();
        cmbDifficulty.Items.Add(new ComboBoxItem
        {
            Text = "Easy (1 Enemy)",
            Value = Enums.ZoneDifficulty.Easy
        });
        if (_zoneManager.CanEnterDifficulty(hero.CharacterID, _currentZone.ZoneID, Enums.ZoneDifficulty.Normal))
        {
            cmbDifficulty.Items.Add(new ComboBoxItem
            {
                Text = "Normal (2 Enemies - 2x Rewards)",
                Value = Enums.ZoneDifficulty.Normal
            });
        }
        if (_zoneManager.CanEnterDifficulty(hero.CharacterID, _currentZone.ZoneID, Enums.ZoneDifficulty.Hard))
        {
            cmbDifficulty.Items.Add(new ComboBoxItem
            {
                Text = "Hard (5 Enemies - 5x Rewards)",
                Value = Enums.ZoneDifficulty.Hard
            });
        }
        bool found = false;
        foreach (ComboBoxItem item in cmbDifficulty.Items)
        {
            if (item.Value == _selectedDifficulty)
            {
                cmbDifficulty.SelectedItem = item;
                found = true;
                break;
            }
        }
        if (!found && cmbDifficulty.Items.Count > 0)
        {
            cmbDifficulty.SelectedIndex = 0;
        }
        RecomputeBossState(hero);
        UpdateProgressBar();
    }

    private void RecomputeBossState(CharacterModel hero)
    {
        int currentProg = _zoneManager.GetProgressValue(hero.CharacterID, _currentZone.ZoneID, _selectedDifficulty);
        bool bossAlreadyKilled = _zoneManager.IsBossKilled(hero.CharacterID, _currentZone.ZoneID, _selectedDifficulty);
        _bossReadyToSpawn = currentProg >= 100 && !bossAlreadyKilled;
    }

    private void UpdateProgressBar()
    {
        CharacterModel hero = SessionManager.CurrentCharacter;
        int currentProg = _zoneManager.GetProgressValue(hero.CharacterID, _currentZone.ZoneID, _selectedDifficulty);
        pbZoneProgress.Value = currentProg;
        pbZoneProgress.BarColor = ((currentProg >= 100) ? Color.LimeGreen : Color.Gold);
        RecomputeBossState(hero);
        if (currentProg >= 100 && _bossReadyToSpawn)
        {
            btnExplore.Text = "BOSS BATTLE";
            btnExplore.BackColor = Color.DarkRed;
            _isBossFight = true;
        }
        else
        {
            btnExplore.Text = "EXPLORE";
            btnExplore.BackColor = Color.DarkOrange;
            _isBossFight = false;
        }
    }

    private void BtnExplore_Click(object sender, EventArgs e)
    {
        if (!_isBossFight)
        {
            Random rnd = new Random();
            if (rnd.Next(1, 100) > 90)
            {
                MessageBox.Show("It seems quiet around here...");
                return;
            }
        }
        StartCombat();
    }

    private void StartCombat()
    {
        pnlInfo.Visible = false;
        ucArena.Visible = true;
        ucArena.Focus();
        CharacterModel hero = SessionManager.CurrentCharacter;
        _currentEnemyIDs.Clear();
        int enemyCount = 1;
        float statMultiplier = 1f;
        if (_isBossFight)
        {
            enemyCount = 1;
            statMultiplier = 1.5f + (float)_selectedDifficulty * 0.5f;
        }
        else
        {
            switch (_selectedDifficulty)
            {
                case Enums.ZoneDifficulty.Easy:
                    enemyCount = 1;
                    statMultiplier = 1f;
                    break;
                case Enums.ZoneDifficulty.Normal:
                    enemyCount = 2;
                    statMultiplier = 1.2f;
                    break;
                case Enums.ZoneDifficulty.Hard:
                    enemyCount = 5;
                    statMultiplier = 1.5f;
                    break;
            }
        }
        EnemyModel enemyTemplate = _zoneManager.GetEnemyForZone(_currentZone.ZoneID, _isBossFight);
        if (enemyTemplate == null)
        {
            MessageBox.Show("No enemy data for this zone!");
            ucArena.Visible = false;
            pnlInfo.Visible = true;
            return;
        }
        List<EnemyModel> enemyList = EnemyFactory.CreateEnemies(enemyTemplate, enemyCount, statMultiplier);
        foreach (EnemyModel en in enemyList)
        {
            _currentEnemyIDs.Add(en.EnemyID);
        }
        if (_isBossFight)
        {
            MessageBox.Show($"BOSS INCOMING: {enemyList[0].Name}\nDifficulty: {_selectedDifficulty}", "WARNING", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        }
        ucArena.StartSurvivalBattle(hero, enemyList);
    }

    private void UcArena_OnBattleEnded(object sender, bool victory)
    {
        // Don't hide arena yet, we want to show results ON the arena.
        CharacterModel hero = SessionManager.CurrentCharacter;
        if (victory)
        {
            HandleVictory(hero);
        }
        else
        {
            HandleDefeat(hero);
        }
    }

    private void HandleVictory(CharacterModel hero)
    {
        string allLoot = "";
        int totalXP = 0;
        double diffMultiplier = 1.0;
        if (_selectedDifficulty == Enums.ZoneDifficulty.Normal)
        {
            diffMultiplier = 2.0;
        }
        if (_selectedDifficulty == Enums.ZoneDifficulty.Hard)
        {
            diffMultiplier = 5.0;
        }
        foreach (int eid in _currentEnemyIDs)
        {
            List<string> logs = _lootManager.ProcessLoot(hero.CharacterID, eid);
            if (logs.Count > 0)
            {
                allLoot = allLoot + string.Join("\n", logs) + "\n";
            }
            totalXP += (int)(20.0 * diffMultiplier);
        }
        _levelManager.AddExperience(hero, totalXP);

        string resultMsg = $"Victory! +{totalXP} XP";
        if (!string.IsNullOrEmpty(allLoot))
        {
            resultMsg = resultMsg + "\n\nLOOT:\n" + allLoot;
        }

        if (_isBossFight)
        {
            _zoneManager.MarkBossKilled(hero.CharacterID, _currentZone.ZoneID, _selectedDifficulty);
            _bossReadyToSpawn = false;
            if (_selectedDifficulty == Enums.ZoneDifficulty.Easy && hero.CurrentZoneID == hero.MaxUnlockedZoneID)
            {
                hero.MaxUnlockedZoneID++;
                _charRepo.UpdateProgress(hero);
                resultMsg += "\n\nNEW ZONE unlocked on map!";
            }
            resultMsg += $"\n{_selectedDifficulty} difficulty completed!\nYou can now play this stage with unlimited normal enemies since you defeated the Boss.";
        }
        else if (!_zoneManager.IsBossKilled(hero.CharacterID, _currentZone.ZoneID, _selectedDifficulty))
        {
            int currentVal = _zoneManager.GetProgressValue(hero.CharacterID, _currentZone.ZoneID, _selectedDifficulty);
            if (currentVal < 100)
            {
                int amount = 10;
                if (_selectedDifficulty == Enums.ZoneDifficulty.Normal)
                {
                    amount = 7;
                }
                if (_selectedDifficulty == Enums.ZoneDifficulty.Hard)
                {
                    amount = 5;
                }
                _zoneManager.AddProgress(hero.CharacterID, _currentZone.ZoneID, _selectedDifficulty, amount);
                RecomputeBossState(hero);
            }
        }

        RefreshDifficultyCombo(hero);
        UpdateProgressBar();
        if (base.ParentForm is FormMain mainForm)
        {
            mainForm.UpdateUI();
        }

        // Show Results on Arena Overlay
        ucArena.ShowBattleResults(resultMsg, () =>
        {
            ucArena.Visible = false;
            pnlInfo.Visible = true;
        });
    }

    private void HandleDefeat(CharacterModel hero)
    {
        bool bossAlreadyKilled = _zoneManager.IsBossKilled(hero.CharacterID, _currentZone.ZoneID, _selectedDifficulty);
        int currentProgress = _zoneManager.GetProgressValue(hero.CharacterID, _currentZone.ZoneID, _selectedDifficulty);
        if (!bossAlreadyKilled && currentProgress < 100)
        {
            _zoneManager.ResetProgress(hero.CharacterID, _currentZone.ZoneID, _selectedDifficulty);
        }
        RecomputeBossState(hero);
        // Ekipman bonuslarını da dahil ederek MaxHP/MaxMana hesapla
        InventoryManager invManager = new InventoryManager();
        var equipment = invManager.GetInventory(hero.CharacterID).FindAll(x => x.Location == Enums.ItemLocation.Equipment);
        int maxHP = StatManager.CalculateTotalMaxHP(hero, equipment);
        int maxMana = StatManager.CalculateTotalMaxMana(hero, equipment);
        hero.HP = maxHP / 2;
        hero.Mana = maxMana / 2;
        _charRepo.UpdateProgress(hero);

        if (base.ParentForm is FormMain mainForm)
        {
            SessionManager.CurrentCharacter.HP = hero.HP;
            SessionManager.CurrentCharacter.Mana = hero.Mana;
            mainForm.RefreshStats();
            mainForm.UpdateBars();
        }

        ucArena.ShowBattleResults("You died... Returning to town wounded.", () =>
        {
            ucArena.Visible = false;
            // Return to town/main menu
            this.OnReturnRequested?.Invoke(this, EventArgs.Empty);
        });
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (ucArena.Visible)
        {
            return base.ProcessCmdKey(ref msg, keyData);
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    public void RelayKeyDown(Keys key)
    {
        if (ucArena.Visible)
        {
            ucArena.HandleKeyDown(key);
        }
    }

    public void RelayKeyUp(Keys key)
    {
        if (ucArena.Visible)
        {
            ucArena.HandleKeyUp(key);
        }
    }

    public void RefreshBattleStats()
    {
        CharacterModel hero = SessionManager.CurrentCharacter;
        if (hero != null)
        {
            RefreshDifficultyCombo(hero);
            UpdateProgressBar();
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
        this.components = new System.ComponentModel.Container();
        base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
    }
}
