using rpg_deneme.Core;

namespace rpg_deneme.Models;

/// <summary>
/// Eşya üzerindeki efsun (attribute) model sınıfı.
/// </summary>
public class ItemAttribute
{
    public long AttributeID { get; set; }
    public long InstanceID { get; set; }
    public Enums.ItemAttributeType AttributeType { get; set; }
    public int Value { get; set; }
}
