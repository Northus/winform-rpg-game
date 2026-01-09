using System.Drawing;
using rpg_deneme.Core;

namespace rpg_deneme.UI.Controls;

public partial class UcArena
{
    // Moved from deleted `UcArena.Rendering.cs`


    private Color GetProjectileColor(int visualType, bool isEnemy)
    {
        if (isEnemy) return Color.Crimson;
        return visualType switch
        {
            1 => Color.OrangeRed, // Fire
            2 => Color.LightCyan, // Ice
            3 => Color.DarkViolet, // Arcane
            _ => Color.Cyan // Basic
        };
    }

    private Color GetHotbarColor(Enums.ItemGrade grade, Enums.ItemType type)
    {
        if (type == Enums.ItemType.Consumable)
            return Color.MediumSeaGreen;

        return grade switch
        {
            Enums.ItemGrade.Common => Color.WhiteSmoke,
            Enums.ItemGrade.Rare => Color.CornflowerBlue,
            Enums.ItemGrade.Epic => Color.MediumPurple,
            Enums.ItemGrade.Legendary => Color.Orange,
            _ => Color.Gray,
        };
    }
}
