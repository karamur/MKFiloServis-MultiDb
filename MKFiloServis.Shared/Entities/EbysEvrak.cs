namespace MKFiloServis.Shared.Entities;

/// <summary>
/// EBYS Gelen/Giden Evrak Entity - Resmi yazışma ve evrak yönetimi
/// </summary>
public class EbysEvrak : BaseEntity
{
    public string EvrakNo { get; set; } = string.Empty;
    public EvrakYonu Yon { get; set; } = EvrakYonu.Gelen;
    public DateTime EvrakTarihi { get; set; } = DateTime.Today;
    public DateTime? KayitTarihi { get; set; } = DateTime.Now;
    
    public string Konu { get; set; } = string.Empty;
    public string? Ozet { get; set; }
    public string? GonderenKurum { get; set; } // Gelen evrak için
    public string? AliciKurum { get; set; } // Giden evrak için
    
    // Gelen evrak alanları
    public string? GelisNo { get; set; }
    public DateTime? GelisTarihi { get; set; }
    
    // Giden evrak alanları
    public string? GidisNo { get; set; }
    public DateTime? GonderimTarihi { get; set; }
    public GonderimYontemi GonderimYontemi { get; set; } = GonderimYontemi.Elden;
    
    // Kategorilendirme
    public int? KategoriId { get; set; }
    public EvrakOncelik Oncelik { get; set; } = EvrakOncelik.Normal;
    public EvrakGizlilik Gizlilik { get; set; } = EvrakGizlilik.Normal;
    
    // Durum takibi
    public EbysEvrakDurum Durum { get; set; } = EbysEvrakDurum.Beklemede;
    public DateTime? SonIslemTarihi { get; set; }
    public DateTime? CevapSuresi { get; set; }
    public bool CevapGerekli { get; set; }
    
    // İlişkili evrak
    public int? UstEvrakId { get; set; }
    
    // Atama bilgisi
    public int? AtananKullaniciId { get; set; }
    public int? AtananDepartmanId { get; set; }
    
    // Ek bilgiler
    public string? Aciklama { get; set; }
    public string? Notlar { get; set; }
    
    // Navigation
    public virtual EbysEvrakKategori? Kategori { get; set; }
    public virtual EbysEvrak? UstEvrak { get; set; }
    public virtual Kullanici? AtananKullanici { get; set; }
    public virtual ICollection<EbysEvrak> AltEvraklar { get; set; } = new List<EbysEvrak>();
    public virtual ICollection<EbysEvrakDosya> Dosyalar { get; set; } = new List<EbysEvrakDosya>();
    public virtual ICollection<EbysEvrakAtama> Atamalar { get; set; } = new List<EbysEvrakAtama>();
    public virtual ICollection<EbysEvrakHareket> Hareketler { get; set; } = new List<EbysEvrakHareket>();
}

/// <summary>
/// Evrak yönü
/// </summary>
public enum EvrakYonu
{
    Gelen = 1,
    Giden = 2
}

/// <summary>
/// Evrak öncelik seviyesi
/// </summary>
public enum EvrakOncelik
{
    Dusuk = 1,
    Normal = 2,
    Yuksek = 3,
    Acil = 4
}

/// <summary>
/// Evrak gizlilik seviyesi
/// </summary>
public enum EvrakGizlilik
{
    Normal = 1,
    Gizli = 2,
    CokGizli = 3
}

/// <summary>
/// Gönderim yöntemi
/// </summary>
public enum GonderimYontemi
{
    Elden = 1,
    Posta = 2,
    Kargo = 3,
    Email = 4,
    Faks = 5,
    KEP = 6 // Kayıtlı Elektronik Posta
}

/// <summary>
/// EBYS Evrak Durumları
/// </summary>
public enum EbysEvrakDurum
{
    Taslak = 0,
    Beklemede = 1,
    Isleniyor = 2,
    AtamaBekliyor = 3,
    CevapBekliyor = 4,
    Cevaplandi = 5,
    Tamamlandi = 6,
    Arsivlendi = 7,
    IptalEdildi = 8
}

