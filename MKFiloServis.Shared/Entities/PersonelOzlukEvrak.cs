namespace MKFiloServis.Shared.Entities;

/// <summary>
/// Personel özlük dosyası evrak durumları
/// </summary>
public class PersonelOzlukEvrak : BaseEntity
{
    public int SoforId { get; set; }
    public int EvrakTanimId { get; set; }

    public bool Tamamlandi { get; set; }
    public DateTime? TamamlanmaTarihi { get; set; }

    /// <summary>
    /// Belgenin geçerlilik bitiş tarihi (Ehliyet, SRC, Psikoteknik, Sağlık Raporu vb. için)
    /// </summary>
    public DateTime? GecerlilikBitisTarihi { get; set; }

    public string? DosyaYolu { get; set; }
    public string? DosyaAdi { get; set; }
    public string? DosyaTipi { get; set; }
    public long? DosyaBoyutu { get; set; }
    public string? Aciklama { get; set; }

    /// <summary>
    /// Mevcut versiyon numarası (1'den başlar)
    /// </summary>
    public int VersiyonNo { get; set; } = 1;

    /// <summary>
    /// Son değişiklik notu
    /// </summary>
    public string? SonDegisiklikNotu { get; set; }

    // Navigation
    public virtual Sofor Sofor { get; set; } = null!;
    public virtual OzlukEvrakTanim EvrakTanim { get; set; } = null!;

    /// <summary>
    /// Versiyon geçmişi - önceki versiyonlar
    /// </summary>
    public virtual ICollection<PersonelOzlukEvrakVersiyon> Versiyonlar { get; set; } = new List<PersonelOzlukEvrakVersiyon>();
}

/// <summary>
/// SGK'lı personel özlük dosyasında olması gereken evrak tanımları
/// </summary>
public class OzlukEvrakTanim : BaseEntity
{
    public string EvrakAdi { get; set; } = string.Empty;
    public string? Aciklama { get; set; }
    public OzlukEvrakKategori Kategori { get; set; } = OzlukEvrakKategori.Genel;
    public bool Zorunlu { get; set; } = true;
    public int SiraNo { get; set; }
    public bool Aktif { get; set; } = true;

    // Hangi görev tipleri için geçerli (null ise tümü için)
    public string? GecerliGorevler { get; set; } // Örn: "1,2,3" veya null (tümü)

    // Navigation
    public virtual ICollection<PersonelOzlukEvrak> PersonelEvraklari { get; set; } = new List<PersonelOzlukEvrak>();
}

/// <summary>
/// Özlük evrak kategorileri
/// </summary>
public enum OzlukEvrakKategori
{
    Genel = 1,
    KimlikBelgeleri = 2,
    EgitimBelgeleri = 3,
    SaglikBelgeleri = 4,
    SoforBelgeleri = 5,
    SGKBelgeleri = 6,
    IseGirisBelgeleri = 7,
    Diger = 99
}


