using System.Globalization;
using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using MKFiloServis.Web.Helpers;
using MKFiloServis.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MKFiloServis.Web.Services;

/// <summary>
/// Kapsamlı (filtreli) hakediş: araç sahibi, güzergâh, eşleştirme, cari, araç, şoför, servis türü ve gün aralığına
/// göre hem canlı önizleme hem de hakediş üretimi. Önizleme ile üretim aynı satır kümesini kullanır.
/// </summary>
public partial class OperasyonelHakedisService
{
    private static readonly StringComparer TrKarsilastirici =
        StringComparer.Create(CultureInfo.GetCultureInfo("tr-TR"), ignoreCase: true);

    /// <summary>Başka bir hakedişe girmiş sayılan (artık değiştirilmeyen) durumlar. Taslak ve iptal hariçtir.</summary>
    private static readonly HakedisDurum[] KesinlesmisDurumlar =
    {
        HakedisDurum.Onaylandi,
        HakedisDurum.Faturalandi,
        HakedisDurum.TahsilEdildi,
        HakedisDurum.Odendi
    };

    // ── Önizleme ────────────────────────────────────────────────────────────

    public async Task<HakedisKapsamOnizleme> KapsamOnizleAsync(HakedisTipi tip, HakedisKapsami kapsam)
    {
        KapsamiDogrula(kapsam);

        await using var context = await _contextFactory.CreateDbContextAsync();
        var veri = await KaynakVeriyiYukleAsync(context, tip, kapsam);

        var satirlar = veri.Secili
            .GroupBy(r => (r.ReferansId, r.CariId, r.GuzergahId, r.EslestirmeId, r.AracId, r.SoforId, r.Uygun,
                HakedisteMi: veri.Kesinlesmis.Contains(r.PuantajId)))
            .Select(g =>
            {
                var ilk = g.First();
                return new HakedisKapsamSatiri
                {
                    ReferansId = ilk.ReferansId,
                    ReferansAd = ilk.ReferansAd,
                    CariId = ilk.CariId,
                    CariAd = ilk.CariAd,
                    GuzergahId = ilk.GuzergahId,
                    GuzergahAd = ilk.GuzergahAd,
                    EslestirmeId = ilk.EslestirmeId,
                    EslestirmeAd = ilk.EslestirmeAd,
                    AracId = ilk.AracId,
                    Plaka = ilk.Plaka,
                    SahiplikTipi = ilk.SahiplikTipi,
                    SahipAnahtari = ilk.SahipAnahtari,
                    SahipAd = ilk.SahipAd,
                    SoforId = ilk.SoforId,
                    SoforAd = ilk.SoforAd,
                    PuantajSayisi = g.Count(),
                    Sefer = g.Sum(x => x.Sefer),
                    Gelir = g.Sum(x => x.KurumUcreti),
                    Gider = g.Sum(x => x.OdenecekTutar),
                    Maliyet = g.Sum(x => x.MaliyetTutari),
                    HakedisTutari = g.Sum(x => x.HakedisTutari),
                    Uygun = ilk.Uygun,
                    UygunsuzlukNedeni = ilk.UygunsuzlukNedeni,
                    HakedisteMi = g.Key.HakedisteMi
                };
            })
            .OrderBy(s => s.ReferansAd, TrKarsilastirici)
            .ThenBy(s => s.GuzergahAd, TrKarsilastirici)
            .ThenBy(s => s.Plaka, TrKarsilastirici)
            .ThenBy(s => s.SoforAd, TrKarsilastirici)
            .ToList();

        return new HakedisKapsamOnizleme
        {
            Tip = tip,
            Kapsam = kapsam,
            Satirlar = satirlar,
            Secenekler = veri.Secenekler,
            TurDisiPuantajSayisi = veri.TurDisiSecili.Count,
            TurDisiSefer = veri.TurDisiSecili.Sum(r => r.Sefer)
        };
    }

    // ── Üretim ──────────────────────────────────────────────────────────────

    public async Task<TopluHakedisSonuc> KapsamliUretAsync(HakedisTipi tip, HakedisKapsami kapsam)
    {
        KapsamiDogrula(kapsam);

        List<KaynakSatir> uretilecek;
        var satirlar = new List<TopluHakedisSatir>();
        string kapsamOzeti;
        int uygunReferansSayisi;

        await using (var context = await _contextFactory.CreateDbContextAsync())
        {
            var veri = await KaynakVeriyiYukleAsync(context, tip, kapsam);
            kapsamOzeti = HakedisKapsamAciklayici.Ozet(kapsam, veri.Secenekler);

            var uygun = veri.Secili.Where(r => r.Uygun).ToList();
            uygunReferansSayisi = uygun.Select(r => r.ReferansId).Distinct().Count();
            uretilecek = uygun.Where(r => !veri.Kesinlesmis.Contains(r.PuantajId)).ToList();

            // Tamamı başka bir hakedişte olan referansları kullanıcıya göster.
            var uretilecekReferanslar = uretilecek.Select(r => r.ReferansId).ToHashSet();
            foreach (var grup in uygun.Where(r => !uretilecekReferanslar.Contains(r.ReferansId)).GroupBy(r => r.ReferansId))
            {
                satirlar.Add(new TopluHakedisSatir(grup.Key, grup.First().ReferansAd, "Atlandı",
                    "Kapsamdaki puantajların tamamı onaylı/faturalı bir hakedişte zaten var.",
                    null, 0m, grup.Sum(x => x.Sefer)));
            }

            // Hakedişe giremeyen satırları (ör. güzergâhta cari yok) gizleme; uyar.
            foreach (var grup in veri.Secili.Where(r => !r.Uygun).GroupBy(r => r.UygunsuzlukNedeni ?? "Uygun değil"))
            {
                satirlar.Add(new TopluHakedisSatir(0, grup.Key, "Atlandı",
                    $"{grup.Count()} puantaj kaydı hakedişe alınamadı: {grup.Key}.",
                    null, 0m, grup.Sum(x => x.Sefer)));
            }
        }

        var imza = kapsam.Imza();
        var uygunsuzGrupSayisi = satirlar.Count(s => s.ReferansId == 0);
        int uretilen = 0, atlanan = satirlar.Count, hatali = 0;
        decimal toplamTutar = 0m;

        foreach (var grup in uretilecek.GroupBy(r => r.ReferansId).OrderBy(g => g.First().ReferansAd, TrKarsilastirici))
        {
            var ad = grup.First().ReferansAd;
            try
            {
                var h = await UretKapsamliReferansAsync(tip, grup.Key, kapsam, imza, kapsamOzeti, grup.ToList(), hedefTaslakId: null);
                uretilen++;
                toplamTutar += h.GenelToplam;
                satirlar.Add(new TopluHakedisSatir(grup.Key, ad, "Üretildi", null, h.Id, h.GenelToplam, h.ToplamSeferSayisi));
            }
            catch (Exception ex)
            {
                hatali++;
                satirlar.Add(new TopluHakedisSatir(grup.Key, ad, "Hata", ex.GetBaseException().Message, null, 0m, 0m));
            }
        }

        return new TopluHakedisSonuc(
            ToplamReferans: uygunReferansSayisi + uygunsuzGrupSayisi,
            UretilenAdet: uretilen,
            AtlananAdet: atlanan,
            HataliAdet: hatali,
            ToplamTutar: toplamTutar,
            Satirlar: satirlar.OrderByDescending(s => s.Tutar).ThenBy(s => s.ReferansAd, TrKarsilastirici).ToList());
    }

