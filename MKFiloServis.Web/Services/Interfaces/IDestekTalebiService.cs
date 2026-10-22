using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Models;
using MKFiloServis.Web.Services.Destek;

namespace MKFiloServis.Web.Services.Interfaces;

/// <summary>
/// Destek talebi servisi. Her işlem, oturumdaki kullanıcıyı ve güncel yetkilerini kendisi doğrular
/// (bkz. <see cref="DestekErisim"/>); sayfalar yalnız gösterim için yetki bilgisi kullanır.
/// </summary>
public interface IDestekTalebiService
{
    /// <summary>Oturumdaki kullanıcı ve Destek yetkileri (oturum yoksa null).</summary>
    Task<DestekKullanici?> GetAktifKullaniciAsync();

    // === Talep eden ===

    /// <summary>Oturumdaki kullanıcı adına talep açar (taslak ya da gönderilmiş).</summary>
    Task<DestekTalebi> TalepOlusturAsync(DestekTalebi talep, bool taslak);

    /// <summary>Kendi taslağını günceller; <paramref name="gonder"/> ise gönderir.</summary>
    Task<DestekTalebi> TaslakGuncelleAsync(DestekTalebi talep, bool gonder);

    Task TaslakSilAsync(int talepId);

    /// <summary>"Sorun Bildir" penceresinden talep açar; sayfa/hata bilgisi eklenir, teknik ayrıntı dahili nota yazılır.</summary>
    Task<DestekTalebi> HataBildirimiOlusturAsync(SorunBildirimi bildirim);

    Task<List<DestekTalebi>> GetTaleplerimAsync();

    // === Talep detayı ve yazışma (talep eden ve ekip) ===

    /// <summary>Erişim yoksa null.</summary>
    Task<DestekTalepDetayi?> GetDetayAsync(int talepId);

    Task<DestekTalebiYanit> YanitEkleAsync(int talepId, DestekYanitIstegi istek);

    Task EylemUygulaAsync(int talepId, DestekEylem eylem, DestekEylemIstegi? istek = null);

    Task<DestekTalebiEk> EkYukleAsync(int talepId, string dosyaAdi, string mimeTipi, Stream icerik, long boyut);

    Task<DestekEkIcerigi?> EkIndirAsync(int ekId);

    // === Ekip ===

    Task<PagedResult<DestekTalebi>> GetPagedAsync(DestekTalebiFilterParams filter);

    /// <summary>Talep Yönetimi panosu: açık ve onay bekleyen talepler.</summary>
    Task<List<DestekTalebi>> GetPanoAsync();

    /// <summary>Destek personeli, hesabı olmayan bir müşteri adına talep açar.</summary>
    Task<DestekTalebi> PersonelTalepOlusturAsync(DestekTalebi talep);

    Task AtaAsync(int talepId, int kullaniciId, string? not = null);

    Task TransferEtAsync(int talepId, int departmanId, string gerekce);

    Task OncelikDegistirAsync(int talepId, DestekOncelik oncelik);

    Task EtiketleriGuncelleAsync(int talepId, string? etiketler);

    Task SilAsync(int talepId);

    Task<List<DestekAtanabilirKullanici>> GetAtanabilirKullanicilarAsync(int? departmanId);

    Task<DestekDashboardStats> GetDashboardStatsAsync();

    Task<DestekRaporStats> GetRaporStatsAsync(DateTime baslangic, DateTime bitis, int? departmanId = null);

    Task<List<DestekPerformansRapor>> GetPersonelPerformansRaporuAsync(DateTime baslangic, DateTime bitis, int? departmanId = null);

    // === Tanımlar (okuma herkese açık; yazma destektalebi.duzenle) ===

    Task<List<DestekDepartman>> GetDepartmanlarAsync(bool pasiflerDahil = false);

    Task<DestekDepartman> DepartmanKaydetAsync(DestekDepartman departman);

    /// <summary>Talebi olan departman silinmez, pasifleştirilir; sonucu açıklayan metin döner.</summary>
    Task<string> DepartmanSilAsync(int departmanId);

    Task<List<DestekDepartmanUye>> GetDepartmanUyeleriAsync(int departmanId);

    Task DepartmanUyesiEkleAsync(int departmanId, int kullaniciId, bool yonetici);

    Task DepartmanUyesiCikarAsync(int departmanId, int kullaniciId);

    Task<List<DestekKategori>> GetKategorilerAsync(bool pasiflerDahil = false);

    Task<DestekKategori> KategoriKaydetAsync(DestekKategori kategori);

    Task KategoriSilAsync(int kategoriId);

    Task<List<DestekSla>> GetSlaListesiAsync();

    Task<DestekSla> SlaKaydetAsync(DestekSla sla);

    Task SlaSilAsync(int slaId);

    Task<List<DestekHazirYanit>> GetHazirYanitlarAsync(int? departmanId = null, int? kategoriId = null, bool pasiflerDahil = false);

    Task<DestekHazirYanit> HazirYanitKaydetAsync(DestekHazirYanit hazirYanit);

    Task HazirYanitSilAsync(int hazirYanitId);

    Task<DestekGenelAyarlar> GetGenelAyarlarAsync();

    Task GenelAyarlariKaydetAsync(DestekGenelAyarlar ayarlar);

    /// <summary>Departman üyeliği ve atama listeleri için aktif kullanıcılar (destektalebi.duzenle).</summary>
    Task<List<Kullanici>> GetAktifKullanicilarAsync();

    // === Bilgi bankası (okuma herkese; yazma destek personeli ya da destektalebi.duzenle) ===

