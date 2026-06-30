namespace MKFiloServis.Shared.Entities;

/// <summary>
/// Masraf kalemleri tan�mlar�
/// </summary>
public class MasrafKalemi : BaseEntity, IFirmaTenant
{
    // Aşama C3 (K4): firma bazlı izolasyon.
    public int? FirmaId { get; set; }
    public virtual Firma? Firma { get; set; }

    public string MasrafKodu { get; set; } = string.Empty;
    public string MasrafAdi { get; set; } = string.Empty;
    public MasrafKategori Kategori { get; set; }
    public bool Aktif { get; set; } = true;
    public string? Notlar { get; set; }

    // Navigation Properties
    public virtual ICollection<AracMasraf> AracMasraflari { get; set; } = new List<AracMasraf>();
}

public enum MasrafKategori
{
    Yakit = 1,
    Bakim = 2,
    Tamir = 3,
    Sigorta = 4,
    Vergi = 5,
    Personel = 6,        // Taksi, ulaşım fişleri vb.
    Lastik = 7,
    YedekParca = 8,
    Mutfak = 9,          // Mutfak giderleri
    Ofis = 10,           // Ofis malzemeleri
    Temizlik = 11,       // Temizlik malzemeleri
    Kirtasiye = 12,      // Kırtasiye giderleri
    Diger = 99
}


