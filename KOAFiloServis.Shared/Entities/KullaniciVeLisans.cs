using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KOAFiloServis.Shared.Entities;

#region Lisans

/// <summary>
/// Lisans Bilgileri
/// </summary>
public class Lisans : BaseEntity
{
    [Required]
    public string LisansAnahtari { get; set; } = string.Empty;

    public LisansTuru Tur { get; set; } = LisansTuru.Trial;

    public DateTime BaslangicTarihi { get; set; } = DateTime.UtcNow;
    public DateTime BitisTarihi { get; set; } = DateTime.UtcNow.AddDays(30);

    public string? FirmaAdi { get; set; }
    public string? YetkiliKisi { get; set; }
    public string? Email { get; set; }
    public string? Telefon { get; set; }

    public string MakineKodu { get; set; } = string.Empty;
    public int MaxKullaniciSayisi { get; set; } = 5;

    // Izinler
    public bool ExcelExportIzni { get; set; } = true;
    public bool PdfExportIzni { get; set; } = true;
    public bool RaporlamaIzni { get; set; } = true;
    public bool YedeklemeIzni { get; set; } = true;
    public bool MuhasebeIzni { get; set; } = true;
    public bool SatisModuluIzni { get; set; } = true;

    public string? Imza { get; set; }

    // Hesaplanan ozellikler
    [NotMapped]
    public LisansDurumu Durum => DateTime.UtcNow > BitisTarihi ? LisansDurumu.SuresiDolmus : LisansDurumu.Aktif;
    [NotMapped]
    public int KalanGun => Math.Max(0, (BitisTarihi.Date - DateTime.UtcNow.Date).Days);
    [NotMapped]
    public bool Gecerli => Durum == LisansDurumu.Aktif && !string.IsNullOrEmpty(LisansAnahtari);
}

public enum LisansTuru
{
    Trial = 0,          // 30 gunluk deneme
    Basic = 1,          // Temel - 5 kullanici
    Professional = 2,   // Profesyonel - 10 kullanici
    Enterprise = 3      // Kurumsal - Sinirsiz
}

public enum LisansDurumu
{
    Aktif = 0,
    SuresiDolmus = 1,
    IptalEdilmis = 2,
    Gecersiz = 3
}

#endregion

#region Kullanici ve Rol

/// <summary>
/// Uygulama Kullanicisi
/// </summary>
public class Kullanici : BaseEntity
{
    [Required]
    [StringLength(50)]
    public string KullaniciAdi { get; set; } = string.Empty;

