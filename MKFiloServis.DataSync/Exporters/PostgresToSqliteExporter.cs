using Microsoft.Data.Sqlite;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MKFiloServis.DataSync.Exporters;

/// <summary>
/// PostgreSQL veritabanindaki tum tablolari okuyup SQLite hedefine aktarir.
/// Sema hedef SQLite'da zaten olmalidir (Web uygulamasi ilk calistiginda
/// migration helper'larla olusturur). Bu sinif sadece VERI kopyalar.
/// </summary>
public sealed class PostgresToSqliteExporter
{
    private readonly string _pgConnectionString;
    private readonly string _sqlitePath;
    private readonly Action<string> _progress;

    public PostgresToSqliteExporter(string pgConnectionString, string sqlitePath, Action<string>? progress = null)
    {
        _pgConnectionString = pgConnectionString;
        _sqlitePath = sqlitePath;
        _progress = progress ?? (_ => { });
    }

    public async Task RunAsync()
    {
        if (!File.Exists(_sqlitePath))
        {
            throw new FileNotFoundException(
                $"Hedef SQLite veritabani bulunamadi: {_sqlitePath}. " +
                "Once MKFiloServis.Web uygulamasini bir kere baslatin ki sema olussun.");
        }

        _progress($"▸ Kaynak: PostgreSQL");
        _progress($"▸ Hedef : {_sqlitePath}");

        await using var pg = new NpgsqlConnection(_pgConnectionString);
        await pg.OpenAsync();

        var sqliteConnString = new SqliteConnectionStringBuilder { DataSource = _sqlitePath }.ToString();
        await using var sqlite = new SqliteConnection(sqliteConnString);
        await sqlite.OpenAsync();

        // 1) Hedef SQLite'da bulunan kullanici tablolarini listele (sqlite_% haric)
        var sqliteTables = await ListSqliteUserTablesAsync(sqlite);
        _progress($"▸ Hedef SQLite'da {sqliteTables.Count} tablo tespit edildi.");

        // 2) Kaynak PG'de public seması içindeki tabloları al
        var pgTables = await ListPostgresUserTablesAsync(pg);
        _progress($"▸ Kaynak PG'de {pgTables.Count} tablo tespit edildi.");

        // 3) Kesisim: iki tarafta da olan tablolar
        var ortakTablolar = sqliteTables.Intersect(pgTables, StringComparer.OrdinalIgnoreCase).ToList();
        _progress($"▸ Kopyalanacak tablo sayisi: {ortakTablolar.Count}");

        // 4) Foreign key kontrollerini gecici kapatalim ve her tabloyu temizleyip dolduralim
        await using (var pragmaOff = sqlite.CreateCommand())
        {
            pragmaOff.CommandText = "PRAGMA foreign_keys = OFF; PRAGMA journal_mode = MEMORY; PRAGMA synchronous = OFF;";
            await pragmaOff.ExecuteNonQueryAsync();
        }

        await using var tx = (SqliteTransaction)await sqlite.BeginTransactionAsync();

        int tabloIndex = 0;
        long toplamSatir = 0;
        foreach (var tablo in ortakTablolar)
        {
            tabloIndex++;
            try
            {
                // Mevcut satirlari temizle
                await using (var del = sqlite.CreateCommand())
                {
                    del.Transaction = tx;
                    del.CommandText = $"DELETE FROM \"{tablo}\";";
                    await del.ExecuteNonQueryAsync();
                }

                var kopyalanan = await KopyalaTabloAsync(pg, sqlite, tx, tablo);
                toplamSatir += kopyalanan;
                _progress($"  [{tabloIndex}/{ortakTablolar.Count}] {tablo}: {kopyalanan} satir");
            }
            catch (Exception ex)
            {
                _progress($"  ⚠ {tablo} atlandi: {ex.Message}");
            }
        }

        await tx.CommitAsync();

        // 5) sqlite_sequence reset (AUTOINCREMENT'li tablolar icin)
        await ResetSqliteSequencesAsync(sqlite);

        await using (var pragmaOn = sqlite.CreateCommand())
        {
            pragmaOn.CommandText = "PRAGMA foreign_keys = ON;";
            await pragmaOn.ExecuteNonQueryAsync();
        }

        _progress($"✔ Toplam {toplamSatir} satir kopyalandi.");
    }

    private static async Task<List<string>> ListSqliteUserTablesAsync(SqliteConnection conn)
    {
        var list = new List<string>();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' AND name NOT LIKE '__EFMigrations%' ORDER BY name;";
        await using var rdr = await cmd.ExecuteReaderAsync();
        while (await rdr.ReadAsync()) list.Add(rdr.GetString(0));
        return list;
    }

    private static async Task<List<string>> ListPostgresUserTablesAsync(NpgsqlConnection conn)
    {
        var list = new List<string>();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT table_name FROM information_schema.tables
                            WHERE table_schema = 'public' AND table_type = 'BASE TABLE'
                              AND table_name NOT LIKE '__EFMigrations%'
                            ORDER BY table_name;";
        await using var rdr = await cmd.ExecuteReaderAsync();
        while (await rdr.ReadAsync()) list.Add(rdr.GetString(0));
        return list;
    }

