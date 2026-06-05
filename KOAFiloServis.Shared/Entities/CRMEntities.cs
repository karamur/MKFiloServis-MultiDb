using System.ComponentModel.DataAnnotations;

namespace KOAFiloServis.Shared.Entities;

#region Bildirim/Uyarı Sistemi

/// <summary>
/// Kullanıcı bildirimleri - uyarı sistemi
/// </summary>
public class Bildirim : BaseEntity
{
    public int KullaniciId { get; set; }
    public virtual Kullanici Kullanici { get; set; } = null!;

    [Required]
    [StringLength(200)]
    public string Baslik { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Icerik { get; set; }

    public BildirimTipi Tip { get; set; } = BildirimTipi.Bilgi;
    public BildirimOncelik Oncelik { get; set; } = BildirimOncelik.Normal;

    public bool Okundu { get; set; } = false;
    public DateTime? OkunmaTarihi { get; set; }

    // İlişkili kayıt bilgisi
    public string? IliskiliTablo { get; set; } // "Cari", "Fatura", "Arac" vs.
    public int? IliskiliKayitId { get; set; }
    public string? Link { get; set; } // Yönlendirilecek sayfa

    // Zamanlama
    public DateTime? SonGosterimTarihi { get; set; }
    public bool Tekrarli { get; set; } = false;
}

public enum BildirimTipi
{
    Bilgi = 0,
    Uyari = 1,
    Hata = 2,
    Basari = 3,
    BelgeSuresi = 4,
    OdemeBildirimi = 5,
    Hatirlatici = 6,
    Mesaj = 7,
    FaturaVade = 8,
    EhliyetBitis = 9,
    SrcBelgesi = 10,
    Psikoteknik = 11,
    SaglikRaporu = 12,
    TrafikSigorta = 13,
    Kasko = 14,
    Muayene = 15,
    DestekTalebi = 16,
    Sistem = 17
}

public enum BildirimOncelik
{
    Dusuk = 0,
    Normal = 1,
    Yuksek = 2,
    Kritik = 3
}

/// <summary>
/// Bildirim ayarları - Kullanıcı bazlı bildirim tercihleri
/// </summary>
public class BildirimAyar : BaseEntity
{
    public int KullaniciId { get; set; }
    public virtual Kullanici? Kullanici { get; set; }

    // Hangi bildirimleri alsın?
    public bool FaturaVadeUyarisi { get; set; } = true;
    public bool EhliyetBitisUyarisi { get; set; } = true;
    public bool SrcBelgesiUyarisi { get; set; } = true;
    public bool PsikoteknikUyarisi { get; set; } = true;
    public bool SaglikRaporuUyarisi { get; set; } = true;
    public bool TrafikSigortaUyarisi { get; set; } = true;
    public bool KaskoUyarisi { get; set; } = true;
    public bool MuayeneUyarisi { get; set; } = true;
    public bool DestekTalebiUyarisi { get; set; } = true;
    public bool SistemBildirimleri { get; set; } = true;

    // E-posta tercihleri
    public bool EpostaAlsin { get; set; } = false;
    public string? EpostaAdresi { get; set; }

    // SMS tercihleri
    public bool SmsAlsin { get; set; } = false;
    [StringLength(20)]
    public string? SmsTelefon { get; set; }
    public bool SmsVadeHatirlatma { get; set; } = true;
    public bool SmsBelgeHatirlatma { get; set; } = false;

    // Kaç gün önceden uyarı verilsin?
    public int VadeUyariGunSayisi { get; set; } = 7;
    public int BelgeUyariGunSayisi { get; set; } = 30;
}

/// <summary>
/// E-posta bildirim gönderim logu - tekrar gönderimi önlemek için
/// </summary>
public class EpostaBildirimLog : BaseEntity
{
    public int KullaniciId { get; set; }
    public virtual Kullanici? Kullanici { get; set; }

    [StringLength(200)]
    public string EpostaAdresi { get; set; } = string.Empty;

    public int UyariSayisi { get; set; }
    public DateTime GonderimTarihi { get; set; }
    public bool Basarili { get; set; } = true;

    [StringLength(500)]
    public string? HataMesaji { get; set; }
}

#endregion

#region Mesajlaşma Sistemi

/// <summary>
/// Dahili mesajlaşma sistemi
/// </summary>
public class Mesaj : BaseEntity
{
    public int GonderenId { get; set; }
    public virtual Kullanici Gonderen { get; set; } = null!;