    public async Task<TopluIslemSonuc> TaslaklariYenileAsync(IEnumerable<int> hakedisIds)
    {
        var ids = hakedisIds?.Distinct().ToList() ?? new List<int>();
        var satirlar = new List<TopluIslemSatir>();
        int basarili = 0, atlanan = 0, hatali = 0;

        if (ids.Count == 0)
            return new TopluIslemSonuc(0, 0, 0, 0, satirlar);

        List<(int Id, HakedisTipi Tip, int ReferansId, int Yil, int Ay, string? Parametreler, HakedisDurum Durum)> basliklar;
        await using (var context = await _contextFactory.CreateDbContextAsync())
        {
            var okunan = await context.Hakedisler.AsNoTracking()
                .Where(h => ids.Contains(h.Id))
                .Select(h => new { h.Id, h.Tip, h.ReferansId, h.Yil, h.Ay, h.GenerationParams, h.Durum })
                .ToListAsync();
            basliklar = okunan.Select(h => (h.Id, h.Tip, h.ReferansId, h.Yil, h.Ay, h.GenerationParams, h.Durum)).ToList();
        }

        var bulunanlar = basliklar.Select(b => b.Id).ToHashSet();
        foreach (var eksik in ids.Where(i => !bulunanlar.Contains(i)))
        {
            hatali++;
            satirlar.Add(new TopluIslemSatir(eksik, "Hata", "Hakediş bulunamadı."));
        }

        foreach (var b in basliklar.OrderBy(x => x.Id))
        {
            if (b.Durum != HakedisDurum.Taslak)
            {
                atlanan++;
                satirlar.Add(new TopluIslemSatir(b.Id, "Atlandı", $"Durum {b.Durum}; yalnızca Taslak yenilenebilir."));
                continue;
            }

            try
            {
                var parametreler = HakedisUretimParametreleri.Coz(b.Parametreler);
                var kapsam = parametreler?.Kapsam?.Kopyala() ?? new HakedisKapsami();
                kapsam.Yil = b.Yil;
                kapsam.Ay = b.Ay;

                List<KaynakSatir> satirKumesi;
                string ozet;
                await using (var context = await _contextFactory.CreateDbContextAsync())
                {
                    var veri = await KaynakVeriyiYukleAsync(context, b.Tip, kapsam);
                    ozet = HakedisKapsamAciklayici.Ozet(kapsam, veri.Secenekler);
                    satirKumesi = veri.Secili
                        .Where(r => r.Uygun && r.ReferansId == b.ReferansId && !veri.Kesinlesmis.Contains(r.PuantajId))
                        .ToList();
                }

                if (satirKumesi.Count == 0)
                {
                    atlanan++;
                    satirlar.Add(new TopluIslemSatir(b.Id, "Atlandı",
                        "Bu taslak için güncel puantaj satırı bulunamadı; taslak değiştirilmedi. Gerekirse silip kapsamıyla yeniden oluşturun."));
                    continue;
                }

                await UretKapsamliReferansAsync(b.Tip, b.ReferansId, kapsam, kapsam.Imza(), ozet, satirKumesi, hedefTaslakId: b.Id);
                basarili++;
                satirlar.Add(new TopluIslemSatir(b.Id, "Tamam", null));
            }
            catch (Exception ex)
            {
                hatali++;
                satirlar.Add(new TopluIslemSatir(b.Id, "Hata", ex.GetBaseException().Message));
            }
        }

        return new TopluIslemSonuc(ids.Count, basarili, atlanan, hatali, satirlar.OrderBy(s => s.HakedisId).ToList());
    }

