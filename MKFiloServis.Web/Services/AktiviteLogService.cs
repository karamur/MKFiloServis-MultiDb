using System.Text.Json;
using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Web.Services;

public class AktiviteLogService : IAktiviteLogService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AppAuthenticationStateProvider _authenticationStateProvider;
    private readonly ILogger<AktiviteLogService> _logger;

    public AktiviteLogService(
        IDbContextFactory<ApplicationDbContext> dbContextFactory,
        IHttpContextAccessor httpContextAccessor,
        AppAuthenticationStateProvider authenticationStateProvider,
        ILogger<AktiviteLogService> logger)
    {

        _dbContextFactory = dbContextFactory;
        _httpContextAccessor = httpContextAccessor;
        _authenticationStateProvider = authenticationStateProvider;
        _logger = logger;
    }

    public async Task LogAsync(string islemTipi, string modul, string? aciklama = null,
        string? entityTipi = null, int? entityId = null, string? entityAdi = null,
        string? eskiDeger = null, string? yeniDeger = null,
        AktiviteSeviye seviye = AktiviteSeviye.Bilgi)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var aktifKullanici = _authenticationStateProvider.GetAktifKullanici();
            var kullaniciAdi = !string.IsNullOrWhiteSpace(aktifKullanici?.AdSoyad)
                ? aktifKullanici!.AdSoyad
                : !string.IsNullOrWhiteSpace(aktifKullanici?.KullaniciAdi)
                    ? aktifKullanici!.KullaniciAdi
                    : GetCurrentUserName(httpContext?.User);

            var log = new AktiviteLog
            {
                IslemZamani = DateTime.Now,
                IslemTipi = islemTipi,
                Modul = modul,
                EntityTipi = entityTipi,
                EntityId = entityId,
                EntityAdi = entityAdi,
                Aciklama = aciklama,
                EskiDeger = eskiDeger,
                YeniDeger = yeniDeger,
                Seviye = seviye,
                KullaniciAdi = kullaniciAdi,
                IpAdresi = httpContext?.Connection?.RemoteIpAddress?.ToString(),
                Tarayici = httpContext?.Request?.Headers["User-Agent"].ToString()
            };

            await using var logContext = await _dbContextFactory.CreateDbContextAsync();
            logContext.AktiviteLoglar.Add(log);
            await logContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Aktivite log kaydı hatası");
        }
    }

    public async Task LogEklemeAsync(string modul, string entityTipi, int entityId, string entityAdi)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        await LogAsync("Ekleme", modul, 
            aciklama: $"{entityTipi} eklendi: {entityAdi}",
            entityTipi: entityTipi, entityId: entityId, entityAdi: entityAdi);
    }

    public async Task LogGuncellemeAsync(string modul, string entityTipi, int entityId, string entityAdi, 
        object? eskiDeger = null, object? yeniDeger = null)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        string? eskiJson = eskiDeger != null ? JsonSerializer.Serialize(eskiDeger) : null;
        string? yeniJson = yeniDeger != null ? JsonSerializer.Serialize(yeniDeger) : null;

        await LogAsync("Güncelleme", modul,
            aciklama: $"{entityTipi} güncellendi: {entityAdi}",
            entityTipi: entityTipi, entityId: entityId, entityAdi: entityAdi,
            eskiDeger: eskiJson, yeniDeger: yeniJson);
    }

    public async Task LogSilmeAsync(string modul, string entityTipi, int entityId, string entityAdi)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        await LogAsync("Silme", modul,
            aciklama: $"{entityTipi} silindi: {entityAdi}",
            entityTipi: entityTipi, entityId: entityId, entityAdi: entityAdi,
            seviye: AktiviteSeviye.Uyari);
    }

    public async Task LogGoruntulemeAsync(string modul, string entityTipi, int entityId, string entityAdi, string? aciklama = null)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        await LogAsync("Görüntüleme", modul,
            aciklama: aciklama ?? $"{entityTipi} görüntülendi: {entityAdi}",
            entityTipi: entityTipi, entityId: entityId, entityAdi: entityAdi);
    }

    public async Task LogHataAsync(string modul, string aciklama, Exception? ex = null)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var detay = ex != null ? $"{aciklama} - Hata: {ex.Message}" : aciklama;
        await LogAsync("Hata", modul, aciklama: detay, seviye: AktiviteSeviye.Hata);
    }

    public async Task<List<AktiviteLogItem>> GetLogsAsync(AktiviteLogFilter? filter = null)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var query = context.AktiviteLoglar.AsQueryable();

        if (filter != null)
        {
            if (filter.BaslangicTarihi.HasValue)
                query = query.Where(l => l.IslemZamani >= filter.BaslangicTarihi.Value);

            if (filter.BitisTarihi.HasValue)
                query = query.Where(l => l.IslemZamani <= filter.BitisTarihi.Value.AddDays(1));

            if (!string.IsNullOrEmpty(filter.Modul))
                query = query.Where(l => l.Modul == filter.Modul);

            if (!string.IsNullOrEmpty(filter.IslemTipi))
                query = query.Where(l => l.IslemTipi == filter.IslemTipi);

            if (filter.Seviye.HasValue)
                query = query.Where(l => l.Seviye == filter.Seviye.Value);

            if (!string.IsNullOrEmpty(filter.AramaMetni))
                query = query.Where(l => 
                    (l.Aciklama != null && l.Aciklama.Contains(filter.AramaMetni)) ||
                    (l.EntityAdi != null && l.EntityAdi.Contains(filter.AramaMetni)));

            if (!string.IsNullOrEmpty(filter.KullaniciAdi))
                query = query.Where(l => l.KullaniciAdi != null && l.KullaniciAdi.Contains(filter.KullaniciAdi));

            if (!string.IsNullOrEmpty(filter.EntityTipi))
                query = query.Where(l => l.EntityTipi != null && l.EntityTipi.Contains(filter.EntityTipi));
        }

        var skip = ((filter?.Sayfa ?? 1) - 1) * (filter?.SayfaBoyutu ?? 50);

        return await query
            .OrderByDescending(l => l.IslemZamani)
            .Skip(skip)
            .Take(filter?.SayfaBoyutu ?? 50)
            .Select(l => new AktiviteLogItem
            {
                Id = l.Id,
                IslemZamani = l.IslemZamani,
                IslemTipi = l.IslemTipi,
                Modul = l.Modul,
                EntityTipi = l.EntityTipi,
                EntityId = l.EntityId,
                EntityAdi = l.EntityAdi,
                Aciklama = l.Aciklama,
                Seviye = l.Seviye,
                KullaniciAdi = l.KullaniciAdi
            })
            .ToListAsync();
    }

    public async Task<AktiviteLogDetay?> GetLogByIdAsync(int id)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        return await context.AktiviteLoglar
            .Where(l => l.Id == id)
            .Select(l => new AktiviteLogDetay
            {
                Id = l.Id,
                IslemZamani = l.IslemZamani,
                IslemTipi = l.IslemTipi,
                Modul = l.Modul,
                EntityTipi = l.EntityTipi,
                EntityId = l.EntityId,
                EntityAdi = l.EntityAdi,
                Aciklama = l.Aciklama,
                EskiDeger = l.EskiDeger,
                YeniDeger = l.YeniDeger,
                IpAdresi = l.IpAdresi,
                Tarayici = l.Tarayici,
                KullaniciAdi = l.KullaniciAdi,
                Seviye = l.Seviye
            })
            .FirstOrDefaultAsync();
    }

    public async Task<AktiviteLogOzet> GetOzetAsync(int gunSayisi = 7)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var baslangic = DateTime.Today.AddDays(-gunSayisi);
        var bugun = DateTime.Today;

        var logs = await context.AktiviteLoglar
            .Where(l => l.IslemZamani >= baslangic)
            .ToListAsync();

        var ozet = new AktiviteLogOzet
        {
            ToplamLog = logs.Count,
            BugunLog = logs.Count(l => l.IslemZamani.Date == bugun),
            EklemeAdet = logs.Count(l => l.IslemTipi == "Ekleme"),
            GuncellemeAdet = logs.Count(l => l.IslemTipi == "Güncelleme"),
            SilmeAdet = logs.Count(l => l.IslemTipi == "Silme"),
            HataAdet = logs.Count(l => l.IslemTipi == "Hata" || l.Seviye == AktiviteSeviye.Hata)
        };

        // Modül aktiviteleri
        ozet.ModulAktiviteleri = logs
            .GroupBy(l => l.Modul)
            .Select(g => new ModulAktivite { Modul = g.Key, Adet = g.Count() })
            .OrderByDescending(m => m.Adet)
            .Take(10)
            .ToList();

        // Günlük aktiviteler
        for (int i = gunSayisi; i >= 0; i--)
        {
            var tarih = DateTime.Today.AddDays(-i);
            ozet.GunlukAktiviteler.Add(new GunlukAktivite
            {
                Tarih = tarih,
                Adet = logs.Count(l => l.IslemZamani.Date == tarih)
            });
        }

        return ozet;
    }

    public async Task<int> GetLogCountAsync(DateTime? baslangic = null, DateTime? bitis = null)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var query = context.AktiviteLoglar.AsQueryable();

        if (baslangic.HasValue)
            query = query.Where(l => l.IslemZamani >= baslangic.Value);

        if (bitis.HasValue)
            query = query.Where(l => l.IslemZamani <= bitis.Value);

        return await query.CountAsync();
    }

    public async Task CleanupOldLogsAsync(int gunSakla = 90)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        // Loglar kesinlikle silinmez - sadece arşivleme yapılabilir
        // Bu metod artık hiçbir şey silmiyor
        _logger.LogInformation("Log silme devre dışı - loglar korunuyor.");
        await Task.CompletedTask;
    }

    public bool GeriAlinabilirMi(AktiviteLogDetay? log)
    {
        if (log == null) return false;

        // Ekleme, Güncelleme, Silme ve Geri Alma işlemleri geri alınabilir
        var geriAlinabilirIslemler = new[] { "Ekleme", "Güncelleme", "Silme", "Geri Alma" };
        if (!geriAlinabilirIslemler.Contains(log.IslemTipi))
            return false;

        // Entity tipi ve ID olmalı
        if (string.IsNullOrEmpty(log.EntityTipi) || !log.EntityId.HasValue)
            return false;

        // Silme ve Güncelleme işlemi için eski değer olmalı
        if ((log.IslemTipi == "Silme" || log.IslemTipi == "Güncelleme") && string.IsNullOrEmpty(log.EskiDeger))
            return false;

        // Geri Alma işlemi için eski değer olmalı (tekrar geri alınabilmesi için)
        if (log.IslemTipi == "Geri Alma" && string.IsNullOrEmpty(log.EskiDeger))
            return false;

        // Desteklenen entity tipleri
        var desteklenenTipler = new[] { 
            "Cari", "Arac", "Sofor", "Fatura", "FaturaKalem", "BankaHesap", "BankaKasaHareket",
            "Guzergah", "MasrafKalemi", "ServisCalisma", "AracMasraf", "BudgetOdeme", 
            "TekrarlayanOdeme", "BudgetMasrafKalemi", "Hatirlatici", "Bildirim",
            "StokKarti", "StokHareket", "MuhasebeHesap", "MuhasebeFis", "Firma", "Kullanici"
        };

        return desteklenenTipler.Contains(log.EntityTipi);
    }

    public async Task<GeriAlmaSonuc> GeriAlAsync(int logId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        try
        {
            var log = await context.AktiviteLoglar.FindAsync(logId);
            if (log == null)
                return new GeriAlmaSonuc { Basarili = false, Mesaj = "Log kaydı bulunamadı." };

            var logDetay = new AktiviteLogDetay
            {
                Id = log.Id,
                IslemTipi = log.IslemTipi,
                EntityTipi = log.EntityTipi,
                EntityId = log.EntityId,
                EskiDeger = log.EskiDeger,
                YeniDeger = log.YeniDeger,
                Modul = log.Modul,
                EntityAdi = log.EntityAdi
            };

            if (!GeriAlinabilirMi(logDetay))
                return new GeriAlmaSonuc { Basarili = false, Mesaj = "Bu işlem geri alınamaz." };

            var sonuc = log.IslemTipi switch
            {
                "Ekleme" => await GeriAlEklemeAsync(context, log),
                "Güncelleme" => await GeriAlGuncellemeAsync(context, log),
                "Silme" => await GeriAlSilmeAsync(context, log),
                "Geri Alma" => await GeriAlGeriAlmaAsync(context, log), // Geri almayı geri al = eski haline getir
                _ => new GeriAlmaSonuc { Basarili = false, Mesaj = "Desteklenmeyen işlem tipi." }
            };

            if (sonuc.Basarili)
            {
                // Geri alma işlemini de logla - eski ve yeni değerlerle birlikte (geri alınabilir olması için)
                await LogAsync("Geri Alma", log.Modul,
                    aciklama: $"{log.IslemTipi} işlemi geri alındı: {log.EntityAdi}",
                    entityTipi: log.EntityTipi, entityId: log.EntityId, entityAdi: log.EntityAdi,
                    eskiDeger: log.YeniDeger, // Geri almadan önceki durum = orijinal işlemin yeni değeri
                    yeniDeger: log.EskiDeger, // Geri aldıktan sonraki durum = orijinal işlemin eski değeri
                    seviye: AktiviteSeviye.Uyari);

                sonuc.OrijinalLogId = logId;
            }

            return sonuc;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Geri alma hatası: LogId={LogId}", logId);
            return new GeriAlmaSonuc { Basarili = false, Mesaj = $"Geri alma hatası: {ex.Message}" };
        }
    }

    private async Task<GeriAlmaSonuc> GeriAlEklemeAsync(ApplicationDbContext context, AktiviteLog log)
    {
        // Ekleme işlemini geri almak = kaydı silmek
        if (!log.EntityId.HasValue || string.IsNullOrEmpty(log.EntityTipi))
            return new GeriAlmaSonuc { Basarili = false, Mesaj = "Entity bilgisi eksik." };

        var entity = await GetEntityByTypeAndIdAsync(context, log.EntityTipi, log.EntityId.Value);
        if (entity == null)
            return new GeriAlmaSonuc { Basarili = false, Mesaj = "Kayıt bulunamadı, zaten silinmiş olabilir." };

        // Soft delete yap
        if (entity is BaseEntity baseEntity)
        {
            baseEntity.IsDeleted = true;
            baseEntity.UpdatedAt = DateTime.UtcNow;
        }

        await context.SaveChangesAsync();

        return new GeriAlmaSonuc
        {
            Basarili = true,
            Mesaj = $"{log.EntityTipi} kaydı silindi (ekleme geri alındı).",
            EntityTipi = log.EntityTipi,
            EntityId = log.EntityId,
            IslemTipi = "Ekleme Geri Alındı"
        };
    }

    private async Task<GeriAlmaSonuc> GeriAlGeriAlmaAsync(ApplicationDbContext context, AktiviteLog log)
    {
        // Geri alma işlemini geri almak = eski haline getirmek (geri alınmadan önceki duruma)
        // Geri alma logunda: EskiDeger = geri alınmadan önceki durum, YeniDeger = geri alındıktan sonraki durum
        // Yani eski haline getirmek için EskiDeger'i (geri alınmadan önceki durumu) yüklemeliyiz
        if (!log.EntityId.HasValue || string.IsNullOrEmpty(log.EntityTipi) || string.IsNullOrEmpty(log.EskiDeger))
            return new GeriAlmaSonuc { Basarili = false, Mesaj = "Entity veya değer bilgisi eksik." };

        var entity = await GetEntityByTypeAndIdAsync(context, log.EntityTipi, log.EntityId.Value, includeDeleted: true);
        if (entity == null)
            return new GeriAlmaSonuc { Basarili = false, Mesaj = "Kayıt bulunamadı." };

        try
        {
            var eskiDegerler = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(log.EskiDeger);
            if (eskiDegerler == null)
                return new GeriAlmaSonuc { Basarili = false, Mesaj = "Değerler okunamadı." };

            var entityType = entity.GetType();

            foreach (var (propertyName, value) in eskiDegerler)
            {
                if (propertyName is "Id" or "CreatedAt" or "UpdatedAt")
                    continue;

                var property = entityType.GetProperty(propertyName);
                if (property == null || !property.CanWrite)
                    continue;

                try
                {
                    var convertedValue = ConvertJsonElementToType(value, property.PropertyType);
                    property.SetValue(entity, convertedValue);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Property geri yüklenemedi: {Property}", propertyName);
                }
            }

            if (entity is BaseEntity baseEntity)
            {
                baseEntity.UpdatedAt = DateTime.UtcNow;
            }

            await context.SaveChangesAsync();

            return new GeriAlmaSonuc
            {
                Basarili = true,
                Mesaj = $"{log.EntityTipi} kaydı eski haline getirildi.",
                EntityTipi = log.EntityTipi,
                EntityId = log.EntityId,
                IslemTipi = "Eski Haline Getirildi"
            };
        }
        catch (Exception ex)
        {
            return new GeriAlmaSonuc { Basarili = false, Mesaj = $"Değer dönüştürme hatası: {ex.Message}" };
        }
    }

    private async Task<GeriAlmaSonuc> GeriAlGuncellemeAsync(ApplicationDbContext context, AktiviteLog log)
    {
        // Güncelleme işlemini geri almak = eski değerleri geri yüklemek
        if (!log.EntityId.HasValue || string.IsNullOrEmpty(log.EntityTipi) || string.IsNullOrEmpty(log.EskiDeger))
            return new GeriAlmaSonuc { Basarili = false, Mesaj = "Entity veya eski değer bilgisi eksik." };

        var entity = await GetEntityByTypeAndIdAsync(context, log.EntityTipi, log.EntityId.Value);
        if (entity == null)
            return new GeriAlmaSonuc { Basarili = false, Mesaj = "Kayıt bulunamadı." };

        try
        {
            var eskiDegerler = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(log.EskiDeger);
            if (eskiDegerler == null)
                return new GeriAlmaSonuc { Basarili = false, Mesaj = "Eski değerler okunamadı." };

            var entityType = entity.GetType();

            foreach (var (propertyName, value) in eskiDegerler)
            {
                // Bazı property'leri atla
                if (propertyName is "Id" or "CreatedAt" or "UpdatedAt" or "IsDeleted")
                    continue;

                var property = entityType.GetProperty(propertyName);
                if (property == null || !property.CanWrite)
                    continue;

                try
                {
                    var convertedValue = ConvertJsonElementToType(value, property.PropertyType);
                    if (convertedValue != null || value.ValueKind == JsonValueKind.Null)
                    {
                        property.SetValue(entity, convertedValue);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Property geri yüklenemedi: {Property}", propertyName);
                }
            }

            if (entity is BaseEntity baseEntity)
            {
                baseEntity.UpdatedAt = DateTime.UtcNow;
            }

            await context.SaveChangesAsync();

            return new GeriAlmaSonuc
            {
                Basarili = true,
                Mesaj = $"{log.EntityTipi} kaydı eski haline getirildi.",
                EntityTipi = log.EntityTipi,
                EntityId = log.EntityId,
                IslemTipi = "Güncelleme Geri Alındı"
            };
        }
        catch (Exception ex)
        {
            return new GeriAlmaSonuc { Basarili = false, Mesaj = $"Değer dönüştürme hatası: {ex.Message}" };
        }
    }

    private async Task<GeriAlmaSonuc> GeriAlSilmeAsync(ApplicationDbContext context, AktiviteLog log)
    {
        // Silme işlemini geri almak = kaydı geri yüklemek (soft delete ise IsDeleted = false)
        if (!log.EntityId.HasValue || string.IsNullOrEmpty(log.EntityTipi))
            return new GeriAlmaSonuc { Basarili = false, Mesaj = "Entity bilgisi eksik." };

        // Önce soft-deleted kaydı bul
        var entity = await GetEntityByTypeAndIdAsync(context, log.EntityTipi, log.EntityId.Value, includeDeleted: true);

        if (entity == null && !string.IsNullOrEmpty(log.EskiDeger))
        {
            // Kayıt tamamen silinmiş, eski değerlerden yeniden oluştur
            return await RecreateEntityFromLogAsync(context, log);
        }

        if (entity == null)
            return new GeriAlmaSonuc { Basarili = false, Mesaj = "Kayıt bulunamadı ve eski değerler eksik." };

        // Soft delete geri al
        if (entity is BaseEntity baseEntity)
        {
            baseEntity.IsDeleted = false;
            baseEntity.UpdatedAt = DateTime.UtcNow;
        }

        await context.SaveChangesAsync();

        return new GeriAlmaSonuc
        {
            Basarili = true,
            Mesaj = $"{log.EntityTipi} kaydı geri yüklendi.",
            EntityTipi = log.EntityTipi,
            EntityId = log.EntityId,
            IslemTipi = "Silme Geri Alındı"
        };
    }

    private async Task<GeriAlmaSonuc> RecreateEntityFromLogAsync(ApplicationDbContext context, AktiviteLog log)
    {
        // Bu metod karmaşık ve riskli olduğundan şimdilik sadece soft-delete desteği sağlayalım
        return new GeriAlmaSonuc 
        { 
            Basarili = false, 
            Mesaj = "Tamamen silinmiş kayıtlar geri yüklenemiyor. Sadece soft-delete yapılmış kayıtlar geri alınabilir." 
        };
    }

    private async Task<object?> GetEntityByTypeAndIdAsync(ApplicationDbContext context, string entityType, int id, bool includeDeleted = false)
    {
        return entityType switch
        {
            "Cari" => includeDeleted 
                ? await context.Cariler.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == id)
                : await context.Cariler.FindAsync(id),
            "Arac" => includeDeleted
                ? await context.Araclar.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == id)
                : await context.Araclar.FindAsync(id),
            "Sofor" => includeDeleted
                ? await context.Soforler.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == id)
                : await context.Soforler.FindAsync(id),
            "Fatura" => includeDeleted
                ? await context.Faturalar.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == id)
                : await context.Faturalar.FindAsync(id),
            "FaturaKalem" => includeDeleted
                ? await context.FaturaKalemleri.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == id)
                : await context.FaturaKalemleri.FindAsync(id),
            "BankaHesap" => includeDeleted
                ? await context.BankaHesaplari.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == id)
                : await context.BankaHesaplari.FindAsync(id),
            "BankaKasaHareket" => includeDeleted
                ? await context.BankaKasaHareketleri.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == id)
                : await context.BankaKasaHareketleri.FindAsync(id),
            "Guzergah" => includeDeleted
                ? await context.Guzergahlar.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == id)
                : await context.Guzergahlar.FindAsync(id),
            "MasrafKalemi" => includeDeleted
                ? await context.MasrafKalemleri.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == id)
                : await context.MasrafKalemleri.FindAsync(id),
            "ServisCalisma" => includeDeleted
                ? await context.ServisCalismalari.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == id)
                : await context.ServisCalismalari.FindAsync(id),
            "AracMasraf" => includeDeleted
                ? await context.AracMasraflari.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == id)
                : await context.AracMasraflari.FindAsync(id),
            "BudgetOdeme" => includeDeleted
                ? await context.BudgetOdemeler.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == id)
                : await context.BudgetOdemeler.FindAsync(id),
            "TekrarlayanOdeme" => includeDeleted
                ? await context.TekrarlayanOdemeler.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == id)
                : await context.TekrarlayanOdemeler.FindAsync(id),
            "BudgetMasrafKalemi" => includeDeleted
                ? await context.BudgetMasrafKalemleri.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == id)
                : await context.BudgetMasrafKalemleri.FindAsync(id),
            "Hatirlatici" => includeDeleted
                ? await context.Hatirlaticilar.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == id)
                : await context.Hatirlaticilar.FindAsync(id),
            "Bildirim" => includeDeleted
                ? await context.Bildirimler.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == id)
                : await context.Bildirimler.FindAsync(id),
            "StokKarti" => includeDeleted
                ? await context.StokKartlari.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == id)
                : await context.StokKartlari.FindAsync(id),
            "StokHareket" => includeDeleted
                ? await context.StokHareketler.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == id)
                : await context.StokHareketler.FindAsync(id),
            "MuhasebeHesap" => includeDeleted
                ? await context.MuhasebeHesaplari.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == id)
                : await context.MuhasebeHesaplari.FindAsync(id),
            "MuhasebeFis" => includeDeleted
                ? await context.MuhasebeFisleri.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == id)
                : await context.MuhasebeFisleri.FindAsync(id),
            "Firma" => includeDeleted
                ? await context.Firmalar.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == id)
                : await context.Firmalar.FindAsync(id),
            "Kullanici" => includeDeleted
                ? await context.Kullanicilar.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == id)
                : await context.Kullanicilar.FindAsync(id),
            _ => null
        };
    }

    private static string GetCurrentUserName(ClaimsPrincipal? user)
    {
        if (user?.Identity?.IsAuthenticated != true)
            return "Sistem";

        return user.FindFirst("AdSoyad")?.Value
            ?? user.FindFirst(ClaimTypes.Name)?.Value
            ?? user.Identity?.Name
            ?? user.FindFirst(ClaimTypes.Email)?.Value
            ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? "Sistem";
    }

    private static object? ConvertJsonElementToType(JsonElement value, Type propertyType)
    {
        var targetType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

        if (value.ValueKind == JsonValueKind.Null)
            return null;

        if (targetType == typeof(string))
            return value.GetString();

        if (targetType == typeof(int))
            return value.GetInt32();

        if (targetType == typeof(long))
            return value.GetInt64();

        if (targetType == typeof(decimal))
            return value.GetDecimal();

        if (targetType == typeof(double))
            return value.GetDouble();

        if (targetType == typeof(float))
            return value.GetSingle();

        if (targetType == typeof(bool))
            return value.GetBoolean();

        if (targetType == typeof(DateTime))
        {
            if (value.TryGetDateTime(out var dt))
                return DateTime.SpecifyKind(dt, DateTimeKind.Utc);
            return null;
        }

        if (targetType == typeof(Guid))
        {
            if (value.TryGetGuid(out var guid))
                return guid;
            return null;
        }

        if (targetType.IsEnum)
        {
            if (value.ValueKind == JsonValueKind.Number)
                return Enum.ToObject(targetType, value.GetInt32());
            if (value.ValueKind == JsonValueKind.String)
            {
                var strVal = value.GetString();
                if (!string.IsNullOrEmpty(strVal) && Enum.TryParse(targetType, strVal, out var enumVal))
                    return enumVal;
            }
            return null;
        }

        return null;
    }
}




