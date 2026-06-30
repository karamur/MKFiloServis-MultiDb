using MKFiloServis.Shared.Entities;
using System.ComponentModel.DataAnnotations;

namespace MKFiloServis.Web.Models;

/// <summary>
/// Toplu fatura oluşturma kaynak türleri
/// </summary>
public enum TopluFaturaKaynak
{
    Puantaj = 1,      // Puantaj kayıtlarından
    Sozlesme = 2,     // Sözleşme bazlı dönemsel
    Manuel = 3        // Manuel seçim
}

/// <summary>
/// Toplu fatura durumu
/// </summary>
public enum TopluFaturaDurum
{
    Hazir = 1,        // Fatura kesilebilir
    EksikBilgi = 2,   // Eksik bilgi var
    FaturaKesildi = 3 // Fatura zaten kesilmiş
}

/// <summary>
/// Toplu fatura oluşturma filtre parametreleri
/// </summary>
public class TopluFaturaFiltre
{
    public int Yil { get; set; } = DateTime.Now.Year;
    public int Ay { get; set; } = DateTime.Now.Month;
    public TopluFaturaKaynak Kaynak { get; set; } = TopluFaturaKaynak.Puantaj;
    public FaturaYonu FaturaYonu { get; set; } = FaturaYonu.Giden; // Giden = Satış, Gelen = Alış
    public int? CariId { get; set; } // Belirli bir cari için
    public int? FirmaId { get; set; } // Belirli bir firma için
    public bool SadeceFaturaKesilmemis { get; set; } = true;
    public bool CariBazliGrupla { get; set; } = true; // Cari bazlı gruplama
}

/// <summary>
/// Toplu fatura önizleme - cari bazlı gruplu
/// </summary>
public class TopluFaturaOnizleme
{
    public int CariId { get; set; }
    public string CariUnvan { get; set; } = string.Empty;
    public string CariKod { get; set; } = string.Empty;
    public string? VergiNo { get; set; }
    public TopluFaturaDurum Durum { get; set; } = TopluFaturaDurum.Hazir;
    public string? DurumMesaji { get; set; }
    
    // Fatura bilgileri
    public FaturaTipi FaturaTipi { get; set; } = FaturaTipi.SatisFaturasi;
    public EFaturaTipi EFaturaTipi { get; set; } = EFaturaTipi.EArsiv;
    public DateTime FaturaTarihi { get; set; } = DateTime.Now;
    public DateTime? VadeTarihi { get; set; }
    public int KdvOrani { get; set; } = 20;
    
    // Tevkifat
    public bool TevkifatliMi { get; set; } = false;
    public decimal TevkifatOrani { get; set; } = 0;
    public string? TevkifatKodu { get; set; }
    
    // Kalem listesi
    public List<TopluFaturaKalemOnizleme> Kalemler { get; set; } = new();
    
    // Toplamlar
    public decimal AraToplam => Kalemler.Sum(k => k.Tutar);
    public decimal KdvToplam => Kalemler.Sum(k => k.KdvTutar);
    public decimal TevkifatToplam => TevkifatliMi ? KdvToplam * TevkifatOrani / 100 : 0;
    public decimal GenelToplam => AraToplam + KdvToplam - TevkifatToplam;
    
    // Kaynak bilgileri
    public List<int> PuantajKayitIdleri { get; set; } = new();
    public int KayitSayisi => PuantajKayitIdleri.Count;
    
    // Seçim durumu
    public bool Secili { get; set; } = true;
}

/// <summary>
/// Toplu fatura kalem önizleme
/// </summary>
public class TopluFaturaKalemOnizleme
{
    public int? PuantajKayitId { get; set; }
    public string Aciklama { get; set; } = string.Empty;
    public string? GuzergahAdi { get; set; }
    public decimal Miktar { get; set; } = 1;
    public string Birim { get; set; } = "Adet";
    public decimal BirimFiyat { get; set; }
    public decimal Tutar => Miktar * BirimFiyat;
    public int KdvOrani { get; set; } = 20;
    public decimal KdvTutar => Tutar * KdvOrani / 100;
    public decimal ToplamTutar => Tutar + KdvTutar;
    
    // Düzenleme için
    public bool Secili { get; set; } = true;
    public bool Duzenlenebilir { get; set; } = true;
}

/// <summary>
/// Toplu fatura oluşturma sonucu
/// </summary>
public class TopluFaturaSonuc
{
    public bool Basarili { get; set; }
    public string Mesaj { get; set; } = string.Empty;
    public int OlusturulanFaturaSayisi { get; set; }
    public int BasarisizFaturaSayisi { get; set; }
    public decimal ToplamTutar { get; set; }
    public List<OlusturulanFaturaBilgi> OlusturulanFaturalar { get; set; } = new();
    public List<string> Hatalar { get; set; } = new();
    public List<string> Uyarilar { get; set; } = new();
}

/// <summary>
/// Oluşturulan fatura bilgisi
/// </summary>
public class OlusturulanFaturaBilgi
{
    public int FaturaId { get; set; }
    public string FaturaNo { get; set; } = string.Empty;
    public string CariUnvan { get; set; } = string.Empty;
    public decimal Tutar { get; set; }
    public int KalemSayisi { get; set; }
    public int PuantajKayitSayisi { get; set; }
}

/// <summary>
/// Toplu fatura özeti (dashboard için)
/// </summary>
public class TopluFaturaOzet
{
    public int Yil { get; set; }
    public int Ay { get; set; }
    public string DonemAdi => $"{Ay:00}/{Yil}";
    
    // Gelir (Satış) Faturaları
    public int GelirFaturaKesilecekCariSayisi { get; set; }
    public int GelirFaturaKesilecekKayitSayisi { get; set; }
    public decimal GelirFaturaKesilecekTutar { get; set; }
    public int GelirFaturaKesilenSayisi { get; set; }
    public decimal GelirFaturaKesilenTutar { get; set; }
    
    // Gider (Alış) Faturaları
    public int GiderFaturaAlinacakCariSayisi { get; set; }
    public int GiderFaturaAlinacakKayitSayisi { get; set; }
    public decimal GiderFaturaAlinacakTutar { get; set; }
    public int GiderFaturaAlinanSayisi { get; set; }
    public decimal GiderFaturaAlinanTutar { get; set; }
}

/// <summary>
/// Fatura şablonu
/// </summary>
public class FaturaSablonu
{
    public int Id { get; set; }
    public string SablonAdi { get; set; } = string.Empty;
    public FaturaTipi FaturaTipi { get; set; }
    public int? VarsayilanKdvOrani { get; set; }
    public bool TevkifatliMi { get; set; }
    public decimal? TevkifatOrani { get; set; }
    public string? TevkifatKodu { get; set; }
    public int? VadeGunSayisi { get; set; } // Fatura tarihinden itibaren
    public string? VarsayilanAciklama { get; set; }
    public string? KalemSablonu { get; set; } // JSON formatında kalem şablonu
}

/// <summary>
/// Cari bazlı fatura ayarları
/// </summary>
public class CariFaturaAyar
{
    public int CariId { get; set; }
    public string CariUnvan { get; set; } = string.Empty;
    public int? VarsayilanKdvOrani { get; set; }
    public bool TevkifatliMi { get; set; }
    public decimal? TevkifatOrani { get; set; }
    public string? TevkifatKodu { get; set; }
    public int? VadeGunSayisi { get; set; }
    public EFaturaTipi EFaturaTipi { get; set; } = EFaturaTipi.EArsiv;
    public string? FaturaAciklamaSablonu { get; set; }
}



