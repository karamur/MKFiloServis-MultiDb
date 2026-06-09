using KOAFiloServis.Shared.Entities;
using KOAFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KOAFiloServis.Web.Services;

public class HakedisPuantajService : IHakedisPuantajService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IAktifFirmaProvider _aktifFirmaProvider;
    private readonly ILogger<HakedisPuantajService> _logger;

    public HakedisPuantajService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IAktifFirmaProvider aktifFirmaProvider,
        ILogger<HakedisPuantajService> logger)
    {
        _contextFactory = contextFactory;
        _aktifFirmaProvider = aktifFirmaProvider;
        _logger = logger;
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
        try
        {
            // ── VALIDATION ──
            if (hakedis.Yil < 2020 || hakedis.Yil > 2100)
                throw new InvalidOperationException("Geçersiz yıl.");
            if (hakedis.Ay < 1 || hakedis.Ay > 12)
                throw new InvalidOperationException("Geçersiz ay.");
            if (hakedis.GuzergahId <= 0)
                throw new InvalidOperationException("Güzergah seçimi zorunludur.");
            if (hakedis.AracId <= 0)
                throw new InvalidOperationException("Araç seçimi zorunludur.");
            if (hakedis.SoforId <= 0)
                throw new InvalidOperationException("Şoför seçimi zorunludur.");
            if (hakedis.CariId <= 0)
                throw new InvalidOperationException("Tedarikçi seçimi zorunludur.");
            if (hakedis.GelirBirimFiyat < 0 || hakedis.GiderBirimFiyat < 0)
                throw new InvalidOperationException("Birim fiyat negatif olamaz.");

            await using var context = await _contextFactory.CreateDbContextAsync();

            var aktifFirmaId = _aktifFirmaProvider.AktifFirmaId;
            if (!aktifFirmaId.HasValue || aktifFirmaId.Value <= 0)
                throw new InvalidOperationException("Aktif firma seçilmeden hakediş oluşturulamaz.");

            hakedis.FirmaId = aktifFirmaId.Value;

            _logger.LogInformation("Hakediş Create başladı. FirmaId={FirmaId}, Yil={Yil}, Ay={Ay}, GuzergahId={GuzergahId}, AracId={AracId}, SoforId={SoforId}, CariId={CariId}",
                hakedis.FirmaId, hakedis.Yil, hakedis.Ay, hakedis.GuzergahId, hakedis.AracId, hakedis.SoforId, hakedis.CariId);

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
                    && h.FirmaId == hakedis.FirmaId
                    && !h.IsDeleted);
            if (mevcut)
                throw new InvalidOperationException("Bu dönem için aynı güzergah/araç/şoför kombinasyonunda hakediş zaten var!");

            hakedis.Durum = HakedisDurumu.Taslak;
            hakedis.CreatedAt = DateTime.UtcNow;
            hakedis.IsDeleted = false;

            context.HakedisPuantajlar.Add(hakedis);
            _logger.LogInformation("Hakediş Add edildi. State={State}", context.Entry(hakedis).State);

            var affected = await context.SaveChangesAsync();
            _logger.LogInformation("Hakediş SaveChanges tamamlandı. Affected={Affected}, Id={Id}", affected, hakedis.Id);

            if (affected <= 0 || hakedis.Id <= 0)
                throw new InvalidOperationException("Hakediş ana kaydı database'e yazılamadı.");

            // Günlük detayları otomatik oluştur (ayın tüm günleri için 0 sefer)
            await GunlukDetayOlusturAsync(context, hakedis);

            var kayitDbdeVarMi = await context.HakedisPuantajlar
                .AsNoTracking()
                .AnyAsync(h => h.Id == hakedis.Id && !h.IsDeleted);

            if (!kayitDbdeVarMi)
                throw new InvalidOperationException("Hakediş kaydı SaveChanges sonrası DB'de bulunamadı.");

            return hakedis;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Hakediş puantaj create hatası. GuzergahId={GuzergahId}, AracId={AracId}, SoforId={SoforId}, CariId={CariId}",
                hakedis.GuzergahId, hakedis.AracId, hakedis.SoforId, hakedis.CariId);
            throw;
        }
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

    public async Task GunlukSeferGuncelleAsync(int hakedisId, int gun, int seferSayisi, int? seferTuruId, decimal fiyatCarpani, bool mesaiMi, bool ekSeferMi, string? aciklama = null)
    {
        if (gun < 1 || gun > 31) throw new InvalidOperationException("Geçersiz gün.");
        if (seferSayisi < 0) throw new InvalidOperationException("Sefer sayısı negatif olamaz.");
        if (fiyatCarpani <= 0) throw new InvalidOperationException("Fiyat çarpanı sıfır veya negatif olamaz.");

        await using var context = await _contextFactory.CreateDbContextAsync();
        var hakedis = await context.HakedisPuantajlar.FindAsync(hakedisId);
        if (hakedis == null) throw new InvalidOperationException("Hakediş bulunamadı.");
        if (hakedis.Durum >= HakedisDurumu.Onaylandi) throw new InvalidOperationException("Onaylanmış hakediş güncellenemez.");

        _logger.LogInformation("Detay kaydediliyor. HakedisId={HakedisId}, Gun={Gun}, SeferTuruId={SeferTuruId}, SeferSayisi={SeferSayisi}",
            hakedisId, gun, seferTuruId, seferSayisi);

        var detay = await context.HakedisPuantajDetaylar
            .FirstOrDefaultAsync(d => d.HakedisPuantajId == hakedisId && d.Gun == gun && !d.IsDeleted);

        if (detay == null)
        {
            detay = new HakedisPuantajDetay
            {
                HakedisPuantajId = hakedisId,
                Gun = gun,
                SeferSayisi = seferSayisi,
                SeferTuruId = seferTuruId,
                FiyatCarpani = fiyatCarpani,
                MesaiMi = mesaiMi,
                EkSeferMi = ekSeferMi,
                Aciklama = aciklama,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };
            context.HakedisPuantajDetaylar.Add(detay);
        }
        else
        {
            detay.SeferSayisi = seferSayisi;
            detay.SeferTuruId = seferTuruId;
            detay.FiyatCarpani = fiyatCarpani;
            detay.MesaiMi = mesaiMi;
            detay.EkSeferMi = ekSeferMi;
            detay.Aciklama = aciklama;
            detay.UpdatedAt = DateTime.UtcNow;
        }

        var affected = await context.SaveChangesAsync();
        if (affected <= 0)
            throw new InvalidOperationException("Günlük sefer detayı database'e yazılamadı.");

        var detayDb = await context.HakedisPuantajDetaylar
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.HakedisPuantajId == hakedisId && d.Gun == gun && !d.IsDeleted);

        if (detayDb == null)
            throw new InvalidOperationException("Günlük sefer detayı SaveChanges sonrası DB'de bulunamadı.");

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
        _logger.LogInformation("Hakediş hesaplama başladı. HakedisId={HakedisId}", hakedis.Id);

        // Toplamları yeniden hesapla
        var detaylar = await context.HakedisPuantajDetaylar
            .Where(d => d.HakedisPuantajId == hakedis.Id && !d.IsDeleted)
            .ToListAsync();

        hakedis.ToplamSefer = detaylar.Sum(d => d.SeferSayisi);
        hakedis.GelirToplam = detaylar.Sum(d => d.SeferSayisi * hakedis.GelirSeferBirimFiyat * d.FiyatCarpani);
        hakedis.GiderToplam = detaylar.Sum(d => d.SeferSayisi * hakedis.GiderSeferBirimFiyat * d.FiyatCarpani);
        hakedis.KdvTutari = hakedis.GiderToplam * hakedis.KdvOrani / 100;
        hakedis.GelirKdvTutari = hakedis.GelirToplam * hakedis.KdvOrani / 100;

        var kesintiler = await context.HakedisKesintiler
            .Where(k => k.HakedisPuantajId == hakedis.Id && !k.IsDeleted)
            .ToListAsync();
        hakedis.ToplamKesinti = kesintiler.Sum(k => k.Tutar);

        hakedis.OdenecekTutar = hakedis.GiderToplam + hakedis.KdvTutari - hakedis.ToplamKesinti;
        hakedis.TahsilEdilecekTutar = hakedis.GelirToplam + hakedis.GelirKdvTutari;
        hakedis.UpdatedAt = DateTime.UtcNow;

        var affected = await context.SaveChangesAsync();
        if (affected <= 0)
            throw new InvalidOperationException($"Hakediş hesaplama sonuçları DB'ye yazılamadı. HakedisId={hakedis.Id}");

        var kontrol = await context.HakedisPuantajlar
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.Id == hakedis.Id && !h.IsDeleted);

        if (kontrol == null)
            throw new InvalidOperationException($"Hakediş hesaplama sonrası kayıt DB'de bulunamadı. HakedisId={hakedis.Id}");

        _logger.LogInformation("Hakediş hesaplama tamamlandı. HakedisId={HakedisId}, ToplamSefer={ToplamSefer}, GiderToplam={GiderToplam}",
            hakedis.Id, kontrol.ToplamSefer, kontrol.GiderToplam);

        return hakedis;
    }

    public async Task<HakedisPuantaj> OnayaGonderAsync(int id)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var hakedis = await context.HakedisPuantajlar.FindAsync(id);
            if (hakedis == null) throw new InvalidOperationException("Hakediş bulunamadı.");
            if (hakedis.Durum != HakedisDurumu.Taslak) throw new InvalidOperationException("Sadece taslak hakediş onaya gönderilebilir.");

            await HakedisHesaplaAsync(context, hakedis);
            hakedis.Durum = HakedisDurumu.OnayBekliyor;
            hakedis.UpdatedAt = DateTime.UtcNow;

            var affected = await context.SaveChangesAsync();
            if (affected <= 0)
                throw new InvalidOperationException($"Hakediş onaya gönderme DB'ye yazılamadı. HakedisId={id}");

            var kontrol = await context.HakedisPuantajlar
                .AsNoTracking()
                .FirstOrDefaultAsync(h => h.Id == id && !h.IsDeleted);

            if (kontrol?.Durum != HakedisDurumu.OnayBekliyor)
                throw new InvalidOperationException($"Hakediş durumu OnayBekliyor olarak doğrulanamadı. HakedisId={id}");

            _logger.LogInformation("Hakediş onaya gönderildi. HakedisId={HakedisId}", id);
            return hakedis;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Hakediş onaya gönderme hatası. HakedisId={HakedisId}", id);
            throw;
        }
    }

    public async Task<HakedisPuantaj> OnaylaAsync(int id)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var hakedis = await context.HakedisPuantajlar.FindAsync(id);
            if (hakedis == null) throw new InvalidOperationException("Hakediş bulunamadı.");
            if (hakedis.Durum != HakedisDurumu.OnayBekliyor) throw new InvalidOperationException("Sadece onay bekleyen hakediş onaylanabilir.");

            hakedis.Durum = HakedisDurumu.Onaylandi;
            hakedis.UpdatedAt = DateTime.UtcNow;

            var affected = await context.SaveChangesAsync();
            if (affected <= 0)
                throw new InvalidOperationException($"Hakediş onaylama DB'ye yazılamadı. HakedisId={id}");

            var kontrol = await context.HakedisPuantajlar
                .AsNoTracking()
                .FirstOrDefaultAsync(h => h.Id == id && !h.IsDeleted);

            if (kontrol?.Durum != HakedisDurumu.Onaylandi)
                throw new InvalidOperationException($"Hakediş durumu Onaylandi olarak doğrulanamadı. HakedisId={id}");

            _logger.LogInformation("Hakediş onaylandı. HakedisId={HakedisId}", id);
            return hakedis;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Hakediş onaylama hatası. HakedisId={HakedisId}", id);
            throw;
        }
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
