using System.Windows.Forms;

namespace rpg_deneme.UI.Controls;

public class ArenaPanel : Panel
{
	public ArenaPanel()
	{
		DoubleBuffered = true;
		SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
		UpdateStyles();
	}
}
