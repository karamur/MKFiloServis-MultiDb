using MKFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace MKFiloServis.Web.Data.Migrations;

/// <summary>
/// Tenant Aşama C2: <see cref="MKFiloServis.Shared.Entities.IFirmaTenant"/> entity'lerinin
/// tablolarındaki NULL <c>FirmaId</c> satırlarını varsayılan firma ile doldurur.
/// <para>
/// Sıra (K9): nullable kolon ekle (Aşama C1) → bu helper ile doldur → sonraki aşamada
/// <c>IsRequired()</c> + NOT NULL migration. Bu sayede global tenant filter altında
/// eski kayıtlar görünmez olmaktan kurtulur.
/// </para>
/// <para>
/// İdempotent: her startup'ta güvenle çalışır. Sadece NULL kayıtları günceller.
/// </para>
/// </summary>
public static class TenantFirmaIdBackfillMigrationHelper
{
    // K9: yeni IFirmaTenant tablosu eklendikçe bu listeye eklenir.
    // (Cariler, Kurumlar, Guzergahlar, Personeller[Sofor], Araclar, BankaHesaplari, BankaKasaHareketleri)
    // Aşama C3 (Stok, MasrafKalemi, Fatura, ServisCalisma)
    private static readonly string[] FirmaTenantTablolari =
    [
        "Cariler",
        "Kurumlar",
        "Guzergahlar",
        "Personeller",
        "Araclar",
        "BankaHesaplari",
        "BankaKasaHareketleri",
        "StokKartlari",
        "StokKategoriler",
        "StokHareketler",
        "MasrafKalemleri",
        "Faturalar",
        "ServisCalismalari",
        "Hakedisler",
        "HakedisDetaylari",
        "GuzergahSeferleri",
        "Kapasiteler",
        "CariSeferUcretleri",
    ];

    public static async Task BackfillAsync(ApplicationDbContext context, ILogger logger)
    {
        // Varsayılan firma id'sini bul. Yoksa hiç firma yok demektir; backfill atlanır
        // (uygulama ilk kez ayağa kalkıyor olabilir, DbSeeder/SeedVarsayilanFirma sonradan firma açar).
        int? varsayilanFirmaId = null;
        try
        {
            varsayilanFirmaId = await context.Firmalar
                .IgnoreQueryFilters()
                .Where(f => f.VarsayilanFirma && f.Aktif)
                .Select(f => (int?)f.Id)
                .FirstOrDefaultAsync();

            // Varsayılan işaretli yoksa en eski aktif firmayı kullan.
            varsayilanFirmaId ??= await context.Firmalar
                .IgnoreQueryFilters()
                .Where(f => f.Aktif)
                .OrderBy(f => f.Id)
                .Select(f => (int?)f.Id)
                .FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            // Firmalar tablosu daha oluşmamış olabilir (ilk migration). Sessiz geç.
            logger.LogWarning(ex, "TenantC2 backfill: Firmalar tablosu okunamadı, atlanıyor.");
            return;
        }

        if (varsayilanFirmaId is null or 0)
        {
            logger.LogInformation("TenantC2 backfill: varsayılan firma bulunamadı, atlanıyor.");
            return;
        }

        var isNpgsql = context.Database.IsNpgsql();
        var isSqlite = context.Database.IsSqlite();

        foreach (var tablo in FirmaTenantTablolari)
        {
            try
            {
                int etkilenen;
                if (isNpgsql)
                {
                    var sql = $@"
                        DO $$
                        BEGIN
                            IF EXISTS (
                                SELECT 1 FROM information_schema.columns
                                WHERE table_name = '{tablo}' AND column_name = 'FirmaId'
                            ) THEN
                                UPDATE ""{tablo}"" SET ""FirmaId"" = {varsayilanFirmaId}
                                WHERE ""FirmaId"" IS NULL;
                            END IF;
                        END $$;";
                    etkilenen = await context.Database.ExecuteSqlRawAsync(sql);
                }
                else if (isSqlite)
                {
                    // SQLite'ta kolon yoksa hata fırlatır → kontrol edip atlayalım.
                    if (!await SqliteKolonVarMiAsync(context, tablo, "FirmaId"))
                    {
                        continue;
                    }

                    var sql = $"UPDATE \"{tablo}\" SET \"FirmaId\" = {varsayilanFirmaId} WHERE \"FirmaId\" IS NULL";
                    etkilenen = await context.Database.ExecuteSqlRawAsync(sql);
                }
                else
                {
                    // Diğer provider'lar için generic dene.
                    var sql = $"UPDATE [{tablo}] SET [FirmaId] = {varsayilanFirmaId} WHERE [FirmaId] IS NULL";
                    etkilenen = await context.Database.ExecuteSqlRawAsync(sql);
                }

                if (etkilenen > 0)
                {
                    logger.LogInformation(
                        "TenantC2 backfill: {Tablo} → {Etkilenen} kayıt FirmaId={FirmaId} ile dolduruldu.",
                        tablo, etkilenen, varsayilanFirmaId);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,
                    "TenantC2 backfill: {Tablo} güncellenirken hata oluştu, atlanıyor.", tablo);
            }
        }
    }

    private static async Task<bool> SqliteKolonVarMiAsync(ApplicationDbContext context, string tablo, string kolon)
    {
        var conn = context.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open)
        {
            await conn.OpenAsync();
        }

        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"PRAGMA table_info('{tablo}')";
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            if (string.Equals(reader.GetString(1), kolon, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }
}