    Task<PagedResult<DestekBilgiBankasi>> GetBilgiBankasiPagedAsync(BilgiBankasiFilterParams filter);

    Task<DestekBilgiBankasi?> GetBilgiBankasiByIdAsync(int makaleId);

    Task<DestekBilgiBankasi> BilgiBankasiKaydetAsync(DestekBilgiBankasi makale);

    Task BilgiBankasiSilAsync(int makaleId);

    Task IncrementBilgiBankasiGoruntulenmeAsync(int makaleId);

    Task YararliBulAsync(int makaleId, bool yararli);

    Task<BilgiBankasiOzeti> GetBilgiBankasiOzetiAsync();

    // === Zamanlanmış işler (oturumsuz) ===

    /// <summary>SLA süresi geçen açık talepleri işaretler ve ilgililere bildirir.</summary>
    Task<int> CheckAndUpdateSlaViolationsAsync();

    /// <summary>Çözüldü durumunda ayarlanan gün kadar bekleyen talepleri kapatır.</summary>
    Task<int> CozulenTalepleriOtomatikKapatAsync();
}

/// <summary>
/// Destek talebi filtreleme parametreleri
/// </summary>
public class DestekTalebiFilterParams
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? AramaMetni { get; set; }
    public DestekDurum? Durum { get; set; }
    public DestekOncelik? Oncelik { get; set; }
    public DestekKaynak? Kaynak { get; set; }
    public int? DepartmanId { get; set; }
    public int? KategoriId { get; set; }

    /// <summary>0 = atanmamış talepler.</summary>
    public int? AtananKullaniciId { get; set; }
    public int? CariId { get; set; }
    public DateTime? BaslangicTarihi { get; set; }
    public DateTime? BitisTarihi { get; set; }
    public bool? SlaAsildi { get; set; }
    public bool? SadeceAcik { get; set; }
    public string? SortBy { get; set; } = "SonAktiviteTarihi";
    public bool SortDesc { get; set; } = true;
}

/// <summary>
/// Bilgi bankası filtreleme parametreleri
/// </summary>
public class BilgiBankasiFilterParams
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? AramaMetni { get; set; }
    public int? KategoriId { get; set; }
    public BilgiBankasiDurum? Durum { get; set; }
    public string? Etiket { get; set; }
    public string? SortBy { get; set; } = "CreatedAt";
    public bool SortDesc { get; set; } = true;
}

/// <summary>
/// Dashboard istatistikleri
/// </summary>
public class DestekDashboardStats
{
    // Genel İstatistikler
    public int ToplamTalepSayisi { get; set; }
    public int AcikTalepSayisi { get; set; }
    public int OnayBekleyenTalepSayisi { get; set; }
    public int BugunAcilanTalepSayisi { get; set; }
    public int BugunKapatilanTalepSayisi { get; set; }
    public int SlaAsildiSayisi { get; set; }

    // Öncelik Dağılımı
    public Dictionary<DestekOncelik, int> OncelikDagilimi { get; set; } = new();

    // Durum Dağılımı
    public Dictionary<DestekDurum, int> DurumDagilimi { get; set; } = new();

    // Departman Dağılımı
    public Dictionary<string, int> DepartmanDagilimi { get; set; } = new();

    // Kaynak Dağılımı
    public Dictionary<DestekKaynak, int> KaynakDagilimi { get; set; } = new();

    // Son Talepler
    public List<DestekTalebi> SonTalepler { get; set; } = new();

    // Performans Metrikleri (veri yoksa null)
    public double? OrtalamaIlkYanitSuresiSaat { get; set; }
    public double? OrtalamaCozumSuresiSaat { get; set; }
    public double? OrtalamaMemuniyetPuani { get; set; }
}

/// <summary>
/// Rapor istatistikleri
/// </summary>
public class DestekRaporStats
{
    public DateTime BaslangicTarihi { get; set; }
    public DateTime BitisTarihi { get; set; }

    public int ToplamAcilanTalep { get; set; }
    public int ToplamKapatilanTalep { get; set; }
    public int ToplamSlaAsimi { get; set; }

    public double OrtalamaIlkYanitSuresiDakika { get; set; }
    public double OrtalamaCozumSuresiDakika { get; set; }
    public double OrtalamaMemuniyetPuani { get; set; }

    // Günlük trend
    public List<GunlukTalepTrend> GunlukTrend { get; set; } = new();

    // Kategori bazlı
    public List<KategoriTalepStats> KategoriBazli { get; set; } = new();
}

/// <summary>
/// Günlük talep trendi
/// </summary>
public class GunlukTalepTrend
{
    public DateTime Tarih { get; set; }
    public int AcilanTalep { get; set; }
    public int KapatilanTalep { get; set; }
}

/// <summary>
/// Kategori bazlı istatistikler
/// </summary>
public class KategoriTalepStats
{
    public int KategoriId { get; set; }
    public string KategoriAdi { get; set; } = string.Empty;
    public int TalepSayisi { get; set; }
    public double OrtalamaСozumSuresi { get; set; }
}

/// <summary>
/// Personel performans raporu
/// </summary>
public class DestekPerformansRapor
{
    public int KullaniciId { get; set; }
    public string KullaniciAdi { get; set; } = string.Empty;
    public int AtananTalepSayisi { get; set; }
    public int CozulenTalepSayisi { get; set; }
    public int SlaAsimSayisi { get; set; }
    public double OrtalamaYanitSuresiDakika { get; set; }
    public double OrtalamaCozumSuresiDakika { get; set; }
    public double OrtalamaMemuniyetPuani { get; set; }
    public int ToplamYanitSayisi { get; set; }
}
