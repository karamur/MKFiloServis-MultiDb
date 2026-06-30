using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using MKFiloServis.Web.Models;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Web.Services;

/// <summary>
/// Cari borç/alacak detaylı takip servisi implementasyonu
/// </summary>
public class CariHareketTakipService : ICariHareketTakipService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public CariHareketTakipService(IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    #region Özet Raporlar

    public async Task<CariBorcAlacakOzet> GetBorcAlacakOzetAsync(
        DateTime? baslangicTarihi = null,
        DateTime? bitisTarihi = null,
        CariTipi? cariTipi = null,
        bool sadeceBorclu = false,
        bool sadeceRiskli = false)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var bugun = DateTime.Today;
        var baslangic = baslangicTarihi ?? DateTime.MinValue;
        var bitis = bitisTarihi ?? DateTime.MaxValue;

        // Carileri getir
        var cariler = await context.Cariler
            .AsNoTracking()
            .Where(c => !c.IsDeleted && c.Aktif)
            .Where(c => cariTipi == null || c.CariTipi == cariTipi)
            .ToListAsync();

        var result = new CariBorcAlacakOzet();
        var ozecListesi = new List<CariHareketTakipOzet>();

        foreach (var cari in cariler)
        {
            // Faturaları getir
            var faturalar = await context.Faturalar
                .AsNoTracking()
                .Where(f => !f.IsDeleted && f.CariId == cari.Id)
                .Where(f => f.FaturaTarihi >= baslangic && f.FaturaTarihi <= bitis)
                .ToListAsync();

            // Ödemeleri getir
            var odemeler = await context.BankaKasaHareketleri
                .AsNoTracking()
                .Where(h => !h.IsDeleted && h.CariId == cari.Id)
                .Where(h => h.IslemTarihi >= baslangic && h.IslemTarihi <= bitis)
                .ToListAsync();

            var aracMasraflari = await context.AracMasraflari
                .AsNoTracking()
                .Where(m => !m.IsDeleted && m.CariId == cari.Id)
                .Where(m => m.MasrafTarihi >= baslangic && m.MasrafTarihi <= bitis)
                .ToListAsync();

            // Borç hesapla (Giden faturalar = Müşteriden alacak = Borçlu)
            var borcFaturalar = faturalar.Where(f => f.FaturaYonu == FaturaYonu.Giden).Sum(f => f.GenelToplam);
            // Alacak hesapla (Gelen faturalar = Tedarikçiye borç veya tahsilatlar)
            var alacakFaturalar = faturalar.Where(f => f.FaturaYonu == FaturaYonu.Gelen).Sum(f => f.GenelToplam);

            // Tahsilatlar (Giriş = alacak azalır)
            var tahsilatlar = odemeler.Where(h => h.HareketTipi == HareketTipi.Giris).Sum(h => h.Tutar);
            // Ödemeler (Çıkış = borç azalır)
            var odemelerToplam = odemeler.Where(h => h.HareketTipi == HareketTipi.Cikis).Sum(h => h.Tutar);

            var toplamBorc = borcFaturalar;
            var toplamAlacak = alacakFaturalar + tahsilatlar - odemelerToplam + aracMasraflari.Sum(m => m.Tutar);

            // Vadesi geçmiş hesapla
            var vadesiGecmisFaturalar = faturalar
                .Where(f => f.FaturaYonu == FaturaYonu.Giden && 
                           f.KalanTutar > 0 && 
                           f.VadeTarihi.HasValue && 
                           f.VadeTarihi.Value < bugun)
                .ToList();

            var vadesiGecmisBakiye = vadesiGecmisFaturalar.Sum(f => f.KalanTutar);
            var vadesiGecmisFaturaSayisi = vadesiGecmisFaturalar.Count;

            // Risk skoru hesapla
            var riskSkoru = HesaplaRiskSkoru(toplamBorc - toplamAlacak, vadesiGecmisBakiye, vadesiGecmisFaturaSayisi);

            // Son işlem tarihi
            var sonFaturaTarihi = faturalar.Any() ? faturalar.Max(f => f.FaturaTarihi) : (DateTime?)null;
            var sonOdemeTarihi = odemeler.Any() ? odemeler.Max(h => h.IslemTarihi) : (DateTime?)null;
            var sonMasrafTarihi = aracMasraflari.Any() ? aracMasraflari.Max(m => m.MasrafTarihi) : (DateTime?)null;
            var sonIslemTarihi = new[] { sonFaturaTarihi, sonOdemeTarihi, sonMasrafTarihi }.Where(d => d.HasValue).DefaultIfEmpty().Max();

            // Ortalama ödeme süresi
            var ortalamaOdemeSuresi = HesaplaOrtalamaOdemeSuresi(faturalar, odemeler);

            var ozet = new CariHareketTakipOzet
            {
                CariId = cari.Id,
                CariKodu = cari.CariKodu,
                Unvan = cari.Unvan,
                CariTipi = cari.CariTipi.ToString(),
                Telefon = cari.Telefon,
                ToplamBorc = toplamBorc,
                ToplamAlacak = toplamAlacak,
                VadesiGecmisBakiye = vadesiGecmisBakiye,
                VadesiGecmisFaturaSayisi = vadesiGecmisFaturaSayisi,
                RiskSkoru = riskSkoru,
                SonIslemTarihi = sonIslemTarihi,
                OrtalamaOdemeSuresi = ortalamaOdemeSuresi
            };

            // Filtrele
            if (sadeceBorclu && ozet.NetBakiye <= 0) continue;
            if (sadeceRiskli && riskSkoru < 60) continue;

            ozecListesi.Add(ozet);
        }

        result.Cariler = ozecListesi.OrderByDescending(c => c.RiskSkoru).ThenByDescending(c => Math.Abs(c.NetBakiye)).ToList();

        // Toplamları hesapla
        result.ToplamBorc = ozecListesi.Sum(c => c.ToplamBorc);
        result.ToplamAlacak = ozecListesi.Sum(c => c.ToplamAlacak);
        result.ToplamCariSayisi = ozecListesi.Count;
        result.BorcluCariSayisi = ozecListesi.Count(c => c.NetBakiye > 0);
        result.AlacakliCariSayisi = ozecListesi.Count(c => c.NetBakiye < 0);
        result.RiskliCariSayisi = ozecListesi.Count(c => c.RiskSkoru >= 60);

        // Vade analizi
        await HesaplaVadeAnaliziAsync(context, result, bugun);

        // Cari tipi dağılımı
        result.TipiDagilimi = ozecListesi
            .GroupBy(c => c.CariTipi)
            .Select(g => new CariTipiBakiyeDagilimi
            {
                CariTipi = g.Key,
                CariSayisi = g.Count(),
                ToplamBorc = g.Sum(c => c.ToplamBorc),
                ToplamAlacak = g.Sum(c => c.ToplamAlacak),
                VadesiGecmis = g.Sum(c => c.VadesiGecmisBakiye),
                Oran = result.ToplamBorc != 0 ? (g.Sum(c => c.ToplamBorc) / result.ToplamBorc) * 100 : 0
            })
            .ToList();

        // Aylık trend
        result.AylikTrendler = await GetGenelAylikTrendAsync(context, baslangic, bitis);

        return result;
    }

    public async Task<CariHareketTakipRapor> GetCariDetayAsync(
        int cariId,
        DateTime? baslangicTarihi = null,
        DateTime? bitisTarihi = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var bugun = DateTime.Today;
        var baslangic = baslangicTarihi ?? DateTime.MinValue;
        var bitis = bitisTarihi ?? DateTime.MaxValue;

        var cari = await context.Cariler
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == cariId && !c.IsDeleted);

        if (cari == null)
            throw new InvalidOperationException("Cari bulunamadı");

        var rapor = new CariHareketTakipRapor
        {
            CariId = cari.Id,
            CariKodu = cari.CariKodu,
            Unvan = cari.Unvan,
            CariTipi = cari.CariTipi.ToString(),
            Telefon = cari.Telefon,
            Email = cari.Email,
            YetkiliKisi = cari.YetkiliKisi
        };

        // Faturaları getir
        var faturalar = await context.Faturalar
            .AsNoTracking()
            .Where(f => !f.IsDeleted && f.CariId == cariId)
            .Where(f => f.FaturaTarihi >= baslangic && f.FaturaTarihi <= bitis)
            .OrderBy(f => f.FaturaTarihi)
            .ToListAsync();

        // Ödemeleri getir
        var odemeler = await context.BankaKasaHareketleri
            .AsNoTracking()
            .Include(h => h.BankaHesap)
            .Where(h => !h.IsDeleted && h.CariId == cariId)
            .Where(h => h.IslemTarihi >= baslangic && h.IslemTarihi <= bitis)
            .OrderBy(h => h.IslemTarihi)
            .ToListAsync();

        var aracMasraflari = await context.AracMasraflari
            .AsNoTracking()
            .Include(m => m.Arac)
            .Include(m => m.MasrafKalemi)
            .Where(m => !m.IsDeleted && m.CariId == cariId)
            .Where(m => m.MasrafTarihi >= baslangic && m.MasrafTarihi <= bitis)
            .OrderBy(m => m.MasrafTarihi)
            .ToListAsync();

        // Borç/Alacak hesapla
        rapor.ToplamBorc = faturalar.Where(f => f.FaturaYonu == FaturaYonu.Giden).Sum(f => f.GenelToplam);
        var alacakFaturalar = faturalar.Where(f => f.FaturaYonu == FaturaYonu.Gelen).Sum(f => f.GenelToplam);
        var tahsilatlar = odemeler.Where(h => h.HareketTipi == HareketTipi.Giris).Sum(h => h.Tutar);
        var odemelerToplam = odemeler.Where(h => h.HareketTipi == HareketTipi.Cikis).Sum(h => h.Tutar);
        rapor.ToplamAlacak = alacakFaturalar + tahsilatlar - odemelerToplam + aracMasraflari.Sum(m => m.Tutar);

        // Vade analizi
        var vadesiGelmemisFaturalar = faturalar
            .Where(f => f.FaturaYonu == FaturaYonu.Giden && f.KalanTutar > 0 && (!f.VadeTarihi.HasValue || f.VadeTarihi.Value >= bugun))
            .Sum(f => f.KalanTutar);

        var vadesiGecmisFaturalar = faturalar
            .Where(f => f.FaturaYonu == FaturaYonu.Giden && f.KalanTutar > 0 && f.VadeTarihi.HasValue && f.VadeTarihi.Value < bugun)
            .ToList();

        rapor.VadesiGelmemisBakiye = vadesiGelmemisFaturalar;
        rapor.VadesiGecmisBakiye = vadesiGecmisFaturalar.Sum(f => f.KalanTutar);

        if (vadesiGecmisFaturalar.Any())
        {
            rapor.VadesiGecmisGunOrtalama = (int)vadesiGecmisFaturalar
                .Average(f => (bugun - f.VadeTarihi!.Value).TotalDays);
        }

        // İstatistikler
        rapor.ToplamFaturaSayisi = faturalar.Count;
        rapor.AcikFaturaSayisi = faturalar.Count(f => f.KalanTutar > 0);
        rapor.VadesiGecmisFaturaSayisi = vadesiGecmisFaturalar.Count;
        rapor.ToplamOdemeSayisi = odemeler.Count + aracMasraflari.Count;

        // Ortalama ödeme süresi
        rapor.OrtalamaOdemeSuresi = HesaplaOrtalamaOdemeSuresi(faturalar, odemeler);

        // Son işlemler
        rapor.SonFaturaTarihi = faturalar.Any() ? faturalar.Max(f => f.FaturaTarihi) : null;
        rapor.SonOdemeTarihi = odemeler.Any() ? odemeler.Max(h => h.IslemTarihi) : aracMasraflari.Any() ? aracMasraflari.Max(m => m.MasrafTarihi) : null;
        rapor.SonOdemeTutari = odemeler.Any() ? odemeler.OrderByDescending(h => h.IslemTarihi).First().Tutar : aracMasraflari.Any() ? aracMasraflari.OrderByDescending(m => m.MasrafTarihi).First().Tutar : 0;

        // Risk skoru
        rapor.RiskSkoru = HesaplaRiskSkoru(rapor.NetBakiye, rapor.VadesiGecmisBakiye, rapor.VadesiGecmisFaturaSayisi);

        // Hareketler
        rapor.Hareketler = await GetCariHareketlerAsync(cariId, baslangicTarihi, bitisTarihi);

        // Açık faturalar
        rapor.AcikFaturalar = await GetAcikFaturalarAsync(cariId);

        // Aylık trend
        rapor.AylikTrendler = await GetAylikTrendAsync(cariId);

        // Tahsilat planı
        rapor.TahsilatPlani = await OlusturTahsilatPlaniAsync(cariId);

        return rapor;
    }

    #endregion

    #region Hareket ve Fatura İşlemleri

    public async Task<List<CariHareketDetay>> GetCariHareketlerAsync(
        int cariId,
        DateTime? baslangicTarihi = null,
        DateTime? bitisTarihi = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var baslangic = baslangicTarihi ?? DateTime.MinValue;
        var bitis = bitisTarihi ?? DateTime.MaxValue;

        var hareketler = new List<CariHareketDetay>();

        // Faturaları ekle
        var faturalar = await context.Faturalar
            .AsNoTracking()
            .Where(f => !f.IsDeleted && f.CariId == cariId)
            .Where(f => f.FaturaTarihi >= baslangic && f.FaturaTarihi <= bitis)
            .ToListAsync();

        foreach (var fatura in faturalar)
        {
            var isGiden = fatura.FaturaYonu == FaturaYonu.Giden;
            hareketler.Add(new CariHareketDetay
            {
                Id = fatura.Id,
                Tarih = fatura.FaturaTarihi,
                HareketTipi = "Fatura",
                Aciklama = $"{(isGiden ? "Satış" : "Alış")} Faturası - {fatura.FaturaNo}",
                BelgeNo = fatura.FaturaNo,
                VadeTarihi = fatura.VadeTarihi,
                Borc = isGiden ? fatura.GenelToplam : 0,
                Alacak = isGiden ? 0 : fatura.GenelToplam
            });
        }

        // Ödemeleri ekle
        var odemeler = await context.BankaKasaHareketleri
            .AsNoTracking()
            .Include(h => h.BankaHesap)
            .Where(h => !h.IsDeleted && h.CariId == cariId)
            .Where(h => h.IslemTarihi >= baslangic && h.IslemTarihi <= bitis)
            .ToListAsync();

        foreach (var odeme in odemeler)
        {
            var isTahsilat = odeme.HareketTipi == HareketTipi.Giris;
            hareketler.Add(new CariHareketDetay
            {
                Id = odeme.Id,
                Tarih = odeme.IslemTarihi,
                HareketTipi = isTahsilat ? "Tahsilat" : "Ödeme",
                Aciklama = $"{odeme.BankaHesap?.HesapAdi ?? "Kasa"} - {odeme.Aciklama}",
                BelgeNo = odeme.BelgeNo,
                Borc = isTahsilat ? 0 : odeme.Tutar,
                Alacak = isTahsilat ? odeme.Tutar : 0
            });
        }

        var aracMasraflari = await context.AracMasraflari
            .AsNoTracking()
            .Include(m => m.Arac)
            .Include(m => m.MasrafKalemi)
            .Where(m => !m.IsDeleted && m.CariId == cariId)
            .Where(m => m.MasrafTarihi >= baslangic && m.MasrafTarihi <= bitis)
            .ToListAsync();

        foreach (var masraf in aracMasraflari)
        {
            hareketler.Add(new CariHareketDetay
            {
                Id = masraf.Id,
                DetayUrl = $"/arac-masraflari/{masraf.Id}",
                Tarih = masraf.MasrafTarihi,
                HareketTipi = "Araç Masrafı",
                Aciklama = $"{masraf.MasrafKalemi?.MasrafAdi ?? "Masraf"} - {masraf.Arac?.AktifPlaka ?? "Araç yok"}",
                BelgeNo = masraf.BelgeNo,
                Borc = 0,
                Alacak = masraf.Tutar
            });
        }

        // Tarihe göre sırala ve bakiye hesapla
        hareketler = hareketler.OrderBy(h => h.Tarih).ThenBy(h => h.Id).ToList();

        decimal bakiye = 0;
        foreach (var hareket in hareketler)
        {
            bakiye += hareket.Borc - hareket.Alacak;
            hareket.Bakiye = bakiye;
        }

        return hareketler;
    }

    public async Task<List<CariAcikFatura>> GetAcikFaturalarAsync(int cariId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var bugun = DateTime.Today;

        // NOT: KalanTutar hesaplanmış property, EF Core'da (GenelToplam - OdenenTutar) kullanılmalı
        var faturalar = await context.Faturalar
            .AsNoTracking()
            .Where(f => !f.IsDeleted && f.CariId == cariId && (f.GenelToplam - f.OdenenTutar) > 0)
            .OrderBy(f => f.VadeTarihi)
            .ToListAsync();

        return faturalar.Select(f => new CariAcikFatura
        {
            FaturaId = f.Id,
            FaturaNo = f.FaturaNo,
            FaturaTarihi = f.FaturaTarihi,
            VadeTarihi = f.VadeTarihi,
            FaturaTutari = f.GenelToplam,
            OdenenTutar = f.OdenenTutar,
            KalanGun = f.VadeTarihi.HasValue ? (int)(f.VadeTarihi.Value - bugun).TotalDays : int.MaxValue
        }).ToList();
    }

    public async Task<List<CariAcikFatura>> GetTumAcikFaturalarAsync(
        CariTipi? cariTipi = null,
        bool sadeceVadesiGecmis = false)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var bugun = DateTime.Today;

        var sorgu = context.Faturalar
            .AsNoTracking()
            .Include(f => f.Cari)
            .Where(f => !f.IsDeleted && (f.GenelToplam - f.OdenenTutar) > 0);

        if (cariTipi.HasValue)
            sorgu = sorgu.Where(f => f.Cari.CariTipi == cariTipi.Value);

        if (sadeceVadesiGecmis)
            sorgu = sorgu.Where(f => f.VadeTarihi.HasValue && f.VadeTarihi.Value < bugun);

        var faturalar = await sorgu.OrderBy(f => f.VadeTarihi).ToListAsync();

        return faturalar.Select(f => new CariAcikFatura
        {
            FaturaId = f.Id,
            FaturaNo = f.FaturaNo,
            FaturaTarihi = f.FaturaTarihi,
            VadeTarihi = f.VadeTarihi,
            FaturaTutari = f.GenelToplam,
            OdenenTutar = f.OdenenTutar,
            KalanGun = f.VadeTarihi.HasValue ? (int)(f.VadeTarihi.Value - bugun).TotalDays : int.MaxValue
        }).ToList();
    }

    #endregion

    #region Trend ve Analiz

    public async Task<List<CariAylikTrend>> GetAylikTrendAsync(int cariId, int aySayisi = 12)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var bugun = DateTime.Today;
        var baslangic = new DateTime(bugun.Year, bugun.Month, 1).AddMonths(-aySayisi + 1);

        var faturalar = await context.Faturalar
            .AsNoTracking()
            .Where(f => !f.IsDeleted && f.CariId == cariId && f.FaturaTarihi >= baslangic)
            .ToListAsync();

        var odemeler = await context.BankaKasaHareketleri
            .AsNoTracking()
            .Where(h => !h.IsDeleted && h.CariId == cariId && h.IslemTarihi >= baslangic)
            .ToListAsync();

        var trendler = new List<CariAylikTrend>();
        decimal kumulatifBakiye = 0;

        for (int i = 0; i < aySayisi; i++)
        {
            var ay = baslangic.AddMonths(i);
            var ayBasi = new DateTime(ay.Year, ay.Month, 1);
            var aySonu = ayBasi.AddMonths(1).AddDays(-1);

            var ayFaturalar = faturalar.Where(f => f.FaturaTarihi >= ayBasi && f.FaturaTarihi <= aySonu).ToList();
            var ayOdemeler = odemeler.Where(h => h.IslemTarihi >= ayBasi && h.IslemTarihi <= aySonu).ToList();

            var borc = ayFaturalar.Where(f => f.FaturaYonu == FaturaYonu.Giden).Sum(f => f.GenelToplam);
            var alacakFatura = ayFaturalar.Where(f => f.FaturaYonu == FaturaYonu.Gelen).Sum(f => f.GenelToplam);
            var tahsilat = ayOdemeler.Where(h => h.HareketTipi == HareketTipi.Giris).Sum(h => h.Tutar);
            var odeme = ayOdemeler.Where(h => h.HareketTipi == HareketTipi.Cikis).Sum(h => h.Tutar);
            var alacak = alacakFatura + tahsilat - odeme;

            kumulatifBakiye += borc - alacak;

            trendler.Add(new CariAylikTrend
            {
                Yil = ay.Year,
                Ay = ay.Month,
                ToplamBorc = borc,
                ToplamAlacak = alacak,
                AySonuBakiye = kumulatifBakiye,
                FaturaSayisi = ayFaturalar.Count,
                OdemeSayisi = ayOdemeler.Count
            });
        }

        return trendler;
    }

    private async Task<List<GenelAylikTrend>> GetGenelAylikTrendAsync(ApplicationDbContext context, DateTime baslangic, DateTime bitis)
    {
        var bugun = DateTime.Today;
        var trendBaslangic = baslangic == DateTime.MinValue 
            ? new DateTime(bugun.Year, bugun.Month, 1).AddMonths(-11)
            : new DateTime(baslangic.Year, baslangic.Month, 1);

        var faturalar = await context.Faturalar
            .AsNoTracking()
            .Where(f => !f.IsDeleted && f.FaturaTarihi >= trendBaslangic)
            .ToListAsync();

        var odemeler = await context.BankaKasaHareketleri
            .AsNoTracking()
            .Where(h => !h.IsDeleted && h.CariId != null && h.IslemTarihi >= trendBaslangic)
            .ToListAsync();

        var trendler = new List<GenelAylikTrend>();
        decimal kumulatifBakiye = 0;

        var aylar = Enumerable.Range(0, 12)
            .Select(i => trendBaslangic.AddMonths(i))
            .Where(d => d <= bugun)
            .ToList();

        foreach (var ay in aylar)
        {
            var ayBasi = new DateTime(ay.Year, ay.Month, 1);
            var aySonu = ayBasi.AddMonths(1).AddDays(-1);

            var ayFaturalar = faturalar.Where(f => f.FaturaTarihi >= ayBasi && f.FaturaTarihi <= aySonu).ToList();
            var ayOdemeler = odemeler.Where(h => h.IslemTarihi >= ayBasi && h.IslemTarihi <= aySonu).ToList();

            var borc = ayFaturalar.Where(f => f.FaturaYonu == FaturaYonu.Giden).Sum(f => f.GenelToplam);
            var alacak = ayOdemeler.Where(h => h.HareketTipi == HareketTipi.Giris).Sum(h => h.Tutar);

            kumulatifBakiye += borc - alacak;

            trendler.Add(new GenelAylikTrend
            {
                Yil = ay.Year,
                Ay = ay.Month,
                ToplamBorc = borc,
                ToplamAlacak = alacak,
                AySonuToplamBakiye = kumulatifBakiye,
                FaturaSayisi = ayFaturalar.Count,
                OdemeSayisi = ayOdemeler.Count
            });
        }

        return trendler;
    }

    #endregion

    #region Risk ve Tahsilat Planı

    public async Task<int> HesaplaRiskSkoruAsync(int cariId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var cari = await GetCariDetayAsync(cariId);
        return cari.RiskSkoru;
    }

    private int HesaplaRiskSkoru(decimal netBakiye, decimal vadesiGecmisBakiye, int vadesiGecmisFaturaSayisi)
    {
        if (netBakiye <= 0) return 0; // Alacaklı veya dengeli = risk yok

        var skor = 0;

        // Net bakiye büyüklüğü (max 30 puan)
        if (netBakiye > 100000) skor += 30;
        else if (netBakiye > 50000) skor += 20;
        else if (netBakiye > 10000) skor += 10;
        else if (netBakiye > 1000) skor += 5;

        // Vadesi geçmiş bakiye oranı (max 40 puan)
        if (netBakiye > 0)
        {
            var vadesiGecmisOran = (vadesiGecmisBakiye / netBakiye) * 100;
            if (vadesiGecmisOran >= 80) skor += 40;
            else if (vadesiGecmisOran >= 60) skor += 30;
            else if (vadesiGecmisOran >= 40) skor += 20;
            else if (vadesiGecmisOran >= 20) skor += 10;
        }

        // Vadesi geçmiş fatura sayısı (max 30 puan)
        if (vadesiGecmisFaturaSayisi >= 5) skor += 30;
        else if (vadesiGecmisFaturaSayisi >= 3) skor += 20;
        else if (vadesiGecmisFaturaSayisi >= 1) skor += 10;

        return Math.Min(skor, 100);
    }

    private int HesaplaOrtalamaOdemeSuresi(List<Fatura> faturalar, List<BankaKasaHareket> odemeler)
    {
        if (!faturalar.Any() || !odemeler.Any()) return 0;

        var odemeSureleri = new List<int>();
        foreach (var fatura in faturalar.Where(f => f.OdenenTutar > 0))
        {
            var ilkOdeme = odemeler
                .Where(o => o.IslemTarihi >= fatura.FaturaTarihi && o.HareketTipi == HareketTipi.Giris)
                .OrderBy(o => o.IslemTarihi)
                .FirstOrDefault();

            if (ilkOdeme != null)
            {
                var sure = (int)(ilkOdeme.IslemTarihi - fatura.FaturaTarihi).TotalDays;
                odemeSureleri.Add(sure);
            }
        }

        return odemeSureleri.Any() ? (int)odemeSureleri.Average() : 0;
    }

    public async Task<List<TahsilatPlanItem>> OlusturTahsilatPlaniAsync(int cariId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var acikFaturalar = await GetAcikFaturalarAsync(cariId);
        var plan = new List<TahsilatPlanItem>();
        var sira = 1;

        // Önce vadesi geçmişleri, sonra yaklaşanları sırala
        var siralilar = acikFaturalar
            .OrderByDescending(f => f.VadesiGecmisMi)
            .ThenBy(f => f.KalanGun)
            .ToList();

        foreach (var fatura in siralilar)
        {
            var oncelik = fatura.GecikmeGunu switch
            {
                > 90 => 1,
                > 60 => 1,
                > 30 => 2,
                > 0 => 2,
                _ => 3
            };

            plan.Add(new TahsilatPlanItem
            {
                FaturaId = fatura.FaturaId,
                FaturaNo = fatura.FaturaNo,
                PlanTarihi = fatura.VadesiGecmisMi ? DateTime.Today : (fatura.VadeTarihi ?? DateTime.Today),
                PlanTutar = fatura.KalanTutar,
                Aciklama = fatura.VadesiGecmisMi 
                    ? $"{fatura.GecikmeGunu} gün gecikmiş - ACİL TAHSİL"
                    : $"{fatura.KalanGun} gün sonra vadeli",
                OncelikSirasi = oncelik
            });
            sira++;
        }

        return plan.OrderBy(p => p.OncelikSirasi).ThenBy(p => p.PlanTarihi).ToList();
    }

    private async Task HesaplaVadeAnaliziAsync(ApplicationDbContext context, CariBorcAlacakOzet result, DateTime bugun)
    {
        // NOT: KalanTutar hesaplanmış property, LINQ'da (GenelToplam - OdenenTutar) kullanılmalı
        var acikFaturalar = await context.Faturalar
            .AsNoTracking()
            .Where(f => !f.IsDeleted && (f.GenelToplam - f.OdenenTutar) > 0 && f.FaturaYonu == FaturaYonu.Giden)
            .ToListAsync();

        foreach (var fatura in acikFaturalar)
        {
            var vadeGunu = fatura.VadeTarihi.HasValue 
                ? (int)(bugun - fatura.VadeTarihi.Value).TotalDays 
                : -9999; // Vadesi belirlenmemiş = güncel

            if (vadeGunu < 0) // Vadesi gelmemiş
                result.VadesiGelmemis += fatura.KalanTutar;
            else if (vadeGunu <= 30)
                result.Vadesi0_30 += fatura.KalanTutar;
            else if (vadeGunu <= 60)
                result.Vadesi31_60 += fatura.KalanTutar;
            else if (vadeGunu <= 90)
                result.Vadesi61_90 += fatura.KalanTutar;
            else
                result.Vadesi90Plus += fatura.KalanTutar;
        }
    }

    #endregion

    #region Excel Export

    public async Task<byte[]> ExportToExcelAsync(CariBorcAlacakOzet rapor)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Cari Borç Alacak");

        // Başlık
        worksheet.Cell("A1").Value = "CARİ BORÇ/ALACAK TAKİP RAPORU";
        worksheet.Range("A1:K1").Merge().Style.Font.Bold = true;
        worksheet.Cell("A1").Style.Font.FontSize = 14;

        // Özet bilgiler
        worksheet.Cell("A3").Value = "Toplam Borç:";
        worksheet.Cell("B3").Value = rapor.ToplamBorc;
        worksheet.Cell("C3").Value = "Toplam Alacak:";
        worksheet.Cell("D3").Value = rapor.ToplamAlacak;
        worksheet.Cell("E3").Value = "Net Bakiye:";
        worksheet.Cell("F3").Value = rapor.NetBakiye;

        worksheet.Cell("A4").Value = "Toplam Cari:";
        worksheet.Cell("B4").Value = rapor.ToplamCariSayisi;
        worksheet.Cell("C4").Value = "Borçlu Cari:";
        worksheet.Cell("D4").Value = rapor.BorcluCariSayisi;
        worksheet.Cell("E4").Value = "Riskli Cari:";
        worksheet.Cell("F4").Value = rapor.RiskliCariSayisi;

        // Tablo başlıkları
        var row = 6;
        var headers = new[] { "Cari Kodu", "Unvan", "Cari Tipi", "Telefon", "Toplam Borç", "Toplam Alacak", "Net Bakiye", "Vadesi Geçmiş", "Risk Skoru", "Risk Seviyesi", "Ort. Ödeme Süresi" };
        for (var i = 0; i < headers.Length; i++)
        {
            worksheet.Cell(row, i + 1).Value = headers[i];
            worksheet.Cell(row, i + 1).Style.Font.Bold = true;
            worksheet.Cell(row, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        // Veriler
        row++;
        foreach (var cari in rapor.Cariler)
        {
            worksheet.Cell(row, 1).Value = cari.CariKodu;
            worksheet.Cell(row, 2).Value = cari.Unvan;
            worksheet.Cell(row, 3).Value = cari.CariTipi;
            worksheet.Cell(row, 4).Value = cari.Telefon;
            worksheet.Cell(row, 5).Value = cari.ToplamBorc;
            worksheet.Cell(row, 6).Value = cari.ToplamAlacak;
            worksheet.Cell(row, 7).Value = cari.NetBakiye;
            worksheet.Cell(row, 8).Value = cari.VadesiGecmisBakiye;
            worksheet.Cell(row, 9).Value = cari.RiskSkoru;
            worksheet.Cell(row, 10).Value = cari.RiskSeviyesi;
            worksheet.Cell(row, 11).Value = $"{cari.OrtalamaOdemeSuresi} gün";

            // Risk renklendirmesi
            if (cari.RiskSkoru >= 60)
                worksheet.Row(row).Style.Fill.BackgroundColor = XLColor.LightPink;
            else if (cari.RiskSkoru >= 40)
                worksheet.Row(row).Style.Fill.BackgroundColor = XLColor.LightYellow;

            row++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<byte[]> ExportCariDetayToExcelAsync(CariHareketTakipRapor rapor)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        using var workbook = new XLWorkbook();
        
        // Özet sayfası
        var ozet = workbook.Worksheets.Add("Özet");
        ozet.Cell("A1").Value = $"CARİ DETAY RAPORU - {rapor.Unvan}";
        ozet.Range("A1:D1").Merge().Style.Font.Bold = true;

        ozet.Cell("A3").Value = "Cari Kodu:";
        ozet.Cell("B3").Value = rapor.CariKodu;
        ozet.Cell("A4").Value = "Cari Tipi:";
        ozet.Cell("B4").Value = rapor.CariTipi;
        ozet.Cell("A5").Value = "Telefon:";
        ozet.Cell("B5").Value = rapor.Telefon;

        ozet.Cell("A7").Value = "Toplam Borç:";
        ozet.Cell("B7").Value = rapor.ToplamBorc;
        ozet.Cell("A8").Value = "Toplam Alacak:";
        ozet.Cell("B8").Value = rapor.ToplamAlacak;
        ozet.Cell("A9").Value = "Net Bakiye:";
        ozet.Cell("B9").Value = rapor.NetBakiye;
        ozet.Cell("A10").Value = "Risk Skoru:";
        ozet.Cell("B10").Value = $"{rapor.RiskSkoru} ({rapor.RiskSeviyesi})";

        // Hareket sayfası
        var hareketler = workbook.Worksheets.Add("Hareketler");
        hareketler.Cell("A1").Value = "Tarih";
        hareketler.Cell("B1").Value = "Tip";
        hareketler.Cell("C1").Value = "Açıklama";
        hareketler.Cell("D1").Value = "Belge No";
        hareketler.Cell("E1").Value = "Vade Tarihi";
        hareketler.Cell("F1").Value = "Borç";
        hareketler.Cell("G1").Value = "Alacak";
        hareketler.Cell("H1").Value = "Bakiye";
        hareketler.Row(1).Style.Font.Bold = true;

        var row = 2;
        foreach (var hareket in rapor.Hareketler)
        {
            hareketler.Cell(row, 1).Value = hareket.Tarih;
            hareketler.Cell(row, 2).Value = hareket.HareketTipi;
            hareketler.Cell(row, 3).Value = hareket.Aciklama;
            hareketler.Cell(row, 4).Value = hareket.BelgeNo;
            hareketler.Cell(row, 5).Value = hareket.VadeTarihi;
            hareketler.Cell(row, 6).Value = hareket.Borc;
            hareketler.Cell(row, 7).Value = hareket.Alacak;
            hareketler.Cell(row, 8).Value = hareket.Bakiye;
            row++;
        }

        hareketler.Columns().AdjustToContents();

        // Açık faturalar sayfası
        var acikFat = workbook.Worksheets.Add("Açık Faturalar");
        acikFat.Cell("A1").Value = "Fatura No";
        acikFat.Cell("B1").Value = "Fatura Tarihi";
        acikFat.Cell("C1").Value = "Vade Tarihi";
        acikFat.Cell("D1").Value = "Fatura Tutarı";
        acikFat.Cell("E1").Value = "Ödenen";
        acikFat.Cell("F1").Value = "Kalan";
        acikFat.Cell("G1").Value = "Vade Durumu";
        acikFat.Row(1).Style.Font.Bold = true;

        row = 2;
        foreach (var fatura in rapor.AcikFaturalar)
        {
            acikFat.Cell(row, 1).Value = fatura.FaturaNo;
            acikFat.Cell(row, 2).Value = fatura.FaturaTarihi;
            acikFat.Cell(row, 3).Value = fatura.VadeTarihi;
            acikFat.Cell(row, 4).Value = fatura.FaturaTutari;
            acikFat.Cell(row, 5).Value = fatura.OdenenTutar;
            acikFat.Cell(row, 6).Value = fatura.KalanTutar;
            acikFat.Cell(row, 7).Value = fatura.VadeDurumu;

            if (fatura.VadesiGecmisMi)
                acikFat.Row(row).Style.Fill.BackgroundColor = XLColor.LightPink;

            row++;
        }

        acikFat.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    #endregion
}