    private async Task<Hakedis> UretKapsamliReferansAsync(
        HakedisTipi tip,
        int referansId,
        HakedisKapsami kapsam,
        string imza,
        string kapsamOzeti,
        IReadOnlyList<KaynakSatir> satirlar,
        int? hedefTaslakId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        Hakedis? hakedis;
        if (hedefTaslakId.HasValue)
        {
            hakedis = await context.Hakedisler
                .Include(h => h.Detaylar)
                .FirstOrDefaultAsync(h => h.Id == hedefTaslakId.Value && h.Durum == HakedisDurum.Taslak)
                ?? throw new InvalidOperationException("Taslak hakediş bulunamadı veya artık taslak değil.");
        }
        else
        {
            hakedis = await TaslakBulAsync(context, tip, referansId, kapsam, imza);
        }

        if (hakedis == null)
        {
            hakedis = new Hakedis
            {
                Tip = tip,
                ReferansId = referansId,
                Yil = kapsam.Yil,
                Ay = kapsam.Ay,
                Durum = HakedisDurum.Taslak,
                CreatedAt = DateTime.UtcNow
            };
            context.Hakedisler.Add(hakedis);
        }
        else
        {
            context.HakedisDetaylari.RemoveRange(hakedis.Detaylar);
            hakedis.Detaylar.Clear();
            hakedis.UpdatedAt = DateTime.UtcNow;
        }

        // Kapsam parametreleri yalnız gerektiğinde yazılır: filtre varsa (taslak kimliği) ya da hesaplama biçimi
        // varsayılandan farklıysa (müşteri kurum eşleştirmeden alındı, araç birim fiyatı başka kaynaktan).
        // Bunların dışında taslak eski kayıtlarla aynı biçimde (parametresiz) kalır.
        var parametreGerekli = imza.Length > 0
            || kapsam.MusteriKaynagi != HakedisMusteriKaynagi.Guzergah
            || kapsam.BirimFiyatKaynagi != HakedisBirimFiyatKaynagi.EslestirmeOdenen;
        hakedis.GenerationParams = parametreGerekli
            ? new HakedisUretimParametreleri { Imza = imza, Ozet = kapsamOzeti, Kapsam = kapsam.Kopyala() }.Serilestir()
            : null;

        decimal toplamSefer = 0m;
        decimal toplamTutar = 0m;

        foreach (var r in satirlar.OrderBy(x => x.Tarih).ThenBy(x => x.PuantajId))
        {
            // Tutar önizlemeyle aynı kaynaktan gelir (r.HakedisTutari); birim fiyat sefer başına etkin tutardır.
            var tutar = r.HakedisTutari;
            var birim = r.Sefer > 0 ? tutar / r.Sefer : 0m;

            hakedis.Detaylar.Add(new HakedisDetay
            {
                Hakedis = hakedis,
                Tarih = r.Tarih,
                ServisTuru = r.ServisTuru,
                AracId = r.AracId,
                SoforId = r.SoforId,
                GuzergahId = r.GuzergahId,
                FiloGunlukPuantajId = r.PuantajId,
                SeferSayisi = r.Sefer,
                BirimFiyat = birim,
                Tutar = tutar,
                CreatedAt = DateTime.UtcNow
            });

            toplamSefer += r.Sefer;
            toplamTutar += tutar;
        }

        hakedis.ToplamSeferSayisi = toplamSefer;
        hakedis.Tutar = toplamTutar;
        hakedis.BirimFiyat = toplamSefer > 0 ? Math.Round(toplamTutar / toplamSefer, 2) : 0m;
        hakedis.KdvTutar = Math.Round(toplamTutar * (hakedis.KdvOran / 100m), 2);
        hakedis.GenelToplam = toplamTutar + hakedis.KdvTutar;

        await context.SaveChangesAsync();
        return hakedis;
    }

    /// <summary>Aynı referans, dönem ve kapsam kimliğine sahip taslağı bulur.</summary>
    private static async Task<Hakedis?> TaslakBulAsync(
        ApplicationDbContext context, HakedisTipi tip, int referansId, HakedisKapsami kapsam, string imza)
    {
        var adaylar = await context.Hakedisler
            .Include(h => h.Detaylar)
            .Where(h => h.Tip == tip && h.ReferansId == referansId
                        && h.Yil == kapsam.Yil && h.Ay == kapsam.Ay
                        && h.Durum == HakedisDurum.Taslak)
            .OrderBy(h => h.Id)
            .ToListAsync();

        var eslesen = adaylar.FirstOrDefault(h => HakedisUretimParametreleri.ImzaOku(h.GenerationParams) == imza);
        if (eslesen != null)
            return eslesen;

        // Eski taslaklar kurum FirmaId'siyle anahtarlanmış olabilir; yalnızca filtresiz üretimde
        // güzergâh carisinden bulunan taslak bu cari için yeniden kullanılır.
        if (imza.Length == 0 && tip == HakedisTipi.Kurum)
        {
            var eskiler = await context.Hakedisler
                .Include(h => h.Detaylar)
                .Where(h => h.Tip == tip && h.Yil == kapsam.Yil && h.Ay == kapsam.Ay
                            && h.Durum == HakedisDurum.Taslak
                            && h.Detaylar.Any(d => d.Guzergah != null && d.Guzergah.CariId == referansId))
                .OrderBy(h => h.Id)
                .ToListAsync();

            var eski = eskiler.FirstOrDefault(h => HakedisUretimParametreleri.ImzaOku(h.GenerationParams).Length == 0);
            if (eski != null)
                eski.ReferansId = referansId;
            return eski;
        }

        return null;
    }

    // ── Onay çakışması (çifte faturalama koruması) ──────────────────────────

