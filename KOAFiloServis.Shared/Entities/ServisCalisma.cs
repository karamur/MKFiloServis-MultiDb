using System.ComponentModel.DataAnnotations.Schema;

namespace KOAFiloServis.Shared.Entities;

/// <summary>
/// Servis �al��ma kay�tlar� - Hangi g�n, hangi ara�, hangi �of�r, hangi g�zergahta �al��t�
/// </summary>
public class ServisCalisma : BaseEntity, IFirmaTenant
{
    // Aşama C3 (K4): firma bazlı izolasyon.
    public int? FirmaId { get; set; }
    public virtual Firma? Firma { get; set; }

    public DateTime CalismaTarihi { get; set; }
    public ServisTuru ServisTuru { get; set; }
    public decimal? Fiyat { get; set; } // Override fiyat, null ise g�zergah fiyat� kullan�l�r
    public int? KmBaslangic { get; set; }
    public int? KmBitis { get; set; }
    public TimeSpan? BaslangicSaati { get; set; }
    public TimeSpan? BitisSaati { get; set; }
    public bool ArizaOlduMu { get; set; } = false;
    public string? ArizaAciklamasi { get; set; }
    public CalismaDurum Durum { get; set; } = CalismaDurum.Tamamlandi;
    public string? Notlar { get; set; }

    // Foreign Keys
    public int AracId { get; set; }
    public int SoforId { get; set; }
    public int GuzergahId { get; set; }

    // Hesaplanan fiyat
    [NotMapped]
    public decimal HesaplananFiyat => Fiyat ?? 0;

    // Navigation Properties
    public virtual Arac Arac { get; set; } = null!;
    public virtual Sofor Sofor { get; set; } = null!;
    public virtual Guzergah Guzergah { get; set; } = null!;
    public virtual ICollection<AracMasraf> ArizaMasraflari { get; set; } = new List<AracMasraf>();
}

public enum ServisTuru
{
    Sabah = 1,
    Aksam = 2,
    SabahAksam = 3,
    Ozel = 4,
    YardaMesai = 5
}

public enum CalismaDurum
{
    Planli = 1,
    Tamamlandi = 2,
    IptalEdildi = 3,
    ArizaNedeniyleYapilamadi = 4
}
