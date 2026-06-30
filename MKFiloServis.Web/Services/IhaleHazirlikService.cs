using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using MKFiloServis.Web.Models;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Web.Services;

public class IhaleHazirlikService : IIhaleHazirlikService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IOllamaService _ollamaService;
    private readonly ILogger<IhaleHazirlikService> _logger;

    private static readonly string[] AyAdlari = ["Ocak", "Şubat", "Mart", "Nisan", "Mayıs", "Haziran",
        "Temmuz", "Ağustos", "Eylül", "Ekim", "Kasım", "Aralık"];

    public IhaleHazirlikService(IDbContextFactory<ApplicationDbContext> contextFactory, IOllamaService ollamaService, ILogger<IhaleHazirlikService> logger)
    {
        _contextFactory = contextFactory;
        _ollamaService = ollamaService;
        _logger = logger;
    }

    // ===== Proje CRUD =====

    public async Task<List<IhaleProje>> GetProjelerAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.IhaleProjeleri
            .Include(p => p.Cari)
            .Include(p => p.Firma)
            .Include(p => p.Kalemler)
            .Where(p => !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<IhaleProje?> GetProjeByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.IhaleProjeleri
            .Include(p => p.Cari)
            .Include(p => p.Firma)
            .Include(p => p.Kalemler).ThenInclude(k => k.Guzergah)
            .Include(p => p.Kalemler).ThenInclude(k => k.Arac)
            .Include(p => p.Kalemler).ThenInclude(k => k.Sofor)
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
    }

    public async Task<IhaleProje> CreateProjeAsync(IhaleProje proje)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        if (string.IsNullOrEmpty(proje.ProjeKodu))
            proje.ProjeKodu = await GenerateProjeKoduAsync();

        context.IhaleProjeleri.Add(proje);
        await context.SaveChangesAsync();
        return proje;
    }

    public async Task<IhaleProje> UpdateProjeAsync(IhaleProje proje)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var mevcut = await context.IhaleProjeleri.FindAsync(proje.Id);
        if (mevcut == null) throw new Exception("Proje bulunamadı.");

        mevcut.ProjeAdi = proje.ProjeAdi;
        mevcut.Aciklama = proje.Aciklama;
        mevcut.CariId = proje.CariId;
        mevcut.FirmaId = proje.FirmaId;
        mevcut.BaslangicTarihi = proje.BaslangicTarihi;
        mevcut.BitisTarihi = proje.BitisTarihi;
        mevcut.SozlesmeSuresiAy = proje.SozlesmeSuresiAy;
        mevcut.Durum = proje.Durum;
        mevcut.EnflasyonOrani = proje.EnflasyonOrani;
        mevcut.YakitZamOrani = proje.YakitZamOrani;
        mevcut.AylikCalismGunu = proje.AylikCalismGunu;
        mevcut.GunlukCalismaSaati = proje.GunlukCalismaSaati;
        mevcut.Notlar = proje.Notlar;
        mevcut.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
        return mevcut;
    }

    public async Task<bool> DeleteProjeAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var proje = await context.IhaleProjeleri.FindAsync(id);
        if (proje == null) return false;

        proje.IsDeleted = true;
        proje.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<IhaleProje> KopyalaProjeAsync(int projeId, string yeniProjeAdi)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var kaynak = await GetProjeByIdAsync(projeId);
        if (kaynak == null) throw new Exception("Kaynak proje bulunamadı.");

        var yeni = new IhaleProje
        {
            ProjeKodu = await GenerateProjeKoduAsync(),
            ProjeAdi = yeniProjeAdi,
            Aciklama = kaynak.Aciklama,
            CariId = kaynak.CariId,
            FirmaId = kaynak.FirmaId,
            BaslangicTarihi = kaynak.BaslangicTarihi,
            BitisTarihi = kaynak.BitisTarihi,
            SozlesmeSuresiAy = kaynak.SozlesmeSuresiAy,
            EnflasyonOrani = kaynak.EnflasyonOrani,
            YakitZamOrani = kaynak.YakitZamOrani,
            AylikCalismGunu = kaynak.AylikCalismGunu,
            GunlukCalismaSaati = kaynak.GunlukCalismaSaati,
            Durum = IhaleProjeDurum.Taslak
        };

        foreach (var k in kaynak.Kalemler)
        {
            yeni.Kalemler.Add(new IhaleGuzergahKalem
            {
                HatAdi = k.HatAdi,
                BaslangicNoktasi = k.BaslangicNoktasi,
                BitisNoktasi = k.BitisNoktasi,
                MesafeKm = k.MesafeKm,
                TahminiSureDakika = k.TahminiSureDakika,
                SeferTipi = k.SeferTipi,
                GunlukSeferSayisi = k.GunlukSeferSayisi,
                AylikSeferGunu = k.AylikSeferGunu,
                PersonelSayisi = k.PersonelSayisi,
                SahiplikDurumu = k.SahiplikDurumu,
                AracModelBilgi = k.AracModelBilgi,
                AracKoltukSayisi = k.AracKoltukSayisi,
                YakitTuketimi = k.YakitTuketimi,
                YakitFiyati = k.YakitFiyati,
                AylikBakimMasrafi = k.AylikBakimMasrafi,
                AylikLastikMasrafi = k.AylikLastikMasrafi,
                AylikSigortaMasrafi = k.AylikSigortaMasrafi,
                AylikKaskoMasrafi = k.AylikKaskoMasrafi,
                AylikMuayeneMasrafi = k.AylikMuayeneMasrafi,
                AylikYedekParcaMasrafi = k.AylikYedekParcaMasrafi,
                AylikDigerMasraf = k.AylikDigerMasraf,
                AylikKiraBedeli = k.AylikKiraBedeli,
                SeferBasiKomisyon = k.SeferBasiKomisyon,
                SoforBrutMaas = k.SoforBrutMaas,
                SoforNetMaas = k.SoforNetMaas,
                SoforSGKIsverenPay = k.SoforSGKIsverenPay,
                SoforToplamMaliyet = k.SoforToplamMaliyet,
                AracDegeri = k.AracDegeri,
                AmortismanYili = k.AmortismanYili,
                KarMarjiOrani = k.KarMarjiOrani
            });
        }

        context.IhaleProjeleri.Add(yeni);
        await context.SaveChangesAsync();

        // Kopyalanan kalemler için maliyet hesapla
        foreach (var kalem in yeni.Kalemler)
        {
            await HesaplaKalemMaliyetAsync(kalem, yeni);
        }
        await context.SaveChangesAsync();

        return yeni;
    }

    public async Task<string> GenerateProjeKoduAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var yil = DateTime.Now.Year;
        var sonProje = await context.IhaleProjeleri
            .Where(p => p.ProjeKodu.StartsWith($"IHL-{yil}-"))
            .OrderByDescending(p => p.ProjeKodu)
            .FirstOrDefaultAsync();

        int sira = 1;
        if (sonProje != null)
        {
            var parts = sonProje.ProjeKodu.Split('-');
            if (parts.Length == 3 && int.TryParse(parts[2], out int sonSira))
                sira = sonSira + 1;
        }

        return $"IHL-{yil}-{sira:D4}";
    }

    // ===== Güzergah Kalem CRUD =====

    public async Task<IhaleGuzergahKalem> AddKalemAsync(IhaleGuzergahKalem kalem)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var proje = await context.IhaleProjeleri.FindAsync(kalem.IhaleProjeId);
        if (proje == null) throw new Exception("Proje bulunamadı.");

        // Güzergah seçildiyse bilgileri aktar
        if (kalem.GuzergahId.HasValue)
        {
            var guzergah = await context.Guzergahlar.FindAsync(kalem.GuzergahId.Value);
            if (guzergah != null)
            {
                if (string.IsNullOrEmpty(kalem.HatAdi)) kalem.HatAdi = guzergah.GuzergahAdi;
                if (string.IsNullOrEmpty(kalem.BaslangicNoktasi)) kalem.BaslangicNoktasi = guzergah.BaslangicNoktasi;
                if (string.IsNullOrEmpty(kalem.BitisNoktasi)) kalem.BitisNoktasi = guzergah.BitisNoktasi;
                if (kalem.MesafeKm == 0 && guzergah.Mesafe.HasValue) kalem.MesafeKm = guzergah.Mesafe.Value;
                if (kalem.TahminiSureDakika == 0 && guzergah.TahminiSure.HasValue) kalem.TahminiSureDakika = guzergah.TahminiSure.Value;
                if (kalem.PersonelSayisi == 0) kalem.PersonelSayisi = guzergah.PersonelSayisi;
                kalem.SeferTipi = guzergah.SeferTipi;
            }
        }

        // Araç seçildiyse bilgileri aktar
        if (kalem.AracId.HasValue)
        {
            var arac = await context.Araclar.FindAsync(kalem.AracId.Value);
            if (arac != null)
            {
                kalem.AracModelBilgi = $"{arac.ModelYili} {arac.Marka} {arac.Model}";
                kalem.AracKoltukSayisi = arac.KoltukSayisi;
                kalem.SahiplikDurumu = arac.SahiplikTipi switch
                {
                    AracSahiplikTipi.Ozmal => AracSahiplikKalem.Ozmal,
                    AracSahiplikTipi.Kiralik => AracSahiplikKalem.Kiralik,
                    _ => AracSahiplikKalem.Ozmal
                };
                if (arac.AylikKiraBedeli.HasValue)
                    kalem.AylikKiraBedeli = arac.AylikKiraBedeli.Value;
                if (arac.KomisyonVar && arac.SabitKomisyonTutari.HasValue)
                    kalem.SeferBasiKomisyon = arac.SabitKomisyonTutari.Value;
            }
        }

        // Şoför seçildiyse maaş bilgisi aktar
        if (kalem.SoforId.HasValue)
        {
            var sofor = await context.Soforler.FindAsync(kalem.SoforId.Value);
            if (sofor != null)
            {
                kalem.SoforBrutMaas = sofor.BrutMaas;
                kalem.SoforNetMaas = sofor.NetMaas;
            }
        }

        await HesaplaKalemMaliyetAsync(kalem, proje);

        context.IhaleGuzergahKalemleri.Add(kalem);
        await context.SaveChangesAsync();
        return kalem;
    }

    public async Task<IhaleGuzergahKalem> UpdateKalemAsync(IhaleGuzergahKalem kalem)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var mevcut = await context.IhaleGuzergahKalemleri
            .Include(k => k.IhaleProje)
            .FirstOrDefaultAsync(k => k.Id == kalem.Id);
        if (mevcut == null) throw new Exception("Kalem bulunamadı.");

        // Tüm alanları güncelle
        mevcut.GuzergahId = kalem.GuzergahId;
        mevcut.HatAdi = kalem.HatAdi;
        mevcut.BaslangicNoktasi = kalem.BaslangicNoktasi;
        mevcut.BitisNoktasi = kalem.BitisNoktasi;
        mevcut.MesafeKm = kalem.MesafeKm;
        mevcut.TahminiSureDakika = kalem.TahminiSureDakika;
        mevcut.SeferTipi = kalem.SeferTipi;
        mevcut.GunlukSeferSayisi = kalem.GunlukSeferSayisi;
        mevcut.AylikSeferGunu = kalem.AylikSeferGunu;
        mevcut.PersonelSayisi = kalem.PersonelSayisi;
        mevcut.AracId = kalem.AracId;
        mevcut.SahiplikDurumu = kalem.SahiplikDurumu;
        mevcut.AracModelBilgi = kalem.AracModelBilgi;
        mevcut.AracKoltukSayisi = kalem.AracKoltukSayisi;
        mevcut.YakitTuketimi = kalem.YakitTuketimi;
        mevcut.YakitFiyati = kalem.YakitFiyati;
        mevcut.AylikBakimMasrafi = kalem.AylikBakimMasrafi;
        mevcut.AylikLastikMasrafi = kalem.AylikLastikMasrafi;
        mevcut.AylikSigortaMasrafi = kalem.AylikSigortaMasrafi;
        mevcut.AylikKaskoMasrafi = kalem.AylikKaskoMasrafi;
        mevcut.AylikMuayeneMasrafi = kalem.AylikMuayeneMasrafi;
        mevcut.AylikYedekParcaMasrafi = kalem.AylikYedekParcaMasrafi;
        mevcut.AylikDigerMasraf = kalem.AylikDigerMasraf;
        mevcut.AylikKiraBedeli = kalem.AylikKiraBedeli;
        mevcut.SeferBasiKomisyon = kalem.SeferBasiKomisyon;
        mevcut.SoforId = kalem.SoforId;
        mevcut.SoforBrutMaas = kalem.SoforBrutMaas;
        mevcut.SoforNetMaas = kalem.SoforNetMaas;
        mevcut.SoforSGKIsverenPay = kalem.SoforSGKIsverenPay;
        mevcut.SoforToplamMaliyet = kalem.SoforToplamMaliyet;
        mevcut.AracDegeri = kalem.AracDegeri;
        mevcut.AmortismanYili = kalem.AmortismanYili;
        mevcut.KarMarjiOrani = kalem.KarMarjiOrani;
        mevcut.AITahminiKullanildi = kalem.AITahminiKullanildi;
        mevcut.AITahminDetay = kalem.AITahminDetay;
        mevcut.UpdatedAt = DateTime.UtcNow;

        await HesaplaKalemMaliyetAsync(mevcut, mevcut.IhaleProje);
        await context.SaveChangesAsync();
        return mevcut;
    }

    public async Task<List<IhaleSozlesmeRevizyon>> GetSozlesmeRevizyonlariAsync(int ihaleProjeId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.IhaleSozlesmeRevizyonlari
            .Where(x => x.IhaleProjeId == ihaleProjeId)
            .OrderByDescending(x => x.RevizyonTarihi)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<IhaleSozlesmeRevizyon> AddSozlesmeRevizyonAsync(IhaleSozlesmeRevizyon revizyon)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var proje = await context.IhaleProjeleri.FindAsync(revizyon.IhaleProjeId);
        if (proje == null)
            throw new Exception("Proje bulunamadı.");

        if (string.IsNullOrWhiteSpace(revizyon.RevizyonNo))
            revizyon.RevizyonNo = await GetSonrakiRevizyonNoAsync(context, revizyon.IhaleProjeId);

        revizyon.CreatedAt = DateTime.UtcNow;
        context.IhaleSozlesmeRevizyonlari.Add(revizyon);
        await context.SaveChangesAsync();
        return revizyon;
    }

    public async Task<IhaleSozlesmeRevizyon> UpdateSozlesmeRevizyonAsync(IhaleSozlesmeRevizyon revizyon)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var mevcut = await context.IhaleSozlesmeRevizyonlari.FindAsync(revizyon.Id);
        if (mevcut == null)
            throw new Exception("Sözleşme revizyon kaydı bulunamadı.");

        mevcut.RevizyonTipi = revizyon.RevizyonTipi;
        mevcut.RevizyonNo = revizyon.RevizyonNo;
        mevcut.Baslik = revizyon.Baslik;
        mevcut.Aciklama = revizyon.Aciklama;
        mevcut.RevizyonTarihi = revizyon.RevizyonTarihi;
        mevcut.YurutmeTarihi = revizyon.YurutmeTarihi;
        mevcut.BedelFarki = revizyon.BedelFarki;
        mevcut.SureFarkiAy = revizyon.SureFarkiAy;
        mevcut.Aktif = revizyon.Aktif;
        mevcut.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
        return mevcut;
    }

    public async Task<bool> DeleteSozlesmeRevizyonAsync(int revizyonId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var mevcut = await context.IhaleSozlesmeRevizyonlari.FindAsync(revizyonId);
        if (mevcut == null)
            return false;

        mevcut.IsDeleted = true;
        mevcut.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteKalemAsync(int kalemId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var kalem = await context.IhaleGuzergahKalemleri.FindAsync(kalemId);
        if (kalem == null) return false;

        kalem.IsDeleted = true;
        kalem.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        return true;
    }

    // ===== Hesaplamalar =====

    public Task HesaplaKalemMaliyetAsync(IhaleGuzergahKalem kalem, IhaleProje proje)
    {
        // Yakıt maliyeti
        var gunlukKm = kalem.MesafeKm * kalem.GunlukSeferSayisi;
        var gunlukYakit = gunlukKm * kalem.YakitTuketimi / 100;
        kalem.GunlukYakitMaliyeti = gunlukYakit * kalem.YakitFiyati;
        kalem.AylikYakitMaliyeti = kalem.GunlukYakitMaliyeti * kalem.AylikSeferGunu;

        // Komisyon hesapla
        kalem.AylikKomisyonToplam = kalem.SeferBasiKomisyon * kalem.GunlukSeferSayisi * kalem.AylikSeferGunu;

        // Şoför SGK İşveren payı (yaklaşık %22.5)
        if (kalem.SoforSGKIsverenPay == 0 && kalem.SoforBrutMaas > 0)
            kalem.SoforSGKIsverenPay = kalem.SoforBrutMaas * 0.225m;

        kalem.SoforToplamMaliyet = kalem.SoforBrutMaas + kalem.SoforSGKIsverenPay;

        // Amortisman (özmal için)
        if (kalem.SahiplikDurumu == AracSahiplikKalem.Ozmal && kalem.AracDegeri > 0 && kalem.AmortismanYili > 0)
            kalem.AylikAmortisman = kalem.AracDegeri / (kalem.AmortismanYili * 12);
        else
            kalem.AylikAmortisman = 0;

        // Toplam aylık maliyet
        var toplamAracMasraf = kalem.AylikBakimMasrafi + kalem.AylikLastikMasrafi +
            kalem.AylikSigortaMasrafi + kalem.AylikKaskoMasrafi + kalem.AylikMuayeneMasrafi +
            kalem.AylikYedekParcaMasrafi + kalem.AylikDigerMasraf;

        kalem.AylikMaliyet = kalem.AylikYakitMaliyeti + toplamAracMasraf +
            kalem.SoforToplamMaliyet + kalem.AylikKiraBedeli +
            kalem.AylikKomisyonToplam + kalem.AylikAmortisman;

        // Kâr marjı
        kalem.AylikKarTutari = kalem.AylikMaliyet * kalem.KarMarjiOrani / 100;

        // Teklif fiyatı
        kalem.AylikTeklifFiyati = kalem.AylikMaliyet + kalem.AylikKarTutari;

        // Toplam proje maliyeti (enflasyonsuz basit hesap)
        kalem.ToplamMaliyet = kalem.AylikTeklifFiyati * proje.SozlesmeSuresiAy;

        // Birim fiyatlar
        var aylikToplamSefer = kalem.GunlukSeferSayisi * kalem.AylikSeferGunu;
        kalem.SeferBasiMaliyet = aylikToplamSefer > 0 ? kalem.AylikMaliyet / aylikToplamSefer : 0;
        kalem.SeferBasiTeklifFiyati = aylikToplamSefer > 0 ? kalem.AylikTeklifFiyati / aylikToplamSefer : 0;

        var aylikToplamSaat = kalem.AylikSeferGunu * proje.GunlukCalismaSaati;
        kalem.SaatlikMaliyet = aylikToplamSaat > 0 ? kalem.AylikMaliyet / aylikToplamSaat : 0;
        kalem.SaatlikTeklifFiyati = aylikToplamSaat > 0 ? kalem.AylikTeklifFiyati / aylikToplamSaat : 0;

        var aylikToplamKm = gunlukKm * kalem.AylikSeferGunu;
        kalem.KmBasiMaliyet = aylikToplamKm > 0 ? kalem.AylikMaliyet / aylikToplamKm : 0;

        return Task.CompletedTask;
    }

    public List<AylikProjeksiyon> HesaplaEnflasyonluProjeksiyon(IhaleGuzergahKalem kalem, IhaleProje proje)
    {
        var projeksiyonlar = new List<AylikProjeksiyon>();
        var baslangic = proje.BaslangicTarihi;
        var aylikEnflasyon = (decimal)Math.Pow((double)(1 + proje.EnflasyonOrani / 100), 1.0 / 12) - 1;
        var aylikYakitZam = (decimal)Math.Pow((double)(1 + proje.YakitZamOrani / 100), 1.0 / 12) - 1;

        var toplamAracMasraf = kalem.AylikBakimMasrafi + kalem.AylikLastikMasrafi +
            kalem.AylikSigortaMasrafi + kalem.AylikKaskoMasrafi + kalem.AylikMuayeneMasrafi +
            kalem.AylikYedekParcaMasrafi + kalem.AylikDigerMasraf;

        for (int i = 0; i < proje.SozlesmeSuresiAy; i++)
        {
            var tarih = baslangic.AddMonths(i);
            var enflasyonCarpani = (decimal)Math.Pow((double)(1 + aylikEnflasyon), i);
            var yakitCarpani = (decimal)Math.Pow((double)(1 + aylikYakitZam), i);

            var p = new AylikProjeksiyon
            {
                Ay = i + 1,
                Yil = tarih.Year,
                AyAdi = $"{AyAdlari[tarih.Month - 1]} {tarih.Year}",
                YakitMaliyeti = kalem.AylikYakitMaliyeti * yakitCarpani,
                AracMasrafi = toplamAracMasraf * enflasyonCarpani,
                SoforMaliyeti = kalem.SoforToplamMaliyet * enflasyonCarpani,
                KiraKomisyon = (kalem.AylikKiraBedeli + kalem.AylikKomisyonToplam) * enflasyonCarpani,
                Amortisman = kalem.AylikAmortisman, // Amortisman sabit
                EnflasyonCarpani = enflasyonCarpani
            };

            p.ToplamMaliyet = p.YakitMaliyeti + p.AracMasrafi + p.SoforMaliyeti + p.KiraKomisyon + p.Amortisman;
            p.KarTutari = p.ToplamMaliyet * kalem.KarMarjiOrani / 100;
            p.TeklifFiyati = p.ToplamMaliyet + p.KarTutari;

            projeksiyonlar.Add(p);
        }

        return projeksiyonlar;
    }

    public async Task<IhaleProjeOzet> GetProjeOzetAsync(int projeId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var proje = await GetProjeByIdAsync(projeId);
        if (proje == null) throw new Exception("Proje bulunamadı.");

        var aktifKalemler = proje.Kalemler.Where(k => !k.IsDeleted).ToList();

        var ozet = new IhaleProjeOzet
        {
            ProjeId = proje.Id,
            ProjeKodu = proje.ProjeKodu,
            ProjeAdi = proje.ProjeAdi,
            MusteriFirma = proje.Cari?.Unvan ?? proje.Firma?.FirmaAdi,
            SozlesmeSuresiAy = proje.SozlesmeSuresiAy,
            GuzergahSayisi = aktifKalemler.Count,
            AracSayisi = aktifKalemler.Count(k => k.AracId.HasValue || !string.IsNullOrEmpty(k.AracModelBilgi))
        };

        foreach (var k in aktifKalemler)
        {
            var toplamAracMasraf = k.AylikBakimMasrafi + k.AylikLastikMasrafi +
                k.AylikSigortaMasrafi + k.AylikKaskoMasrafi + k.AylikMuayeneMasrafi +
                k.AylikYedekParcaMasrafi + k.AylikDigerMasraf;

            ozet.ToplamAylikYakit += k.AylikYakitMaliyeti;
            ozet.ToplamAylikAracMasraf += toplamAracMasraf;
            ozet.ToplamAylikSoforMaliyet += k.SoforToplamMaliyet;
            ozet.ToplamAylikKiraKomisyon += k.AylikKiraBedeli + k.AylikKomisyonToplam;
            ozet.ToplamAylikAmortisman += k.AylikAmortisman;
            ozet.ToplamAylikMaliyet += k.AylikMaliyet;
            ozet.ToplamAylikKar += k.AylikKarTutari;
            ozet.ToplamAylikTeklifFiyati += k.AylikTeklifFiyati;

            ozet.KalemOzetleri.Add(new IhaleKalemOzet
            {
                KalemId = k.Id,
                HatAdi = k.HatAdi,
                MesafeKm = k.MesafeKm,
                SahiplikDurumu = k.SahiplikDurumu switch
                {
                    AracSahiplikKalem.Ozmal => "Özmal",
                    AracSahiplikKalem.Kiralik => "Kiralık",
                    AracSahiplikKalem.Komisyon => "Komisyon",
                    _ => "-"
                },
                AracBilgi = k.AracModelBilgi ?? "-",
                AylikMaliyet = k.AylikMaliyet,
                AylikTeklifFiyati = k.AylikTeklifFiyati,
                KarMarji = k.KarMarjiOrani,
                SeferBasiTeklif = k.SeferBasiTeklifFiyati
            });
        }

        // Toplam proje
        ozet.ToplamProjeMaliyeti = ozet.ToplamAylikMaliyet * proje.SozlesmeSuresiAy;
        ozet.ToplamProjeKar = ozet.ToplamAylikKar * proje.SozlesmeSuresiAy;
        ozet.ToplamProjeTeklif = ozet.ToplamAylikTeklifFiyati * proje.SozlesmeSuresiAy;

        // Ortalama birim fiyatlar
        if (aktifKalemler.Count > 0)
        {
            ozet.KarMarjiOrtalama = aktifKalemler.Average(k => k.KarMarjiOrani);
            ozet.OrtalamaSeferBasiMaliyet = aktifKalemler.Average(k => k.SeferBasiMaliyet);
            ozet.OrtalamaSaatlikMaliyet = aktifKalemler.Average(k => k.SaatlikMaliyet);
            ozet.OrtalamaKmBasiMaliyet = aktifKalemler.Average(k => k.KmBasiMaliyet);
        }

        // Enflasyonlu aylık projeksiyon
        decimal kumulatifMaliyet = 0, kumulatifKar = 0;
        for (int ay = 0; ay < proje.SozlesmeSuresiAy; ay++)
        {
            var tarih = proje.BaslangicTarihi.AddMonths(ay);
            decimal aylikMaliyet = 0, aylikKar = 0, aylikTeklif = 0;

            foreach (var k in aktifKalemler)
            {
                var projeksiyonlar = HesaplaEnflasyonluProjeksiyon(k, proje);
                if (ay < projeksiyonlar.Count)
                {
                    aylikMaliyet += projeksiyonlar[ay].ToplamMaliyet;
                    aylikKar += projeksiyonlar[ay].KarTutari;
                    aylikTeklif += projeksiyonlar[ay].TeklifFiyati;
                }
            }

            kumulatifMaliyet += aylikMaliyet;
            kumulatifKar += aylikKar;

            ozet.AylikProjeksiyonlar.Add(new AylikProjeksiyonOzet
            {
                Ay = ay + 1,
                DonemAdi = $"{AyAdlari[tarih.Month - 1]} {tarih.Year}",
                ToplamMaliyet = aylikMaliyet,
                ToplamKar = aylikKar,
                ToplamTeklif = aylikTeklif,
                KumulatifMaliyet = kumulatifMaliyet,
                KumulatifKar = kumulatifKar,
                EnflasyonEtkisi = aylikMaliyet - ozet.ToplamAylikMaliyet
            });
        }

        return ozet;
    }

    public async Task<IhaleGerceklesenAnalizOzet> GetProjeGerceklesenAnalizAsync(int projeId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var proje = await GetProjeByIdAsync(projeId);
        if (proje == null)
            throw new Exception("Proje bulunamadı.");

        var analizBaslangic = proje.BaslangicTarihi.Date;
        var analizBitis = proje.BitisTarihi.Date < DateTime.Today ? proje.BitisTarihi.Date : DateTime.Today;

        if (analizBitis < analizBaslangic)
        {
            return new IhaleGerceklesenAnalizOzet
            {
                ProjeId = projeId,
                AnalizBaslangicTarihi = analizBaslangic,
                AnalizBitisTarihi = analizBaslangic
            };
        }

        var gecenAySayisi = GetInclusiveMonthCount(analizBaslangic, analizBitis);
        var aktifKalemler = proje.Kalemler.Where(k => !k.IsDeleted).ToList();
        var aktifRevizyonlar = await context.IhaleSozlesmeRevizyonlari
            .AsNoTracking()
            .Where(x => x.IhaleProjeId == proje.Id && x.Aktif)
            .OrderByDescending(x => x.RevizyonTarihi)
            .ToListAsync();

        var sonuc = new IhaleGerceklesenAnalizOzet
        {
            ProjeId = proje.Id,
            AnalizBaslangicTarihi = analizBaslangic,
            AnalizBitisTarihi = analizBitis,
            GecenAySayisi = gecenAySayisi,
            AktifSozlesmeRevizyonSayisi = aktifRevizyonlar.Count,
            ToplamRevizyonBedelFarki = aktifRevizyonlar.Sum(x => x.BedelFarki),
            ToplamRevizyonSureFarkiAy = aktifRevizyonlar.Sum(x => x.SureFarkiAy),
            AktifRevizyonlar = aktifRevizyonlar.Select(x => new IhaleSozlesmeRevizyonEtkisiOzet
            {
                RevizyonNo = x.RevizyonNo,
                Tip = GetSozlesmeRevizyonTipiMetni(x.RevizyonTipi),
                Baslik = x.Baslik,
                RevizyonTarihi = x.RevizyonTarihi,
                YurutmeTarihi = x.YurutmeTarihi,
                BedelFarki = x.BedelFarki,
                SureFarkiAy = x.SureFarkiAy
            }).ToList()
        };

        foreach (var kalem in aktifKalemler)
        {
            var servisler = await GetKalemServisCalismalariAsync(context, kalem, analizBaslangic, analizBitis);
            var gerceklesenSeferSayisi = servisler.Count(s => s.Durum == CalismaDurum.Tamamlandi);
            var aktifAySayisi = servisler
                .Where(s => s.Durum == CalismaDurum.Tamamlandi)
                .Select(s => GetMonthKey(s.CalismaTarihi))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();

            var gerceklesenYakitMaliyeti = gerceklesenSeferSayisi * kalem.MesafeKm * kalem.YakitTuketimi / 100m * kalem.YakitFiyati;
            var gerceklesenAracMasrafi = await GetGerceklesenAracMasrafiAsync(context, kalem, analizBaslangic, analizBitis);
            var gerceklesenSoforMaliyeti = await GetGerceklesenSoforMaliyetiAsync(context, kalem, analizBaslangic, analizBitis, gecenAySayisi);
            var gerceklesenKiraKomisyon = kalem.SahiplikDurumu switch
            {
                AracSahiplikKalem.Kiralik => kalem.AylikKiraBedeli * gecenAySayisi,
                AracSahiplikKalem.Komisyon => kalem.SeferBasiKomisyon * gerceklesenSeferSayisi,
                _ => 0m
            };
            var gerceklesenAmortisman = kalem.SahiplikDurumu == AracSahiplikKalem.Ozmal
                ? kalem.AylikAmortisman * gecenAySayisi
                : 0m;

            var gerceklesenToplamMaliyet = gerceklesenYakitMaliyeti + gerceklesenAracMasrafi + gerceklesenSoforMaliyeti + gerceklesenKiraKomisyon + gerceklesenAmortisman;
            var gerceklesenGelir = servisler
                .Where(s => s.Durum == CalismaDurum.Tamamlandi)
                .Sum(s => s.Fiyat ?? s.Guzergah?.BirimFiyat ?? 0m);

            var planlananMaliyet = kalem.AylikMaliyet * gecenAySayisi;
            var planlananTeklif = kalem.AylikTeklifFiyati * gecenAySayisi;
            var planlananKar = planlananTeklif - planlananMaliyet;
            var gerceklesenKar = gerceklesenGelir - gerceklesenToplamMaliyet;
            var maliyetSapmasi = gerceklesenToplamMaliyet - planlananMaliyet;
            var gelirSapmasi = gerceklesenGelir - planlananTeklif;
            var karSapmasi = gerceklesenKar - planlananKar;
            var maliyetSapmaOrani = planlananMaliyet > 0 ? maliyetSapmasi / planlananMaliyet * 100m : 0m;
            var gelirSapmaOrani = planlananTeklif > 0 ? gelirSapmasi / planlananTeklif * 100m : 0m;
            var karSapmaOrani = planlananKar != 0 ? karSapmasi / Math.Abs(planlananKar) * 100m : 0m;
            var kalemDogrulukSkoru = HesaplaTeklifDogrulukSkoru(maliyetSapmaOrani, gelirSapmaOrani, karSapmaOrani);
            var kalemRiskSeviyesi = HesaplaRiskSeviyesi(kalemDogrulukSkoru, maliyetSapmaOrani, karSapmasi);

            sonuc.Kalemler.Add(new IhaleGerceklesenKalemAnalizi
            {
                KalemId = kalem.Id,
                HatAdi = kalem.HatAdi,
                SahiplikDurumu = kalem.SahiplikDurumu.ToString(),
                GecenAySayisi = gecenAySayisi,
                GerceklesenSeferSayisi = gerceklesenSeferSayisi,
                PlanlananMaliyet = planlananMaliyet,
                GerceklesenMaliyet = gerceklesenToplamMaliyet,
                MaliyetSapmasi = maliyetSapmasi,
                PlanlananTeklif = planlananTeklif,
                GerceklesenGelir = gerceklesenGelir,
                GelirSapmasi = gelirSapmasi,
                PlanlananKar = planlananKar,
                GerceklesenKar = gerceklesenKar,
                KarSapmasi = karSapmasi,
                TeklifDogrulukSkoru = kalemDogrulukSkoru,
                RiskSeviyesi = kalemRiskSeviyesi
            });

            sonuc.ToplamGerceklesenSeferSayisi += gerceklesenSeferSayisi;
        }

        sonuc.PlanlananToplamMaliyet = sonuc.Kalemler.Sum(x => x.PlanlananMaliyet);
        sonuc.GerceklesenToplamMaliyet = sonuc.Kalemler.Sum(x => x.GerceklesenMaliyet);
        sonuc.MaliyetSapmasi = sonuc.GerceklesenToplamMaliyet - sonuc.PlanlananToplamMaliyet;
        sonuc.MaliyetSapmaOrani = sonuc.PlanlananToplamMaliyet > 0
            ? sonuc.MaliyetSapmasi / sonuc.PlanlananToplamMaliyet * 100m
            : 0m;

        sonuc.PlanlananToplamTeklif = sonuc.Kalemler.Sum(x => x.PlanlananTeklif);
        sonuc.GerceklesenToplamGelir = sonuc.Kalemler.Sum(x => x.GerceklesenGelir);
        sonuc.GelirSapmasi = sonuc.GerceklesenToplamGelir - sonuc.PlanlananToplamTeklif;
        sonuc.GelirSapmaOrani = sonuc.PlanlananToplamTeklif > 0
            ? sonuc.GelirSapmasi / sonuc.PlanlananToplamTeklif * 100m
            : 0m;
        sonuc.RevizyonEtkiliPlanlananTeklif = sonuc.PlanlananToplamTeklif + sonuc.ToplamRevizyonBedelFarki;
        sonuc.RevizyonEtkiliGelirSapmasi = sonuc.GerceklesenToplamGelir - sonuc.RevizyonEtkiliPlanlananTeklif;

        sonuc.PlanlananToplamKar = sonuc.Kalemler.Sum(x => x.PlanlananKar);
        sonuc.GerceklesenToplamKar = sonuc.Kalemler.Sum(x => x.GerceklesenKar);
        sonuc.KarSapmasi = sonuc.GerceklesenToplamKar - sonuc.PlanlananToplamKar;
        sonuc.KarSapmaOrani = sonuc.PlanlananToplamKar != 0
            ? sonuc.KarSapmasi / Math.Abs(sonuc.PlanlananToplamKar) * 100m
            : 0m;

        sonuc.TeklifDogrulukSkoru = sonuc.Kalemler.Any()
            ? Math.Round(sonuc.Kalemler.Average(x => x.TeklifDogrulukSkoru), 1)
            : HesaplaTeklifDogrulukSkoru(sonuc.MaliyetSapmaOrani, sonuc.GelirSapmaOrani, sonuc.KarSapmaOrani);
        sonuc.RiskSeviyesi = HesaplaRiskSeviyesi(sonuc.TeklifDogrulukSkoru, sonuc.MaliyetSapmaOrani, sonuc.KarSapmasi);

        return sonuc;
    }

    public async Task<IhaleOperasyonDashboardOzet> GetOperasyonDashboardOzetAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var kazanilanProjeler = await context.IhaleProjeleri
            .AsNoTracking()
            .Where(p => !p.IsDeleted && p.Durum == IhaleProjeDurum.Kazanildi)
            .OrderByDescending(p => p.UpdatedAt ?? p.CreatedAt)
            .ToListAsync();

        var sonuc = new IhaleOperasyonDashboardOzet
        {
            KazanilanProjeSayisi = kazanilanProjeler.Count
        };

        foreach (var proje in kazanilanProjeler)
        {
            var analiz = await GetProjeGerceklesenAnalizAsync(proje.Id);
            if (analiz.GecenAySayisi == 0 && analiz.ToplamGerceklesenSeferSayisi == 0)
                continue;

            sonuc.AnalizEdilenProjeSayisi++;
            sonuc.ToplamKarSapmasi += analiz.KarSapmasi;
            sonuc.ToplamRevizyonBedelFarki += analiz.ToplamRevizyonBedelFarki;
            sonuc.EnKotusuKarSapmasi = sonuc.AnalizEdilenProjeSayisi == 1
                ? analiz.KarSapmasi
                : Math.Min(sonuc.EnKotusuKarSapmasi, analiz.KarSapmasi);

            if (string.Equals(analiz.RiskSeviyesi, "Yuksek", StringComparison.OrdinalIgnoreCase))
                sonuc.RiskliProjeSayisi++;

            if (analiz.AktifSozlesmeRevizyonSayisi > 0)
                sonuc.RevizyonluProjeSayisi++;

            if (analiz.ToplamRevizyonSureFarkiAy > 0)
                sonuc.SureUzatimliProjeSayisi++;

            sonuc.RiskliProjeler.Add(new IhaleRiskliProjeOzet
            {
                ProjeId = proje.Id,
                ProjeKodu = proje.ProjeKodu,
                ProjeAdi = proje.ProjeAdi,
                TeklifDogrulukSkoru = analiz.TeklifDogrulukSkoru,
                KarSapmasi = analiz.KarSapmasi,
                MaliyetSapmaOrani = analiz.MaliyetSapmaOrani,
                AktifRevizyonSayisi = analiz.AktifSozlesmeRevizyonSayisi,
                ToplamRevizyonBedelFarki = analiz.ToplamRevizyonBedelFarki,
                RiskSeviyesi = analiz.RiskSeviyesi
            });
        }

        sonuc.OrtalamaTeklifDogrulukSkoru = sonuc.RiskliProjeler.Any()
            ? sonuc.RiskliProjeler.Average(x => x.TeklifDogrulukSkoru)
            : 0m;

        sonuc.RiskliProjeler = sonuc.RiskliProjeler
            .OrderBy(x => x.TeklifDogrulukSkoru)
            .ThenBy(x => x.KarSapmasi)
            .Take(5)
            .ToList();

        return sonuc;
    }

    private async Task<List<ServisCalisma>> GetKalemServisCalismalariAsync(ApplicationDbContext context, IhaleGuzergahKalem kalem, DateTime baslangic, DateTime bitis)
    {
        var query = context.ServisCalismalari
            .AsNoTracking()
            .Include(s => s.Guzergah)
            .Where(s => !s.IsDeleted && s.CalismaTarihi >= baslangic && s.CalismaTarihi <= bitis);

        if (kalem.GuzergahId.HasValue)
        {
            query = query.Where(s => s.GuzergahId == kalem.GuzergahId.Value);
        }
        else if (!string.IsNullOrWhiteSpace(kalem.HatAdi))
        {
            query = query.Where(s => s.Guzergah.GuzergahAdi == kalem.HatAdi);
        }

        if (kalem.AracId.HasValue)
            query = query.Where(s => s.AracId == kalem.AracId.Value || s.GuzergahId == kalem.GuzergahId);

        return await query.ToListAsync();
    }

    private async Task<decimal> GetGerceklesenAracMasrafiAsync(ApplicationDbContext context, IhaleGuzergahKalem kalem, DateTime baslangic, DateTime bitis)
    {
        var query = context.AracMasraflari
            .AsNoTracking()
            .Where(m => !m.IsDeleted && m.MasrafTarihi >= baslangic && m.MasrafTarihi <= bitis);

        if (kalem.GuzergahId.HasValue)
        {
            query = query.Where(m => m.GuzergahId == kalem.GuzergahId.Value);
        }
        else if (kalem.AracId.HasValue)
        {
            query = query.Where(m => m.AracId == kalem.AracId.Value);
        }
        else
        {
            return 0m;
        }

        return await query.SumAsync(m => (decimal?)m.Tutar) ?? 0m;
    }

    private async Task<decimal> GetGerceklesenSoforMaliyetiAsync(ApplicationDbContext context, IhaleGuzergahKalem kalem, DateTime baslangic, DateTime bitis, int gecenAySayisi)
    {
        if (!kalem.SoforId.HasValue)
            return 0m;

        var aktifAylar = GetMonthKeysBetween(baslangic, bitis).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var maaslar = await context.PersonelMaaslari
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.SoforId == kalem.SoforId.Value)
            .ToListAsync();

        var gerceklesenMaasToplami = maaslar
            .Where(x => aktifAylar.Contains(GetMonthKey(x.Yil, x.Ay)))
            .Sum(x => x.BrutMaas + x.SGKIsverenPayi + x.ToplamEklemeler);

        if (gerceklesenMaasToplami > 0)
            return gerceklesenMaasToplami;

        return kalem.SoforToplamMaliyet * gecenAySayisi;
    }

    private static int GetInclusiveMonthCount(DateTime baslangic, DateTime bitis)
    {
        if (bitis < baslangic)
            return 0;

        return ((bitis.Year - baslangic.Year) * 12) + bitis.Month - baslangic.Month + 1;
    }

    private static IEnumerable<string> GetMonthKeysBetween(DateTime baslangic, DateTime bitis)
    {
        if (bitis < baslangic)
            yield break;

        var cursor = new DateTime(baslangic.Year, baslangic.Month, 1);
        var end = new DateTime(bitis.Year, bitis.Month, 1);

        while (cursor <= end)
        {
            yield return GetMonthKey(cursor);
            cursor = cursor.AddMonths(1);
        }
    }

    private static string GetMonthKey(DateTime tarih) => $"{tarih.Year:D4}-{tarih.Month:D2}";

    private static string GetMonthKey(int yil, int ay) => $"{yil:D4}-{ay:D2}";

    private async Task<string> GetSonrakiRevizyonNoAsync(ApplicationDbContext context, int ihaleProjeId)
    {
        var mevcutNumaralar = await context.IhaleSozlesmeRevizyonlari
            .Where(x => x.IhaleProjeId == ihaleProjeId)
            .Select(x => x.RevizyonNo)
            .ToListAsync();

        var sonSira = mevcutNumaralar
            .Select(x => Regex.Match(x ?? string.Empty, @"(\d+)$"))
            .Where(m => m.Success)
            .Select(m => int.Parse(m.Groups[1].Value))
            .DefaultIfEmpty(0)
            .Max();

        return $"REV-{sonSira + 1:D2}";
    }

    private static string GetSozlesmeRevizyonTipiMetni(IhaleSozlesmeRevizyonTipi tip)
    {
        return tip switch
        {
            IhaleSozlesmeRevizyonTipi.SozlesmeRevizyonu => "Sözleşme Revizyonu",
            IhaleSozlesmeRevizyonTipi.EkProtokol => "Ek Protokol",
            IhaleSozlesmeRevizyonTipi.FiyatFarki => "Fiyat Farkı",
            IhaleSozlesmeRevizyonTipi.SureUzatimi => "Süre Uzatımı",
            _ => "-"
        };
    }

    private static decimal HesaplaTeklifDogrulukSkoru(decimal maliyetSapmaOrani, decimal gelirSapmaOrani, decimal karSapmaOrani)
    {
        var maliyetSkoru = Math.Max(0m, 100m - Math.Abs(maliyetSapmaOrani));
        var gelirSkoru = Math.Max(0m, 100m - Math.Abs(gelirSapmaOrani));
        var karSkoru = Math.Max(0m, 100m - Math.Abs(karSapmaOrani));

        return Math.Round((maliyetSkoru * 0.45m) + (gelirSkoru * 0.20m) + (karSkoru * 0.35m), 1);
    }

    private static string HesaplaRiskSeviyesi(decimal teklifDogrulukSkoru, decimal maliyetSapmaOrani, decimal karSapmasi)
    {
        if (teklifDogrulukSkoru < 60m || karSapmasi < 0 || maliyetSapmaOrani > 15m)
            return "Yuksek";

        if (teklifDogrulukSkoru < 80m || maliyetSapmaOrani > 7.5m)
            return "Orta";

        return "Dusuk";
    }

    // ===== AI Tahmin =====

    public async Task<IhaleMaliyetTahminSonuc> AIAracMasrafTahminAsync(IhaleMaliyetTahminIstek istek)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var sonuc = new IhaleMaliyetTahminSonuc();

        try
        {
            // Gerçek verileri topla
            var gercekVeriler = await ToplamGercekMasrafVerisi(context, istek);

            var prompt = $$"""
                Personel servis aracı için aylık masraf tahmini yap.

                ARAÇ BİLGİLERİ:
                - Model: {{istek.AracModel}}
                - Model Yılı: {{istek.AracModelYili}}
                - Koltuk Sayısı: {{istek.KoltukSayisi}}
                - Günlük Mesafe: {{istek.MesafeKm * istek.GunlukSeferSayisi}} km ({{istek.MesafeKm}} km x {{istek.GunlukSeferSayisi}} sefer)
                - Aylık Çalışma Günü: {{istek.AylikCalismGunu}}
                - Aylık Toplam Km: {{istek.MesafeKm * istek.GunlukSeferSayisi * istek.AylikCalismGunu}} km
                - Sahiplik: {{istek.SahiplikDurumu}}
                - Yakıt Tüketimi: {{istek.YakitTuketimi}} lt/100km
                - Sözleşme Süresi: {{istek.SozlesmeSuresiAy}} ay

                {{gercekVeriler}}

                Yanıtını SADECE aşağıdaki JSON formatında ver, başka metin ekleme:
                {
                    "aylikBakim": 0,
                    "aylikLastik": 0,
                    "aylikSigorta": 0,
                    "aylikKasko": 0,
                    "aylikMuayene": 0,
                    "aylikYedekParca": 0,
                    "aylikDigerMasraf": 0,
                    "aciklama": "kısa açıklama"
                }

                Türk Lirası cinsinden güncel piyasa fiyatlarına göre hesapla.
                Aylık muayene = yıllık muayene ücreti / 12 şeklinde hesapla.
                """;

            var sistemPrompt = "Sen bir filo yönetimi uzmanısın. Personel servis araçlarının aylık masraflarını tahmin ediyorsun. Gerçekçi ve güncel Türkiye piyasa fiyatlarını kullan.";

            var aiYanit = await _ollamaService.AnalizYapAsync(prompt, sistemPrompt);

            // JSON parse
            var jsonMatch = Regex.Match(aiYanit, @"\{[^{}]*\}", RegexOptions.Singleline);
            if (jsonMatch.Success)
            {
                var json = JsonDocument.Parse(jsonMatch.Value);
                var root = json.RootElement;

                sonuc.TahminiAylikBakim = GetDecimalProperty(root, "aylikBakim");
                sonuc.TahminiAylikLastik = GetDecimalProperty(root, "aylikLastik");
                sonuc.TahminiAylikSigorta = GetDecimalProperty(root, "aylikSigorta");
                sonuc.TahminiAylikKasko = GetDecimalProperty(root, "aylikKasko");
                sonuc.TahminiAylikMuayene = GetDecimalProperty(root, "aylikMuayene");
                sonuc.TahminiAylikYedekParca = GetDecimalProperty(root, "aylikYedekParca");
                sonuc.TahminiAylikDigerMasraf = GetDecimalProperty(root, "aylikDigerMasraf");

                if (root.TryGetProperty("aciklama", out var aciklama))
                    sonuc.AIAciklama = aciklama.GetString();

                sonuc.Basarili = true;
            }
            else
            {
                sonuc.AIAciklama = aiYanit;
                sonuc.Basarili = false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI araç masraf tahmini hatası");
            sonuc.AIAciklama = $"AI tahmin hatası: {ex.Message}";
            sonuc.Basarili = false;
        }

        return sonuc;
    }

    public async Task<IhaleSoforMaasTahmin> AISoforMaasTahminAsync(string aracTipi, decimal mesafeKm, int seferSayisi, decimal enflasyonOrani, int sozlesmeSuresiAy)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var sonuc = new IhaleSoforMaasTahmin();

        try
        {
            // Mevcut şoför maaş ortalaması
            var ortMaas = await GetGecmisSoforMaasOrtalamaAsync();
            var minUcret = 22104.67m; // 2025 asgari ücret brüt

            var prompt = $$"""
                Personel servis şoförü için maaş tahmini yap.

                BİLGİLER:
                - Araç Tipi: {{aracTipi}}
                - Günlük Mesafe: {{mesafeKm * seferSayisi}} km
                - Günlük Sefer Sayısı: {{seferSayisi}}
                - Firmadaki mevcut şoför brüt maaş ortalaması: {{ortMaas:N0}} TL
                - 2025 asgari ücret brüt: {{minUcret:N0}} TL
                - Enflasyon oranı (yıllık): %{{enflasyonOrani}}
                - Sözleşme süresi: {{sozlesmeSuresiAy}} ay

                Yanıtını SADECE aşağıdaki JSON formatında ver:
                {
                    "brutMaas": 0,
                    "netMaas": 0,
                    "sgkIsverenPay": 0,
                    "toplamMaliyet": 0,
                    "enflasyonluBrutMaas": 0,
                    "aciklama": "kısa açıklama"
                }

                brutMaas: Güncel piyasa brüt maaş
                netMaas: Tahmini net maaş
                sgkIsverenPay: Brüt maaşın yaklaşık %22.5'i
                toplamMaliyet: brutMaas + sgkIsverenPay
                enflasyonluBrutMaas: {{sozlesmeSuresiAy}} ay sonraki tahmini brüt maaş (enflasyon dahil)
                """;

            var sistemPrompt = "Sen bir İK ve maaş uzmanısın. Türkiye'de personel servis şoförü maaşlarını güncel piyasa koşullarına göre tahmin ediyorsun.";

            var aiYanit = await _ollamaService.AnalizYapAsync(prompt, sistemPrompt);

            var jsonMatch = Regex.Match(aiYanit, @"\{[^{}]*\}", RegexOptions.Singleline);
            if (jsonMatch.Success)
            {
                var json = JsonDocument.Parse(jsonMatch.Value);
                var root = json.RootElement;

                sonuc.TahminiBrutMaas = GetDecimalProperty(root, "brutMaas");
                sonuc.TahminiNetMaas = GetDecimalProperty(root, "netMaas");
                sonuc.TahminiSGKIsverenPay = GetDecimalProperty(root, "sgkIsverenPay");
                sonuc.TahminiToplamMaliyet = GetDecimalProperty(root, "toplamMaliyet");
                sonuc.EnflasyonluBrutMaas = GetDecimalProperty(root, "enflasyonluBrutMaas");

                if (root.TryGetProperty("aciklama", out var aciklama))
                    sonuc.AIAciklama = aciklama.GetString();

                sonuc.Basarili = true;
            }
            else
            {
                sonuc.AIAciklama = aiYanit;
                sonuc.Basarili = false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI şoför maaş tahmini hatası");
            sonuc.AIAciklama = $"AI tahmin hatası: {ex.Message}";
            sonuc.Basarili = false;
        }

        return sonuc;
    }

    public async Task<string> AIProjeAnalizAsync(int projeId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var ozet = await GetProjeOzetAsync(projeId);

        var kalemDetay = string.Join("\n", ozet.KalemOzetleri.Select(k =>
            $"  - {k.HatAdi}: {k.MesafeKm}km, {k.SahiplikDurumu}, Aylık Maliyet: {k.AylikMaliyet:N0}₺, Teklif: {k.AylikTeklifFiyati:N0}₺, Kâr Marjı: %{k.KarMarji:N1}"));

        var prompt = $"""
            İhale projesini analiz et ve önerilerde bulun.

            PROJE: {ozet.ProjeAdi}
            Müşteri: {ozet.MusteriFirma ?? "Belirtilmemiş"}
            Süre: {ozet.SozlesmeSuresiAy} ay
            Güzergah Sayısı: {ozet.GuzergahSayisi}

            MALİYET ÖZETİ (AYLIK):
            - Yakıt: {ozet.ToplamAylikYakit:N0} ₺
            - Araç Masraf: {ozet.ToplamAylikAracMasraf:N0} ₺
            - Şoför Maliyet: {ozet.ToplamAylikSoforMaliyet:N0} ₺
            - Kira/Komisyon: {ozet.ToplamAylikKiraKomisyon:N0} ₺
            - Amortisman: {ozet.ToplamAylikAmortisman:N0} ₺
            - TOPLAM MALİYET: {ozet.ToplamAylikMaliyet:N0} ₺
            - TOPLAM KÂR: {ozet.ToplamAylikKar:N0} ₺
            - TOPLAM TEKLİF: {ozet.ToplamAylikTeklifFiyati:N0} ₺

            HAT DETAYLARI:
            {kalemDetay}

            PROJE TOPLAMI:
            - Toplam Maliyet: {ozet.ToplamProjeMaliyeti:N0} ₺
            - Toplam Kâr: {ozet.ToplamProjeKar:N0} ₺
            - Toplam Teklif: {ozet.ToplamProjeTeklif:N0} ₺

            Lütfen şunları analiz et:
            1. Kâr marjı yeterliliği ve riskleri
            2. Yakıt maliyeti oranı ve optimizasyon önerileri
            3. Enflasyon riski değerlendirmesi
            4. Rekabetçi fiyat analizi
            5. Risk faktörleri ve önlemler
            6. İhaleyi kazanma şansını artırmak için öneriler
            """;

        var sistemPrompt = "Sen bir personel taşımacılığı ve ihale uzmanısın. Türkiye'deki personel servis ihaleleri hakkında derin bilgiye sahipsin. Kapsamlı ve stratejik analiz yap.";

        return await _ollamaService.AnalizYapAsync(prompt, sistemPrompt);
    }

    // ===== Veri Yardımcıları =====

    public async Task<decimal> GetGecmisMasrafOrtalamaAsync(int? aracId, MasrafKategori kategori, int aySayisi = 12)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var baslangic = DateTime.Today.AddMonths(-aySayisi);
        var query = context.AracMasraflari
            .Include(m => m.MasrafKalemi)
            .Where(m => !m.IsDeleted && m.MasrafTarihi >= baslangic && m.MasrafKalemi.Kategori == kategori);

        if (aracId.HasValue)
            query = query.Where(m => m.AracId == aracId.Value);

        var toplam = await query.SumAsync(m => (decimal?)m.Tutar) ?? 0;
        return aySayisi > 0 ? toplam / aySayisi : 0;
    }

    public async Task<decimal> GetGecmisSoforMaasOrtalamaAsync(int aySayisi = 6)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var soforlar = await context.Soforler
            .Where(s => !s.IsDeleted && s.Aktif && s.Gorev == PersonelGorev.Sofor && s.BrutMaas > 0)
            .ToListAsync();

        if (soforlar.Count == 0) return 0;
        return soforlar.Average(s => s.BrutMaas);
    }

    // ===== Private Yardımcılar =====

    private async Task<string> ToplamGercekMasrafVerisi(ApplicationDbContext context, IhaleMaliyetTahminIstek istek)
    {
        var sb = new System.Text.StringBuilder();

        if (istek.AracModelYili > 0)
        {
            // Son 12 aylık gerçek masraf ortalamaları
            var bakimOrt = await GetGecmisMasrafOrtalamaAsync(null, MasrafKategori.Bakim);
            var lastikOrt = await GetGecmisMasrafOrtalamaAsync(null, MasrafKategori.Lastik);
            var sigortaOrt = await GetGecmisMasrafOrtalamaAsync(null, MasrafKategori.Sigorta);
            var yedekParcaOrt = await GetGecmisMasrafOrtalamaAsync(null, MasrafKategori.YedekParca);

            sb.AppendLine("\nFİRMADAKİ GERÇEKLEŞMİŞ MASRAF ORTALAMALARI (son 12 ay, araç başı):");
            if (bakimOrt > 0) sb.AppendLine($"- Bakım ortalaması: {bakimOrt:N0} TL/ay");
            if (lastikOrt > 0) sb.AppendLine($"- Lastik ortalaması: {lastikOrt:N0} TL/ay");
            if (sigortaOrt > 0) sb.AppendLine($"- Sigorta ortalaması: {sigortaOrt:N0} TL/ay");
            if (yedekParcaOrt > 0) sb.AppendLine($"- Yedek parça ortalaması: {yedekParcaOrt:N0} TL/ay");

            if (bakimOrt == 0 && lastikOrt == 0 && sigortaOrt == 0)
                sb.AppendLine("- Henüz gerçekleşmiş masraf verisi yok, piyasa değerlerini kullan.");
        }

        return sb.ToString();
    }

    private static decimal GetDecimalProperty(JsonElement root, string property)
    {
        if (root.TryGetProperty(property, out var val))
        {
            if (val.ValueKind == JsonValueKind.Number)
                return val.GetDecimal();
            if (val.ValueKind == JsonValueKind.String && decimal.TryParse(val.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
                return d;
        }
        return 0;
    }

    // ===== Örnek Veri Oluşturma =====

    public async Task<IhaleProje> OrnekProjeOlusturAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        // Rastgele değerler için
        var random = new Random();
        var simdi = DateTime.Now;

        // Önce örnek güzergah oluştur (veritabanında yoksa)
        var ornekGuzergah = await context.Guzergahlar
            .FirstOrDefaultAsync(g => g.GuzergahAdi == "Merkez - Organize Sanayi (ÖRNEK)" && !g.IsDeleted);

        if (ornekGuzergah == null)
        {
            ornekGuzergah = new Guzergah
            {
                GuzergahAdi = "Merkez - Organize Sanayi (ÖRNEK)",
                BaslangicNoktasi = "Şehir Merkezi (Otogar Mevkii)",
                BitisNoktasi = "ABC Fabrikası - OSB",
                Mesafe = 45,
                TahminiSure = 55,
                SeferTipi = SeferTipi.SabahAksam,
                PersonelSayisi = 35,
                Aktif = true,
                FirmaId = 1
            };
            context.Guzergahlar.Add(ornekGuzergah);
            await context.SaveChangesAsync();
        }

        // Örnek şoför oluştur (veritabanında yoksa)
        var ornekSofor = await context.Soforler
            .FirstOrDefaultAsync(s => s.Ad == "Örnek" && s.Soyad == "Şoför" && !s.IsDeleted);

        if (ornekSofor == null)
        {
            ornekSofor = new Sofor
            {
                SoforKodu = "ORNEK001",
                Ad = "Örnek",
                Soyad = "Şoför",
                TcKimlikNo = "12345678901",
                Telefon = "0532 000 0001",
                Aktif = true,
                Gorev = PersonelGorev.Sofor,
                BrutMaas = 32000,
                NetMaas = 26500,
                EhliyetNo = "TR123456"
            };
            context.Soforler.Add(ornekSofor);
            await context.SaveChangesAsync();
        }

        // Örnek araç oluştur (veritabanında yoksa)
        var ornekArac = await context.Araclar
            .OrderBy(a => a.Id)
            .FirstOrDefaultAsync(a => a.SaseNo == "ORNEK2022001" && !a.IsDeleted);

        if (ornekArac == null)
        {
            ornekArac = new Arac
            {
                SaseNo = "ORNEK2022001",
                AktifPlaka = "34 ORNEK 001",
                Marka = "Mercedes",
                Model = "Sprinter 516 CDI",
                ModelYili = 2022,
                KoltukSayisi = 27,
                SahiplikTipi = AracSahiplikTipi.Ozmal,
                Aktif = true
            };
            context.Araclar.Add(ornekArac);
            await context.SaveChangesAsync();
        }

        // Örnek proje oluştur
        var proje = new IhaleProje
        {
            ProjeKodu = await GenerateProjeKoduAsync(),
            ProjeAdi = "Örnek Personel Servis İhalesi - ABC Fabrikası",
            Aciklama = "Test amaçlı oluşturulmuş örnek ihale projesi. Tüm maliyet kalemleri ve hesaplamalar otomatik doldurulmuştur.",
            BaslangicTarihi = new DateTime(simdi.Year, simdi.Month, 1),
            BitisTarihi = new DateTime(simdi.Year, simdi.Month, 1).AddMonths(12),
            SozlesmeSuresiAy = 12,
            Durum = IhaleProjeDurum.Hazirlaniyor,
            EnflasyonOrani = 30, // Yıllık %30 enflasyon
            YakitZamOrani = 35, // Yıllık %35 yakıt zamları
            AylikCalismGunu = 22,
            GunlukCalismaSaati = 8,
            Notlar = "Bu örnek proje, ihale hazırlık modülünün test edilmesi için otomatik oluşturulmuştur. Puantaj verileri de dahildir."
        };

        context.IhaleProjeleri.Add(proje);
        await context.SaveChangesAsync();

        // Örnek güzergah kalemi oluştur
        var kalem = new IhaleGuzergahKalem
        {
            IhaleProjeId = proje.Id,
            GuzergahId = ornekGuzergah.Id,
            HatAdi = ornekGuzergah.GuzergahAdi,
            BaslangicNoktasi = ornekGuzergah.BaslangicNoktasi,
            BitisNoktasi = ornekGuzergah.BitisNoktasi,
            MesafeKm = ornekGuzergah.Mesafe ?? 45,
            TahminiSureDakika = ornekGuzergah.TahminiSure ?? 55,
            SeferTipi = SeferTipi.SabahAksam,
            GunlukSeferSayisi = 2, // Sabah gidiş + Akşam dönüş
            AylikSeferGunu = 22,
            PersonelSayisi = 35, // 35 personel taşınacak

            // Araç bilgileri
            AracId = ornekArac.Id,
            SahiplikDurumu = AracSahiplikKalem.Ozmal,
            AracModelBilgi = $"{ornekArac.ModelYili} {ornekArac.Marka} {ornekArac.Model}",
            AracKoltukSayisi = ornekArac.KoltukSayisi,
            YakitTuketimi = 18,
            YakitFiyati = 45.50m,

            // Araç masrafları (aylık)
            AylikBakimMasrafi = 3500,
            AylikLastikMasrafi = 1800,
            AylikSigortaMasrafi = 2500,
            AylikKaskoMasrafi = 4000,
            AylikMuayeneMasrafi = 250,
            AylikYedekParcaMasrafi = 1500,
            AylikDigerMasraf = 1000,

            // Şoför bilgileri
            SoforId = ornekSofor.Id,
            SoforBrutMaas = ornekSofor.BrutMaas,
            SoforNetMaas = ornekSofor.NetMaas,
            SoforSGKIsverenPay = ornekSofor.BrutMaas * 0.225m,
            SoforToplamMaliyet = ornekSofor.BrutMaas * 1.225m,

            // Amortisman
            AracDegeri = 3500000,
            AmortismanYili = 5,

            // Kâr marjı
            KarMarjiOrani = 18,

            AITahminiKullanildi = false,
            AITahminDetay = "Manuel örnek veri girişi"
        };

        await HesaplaKalemMaliyetAsync(kalem, proje);

        context.IhaleGuzergahKalemleri.Add(kalem);
        await context.SaveChangesAsync();

        // Puantaj kaydı oluştur (mevcut ay için)
        var puantaj = new PuantajKayit
        {
            Yil = simdi.Year,
            Ay = simdi.Month,
            Bolge = "Merkez",
            SiraNo = 1,
            KurumAdi = "ABC Fabrikası (Örnek)",
            GuzergahId = ornekGuzergah.Id,
            GuzergahAdi = ornekGuzergah.GuzergahAdi,
            Yon = PuantajYon.SabahAksam,
            AracId = ornekArac.Id,
            Plaka = ornekArac.AktifPlaka ?? "34 ORNEK 001",
            SoforId = ornekSofor.Id,
            SoforAdi = $"{ornekSofor.Ad} {ornekSofor.Soyad}",
            SoforOdemeTipi = SoforOdemeTipi.Ozmal,
            Gun = 22, // 22 gün çalışma
            SeferSayisi = 2, // Sabah + Akşam

            // Gelir bilgileri (örnek)
            BirimGelir = kalem.SeferBasiTeklifFiyati,
            GelirKdvOrani = 20,

            // Gider bilgileri (örnek)
            BirimGider = kalem.SeferBasiMaliyet,

            // Ayın iş günlerini doldur (haftaiçi günler için 2 sefer)
            Gun01 = HaftaIciMi(simdi.Year, simdi.Month, 1) ? 2 : 0,
            Gun02 = HaftaIciMi(simdi.Year, simdi.Month, 2) ? 2 : 0,
            Gun03 = HaftaIciMi(simdi.Year, simdi.Month, 3) ? 2 : 0,
            Gun04 = HaftaIciMi(simdi.Year, simdi.Month, 4) ? 2 : 0,
            Gun05 = HaftaIciMi(simdi.Year, simdi.Month, 5) ? 2 : 0,
            Gun06 = HaftaIciMi(simdi.Year, simdi.Month, 6) ? 2 : 0,
            Gun07 = HaftaIciMi(simdi.Year, simdi.Month, 7) ? 2 : 0,
            Gun08 = HaftaIciMi(simdi.Year, simdi.Month, 8) ? 2 : 0,
            Gun09 = HaftaIciMi(simdi.Year, simdi.Month, 9) ? 2 : 0,
            Gun10 = HaftaIciMi(simdi.Year, simdi.Month, 10) ? 2 : 0,
            Gun11 = HaftaIciMi(simdi.Year, simdi.Month, 11) ? 2 : 0,
            Gun12 = HaftaIciMi(simdi.Year, simdi.Month, 12) ? 2 : 0,
            Gun13 = HaftaIciMi(simdi.Year, simdi.Month, 13) ? 2 : 0,
            Gun14 = HaftaIciMi(simdi.Year, simdi.Month, 14) ? 2 : 0,
            Gun15 = HaftaIciMi(simdi.Year, simdi.Month, 15) ? 2 : 0,
            Gun16 = HaftaIciMi(simdi.Year, simdi.Month, 16) ? 2 : 0,
            Gun17 = HaftaIciMi(simdi.Year, simdi.Month, 17) ? 2 : 0,
            Gun18 = HaftaIciMi(simdi.Year, simdi.Month, 18) ? 2 : 0,
            Gun19 = HaftaIciMi(simdi.Year, simdi.Month, 19) ? 2 : 0,
            Gun20 = HaftaIciMi(simdi.Year, simdi.Month, 20) ? 2 : 0,
            Gun21 = HaftaIciMi(simdi.Year, simdi.Month, 21) ? 2 : 0,
            Gun22 = HaftaIciMi(simdi.Year, simdi.Month, 22) ? 2 : 0,
            Gun23 = HaftaIciMi(simdi.Year, simdi.Month, 23) ? 2 : 0,
            Gun24 = HaftaIciMi(simdi.Year, simdi.Month, 24) ? 2 : 0,
            Gun25 = HaftaIciMi(simdi.Year, simdi.Month, 25) ? 2 : 0,
            Gun26 = HaftaIciMi(simdi.Year, simdi.Month, 26) ? 2 : 0,
            Gun27 = HaftaIciMi(simdi.Year, simdi.Month, 27) ? 2 : 0,
            Gun28 = HaftaIciMi(simdi.Year, simdi.Month, 28) ? 2 : 0,
            Gun29 = DateTime.DaysInMonth(simdi.Year, simdi.Month) >= 29 && HaftaIciMi(simdi.Year, simdi.Month, 29) ? 2 : 0,
            Gun30 = DateTime.DaysInMonth(simdi.Year, simdi.Month) >= 30 && HaftaIciMi(simdi.Year, simdi.Month, 30) ? 2 : 0,
            Gun31 = DateTime.DaysInMonth(simdi.Year, simdi.Month) >= 31 && HaftaIciMi(simdi.Year, simdi.Month, 31) ? 2 : 0
        };

        // Toplam gelir ve gider hesapla
        var toplamSefer = puantaj.SeferGunuToplami;
        puantaj.ToplamGelir = puantaj.BirimGelir * toplamSefer;
        puantaj.GelirKdvTutari = puantaj.ToplamGelir * puantaj.GelirKdvOrani / 100;
        puantaj.GelirToplam = puantaj.ToplamGelir + puantaj.GelirKdvTutari;
        puantaj.Alinacak = puantaj.GelirToplam;

        puantaj.ToplamGider = puantaj.BirimGider * toplamSefer;
        puantaj.GiderKdv20Tutari = puantaj.ToplamGider * 20 / 100;
        puantaj.Odenecek = puantaj.ToplamGider + puantaj.GiderKdv20Tutari;

        context.PuantajKayitlar.Add(puantaj);
        await context.SaveChangesAsync();

        // Güncel proje ile döndür
        return await GetProjeByIdAsync(proje.Id) ?? proje;
    }

    private static bool HaftaIciMi(int yil, int ay, int gun)
    {
        try
        {
            var tarih = new DateTime(yil, ay, gun);
            return tarih.DayOfWeek != DayOfWeek.Saturday && tarih.DayOfWeek != DayOfWeek.Sunday;
        }
        catch
        {
            return false;
        }
    }
}