    /// <summary>
    /// Onaya çıkacak taslakların puantaj satırlarından herhangi biri aynı türdeki onaylı/faturalı bir hakedişte
    /// (ya da aynı toplu onayda önce onaylanan başka bir taslakta) varsa hakediş başına açıklayıcı bir mesaj döner.
    /// </summary>
    private static async Task<Dictionary<int, string>> OnayCakismalariniBulAsync(
        ApplicationDbContext context, IReadOnlyCollection<Hakedis> taslaklar)
    {
        var sonuc = new Dictionary<int, string>();
        if (taslaklar.Count == 0)
            return sonuc;

        var taslakIdleri = taslaklar.Select(t => t.Id).ToList();
        var detaylar = await context.HakedisDetaylari.AsNoTracking()
            .Where(d => taslakIdleri.Contains(d.HakedisId) && d.FiloGunlukPuantajId != null)
            .Select(d => new { d.HakedisId, PuantajId = d.FiloGunlukPuantajId!.Value })
            .ToListAsync();
        if (detaylar.Count == 0)
            return sonuc;

        var puantajIdleri = detaylar.Select(d => d.PuantajId).Distinct().ToList();
        var kesinler = await context.HakedisDetaylari.AsNoTracking()
            .Where(d => d.FiloGunlukPuantajId != null
                        && puantajIdleri.Contains(d.FiloGunlukPuantajId.Value)
                        && d.Hakedis != null
                        && KesinlesmisDurumlar.Contains(d.Hakedis.Durum))
            .Select(d => new { d.HakedisId, Tip = d.Hakedis!.Tip, PuantajId = d.FiloGunlukPuantajId!.Value })
            .ToListAsync();

        var tipByHakedis = taslaklar.ToDictionary(t => t.Id, t => t.Tip);
        var buTopluOnaydaOnaylananlar = new Dictionary<(HakedisTipi Tip, int PuantajId), int>();

        foreach (var taslak in taslaklar.OrderBy(t => t.Id))
        {
            var puantajlar = detaylar.Where(d => d.HakedisId == taslak.Id).Select(d => d.PuantajId).ToHashSet();

            var cakisanKesinler = kesinler
                .Where(k => k.Tip == taslak.Tip && puantajlar.Contains(k.PuantajId) && k.HakedisId != taslak.Id)
                .ToList();
            var cakisanTopluOnay = puantajlar
                .Where(p => buTopluOnaydaOnaylananlar.ContainsKey((tipByHakedis[taslak.Id], p)))
                .Select(p => (PuantajId: p, HakedisId: buTopluOnaydaOnaylananlar[(tipByHakedis[taslak.Id], p)]))
                .ToList();

            if (cakisanKesinler.Count > 0 || cakisanTopluOnay.Count > 0)
            {
                var digerHakedisler = cakisanKesinler.Select(k => k.HakedisId)
                    .Concat(cakisanTopluOnay.Select(c => c.HakedisId))
                    .Distinct().OrderBy(x => x).Select(x => $"#{x}");
                var adet = cakisanKesinler.Select(k => k.PuantajId)
                    .Concat(cakisanTopluOnay.Select(c => c.PuantajId))
                    .Distinct().Count();

                sonuc[taslak.Id] =
                    $"Bu hakedişteki {adet} puantaj kaydı {string.Join(", ", digerHakedisler)} numaralı onaylı/faturalı hakedişte de bulunuyor; " +
                    "onaylanırsa aynı sefer iki kez faturalanır. Taslağı 'Yenile' ile güncelleyin (kesinleşmiş kayıtlar dışarıda kalır), " +
                    "taslağı silin ya da diğer hakedişi iptal edin.";
                continue;
            }

            foreach (var p in puantajlar)
                buTopluOnaydaOnaylananlar[(taslak.Tip, p)] = taslak.Id;
        }

        return sonuc;
    }

    // ── Kaynak veri ────────────────────────────────────────────────────────

    private static void KapsamiDogrula(HakedisKapsami kapsam)
    {
        ArgumentNullException.ThrowIfNull(kapsam);
        if (kapsam.Yil < 2000 || kapsam.Ay < 1 || kapsam.Ay > 12)
            throw new ArgumentException("Geçersiz dönem (yıl/ay).");
    }

    private sealed record HamPuantaj(
        int Id, DateTime Tarih, ServisTuru ServisTuru, int GuzergahId, int AracId, int SoforId,
        int? EslestirmeId, int KurumFirmaId, decimal Sefer, decimal Carpan,
        decimal KurumUcreti, decimal TaseronUcreti, decimal? BirimMaliyet);

    private sealed record GuzergahBilgi(int Id, string GuzergahAdi, int CariId, decimal GelirFiyat, decimal GiderFiyat);

    private sealed record AracBilgi(
        int Id, string? AktifPlaka, AracSahiplikTipi SahiplikTipi, int? KiralikCariId, int? KomisyoncuCariId, int? TasimaTedarikciId);

    private sealed record SoforBilgi(int Id, string Ad, string Soyad);

    /// <summary>Eşleştirme şablonu: müşteri kurum (cari) ve sefer başına iki ücret.</summary>
    private sealed record EslestirmeBilgi(
        int Id, int CariId, int GuzergahId, int AracId, int SoforId, decimal KesilecekUcret, decimal OdenenUcret);

    /// <summary>Tek bir günlük puantaj kaydının hakedişe esas alınan, isimleri çözülmüş hali.</summary>
    private sealed class KaynakSatir
    {
        public int PuantajId { get; init; }
        public DateTime Tarih { get; init; }
        public ServisTuru ServisTuru { get; init; }

        public int ReferansId { get; set; }
        public string ReferansAd { get; set; } = string.Empty;

        public int CariId { get; init; }
        public string CariAd { get; init; } = string.Empty;
        public int GuzergahId { get; init; }
        public string GuzergahAd { get; init; } = string.Empty;
        public int EslestirmeId { get; init; }
        public string EslestirmeAd { get; init; } = string.Empty;
        public int AracId { get; init; }
        public string Plaka { get; init; } = string.Empty;
        public AracSahiplikTipi SahiplikTipi { get; init; }
        public string SahipAnahtari { get; init; } = string.Empty;
        public string SahipAd { get; init; } = string.Empty;
        public int SoforId { get; init; }
        public string SoforAd { get; init; } = string.Empty;

        public decimal Sefer { get; init; }
        public decimal Carpan { get; init; }

        /// <summary>Puantajda saklı kuruma kesilecek tahakkuk.</summary>
        public decimal KurumUcreti { get; init; }

