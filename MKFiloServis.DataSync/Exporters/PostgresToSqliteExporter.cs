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
        await using var sourceSnapshot = await pg.BeginTransactionAsync(IsolationLevel.RepeatableRead);

        var sqliteConnString = new SqliteConnectionStringBuilder { DataSource = _sqlitePath }.ToString();
        await using var sqlite = new SqliteConnection(sqliteConnString);
        await sqlite.OpenAsync();
        await MKFiloServis.Shared.Auditing.DatabaseWriteAudit.EnsureAsync(sqlite);
        int foreignKeysBefore;
        int synchronousBefore;
        await using (var pragmaState = sqlite.CreateCommand())
        {
            pragmaState.CommandText = "PRAGMA foreign_keys;";
            foreignKeysBefore = Convert.ToInt32(await pragmaState.ExecuteScalarAsync());
            pragmaState.CommandText = "PRAGMA synchronous;";
            synchronousBefore = Convert.ToInt32(await pragmaState.ExecuteScalarAsync());
        }

        // 1) Hedef SQLite'da bulunan kullanici tablolarini listele (sqlite_% haric)
        var sqliteTables = await ListSqliteUserTablesAsync(sqlite);
        _progress($"▸ Hedef SQLite'da {sqliteTables.Count} tablo tespit edildi.");

        // 2) Kaynak PG'de public seması içindeki tabloları al
        var pgTables = await ListPostgresUserTablesAsync(pg);
        _progress($"▸ Kaynak PG'de {pgTables.Count} tablo tespit edildi.");

        // 3) Kesisim: iki tarafta da olan tablolar
        var ortakTablolar = sqliteTables.Intersect(pgTables, StringComparer.Ordinal).ToList();
        _progress($"▸ Kopyalanacak tablo sayisi: {ortakTablolar.Count}");

        // 3a) Sema on kosulu: Kaynakta var ama hedefte olmayan tablolar sessizce atlanir ve
        // veri kaybi olusturur. Once Web uygulamasi ile hedef semayi guncelleyerek tekrar deneyin.
        var hedefEksikTablolar = pgTables
            .Except(sqliteTables, StringComparer.Ordinal)
            .OrderBy(t => t, StringComparer.Ordinal)
            .ToList();
        if (hedefEksikTablolar.Count > 0)
        {
            foreach (var t in hedefEksikTablolar)
                Console.Error.WriteLine($"[UYARI] Kaynakta var ama hedefte eksik, ATLANDI: {t}");

            throw new InvalidOperationException(
                $"Hedef SQLite semasi gecersiz: {hedefEksikTablolar.Count} tablo eksik " +
                $"({string.Join(", ", hedefEksikTablolar)}). " +
                "Once MKFiloServis.Web uygulamasini baslatip semayi olusturun, sonra aktarimi tekrarlayin.");
        }

        // 4) Foreign key kontrollerini gecici kapat. Her hata yolunda bağlantıyı normal
        // FK kipine döndür; aksi halde havuzlanan bağlantı sonraki işlemlerde FK'siz kalabilir.
        long toplamSatir = 0;
        try
        {
            await using (var pragmaOff = sqlite.CreateCommand())
            {
                pragmaOff.CommandText = "PRAGMA foreign_keys = OFF; PRAGMA synchronous = FULL;";
                await pragmaOff.ExecuteNonQueryAsync();
            }

            await using var tx = (SqliteTransaction)await sqlite.BeginTransactionAsync();

            int tabloIndex = 0;
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
                    throw new InvalidOperationException($"{tablo} aktarımı başarısız; tüm hedef değişiklikleri geri alınacak.", ex);
                }
            }

            await ResetSqliteSequencesAsync(sqlite, tx);
            await using (var check = sqlite.CreateCommand())
            {
                check.Transaction = tx;
                check.CommandText = "PRAGMA foreign_key_check;";
                await using var reader = await check.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                    throw new InvalidOperationException($"SQLite yabancı anahtar ihlali: {reader.GetString(0)}. Aktarım geri alınacak.");
            }
            await tx.CommitAsync();
            await sourceSnapshot.CommitAsync();
        }
        finally
        {
            await using var pragmaOn = sqlite.CreateCommand();
            pragmaOn.CommandText = $"PRAGMA foreign_keys = {foreignKeysBefore}; PRAGMA synchronous = {synchronousBefore};";
            await pragmaOn.ExecuteNonQueryAsync();
        }

        _progress($"✔ Toplam {toplamSatir} satir kopyalandi.");
    }

    private static async Task<List<string>> ListSqliteUserTablesAsync(SqliteConnection conn)
    {
        var list = new List<string>();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' AND name NOT LIKE '__EFMigrations%' AND name <> '__MKWriteJournal' ORDER BY name;";
        await using var rdr = await cmd.ExecuteReaderAsync();
        while (await rdr.ReadAsync()) list.Add(rdr.GetString(0));
        if (list.Any(value => value.Contains('"')))
            throw new InvalidOperationException("Çift tırnak içeren şema kimlikleri aktarımda desteklenmiyor.");
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
        if (list.Any(value => value.Contains('"')))
            throw new InvalidOperationException("Çift tırnak içeren şema kimlikleri aktarımda desteklenmiyor.");
        return list;
    }

    private async Task<long> KopyalaTabloAsync(NpgsqlConnection pg, SqliteConnection sqlite, SqliteTransaction tx, string tablo)
    {
        // Hem kaynak hem hedef kolonlarini al, kesisimi kullan
        var sqliteKolonlar = await ListSqliteColumnsAsync(sqlite, tablo, tx);
        if (sqliteKolonlar.Count == 0) throw new InvalidOperationException($"{tablo}: hedef kolonları okunamadı.");

        var pgKolonlar = await ListPgColumnsAsync(pg, tablo);
        var ortakKolonlar = sqliteKolonlar.Intersect(pgKolonlar, StringComparer.Ordinal).ToList();
        var missing = pgKolonlar.Except(sqliteKolonlar, StringComparer.Ordinal).ToList();
        if (missing.Count > 0 || ortakKolonlar.Count == 0)
            throw new InvalidOperationException($"{tablo}: kaynak kolonlarının hedefte karşılığı yok: {string.Join(", ", missing)}");
        long expected;
        await using (var count = pg.CreateCommand())
        {
            count.CommandText = $"SELECT COUNT(*) FROM public.\"{tablo}\";";
            expected = Convert.ToInt64(await count.ExecuteScalarAsync());
        }

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
        if (sayac != expected) throw new InvalidOperationException($"{tablo}: kaynak satır sayısı değişti.");
        await using var targetCount = sqlite.CreateCommand();
        targetCount.Transaction = tx;
        targetCount.CommandText = $"SELECT COUNT(*) FROM \"{tablo}\";";
        if (Convert.ToInt64(await targetCount.ExecuteScalarAsync()) != expected)
            throw new InvalidOperationException($"{tablo}: hedef satır sayısı uyuşmuyor.");
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
            DateTime dt => dt.ToString("yyyy-MM-dd HH:mm:ss.fffffff", System.Globalization.CultureInfo.InvariantCulture),
            DateTimeOffset dto => dto.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss.fffffff", System.Globalization.CultureInfo.InvariantCulture),
            DateOnly date => date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
            TimeOnly time => time.ToString("HH:mm:ss.fffffff", System.Globalization.CultureInfo.InvariantCulture),
            TimeSpan ts => ts.ToString("c", System.Globalization.CultureInfo.InvariantCulture),
            bool b => b ? 1 : 0,
            Guid g => g.ToString(),
            byte[] ba => ba,
            _ => val
        };
    }

    private static async Task<List<string>> ListSqliteColumnsAsync(SqliteConnection conn, string tablo, SqliteTransaction tx)
    {
        var list = new List<string>();
        await using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = $"PRAGMA table_info(\"{tablo}\");";
        await using var rdr = await cmd.ExecuteReaderAsync();
        while (await rdr.ReadAsync()) list.Add(rdr.GetString(1));
        if (list.Any(value => value.Contains('"')))
            throw new InvalidOperationException("Çift tırnak içeren şema kimlikleri aktarımda desteklenmiyor.");
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
        if (list.Any(value => value.Contains('"')))
            throw new InvalidOperationException("Çift tırnak içeren şema kimlikleri aktarımda desteklenmiyor.");
        return list;
    }

    private static async Task ResetSqliteSequencesAsync(SqliteConnection conn, SqliteTransaction tx)
    {
        await using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND sql LIKE '%AUTOINCREMENT%';";
        var tables = new List<string>();
        await using (var reader = await cmd.ExecuteReaderAsync())
            while (await reader.ReadAsync()) tables.Add(reader.GetString(0));
        foreach (var table in tables)
        {
            var columns = await ListSqliteColumnsAsync(conn, table, tx);
            var id = columns.FirstOrDefault(c => c.Equals("Id", StringComparison.OrdinalIgnoreCase));
            if (id == null) throw new InvalidOperationException($"{table}: otomatik kimlik kolonu bulunamadı.");
            await using var reset = conn.CreateCommand();
            reset.Transaction = tx;
            reset.CommandText = $"DELETE FROM sqlite_sequence WHERE name=@name; INSERT INTO sqlite_sequence(name,seq) SELECT @name, COALESCE(MAX(\"{id}\"),0) FROM \"{table}\";";
            reset.Parameters.AddWithValue("name", table);
            await reset.ExecuteNonQueryAsync();
        }
    }
}
