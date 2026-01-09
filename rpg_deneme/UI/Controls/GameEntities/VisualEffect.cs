using System.Drawing;

namespace rpg_deneme.UI.Controls.GameEntities;

public class VisualEffect
{
    public float X;
    public float Y;
    public float TargetX; // Zincirleme efektler için hedef
    public float TargetY;
    public string Text;
    public Color Color;
    public int LifeTime;
    public bool IsText;
    public int Size;
    public int EffectType;
    // EffectType values:
    // 0 = Text/Spark
    // 1 = Hit flash
    // 2 = Death puff
    // 3 = Potion Ring (expanding)
    // 10 = Slash (directional melee)
    // 11 = Whirlwind (rotating AoE)
    // 12 = Fire burst
    // 13 = Ice shatter  
    // 14 = Arcane nova
    // 15 = Lightning chain

    public float Duration; // Total duration in frames
    public int MaxRadius; // For expanding effects
    public float Angle; // Direction for directional effects
    public int SkillVisualType; // 0=neutral, 1=fire, 2=ice, 3=arcane
    public int InitialLifeTime; // Store initial lifetime for progress calculation
}
