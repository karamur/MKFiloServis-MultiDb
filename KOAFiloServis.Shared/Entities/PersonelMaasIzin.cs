using System.ComponentModel.DataAnnotations.Schema;

namespace KOAFiloServis.Shared.Entities;

/// <summary>
/// Personel maa� bilgileri
/// </summary>
public class PersonelMaas : BaseEntity, IFirmaTenant
{
    // Kural 4: FirmaId dogrudan entity'de (Sofor uzerinden join gerekmez)
    public int? FirmaId { get; set; }
    public virtual Firma? Firma { get; set; }

    public int SoforId { get; set; }
    public int Yil { get; set; }
    public int Ay { get; set; }

    // Maas Bilgileri
    public decimal BrutMaas { get; set; }
    public decimal NetMaas { get; set; }
    public decimal SGKIsciPayi { get; set; }
    public decimal SGKIsverenPayi { get; set; }
    public decimal GelirVergisi { get; set; }
    public decimal DamgaVergisi { get; set; }
    public decimal IssizlikPrimi { get; set; }

    // Ek Odemeler
    public decimal Prim { get; set; }
    public decimal Ikramiye { get; set; }
    public decimal Yemek { get; set; }
    public decimal Yol { get; set; }
    public decimal Mesai { get; set; }
    public decimal DigerEklemeler { get; set; }

    // Kesintiler
    public decimal Avans { get; set; }
    public decimal IcraTakibi { get; set; }
    public decimal DigerKesintiler { get; set; }

    // Hesaplanan
    [NotMapped]
    public decimal ToplamEklemeler => Prim + Ikramiye + Yemek + Yol + Mesai + DigerEklemeler;
    [NotMapped]
    public decimal ToplamKesintiler => SGKIsciPayi + GelirVergisi + DamgaVergisi + IssizlikPrimi + Avans + IcraTakibi + DigerKesintiler;
    [NotMapped]
    public decimal OdenecekTutar => NetMaas + ToplamEklemeler - Avans - IcraTakibi - DigerKesintiler;

    // Odeme Bilgileri
    public DateTime? OdemeTarihi { get; set; }
    public MaasOdemeDurum OdemeDurum { get; set; } = MaasOdemeDurum.Bekliyor;
    public string? OdemeAciklama { get; set; }

    // Calisma Bilgileri
    public int CalismaGunu { get; set; } = 26;
    public int IzinliGun { get; set; }
    public int RaporluGun { get; set; }
    public int DevamsizlikGun { get; set; }

    public string? Notlar { get; set; }

    // Navigation
    public virtual Sofor Sofor { get; set; } = null!;
}

/// <summary>
/// Personel izin kay�tlar�
/// </summary>
public class PersonelIzin : BaseEntity, IFirmaTenant
{
    // Kural 4: FirmaId dogrudan entity'de
    public int? FirmaId { get; set; }
    public virtual Firma? Firma { get; set; }

    public int SoforId { get; set; }
    public IzinTipi IzinTipi { get; set; }
    public DateTime BaslangicTarihi { get; set; }
    public DateTime BitisTarihi { get; set; }
    [NotMapped]
    public int ToplamGun => (BitisTarihi - BaslangicTarihi).Days + 1;

    public IzinDurum Durum { get; set; } = IzinDurum.Beklemede;
    public string? OnaylayanKisi { get; set; }
    public DateTime? OnayTarihi { get; set; }
    public string? RedNedeni { get; set; }

    public string? Aciklama { get; set; }
    public string? Notlar { get; set; }

    // Navigation
    public virtual Sofor Sofor { get; set; } = null!;
}

/// <summary>
/// Personel y�ll�k izin haklar�
/// </summary>
public class PersonelIzinHakki : BaseEntity, IFirmaTenant
{
    // Kural 4: FirmaId dogrudan entity'de
    public int? FirmaId { get; set; }
    public virtual Firma? Firma { get; set; }

    public int SoforId { get; set; }
    public int Yil { get; set; }

    public int YillikIzinHakki { get; set; } = 14;
    public int KullanilanIzin { get; set; }
    public int DevirenIzin { get; set; }
    [NotMapped]
    public int KalanIzin => YillikIzinHakki + DevirenIzin - KullanilanIzin;

    public string? Notlar { get; set; }

    // Navigation
    public virtual Sofor Sofor { get; set; } = null!;
}

public enum MaasOdemeDurum
{
    Bekliyor = 0,
    Odendi = 1,
    KismiOdendi = 2,
    IptalEdildi = 3
}

public enum IzinTipi
{
    YillikIzin = 1,
    UcretsizIzin = 2,
    RaporluIzin = 3,
    MazeretIzni = 4,
    EvlilikIzni = 5,
    DogumIzni = 6,
    OlumIzni = 7,
    IdariIzin = 8
}

public enum IzinDurum
{
    Beklemede = 0,
    Onaylandi = 1,
    Reddedildi = 2,
    IptalEdildi = 3
}
