using MKFiloServis.Shared.Entities;

namespace MKFiloServis.Web.Services.Interfaces;

/// <summary>
/// Audit log servisi arayüzü - tüm sistem işlemlerinin loglanması
/// </summary>
public interface IAuditLogService
{
    #region Log Oluşturma
    
    /// <summary>
    /// Yeni audit log kaydı oluşturur
    /// </summary>
    Task<AuditLog> LogAsync(AuditLogCreateDto dto);
    
    /// <summary>
    /// Create işlemi loglar
    /// </summary>
    Task LogCreateAsync<T>(T entity, string? aciklama = null) where T : class;
    
    /// <summary>
    /// Update işlemi loglar (eski ve yeni değerlerle)
    /// </summary>
    Task LogUpdateAsync<T>(T eskiEntity, T yeniEntity, string? aciklama = null) where T : class;
    
    /// <summary>
    /// Delete işlemi loglar
    /// </summary>
    Task LogDeleteAsync<T>(T entity, string? aciklama = null) where T : class;
    
    /// <summary>
    /// Soft delete işlemi loglar
    /// </summary>
    Task LogSoftDeleteAsync<T>(T entity, string? aciklama = null) where T : class;
    
    /// <summary>
    /// Restore işlemi loglar
    /// </summary>
    Task LogRestoreAsync<T>(T entity, string? aciklama = null) where T : class;
    
    /// <summary>
    /// Login işlemi loglar
    /// </summary>
    Task LogLoginAsync(int kullaniciId, string kullaniciAdi, bool basarili, string? hataMesaji = null);
    
    /// <summary>
    /// Logout işlemi loglar
    /// </summary>
    Task LogLogoutAsync(int kullaniciId, string kullaniciAdi);
    
    /// <summary>
    /// Export işlemi loglar
    /// </summary>
    Task LogExportAsync(string entityAdi, int kayitSayisi, string format, string? aciklama = null);
    
    /// <summary>
    /// Import işlemi loglar
    /// </summary>
    Task LogImportAsync(string entityAdi, int kayitSayisi, bool basarili, string? hataMesaji = null);
    
    /// <summary>
    /// Özel işlem loglar
    /// </summary>
    Task LogCustomAsync(string islemTipi, string entityAdi, int? entityId, string? aciklama = null, 
        string kategori = AuditKategorileri.Sistem, string seviye = AuditSeviyeleri.Info);
    
    #endregion
    
    #region Log Sorgulama
    
    /// <summary>
    /// Sayfalanmış audit log listesi
    /// </summary>
    Task<AuditLogPagedResult> GetPagedAsync(AuditLogFiltre filtre);
    
    /// <summary>
    /// Belirli bir entity'nin geçmişi
    /// </summary>
    Task<List<AuditLog>> GetEntityHistoryAsync(string entityAdi, int entityId);
    
    /// <summary>
    /// Belirli bir kullanıcının işlemleri
    /// </summary>
    Task<List<AuditLog>> GetByKullaniciAsync(int kullaniciId, DateTime? baslangic = null, DateTime? bitis = null);
    
    /// <summary>
    /// Tarih aralığında loglar
    /// </summary>
    Task<List<AuditLog>> GetByDateRangeAsync(DateTime baslangic, DateTime bitis);
    
    /// <summary>
    /// Tek bir log detayı
    /// </summary>
    Task<AuditLog?> GetByIdAsync(int id);
    
    #endregion
    
    #region İstatistikler
    
    /// <summary>
    /// Dashboard istatistikleri
    /// </summary>
    Task<AuditLogDashboard> GetDashboardAsync(DateTime? baslangic = null, DateTime? bitis = null);
    
    /// <summary>
    /// İşlem tipi bazlı istatistikler
    /// </summary>
    Task<List<AuditLogIslemStat>> GetIslemStatistikleriAsync(DateTime baslangic, DateTime bitis);
    
    /// <summary>
    /// Kullanıcı bazlı istatistikler
    /// </summary>
    Task<List<AuditLogKullaniciStat>> GetKullaniciStatistikleriAsync(DateTime baslangic, DateTime bitis);
    
    #endregion
    
