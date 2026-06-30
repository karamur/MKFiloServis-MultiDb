using System;
using System.Threading;
using System.Threading.Tasks;

namespace MKFiloServis.Web.Services.Interfaces;

/// <summary>
/// Distributed cache servisi interface'i
/// Redis veya Memory cache ile çalışabilir
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Cache'den veri al
    /// </summary>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;
    
    /// <summary>
    /// Cache'e veri yaz (varsayılan süre: 5 dakika)
    /// </summary>
    Task SetAsync<T>(string key, T value, CancellationToken cancellationToken = default) where T : class;
    
    /// <summary>
    /// Cache'e veri yaz (özel süre)
    /// </summary>
    Task SetAsync<T>(string key, T value, TimeSpan absoluteExpiration, CancellationToken cancellationToken = default) where T : class;
    
    /// <summary>
    /// Cache'e veri yaz (sliding expiration)
    /// </summary>
    Task SetWithSlidingAsync<T>(string key, T value, TimeSpan slidingExpiration, CancellationToken cancellationToken = default) where T : class;
    
    /// <summary>
    /// Cache'den veri sil
    /// </summary>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Belirli prefix ile başlayan tüm key'leri sil
    /// </summary>
    Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Cache'de key var mı kontrol et
    /// </summary>
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Cache'den al, yoksa factory ile oluştur ve cache'e yaz
    /// </summary>
    Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? absoluteExpiration = null, CancellationToken cancellationToken = default) where T : class;
    
    /// <summary>
    /// Cache süresini yenile (refresh)
    /// </summary>
    Task RefreshAsync(string key, CancellationToken cancellationToken = default);
}

/// <summary>
/// Cache key sabitleri - tutarlı key isimlendirme için
/// </summary>
public static class CacheKeys
{
    public const string Prefix = "CRMFilo:";
    
    // Dashboard
    public const string DashboardOzet = Prefix + "Dashboard:Ozet";
    public const string DashboardGrafikler = Prefix + "Dashboard:Grafikler";
    
    // Cari
    public static string CariListesi => Prefix + "Cari:Liste";
    public static string CariDetay(int id) => $"{Prefix}Cari:Detay:{id}";
    public static string CariBakiye(int id) => $"{Prefix}Cari:Bakiye:{id}";
    
    // Araç
    public static string AracListesi => Prefix + "Arac:Liste";
    public static string AracAktif => Prefix + "Arac:Aktif";
    public static string AracDetay(int id) => $"{Prefix}Arac:Detay:{id}";
    
    // Şoför
    public static string SoforListesi => Prefix + "Sofor:Liste";
    public static string SoforAktif => Prefix + "Sofor:Aktif";
    public static string SoforDetay(int id) => $"{Prefix}Sofor:Detay:{id}";
    
    // Güzergah
    public static string GuzergahListesi => Prefix + "Guzergah:Liste";
    public static string GuzergahAktif => Prefix + "Guzergah:Aktif";

    // Kapasite
    public static string KapasiteListesi => Prefix + "Kapasite:Liste";
    public static string KapasiteAktif => Prefix + "Kapasite:Aktif";
    public static string KapasitePrefix => Prefix + "Kapasite:";
    
    // Fatura
    public static string FaturaListesi => Prefix + "Fatura:Liste";
    public static string FaturaDetay(int id) => $"{Prefix}Fatura:Detay:{id}";
    
    // Masraf Kalemleri
    public const string MasrafKalemiPrefix = Prefix + "MasrafKalemi:";
    public static string MasrafKalemiListesi => Prefix + "MasrafKalemi:Liste";
    public static string MasrafKalemiAktif => Prefix + "MasrafKalemi:Aktif";

    // Güzergah (prefix ile invalidate)
    public const string GuzergahPrefix = Prefix + "Guzergah:";

    // Araç (prefix ile invalidate)
    public const string AracPrefix = Prefix + "Arac:";

    // Şoför (prefix ile invalidate)
    public const string SoforPrefix = Prefix + "Sofor:";
    
    // İstatistikler
    public static string AylikIstatistik(int yil, int ay) => $"{Prefix}Istatistik:Aylik:{yil}:{ay}";
    public static string YillikIstatistik(int yil) => $"{Prefix}Istatistik:Yillik:{yil}";
}

/// <summary>
/// Cache süreleri - tutarlı TTL için
/// </summary>
public static class CacheDurations
{
    /// <summary>Kısa süreli cache - 1 dakika</summary>
    public static readonly TimeSpan Short = TimeSpan.FromMinutes(1);
    
    /// <summary>Varsayılan cache - 5 dakika</summary>
    public static readonly TimeSpan Default = TimeSpan.FromMinutes(5);
    
    /// <summary>Orta süreli cache - 15 dakika</summary>
    public static readonly TimeSpan Medium = TimeSpan.FromMinutes(15);
    
    /// <summary>Uzun süreli cache - 1 saat</summary>
    public static readonly TimeSpan Long = TimeSpan.FromHours(1);
    
    /// <summary>Günlük cache - 24 saat (istatistikler için)</summary>
    public static readonly TimeSpan Daily = TimeSpan.FromHours(24);
    
    /// <summary>Sliding expiration - 10 dakika</summary>
    public static readonly TimeSpan Sliding = TimeSpan.FromMinutes(10);
}