    [Required]
    public string SifreHash { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string AdSoyad { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Email { get; set; }

    [StringLength(20)]
    public string? Telefon { get; set; }

    public int? SoforId { get; set; } // Personel ile iliskilendirme
    public virtual Sofor? Sofor { get; set; }

    public int RolId { get; set; }
    public virtual Rol Rol { get; set; } = null!;

    public bool Aktif { get; set; } = true;
    public DateTime? SonGirisTarihi { get; set; }
    public int BasarisizGirisSayisi { get; set; } = 0;
    public bool Kilitli { get; set; } = false;

    public bool IkiFaktorAktif { get; set; } = false;

    [StringLength(200)]
    public string? IkiFaktorSecretKey { get; set; }

    public DateTime? IkiFaktorEtkinlestirmeTarihi { get; set; }

    // Tercihler
    public string Tema { get; set; } = "Default";
    public bool KompaktMod { get; set; } = false;
    
    // CRM İlişkileri
    public virtual ICollection<Bildirim> Bildirimler { get; set; } = new List<Bildirim>();
    public virtual ICollection<Mesaj> GonderilenMesajlar { get; set; } = new List<Mesaj>();
    public virtual ICollection<Mesaj> AlinanMesajlar { get; set; } = new List<Mesaj>();
    public virtual ICollection<Hatirlatici> Hatirlaticilar { get; set; } = new List<Hatirlatici>();
    public virtual ICollection<KullaniciCari> BagliCariler { get; set; } = new List<KullaniciCari>();
    public virtual ICollection<DashboardWidget> DashboardWidgetlari { get; set; } = new List<DashboardWidget>();
}

/// <summary>
/// Kullanici Rolleri
/// </summary>
public class Rol : BaseEntity
{
    [Required]
    [StringLength(50)]
    public string RolAdi { get; set; } = string.Empty;

    public string? Aciklama { get; set; }

    public string? Renk { get; set; }

    public bool SistemRolu { get; set; } = false; // Admin gibi silinemeyen roller

    // Navigation
    public virtual ICollection<Kullanici> Kullanicilar { get; set; } = new List<Kullanici>();
    public virtual ICollection<RolYetki> Yetkiler { get; set; } = new List<RolYetki>();
}

/// <summary>
/// Rol Yetkileri
/// </summary>
public class RolYetki : BaseEntity
{
    public int RolId { get; set; }
    public virtual Rol Rol { get; set; } = null!;

    [Required]
    [StringLength(100)]
    public string YetkiKodu { get; set; } = string.Empty;

    public bool Izin { get; set; } = false;
}

#endregion

#region Sistem Rolleri

/// <summary>
/// Sistem rol tanimlari - crmdestek projesinden uyarlandi
/// </summary>
public static class SistemRolleri
{
    public const string Admin = "Admin";
    public const string Muhasebeci = "Muhasebeci";
    public const string Operasyon = "Operasyon";
    public const string SatisTemsilcisi = "SatisTemsilcisi";
    public const string Sofor = "SoforRol";
    public const string Kullanici = "Kullanici";
    public const string HoldingYoneticisi = "HoldingYoneticisi";

    public static List<RolTanim> GetAllRoles()
    {
        return new List<RolTanim>
        {
            new(Admin, "Sistem Yoneticisi", "Tum sistem yetkilerine sahip tam yetkili yonetici", "#dc3545", "bi-shield-lock"),
            new(Muhasebeci, "Muhasebeci", "Butce, fatura, banka ve muhasebe islemleri", "#6f42c1", "bi-calculator"),
            new(Operasyon, "Operasyon Sorumlusu", "Arac, sofor, guzergah ve servis islemleri", "#0d6efd", "bi-truck"),
            new(SatisTemsilcisi, "Satis Temsilcisi", "Satis modulu ve piyasa arastirma", "#198754", "bi-graph-up-arrow"),
            new(Sofor, "Sofor", "Kendine atanan arac ve guzergah bilgileri", "#fd7e14", "bi-person-badge"),
            new(Kullanici, "Genel Kullanici", "Temel goruntuleme yetkilerine sahip kullanici", "#6c757d", "bi-person"),
            new(HoldingYoneticisi, "Holding Yoneticisi", "Tum firmalarin konsolide raporlarini ve holding verilerini goruntuleme", "#0dcaf0", "bi-buildings"),
        };
    }

    /// <summary>
    /// Role gore varsayilan yetkileri dondurur
    /// </summary>
    public static List<string> GetDefaultPermissions(string roleName)
    {
        return roleName switch
        {
            Admin => Yetkiler.GetAll(),

            Muhasebeci => new List<string>
            {
                Yetkiler.Dashboard,
                // Ana Menu Erisim
                Yetkiler.MenuCariModulu, Yetkiler.MenuMuhasebe, Yetkiler.MenuFaturaModulu, 
                Yetkiler.MenuBankaKasa, Yetkiler.MenuButceModulu, Yetkiler.MenuRaporlar,
                // Cari
                Yetkiler.CarilerOku, Yetkiler.CarilerYaz, Yetkiler.CarilerDuzenle,
                // Fatura
                Yetkiler.FaturalarOku, Yetkiler.FaturalarYaz, Yetkiler.FaturalarDuzenle, Yetkiler.FaturalarSil,
                Yetkiler.KesilenFaturalarOku, Yetkiler.KesilenFaturalarYaz, Yetkiler.KesilenFaturalarDuzenle,
                Yetkiler.GelenFaturalarOku, Yetkiler.GelenFaturalarYaz, Yetkiler.GelenFaturalarDuzenle,
                // Banka
                Yetkiler.BankaHesaplariOku, Yetkiler.BankaHesaplariYaz, Yetkiler.BankaHesaplariDuzenle,
                Yetkiler.BankaHareketleriOku, Yetkiler.BankaHareketleriYaz, Yetkiler.BankaHareketleriDuzenle,
                // Butce
                Yetkiler.ButceAnalizOku, Yetkiler.ButceAnalizYaz, Yetkiler.ButceAnalizDuzenle, Yetkiler.ButceAnalizSil,
                Yetkiler.OdemeYonetimiOku, Yetkiler.OdemeYonetimiYaz, Yetkiler.OdemeYonetimiDuzenle,
                Yetkiler.TekrarlayanOdemeOku, Yetkiler.TekrarlayanOdemeYaz, Yetkiler.TekrarlayanOdemeDuzenle,
                // Muhasebe
                Yetkiler.MuhasebeDashboardOku, 
                Yetkiler.HesapPlaniOku, Yetkiler.HesapPlaniYaz, Yetkiler.HesapPlaniDuzenle,
                Yetkiler.MuhasebeFisleriOku, Yetkiler.MuhasebeFisleriYaz, Yetkiler.MuhasebeFisleriDuzenle,
                Yetkiler.MuhasebeRaporlariOku, Yetkiler.MuhasebeRaporlariExport,
                Yetkiler.MaliAnalizOku, Yetkiler.MaliAnalizExport,
                // Rapor
                Yetkiler.RaporlarOku, Yetkiler.RaporlarExport,
                // Yedek
                Yetkiler.YedeklemeOku, Yetkiler.YedeklemeOlustur,
                // Planlama
                Yetkiler.MenuPlanlama, Yetkiler.PlanlamaOku, Yetkiler.PlanlamaDashboardOku,
            },

            Operasyon => new List<string>
            {
                Yetkiler.Dashboard,
                // Ana Menu Erisim
                Yetkiler.MenuFiloServis, Yetkiler.MenuPersonel, Yetkiler.MenuStokEnvanter, Yetkiler.MenuRaporlar,
                Yetkiler.MenuPlanlama,
                // Arac
                Yetkiler.AraclarOku, Yetkiler.AraclarYaz, Yetkiler.AraclarDuzenle,
                // Guzergah
                Yetkiler.GuzergahlarOku, Yetkiler.GuzergahlarYaz, Yetkiler.GuzergahlarDuzenle,
                // Servis Calismalari
                Yetkiler.ServisCalismalariOku, Yetkiler.ServisCalismalariYaz, Yetkiler.ServisCalismalariDuzenle,
                Yetkiler.TedarikciServisOperasyonOku, Yetkiler.TedarikciAraclariOku, Yetkiler.TedarikciPersonelOku, Yetkiler.TedarikciAracEvraklariOku,
                Yetkiler.TopluCalismaOku, Yetkiler.TopluCalismaYaz, Yetkiler.TopluCalismaDuzenle,
                // Masraf
                Yetkiler.MasrafKalemleriOku, Yetkiler.MasrafKalemleriYaz,
                Yetkiler.AracMasraflariOku, Yetkiler.AracMasraflariYaz,
                // Personel
                Yetkiler.PersonelOku, Yetkiler.PersonelYaz, Yetkiler.PersonelDuzenle, Yetkiler.PersonelBorcSil,
                // Stok
                Yetkiler.StokDashboardOku, Yetkiler.StokKartlariOku, Yetkiler.AracIslemOku, Yetkiler.ServisKaydiOku,
                // Rapor
                Yetkiler.RaporlarOku,
                // Planlama
                Yetkiler.PlanlamaOku, Yetkiler.PlanlamaYaz, Yetkiler.PlanlamaDashboardOku,
                Yetkiler.PlanlamaSablonOlustur, Yetkiler.PlanlamaCakismaKontrol, Yetkiler.PlanlamaTopluKaydet,
            },

            SatisTemsilcisi => new List<string>
            {
                Yetkiler.Dashboard,
                // Ana Menu Erisim
                Yetkiler.MenuSatisModulu, Yetkiler.MenuCariModulu,
                // Satis
                Yetkiler.SatisDashboardOku, Yetkiler.SatisMenuOku,
                Yetkiler.PiyasaMenuOku,
                Yetkiler.PiyasaArastirmaOku, Yetkiler.PiyasaArastirmaYaz, Yetkiler.PiyasaArastirmaDuzenle,
                Yetkiler.SatisIlanlariOku, Yetkiler.SatisIlanlariYaz, Yetkiler.SatisIlanlariDuzenle,
                // Cari
                Yetkiler.CarilerOku,
                // Rapor
                Yetkiler.RaporlarOku,
            },

            Sofor => new List<string>
            {
                Yetkiler.Dashboard,
                // Ana Menu Erisim
                Yetkiler.MenuFiloServis,
                // Sadece okuma yetkileri
                Yetkiler.AraclarOku,
                Yetkiler.GuzergahlarOku,
                Yetkiler.ServisCalismalariOku,
            },

            Kullanici => new List<string>
            {
                Yetkiler.Dashboard,
                // Ana Menu Erisim
                Yetkiler.MenuCariModulu, Yetkiler.MenuRaporlar, Yetkiler.MenuButceModulu,
                // Sadece okuma yetkileri
                Yetkiler.CarilerOku,
                Yetkiler.ButceAnalizOku,
                Yetkiler.RaporlarOku,
            },

            HoldingYoneticisi => new List<string>
            {
                Yetkiler.Dashboard,
                Yetkiler.MenuHolding,
                Yetkiler.HoldingDashboardOku,
                Yetkiler.HoldingKarsilastirmaOku,
                Yetkiler.HoldingButceOku,
                Yetkiler.HoldingOdemelerOku,
                Yetkiler.HoldingAracMaliyetOku,
                Yetkiler.HoldingPersonelGiderOku,
                Yetkiler.HoldingHakedisOku,
                Yetkiler.HoldingVeriTopla,
            },

            _ => new List<string> { Yetkiler.Dashboard }
        };
    }
}

public class RolTanim
{
    public string Name { get; set; }
    public string DisplayName { get; set; }
    public string Description { get; set; }
    public string Color { get; set; }
    public string Icon { get; set; }

    public RolTanim(string name, string displayName, string description, string color, string icon)
    {
        Name = name;
        DisplayName = displayName;
        Description = description;
        Color = color;
        Icon = icon;
    }
}

#endregion

#region Yetki Tanimlari

/// <summary>
/// Yetki Tanimlari - modullere ve menulere gore gruplanmis
/// Her alt menu icin Okuma, Yazma, Duzenleme, Silme yetkileri
/// </summary>
public static class Yetkiler
{
    // Genel
    public const string Dashboard = "dashboard";

    // === ANA MENU ERISIM YETKILERI ===
    public const string MenuAnaSayfa = "menu.anasayfa";
    public const string MenuCRM = "menu.crm";
    public const string MenuCariModulu = "menu.cari";
    public const string MenuFiloServis = "menu.filoservis";
    public const string MenuMuhasebe = "menu.muhasebe";
    public const string MenuPersonel = "menu.personel";
    public const string MenuFaturaModulu = "menu.fatura";
    public const string MenuBankaKasa = "menu.bankakasa";
    public const string MenuButceModulu = "menu.butce";
    public const string MenuChecklist = "menu.checklist";
    public const string MenuRaporlar = "menu.raporlar";
    public const string MenuSatisModulu = "menu.satis";
    public const string MenuStokEnvanter = "menu.stok";
    public const string MenuAyarlar = "menu.ayarlar";
    public const string MenuHolding = "menu.holding";
    public const string MenuPlanlama = "menu.planlama";

    // === CRM MODULU YETKILERI ===
    
    // -- Bildirimler/Uyarilar --
    public const string BildirimlerOku = "bildirim.oku";
    public const string BildirimlerYaz = "bildirim.yaz";
    public const string BildirimlerSil = "bildirim.sil";
    public const string BildirimlerAdmin = "bildirim.admin"; // Tum kullanicilarin bildirimlerini yonetme
    
    // -- Mesajlasma --
    public const string MesajlarOku = "mesaj.oku";
    public const string MesajlarYaz = "mesaj.yaz";
    public const string MesajlarSil = "mesaj.sil";
    public const string MesajlarAdmin = "mesaj.admin";
    
    // -- WhatsApp --
    public const string WhatsAppOku = "whatsapp.oku";
    public const string WhatsAppGonder = "whatsapp.gonder";
    public const string WhatsAppAyar = "whatsapp.ayar";
    
    // -- Email --
    public const string EmailOku = "email.oku";
    public const string EmailGonder = "email.gonder";
    public const string EmailAyar = "email.ayar";
    
    // -- Hatirlatici/Randevu --
    public const string HatirlaticiOku = "hatirlatici.oku";
    public const string HatirlaticiYaz = "hatirlatici.yaz";
    public const string HatirlaticiDuzenle = "hatirlatici.duzenle";
    public const string HatirlaticiSil = "hatirlatici.sil";
    public const string HatirlaticiAdmin = "hatirlatici.admin"; // Tum kullanicilarin hatirlaticilarini yonetme
    
    // -- Kullanici-Cari Eslestirme --
    public const string KullaniciCariOku = "kullanicicari.oku";
    public const string KullaniciCariYaz = "kullanicicari.yaz";
    public const string KullaniciCariDuzenle = "kullanicicari.duzenle";
    public const string KullaniciCariSil = "kullanicicari.sil";
    public const string KullaniciCariEkstreGor = "kullanicicari.ekstre";

    // === ALT MENU YETKILERI (Her biri icin Oku, Yaz, Duzenle, Sil) ===
    
    // -- Belge Uyarilari --
    public const string BelgeUyarilariOku = "belgeuyari.oku";
    public const string BelgeUyarilariYaz = "belgeuyari.yaz";
    public const string BelgeUyarilariDuzenle = "belgeuyari.duzenle";
    public const string BelgeUyarilariSil = "belgeuyari.sil";

    // -- Dashboard --
    public const string DashboardOku = "dashboard";

    // -- Cariler --
    public const string CarilerOku = "cariler.oku";
    public const string CarilerYaz = "cariler.yaz";
    public const string CarilerDuzenle = "cariler.duzenle";
    public const string CarilerSil = "cariler.sil";

    // -- Kesilen Faturalar --
    public const string KesilenFaturalarOku = "kesilenfatura.oku";
    public const string KesilenFaturalarYaz = "kesilenfatura.yaz";
    public const string KesilenFaturalarDuzenle = "kesilenfatura.duzenle";
    public const string KesilenFaturalarSil = "kesilenfatura.sil";

    // -- Gelen Faturalar --
    public const string GelenFaturalarOku = "gelenfatura.oku";
    public const string GelenFaturalarYaz = "gelenfatura.yaz";
    public const string GelenFaturalarDuzenle = "gelenfatura.duzenle";
    public const string GelenFaturalarSil = "gelenfatura.sil";

    // -- Araclar --
    public const string AraclarOku = "araclar.oku";
    public const string AraclarYaz = "araclar.yaz";
    public const string AraclarDuzenle = "araclar.duzenle";
    public const string AraclarSil = "araclar.sil";

    // -- Guzergahlar --
    public const string GuzergahlarOku = "guzergahlar.oku";
    public const string GuzergahlarYaz = "guzergahlar.yaz";
    public const string GuzergahlarDuzenle = "guzergahlar.duzenle";
    public const string GuzergahlarSil = "guzergahlar.sil";

    // -- Servis Calismalari --
    public const string ServisCalismalariOku = "serviscalisma.oku";
    public const string ServisCalismalariYaz = "serviscalisma.yaz";
    public const string ServisCalismalariDuzenle = "serviscalisma.duzenle";
    public const string ServisCalismalariSil = "serviscalisma.sil";

    // -- Tedarikci Servis Operasyon --
    public const string TedarikciServisOperasyonOku = "tedarikciservis.oku";
    public const string TedarikciAraclariOku = "tedarikciarac.oku";
    public const string TedarikciPersonelOku = "tedarikcipersonel.oku";
    public const string TedarikciAracEvraklariOku = "tedarikciaracevrak.oku";

    // -- Toplu Calisma --
    public const string TopluCalismaOku = "toplucalisma.oku";
    public const string TopluCalismaYaz = "toplucalisma.yaz";
    public const string TopluCalismaDuzenle = "toplucalisma.duzenle";
    public const string TopluCalismaSil = "toplucalisma.sil";

    // -- Masraf Kalemleri --
    public const string MasrafKalemleriOku = "masrafkalem.oku";
    public const string MasrafKalemleriYaz = "masrafkalem.yaz";
    public const string MasrafKalemleriDuzenle = "masrafkalem.duzenle";
    public const string MasrafKalemleriSil = "masrafkalem.sil";

    // -- Arac Masraflari --
    public const string AracMasraflariOku = "aracmasraf.oku";
    public const string AracMasraflariYaz = "aracmasraf.yaz";
    public const string AracMasraflariDuzenle = "aracmasraf.duzenle";
    public const string AracMasraflariSil = "aracmasraf.sil";

    // -- Muhasebe Dashboard --
    public const string MuhasebeDashboardOku = "muhasebedash.oku";

    // -- Hesap Plani --
    public const string HesapPlaniOku = "hesapplani.oku";
    public const string HesapPlaniYaz = "hesapplani.yaz";
    public const string HesapPlaniDuzenle = "hesapplani.duzenle";
    public const string HesapPlaniSil = "hesapplani.sil";

    // -- Muhasebe Fisleri --
    public const string MuhasebeFisleriOku = "muhasebefis.oku";
    public const string MuhasebeFisleriYaz = "muhasebefis.yaz";
    public const string MuhasebeFisleriDuzenle = "muhasebefis.duzenle";
    public const string MuhasebeFisleriSil = "muhasebefis.sil";

    // -- Muhasebe Raporlari --
    public const string MuhasebeRaporlariOku = "muhaseberapor.oku";
    public const string MuhasebeRaporlariExport = "muhaseberapor.export";

    // -- Mali Analiz --
    public const string MaliAnalizOku = "malianaliz.oku";
    public const string MaliAnalizExport = "malianaliz.export";

    // -- Personel Listesi --
    public const string PersonelOku = "personel.oku";
    public const string PersonelYaz = "personel.yaz";
    public const string PersonelDuzenle = "personel.duzenle";
    public const string PersonelSil = "personel.sil";

    // -- Maas Yonetimi --
    public const string MaasOku = "maas.oku";
    public const string MaasYaz = "maas.yaz";
    public const string MaasDuzenle = "maas.duzenle";
    public const string MaasSil = "maas.sil";
    public const string PersonelBorcSil = "personel.borc.sil";

    // -- Izin Yonetimi --
    public const string IzinOku = "izin.oku";
    public const string IzinYaz = "izin.yaz";
    public const string IzinDuzenle = "izin.duzenle";
    public const string IzinSil = "izin.sil";

    // -- Faturalar --
    public const string FaturalarOku = "faturalar.oku";
    public const string FaturalarYaz = "faturalar.yaz";
    public const string FaturalarDuzenle = "faturalar.duzenle";
    public const string FaturalarSil = "faturalar.sil";

    // -- Fatura Hazirlik --
    public const string FaturaHazirlikOku = "faturahazirlik.oku";
    public const string FaturaHazirlikYaz = "faturahazirlik.yaz";
    public const string FaturaHazirlikDuzenle = "faturahazirlik.duzenle";

    // -- Banka Hesaplari --
    public const string BankaHesaplariOku = "bankahesap.oku";
    public const string BankaHesaplariYaz = "bankahesap.yaz";
    public const string BankaHesaplariDuzenle = "bankahesap.duzenle";
    public const string BankaHesaplariSil = "bankahesap.sil";

    // -- Banka Hareketleri --
    public const string BankaHareketleriOku = "bankahareket.oku";
    public const string BankaHareketleriYaz = "bankahareket.yaz";
    public const string BankaHareketleriDuzenle = "bankahareket.duzenle";
    public const string BankaHareketleriSil = "bankahareket.sil";

    // -- Odeme Eslestirme --
    public const string OdemeEslestirmeOku = "odemeeslestir.oku";
    public const string OdemeEslestirmeYaz = "odemeeslestir.yaz";
    public const string OdemeEslestirmeDuzenle = "odemeeslestir.duzenle";

    // -- Butce Analiz --
    public const string ButceAnalizOku = "butceanaliz.oku";
    public const string ButceAnalizYaz = "butceanaliz.yaz";
    public const string ButceAnalizDuzenle = "butceanaliz.duzenle";
    public const string ButceAnalizSil = "butceanaliz.sil";

    // -- Odeme Yonetimi --
    public const string OdemeYonetimiOku = "odemeyonetim.oku";
    public const string OdemeYonetimiYaz = "odemeyonetim.yaz";
    public const string OdemeYonetimiDuzenle = "odemeyonetim.duzenle";
    public const string OdemeYonetimiSil = "odemeyonetim.sil";

    // -- Tekrarlayan Odemeler / Kredi Taksitler --
    public const string TekrarlayanOdemeOku = "tekrarlayanodem.oku";
    public const string TekrarlayanOdemeYaz = "tekrarlayanodem.yaz";
    public const string TekrarlayanOdemeDuzenle = "tekrarlayanodem.duzenle";
    public const string TekrarlayanOdemeSil = "tekrarlayanodem.sil";

    // -- Raporlar (Genel) --
    public const string RaporlarOku = "raporlar.oku";
    public const string RaporlarExport = "raporlar.export";

    // -- Holding Yonetimi --
    public const string HoldingDashboardOku = "holding.dashboard.oku";
    public const string HoldingKarsilastirmaOku = "holding.karsilastirma.oku";
    public const string HoldingButceOku = "holding.butce.oku";
    public const string HoldingOdemelerOku = "holding.odemeler.oku";
    public const string HoldingAracMaliyetOku = "holding.aracmaliyet.oku";
    public const string HoldingPersonelGiderOku = "holding.personelgider.oku";
    public const string HoldingHakedisOku = "holding.hakedis.oku";
    public const string HoldingVeriTopla = "holding.veritopla";

    // -- Planlama (Operasyon Planlama) --
    public const string PlanlamaOku = "planlama.oku";
    public const string PlanlamaYaz = "planlama.yaz";
    public const string PlanlamaDashboardOku = "planlama.dashboard.oku";
    public const string PlanlamaSablonOlustur = "planlama.sablon";
    public const string PlanlamaCakismaKontrol = "planlama.cakisma";
    public const string PlanlamaTopluKaydet = "planlama.kaydet";

    // -- Satis Dashboard --
    public const string SatisDashboardOku = "satisdash.oku";
    public const string SatisMenuOku = "satis.oku";

    // -- Piyasa Arastirma --
    public const string PiyasaMenuOku = "piyasa.oku";
    public const string PiyasaArastirmaOku = "piyasaarastir.oku";
    public const string PiyasaArastirmaYaz = "piyasaarastir.yaz";
    public const string PiyasaArastirmaDuzenle = "piyasaarastir.duzenle";
    public const string PiyasaArastirmaSil = "piyasaarastir.sil";

    // -- Stok / Envanter --
    public const string StokDashboardOku = "stokdash.oku";
    public const string StokKartlariOku = "stokkart.oku";
    public const string StokKartlariYaz = "stokkart.yaz";
    public const string StokKartlariDuzenle = "stokkart.duzenle";
    public const string StokKartlariSil = "stokkart.sil";
    public const string AracIslemOku = "aracislem.oku";
    public const string AracIslemYaz = "aracislem.yaz";
    public const string AracIslemDuzenle = "aracislem.duzenle";
    public const string AracIslemSil = "aracislem.sil";
    public const string ServisKaydiOku = "serviskaydi.oku";
    public const string ServisKaydiYaz = "serviskaydi.yaz";
    public const string ServisKaydiDuzenle = "serviskaydi.duzenle";
    public const string ServisKaydiSil = "serviskaydi.sil";

    // -- Ek rapor/menu uyumluluklari --
    public const string KiralikAracRaporuOkuAlias = "raporkirala.oku";

    // -- Satis Ilanlari --
    public const string SatisIlanlariOku = "satisilan.oku";
    public const string SatisIlanlariYaz = "satisilan.yaz";
    public const string SatisIlanlariDuzenle = "satisilan.duzenle";
    public const string SatisIlanlariSil = "satisilan.sil";

    // -- Satis Personeli --
    public const string SatisPersoneliOku = "satispersonel.oku";
    public const string SatisPersoneliYaz = "satispersonel.yaz";
    public const string SatisPersoneliDuzenle = "satispersonel.duzenle";
    public const string SatisPersoneliSil = "satispersonel.sil";

    // -- Firma Yonetimi --
    public const string FirmaYonetimiOku = "firmayonetim.oku";
    public const string FirmaYonetimiYaz = "firmayonetim.yaz";
    public const string FirmaYonetimiDuzenle = "firmayonetim.duzenle";
    public const string FirmaYonetimiSil = "firmayonetim.sil";

    // -- Veritabani Ayarlari --
    public const string VeritabaniAyarlariOku = "veritabani.oku";
    public const string VeritabaniAyarlariDuzenle = "veritabani.duzenle";

    // -- Lisans Bilgileri --
    public const string LisansBilgileriOku = "lisans.oku";
    public const string LisansBilgileriDuzenle = "lisans.duzenle";

    // -- Kullanici Yonetimi --
    public const string KullaniciYonetimiOku = "kullanici.oku";
    public const string KullaniciYonetimiYaz = "kullanici.yaz";
    public const string KullaniciYonetimiDuzenle = "kullanici.duzenle";
    public const string KullaniciYonetimiSil = "kullanici.sil";

    // -- Rol Yonetimi --
    public const string RolYonetimiOku = "rol.oku";
    public const string RolYonetimiYaz = "rol.yaz";
    public const string RolYonetimiDuzenle = "rol.duzenle";
    public const string RolYonetimiSil = "rol.sil";

    // -- Piyasa Kaynaklari --
    public const string PiyasaKaynaklariOku = "piyasakaynak.oku";
    public const string PiyasaKaynaklariYaz = "piyasakaynak.yaz";
    public const string PiyasaKaynaklariDuzenle = "piyasakaynak.duzenle";
    public const string PiyasaKaynaklariSil = "piyasakaynak.sil";

    // -- Sistem Durumu --
    public const string SistemDurumuOku = "sistemdurumu.oku";

    // -- Aktivite Logu --
    public const string AktiviteLogOku = "aktivitelog.oku";
    public const string AktiviteLogSil = "aktivitelog.sil";

    // -- Yedekleme --
    public const string YedeklemeOku = "yedekleme.oku";
    public const string YedeklemeOlustur = "yedekleme.olustur";
    public const string YedeklemeGeriYukle = "yedekleme.geriyukle";
    public const string YedeklemeSil = "yedekleme.sil";

    // -- Uygulama Guncelleme --
    public const string GuncellemeOku = "guncelleme.oku";
    public const string GuncellemeUygula = "guncelleme.uygula";

    // -- Arac Sase Yonetimi --
    public const string AracSaseYonetimiOku = "aracsase.oku";
    public const string AracSaseYonetimiDuzenle = "aracsase.duzenle";

    /// <summary>
    /// Tum yetki kodlarini dondurur
    /// </summary>
    public static List<string> GetAll()
    {
        return GetMenuYetkiGruplari().SelectMany(g => g.AltMenuler.SelectMany(m => m.Yetkiler.Select(y => y.Kod))).ToList();
    }

    /// <summary>
    /// Menu ve alt menuleri CRUD yetkileriyle birlikte gruplar
    /// </summary>
    public static List<AnaMenuYetkiGrup> GetMenuYetkiGruplari()
    {
        return new List<AnaMenuYetkiGrup>
        {
            new("Ana Sayfa", "bi-house-door", MenuAnaSayfa, new List<AltMenuYetki>
            {
                new("Dashboard", "bi-speedometer2", new List<YetkiTanim>
                {
                    new(DashboardOku, "Goruntuleme", "bi-eye"),
                }),
                new("Belge Uyarilari", "bi-exclamation-triangle", new List<YetkiTanim>
                {
                    new(BelgeUyarilariOku, "Okuma", "bi-eye"),
                    new(BelgeUyarilariYaz, "Yazma", "bi-plus"),
                    new(BelgeUyarilariDuzenle, "Duzenleme", "bi-pencil"),
                    new(BelgeUyarilariSil, "Silme", "bi-trash"),
                }),
            }),

            new("CRM Modulu", "bi-chat-dots", MenuCRM, new List<AltMenuYetki>
            {
                new("Bildirimler", "bi-bell", new List<YetkiTanim>
                {
                    new(BildirimlerOku, "Okuma", "bi-eye"),
                    new(BildirimlerYaz, "Yazma", "bi-plus"),
                    new(BildirimlerSil, "Silme", "bi-trash"),
                }),
                new("Mesajlasma", "bi-envelope", new List<YetkiTanim>
                {
                    new(MesajlarOku, "Okuma", "bi-eye"),
                    new(MesajlarYaz, "Yazma", "bi-plus"),
                    new(MesajlarSil, "Silme", "bi-trash"),
                }),
                new("WhatsApp Entegrasyonu", "bi-whatsapp", new List<YetkiTanim>
                {
                    new(WhatsAppOku, "Okuma", "bi-eye"),
                    new(WhatsAppGonder, "Gonder", "bi-send"),
                    new(WhatsAppAyar, "Ayarlar", "bi-gear"),
                }),
                new("Email Entegrasyonu", "bi-envelope-open", new List<YetkiTanim>
                {
                    new(EmailOku, "Okuma", "bi-eye"),
                    new(EmailGonder, "Gonder", "bi-send"),
                    new(EmailAyar, "Ayarlar", "bi-gear"),
                }),
                new("Hatırlatıcılar", "bi-alarm", new List<YetkiTanim>
                {
                    new(HatirlaticiOku, "Okuma", "bi-eye"),
                    new(HatirlaticiYaz, "Yazma", "bi-plus"),
                    new(HatirlaticiDuzenle, "Duzenleme", "bi-pencil"),
                    new(HatirlaticiSil, "Silme", "bi-trash"),
                }),
                new("Kullanici-Cari Eslestirme", "bi-link", new List<YetkiTanim>
                {
                    new(KullaniciCariOku, "Okuma", "bi-eye"),
                    new(KullaniciCariYaz, "Yazma", "bi-plus"),
                    new(KullaniciCariDuzenle, "Duzenleme", "bi-pencil"),
                    new(KullaniciCariSil, "Silme", "bi-trash"),
                }),
            }),

            new("Cari Modulu", "bi-people", MenuCariModulu, new List<AltMenuYetki>
            {
                new("Cariler", "bi-person-lines-fill", new List<YetkiTanim>
                {
                    new(CarilerOku, "Okuma", "bi-eye"),
                    new(CarilerYaz, "Yazma", "bi-plus"),
                    new(CarilerDuzenle, "Duzenleme", "bi-pencil"),
                    new(CarilerSil, "Silme", "bi-trash"),
                }),
                new("Kesilen Faturalar", "bi-file-earmark-arrow-up", new List<YetkiTanim>
                {
                    new(KesilenFaturalarOku, "Okuma", "bi-eye"),
                    new(KesilenFaturalarYaz, "Yazma", "bi-plus"),
                    new(KesilenFaturalarDuzenle, "Duzenleme", "bi-pencil"),
                    new(KesilenFaturalarSil, "Silme", "bi-trash"),
                }),
                new("Gelen Faturalar", "bi-file-earmark-arrow-down", new List<YetkiTanim>
                {
                    new(GelenFaturalarOku, "Okuma", "bi-eye"),
                    new(GelenFaturalarYaz, "Yazma", "bi-plus"),
                    new(GelenFaturalarDuzenle, "Duzenleme", "bi-pencil"),
                    new(GelenFaturalarSil, "Silme", "bi-trash"),
                }),
            }),

            new("Filo Servis", "bi-truck", MenuFiloServis, new List<AltMenuYetki>
            {
                new("Araclar", "bi-car-front-fill", new List<YetkiTanim>
                {
                    new(AraclarOku, "Okuma", "bi-eye"),
                    new(AraclarYaz, "Yazma", "bi-plus"),
                    new(AraclarDuzenle, "Duzenleme", "bi-pencil"),
                    new(AraclarSil, "Silme", "bi-trash"),
                }),
                new("Guzergahlar", "bi-signpost-split-fill", new List<YetkiTanim>
                {
                    new(GuzergahlarOku, "Okuma", "bi-eye"),
                    new(GuzergahlarYaz, "Yazma", "bi-plus"),
                    new(GuzergahlarDuzenle, "Duzenleme", "bi-pencil"),
                    new(GuzergahlarSil, "Silme", "bi-trash"),
                }),
                new("Servis Calismalari", "bi-calendar-check-fill", new List<YetkiTanim>
                {
                    new(ServisCalismalariOku, "Okuma", "bi-eye"),
                    new(ServisCalismalariYaz, "Yazma", "bi-plus"),
                    new(ServisCalismalariDuzenle, "Duzenleme", "bi-pencil"),
                    new(ServisCalismalariSil, "Silme", "bi-trash"),
                }),
                new("Tedarikci Servis Operasyon", "bi-truck", new List<YetkiTanim>
                {
                    new(TedarikciServisOperasyonOku, "Modul Goruntuleme", "bi-eye"),
                    new(TedarikciAraclariOku, "Tedarikci Araclari", "bi-truck-front"),
                    new(TedarikciPersonelOku, "Tedarikci Personel", "bi-people"),
                    new(TedarikciAracEvraklariOku, "Tedarikci Arac Evraklari", "bi-folder2-open"),
                }),
                new("Toplu Calisma Girisi", "bi-list-check", new List<YetkiTanim>
                {
                    new(TopluCalismaOku, "Okuma", "bi-eye"),
                    new(TopluCalismaYaz, "Yazma", "bi-plus"),
                    new(TopluCalismaDuzenle, "Duzenleme", "bi-pencil"),
                    new(TopluCalismaSil, "Silme", "bi-trash"),
                }),
                new("Masraf Kalemleri", "bi-list-task", new List<YetkiTanim>
                {
                    new(MasrafKalemleriOku, "Okuma", "bi-eye"),
                    new(MasrafKalemleriYaz, "Yazma", "bi-plus"),
                    new(MasrafKalemleriDuzenle, "Duzenleme", "bi-pencil"),
                    new(MasrafKalemleriSil, "Silme", "bi-trash"),
                }),
                new("Arac Masraflari", "bi-receipt", new List<YetkiTanim>
                {
                    new(AracMasraflariOku, "Okuma", "bi-eye"),
                    new(AracMasraflariYaz, "Yazma", "bi-plus"),
                    new(AracMasraflariDuzenle, "Duzenleme", "bi-pencil"),
                    new(AracMasraflariSil, "Silme", "bi-trash"),
                }),
            }),

            new("Muhasebe", "bi-journal-text", MenuMuhasebe, new List<AltMenuYetki>
            {
                new("Muhasebe Dashboard", "bi-speedometer2", new List<YetkiTanim>
                {
                    new(MuhasebeDashboardOku, "Goruntuleme", "bi-eye"),
                }),
                new("Hesap Plani", "bi-list-nested", new List<YetkiTanim>
                {
                    new(HesapPlaniOku, "Okuma", "bi-eye"),
                    new(HesapPlaniYaz, "Yazma", "bi-plus"),
                    new(HesapPlaniDuzenle, "Duzenleme", "bi-pencil"),
                    new(HesapPlaniSil, "Silme", "bi-trash"),
                }),
                new("Muhasebe Fisleri", "bi-receipt", new List<YetkiTanim>
                {
                    new(MuhasebeFisleriOku, "Okuma", "bi-eye"),
                    new(MuhasebeFisleriYaz, "Yazma", "bi-plus"),
                    new(MuhasebeFisleriDuzenle, "Duzenleme", "bi-pencil"),
                    new(MuhasebeFisleriSil, "Silme", "bi-trash"),
                }),
                new("Muhasebe Raporlari", "bi-file-earmark-bar-graph", new List<YetkiTanim>
                {
                    new(MuhasebeRaporlariOku, "Goruntuleme", "bi-eye"),
                    new(MuhasebeRaporlariExport, "Export", "bi-download"),
                }),
                new("Mali Analiz", "bi-graph-up", new List<YetkiTanim>
                {
                    new(MaliAnalizOku, "Goruntuleme", "bi-eye"),
                    new(MaliAnalizExport, "Export", "bi-download"),
                }),
            }),

            new("Personel", "bi-people", MenuPersonel, new List<AltMenuYetki>
            {
                new("Personel Listesi", "bi-people", new List<YetkiTanim>
                {
                    new(PersonelOku, "Okuma", "bi-eye"),
                    new(PersonelYaz, "Yazma", "bi-plus"),
                    new(PersonelDuzenle, "Duzenleme", "bi-pencil"),
                    new(PersonelSil, "Silme", "bi-trash"),
                }),
                new("Maas Yonetimi", "bi-cash-stack", new List<YetkiTanim>
                {
                    new(MaasOku, "Okuma", "bi-eye"),
                    new(MaasYaz, "Yazma", "bi-plus"),
                    new(MaasDuzenle, "Duzenleme", "bi-pencil"),
                    new(MaasSil, "Silme", "bi-trash"),
                    new(PersonelBorcSil, "Borc Kaydi Kalici Silme", "bi-trash-fill"),
                }),
                new("Izin Yonetimi", "bi-calendar-check", new List<YetkiTanim>
                {
                    new(IzinOku, "Okuma", "bi-eye"),
                    new(IzinYaz, "Yazma", "bi-plus"),
                    new(IzinDuzenle, "Duzenleme", "bi-pencil"),
                    new(IzinSil, "Silme", "bi-trash"),
                }),
            }),

            new("Fatura Modulu", "bi-receipt", MenuFaturaModulu, new List<AltMenuYetki>
            {
                new("Faturalar", "bi-file-earmark-text-fill", new List<YetkiTanim>
                {
                    new(FaturalarOku, "Okuma", "bi-eye"),
                    new(FaturalarYaz, "Yazma", "bi-plus"),
                    new(FaturalarDuzenle, "Duzenleme", "bi-pencil"),
                    new(FaturalarSil, "Silme", "bi-trash"),
                }),
                new("Fatura Hazirlik", "bi-clipboard-check", new List<YetkiTanim>
                {
                    new(FaturaHazirlikOku, "Okuma", "bi-eye"),
                    new(FaturaHazirlikYaz, "Yazma", "bi-plus"),
                    new(FaturaHazirlikDuzenle, "Duzenleme", "bi-pencil"),
                }),
            }),

            new("Banka / Kasa", "bi-bank", MenuBankaKasa, new List<AltMenuYetki>
            {
                new("Banka Hesaplari", "bi-bank2", new List<YetkiTanim>
                {
                    new(BankaHesaplariOku, "Okuma", "bi-eye"),
                    new(BankaHesaplariYaz, "Yazma", "bi-plus"),
                    new(BankaHesaplariDuzenle, "Duzenleme", "bi-pencil"),
                    new(BankaHesaplariSil, "Silme", "bi-trash"),
                }),
                new("Banka Hareketleri", "bi-arrow-left-right", new List<YetkiTanim>
                {
                    new(BankaHareketleriOku, "Okuma", "bi-eye"),
                    new(BankaHareketleriYaz, "Yazma", "bi-plus"),
                    new(BankaHareketleriDuzenle, "Duzenleme", "bi-pencil"),
                    new(BankaHareketleriSil, "Silme", "bi-trash"),
                }),
                new("Odeme Eslestirme", "bi-link-45deg", new List<YetkiTanim>
                {
                    new(OdemeEslestirmeOku, "Okuma", "bi-eye"),
                    new(OdemeEslestirmeYaz, "Yazma", "bi-plus"),
                    new(OdemeEslestirmeDuzenle, "Duzenleme", "bi-pencil"),
                }),
            }),

            new("Butce Modulu", "bi-wallet2", MenuButceModulu, new List<AltMenuYetki>
            {
                new("Butce Analiz", "bi-wallet2", new List<YetkiTanim>
                {
                    new(ButceAnalizOku, "Okuma", "bi-eye"),
                    new(ButceAnalizYaz, "Yazma", "bi-plus"),
                    new(ButceAnalizDuzenle, "Duzenleme", "bi-pencil"),
                    new(ButceAnalizSil, "Silme", "bi-trash"),
                }),
                new("Odeme Yonetimi", "bi-credit-card", new List<YetkiTanim>
                {
                    new(OdemeYonetimiOku, "Okuma", "bi-eye"),
                    new(OdemeYonetimiYaz, "Yazma", "bi-plus"),
                    new(OdemeYonetimiDuzenle, "Duzenleme", "bi-pencil"),
                    new(OdemeYonetimiSil, "Silme", "bi-trash"),
                }),
                new("Kredi / Taksitler", "bi-calendar-plus", new List<YetkiTanim>
                {
                    new(TekrarlayanOdemeOku, "Okuma", "bi-eye"),
                    new(TekrarlayanOdemeYaz, "Yazma", "bi-plus"),
                    new(TekrarlayanOdemeDuzenle, "Duzenleme", "bi-pencil"),
                    new(TekrarlayanOdemeSil, "Silme", "bi-trash"),
                }),
            }),

            new("Raporlar", "bi-bar-chart", MenuRaporlar, new List<AltMenuYetki>
            {
                new("Tum Raporlar", "bi-file-earmark-bar-graph", new List<YetkiTanim>
                {
                    new(RaporlarOku, "Goruntuleme", "bi-eye"),
                    new(RaporlarExport, "Export", "bi-download"),
                }),
            }),

            new("Planlama", "bi-calendar-check", MenuPlanlama, new List<AltMenuYetki>
            {
                new("Operasyon Planlama", "bi-calendar-check", new List<YetkiTanim>
                {
                    new(PlanlamaOku, "Goruntuleme", "bi-eye"),
                    new(PlanlamaYaz, "Yazma/Duzenleme", "bi-pencil"),
                }),
                new("Planlama Dashboard", "bi-speedometer2", new List<YetkiTanim>
                {
                    new(PlanlamaDashboardOku, "Goruntuleme", "bi-eye"),
                }),
                new("Sablon Olusturma", "bi-lightning", new List<YetkiTanim>
                {
                    new(PlanlamaSablonOlustur, "Sablon Olustur", "bi-plus"),
                }),
                new("Cakisma Kontrol", "bi-shield-check", new List<YetkiTanim>
                {
                    new(PlanlamaCakismaKontrol, "Kontrol Et", "bi-search"),
                }),
                new("Toplu Kaydet", "bi-save", new List<YetkiTanim>
                {
                    new(PlanlamaTopluKaydet, "Toplu Kaydet", "bi-download"),
                }),
            }),

            new("Holding Yonetimi", "bi-buildings", MenuHolding, new List<AltMenuYetki>
            {
                new("Holding Dashboard", "bi-speedometer2", new List<YetkiTanim>
                {
                    new(HoldingDashboardOku, "Goruntuleme", "bi-eye"),
                }),
                new("Firma Karsilastirma", "bi-bar-chart", new List<YetkiTanim>
                {
                    new(HoldingKarsilastirmaOku, "Goruntuleme", "bi-eye"),
                }),
                new("Butce Konsolidasyonu", "bi-calculator", new List<YetkiTanim>
                {
                    new(HoldingButceOku, "Goruntuleme", "bi-eye"),
                }),
                new("Odeme Plani", "bi-cash-stack", new List<YetkiTanim>
                {
                    new(HoldingOdemelerOku, "Goruntuleme", "bi-eye"),
                }),
                new("Arac Maliyet Ozeti", "bi-truck", new List<YetkiTanim>
                {
                    new(HoldingAracMaliyetOku, "Goruntuleme", "bi-eye"),
                }),
                new("Personel Gider Ozeti", "bi-people", new List<YetkiTanim>
                {
                    new(HoldingPersonelGiderOku, "Goruntuleme", "bi-eye"),
                }),
                new("Hakedis Ozeti", "bi-clipboard-check", new List<YetkiTanim>
                {
                    new(HoldingHakedisOku, "Goruntuleme", "bi-eye"),
                }),
                new("Veri Toplama", "bi-database-down", new List<YetkiTanim>
                {
                    new(HoldingVeriTopla, "Veri Toplama", "bi-download"),
                }),
            }),

            new("Satis Modulu", "bi-car-front", MenuSatisModulu, new List<AltMenuYetki>
            {
                new("Satis Dashboard", "bi-speedometer2", new List<YetkiTanim>
                {
                    new(SatisDashboardOku, "Goruntuleme", "bi-eye"),
                    new(SatisMenuOku, "Menu Giris", "bi-box-arrow-in-right"),
                }),
                new("Piyasa Arastirma", "bi-search", new List<YetkiTanim>
                {
                    new(PiyasaMenuOku, "Menu Giris", "bi-box-arrow-in-right"),
                    new(PiyasaArastirmaOku, "Okuma", "bi-eye"),
                    new(PiyasaArastirmaYaz, "Yazma", "bi-plus"),
                    new(PiyasaArastirmaDuzenle, "Duzenleme", "bi-pencil"),
                    new(PiyasaArastirmaSil, "Silme", "bi-trash"),
                }),
                new("Satis Ilanlari", "bi-megaphone", new List<YetkiTanim>
                {
                    new(SatisIlanlariOku, "Okuma", "bi-eye"),
                    new(SatisIlanlariYaz, "Yazma", "bi-plus"),
                    new(SatisIlanlariDuzenle, "Duzenleme", "bi-pencil"),
                    new(SatisIlanlariSil, "Silme", "bi-trash"),
                }),
                new("Satis Personeli", "bi-people", new List<YetkiTanim>
                {
                    new(SatisPersoneliOku, "Okuma", "bi-eye"),
                    new(SatisPersoneliYaz, "Yazma", "bi-plus"),
                    new(SatisPersoneliDuzenle, "Duzenleme", "bi-pencil"),
                    new(SatisPersoneliSil, "Silme", "bi-trash"),
                }),
            }),

            new("Stok / Envanter", "bi-box-seam", MenuStokEnvanter, new List<AltMenuYetki>
            {
                new("Stok Dashboard", "bi-speedometer2", new List<YetkiTanim>
                {
                    new(StokDashboardOku, "Goruntuleme", "bi-eye"),
                }),
                new("Stok Kartlari", "bi-box-seam", new List<YetkiTanim>
                {
                    new(StokKartlariOku, "Okuma", "bi-eye"),
                    new(StokKartlariYaz, "Yazma", "bi-plus"),
                    new(StokKartlariDuzenle, "Duzenleme", "bi-pencil"),
                    new(StokKartlariSil, "Silme", "bi-trash"),
                }),
                new("Arac Alis / Satis", "bi-truck", new List<YetkiTanim>
                {
                    new(AracIslemOku, "Okuma", "bi-eye"),
                    new(AracIslemYaz, "Yazma", "bi-plus"),
                    new(AracIslemDuzenle, "Duzenleme", "bi-pencil"),
                    new(AracIslemSil, "Silme", "bi-trash"),
                }),
                new("Servis Kayitlari", "bi-tools", new List<YetkiTanim>
                {
                    new(ServisKaydiOku, "Okuma", "bi-eye"),
                    new(ServisKaydiYaz, "Yazma", "bi-plus"),
                    new(ServisKaydiDuzenle, "Duzenleme", "bi-pencil"),
                    new(ServisKaydiSil, "Silme", "bi-trash"),
                }),
            }),

            new("Ayarlar", "bi-gear", MenuAyarlar, new List<AltMenuYetki>
            {
                new("Firma Yonetimi", "bi-building", new List<YetkiTanim>
                {
                    new(FirmaYonetimiOku, "Okuma", "bi-eye"),
                    new(FirmaYonetimiYaz, "Yazma", "bi-plus"),
                    new(FirmaYonetimiDuzenle, "Duzenleme", "bi-pencil"),
                    new(FirmaYonetimiSil, "Silme", "bi-trash"),
                }),
                new("Veritabani Ayarlari", "bi-database-gear", new List<YetkiTanim>
                {
                    new(VeritabaniAyarlariOku, "Goruntuleme", "bi-eye"),
                    new(VeritabaniAyarlariDuzenle, "Duzenleme", "bi-pencil"),
                }),
                new("Lisans Bilgileri", "bi-key", new List<YetkiTanim>
                {
                    new(LisansBilgileriOku, "Goruntuleme", "bi-eye"),
                    new(LisansBilgileriDuzenle, "Duzenleme", "bi-pencil"),
                }),
                new("Kullanici Yonetimi", "bi-person-badge", new List<YetkiTanim>
                {
                    new(KullaniciYonetimiOku, "Okuma", "bi-eye"),
                    new(KullaniciYonetimiYaz, "Yazma", "bi-plus"),
                    new(KullaniciYonetimiDuzenle, "Duzenleme", "bi-pencil"),
                    new(KullaniciYonetimiSil, "Silme", "bi-trash"),
                }),
                new("Rol Yonetimi", "bi-shield-check", new List<YetkiTanim>
                {
                    new(RolYonetimiOku, "Okuma", "bi-eye"),
                    new(RolYonetimiYaz, "Yazma", "bi-plus"),
                    new(RolYonetimiDuzenle, "Duzenleme", "bi-pencil"),
                    new(RolYonetimiSil, "Silme", "bi-trash"),
                }),
                new("Piyasa Kaynaklari", "bi-globe", new List<YetkiTanim>
                {
                    new(PiyasaKaynaklariOku, "Okuma", "bi-eye"),
                    new(PiyasaKaynaklariYaz, "Yazma", "bi-plus"),
                    new(PiyasaKaynaklariDuzenle, "Duzenleme", "bi-pencil"),
                    new(PiyasaKaynaklariSil, "Silme", "bi-trash"),
                }),
                new("Sistem Durumu", "bi-heart-pulse", new List<YetkiTanim>
                {
                    new(SistemDurumuOku, "Goruntuleme", "bi-eye"),
                }),
                new("Aktivite Logu", "bi-clock-history", new List<YetkiTanim>
                {
                    new(AktiviteLogOku, "Goruntuleme", "bi-eye"),
                    new(AktiviteLogSil, "Silme", "bi-trash"),
                }),
                new("Yedekleme", "bi-database-fill-gear", new List<YetkiTanim>
                {
                    new(YedeklemeOku, "Goruntuleme", "bi-eye"),
                    new(YedeklemeOlustur, "Olusturma", "bi-plus"),
                    new(YedeklemeGeriYukle, "Geri Yukleme", "bi-arrow-counterclockwise"),
                    new(YedeklemeSil, "Silme", "bi-trash"),
                }),
                new("Kiralik Arac Raporu Menu Uyumlulugu", "bi-building", new List<YetkiTanim>
                {
                    new(KiralikAracRaporuOkuAlias, "Goruntuleme", "bi-eye"),
                }),
                new("Uygulama Guncelleme", "bi-cloud-arrow-down", new List<YetkiTanim>
                {
                    new(GuncellemeOku, "Kontrol", "bi-eye"),
                    new(GuncellemeUygula, "Uygulama", "bi-download"),
                }),
                new("Arac Sase Yonetimi", "bi-upc-scan", new List<YetkiTanim>
                {
                    new(AracSaseYonetimiOku, "Goruntuleme", "bi-eye"),
                    new(AracSaseYonetimiDuzenle, "Duzenleme", "bi-pencil"),
                }),
            }),
        };
    }

    /// <summary>
    /// Geriye uyumluluk icin eski metodlar
    /// </summary>
    public static List<MenuYetkiGrup> GetMenuYetkileri()
    {
        return GetMenuYetkiGruplari().Select(g => new MenuYetkiGrup(
            g.GrupAdi, 
            g.Icon, 
            g.AltMenuler.SelectMany(m => m.Yetkiler).ToList()
        )).ToList();
    }

    public static Dictionary<string, List<YetkiTanim>> GetIslemYetkileri()
    {
        return GetMenuYetkiGruplari().ToDictionary(
            g => g.GrupAdi,
            g => g.AltMenuler.SelectMany(m => m.Yetkiler).ToList()
        );
    }

    public static Dictionary<string, List<YetkiTanim>> GetGrouped() => GetIslemYetkileri();
}

/// <summary>
/// Ana menu yetki grubu
/// </summary>
public class AnaMenuYetkiGrup
{
    public string GrupAdi { get; set; }
    public string Icon { get; set; }
    public string AnaMenuYetkiKodu { get; set; }
    public List<AltMenuYetki> AltMenuler { get; set; }

    public AnaMenuYetkiGrup(string grupAdi, string icon, string anaMenuYetkiKodu, List<AltMenuYetki> altMenuler)
    {
        GrupAdi = grupAdi;
        Icon = icon;
        AnaMenuYetkiKodu = anaMenuYetkiKodu;
        AltMenuler = altMenuler;
    }
}

/// <summary>
/// Alt menu yetkileri
/// </summary>
public class AltMenuYetki
{
    public string MenuAdi { get; set; }
    public string Icon { get; set; }
    public List<YetkiTanim> Yetkiler { get; set; }

    public AltMenuYetki(string menuAdi, string icon, List<YetkiTanim> yetkiler)
    {
        MenuAdi = menuAdi;
        Icon = icon;
        Yetkiler = yetkiler;
    }
}

/// <summary>
/// Menu bazli yetki grubu (geriye uyumluluk)
/// </summary>
public class MenuYetkiGrup
{
    public string GrupAdi { get; set; }
    public string Icon { get; set; }
    public List<YetkiTanim> Yetkiler { get; set; }

    public MenuYetkiGrup(string grupAdi, string icon, List<YetkiTanim> yetkiler)
    {
        GrupAdi = grupAdi;
        Icon = icon;
        Yetkiler = yetkiler;
    }
}

public class YetkiTanim
{
    public string Kod { get; set; }
    public string Adi { get; set; }
    public string Icon { get; set; }

    public YetkiTanim(string kod, string adi, string icon)
    {
        Kod = kod;
        Adi = adi;
        Icon = icon;
    }
}

#endregion