    public int? AliciId { get; set; } // null ise tüm kullanıcılara
    public virtual Kullanici? Alici { get; set; }

    [Required]
    [StringLength(200)]
    public string Konu { get; set; } = string.Empty;

    [Required]
    public string Icerik { get; set; } = string.Empty;

    public MesajTipi Tip { get; set; } = MesajTipi.Dahili;
    public MesajDurum Durum { get; set; } = MesajDurum.Gonderildi;

    public bool Okundu { get; set; } = false;
    public DateTime? OkunmaTarihi { get; set; }

    // Dış sistemler için
    public string? DisAlici { get; set; } // Telefon veya email
    public string? DisGonderimId { get; set; } // WhatsApp/SMS ID

    // Yanıt zinciri
    public int? UstMesajId { get; set; }
    public virtual Mesaj? UstMesaj { get; set; }
    public virtual ICollection<Mesaj> Yanitlar { get; set; } = new List<Mesaj>();
}

public enum MesajTipi
{
    Dahili = 0,
    Email = 1,
    SMS = 2,
    WhatsApp = 3
}

public enum MesajDurum
{
    Taslak = 0,
    Gonderildi = 1,
    Iletildi = 2,
    Okundu = 3,
    Hata = 4
}

/// <summary>
/// Email ayarları
/// </summary>
public class EmailAyar : BaseEntity
{
    public int? KullaniciId { get; set; } // null ise firma geneli
    public virtual Kullanici? Kullanici { get; set; }

    [Required]
    [StringLength(100)]
    public string SmtpSunucu { get; set; } = string.Empty;

    public int SmtpPort { get; set; } = 587;
    public bool SslKullan { get; set; } = true;

    [StringLength(100)]
    public string? ImapSunucu { get; set; }

    public int ImapPort { get; set; } = 993;
    public bool ImapSslKullan { get; set; } = true;
    public bool GelenKutusuAktif { get; set; } = false;

    [StringLength(100)]
    public string GelenKlasoru { get; set; } = "INBOX";

    [Required]
    [StringLength(100)]
    public string Email { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Sifre { get; set; }

    [StringLength(100)]
    public string? GonderenAdi { get; set; }

    public bool Aktif { get; set; } = true;
}

/// <summary>
/// WhatsApp ayarları
/// </summary>
public class WhatsAppAyar : BaseEntity
{
    public int? KullaniciId { get; set; }
    public virtual Kullanici? Kullanici { get; set; }

    [StringLength(20)]
    public string? Telefon { get; set; }

    [StringLength(500)]
    public string? ApiKey { get; set; }

    [StringLength(200)]
    public string? WebhookUrl { get; set; }

    public string? HizliSablonlarJson { get; set; }

    public bool Aktif { get; set; } = false;
}

/// <summary>
/// SMS ayarları - provider bazlı (NetGSM, İletimerkezi, Twilio vb.)
/// </summary>
public class SmsAyar : BaseEntity, IFirmaTenant
{
    public int? FirmaId { get; set; }
    public virtual Firma? Firma { get; set; }

    [Required]
    public SmsProvider Provider { get; set; } = SmsProvider.NetGsm;

    [StringLength(100)]
    public string? KullaniciAdi { get; set; } // API kullanıcı adı

    [StringLength(200)]
    public string? ApiKey { get; set; } // API anahtarı / şifre

    [StringLength(50)]
    public string? GondericiNumara { get; set; } // Originator / Başlık (FIRMAADI gibi)

    [StringLength(200)]
    public string? ApiUrl { get; set; } // Özel API URL (opsiyonel)

    public bool Aktif { get; set; } = false;

    // Bakiye/Limit Takibi
    public decimal? Bakiye { get; set; }
    public DateTime? SonBakiyeSorguTarihi { get; set; }

    // İstatistikler
    public int ToplamGonderilenSms { get; set; } = 0;
    public int ToplamBasarisizSms { get; set; } = 0;
    public DateTime? SonGonderimTarihi { get; set; }
}

/// <summary>
/// SMS sağlayıcı türleri
/// </summary>
public enum SmsProvider
{
    NetGsm = 0,        // NetGSM
    Iletimerkezi = 1,  // İletimerkezi
    Twilio = 2,        // Twilio
    Mutlucell = 3,     // Mutlucell
    JetSms = 4,        // JetSMS
    Verimor = 5,       // Verimor
    Diger = 99         // Diğer/Özel
}

/// <summary>
/// SMS gönderim logu - gönderilen SMS kayıtları
/// </summary>
public class SmsLog : BaseEntity
{
    public int? SmsAyarId { get; set; }
    public virtual SmsAyar? SmsAyar { get; set; }

