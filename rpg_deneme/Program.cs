using System;
using System.Windows.Forms;
using rpg_deneme.Data;
using rpg_deneme.UI;

namespace rpg_deneme;

/// <summary>
/// Uygulamanın ana giriş noktası.
/// </summary>
internal static class Program
{
    /// <summary>
    /// Uygulamayı başlatan ana metod.
    /// </summary>
    [STAThread]
    private static void Main()
    {
        // Uygulama konfigürasyonunu başlat
        ApplicationConfiguration.Initialize();

        // Veritabanı şemasının varlığını kontrol et ve gerekirse oluştur
        SQLiteSchemaInitializer.EnsureCreated();

        // Skills tablosunu zorla doldur (boşsa)
        //SQLiteSchemaInitializer.ForceSeedSkills();

        // Karakter seçim formunu başlat
        Application.Run(new FormCharSelect());
    }
}
