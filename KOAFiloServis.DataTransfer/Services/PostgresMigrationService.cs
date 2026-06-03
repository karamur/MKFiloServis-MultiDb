using System.Data;
using KOAFiloServis.DataTransfer.Models;
using Npgsql;

namespace KOAFiloServis.DataTransfer.Services;

public class PostgresMigrationService
{
    private static readonly HashSet<string> MasterTables = new(StringComparer.OrdinalIgnoreCase)
        { "Firmalar", "Kullanicilar", "Lisanslar", "Roller", "RolYetkileri", "AppAyarlari" };

    private static readonly string[] FirmaColumns =
        ["Id", "FirmaKodu", "FirmaAdi", "Aktif", "VarsayilanFirma", "SiraNo",
         "AktifDonemYil", "AktifDonemAy", "CreatedAt", "IsDeleted", "DatabaseName"];

    public async Task MigrateAsync(
        ConnectionInfo source, ConnectionInfo target,
        int firmaId,
        IProgress<MigrationProgress> progress,
        CancellationToken ct,
        bool step0 = true, bool step1 = true, bool step2 = true, bool resetSeq = true)
    {
        var sourceConnStr = source.BuildConnectionString();
        var targetConnStr = target.BuildConnectionString();

        await using var sourceConn = new NpgsqlConnection(sourceConnStr);
        await sourceConn.OpenAsync(ct);
        await using var targetConn = new NpgsqlConnection(targetConnStr);
        await targetConn.OpenAsync(ct);

        // FK'ları geçici devre dışı bırak
        await using (var disableCmd = new NpgsqlCommand("SET session_replication_role = 'replica';", targetConn))
            await disableCmd.ExecuteNonQueryAsync(ct);

        // Tablo listelerini al
        var sourceTables = await GetTableListAsync(sourceConn, ct);
        var targetTables = await GetTableListAsync(targetConn, ct);

        var allTables = targetTables
            .Where(t => !MasterTables.Contains(t) || t == "Firmalar")
            .OrderBy(t => t)
            .ToList();

        var total = allTables.Count;
        var processed = 0;

        // ─── Adim 0: Firma kaydı ───
        if (step0)
        {
            Report(progress, "Adim 0: Firma kaydı...", 0, total, processed);
            await CopyFirmaRecordAsync(sourceConn, targetConn, firmaId, ct);
            Report(progress, "Adim 0: Firma kaydı tamam", 0, total, processed);
        }

        // ─── Adim 1: Lookup tabloları (FirmaId'siz) ───
        if (step1)
        {
            Report(progress, "Adim 1: Lookup tabloları (FirmaId'siz)...", 0, total, processed);
            foreach (var table in allTables)
            {
                if (MasterTables.Contains(table)) continue;
                if (await HasColumnAsync(targetConn, table, "FirmaId", ct)) continue;
                if (!sourceTables.Contains(table)) continue;

                var rows = await CopyTableDataAsync(sourceConn, targetConn, table, null, ct);
                processed++;
                if (rows > 0)
                    Report(progress, $"  {table}: {rows} satır", rows, total, processed);
            }
            Report(progress, "Adim 1: Tamamlandı", 0, total, processed);
        }

        // ─── Adim 2: Tenant tabloları (FirmaId'li) ───
        if (step2)
        {
            Report(progress, $"Adim 2: Tenant tabloları (FirmaId={firmaId})...", 0, total, processed);
            foreach (var table in allTables)
            {
                if (MasterTables.Contains(table)) continue;
                if (!await HasColumnAsync(targetConn, table, "FirmaId", ct)) continue;
                if (!sourceTables.Contains(table)) continue;

                var rows = await CopyTableDataAsync(sourceConn, targetConn, table, firmaId, ct);
                processed++;
                if (rows > 0)
                    Report(progress, $"  {table}: {rows} satır (FirmaId={firmaId})", rows, total, processed);
            }
            Report(progress, "Adim 2: Tamamlandı", 0, total, processed);
        }

        // ─── FK'ları tekrar aktif et ───
        await using (var enableCmd = new NpgsqlCommand("SET session_replication_role = 'origin';", targetConn))
            await enableCmd.ExecuteNonQueryAsync(ct);

        // ─── Sequence reset ───
        if (resetSeq)
        {
            Report(progress, "Sequence reset...", 0, total, processed);
            await ResetAllSequencesAsync(targetConn, ct);
            Report(progress, "Sequence reset: Tamamlandı", 0, total, processed);
        }

        Report(progress, "MIGRASYON TAMAMLANDI!", 0, total, total, completed: true);
    }

