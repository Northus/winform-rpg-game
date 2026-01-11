using System;
using System.Windows.Forms;
using rpg_deneme.Data;
using rpg_deneme.UI;
using Microsoft.Data.Sqlite;

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

        // Patch Icons
        try
        {
            using (var conn = Data.DatabaseHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "UPDATE Skills SET IconPath = @Icon WHERE Name = @Name";
                    var pName = cmd.Parameters.Add("@Name", SqliteType.Text);
                    var pIcon = cmd.Parameters.Add("@Icon", SqliteType.Text);

                    void Update(string n, string i) { pName.Value = n; pIcon.Value = i; cmd.ExecuteNonQuery(); }

                    Update("Savaş Çılgınlığı", "fury_icon");
                    Update("Ezici Darbe", "crush_icon");
                    Update("Dayanıklılık", "hp_icon");
                    Update("Kritik Ustalığı", "crit_icon");

                    Update("Ateş Ustalığı", "fire_mastery_icon");
                    Update("Buz Ustalığı", "ice_mastery_icon");
                    Update("Yıldırım Ustalığı", "lightning_mastery_icon");
                    Update("Zincirleme Yıldırım", "chain_lightning_icon");
                    Update("Mana Havuzu", "mana_icon");
                    Update("Gizemli Güç", "arcane_icon");

                    Update("Çeviklik", "agility_icon");
                    Update("Zehirleme", "envenom_icon");
                    Update("Çift Darbe", "dual_strike_icon");
                    Update("Kaçınma", "evasion_icon");
                    Update("Ölüm İşareti", "death_mark_icon");
                    Update("Kritik Vuruş", "crit_strike_icon");
                    Update("Can Çalma", "lifesteal_icon");
                }
            }
        }
        catch { }

        // Karakter seçim formunu başlat
        Application.Run(new FormCharSelect());
    }
}