    [Required]
    [StringLength(20)]
    public string Telefon { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string Mesaj { get; set; } = string.Empty;

    public SmsGonderimDurum Durum { get; set; } = SmsGonderimDurum.Bekliyor;

    [StringLength(100)]
    public string? ProviderMesajId { get; set; } // Provider'dan dönen ID

    [StringLength(500)]
    public string? HataMesaji { get; set; }

    public DateTime? GonderimTarihi { get; set; }
    public DateTime? IletimTarihi { get; set; }

    // İlişkili kayıt
    [StringLength(50)]
    public string? IliskiliTablo { get; set; } // "Cari", "Fatura", "Personel" vb.
    public int? IliskiliKayitId { get; set; }

    // SMS tipi
    public SmsTipi Tip { get; set; } = SmsTipi.Bildirim;

    // Gönderen kullanıcı
    public int? GonderenKullaniciId { get; set; }
    public virtual Kullanici? GonderenKullanici { get; set; }
}

/// <summary>
/// SMS gönderim durumu
/// </summary>
public enum SmsGonderimDurum
{
    Bekliyor = 0,      // Henüz gönderilmedi
    Gonderildi = 1,    // Provider'a iletildi
    Iletildi = 2,      // Alıcıya ulaştı
    Basarisiz = 3,     // Gönderilemedi
    Iptal = 4          // İptal edildi
}

/// <summary>
/// SMS tipi
/// </summary>
public enum SmsTipi
{
    Bildirim = 0,       // Genel bildirim
    VadeHatirlatma = 1, // Vade hatırlatması
    OdemeBildirimi = 2, // Ödeme bildirimi
    FaturaBildirimi = 3,// Fatura bildirimi
    Duyuru = 4,         // Toplu duyuru
    DogrulamaKodu = 5,  // OTP/Doğrulama kodu
    Pazarlama = 6       // Pazarlama SMS'i
}

/// <summary>
/// SMS şablonları
/// </summary>
public class SmsSablon : BaseEntity, IFirmaTenant
{
    public int? FirmaId { get; set; }
    public virtual Firma? Firma { get; set; }

    [Required]
    [StringLength(100)]
    public string Adi { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Aciklama { get; set; }

    [Required]
    [StringLength(500)]
    public string Sablon { get; set; } = string.Empty; // {MusteriAdi}, {FaturaNo}, {Tutar} gibi değişkenler

    public SmsTipi Tip { get; set; } = SmsTipi.Bildirim;

    public bool Aktif { get; set; } = true;
    public bool Varsayilan { get; set; } = false;
}

#endregion

#region Hatırlatıcı/Randevu Sistemi

/// <summary>
/// Kullanıcı hatırlatıcıları ve randevuları
/// </summary>
public class Hatirlatici : BaseEntity
{
    public int KullaniciId { get; set; }
    public virtual Kullanici Kullanici { get; set; } = null!;

