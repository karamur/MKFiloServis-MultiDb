using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MKFiloServis.Shared.Entities;

/// <summary>
/// EBYS Gelişmiş Arama Filtresi
/// </summary>
public class EbysGelismisAramaFiltre
{
    // Genel Arama
    public string? AramaMetni { get; set; }
    public EbysAramaTipi AramaTipi { get; set; } = EbysAramaTipi.Tumu;
    
    // Kaynak Filtreleri
    public List<EbysAramaKaynak> Kaynaklar { get; set; } = [];
    
    // Tarih Filtreleri
    public DateTime? BaslangicTarihi { get; set; }
    public DateTime? BitisTarihi { get; set; }
    public EbysTarihAlani TarihAlani { get; set; } = EbysTarihAlani.OlusturmaTarihi;
    
    // Kategori Filtreleri
    public List<string> Kategoriler { get; set; } = [];
    public List<int> KategoriIdler { get; set; } = [];
    
    // Durum Filtreleri
    public List<string> Durumlar { get; set; } = [];
    public bool? SadeceDosyasiOlanlar { get; set; }
    public bool? SadeceAktifKayitlar { get; set; } = true;
    
    // Risk Filtreleri
    public bool? SadeceSuresiDolmuslar { get; set; }
    public bool? SadeceYaklasanlar { get; set; }
    public int? YaklasanGunSayisi { get; set; } = 30;
    
    // İlgili Kayıt Filtreleri
    public int? PersonelId { get; set; }
    public int? AracId { get; set; }
    public int? KullaniciId { get; set; }
    
    // Evrak Yönü (Gelen/Giden)
    public EvrakYonu? EvrakYonu { get; set; }
    
    // Öncelik ve Gizlilik
    public EvrakOncelik? Oncelik { get; set; }
    public EvrakGizlilik? Gizlilik { get; set; }
    
    // Sıralama
    public EbysAramaSiralama Siralama { get; set; } = EbysAramaSiralama.TarihAzalan;
    
    // Sayfalama
    public int Sayfa { get; set; } = 1;
    public int SayfaBoyutu { get; set; } = 25;
}

/// <summary>
/// Arama yapılacak alanlar
/// </summary>
public enum EbysAramaTipi
{
    Tumu = 0,
    BelgeAdi = 1,
    Icerik = 2,
    DosyaAdi = 3,
    Aciklama = 4,
    Kategori = 5,
    IlgiliKayit = 6
}

/// <summary>
/// Arama kaynakları
/// </summary>
public enum EbysAramaKaynak
{
    PersonelOzluk = 1,
    AracEvrak = 2,
    GelenEvrak = 3,
    GidenEvrak = 4
}

/// <summary>
/// Tarih filtreleme alanı
/// </summary>
public enum EbysTarihAlani
{
    OlusturmaTarihi = 0,
    GuncellemeTarihi = 1,
    BitisTarihi = 2,
    EvrakTarihi = 3
}

/// <summary>
/// Sıralama seçenekleri
/// </summary>
public enum EbysAramaSiralama
{
    TarihAzalan = 0,
    TarihArtan = 1,
    AdAZ = 2,
    AdZA = 3,
    Alaka = 4,
    KategoriAZ = 5
}

/// <summary>
/// Arama sonucu
/// </summary>
public class EbysAramaSonuc
{
    public List<EbysAramaSonucItem> Sonuclar { get; set; } = [];
    public int ToplamSonuc { get; set; }
    public int Sayfa { get; set; }
    public int SayfaBoyutu { get; set; }
    public int ToplamSayfa => (int)Math.Ceiling((double)ToplamSonuc / SayfaBoyutu);
    public TimeSpan AramaSuresi { get; set; }
    public EbysAramaIstatistik Istatistikler { get; set; } = new();
}

/// <summary>
/// Arama sonuç satırı
/// </summary>
public class EbysAramaSonucItem
{
    public EbysAramaKaynak Kaynak { get; set; }
    public int KayitId { get; set; }
    public int? DosyaId { get; set; }
    
    public string BelgeAdi { get; set; } = string.Empty;
    public string Kategori { get; set; } = string.Empty;
    public string IlgiliKayitAdi { get; set; } = string.Empty;
    public string IlgiliKayitKodu { get; set; } = string.Empty;
    
    public string? Aciklama { get; set; }
    public string? Ozet { get; set; }
    public string? DosyaAdi { get; set; }
    public string? DosyaTipi { get; set; }
    public long? DosyaBoyutu { get; set; }
    
    public DateTime? OlusturmaTarihi { get; set; }
    public DateTime? GuncellemeTarihi { get; set; }
    public DateTime? BitisTarihi { get; set; }
    
    public string Durum { get; set; } = string.Empty;
    public string RiskDurumu { get; set; } = "Normal";
    public bool YaklasanMi { get; set; }
    public bool SuresiDolmusMu { get; set; }
    public bool DosyaVar { get; set; }
    
    public string DetayUrl { get; set; } = string.Empty;
    public double AlakaSkoru { get; set; }
    
    // Eşleşen metin vurgulama için
    public string? EslesenMetin { get; set; }
}