        /// <summary>Puantajda saklı taşerona ödenecek tahakkuk (yalnız komisyon / tedarikçi araçlarında dolu).</summary>
        public decimal TaseronUcreti { get; init; }
        public decimal? BirimMaliyet { get; init; }

        /// <summary>Eşleştirmedeki "Kuruma Kesilecek Ücret" (sefer başına). Eşleştirmesiz puantajda null.</summary>
        public decimal? EslKesilecek { get; init; }

        /// <summary>Eşleştirmedeki "Taşerona / Şoföre Ödenen" ücret (sefer başına). Eşleştirmesiz puantajda null.</summary>
        public decimal? EslOdenen { get; init; }

        /// <summary>Güzergâh kartındaki gider fiyatı (sefer başına).</summary>
        public decimal GuzergahGiderFiyat { get; init; }

        public bool Uygun { get; set; } = true;
        public string? UygunsuzlukNedeni { get; set; }

        /// <summary>Özmal / kiralık araç iç maliyeti (sefer × çarpan × birim maliyet).</summary>
        public decimal MaliyetTutari => (BirimMaliyet ?? 0m) * Sefer * Carpan;

        /// <summary>Araç sahibine / tedarikçiye ödenecek etkin tutar (özmal ve diğer araçlarda 0).</summary>
        public decimal OdenecekTutar { get; set; }

        /// <summary>Araç hakedişi için seçilen birim fiyat kaynağına göre tutar.</summary>
        public decimal AracTutari { get; set; }

        /// <summary>Seçili hakediş türünün hakedişe yazacağı tutar.</summary>
        public decimal HakedisTutari { get; set; }
    }

    private static decimal SeferTutari(decimal birim, KaynakSatir r) =>
        Math.Round(birim * r.Sefer * r.Carpan, 2, MidpointRounding.AwayFromZero);

    /// <summary>
    /// Dışarıya ödenecek etkin tutar. Tedarikçi araçlarında mevcut tahakkuk aynen kullanılır (Tedarikçi hakedişiyle aynı);
    /// komisyon ve kiralık araçlarda önce eşleştirmedeki "Taşerona / Şoföre Ödenen" ücret, yoksa puantaj tahakkuku,
    /// o da yoksa güzergâh gider fiyatı esas alınır. Özmal ve diğer araçlarda ödenecek tutar yoktur.
    /// </summary>
    private static decimal OdenecekHesapla(KaynakSatir r)
    {
        switch (r.SahiplikTipi)
        {
            case AracSahiplikTipi.Tedarikci:
                return r.TaseronUcreti;

            case AracSahiplikTipi.Komisyon:
            case AracSahiplikTipi.Kiralik:
                if (r.EslOdenen is > 0)
                    return SeferTutari(r.EslOdenen.Value, r);
                if (r.TaseronUcreti > 0)
                    return r.TaseronUcreti;
                return r.GuzergahGiderFiyat > 0 ? SeferTutari(r.GuzergahGiderFiyat, r) : 0m;

            default:
                return 0m;
        }
    }

    /// <summary>
    /// Araç hakedişinin tutarı. Birim fiyat varsayılan olarak eşleştirmeden alınır; eşleştirmede ücret yoksa
    /// puantaj tahakkukuna, o da yoksa araç maliyet kaydına (özmal araçlar) düşülür.
    /// </summary>
    private static decimal AracTutariHesapla(KaynakSatir r, HakedisBirimFiyatKaynagi kaynak)
    {
        switch (kaynak)
        {
            case HakedisBirimFiyatKaynagi.MaliyetKaydi:
                return r.MaliyetTutari;

            case HakedisBirimFiyatKaynagi.EslestirmeKesilecek:
                if (r.EslKesilecek is > 0)
                    return SeferTutari(r.EslKesilecek.Value, r);
                return r.KurumUcreti > 0 ? r.KurumUcreti : r.MaliyetTutari;

            default:
                if (r.EslOdenen is > 0)
                    return SeferTutari(r.EslOdenen.Value, r);
                return r.TaseronUcreti > 0 ? r.TaseronUcreti : r.MaliyetTutari;
        }
    }

    private sealed class KapsamVerisi
    {
        /// <summary>Ayın tamamı: bu hakediş türüne girebilen ve girmeyen-ama-uyarılması-gereken satırlar.</summary>
        public List<KaynakSatir> Tumu { get; init; } = new();

        /// <summary>Kapsam filtrelerinden geçenler.</summary>
        public List<KaynakSatir> Secili { get; init; } = new();

        /// <summary>Bu türde hiç hakedişe girmeyecek, kapsama uyan satırlar (ör. tedarikçi dışı araçlar).</summary>
        public List<KaynakSatir> TurDisiSecili { get; init; } = new();

        /// <summary>Aynı türdeki onaylı/faturalı hakedişlerde bulunan puantaj id'leri.</summary>
        public HashSet<int> Kesinlesmis { get; init; } = new();

        public HakedisFiltreSecenekleri Secenekler { get; init; } = new();
    }