/// <summary>
/// EBYS Evrak Kategorileri
/// </summary>
public class EbysEvrakKategori : BaseEntity
{
    public string KategoriAdi { get; set; } = string.Empty;
    public string? Aciklama { get; set; }
    public int SiraNo { get; set; }
    public bool Aktif { get; set; } = true;
    public string? Renk { get; set; } = "#6c757d";
    public string? Ikon { get; set; } = "bi-folder";
    
    public virtual ICollection<EbysEvrak> Evraklar { get; set; } = new List<EbysEvrak>();
}

/// <summary>
/// EBYS Evrak Dosyaları
/// </summary>
public class EbysEvrakDosya : BaseEntity
{
    public int EvrakId { get; set; }
    public string DosyaAdi { get; set; } = string.Empty;
    public string DosyaYolu { get; set; } = string.Empty;
    public string? DosyaTipi { get; set; }
    public long DosyaBoyutu { get; set; }
    public string? Aciklama { get; set; }
    public bool AsilNusha { get; set; }

    /// <summary>
    /// Mevcut versiyon numarası (1'den başlar)
    /// </summary>
    public int VersiyonNo { get; set; } = 1;

    /// <summary>
    /// Son değişiklik notu
    /// </summary>
    public string? SonDegisiklikNotu { get; set; }

    public virtual EbysEvrak? Evrak { get; set; }

    /// <summary>
    /// Versiyon geçmişi - önceki versiyonlar
    /// </summary>
    public virtual ICollection<EbysEvrakDosyaVersiyon> Versiyonlar { get; set; } = new List<EbysEvrakDosyaVersiyon>();
}

/// <summary>
/// EBYS Evrak Atama Kayıtları
/// </summary>
public class EbysEvrakAtama : BaseEntity
{
    public int EvrakId { get; set; }
    public int? AtananKullaniciId { get; set; }
    public int? AtananDepartmanId { get; set; }
    public int AtayanKullaniciId { get; set; }
    public DateTime AtamaTarihi { get; set; } = DateTime.Now;
    public string? Talimat { get; set; }
    public DateTime? TeslimTarihi { get; set; }
    public AtamaDurum Durum { get; set; } = AtamaDurum.Beklemede;
    public string? Sonuc { get; set; }
    
    public virtual EbysEvrak? Evrak { get; set; }
    public virtual Kullanici? AtananKullanici { get; set; }
    public virtual Kullanici? AtayanKullanici { get; set; }
}

/// <summary>
/// Atama durumları
/// </summary>
public enum AtamaDurum
{
    Beklemede = 1,
    Isleniyor = 2,
    Tamamlandi = 3,
    Reddedildi = 4,
    Devredildi = 5
}

/// <summary>
/// EBYS Evrak Hareket/İşlem Geçmişi
/// </summary>
public class EbysEvrakHareket : BaseEntity
{
    public int EvrakId { get; set; }
    public int KullaniciId { get; set; }
    public EbysHareketTipi HareketTipi { get; set; }
    public string Aciklama { get; set; } = string.Empty;
    public DateTime IslemTarihi { get; set; } = DateTime.Now;
    public string? EskiDeger { get; set; }
    public string? YeniDeger { get; set; }
    
    public virtual EbysEvrak? Evrak { get; set; }
    public virtual Kullanici? Kullanici { get; set; }
}

/// <summary>
/// Hareket tipleri
/// </summary>
public enum EbysHareketTipi
{
    Olusturuldu = 1,
    Guncellendi = 2,
    AtamaYapildi = 3,
    DurumDegisti = 4,
    DosyaEklendi = 5,
    DosyaSilindi = 6,
    CevapYazildi = 7,
    Arsivlendi = 8,
    IptalEdildi = 9,
    NotEklendi = 10,
    Devredildi = 11
}