    #region Temizlik
    
    /// <summary>
    /// Eski logları siler (retention policy)
    /// </summary>
    Task<int> CleanupOldLogsAsync(int gunSayisi);
    
    /// <summary>
    /// Logları arşivler
    /// </summary>
    Task<string> ArchiveLogsAsync(DateTime oncesiTarih);
    
    #endregion
}

#region DTOs

/// <summary>
/// Audit log oluşturma DTO
/// </summary>
public class AuditLogCreateDto
{
    public string IslemTipi { get; set; } = string.Empty;
    public string EntityAdi { get; set; } = string.Empty;
    public int? EntityId { get; set; }
    public string? EntityGuid { get; set; }
    public string? EskiDeger { get; set; }
    public string? YeniDeger { get; set; }
    public string? DegisenAlanlar { get; set; }
    public string? Aciklama { get; set; }
    public string? Kategori { get; set; }
    public string Seviye { get; set; } = AuditSeviyeleri.Info;
    public bool Basarili { get; set; } = true;
    public string? HataMesaji { get; set; }
    public long? IslemSuresiMs { get; set; }
}

/// <summary>
/// Audit log filtreleme DTO
/// </summary>
public class AuditLogFiltre
{
    public int Sayfa { get; set; } = 1;
    public int SayfaBoyutu { get; set; } = 50;
    public DateTime? BaslangicTarihi { get; set; }
    public DateTime? BitisTarihi { get; set; }
    public string? IslemTipi { get; set; }
    public string? EntityAdi { get; set; }
    public int? EntityId { get; set; }
    public int? KullaniciId { get; set; }
    public string? Kategori { get; set; }
    public string? Seviye { get; set; }
    public bool? Basarili { get; set; }
    public string? AramaMetni { get; set; }
    public string SiralamaAlani { get; set; } = "IslemTarihi";
    public bool AzalanSiralama { get; set; } = true;
}

/// <summary>
/// Sayfalanmış audit log sonucu
/// </summary>
public class AuditLogPagedResult
{
    public List<AuditLog> Items { get; set; } = [];
    public int ToplamKayit { get; set; }
    public int ToplamSayfa { get; set; }
    public int MevcutSayfa { get; set; }
    public int SayfaBoyutu { get; set; }
}

/// <summary>
/// Audit log dashboard istatistikleri
/// </summary>
public class AuditLogDashboard
{
    public int ToplamLog { get; set; }
    public int BugunkuLog { get; set; }
    public int BasarisizIslem { get; set; }
    public int KritikIslem { get; set; }
    public int AktifKullanici { get; set; }
    public List<AuditLogGunlukStat> GunlukTrend { get; set; } = [];
    public List<AuditLogIslemStat> IslemDagilimi { get; set; } = [];
    public List<AuditLogKategoriStat> KategoriDagilimi { get; set; } = [];
    public List<AuditLog> SonIslemler { get; set; } = [];
}

/// <summary>
/// Günlük istatistik
/// </summary>
public class AuditLogGunlukStat
{
    public DateTime Tarih { get; set; }
    public int ToplamIslem { get; set; }
    public int BasariliIslem { get; set; }
    public int BasarisizIslem { get; set; }
}

/// <summary>
/// İşlem tipi istatistiği
/// </summary>
public class AuditLogIslemStat
{
    public string IslemTipi { get; set; } = string.Empty;
    public int Sayi { get; set; }
    public decimal Yuzde { get; set; }
}

/// <summary>
/// Kategori istatistiği
/// </summary>
public class AuditLogKategoriStat
{
    public string Kategori { get; set; } = string.Empty;
    public int Sayi { get; set; }
    public decimal Yuzde { get; set; }
}

/// <summary>
/// Kullanıcı istatistiği
/// </summary>
public class AuditLogKullaniciStat
{
    public int KullaniciId { get; set; }
    public string KullaniciAdi { get; set; } = string.Empty;
    public int ToplamIslem { get; set; }
    public int CreateSayisi { get; set; }
    public int UpdateSayisi { get; set; }
    public int DeleteSayisi { get; set; }
    public DateTime SonIslemTarihi { get; set; }
}

#endregion