    [Required]
    [StringLength(200)]
    public string Baslik { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Aciklama { get; set; }

    public HatirlaticiTip Tip { get; set; } = HatirlaticiTip.Hatirlatici;

    public DateTime BaslangicTarihi { get; set; }
    public DateTime? BitisTarihi { get; set; }
    public bool TumGun { get; set; } = false;

    // Tekrar ayarları
    public TekrarTipi TekrarTipi { get; set; } = TekrarTipi.Yok;
    public int TekrarAraligi { get; set; } = 1; // Her X gün/hafta/ay
    public DateTime? TekrarBitisTarihi { get; set; }

    // Bildirim
    public int BildirimDakikaOnce { get; set; } = 15;
    public bool EmailBildirim { get; set; } = false;
    public bool PushBildirim { get; set; } = true;

    // İlişkili kayıt
    public string? IliskiliTablo { get; set; }
    public int? IliskiliKayitId { get; set; }

    // Durum
    public HatirlaticiDurum Durum { get; set; } = HatirlaticiDurum.Bekliyor;
    public string? Renk { get; set; } = "#0d6efd";

    // Cari/Kişi bağlantısı
    public int? CariId { get; set; }
    public virtual Cari? Cari { get; set; }
}

public enum HatirlaticiTip
{
    Hatirlatici = 0,
    Randevu = 1,
    Toplanti = 2,
    Gorev = 3,
    Arama = 4,
    Ziyaret = 5
}

public enum TekrarTipi
{
    Yok = 0,
    Gunluk = 1,
    Haftalik = 2,
    Aylik = 3,
    Yillik = 4
}

public enum HatirlaticiDurum
{
    Bekliyor = 0,
    Tamamlandi = 1,
    Iptal = 2,
    Ertelendi = 3
}

#endregion

#region Kullanıcı-Cari Eşleştirme

/// <summary>
/// Kullanıcıya bağlı cariler
/// </summary>
public class KullaniciCari : BaseEntity
{
    public int KullaniciId { get; set; }
    public virtual Kullanici Kullanici { get; set; } = null!;

    public int CariId { get; set; }
    public virtual Cari Cari { get; set; } = null!;

    // İzinler
    public bool EkstreGorebilir { get; set; } = true;
    public bool FaturaGorebilir { get; set; } = true;
    public bool OdemeYapabilir { get; set; } = false;
    public bool DuzenlemeYapabilir { get; set; } = false;

    // Cari tipi
    public KullaniciCariTip Tip { get; set; } = KullaniciCariTip.Musteri;

    public string? Not { get; set; }
}

public enum KullaniciCariTip
{
    Musteri = 0,      // Müşteri carisi
    Tedarikci = 1,    // Tedarikçi/Satıcı
    Personel = 2,     // Personel carisi
    Ozel = 3          // Özel tanımlı
}

#endregion

#region Cari İletişim Geçmişi

/// <summary>
/// Cari iletişim geçmişi - arama, email, ziyaret, not kayıtları
/// </summary>
public class CariIletisimNot : BaseEntity
{
    public int CariId { get; set; }
    public virtual Cari Cari { get; set; } = null!;

    public int? KullaniciId { get; set; }
    public virtual Kullanici? Kullanici { get; set; }

    [Required]
    [StringLength(200)]
    public string Konu { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Notlar { get; set; }

    public IletisimTipi IletisimTipi { get; set; } = IletisimTipi.Not;

    public DateTime IletisimTarihi { get; set; } = DateTime.Now;

    [StringLength(100)]
    public string? IletisimYapanKisi { get; set; }

    [StringLength(100)]
    public string? MuhatapKisi { get; set; }

    // Sonraki aksiyon
    [StringLength(500)]
    public string? SonrakiAksiyon { get; set; }
    public DateTime? SonrakiAksiyonTarihi { get; set; }
    public bool AksiyonTamamlandi { get; set; } = false;
}

public enum IletisimTipi
{
    Not = 0,
    TelefonArama = 1,
    Email = 2,
    Ziyaret = 3,
    Toplanti = 4,
    WhatsApp = 5,
    Teklif = 6
}

#endregion

#region Dashboard Widget

/// <summary>
/// Kullanıcı dashboard widget ayarları
/// </summary>
public class DashboardWidget : BaseEntity
{
    public int KullaniciId { get; set; }
    public virtual Kullanici Kullanici { get; set; } = null!;

    [Required]
    [StringLength(50)]
    public string WidgetKodu { get; set; } = string.Empty; // "bildirimler", "mesajlar", "randevular" vs.

    public int Sira { get; set; } = 0;
    public int Kolon { get; set; } = 0; // 0-11 (12 sütunlu grid)
    public int Genislik { get; set; } = 4; // col-md-X

    public bool Gorunur { get; set; } = true;
    public bool Kucultulmus { get; set; } = false;