    private async Task<long> KopyalaTabloAsync(NpgsqlConnection pg, SqliteConnection sqlite, SqliteTransaction tx, string tablo)
    {
        // Hem kaynak hem hedef kolonlarini al, kesisimi kullan
        var sqliteKolonlar = await ListSqliteColumnsAsync(sqlite, tablo);
        if (sqliteKolonlar.Count == 0) return 0;

        var pgKolonlar = await ListPgColumnsAsync(pg, tablo);
        var ortakKolonlar = sqliteKolonlar.Intersect(pgKolonlar, StringComparer.OrdinalIgnoreCase).ToList();
        if (ortakKolonlar.Count == 0) return 0;

        var pgSelect = "SELECT " + string.Join(", ", ortakKolonlar.Select(c => $"\"{c}\"")) + $" FROM public.\"{tablo}\";";

        var insertSql = BuildInsertSql(tablo, ortakKolonlar);

        await using var pgCmd = pg.CreateCommand();
        pgCmd.CommandText = pgSelect;
        await using var rdr = await pgCmd.ExecuteReaderAsync();

        long sayac = 0;
        await using var ins = sqlite.CreateCommand();
        ins.Transaction = tx;
        ins.CommandText = insertSql;
        for (int i = 0; i < ortakKolonlar.Count; i++)
            ins.Parameters.Add(new SqliteParameter("@p" + i, DBNull.Value));

        while (await rdr.ReadAsync())
        {
            for (int i = 0; i < ortakKolonlar.Count; i++)
            {
                var val = rdr.IsDBNull(i) ? DBNull.Value : rdr.GetValue(i);
                ins.Parameters[i].Value = NormalizeValue(val);
            }
            await ins.ExecuteNonQueryAsync();
            sayac++;
        }
        return sayac;
    }

    private static string BuildInsertSql(string tablo, List<string> kolonlar)
    {
        var sb = new StringBuilder();
        sb.Append($"INSERT INTO \"{tablo}\" (");
        sb.Append(string.Join(", ", kolonlar.Select(k => $"\"{k}\"")));
        sb.Append(") VALUES (");
        sb.Append(string.Join(", ", kolonlar.Select((_, i) => "@p" + i)));
        sb.Append(");");
        return sb.ToString();
    }

    private static object NormalizeValue(object val)
    {
        return val switch
        {
            DateTime dt => dt.ToString("yyyy-MM-dd HH:mm:ss.fff"),
            DateTimeOffset dto => dto.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss.fff"),
            TimeSpan ts => ts.ToString(),
            bool b => b ? 1 : 0,
            Guid g => g.ToString(),
            byte[] ba => ba,
            _ => val
        };
    }

    private static async Task<List<string>> ListSqliteColumnsAsync(SqliteConnection conn, string tablo)
    {
        var list = new List<string>();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = $"PRAGMA table_info(\"{tablo}\");";
        await using var rdr = await cmd.ExecuteReaderAsync();
        while (await rdr.ReadAsync()) list.Add(rdr.GetString(1));
        return list;
    }

    private static async Task<List<string>> ListPgColumnsAsync(NpgsqlConnection conn, string tablo)
    {
        var list = new List<string>();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT column_name FROM information_schema.columns
                            WHERE table_schema = 'public' AND table_name = @t
                            ORDER BY ordinal_position;";
        cmd.Parameters.AddWithValue("@t", tablo);
        await using var rdr = await cmd.ExecuteReaderAsync();
        while (await rdr.ReadAsync()) list.Add(rdr.GetString(0));
        return list;
    }

    private static async Task ResetSqliteSequencesAsync(SqliteConnection conn)
    {
        // Her tablonun maksimum Id'sini sqlite_sequence'a yaz
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%';";
        var tablolar = new List<string>();
        await using (var rdr = await cmd.ExecuteReaderAsync())
            while (await rdr.ReadAsync()) tablolar.Add(rdr.GetString(0));

        foreach (var t in tablolar)
        {
            try
            {
                await using var chk = conn.CreateCommand();
                chk.CommandText = $"PRAGMA table_info(\"{t}\");";
                bool hasId = false;
                await using (var rdr = await chk.ExecuteReaderAsync())
                    while (await rdr.ReadAsync())
                        if (string.Equals(rdr.GetString(1), "Id", StringComparison.OrdinalIgnoreCase)) { hasId = true; break; }
                if (!hasId) continue;

                await using var max = conn.CreateCommand();
                max.CommandText = $"SELECT COALESCE(MAX(\"Id\"), 0) FROM \"{t}\";";
                var maxId = Convert.ToInt64(await max.ExecuteScalarAsync() ?? 0L);
                if (maxId <= 0) continue;

                await using var upd = conn.CreateCommand();
                upd.CommandText = "INSERT OR REPLACE INTO sqlite_sequence (name, seq) VALUES (@n, @s);";
                upd.Parameters.AddWithValue("@n", t);
                upd.Parameters.AddWithValue("@s", maxId);
                await upd.ExecuteNonQueryAsync();
            }
            catch { /* sqlite_sequence yoksa yoktur, sorun degil */ }
        }
    }
}


