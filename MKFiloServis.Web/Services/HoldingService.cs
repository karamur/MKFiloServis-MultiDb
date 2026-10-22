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
/// <para>
/// Toplama tüm aktif firmaları kapsar: kullanıcının üst menüden seçtiği aktif firma (firma/tenant filtresi) toplamayı
/// kısıtlamaz. Yoksa yalnız seçili firma dolu, diğerleri 0 çıkardı; oturumsuz çalışan aylık Quartz işi ise hiçbir firmayı
/// göremezdi. Yazılımsal silme filtresi korunur.
/// </para>
/// </remarks>
public sealed class HoldingService : IHoldingService
{
    /// <summary><see cref="ApplicationDbContext"/> içindeki firma (tenant) filtresinin adı.</summary>
    private const string TenantFiltresi = "Tenant";

    private readonly IDbContextFactory<ApplicationDbContext> _appFactory;
    private readonly ILogger<HoldingService> _logger;

    public HoldingService(
        IDbContextFactory<ApplicationDbContext> appFactory,
        ILogger<HoldingService> logger)
    {
        _appFactory = appFactory;
        _logger = logger;
    }

    /// <summary>Aktif firma seçiminden bağımsız okuma: yalnız firma (tenant) filtresi kapatılır.</summary>
    private static IQueryable<T> FirmaFiltresiz<T>(IQueryable<T> kaynak) where T : class
        => kaynak.IgnoreQueryFilters([TenantFiltresi]);

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
        var results = await Task.WhenAll(firmalar.Select(firma => FirmaKarZararAsync(firma, yil, ay, start, end)));