    public string? Ayarlar { get; set; } // JSON formatında widget ayarları
}

#endregion

#region Webhook Sistemi

/// <summary>
/// Webhook endpoint tanımları - dış sistemlere olay bildirimi
/// </summary>
public class WebhookEndpoint : BaseEntity
{
    [Required]
    [StringLength(100)]
    public string Ad { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Aciklama { get; set; }

    [Required]
    [StringLength(500)]
    public string Url { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Secret { get; set; } // HMAC imza için

    public bool Aktif { get; set; } = true;

    // Retry ayarları
    public int MaxRetry { get; set; } = 3;
    public int RetryDelaySaniye { get; set; } = 30;

    // Hangi olayları dinlesin?
    public string? OlayFiltresi { get; set; } // JSON array: ["Fatura.Olusturuldu", "Cari.Guncellendi"]

    // HTTP ayarları
    public string HttpMethod { get; set; } = "POST";
    public string? Headers { get; set; } // JSON formatında ek headerlar

    // İstatistikler
    public int ToplamGonderim { get; set; } = 0;
    public int BasariliGonderim { get; set; } = 0;
    public int BasarisizGonderim { get; set; } = 0;
    public DateTime? SonGonderimTarihi { get; set; }
    public DateTime? SonBasariliTarih { get; set; }

    // İlişkiler
    public virtual ICollection<WebhookLog> Loglar { get; set; } = new List<WebhookLog>();
}

/// <summary>
/// Webhook gönderim logları
/// </summary>
public class WebhookLog : BaseEntity
{
    public int WebhookEndpointId { get; set; }
    public virtual WebhookEndpoint WebhookEndpoint { get; set; } = null!;

    [Required]
    [StringLength(100)]
    public string OlayTipi { get; set; } = string.Empty; // "Fatura.Olusturuldu", "Cari.Guncellendi"

    public string? Payload { get; set; } // Gönderilen JSON veri

    public WebhookLogDurum Durum { get; set; } = WebhookLogDurum.Bekliyor;

    public int HttpStatusCode { get; set; } = 0;
    public string? ResponseBody { get; set; }

    public DateTime? GonderimTarihi { get; set; }
    public DateTime? YanitTarihi { get; set; }
    public int SureMilisaniye { get; set; } = 0;

    public int RetryCount { get; set; } = 0;
    public string? HataMesaji { get; set; }

    // İlişkili kayıt
    public string? IliskiliTablo { get; set; }
    public int? IliskiliKayitId { get; set; }
}

public enum WebhookLogDurum
{
    Bekliyor = 0,
    Gonderiliyor = 1,
    Basarili = 2,
    Basarisiz = 3,
    YenidenDeneniyor = 4,
    Iptal = 5
}

/// <summary>
/// Webhook olay tipleri
/// </summary>
public static class WebhookOlayTipleri
{
    // Fatura olayları
    public const string FaturaOlusturuldu = "Fatura.Olusturuldu";
    public const string FaturaGuncellendi = "Fatura.Guncellendi";
    public const string FaturaSilindi = "Fatura.Silindi";
    public const string FaturaOdendi = "Fatura.Odendi";

    // Cari olayları
    public const string CariOlusturuldu = "Cari.Olusturuldu";
    public const string CariGuncellendi = "Cari.Guncellendi";
    public const string CariSilindi = "Cari.Silindi";

    // Araç olayları
    public const string AracOlusturuldu = "Arac.Olusturuldu";
    public const string AracGuncellendi = "Arac.Guncellendi";
    public const string AracSilindi = "Arac.Silindi";

    // Şoför olayları
    public const string SoforOlusturuldu = "Sofor.Olusturuldu";
    public const string SoforGuncellendi = "Sofor.Guncellendi";
    public const string SoforSilindi = "Sofor.Silindi";

    // Güzergah olayları
    public const string GuzergahOlusturuldu = "Guzergah.Olusturuldu";
    public const string GuzergahGuncellendi = "Guzergah.Guncellendi";
    public const string GuzergahSilindi = "Guzergah.Silindi";

    // Servis çalışması olayları
    public const string ServisCalismasi = "Servis.Calisma";

    // Ödeme olayları
    public const string OdemeAlindi = "Odeme.Alindi";
    public const string OdemeYapildi = "Odeme.Yapildi";

    public static string[] TumOlaylar => new[]
    {
        FaturaOlusturuldu, FaturaGuncellendi, FaturaSilindi, FaturaOdendi,
        CariOlusturuldu, CariGuncellendi, CariSilindi,
        AracOlusturuldu, AracGuncellendi, AracSilindi,
        SoforOlusturuldu, SoforGuncellendi, SoforSilindi,
        GuzergahOlusturuldu, GuzergahGuncellendi, GuzergahSilindi,
        ServisCalismasi, OdemeAlindi, OdemeYapildi
    };
}

#endregion
