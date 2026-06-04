using KOAFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace KOAFiloServis.Web.Services;

/// <summary>
/// Firma bazlı numara serisi üretici (Kural 15).
/// </summary>
/// <remarks>
/// <para>
/// Her firma kendi belge numaralarını bağımsız üretir.
/// Örnek: FAT-2026-00001 her firmada ayrı sayılır.
/// </para>
/// <para>
/// PostgreSQL <c>INSERT ... ON CONFLICT DO UPDATE ... RETURNING</c> ile
/// atomik, lock-free numara üretimi sağlar. Multi-instance deployment'da
/// dahi race-condition oluşmaz.
/// </para>
/// <para>
/// Kullanım: <c>NumaraSerisiService</c>'i DI'dan al, <c>GenerateNextAsync</c> çağır.
/// </para>
/// </remarks>
public class NumaraSerisiService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;

    public NumaraSerisiService(IDbContextFactory<ApplicationDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    /// <summary>
    /// Belirtilen prefix, firma ve dönem için sıradaki numarayı atomik üretir.
    /// </summary>
    /// <param name="prefix">Numara ön eki (örn: "FAT", "MH", "SF", "STK")</param>
    /// <param name="firmaId">Firma Id (Kural 15: her firma bağımsız seri)</param>
    /// <param name="yilAy">Opsiyonel dönem (null = bu ay). Format: "202606"</param>
    /// <returns>Sıradaki numara (1'den başlar)</returns>
    public async Task<int> GenerateNextAsync(string prefix, int firmaId, string? yilAy = null)
    {
        yilAy ??= DateTime.UtcNow.ToString("yyyyMM");

        await using var ctx = await _dbFactory.CreateDbContextAsync();
        var conn = ctx.Database.GetDbConnection();
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO ""FisNoCounters"" (""Prefix"", ""FirmaId"", ""YilAy"", ""SonNo"")
            VALUES (@prefix, @firmaId, @yilAy, 1)
            ON CONFLICT (""Prefix"", ""FirmaId"", ""YilAy"")
            DO UPDATE SET ""SonNo"" = ""FisNoCounters"".""SonNo"" + 1
            RETURNING ""SonNo"";";

        cmd.Parameters.Add(new NpgsqlParameter<string>("prefix", prefix));
        cmd.Parameters.Add(new NpgsqlParameter<int>("firmaId", firmaId));
        cmd.Parameters.Add(new NpgsqlParameter<string>("yilAy", yilAy));

        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    /// <summary>
    /// Belirtilen prefix ve firma için formatlı numara üretir.
    /// </summary>
    /// <param name="prefix">Ön ek (örn: "SF" → "SF-2026-000001")</param>
    /// <param name="firmaId">Firma Id</param>
    /// <param name="digitCount">Basamak sayısı (varsayılan 6)</param>
    /// <returns>Formatlanmış numara: "SF-2026-000001"</returns>
    public async Task<string> GenerateFormattedAsync(string prefix, int firmaId, int digitCount = 6)
    {
        var yil = DateTime.UtcNow.Year;
        var yilAy = DateTime.UtcNow.ToString("yyyyMM");
        var sonNo = await GenerateNextAsync(prefix, firmaId, yilAy);
        var format = new string('0', digitCount);
        return $"{prefix}-{yil}-{sonNo.ToString(format)}";
    }

    /// <summary>
    /// FisNoCounters tablosunun FirmaId kolonunu idempotent olarak ekler.
    /// Uygulama başlangıcında çağrılmalıdır.
    /// </summary>
    public async Task EnsureSchemaAsync()
    {
        await using var ctx = await _dbFactory.CreateDbContextAsync();
        var conn = ctx.Database.GetDbConnection();
        await conn.OpenAsync();

        // FirmaId kolonunu ekle (geriye dönük uyumlu)
        await using var cmd1 = conn.CreateCommand();
        cmd1.CommandText = @"
            DO $$ BEGIN
                ALTER TABLE ""FisNoCounters"" ADD COLUMN IF NOT EXISTS ""FirmaId"" integer NOT NULL DEFAULT 0;
            EXCEPTION WHEN duplicate_column THEN END; $$;";
        await cmd1.ExecuteNonQueryAsync();

        // Eski unique constraint'i kaldır (Prefix + YilAy)
        await using var cmd2 = conn.CreateCommand();
        cmd2.CommandText = @"
            DO $$ BEGIN
                ALTER TABLE ""FisNoCounters"" DROP CONSTRAINT IF EXISTS ""PK_FisNoCounters"";
            EXCEPTION WHEN undefined_object THEN END; $$;";
        await cmd2.ExecuteNonQueryAsync();

        // Yeni composite PK ekle (Prefix + FirmaId + YilAy)
        await using var cmd3 = conn.CreateCommand();
        cmd3.CommandText = @"
            DO $$ BEGIN
                ALTER TABLE ""FisNoCounters"" ADD PRIMARY KEY (""Prefix"", ""FirmaId"", ""YilAy"");
            EXCEPTION WHEN duplicate_table THEN END; $$;";
        await cmd3.ExecuteNonQueryAsync();
    }
}
