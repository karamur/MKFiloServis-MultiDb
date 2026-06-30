using MKFiloServis.Shared.Entities;
using Microsoft.AspNetCore.Components.Forms;

namespace MKFiloServis.Web.Services.Interfaces;

/// <summary>
/// EBYS Gelen/Giden Evrak Yönetimi Servis Interface
/// </summary>
public interface IEbysEvrakService
{
    // Evrak CRUD
    Task<List<EbysEvrak>> GetEvraklarAsync(EbysEvrakFiltre? filtre = null);
    Task<EbysEvrak?> GetEvrakByIdAsync(int id);
    Task<EbysEvrak> CreateEvrakAsync(EbysEvrakOlusturModel model);
    Task<EbysEvrak> UpdateEvrakAsync(EbysEvrakGuncelleModel model);
    Task DeleteEvrakAsync(int id);
    
    // Kategori işlemleri
    Task<List<EbysEvrakKategori>> GetKategorilerAsync();
    Task<EbysEvrakKategori> CreateKategoriAsync(EbysEvrakKategori kategori);
    Task UpdateKategoriAsync(EbysEvrakKategori kategori);
    Task DeleteKategoriAsync(int id);
    
    // Dosya işlemleri
    Task<EbysEvrakDosya> DosyaYukleAsync(int evrakId, IBrowserFile file, bool asilNusha = false);
    Task<EbysEvrakDosya?> GetDosyaAsync(int dosyaId);
    Task<byte[]?> GetDosyaIcerikAsync(int dosyaId);
    Task DosyaSilAsync(int dosyaId);
    Task<EbysEvrakDosya> DosyaGuncelleAsync(int dosyaId, IBrowserFile file, string? degisiklikNotu = null);
    
    // Atama işlemleri
    Task<EbysEvrakAtama> AtamaYapAsync(EbysEvrakAtamaModel model);
    Task AtamaTamamlaAsync(int atamaId, string sonuc);
    Task AtamaReddetAsync(int atamaId, string sebep);
    Task<List<EbysEvrakAtama>> GetEvrakAtalamariAsync(int evrakId);
    Task<List<EbysEvrakAtama>> GetKullaniciAtamalariAsync(int kullaniciId);
    
    // Durum değişikliği
    Task DurumDegistirAsync(int evrakId, EbysEvrakDurum yeniDurum, string? aciklama = null);
    
    // Hareket geçmişi
    Task<List<EbysEvrakHareket>> GetEvrakHareketleriAsync(int evrakId);
    
    // İstatistikler
    Task<EbysEvrakIstatistik> GetIstatistiklerAsync();
    
    // Yeni evrak numarası oluşturma
    Task<string> YeniEvrakNoOlusturAsync(EvrakYonu yon);
}

/// <summary>
/// Evrak listeleme filtresi
/// </summary>
public class EbysEvrakFiltre
{
    public string? AramaMetni { get; set; }
    public EvrakYonu? Yon { get; set; }
    public int? KategoriId { get; set; }
    public EbysEvrakDurum? Durum { get; set; }
    public EvrakOncelik? Oncelik { get; set; }
    public DateTime? BaslangicTarihi { get; set; }
    public DateTime? BitisTarihi { get; set; }
    public int? AtananKullaniciId { get; set; }
    public bool SadeceCevapBekleyenler { get; set; }
}

/// <summary>
/// Evrak oluşturma modeli
/// </summary>
public class EbysEvrakOlusturModel
{
    public EvrakYonu Yon { get; set; } = EvrakYonu.Gelen;
    public DateTime EvrakTarihi { get; set; } = DateTime.Today;
    public string Konu { get; set; } = string.Empty;
    public string? Ozet { get; set; }
    public string? GonderenKurum { get; set; }
    public string? AliciKurum { get; set; }
    public string? GelisNo { get; set; }
    public DateTime? GelisTarihi { get; set; }
    public string? GidisNo { get; set; }
    public DateTime? GonderimTarihi { get; set; }
    public GonderimYontemi GonderimYontemi { get; set; } = GonderimYontemi.Elden;
    public int? KategoriId { get; set; }
    public EvrakOncelik Oncelik { get; set; } = EvrakOncelik.Normal;
    public EvrakGizlilik Gizlilik { get; set; } = EvrakGizlilik.Normal;
    public bool CevapGerekli { get; set; }
    public DateTime? CevapSuresi { get; set; }
    public int? UstEvrakId { get; set; }
    public string? Aciklama { get; set; }
}

/// <summary>
/// Evrak güncelleme modeli
/// </summary>
public class EbysEvrakGuncelleModel : EbysEvrakOlusturModel
{
    public int Id { get; set; }
    public string? Notlar { get; set; }
}

/// <summary>
/// Evrak atama modeli
/// </summary>
public class EbysEvrakAtamaModel
{
    public int EvrakId { get; set; }
    public int? AtananKullaniciId { get; set; }
    public int? AtananDepartmanId { get; set; }
    public string? Talimat { get; set; }
    public DateTime? TeslimTarihi { get; set; }
}

/// <summary>
/// Dashboard istatistikleri
/// </summary>
public class EbysEvrakIstatistik
{
    public int ToplamGelen { get; set; }
    public int ToplamGiden { get; set; }
    public int BekleyenGelen { get; set; }
    public int BekleyenGiden { get; set; }
    public int CevapBekleyen { get; set; }
    public int BugunGelenSayisi { get; set; }
    public int BugunGidenSayisi { get; set; }
    public int GecikmisCevap { get; set; }
    public Dictionary<string, int> KategoriBazindaDagilim { get; set; } = new();
    public Dictionary<string, int> DurumBazindaDagilim { get; set; } = new();
}