        // Tek tek firma hataları günlüğe yazılıp atlanır; ama hiçbir firma toplanamadıysa ekran "güncellendi" demesin.
        if (results.All(v => v == null))
            throw new InvalidOperationException($"{yil}-{ay:D2} dönemi için hiçbir firmanın verisi toplanamadı (ayrıntı uygulama günlüğünde).");

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
                existing.FirmaKodu = veri.FirmaKodu;
                existing.FirmaAdi = veri.FirmaAdi;
                existing.ToplamGelir = veri.ToplamGelir;
                existing.ToplamGider = veri.ToplamGider;
                existing.Kar = veri.Kar;
                existing.AraclarMaliyet = veri.AraclarMaliyet;
                existing.PersonelMaliyet = veri.PersonelMaliyet;
                existing.HakedisToplam = veri.HakedisToplam;
                existing.OdenmemisFaturaToplam = veri.OdenmemisFaturaToplam;
                existing.AktifAracSayisi = veri.AktifAracSayisi;
                existing.PersonelSayisi = veri.PersonelSayisi;
                existing.JsonDetay = veri.JsonDetay;
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
    /// Bir firmanın dönem kâr/zarar rakamları. Tutar toplamları dar projeksiyonla çekilip bellekte toplanır
    /// (decimal toplamı SQLite'ta çevrilemez; PostgreSQL'de de yük önemsizdir).
    /// </summary>
    private async Task<HoldingVeri?> FirmaKarZararAsync(Firma firma, int yil, int ay, DateTime start, DateTime end)
    {
        try
        {
            using var firmaCtx = await _appFactory.CreateDbContextAsync();
            var firmaId = firma.Id;

            // Gelir/gider: iptal edilen faturalar sayılmaz. Ödenmemiş tutar yalnız alış faturalarının kalanıdır.
            var faturalar = await FirmaFiltresiz(firmaCtx.Faturalar).AsNoTracking()
                .Where(f => !f.IsDeleted && f.FirmaId == firmaId
                    && f.Durum != FaturaDurum.IptalEdildi
                    && (f.FaturaTipi == FaturaTipi.SatisFaturasi || f.FaturaTipi == FaturaTipi.AlisFaturasi)
                    && f.FaturaTarihi >= start && f.FaturaTarihi < end)
                .Select(f => new { f.FaturaTipi, f.Durum, f.GenelToplam, f.OdenenTutar })
                .ToListAsync();

            var gelir = faturalar.Where(f => f.FaturaTipi == FaturaTipi.SatisFaturasi).Sum(f => f.GenelToplam);
            var alislar = faturalar.Where(f => f.FaturaTipi == FaturaTipi.AlisFaturasi).ToList();
            var gider = alislar.Sum(f => f.GenelToplam);
            var odenmemis = alislar
                .Where(f => f.Durum != FaturaDurum.Odendi)
                .Sum(f => Math.Max(0m, f.GenelToplam - f.OdenenTutar));

            // Araç masrafı masraf tarihine, maaş ve hakediş kendi dönem (yıl/ay) alanına göre aylandırılır;
            // kayıt (CreatedAt) tarihi geç girilen kayıtları yanlış aya atardı.
            var aracMaliyet = (await FirmaFiltresiz(firmaCtx.AracMasraflari).AsNoTracking()
                    .Where(m => !m.IsDeleted && m.FirmaId == firmaId && m.MasrafTarihi >= start && m.MasrafTarihi < end)
                    .Select(m => m.Tutar)
                    .ToListAsync())
                .Sum();

            var personelMaliyet = (await FirmaFiltresiz(firmaCtx.PersonelMaaslari).AsNoTracking()
                    .Where(p => !p.IsDeleted && p.FirmaId == firmaId && p.Yil == yil && p.Ay == ay)
                    .Select(p => p.NetMaas)
                    .ToListAsync())
                .Sum();

            // Hakediş: kuruma kesilen (gelir) ve dışarıya ödenen (gider) onaylı hakedişler ayrı tutulur; yalnız tutarı kesinleşmiş
            // durumlar (onaylı, faturalı, tahsil edilmiş, ödenmiş) sayılır — taslaklar birbiriyle çakışabilir, iptaller geçersizdir.
            // Araç bazlı iç raporlar (faturalanmaz) hiç sayılmaz.
            var hakedisler = await FirmaFiltresiz(firmaCtx.Hakedisler).AsNoTracking()
                .Where(h => !h.IsDeleted && h.FirmaId == firmaId && h.Yil == yil && h.Ay == ay
                    && (h.Durum == HakedisDurum.Onaylandi || h.Durum == HakedisDurum.Faturalandi
                        || h.Durum == HakedisDurum.TahsilEdildi || h.Durum == HakedisDurum.Odendi))
                .Select(h => new { h.Tip, h.GenelToplam })
                .ToListAsync();
            var hakedisGelir = hakedisler.Where(h => h.Tip.GelirMi()).Sum(h => h.GenelToplam);
            var hakedisGider = hakedisler.Where(h => h.Tip.GiderMi()).Sum(h => h.GenelToplam);

            var aktifAracSayisi = await FirmaFiltresiz(firmaCtx.Araclar)
                .CountAsync(a => !a.IsDeleted && a.Aktif && a.FirmaId == firmaId);

            var personelSayisi = await FirmaFiltresiz(firmaCtx.Soforler)
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
                HakedisToplam = hakedisGelir,
                OdenmemisFaturaToplam = odenmemis,
                AktifAracSayisi = aktifAracSayisi,
                PersonelSayisi = personelSayisi,
                JsonDetay = HoldingDetay.Yaz(new HoldingDetay { HakedisGider = hakedisGider }),
                OlusturmaTarihi = DateTime.UtcNow,
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Firma {FirmaId} ({FirmaAdi}) veri toplama hatasi.", firma.Id, firma.FirmaAdi);
            return null;
        }
    }

    /// <summary>
    /// Tüm aktif firmalar için bütçe hedef ve gerçekleşen verilerini toplar
    /// ve <see cref="HoldingVeri"/> tablosuna Kategori="BUTCE" olarak upsert eder.
    /// </summary>
    /// <remarks>
    /// Tanımlar Bütçe modülüyle aynıdır (<c>BudgetService.GetYillikHedefOzetAsync</c>): yıllık hedef "Ay = 0" kayıtlarıdır
    /// (yoksa aylık hedeflerin toplamı), gerçekleşen "ödendi ya da faturayla kapatıldı" ödemelerin ödenen tutarıdır.
    /// Firması boş (ortak) bütçe kayıtları firma anlık görüntüsüne yazılamadığından bu özete girmez.
    /// </remarks>
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

