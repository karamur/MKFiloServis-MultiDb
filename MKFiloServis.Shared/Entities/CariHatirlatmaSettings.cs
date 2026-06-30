namespace MKFiloServis.Shared.Entities;

/// <summary>
/// Cari hatırlatma ayarları (firma bazlı JSON dosyasında saklanır)
/// </summary>
public class CariHatirlatmaSettings
{
    public bool HatirlatmaAktif { get; set; } = false;
    public int KontrolSaati { get; set; } = 9; // Günün hangi saatinde kontrol yapılacak (0-23)
    
    // Vade Hatırlatmaları
    public bool VadeYaklasanHatirlatma { get; set; } = true;
    public int[] VadeYaklasanGunleri { get; set; } = [7, 3, 1]; // Kaç gün önce hatırlat
    public bool VadeGecmisHatirlatma { get; set; } = true;
    public int[] VadeGecmisGunleri { get; set; } = [1, 3, 7, 15, 30]; // Kaç gün geçince hatırlat
    public decimal VadeGecmisMinTutar { get; set; } = 100; // Minimum tutar (altı için hatırlatma yapılmaz)
    
    // Borç Eşik Hatırlatmaları  
    public bool BorcEsikHatirlatma { get; set; } = true;
    public decimal BorcEsikTutar { get; set; } = 10000; // Bu tutarı geçince uyarı
    public bool AlacakEsikHatirlatma { get; set; } = true;
    public decimal AlacakEsikTutar { get; set; } = 10000; // Bu tutarı geçince uyarı
    
    // Tahsilat Hatırlatmaları
    public bool TahsilatHatirlatma { get; set; } = true;
    public int TahsilatHatirlatmaGunu { get; set; } = 1; // Ayın hangi günü hatırlat (aylık özet)
    
    // Hareketsiz Cari Hatırlatması
    public bool HareketsizCariHatirlatma { get; set; } = false;
    public int HareketsizCariGunSayisi { get; set; } = 90; // Bu kadar gün hareketsiz ise hatırlat
    
    // E-posta Ayarları
    public bool EmailGonder { get; set; } = true;
    public bool AdminlereGonder { get; set; } = true;
    public string? EkEmailAdresleri { get; set; } // Virgülle ayrılmış ek email adresleri
    
    // Müşteriye Direkt E-posta
    public bool MusteriyeEmailGonder { get; set; } = false;
    public string? MusteriEmailSablonu { get; set; } // HTML şablon
    
    // Sistem Bildirimi Ayarları
    public bool SistemBildirimiOlustur { get; set; } = true;
    public BildirimOncelik VadeYaklasanOncelik { get; set; } = BildirimOncelik.Normal;
    public BildirimOncelik VadeGecmisOncelik { get; set; } = BildirimOncelik.Yuksek;
    public BildirimOncelik BorcEsikOncelik { get; set; } = BildirimOncelik.Kritik;
    
    // Son Çalışma Bilgisi
    public DateTime? SonKontrolTarihi { get; set; }
    public int SonKontrolUyariSayisi { get; set; }
}

/// <summary>
/// Cari hatırlatma kaydı - geçmiş hatırlatmaları saklar
/// </summary>
public class CariHatirlatma : BaseEntity, IFirmaTenant
{
    public int CariId { get; set; }
    public virtual Cari Cari { get; set; } = null!;
    
    public int? FaturaId { get; set; }
    public virtual Fatura? Fatura { get; set; }
    
    public CariHatirlatmaTipi Tip { get; set; }
    public string Baslik { get; set; } = string.Empty;
    public string? Aciklama { get; set; }
    public decimal? Tutar { get; set; }
    public DateTime? VadeTarihi { get; set; }
    public int? VadeGecenGun { get; set; }
    
    // Gönderim Durumu
    public bool EmailGonderildi { get; set; } = false;
    public DateTime? EmailGonderimTarihi { get; set; }
    public bool MusteriyeEmailGonderildi { get; set; } = false;
    public bool BildirimOlusturuldu { get; set; } = false;
    public int? BildirimId { get; set; }
    
    // Firma
    public int? FirmaId { get; set; }
    public virtual Firma? Firma { get; set; }
}

public enum CariHatirlatmaTipi
{
    VadeYaklasan = 1,       // Vadesi yaklaşan fatura
    VadeGecmis = 2,         // Vadesi geçmiş fatura
    BorcEsikAsildi = 3,     // Borç eşik tutarı aşıldı
    AlacakEsikAsildi = 4,   // Alacak eşik tutarı aşıldı
    TahsilatHatirlatma = 5, // Aylık tahsilat hatırlatması
    HareketsizCari = 6,     // Uzun süredir hareket yok
    OdemeAlindi = 7,        // Ödeme alındı bildirimi
    FaturaOdendi = 8        // Fatura tamamen ödendi
}

/// <summary>
/// Hatırlatma rapor modeli
/// </summary>
public class CariHatirlatmaRapor
{
    public DateTime RaporTarihi { get; set; } = DateTime.Now;
    public int ToplamUyariSayisi { get; set; }
    public decimal ToplamVadeGecmisTutar { get; set; }
    public int VadeYaklasanFaturaSayisi { get; set; }
    public int VadeGecmisFaturaSayisi { get; set; }
    public int BorcEsikAsilanCariSayisi { get; set; }
    public int AlacakEsikAsilanCariSayisi { get; set; }
    public int HareketsizCariSayisi { get; set; }
    
    public List<CariHatirlatmaDetay> Detaylar { get; set; } = new();
}

public class CariHatirlatmaDetay
{
    public int CariId { get; set; }
    public string CariKodu { get; set; } = string.Empty;
    public string CariUnvan { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Telefon { get; set; }
    public CariHatirlatmaTipi Tip { get; set; }
    public string Aciklama { get; set; } = string.Empty;
    public decimal? Tutar { get; set; }
    public DateTime? VadeTarihi { get; set; }
    public int? VadeGecenGun { get; set; }
    public string? FaturaNo { get; set; }
    public int? FaturaId { get; set; }
    public BildirimOncelik Oncelik { get; set; } = BildirimOncelik.Normal;
}


