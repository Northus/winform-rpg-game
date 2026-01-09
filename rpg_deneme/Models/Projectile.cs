using System;
using System.Drawing;

namespace rpg_deneme.Models;

public class SkillProjectile
{
	public float X { get; set; }

	public float Y { get; set; }

	public float Speed { get; set; } = 10f;

	public int Damage { get; set; }

	public float Angle { get; set; }

	public int Size { get; set; } = 8;

	public Rectangle Bounds => new Rectangle((int)X, (int)Y, Size, Size);

	public SkillProjectile(float startX, float startY, float targetX, float targetY, int dmg)
	{
		X = startX;
		Y = startY;
		Damage = dmg;
		float diffX = targetX - startX;
		float diffY = targetY - startY;
		Angle = (float)Math.Atan2(diffY, diffX);
	}

	public void Move()
	{
		X += (float)(Math.Cos(Angle) * (double)Speed);
		Y += (float)(Math.Sin(Angle) * (double)Speed);
	}
}