                var hedefler = await firmaCtx.BudgetHedefler.AsNoTracking()
                    .Where(h => !h.IsDeleted && h.FirmaId == firmaId && h.Yil == yil)
                    .Select(h => new { h.Ay, h.HedefTutar })
                    .ToListAsync();
                var yillikHedef = hedefler.Where(h => h.Ay == 0).Sum(h => h.HedefTutar);
                var aylikHedefToplami = hedefler.Where(h => h.Ay is >= 1 and <= 12).Sum(h => h.HedefTutar);
                var butceHedef = yillikHedef > 0m ? yillikHedef : aylikHedefToplami;

                // Yıl başından bu aya kadar gerçekleşen ödemeler (ay bazında ayrıştırılabilsin diye tek sorgu).
                var odemeler = await firmaCtx.BudgetOdemeler.AsNoTracking()
                    .Where(o => !o.IsDeleted && o.FirmaId == firmaId
                        && o.OdemeYil == yil && o.OdemeAy <= ay
                        && (o.Durum == OdemeDurum.Odendi || o.FaturaIleKapatildi))
                    .Select(o => new { o.OdemeAy, o.OdenenTutar, o.Miktar })
                    .ToListAsync();

                var butceGerceklesen = odemeler.Where(o => o.OdemeAy == ay).Sum(o => o.OdenenTutar ?? o.Miktar);
                // Yıllık kümülatif gerçekleşen (Ocak'tan bu aya kadar)
                var yillikGerceklesen = odemeler.Sum(o => o.OdenenTutar ?? o.Miktar);

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
                existing.FirmaKodu = veri.FirmaKodu;
                existing.FirmaAdi = veri.FirmaAdi;
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

    public async Task<HoldingDonemSecimi?> GetSonVeriDonemiAsync()
    {
        using var ctx = await _appFactory.CreateDbContextAsync();
        var son = await ctx.HoldingVeriler.AsNoTracking()
            .Where(v => v.Kategori == "KARZARAR")
            .OrderByDescending(v => v.Yil).ThenByDescending(v => v.Ay)
            .Select(v => new { v.Yil, v.Ay })
            .FirstOrDefaultAsync();

        return son == null ? null : new HoldingDonemSecimi(son.Yil, son.Ay);
    }

    public async Task<List<HoldingVadeKalemi>> GetAcikFaturalarAsync()
    {
        using var ctx = await _appFactory.CreateDbContextAsync();

        var firmalar = await ctx.Firmalar.AsNoTracking()
            .Where(f => f.Aktif && !f.IsDeleted)
            .Select(f => new { f.Id, f.FirmaAdi })
            .ToListAsync();
        if (firmalar.Count == 0)
            return [];

        var adlar = firmalar.ToDictionary(f => f.Id, f => f.FirmaAdi);
        var idler = adlar.Keys.ToList();

        // Tam ödenmiş ve iptal faturalar sunucuda elenir; kalan tutar kontrolü (eski tutarsız kayıtlar için) bellekte yapılır.
        var ham = await FirmaFiltresiz(ctx.Faturalar).AsNoTracking()
            .Where(f => !f.IsDeleted && f.FirmaId != null && idler.Contains(f.FirmaId.Value)
                && f.Durum != FaturaDurum.IptalEdildi && f.Durum != FaturaDurum.Odendi
                && (f.FaturaTipi == FaturaTipi.SatisFaturasi || f.FaturaTipi == FaturaTipi.AlisFaturasi))
            .Select(f => new { FirmaId = f.FirmaId!.Value, f.FaturaTipi, f.VadeTarihi, f.GenelToplam, f.OdenenTutar })
            .ToListAsync();

        return ham
            .Select(f => new HoldingVadeKalemi(
                f.FirmaId,
                adlar[f.FirmaId],
                f.FaturaTipi == FaturaTipi.AlisFaturasi,
                f.VadeTarihi,
                f.GenelToplam - f.OdenenTutar))
            .Where(k => k.Kalan > 0m)
            .ToList();
    }
}
