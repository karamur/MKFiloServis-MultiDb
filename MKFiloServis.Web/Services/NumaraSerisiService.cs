using MKFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace MKFiloServis.Web.Services;

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

        //cmd.Parameters.Add(new NpgsqlParameter<string>("prefix", prefix));
        //cmd.Parameters.Add(new NpgsqlParameter<int>("firmaId", firmaId));
        //cmd.Parameters.Add(new NpgsqlParameter<string>("yilAy", yilAy));

        // create provider-agnostic parameters
        var p1 = cmd.CreateParameter();
        p1.ParameterName = "@prefix";
        p1.DbType = System.Data.DbType.String;
        p1.Value = prefix;
        cmd.Parameters.Add(p1);

        var p2 = cmd.CreateParameter();
        p2.ParameterName = "@firmaId";
        p2.DbType = System.Data.DbType.Int32;
        p2.Value = firmaId;
        cmd.Parameters.Add(p2);

        var p3 = cmd.CreateParameter();
        p3.ParameterName = "@yilAy";
        p3.DbType = System.Data.DbType.String;
        p3.Value = yilAy;
        cmd.Parameters.Add(p3);

        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    /// <summary>
    /// Belirtilen prefix ve firma için formatlı numara üretir.
    /// </summary>
    /// <remarks>
    /// Numara yalnız yılı içerdiği için sayaç da <b>yıllıktır</b>. Eskiden sayaç aylık tutuluyordu: her ay 1'den
    /// başlayıp önceki aylarda verilmiş numaraları ("PRF-2026-00001") yeniden üretiyor, benzersiz indeksli belgeler
    /// (proforma, fatura) "An error occurred while saving the entity changes" ile kaydedilemiyordu.
    /// Yıllık sayaç ilk kullanımda aynı yılın aylık sayaçlarının en büyüğünden devam eder; böylece geçiş sırasında
    /// verilmiş hiçbir numara tekrarlanmaz.
    /// </remarks>
    /// <param name="prefix">Ön ek (örn: "SF" → "SF-2026-000001")</param>
    /// <param name="firmaId">Firma Id</param>
    /// <param name="digitCount">Basamak sayısı (varsayılan 6)</param>
    /// <returns>Formatlanmış numara: "SF-2026-000001"</returns>
    public async Task<string> GenerateFormattedAsync(string prefix, int firmaId, int digitCount = 6)
    {
        var yil = DateTime.UtcNow.Year;
        var sonNo = await GenerateNextForYearAsync(prefix, firmaId, yil);
        var format = new string('0', digitCount);
        return $"{prefix}-{yil}-{sonNo.ToString(format)}";
    }

    private async Task<int> GenerateNextForYearAsync(string prefix, int firmaId, int yil)
    {
        await using var ctx = await _dbFactory.CreateDbContextAsync();
        var conn = ctx.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open)
            await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        // Yıllık satır ("2026") yoksa aynı yılın aylık satırlarının ("202608"…) en büyük SonNo'sundan başlar;
        // varsa ON CONFLICT bir artırır. İki eşzamanlı ilk çağrıda biri ekler, diğeri artırır: numaralar çakışmaz.
        cmd.CommandText = @"
            INSERT INTO ""FisNoCounters"" (""Prefix"", ""FirmaId"", ""YilAy"", ""SonNo"")
            VALUES (@prefix, @firmaId, @yil,
                    COALESCE((SELECT MAX(""SonNo"") FROM ""FisNoCounters""
                              WHERE ""Prefix"" = @prefix AND ""FirmaId"" = @firmaId
                                AND ""YilAy"" LIKE @aylar AND LENGTH(""YilAy"") = 6), 0) + 1)
            ON CONFLICT (""Prefix"", ""FirmaId"", ""YilAy"")
            DO UPDATE SET ""SonNo"" = ""FisNoCounters"".""SonNo"" + 1
            RETURNING ""SonNo"";";

        void Ekle(string ad, System.Data.DbType tur, object deger)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = ad;
            p.DbType = tur;
            p.Value = deger;
            cmd.Parameters.Add(p);
        }

        Ekle("@prefix", System.Data.DbType.String, prefix);
        Ekle("@firmaId", System.Data.DbType.Int32, firmaId);
        Ekle("@yil", System.Data.DbType.String, yil.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Ekle("@aylar", System.Data.DbType.String, yil.ToString(System.Globalization.CultureInfo.InvariantCulture) + "%");

        var sonuc = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(sonuc);
    }

}