    private async Task<KapsamVerisi> KaynakVeriyiYukleAsync(ApplicationDbContext context, HakedisTipi tip, HakedisKapsami kapsam)
    {
        var donemBas = kapsam.DonemBaslangici;
        var donemSon = kapsam.DonemBitisi;

        // Hakedişe esas satırlar: sefer > 0 ve gitmedi / iptal olmayanlar (mevcut hakediş üretimiyle aynı kural).
        var ham = await context.FiloGunlukPuantajlar
            .AsNoTracking()
            .Where(p => p.Tarih >= donemBas && p.Tarih < donemSon && !p.IsDeleted && p.SeferSayisi > 0
                        && p.Durum != OperasyonDurumu.Gitmedi_Mazeretli
                        && p.Durum != OperasyonDurumu.Gitmedi_Mazeretsiz
                        && p.Durum != OperasyonDurumu.Iptal_KurumTarafindan)
            .Select(p => new HamPuantaj(p.Id, p.Tarih, p.ServisTuru, p.GuzergahId, p.AracId, p.SoforId,
                p.FiloGuzergahEslestirmeId, p.KurumFirmaId, p.SeferSayisi, p.PuantajCarpani,
                p.TahakkukEdenKurumUcreti, p.TahakkukEdenTaseronUcreti, p.MaliyetOzmalKiralik))
            .ToListAsync();

        var eslestirmeIdleri = ham.Where(p => p.EslestirmeId.HasValue).Select(p => p.EslestirmeId!.Value).Distinct().ToList();
        var eslestirmeler = eslestirmeIdleri.Count == 0
            ? new Dictionary<int, EslestirmeBilgi>()
            : await context.FiloGuzergahEslestirmeleri.AsNoTracking()
                .Where(e => eslestirmeIdleri.Contains(e.Id) && !e.IsDeleted)
                .Select(e => new EslestirmeBilgi(e.Id, e.KurumFirmaId, e.GuzergahId, e.AracId, e.SoforId,
                    e.KurumaKesilecekUcret, e.TaseronaOdenenUcret))
                .ToDictionaryAsync(e => e.Id);

        var guzergahIdleri = ham.Select(p => p.GuzergahId).Concat(eslestirmeler.Values.Select(e => e.GuzergahId)).Distinct().ToList();
        var aracIdleri = ham.Select(p => p.AracId).Concat(eslestirmeler.Values.Select(e => e.AracId)).Distinct().ToList();
        var soforIdleri = ham.Select(p => p.SoforId).Concat(eslestirmeler.Values.Select(e => e.SoforId)).Distinct().ToList();

        var guzergahlar = guzergahIdleri.Count == 0
            ? new Dictionary<int, GuzergahBilgi>()
            : await context.Guzergahlar.AsNoTracking()
                .Where(g => guzergahIdleri.Contains(g.Id))
                .Select(g => new GuzergahBilgi(g.Id, g.GuzergahAdi, g.CariId, g.BirimFiyat, g.GiderFiyat))
                .ToDictionaryAsync(g => g.Id);

        var araclar = aracIdleri.Count == 0
            ? new Dictionary<int, AracBilgi>()
            : await context.Araclar.AsNoTracking()
                .Where(a => aracIdleri.Contains(a.Id))
                .Select(a => new AracBilgi(a.Id, a.AktifPlaka, a.SahiplikTipi, a.KiralikCariId, a.KomisyoncuCariId, a.TasimaTedarikciId))
                .ToDictionaryAsync(a => a.Id);

        var soforler = soforIdleri.Count == 0
            ? new Dictionary<int, SoforBilgi>()
            : await context.Soforler.AsNoTracking()
                .Where(s => soforIdleri.Contains(s.Id))
                .Select(s => new SoforBilgi(s.Id, s.Ad, s.Soyad))
                .ToDictionaryAsync(s => s.Id);

        var cariIdleri = guzergahlar.Values.Select(g => g.CariId)
            .Concat(eslestirmeler.Values.Select(e => e.CariId))
            .Concat(ham.Select(p => p.KurumFirmaId))
            .Concat(araclar.Values.Where(a => a.KiralikCariId.HasValue).Select(a => a.KiralikCariId!.Value))
            .Concat(araclar.Values.Where(a => a.KomisyoncuCariId.HasValue).Select(a => a.KomisyoncuCariId!.Value))
            .Where(id => id > 0)
            .Distinct()
            .ToList();
        var cariAdlari = cariIdleri.Count == 0
            ? new Dictionary<int, string>()
            : await context.Cariler.AsNoTracking()
                .Where(c => cariIdleri.Contains(c.Id))
                .Select(c => new { c.Id, c.Unvan })
                .ToDictionaryAsync(c => c.Id, c => c.Unvan);

        var tedarikciIdleri = araclar.Values.Where(a => a.TasimaTedarikciId.HasValue)
            .Select(a => a.TasimaTedarikciId!.Value).Distinct().ToList();
        var tedarikciAdlari = tedarikciIdleri.Count == 0
            ? new Dictionary<int, string>()
            : await context.TasimaTedarikciler.AsNoTracking()
                .Where(t => tedarikciIdleri.Contains(t.Id))
                .Select(t => new { t.Id, t.Unvan })
                .ToDictionaryAsync(t => t.Id, t => t.Unvan);

        string CariAdi(int id) => id > 0 && cariAdlari.TryGetValue(id, out var ad) ? ad : id > 0 ? $"Cari #{id}" : "Cari tanımsız";
        string Plaka(int id) => araclar.TryGetValue(id, out var a) && !string.IsNullOrWhiteSpace(a.AktifPlaka) ? a.AktifPlaka! : $"Araç #{id}";
        string SoforAdi(int id) => soforler.TryGetValue(id, out var s) ? $"{s.Ad} {s.Soyad}".Trim() : $"Şoför #{id}";
        string GuzergahAdi(int id) => guzergahlar.TryGetValue(id, out var g) && !string.IsNullOrWhiteSpace(g.GuzergahAdi) ? g.GuzergahAdi : $"Güzergâh #{id}";

        string EslestirmeAdi(int id)
        {
            if (id <= 0)
                return "Eşleştirmesiz (elle girilen)";
            if (!eslestirmeler.TryGetValue(id, out var e))
                return $"Eşleştirme #{id}";
            return $"{CariAdi(e.CariId)} • {GuzergahAdi(e.GuzergahId)} • {Plaka(e.AracId)} • {SoforAdi(e.SoforId)}";
        }

        var tumu = new List<KaynakSatir>();
        var turDisi = new List<KaynakSatir>();

        var musteriEslestirmeden = kapsam.MusteriKaynagi == HakedisMusteriKaynagi.Eslestirme;

        foreach (var p in ham)
        {
            guzergahlar.TryGetValue(p.GuzergahId, out var guzergah);
            araclar.TryGetValue(p.AracId, out var arac);
            EslestirmeBilgi? eslestirme = null;
            if (p.EslestirmeId.HasValue)
                eslestirmeler.TryGetValue(p.EslestirmeId.Value, out eslestirme);

            // Müşteri kurum: eşleştirmedeki "Müşteri Kurum" (eşleştirmesiz puantajda puantajın kendi müşterisi) ya da
            // güzergâh kartındaki cari.
            var cariId = musteriEslestirmeden
                ? (eslestirme is { CariId: > 0 } ? eslestirme.CariId : p.KurumFirmaId)
                : guzergah?.CariId ?? 0;
            var (sahipAnahtari, sahipAd, sahiplik) = SahipCoz(arac, cariAdlari, tedarikciAdlari);
            var eslestirmeId = p.EslestirmeId ?? 0;

            var satir = new KaynakSatir
            {
                PuantajId = p.Id,
                Tarih = p.Tarih,
                ServisTuru = p.ServisTuru,
                CariId = cariId,
                CariAd = CariAdi(cariId),
                GuzergahId = p.GuzergahId,
                GuzergahAd = GuzergahAdi(p.GuzergahId),
                EslestirmeId = eslestirmeId,
                EslestirmeAd = EslestirmeAdi(eslestirmeId),
                AracId = p.AracId,
                Plaka = Plaka(p.AracId),
                SahiplikTipi = sahiplik,
                SahipAnahtari = sahipAnahtari,
                SahipAd = sahipAd,
                SoforId = p.SoforId,
                SoforAd = SoforAdi(p.SoforId),
                Sefer = p.Sefer,
                Carpan = p.Carpan,
                KurumUcreti = p.KurumUcreti,
                TaseronUcreti = p.TaseronUcreti,
                BirimMaliyet = p.BirimMaliyet,
                EslKesilecek = eslestirme?.KesilecekUcret,
                EslOdenen = eslestirme?.OdenenUcret,
                GuzergahGiderFiyat = guzergah?.GiderFiyat ?? 0m
            };

            satir.OdenecekTutar = OdenecekHesapla(satir);
            satir.AracTutari = AracTutariHesapla(satir, kapsam.BirimFiyatKaynagi);

            switch (tip)
            {
                case HakedisTipi.Kurum:
                    satir.ReferansId = cariId;
                    satir.ReferansAd = satir.CariAd;
                    satir.HakedisTutari = satir.KurumUcreti;
                    if (cariId <= 0)
                    {
                        satir.Uygun = false;
                        satir.UygunsuzlukNedeni = musteriEslestirmeden
                            ? "Eşleştirmede müşteri kurum tanımlı değil"
                            : "Güzergâh kartında cari tanımlı değil";
                    }
                    tumu.Add(satir);
                    break;

                case HakedisTipi.Tedarikci:
                    if (arac is { SahiplikTipi: AracSahiplikTipi.Tedarikci, TasimaTedarikciId: > 0 })
                    {
                        var tedarikciId = arac.TasimaTedarikciId!.Value;
                        satir.ReferansId = tedarikciId;
                        satir.ReferansAd = tedarikciAdlari.TryGetValue(tedarikciId, out var tedarikciAd) ? tedarikciAd : $"Tedarikçi #{tedarikciId}";
                        satir.HakedisTutari = satir.OdenecekTutar;
                        tumu.Add(satir);
                    }
                    else
                    {
                        turDisi.Add(satir);
                    }
                    break;

                case HakedisTipi.AracSahibi:
                    // Komisyon ve kiralık araçların sahibi bir cari kartıdır; hakediş bu cariye yapılır.
                    if (arac is { SahiplikTipi: AracSahiplikTipi.Komisyon or AracSahiplikTipi.Kiralik })
                    {
                        var sahipCariId = arac.SahiplikTipi == AracSahiplikTipi.Komisyon ? arac.KomisyoncuCariId : arac.KiralikCariId;
                        satir.HakedisTutari = satir.OdenecekTutar;
                        if (sahipCariId is > 0)
                        {
                            satir.ReferansId = sahipCariId.Value;
                            satir.ReferansAd = CariAdi(sahipCariId.Value);
                        }
                        else
                        {
                            satir.ReferansAd = "Araç sahibi tanımsız";
                            satir.Uygun = false;
                            satir.UygunsuzlukNedeni = arac.SahiplikTipi == AracSahiplikTipi.Komisyon
                                ? "Araç kartında komisyoncu (cari) tanımlı değil"
                                : "Araç kartında kiralık plaka sahibi (cari) tanımlı değil";
                        }

                        tumu.Add(satir);
                    }
                    else
                    {
                        turDisi.Add(satir);
                    }
                    break;

                case HakedisTipi.Arac:
                    satir.ReferansId = p.AracId;
                    satir.ReferansAd = satir.Plaka;
                    satir.HakedisTutari = satir.AracTutari;
                    tumu.Add(satir);
                    break;

                default:
                    turDisi.Add(satir);
                    break;
            }
        }

        var secili = KapsamdanGecir(tumu, kapsam);
        var turDisiSecili = KapsamdanGecir(turDisi, kapsam);

        var kesinlesmis = (await context.HakedisDetaylari.AsNoTracking()
                .Where(d => d.FiloGunlukPuantajId != null
                            && d.Tarih >= donemBas && d.Tarih < donemSon
                            && d.Hakedis != null && d.Hakedis.Tip == tip
                            && KesinlesmisDurumlar.Contains(d.Hakedis.Durum))
                .Select(d => d.FiloGunlukPuantajId!.Value)
                .Distinct()
                .ToListAsync())
            .ToHashSet();

        return new KapsamVerisi
        {
            Tumu = tumu,
            Secili = secili,
            TurDisiSecili = turDisiSecili,
            Kesinlesmis = kesinlesmis,
            Secenekler = SecenekleriOlustur(tumu)
        };
    }

