namespace MKFiloServis.Shared.Entities;

/// <summary>
/// Lastik deposu / saklama yeri
/// </summary>
public class LastikDepo : BaseEntity
{
    /// <summary>Depo adı (ör: "Ana Depo", "Şube Garaj", "Dış Oto")</summary>
    public string DepoAdi { get; set; } = string.Empty;

    /// <summary>Depo adres / konum bilgisi</summary>
    public string? Adres { get; set; }

    /// <summary>Sorumlu kişi adı</summary>
    public string? SorumluKisi { get; set; }

    /// <summary>İletişim numarası</summary>
    public string? Telefon { get; set; }

    public string? Notlar { get; set; }
    public bool Aktif { get; set; } = true;

    public virtual ICollection<LastikStok> Stoklar { get; set; } = new List<LastikStok>();
}

/// <summary>
/// Bireysel lastik kaydı — her lastik ayrı bir kayıt olarak tutulur.
/// </summary>
public class LastikStok : BaseEntity
{
    /// <summary>Lastiğin şu an bulunduğu depo (araçta takılı değilse)</summary>
    public int? DepoId { get; set; }
    public virtual LastikDepo? Depo { get; set; }

    /// <summary>Lastiğin takılı olduğu araç (null = depoda bekliyor)</summary>
    public int? AracId { get; set; }
    public virtual Arac? Arac { get; set; }

    /// <summary>Yedek lastik mi? true ise plaka atanmaz, depoda yedek olarak beklenir.</summary>
    public bool YedekMi { get; set; } = false;

    /// <summary>
    /// Lastiğin ilişkili olduğu araç (depodayken de plakaya bağlı izlenir).
    /// Değişimde araçtan sökülen lastikler bu alana sahip olur; AracId=null ama KaynakAracId set.
    /// </summary>
    public int? KaynakAracId { get; set; }
    public virtual Arac? KaynakArac { get; set; }

    /// <summary>Marka (Bridgestone, Michelin vb.)</summary>
    public string? Marka { get; set; }

    /// <summary>Ebat (ör: 195/65R15)</summary>
    public string Ebat { get; set; } = string.Empty;

    /// <summary>Sezon: Yaz / Kış / Dört Mevsim</summary>
    public LastikSezon Sezon { get; set; } = LastikSezon.YazLastigi;

    /// <summary>Seri / DOT numarası (isteğe bağlı)</summary>
    public string? SeriNo { get; set; }

    /// <summary>Durum: Kullanılabilir, Hasarlı, Hurda</summary>
    public LastikDurum Durum { get; set; } = LastikDurum.Kullanilabilir;

    /// <summary>Aktif mi? false = hurda atıldı / pasif (soft discard)</summary>
    public bool Aktif { get; set; } = true;

    public string? Notlar { get; set; }

    public virtual ICollection<LastikDegisim> Degisimler { get; set; } = new List<LastikDegisim>();
}

/// <summary>
/// Araç bazında lastik değişim kaydı
/// </summary>
public class LastikDegisim : BaseEntity
{
    public int AracId { get; set; }
    public virtual Arac Arac { get; set; } = null!;

    /// <summary>Değişim tarihi</summary>
    public DateTime DegisimTarihi { get; set; }

    /// <summary>Km durumu değişim anında</summary>
    public int? KmDurumu { get; set; }

    /// <summary>Değişim tipi: Mevsimlik, Arıza, Periyodik</summary>
    public LastikDegisimTipi DegisimTipi { get; set; } = LastikDegisimTipi.Mevsimlik;

    // ----- Sökülen Lastik -----

    /// <summary>Sökülen lastiğin stok kaydı (opsiyonel)</summary>
    public int? SokulenStokId { get; set; }
    public virtual LastikStok? SokulenStok { get; set; }

    /// <summary>Sökülen lastik pozisyonu (ör: "Ön Sol")</summary>
    public string? SokulenPozisyon { get; set; }

    /// <summary>Sökülen lastiğin gönderildiği depo (saklanacaksa)</summary>
    public int? HedefDepoId { get; set; }
    public virtual LastikDepo? HedefDepo { get; set; }

    // ----- Takılan Lastik -----

    /// <summary>Takılan lastiğin stok kaydı (opsiyonel)</summary>
    public int? TakilanStokId { get; set; }
    public virtual LastikStok? TakilanStok { get; set; }

    /// <summary>Takılan lastik pozisyonu (ör: "Ön Sol")</summary>
    public string? TakilanPozisyon { get; set; }

    /// <summary>Takılan lastiğin alındığı depo</summary>
    public int? KaynakDepoId { get; set; }
    public virtual LastikDepo? KaynakDepo { get; set; }

    /// <summary>İşlemi yapan servis/atölye adı</summary>
    public string? YapilanYer { get; set; }

    /// <summary>İşlem ücreti</summary>
    public decimal? Ucret { get; set; }

    public string? Notlar { get; set; }
}

// -------------------- Enum'lar --------------------

public enum LastikSezon
{
    YazLastigi = 1,
    KisLastigi = 2,
    DortMevsim = 3
}

public enum LastikDurum
{
    Kullanilabilir = 1,
    Hasarli = 2,
    Hurda = 3,
    /// <summary>
    /// Lastik değişiminde sökülen ancak depoya teslim edilmediği için kayıp/eksik sayılan lastik.
    /// </summary>
    Kayip = 4,
    /// <summary>
    /// Araçtan sökülen, tamir/rekap için bekleyen lastik. Aktif=true, depoda muhafaza.
    /// </summary>
    Tamir = 5
}

public enum LastikDegisimTipi
{
    Mevsimlik = 1,
    Ariza = 2,
    Periyodik = 3,
    Diger = 4
}

public enum LastikSezonTipi
{
    Yaz = 1,
    Kis = 2
}

/// <summary>
/// Mevsimlik lastik değişim dönem ayarları.
/// Her sezon (Yaz/Kış) için başlangıç/bitiş ayı-günü ve uyarı eşiği tanımlanır.
/// </summary>
public class LastikSezonAyar : BaseEntity
{
    /// <summary>Dönem adı (ör: "Yaz Dönemi", "Kış Dönemi")</summary>
    public string Ad { get; set; } = string.Empty;

    /// <summary>Sezon tipi: Yaz veya Kış</summary>
    public LastikSezonTipi SezonTipi { get; set; }

    /// <summary>Dönem başlangıç ayı (1-12)</summary>
    public int BaslangicAyi { get; set; }

    /// <summary>Dönem başlangıç günü (1-31)</summary>
    public int BaslangicGunu { get; set; }

    /// <summary>Dönem bitiş ayı (1-12)</summary>
    public int BitisAyi { get; set; }

    /// <summary>Dönem bitiş günü (1-31)</summary>
    public int BitisGunu { get; set; }

    /// <summary>Dönem başlamadan kaç gün önce uyarı gösterilsin</summary>
    public int UyariOncesiGun { get; set; } = 14;

    public string? Notlar { get; set; }
    public bool Aktif { get; set; } = true;
}


