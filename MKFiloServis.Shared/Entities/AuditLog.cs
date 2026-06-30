using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MKFiloServis.Shared.Entities;

/// <summary>
/// Sistem genelinde tüm CRUD işlemlerini loglayan audit entity
/// </summary>
public class AuditLog : IFirmaTenant
{
    [Key]
    public int Id { get; set; }
    
    /// <summary>
    /// İşlem tipi: Create, Update, Delete, Read, Login, Logout, Export, Import
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string IslemTipi { get; set; } = string.Empty;
    
    /// <summary>
    /// Etkilenen entity adı (örn: Fatura, Cari, Arac)
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string EntityAdi { get; set; } = string.Empty;
    
    /// <summary>
    /// Etkilenen kaydın ID'si
    /// </summary>
    public int? EntityId { get; set; }
    
    /// <summary>
    /// Entity'nin benzersiz tanımlayıcısı (Guid varsa)
    /// </summary>
    [MaxLength(50)]
    public string? EntityGuid { get; set; }
    
    /// <summary>
    /// İşlemi yapan kullanıcı ID'si
    /// </summary>
    public int? KullaniciId { get; set; }
    
    /// <summary>
    /// İşlemi yapan kullanıcı adı
    /// </summary>
    [MaxLength(100)]
    public string? KullaniciAdi { get; set; }
    
    /// <summary>
    /// Kullanıcının IP adresi
    /// </summary>
    [MaxLength(50)]
    public string? IpAdresi { get; set; }
    
    /// <summary>
    /// Kullanıcının tarayıcı bilgisi
    /// </summary>
    [MaxLength(500)]
    public string? UserAgent { get; set; }
    
    /// <summary>
    /// İşlem yapılan sayfa/endpoint
    /// </summary>
    [MaxLength(500)]
    public string? RequestPath { get; set; }
    
    /// <summary>
    /// Değişiklik öncesi veri (JSON)
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string? EskiDeger { get; set; }
    
    /// <summary>
    /// Değişiklik sonrası veri (JSON)
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string? YeniDeger { get; set; }
    
    /// <summary>
    /// Değişen alanların listesi (JSON array)
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string? DegisenAlanlar { get; set; }
    
    /// <summary>
    /// İşlem açıklaması
    /// </summary>
    [MaxLength(1000)]
    public string? Aciklama { get; set; }
    
    /// <summary>
    /// Aktif firma ID (multi-tenant) — eski adı SirketId, Faz 5.3-B4'te FirmaId'ye rename edildi.
    /// </summary>
    public int? FirmaId { get; set; }
    
    /// <summary>
    /// İşlem tarihi
    /// </summary>
    public DateTime IslemTarihi { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// İşlem süresi (milisaniye)
    /// </summary>
    public long? IslemSuresiMs { get; set; }
    
    /// <summary>
    /// İşlem başarılı mı?
    /// </summary>
    public bool Basarili { get; set; } = true;
    
    /// <summary>
    /// Hata mesajı (başarısız işlemler için)
    /// </summary>
    [MaxLength(2000)]
    public string? HataMesaji { get; set; }
    
    /// <summary>
    /// İşlem kategorisi (Finans, Personel, Araç, Sistem vb.)
    /// </summary>
    [MaxLength(50)]
    public string? Kategori { get; set; }
    
    /// <summary>
    /// Önem seviyesi: Info, Warning, Error, Critical
    /// </summary>
    [MaxLength(20)]
    public string Seviye { get; set; } = "Info";
    
    // Navigation
    public virtual Kullanici? Kullanici { get; set; }
}

/// <summary>
/// Audit log işlem tipleri
/// </summary>
public static class AuditIslemTipleri
{
    public const string Create = "Create";
    public const string Update = "Update";
    public const string Delete = "Delete";
    public const string SoftDelete = "SoftDelete";
    public const string Restore = "Restore";
    public const string Read = "Read";
    public const string Login = "Login";
    public const string Logout = "Logout";
    public const string LoginFailed = "LoginFailed";
    public const string PasswordChange = "PasswordChange";
    public const string PasswordReset = "PasswordReset";
    public const string Export = "Export";
    public const string Import = "Import";
    public const string Print = "Print";
    public const string Email = "Email";
    public const string Approve = "Approve";
    public const string Reject = "Reject";
    public const string Transfer = "Transfer";
    public const string StatusChange = "StatusChange";
}

/// <summary>
/// Audit log kategorileri
/// </summary>
public static class AuditKategorileri
{
    public const string Sistem = "Sistem";
    public const string Kullanici = "Kullanici";
    public const string Finans = "Finans";
    public const string Fatura = "Fatura";
    public const string Cari = "Cari";
    public const string Arac = "Arac";
    public const string Sofor = "Sofor";
    public const string Personel = "Personel";
    public const string Maas = "Maas";
    public const string Puantaj = "Puantaj";
    public const string Ebys = "Ebys";
    public const string Rapor = "Rapor";
    public const string Ayarlar = "Ayarlar";
    public const string Api = "Api";
}

/// <summary>
/// Audit log seviyeleri
/// </summary>
public static class AuditSeviyeleri
{
    public const string Info = "Info";
    public const string Warning = "Warning";
    public const string Error = "Error";
    public const string Critical = "Critical";
}