    private static List<KaynakSatir> KapsamdanGecir(IEnumerable<KaynakSatir> satirlar, HakedisKapsami k)
    {
        var (ilk, son) = k.GunAraligi();
        var cari = k.CariIdleri.ToHashSet();
        var guzergah = k.GuzergahIdleri.ToHashSet();
        var eslestirme = k.EslestirmeIdleri.ToHashSet();
        var arac = k.AracIdleri.ToHashSet();
        var sofor = k.SoforIdleri.ToHashSet();
        var sahiplik = k.SahiplikTipleri.ToHashSet();
        var sahip = k.SahipAnahtarlari.ToHashSet(StringComparer.Ordinal);
        var servis = k.ServisTurleri.ToHashSet();

        return satirlar.Where(r =>
                r.Tarih.Date >= ilk && r.Tarih.Date <= son
                && (cari.Count == 0 || cari.Contains(r.CariId))
                && (guzergah.Count == 0 || guzergah.Contains(r.GuzergahId))
                && (eslestirme.Count == 0 || eslestirme.Contains(r.EslestirmeId))
                && (arac.Count == 0 || arac.Contains(r.AracId))
                && (sofor.Count == 0 || sofor.Contains(r.SoforId))
                && (sahiplik.Count == 0 || sahiplik.Contains(r.SahiplikTipi))
                && (sahip.Count == 0 || sahip.Contains(r.SahipAnahtari))
                && (servis.Count == 0 || servis.Contains(r.ServisTuru)))
            .ToList();
    }

