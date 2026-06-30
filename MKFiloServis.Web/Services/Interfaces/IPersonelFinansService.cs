using MKFiloServis.Shared.Entities;

namespace MKFiloServis.Web.Services.Interfaces;

public interface IPersonelFinansService
{
    // Avans İşlemleri
    Task<List<PersonelAvans>> GetAvanslarAsync(int? firmaId = null, int? personelId = null, DateTime? baslangic = null, DateTime? bitis = null);
    Task<PersonelAvans?> GetAvansByIdAsync(int id);
    Task<PersonelAvans> CreateAvansAsync(PersonelAvans avans, bool muhasebeKaydiOlustur = true);
    Task<PersonelAvans> UpdateAvansAsync(PersonelAvans avans);
    Task DeleteAvansAsync(int id);
    Task<PersonelAvans> IptalEtAvansAsync(int id, string iptalNedeni);
    
    // Avans Mahsup
    Task<PersonelAvansMahsup> MahsupEtAvansAsync(int avansId, PersonelAvansMahsup mahsup);
    Task<decimal> MaasaAcikAvansMahsupEtAsync(int maasId, DateTime? mahsupTarihi = null, string? aciklama = null);
    Task<List<PersonelAvansMahsup>> GetAvansMahsuplasmalarAsync(int avansId);
    Task DeleteMahsupAsync(int mahsupId);
    
    // Borç İşlemleri
    Task<List<PersonelBorc>> GetBorclarAsync(int? firmaId = null, int? personelId = null, BorcOdemeDurum? durum = null);
    Task<PersonelBorc?> GetBorcByIdAsync(int id);
    Task<PersonelBorc> CreateBorcAsync(PersonelBorc borc, bool muhasebeKaydiOlustur = true);
    Task<PersonelBorc> UpdateBorcAsync(PersonelBorc borc);
    Task DeleteBorcAsync(int id);
    Task<PersonelBorc> IptalEtBorcAsync(int id, string iptalNedeni);
    
    // Borç Ödeme
    Task<PersonelBorcOdeme> OdemeYapBorcAsync(int borcId, PersonelBorcOdeme odeme, bool muhasebeKaydiOlustur = true);
    Task<List<PersonelBorcOdeme>> GetBorcOdemelerAsync(int borcId);
    Task DeleteBorcOdemeAsync(int odemeId);
    
    // Personel Özet Bilgileri
    Task<PersonelFinansOzet> GetPersonelFinansOzetAsync(int personelId);
    Task<List<PersonelFinansOzet>> GetTumPersonelFinansOzetAsync(int? firmaId = null);
    Task<List<PersonelCebindenHarcamaItem>> GetPersonelCebindenHarcamalarAsync(int personelId);
    
    // Ayarlar
    Task<PersonelFinansAyar?> GetAyarlarAsync(int? firmaId = null);
    Task<PersonelFinansAyar> SaveAyarlarAsync(PersonelFinansAyar ayar);
    
    // Raporlama
    Task<byte[]> ExportAvansRaporAsync(List<PersonelAvans> avanslar);
    Task<byte[]> ExportBorcRaporAsync(List<PersonelBorc> borclar);
    Task<byte[]> ExportPersonelOzetRaporAsync(List<PersonelFinansOzet> ozetler);
    
    // Toplu İşlemler
    Task<int> TopluAvansMahsupAsync(List<int> avansIdler, DateTime mahsupTarihi, string aciklama);
    Task<int> TopluBorcOdemeAsync(List<int> borcIdler, DateTime odemeTarihi, BorcOdemeSekli odemeSekli, int? bankaHesapId);
}

/// <summary>
/// Personel Finans Özet Bilgisi
/// </summary>
public class PersonelFinansOzet
{
    public int PersonelId { get; set; }
    public string PersonelKodu { get; set; } = string.Empty;
    public string PersonelAdSoyad { get; set; } = string.Empty;
    public string? Departman { get; set; }
    public bool Aktif { get; set; }
    
    // Avans Bilgileri
    public int ToplamAvansSayisi { get; set; }
    public decimal ToplamAvans { get; set; }
    public decimal MahsupEdilenAvans { get; set; }
    public decimal KalanAvans => ToplamAvans - MahsupEdilenAvans;
    public int AcikAvansSayisi { get; set; }
    
    // Borç Bilgileri
    public int ToplamBorcSayisi { get; set; }
    public decimal ToplamBorc { get; set; }
    public decimal OdenenBorc { get; set; }
    public decimal KalanBorc => ToplamBorc - OdenenBorc;
    public int OdenmemişBorcSayisi { get; set; }
    
    // Cebinden Harcama (AracMasraf + BankaKasaHareket, PersoneleOdendi=false)
    public decimal ToplamHarcama { get; set; }
    public int HarcamaAdet { get; set; }

    // Net Durum
    public decimal NetDurum => KalanBorc - KalanAvans; // Pozitif ise personele borç, negatif ise personelden alacak
    public string NetDurumAciklama => NetDurum > 0 ? "Personele Borç" : NetDurum < 0 ? "Personelden Alacak" : "Bakiye Yok";
}

/// <summary>
/// Personelin cebinden yaptığı harcama kalemi
/// </summary>
public class PersonelCebindenHarcamaItem
{
    public DateTime Tarih { get; set; }
    public string Aciklama { get; set; } = "";
    public decimal Tutar { get; set; }
    public string Kaynak { get; set; } = ""; // "AracMasraf" | "BankaHareket"
    public int KaynakId { get; set; }
    public bool PersoneleOdendi { get; set; }
}

/// <summary>
/// Avans Oluşturma/Güncelleme Request
/// </summary>
public class AvansRequest
{
    public int? Id { get; set; }
    public int PersonelId { get; set; }
    public int? FirmaId { get; set; }
    public DateTime AvansTarihi { get; set; } = DateTime.Today;
    public decimal Tutar { get; set; }
    public string? Aciklama { get; set; }
    public AvansOdemeSekli OdemeSekli { get; set; }
    public int? BankaHesapId { get; set; }
    public bool MuhasebeKaydiOlustur { get; set; } = true;
}

/// <summary>
/// Borç Oluşturma/Güncelleme Request
/// </summary>
public class BorcRequest
{
    public int? Id { get; set; }
    public int PersonelId { get; set; }
    public int? FirmaId { get; set; }
    public DateTime BorcTarihi { get; set; } = DateTime.Today;
    public decimal Tutar { get; set; }
    public string BorcNedeni { get; set; } = string.Empty;
    public string? Aciklama { get; set; }
    public BorcTipi BorcTipi { get; set; }
    public DateTime? PlanlananOdemeTarihi { get; set; }
    public bool MuhasebeKaydiOlustur { get; set; } = true;
}




