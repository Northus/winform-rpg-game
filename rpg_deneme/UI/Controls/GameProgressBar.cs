using System.Drawing;
using System.Drawing.Text;
using System.Windows.Forms;

namespace rpg_deneme.UI.Controls;

public class GameProgressBar : Control
{
	private int _maximum = 100;

	private long _value = 0L;

	private Color _barColor = Color.Red;

	public Color BarColor
	{
		get
		{
			return _barColor;
		}
		set
		{
			_barColor = value;
			Invalidate();
		}
	}

	public bool ShowPercentage { get; set; } = false;

	public int Maximum
	{
		get
		{
			return _maximum;
		}
		set
		{
			_maximum = value;
			Invalidate();
		}
	}

	public long Value
	{
		get
		{
			return _value;
		}
		set
		{
			long newVal = value;
			if (newVal < 0)
			{
				newVal = 0L;
			}
			if (newVal > _maximum)
			{
				newVal = _maximum;
			}
			_value = newVal;
			Invalidate();
		}
	}

	public GameProgressBar()
	{
		DoubleBuffered = true;
		base.Size = new Size(200, 30);
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		base.OnPaint(e);
		Graphics g = e.Graphics;
		g.TextRenderingHint = TextRenderingHint.AntiAlias;
		using (SolidBrush bgBrush = new SolidBrush(Color.FromArgb(40, 40, 40)))
		{
			g.FillRectangle(bgBrush, base.ClientRectangle);
		}
		if (_maximum > 0)
		{
			float percent = (float)_value / (float)_maximum;
			int width = (int)((float)base.Width * percent);
			if (width > 0)
			{
				Rectangle rect = new Rectangle(0, 0, width, base.Height);
				using SolidBrush barBrush = new SolidBrush(_barColor);
				g.FillRectangle(barBrush, rect);
			}
		}
		string text;
		if (ShowPercentage && _maximum > 0)
		{
			float percentVal = (float)_value / (float)_maximum * 100f;
			text = $"%{percentVal:F1}";
		}
		else
		{
			text = $"{_value} / {_maximum}";
		}
		using (Font f = new Font("Segoe UI", 10f, FontStyle.Bold))
		{
			SizeF textSize = g.MeasureString(text, f);
			float x = ((float)base.Width - textSize.Width) / 2f;
			float y = ((float)base.Height - textSize.Height) / 2f;
			using (SolidBrush shadow = new SolidBrush(Color.Black))
			{
				g.DrawString(text, f, shadow, x + 1f, y + 1f);
			}
			using SolidBrush fore = new SolidBrush(Color.White);
			g.DrawString(text, f, fore, x, y);
		}
		using Pen pen = new Pen(Color.DimGray);
		g.DrawRectangle(pen, 0, 0, base.Width - 1, base.Height - 1);
	}
}