    /// <summary>Araç sahibini sahiplik tipine göre çözer: özmal, kiralık/komisyon (cari) ya da tedarikçi.</summary>
    private static (string Anahtar, string Ad, AracSahiplikTipi Tip) SahipCoz(
        AracBilgi? arac, IReadOnlyDictionary<int, string> cariAdlari, IReadOnlyDictionary<int, string> tedarikciAdlari)
    {
        if (arac == null)
            return ("D", "Bilinmeyen araç sahibi", AracSahiplikTipi.Diger);

        switch (arac.SahiplikTipi)
        {
            case AracSahiplikTipi.Ozmal:
                return ("O", "Özmal (firma aracı)", AracSahiplikTipi.Ozmal);

            case AracSahiplikTipi.Kiralik:
                return arac.KiralikCariId is > 0 and var kiralikCari
                    ? ($"K:{kiralikCari}", cariAdlari.GetValueOrDefault(kiralikCari) ?? $"Cari #{kiralikCari}", AracSahiplikTipi.Kiralik)
                    : ("K:0", "Kiralık (sahip tanımsız)", AracSahiplikTipi.Kiralik);

            case AracSahiplikTipi.Komisyon:
                return arac.KomisyoncuCariId is > 0 and var komisyoncu
                    ? ($"M:{komisyoncu}", cariAdlari.GetValueOrDefault(komisyoncu) ?? $"Cari #{komisyoncu}", AracSahiplikTipi.Komisyon)
                    : ("M:0", "Komisyon (komisyoncu tanımsız)", AracSahiplikTipi.Komisyon);

            case AracSahiplikTipi.Tedarikci:
                return arac.TasimaTedarikciId is > 0 and var tedarikci
                    ? ($"T:{tedarikci}", tedarikciAdlari.GetValueOrDefault(tedarikci) ?? $"Tedarikçi #{tedarikci}", AracSahiplikTipi.Tedarikci)
                    : ("T:0", "Tedarikçi (tanımsız)", AracSahiplikTipi.Tedarikci);

            default:
                return ("D", "Diğer", AracSahiplikTipi.Diger);
        }
    }

    /// <summary>Filtre kutuları yalnızca dönemde gerçekten puantajı olan değerleri listeler.</summary>
    private static HakedisFiltreSecenekleri SecenekleriOlustur(IReadOnlyCollection<KaynakSatir> satirlar)
    {
        static List<HakedisSecenek> Sirala(IEnumerable<HakedisSecenek> secenekler) =>
            secenekler.OrderBy(s => s.Ad, TrKarsilastirici).ToList();

        return new HakedisFiltreSecenekleri
        {
            Cariler = Sirala(satirlar.Where(r => r.CariId > 0)
                .DistinctBy(r => r.CariId)
                .Select(r => new HakedisSecenek(r.CariId.ToString(), r.CariAd))),

            Guzergahlar = Sirala(satirlar
                .DistinctBy(r => r.GuzergahId)
                .Select(r => new HakedisSecenek(r.GuzergahId.ToString(), r.GuzergahAd, r.CariAd, r.CariId.ToString()))),

            Eslestirmeler = Sirala(satirlar
                .DistinctBy(r => r.EslestirmeId)
                .Select(r => new HakedisSecenek(r.EslestirmeId.ToString(), r.EslestirmeAd))),

            Araclar = Sirala(satirlar
                .DistinctBy(r => r.AracId)
                .Select(r => new HakedisSecenek(r.AracId.ToString(), r.Plaka, SahiplikHelper.GetMetin(r.SahiplikTipi), null, r.SahiplikTipi))),

            Soforler = Sirala(satirlar
                .DistinctBy(r => r.SoforId)
                .Select(r => new HakedisSecenek(r.SoforId.ToString(), r.SoforAd))),

            Sahipler = Sirala(satirlar
                .DistinctBy(r => r.SahipAnahtari)
                .Select(r => new HakedisSecenek(r.SahipAnahtari, r.SahipAd, SahiplikHelper.GetMetin(r.SahiplikTipi), null, r.SahiplikTipi))),

            SahiplikTipleri = satirlar.Select(r => r.SahiplikTipi).Distinct().OrderBy(t => (int)t).ToList(),
            ServisTurleri = satirlar.Select(r => r.ServisTuru).Distinct().OrderBy(t => (int)t).ToList()
        };
    }
}