/// <summary>
/// Arama istatistikleri
/// </summary>
public class EbysAramaIstatistik
{
    public int PersonelOzlukSayisi { get; set; }
    public int AracEvrakSayisi { get; set; }
    public int GelenEvrakSayisi { get; set; }
    public int GidenEvrakSayisi { get; set; }
    
    public Dictionary<string, int> KategoriBazliSayilar { get; set; } = [];
    public Dictionary<string, int> DurumBazliSayilar { get; set; } = [];
    public int DosyasiOlanSayisi { get; set; }
    public int RiskliSayisi { get; set; }
}

/// <summary>
/// Arama geçmişi kaydı
/// </summary>
public class EbysAramaGecmisi : BaseEntity
{
    [Required]
    public int KullaniciId { get; set; }
    
    [Required, MaxLength(500)]
    public string AramaMetni { get; set; } = string.Empty;
    
    [MaxLength(2000)]
    public string? FiltreJson { get; set; }
    
    public int SonucSayisi { get; set; }
    
    public DateTime AramaTarihi { get; set; } = DateTime.Now;
    
    [ForeignKey(nameof(KullaniciId))]
    public virtual Kullanici? Kullanici { get; set; }
}

/// <summary>
/// Kayıtlı arama (favori arama)
/// </summary>
public class EbysKayitliArama : BaseEntity
{
    [Required]
    public int KullaniciId { get; set; }
    
    [Required, MaxLength(100)]
    public string AramaAdi { get; set; } = string.Empty;
    
    [MaxLength(250)]
    public string? Aciklama { get; set; }
    
    [Required, MaxLength(2000)]
    public string FiltreJson { get; set; } = string.Empty;
    
    public bool BildirimAktif { get; set; }
    
    public int SiraNo { get; set; }
    
    [ForeignKey(nameof(KullaniciId))]
    public virtual Kullanici? Kullanici { get; set; }
}

/// <summary>
/// Arama önerisi
/// </summary>
public class EbysAramaOnerisi
{
    public string Oneri { get; set; } = string.Empty;
    public EbysOneriTipi Tip { get; set; }
    public int Skor { get; set; }
}

public enum EbysOneriTipi
{
    SonArama = 1,
    PopulerArama = 2,
    BenzerArama = 3,
    Kategori = 4,
    Etiket = 5
}

/// <summary>
/// Belge embedding'i (Semantic Search için vektör depolama)
/// </summary>
public class EbysBelgeEmbedding : BaseEntity
{
    /// <summary>
    /// Kaynak tipi (PersonelOzluk, AracEvrak, GelenEvrak, GidenEvrak)
    /// </summary>
    [Required]
    public EbysAramaKaynak Kaynak { get; set; }

    /// <summary>
    /// Kaynak kaydın ID'si
    /// </summary>
    [Required]
    public int KaynakId { get; set; }

    /// <summary>
    /// Dosya ID'si (varsa)
    /// </summary>
    public int? DosyaId { get; set; }

    /// <summary>
    /// Embedding oluşturulan metin
    /// </summary>
    [Required, MaxLength(8000)]
    public string Metin { get; set; } = string.Empty;

    /// <summary>
    /// Metnin özet/başlık hali (önizleme için)
    /// </summary>
    [MaxLength(500)]
    public string? MetinOzet { get; set; }

    /// <summary>
    /// Embedding vektörü (JSON formatında)
    /// </summary>
    [Required]
    public string EmbeddingJson { get; set; } = string.Empty;

    /// <summary>
    /// Embedding boyutu (tipik: 768 veya 1024)
    /// </summary>
    public int EmbeddingBoyutu { get; set; }

    /// <summary>
    /// Kullanılan model adı
    /// </summary>
    [MaxLength(100)]
    public string? ModelAdi { get; set; }

    /// <summary>
    /// Embedding oluşturulma tarihi
    /// </summary>
    public DateTime OlusturmaTarihi { get; set; } = DateTime.Now;

    /// <summary>
    /// Son güncelleme tarihi
    /// </summary>
    public DateTime? GuncellemeTarihi { get; set; }

    /// <summary>
    /// Embedding vektörünü float dizisine çevirir
    /// </summary>
    [NotMapped]
    public float[]? Embedding
    {
        get
        {
            if (string.IsNullOrEmpty(EmbeddingJson))
                return null;
            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<float[]>(EmbeddingJson);
            }
            catch
            {
                return null;
            }
        }
        set
        {
            if (value == null)
            {
                EmbeddingJson = string.Empty;
                EmbeddingBoyutu = 0;
            }
            else
            {
                EmbeddingJson = System.Text.Json.JsonSerializer.Serialize(value);
                EmbeddingBoyutu = value.Length;
            }
        }
    }
}

/// <summary>
/// Semantic arama sonucu
/// </summary>
public class SemanticAramaSonuc
{
    public EbysBelgeEmbedding Embedding { get; set; } = null!;
    public double BenzerlikSkoru { get; set; }
    public string Kaynak { get; set; } = string.Empty;
    public string BelgeAdi { get; set; } = string.Empty;
    public string? Ozet { get; set; }
    public DateTime? Tarih { get; set; }
    public string DetayUrl { get; set; } = string.Empty;
}


