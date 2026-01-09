using System.Collections.Generic;
using rpg_deneme.Models;

namespace rpg_deneme.Core;

/// <summary>
/// Mevcut oyun oturumunu (seçili karakter, kısa yollar vb.) yöneten sınıf.
/// </summary>
public static class SessionManager
{
    private static HashSet<long> _reservedInstanceIds = new HashSet<long>();

    /// <summary>
    /// Şu an oynanan karakter.
    /// </summary>
    public static CharacterModel CurrentCharacter { get; set; }

    /// <summary>
    /// Hızlı erişim çubuğu (hotbar) slotları.
    /// </summary>
    public static HotbarInfo[] HotbarSlots { get; } = new HotbarInfo[5];

    /// <summary>
    /// Bir eşyayı (işlemde olduğu için) rezerve eder.
    /// </summary>
    public static void ReserveItem(long instanceId)
    {
        if (instanceId > 0)
        {
            _reservedInstanceIds.Add(instanceId);
        }
    }

    /// <summary>
    /// Eşya üzerindeki rezervasyonu kaldırır.
    /// </summary>
    public static void UnreserveItem(long instanceId)
    {
        if (instanceId > 0)
        {
            _reservedInstanceIds.Remove(instanceId);
        }
    }

    /// <summary>
    /// Eşyanın rezerve edilip edilmediğini kontrol eder.
    /// </summary>
    public static bool IsReserved(long instanceId)
    {
        return instanceId > 0 && _reservedInstanceIds.Contains(instanceId);
    }

    /// <summary>
    /// Tüm rezervasyonları temizler.
    /// </summary>
    public static void ClearAllReservations()
    {
        _reservedInstanceIds.Clear();
    }
}
