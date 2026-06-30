using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using MKFiloServis.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
using System.Globalization;

namespace MKFiloServis.Web.Services;

public class BordroService : IBordroService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IMuhasebeService _muhasebeService;
    private readonly IAktifFirmaProvider _firmaProvider;
    private static readonly CultureInfo TrCulture = new("tr-TR");

    public BordroService(IDbContextFactory<ApplicationDbContext> contextFactory, IMuhasebeService muhasebeService, IAktifFirmaProvider firmaProvider)
    {
        _contextFactory = contextFactory;
        _muhasebeService = muhasebeService;
        _firmaProvider = firmaProvider;
    }

    #region Bordro Listeleme

    public async Task<List<Bordro>> GetBordrolarAsync(int? firmaId = null, int? yil = null, BordroTipi? tip = null)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        var aktifFirmaId = firmaId ?? _firmaProvider.AktifFirmaId;

        var query = context.Bordrolar
            .Include(b => b.Firma)
            .Include(b => b.BordroDetaylar)
            .AsQueryable();

        if (aktifFirmaId.HasValue)
            query = query.Where(b => b.FirmaId == aktifFirmaId);

        if (yil.HasValue)
            query = query.Where(b => b.Yil == yil);

        if (tip.HasValue)
            query = query.Where(b => b.BordroTipi == tip);

        return await query.OrderByDescending(b => b.Yil).ThenByDescending(b => b.Ay).ToListAsync();
    }

    public async Task<Bordro?> GetBordroByIdAsync(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        var aktifFirmaId = _firmaProvider.AktifFirmaId;

        return await context.Bordrolar
            .Include(b => b.Firma)
            .Include(b => b.BordroDetaylar)
                .ThenInclude(d => d.Personel)
            .Include(b => b.BordroDetaylar)
                .ThenInclude(d => d.Firma)
            .FirstOrDefaultAsync(b => b.Id == id && b.FirmaId == aktifFirmaId);
    }

    public async Task<Bordro?> GetBordroByDönemAsync(int yil, int ay, int? firmaId, BordroTipi tip)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        
        var query = context.Bordrolar
            .Include(b => b.Firma)
            .Include(b => b.BordroDetaylar)
                .ThenInclude(d => d.Personel)
            .Where(b => b.Yil == yil && b.Ay == ay && b.BordroTipi == tip);

        var aktifFirmaId = firmaId ?? _firmaProvider.AktifFirmaId;
        if (aktifFirmaId.HasValue)
            query = query.Where(b => b.FirmaId == aktifFirmaId);

        return await query.FirstOrDefaultAsync();
    }

    #endregion

    #region Bordro Oluşturma ve Hesaplama

    public async Task<Bordro> CreateBordroAsync(int yil, int ay, int? firmaId, BordroTipi tip)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        
        // Aynı dönem için bordro var mı kontrol et
        var mevcutBordro = await GetBordroByDönemAsync(yil, ay, firmaId, tip);
        if (mevcutBordro != null)
            throw new InvalidOperationException($"{yil}/{ay} dönemi için {tip} bordro zaten mevcut!");

        var bordro = new Bordro
        {
            Yil = yil,
            Ay = ay,
            FirmaId = firmaId,
            BordroTipi = tip,
            HesaplamaTarihi = DateTime.Now,
            Onaylandi = false
        };

        context.Bordrolar.Add(bordro);
        await context.SaveChangesAsync();

        // Bordroyu hesapla
        await HesaplaBordroAsync(bordro.Id);

        return bordro;
    }

    public async Task<bool> HesaplaBordroAsync(int bordroId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        
        var bordro = await context.Bordrolar
            .Include(b => b.BordroDetaylar)
            .FirstOrDefaultAsync(b => b.Id == bordroId);

        if (bordro == null)
            throw new InvalidOperationException("Bordro bulunamadı!");

        if (bordro.Onaylandi)
            throw new InvalidOperationException("Onaylı bordro yeniden hesaplanamaz!");

        // Ayarları al
        var ayarlar = await GetBordroAyarAsync(bordro.FirmaId);
        var donemBaslangic = new DateTime(bordro.Yil, bordro.Ay, 1);
        var donemBitis = new DateTime(bordro.Yil, bordro.Ay, DateTime.DaysInMonth(bordro.Yil, bordro.Ay));

        // Mevcut detayları temizle
        context.BordroDetaylar.RemoveRange(bordro.BordroDetaylar);
        await context.SaveChangesAsync();

        var personelQuery = context.Soforler.AsQueryable();

        // Not: Sofor entity'sinde FirmaId yok, tüm aktif personeller alınır
        // Firma bilgisi bordro detayında tutulur

        // Bordro tipine göre filtrele (SGKBordroDahilMi ve BordroTipiPersonel'e göre)
        personelQuery = personelQuery.Where(s => s.SGKBordroDahilMi);

        if (bordro.BordroTipi == BordroTipi.Arge)
            personelQuery = personelQuery.Where(s => s.BordroTipiPersonel == PersonelBordroTipi.Arge);
        else if (bordro.BordroTipi == BordroTipi.Normal)
            personelQuery = personelQuery.Where(s => s.BordroTipiPersonel == PersonelBordroTipi.Normal);

        var personeller = (await personelQuery.ToListAsync())
            .Where(p => PersonelBordroyaDahilEdilsinMi(p, donemBaslangic, donemBitis))
            .ToList();

        if (!personeller.Any())
            throw new InvalidOperationException("Hesaplanacak personel bulunamadı!");

        // Her personel için detay oluştur
        foreach (var personel in personeller)
        {
            var sgkMaasOrani = GetDonemMaasOrani(personel.SgkCikisTarihi, donemBaslangic, donemBitis);
            var gercekMaasOrani = GetDonemMaasOrani(personel.IstenAyrilmaTarihi, donemBaslangic, donemBitis);
            var resmiNetMaasTam = personel.ResmiNetMaas > 0 || personel.DigerMaas > 0
                ? personel.ResmiNetMaas
                : personel.NetMaas;
            var resmiNetMaas = YuvarlaTutar(resmiNetMaasTam * sgkMaasOrani);
            var digerMaas = YuvarlaTutar(personel.DigerMaas * gercekMaasOrani);
            var brutMaas = YuvarlaTutar(personel.BrutMaas * sgkMaasOrani);

            var detay = new BordroDetay
            {
                BordroId = bordro.Id,
                PersonelId = personel.Id,
                FirmaId = bordro.FirmaId,
                BrutMaas = brutMaas,
                NetMaas = resmiNetMaas,
                TopluMaas = resmiNetMaas + digerMaas,
                SgkMaasi = resmiNetMaas,
                EkOdeme = digerMaas,
                // Sosyal Yardımlar (personelden al)
                YemekYardimi = YuvarlaTutar(personel.YemekYardimi * sgkMaasOrani),
                YolYardimi = YuvarlaTutar(personel.YolYardimi * sgkMaasOrani),
                AileYardimi = YuvarlaTutar(personel.AileYardimi * sgkMaasOrani),
                // Özel Kesintiler (personelden al)
                IcraKesintisi = YuvarlaTutar(personel.IcraKesintisi * sgkMaasOrani),
                BESKesintisi = YuvarlaTutar(personel.BESKesintisi * sgkMaasOrani),
                SendikaKesintisi = YuvarlaTutar(personel.SendikaKesintisi * sgkMaasOrani),
                HayatSigortasi = YuvarlaTutar(personel.HayatSigortasi * sgkMaasOrani),
                BireyselEmeklilik = YuvarlaTutar(personel.BireyselEmeklilik * sgkMaasOrani),
                DigerOzelKesinti = YuvarlaTutar(personel.DigerOzelKesinti * sgkMaasOrani)
            };

            // İşçi SGK Kesintileri (ayrı ayrı)
            detay.SgkIsciPrim = YuvarlaTutar(detay.SgkMaasi * ayarlar.SgkIsciPayiOrani / 100);
            detay.IssizlikIsciPrim = YuvarlaTutar(detay.SgkMaasi * ayarlar.IssizlikIsciPayiOrani / 100);
            detay.SgkIssizlikKesinti = detay.SgkIsciPrim + detay.IssizlikIsciPrim;

            // İşveren SGK Maliyeti
            var isverenSgkOrani = ayarlar.SgkIsverenPayiOrani;
            if (ayarlar.Sgk5PuanIndirimVarMi)
                isverenSgkOrani -= 5; // 5 puan SGK indirimi
            detay.SgkIsverenPrim = YuvarlaTutar(detay.SgkMaasi * isverenSgkOrani / 100);
            detay.IssizlikIsverenPrim = YuvarlaTutar(detay.SgkMaasi * ayarlar.IssizlikIsverenPayiOrani / 100);

            // Gelir Vergisi Matrahı
            var gelirVergisiMatrahi = detay.SgkMaasi - detay.SgkIssizlikKesinti;
            // BES/Hayat sigortası vergi matrahından düşülür (brütün %15'ine kadar)
            var sigortaIndirimi = Math.Min(detay.BireyselEmeklilik + detay.HayatSigortasi, brutMaas * 0.15M);
            gelirVergisiMatrahi -= sigortaIndirimi;
            if (gelirVergisiMatrahi < 0) gelirVergisiMatrahi = 0;

            // Kümülatif vergi matrahı hesapla (önceki ayların toplamı)
            var kumulatifOncekiAylar = await GetKumulatifVergiMatrahiAsync(context, personel.Id, bordro.Yil, bordro.Ay);
            var kumulatifYeni = kumulatifOncekiAylar + gelirVergisiMatrahi;
            detay.KumulatifVergiMatrahi = kumulatifYeni;

            // Kademeli gelir vergisi hesaplama
            detay.GelirVergisi = HesaplaKademeliGelirVergisi(ayarlar, kumulatifOncekiAylar, gelirVergisiMatrahi, out int vergiDilimi);
            detay.UygulananVergiDilimi = vergiDilimi;

            // Damga vergisi
            detay.DamgaVergisi = YuvarlaTutar(detay.SgkMaasi * ayarlar.DamgaVergisiOrani / 100);

            // Ek ödeme bordroda diğer maaş olarak taşınır
            detay.TopluMaas = detay.NetMaas + detay.EkOdeme;

            context.BordroDetaylar.Add(detay);
        }

        await context.SaveChangesAsync();

        // Bordro özetini güncelle
        await UpdateBordroOzetAsync(bordro.Id);

        return true;
    }

    /// <summary>
    /// Kümülatif gelir vergisi matrahını hesaplar (yıl içi önceki aylar toplamı)
    /// </summary>
    private async Task<decimal> GetKumulatifVergiMatrahiAsync(ApplicationDbContext context, int personelId, int yil, int ay)
    {
        // Önceki ayların gelir vergisi matrahı toplamını al
        var oncekiAylarMatrahi = await context.BordroDetaylar
            .Include(d => d.Bordro)
            .Where(d => d.Bordro.Yil == yil && d.Bordro.Ay < ay && d.PersonelId == personelId)
            .SumAsync(d => (decimal?)(d.SgkMaasi - d.SgkIssizlikKesinti)) ?? 0;

        return oncekiAylarMatrahi;
    }

    /// <summary>
    /// Kademeli gelir vergisi hesaplama (RaptinBordro gibi dilimli hesap)
    /// </summary>
    private static decimal HesaplaKademeliGelirVergisi(BordroAyar ayar, decimal kumulatifOnceki, decimal aylikMatrah, out int dilim)
    {
        var dilimler = new (decimal sinir, decimal oran)[]
        {
            (ayar.GelirVergisiDilim1Sinir, ayar.GelirVergisiDilim1Oran),
            (ayar.GelirVergisiDilim2Sinir, ayar.GelirVergisiDilim2Oran),
            (ayar.GelirVergisiDilim3Sinir, ayar.GelirVergisiDilim3Oran),
            (ayar.GelirVergisiDilim4Sinir, ayar.GelirVergisiDilim4Oran),
            (decimal.MaxValue, ayar.GelirVergisiDilim5Oran)
        };

        decimal vergi = 0;
        decimal kalan = aylikMatrah;
        decimal pozisyon = kumulatifOnceki;
        dilim = 1;

        foreach (var (sinir, oran) in dilimler)
        {
            if (kalan <= 0) break;

            var dilimdeKalanBosluk = sinir - pozisyon;
            if (dilimdeKalanBosluk <= 0)
            {
                dilim++;
                continue;
            }

            var buDilimdekiTutar = Math.Min(kalan, dilimdeKalanBosluk);
            vergi += buDilimdekiTutar * oran / 100;
            kalan -= buDilimdekiTutar;
            pozisyon += buDilimdekiTutar;

            if (kalan > 0) dilim++;
        }

        return Math.Round(vergi, 2);
    }

    /// <summary>
    /// Netten brüte hesaplama (iteratif yaklaşım - RaptinBordro benzeri)
    /// </summary>
    public async Task<NettenBruteHesapSonucu> NettenBruteHesaplaAsync(decimal netMaas, int? firmaId, decimal kumulatifVergiMatrahi = 0)
    {
        var ayar = await GetBordroAyarAsync(firmaId);

        // İteratif hesaplama: brüt tahminiyle başla, yakınsa
        var sgkIsciOrani = (ayar.SgkIsciPayiOrani + ayar.IssizlikIsciPayiOrani) / 100;
        var damgaOrani = ayar.DamgaVergisiOrani / 100;

        // İlk tahmin: brüt ≈ net / (1 - sgk - damga - ortalama vergi)
        decimal brut = netMaas / (1 - sgkIsciOrani - damgaOrani - 0.15M);

        // 50 iterasyonla yakınsa
        for (int i = 0; i < 50; i++)
        {
            var sgkIsci = YuvarlaTutar(brut * ayar.SgkIsciPayiOrani / 100);
            var issizlikIsci = YuvarlaTutar(brut * ayar.IssizlikIsciPayiOrani / 100);
            var sgkToplam = sgkIsci + issizlikIsci;

            var gelirVergisiMatrahi = brut - sgkToplam;
            if (gelirVergisiMatrahi < 0) gelirVergisiMatrahi = 0;

            var gelirVergisi = HesaplaKademeliGelirVergisi(ayar, kumulatifVergiMatrahi, gelirVergisiMatrahi, out _);
            var damgaVergisi = YuvarlaTutar(brut * ayar.DamgaVergisiOrani / 100);

            var hesaplananNet = brut - sgkToplam - gelirVergisi - damgaVergisi;
            var fark = netMaas - hesaplananNet;

            if (Math.Abs(fark) < 0.01M)
                break;

            brut += fark;
        }

        brut = YuvarlaTutar(brut);

        // Son hesaplama
        var sonSgkIsci = YuvarlaTutar(brut * ayar.SgkIsciPayiOrani / 100);
        var sonIssizlikIsci = YuvarlaTutar(brut * ayar.IssizlikIsciPayiOrani / 100);
        var sonGelirMatrahi = brut - sonSgkIsci - sonIssizlikIsci;
        if (sonGelirMatrahi < 0) sonGelirMatrahi = 0;
        var sonGelirVergisi = HesaplaKademeliGelirVergisi(ayar, kumulatifVergiMatrahi, sonGelirMatrahi, out int dilim);
        var sonDamgaVergisi = YuvarlaTutar(brut * ayar.DamgaVergisiOrani / 100);

        var isverenSgkOrani = ayar.SgkIsverenPayiOrani;
        if (ayar.Sgk5PuanIndirimVarMi)
            isverenSgkOrani -= 5;

        return new NettenBruteHesapSonucu
        {
            IstenenNetMaas = netMaas,
            HesaplananBrutMaas = brut,
            SgkIsciPrim = sonSgkIsci,
            IssizlikIsciPrim = sonIssizlikIsci,
            GelirVergisiMatrahi = sonGelirMatrahi,
            GelirVergisi = sonGelirVergisi,
            DamgaVergisi = sonDamgaVergisi,
            SgkIsverenPrim = YuvarlaTutar(brut * isverenSgkOrani / 100),
            IssizlikIsverenPrim = YuvarlaTutar(brut * ayar.IssizlikIsverenPayiOrani / 100),
            UygulananVergiDilimi = dilim,
            KumulatifVergiMatrahi = kumulatifVergiMatrahi + sonGelirMatrahi
        };
    }

    private static bool PersonelBordroyaDahilEdilsinMi(Sofor personel, DateTime donemBaslangic, DateTime donemBitis)
    {
        if (personel.Aktif)
            return true;

        return TarihDonemIcinde(personel.IstenAyrilmaTarihi, donemBaslangic, donemBitis)
            || TarihDonemIcinde(personel.SgkCikisTarihi, donemBaslangic, donemBitis);
    }

    private static decimal GetDonemMaasOrani(DateTime? cikisTarihi, DateTime donemBaslangic, DateTime donemBitis)
    {
        if (!cikisTarihi.HasValue)
            return 1m;

        var tarih = cikisTarihi.Value.Date;
        if (tarih < donemBaslangic.Date)
            return 0m;

        if (tarih > donemBitis.Date)
            return 1m;

        var donemGunSayisi = DateTime.DaysInMonth(donemBaslangic.Year, donemBaslangic.Month);
        return tarih.Day / (decimal)donemGunSayisi;
    }

    private static bool TarihDonemIcinde(DateTime? tarih, DateTime donemBaslangic, DateTime donemBitis)
    {
        return tarih.HasValue && tarih.Value.Date >= donemBaslangic.Date && tarih.Value.Date <= donemBitis.Date;
    }

    private static decimal YuvarlaTutar(decimal tutar)
    {
        return Math.Round(tutar, 2, MidpointRounding.AwayFromZero);
    }

    private async Task UpdateBordroOzetAsync(int bordroId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        
        var bordro = await context.Bordrolar
            .Include(b => b.BordroDetaylar)
            .FirstOrDefaultAsync(b => b.Id == bordroId);

        if (bordro == null) return;

        bordro.ToplamPersonelSayisi = bordro.BordroDetaylar.Count;
        bordro.ToplamBrutMaas = bordro.BordroDetaylar.Sum(d => d.BrutMaas);
        bordro.ToplamNetMaas = bordro.BordroDetaylar.Sum(d => d.NetMaas);
        bordro.ToplamSgkMatrahi = bordro.BordroDetaylar.Sum(d => d.SgkMaasi);
        bordro.ToplamEkOdeme = bordro.BordroDetaylar.Sum(d => d.EkOdeme);

        await context.SaveChangesAsync();
    }

    public async Task<bool> OnaylaBordroAsync(int bordroId, string onaylayanKullanici)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        
        var bordro = await context.Bordrolar
            .Include(b => b.BordroDetaylar)
            .FirstOrDefaultAsync(b => b.Id == bordroId);

        if (bordro == null)
            throw new InvalidOperationException("Bordro bulunamadı!");

        if (bordro.Onaylandi)
            throw new InvalidOperationException("Bordro zaten onaylı!");

        if (!bordro.BordroDetaylar.Any())
            throw new InvalidOperationException("Detaysız bordro onaylanamaz!");

        bordro.Onaylandi = true;
        bordro.OnayTarihi = DateTime.Now;
        bordro.OnaylayanKullanici = onaylayanKullanici;

        // Muhasebe fişi oluştur
        await CreateBordroMuhasebeFisiAsync(bordro);

        await context.SaveChangesAsync();

        return true;
    }

    /// <summary>
    /// Bordro onayında otomatik muhasebe mahsup fişi oluşturur.
    ///
    /// Fiş yapısı:
    ///   BORÇ:  770.xx — Personel Giderleri (Brüt + İşveren SGK + İşveren İşsizlik)
    ///   ALACAK: 335.xx.xxxx — Personellere Borçlar (her personel için ayrı satır, Net Ücret)
    ///   ALACAK: 360.xx — Ödenecek Vergi ve Fonlar (Gelir Vergisi + Damga Vergisi)
    ///   ALACAK: 361.xx — Ödenecek SGK Kesintileri (İşçi SGK + İşçi İşsizlik)
    ///   ALACAK: 368.xx — Ödenecek İşveren SGK (İşveren SGK + İşveren İşsizlik)
    /// </summary>
    private async Task CreateBordroMuhasebeFisiAsync(Bordro bordro)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        // Mükerrer kontrol: aynı bordro için zaten fiş var mı?
        var mevcutFisVar = await context.MuhasebeFisleri
            .AnyAsync(f => f.BordroId == bordro.Id && !f.IsDeleted);
        if (mevcutFisVar)
            return;

        var detaylar = bordro.BordroDetaylar.ToList();
        if (!detaylar.Any())
            throw new InvalidOperationException("Detaysız bordro için muhasebe fişi oluşturulamaz!");

        // Muhasebe ayarlarını al
        var muhasebeAyar = await context.MuhasebeAyarlari.FirstOrDefaultAsync();
        var giderHesapKodu = muhasebeAyar?.PersonelGiderHesabi ?? "770.01";
        var vergiHesapKodu = muhasebeAyar?.VergiHesabi ?? "360.01";
        var sgkHesapKodu = muhasebeAyar?.SGKHesabi ?? "361.01";
        var isverenSGKHesapKodu = muhasebeAyar?.IsverenSGKHesabi ?? "368.01";

        // Hesapları bul (bulunamazsa hata ver)
        var giderHesap = await context.MuhasebeHesaplari
            .FirstOrDefaultAsync(h => h.HesapKodu.StartsWith(giderHesapKodu) && !h.IsDeleted)
            ?? await context.MuhasebeHesaplari
                .FirstOrDefaultAsync(h => h.HesapKodu == giderHesapKodu && !h.IsDeleted);
        if (giderHesap == null)
            throw new InvalidOperationException($"Personel gider hesabı bulunamadı: {giderHesapKodu}. Lütfen Muhasebe Ayarları'ndan PersonelGiderHesabi'ni kontrol edin.");

        var vergiHesap = await context.MuhasebeHesaplari
            .FirstOrDefaultAsync(h => h.HesapKodu.StartsWith(vergiHesapKodu) && !h.IsDeleted)
            ?? await context.MuhasebeHesaplari
                .FirstOrDefaultAsync(h => h.HesapKodu == vergiHesapKodu && !h.IsDeleted);
        if (vergiHesap == null)
            throw new InvalidOperationException($"Vergi hesabı bulunamadı: {vergiHesapKodu}. Lütfen Muhasebe Ayarları'ndan VergiHesabi'ni kontrol edin.");

        var sgkHesap = await context.MuhasebeHesaplari
            .FirstOrDefaultAsync(h => h.HesapKodu.StartsWith(sgkHesapKodu) && !h.IsDeleted)
            ?? await context.MuhasebeHesaplari
                .FirstOrDefaultAsync(h => h.HesapKodu == sgkHesapKodu && !h.IsDeleted);
        if (sgkHesap == null)
            throw new InvalidOperationException($"SGK hesabı bulunamadı: {sgkHesapKodu}. Lütfen Muhasebe Ayarları'ndan SGKHesabi'ni kontrol edin.");

        var isverenSGKHesap = await context.MuhasebeHesaplari
            .FirstOrDefaultAsync(h => h.HesapKodu.StartsWith(isverenSGKHesapKodu) && !h.IsDeleted)
            ?? await context.MuhasebeHesaplari
                .FirstOrDefaultAsync(h => h.HesapKodu == isverenSGKHesapKodu && !h.IsDeleted);
        if (isverenSGKHesap == null)
            throw new InvalidOperationException($"İşveren SGK hesabı bulunamadı: {isverenSGKHesapKodu}. Lütfen Muhasebe Ayarları'ndan IsverenSGKHesabi'ni kontrol edin.");

        // Personel ID'lerini topla ve personelleri yükle (335 hesapları için)
        var personelIds = detaylar.Select(d => d.PersonelId).Distinct().ToList();
        var personeller = await context.Soforler
            .Where(s => personelIds.Contains(s.Id) && !s.IsDeleted)
            .ToListAsync();

        // Her personel için 335 hesabını bul
        var personelHesapMap = new Dictionary<int, MuhasebeHesap>();
        foreach (var p in personeller)
        {
            MuhasebeHesap? personelHesap = null;
            if (p.MuhasebeHesapId.HasValue)
            {
                personelHesap = await context.MuhasebeHesaplari
                    .FirstOrDefaultAsync(h => h.Id == p.MuhasebeHesapId.Value && !h.IsDeleted);
            }

            if (personelHesap == null)
            {
                // Otomatik hesap ara: prefix + personel adı
                var personelPrefix = muhasebeAyar?.PersonelPrefix ?? "335.01";
                var tamAd = $"{p.Ad} {p.Soyad}";
                personelHesap = await context.MuhasebeHesaplari
                    .FirstOrDefaultAsync(h => h.HesapKodu.StartsWith(personelPrefix + ".") && h.HesapAdi == tamAd && !h.IsDeleted);
            }

            if (personelHesap == null)
                throw new InvalidOperationException(
                    $"Personel muhasebe hesabı bulunamadı: {p.Ad} {p.Soyad} ({p.SoforKodu}). " +
                    $"Lütfen personel kartından MuhasebeHesapId'yi atayın veya kaydedip otomatik oluşturun.");

            personelHesapMap[p.Id] = personelHesap;
        }

        // Toplamları hesapla
        var donemSonGun = DateTime.DaysInMonth(bordro.Yil, bordro.Ay);
        var fisTarihi = new DateTime(bordro.Yil, bordro.Ay, Math.Min(donemSonGun, 1));
        // Ayın son gününü kullan (maaş tahakkuk tarihi)
        fisTarihi = new DateTime(bordro.Yil, bordro.Ay, donemSonGun);

        var toplamBrut = detaylar.Sum(d => d.BrutMaas);
        var toplamIsverenSGK = detaylar.Sum(d => d.SgkIsverenPrim);
        var toplamIsverenIssizlik = detaylar.Sum(d => d.IssizlikIsverenPrim);
        var toplamIsciSGK = detaylar.Sum(d => d.SgkIsciPrim);
        var toplamIsciIssizlik = detaylar.Sum(d => d.IssizlikIsciPrim);
        var toplamGelirVergisi = detaylar.Sum(d => d.GelirVergisi);
        var toplamDamgaVergisi = detaylar.Sum(d => d.DamgaVergisi);
        var toplamNet = detaylar.Sum(d => d.NetMaas);

        // 770 BORÇ = Brüt + İşveren SGK + İşveren İşsizlik
        var borc770 = toplamBrut + toplamIsverenSGK + toplamIsverenIssizlik;
        // ALACAK toplamı = Net + (GV+DV) + (İşçi SGK+İşsizlik) + (İşveren SGK+İşsizlik)
        var alacak335 = toplamNet; // 335 satırları
        var alacak360 = toplamGelirVergisi + toplamDamgaVergisi;
        var alacak361 = toplamIsciSGK + toplamIsciIssizlik;
        var alacak368 = toplamIsverenSGK + toplamIsverenIssizlik;

        // Fiş numarası üret
        var fisNo = await _muhasebeService.GenerateNextFisNoAsync(FisTipi.Mahsup, bordro.FirmaId ?? 0);

        // Fiş oluştur
        var fis = new MuhasebeFis
        {
            FisNo = fisNo,
            FisTarihi = fisTarihi,
            FisTipi = FisTipi.Mahsup,
            Aciklama = $"{bordro.Ay}/{bordro.Yil} Bordro Tahakkuku ({bordro.ToplamPersonelSayisi} personel)",
            ToplamBorc = borc770,
            ToplamAlacak = alacak335 + alacak360 + alacak361 + alacak368,
            Durum = FisDurum.Onaylandi,
            Kaynak = FisKaynak.Otomatik,
            KaynakId = bordro.Id,
            KaynakTip = "Bordro",
            BordroId = bordro.Id,
            CreatedAt = DateTime.UtcNow
        };

        context.MuhasebeFisleri.Add(fis);
        await context.SaveChangesAsync(); // FisId oluşsun

        int sira = 0;

        // 1) 770 BORÇ — Personel Giderleri
        sira++;
        context.MuhasebeFisKalemleri.Add(new MuhasebeFisKalem
        {
            FisId = fis.Id,
            HesapId = giderHesap.Id,
            SiraNo = sira,
            Borc = borc770,
            Alacak = 0,
            Tarih = fisTarihi,
            Aciklama = $"Brüt Ücret + İşveren SGK/İşsizlik",
            CreatedAt = DateTime.UtcNow
        });

        // 2) 335 ALACAK — Her personel için ayrı satır
        foreach (var detay in detaylar)
        {
            if (!personelHesapMap.TryGetValue(detay.PersonelId, out var personelHesap))
                continue;

            sira++;
            var personel = personeller.FirstOrDefault(p => p.Id == detay.PersonelId);
            context.MuhasebeFisKalemleri.Add(new MuhasebeFisKalem
            {
                FisId = fis.Id,
                HesapId = personelHesap.Id,
                SiraNo = sira,
                Borc = 0,
                Alacak = detay.NetMaas,
                Tarih = fisTarihi,
                Aciklama = $"Net Ücret — {personel?.TamAd ?? detay.PersonelId.ToString()}",
                CreatedAt = DateTime.UtcNow
            });
        }

        // 3) 360 ALACAK — Ödenecek Vergi ve Fonlar (Gelir Vergisi + Damga Vergisi)
        if (alacak360 > 0)
        {
            sira++;
            context.MuhasebeFisKalemleri.Add(new MuhasebeFisKalem
            {
                FisId = fis.Id,
                HesapId = vergiHesap.Id,
                SiraNo = sira,
                Borc = 0,
                Alacak = alacak360,
                Tarih = fisTarihi,
                Aciklama = $"Gelir Vergisi + Damga Vergisi",
                CreatedAt = DateTime.UtcNow
            });
        }

        // 4) 361 ALACAK — Ödenecek SGK (İşçi SGK + İşçi İşsizlik)
        if (alacak361 > 0)
        {
            sira++;
            context.MuhasebeFisKalemleri.Add(new MuhasebeFisKalem
            {
                FisId = fis.Id,
                HesapId = sgkHesap.Id,
                SiraNo = sira,
                Borc = 0,
                Alacak = alacak361,
                Tarih = fisTarihi,
                Aciklama = $"İşçi SGK Primi + İşsizlik İşçi Payı",
                CreatedAt = DateTime.UtcNow
            });
        }

        // 5) 368 ALACAK — Ödenecek İşveren SGK
        if (alacak368 > 0)
        {
            sira++;
            context.MuhasebeFisKalemleri.Add(new MuhasebeFisKalem
            {
                FisId = fis.Id,
                HesapId = isverenSGKHesap.Id,
                SiraNo = sira,
                Borc = 0,
                Alacak = alacak368,
                Tarih = fisTarihi,
                Aciklama = $"İşveren SGK Primi + İşsizlik İşveren Payı",
                CreatedAt = DateTime.UtcNow
            });
        }

        // Fiş toplamlarını güncelle (kalem toplamlarıyla eşleşsin)
        fis.ToplamBorc = fis.Kalemler.Sum(k => k.Borc);
        fis.ToplamAlacak = fis.Kalemler.Sum(k => k.Alacak);
        await context.SaveChangesAsync();
    }

    public async Task<bool> OnayIptalEtAsync(int bordroId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        
        var bordro = await context.Bordrolar
            .Include(b => b.BordroDetaylar)
            .FirstOrDefaultAsync(b => b.Id == bordroId);

        if (bordro == null)
            throw new InvalidOperationException("Bordro bulunamadı!");

        if (!bordro.Onaylandi)
            throw new InvalidOperationException("Bordro zaten onaysız!");

        // Ödeme yapılmış mı kontrol et
        var odemeYapilmis = bordro.BordroDetaylar.Any(d => d.BankaOdemesiYapildi || d.EkOdemeYapildi);
        if (odemeYapilmis)
            throw new InvalidOperationException("Ödeme yapılmış bordronun onayı iptal edilemez!");

        bordro.Onaylandi = false;
        bordro.OnayTarihi = null;
        bordro.OnaylayanKullanici = null;

        await context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteBordroAsync(int bordroId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        
        var bordro = await context.Bordrolar
            .Include(b => b.BordroDetaylar)
            .FirstOrDefaultAsync(b => b.Id == bordroId);

        if (bordro == null)
            return false;

        if (bordro.Onaylandi)
            throw new InvalidOperationException("Onaylı bordro silinemez!");

        context.Bordrolar.Remove(bordro);
        await context.SaveChangesAsync();

        return true;
    }

    #endregion

    #region Bordro Detay İşlemleri

    public async Task<List<BordroDetay>> GetBordroDetaylarAsync(int bordroId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        return await context.BordroDetaylar
            .Include(d => d.Personel)
            .Include(d => d.Firma)
            .Include(d => d.Bordro)
            .Where(d => d.BordroId == bordroId)
            .OrderBy(d => d.Personel.SiralamaNo == 0 ? int.MaxValue : d.Personel.SiralamaNo)
            .ThenBy(d => d.Personel.Ad)
            .ToListAsync();
    }

    public async Task<BordroDetay?> GetBordroDetayByIdAsync(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        
        return await context.BordroDetaylar
            .Include(d => d.Personel)
            .Include(d => d.Firma)
            .Include(d => d.Bordro)
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task UpdateBordroDetayAsync(BordroDetay detay)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        
        var mevcutDetay = await context.BordroDetaylar
            .Include(d => d.Bordro)
            .FirstOrDefaultAsync(d => d.Id == detay.Id);

        if (mevcutDetay == null)
            throw new InvalidOperationException("Detay bulunamadı!");

        if (mevcutDetay.Bordro.Onaylandi)
            throw new InvalidOperationException("Onaylı bordro detayı güncellenemez!");

        mevcutDetay.BrutMaas = detay.BrutMaas;
        mevcutDetay.NetMaas = detay.NetMaas;
        mevcutDetay.TopluMaas = detay.TopluMaas;
        mevcutDetay.SgkMaasi = detay.SgkMaasi;
        mevcutDetay.EkOdeme = detay.EkOdeme;
        mevcutDetay.SgkIssizlikKesinti = detay.SgkIssizlikKesinti;
        mevcutDetay.GelirVergisi = detay.GelirVergisi;
        mevcutDetay.DamgaVergisi = detay.DamgaVergisi;
        mevcutDetay.YemekYardimi = detay.YemekYardimi;
        mevcutDetay.YolYardimi = detay.YolYardimi;
        mevcutDetay.PrimTutar = detay.PrimTutar;
        mevcutDetay.DigerEkOdeme = detay.DigerEkOdeme;
        mevcutDetay.Notlar = detay.Notlar;

        await context.SaveChangesAsync();

        // Bordro özetini güncelle
        await UpdateBordroOzetAsync(mevcutDetay.BordroId);
    }

    public async Task<bool> SilBordroDetayAsync(int detayId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        
        var detay = await context.BordroDetaylar
            .Include(d => d.Bordro)
            .FirstOrDefaultAsync(d => d.Id == detayId);

        if (detay == null)
            return false;

        if (detay.Bordro.Onaylandi)
            throw new InvalidOperationException("Onaylı bordro detayı silinemez!");

        if (detay.BankaOdemesiYapildi || detay.EkOdemeYapildi)
            throw new InvalidOperationException("Ödeme yapılmış detay silinemez!");

        context.BordroDetaylar.Remove(detay);
        await context.SaveChangesAsync();

        // Bordro özetini güncelle
        await UpdateBordroOzetAsync(detay.BordroId);

        return true;
    }

    #endregion

    #region Ödeme İşlemleri

    public async Task<bool> BankaOdemesiYapAsync(List<int> detayIds, DateTime odemeTarihi, int? bankaHesapId, string? aciklama)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        
        var detaylar = await context.BordroDetaylar
            .Include(d => d.Bordro)
            .Where(d => detayIds.Contains(d.Id))
            .ToListAsync();

        if (!detaylar.Any())
            throw new InvalidOperationException("Detay bulunamadı!");

        if (detaylar.Any(d => !d.Bordro.Onaylandi))
            throw new InvalidOperationException("Onaysız bordro için ödeme yapılamaz!");

        if (detaylar.Any(d => d.BankaOdemesiYapildi))
            throw new InvalidOperationException("Bazı kayıtlar için zaten banka ödemesi yapılmış!");

        foreach (var detay in detaylar)
        {
            var odeme = new BordroOdeme
            {
                BordroDetayId = detay.Id,
                OdemeTipi = Shared.Entities.OdemeTipi.BankaOdemesi,
                OdemeTarihi = odemeTarihi,
                OdemeTutari = detay.NetMaas + detay.ToplamEkOdeme,
                OdemeSekli = Shared.Entities.OdemeSekli.BankaTransfer,
                BankaHesapId = bankaHesapId,
                Aciklama = aciklama ?? "SGK Maaşı Banka Ödemesi"
            };

            context.BordroOdemeler.Add(odeme);

            detay.BankaOdemesiYapildi = true;
            detay.BankaOdemeTarihi = odemeTarihi;
        }

        await context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> EkOdemeYapAsync(List<int> detayIds, DateTime odemeTarihi, OdemeSekli odemeSekli, int? bankaHesapId, string? aciklama)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        
        var detaylar = await context.BordroDetaylar
            .Include(d => d.Bordro)
            .Where(d => detayIds.Contains(d.Id))
            .ToListAsync();

        if (!detaylar.Any())
            throw new InvalidOperationException("Detay bulunamadı!");

        if (detaylar.Any(d => !d.Bordro.Onaylandi))
            throw new InvalidOperationException("Onaysız bordro için ödeme yapılamaz!");

        if (detaylar.Any(d => d.EkOdemeYapildi))
            throw new InvalidOperationException("Bazı kayıtlar için zaten ek ödeme yapılmış!");

        foreach (var detay in detaylar)
        {
            if (detay.EkOdeme <= 0)
                continue;

            var odeme = new BordroOdeme
            {
                BordroDetayId = detay.Id,
                OdemeTipi = Shared.Entities.OdemeTipi.EkOdeme,
                OdemeTarihi = odemeTarihi,
                OdemeTutari = detay.EkOdeme,
                OdemeSekli = odemeSekli,
                BankaHesapId = bankaHesapId,
                Aciklama = aciklama ?? "Ek Ödeme (Toplu Maaş Farkı)"
            };

            context.BordroOdemeler.Add(odeme);

            detay.EkOdemeYapildi = true;
            detay.EkOdemeTarihi = odemeTarihi;
        }

        await context.SaveChangesAsync();

        return true;
    }

    public async Task<List<BordroOdeme>> GetOdemelerAsync(int bordroDetayId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        
        return await context.BordroOdemeler
            .Include(o => o.BankaHesap)
            .Where(o => o.BordroDetayId == bordroDetayId)
            .OrderByDescending(o => o.OdemeTarihi)
            .ToListAsync();
    }

    #endregion

    #region Raporlama

    public async Task<byte[]> ExportBankaOdemeListesiAsync(int bordroId)
    {
        var bordro = await GetBordroByIdAsync(bordroId);
        if (bordro == null)
            throw new InvalidOperationException("Bordro bulunamadı!");

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Banka Ödeme Listesi");

        // Başlık
        worksheet.Cell("A1").Value = $"{bordro.DonemeAdi} {bordro.BordroTipi} Bordro - Banka Ödeme Listesi";
        worksheet.Range("A1:H1").Merge().Style.Font.Bold = true;
        worksheet.Range("A1:H1").Style.Font.FontSize = 14;
        worksheet.Range("A1:H1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        // Sütun başlıkları
        var header = worksheet.Row(3);
        header.Cell(1).Value = "Sıra";
        header.Cell(2).Value = "Personel Kodu";
        header.Cell(3).Value = "Ad Soyad";
        header.Cell(4).Value = "TC Kimlik No";
        header.Cell(5).Value = "IBAN";
        header.Cell(6).Value = "SGK Maaşı (Net)";
        header.Cell(7).Value = "Ek Ödemeler";
        header.Cell(8).Value = "Toplam";
        header.Cell(9).Value = "Durum";
        header.Style.Font.Bold = true;
        header.Style.Fill.BackgroundColor = XLColor.LightGray;

        // Veriler
        int row = 4;
        decimal toplamSgk = 0, toplamEk = 0, toplamGenel = 0;

        foreach (var detay in bordro.BordroDetaylar.OrderBy(d => d.Personel.SiralamaNo == 0 ? int.MaxValue : d.Personel.SiralamaNo).ThenBy(d => d.Personel.Ad))
        {
            worksheet.Cell(row, 1).Value = detay.Personel.SiralamaNo > 0 ? detay.Personel.SiralamaNo : row - 3;
            worksheet.Cell(row, 2).Value = detay.Personel.SoforKodu;
            worksheet.Cell(row, 3).Value = detay.Personel.TamAd;
            worksheet.Cell(row, 4).Value = detay.Personel.TcKimlikNo ?? "";
            worksheet.Cell(row, 5).Value = detay.Personel.IBAN ?? "";
            worksheet.Cell(row, 6).Value = detay.NetMaas;
            worksheet.Cell(row, 7).Value = detay.ToplamEkOdeme;
            worksheet.Cell(row, 8).Value = detay.NetMaas + detay.ToplamEkOdeme;
            worksheet.Cell(row, 9).Value = detay.BankaOdemesiYapildi ? "Ödendi" : "Bekliyor";

            toplamSgk += detay.NetMaas;
            toplamEk += detay.ToplamEkOdeme;
            toplamGenel += detay.NetMaas + detay.ToplamEkOdeme;

            row++;
        }

        // Toplam
        worksheet.Cell(row, 5).Value = "TOPLAM:";
        worksheet.Cell(row, 6).Value = toplamSgk;
        worksheet.Cell(row, 7).Value = toplamEk;
        worksheet.Cell(row, 8).Value = toplamGenel;
        worksheet.Range(row, 5, row, 8).Style.Font.Bold = true;
        worksheet.Range(row, 5, row, 8).Style.Fill.BackgroundColor = XLColor.LightYellow;

        // Para formatı
        worksheet.Range(4, 6, row, 8).Style.NumberFormat.Format = "#,##0.00 ₺";

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<byte[]> ExportEkOdemeListesiAsync(int bordroId)
    {
        var bordro = await GetBordroByIdAsync(bordroId);
        if (bordro == null)
            throw new InvalidOperationException("Bordro bulunamadı!");

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Ek Ödeme Listesi");

        // Başlık
        worksheet.Cell("A1").Value = $"{bordro.DonemeAdi} {bordro.BordroTipi} Bordro - Ek Ödeme Listesi (Toplu Maaş Farkı)";
        worksheet.Range("A1:H1").Merge().Style.Font.Bold = true;
        worksheet.Range("A1:H1").Style.Font.FontSize = 14;
        worksheet.Range("A1:H1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        // Sütun başlıkları
        var header = worksheet.Row(3);
        header.Cell(1).Value = "Personel Kodu";
        header.Cell(2).Value = "Ad Soyad";
        header.Cell(3).Value = "Toplu Maaş";
        header.Cell(4).Value = "SGK Maaşı";
        header.Cell(5).Value = "Ek Ödeme";
        header.Cell(6).Value = "Ödeme Şekli";
        header.Cell(7).Value = "Durum";
        header.Style.Font.Bold = true;
        header.Style.Fill.BackgroundColor = XLColor.LightGray;

        // Veriler
        int row = 4;
        decimal toplamTopluMaas = 0, toplamSgkMaas = 0, toplamEkOdeme = 0;

        var detaylarWithEkOdeme = bordro.BordroDetaylar.Where(d => d.EkOdeme > 0)
                    .OrderBy(d => d.Personel.SiralamaNo == 0 ? int.MaxValue : d.Personel.SiralamaNo)
                    .ThenBy(d => d.Personel.Ad);

        foreach (var detay in detaylarWithEkOdeme)
        {
            worksheet.Cell(row, 1).Value = detay.Personel.SoforKodu;
            worksheet.Cell(row, 2).Value = detay.Personel.TamAd;
            worksheet.Cell(row, 3).Value = detay.TopluMaas;
            worksheet.Cell(row, 4).Value = detay.SgkMaasi;
            worksheet.Cell(row, 5).Value = detay.EkOdeme;
            worksheet.Cell(row, 6).Value = ""; // Ödeme şekli sonra girilecek
            worksheet.Cell(row, 7).Value = detay.EkOdemeYapildi ? "Ödendi" : "Bekliyor";

            toplamTopluMaas += detay.TopluMaas;
            toplamSgkMaas += detay.SgkMaasi;
            toplamEkOdeme += detay.EkOdeme;

            row++;
        }

        // Toplam
        worksheet.Cell(row, 2).Value = "TOPLAM:";
        worksheet.Cell(row, 3).Value = toplamTopluMaas;
        worksheet.Cell(row, 4).Value = toplamSgkMaas;
        worksheet.Cell(row, 5).Value = toplamEkOdeme;
        worksheet.Range(row, 2, row, 5).Style.Font.Bold = true;
        worksheet.Range(row, 2, row, 5).Style.Fill.BackgroundColor = XLColor.LightYellow;

        // Para formatı
        worksheet.Range(4, 3, row, 5).Style.NumberFormat.Format = "#,##0.00 ₺";

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<byte[]> ExportTumBordroAsync(int bordroId)
    {
        var bordro = await GetBordroByIdAsync(bordroId);
        if (bordro == null)
            throw new InvalidOperationException("Bordro bulunamadı!");

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Bordro Detayı");

        // Başlık
        worksheet.Cell("A1").Value = $"{bordro.DonemeAdi} {bordro.BordroTipi} Bordro - Detaylı Liste";
        worksheet.Range("A1:O1").Merge().Style.Font.Bold = true;
        worksheet.Range("A1:O1").Style.Font.FontSize = 14;
        worksheet.Range("A1:O1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        // Sütun başlıkları
        var header = worksheet.Row(3);
        header.Cell(1).Value = "Sıra";
        header.Cell(2).Value = "Personel";
        header.Cell(3).Value = "Brüt Maaş";
        header.Cell(4).Value = "SGK Maaşı";
        header.Cell(5).Value = "SGK Kesinti";
        header.Cell(6).Value = "Gelir Vergisi";
        header.Cell(7).Value = "Damga Vergisi";
        header.Cell(8).Value = "Net Maaş";
        header.Cell(9).Value = "Yemek Yardımı";
        header.Cell(10).Value = "Yol Yardımı";
        header.Cell(11).Value = "Prim";
        header.Cell(12).Value = "Diğer Ek";
        header.Cell(13).Value = "Toplu Maaş";
        header.Cell(14).Value = "Ek Ödeme";
        header.Cell(15).Value = "Toplam Ödenecek";
        header.Cell(16).Value = "IBAN";
        header.Style.Font.Bold = true;
        header.Style.Fill.BackgroundColor = XLColor.LightGray;

        // Veriler
        int row = 4;
        foreach (var detay in bordro.BordroDetaylar.OrderBy(d => d.Personel.SiralamaNo == 0 ? int.MaxValue : d.Personel.SiralamaNo).ThenBy(d => d.Personel.Ad))
        {
            worksheet.Cell(row, 1).Value = detay.Personel.SiralamaNo > 0 ? detay.Personel.SiralamaNo : row - 3;
            worksheet.Cell(row, 2).Value = detay.Personel.TamAd;
            worksheet.Cell(row, 3).Value = detay.BrutMaas;
            worksheet.Cell(row, 4).Value = detay.SgkMaasi;
            worksheet.Cell(row, 5).Value = detay.SgkIssizlikKesinti;
            worksheet.Cell(row, 6).Value = detay.GelirVergisi;
            worksheet.Cell(row, 7).Value = detay.DamgaVergisi;
            worksheet.Cell(row, 8).Value = detay.NetMaas;
            worksheet.Cell(row, 9).Value = detay.YemekYardimi;
            worksheet.Cell(row, 10).Value = detay.YolYardimi;
            worksheet.Cell(row, 11).Value = detay.PrimTutar;
            worksheet.Cell(row, 12).Value = detay.DigerEkOdeme;
            worksheet.Cell(row, 13).Value = detay.TopluMaas;
            worksheet.Cell(row, 14).Value = detay.EkOdeme;
            worksheet.Cell(row, 15).Value = detay.ToplamOdenecek;
            worksheet.Cell(row, 16).Value = detay.Personel.IBAN ?? "";
            row++;
        }

        // Para formatı
        worksheet.Range(4, 3, row - 1, 15).Style.NumberFormat.Format = "#,##0.00 ₺";

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<List<BordroKalanOdemeSatir>> GetKalanOdemeRaporuAsync(int bordroId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        var bordro = await context.Bordrolar
            .Include(b => b.BordroDetaylar)
                .ThenInclude(d => d.Personel)
            .FirstOrDefaultAsync(b => b.Id == bordroId);

        if (bordro == null)
            throw new InvalidOperationException("Bordro bulunamadı!");

        var personelIds = bordro.BordroDetaylar.Select(d => d.PersonelId).Distinct().ToList();

        var acikBorclar = await context.PersonelBorclar
            .Where(b => personelIds.Contains(b.PersonelId)
                && b.OdemeDurum != BorcOdemeDurum.IptalEdildi
                && (b.Tutar - b.OdenenTutar) > 0
                && b.BorcTipi != BorcTipi.MaasAlacagi)
            .GroupBy(b => b.PersonelId)
            .Select(g => new { PersonelId = g.Key, Tutar = g.Sum(x => x.Tutar - x.OdenenTutar) })
            .ToDictionaryAsync(x => x.PersonelId, x => x.Tutar);

        var acikAvanslar = await context.PersonelAvanslar
            .Where(a => personelIds.Contains(a.PersonelId)
                && a.Durum != AvansDurum.IptalEdildi
                && (a.Tutar - a.MahsupEdilen) > 0)
            .GroupBy(a => a.PersonelId)
            .Select(g => new { PersonelId = g.Key, Tutar = g.Sum(x => x.Tutar - x.MahsupEdilen) })
            .ToDictionaryAsync(x => x.PersonelId, x => x.Tutar);

        var bordroOdemeleri = await context.BordroOdemeler
            .Where(o => personelIds.Contains(o.BordroDetay.PersonelId) && o.BordroDetay.BordroId == bordroId)
            .GroupBy(o => o.BordroDetayId)
            .Select(g => new { BordroDetayId = g.Key, Tutar = g.Sum(x => x.OdemeTutari) })
            .ToDictionaryAsync(x => x.BordroDetayId, x => x.Tutar);

        return bordro.BordroDetaylar
            .OrderBy(d => d.Personel.Ad)
            .ThenBy(d => d.Personel.Soyad)
            .Select(detay =>
            {
                var toplamNetMaas = detay.NetMaas + detay.EkOdeme;
                var bordrodaEleGecen = Math.Min(toplamNetMaas, bordroOdemeleri.GetValueOrDefault(detay.Id));
                var personelHarcamalari = acikBorclar.GetValueOrDefault(detay.PersonelId);
                var personelAvansAlacaklari = acikAvanslar.GetValueOrDefault(detay.PersonelId);
                var avansVeOdemeler = personelHarcamalari - personelAvansAlacaklari;
                var kalanMaas = Math.Max(0, toplamNetMaas - bordrodaEleGecen);

                return new BordroKalanOdemeSatir
                {
                    BordroId = bordroId,
                    BordroDetayId = detay.Id,
                    PersonelId = detay.PersonelId,
                    PersonelKodu = detay.Personel.SoforKodu,
                    PersonelAdSoyad = detay.Personel.TamAd,
                    Iban = detay.Personel.IBAN,
                    NetMaas = toplamNetMaas,
                    BordrodaEleGecen = bordrodaEleGecen,
                    KalanMaas = kalanMaas,
                    PersonelHarcamalari = personelHarcamalari,
                    PersonelAvansAlacaklari = personelAvansAlacaklari,
                    AvansVeOdemeler = avansVeOdemeler,
                    OdenecekMiktar = kalanMaas + avansVeOdemeler,
                    BankaOdemesiYapildi = detay.BankaOdemesiYapildi,
                    EkOdemeYapildi = detay.EkOdemeYapildi
                };
            })
            .ToList();
    }

    public async Task<byte[]> ExportKalanOdemeRaporuAsync(int bordroId)
    {
        var bordro = await GetBordroByIdAsync(bordroId);
        if (bordro == null)
            throw new InvalidOperationException("Bordro bulunamadı!");

        var satirlar = await GetKalanOdemeRaporuAsync(bordroId);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Kalan Ödeme Raporu");

        worksheet.Cell("A1").Value = $"{bordro.DonemeAdi} {bordro.BordroTipi} Bordro - Kalan Ödeme Raporu";
        worksheet.Range("A1:G1").Merge().Style.Font.Bold = true;
        worksheet.Range("A1:G1").Style.Font.FontSize = 14;
        worksheet.Range("A1:G1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        var header = worksheet.Row(3);
        header.Cell(1).Value = "Personel Kodu";
        header.Cell(2).Value = "Ad Soyad";
        header.Cell(3).Value = "IBAN";
        header.Cell(4).Value = "Net Maaş";
        header.Cell(5).Value = "Bordroda Ele Geçen";
        header.Cell(6).Value = "Kalan Maaş";
        header.Cell(7).Value = "Avans + Ödemeler";
        header.Cell(8).Value = "Ödenecek Miktar";
        header.Style.Font.Bold = true;
        header.Style.Fill.BackgroundColor = XLColor.LightGray;

        var row = 4;
        foreach (var satir in satirlar)
        {
            worksheet.Cell(row, 1).Value = satir.PersonelKodu;
            worksheet.Cell(row, 2).Value = satir.PersonelAdSoyad;
            worksheet.Cell(row, 3).Value = satir.Iban ?? string.Empty;
            worksheet.Cell(row, 4).Value = satir.NetMaas;
            worksheet.Cell(row, 5).Value = satir.BordrodaEleGecen;
            worksheet.Cell(row, 6).Value = satir.KalanMaas;
            worksheet.Cell(row, 7).Value = satir.AvansVeOdemeler;
            worksheet.Cell(row, 8).Value = satir.OdenecekMiktar;
            row++;
        }

        worksheet.Cell(row, 3).Value = "TOPLAM:";
        worksheet.Cell(row, 4).Value = satirlar.Sum(x => x.NetMaas);
        worksheet.Cell(row, 5).Value = satirlar.Sum(x => x.BordrodaEleGecen);
        worksheet.Cell(row, 6).Value = satirlar.Sum(x => x.KalanMaas);
        worksheet.Cell(row, 7).Value = satirlar.Sum(x => x.AvansVeOdemeler);
        worksheet.Cell(row, 8).Value = satirlar.Sum(x => x.OdenecekMiktar);
        worksheet.Range(row, 3, row, 8).Style.Font.Bold = true;
        worksheet.Range(row, 3, row, 8).Style.Fill.BackgroundColor = XLColor.LightYellow;

        worksheet.Range(4, 4, row, 8).Style.NumberFormat.Format = "#,##0.00 ₺";
        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<byte[]> ExportBordroOzetAsync(int? firmaId, int? yil)
    {
        var bordrolar = await GetBordrolarAsync(firmaId, yil);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Bordro Özet");

        // Başlık
        worksheet.Cell("A1").Value = $"{yil ?? DateTime.Now.Year} Yılı Bordro Özet Raporu";
        worksheet.Range("A1:I1").Merge().Style.Font.Bold = true;
        worksheet.Range("A1:I1").Style.Font.FontSize = 14;
        worksheet.Range("A1:I1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        // Sütun başlıkları
        var header = worksheet.Row(3);
        header.Cell(1).Value = "Dönem";
        header.Cell(2).Value = "Tip";
        header.Cell(3).Value = "Personel Sayısı";
        header.Cell(4).Value = "Brüt Maaş";
        header.Cell(5).Value = "Net Maaş";
        header.Cell(6).Value = "SGK Matrahı";
        header.Cell(7).Value = "Ek Ödeme";
        header.Cell(8).Value = "Genel Toplam";
        header.Cell(9).Value = "Durum";
        header.Style.Font.Bold = true;
        header.Style.Fill.BackgroundColor = XLColor.LightGray;

        // Veriler
        int row = 4;
        foreach (var bordro in bordrolar)
        {
            worksheet.Cell(row, 1).Value = bordro.DonemeAdi;
            worksheet.Cell(row, 2).Value = bordro.BordroTipi.ToString();
            worksheet.Cell(row, 3).Value = bordro.ToplamPersonelSayisi;
            worksheet.Cell(row, 4).Value = bordro.ToplamBrutMaas;
            worksheet.Cell(row, 5).Value = bordro.ToplamNetMaas;
            worksheet.Cell(row, 6).Value = bordro.ToplamSgkMatrahi;
            worksheet.Cell(row, 7).Value = bordro.ToplamEkOdeme;
            worksheet.Cell(row, 8).Value = bordro.GenelToplam;
            worksheet.Cell(row, 9).Value = bordro.Onaylandi ? "Onaylı" : "Bekliyor";
            row++;
        }

        // Para formatı
        worksheet.Range(4, 4, row - 1, 8).Style.NumberFormat.Format = "#,##0.00 ₺";

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    #endregion

    #region Ayarlar

    public async Task<BordroAyar> GetBordroAyarAsync(int? firmaId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        
        var ayar = await context.BordroAyarlar
            .OrderBy(a => a.Id)
            .FirstOrDefaultAsync(a => a.FirmaId == firmaId);

        if (ayar == null)
        {
            // Varsayılan ayar oluştur
            ayar = new BordroAyar
            {
                FirmaId = firmaId,
                PersonelMaasHesapKodu = "335",
                SgkPrimHesapKodu = "361",
                GelirVergisiHesapKodu = "360",
                KasaHesapKodu = "100",
                BankaHesapKodu = "102",
                PersonelAvansHesapKodu = "195",
                SgkIsciPayiOrani = 14,
                IssizlikIsciPayiOrani = 1,
                DamgaVergisiOrani = 0.759M,
                ArgeSgkIsverenDestekVarMi = true,
                ArgeSgkIsverenDestekOrani = 100
            };

            context.BordroAyarlar.Add(ayar);
            await context.SaveChangesAsync();
        }

        return ayar;
    }

    public async Task SaveBordroAyarAsync(BordroAyar ayar)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        
        var mevcutAyar = await context.BordroAyarlar
            .FirstOrDefaultAsync(a => a.FirmaId == ayar.FirmaId);

        if (mevcutAyar != null)
        {
            mevcutAyar.PersonelMaasHesapKodu = ayar.PersonelMaasHesapKodu;
            mevcutAyar.SgkPrimHesapKodu = ayar.SgkPrimHesapKodu;
            mevcutAyar.GelirVergisiHesapKodu = ayar.GelirVergisiHesapKodu;
            mevcutAyar.KasaHesapKodu = ayar.KasaHesapKodu;
            mevcutAyar.BankaHesapKodu = ayar.BankaHesapKodu;
            mevcutAyar.PersonelAvansHesapKodu = ayar.PersonelAvansHesapKodu;
            mevcutAyar.SgkIsciPayiOrani = ayar.SgkIsciPayiOrani;
            mevcutAyar.IssizlikIsciPayiOrani = ayar.IssizlikIsciPayiOrani;
            mevcutAyar.DamgaVergisiOrani = ayar.DamgaVergisiOrani;
            mevcutAyar.ArgeSgkIsverenDestekVarMi = ayar.ArgeSgkIsverenDestekVarMi;
            mevcutAyar.ArgeSgkIsverenDestekOrani = ayar.ArgeSgkIsverenDestekOrani;
        }
        else
        {
            context.BordroAyarlar.Add(ayar);
        }

        await context.SaveChangesAsync();
    }

    #endregion

    #region Özet Bilgiler

    public async Task<BordroOzet> GetBordroOzetAsync(int? firmaId, int? yil, int? ay)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        
        var query = context.BordroDetaylar
            .Include(d => d.Bordro)
            .Include(d => d.Personel)
            .AsQueryable();

        if (firmaId.HasValue)
            query = query.Where(d => d.FirmaId == firmaId);

        if (yil.HasValue)
            query = query.Where(d => d.Bordro.Yil == yil);

        if (ay.HasValue)
            query = query.Where(d => d.Bordro.Ay == ay);

        var detaylar = await query.ToListAsync();
        var bordrolar = await GetBordrolarAsync(firmaId, yil);

        return new BordroOzet
        {
            ToplamPersonel = detaylar.Select(d => d.PersonelId).Distinct().Count(),
            NormalPersonel = detaylar.Where(d => !d.Personel.ArgePersoneli).Select(d => d.PersonelId).Distinct().Count(),
            ArgePersonel = detaylar.Where(d => d.Personel.ArgePersoneli).Select(d => d.PersonelId).Distinct().Count(),
            ToplamBrutMaas = detaylar.Sum(d => d.BrutMaas),
            ToplamNetMaas = detaylar.Sum(d => d.NetMaas),
            ToplamSgkMaasi = detaylar.Sum(d => d.SgkMaasi),
            ToplamEkOdeme = detaylar.Sum(d => d.EkOdeme),
            OnayliDönemSayisi = bordrolar.Count(b => b.Onaylandi),
            BekleyenDönemSayisi = bordrolar.Count(b => !b.Onaylandi)
        };
    }

    #endregion

    #region Ücret Bordrosu Export

    public async Task<byte[]> ExportUcretBordrosuAsync(int bordroDetayId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        var detay = await context.BordroDetaylar
            .Include(d => d.Bordro)
                .ThenInclude(b => b.Firma)
            .Include(d => d.Personel)
            .Include(d => d.Firma)
            .FirstOrDefaultAsync(d => d.Id == bordroDetayId);

        if (detay == null)
            throw new InvalidOperationException("Bordro detayı bulunamadı!");

        using var workbook = new XLWorkbook();
        CreateUcretBordrosuWorksheet(workbook, detay);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<byte[]> ExportTopluUcretBordrosuAsync(List<int> bordroDetayIds)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        var detaylar = await context.BordroDetaylar
            .Include(d => d.Bordro)
                .ThenInclude(b => b.Firma)
            .Include(d => d.Personel)
            .Include(d => d.Firma)
            .Where(d => bordroDetayIds.Contains(d.Id))
            .OrderBy(d => d.Personel.SiralamaNo == 0 ? int.MaxValue : d.Personel.SiralamaNo)
            .ThenBy(d => d.Personel.Ad)
            .ToListAsync();

        if (!detaylar.Any())
            throw new InvalidOperationException("Bordro detayı bulunamadı!");

        using var workbook = new XLWorkbook();
        foreach (var detay in detaylar)
        {
            CreateUcretBordrosuWorksheet(workbook, detay);
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<byte[]> ExportTumUcretBordrolariAsync(int bordroId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        var detaylar = await context.BordroDetaylar
            .Include(d => d.Bordro)
                .ThenInclude(b => b.Firma)
            .Include(d => d.Personel)
            .Include(d => d.Firma)
            .Where(d => d.BordroId == bordroId)
            .OrderBy(d => d.Personel.SiralamaNo == 0 ? int.MaxValue : d.Personel.SiralamaNo)
            .ThenBy(d => d.Personel.Ad)
            .ToListAsync();

        if (!detaylar.Any())
            throw new InvalidOperationException("Bordro detayları bulunamadı!");

        using var workbook = new XLWorkbook();
        foreach (var detay in detaylar)
        {
            CreateUcretBordrosuWorksheet(workbook, detay);
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private void CreateUcretBordrosuWorksheet(XLWorkbook workbook, BordroDetay detay)
    {
        var personel = detay.Personel;
        var firma = detay.Firma ?? detay.Bordro?.Firma;
        var ayAdi = GetAyAdi(detay.Bordro?.Ay ?? 1);
        var yil = detay.Bordro?.Yil ?? DateTime.Now.Year;

        // Sayfa adı (max 31 karakter)
        var sheetName = $"{personel.Ad} {personel.Soyad}".Length > 28 
            ? $"{personel.Ad} {personel.Soyad}".Substring(0, 28) 
            : $"{personel.Ad} {personel.Soyad}";

        // Aynı isimde sayfa varsa numaralandır
        var baseName = sheetName;
        var counter = 1;
        while (workbook.Worksheets.Any(ws => ws.Name == sheetName))
        {
            sheetName = $"{baseName}_{counter++}";
            if (sheetName.Length > 31) sheetName = sheetName.Substring(0, 31);
        }

        var ws = workbook.Worksheets.Add(sheetName);

        // Sayfa ayarları
        ws.PageSetup.PageOrientation = XLPageOrientation.Landscape;
        ws.PageSetup.PaperSize = XLPaperSize.A4Paper;
        ws.PageSetup.FitToPages(1, 1);
        ws.PageSetup.Margins.Top = 0.5;
        ws.PageSetup.Margins.Bottom = 0.5;
        ws.PageSetup.Margins.Left = 0.5;
        ws.PageSetup.Margins.Right = 0.5;

        // Sütun genişlikleri
        ws.Column(1).Width = 15;
        ws.Column(2).Width = 12;
        ws.Column(3).Width = 12;
        ws.Column(4).Width = 12;
        ws.Column(5).Width = 12;
        ws.Column(6).Width = 12;
        ws.Column(7).Width = 12;
        ws.Column(8).Width = 12;
        ws.Column(9).Width = 15;
        ws.Column(10).Width = 15;

        var row = 1;

        // BAŞLIK
        ws.Cell(row, 1).Value = $"{ayAdi.ToUpper()} / {yil} ÜCRET BORDROSU";
        ws.Range(row, 1, row, 10).Merge();
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Font.FontSize = 14;
        ws.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        row += 2;

        // FİRMA BİLGİLERİ ve PERSONEL BİLGİLERİ (yan yana)
        // Sol taraf - Firma bilgileri
        ws.Cell(row, 1).Value = "Firma Ünvanı:";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 2).Value = firma?.FirmaAdi ?? "-";
        ws.Range(row, 2, row, 4).Merge();

        // Sağ taraf - Personel bilgileri
        ws.Cell(row, 6).Value = "Ad-Soyadı:";
        ws.Cell(row, 6).Style.Font.Bold = true;
        ws.Cell(row, 7).Value = personel.TamAd;
        ws.Range(row, 7, row, 10).Merge();
        row++;

        ws.Cell(row, 1).Value = "Şube Adres:";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 2).Value = firma?.Adres ?? "-";
        ws.Range(row, 2, row, 4).Merge();

        ws.Cell(row, 6).Value = "T.C. Kimlik No:";
        ws.Cell(row, 6).Style.Font.Bold = true;
        ws.Cell(row, 7).Value = personel.TcKimlikNo ?? "-";
        ws.Range(row, 7, row, 10).Merge();
        row++;

        ws.Cell(row, 1).Value = "Vergi Dairesi:";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 2).Value = firma?.VergiDairesi ?? "-";
        ws.Range(row, 2, row, 4).Merge();

        ws.Cell(row, 6).Value = "Ünvanı/Sicil No:";
        ws.Cell(row, 6).Style.Font.Bold = true;
        ws.Cell(row, 7).Value = personel.SoforKodu ?? "-";
        ws.Range(row, 7, row, 10).Merge();
        row++;

        ws.Cell(row, 1).Value = "Firma Vergi No:";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 2).Value = firma?.VergiNo ?? "-";
        ws.Range(row, 2, row, 4).Merge();

        ws.Cell(row, 6).Value = "İşe Giriş Tarihi:";
        ws.Cell(row, 6).Style.Font.Bold = true;
        ws.Cell(row, 7).Value = personel.IseBaslamaTarihi?.ToString("dd.MM.yyyy") ?? "-";
        ws.Range(row, 7, row, 10).Merge();
        row++;

        ws.Cell(row, 1).Value = "SGK İşyeri No:";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 2).Value = "-";
        ws.Range(row, 2, row, 4).Merge();

        ws.Cell(row, 6).Value = "SSK/TİC Sicil No:";
        ws.Cell(row, 6).Style.Font.Bold = true;
        ws.Cell(row, 7).Value = personel.SoforKodu ?? "-";
        ws.Range(row, 7, row, 10).Merge();
        row++;

        ws.Cell(row, 1).Value = "Firma İnternet Adresi:";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 2).Value = firma?.WebSite ?? "-";
        ws.Range(row, 2, row, 4).Merge();
        row += 2;

        // ÇALIŞMA BİLGİLERİ TABLOSU
        var calismaSatir = row;
        ws.Cell(row, 1).Value = "S.No";
        ws.Cell(row, 2).Value = "Sapmı No";
        ws.Cell(row, 3).Value = "Gün";
        ws.Cell(row, 4).Value = "Öğr.Tat.Gn.";
        ws.Cell(row, 5).Value = "Saat";
        ws.Cell(row, 6).Value = "İ.şt.Saat";
        ws.Cell(row, 7).Value = "U.İzin Gn.";
        ws.Cell(row, 8).Value = "Ü.İzin Saat";
        ws.Cell(row, 9).Value = "Hafta Tatil";
        ws.Cell(row, 10).Value = "Res.Tat.Gn";
        ws.Range(row, 1, row, 10).Style.Font.Bold = true;
        ws.Range(row, 1, row, 10).Style.Fill.BackgroundColor = XLColor.LightGray;
        ws.Range(row, 1, row, 10).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        row++;

        // Çalışma verileri (varsayılan değerler)
        var gunSayisi = DateTime.DaysInMonth(yil, detay.Bordro?.Ay ?? 1);
        var haftaTatilGunu = (int)Math.Floor(gunSayisi / 7.0);
        ws.Cell(row, 1).Value = 1;
        ws.Cell(row, 3).Value = gunSayisi - haftaTatilGunu; // İş günü
        ws.Cell(row, 5).Value = (gunSayisi - haftaTatilGunu) * 7.5m; // Çalışma saati
        ws.Cell(row, 9).Value = haftaTatilGunu;
        ws.Range(row, 1, row, 10).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        row += 2;

        // TAHAKKUK BİLGİLERİ ve SGK BİLGİLERİ
        var tahakkukBaslik = row;
        ws.Cell(row, 1).Value = "TAHAKKUK BİLGİLERİ";
        ws.Range(row, 1, row, 5).Merge();
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.LightBlue;
        ws.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        ws.Cell(row, 6).Value = "SGK BİLGİLERİ";
        ws.Range(row, 6, row, 10).Merge();
        ws.Cell(row, 6).Style.Font.Bold = true;
        ws.Cell(row, 6).Style.Fill.BackgroundColor = XLColor.LightGreen;
        ws.Cell(row, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        row++;

        // Tahakkuk detayları
        ws.Cell(row, 1).Value = "Aylık Ücret Toplamı:";
        ws.Cell(row, 2).Value = detay.BrutMaas;
        ws.Cell(row, 2).Style.NumberFormat.Format = "#,##0.00 ₺";

        ws.Cell(row, 6).Value = "Brüt Ücret:";
        ws.Cell(row, 7).Value = detay.BrutMaas;
        ws.Cell(row, 7).Style.NumberFormat.Format = "#,##0.00 ₺";

        ws.Cell(row, 9).Value = "Sigorta Matrahı:";
        ws.Cell(row, 10).Value = detay.SgkMaasi;
        ws.Cell(row, 10).Style.NumberFormat.Format = "#,##0.00 ₺";
        row++;

        ws.Cell(row, 1).Value = "Fazla Mesai Toplamı:";
        ws.Cell(row, 2).Value = 0;
        ws.Cell(row, 2).Style.NumberFormat.Format = "#,##0.00 ₺";

        ws.Cell(row, 6).Value = "Fazla Mesai:";
        ws.Cell(row, 7).Value = 0;
        ws.Cell(row, 7).Style.NumberFormat.Format = "#,##0.00 ₺";

        ws.Cell(row, 9).Value = "Sigorta Primi İşçi:";
        ws.Cell(row, 10).Value = detay.SgkIssizlikKesinti;
        ws.Cell(row, 10).Style.NumberFormat.Format = "#,##0.00 ₺";
        row++;

        ws.Cell(row, 1).Value = "Sair Ödemeler Toplamı:";
        ws.Cell(row, 2).Value = detay.ToplamEkOdeme;
        ws.Cell(row, 2).Style.NumberFormat.Format = "#,##0.00 ₺";

        ws.Cell(row, 6).Value = "Sair Ödeme:";
        ws.Cell(row, 7).Value = detay.ToplamEkOdeme;
        ws.Cell(row, 7).Style.NumberFormat.Format = "#,##0.00 ₺";

        ws.Cell(row, 9).Value = "İşsizlik Primi İşçi:";
        var issizlikPrimi = detay.SgkMaasi * 0.01m;
        ws.Cell(row, 10).Value = issizlikPrimi;
        ws.Cell(row, 10).Style.NumberFormat.Format = "#,##0.00 ₺";
        row++;

        ws.Cell(row, 1).Value = "SGK Hissesi:";
        ws.Cell(row, 2).Value = detay.SgkIssizlikKesinti;
        ws.Cell(row, 2).Style.NumberFormat.Format = "#,##0.00 ₺";

        ws.Cell(row, 6).Value = "Vergi Matrahı:";
        ws.Cell(row, 7).Value = detay.BrutMaas - detay.SgkIssizlikKesinti;
        ws.Cell(row, 7).Style.NumberFormat.Format = "#,##0.00 ₺";

        ws.Cell(row, 9).Value = "Toplam SGK Primi:";
        ws.Cell(row, 10).Value = detay.SgkIssizlikKesinti + issizlikPrimi;
        ws.Cell(row, 10).Style.NumberFormat.Format = "#,##0.00 ₺";
        row++;

        ws.Cell(row, 1).Value = "İşsizlik Sigorta Kesintisi:";
        ws.Cell(row, 2).Value = issizlikPrimi;
        ws.Cell(row, 2).Style.NumberFormat.Format = "#,##0.00 ₺";

        ws.Cell(row, 6).Value = "Gelir Vergisi:";
        ws.Cell(row, 7).Value = detay.GelirVergisi;
        ws.Cell(row, 7).Style.NumberFormat.Format = "#,##0.00 ₺";

        ws.Cell(row, 9).Value = "Sigorta İşveren:";
        var sgkIsverenPay = detay.SgkMaasi * 0.205m; // %20.5 işveren payı
        ws.Cell(row, 10).Value = sgkIsverenPay;
        ws.Cell(row, 10).Style.NumberFormat.Format = "#,##0.00 ₺";
        row++;

        ws.Cell(row, 1).Value = "Damga Vergisi:";
        ws.Cell(row, 2).Value = detay.DamgaVergisi;
        ws.Cell(row, 2).Style.NumberFormat.Format = "#,##0.00 ₺";

        ws.Cell(row, 6).Value = "Damga Vergisi:";
        ws.Cell(row, 7).Value = detay.DamgaVergisi;
        ws.Cell(row, 7).Style.NumberFormat.Format = "#,##0.00 ₺";

        ws.Cell(row, 9).Value = "İşsizlik İşveren:";
        var issizlikIsverenPay = detay.SgkMaasi * 0.02m; // %2 işveren payı
        ws.Cell(row, 10).Value = issizlikIsverenPay;
        ws.Cell(row, 10).Style.NumberFormat.Format = "#,##0.00 ₺";
        row++;

        ws.Cell(row, 1).Value = "Gelir Vergisi:";
        ws.Cell(row, 2).Value = detay.GelirVergisi;
        ws.Cell(row, 2).Style.NumberFormat.Format = "#,##0.00 ₺";

        ws.Cell(row, 6).Value = "SGK Kesintisi:";
        ws.Cell(row, 7).Value = detay.SgkIssizlikKesinti;
        ws.Cell(row, 7).Style.NumberFormat.Format = "#,##0.00 ₺";

        ws.Cell(row, 9).Value = "Toplam SGK İşveren:";
        ws.Cell(row, 10).Value = sgkIsverenPay + issizlikIsverenPay;
        ws.Cell(row, 10).Style.NumberFormat.Format = "#,##0.00 ₺";
        row++;

        ws.Cell(row, 1).Value = "Sair Kesintiler:";
        ws.Cell(row, 2).Value = 0;
        ws.Cell(row, 2).Style.NumberFormat.Format = "#,##0.00 ₺";

        ws.Cell(row, 6).Value = "Vergi İndirimi:";
        ws.Cell(row, 7).Value = 0;
        ws.Cell(row, 7).Style.NumberFormat.Format = "#,##0.00 ₺";
        row++;

        // TOPLAM satırı
        row++;
        ws.Cell(row, 1).Value = "TOPLAM:";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 2).Value = detay.BrutMaas + detay.ToplamEkOdeme;
        ws.Cell(row, 2).Style.Font.Bold = true;
        ws.Cell(row, 2).Style.NumberFormat.Format = "#,##0.00 ₺";

        ws.Cell(row, 6).Value = "Ödenecek Tutar:";
        ws.Cell(row, 6).Style.Font.Bold = true;
        ws.Cell(row, 7).Value = detay.NetMaas + detay.ToplamEkOdeme;
        ws.Cell(row, 7).Style.Font.Bold = true;
        ws.Cell(row, 7).Style.NumberFormat.Format = "#,##0.00 ₺";
        row += 2;

        // ÖDENECEK NET ÜCRETLER
        ws.Cell(row, 1).Value = "ÖDENECEK NET ÜCRETLER";
        ws.Range(row, 1, row, 5).Merge();
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.LightYellow;
        ws.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        row++;

        ws.Cell(row, 1).Value = "Ödenecek Net Ücret:";
        ws.Cell(row, 2).Value = detay.NetMaas;
        ws.Cell(row, 2).Style.NumberFormat.Format = "#,##0.00 ₺";
        ws.Cell(row, 2).Style.Font.Bold = true;

        ws.Cell(row, 4).Value = "Net Ücret (Banka):";
        ws.Cell(row, 5).Value = detay.SgkMaasi - detay.ToplamKesinti;
        ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0.00 ₺";
        row++;

        ws.Cell(row, 1).Value = "Ödenecek SGD Primi:";
        ws.Cell(row, 2).Value = 0;
        ws.Cell(row, 2).Style.NumberFormat.Format = "#,##0.00 ₺";
        row++;

        ws.Cell(row, 1).Value = "Ödenecek Gelir Vergisi:";
        ws.Cell(row, 2).Value = detay.GelirVergisi;
        ws.Cell(row, 2).Style.NumberFormat.Format = "#,##0.00 ₺";
        row++;

        ws.Cell(row, 1).Value = "Ödenecek Damga Vergisi:";
        ws.Cell(row, 2).Value = detay.DamgaVergisi;
        ws.Cell(row, 2).Style.NumberFormat.Format = "#,##0.00 ₺";
        row++;

        ws.Cell(row, 1).Value = "Ödenecek İşsizlik Sig. Kesintisi:";
        ws.Cell(row, 2).Value = issizlikPrimi;
        ws.Cell(row, 2).Style.NumberFormat.Format = "#,##0.00 ₺";
        row += 2;

        // TOPLAM KESİNTİ ve NET ÖDENECEK
        ws.Cell(row, 1).Value = "Toplam SGK İşveren Payı:";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 2).Value = sgkIsverenPay + issizlikIsverenPay;
        ws.Cell(row, 2).Style.NumberFormat.Format = "#,##0.00 ₺";
        ws.Cell(row, 2).Style.Font.Bold = true;

        ws.Cell(row, 4).Value = "İndirilecek Damga Vergisi:";
        ws.Cell(row, 5).Value = detay.DamgaVergisi;
        ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0.00 ₺";
        row++;

        ws.Cell(row, 1).Value = "İndirilecek SGK İşveren Payı:";
        ws.Cell(row, 2).Value = 0;
        ws.Cell(row, 2).Style.NumberFormat.Format = "#,##0.00 ₺";

        ws.Cell(row, 4).Value = "İndirilecek Gelir Vergisi:";
        ws.Cell(row, 5).Value = 0;
        ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0.00 ₺";
        row += 2;

        // NET ÖDENECEK SGK PRİMİ
        ws.Cell(row, 1).Value = "Net Ödenecek SGK Primi:";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.LightGreen;
        ws.Cell(row, 2).Value = detay.SgkIssizlikKesinti + issizlikPrimi + sgkIsverenPay + issizlikIsverenPay;
        ws.Cell(row, 2).Style.NumberFormat.Format = "#,##0.00 ₺";
        ws.Cell(row, 2).Style.Font.Bold = true;
        ws.Cell(row, 2).Style.Fill.BackgroundColor = XLColor.LightGreen;

        ws.Cell(row, 4).Value = "Asgari Geçim İndirimi Tutarı:";
        ws.Cell(row, 5).Value = 0;
        ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0.00 ₺";
        row += 2;

        // İMZA ALANI
        ws.Cell(row, 1).Value = "İşveren Kaşe / İmza";
        ws.Cell(row, 1).Style.Font.Bold = true;

        ws.Cell(row, 5).Value = "İşçi İmza";
        ws.Cell(row, 5).Style.Font.Bold = true;
        row++;

        // Çerçeve ekle
        ws.Range(1, 1, row, 10).Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
    }

    private static string GetAyAdi(int ay)
    {
        return ay switch
        {
            1 => "OCAK",
            2 => "ŞUBAT",
            3 => "MART",
            4 => "NİSAN",
            5 => "MAYIS",
            6 => "HAZİRAN",
            7 => "TEMMUZ",
            8 => "AĞUSTOS",
            9 => "EYLÜL",
            10 => "EKİM",
            11 => "KASIM",
            12 => "ARALIK",
            _ => "BİLİNMİYOR"
        };
    }

    #endregion
}


