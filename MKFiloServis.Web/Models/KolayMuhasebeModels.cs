using MKFiloServis.Shared.Entities;

namespace MKFiloServis.Web.Models;

/// <summary>
/// Kolay Muhasebe Giriş - İşlem Türleri
/// </summary>
public enum KolayIslemTuru
{
    GelirFatura = 1,      // Müşteriye kesilen fatura (Satış)
    GiderFatura = 2,      // Tedarikçiden gelen fatura (Alış)
    MasrafGirisi = 3,     // Genel masraf (ofis, araç vb.)
    TahsilatGirisi = 4,   // Müşteriden tahsilat
    OdemeGirisi = 5,      // Tedarikçiye ödeme
    MahsupKaydi = 6,      // Hesaplar arası transfer/mahsup
    AvansGirisi = 7       // Personel avans
}

/// <summary>
/// Kolay Muhasebe Giriş Form Modeli
/// </summary>
public class KolayMuhasebeGiris
{
    public KolayIslemTuru IslemTuru { get; set; } = KolayIslemTuru.GiderFatura;
    public DateTime IslemTarihi { get; set; } = DateTime.Today;
    public string? BelgeNo { get; set; }  // Fatura No, Fiş No vb.
    public DateTime? VadeTarihi { get; set; }

    // Cari Bilgileri
    public int? CariId { get; set; }
    public string? CariUnvan { get; set; }
    public CariTipi? YeniCariTipi { get; set; }  // Yeni cari oluşturulacaksa

    // Tutar Bilgileri
    public decimal AraToplam { get; set; }
    public decimal KdvOrani { get; set; } = 20;
    public decimal KdvTutar { get; set; }
    public decimal GenelToplam { get; set; }
    public bool KdvDahilMi { get; set; } = false;  // true = KDV Dahil tutar girildi

    // Tevkifat (Opsiyonel)
    public bool TevkifatliMi { get; set; } = false;
    public decimal TevkifatOrani { get; set; } = 0;
    public string? TevkifatKodu { get; set; }
    public decimal TevkifatTutar { get; set; }

    // Ödeme/Tahsilat için
    public int? BankaHesapId { get; set; }
    public OdemeYontemi OdemeYontemi { get; set; } = OdemeYontemi.Nakit;

    // Avans için (personel doğrudan seçimi)
    public int? PersonelId { get; set; }
    public string? PersonelAdSoyad { get; set; }
    public int? PersonelAvansHesapId { get; set; }   // 195.01.XXX hesap Id
    public int? PersonelBorcHesapId { get; set; }    // 335.xx.xxx hesap Id

    // Masraf için
    public int? MasrafKalemiId { get; set; }
    public int? AracId { get; set; }  // Araç masrafı ise
    public MasrafOdemeKaynagi MasrafOdemeKaynagi { get; set; } = MasrafOdemeKaynagi.KasaBanka;

    // Açıklama
    public string? Aciklama { get; set; }
    public string? Notlar { get; set; }

    // Detay kalemler (stok/hizmet kalemleri)
    public List<KolayFaturaKalem> Kalemler { get; set; } = new();
    public bool DetayliGiris { get; set; } = false;

    // Hesaplanan değerler
    public decimal OdenecekTutar => TevkifatliMi ? GenelToplam - TevkifatTutar : GenelToplam;
}

/// <summary>
/// Muhasebe Kaydı Önizleme Modeli
/// </summary>
public class MuhasebeOnizleme
{
    public string FisNo { get; set; } = string.Empty;
    public DateTime FisTarihi { get; set; }
    public FisTipi FisTipi { get; set; }
    public string Aciklama { get; set; } = string.Empty;
    public List<MuhasebeKalemOnizleme> Kalemler { get; set; } = new();
    
    public decimal ToplamBorc => Kalemler.Sum(k => k.Borc);
    public decimal ToplamAlacak => Kalemler.Sum(k => k.Alacak);
    public bool Dengeli => Math.Abs(ToplamBorc - ToplamAlacak) < 0.01m;
}

public class MuhasebeKalemOnizleme
{
    public int SiraNo { get; set; }
    public string HesapKodu { get; set; } = string.Empty;
    public string HesapAdi { get; set; } = string.Empty;
    public int? HesapId { get; set; }
    public decimal Borc { get; set; }
    public decimal Alacak { get; set; }
    public string? Aciklama { get; set; }
    public int? CariId { get; set; }
    public string? CariUnvan { get; set; }
    
    // Manuel düzenleme için
    public bool DuzenlendiMi { get; set; } = false;
}

/// <summary>
/// Kolay Muhasebe Kaydetme Sonucu
/// </summary>
public class KolayMuhasebeSonuc
{
    public bool Basarili { get; set; }
    public string Mesaj { get; set; } = string.Empty;
    
    // Oluşturulan kayıtlar
    public int? FaturaId { get; set; }
    public int? MasrafId { get; set; }
    public int? BankaHareketId { get; set; }
    public int? MuhasebeFisId { get; set; }
    public int? CariId { get; set; }
    
    public List<string> Uyarilar { get; set; } = new();
}

/// <summary>
/// Masraf Kalemi basit model
/// </summary>
public class MasrafKalemiBasit
{
    public int Id { get; set; }
    public string Ad { get; set; } = string.Empty;
    public string? MuhasebeHesapKodu { get; set; }
}

/// <summary>
/// Banka Hesap basit model
/// </summary>
public class BankaHesapBasit
{
    public int Id { get; set; }
    public string HesapAdi { get; set; } = string.Empty;
    public string? MuhasebeHesapKodu { get; set; }
    public decimal Bakiye { get; set; }
}

public enum OdemeYontemi
{
    Nakit = 1,
    BankaHavalesi = 2,
    Cek = 3,
    KrediKarti = 4,
    Diger = 99
}

public enum MasrafOdemeKaynagi
{
    KasaBanka = 1,
    Personel = 2,
    Cari = 3
}

/// <summary>
/// Kolay fatura kalem modeli (stok/hizmet kalemi)
/// </summary>
public class KolayFaturaKalem
{
    public int SiraNo { get; set; }
    public string Aciklama { get; set; } = string.Empty;
    public decimal Miktar { get; set; } = 1;
    public string Birim { get; set; } = "Adet";
    public decimal BirimFiyat { get; set; }
    public decimal KdvOrani { get; set; } = 20;
    public decimal KdvTutar => Math.Round(AraTutar * KdvOrani / 100, 2);
    public decimal AraTutar => Math.Round(Miktar * BirimFiyat, 2);
    public decimal ToplamTutar => AraTutar + KdvTutar;

    // Stok takibi için (opsiyonel)
    public int? StokId { get; set; }
    public string? StokKodu { get; set; }
}

/// <summary>
/// Stok basit model (dropdown için)
/// </summary>
public class StokBasit
{
    public int Id { get; set; }
    public string StokKodu { get; set; } = string.Empty;
    public string StokAdi { get; set; } = string.Empty;
    public string Birim { get; set; } = "Adet";
    public decimal AlisFiyati { get; set; }
    public decimal SatisFiyati { get; set; }
    public decimal KdvOrani { get; set; } = 20;
    public decimal MevcutStok { get; set; }
}



