using System;
using System.Drawing;
using System.Windows.Forms;

namespace rpg_deneme.UI.Windows;

public class FormGameMenu : Form
{
    public enum MenuAction
    {
        Resume,
        CharSelect,
        Exit
    }

    public MenuAction SelectedAction { get; private set; } = MenuAction.Resume;

    public FormGameMenu()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        this.FormBorderStyle = FormBorderStyle.None;
        this.StartPosition = FormStartPosition.CenterParent;
        this.Size = new Size(300, 250);
        this.BackColor = Color.FromArgb(45, 45, 48);
        this.Padding = new Padding(2);

        // Border mechanism (Panel inside with padding)? 
        // Or just Paint event. Let's use a Panel for the content to simulate border if needed, 
        // or just set Form BorderStyle to distinct. 
        // FixedSingle gives a standard border.
        this.FormBorderStyle = FormBorderStyle.FixedSingle;
        this.ControlBox = false;

        Label lblTitle = new Label();
        lblTitle.Text = "GAME MENU";
        lblTitle.Font = new Font("Segoe UI", 14, FontStyle.Bold);
        lblTitle.ForeColor = Color.White;
        lblTitle.TextAlign = ContentAlignment.MiddleCenter;
        lblTitle.Dock = DockStyle.Top;
        lblTitle.Height = 50;
        this.Controls.Add(lblTitle);

        int btnHeight = 40;
        int spacing = 10;
        int startY = 70;

        Button btnResume = CreateButton("RESUME GAME", Color.FromArgb(60, 60, 60));
        btnResume.Location = new Point(50, startY);
        btnResume.Click += (s, e) => { SelectedAction = MenuAction.Resume; this.Close(); };
        this.Controls.Add(btnResume);

        Button btnCharSelect = CreateButton("CHARACTER SELECT", Color.FromArgb(60, 60, 60));
        btnCharSelect.Location = new Point(50, startY + btnHeight + spacing);
        btnCharSelect.Click += (s, e) => { SelectedAction = MenuAction.CharSelect; this.Close(); };
        this.Controls.Add(btnCharSelect);

        Button btnExit = CreateButton("EXIT GAME", Color.Maroon);
        btnExit.Location = new Point(50, startY + (btnHeight + spacing) * 2);
        btnExit.Click += (s, e) => { SelectedAction = MenuAction.Exit; this.Close(); };
        this.Controls.Add(btnExit);
    }

    private Button CreateButton(string text, Color backParams)
    {
        Button btn = new Button();
        btn.Text = text;
        btn.Size = new Size(200, 40);
        btn.FlatStyle = FlatStyle.Flat;
        btn.ForeColor = Color.White;
        btn.BackColor = backParams;
        btn.Cursor = Cursors.Hand;
        btn.FlatAppearance.BorderSize = 0;
        return btn;
    }
}
