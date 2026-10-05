using Microsoft.Data.Sqlite;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MKFiloServis.DataSync.Exporters;

/// <summary>
/// SQLite veritabanindaki tum tablolari okuyup PostgreSQL hedefine aktarir.
/// Sema hedef PostgreSQL'de zaten olmalidir. Bu sinif sadece VERI kopyalar.
/// FK bakimindan session_replication_role=replica ile tek transaction kullanir,
/// aktarim sonrasi identity sequence'lari MAX(Id)'ye gore resetler.
/// </summary>
public sealed class SqliteToPostgresImporter
{
    private readonly string _sqlitePath;
    private readonly string _pgConnectionString;
    private readonly Action<string> _progress;

    public SqliteToPostgresImporter(string sqlitePath, string pgConnectionString, Action<string>? progress = null)
    {
        _sqlitePath = sqlitePath;
        _pgConnectionString = pgConnectionString;
        _progress = progress ?? (_ => { });
    }

    public async Task RunAsync()
    {
        if (!File.Exists(_sqlitePath))
            throw new FileNotFoundException($"Kaynak SQLite veritabani bulunamadi: {_sqlitePath}");

        _progress($"▸ Kaynak: {_sqlitePath}");
        _progress($"▸ Hedef : PostgreSQL");

        var sqliteConnString = new SqliteConnectionStringBuilder { DataSource = _sqlitePath, Mode = SqliteOpenMode.ReadOnly }.ToString();
        await using var sqlite = new SqliteConnection(sqliteConnString);
        await sqlite.OpenAsync();
        await using var sourceSnapshot = sqlite.BeginTransaction(deferred: true);

        await using var pg = new NpgsqlConnection(_pgConnectionString);
        await pg.OpenAsync();

        var sqliteTables = await ListSqliteUserTablesAsync(sqlite, sourceSnapshot);
        _progress($"▸ Kaynak SQLite'da {sqliteTables.Count} tablo tespit edildi.");

        var pgTables = await ListPostgresUserTablesAsync(pg);
        _progress($"▸ Hedef PG'de {pgTables.Count} tablo tespit edildi.");

        var ortakTablolar = sqliteTables
            .Where(t => pgTables.Contains(t, StringComparer.OrdinalIgnoreCase))
            .ToList();
        _progress($"▸ Kopyalanacak tablo sayisi: {ortakTablolar.Count}");

        // Sema on kosulu: Kaynakta var ama hedefte olmayan tablolar veri kaybi yaratir.
        // Sessizce atlanmak yerine islem durdurulur.
        var atlanan = sqliteTables.Except(ortakTablolar, StringComparer.OrdinalIgnoreCase)
            .OrderBy(t => t, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (atlanan.Count > 0)
        {
            foreach (var t in atlanan)
            {
                _progress($"  ⚠ PG'de karsiligi yok, ATLANDI: {t}");
                Console.Error.WriteLine($"[UYARI] Kaynakta var ama hedefte eksik, ATLANDI: {t}");
            }

            throw new InvalidOperationException(
                $"Hedef PostgreSQL semasi gecersiz: {atlanan.Count} tablo eksik " +
                $"({string.Join(", ", atlanan)}). Hedef semayi guncelleyip aktarimi tekrarlayin.");
        }

        await using (var sourceIntegrity = sqlite.CreateCommand())
        {
            sourceIntegrity.Transaction = sourceSnapshot;
            sourceIntegrity.CommandText = "PRAGMA foreign_key_check;";
            await using var reader = await sourceIntegrity.ExecuteReaderAsync();
            if (await reader.ReadAsync()) throw new InvalidDataException($"Kaynak SQLite yabancı anahtar ihlali: {reader.GetString(0)}");
        }
        await using var tx = await pg.BeginTransactionAsync(IsolationLevel.Serializable);
        if (pgTables.Count == 0) throw new InvalidOperationException("Hedefte aktarılabilir tablo bulunamadı.");
        await using (var tableLock = pg.CreateCommand())
        {
            tableLock.Transaction = tx;
            tableLock.CommandText = "LOCK TABLE " + string.Join(", ", pgTables.Select(t => "public." + Quote(t))) + " IN ACCESS EXCLUSIVE MODE";
            await tableLock.ExecuteNonQueryAsync();
        }

        // FK tetikleyicilerini bu oturum icin devre disi birak
        await using (var replica = pg.CreateCommand())
        {
            replica.Transaction = tx;
            replica.CommandText = "SET LOCAL session_replication_role = replica;";
            await replica.ExecuteNonQueryAsync();
        }

        var sonuclar = new List<(string Tablo, long Kaynak, long Kopyalanan)>();
        int tabloIndex = 0;
        long toplamSatir = 0;

        foreach (var tablo in ortakTablolar)
        {
            tabloIndex++;
            var pgTabloAdi = pgTables.First(t => t.Equals(tablo, StringComparison.OrdinalIgnoreCase));

            await using (var del = pg.CreateCommand())
            {
                del.Transaction = tx;
                del.CommandText = $"DELETE FROM public.\"{pgTabloAdi}\";";
                await del.ExecuteNonQueryAsync();
            }

            var (kaynakSatir, kopyalanan) = await KopyalaTabloAsync(sqlite, sourceSnapshot, pg, tx, tablo, pgTabloAdi);
            toplamSatir += kopyalanan;
            sonuclar.Add((pgTabloAdi, kaynakSatir, kopyalanan));
            _progress($"  [{tabloIndex}/{ortakTablolar.Count}] {pgTabloAdi}: {kopyalanan}/{kaynakSatir} satir");

            if (kaynakSatir != kopyalanan)
                throw new InvalidOperationException($"Satir sayisi uyusmuyor: {pgTabloAdi} kaynak={kaynakSatir}, kopyalanan={kopyalanan}");
        }

        await ValidateForeignKeysAsync(pg, tx);
        await ResetSequencesAsync(pg, tx, sonuclar.Select(s => s.Tablo).ToList());
        await tx.CommitAsync();
        await sourceSnapshot.CommitAsync();

        _progress($"✔ Toplam {toplamSatir} satir kopyalandi.");
    }

    private static async Task<List<string>> ListSqliteUserTablesAsync(SqliteConnection conn, SqliteTransaction sourceTx)
    {
        var list = new List<string>();
        await using var cmd = conn.CreateCommand();
        cmd.Transaction = sourceTx;
        cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' AND name NOT LIKE '__EFMigrations%' ORDER BY name;";
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

    private static async Task<List<string>> ListSqliteColumnsAsync(SqliteConnection conn, string tablo, SqliteTransaction sourceTx)
    {
        var list = new List<string>();
        await using var cmd = conn.CreateCommand();
        cmd.Transaction = sourceTx;
        cmd.CommandText = $"PRAGMA table_info(\"{tablo}\");";
        await using var rdr = await cmd.ExecuteReaderAsync();
        while (await rdr.ReadAsync()) list.Add(rdr.GetString(1));
        return list;
    }

    private sealed record PgColumn(string Name, string DataType, bool IsNullable);

    private static async Task<List<PgColumn>> ListPgColumnsAsync(NpgsqlConnection conn, NpgsqlTransaction tx, string tablo)
    {
        var list = new List<PgColumn>();
        await using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = @"SELECT column_name, data_type, is_nullable FROM information_schema.columns
                            WHERE table_schema = 'public' AND table_name = @t
                            ORDER BY ordinal_position;";
        cmd.Parameters.AddWithValue("t", tablo);
        await using var rdr = await cmd.ExecuteReaderAsync();
        while (await rdr.ReadAsync())
            list.Add(new PgColumn(rdr.GetString(0), rdr.GetString(1), rdr.GetString(2) == "YES"));
        if (list.Any(column => column.Name.Contains('"')))
            throw new InvalidOperationException("Çift tırnak içeren kolon kimlikleri desteklenmiyor.");
        return list;
    }

    private async Task<(long Kaynak, long Kopyalanan)> KopyalaTabloAsync(
        SqliteConnection sqlite, SqliteTransaction sourceTx, NpgsqlConnection pg, NpgsqlTransaction tx, string sqliteTablo, string pgTablo)
    {
        var sqliteKolonlar = await ListSqliteColumnsAsync(sqlite, sqliteTablo, sourceTx);
        var pgKolonlar = await ListPgColumnsAsync(pg, tx, pgTablo);

        var ortak = pgKolonlar
            .Where(p => sqliteKolonlar.Contains(p.Name, StringComparer.OrdinalIgnoreCase))
            .ToList();
        var missing = sqliteKolonlar.Where(c => !pgKolonlar.Any(p => p.Name.Equals(c, StringComparison.OrdinalIgnoreCase))).ToList();
        if (missing.Count > 0 || ortak.Count == 0)
            throw new InvalidOperationException($"{sqliteTablo}: hedefte kaynak kolonları eksik: {string.Join(", ", missing)}");

        // Kaynak satir sayisi
        long kaynakSatir;
        await using (var cnt = sqlite.CreateCommand())
        {
            cnt.Transaction = sourceTx;
            cnt.CommandText = $"SELECT COUNT(*) FROM \"{sqliteTablo}\";";
            kaynakSatir = Convert.ToInt64(await cnt.ExecuteScalarAsync(), CultureInfo.InvariantCulture);
        }
        if (kaynakSatir == 0) return (0, 0);

        var sqliteSelectKolonlari = ortak
            .Select(p => sqliteKolonlar.First(s => s.Equals(p.Name, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        var select = "SELECT " + string.Join(", ", sqliteSelectKolonlari.Select(c => $"\"{c}\"")) + $" FROM \"{sqliteTablo}\";";

        await using var srcCmd = sqlite.CreateCommand();
        srcCmd.Transaction = sourceTx;
        srcCmd.CommandText = select;
        await using var rdr = await srcCmd.ExecuteReaderAsync();

        var copyCmd = $"COPY public.\"{pgTablo}\" ({string.Join(", ", ortak.Select(c => $"\"{c.Name}\""))}) FROM STDIN (FORMAT BINARY)";
        long kopyalanan = 0;

        await using (var writer = await pg.BeginBinaryImportAsync(copyCmd))
        {
            while (await rdr.ReadAsync())
            {
                await writer.StartRowAsync();
                for (int i = 0; i < ortak.Count; i++)
                {
                    var deger = rdr.IsDBNull(i) ? null : rdr.GetValue(i);
                    await YazDegerAsync(writer, deger, ortak[i].DataType);
                }
                kopyalanan++;
            }
            await writer.CompleteAsync();
        }

        await using var targetCount = pg.CreateCommand();
        targetCount.Transaction = tx;
        targetCount.CommandText = $"SELECT COUNT(*) FROM public.{Quote(pgTablo)}";
        if (Convert.ToInt64(await targetCount.ExecuteScalarAsync(), CultureInfo.InvariantCulture) != kaynakSatir)
            throw new InvalidOperationException($"{pgTablo}: hedef satır sayısı uyuşmuyor.");
        return (kaynakSatir, kopyalanan);
    }

    private static async Task YazDegerAsync(NpgsqlBinaryImporter writer, object? deger, string pgDataType)
    {
        if (deger is null || deger is DBNull)
        {
            await writer.WriteNullAsync();
            return;
        }

        switch (pgDataType)
        {
            case "boolean":
                await writer.WriteAsync(ToBool(deger), NpgsqlDbType.Boolean);
                break;
            case "smallint":
                await writer.WriteAsync(Convert.ToInt16(deger, CultureInfo.InvariantCulture), NpgsqlDbType.Smallint);
                break;
            case "integer":
                await writer.WriteAsync(Convert.ToInt32(deger, CultureInfo.InvariantCulture), NpgsqlDbType.Integer);
                break;
            case "bigint":
                await writer.WriteAsync(Convert.ToInt64(deger, CultureInfo.InvariantCulture), NpgsqlDbType.Bigint);
                break;
            case "numeric":
                await writer.WriteAsync(ToDecimal(deger), NpgsqlDbType.Numeric);
                break;
            case "real":
                await writer.WriteAsync(Convert.ToSingle(deger, CultureInfo.InvariantCulture), NpgsqlDbType.Real);
                break;
            case "double precision":
                await writer.WriteAsync(Convert.ToDouble(deger, CultureInfo.InvariantCulture), NpgsqlDbType.Double);
                break;
            case "uuid":
                await writer.WriteAsync(Guid.Parse(Convert.ToString(deger, CultureInfo.InvariantCulture)!), NpgsqlDbType.Uuid);
                break;
            case "timestamp with time zone":
                await writer.WriteAsync(DateTime.SpecifyKind(ToDateTime(deger), DateTimeKind.Utc), NpgsqlDbType.TimestampTz);
                break;
            case "timestamp without time zone":
                await writer.WriteAsync(DateTime.SpecifyKind(ToDateTime(deger), DateTimeKind.Unspecified), NpgsqlDbType.Timestamp);
                break;
            case "date":
                await writer.WriteAsync(ToDateTime(deger).Date, NpgsqlDbType.Date);
                break;
            case "time without time zone":
                await writer.WriteAsync(ToTimeSpan(deger), NpgsqlDbType.Time);
                break;
            case "interval":
                await writer.WriteAsync(ToTimeSpan(deger), NpgsqlDbType.Interval);
                break;
            case "bytea":
                await writer.WriteAsync((byte[])deger, NpgsqlDbType.Bytea);
                break;
            case "jsonb":
                await writer.WriteAsync(Convert.ToString(deger, CultureInfo.InvariantCulture)!, NpgsqlDbType.Jsonb);
                break;
            case "json":
                await writer.WriteAsync(Convert.ToString(deger, CultureInfo.InvariantCulture)!, NpgsqlDbType.Json);
                break;
            default: // text, character varying vs.
                await writer.WriteAsync(Convert.ToString(deger, CultureInfo.InvariantCulture)!, NpgsqlDbType.Text);
                break;
        }
    }

    private static bool ToBool(object deger) => deger switch
    {
        bool b => b,
        long l => l != 0,
        int i => i != 0,
        string s => s == "1" || s.Equals("true", StringComparison.OrdinalIgnoreCase),
        _ => Convert.ToInt64(deger, CultureInfo.InvariantCulture) != 0
    };

    private static decimal ToDecimal(object deger) => deger switch
    {
        decimal d => d,
        string s => decimal.Parse(s, NumberStyles.Any, CultureInfo.InvariantCulture),
        _ => Convert.ToDecimal(deger, CultureInfo.InvariantCulture)
    };

    private static DateTime ToDateTime(object deger) => deger switch
    {
        DateTime dt => dt,
        string s => DateTime.Parse(s, CultureInfo.InvariantCulture, DateTimeStyles.None),
        long l => DateTime.FromBinary(l),
        _ => Convert.ToDateTime(deger, CultureInfo.InvariantCulture)
    };

    private static TimeSpan ToTimeSpan(object deger) => deger switch
    {
        TimeSpan ts => ts,
        string s => TimeSpan.Parse(s, CultureInfo.InvariantCulture),
        long l => TimeSpan.FromTicks(l),
        _ => throw new InvalidDataException("Aktarımda geçersiz zaman aralığı değeri.")
    };

    private static string Quote(string value) => "\"" + value.Replace("\"", "\"\"") + "\"";

    private static async Task ValidateForeignKeysAsync(NpgsqlConnection pg, NpgsqlTransaction tx)
    {
        await using var metadata = pg.CreateCommand();
        metadata.Transaction = tx;
        metadata.CommandText = @"SELECT c.conname, ns.nspname, t.relname, pns.nspname, pt.relname,
            c.confmatchtype::text,
            array_agg(a.attname::text ORDER BY keys.ord),
            array_agg(pa.attname::text ORDER BY keys.ord)
            FROM pg_constraint c
            JOIN pg_class t ON t.oid=c.conrelid JOIN pg_namespace ns ON ns.oid=t.relnamespace
            JOIN pg_class pt ON pt.oid=c.confrelid JOIN pg_namespace pns ON pns.oid=pt.relnamespace
            CROSS JOIN LATERAL unnest(c.conkey,c.confkey) WITH ORDINALITY AS keys(child,parent,ord)
            JOIN pg_attribute a ON a.attrelid=t.oid AND a.attnum=keys.child
            JOIN pg_attribute pa ON pa.attrelid=pt.oid AND pa.attnum=keys.parent
            WHERE c.contype='f' AND ns.nspname='public'
            GROUP BY c.oid,c.conname,ns.nspname,t.relname,pns.nspname,pt.relname,c.confmatchtype";
        var constraints = new List<(string Name, string Child, string Parent, string Match, string[] Columns, string[] ParentColumns)>();
        await using (var reader = await metadata.ExecuteReaderAsync())
            while (await reader.ReadAsync())
                constraints.Add((reader.GetString(0), Quote(reader.GetString(1))+"."+Quote(reader.GetString(2)),
                    Quote(reader.GetString(3))+"."+Quote(reader.GetString(4)), reader.GetString(5),
                    reader.GetFieldValue<string[]>(6), reader.GetFieldValue<string[]>(7)));
        foreach (var fk in constraints)
        {
            var allPresent = string.Join(" AND ", fk.Columns.Select(c => "child."+Quote(c)+" IS NOT NULL"));
            var someMissing = string.Join(" OR ", fk.Columns.Select(c => "child."+Quote(c)+" IS NULL"));
            var somePresent = string.Join(" OR ", fk.Columns.Select(c => "child."+Quote(c)+" IS NOT NULL"));
            var equal = string.Join(" AND ", fk.Columns.Select((c,i) => "child."+Quote(c)+"=parent."+Quote(fk.ParentColumns[i])));
            var partialNull = fk.Match == "f" ? $" OR (({someMissing}) AND ({somePresent}))" : "";
            await using var check = pg.CreateCommand();
            check.Transaction = tx;
            check.CommandText = $"SELECT EXISTS(SELECT 1 FROM {fk.Child} child WHERE (({allPresent}) AND NOT EXISTS(SELECT 1 FROM {fk.Parent} parent WHERE {equal})){partialNull})";
            if ((bool)(await check.ExecuteScalarAsync())!)
                throw new InvalidOperationException($"Yabancı anahtar ihlali: {fk.Name}. Aktarım geri alınacak.");
        }
    }

    private async Task ResetSequencesAsync(NpgsqlConnection pg, NpgsqlTransaction tx, List<string> tablolar)
    {
        _progress("▸ Sequence'lar resetleniyor...");
        foreach (var tablo in tablolar)
        {
            await using var cmd = pg.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = @"SELECT column_name FROM information_schema.columns
                                WHERE table_schema='public' AND table_name=@t
                                  AND (column_default LIKE 'nextval%' OR is_identity='YES');";
            cmd.Parameters.AddWithValue("t", tablo);

            var idKolonlari = new List<string>();
            await using (var rdr = await cmd.ExecuteReaderAsync())
            {
                while (await rdr.ReadAsync()) idKolonlari.Add(rdr.GetString(0));
            }

            foreach (var kolon in idKolonlari)
            {
                await using var sequence = pg.CreateCommand();
                sequence.Transaction = tx;
                sequence.CommandText = "SELECT quote_ident(n.nspname)||'.'||quote_ident(c.relname) FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace WHERE c.oid=pg_get_serial_sequence(@table,@column)::regclass";
                sequence.Parameters.AddWithValue("table", "public." + Quote(tablo));
                sequence.Parameters.AddWithValue("column", kolon);
                var sequenceName = (string?)await sequence.ExecuteScalarAsync()
                    ?? throw new InvalidOperationException($"{tablo}.{kolon}: sequence bulunamadı.");
                await using var maximum = pg.CreateCommand();
                maximum.Transaction = tx;
                maximum.CommandText = $"SELECT GREATEST(COALESCE(MAX({Quote(kolon)}),0),0)+1 FROM public.{Quote(tablo)}";
                var next = Convert.ToInt64(await maximum.ExecuteScalarAsync(), CultureInfo.InvariantCulture);
                await using var reset = pg.CreateCommand();
                reset.Transaction = tx;
                reset.CommandText = $"ALTER SEQUENCE {sequenceName} RESTART WITH {next.ToString(CultureInfo.InvariantCulture)}";
                await reset.ExecuteNonQueryAsync();
            }
        }
        _progress("▸ Sequence reset tamamlandi.");
    }
}
