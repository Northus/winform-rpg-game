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
        try
        {
            // Uygulama konfigürasyonunu başlat
            ApplicationConfiguration.Initialize();

            // Veritabanı şemasının varlığını kontrol et ve gerekirse oluştur
            try
            {
                SQLiteSchemaInitializer.EnsureCreated();
            }
            catch (Exception dbEx)
            {
                MessageBox.Show($"Veritabanı başlatma hatası: {dbEx.Message}\n\n{dbEx.StackTrace}", "Kritik Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Karakter seçim formunu başlat
            Application.Run(new FormCharSelect());
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Beklenmeyen bir hata oluştu: {ex.Message}\n\n{ex.StackTrace}", "Kritik Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
