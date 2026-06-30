using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Web.Services;

public class SatisService : ISatisService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private static readonly string[] AyAdlari = { "", "Ocak", "Subat", "Mart", "Nisan", "Mayis", "Haziran", 
                                                   "Temmuz", "Agustos", "Eylul", "Ekim", "Kasim", "Aralik" };

    public SatisService(IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    #region Satis Personeli

    public async Task<List<SatisPersoneli>> GetSatisPersonelListesiAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.SatisPersonelleri
            .Include(p => p.Ilanlar.Where(i => i.IlanDurum == IlanDurum.Aktif))
            .OrderBy(p => p.AdSoyad)
            .ToListAsync();
    }

    public async Task<SatisPersoneli?> GetSatisPersonelByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.SatisPersonelleri
            .Include(p => p.Ilanlar)
            .Include(p => p.Satislar)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<SatisPersoneli> CreateSatisPersonelAsync(SatisPersoneli personel)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        personel.CreatedAt = DateTime.UtcNow;
        context.SatisPersonelleri.Add(personel);
        await context.SaveChangesAsync();
        return personel;
    }

    public async Task<SatisPersoneli> UpdateSatisPersonelAsync(SatisPersoneli personel)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var existing = await context.SatisPersonelleri.FindAsync(personel.Id);
        if (existing == null) throw new Exception("Personel bulunamadi");

        existing.PersonelKodu = personel.PersonelKodu;
        existing.AdSoyad = personel.AdSoyad;
        existing.Telefon = personel.Telefon;
        existing.Email = personel.Email;
        existing.KomisyonOrani = personel.KomisyonOrani;
        existing.SabitKomisyon = personel.SabitKomisyon;
        existing.AylikSatisHedefi = personel.AylikSatisHedefi;
        existing.AylikAracHedefi = personel.AylikAracHedefi;
        existing.Aktif = personel.Aktif;
        existing.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
        return existing;
    }

    public async Task DeleteSatisPersonelAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var personel = await context.SatisPersonelleri.FindAsync(id);
        if (personel == null) return;

        personel.IsDeleted = true;
        personel.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
    }

    public async Task<SatisPersonelPerformans> GetPersonelPerformansAsync(int personelId, int yil, int? ay = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var personel = await context.SatisPersonelleri.FindAsync(personelId);
        if (personel == null) throw new Exception("Personel bulunamadi");

        var satisQuery = context.AracSatislari
            .Where(s => s.SatisPersoneliId == personelId && s.SatisTarihi.Year == yil);

        if (ay.HasValue)
            satisQuery = satisQuery.Where(s => s.SatisTarihi.Month == ay.Value);

        var satislar = await satisQuery.ToListAsync();
        var ilanlar = await context.AracIlanlari.Where(i => i.SatisPersoneliId == personelId).ToListAsync();

        var performans = new SatisPersonelPerformans
        {
            PersonelId = personelId,
            PersonelAdi = personel.AdSoyad,
            ToplamIlan = ilanlar.Count,
            SatilanArac = satislar.Count,
            AktifIlan = ilanlar.Count(i => i.IlanDurum == IlanDurum.Aktif),
            ToplamSatisTutari = satislar.Sum(s => s.SatisFiyati),
            ToplamKomisyon = satislar.Sum(s => s.KomisyonTutari)
        };

        if (personel.AylikSatisHedefi > 0)
        {
            var hedefToplam = ay.HasValue ? personel.AylikSatisHedefi : personel.AylikSatisHedefi * 12;
            performans.HedefGerceklesme = Math.Round(performans.ToplamSatisTutari / hedefToplam * 100, 1);
        }

        // Aylik veriler
        for (int m = 1; m <= 12; m++)
        {
            var aySatislari = satislar.Where(s => s.SatisTarihi.Month == m).ToList();
            performans.AylikVeriler.Add(new AylikSatisData
            {
                Ay = m,
                AyAdi = AyAdlari[m],
                SatisSayisi = aySatislari.Count,
                SatisTutari = aySatislari.Sum(s => s.SatisFiyati),
                Komisyon = aySatislari.Sum(s => s.KomisyonTutari)
            });
        }

        return performans;
    }

    #endregion

    #region Arac Ilanlari

    public async Task<List<AracIlan>> GetAracIlanListesiAsync(IlanDurum? durum = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.AracIlanlari
            .Include(i => i.SatisPersoneli)
            .Include(i => i.SahipCari)
            .AsQueryable();

        if (durum.HasValue)
            query = query.Where(i => i.IlanDurum == durum.Value);

        return await query.OrderByDescending(i => i.IlanTarihi).ToListAsync();
    }

    public async Task<AracIlan?> GetAracIlanByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.AracIlanlari
            .Include(i => i.SatisPersoneli)
            .Include(i => i.SahipCari)
            .Include(i => i.PiyasaIlanlari)
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task<AracIlan> CreateAracIlanAsync(AracIlan ilan)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        ilan.IlanTarihi = DateTime.Today;
        ilan.CreatedAt = DateTime.UtcNow;
        context.AracIlanlari.Add(ilan);
        await context.SaveChangesAsync();
        return ilan;
    }

    public async Task<AracIlan> UpdateAracIlanAsync(AracIlan ilan)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var existing = await context.AracIlanlari.FindAsync(ilan.Id);
        if (existing == null) throw new Exception("Ilan bulunamadi");

        existing.Plaka = ilan.Plaka;
        existing.Marka = ilan.Marka;
        existing.Model = ilan.Model;
        existing.ModelYili = ilan.ModelYili;
        existing.Versiyon = ilan.Versiyon;
        existing.Kilometre = ilan.Kilometre;
        existing.YakitTuru = ilan.YakitTuru;
        existing.VitesTuru = ilan.VitesTuru;
        existing.KasaTipi = ilan.KasaTipi;
        existing.Renk = ilan.Renk;
        existing.Durum = ilan.Durum;
        existing.Boyali = ilan.Boyali;
        existing.BoyaliParcaSayisi = ilan.BoyaliParcaSayisi;
        existing.BoyaliParcalar = ilan.BoyaliParcalar;
        existing.DegisenVar = ilan.DegisenVar;
        existing.DegisenParcaSayisi = ilan.DegisenParcaSayisi;
        existing.DegisenParcalar = ilan.DegisenParcalar;
        existing.HasarKaydi = ilan.HasarKaydi;
        existing.HasarAciklama = ilan.HasarAciklama;
        existing.TramerKaydi = ilan.TramerKaydi;
        existing.TramerTutari = ilan.TramerTutari;
        existing.AlisFiyati = ilan.AlisFiyati;
        existing.SatisFiyati = ilan.SatisFiyati;
        existing.KaskoDegeri = ilan.KaskoDegeri;
        existing.IlanDurum = ilan.IlanDurum;
        existing.Aciklama = ilan.Aciklama;
        existing.Notlar = ilan.Notlar;
        existing.SatisPersoneliId = ilan.SatisPersoneliId;
        existing.SahipCariId = ilan.SahipCariId;
        existing.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
        return existing;
    }

    public async Task DeleteAracIlanAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var ilan = await context.AracIlanlari.FindAsync(id);
        if (ilan == null) return;

        ilan.IsDeleted = true;
        ilan.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
    }

    public async Task<AracIlan> IlanSatAsync(int ilanId, AracSatis satisInfo)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var ilan = await context.AracIlanlari.FindAsync(ilanId);
        if (ilan == null) throw new Exception("Ilan bulunamadi");

        // Satis kaydini olustur
        satisInfo.AracIlanId = ilanId;
        satisInfo.SatisTarihi = DateTime.Today;
        satisInfo.CreatedAt = DateTime.UtcNow;

        // Komisyon hesapla
        if (satisInfo.SatisPersoneliId.HasValue)
        {
            var personel = await context.SatisPersonelleri.FindAsync(satisInfo.SatisPersoneliId.Value);
            if (personel != null)
            {
                satisInfo.KomisyonTutari = personel.SabitKomisyon + (satisInfo.SatisFiyati * personel.KomisyonOrani / 100);
            }
        }

        context.AracSatislari.Add(satisInfo);

        // Ilani guncelle
        ilan.IlanDurum = IlanDurum.Satildi;
        ilan.SatisTarihi = DateTime.Today;
        ilan.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
        return ilan;
    }

    #endregion

    #region Piyasa Karsilastirma

    public async Task<List<PiyasaIlan>> GetPiyasaIlanlariAsync(int aracIlanId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.PiyasaIlanlari
            .Where(p => p.AracIlanId == aracIlanId)
            .OrderByDescending(p => p.TaramaTarihi)
            .ToListAsync();
    }

    public async Task<PiyasaIlan> AddPiyasaIlanAsync(PiyasaIlan piyasaIlan)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        piyasaIlan.TaramaTarihi = DateTime.Now;
        piyasaIlan.CreatedAt = DateTime.UtcNow;
        context.PiyasaIlanlari.Add(piyasaIlan);
        await context.SaveChangesAsync();

        // Piyasa degerlerini guncelle
        await UpdatePiyasaDegerleriAsync(context, piyasaIlan.AracIlanId);

        return piyasaIlan;
    }

    public async Task DeletePiyasaIlanAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var piyasaIlan = await context.PiyasaIlanlari.FindAsync(id);
        if (piyasaIlan == null) return;

        var aracIlanId = piyasaIlan.AracIlanId;
        piyasaIlan.IsDeleted = true;
        await context.SaveChangesAsync();

        await UpdatePiyasaDegerleriAsync(context, aracIlanId);
    }

    private async Task UpdatePiyasaDegerleriAsync(ApplicationDbContext context, int aracIlanId)
    {
        var piyasaIlanlari = await context.PiyasaIlanlari
            .Where(p => p.AracIlanId == aracIlanId && !p.IsDeleted)
            .ToListAsync();

        if (!piyasaIlanlari.Any()) return;

        var ilan = await context.AracIlanlari.FindAsync(aracIlanId);
        if (ilan == null) return;

        ilan.PiyasaDegeriMin = piyasaIlanlari.Min(p => p.Fiyat);
        ilan.PiyasaDegeriMax = piyasaIlanlari.Max(p => p.Fiyat);
        ilan.PiyasaDegeriOrtalama = piyasaIlanlari.Average(p => p.Fiyat);
        ilan.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
    }

    public async Task<PiyasaDegerlendirme> GetPiyasaDegerlendirmeAsync(int aracIlanId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var ilan = await context.AracIlanlari.FindAsync(aracIlanId);
        if (ilan == null) throw new Exception("Ilan bulunamadi");

        var piyasaIlanlari = await context.PiyasaIlanlari
            .Where(p => p.AracIlanId == aracIlanId && !p.IsDeleted)
            .ToListAsync();

        var degerlendirme = new PiyasaDegerlendirme
        {
            AracIlanId = aracIlanId,
            KarsilastirmaAdet = piyasaIlanlari.Count,
            BizimFiyat = ilan.SatisFiyati
        };

        if (piyasaIlanlari.Any())
        {
            var fiyatlar = piyasaIlanlari.Select(p => p.Fiyat).OrderBy(f => f).ToList();
            degerlendirme.MinFiyat = fiyatlar.First();
            degerlendirme.MaxFiyat = fiyatlar.Last();
            degerlendirme.OrtalamaFiyat = fiyatlar.Average();
            degerlendirme.MedianFiyat = fiyatlar[fiyatlar.Count / 2];

            degerlendirme.FiyatFarki = ilan.SatisFiyati - degerlendirme.OrtalamaFiyat;
            degerlendirme.FiyatFarkiYuzde = Math.Round(degerlendirme.FiyatFarki / degerlendirme.OrtalamaFiyat * 100, 1);

            degerlendirme.Degerlendirme = degerlendirme.FiyatFarkiYuzde switch
            {
                < -5 => "Uygun Fiyat",
                < 5 => "Piyasa Fiyatinda",
                _ => "Piyasanin Ustunde"
            };

            // Sehir dagilimi
            degerlendirme.SehirDagilimi = piyasaIlanlari
                .Where(p => !string.IsNullOrEmpty(p.Sehir))
                .GroupBy(p => p.Sehir!)
                .Select(g => new SehirDagilimi
                {
                    Sehir = g.Key,
                    IlanSayisi = g.Count(),
                    OrtalamaFiyat = g.Average(p => p.Fiyat)
                })
                .OrderByDescending(s => s.IlanSayisi)
                .ToList();
        }

        return degerlendirme;
    }

    public Task<List<PiyasaIlan>> TaraPiyasaAsync(AracIlan aracIlan)
    {
        // Bu method web scraping veya API ile sahibinden/arabam taraması yapacak
        // Şimdilik boş liste dönüyor - ileride implementasyon eklenecek
        return Task.FromResult(new List<PiyasaIlan>());
    }

    #endregion

    #region Arac Satislari

    public async Task<List<AracSatis>> GetAracSatisListesiAsync(int yil, int? ay = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.AracSatislari
            .Include(s => s.AracIlan)
            .Include(s => s.SatisPersoneli)
            .Include(s => s.AliciCari)
            .Where(s => s.SatisTarihi.Year == yil);

        if (ay.HasValue)
            query = query.Where(s => s.SatisTarihi.Month == ay.Value);

        return await query.OrderByDescending(s => s.SatisTarihi).ToListAsync();
    }

    public async Task<AracSatis?> GetAracSatisByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.AracSatislari
            .Include(s => s.AracIlan)
            .Include(s => s.SatisPersoneli)
            .Include(s => s.AliciCari)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    #endregion

    #region Marka/Model

    public async Task<List<AracMarka>> GetAracMarkalarAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.AracMarkalari
            .Where(m => m.Aktif)
            .OrderBy(m => m.SiraNo)
            .ThenBy(m => m.MarkaAdi)
            .ToListAsync();
    }

    public async Task<List<AracModelTanim>> GetAracModelleriAsync(int markaId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.AracModelleri
            .Where(m => m.MarkaId == markaId && m.Aktif)
            .OrderBy(m => m.ModelAdi)
            .ToListAsync();
    }

    public async Task SeedMarkaModelAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        if (await context.AracMarkalari.AnyAsync()) return;

        var markalar = new List<AracMarka>
        {
            new() { MarkaAdi = "Volkswagen", SiraNo = 1 },
            new() { MarkaAdi = "BMW", SiraNo = 2 },
            new() { MarkaAdi = "Mercedes-Benz", SiraNo = 3 },
            new() { MarkaAdi = "Audi", SiraNo = 4 },
            new() { MarkaAdi = "Ford", SiraNo = 5 },
            new() { MarkaAdi = "Renault", SiraNo = 6 },
            new() { MarkaAdi = "Fiat", SiraNo = 7 },
            new() { MarkaAdi = "Toyota", SiraNo = 8 },
            new() { MarkaAdi = "Honda", SiraNo = 9 },
            new() { MarkaAdi = "Hyundai", SiraNo = 10 },
            new() { MarkaAdi = "Opel", SiraNo = 11 },
            new() { MarkaAdi = "Peugeot", SiraNo = 12 },
            new() { MarkaAdi = "Citroen", SiraNo = 13 },
            new() { MarkaAdi = "Skoda", SiraNo = 14 },
            new() { MarkaAdi = "Seat", SiraNo = 15 },
            new() { MarkaAdi = "Volvo", SiraNo = 16 },
            new() { MarkaAdi = "Nissan", SiraNo = 17 },
            new() { MarkaAdi = "Mazda", SiraNo = 18 },
            new() { MarkaAdi = "Kia", SiraNo = 19 },
            new() { MarkaAdi = "Dacia", SiraNo = 20 }
        };

        context.AracMarkalari.AddRange(markalar);
        await context.SaveChangesAsync();
    }

    #endregion

    #region Dashboard

    public async Task<SatisDashboardData> GetDashboardDataAsync(int yil, int? ay = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var buAy = ay ?? DateTime.Today.Month;
        var ilanlar = await context.AracIlanlari.ToListAsync();
        var satislar = await context.AracSatislari
            .Include(s => s.SatisPersoneli)
            .Where(s => s.SatisTarihi.Year == yil)
            .ToListAsync();
        var personeller = await context.SatisPersonelleri.Where(p => p.Aktif).ToListAsync();

        var buAySatislari = satislar.Where(s => s.SatisTarihi.Month == buAy).ToList();

        var dashboard = new SatisDashboardData
        {
            ToplamAktifIlan = ilanlar.Count(i => i.IlanDurum == IlanDurum.Aktif),
            BuAySatilanArac = buAySatislari.Count,
            BuAySatisTutari = buAySatislari.Sum(s => s.SatisFiyati),
            BuAyKomisyon = buAySatislari.Sum(s => s.KomisyonTutari),
            BekleyenIlan = ilanlar.Count(i => i.IlanDurum == IlanDurum.Aktif),
            RezerveIlan = ilanlar.Count(i => i.IlanDurum == IlanDurum.Rezerve)
        };

        // Ortalama karlilik
        var satilmisIlanlar = ilanlar.Where(i => i.IlanDurum == IlanDurum.Satildi && i.AlisFiyati > 0).ToList();
        if (satilmisIlanlar.Any())
        {
            var toplamKar = satilmisIlanlar.Sum(i => i.SatisFiyati - i.AlisFiyati);
            var toplamAlis = satilmisIlanlar.Sum(i => i.AlisFiyati);
            dashboard.OrtalamaKarlilik = Math.Round(toplamKar / toplamAlis * 100, 1);
        }

        // Personel ozetleri
        foreach (var personel in personeller)
        {
            var personelSatislari = buAySatislari.Where(s => s.SatisPersoneliId == personel.Id).ToList();
            dashboard.PersonelOzetleri.Add(new SatisPersonelOzet
            {
                PersonelId = personel.Id,
                PersonelAdi = personel.AdSoyad,
                AktifIlan = ilanlar.Count(i => i.SatisPersoneliId == personel.Id && i.IlanDurum == IlanDurum.Aktif),
                BuAySatis = personelSatislari.Count,
                BuAyKomisyon = personelSatislari.Sum(s => s.KomisyonTutari)
            });
        }

        // Marka dagilimi
        dashboard.MarkaIlanDagilimi = ilanlar
            .GroupBy(i => i.Marka)
            .Select(g => new MarkaIlanDagilimi
            {
                Marka = g.Key,
                IlanSayisi = g.Count(i => i.IlanDurum == IlanDurum.Aktif),
                SatisSayisi = g.Count(i => i.IlanDurum == IlanDurum.Satildi)
            })
            .OrderByDescending(m => m.IlanSayisi)
            .Take(10)
            .ToList();

        // Aylik trend
        for (int m = 1; m <= 12; m++)
        {
            var aySatislari = satislar.Where(s => s.SatisTarihi.Month == m).ToList();
            dashboard.AylikTrend.Add(new AylikSatisData
            {
                Ay = m,
                AyAdi = AyAdlari[m],
                SatisSayisi = aySatislari.Count,
                SatisTutari = aySatislari.Sum(s => s.SatisFiyati),
                Komisyon = aySatislari.Sum(s => s.KomisyonTutari)
            });
        }

        return dashboard;
    }

    #endregion
}



