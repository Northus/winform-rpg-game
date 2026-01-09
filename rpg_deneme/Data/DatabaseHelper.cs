using System.IO;
using System.Windows.Forms;
using Microsoft.Data.Sqlite;

namespace rpg_deneme.Data;

/// <summary>
/// Veritabanı bağlantılarını ve dosya kontrolünü yöneten yardımcı sınıf.
/// </summary>
public static class DatabaseHelper
{
    private static readonly string DbPath = Path.Combine(Application.StartupPath, "database.db");

    private static readonly string ConnectionString = new SqliteConnectionStringBuilder
    {
        DataSource = DbPath,
        Mode = SqliteOpenMode.ReadWriteCreate,
        ForeignKeys = true
    }.ToString();

    /// <summary>
    /// Yeni bir veritabanı bağlantısı oluşturur.
    /// </summary>
    public static SqliteConnection GetConnection()
    {
        return new SqliteConnection(ConnectionString);
    }

    /// <summary>
    /// Veritabanı dosyasının varlığını kontrol eder, yoksa oluşturur.
    /// </summary>
    public static void EnsureDatabaseFileExists()
    {
        string dir = Path.GetDirectoryName(DbPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        if (!File.Exists(DbPath))
        {
            using (SqliteConnection conn = GetConnection())
            {
                conn.Open();
            }
        }
    }

    /// <summary>
    /// Veritabanı dosyasının tam yolunu döner.
    /// </summary>
    public static string GetDatabasePath()
    {
        return DbPath;
    }
}
