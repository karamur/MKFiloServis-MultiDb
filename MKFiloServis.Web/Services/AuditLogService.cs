using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using MKFiloServis.Web.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MKFiloServis.Web.Services;

/// <summary>
/// Audit log servisi implementasyonu
/// </summary>
public class AuditLogService : IAuditLogService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IAktifFirmaProvider _aktifFirmaProvider;
    private readonly ILogger<AuditLogService> _logger;
    
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = false,
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
    
    public AuditLogService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IHttpContextAccessor httpContextAccessor,
        IAktifFirmaProvider aktifFirmaProvider,
        ILogger<AuditLogService> logger)
    {
        _contextFactory = contextFactory;
        _httpContextAccessor = httpContextAccessor;
        _aktifFirmaProvider = aktifFirmaProvider;
        _logger = logger;
    }
    
    #region Private Helpers
    
    private (int? kullaniciId, string? kullaniciAdi, string? ipAdresi, string? userAgent, string? requestPath) GetRequestInfo()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
            return (null, null, null, null, null);
        
        var kullaniciId = httpContext.User.FindFirst("KullaniciId")?.Value;
        var kullaniciAdi = httpContext.User.Identity?.Name;
        var ipAdresi = httpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = httpContext.Request.Headers.UserAgent.ToString();
        var requestPath = httpContext.Request.Path.Value;
        
        return (
            int.TryParse(kullaniciId, out var id) ? id : null,
            kullaniciAdi,
            ipAdresi,
            userAgent?.Length > 500 ? userAgent[..500] : userAgent,
            requestPath?.Length > 500 ? requestPath[..500] : requestPath
        );
    }
    
    private string? SerializeEntity<T>(T entity) where T : class
    {
        if (entity == null) return null;
        
        try
        {
            // Navigation property'leri hariç tut
            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => !p.PropertyType.IsClass || p.PropertyType == typeof(string))
                .ToDictionary(p => p.Name, p => p.GetValue(entity));
            
            return JsonSerializer.Serialize(properties, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Entity serialize edilemedi: {EntityType}", typeof(T).Name);
            return null;
        }
    }
    
    private string? GetChangedFields<T>(T eskiEntity, T yeniEntity) where T : class
    {
        try
        {
            var changedFields = new List<string>();
            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => !p.PropertyType.IsClass || p.PropertyType == typeof(string));
            
            foreach (var prop in properties)
            {
                var eskiDeger = prop.GetValue(eskiEntity);
                var yeniDeger = prop.GetValue(yeniEntity);
                
                if (!Equals(eskiDeger, yeniDeger))
                {
                    changedFields.Add(prop.Name);
                }
            }
            
            return changedFields.Count > 0 ? JsonSerializer.Serialize(changedFields, _jsonOptions) : null;
        }
        catch
        {
            return null;
        }
    }
    
    private static int? GetEntityId<T>(T entity) where T : class
    {
        var idProp = typeof(T).GetProperty("Id");
        if (idProp != null)
        {
            var value = idProp.GetValue(entity);
            if (value is int intId) return intId;
        }
        return null;
    }
    
    private static string GetKategori(string entityAdi)
    {
        return entityAdi.ToLower() switch
        {
            "fatura" or "faturakasem" => AuditKategorileri.Fatura,
            "cari" => AuditKategorileri.Cari,
            "arac" or "aracmasraf" => AuditKategorileri.Arac,
            "sofor" => AuditKategorileri.Sofor,
            "personel" or "personelmaas" => AuditKategorileri.Personel,
            "personelpuantaj" => AuditKategorileri.Puantaj,
            "kullanici" or "rol" => AuditKategorileri.Kullanici,
            "bankahesap" or "bankakasahareket" => AuditKategorileri.Finans,
            "ebysgelenevrak" or "ebysgidenevrak" => AuditKategorileri.Ebys,
            _ => AuditKategorileri.Sistem
        };
    }
    
    #endregion
    
    #region Log Oluşturma
    
    public async Task<AuditLog> LogAsync(AuditLogCreateDto dto)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var (kullaniciId, kullaniciAdi, ipAdresi, userAgent, requestPath) = GetRequestInfo();
        
        var log = new AuditLog
        {
            IslemTipi = dto.IslemTipi,
            EntityAdi = dto.EntityAdi,
            EntityId = dto.EntityId,
            EntityGuid = dto.EntityGuid,
            KullaniciId = kullaniciId,
            KullaniciAdi = kullaniciAdi,
            IpAdresi = ipAdresi,
            UserAgent = userAgent,
            RequestPath = requestPath,
            EskiDeger = dto.EskiDeger,
            YeniDeger = dto.YeniDeger,
            DegisenAlanlar = dto.DegisenAlanlar,
            Aciklama = dto.Aciklama,
            Kategori = dto.Kategori ?? GetKategori(dto.EntityAdi),
            Seviye = dto.Seviye,
            Basarili = dto.Basarili,
            HataMesaji = dto.HataMesaji,
            IslemSuresiMs = dto.IslemSuresiMs,
            FirmaId = _aktifFirmaProvider.AktifFirmaId,
            IslemTarihi = DateTime.UtcNow
        };
        
        context.Set<AuditLog>().Add(log);
        await context.SaveChangesAsync();
        
        return log;
    }
    
    public async Task LogCreateAsync<T>(T entity, string? aciklama = null) where T : class
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var entityAdi = typeof(T).Name;
        var entityId = GetEntityId(entity);
        
        await LogAsync(new AuditLogCreateDto
        {
            IslemTipi = AuditIslemTipleri.Create,
            EntityAdi = entityAdi,
            EntityId = entityId,
            YeniDeger = SerializeEntity(entity),
            Aciklama = aciklama ?? $"{entityAdi} kaydı oluşturuldu",
            Kategori = GetKategori(entityAdi)
        });
    }
    
    public async Task LogUpdateAsync<T>(T eskiEntity, T yeniEntity, string? aciklama = null) where T : class
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var entityAdi = typeof(T).Name;
        var entityId = GetEntityId(yeniEntity);
        
        await LogAsync(new AuditLogCreateDto
        {
            IslemTipi = AuditIslemTipleri.Update,
            EntityAdi = entityAdi,
            EntityId = entityId,
            EskiDeger = SerializeEntity(eskiEntity),
            YeniDeger = SerializeEntity(yeniEntity),
            DegisenAlanlar = GetChangedFields(eskiEntity, yeniEntity),
            Aciklama = aciklama ?? $"{entityAdi} kaydı güncellendi",
            Kategori = GetKategori(entityAdi)
        });
    }
    
    public async Task LogDeleteAsync<T>(T entity, string? aciklama = null) where T : class
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var entityAdi = typeof(T).Name;
        var entityId = GetEntityId(entity);
        
        await LogAsync(new AuditLogCreateDto
        {
            IslemTipi = AuditIslemTipleri.Delete,
            EntityAdi = entityAdi,
            EntityId = entityId,
            EskiDeger = SerializeEntity(entity),
            Aciklama = aciklama ?? $"{entityAdi} kaydı silindi",
            Kategori = GetKategori(entityAdi),
            Seviye = AuditSeviyeleri.Warning
        });
    }
    
    public async Task LogSoftDeleteAsync<T>(T entity, string? aciklama = null) where T : class
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var entityAdi = typeof(T).Name;
        var entityId = GetEntityId(entity);
        
        await LogAsync(new AuditLogCreateDto
        {
            IslemTipi = AuditIslemTipleri.SoftDelete,
            EntityAdi = entityAdi,
            EntityId = entityId,
            Aciklama = aciklama ?? $"{entityAdi} kaydı pasif yapıldı",
            Kategori = GetKategori(entityAdi)
        });
    }
    
    public async Task LogRestoreAsync<T>(T entity, string? aciklama = null) where T : class
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var entityAdi = typeof(T).Name;
        var entityId = GetEntityId(entity);
        
        await LogAsync(new AuditLogCreateDto
        {
            IslemTipi = AuditIslemTipleri.Restore,
            EntityAdi = entityAdi,
            EntityId = entityId,
            Aciklama = aciklama ?? $"{entityAdi} kaydı geri yüklendi",
            Kategori = GetKategori(entityAdi)
        });
    }
    
    public async Task LogLoginAsync(int kullaniciId, string kullaniciAdi, bool basarili, string? hataMesaji = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var (_, _, ipAdresi, userAgent, requestPath) = GetRequestInfo();
        
        var log = new AuditLog
        {
            IslemTipi = basarili ? AuditIslemTipleri.Login : AuditIslemTipleri.LoginFailed,
            EntityAdi = "Kullanici",
            EntityId = kullaniciId,
            KullaniciId = kullaniciId,
            KullaniciAdi = kullaniciAdi,
            IpAdresi = ipAdresi,
            UserAgent = userAgent,
            RequestPath = requestPath,
            Basarili = basarili,
            HataMesaji = hataMesaji,
            Aciklama = basarili ? $"{kullaniciAdi} giriş yaptı" : $"{kullaniciAdi} giriş başarısız",
            Kategori = AuditKategorileri.Kullanici,
            Seviye = basarili ? AuditSeviyeleri.Info : AuditSeviyeleri.Warning,
            IslemTarihi = DateTime.UtcNow
        };
        
        context.Set<AuditLog>().Add(log);
        await context.SaveChangesAsync();
    }
    
    public async Task LogLogoutAsync(int kullaniciId, string kullaniciAdi)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        await LogAsync(new AuditLogCreateDto
        {
            IslemTipi = AuditIslemTipleri.Logout,
            EntityAdi = "Kullanici",
            EntityId = kullaniciId,
            Aciklama = $"{kullaniciAdi} çıkış yaptı",
            Kategori = AuditKategorileri.Kullanici
        });
    }
    
    public async Task LogExportAsync(string entityAdi, int kayitSayisi, string format, string? aciklama = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        await LogAsync(new AuditLogCreateDto
        {
            IslemTipi = AuditIslemTipleri.Export,
            EntityAdi = entityAdi,
            Aciklama = aciklama ?? $"{entityAdi} {kayitSayisi} kayıt {format} formatında export edildi",
            Kategori = AuditKategorileri.Rapor
        });
    }
    
    public async Task LogImportAsync(string entityAdi, int kayitSayisi, bool basarili, string? hataMesaji = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        await LogAsync(new AuditLogCreateDto
        {
            IslemTipi = AuditIslemTipleri.Import,
            EntityAdi = entityAdi,
            Basarili = basarili,
            HataMesaji = hataMesaji,
            Aciklama = basarili 
                ? $"{entityAdi} {kayitSayisi} kayıt import edildi" 
                : $"{entityAdi} import başarısız",
            Kategori = GetKategori(entityAdi),
            Seviye = basarili ? AuditSeviyeleri.Info : AuditSeviyeleri.Error
        });
    }
    
    public async Task LogCustomAsync(string islemTipi, string entityAdi, int? entityId, string? aciklama = null,
        string kategori = AuditKategorileri.Sistem, string seviye = AuditSeviyeleri.Info)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        await LogAsync(new AuditLogCreateDto
        {
            IslemTipi = islemTipi,
            EntityAdi = entityAdi,
            EntityId = entityId,
            Aciklama = aciklama,
            Kategori = kategori,
            Seviye = seviye
        });
    }
    
    #endregion
    
    #region Log Sorgulama
    
    public async Task<AuditLogPagedResult> GetPagedAsync(AuditLogFiltre filtre)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.Set<AuditLog>().AsNoTracking().AsQueryable();
        
        // Filtreler
        if (filtre.BaslangicTarihi.HasValue)
            query = query.Where(x => x.IslemTarihi >= filtre.BaslangicTarihi.Value);
        
        if (filtre.BitisTarihi.HasValue)
            query = query.Where(x => x.IslemTarihi <= filtre.BitisTarihi.Value);
        
        if (!string.IsNullOrEmpty(filtre.IslemTipi))
            query = query.Where(x => x.IslemTipi == filtre.IslemTipi);
        
        if (!string.IsNullOrEmpty(filtre.EntityAdi))
            query = query.Where(x => x.EntityAdi == filtre.EntityAdi);
        
        if (filtre.EntityId.HasValue)
            query = query.Where(x => x.EntityId == filtre.EntityId);
        
        if (filtre.KullaniciId.HasValue)
            query = query.Where(x => x.KullaniciId == filtre.KullaniciId);
        
        if (!string.IsNullOrEmpty(filtre.Kategori))
            query = query.Where(x => x.Kategori == filtre.Kategori);
        
        if (!string.IsNullOrEmpty(filtre.Seviye))
            query = query.Where(x => x.Seviye == filtre.Seviye);
        
        if (filtre.Basarili.HasValue)
            query = query.Where(x => x.Basarili == filtre.Basarili.Value);
        
        if (!string.IsNullOrEmpty(filtre.AramaMetni))
        {
            var arama = filtre.AramaMetni.ToLower();
            query = query.Where(x => 
                (x.Aciklama != null && x.Aciklama.ToLower().Contains(arama)) ||
                (x.KullaniciAdi != null && x.KullaniciAdi.ToLower().Contains(arama)) ||
                x.EntityAdi.ToLower().Contains(arama));
        }
        
        // Multi-tenant: TumFirmalar (eski SuperAdmin) modunda filter atlanır.
        if (!_aktifFirmaProvider.TumFirmalar && _aktifFirmaProvider.AktifFirmaId.HasValue)
        {
            var aktifId = _aktifFirmaProvider.AktifFirmaId.Value;
            query = query.Where(x => x.FirmaId == null || x.FirmaId == aktifId);
        }

        // Toplam kayıt
        var toplamKayit = await query.CountAsync();
        
        // Sıralama
        query = filtre.SiralamaAlani.ToLower() switch
        {
            "islemtarihi" => filtre.AzalanSiralama 
                ? query.OrderByDescending(x => x.IslemTarihi) 
                : query.OrderBy(x => x.IslemTarihi),
            "islemtipi" => filtre.AzalanSiralama 
                ? query.OrderByDescending(x => x.IslemTipi) 
                : query.OrderBy(x => x.IslemTipi),
            "entityadi" => filtre.AzalanSiralama 
                ? query.OrderByDescending(x => x.EntityAdi) 
                : query.OrderBy(x => x.EntityAdi),
            "kullaniciadi" => filtre.AzalanSiralama 
                ? query.OrderByDescending(x => x.KullaniciAdi) 
                : query.OrderBy(x => x.KullaniciAdi),
            _ => query.OrderByDescending(x => x.IslemTarihi)
        };
        
        // Sayfalama
        var items = await query
            .Skip((filtre.Sayfa - 1) * filtre.SayfaBoyutu)
            .Take(filtre.SayfaBoyutu)
            .ToListAsync();
        
        return new AuditLogPagedResult
        {
            Items = items,
            ToplamKayit = toplamKayit,
            ToplamSayfa = (int)Math.Ceiling(toplamKayit / (double)filtre.SayfaBoyutu),
            MevcutSayfa = filtre.Sayfa,
            SayfaBoyutu = filtre.SayfaBoyutu
        };
    }
    
    public async Task<List<AuditLog>> GetEntityHistoryAsync(string entityAdi, int entityId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Set<AuditLog>()
            .AsNoTracking()
            .Where(x => x.EntityAdi == entityAdi && x.EntityId == entityId)
            .OrderByDescending(x => x.IslemTarihi)
            .Take(100)
            .ToListAsync();
    }
    
    public async Task<List<AuditLog>> GetByKullaniciAsync(int kullaniciId, DateTime? baslangic = null, DateTime? bitis = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.Set<AuditLog>()
            .AsNoTracking()
            .Where(x => x.KullaniciId == kullaniciId);
        
        if (baslangic.HasValue)
            query = query.Where(x => x.IslemTarihi >= baslangic.Value);
        
        if (bitis.HasValue)
            query = query.Where(x => x.IslemTarihi <= bitis.Value);
        
        return await query
            .OrderByDescending(x => x.IslemTarihi)
            .Take(500)
            .ToListAsync();
    }
    
    public async Task<List<AuditLog>> GetByDateRangeAsync(DateTime baslangic, DateTime bitis)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Set<AuditLog>()
            .AsNoTracking()
            .Where(x => x.IslemTarihi >= baslangic && x.IslemTarihi <= bitis)
            .OrderByDescending(x => x.IslemTarihi)
            .Take(1000)
            .ToListAsync();
    }
    
    public async Task<AuditLog?> GetByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Set<AuditLog>()
            .AsNoTracking()
            .Include(x => x.Kullanici)
            .FirstOrDefaultAsync(x => x.Id == id);
    }
    
    #endregion
    
    #region İstatistikler
    
    public async Task<AuditLogDashboard> GetDashboardAsync(DateTime? baslangic = null, DateTime? bitis = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var now = DateTime.UtcNow;
        var bas = baslangic ?? now.AddDays(-30);
        var bit = bitis ?? now;
        var bugun = now.Date;
        
        var query = context.Set<AuditLog>().AsNoTracking();
        
        // Multi-tenant: TumFirmalar (eski SuperAdmin) modunda filter atlanır.
        if (!_aktifFirmaProvider.TumFirmalar && _aktifFirmaProvider.AktifFirmaId.HasValue)
        {
            var aktifId = _aktifFirmaProvider.AktifFirmaId.Value;
            query = query.Where(x => x.FirmaId == null || x.FirmaId == aktifId);
        }

        var tarihQuery = query.Where(x => x.IslemTarihi >= bas && x.IslemTarihi <= bit);
        
        var dashboard = new AuditLogDashboard
        {
            ToplamLog = await tarihQuery.CountAsync(),
            BugunkuLog = await query.CountAsync(x => x.IslemTarihi >= bugun),
            BasarisizIslem = await tarihQuery.CountAsync(x => !x.Basarili),
            KritikIslem = await tarihQuery.CountAsync(x => x.Seviye == AuditSeviyeleri.Critical || x.Seviye == AuditSeviyeleri.Error),
            AktifKullanici = await tarihQuery.Where(x => x.KullaniciId != null).Select(x => x.KullaniciId).Distinct().CountAsync()
        };
        
        // Günlük trend (son 30 gün)
        var gunlukData = await tarihQuery
            .GroupBy(x => x.IslemTarihi.Date)
            .Select(g => new
            {
                Tarih = g.Key,
                Toplam = g.Count(),
                Basarili = g.Count(x => x.Basarili),
                Basarisiz = g.Count(x => !x.Basarili)
            })
            .OrderBy(x => x.Tarih)
            .ToListAsync();
        
        dashboard.GunlukTrend = gunlukData.Select(x => new AuditLogGunlukStat
        {
            Tarih = x.Tarih,
            ToplamIslem = x.Toplam,
            BasariliIslem = x.Basarili,
            BasarisizIslem = x.Basarisiz
        }).ToList();
        
        // İşlem dağılımı
        var islemData = await tarihQuery
            .GroupBy(x => x.IslemTipi)
            .Select(g => new { IslemTipi = g.Key, Sayi = g.Count() })
            .OrderByDescending(x => x.Sayi)
            .Take(10)
            .ToListAsync();
        
        var toplamIslem = islemData.Sum(x => x.Sayi);
        dashboard.IslemDagilimi = islemData.Select(x => new AuditLogIslemStat
        {
            IslemTipi = x.IslemTipi,
            Sayi = x.Sayi,
            Yuzde = toplamIslem > 0 ? Math.Round(x.Sayi * 100m / toplamIslem, 1) : 0
        }).ToList();
        
        // Kategori dağılımı
        var kategoriData = await tarihQuery
            .Where(x => x.Kategori != null)
            .GroupBy(x => x.Kategori!)
            .Select(g => new { Kategori = g.Key, Sayi = g.Count() })
            .OrderByDescending(x => x.Sayi)
            .ToListAsync();
        
        var toplamKategori = kategoriData.Sum(x => x.Sayi);
        dashboard.KategoriDagilimi = kategoriData.Select(x => new AuditLogKategoriStat
        {
            Kategori = x.Kategori,
            Sayi = x.Sayi,
            Yuzde = toplamKategori > 0 ? Math.Round(x.Sayi * 100m / toplamKategori, 1) : 0
        }).ToList();
        
        // Son işlemler
        dashboard.SonIslemler = await query
            .OrderByDescending(x => x.IslemTarihi)
            .Take(10)
            .ToListAsync();
        
        return dashboard;
    }
    
    public async Task<List<AuditLogIslemStat>> GetIslemStatistikleriAsync(DateTime baslangic, DateTime bitis)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.Set<AuditLog>()
            .AsNoTracking()
            .Where(x => x.IslemTarihi >= baslangic && x.IslemTarihi <= bitis);
        
        var data = await query
            .GroupBy(x => x.IslemTipi)
            .Select(g => new { IslemTipi = g.Key, Sayi = g.Count() })
            .OrderByDescending(x => x.Sayi)
            .ToListAsync();
        
        var toplam = data.Sum(x => x.Sayi);
        return data.Select(x => new AuditLogIslemStat
        {
            IslemTipi = x.IslemTipi,
            Sayi = x.Sayi,
            Yuzde = toplam > 0 ? Math.Round(x.Sayi * 100m / toplam, 1) : 0
        }).ToList();
    }
    
    public async Task<List<AuditLogKullaniciStat>> GetKullaniciStatistikleriAsync(DateTime baslangic, DateTime bitis)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var data = await context.Set<AuditLog>()
            .AsNoTracking()
            .Where(x => x.IslemTarihi >= baslangic && x.IslemTarihi <= bitis && x.KullaniciId != null)
            .GroupBy(x => new { x.KullaniciId, x.KullaniciAdi })
            .Select(g => new
            {
                g.Key.KullaniciId,
                g.Key.KullaniciAdi,
                Toplam = g.Count(),
                Create = g.Count(x => x.IslemTipi == AuditIslemTipleri.Create),
                Update = g.Count(x => x.IslemTipi == AuditIslemTipleri.Update),
                Delete = g.Count(x => x.IslemTipi == AuditIslemTipleri.Delete || x.IslemTipi == AuditIslemTipleri.SoftDelete),
                SonIslem = g.Max(x => x.IslemTarihi)
            })
            .OrderByDescending(x => x.Toplam)
            .Take(20)
            .ToListAsync();
        
        return data.Select(x => new AuditLogKullaniciStat
        {
            KullaniciId = x.KullaniciId!.Value,
            KullaniciAdi = x.KullaniciAdi ?? "Bilinmiyor",
            ToplamIslem = x.Toplam,
            CreateSayisi = x.Create,
            UpdateSayisi = x.Update,
            DeleteSayisi = x.Delete,
            SonIslemTarihi = x.SonIslem
        }).ToList();
    }
    
    #endregion
    
    #region Temizlik
    
    public async Task<int> CleanupOldLogsAsync(int gunSayisi)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var kesimTarihi = DateTime.UtcNow.AddDays(-gunSayisi);
        
        // Kritik olmayan logları sil
        var silinecekler = await context.Set<AuditLog>()
            .Where(x => x.IslemTarihi < kesimTarihi && x.Seviye != AuditSeviyeleri.Critical)
            .ToListAsync();
        
        if (silinecekler.Count > 0)
        {
            context.Set<AuditLog>().RemoveRange(silinecekler);
            await context.SaveChangesAsync();
            
            _logger.LogInformation("{Count} adet eski audit log silindi (>{GunSayisi} gün)", silinecekler.Count, gunSayisi);
        }
        
        return silinecekler.Count;
    }
    
    public async Task<string> ArchiveLogsAsync(DateTime oncesiTarih)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var arsivlenecekler = await context.Set<AuditLog>()
            .AsNoTracking()
            .Where(x => x.IslemTarihi < oncesiTarih)
            .OrderBy(x => x.IslemTarihi)
            .ToListAsync();
        
        if (arsivlenecekler.Count == 0)
            return string.Empty;
        
        // JSON olarak arşivle
        var dosyaAdi = $"audit_archive_{oncesiTarih:yyyyMMdd}_{DateTime.UtcNow:yyyyMMddHHmmss}.json";
        var jsonContent = JsonSerializer.Serialize(arsivlenecekler, new JsonSerializerOptions { WriteIndented = true });
        
        // wwwroot/archives klasörüne kaydet
        var archivePath = Path.Combine("wwwroot", "archives");
        Directory.CreateDirectory(archivePath);
        var fullPath = Path.Combine(archivePath, dosyaAdi);
        
        await File.WriteAllTextAsync(fullPath, jsonContent);
        
        _logger.LogInformation("Audit log arşivlendi: {DosyaAdi}, {Count} kayıt", dosyaAdi, arsivlenecekler.Count);
        
        return dosyaAdi;
    }
    
    #endregion
}


