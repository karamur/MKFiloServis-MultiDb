using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using MKFiloServis.Web.Services.Interfaces;
using MKFiloServis.Web.Models;
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
            .Include(e => e.MusteriCari)
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
            .Include(e => e.MusteriCari)
            .Include(e => e.Guzergah)
            .Include(e => e.Arac)
            .Include(e => e.Sofor)
            .Include(e => e.Kullanici)
            .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted);
    }

    public async Task<FiloGuzergahEslestirme> CreateEslestirmeAsync(FiloGuzergahEslestirme eslestirme)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        context.FiloGuzergahEslestirmeleri.Add(eslestirme);
        await context.SaveChangesAsync();
        return eslestirme;
    }

    public async Task<FiloGuzergahEslestirme> UpdateEslestirmeAsync(FiloGuzergahEslestirme eslestirme)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        // Garanti DB yazımı: doğrudan SQL UPDATE (tracking/audit akışını atlar).
        // IgnoreQueryFilters: global filtre Arac/Firma join'i içerdiğinden SQLite UPDATE'te
        // "no such column" hatasına yol açıyor; soft-delete kontrolü açıkça yapılıyor.
        var etkilenen = await context.FiloGuzergahEslestirmeleri
            .IgnoreQueryFilters()
            .Where(e => e.Id == eslestirme.Id && !e.IsDeleted)
            .ExecuteUpdateAsync(s => s
                .SetProperty(e => e.KurumFirmaId, eslestirme.KurumFirmaId)
                .SetProperty(e => e.GuzergahId, eslestirme.GuzergahId)
                .SetProperty(e => e.AracId, eslestirme.AracId)
                .SetProperty(e => e.SoforId, eslestirme.SoforId)
                .SetProperty(e => e.KullaniciId, eslestirme.KullaniciId)
                .SetProperty(e => e.ServisTuru, eslestirme.ServisTuru)
                .SetProperty(e => e.KurumaKesilecekUcret, eslestirme.KurumaKesilecekUcret)
                .SetProperty(e => e.TaseronaOdenenUcret, eslestirme.TaseronaOdenenUcret)
                .SetProperty(e => e.IsActive, eslestirme.IsActive)
                .SetProperty(e => e.UpdatedAt, DateTime.UtcNow));

        if (etkilenen == 0)
            throw new InvalidOperationException($"Eşleştirme (Id={eslestirme.Id}) veritabanında bulunamadı; güncelleme yapılamadı.");

        // Doğrulama: yazılan değerleri geri oku
        var guncel = await context.FiloGuzergahEslestirmeleri
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == eslestirme.Id);

        if (guncel == null ||
            guncel.KurumaKesilecekUcret != eslestirme.KurumaKesilecekUcret ||
            guncel.TaseronaOdenenUcret != eslestirme.TaseronaOdenenUcret ||
            guncel.AracId != eslestirme.AracId ||
            guncel.SoforId != eslestirme.SoforId ||
            guncel.GuzergahId != eslestirme.GuzergahId)
        {
            throw new InvalidOperationException("Eşleştirme değişiklikleri veritabanına yazılamadı (doğrulama başarısız).");
        }

        return guncel;
    }

    public async Task<bool> DeleteEslestirmeAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var existing = await context.FiloGuzergahEslestirmeleri.FindAsync(id);
        if (existing == null || existing.IsDeleted)
            return false;

        existing.IsDeleted = true;
        existing.UpdatedAt = DateTime.UtcNow;

        var bagliPuantajlar = await context.FiloGunlukPuantajlar
            .Where(p => p.FiloGuzergahEslestirmeId == id && !p.IsDeleted)
            .ToListAsync();

        foreach (var p in bagliPuantajlar)
        {
            p.IsDeleted = true;
            p.UpdatedAt = DateTime.UtcNow;
        }

        var etkilenen = await context.SaveChangesAsync();
        return etkilenen > 0;
    }

    public async Task TopluPuantajUretAsync(int firmaId, int yil, int ay)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        // 1. Ayın günlerini belirle
        var baslangic = new DateTime(yil, ay, 1);
        var bitis = baslangic.AddMonths(1).AddDays(-1);
        Console.WriteLine($"TopluPuantajUretAsync: Firma={firmaId}, Tarih={baslangic:yyyy-MM-dd} ~ {bitis:yyyy-MM-dd}");

        // 2. Halihazırda var olan o aya ait puantaj kayıtlarını al (mükerrer kayıt oluşmaması için)
        var mevcutPuantajlar = await context.FiloGunlukPuantajlar
            .Where(p => p.FirmaId == firmaId && p.Tarih >= baslangic && p.Tarih <= bitis && !p.IsDeleted)
            .ToListAsync();
        Console.WriteLine($"TopluPuantajUretAsync: Mevcut puantaj sayısı = {mevcutPuantajlar.Count}");

        // 3. Aktif eşleştirmeleri çek
        var aktifEslestirmeler = await GetEslestirmelerAsync(firmaId, sadeceAktifler: true);
        Console.WriteLine($"TopluPuantajUretAsync: Aktif eslestirme sayısı = {aktifEslestirmeler.Count}");

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
                        SeferSayisi = 0m,
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
            Console.WriteLine($"TopluPuantajUretAsync: {yeniKavitlar.Count} yeni puantaj ekleniyor");
            await context.FiloGunlukPuantajlar.AddRangeAsync(yeniKavitlar);
            var kayitlandi = await context.SaveChangesAsync();
            Console.WriteLine($"TopluPuantajUretAsync: {kayitlandi} kayit veritabanina yazildi");
        }
        else
        {
            Console.WriteLine($"TopluPuantajUretAsync: Yeni puantaj eklenmedi");
        }
    }

    public async Task<List<FiloGunlukPuantaj>> GetGunlukPuantajlarSiraliAsync(int firmaId, DateTime tarih)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var result = await context.FiloGunlukPuantajlar
            .Include(p => p.MusteriCari)
            .Include(p => p.Guzergah)
            .Include(p => p.Arac)
                .ThenInclude(a => a!.KiralikCari)
            .Include(p => p.Arac)
                .ThenInclude(a => a!.KomisyoncuCari)
            .Include(p => p.Arac)
                .ThenInclude(a => a!.TasimaTedarikci)
            .Include(p => p.Sofor)
            .Where(p => p.FirmaId == firmaId && p.Tarih.Date == tarih.Date && !p.IsDeleted)
            .OrderBy(p => p.MusteriCari!.Unvan)
            .ThenBy(p => p.Guzergah!.GuzergahAdi)
            .ToListAsync();
        Console.WriteLine($"GetGunlukPuantajlarSiraliAsync: Firma={firmaId}, Tarih={tarih:yyyy-MM-dd}, Toplam={result.Count} kayit");
        foreach(var item in result)
        {
            Console.WriteLine($"  - Id={item.Id}, Eslestirme={item.FiloGuzergahEslestirmeId}, SeferSayisi={item.SeferSayisi}, Arac={item.Arac?.Plaka}");
        }
        return result;
    }

    public Task<List<PuantajSatirDetayDto>> GetGunlukPuantajDetayliAsync(int firmaId, DateTime tarih)
        => GetPuantajDetayliByTarihAraligiAsync(firmaId, tarih.Date, tarih.Date);

    public async Task<List<PuantajSatirDetayDto>> GetPuantajDetayliByTarihAraligiAsync(int firmaId, DateTime baslangic, DateTime bitis)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var puantajlar = await context.FiloGunlukPuantajlar
            .Include(p => p.MusteriCari)
            .Include(p => p.Guzergah)
            .Include(p => p.Arac)
                .ThenInclude(a => a!.KiralikCari)
            .Include(p => p.Arac)
                .ThenInclude(a => a!.KomisyoncuCari)
            .Include(p => p.Arac)
                .ThenInclude(a => a!.TasimaTedarikci)
            .Include(p => p.Arac)
                .ThenInclude(a => a!.Firma)
            .Include(p => p.Sofor)
            .Include(p => p.EslestirmeSablonu)
            .Where(p => p.FirmaId == firmaId && p.Tarih.Date >= baslangic.Date && p.Tarih.Date <= bitis.Date && !p.IsDeleted)
            .OrderBy(p => p.MusteriCari!.Unvan)
            .ThenBy(p => p.Guzergah!.GuzergahAdi)
            .ToListAsync();

        var faturaIds = puantajlar
            .SelectMany(p => new[] { p.KurumFaturaId, p.TedarikciOdemeFaturaId })
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        var faturaTutarlari = faturaIds.Any()
            ? await context.Faturalar
                .Where(f => faturaIds.Contains(f.Id) && !f.IsDeleted)
                .ToDictionaryAsync(f => f.Id, f => f.GenelToplam)
            : new Dictionary<int, decimal>();

        return puantajlar.Select(p =>
        {
            var sahipAd = GetAracSahibiAd(p.Arac);
            var sahipTip = GetAracSahibiTip(p.Arac);

            var giden = p.KurumFaturaId.HasValue && faturaTutarlari.TryGetValue(p.KurumFaturaId.Value, out var gidenTutar)
                ? gidenTutar
                : 0m;
            var gelen = p.TedarikciOdemeFaturaId.HasValue && faturaTutarlari.TryGetValue(p.TedarikciOdemeFaturaId.Value, out var gelenTutar)
                ? gelenTutar
                : 0m;

            return new PuantajSatirDetayDto
            {
                Puantaj = p,
                AracSahibiAd = sahipAd,
                AracSahibiTip = sahipTip,
                GidenFaturaTutari = giden,
                GelenFaturaTutari = gelen
            };
        }).ToList();
    }

    public async Task<PuantajDonemOzetDto> GetPuantajDonemOzetiAsync(int firmaId, int yil, int ay)
    {
        var baslangic = new DateTime(yil, ay, 1);
        var bitis = baslangic.AddMonths(1);

        await using var context = await _contextFactory.CreateDbContextAsync();

        var query = context.FiloGunlukPuantajlar
            .AsNoTracking()
            .Where(p => p.FirmaId == firmaId && p.Tarih >= baslangic && p.Tarih < bitis && !p.IsDeleted);

        return new PuantajDonemOzetDto
        {
            KayitSayisi = await query.CountAsync(),
            ToplamSefer = await query.SumAsync(p => p.SeferSayisi),
            // Gelir/Gider güzergah kartındaki güncel fiyattan hesaplanır (Sefer × Fiyat).
            // Eski kayıtlarda kalan çarpanlı tahakkuk değerleri mutabakatı bozmasın diye
            // TahakkukEden* alanları yerine güzergah fiyatı kullanılır.
            ToplamGelir = await query.SumAsync(p => p.SeferSayisi * (p.Guzergah != null ? p.Guzergah.BirimFiyat : 0m)),
            ToplamGider = await query.SumAsync(p => p.SeferSayisi * (p.Guzergah != null ? p.Guzergah.GiderFiyat : 0m))
        };
    }

    public async Task<List<FiloGunlukPuantaj>> GetPuantajlarByTarihAraligiAsync(int? firmaId, DateTime baslangic, DateTime bitis, int? kurumId = null, int? aracId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.FiloGunlukPuantajlar
            .Include(p => p.MusteriCari)
            .Include(p => p.Guzergah)
            .Include(p => p.Arac)
                .ThenInclude(a => a!.KiralikCari)
            .Include(p => p.Arac)
                .ThenInclude(a => a!.KomisyoncuCari)
            .Include(p => p.Arac)
                .ThenInclude(a => a!.TasimaTedarikci)
            .Include(p => p.Sofor)
            .Where(p => p.Tarih >= baslangic && p.Tarih <= bitis && !p.IsDeleted);

        if (firmaId.HasValue && firmaId.Value > 0)
            query = query.Where(p => p.FirmaId == firmaId.Value);

        if (kurumId.HasValue && kurumId.Value > 0)
            query = query.Where(p => p.KurumFirmaId == kurumId.Value);

        if (aracId.HasValue && aracId.Value > 0)
            query = query.Where(p => p.AracId == aracId.Value);

        var result = await query.OrderBy(p => p.Tarih).ThenBy(p => p.MusteriCari!.Unvan).ToListAsync();
        Console.WriteLine($"GetPuantajlarByTarihAraligiAsync: Tarih={baslangic:yyyy-MM-dd} ~ {bitis:yyyy-MM-dd}, Firma={firmaId}, Toplam={result.Count} kayit");
        foreach(var item in result.GroupBy(x => x.Tarih.Date).Take(3))
        {
            Console.WriteLine($"  - Tarih={item.Key:yyyy-MM-dd}: {item.Count()} kayit");
        }
        return result;
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
        var existing = await context.FiloGunlukPuantajlar
            .FirstOrDefaultAsync(p => p.Id == puantaj.Id && !p.IsDeleted);

        if (existing == null)
            throw new InvalidOperationException($"Puantaj kaydı bulunamadı (Id={puantaj.Id}). Sayfayı yenileyip tekrar deneyin.");

        EnsurePuantajDegistirilebilir(existing);

        await MapAndApplyRulesAsync(context, existing, puantaj);
        // ChangeTracker'ı manuel set et
        context.Entry(existing).State = Microsoft.EntityFrameworkCore.EntityState.Modified;

        Console.WriteLine($"UpdateGunlukPuantajAsync: {existing.Id} kayıt güncellenecek, SeferSayisi={existing.SeferSayisi}");
        var etkilenen = await context.SaveChangesAsync();
        Console.WriteLine($"UpdateGunlukPuantajAsync: SaveChangesAsync {etkilenen} satır etkiledi.");

        if (etkilenen == 0)
            throw new InvalidOperationException("Puantaj güncellemesi veritabanına yazılamadı.");

        return existing;
    }

    public async Task UpdateGunlukPuantajlarAsync(List<FiloGunlukPuantaj> puantajlar)
    {
        if (puantajlar is null || puantajlar.Count == 0)
            return;

        Console.WriteLine($"UpdateGunlukPuantajlarAsync: {puantajlar.Count} kayit guncelleniyor");
        foreach (var p in puantajlar)
        {
            Console.WriteLine($"  - Id={p.Id}, SeferSayisi={p.SeferSayisi}, Tarih={p.Tarih:yyyy-MM-dd}");
        }

        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var ids = puantajlar.Select(x => x.Id).Distinct().ToList();
            var mevcutlar = await context.FiloGunlukPuantajlar
                .Where(x => ids.Contains(x.Id) && !x.IsDeleted)
                .ToListAsync();

            Console.WriteLine($"UpdateGunlukPuantajlarAsync: {mevcutlar.Count} mevcut kayit bulundu");

            if (!mevcutlar.Any())
                return;

            var mevcutById = mevcutlar.ToDictionary(x => x.Id);

            foreach (var gelen in puantajlar)
            {
                if (!mevcutById.TryGetValue(gelen.Id, out var existing))
                    continue;

                EnsurePuantajDegistirilebilir(existing);
                Console.WriteLine($"UpdateGunlukPuantajlarAsync: Id={gelen.Id}, Eski SeferSayisi={existing.SeferSayisi} -> Yeni SeferSayisi={gelen.SeferSayisi}");
                await MapAndApplyRulesAsync(context, existing, gelen);
                // ChangeTracker'ı manuel set et - ToList() ile detach olabilir
                context.Entry(existing).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
            }

            Console.WriteLine($"UpdateGunlukPuantajlarAsync: ChangeTracker entries: {context.ChangeTracker.Entries().Where(e => e.State == Microsoft.EntityFrameworkCore.EntityState.Modified).Count()} Modified");
            var etkilenen = await context.SaveChangesAsync();
            Console.WriteLine($"UpdateGunlukPuantajlarAsync: SaveChangesAsync {etkilenen} satır etkiledi.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"UpdateGunlukPuantajlarAsync hatası: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Dönemdeki tüm puantaj kayıtlarının tahakkuk tutarlarını güncel kurallara göre yeniden hesaplar.
    /// Eski çarpanlı (2x) tahakkuk değerlerini temizlemek için kullanılır.
    /// </summary>
    public async Task<int> TahakkuklariYenidenHesaplaAsync(int firmaId, int yil, int ay)
    {
        var baslangic = new DateTime(yil, ay, 1);
        var bitis = baslangic.AddMonths(1);

        await using var context = await _contextFactory.CreateDbContextAsync();
        var kayitlar = await context.FiloGunlukPuantajlar
            .Where(p => p.FirmaId == firmaId && p.Tarih >= baslangic && p.Tarih < bitis && !p.IsDeleted && !p.Onaylandi)
            .ToListAsync();

        foreach (var p in kayitlar)
        {
            await UygulaPuantajKurallariAsync(context, p);
            p.UpdatedAt = DateTime.UtcNow;
            context.Entry(p).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
        }

        var etkilenen = await context.SaveChangesAsync();
        Console.WriteLine($"TahakkuklariYenidenHesaplaAsync: {etkilenen} satır güncellendi ({yil}/{ay}).");
        return kayitlar.Count;
    }

    public async Task<int> DeleteGunlukPuantajlarAsync(List<int> puantajIds)
    {
        if (puantajIds is null || puantajIds.Count == 0)
            return 0;

        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var kayitlar = await context.FiloGunlukPuantajlar
                .Where(p => puantajIds.Contains(p.Id) && !p.IsDeleted)
                .ToListAsync();

            if (!kayitlar.Any())
            {
                Console.WriteLine($"DeleteGunlukPuantajlarAsync: İstenilen IDs'lerin hiçbiri bulunamadı. IDs: {string.Join(",", puantajIds)}");
                return 0;
            }

            Console.WriteLine($"DeleteGunlukPuantajlarAsync: {kayitlar.Count} kayıt siliniyor. IDs: {string.Join(",", kayitlar.Select(k => k.Id))}");

            foreach (var kayit in kayitlar)
            {
                EnsurePuantajDegistirilebilir(kayit);
                kayit.IsDeleted = true;
                kayit.UpdatedAt = DateTime.UtcNow;
                // ChangeTracker'ı manuel set et - ToList() ile detach olabilir
                context.Entry(kayit).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
            }

            Console.WriteLine($"DeleteGunlukPuantajlarAsync: ChangeTracker entries: {context.ChangeTracker.Entries().Where(e => e.State == Microsoft.EntityFrameworkCore.EntityState.Modified).Count()} Modified");
            var etkilenen = await context.SaveChangesAsync();
            Console.WriteLine($"DeleteGunlukPuantajlarAsync: SaveChangesAsync {etkilenen} satır etkiledi.");
            return etkilenen;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"DeleteGunlukPuantajlarAsync hatası: {ex.Message}");
            throw;
        }
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

        var query = context.Cariler
            .Where(c => !c.IsDeleted && (c.CariTipi == CariTipi.Musteri || c.CariTipi == CariTipi.MusteriTedarikci));

        if (firmaId > 0)
        {
            query = query.Where(c => c.FirmaId == firmaId);
        }

        var sonuc = await query.OrderBy(c => c.Unvan).ToListAsync();

        if (sonuc.Count == 0 && firmaId > 0)
        {
            sonuc = await context.Cariler
                .Where(c => !c.IsDeleted && (c.CariTipi == CariTipi.Musteri || c.CariTipi == CariTipi.MusteriTedarikci))
                .OrderBy(c => c.Unvan)
                .ToListAsync();
        }

        return sonuc;
    }

    public async Task<List<Sofor>> GetSoforlerAsync(int firmaId = 0)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.Soforler
            .Where(s => !s.IsDeleted && s.Aktif);
        if (firmaId > 0)
            query = query.Where(s => s.FirmaId == firmaId);
        var sonuc = await query.OrderBy(s => s.Ad).ThenBy(s => s.Soyad).ToListAsync();
        if (sonuc.Count == 0 && firmaId > 0)
        {
            sonuc = await context.Soforler
                .Where(s => !s.IsDeleted && s.Aktif)
                .OrderBy(s => s.Ad).ThenBy(s => s.Soyad)
                .ToListAsync();
        }
        return sonuc;
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

    private async Task MapAndApplyRulesAsync(ApplicationDbContext context, FiloGunlukPuantaj existing, FiloGunlukPuantaj source)
    {
        existing.Durum = source.Durum;
        existing.PuantajCarpani = source.PuantajCarpani;
        existing.SeferSayisi = source.SeferSayisi;
        existing.TahakkukEdenKurumUcreti = source.TahakkukEdenKurumUcreti;
        existing.TahakkukEdenTaseronUcreti = source.TahakkukEdenTaseronUcreti;
        existing.TaksiKullanildiMi = source.TaksiKullanildiMi;
        existing.TaksiFisTutari = source.TaksiFisTutari;
        existing.TaksiFisAciklama = source.TaksiFisAciklama;
        existing.ArizaYaptiMi = source.ArizaYaptiMi;
        existing.ArizaAciklamasi = source.ArizaAciklamasi;
        existing.Notlar = source.Notlar;

        await UygulaPuantajKurallariAsync(context, existing);
        existing.UpdatedAt = DateTime.UtcNow;
    }

    private static void EnsurePuantajDegistirilebilir(FiloGunlukPuantaj puantaj)
    {
        if (puantaj.Onaylandi || puantaj.KurumFaturaKesildiMi || puantaj.TaseronOdemeYapildiMi ||
            puantaj.KurumFaturaId.HasValue || puantaj.TedarikciOdemeFaturaId.HasValue)
        {
            throw new InvalidOperationException(
                $"Puantaj kaydı onaylandığı veya finansal belgeye bağlandığı için değiştirilemez (Id={puantaj.Id}).");
        }
    }

    private async Task UygulaPuantajKurallariAsync(ApplicationDbContext context, FiloGunlukPuantaj puantaj, FiloGuzergahEslestirme? eslestirme = null)
    {
        eslestirme ??= puantaj.FiloGuzergahEslestirmeId.HasValue
            ? await context.FiloGuzergahEslestirmeleri
                .Include(e => e.Arac)
                .Include(e => e.Guzergah)
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
        var seferSayisi = puantaj.SeferSayisi < 0 ? 0 : puantaj.SeferSayisi;
        var servisCarpani = GetServisTuruCarpani(puantaj.ServisTuru);

        // Öncelik: güzergah üzerinde güncel gelir/gider fiyatı. Yoksa eşleştirme fiyatına geri düş.
        var gelirBirim = eslestirme.Guzergah?.GelirFiyat > 0
            ? eslestirme.Guzergah.GelirFiyat
            : eslestirme.KurumaKesilecekUcret;

        var giderBirim = eslestirme.Guzergah?.GiderFiyat > 0
            ? eslestirme.Guzergah.GiderFiyat
            : eslestirme.TaseronaOdenenUcret;

        if (gelirBirim < 0) gelirBirim = 0;
        if (giderBirim < 0) giderBirim = 0;

        puantaj.TahakkukEdenKurumUcreti = Math.Round(
            gelirBirim * seferSayisi * puantajCarpani * servisCarpani,
            2,
            MidpointRounding.AwayFromZero);

        puantaj.TahakkukEdenTaseronUcreti = eslestirme.Arac.SahiplikTipi is AracSahiplikTipi.Komisyon or AracSahiplikTipi.Tedarikci
            ? Math.Round(giderBirim * seferSayisi * puantajCarpani * servisCarpani, 2, MidpointRounding.AwayFromZero)
            : 0;

        // Özmal / Kiralık araçlarda, ilgili dönem snapshot varsa sefer başı maliyeti puantaja yansıt.
        if (eslestirme.Arac.SahiplikTipi is AracSahiplikTipi.Ozmal or AracSahiplikTipi.Kiralik)
        {
            var snap = await context.AracMaliyetSnapshotlari
                .AsNoTracking()
                .Where(s => s.AracId == puantaj.AracId && s.Yil == puantaj.Tarih.Year && s.Ay == puantaj.Tarih.Month)
                .Select(s => new { s.ToplamSefer, ToplamMaliyet = s.ToplamMaliyet })
                .FirstOrDefaultAsync();

            puantaj.MaliyetOzmalKiralik = snap != null && snap.ToplamSefer > 0
                ? Math.Round(snap.ToplamMaliyet / snap.ToplamSefer, 2, MidpointRounding.AwayFromZero)
                : null;
        }
        else
        {
            puantaj.MaliyetOzmalKiralik = null;
        }
    }

    private static decimal GetServisTuruCarpani(ServisTuru servisTuru)
    {
        // Tutar = Sefer × Güzergah Fiyatı olmalı; servis türü kaynaklı ek çarpan uygulanmaz.
        // (SabahAksam için 2x çarpan, tutarların 2 kat fazla çıkmasına neden oluyordu.)
        return 1m;
    }

    private static string GetAracSahibiAd(Arac? arac)
    {
        if (arac is null)
            return "Bilinmeyen Sahip";

        if (arac.SahiplikTipi == AracSahiplikTipi.Kiralik && !string.IsNullOrWhiteSpace(arac.KiralikCari?.Unvan))
            return arac.KiralikCari.Unvan;

        if (arac.SahiplikTipi == AracSahiplikTipi.Komisyon && !string.IsNullOrWhiteSpace(arac.KomisyoncuCari?.Unvan))
            return arac.KomisyoncuCari.Unvan;

        if (arac.SahiplikTipi == AracSahiplikTipi.Tedarikci && !string.IsNullOrWhiteSpace(arac.TasimaTedarikci?.Unvan))
            return arac.TasimaTedarikci.Unvan;

        if (!string.IsNullOrWhiteSpace(arac.Firma?.FirmaAdi))
            return arac.Firma.FirmaAdi;

        return "Bilinmeyen Sahip";
    }

    private static string GetAracSahibiTip(Arac? arac)
    {
        return arac?.SahiplikTipi switch
        {
            AracSahiplikTipi.Kiralik => "Kiralık Cari",
            AracSahiplikTipi.Komisyon => "Komisyoncu Cari",
            AracSahiplikTipi.Tedarikci => "Tedarikçi",
            AracSahiplikTipi.Ozmal => "Firma Özmalı",
            AracSahiplikTipi.Diger => "Diğer",
            _ => "Bilinmeyen"
        };
    }
}