    private async Task CopyFirmaRecordAsync(NpgsqlConnection source, NpgsqlConnection target, int firmaId, CancellationToken ct)
    {
        var columns = string.Join("\", \"", FirmaColumns);
        var sql = $@"SELECT ""Id"", ""FirmaKodu"", ""FirmaAdi"", ""Aktif"", ""VarsayilanFirma"", ""SiraNo"",
                            ""AktifDonemYil"", ""AktifDonemAy"", ""CreatedAt"", ""IsDeleted"", ""DatabaseName""
                     FROM ""Firmalar"" WHERE ""Id"" = @fid";

        await using var cmd = new NpgsqlCommand(sql, source);
        cmd.Parameters.AddWithValue("@fid", firmaId);
        await using var reader = await cmd.ExecuteReaderAsync(ct);

        if (!await reader.ReadAsync(ct)) return;

        var insertSql = $@"INSERT INTO ""Firmalar"" (""{columns}"") VALUES
            (@p0,@p1,@p2,@p3,@p4,@p5,@p6,@p7,@p8,@p9,@p10)
            ON CONFLICT (""Id"") DO UPDATE SET
              ""FirmaKodu""=EXCLUDED.""FirmaKodu"", ""FirmaAdi""=EXCLUDED.""FirmaAdi"",
              ""DatabaseName""=EXCLUDED.""DatabaseName"",
              ""AktifDonemYil""=EXCLUDED.""AktifDonemYil"",
              ""AktifDonemAy""=EXCLUDED.""AktifDonemAy""";

        await using var insertCmd = new NpgsqlCommand(insertSql, target);
        for (int i = 0; i < FirmaColumns.Length; i++)
            insertCmd.Parameters.AddWithValue($"@p{i}", reader.IsDBNull(i) ? DBNull.Value : reader.GetValue(i));

        await reader.CloseAsync();
        await insertCmd.ExecuteNonQueryAsync(ct);
    }

    private async Task<int> CopyTableDataAsync(
        NpgsqlConnection source, NpgsqlConnection target,
        string table, int? firmaId, CancellationToken ct)
    {
        try
        {
            var targetCols = await GetColumnNamesAsync(target, table, ct);
            if (targetCols.Count == 0) return 0;

            var sql = firmaId.HasValue
                ? $"SELECT * FROM \"{table}\" WHERE \"FirmaId\" = @fid"
                : $"SELECT * FROM \"{table}\"";

            await using var readCmd = new NpgsqlCommand(sql, source);
            if (firmaId.HasValue) readCmd.Parameters.AddWithValue("@fid", firmaId.Value);

            await using var reader = await readCmd.ExecuteReaderAsync(ct);

            var sourceCols = new List<string>();
            for (int i = 0; i < reader.FieldCount; i++)
                sourceCols.Add(reader.GetName(i));

            var common = sourceCols.Where(c => targetCols.Contains(c)).ToList();
            if (common.Count == 0) return 0;

            var inserted = 0;
            while (await reader.ReadAsync(ct))
            {
                var colNames = string.Join("\", \"", common);
                var paramNames = string.Join(", ", common.Select((_, i) => $"@p{i}"));
                var insertSql = $"INSERT INTO \"{table}\" (\"{colNames}\") VALUES ({paramNames}) ON CONFLICT DO NOTHING";

                await using var insertCmd = new NpgsqlCommand(insertSql, target);
                for (int i = 0; i < common.Count; i++)
                {
                    var srcIdx = sourceCols.IndexOf(common[i]);
                    insertCmd.Parameters.AddWithValue($"@p{i}", reader.IsDBNull(srcIdx) ? DBNull.Value : reader.GetValue(srcIdx));
                }

                try { await insertCmd.ExecuteNonQueryAsync(ct); inserted++; }
                catch { /* skip row errors */ }
            }
            await reader.CloseAsync();
            return inserted;
        }
        catch (PostgresException ex) when (ex.SqlState == "42P01") { return 0; }
        catch { return 0; }
    }

    private async Task ResetAllSequencesAsync(NpgsqlConnection conn, CancellationToken ct)
    {
        var sql = """
            DO $$
            DECLARE r RECORD; max_id BIGINT;
            BEGIN
              FOR r IN
                SELECT table_name FROM information_schema.columns
                WHERE table_schema='public' AND column_name='Id'
                  AND column_default LIKE 'nextval%'
              LOOP
                EXECUTE format('SELECT COALESCE(MAX("Id"),0) FROM %I', r.table_name) INTO max_id;
                IF max_id > 0 THEN
                  EXECUTE format('SELECT setval(pg_get_serial_sequence(''%I'',''Id''), %s, true)',
                                 r.table_name, max_id);
                END IF;
              END LOOP;
            END $$;
            """;
        await using var cmd = new NpgsqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private static async Task<HashSet<string>> GetTableListAsync(NpgsqlConnection conn, CancellationToken ct)
    {
        var tables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using var cmd = new NpgsqlCommand(
            "SELECT table_name FROM information_schema.tables WHERE table_schema='public' AND table_type='BASE TABLE'", conn);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct)) tables.Add(reader.GetString(0));
        return tables;
    }

    private static async Task<HashSet<string>> GetColumnNamesAsync(NpgsqlConnection conn, string table, CancellationToken ct)
    {
        var cols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using var cmd = new NpgsqlCommand(
            "SELECT column_name FROM information_schema.columns WHERE table_name=@t AND table_schema='public'", conn);
        cmd.Parameters.AddWithValue("@t", table);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct)) cols.Add(reader.GetString(0));
        return cols;
    }

    private static async Task<bool> HasColumnAsync(NpgsqlConnection conn, string table, string column, CancellationToken ct)
    {
        await using var cmd = new NpgsqlCommand(
            "SELECT EXISTS(SELECT 1 FROM information_schema.columns WHERE table_name=@t AND column_name=@c)", conn);
        cmd.Parameters.AddWithValue("@t", table);
        cmd.Parameters.AddWithValue("@c", column);
        return (bool)(await cmd.ExecuteScalarAsync(ct))!;
    }

    private void Report(IProgress<MigrationProgress> progress, string msg, int rows, int total, int processed, bool completed = false)
    {
        progress.Report(new MigrationProgress
        {
            Mesaj = msg,
            SatirSayisi = rows,
            ToplamTablo = total,
            IslenenTablo = processed,
            Tamamlandi = completed
        });
    }
}
