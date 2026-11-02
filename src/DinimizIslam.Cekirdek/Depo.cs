using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace DinimizIslam.Cekirdek;

/// <summary>Önceki Python sürümünün SQLite dosyasını ve yer imlerini de okuyabilen yerel depo.</summary>
public sealed class Depo
{
    private readonly string _connection;
    public string Klasor { get; }
    public static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };
    public Depo(string klasor)
    {
        Klasor = klasor;
        Directory.CreateDirectory(klasor);
        _connection = new SqliteConnectionStringBuilder { DataSource = Path.Combine(klasor, "veriler.sqlite3"), DefaultTimeout = 10 }.ToString();
        using var db = Ac();
        using var command = db.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS cache(url TEXT PRIMARY KEY, kind TEXT, payload TEXT, fetched TEXT);
            CREATE TABLE IF NOT EXISTS marks(url TEXT PRIMARY KEY, title TEXT);
            CREATE TABLE IF NOT EXISTS history(url TEXT PRIMARY KEY, title TEXT, opened TEXT);
            CREATE TABLE IF NOT EXISTS settings(key TEXT PRIMARY KEY, value TEXT);
            """;
        command.ExecuteNonQuery();
    }
    private SqliteConnection Ac() { var db = new SqliteConnection(_connection); db.Open(); return db; }
    public Kayit<T>? Oku<T>(string key)
    {
        using var db = Ac(); using var c = db.CreateCommand();
        c.CommandText = "SELECT payload,fetched FROM cache WHERE url=$key"; c.Parameters.AddWithValue("$key", key);
        using var r = c.ExecuteReader();
        if (!r.Read()) return null;
        try { var data = JsonSerializer.Deserialize<T>(r.GetString(0), Json); return data is null ? null : new(data, DateTimeOffset.Parse(r.GetString(1))); }
        catch (Exception e) when (e is JsonException or FormatException) { return null; }
    }
    public void Yaz<T>(string key, string kind, T value)
    {
        using var db = Ac(); using var c = db.CreateCommand();
        c.CommandText = "INSERT OR REPLACE INTO cache VALUES($key,$kind,$value,$date)";
        c.Parameters.AddWithValue("$key", key); c.Parameters.AddWithValue("$kind", kind);
        c.Parameters.AddWithValue("$value", JsonSerializer.Serialize(value)); c.Parameters.AddWithValue("$date", DateTimeOffset.UtcNow.ToString("O")); c.ExecuteNonQuery();
    }
    public T Ayar<T>(string key, T varsayilan)
    {
        using var db = Ac(); using var c = db.CreateCommand(); c.CommandText = "SELECT value FROM settings WHERE key=$key"; c.Parameters.AddWithValue("$key", key);
        var value = c.ExecuteScalar() as string;
        try { return value is null ? varsayilan : JsonSerializer.Deserialize<T>(value, Json) ?? varsayilan; }
        catch (JsonException) { return varsayilan; }
    }
    public void AyarYaz<T>(string key, T value)
    {
        using var db = Ac(); using var c = db.CreateCommand(); c.CommandText = "INSERT OR REPLACE INTO settings VALUES($key,$value)";
        c.Parameters.AddWithValue("$key", key); c.Parameters.AddWithValue("$value", JsonSerializer.Serialize(value)); c.ExecuteNonQuery();
    }
    public void Okundu(Makale makale)
    {
        using var db = Ac(); using var c = db.CreateCommand(); c.CommandText = "INSERT OR REPLACE INTO history VALUES($url,$title,$date)";
        c.Parameters.AddWithValue("$url", makale.Url); c.Parameters.AddWithValue("$title", makale.Title); c.Parameters.AddWithValue("$date", DateTimeOffset.UtcNow.ToString("O")); c.ExecuteNonQuery();
    }
    public bool Isaretle(Baslik baslik)
    {
        using var db = Ac(); using var c = db.CreateCommand();
        c.CommandText = "SELECT COUNT(*) FROM marks WHERE url=$url"; c.Parameters.AddWithValue("$url", baslik.Url);
        bool var = Convert.ToInt64(c.ExecuteScalar()) > 0;
        c.CommandText = var ? "DELETE FROM marks WHERE url=$url" : "INSERT INTO marks VALUES($url,$title)";
        if (!var) c.Parameters.AddWithValue("$title", baslik.Title); c.ExecuteNonQuery(); return !var;
    }
    public List<Baslik> Liste(bool gecmis = false)
    {
        using var db = Ac(); using var c = db.CreateCommand();
        c.CommandText = gecmis ? "SELECT title,url FROM history ORDER BY opened DESC LIMIT 100" : "SELECT title,url FROM marks ORDER BY title";
        using var r = c.ExecuteReader(); var result = new List<Baslik>(); while (r.Read()) result.Add(new(r.GetString(0), r.GetString(1))); return result;
    }
    public List<Baslik> Ara(string query)
    {
        using var db = Ac(); using var c = db.CreateCommand(); c.CommandText = "SELECT payload FROM cache WHERE kind='article'";
        using var r = c.ExecuteReader(); var result = new List<Baslik>(); var seen = new HashSet<string>();
        while (r.Read())
        {
            try
            {
                var m = JsonSerializer.Deserialize<Makale>(r.GetString(0), Json);
                if (m is not null && Turkce.Icerir(m.Title + " " + m.Text, query) && seen.Add(m.Url)) result.Add(new(m.Title, m.Url));
            }
            catch (JsonException) { }
        }
        return result;
    }
    public void Temizle(bool gecmis)
    {
        using var db = Ac(); using var c = db.CreateCommand(); c.CommandText = gecmis ? "DELETE FROM history" : "DELETE FROM cache"; c.ExecuteNonQuery();
    }
}
