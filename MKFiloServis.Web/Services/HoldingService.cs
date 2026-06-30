using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Web.Services;

/// <summary>
/// Holding konsolide veri servisi (Nihai Mimari Kural 13).
/// </summary>
/// <remarks>
/// <para><b>Nihai mimari değişikliği:</b></para>
/// <list type="bullet">
///   <item>Eski: Her firma için ayrı tenant DB'ye paralel bağlantı (fan-out).</item>
///   <item>Yeni: Tek KOAFiloServis veritabanında FirmaId bazlı filtreleme.</item>
/// </list>
/// <para>
/// Veriler <see cref="HoldingVeri"/> tablosunda snapshot olarak saklanır;
/// <see cref="ToplaVeKaydetAsync"/> metodu periyodik olarak (Quartz job veya manuel)
/// çağrılarak güncel verileri toplar.
/// </para>
/// </remarks>
public sealed class HoldingService : IHoldingService
{
    private readonly IDbContextFactory<ApplicationDbContext> _appFactory;
    private readonly ILogger<HoldingService> _logger;

    public HoldingService(
        IDbContextFactory<ApplicationDbContext> appFactory,
        ILogger<HoldingService> logger)
    {
        _appFactory = appFactory;
        _logger = logger;
    }

    /// <summary>
    /// Tüm aktif firmalar için belirtilen döneme ait konsolide verileri toplar
    /// ve <see cref="HoldingVeri"/> tablosuna upsert eder.
    /// </summary>
    /// <remarks>
    /// Nihai mimari: Tek veritabanında FirmaId bazlı sorgu.
    /// Eski tenant DB fan-out yaklaşımı terk edilmiştir.
    /// </remarks>
    public async Task ToplaVeKaydetAsync(int yil, int ay)
    {
        using var ctx = await _appFactory.CreateDbContextAsync();

        var firmalar = await ctx.Firmalar
            .Where(f => f.Aktif && !f.IsDeleted)
            .ToListAsync();

        if (firmalar.Count == 0)
        {
            _logger.LogInformation("ToplaVeKaydet: Aktif firma bulunamadi.");
            return;
        }

        _logger.LogInformation("ToplaVeKaydet: {Count} firma icin {Yil}-{Ay} verisi toplaniyor (tek veritabani)...",
            firmalar.Count, yil, ay);

        var start = new DateTime(yil, ay, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(1);

        // Her firma için kendi DbContext'i ile paralel sorgu (DbContext thread-safe değil)
        var tasks = firmalar.Select(async firma =>
        {
            try
            {
                using var firmaCtx = await _appFactory.CreateDbContextAsync();
                var firmaId = firma.Id;

                var gelir = await firmaCtx.Faturalar
                    .Where(f => !f.IsDeleted && f.FirmaId == firmaId
                        && f.FaturaTipi == FaturaTipi.SatisFaturasi
                        && f.FaturaTarihi >= start && f.FaturaTarihi < end)
                    .SumAsync(f => f.GenelToplam);

                var gider = await firmaCtx.Faturalar
                    .Where(f => !f.IsDeleted && f.FirmaId == firmaId
                        && f.FaturaTipi == FaturaTipi.AlisFaturasi
                        && f.FaturaTarihi >= start && f.FaturaTarihi < end)
                    .SumAsync(f => f.GenelToplam);

                // Kural 4: AracMasraf ve PersonelMaas artik dogrudan FirmaId'ye sahip
                var aracMaliyet = await firmaCtx.AracMasraflari
                    .Where(m => !m.IsDeleted && m.FirmaId == firmaId
                        && m.CreatedAt >= start && m.CreatedAt < end)
                    .SumAsync(m => m.Tutar);

                var personelMaliyet = await firmaCtx.PersonelMaaslari
                    .Where(p => !p.IsDeleted && p.FirmaId == firmaId
                        && p.CreatedAt >= start && p.CreatedAt < end)
                    .SumAsync(p => p.NetMaas);

                var hakedisToplam = await firmaCtx.Hakedisler
                    .Where(h => !h.IsDeleted && h.FirmaId == firmaId
                        && h.CreatedAt >= start && h.CreatedAt < end)
                    .SumAsync(h => h.GenelToplam);

                var aktifAracSayisi = await firmaCtx.Araclar
                    .CountAsync(a => !a.IsDeleted && a.Aktif && a.FirmaId == firmaId);

                var personelSayisi = await firmaCtx.Soforler
                    .CountAsync(s => !s.IsDeleted && s.Aktif && s.FirmaId == firmaId);

                return new HoldingVeri
                {
                    FirmaId = firma.Id,
                    FirmaKodu = firma.FirmaKodu,
                    FirmaAdi = firma.FirmaAdi,
                    Yil = yil,
                    Ay = ay,
                    Kategori = "KARZARAR",
                    ToplamGelir = gelir,
                    ToplamGider = gider,
                    Kar = gelir - gider,
                    AraclarMaliyet = aracMaliyet,
                    PersonelMaliyet = personelMaliyet,
                    HakedisToplam = hakedisToplam,
                    AktifAracSayisi = aktifAracSayisi,
                    PersonelSayisi = personelSayisi,
                    OlusturmaTarihi = DateTime.UtcNow,
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Firma {FirmaId} ({FirmaAdi}) veri toplama hatasi.", firma.Id, firma.FirmaAdi);
                return null;
            }
        });

        var results = await Task.WhenAll(tasks);

        // Upsert — ayrı bir context ile toplu yazma
        using var writeCtx = await _appFactory.CreateDbContextAsync();
        foreach (var veri in results.Where(v => v != null).Cast<HoldingVeri>())
        {
            var existing = await writeCtx.HoldingVeriler
                .FirstOrDefaultAsync(v => v.FirmaId == veri.FirmaId
                    && v.Yil == veri.Yil && v.Ay == veri.Ay
                    && v.Kategori == veri.Kategori);

            if (existing != null)
            {
                existing.ToplamGelir = veri.ToplamGelir;
                existing.ToplamGider = veri.ToplamGider;
                existing.Kar = veri.Kar;
                existing.AraclarMaliyet = veri.AraclarMaliyet;
                existing.PersonelMaliyet = veri.PersonelMaliyet;
                existing.HakedisToplam = veri.HakedisToplam;
                existing.AktifAracSayisi = veri.AktifAracSayisi;
                existing.PersonelSayisi = veri.PersonelSayisi;
                existing.OlusturmaTarihi = DateTime.UtcNow;
            }
            else
            {
                writeCtx.HoldingVeriler.Add(veri);
            }
        }
        await writeCtx.SaveChangesAsync();

        _logger.LogInformation("ToplaVeKaydet: {Count} firma KARZARAR verisi kaydedildi.",
            results.Count(v => v != null));

        // ── BUTCE Konsolidasyonu (Faz 5.2) ──────────────────────────────
        await ToplaButceVeKaydetAsync(firmalar, yil, ay);
    }

    /// <summary>
    /// Tüm aktif firmalar için bütçe hedef ve gerçekleşen verilerini toplar
    /// ve <see cref="HoldingVeri"/> tablosuna Kategori="BUTCE" olarak upsert eder.
    /// </summary>
    private async Task ToplaButceVeKaydetAsync(List<Firma> firmalar, int yil, int ay)
    {
        _logger.LogInformation("ToplaButce: {Count} firma icin {Yil}-{Ay} BUTCE verisi toplaniyor...",
            firmalar.Count, yil, ay);

        var butceTasks = firmalar.Select(async firma =>
        {
            try
            {
                using var firmaCtx = await _appFactory.CreateDbContextAsync();
                var firmaId = firma.Id;

                // BudgetHedef: Bu firma için yılın tüm aylarındaki hedef toplamı
                var butceHedef = await firmaCtx.BudgetHedefler
                    .Where(h => !h.IsDeleted && h.FirmaId == firmaId && h.Yil == yil)
                    .SumAsync(h => h.HedefTutar);

                // BudgetOdeme: Bu firma için ilgili aydaki gerçekleşen ödemeler
                var butceGerceklesen = await firmaCtx.BudgetOdemeler
                    .Where(o => !o.IsDeleted && o.FirmaId == firmaId
                        && o.OdemeYil == yil && o.OdemeAy == ay
                        && o.Durum == OdemeDurum.Odendi)
                    .SumAsync(o => o.Miktar);

                // Yıllık kümülatif gerçekleşen (Ocak'tan bu aya kadar)
                var yillikGerceklesen = await firmaCtx.BudgetOdemeler
                    .Where(o => !o.IsDeleted && o.FirmaId == firmaId
                        && o.OdemeYil == yil && o.OdemeAy <= ay
                        && o.Durum == OdemeDurum.Odendi)
                    .SumAsync(o => o.Miktar);

                return new HoldingVeri
                {
                    FirmaId = firma.Id,
                    FirmaKodu = firma.FirmaKodu,
                    FirmaAdi = firma.FirmaAdi,
                    Yil = yil,
                    Ay = ay,
                    Kategori = "BUTCE",
                    ButceHedef = butceHedef,
                    ButceGerceklesen = butceGerceklesen,
                    ToplamGelir = yillikGerceklesen, // Yıllık kümülatif (UI'da kullanılıyor)
                    OlusturmaTarihi = DateTime.UtcNow,
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Firma {FirmaId} ({FirmaAdi}) BUTCE veri toplama hatasi.", firma.Id, firma.FirmaAdi);
                return null;
            }
        });

        var butceResults = await Task.WhenAll(butceTasks);

        using var writeCtx = await _appFactory.CreateDbContextAsync();
        foreach (var veri in butceResults.Where(v => v != null).Cast<HoldingVeri>())
        {
            var existing = await writeCtx.HoldingVeriler
                .FirstOrDefaultAsync(v => v.FirmaId == veri.FirmaId
                    && v.Yil == veri.Yil && v.Ay == veri.Ay
                    && v.Kategori == veri.Kategori);

            if (existing != null)
            {
                existing.ButceHedef = veri.ButceHedef;
                existing.ButceGerceklesen = veri.ButceGerceklesen;
                existing.ToplamGelir = veri.ToplamGelir;
                existing.OlusturmaTarihi = DateTime.UtcNow;
            }
            else
            {
                writeCtx.HoldingVeriler.Add(veri);
            }
        }
        await writeCtx.SaveChangesAsync();

        _logger.LogInformation("ToplaButce: {Count} firma BUTCE verisi kaydedildi.",
            butceResults.Count(v => v != null));
    }

    public async Task<List<HoldingVeri>> GetFirmaKarsilastirmaAsync(int yil, int? ay = null)
    {
        using var ctx = await _appFactory.CreateDbContextAsync();
        var query = ctx.HoldingVeriler.Where(v => v.Yil == yil && v.Kategori == "KARZARAR");
        if (ay.HasValue)
            query = query.Where(v => v.Ay == ay.Value);
        return await query.OrderBy(v => v.FirmaId).ThenBy(v => v.Ay).ToListAsync();
    }

    public async Task<List<HoldingVeri>> GetButceKonsolidasyonAsync(int yil)
    {
        using var ctx = await _appFactory.CreateDbContextAsync();
        return await ctx.HoldingVeriler
            .Where(v => v.Yil == yil && v.Kategori == "BUTCE")
            .OrderBy(v => v.FirmaId).ThenBy(v => v.Ay)
            .ToListAsync();
    }

    public async Task<List<HoldingVeri>> GetAracMaliyetOzetiAsync(int yil, int? ay = null)
    {
        using var ctx = await _appFactory.CreateDbContextAsync();
        var query = ctx.HoldingVeriler.Where(v => v.Yil == yil && v.Kategori == "KARZARAR");
        if (ay.HasValue)
            query = query.Where(v => v.Ay == ay.Value);
        return await query.OrderBy(v => v.FirmaId).ToListAsync();
    }

    public async Task<List<HoldingVeri>> GetPersonelGiderOzetiAsync(int yil, int? ay = null)
    {
        using var ctx = await _appFactory.CreateDbContextAsync();
        var query = ctx.HoldingVeriler.Where(v => v.Yil == yil && v.Kategori == "KARZARAR");
        if (ay.HasValue)
            query = query.Where(v => v.Ay == ay.Value);
        return await query.OrderBy(v => v.FirmaId).ToListAsync();
    }

    public async Task<List<HoldingVeri>> GetHakedisOzetiAsync(int yil, int? ay = null)
    {
        using var ctx = await _appFactory.CreateDbContextAsync();
        var query = ctx.HoldingVeriler.Where(v => v.Yil == yil && v.Kategori == "KARZARAR");
        if (ay.HasValue)
            query = query.Where(v => v.Ay == ay.Value);
        return await query.OrderBy(v => v.FirmaId).ToListAsync();
    }

    public async Task<List<HoldingRapor>> GetKayitliRaporlarAsync()
    {
        using var ctx = await _appFactory.CreateDbContextAsync();
        return await ctx.HoldingRaporlar
            .OrderByDescending(r => r.OlusturmaTarihi)
            .Take(50)
            .ToListAsync();
    }

    public async Task<HoldingRapor> RaporKaydetAsync(HoldingRapor rapor)
    {
        using var ctx = await _appFactory.CreateDbContextAsync();
        if (rapor.Id == 0)
            ctx.HoldingRaporlar.Add(rapor);
        else
            ctx.HoldingRaporlar.Update(rapor);
        await ctx.SaveChangesAsync();
        return rapor;
    }
}



