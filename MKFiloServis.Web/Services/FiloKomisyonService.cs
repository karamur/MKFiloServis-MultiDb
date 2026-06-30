using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using MKFiloServis.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MKFiloServis.Web.Services;

public class FiloKomisyonService : IFiloKomisyonService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public FiloKomisyonService(IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<FiloGuzergahEslestirme>> GetEslestirmelerAsync(int? firmaId = null, bool sadeceAktifler = true)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.FiloGuzergahEslestirmeleri
            .Include(e => e.KurumFirma)
            .Include(e => e.Guzergah)
            .Include(e => e.Arac)
                .ThenInclude(a => a!.KiralikCari)
            .Include(e => e.Arac)
                .ThenInclude(a => a!.KomisyoncuCari)
            .Include(e => e.Arac)
                .ThenInclude(a => a!.TasimaTedarikci)
            .Include(e => e.Sofor)
                .ThenInclude(s => s!.Firma)
            .Include(e => e.Kullanici)
            .Where(e => !e.IsDeleted);

        if (firmaId.HasValue && firmaId.Value > 0)
        {
            query = query.Where(e => e.FirmaId == firmaId.Value);
        }

        if (sadeceAktifler)
        {
            query = query.Where(e => e.IsActive);
        }

        return await query
            .OrderBy(e => e.Guzergah != null ? e.Guzergah.GuzergahKodu : string.Empty)
            .ThenBy(e => e.Guzergah != null ? e.Guzergah.GuzergahAdi : string.Empty)
            .ToListAsync();
    }

    public async Task<FiloGuzergahEslestirme?> GetEslestirmeByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.FiloGuzergahEslestirmeleri
            .Include(e => e.KurumFirma)
            .Include(e => e.Guzergah)
            .Include(e => e.Arac)
            .Include(e => e.Sofor)
            .Include(e => e.Kullanici)
            .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted);
    }

    public async Task<FiloGuzergahEslestirme> CreateEslestirmeAsync(FiloGuzergahEslestirme eslestirme)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        await UygulaSahiplikKurallariAsync(context, eslestirme);
        context.FiloGuzergahEslestirmeleri.Add(eslestirme);
        await context.SaveChangesAsync();
        return eslestirme;
    }

    public async Task<FiloGuzergahEslestirme> UpdateEslestirmeAsync(FiloGuzergahEslestirme eslestirme)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        await UygulaSahiplikKurallariAsync(context, eslestirme);
        var existing = await context.FiloGuzergahEslestirmeleri.FindAsync(eslestirme.Id);
        if (existing != null)
        {
            existing.KurumFirmaId = eslestirme.KurumFirmaId;
            existing.GuzergahId = eslestirme.GuzergahId;
            existing.AracId = eslestirme.AracId;
            existing.SoforId = eslestirme.SoforId;
            existing.KullaniciId = eslestirme.KullaniciId;
            existing.ServisTuru = eslestirme.ServisTuru;
            existing.KurumaKesilecekUcret = eslestirme.KurumaKesilecekUcret;
            existing.TaseronaOdenenUcret = eslestirme.TaseronaOdenenUcret;
            existing.IsActive = eslestirme.IsActive;
            existing.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();
        }
        return existing ?? eslestirme;
    }

    public async Task DeleteEslestirmeAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var existing = await context.FiloGuzergahEslestirmeleri.FindAsync(id);
        if (existing != null)
        {
            existing.IsDeleted = true;
            existing.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }
    }

    public async Task TopluPuantajUretAsync(int firmaId, int yil, int ay)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        // 1. Ayın günlerini belirle
        var baslangic = new DateTime(yil, ay, 1);
        var bitis = baslangic.AddMonths(1).AddDays(-1);

        // 2. Halihazırda var olan o aya ait puantaj kayıtlarını al (mükerrer kayıt oluşmaması için)
        var mevcutPuantajlar = await context.FiloGunlukPuantajlar
            .Where(p => p.FirmaId == firmaId && p.Tarih >= baslangic && p.Tarih <= bitis && !p.IsDeleted)
            .ToListAsync();

        // 3. Aktif eşleştirmeleri çek
        var aktifEslestirmeler = await GetEslestirmelerAsync(firmaId, sadeceAktifler: true);

        // 4. Yeni puantajları oluştur
        var yeniKavitlar = new List<FiloGunlukPuantaj>();

        for (int day = 1; day <= bitis.Day; day++)
        {
            var currentDate = new DateTime(yil, ay, day);
            bool isWeekend = currentDate.DayOfWeek == DayOfWeek.Saturday || currentDate.DayOfWeek == DayOfWeek.Sunday;

            foreach (var eslestirme in aktifEslestirmeler)
            {
                // Eğer o günde bu eşleşmeye ait bir kayıt yoksa
                bool varMi = mevcutPuantajlar.Any(p => p.FiloGuzergahEslestirmeId == eslestirme.Id && p.Tarih.Date == currentDate.Date);

                if (!varMi)
                {
                    var yeniPuantaj = new FiloGunlukPuantaj
                    {
                        FirmaId = firmaId,
                        Tarih = currentDate,
                        FiloGuzergahEslestirmeId = eslestirme.Id,
                        KurumFirmaId = eslestirme.KurumFirmaId,
                        GuzergahId = eslestirme.GuzergahId,
                        AracId = eslestirme.AracId,
                        SoforId = eslestirme.SoforId,
                        KullaniciId = eslestirme.KullaniciId,
                        Durum = isWeekend ? OperasyonDurumu.Gitmedi_Mazeretli : OperasyonDurumu.Gitti,
                        ServisTuru = eslestirme.ServisTuru,
                        PuantajCarpani = isWeekend ? 0m : 1.0m,
                        TahakkukEdenKurumUcreti = 0m,
                        TahakkukEdenTaseronUcreti = 0m
                    };

                    await UygulaPuantajKurallariAsync(context, yeniPuantaj, eslestirme);
                    yeniKavitlar.Add(yeniPuantaj);
                }
            }
        }

        if (yeniKavitlar.Any())
        {
            await context.FiloGunlukPuantajlar.AddRangeAsync(yeniKavitlar);
            await context.SaveChangesAsync();
        }
    }

    public async Task<List<FiloGunlukPuantaj>> GetGunlukPuantajlarSiraliAsync(int firmaId, DateTime tarih)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.FiloGunlukPuantajlar
            .Include(p => p.KurumFirma)
            .Include(p => p.Guzergah)
            .Include(p => p.Arac)
            .Include(p => p.Sofor)
            .Where(p => p.FirmaId == firmaId && p.Tarih.Date == tarih.Date && !p.IsDeleted)
            .OrderBy(p => p.KurumFirma!.FirmaAdi)
            .ThenBy(p => p.Guzergah!.GuzergahAdi)
            .ToListAsync();
    }

    public async Task<List<FiloGunlukPuantaj>> GetPuantajlarByTarihAraligiAsync(int? firmaId, DateTime baslangic, DateTime bitis, int? kurumId = null, int? aracId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.FiloGunlukPuantajlar
            .Include(p => p.KurumFirma)
            .Include(p => p.Guzergah)
            .Include(p => p.Arac)
            .Include(p => p.Sofor)
            .Where(p => p.Tarih >= baslangic && p.Tarih <= bitis && !p.IsDeleted);

        if (firmaId.HasValue && firmaId.Value > 0)
            query = query.Where(p => p.FirmaId == firmaId.Value);

        if (kurumId.HasValue && kurumId.Value > 0)
            query = query.Where(p => p.KurumFirmaId == kurumId.Value);

        if (aracId.HasValue && aracId.Value > 0)
            query = query.Where(p => p.AracId == aracId.Value);

        return await query.OrderBy(p => p.Tarih).ThenBy(p => p.KurumFirma!.FirmaAdi).ToListAsync();
    }

    public async Task<FiloGunlukPuantaj> CreatePuantajAsync(FiloGunlukPuantaj puantaj)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        await UygulaPuantajKurallariAsync(context, puantaj);
        context.FiloGunlukPuantajlar.Add(puantaj);
        await context.SaveChangesAsync();
        return puantaj;
    }

    public async Task<FiloGunlukPuantaj> UpdateGunlukPuantajAsync(FiloGunlukPuantaj puantaj)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var existing = await context.FiloGunlukPuantajlar.FindAsync(puantaj.Id);
        if(existing != null)
        {
            existing.Durum = puantaj.Durum;
            existing.PuantajCarpani = puantaj.PuantajCarpani;
            existing.TahakkukEdenKurumUcreti = puantaj.TahakkukEdenKurumUcreti;
            existing.TahakkukEdenTaseronUcreti = puantaj.TahakkukEdenTaseronUcreti;
            existing.TaksiKullanildiMi = puantaj.TaksiKullanildiMi;
            existing.TaksiFisTutari = puantaj.TaksiFisTutari;
            existing.TaksiFisAciklama = puantaj.TaksiFisAciklama;
            existing.ArizaYaptiMi = puantaj.ArizaYaptiMi;
            existing.ArizaAciklamasi = puantaj.ArizaAciklamasi;
            existing.Notlar = puantaj.Notlar;

            await UygulaPuantajKurallariAsync(context, existing);
            existing.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();
        }
        return existing ?? puantaj;
    }

    public async Task KurumFaturalastirAsync(List<int> puantajIds)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var puantajlar = await context.FiloGunlukPuantajlar
            .Where(p => puantajIds.Contains(p.Id))
            .ToListAsync();

        foreach(var p in puantajlar)
        {
            p.KurumFaturaKesildiMi = true;
            p.UpdatedAt = DateTime.UtcNow;
        }

        await context.SaveChangesAsync();
    }

    public async Task TaseronOdeAsync(List<int> puantajIds)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var puantajlar = await context.FiloGunlukPuantajlar
            .Where(p => puantajIds.Contains(p.Id))
            .ToListAsync();

        foreach(var p in puantajlar)
        {
            p.TaseronOdemeYapildiMi = true;
            p.UpdatedAt = DateTime.UtcNow;
        }

        await context.SaveChangesAsync();
    }

    public async Task<List<Arac>> GetAraclarAsync(int firmaId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Araclar
            .Where(a => !a.IsDeleted)
            .OrderBy(a => a.AktifPlaka)
            .ThenBy(a => a.SaseNo)
            .ToListAsync();
    }

    public async Task<List<Cari>> GetKurumlarAsync(int firmaId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Cariler
            .Where(c => !c.IsDeleted && (c.CariTipi == CariTipi.Musteri || c.CariTipi == CariTipi.MusteriTedarikci))
            .OrderBy(c => c.Unvan)
            .ToListAsync();
    }

    public async Task<List<Sofor>> GetSoforlerAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Soforler
            .Where(s => !s.IsDeleted && s.Aktif)
            .OrderBy(s => s.Ad)
            .ThenBy(s => s.Soyad)
            .ToListAsync();
    }

    public async Task<List<Guzergah>> GetGuzergahlarAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Guzergahlar
            .Where(g => !g.IsDeleted && g.Aktif)
            .OrderBy(g => g.GuzergahKodu)
            .ThenBy(g => g.GuzergahAdi)
            .ToListAsync();
    }

    public async Task<List<Kullanici>> GetKullanicilarAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Kullanicilar
            .AsNoTracking()
            .Where(k => !k.IsDeleted && k.Aktif)
            .OrderBy(k => k.AdSoyad)
            .ThenBy(k => k.KullaniciAdi)
            .ToListAsync();
    }

    private async Task UygulaSahiplikKurallariAsync(ApplicationDbContext context, FiloGuzergahEslestirme eslestirme)
    {
        var arac = await context.Araclar
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == eslestirme.AracId && !a.IsDeleted);

        if (arac == null)
            throw new InvalidOperationException("Eşleştirme için seçilen araç bulunamadı.");

        if (arac.SahiplikTipi is AracSahiplikTipi.Ozmal or AracSahiplikTipi.Kiralik)
        {
            eslestirme.TaseronaOdenenUcret = 0;
        }
    }

    private async Task UygulaPuantajKurallariAsync(ApplicationDbContext context, FiloGunlukPuantaj puantaj, FiloGuzergahEslestirme? eslestirme = null)
    {
        eslestirme ??= puantaj.FiloGuzergahEslestirmeId.HasValue
            ? await context.FiloGuzergahEslestirmeleri
                .Include(e => e.Arac)
                .FirstOrDefaultAsync(e => e.Id == puantaj.FiloGuzergahEslestirmeId.Value && !e.IsDeleted)
            : null;

        if (eslestirme?.Arac == null)
            return;

        if (puantaj.Durum is OperasyonDurumu.Gitmedi_Mazeretli or OperasyonDurumu.Gitmedi_Mazeretsiz or OperasyonDurumu.Iptal_KurumTarafindan)
        {
            puantaj.TahakkukEdenKurumUcreti = 0;
            puantaj.TahakkukEdenTaseronUcreti = 0;
            return;
        }

        var puantajCarpani = puantaj.PuantajCarpani < 0 ? 0 : puantaj.PuantajCarpani;
        var servisCarpani = GetServisTuruCarpani(puantaj.ServisTuru);
        puantaj.TahakkukEdenKurumUcreti = Math.Round(eslestirme.KurumaKesilecekUcret * puantajCarpani * servisCarpani, 2, MidpointRounding.AwayFromZero);

        puantaj.TahakkukEdenTaseronUcreti = eslestirme.Arac.SahiplikTipi == AracSahiplikTipi.Komisyon
            ? Math.Round(eslestirme.TaseronaOdenenUcret * puantajCarpani * servisCarpani, 2, MidpointRounding.AwayFromZero)
            : 0;
    }

    private static decimal GetServisTuruCarpani(ServisTuru servisTuru)
    {
        return servisTuru switch
        {
            ServisTuru.Sabah => 1m,
            ServisTuru.Aksam => 1m,
            ServisTuru.SabahAksam => 2m,
            ServisTuru.Ozel => 1.25m,
            ServisTuru.YardaMesai => 1.5m,
            _ => 1m
        };
    }
}


