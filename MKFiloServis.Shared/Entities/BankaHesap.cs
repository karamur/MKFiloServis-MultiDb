namespace MKFiloServis.Shared.Entities;

/// <summary>
/// Banka hesapları
/// </summary>
/// Kural 4: FirmaId NOT NULL (TenantNullableFirmaId kaldırıldı, DB seviyesinde NOT NULL).
public class BankaHesap : BaseEntity, IFirmaTenant
{
    /// <summary>
    /// Tenant: Bu kasa/banka hesabının ait olduğu firma. (K6)
    /// </summary>
    public int? FirmaId { get; set; }
    public virtual Firma? Firma { get; set; }

    public string? HesapKodu { get; set; }
    public string HesapAdi { get; set; } = string.Empty;
    public HesapTipi HesapTipi { get; set; }
    public string? BankaAdi { get; set; }
    public string? SubeAdi { get; set; }
    public string? SubeKodu { get; set; }
    public string? HesapNo { get; set; }
    public string? Iban { get; set; }
    public string? ParaBirimi { get; set; } = "TRY";
    public decimal AcilisBakiye { get; set; } = 0;
    public bool Aktif { get; set; } = true;
    public string? Notlar { get; set; }

    // Kredi Kartı için ek alanlar
    public Guid? KrediTaksitGrupId { get; set; } // İlişkili kredi/taksit grubu

    // Muhasebe Eşleştirme - Varsayılan Kodlar
    public string? VarsayilanMuhasebeKodu { get; set; } // Örn: Kasa=100, Banka=102
    public string? VarsayilanKostMerkezi { get; set; }

    // Navigation Properties
    public virtual ICollection<BankaKasaHareket> Hareketler { get; set; } = new List<BankaKasaHareket>();
}

public enum HesapTipi
{
    Kasa = 1,
    VadesizHesap = 2,
    VadeliHesap = 3,
    KrediHesabi = 4,
    KrediKarti = 5
}


