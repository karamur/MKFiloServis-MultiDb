using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using MKFiloServis.Web.Helpers;
using MKFiloServis.Web.Models;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Web.Services;

public class RaporService : IRaporService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public RaporService(IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<ServisCalismaRaporItem>> GetServisCalismaRaporuAsync(
        DateTime startDate,
        DateTime endDate,
        int? aracId = null,
        int? soforId = null,
        int? guzergahId = null,
        int? cariId = null,
        AracSahiplikTipi? sahiplikTipi = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.ServisCalismalari
            .Include(s => s.Arac)
            .Include(s => s.Sofor)
            .Include(s => s.Guzergah)
                .ThenInclude(g => g.Cari)
            .Where(s => s.CalismaTarihi >= startDate && s.CalismaTarihi <= endDate)
            .Where(s => s.Durum == CalismaDurum.Tamamlandi);

        if (aracId.HasValue)
            query = query.Where(s => s.AracId == aracId.Value);

        if (soforId.HasValue)
            query = query.Where(s => s.SoforId == soforId.Value);

        if (guzergahId.HasValue)
            query = query.Where(s => s.GuzergahId == guzergahId.Value);

        if (cariId.HasValue)
            query = query.Where(s => s.Guzergah.CariId == cariId.Value);

        // Sahiplik tipi filtreleme
        if (sahiplikTipi.HasValue)
            query = query.Where(s => s.Arac.SahiplikTipi == sahiplikTipi.Value);

        var data = await query.ToListAsync();

        // Gruplama ve özet hesaplama
        var grouped = data
            .GroupBy(s => new { s.AracId, s.SoforId, s.GuzergahId })
            .Select(g => new ServisCalismaRaporItem
            {
                Tarih = g.Min(x => x.CalismaTarihi),
                Plaka = g.First().Arac?.AktifPlaka ?? string.Empty,
                SoforAdi = g.First().Sofor.TamAd,
                GuzergahAdi = g.First().Guzergah.GuzergahAdi,
                FirmaAdi = g.First().Guzergah.Cari.Unvan,
                ServisTuru = string.Join(", ", g.Select(x => x.ServisTuru.ToString()).Distinct()),
                BirimFiyat = g.First().Fiyat ?? g.First().Guzergah.BirimFiyat,
                CalisilanGun = g.Count(),
                ToplamTutar = g.Sum(x => x.Fiyat ?? x.Guzergah.BirimFiyat)
            })
            .OrderBy(x => x.FirmaAdi)
            .ThenBy(x => x.GuzergahAdi)
            .ThenBy(x => x.Plaka)
            .ToList();

        return grouped;
    }

    public async Task<List<FaturaOdemeRaporItem>> GetFaturaOdemeRaporuAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        int? cariId = null,
        bool? sadeceBekleyenler = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.Faturalar
            .Include(f => f.Cari)
            .AsQueryable();

        if (startDate.HasValue)
            query = query.Where(f => f.FaturaTarihi >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(f => f.FaturaTarihi <= endDate.Value);

        if (cariId.HasValue)
            query = query.Where(f => f.CariId == cariId.Value);

        if (sadeceBekleyenler == true)
            query = query.Where(f => f.Durum == FaturaDurum.Beklemede || f.Durum == FaturaDurum.KismiOdendi);

        var data = await query.OrderBy(f => f.VadeTarihi).ToListAsync();

        return data.Select(f => new FaturaOdemeRaporItem
        {
            FaturaId = f.Id,
            FaturaNo = f.FaturaNo,
            FaturaTarihi = f.FaturaTarihi,
            VadeTarihi = f.VadeTarihi,
            CariUnvan = f.Cari.Unvan,
            FaturaTipi = f.FaturaTipi.ToString(),
            Durum = f.Durum.ToString(),
            GenelToplam = f.GenelToplam,
            OdenenTutar = f.OdenenTutar,
            KalanTutar = f.KalanTutar,
            VadeGunu = f.VadeTarihi.HasValue 
                ? (f.VadeTarihi.Value - DateTime.Today).Days 
                : 0
        }).ToList();
    }

    public async Task<List<AracMasrafRaporItem>> GetAracMasrafRaporuAsync(
        DateTime startDate,
        DateTime endDate,
        int? aracId = null,
        AracSahiplikTipi? sahiplikTipi = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.AracMasraflari
            .Include(m => m.Arac)
            .Include(m => m.MasrafKalemi)
            .Include(m => m.Guzergah)
            .Where(m => m.MasrafTarihi >= startDate && m.MasrafTarihi <= endDate);

        if (aracId.HasValue)
            query = query.Where(m => m.AracId == aracId.Value);

        // Sahiplik tipi filtreleme
        if (sahiplikTipi.HasValue)
            query = query.Where(m => m.Arac.SahiplikTipi == sahiplikTipi.Value);

        var data = await query.OrderByDescending(m => m.MasrafTarihi).ToListAsync();

        var items = data.Select(m => new AracMasrafRaporItem
        {
            MasrafTarihi = m.MasrafTarihi,
            Plaka = m.Arac?.AktifPlaka ?? string.Empty,
            MasrafKalemi = m.MasrafKalemi.MasrafAdi,
            Kategori = m.MasrafKalemi.Kategori.ToString(),
            GuzergahAdi = m.Guzergah?.GuzergahAdi,
            Tutar = m.Tutar,
            BelgeNo = m.BelgeNo,
            Aciklama = m.Aciklama,
            ArizaKaynakli = m.ArizaKaynaklimi
        }).ToList();

        // Servis kayıtlarını da dahil et (AracMasraf bağlantısı olmayanlar)
        var servisQuery = context.ServisKayitlari
            .Include(s => s.Arac)
            .Include(s => s.ServisciCari)
            .Where(s => s.ServisTarihi >= startDate && s.ServisTarihi <= endDate && !s.IsDeleted)
            .Where(s => s.AracMasrafId == null); // sadece henüz masrafa bağlanmamış servisler

        if (aracId.HasValue)
            servisQuery = servisQuery.Where(s => s.AracId == aracId.Value);

        if (sahiplikTipi.HasValue)
            servisQuery = servisQuery.Where(s => s.Arac.SahiplikTipi == sahiplikTipi.Value);

        var servisler = await servisQuery.OrderByDescending(s => s.ServisTarihi).ToListAsync();

        items.AddRange(servisler.Select(s => new AracMasrafRaporItem
        {
            MasrafTarihi = s.ServisTarihi,
            Plaka = s.Arac?.AktifPlaka ?? string.Empty,
            MasrafKalemi = s.ServisAdi,
            Kategori = "Servis",
            GuzergahAdi = s.ServisciCari?.Unvan,
            Tutar = s.ToplamTutar > 0 ? s.ToplamTutar : (s.IscilikTutari + s.ParcaTutari + s.KdvTutar),
            BelgeNo = null,
            Aciklama = s.Aciklama,
            ArizaKaynakli = false
        }));

        return items.OrderByDescending(i => i.MasrafTarihi).ToList();
    }

    public async Task<CariEkstre> GetCariEkstreAsync(
        int cariId,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var cari = await context.Cariler.FindAsync(cariId);
        if (cari == null)
            throw new ArgumentException("Cari bulunamadı", nameof(cariId));

        var ekstre = new CariEkstre
        {
            CariId = cari.Id,
            CariKodu = cari.CariKodu,
            CariUnvan = cari.Unvan,
            BaslangicTarihi = startDate,
            BitisTarihi = endDate
        };

        var hareketler = new List<CariEkstreItem>();

        // Faturaları getir
        var faturalar = await context.Faturalar
            .Where(f => f.CariId == cariId)
            .Where(f => !startDate.HasValue || f.FaturaTarihi >= startDate.Value)
            .Where(f => !endDate.HasValue || f.FaturaTarihi <= endDate.Value)
            .OrderBy(f => f.FaturaTarihi)
            .ToListAsync();

        foreach (var fatura in faturalar)
        {
            var isBorc = fatura.FaturaTipi == FaturaTipi.SatisFaturasi || fatura.FaturaTipi == FaturaTipi.AlisIadeFaturasi;
            hareketler.Add(new CariEkstreItem
            {
                Tarih = fatura.FaturaTarihi,
                BelgeNo = fatura.FaturaNo,
                IslemTipi = fatura.FaturaTipi.ToString(),
                Aciklama = fatura.Aciklama ?? "Fatura",
                Borc = isBorc ? fatura.GenelToplam : 0,
                Alacak = isBorc ? 0 : fatura.GenelToplam
            });
        }

        // Banka/Kasa hareketlerini getir
        var bankaHareketler = await context.BankaKasaHareketleri
            .Where(h => h.CariId == cariId)
            .Where(h => !startDate.HasValue || h.IslemTarihi >= startDate.Value)
            .Where(h => !endDate.HasValue || h.IslemTarihi <= endDate.Value)
            .OrderBy(h => h.IslemTarihi)
            .ToListAsync();

        foreach (var hareket in bankaHareketler)
        {
            hareketler.Add(new CariEkstreItem
            {
                Tarih = hareket.IslemTarihi,
                BelgeNo = hareket.IslemNo,
                IslemTipi = hareket.HareketTipi.ToString(),
                Aciklama = hareket.Aciklama ?? "Ödeme/Tahsilat",
                Borc = hareket.HareketTipi == HareketTipi.Cikis ? hareket.Tutar : 0,
                Alacak = hareket.HareketTipi == HareketTipi.Giris ? hareket.Tutar : 0
            });
        }

        // Tarihe göre sırala ve bakiye hesapla
        hareketler = hareketler.OrderBy(h => h.Tarih).ThenBy(h => h.BelgeNo).ToList();

        decimal bakiye = 0;
        foreach (var hareket in hareketler)
        {
            bakiye += hareket.Borc - hareket.Alacak;
            hareket.Bakiye = bakiye;
        }

        ekstre.Hareketler = hareketler;
        ekstre.ToplamBorc = hareketler.Sum(h => h.Borc);
        ekstre.ToplamAlacak = hareketler.Sum(h => h.Alacak);
        ekstre.Bakiye = bakiye;

        return ekstre;
    }

    public async Task<SoforPerformansOzet> GetSoforPerformansAsync(
        int soforId,
        DateTime startDate,
        DateTime endDate,
        AracSahiplikTipi? sahiplikTipi = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var sofor = await context.Soforler.FindAsync(soforId);
        if (sofor == null)
            throw new ArgumentException("Şoför bulunamadı", nameof(soforId));

        var query = context.ServisCalismalari
            .Include(s => s.Arac)
            .Include(s => s.Guzergah)
                .ThenInclude(g => g.Cari)
            .Where(s => s.SoforId == soforId)
            .Where(s => s.CalismaTarihi >= startDate && s.CalismaTarihi <= endDate)
            .Where(s => s.Durum == CalismaDurum.Tamamlandi);

        // Sahiplik tipi filtreleme
        if (sahiplikTipi.HasValue)
            query = query.Where(s => s.Arac.SahiplikTipi == sahiplikTipi.Value);

        var calismalar = await query.AsNoTracking().ToListAsync();

        var ozet = new SoforPerformansOzet
        {
            SoforId = sofor.Id,
            SoforAdi = sofor.TamAd,
            SoforKodu = sofor.SoforKodu,
            ToplamSeferSayisi = calismalar.Count,
            CalistigiGunSayisi = calismalar.Select(c => c.CalismaTarihi.Date).Distinct().Count(),
            ToplamKazanc = calismalar.Sum(c => c.Fiyat ?? c.Guzergah.BirimFiyat),
            ArizaliSeferSayisi = calismalar.Count(c => c.ArizaOlduMu),
            ToplamKm = calismalar.Where(c => c.KmBaslangic.HasValue && c.KmBitis.HasValue)
                        .Sum(c => c.KmBitis!.Value - c.KmBaslangic!.Value)
        };

        // Araç bazlı performans
        ozet.CalistigiAraclar = calismalar
            .GroupBy(c => new { c.AracId, c.Arac?.AktifPlaka })
            .Select(g => new SoforAracPerformansi
            {
                AracId = g.Key.AracId,
                Plaka = g.Key.AktifPlaka ?? "-",
                SeferSayisi = g.Count(),
                ToplamKazanc = g.Sum(c => c.Fiyat ?? c.Guzergah.BirimFiyat),
                ArizaSayisi = g.Count(c => c.ArizaOlduMu)
            })
            .OrderByDescending(a => a.SeferSayisi)
            .ToList();

        // Güzergah bazlı performans
        ozet.CalistigiGuzergahlar = calismalar
            .GroupBy(c => new { c.GuzergahId, c.Guzergah.GuzergahAdi, CariAdi = c.Guzergah.Cari.Unvan })
            .Select(g => new SoforGuzergahPerformansi
            {
                GuzergahId = g.Key.GuzergahId,
                GuzergahAdi = g.Key.GuzergahAdi,
                CariAdi = g.Key.CariAdi,
                SeferSayisi = g.Count(),
                ToplamKazanc = g.Sum(c => c.Fiyat ?? c.Guzergah.BirimFiyat)
            })
            .OrderByDescending(g => g.SeferSayisi)
            .ToList();

        // Aylık performans (grafik için)
        ozet.AylikPerformans = calismalar
            .GroupBy(c => new { c.CalismaTarihi.Year, c.CalismaTarihi.Month })
            .Select(g => new SoforAylikPerformans
            {
                Yil = g.Key.Year,
                Ay = g.Key.Month,
                AyAdi = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy", new System.Globalization.CultureInfo("tr-TR")),
                SeferSayisi = g.Count(),
                ToplamKazanc = g.Sum(c => c.Fiyat ?? c.Guzergah.BirimFiyat),
                CalistigiGun = g.Select(c => c.CalismaTarihi.Date).Distinct().Count()
            })
            .OrderBy(m => m.Yil).ThenBy(m => m.Ay)
            .ToList();

        return ozet;
    }

    public async Task<List<SoforKarsilastirmaOzeti>> GetSoforKarsilastirmaAsync(
        DateTime startDate,
        DateTime endDate,
        AracSahiplikTipi? sahiplikTipi = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.ServisCalismalari
            .Include(s => s.Sofor)
            .Include(s => s.Arac)
            .Include(s => s.Guzergah)
            .Where(s => s.CalismaTarihi >= startDate && s.CalismaTarihi <= endDate)
            .Where(s => s.Durum == CalismaDurum.Tamamlandi);

        // Sahiplik tipi filtreleme
        if (sahiplikTipi.HasValue)
            query = query.Where(s => s.Arac.SahiplikTipi == sahiplikTipi.Value);

        var calismalar = await query.AsNoTracking().ToListAsync();

        return calismalar
            .GroupBy(c => new { c.SoforId, c.Sofor.TamAd })
            .Select(g => new SoforKarsilastirmaOzeti
            {
                SoforId = g.Key.SoforId,
                SoforAdi = g.Key.TamAd,
                SeferSayisi = g.Count(),
                ToplamKazanc = g.Sum(c => c.Fiyat ?? c.Guzergah.BirimFiyat),
                ArizaOrani = g.Count() > 0 ? (decimal)g.Count(c => c.ArizaOlduMu) / g.Count() * 100 : 0,
                CalistigiGun = g.Select(c => c.CalismaTarihi.Date).Distinct().Count()
            })
            .OrderByDescending(s => s.ToplamKazanc)
            .ToList();
    }

    public async Task<AracKarlilikOzet> GetAracKarlilikAsync(
        int aracId,
        DateTime startDate,
        DateTime endDate)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var arac = await context.Araclar
            .Include(a => a.KiralikCari)
            .FirstOrDefaultAsync(a => a.Id == aracId);

        if (arac == null)
            throw new ArgumentException("Araç bulunamadı", nameof(aracId));

        // Servis çalışmalarını getir (gelir)
        var calismalar = await context.ServisCalismalari
            .Include(s => s.Guzergah)
                .ThenInclude(g => g.Cari)
            .Where(s => s.AracId == aracId)
            .Where(s => s.CalismaTarihi >= startDate && s.CalismaTarihi <= endDate)
            .Where(s => s.Durum == CalismaDurum.Tamamlandi)
            .AsNoTracking()
            .ToListAsync();

        // Masrafları getir (gider)
        // AracMasraflari
        var masraflar = await context.AracMasraflari
            .Include(m => m.MasrafKalemi)
            .Where(m => m.AracId == aracId)
            .Where(m => m.MasrafTarihi >= startDate && m.MasrafTarihi <= endDate)
            .Where(m => !m.IsDeleted)
            .AsNoTracking()
            .ToListAsync();

        // ServisKaydi giderleri (AracMasraf bağlantısı olmayanlar)
        var servisGiderleri = await context.ServisKayitlari
            .Where(s => s.AracId == aracId)
            .Where(s => s.ServisTarihi >= startDate && s.ServisTarihi <= endDate)
            .Where(s => !s.IsDeleted)
            .Where(s => s.AracMasrafId == null)
            .AsNoTracking()
            .ToListAsync();

        // Dönem içindeki ay sayısı (kira hesaplama için)
        var aylikKiraBedeli = arac.AylikKiraBedeli ?? 0;
        var aySayisi = ((endDate.Year - startDate.Year) * 12) + endDate.Month - startDate.Month + 1;
        var toplamKiraBedeli = arac.SahiplikTipi == AracSahiplikTipi.Kiralik ? aylikKiraBedeli * aySayisi : 0;

        // Komisyon hesaplama
        decimal toplamKomisyon = 0;
        if (arac.KomisyonVar && arac.KomisyonOrani.HasValue)
        {
            var toplamGelir = calismalar.Sum(c => c.Fiyat ?? c.Guzergah.BirimFiyat);
            toplamKomisyon = toplamGelir * arac.KomisyonOrani.Value / 100;
        }
        else if (arac.KomisyonVar && arac.SabitKomisyonTutari.HasValue)
        {
            toplamKomisyon = calismalar.Count * arac.SabitKomisyonTutari.Value;
        }

        var ozet = new AracKarlilikOzet
        {
            AracId = arac.Id,
            Plaka = arac.AktifPlaka ?? "-",
            Marka = arac.Marka,
            Model = arac.Model,
            SahiplikTipi = arac.SahiplikTipi == AracSahiplikTipi.Ozmal ? "Özmal" : "Kiralık",
            ToplamSeferSayisi = calismalar.Count,
            ToplamGelir = calismalar.Sum(c => c.Fiyat ?? c.Guzergah.BirimFiyat),
            ToplamMasraf = masraflar.Sum(m => m.Tutar),
            KiraBedeli = toplamKiraBedeli,
            KomisyonTutari = toplamKomisyon,
            ArizaSayisi = calismalar.Count(c => c.ArizaOlduMu),
            CalismaGunSayisi = calismalar.Select(c => c.CalismaTarihi.Date).Distinct().Count(),
            SonCalismaTarihi = calismalar.MaxBy(c => c.CalismaTarihi)?.CalismaTarihi
        };

        // Masraf detayları (kategori bazlı)
        var servisToplamMasraf = servisGiderleri.Sum(s => s.ToplamTutar > 0 ? s.ToplamTutar : (s.IscilikTutari + s.ParcaTutari + s.KdvTutar));
        var toplamMasraf = masraflar.Sum(m => m.Tutar) + servisToplamMasraf;
        ozet.ToplamMasraf = masraflar.Sum(m => m.Tutar) + servisToplamMasraf;

        ozet.MasrafDetaylari = masraflar
            .GroupBy(m => new { m.MasrafKalemiId, m.MasrafKalemi.MasrafAdi })
            .Select(g => new AracMasrafDetay
            {
                MasrafKalemiId = g.Key.MasrafKalemiId,
                MasrafKalemiAdi = g.Key.MasrafAdi,
                ToplamTutar = g.Sum(m => m.Tutar),
                Adet = g.Count(),
                Oran = toplamMasraf > 0 ? g.Sum(m => m.Tutar) / toplamMasraf * 100 : 0
            })
            .ToList()
            .Concat(servisToplamMasraf > 0
                ? new[] { new AracMasrafDetay { MasrafKalemiId = 0, MasrafKalemiAdi = "Servis/Bakım (Yeni Servis)", ToplamTutar = servisToplamMasraf, Adet = servisGiderleri.Count, Oran = toplamMasraf > 0 ? servisToplamMasraf / toplamMasraf * 100 : 0 } }
                : Array.Empty<AracMasrafDetay>())
            .OrderByDescending(d => d.ToplamTutar)
            .ToList();

        // Güzergah performansları
        ozet.GuzergahPerformanslari = calismalar
            .GroupBy(c => new { c.GuzergahId, c.Guzergah.GuzergahAdi, FirmaAdi = c.Guzergah.Cari.Unvan })
            .Select(g => new AracGuzergahPerformansi
            {
                GuzergahId = g.Key.GuzergahId,
                GuzergahAdi = g.Key.GuzergahAdi,
                FirmaAdi = g.Key.FirmaAdi,
                SeferSayisi = g.Count(),
                ToplamGelir = g.Sum(c => c.Fiyat ?? c.Guzergah.BirimFiyat)
            })
            .OrderByDescending(g => g.ToplamGelir)
            .ToList();

        // Aylık karlılık (grafik için)
        var aylar = calismalar
            .GroupBy(c => new { c.CalismaTarihi.Year, c.CalismaTarihi.Month })
            .Select(g => new { g.Key.Year, g.Key.Month })
            .Union(masraflar
                .GroupBy(m => new { m.MasrafTarihi.Year, m.MasrafTarihi.Month })
                .Select(g => new { g.Key.Year, g.Key.Month }))
            .Distinct()
            .OrderBy(a => a.Year).ThenBy(a => a.Month)
            .ToList();

        ozet.AylikKarlilik = aylar.Select(ay =>
        {
            var ayGelir = calismalar
                .Where(c => c.CalismaTarihi.Year == ay.Year && c.CalismaTarihi.Month == ay.Month)
                .Sum(c => c.Fiyat ?? c.Guzergah.BirimFiyat);

            var ayMasraf = masraflar
                .Where(m => m.MasrafTarihi.Year == ay.Year && m.MasrafTarihi.Month == ay.Month)
                .Sum(m => m.Tutar);

            var ayKira = arac.SahiplikTipi == AracSahiplikTipi.Kiralik ? aylikKiraBedeli : 0;

            decimal ayKomisyon = 0;
            if (arac.KomisyonVar && arac.KomisyonOrani.HasValue)
                ayKomisyon = ayGelir * arac.KomisyonOrani.Value / 100;
            else if (arac.KomisyonVar && arac.SabitKomisyonTutari.HasValue)
            {
                var aySeferSayisi = calismalar.Count(c => c.CalismaTarihi.Year == ay.Year && c.CalismaTarihi.Month == ay.Month);
                ayKomisyon = aySeferSayisi * arac.SabitKomisyonTutari.Value;
            }

            return new AracAylikKarlilik
            {
                Yil = ay.Year,
                Ay = ay.Month,
                AyAdi = new DateTime(ay.Year, ay.Month, 1).ToString("MMM yyyy", new System.Globalization.CultureInfo("tr-TR")),
                SeferSayisi = calismalar.Count(c => c.CalismaTarihi.Year == ay.Year && c.CalismaTarihi.Month == ay.Month),
                Gelir = ayGelir,
                Masraf = ayMasraf,
                KiraBedeli = ayKira,
                Komisyon = ayKomisyon
            };
        }).ToList();

        return ozet;
    }

    public async Task<List<AracKarsilastirmaOzeti>> GetAracKarsilastirmaAsync(
        DateTime startDate,
        DateTime endDate,
        AracSahiplikTipi? sahiplikTipi = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        // Tüm aktif araçları getir
        var araclarQuery = context.Araclar
            .Where(a => a.Aktif && !a.IsDeleted);

        // Sahiplik tipi filtreleme
        if (sahiplikTipi.HasValue)
            araclarQuery = araclarQuery.Where(a => a.SahiplikTipi == sahiplikTipi.Value);

        var araclar = await araclarQuery.AsNoTracking().ToListAsync();

        // Servis çalışmalarını getir
        var calismaQuery = context.ServisCalismalari
            .Include(s => s.Guzergah)
            .Include(s => s.Arac)
            .Where(s => s.CalismaTarihi >= startDate && s.CalismaTarihi <= endDate)
            .Where(s => s.Durum == CalismaDurum.Tamamlandi);

        // Sahiplik tipi filtreleme
        if (sahiplikTipi.HasValue)
            calismaQuery = calismaQuery.Where(s => s.Arac.SahiplikTipi == sahiplikTipi.Value);

        var calismalar = await calismaQuery.AsNoTracking().ToListAsync();

        // Masrafları getir
        var masrafQuery = context.AracMasraflari
            .Include(m => m.Arac)
            .Where(m => m.MasrafTarihi >= startDate && m.MasrafTarihi <= endDate)
            .Where(m => !m.IsDeleted);

        // Sahiplik tipi filtreleme
        if (sahiplikTipi.HasValue)
            masrafQuery = masrafQuery.Where(m => m.Arac.SahiplikTipi == sahiplikTipi.Value);

        var masraflar = await masrafQuery.AsNoTracking().ToListAsync();

        // ServisKaydi giderleri (AracMasraf bağlantısı olmayanlar)
        var servisKayitlariQuery = context.ServisKayitlari
            .Include(s => s.Arac)
            .Where(s => s.ServisTarihi >= startDate && s.ServisTarihi <= endDate)
            .Where(s => !s.IsDeleted)
            .Where(s => s.AracMasrafId == null);

        if (sahiplikTipi.HasValue)
            servisKayitlariQuery = servisKayitlariQuery.Where(s => s.Arac.SahiplikTipi == sahiplikTipi.Value);

        var servisKayitlari = await servisKayitlariQuery.AsNoTracking().ToListAsync();

        var aySayisi = ((endDate.Year - startDate.Year) * 12) + endDate.Month - startDate.Month + 1;

        var sonuclar = araclar.Select(arac =>
        {
            var aracCalismalari = calismalar.Where(c => c.AracId == arac.Id).ToList();
            var aracMasraflari = masraflar.Where(m => m.AracId == arac.Id).ToList();
            var aracServisleri = servisKayitlari.Where(s => s.AracId == arac.Id).ToList();

            var toplamGelir = aracCalismalari.Sum(c => c.Fiyat ?? c.Guzergah.BirimFiyat);
            var toplamMasraf = aracMasraflari.Sum(m => m.Tutar) +
                               aracServisleri.Sum(s => s.ToplamTutar > 0 ? s.ToplamTutar : (s.IscilikTutari + s.ParcaTutari + s.KdvTutar));
            var kiraBedeli = arac.SahiplikTipi == AracSahiplikTipi.Kiralik ? (arac.AylikKiraBedeli ?? 0) * aySayisi : 0;

            decimal komisyon = 0;
            if (arac.KomisyonVar && arac.KomisyonOrani.HasValue)
                komisyon = toplamGelir * arac.KomisyonOrani.Value / 100;
            else if (arac.KomisyonVar && arac.SabitKomisyonTutari.HasValue)
                komisyon = aracCalismalari.Count * arac.SabitKomisyonTutari.Value;

            var toplamGider = toplamMasraf + kiraBedeli + komisyon;
            var netKar = toplamGelir - toplamGider;

            return new AracKarsilastirmaOzeti
            {
                AracId = arac.Id,
                Plaka = arac.AktifPlaka ?? "-",
                MarkaModel = $"{arac.Marka} {arac.Model}".Trim(),
                SahiplikTipi = arac.SahiplikTipi == AracSahiplikTipi.Ozmal ? "Özmal" : "Kiralık",
                SeferSayisi = aracCalismalari.Count,
                ToplamGelir = toplamGelir,
                ToplamGider = toplamGider,
                NetKar = netKar,
                KarMarji = toplamGelir > 0 ? netKar / toplamGelir * 100 : 0,
                ArizaSayisi = aracCalismalari.Count(c => c.ArizaOlduMu),
                ArizaOrani = aracCalismalari.Count > 0 ? (decimal)aracCalismalari.Count(c => c.ArizaOlduMu) / aracCalismalari.Count * 100 : 0
            };
        })
        .Where(a => a.SeferSayisi > 0) // En az 1 sefer yapmış araçlar
        .OrderByDescending(a => a.NetKar)
        .ToList();

        // Sıralama bilgilerini ekle
        for (int i = 0; i < sonuclar.Count; i++)
        {
            sonuclar[i].KarlilikSirasi = i + 1;
        }

        var verimlilikSirali = sonuclar
            .OrderByDescending(a => a.SeferSayisi > 0 ? a.NetKar / a.SeferSayisi : 0)
            .ToList();
        for (int i = 0; i < verimlilikSirali.Count; i++)
        {
            verimlilikSirali[i].VerimlilikSirasi = i + 1;
        }

        return sonuclar;
    }

    /// <summary>
    /// Cari bakiye yaşlandırma raporu
    /// </summary>
    public async Task<CariYaslandirmaRapor> GetCariYaslandirmaAsync(
        DateTime? raporTarihi = null,
        int? cariId = null,
        CariTipi? cariTipi = null,
        bool sadeceBorcluCariler = false)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var bugun = raporTarihi?.Date ?? DateTime.Today;

        // Tüm carileri al
        var carilerQuery = context.Cariler
            .Include(c => c.Faturalar.Where(f => !f.IsDeleted))
            .Where(c => !c.IsDeleted && c.Aktif);

        if (cariId.HasValue)
            carilerQuery = carilerQuery.Where(c => c.Id == cariId.Value);

        if (cariTipi.HasValue)
            carilerQuery = carilerQuery.Where(c => c.CariTipi == cariTipi.Value);

        var cariler = await carilerQuery.AsNoTracking().ToListAsync();

        // Cari bazlı yaşlandırma hesapla
        var cariOzetler = new List<CariYaslandirmaOzet>();

        foreach (var cari in cariler)
        {
            var ozet = HesaplaCariYaslandirma(cari, bugun);
            if (sadeceBorcluCariler && ozet.ToplamBakiye <= 0)
                continue;

            cariOzetler.Add(ozet);
        }

        // Toplam değerleri hesapla
        var rapor = new CariYaslandirmaRapor
        {
            Cariler = cariOzetler.OrderByDescending(c => c.ToplamBakiye).ToList(),
            ToplamBakiye = cariOzetler.Sum(c => c.ToplamBakiye),
            Guncel = cariOzetler.Sum(c => c.Guncel),
            Vadesi30_60 = cariOzetler.Sum(c => c.Vadesi30_60),
            Vadesi60_90 = cariOzetler.Sum(c => c.Vadesi60_90),
            Vadesi90Plus = cariOzetler.Sum(c => c.Vadesi90Plus),
            ToplamCariSayisi = cariOzetler.Count,
            BorcluCariSayisi = cariOzetler.Count(c => c.ToplamBakiye > 0),
            AlacakliCariSayisi = cariOzetler.Count(c => c.ToplamBakiye < 0)
        };

        // Yaşlandırma bantları özeti
        var toplamBakiye = Math.Abs(rapor.ToplamBakiye);
        rapor.YaslandirmaBantlari =
        [
            new YaslandirmaBandi
            {
                BantAdi = "Güncel (0-30 Gün)",
                MinGun = 0,
                MaxGun = 30,
                Tutar = rapor.Guncel,
                FaturaSayisi = cariOzetler.SelectMany(c => c.FaturaDetaylari).Count(f => f.GecikmeGunSayisi <= 30),
                CariSayisi = cariOzetler.Count(c => c.Guncel > 0),
                Oran = toplamBakiye > 0 ? rapor.Guncel / toplamBakiye * 100 : 0,
                Renk = "success"
            },
            new YaslandirmaBandi
            {
                BantAdi = "31-60 Gün",
                MinGun = 31,
                MaxGun = 60,
                Tutar = rapor.Vadesi30_60,
                FaturaSayisi = cariOzetler.SelectMany(c => c.FaturaDetaylari).Count(f => f.GecikmeGunSayisi > 30 && f.GecikmeGunSayisi <= 60),
                CariSayisi = cariOzetler.Count(c => c.Vadesi30_60 > 0),
                Oran = toplamBakiye > 0 ? rapor.Vadesi30_60 / toplamBakiye * 100 : 0,
                Renk = "warning"
            },
            new YaslandirmaBandi
            {
                BantAdi = "61-90 Gün",
                MinGun = 61,
                MaxGun = 90,
                Tutar = rapor.Vadesi60_90,
                FaturaSayisi = cariOzetler.SelectMany(c => c.FaturaDetaylari).Count(f => f.GecikmeGunSayisi > 60 && f.GecikmeGunSayisi <= 90),
                CariSayisi = cariOzetler.Count(c => c.Vadesi60_90 > 0),
                Oran = toplamBakiye > 0 ? rapor.Vadesi60_90 / toplamBakiye * 100 : 0,
                Renk = "orange"
            },
            new YaslandirmaBandi
            {
                BantAdi = "90+ Gün",
                MinGun = 91,
                MaxGun = int.MaxValue,
                Tutar = rapor.Vadesi90Plus,
                FaturaSayisi = cariOzetler.SelectMany(c => c.FaturaDetaylari).Count(f => f.GecikmeGunSayisi > 90),
                CariSayisi = cariOzetler.Count(c => c.Vadesi90Plus > 0),
                Oran = toplamBakiye > 0 ? rapor.Vadesi90Plus / toplamBakiye * 100 : 0,
                Renk = "danger"
            }
        ];

        // Cari tipi bazlı dağılım
        rapor.CariTipiDagilimi = cariOzetler
            .GroupBy(c => c.CariTipi)
            .Select(g => new CariTipiDagilimi
            {
                CariTipi = g.Key,
                CariSayisi = g.Count(),
                ToplamBakiye = g.Sum(c => c.ToplamBakiye),
                VadesiGecmisBakiye = g.Sum(c => c.VadesiGecmisBakiye),
                Oran = toplamBakiye > 0 ? g.Sum(c => c.ToplamBakiye) / toplamBakiye * 100 : 0
            })
            .OrderByDescending(d => d.ToplamBakiye)
            .ToList();

        return rapor;
    }

    /// <summary>
    /// Tek cari için detaylı yaşlandırma
    /// </summary>
    public async Task<CariYaslandirmaOzet> GetCariYaslandirmaDetayAsync(
        int cariId,
        DateTime? raporTarihi = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var bugun = raporTarihi?.Date ?? DateTime.Today;

        var cari = await context.Cariler
            .Include(c => c.Faturalar.Where(f => !f.IsDeleted))
            .Where(c => c.Id == cariId && !c.IsDeleted)
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (cari == null)
            return new CariYaslandirmaOzet();

        return HesaplaCariYaslandirma(cari, bugun);
    }

    /// <summary>
    /// Cari yaşlandırma hesaplaması
    /// </summary>
    private CariYaslandirmaOzet HesaplaCariYaslandirma(Cari cari, DateTime bugun)
    {
        var ozet = new CariYaslandirmaOzet
        {
            CariId = cari.Id,
            CariKodu = cari.CariKodu,
            Unvan = cari.Unvan,
            CariTipi = cari.CariTipi.ToString(),
            Telefon = cari.Telefon,
            Email = cari.Email
        };

        // Sadece ödenmemiş (kalan tutarı > 0) satış faturalarını al
        var odenmeyen = cari.Faturalar
            .Where(f => f.FaturaTipi == FaturaTipi.SatisFaturasi 
                     && f.Durum != FaturaDurum.Odendi 
                     && f.GenelToplam - f.OdenenTutar > 0)
            .ToList();

        ozet.ToplamFaturaSayisi = odenmeyen.Count;
        ozet.SonFaturaTarihi = cari.Faturalar.OrderByDescending(f => f.FaturaTarihi).FirstOrDefault()?.FaturaTarihi;

        foreach (var fatura in odenmeyen)
        {
            var kalanTutar = fatura.GenelToplam - fatura.OdenenTutar;
            var vadeTarihi = fatura.VadeTarihi ?? fatura.FaturaTarihi;
            var gecikmeGun = (bugun - vadeTarihi).Days;

            var faturaDetay = new YaslandirmaFaturaDetay
            {
                FaturaId = fatura.Id,
                FaturaNo = fatura.FaturaNo,
                FaturaTarihi = fatura.FaturaTarihi,
                VadeTarihi = fatura.VadeTarihi,
                GenelToplam = fatura.GenelToplam,
                OdenenTutar = fatura.OdenenTutar,
                KalanTutar = kalanTutar,
                VadeGunSayisi = -gecikmeGun // Negatif = vadesi geçmiş
            };

            ozet.FaturaDetaylari.Add(faturaDetay);

            // Yaşlandırma bantlarına dağıt
            if (gecikmeGun <= 0)
            {
                // Vadesi gelmemiş
                ozet.Guncel += kalanTutar;
            }
            else if (gecikmeGun <= 30)
            {
                ozet.Guncel += kalanTutar;
                ozet.VadesiGecmisFaturaSayisi++;
            }
            else if (gecikmeGun <= 60)
            {
                ozet.Vadesi30_60 += kalanTutar;
                ozet.VadesiGecmisFaturaSayisi++;
            }
            else if (gecikmeGun <= 90)
            {
                ozet.Vadesi60_90 += kalanTutar;
                ozet.VadesiGecmisFaturaSayisi++;
            }
            else
            {
                ozet.Vadesi90Plus += kalanTutar;
                ozet.VadesiGecmisFaturaSayisi++;
            }
        }

        ozet.ToplamBakiye = ozet.Guncel + ozet.Vadesi30_60 + ozet.Vadesi60_90 + ozet.Vadesi90Plus;

        // Fatura detaylarını vade tarihine göre sırala
        ozet.FaturaDetaylari = ozet.FaturaDetaylari
            .OrderBy(f => f.VadeTarihi ?? f.FaturaTarihi)
            .ToList();

        return ozet;
    }

    public async Task<IseGirisCikisBildirgeRaporu> GetIseGirisCikisBildirgeAsync(
        DateTime baslangicTarihi,
        DateTime bitisTarihi,
        IseGirisCikisFiltreTipi filtreTipi = IseGirisCikisFiltreTipi.Tumu,
        PersonelGorev? gorev = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var baslangic = baslangicTarihi.Date;
        var bitis = bitisTarihi.Date;

        var query = context.Soforler
            .Where(x => !x.IsDeleted)
            .AsNoTracking();

        if (gorev.HasValue)
        {
            query = query.Where(x => x.Gorev == gorev.Value);
        }

        var personeller = await query
            .OrderBy(x => x.Ad)
            .ThenBy(x => x.Soyad)
            .ToListAsync();

        var kayitlar = new List<IseGirisCikisBildirgeSatiri>();

        foreach (var personel in personeller)
        {
            if ((filtreTipi == IseGirisCikisFiltreTipi.Tumu || filtreTipi == IseGirisCikisFiltreTipi.IseGiris) &&
                personel.IseBaslamaTarihi.HasValue)
            {
                var iseGiris = personel.IseBaslamaTarihi.Value.Date;
                if (iseGiris >= baslangic && iseGiris <= bitis)
                {
                    kayitlar.Add(new IseGirisCikisBildirgeSatiri
                    {
                        SoforId = personel.Id,
                        KayitTipi = IseGirisCikisKayitTipi.IseGiris,
                        BildirgeTarihi = iseGiris,
                        PersonelKodu = personel.SoforKodu,
                        PersonelAdi = personel.TamAd,
                        TcKimlikNo = personel.TcKimlikNo,
                        Gorev = personel.Gorev,
                        Departman = personel.Departman,
                        IseBaslamaTarihi = personel.IseBaslamaTarihi,
                        IstenAyrilmaTarihi = personel.IstenAyrilmaTarihi,
                        SgkCikisTarihi = personel.SgkCikisTarihi,
                        AktifMi = personel.Aktif,
                        Notlar = personel.Notlar
                    });
                }
            }

            var cikisTarihi = personel.SgkCikisTarihi ?? personel.IstenAyrilmaTarihi;
            if ((filtreTipi == IseGirisCikisFiltreTipi.Tumu || filtreTipi == IseGirisCikisFiltreTipi.IstenCikis) &&
                cikisTarihi.HasValue)
            {
                var istenCikis = cikisTarihi.Value.Date;
                if (istenCikis >= baslangic && istenCikis <= bitis)
                {
                    kayitlar.Add(new IseGirisCikisBildirgeSatiri
                    {
                        SoforId = personel.Id,
                        KayitTipi = IseGirisCikisKayitTipi.IstenCikis,
                        BildirgeTarihi = istenCikis,
                        PersonelKodu = personel.SoforKodu,
                        PersonelAdi = personel.TamAd,
                        TcKimlikNo = personel.TcKimlikNo,
                        Gorev = personel.Gorev,
                        Departman = personel.Departman,
                        IseBaslamaTarihi = personel.IseBaslamaTarihi,
                        IstenAyrilmaTarihi = personel.IstenAyrilmaTarihi,
                        SgkCikisTarihi = personel.SgkCikisTarihi,
                        AktifMi = personel.Aktif,
                        Notlar = personel.Notlar
                    });
                }
            }
        }

        kayitlar = kayitlar
            .OrderByDescending(x => x.BildirgeTarihi)
            .ThenBy(x => x.PersonelAdi)
            .ToList();

        return new IseGirisCikisBildirgeRaporu
        {
            BaslangicTarihi = baslangic,
            BitisTarihi = bitis,
            FiltreTipi = filtreTipi,
            Gorev = gorev,
            ToplamKayit = kayitlar.Count,
            IseGirisSayisi = kayitlar.Count(x => x.KayitTipi == IseGirisCikisKayitTipi.IseGiris),
            IstenCikisSayisi = kayitlar.Count(x => x.KayitTipi == IseGirisCikisKayitTipi.IstenCikis),
            AktifPersonelSayisi = personeller.Count(x => x.Aktif),
            Kayitlar = kayitlar
        };
    }

    public async Task<byte[]> ExportIseGirisCikisBildirgeExcelAsync(
        DateTime baslangicTarihi,
        DateTime bitisTarihi,
        IseGirisCikisFiltreTipi filtreTipi = IseGirisCikisFiltreTipi.Tumu,
        PersonelGorev? gorev = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var rapor = await GetIseGirisCikisBildirgeAsync(baslangicTarihi, bitisTarihi, filtreTipi, gorev);

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("IseGirisCikis");

        ws.Cell(1, 1).Value = "İŞE GİRİŞ / ÇIKIŞ BİLDİRGE RAPORU";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 14;
        ws.Range(1, 1, 1, 9).Merge();

        ws.Cell(2, 1).Value = $"Tarih Aralığı: {rapor.BaslangicTarihi:dd.MM.yyyy} - {rapor.BitisTarihi:dd.MM.yyyy}";
        ws.Range(2, 1, 2, 9).Merge();

        var basliklar = new[] { "Bildirge Tarihi", "Tip", "Personel Kodu", "Personel", "TC Kimlik No", "Görev", "Departman", "İşe Başlama", "İşten Çıkış / SGK Çıkış" };
        for (int i = 0; i < basliklar.Length; i++)
        {
            ws.Cell(4, i + 1).Value = basliklar[i];
            ws.Cell(4, i + 1).Style.Font.Bold = true;
            ws.Cell(4, i + 1).Style.Fill.BackgroundColor = XLColor.LightSteelBlue;
        }

        var row = 5;
        foreach (var kayit in rapor.Kayitlar)
        {
            ws.Cell(row, 1).Value = kayit.BildirgeTarihi.ToString("dd.MM.yyyy");
            ws.Cell(row, 2).Value = kayit.KayitTipi == IseGirisCikisKayitTipi.IseGiris ? "İşe Giriş" : "İşten Çıkış";
            ws.Cell(row, 3).Value = kayit.PersonelKodu;
            ws.Cell(row, 4).Value = kayit.PersonelAdi;
            ws.Cell(row, 5).Value = kayit.TcKimlikNo ?? string.Empty;
            ws.Cell(row, 6).Value = kayit.Gorev.ToString();
            ws.Cell(row, 7).Value = kayit.Departman ?? string.Empty;
            ws.Cell(row, 8).Value = kayit.IseBaslamaTarihi?.ToString("dd.MM.yyyy") ?? string.Empty;
            ws.Cell(row, 9).Value = (kayit.SgkCikisTarihi ?? kayit.IstenAyrilmaTarihi)?.ToString("dd.MM.yyyy") ?? string.Empty;
            row++;
        }

        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    // ── Operasyon Kar Raporu ────────────────────────────────────────────

    public async Task<List<OperasyonKarRaporuSatir>> GetOperasyonKarRaporuAsync(
        DateTime baslangic, DateTime bitis,
        AracSahiplikTipi? sahiplikTipi = null,
        int? aracId = null,
        int? guzergahId = null)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();

        var query = ctx.OperasyonKayitlari
            .Include(o => o.Arac)
            .Include(o => o.Sofor)
            .Include(o => o.Guzergah)
            .Where(o => !o.IsDeleted
                        && o.Tarih >= baslangic && o.Tarih <= bitis
                        && o.OperasyonDurumu == OperasyonDurumu.Gitti);

        if (sahiplikTipi.HasValue)
            query = query.Where(o => o.Arac!.SahiplikTipi == sahiplikTipi.Value);
        if (aracId.HasValue)
            query = query.Where(o => o.AracId == aracId.Value);
        if (guzergahId.HasValue)
            query = query.Where(o => o.GuzergahId == guzergahId.Value);

        var data = await query.ToListAsync();

        return data
            .GroupBy(o => new
            {
                SahiplikTipi = o.Arac?.SahiplikTipi ?? AracSahiplikTipi.Ozmal,
                o.AracId,
                o.GuzergahId,
                o.Slot
            })
            .Select(g =>
            {
                var ilk = g.First();
                var toplamSefer = (int)g.Sum(o => o.SeferSayisi * o.PuantajCarpani);
                var birimFiyat = ilk.Guzergah?.BirimFiyat ?? 0;
                return new OperasyonKarRaporuSatir
                {
                    SahiplikTipi = g.Key.SahiplikTipi,
                    FirmaTipi = SahiplikHelper.GetMetin(g.Key.SahiplikTipi),
                    AracId = g.Key.AracId,
                    Plaka = ilk.Arac?.AktifPlaka ?? ilk.Arac?.Plaka ?? "-",
                    SoforAdi = ilk.Sofor != null ? $"{ilk.Sofor.Ad} {ilk.Sofor.Soyad}" : "-",
                    GuzergahId = g.Key.GuzergahId,
                    GuzergahAdi = ilk.Guzergah?.GuzergahAdi ?? "-",
                    Slot = g.Key.Slot switch
                    {
                        SeferSlot.Sabah => "Sabah",
                        SeferSlot.Aksam => "Akşam",
                        SeferSlot.Mesai => "Mesai",
                        _ => g.Key.Slot.ToString()
                    },
                    SeferSayisi = toplamSefer,
                    BirimFiyat = birimFiyat,
                    ToplamGelir = toplamSefer * birimFiyat
                };
            })
            .OrderBy(x => x.SahiplikTipi)
            .ThenBy(x => x.Plaka)
            .ThenBy(x => x.GuzergahAdi)
            .ToList();
    }
}



