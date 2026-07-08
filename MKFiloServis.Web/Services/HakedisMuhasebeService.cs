using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Web.Services;

/// <summary>
/// Yeni hakediş modeli (Hakedis) → Muhasebe fişi entegrasyonu.
/// Kurum hakedişinde GELİR, Tedarikçi hakedişinde GİDER fişi oluşturur.
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

            var hakedis = await context.Hakedisler
                .Include(h => h.Fatura)
                .FirstOrDefaultAsync(h => h.Id == hakedisId && !h.IsDeleted)
                ?? throw new InvalidOperationException("Hakediş bulunamadı.");

            if (hakedis.FirmaId is null or <= 0)
                throw new InvalidOperationException("Hakediş için FirmaId zorunludur.");

            if (hakedis.Durum is not (HakedisDurum.Onaylandi or HakedisDurum.Faturalandi))
                throw new InvalidOperationException("Sadece onaylı veya faturalanmış hakediş muhasebeye aktarılabilir.");

            if (hakedis.Tip == HakedisTipi.Arac)
                throw new InvalidOperationException("Araç tipi hakediş muhasebeye aktarılmaz.");

            var zatenVar = await context.MuhasebeFisleri
                .AsNoTracking()
                .AnyAsync(f => !f.IsDeleted && f.KaynakTip == "Hakedis" && f.KaynakId == hakedis.Id);

            if (zatenVar)
                throw new InvalidOperationException("Bu hakediş için muhasebe fişi zaten oluşturulmuş.");

            var fatura = hakedis.FaturaId.HasValue
                ? await context.Faturalar.FirstOrDefaultAsync(f => f.Id == hakedis.FaturaId.Value && !f.IsDeleted)
                : null;

            if (fatura != null && fatura.MuhasebeFisId.HasValue)
                throw new InvalidOperationException("Bağlı fatura için muhasebe fişi zaten mevcut.");

            var cariId = fatura?.CariId ?? await ResolveCariIdAsync(context, hakedis);

            _logger.LogInformation("Hakediş muhasebe aktarım başladı. HakedisId={HakedisId}, Tip={Tip}", hakedisId, hakedis.Tip);

            var ayar = await context.MuhasebeAyarlari.FirstOrDefaultAsync();
            var gelirHesapKodu = !string.IsNullOrWhiteSpace(ayar?.SatisGelirHesabi) ? ayar!.SatisGelirHesabi : "600.01";
            var giderHesapKodu = !string.IsNullOrWhiteSpace(ayar?.AlisGiderHesabi) ? ayar!.AlisGiderHesabi : "740.01";
            var musteriPrefix = ayar?.MusteriPrefix ?? "120.01";
            var tedarikciPrefix = ayar?.TedarikciPrefix ?? "320.01";
            var hesaplananKdv = !string.IsNullOrWhiteSpace(ayar?.HesaplananKdvHesabi) ? ayar!.HesaplananKdvHesabi : "391.01";
            var indirilecekKdv = !string.IsNullOrWhiteSpace(ayar?.IndirilecekKdvHesabi) ? ayar!.IndirilecekKdvHesabi : "191.01";

            var musteriHesap = await FindHesap(context, musteriPrefix);
            var tedarikciHesap = await FindHesap(context, tedarikciPrefix);
            var gelirHesap = await FindHesap(context, gelirHesapKodu);
            var giderHesap = await FindHesap(context, giderHesapKodu);
            var hkdvHesap = await FindHesap(context, hesaplananKdv);
            var ikdvHesap = await FindHesap(context, indirilecekKdv);

            var fisTarihi = fatura?.FaturaTarihi.Date ?? new DateTime(hakedis.Yil, hakedis.Ay, DateTime.DaysInMonth(hakedis.Yil, hakedis.Ay));
            var donemAdi = $"{hakedis.Ay:D2}/{hakedis.Yil}";

            await using var tx = await context.Database.BeginTransactionAsync();

            try
            {
                var fisNo = await _ms.GenerateNextFisNoAsync(FisTipi.Mahsup, hakedis.FirmaId.Value);
                var gelirMi = hakedis.Tip == HakedisTipi.Kurum;

                var fis = new MuhasebeFis
                {
                    FisNo = fisNo,
                    FisTarihi = fisTarihi,
                    FisTipi = FisTipi.Mahsup,
                    Aciklama = gelirMi
                        ? $"Hakediş Gelir — {donemAdi} / Hakedis #{hakedis.Id}"
                        : $"Hakediş Gider — {donemAdi} / Hakedis #{hakedis.Id}",
                    ToplamBorc = hakedis.GenelToplam,
                    ToplamAlacak = hakedis.GenelToplam,
                    Durum = FisDurum.Onaylandi,
                    Kaynak = FisKaynak.Otomatik,
                    KaynakTip = "Hakedis",
                    KaynakId = hakedis.Id,
                    CreatedAt = DateTime.UtcNow
                };

                context.MuhasebeFisleri.Add(fis);
                await context.SaveChangesAsync();

                var sira = 0;
                if (gelirMi)
                {
                    context.MuhasebeFisKalemleri.Add(new MuhasebeFisKalem
                    {
                        FisId = fis.Id,
                        HesapId = musteriHesap.Id,
                        SiraNo = ++sira,
                        Borc = hakedis.GenelToplam,
                        Alacak = 0,
                        Tarih = fisTarihi,
                        CariId = cariId,
                        Aciklama = "Hakediş tahsilat",
                        CreatedAt = DateTime.UtcNow
                    });

                    context.MuhasebeFisKalemleri.Add(new MuhasebeFisKalem
                    {
                        FisId = fis.Id,
                        HesapId = gelirHesap.Id,
                        SiraNo = ++sira,
                        Borc = 0,
                        Alacak = hakedis.Tutar,
                        Tarih = fisTarihi,
                        Aciklama = "Hakediş gelir",
                        CreatedAt = DateTime.UtcNow
                    });

                    if (hakedis.KdvTutar > 0)
                    {
                        context.MuhasebeFisKalemleri.Add(new MuhasebeFisKalem
                        {
                            FisId = fis.Id,
                            HesapId = hkdvHesap.Id,
                            SiraNo = ++sira,
                            Borc = 0,
                            Alacak = hakedis.KdvTutar,
                            Tarih = fisTarihi,
                            Aciklama = "Hesaplanan KDV",
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }
                else
                {
                    context.MuhasebeFisKalemleri.Add(new MuhasebeFisKalem
                    {
                        FisId = fis.Id,
                        HesapId = giderHesap.Id,
                        SiraNo = ++sira,
                        Borc = hakedis.Tutar,
                        Alacak = 0,
                        Tarih = fisTarihi,
                        Aciklama = "Hakediş gider",
                        CreatedAt = DateTime.UtcNow
                    });

                    if (hakedis.KdvTutar > 0)
                    {
                        context.MuhasebeFisKalemleri.Add(new MuhasebeFisKalem
                        {
                            FisId = fis.Id,
                            HesapId = ikdvHesap.Id,
                            SiraNo = ++sira,
                            Borc = hakedis.KdvTutar,
                            Alacak = 0,
                            Tarih = fisTarihi,
                            Aciklama = "İndirilecek KDV",
                            CreatedAt = DateTime.UtcNow
                        });
                    }

                    context.MuhasebeFisKalemleri.Add(new MuhasebeFisKalem
                    {
                        FisId = fis.Id,
                        HesapId = tedarikciHesap.Id,
                        SiraNo = ++sira,
                        Borc = 0,
                        Alacak = hakedis.GenelToplam,
                        Tarih = fisTarihi,
                        CariId = cariId,
                        Aciklama = "Tedarikçiye borç",
                        CreatedAt = DateTime.UtcNow
                    });
                }

                await context.SaveChangesAsync();

                if (fatura != null)
                {
                    fatura.MuhasebeFisiOlusturuldu = true;
                    fatura.MuhasebeFisId = fis.Id;
                    fatura.UpdatedAt = DateTime.UtcNow;
                }

                hakedis.UpdatedAt = DateTime.UtcNow;
                await context.SaveChangesAsync();

                await tx.CommitAsync();

                _logger.LogInformation("Hakediş muhasebe aktarım tamamlandı. HakedisId={HakedisId}, FisId={FisId}", hakedisId, fis.Id);
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "Hakediş muhasebe aktarım hatası. HakedisId={HakedisId}", hakedisId);
                throw;
            }
        });
    }

    private static async Task<int> ResolveCariIdAsync(ApplicationDbContext context, Hakedis h)
    {
        if (h.Tip == HakedisTipi.Kurum)
        {
            var firma = await context.Firmalar.FirstOrDefaultAsync(f => f.Id == h.ReferansId)
                        ?? throw new InvalidOperationException("Kurum (Firma) bulunamadı.");

            if (firma.CariId.HasValue && firma.CariId.Value > 0)
                return firma.CariId.Value;

            var cari = await context.Cariler.FirstOrDefaultAsync(c => !c.IsDeleted && (c.Unvan == firma.FirmaAdi || c.Unvan == firma.UnvanTam));
            return cari?.Id ?? throw new InvalidOperationException("Kurum için Cari kaydı bulunamadı.");
        }

        if (h.Tip == HakedisTipi.Tedarikci)
        {
            var t = await context.TasimaTedarikciler.FirstOrDefaultAsync(x => x.Id == h.ReferansId)
                    ?? throw new InvalidOperationException("Tedarikçi bulunamadı.");

            if (t.CariId is null or 0)
                throw new InvalidOperationException("Tedarikçi için Cari kaydı bulunamadı.");

            return t.CariId.Value;
        }

        throw new InvalidOperationException("Bu hakediş tipi için cari hesaplanamaz.");
    }

    private static async Task<MuhasebeHesap> FindHesap(ApplicationDbContext ctx, string kod)
    {
        var h = await ctx.MuhasebeHesaplari
            .FirstOrDefaultAsync(x => x.HesapKodu.StartsWith(kod) && !x.IsDeleted)
            ?? await ctx.MuhasebeHesaplari
                .FirstOrDefaultAsync(x => x.HesapKodu == kod && !x.IsDeleted);

        return h ?? throw new InvalidOperationException($"Muhasebe hesabı bulunamadı: {kod}. Lütfen hesap planını kontrol edin.");
    }
}
