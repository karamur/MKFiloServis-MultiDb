using KOAFiloServis.Shared.Entities;
using KOAFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KOAFiloServis.Web.Services;

/// <summary>
/// Hakediş → Muhasebe fişi entegrasyonu.
/// Her hakediş için 2 AYRI fiş oluşturur: GELIR + GIDER.
/// Bordro muhasebesinden BAĞIMSIZDIR.
/// Transaction güvenliklidir.
/// </summary>
public class HakedisMuhasebeService : IHakedisMuhasebeService
{
    private readonly IDbContextFactory<ApplicationDbContext> _cf;
    private readonly IMuhasebeService _ms;
    private readonly ILogger<HakedisMuhasebeService> _logger;

    public HakedisMuhasebeService(IDbContextFactory<ApplicationDbContext> cf, IMuhasebeService ms, ILogger<HakedisMuhasebeService> logger)
    {
        _cf = cf;
        _ms = ms;
        _logger = logger;
    }

    public async Task MuhasebeyeAktarAsync(int hakedisId)
    {
        await using var tempContext = await _cf.CreateDbContextAsync();
        var strategy = tempContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var context = await _cf.CreateDbContextAsync();

            var hakedis = await context.HakedisPuantajlar
                .Include(h => h.Cari)
                .FirstOrDefaultAsync(h => h.Id == hakedisId && !h.IsDeleted);

            if (hakedis == null)
                throw new InvalidOperationException("Hakediş bulunamadı.");
            if (hakedis.Durum != HakedisDurumu.Onaylandi)
                throw new InvalidOperationException("Sadece onaylanmış hakediş muhasebeye aktarılabilir.");
            if (hakedis.MuhasebeyeAktarildiMi)
                throw new InvalidOperationException("Bu hakediş zaten muhasebeye aktarılmış.");

            _logger.LogInformation("Hakediş muhasebe aktarım başladı. HakedisId={HakedisId}", hakedisId);

            // ── Ayarları DB'den al ──
            var ayar = await context.MuhasebeAyarlari.FirstOrDefaultAsync();
            var gelirHesapKodu = !string.IsNullOrWhiteSpace(ayar?.SatisGelirHesabi) ? ayar!.SatisGelirHesabi : "600.01";
            var giderHesapKodu = !string.IsNullOrWhiteSpace(ayar?.AlisGiderHesabi) ? ayar!.AlisGiderHesabi : "740.01";
            var musteriPrefix = ayar?.MusteriPrefix ?? "120.01";
            var tedarikciPrefix = ayar?.TedarikciPrefix ?? "320.01";
            var hesaplananKdv = !string.IsNullOrWhiteSpace(ayar?.HesaplananKdvHesabi) ? ayar!.HesaplananKdvHesabi : "391.01";
            var indirilecekKdv = !string.IsNullOrWhiteSpace(ayar?.IndirilecekKdvHesabi) ? ayar!.IndirilecekKdvHesabi : "191.01";

            // ── Hesapları bul ──
            var musteriHesap = await FindHesap(context, musteriPrefix);
            var tedarikciHesap = await FindHesap(context, tedarikciPrefix);
            var gelirHesap = await FindHesap(context, gelirHesapKodu);
            var giderHesap = await FindHesap(context, giderHesapKodu);
            var hkdvHesap = await FindHesap(context, hesaplananKdv);
            var ikdvHesap = await FindHesap(context, indirilecekKdv);

            var fisTarihi = new DateTime(hakedis.Yil, hakedis.Ay, DateTime.DaysInMonth(hakedis.Yil, hakedis.Ay));
            var donemAdi = $"{hakedis.Ay}/{hakedis.Yil}";

            await using var tx = await context.Database.BeginTransactionAsync();

            try
            {
                // ═══════ GELİR FİŞİ ═══════
                var gfNo = await _ms.GenerateNextFisNoAsync(FisTipi.Mahsup, hakedis.FirmaId ?? 0);
                var gf = new MuhasebeFis
                {
                    FisNo = gfNo, FisTarihi = fisTarihi, FisTipi = FisTipi.Mahsup,
                    Aciklama = $"Hakediş Gelir — {donemAdi} / {hakedis.Cari?.Unvan}",
                    ToplamBorc = hakedis.TahsilEdilecekTutar,
                    ToplamAlacak = hakedis.TahsilEdilecekTutar,
                    Durum = FisDurum.Onaylandi, Kaynak = FisKaynak.Otomatik,
                    KaynakTip = "HakedisGelir", KaynakId = hakedis.Id, CreatedAt = DateTime.UtcNow
                };
                context.MuhasebeFisleri.Add(gf);
                await context.SaveChangesAsync();

                int s = 0;
                // 120 BORÇ — Müşteriden alacak
                context.MuhasebeFisKalemleri.Add(new MuhasebeFisKalem { FisId = gf.Id, HesapId = musteriHesap.Id, SiraNo = ++s, Borc = hakedis.TahsilEdilecekTutar, Alacak = 0, Tarih = fisTarihi, CariId = hakedis.CariId, Aciklama = "Hakediş tahsilat", CreatedAt = DateTime.UtcNow });
                // 600 ALACAK — Gelir
                context.MuhasebeFisKalemleri.Add(new MuhasebeFisKalem { FisId = gf.Id, HesapId = gelirHesap.Id, SiraNo = ++s, Borc = 0, Alacak = hakedis.GelirToplam, Tarih = fisTarihi, Aciklama = "Hakediş gelir", CreatedAt = DateTime.UtcNow });
                // 391 ALACAK — Hesaplanan KDV
                if (hakedis.GelirKdvTutari > 0)
                    context.MuhasebeFisKalemleri.Add(new MuhasebeFisKalem { FisId = gf.Id, HesapId = hkdvHesap.Id, SiraNo = ++s, Borc = 0, Alacak = hakedis.GelirKdvTutari, Tarih = fisTarihi, Aciklama = "Hesaplanan KDV", CreatedAt = DateTime.UtcNow });

                // ═══════ GİDER FİŞİ ═══════
                var gf2No = await _ms.GenerateNextFisNoAsync(FisTipi.Mahsup, hakedis.FirmaId ?? 0);
                var gf2 = new MuhasebeFis
                {
                    FisNo = gf2No, FisTarihi = fisTarihi, FisTipi = FisTipi.Mahsup,
                    Aciklama = $"Hakediş Gider — {donemAdi} / {hakedis.Cari?.Unvan}",
                    ToplamBorc = hakedis.OdenecekTutar,
                    ToplamAlacak = hakedis.OdenecekTutar,
                    Durum = FisDurum.Onaylandi, Kaynak = FisKaynak.Otomatik,
                    KaynakTip = "HakedisGider", KaynakId = hakedis.Id, CreatedAt = DateTime.UtcNow
                };
                context.MuhasebeFisleri.Add(gf2);
                await context.SaveChangesAsync();

                s = 0;
                // 740 BORÇ — Hizmet gideri
                context.MuhasebeFisKalemleri.Add(new MuhasebeFisKalem { FisId = gf2.Id, HesapId = giderHesap.Id, SiraNo = ++s, Borc = hakedis.GiderToplam, Alacak = 0, Tarih = fisTarihi, Aciklama = "Hakediş gider", CreatedAt = DateTime.UtcNow });
                // 191 BORÇ — İndirilecek KDV
                if (hakedis.KdvTutari > 0)
                    context.MuhasebeFisKalemleri.Add(new MuhasebeFisKalem { FisId = gf2.Id, HesapId = ikdvHesap.Id, SiraNo = ++s, Borc = hakedis.KdvTutari, Alacak = 0, Tarih = fisTarihi, Aciklama = "İndirilecek KDV", CreatedAt = DateTime.UtcNow });
                // 320 ALACAK — Tedarikçiye borç
                context.MuhasebeFisKalemleri.Add(new MuhasebeFisKalem { FisId = gf2.Id, HesapId = tedarikciHesap.Id, SiraNo = ++s, Borc = 0, Alacak = hakedis.OdenecekTutar, Tarih = fisTarihi, CariId = hakedis.CariId, Aciklama = $"Ödenecek — {hakedis.Cari?.Unvan}", CreatedAt = DateTime.UtcNow });

                // ── Hakedişi güncelle ──
                hakedis.GelirFisId = gf.Id;
                hakedis.GiderFisId = gf2.Id;
                hakedis.MuhasebeyeAktarildiMi = true;
                hakedis.UpdatedAt = DateTime.UtcNow;
                var affected = await context.SaveChangesAsync();

                if (affected <= 0)
                    throw new InvalidOperationException($"Hakediş muhasebe aktarım sonucu DB'ye yazılamadı. HakedisId={hakedisId}");

                await tx.CommitAsync();

                var kontrol = await context.HakedisPuantajlar
                    .AsNoTracking()
                    .FirstOrDefaultAsync(h => h.Id == hakedisId && !h.IsDeleted);

                if (kontrol == null || !kontrol.MuhasebeyeAktarildiMi || !kontrol.GelirFisId.HasValue || !kontrol.GiderFisId.HasValue)
                    throw new InvalidOperationException($"Muhasebe aktarım sonrası hakediş doğrulanamadı. HakedisId={hakedisId}");

                _logger.LogInformation("Hakediş muhasebe aktarım tamamlandı. HakedisId={HakedisId}, GelirFisId={GelirFisId}, GiderFisId={GiderFisId}",
                    hakedisId, kontrol.GelirFisId, kontrol.GiderFisId);
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "Hakediş muhasebe aktarım hatası. HakedisId={HakedisId}", hakedisId);
                throw;
            }
        });
    }

    private static async Task<MuhasebeHesap> FindHesap(ApplicationDbContext ctx, string kod)
    {
        var h = await ctx.MuhasebeHesaplari
            .FirstOrDefaultAsync(x => x.HesapKodu.StartsWith(kod) && !x.IsDeleted)
            ?? await ctx.MuhasebeHesaplari
                .FirstOrDefaultAsync(x => x.HesapKodu == kod && !x.IsDeleted);

        return h ?? throw new InvalidOperationException(
            $"Muhasebe hesabı bulunamadı: {kod}. Lütfen hesap planını kontrol edin.");
    }
}
