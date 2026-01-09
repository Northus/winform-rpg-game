using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace rpg_deneme.UI.Controls;

public class GameWindow : UserControl
{
	public const int WM_NCLBUTTONDOWN = 161;

	public const int HT_CAPTION = 2;

	private Panel pnlHeader;

	private Label lblTitle;

	private Button btnClose;

	private IContainer components = null;

	public string Title
	{
		get
		{
			return lblTitle.Text;
		}
		set
		{
			lblTitle.Text = value;
		}
	}

	[DllImport("user32.dll")]
	public static extern int SendMessage(nint hWnd, int Msg, int wParam, int lParam);

	[DllImport("user32.dll")]
	public static extern bool ReleaseCapture();

	public GameWindow()
	{
		InitializeComponent();
		SetupDesign();
	}

	private void SetupDesign()
	{
		BackColor = Color.FromArgb(45, 45, 48);
		base.BorderStyle = BorderStyle.FixedSingle;
		base.Size = new Size(300, 400);
		pnlHeader = new Panel
		{
			Dock = DockStyle.Top,
			Height = 30,
			BackColor = Color.FromArgb(60, 60, 60)
		};
		pnlHeader.MouseDown += Header_MouseDown;
		lblTitle = new Label
		{
			Text = "Window",
			ForeColor = Color.WhiteSmoke,
			Dock = DockStyle.Left,
			TextAlign = ContentAlignment.MiddleLeft,
			Padding = new Padding(10, 0, 0, 0),
			AutoSize = true
		};
		lblTitle.MouseDown += Header_MouseDown;
		btnClose = new Button
		{
			Text = "X",
			Dock = DockStyle.Right,
			Width = 30,
			FlatStyle = FlatStyle.Flat,
			ForeColor = Color.Red,
			Cursor = Cursors.Hand
		};
		btnClose.FlatAppearance.BorderSize = 0;
		btnClose.Click += delegate
		{
			CloseWindow();
		};
		pnlHeader.Controls.Add(btnClose);
		pnlHeader.Controls.Add(lblTitle);
		base.Controls.Add(pnlHeader);
	}

	private void Header_MouseDown(object sender, MouseEventArgs e)
	{
		if (e.Button == MouseButtons.Left)
		{
			ReleaseCapture();
			SendMessage(base.Handle, 161, 2, 0);
		}
	}

	public void CloseWindow()
	{
		try
		{
			if (OnClosing())
			{
				Hide();
			}
		}
		catch
		{
			Hide();
		}
	}

	protected virtual bool OnClosing()
	{
		return true;
	}

	protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
	{
		if (keyData == Keys.Escape)
		{
			CloseWindow();
			return true;
		}
		return base.ProcessCmdKey(ref msg, keyData);
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
