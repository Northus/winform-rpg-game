using System.Drawing;
using rpg_deneme.Models;

namespace rpg_deneme.UI.Controls.GameEntities;

public class HotbarSlot
{
    public int Index { get; set; }

    public int Type { get; set; } // 0 = Item, 1 = Skill

    public long? ReferenceID { get; set; }

    public ItemInstance Item { get; set; }

    public SkillModel Skill { get; set; }

    public Image CachedImage { get; set; }
}
