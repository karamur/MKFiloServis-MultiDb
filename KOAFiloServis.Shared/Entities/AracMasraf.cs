using System.ComponentModel.DataAnnotations.Schema;

namespace KOAFiloServis.Shared.Entities;

/// <summary>
/// Araç masraf girişleri
/// </summary>
public class AracMasraf : BaseEntity, IFirmaTenant
{
    // Kural 4: FirmaId dogrudan entity'de (Arac uzerinden join gerekmez)
    public int? FirmaId { get; set; }
    public virtual Firma? Firma { get; set; }

    public DateTime MasrafTarihi { get; set; }
    public decimal Tutar { get; set; }
    public string? Aciklama { get; set; }
    public string? BelgeNo { get; set; } // Fatura/Fis numarasi
    public bool ArizaKaynaklimi { get; set; } = false; // Ariza nedeniyle mi?

    /// <summary>
    /// Odeme kaynagi (Kasa, Banka, Personel Cebinden)
    /// </summary>
    public MasrafOdemeKaynak OdemeKaynak { get; set; } = MasrafOdemeKaynak.Kasa;

    /// <summary>
    /// Personel cebinden odendiyse hangi personel
    /// </summary>
    public int? PersonelCebindenId { get; set; }
    public virtual Sofor? PersonelCebinden { get; set; }

    /// <summary>
    /// Personele geri odeme yapildi mi?
    /// </summary>
    public bool PersoneleOdendi { get; set; } = false;

    /// <summary>
    /// Geri odeme tarihi
    /// </summary>
    public DateTime? PersonelOdemeTarihi { get; set; }

    /// <summary>
    /// Odeme yapilan banka/kasa hesabi
    /// </summary>
    public int? BankaHesapId { get; set; }
    public virtual BankaHesap? BankaHesap { get; set; }

    // Foreign Keys
    public int AracId { get; set; }
    public int MasrafKalemiId { get; set; }
    public int? GuzergahId { get; set; } // Ariza kaynakli personel ulasim masraflari icin
    public int? ServisCalismaId { get; set; } // Ilgili servis calismasi
    public int? SoforId { get; set; }
    public int? CariId { get; set; }
    public int? MuhasebeFisId { get; set; }

    // Navigation Properties
    public virtual Arac Arac { get; set; } = null!;
    public virtual MasrafKalemi MasrafKalemi { get; set; } = null!;
    public virtual Guzergah? Guzergah { get; set; }
    public virtual ServisCalisma? ServisCalisma { get; set; }
    public virtual Sofor? Sofor { get; set; }
    public virtual Cari? Cari { get; set; }
    public virtual MuhasebeFis? MuhasebeFis { get; set; }

    /// <summary>
    /// Personel cebinden harcama mı?
    /// </summary>
    [NotMapped]
    public bool IsPersonelCebinden => OdemeKaynak == MasrafOdemeKaynak.PersonelCebinden;
}

/// <summary>
/// Masraf ödeme kaynağı
/// </summary>
public enum MasrafOdemeKaynak
{
    Kasa = 1,
    Banka = 2,
    PersonelCebinden = 3
}
