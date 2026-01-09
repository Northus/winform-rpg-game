using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using rpg_deneme.Business;
using rpg_deneme.Core;
using rpg_deneme.Models;
using rpg_deneme.UI.Controls;

namespace rpg_deneme.UI.Windows;

public class UcSurvivalLobby : GameWindow
{
	private ComboBox cmbWaves;

	private Label lblInfo;

	private Button btnStart;

	private IContainer components = null;

	public event EventHandler<int> OnStartRequested;

	public UcSurvivalLobby()
	{
		base.Title = "ARENA MASTER";
		base.Size = new Size(300, 300);
		SetupUI();
	}

	private void SetupUI()
	{
		Label lblSelect = new Label
		{
			Text = "Dalga Seç:",
			Location = new Point(20, 50),
			ForeColor = Color.White,
			AutoSize = true
		};
		base.Controls.Add(lblSelect);
		cmbWaves = new ComboBox
		{
			Location = new Point(100, 45),
			Width = 150,
			DropDownStyle = ComboBoxStyle.DropDownList
		};
		base.Controls.Add(cmbWaves);
		cmbWaves.SelectedIndexChanged += delegate
		{
			UpdateInfo();
		};
		lblInfo = new Label
		{
			Location = new Point(20, 90),
			Size = new Size(260, 100),
			ForeColor = Color.LightGray,
			Text = "..."
		};
		base.Controls.Add(lblInfo);
		btnStart = new Button
		{
			Text = "SAVAŞI BAŞLAT",
			Location = new Point(50, 200),
			Size = new Size(200, 50),
			BackColor = Color.DarkRed,
			ForeColor = Color.White,
			FlatStyle = FlatStyle.Flat
		};
		btnStart.Click += delegate
		{
			int e2 = (int)cmbWaves.SelectedItem;
			this.OnStartRequested?.Invoke(this, e2);
		};
		base.Controls.Add(btnStart);
	}

	public void LoadData()
	{
		CharacterModel hero = SessionManager.CurrentCharacter;
		cmbWaves.Items.Clear();
		for (int i = 1; i <= hero.MaxSurvivalWave; i++)
		{
			cmbWaves.Items.Add(i);
		}
		cmbWaves.SelectedIndex = cmbWaves.Items.Count - 1;
	}

	private void UpdateInfo()
	{
		if (cmbWaves.SelectedItem != null)
		{
			int wave = (int)cmbWaves.SelectedItem;
			SurvivalManager logic = new SurvivalManager();
			(int EnemyCount, int MinLevel, int MaxLevel) waveInfo = logic.GetWaveInfo(wave);
			int count = waveInfo.EnemyCount;
			int min = waveInfo.MinLevel;
			int max = waveInfo.MaxLevel;
			CharacterModel hero = SessionManager.CurrentCharacter;
			bool isFirstTime = wave == hero.MaxSurvivalWave;
			int reward = logic.CalculateReward(wave, isFirstTime);
			lblInfo.Text = $"DALGA {wave}\n\nDüşman Sayısı: {count}\nDüşman Seviyesi: {min} - {max}\n\nÖdül: {reward} Gold";
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
