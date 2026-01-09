using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using rpg_deneme.Business;
using rpg_deneme.Core;
using rpg_deneme.Models;

namespace rpg_deneme.UI.Windows;

public class UcZoneMap : UserControl
{
	private ZoneManager _zoneManager = new ZoneManager();

	private Panel _pnlTop;

	private Button _btnBackToTown;

	private IContainer components = null;

	private FlowLayoutPanel flowLayoutPanel1;

	public event EventHandler<ZoneModel> OnZoneSelected;

	public event EventHandler OnTownRequested;

	public UcZoneMap()
	{
		InitializeComponent();
		Dock = DockStyle.Fill;
		BackColor = Color.FromArgb(40, 40, 40);
		SetupUI();
		GenerateZones();
	}

	private void SetupUI()
	{
		_pnlTop = new Panel
		{
			Dock = DockStyle.Top,
			Height = 60,
			BackColor = Color.FromArgb(30, 30, 30),
			Padding = new Padding(10)
		};
		_btnBackToTown = new Button
		{
			Text = "⬅ KÖYE DÖN",
			Size = new Size(150, 40),
			Dock = DockStyle.Left,
			BackColor = Color.IndianRed,
			ForeColor = Color.White,
			FlatStyle = FlatStyle.Flat,
			Font = new Font("Segoe UI", 10f, FontStyle.Bold),
			Cursor = Cursors.Hand
		};
		_btnBackToTown.Click += delegate
		{
			this.OnTownRequested?.Invoke(this, EventArgs.Empty);
		};
		_pnlTop.Controls.Add(_btnBackToTown);
		base.Controls.Add(_pnlTop);
		if (flowLayoutPanel1 != null)
		{
			flowLayoutPanel1.Parent = this;
			flowLayoutPanel1.Dock = DockStyle.Fill;
			flowLayoutPanel1.BringToFront();
			_pnlTop.SendToBack();
		}
	}

	private void GenerateZones()
	{
		flowLayoutPanel1.Controls.Clear();
		CharacterModel hero = SessionManager.CurrentCharacter;
		List<ZoneModel> zones = _zoneManager.GetAvailableZones(hero);
		foreach (ZoneModel zone in zones)
		{
			int progressVal = 0;
			if (zone.ZoneID < hero.MaxUnlockedZoneID)
			{
				progressVal = 100;
			}
			else if (zone.ZoneID == hero.MaxUnlockedZoneID)
			{
				progressVal = _zoneManager.GetProgressValue(hero.CharacterID, zone.ZoneID, Enums.ZoneDifficulty.Easy);
			}
			Button btn = new Button
			{
				Text = zone.Name + (zone.IsUnlocked ? $"\n%{progressVal}" : "\n(Kilitli)"),
				Size = new Size(180, 180),
				Margin = new Padding(15),
				BackColor = (zone.IsUnlocked ? Color.FromArgb(60, 60, 65) : Color.FromArgb(45, 45, 45)),
				ForeColor = (zone.IsUnlocked ? Color.White : Color.DarkGray),
				FlatStyle = FlatStyle.Flat,
				Font = new Font("Segoe UI", 12f, FontStyle.Bold),
				Enabled = true,
				Cursor = (zone.IsUnlocked ? Cursors.Hand : Cursors.No),
				Tag = zone
			};
			btn.FlatAppearance.BorderSize = 1;
			btn.FlatAppearance.BorderColor = Color.DimGray;
			btn.Paint += delegate(object? s, PaintEventArgs e)
			{
				Button button = (Button)s;
				Graphics graphics = e.Graphics;
				if (progressVal > 0)
				{
					int num = 15;
					int width = (int)((double)(button.Width * progressVal) / 100.0);
					Rectangle rect = new Rectangle(0, button.Height - num, width, num);
					Color color = ((progressVal == 100) ? Color.LimeGreen : Color.DarkOrange);
					using SolidBrush brush = new SolidBrush(color);
					graphics.FillRectangle(brush, rect);
				}
			};
			btn.Click += delegate
			{
				if (zone.IsUnlocked)
				{
					hero.CurrentZoneID = zone.ZoneID;
					this.OnZoneSelected?.Invoke(this, zone);
				}
			};
			flowLayoutPanel1.Controls.Add(btn);
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
		this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
		base.SuspendLayout();
		this.flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
		this.flowLayoutPanel1.Location = new System.Drawing.Point(0, 0);
		this.flowLayoutPanel1.Name = "flowLayoutPanel1";
		this.flowLayoutPanel1.Size = new System.Drawing.Size(150, 150);
		this.flowLayoutPanel1.TabIndex = 0;
		base.AutoScaleDimensions = new System.Drawing.SizeF(7f, 15f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		base.Controls.Add(this.flowLayoutPanel1);
		base.Name = "UcZoneMap";
		base.ResumeLayout(false);
	}
}
