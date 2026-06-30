using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MKFiloServis.Shared.Entities;

/// <summary>
/// Fatura şablon ayarları - Özelleştirilebilir fatura görünümü
/// </summary>
public class FaturaSablon : BaseEntity, IFirmaTenant
{
    public int FirmaId { get; set; }
    int? IFirmaTenant.FirmaId { get => FirmaId; set => FirmaId = value ?? 0; }
    public virtual Firma? Firma { get; set; }

    [Required]
    [StringLength(100)]
    public string SablonAdi { get; set; } = "Varsayılan Şablon";

    public bool Varsayilan { get; set; } = false;
    public bool Aktif { get; set; } = true;

    // Sayfa Ayarları
    public SayfaBoyutu SayfaBoyutu { get; set; } = SayfaBoyutu.A4;
    public SayfaYonelimi SayfaYonelimi { get; set; } = SayfaYonelimi.Dikey;
    public int SayfaKenarBoslukSol { get; set; } = 30;
    public int SayfaKenarBoslukSag { get; set; } = 30;
    public int SayfaKenarBoslukUst { get; set; } = 30;
    public int SayfaKenarBoslukAlt { get; set; } = 30;

    // Firma Logo ve Başlık Ayarları
    public bool LogoGoster { get; set; } = true;
    public LogoKonumu LogoKonumu { get; set; } = LogoKonumu.Sol;
    public int LogoGenislik { get; set; } = 150;
    public int LogoYukseklik { get; set; } = 60;
    public string? OzelLogo { get; set; } // Base64 - firma logosu yerine özel logo

    public bool FirmaAdiGoster { get; set; } = true;
    public int FirmaAdiFontBoyutu { get; set; } = 16;
    public bool FirmaAdresGoster { get; set; } = true;
    public bool FirmaTelefonGoster { get; set; } = true;
    public bool FirmaEmailGoster { get; set; } = true;
    public bool FirmaVergiGoster { get; set; } = true;

    // Renk Ayarları
    [StringLength(10)]
    public string AnaPrimaryRenk { get; set; } = "#1976D2"; // Mavi
    [StringLength(10)]
    public string AnaSecondaryRenk { get; set; } = "#424242"; // Koyu gri
    [StringLength(10)]
    public string TabloBaslikArkaplanRenk { get; set; } = "#E3F2FD"; // Açık mavi
    [StringLength(10)]
    public string TabloBaslikYaziRenk { get; set; } = "#1976D2";
    [StringLength(10)]
    public string TabloSatirCizgiRenk { get; set; } = "#E0E0E0";
    [StringLength(10)]
    public string ToplamArkaplanRenk { get; set; } = "#FFF8E1"; // Açık sarı

    // Font Ayarları
    [StringLength(50)]
    public string FontAdi { get; set; } = "Arial";
    public int VarsayilanFontBoyutu { get; set; } = 10;
    public int BaslikFontBoyutu { get; set; } = 14;

    // Fatura Başlığı Ayarları
    [StringLength(100)]
    public string FaturaBaslikMetni { get; set; } = "FATURA";
    public BaslikKonumu FaturaBaslikKonumu { get; set; } = BaslikKonumu.Sag;

    // Bilgi Kutuları Ayarları
    public bool FaturaBilgiKutusuGoster { get; set; } = true;
    public bool CariBilgiKutusuGoster { get; set; } = true;
    public bool KutuCercevesiGoster { get; set; } = true;
    public int KutuPadding { get; set; } = 10;

    // Tablo Ayarları
    public bool TabloSiraNoGoster { get; set; } = true;
    public bool TabloKdvSutunuGoster { get; set; } = true;
    public bool TabloIskontoSutunuGoster { get; set; } = false;
    public bool TabloZebraDeseni { get; set; } = false;
    [StringLength(10)]
    public string TabloZebraRenk { get; set; } = "#F5F5F5";

    // Toplam Bölümü Ayarları
    public ToplamKonumu ToplamKonumu { get; set; } = ToplamKonumu.Sag;
    public int ToplamBolumGenislik { get; set; } = 200;
    public bool AraToplamGoster { get; set; } = true;
    public bool KdvToplamGoster { get; set; } = true;
    public bool OdenenGoster { get; set; } = true;
    public bool KalanGoster { get; set; } = true;

    // Banka Bilgileri
    public bool BankaBilgileriGoster { get; set; } = false;
    [StringLength(500)]
    public string? BankaBilgileri { get; set; }

    // Alt Bilgi / Notlar
    public bool NotlarGoster { get; set; } = true;
    [StringLength(1000)]
    public string? AltBilgiMetni { get; set; } // Fatura altına eklenen sabit metin
    public bool SayfaNumarasiGoster { get; set; } = true;

    // Kaşe ve İmza Alanları
    public bool KaseAlaniGoster { get; set; } = false;
    public bool ImzaAlaniGoster { get; set; } = false;
    [StringLength(100)]
    public string? ImzaMetni { get; set; } = "İmza";
    public string? KaseResmi { get; set; } // Base64

    // QR Kod Ayarları
    public bool QrKodGoster { get; set; } = false;
    public QrKodIcerik QrKodIcerik { get; set; } = QrKodIcerik.FaturaNo;
}

/// <summary>
/// Sayfa boyutu seçenekleri
/// </summary>
public enum SayfaBoyutu
{
    A4 = 0,
    A5 = 1,
    Letter = 2
}

/// <summary>
/// Sayfa yönelimi
/// </summary>
public enum SayfaYonelimi
{
    Dikey = 0,      // Portrait
    Yatay = 1       // Landscape
}

/// <summary>
/// Logo konumu seçenekleri
/// </summary>
public enum LogoKonumu
{
    Sol = 0,
    Orta = 1,
    Sag = 2
}

/// <summary>
/// Başlık konumu seçenekleri
/// </summary>
public enum BaslikKonumu
{
    Sol = 0,
    Orta = 1,
    Sag = 2
}

/// <summary>
/// Toplam bölümü konumu
/// </summary>
public enum ToplamKonumu
{
    Sol = 0,
    Orta = 1,
    Sag = 2
}

/// <summary>
/// QR kod içerik türü
/// </summary>
public enum QrKodIcerik
{
    FaturaNo = 0,
    FaturaUrl = 1,
    OdemeLinki = 2,
    OzelMetin = 3
}

/// <summary>
/// Fatura yazdırma/email için request modeli
/// </summary>
public class FaturaYazdirRequest
{
    public int FaturaId { get; set; }
    public int? SablonId { get; set; } // null ise varsayılan şablon
    public bool EmailGonder { get; set; } = false;
    public string? EmailAdresi { get; set; }
    public string? EmailKonu { get; set; }
    public string? EmailMesaj { get; set; }
}

/// <summary>
/// Fatura PDF oluşturma sonucu
/// </summary>
public class FaturaPdfResult
{
    public bool Basarili { get; set; }
    public string? Mesaj { get; set; }
    public byte[]? PdfData { get; set; }
    public string? DosyaAdi { get; set; }
    public string? Base64Data { get; set; }
}


