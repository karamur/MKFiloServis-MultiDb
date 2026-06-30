using System.ComponentModel.DataAnnotations;

namespace MKFiloServis.Shared.Entities;

#region Araç İlan Yayın Yönetimi

/// <summary>
/// Araç ilanı yayın platformları
/// </summary>
public class IlanPlatformu : BaseEntity
{
    [Required]
    [StringLength(50)]
    public string PlatformAdi { get; set; } = string.Empty;

    [StringLength(100)]
    public string? WebSiteUrl { get; set; }

    [StringLength(100)]
    public string? ApiUrl { get; set; }

    [StringLength(200)]
    public string? ApiKey { get; set; }

    [StringLength(100)]
    public string? ApiSecret { get; set; }

    [StringLength(100)]
    public string? KullaniciAdi { get; set; }

    [StringLength(100)]
    public string? Sifre { get; set; }

    public PlatformTipi PlatformTipi { get; set; } = PlatformTipi.WebSitesi;

    public bool Aktif { get; set; } = true;

    public int SiraNo { get; set; } = 0;

    [StringLength(50)]
    public string? Icon { get; set; } // bi-globe, bi-phone vs.

    [StringLength(20)]
    public string? Renk { get; set; } // Bootstrap renk sınıfı

    public bool OtomatikYayinDestegi { get; set; } = false;

    [StringLength(500)]
    public string? Notlar { get; set; }

    // Navigation
    public virtual ICollection<AracIlanYayin> Yayinlar { get; set; } = new List<AracIlanYayin>();
}

/// <summary>
/// Araç ilanı yayın kaydı - hangi araç hangi platformda yayında
/// </summary>
public class AracIlanYayin : BaseEntity
{
    public int AracId { get; set; }
    public virtual Arac Arac { get; set; } = null!;

    public int PlatformId { get; set; }
    public virtual IlanPlatformu Platform { get; set; } = null!;

    // Yayın Bilgileri
    public IlanYayinDurum Durum { get; set; } = IlanYayinDurum.Taslak;

    public DateTime? YayinBaslangic { get; set; }
    public DateTime? YayinBitis { get; set; }

    [StringLength(100)]
    public string? PlatformIlanNo { get; set; } // Platformdaki ilan numarası

    [StringLength(500)]
    public string? PlatformIlanUrl { get; set; } // Platformdaki ilan linki

    // Fiyat Bilgileri
    public decimal YayinFiyati { get; set; }
    public bool FiyatGizli { get; set; } = false;

    [StringLength(50)]
    public string? FiyatAciklama { get; set; } // "Pazarlık Olur", "Takas Olur" vs.

    // İstatistikler
    public int GoruntulenmeSayisi { get; set; } = 0;
    public int TiklamaSayisi { get; set; } = 0;
    public int FavorilenmeSayisi { get; set; } = 0;
    public int MesajSayisi { get; set; } = 0;
    public DateTime? SonGuncelleme { get; set; }

    // Öne Çıkarma
    public bool OneCikarildiMi { get; set; } = false;
    public DateTime? OneCikarmaBitis { get; set; }
    public decimal? OneCikarmaBedeli { get; set; }

    [StringLength(500)]
    public string? Notlar { get; set; }

    // Yayınlayan kullanıcı
    public int? YayinlayanKullaniciId { get; set; }
    public virtual Kullanici? YayinlayanKullanici { get; set; }
}

/// <summary>
/// Araç ilan içeriği - her platform için özelleştirilebilir
/// </summary>
public class AracIlanIcerik : BaseEntity
{
    public int AracId { get; set; }
    public virtual Arac Arac { get; set; } = null!;

    public int? PlatformId { get; set; } // null ise genel ilan içeriği
    public virtual IlanPlatformu? Platform { get; set; }

    [Required]
    [StringLength(200)]
    public string IlanBasligi { get; set; } = string.Empty;

    public string? IlanAciklamasi { get; set; }

    // Özellik Vurguları
    public string? OzellikListesi { get; set; } // JSON array

    // Fotoğraflar
    public string? FotografListesi { get; set; } // JSON array - sıralı dosya yolları

    public string? VitrinFotografi { get; set; } // Ana fotoğraf

    // SEO
    [StringLength(200)]
    public string? MetaBaslik { get; set; }

    [StringLength(500)]
    public string? MetaAciklama { get; set; }

    [StringLength(200)]
    public string? AnahtarKelimeler { get; set; }
}

#endregion

#region Kullanıcı Tercihleri

/// <summary>
/// Kullanıcı uygulama tercihleri
/// </summary>
public class KullaniciTercihi : BaseEntity
{
    public int KullaniciId { get; set; }
    public virtual Kullanici Kullanici { get; set; } = null!;

    // Anasayfa Tercihleri
    public string? VarsayilanAnasayfa { get; set; } // dashboard, filo-operasyon, araclar vs.

    public bool AnasayfaWidgetGoster { get; set; } = true;

    public string? AnasayfaWidgetSirasi { get; set; } // JSON - widget sıralaması

    // Tema Tercihleri
    public string? Tema { get; set; } // light, dark, auto
    public string? SidebarDurum { get; set; } // expanded, collapsed, auto

    // Bildirim Tercihleri
    public bool EmailBildirimAktif { get; set; } = true;
    public bool TarayiciBildirimAktif { get; set; } = true;
    public bool SesBildirimAktif { get; set; } = false;

    // Liste Tercihleri
    public int VarsayilanSayfaBoyutu { get; set; } = 25;
    public string? VarsayilanSiralama { get; set; } // JSON - entity bazlı sıralama tercihleri

    // Diğer
    public string? DigerTercihler { get; set; } // JSON - ek tercihler
}

/// <summary>
/// Kullanıcının son açtığı sayfalar / son işlemler
/// </summary>
public class KullaniciSonIslem : BaseEntity
{
    public int KullaniciId { get; set; }
    public virtual Kullanici Kullanici { get; set; } = null!;

    [Required]
    [StringLength(200)]
    public string SayfaYolu { get; set; } = string.Empty;

    [StringLength(200)]
    public string? SayfaBasligi { get; set; }

    [StringLength(50)]
    public string? Icon { get; set; }

    public DateTime ErisimZamani { get; set; } = DateTime.UtcNow;

    public int ErisimSayisi { get; set; } = 1;
}

#endregion

#region Enums

public enum PlatformTipi
{
    WebSitesi = 1,
    MobilUygulama = 2,
    SosyalMedya = 3,
    Marketplace = 4,
    Diger = 99
}

public enum IlanYayinDurum
{
    Taslak = 0,
    OnayBekliyor = 1,
    Aktif = 2,
    Durduruldu = 3,
    SuresiDoldu = 4,
    Satildi = 5,
    Silindi = 6
}

#endregion


