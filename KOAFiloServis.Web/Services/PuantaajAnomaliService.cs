using KOAFiloServis.Shared.Entities;
using KOAFiloServis.Web.Data;
using KOAFiloServis.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KOAFiloServis.Web.Services;

/// <summary>
/// Puantaj verilerinde kural-tabanlı ve AI destekli anomali tespit servisi.
/// </summary>
public sealed class PuantajAnomaliService : IPuantajAnomaliService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly IOllamaService? _ollamaService;
    private readonly ILogger<PuantajAnomaliService> _logger;

    public PuantajAnomaliService(
        IDbContextFactory<ApplicationDbContext> dbFactory,
        IServiceProvider serviceProvider,
        ILogger<PuantajAnomaliService> logger)
    {
        _dbFactory = dbFactory;
        _ollamaService = serviceProvider.GetService<IOllamaService>(); // Optional: Ollama yoksa kural-tabanlı çalışır
        _logger = logger;
    }

    /// <summary>
    /// Belirtilen dönem için tüm anomali taramalarını çalıştırır.
    /// </summary>
    /// <returns>Tespit edilen anomali sayısı</returns>
    public async Task<int> TumTaramaAsync(int yil, int ay, int? firmaId = null)
    {
        _logger.LogInformation("PuantaajAnomali tarama basladi: {Yil}-{Ay}, Firma={FirmaId}", yil, ay, firmaId ?? 0);

        var toplam = 0;
        toplam += await SifirTutarTaraAsync(yil, ay, firmaId);
        toplam += await NegatifMarjTaraAsync(yil, ay, firmaId);
        toplam += await AsiriFiyatTaraAsync(yil, ay, firmaId);
        toplam += await MukerrerKayitTaraAsync(yil, ay, firmaId);
        toplam += await GunTutarsizligiTaraAsync(yil, ay, firmaId);
        toplam += await OdemeGecikmesiTaraAsync(yil, ay, firmaId);
        toplam += await AsiriDegisimTaraAsync(yil, ay, firmaId);

        _logger.LogInformation("PuantaajAnomali tarama tamamlandi: {Count} anomali tespit edildi.", toplam);
        return toplam;
    }

    /// <summary>
    /// AI destekli derinlemesine pattern analizi. Mevcut Ollama modelini kullanır.
    /// </summary>
    public async Task<string> AIAnalizAsync(int yil, int ay, int? firmaId = null)
    {
        if (_ollamaService == null)
            return "AI analizi için Ollama servisi gerekli. Lütfen Ollama'nın çalıştığından emin olun.";

        using var ctx = await _dbFactory.CreateDbContextAsync();

        var query = ctx.PuantajKayitlar
            .Where(p => !p.IsDeleted && p.Yil == yil && p.Ay == ay);
        if (firmaId.HasValue)
            query = query.Where(p => p.IsverenFirmaId == firmaId.Value);

        var kayitlar = await query
            .OrderBy(p => p.KurumAdi).ThenBy(p => p.GuzergahAdi)
            .Take(100)
            .Select(p => new
            {
                p.KurumAdi, p.GuzergahAdi, p.Plaka, p.SoforAdi,
                p.BirimGelir, p.BirimGider, p.Gun, p.SeferSayisi,
                p.ToplamGelir, p.ToplamGider, p.Odenecek, p.Alinacak
            })
            .ToListAsync();

        if (kayitlar.Count == 0)
            return "Analiz edilecek puantaj kaydı bulunamadı.";

        var veriOzeti = string.Join("\n", kayitlar.Select(k =>
            $"{k.KurumAdi} | {k.GuzergahAdi} | {k.Plaka} | Şoför:{k.SoforAdi} | " +
            $"Gün:{k.Gun} Sefer:{k.SeferSayisi} | BirimGelir:{k.BirimGelir:N2} BirimGider:{k.BirimGider:N2} | " +
            $"ToplamGelir:{k.ToplamGelir:N2} ToplamGider:{k.ToplamGider:N2} | " +
            $"Ödenecek:{k.Odenecek:N2} Alınacak:{k.Alinacak:N2}"));

        var sistemPrompt = @"Sen bir personel taşımacılığı filo yönetim sisteminde puantaj verilerini analiz eden bir AI denetçisin.
Aşağıdaki puantaj kayıtlarında anomali, şüpheli durum veya tutarsızlıkları tespit et.
Şunlara odaklan:
- BirimGelir < BirimGider (zarar eden sefer)
- Sıfır veya aşırı düşük/yüksek tutarlar
- Aynı rota için diğer kayıtlardan çok farklı fiyatlar
- Gün sayısı ile sefer sayısı arasında tutarsızlık (örn: 22 gün ama 5 sefer)
- Ödenecek/Alınacak tutarın manuel hesapla uyuşmaması

Yanıtını ŞU FORMATA göre ver (her anomali bir satır, | ile ayrılmış):
ÖNEM (1-4) | ANOMALİ TİPİ | ETKİLENEN KAYIT (Kurum-Güzergah-Plaka) | AÇIKLAMA | ÖNERİ";

        try
        {
            var aiYanit = await _ollamaService.AnalizYapAsync(veriOzeti, sistemPrompt);
            return aiYanit ?? "AI analizi yanıt döndürmedi.";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI anomali analizi sırasında Ollama hatası");
            return $"AI analizi başarısız: {ex.Message}";
        }
    }

    // ── Kural-tabanlı taramalar ────────────────────────────────────────

    private async Task<int> SifirTutarTaraAsync(int yil, int ay, int? firmaId)
    {
        using var ctx = await _dbFactory.CreateDbContextAsync();
        var kayitlar = await Filtrele(ctx, yil, ay, firmaId)
            .Where(p => p.BirimGelir == 0 || p.BirimGider == 0 || p.Odenecek == 0)
            .ToListAsync();

        return await KaydetAnomalilerAsync(ctx, kayitlar, PuantajAnomaliTipi.SifirTutar, 3,
            p => $"BirimGelir={p.BirimGelir:N2}, BirimGider={p.BirimGider:N2}, Ödenecek={p.Odenecek:N2}",
            p => "Birim fiyat veya ödenecek tutar sıfır. Eksik veri girişi veya hesaplama hatası olabilir. İlgili kaydı kontrol edin.");
    }

    private async Task<int> NegatifMarjTaraAsync(int yil, int ay, int? firmaId)
    {
        using var ctx = await _dbFactory.CreateDbContextAsync();
        var kayitlar = await Filtrele(ctx, yil, ay, firmaId)
            .Where(p => p.BirimGelir > 0 && p.BirimGider > 0 && p.BirimGelir < p.BirimGider)
            .ToListAsync();

        return await KaydetAnomalilerAsync(ctx, kayitlar, PuantajAnomaliTipi.NegatifMarj, 4,
            p => $"BirimGelir={p.BirimGelir:N2} < BirimGider={p.BirimGider:N2}, Zarar={p.BirimGider - p.BirimGelir:N2}",
            p => "KRİTİK: Birim gelir, birim giderden düşük. Bu sefer zarar ediyor. Fiyatlandırmayı acil kontrol edin.");
    }

    private async Task<int> AsiriFiyatTaraAsync(int yil, int ay, int? firmaId)
    {
        using var ctx = await _dbFactory.CreateDbContextAsync();
        var gruplar = await Filtrele(ctx, yil, ay, firmaId)
            .Where(p => p.BirimGelir > 0)
            .GroupBy(p => p.GuzergahAdi)
            .Select(g => new { Guzergah = g.Key, Ortalama = g.Average(p => p.BirimGelir), Kayitlar = g.ToList() })
            .ToListAsync();

        var count = 0;
        foreach (var grup in gruplar.Where(g => g.Kayitlar.Count >= 3))
        {
            var asiriYuksek = grup.Kayitlar
                .Where(p => p.BirimGelir > grup.Ortalama * 3)
                .ToList();
            var asiriDusuk = grup.Kayitlar
                .Where(p => p.BirimGelir < grup.Ortalama / 3 && p.BirimGelir > 0)
                .ToList();

            count += await KaydetAnomalilerAsync(ctx, asiriYuksek, PuantajAnomaliTipi.AsiriYuksekFiyat, 2,
                p => $"BirimGelir={p.BirimGelir:N2}, Rota Ortalaması={grup.Ortalama:N2}, Sapma={p.BirimGelir / grup.Ortalama:F1}x",
                p => $"Birim gelir rota ortalamasının {p.BirimGelir / grup.Ortalama:F1} katı. Fiyatlandırma hatası veya özel durum olabilir.");

            count += await KaydetAnomalilerAsync(ctx, asiriDusuk, PuantajAnomaliTipi.AsiriDusukFiyat, 2,
                p => $"BirimGelir={p.BirimGelir:N2}, Rota Ortalaması={grup.Ortalama:N2}",
                p => "Birim gelir rota ortalamasının çok altında. Eksik faturalandırma olabilir.");
        }
        return count;
    }

    private async Task<int> MukerrerKayitTaraAsync(int yil, int ay, int? firmaId)
    {
        using var ctx = await _dbFactory.CreateDbContextAsync();
        var gruplar = await Filtrele(ctx, yil, ay, firmaId)
            .GroupBy(p => new { p.GuzergahAdi, p.Plaka, p.Slot, p.Yon })
            .Where(g => g.Count() > 1)
            .Select(g => g.OrderBy(p => p.Id).Skip(1).ToList())
            .ToListAsync();

        var count = 0;
        foreach (var mukerler in gruplar)
        {
            count += await KaydetAnomalilerAsync(ctx, mukerler, PuantajAnomaliTipi.MukerrerKayit, 3,
                p => $"Aynı rota+araç+slot için mükerrer kayıt. İlk kayıt ID={p.Id}",
                p => "Aynı güzergah, araç ve slot için birden fazla puantaj kaydı var. Birleştirme veya silme yapın.");
        }
        return count;
    }

    private async Task<int> GunTutarsizligiTaraAsync(int yil, int ay, int? firmaId)
    {
        using var ctx = await _dbFactory.CreateDbContextAsync();
        var kayitlar = await Filtrele(ctx, yil, ay, firmaId)
            .Where(p => p.Gun > 0)
            .ToListAsync();

        var tutarsiz = kayitlar.Where(p =>
        {
            var gunToplami = p.Gun01 + p.Gun02 + p.Gun03 + p.Gun04 + p.Gun05 +
                             p.Gun06 + p.Gun07 + p.Gun08 + p.Gun09 + p.Gun10 +
                             p.Gun11 + p.Gun12 + p.Gun13 + p.Gun14 + p.Gun15 +
                             p.Gun16 + p.Gun17 + p.Gun18 + p.Gun19 + p.Gun20 +
                             p.Gun21 + p.Gun22 + p.Gun23 + p.Gun24 + p.Gun25 +
                             p.Gun26 + p.Gun27 + p.Gun28 + p.Gun29 + p.Gun30 + p.Gun31;
            return Math.Abs(p.Gun - (decimal)gunToplami) > 0;
        }).ToList();

        return await KaydetAnomalilerAsync(ctx, tutarsiz, PuantajAnomaliTipi.GunTutarsizligi, 3,
            p => $"Gun={p.Gun}, Gun01..Gun31 toplamı farklı",
            p => "Gün sayısı ile günlük detay toplamı uyuşmuyor. HesaplaPuantajToplam() tekrar çalıştırın.");
    }

    private async Task<int> OdemeGecikmesiTaraAsync(int yil, int ay, int? firmaId)
    {
        using var ctx = await _dbFactory.CreateDbContextAsync();
        var kayitlar = await Filtrele(ctx, yil, ay, firmaId)
            .Where(p =>
                (p.GelirOdemeDurumu == PuantajOdemeDurum.Odenmedi && p.Alinacak > 0) ||
                (p.GiderOdemeDurumu == PuantajOdemeDurum.Odenmedi && p.Odenecek > 0))
            .ToListAsync();

        return await KaydetAnomalilerAsync(ctx, kayitlar, PuantajAnomaliTipi.OdemeGecikmesi, 1,
            p => $"GelirDurum={p.GelirOdemeDurumu}, GiderDurum={p.GiderOdemeDurumu}, Alınacak={p.Alinacak:N2}, Ödenecek={p.Odenecek:N2}",
            p => "Ödeme tahsilatı/ödemesi henüz yapılmamış. İlgili cariye hatırlatma gönderin.");
    }

    private async Task<int> AsiriDegisimTaraAsync(int yil, int ay, int? firmaId)
    {
        using var ctx = await _dbFactory.CreateDbContextAsync();
        var oncekiAy = ay == 1 ? 12 : ay - 1;
        var oncekiYil = ay == 1 ? yil - 1 : yil;

        var mevcutAy = await Filtrele(ctx, yil, ay, firmaId)
            .GroupBy(p => p.GuzergahAdi)
            .Select(g => new { Guzergah = g.Key, ToplamGelir = g.Sum(p => p.ToplamGelir), KayitSayisi = g.Count() })
            .ToListAsync();

        var oncekiAyVeri = await Filtrele(ctx, oncekiYil, oncekiAy, firmaId)
            .GroupBy(p => p.GuzergahAdi)
            .Select(g => new { Guzergah = g.Key, ToplamGelir = g.Sum(p => p.ToplamGelir) })
            .ToListAsync();

        var count = 0;
        foreach (var mevcut in mevcutAy)
        {
            var onceki = oncekiAyVeri.FirstOrDefault(o => o.Guzergah == mevcut.Guzergah);
            if (onceki == null || onceki.ToplamGelir == 0) continue;

            var degisim = Math.Abs(mevcut.ToplamGelir - onceki.ToplamGelir) / onceki.ToplamGelir;
            if (degisim > 0.5m) // %50'den fazla değişim
            {
                using var ctx2 = await _dbFactory.CreateDbContextAsync();
                var anomali = new PuantajAnomali
                {
                    AnomaliTipi = PuantajAnomaliTipi.AsiriDegisim,
                    TespitYontemi = PuantajAnomaliTespitYontemi.Kural,
                    OnemSeviyesi = 2,
                    FirmaId = firmaId,
                    Yil = yil, Ay = ay,
                    Baslik = $"'{mevcut.Guzergah}' rotasında aylık gelir %{degisim * 100:F0} değişti",
                    Aciklama = $"Önceki ay: {onceki.ToplamGelir:N2} TL → Bu ay: {mevcut.ToplamGelir:N2} TL",
                    AnomaliDetay = $"{{\"oncekiAy\":{onceki.ToplamGelir},\"buAy\":{mevcut.ToplamGelir},\"degisim\":{degisim:P}}}",
                    Oneri = "Aylık gelirde olağandışı değişim var. Yeni eklenen/çıkarılan sefer, fiyat değişikliği veya veri giriş hatası olabilir.",
                };
                ctx2.PuantajAnomaliler.Add(anomali);
                await ctx2.SaveChangesAsync();
                count++;
            }
        }
        return count;
    }

    // ── Yardımcılar ────────────────────────────────────────────────────

    private static IQueryable<PuantajKayit> Filtrele(ApplicationDbContext ctx, int yil, int ay, int? firmaId)
    {
        var query = ctx.PuantajKayitlar
            .Where(p => !p.IsDeleted && p.Yil == yil && p.Ay == ay);
        if (firmaId.HasValue)
            query = query.Where(p => p.IsverenFirmaId == firmaId.Value);
        return query;
    }

    private async Task<int> KaydetAnomalilerAsync(
        ApplicationDbContext ctx,
        List<PuantajKayit> kayitlar,
        PuantajAnomaliTipi tip,
        int onemSeviyesi,
        Func<PuantajKayit, string> detayOlustur,
        Func<PuantajKayit, string> oneriOlustur)
    {
        var count = 0;
        foreach (var kayit in kayitlar)
        {
            // Aynı anomali zaten kaydedilmiş mi kontrol et (son 7 gün içinde)
            var mevcut = await ctx.PuantajAnomaliler
                .AnyAsync(a => a.PuantajKayitId == kayit.Id
                    && a.AnomaliTipi == tip
                    && a.TespitTarihi > DateTime.UtcNow.AddDays(-7));

            if (mevcut) continue;

            var anomali = new PuantajAnomali
            {
                AnomaliTipi = tip,
                TespitYontemi = PuantajAnomaliTespitYontemi.Kural,
                OnemSeviyesi = onemSeviyesi,
                FirmaId = kayit.IsverenFirmaId,
                PuantajKayitId = kayit.Id,
                Yil = kayit.Yil,
                Ay = kayit.Ay,
                Baslik = $"{kayit.KurumAdi} - {kayit.GuzergahAdi} - {tip}",
                Aciklama = $"{kayit.Plaka} | Şoför: {kayit.SoforAdi} | Slot: {kayit.Slot} | {detayOlustur(kayit)}",
                AnomaliDetay = System.Text.Json.JsonSerializer.Serialize(new
                {
                    kayit.Id, kayit.KurumAdi, kayit.GuzergahAdi, kayit.Plaka, kayit.SoforAdi,
                    kayit.BirimGelir, kayit.BirimGider, kayit.Gun, kayit.SeferSayisi,
                    kayit.ToplamGelir, kayit.ToplamGider, kayit.Alinacak, kayit.Odenecek
                }),
                Oneri = oneriOlustur(kayit),
            };
            ctx.PuantajAnomaliler.Add(anomali);
            count++;
        }

        if (count > 0)
            await ctx.SaveChangesAsync();

        return count;
    }
}
