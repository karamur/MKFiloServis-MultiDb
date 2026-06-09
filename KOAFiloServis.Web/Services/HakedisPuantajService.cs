using KOAFiloServis.Shared.Entities;
using KOAFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace KOAFiloServis.Web.Services;

public class HakedisPuantajService : IHakedisPuantajService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public HakedisPuantajService(IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    #region Hakediş Puantaj CRUD

    public async Task<List<HakedisPuantaj>> GetAllAsync(int? firmaId = null, int? yil = null, int? ay = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.HakedisPuantajlar
            .Include(h => h.Guzergah)
            .Include(h => h.Arac)
            .Include(h => h.Sofor)
            .Include(h => h.Cari)
            .Where(h => !h.IsDeleted);

        if (firmaId.HasValue) query = query.Where(h => h.FirmaId == firmaId);
        if (yil.HasValue) query = query.Where(h => h.Yil == yil);
        if (ay.HasValue) query = query.Where(h => h.Ay == ay);

        return await query.OrderByDescending(h => h.Yil).ThenByDescending(h => h.Ay).ToListAsync();
    }

    public async Task<HakedisPuantaj?> GetByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.HakedisPuantajlar
            .Include(h => h.Guzergah)
            .Include(h => h.Arac)
            .Include(h => h.Sofor)
            .Include(h => h.Cari)
            .Include(h => h.Detaylar.Where(d => !d.IsDeleted).OrderBy(d => d.Gun))
            .Include(h => h.Kesintiler.Where(k => !k.IsDeleted))
            .FirstOrDefaultAsync(h => h.Id == id && !h.IsDeleted);
    }

    public async Task<HakedisPuantaj> CreateAsync(HakedisPuantaj hakedis)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        // Güzergah varsayılanlarından al
        var guzergah = await context.Guzergahlar.FindAsync(hakedis.GuzergahId);
        if (guzergah != null)
        {
            // Fiyat yoksa güzergah fiyatını kullan
            if (hakedis.BirimFiyat == 0 && guzergah.BirimFiyat > 0)
                hakedis.BirimFiyat = guzergah.BirimFiyat;

            // Yön tipi güzergahtan al
            hakedis.YonTipi = guzergah.SeferTipi switch
            {
                SeferTipi.Sabah => YonTipi.Sabah,
                SeferTipi.Aksam => YonTipi.Aksam,
                SeferTipi.SabahAksam => YonTipi.SabahAksam,
                _ => YonTipi.SabahAksam
            };

            // Günlük sefer sayısı yön tipine göre
            hakedis.GunlukSeferSayisi = hakedis.YonTipi switch
            {
                YonTipi.Sabah => 1,
                YonTipi.Aksam => 1,
                YonTipi.SabahAksam => 2,
                _ => 2
            };
        }

        // Aynı dönemde aynı (Güzergah+Araç+Şoför) için mükerrer kontrol
        var mevcut = await context.HakedisPuantajlar
            .AnyAsync(h => h.Yil == hakedis.Yil && h.Ay == hakedis.Ay
                && h.GuzergahId == hakedis.GuzergahId
                && h.AracId == hakedis.AracId
                && h.SoforId == hakedis.SoforId
                && !h.IsDeleted);
        if (mevcut)
            throw new InvalidOperationException("Bu dönem için aynı güzergah/araç/şoför kombinasyonunda hakediş zaten var!");

        hakedis.Durum = HakedisDurumu.Taslak;
        hakedis.CreatedAt = DateTime.UtcNow;

        context.HakedisPuantajlar.Add(hakedis);
        await context.SaveChangesAsync();

        // Günlük detayları otomatik oluştur (ayın tüm günleri için 0 sefer)
        await GunlukDetayOlusturAsync(context, hakedis);

        return hakedis;
    }

    public async Task<HakedisPuantaj> UpdateAsync(HakedisPuantaj hakedis)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var existing = await context.HakedisPuantajlar
            .FirstOrDefaultAsync(h => h.Id == hakedis.Id && !h.IsDeleted);

        if (existing == null)
            throw new InvalidOperationException("Hakediş kaydı bulunamadı.");

        if (existing.Durum >= HakedisDurumu.Onaylandi)
            throw new InvalidOperationException("Onaylanmış hakediş güncellenemez.");

        existing.BirimFiyat = hakedis.BirimFiyat;
        existing.KdvOrani = hakedis.KdvOrani;
        existing.YonTipi = hakedis.YonTipi;
        existing.GunlukSeferSayisi = hakedis.GunlukSeferSayisi;
        existing.CariId = hakedis.CariId;
        existing.Aciklama = hakedis.Aciklama;
        existing.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
        return existing;
    }

    public async Task DeleteAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var hakedis = await context.HakedisPuantajlar.FindAsync(id);
        if (hakedis == null) return;

        if (hakedis.Durum >= HakedisDurumu.Onaylandi)
            throw new InvalidOperationException("Onaylanmış hakediş silinemez.");

        hakedis.IsDeleted = true;
        hakedis.DeletedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
    }

    #endregion

    #region Günlük Detay İşlemleri

    public async Task<List<HakedisPuantajDetay>> GetDetaylarAsync(int hakedisId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.HakedisPuantajDetaylar
            .Where(d => d.HakedisPuantajId == hakedisId && !d.IsDeleted)
            .OrderBy(d => d.Gun)
            .ToListAsync();
    }

    public async Task GunlukSeferGuncelleAsync(int hakedisId, int gun, int seferSayisi, bool ekSeferMi, string? aciklama = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var hakedis = await context.HakedisPuantajlar.FindAsync(hakedisId);
        if (hakedis == null) throw new InvalidOperationException("Hakediş bulunamadı.");
        if (hakedis.Durum >= HakedisDurumu.Onaylandi) throw new InvalidOperationException("Onaylanmış hakediş güncellenemez.");

        var detay = await context.HakedisPuantajDetaylar
            .FirstOrDefaultAsync(d => d.HakedisPuantajId == hakedisId && d.Gun == gun && !d.IsDeleted);

        if (detay == null)
        {
            detay = new HakedisPuantajDetay
            {
                HakedisPuantajId = hakedisId,
                Gun = gun,
                SeferSayisi = seferSayisi,
                EkSeferMi = ekSeferMi,
                Aciklama = aciklama,
                CreatedAt = DateTime.UtcNow
            };
            context.HakedisPuantajDetaylar.Add(detay);
        }
        else
        {
            detay.SeferSayisi = seferSayisi;
            detay.EkSeferMi = ekSeferMi;
            detay.Aciklama = aciklama;
            detay.UpdatedAt = DateTime.UtcNow;
        }

        await context.SaveChangesAsync();
        await HakedisHesaplaAsync(context, hakedis);
    }

    private async Task GunlukDetayOlusturAsync(ApplicationDbContext context, HakedisPuantaj hakedis)
    {
        var gunSayisi = DateTime.DaysInMonth(hakedis.Yil, hakedis.Ay);
        for (int gun = 1; gun <= gunSayisi; gun++)
        {
            context.HakedisPuantajDetaylar.Add(new HakedisPuantajDetay
            {
                HakedisPuantajId = hakedis.Id,
                Gun = gun,
                SeferSayisi = 0,
                EkSeferMi = false,
                CreatedAt = DateTime.UtcNow
            });
        }
        await context.SaveChangesAsync();
    }

    #endregion

    #region Kesinti İşlemleri

    public async Task<List<HakedisKesinti>> GetKesintilerAsync(int hakedisId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.HakedisKesintiler
            .Where(k => k.HakedisPuantajId == hakedisId && !k.IsDeleted)
            .ToListAsync();
    }

    public async Task KesintiEkleAsync(int hakedisId, string kesintiAdi, decimal tutar, string? aciklama = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var hakedis = await context.HakedisPuantajlar.FindAsync(hakedisId);
        if (hakedis == null) throw new InvalidOperationException("Hakediş bulunamadı.");
        if (hakedis.Durum >= HakedisDurumu.Onaylandi) throw new InvalidOperationException("Onaylanmış hakedişe kesinti eklenemez.");

        context.HakedisKesintiler.Add(new HakedisKesinti
        {
            HakedisPuantajId = hakedisId,
            KesintiAdi = kesintiAdi,
            Tutar = tutar,
            Aciklama = aciklama,
            CreatedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();
        await HakedisHesaplaAsync(context, hakedis);
    }

    public async Task KesintiSilAsync(int kesintiId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var kesinti = await context.HakedisKesintiler.FindAsync(kesintiId);
        if (kesinti == null) return;

        var hakedis = await context.HakedisPuantajlar.FindAsync(kesinti.HakedisPuantajId);
        if (hakedis?.Durum >= HakedisDurumu.Onaylandi)
            throw new InvalidOperationException("Onaylanmış hakedişten kesinti silinemez.");

        kesinti.IsDeleted = true;
        kesinti.DeletedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        if (hakedis != null)
            await HakedisHesaplaAsync(context, hakedis);
    }

    #endregion

    #region Hesaplama ve Onay

    public async Task<HakedisPuantaj> HakedisHesaplaAsync(int hakedisId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var hakedis = await context.HakedisPuantajlar
            .Include(h => h.Detaylar.Where(d => !d.IsDeleted))
            .Include(h => h.Kesintiler.Where(k => !k.IsDeleted))
            .FirstOrDefaultAsync(h => h.Id == hakedisId && !h.IsDeleted);

        if (hakedis == null) throw new InvalidOperationException("Hakediş bulunamadı.");
        return await HakedisHesaplaAsync(context, hakedis);
    }

    private async Task<HakedisPuantaj> HakedisHesaplaAsync(ApplicationDbContext context, HakedisPuantaj hakedis)
    {
        // Sefer başı birim fiyat
        var seferBirimFiyat = hakedis.GunlukSeferSayisi > 0
            ? hakedis.BirimFiyat / hakedis.GunlukSeferSayisi
            : 0;

        // Toplamları yeniden hesapla
        var detaylar = await context.HakedisPuantajDetaylar
            .Where(d => d.HakedisPuantajId == hakedis.Id && !d.IsDeleted)
            .ToListAsync();

        hakedis.ToplamSefer = detaylar.Sum(d => d.SeferSayisi);
        hakedis.GelirToplam = detaylar.Sum(d => d.SeferSayisi * hakedis.GelirSeferBirimFiyat * d.FiyatCarpani);
        hakedis.GiderToplam = detaylar.Sum(d => d.SeferSayisi * hakedis.GiderSeferBirimFiyat * d.FiyatCarpani);
        hakedis.KdvTutari = hakedis.GiderToplam * hakedis.KdvOrani / 100;

        var kesintiler = await context.HakedisKesintiler
            .Where(k => k.HakedisPuantajId == hakedis.Id && !k.IsDeleted)
            .ToListAsync();
        hakedis.ToplamKesinti = kesintiler.Sum(k => k.Tutar);

        hakedis.OdenecekTutar = hakedis.GiderToplam + hakedis.KdvTutari - hakedis.ToplamKesinti;
        hakedis.TahsilEdilecekTutar = hakedis.GelirToplam;
        hakedis.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
        return hakedis;
    }

    public async Task<HakedisPuantaj> OnayaGonderAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var hakedis = await context.HakedisPuantajlar.FindAsync(id);
        if (hakedis == null) throw new InvalidOperationException("Hakediş bulunamadı.");
        if (hakedis.Durum != HakedisDurumu.Taslak) throw new InvalidOperationException("Sadece taslak hakediş onaya gönderilebilir.");

        await HakedisHesaplaAsync(context, hakedis);
        hakedis.Durum = HakedisDurumu.OnayBekliyor;
        hakedis.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        return hakedis;
    }

    public async Task<HakedisPuantaj> OnaylaAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var hakedis = await context.HakedisPuantajlar.FindAsync(id);
        if (hakedis == null) throw new InvalidOperationException("Hakediş bulunamadı.");
        if (hakedis.Durum != HakedisDurumu.OnayBekliyor) throw new InvalidOperationException("Sadece onay bekleyen hakediş onaylanabilir.");

        hakedis.Durum = HakedisDurumu.Onaylandi;
        hakedis.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        return hakedis;
    }

    #endregion

    #region Dashboard

    public async Task<HakedisDashboard> GetDashboardAsync(int yil, int ay, int? firmaId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.HakedisPuantajlar
            .Where(h => h.Yil == yil && h.Ay == ay && !h.IsDeleted);

        if (firmaId.HasValue) query = query.Where(h => h.FirmaId == firmaId);

        var hakedisler = await query.ToListAsync();

        return new HakedisDashboard
        {
            ToplamHakedis = hakedisler.Sum(h => h.GiderToplam),
            ToplamSefer = hakedisler.Sum(h => h.ToplamSefer),
            ToplamGuzergah = hakedisler.Select(h => h.GuzergahId).Distinct().Count(),
            ToplamArac = hakedisler.Select(h => h.AracId).Distinct().Count(),
            ToplamSofor = hakedisler.Select(h => h.SoforId).Distinct().Count(),
            ToplamTedarikci = hakedisler.Select(h => h.CariId).Distinct().Count(),
            ToplamKesinti = hakedisler.Sum(h => h.ToplamKesinti),
            ToplamKDV = hakedisler.Sum(h => h.KdvTutari),
            ToplamOdeme = hakedisler.Sum(h => h.OdenecekTutar)
        };
    }

    #endregion
}

public class HakedisDashboard
{
    public decimal ToplamHakedis { get; set; }
    public int ToplamSefer { get; set; }
    public int ToplamGuzergah { get; set; }
    public int ToplamArac { get; set; }
    public int ToplamSofor { get; set; }
    public int ToplamTedarikci { get; set; }
    public decimal ToplamKesinti { get; set; }
    public decimal ToplamKDV { get; set; }
    public decimal ToplamOdeme { get; set; }
}
