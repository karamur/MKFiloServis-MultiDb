using System.ComponentModel.DataAnnotations.Schema;

namespace KOAFiloServis.Shared.Entities;

/// <summary>
/// Fatura kalemleri
/// </summary>
public class FaturaKalem : BaseEntity, IFirmaTenant
{
    // Kural 4: FirmaId dogrudan entity'de (Fatura uzerinden join gerekmez)
    public int? FirmaId { get; set; }
    public virtual Firma? Firma { get; set; }

    public int SiraNo { get; set; }
    public string? UrunKodu { get; set; } // Urun/Hizmet kodu
    public string Aciklama { get; set; } = string.Empty;
    public decimal Miktar { get; set; } = 1;
    public string Birim { get; set; } = "Adet";
    public decimal BirimFiyat { get; set; }
    public decimal IskontoOrani { get; set; } = 0;
    public decimal IskontoTutar { get; set; } = 0;
    public decimal KdvOrani { get; set; } = 20;
    public decimal KdvTutar { get; set; }
    public decimal ToplamTutar { get; set; }

    // Kalem Tipi - Hizmet, Mal, Demirbas vb.
    public FaturaKalemTipi KalemTipi { get; set; } = FaturaKalemTipi.Hizmet;
    public FaturaKalemAltTipi? AltTipi { get; set; }

    // Tevkifat (kalem bazinda)
    public decimal TevkifatOrani { get; set; } = 0;
    public decimal TevkifatTutar { get; set; } = 0;

    // Muhasebe Hesap Eslestirme (Gelir/Gider hesabi)
    public int? MuhasebeHesapId { get; set; }
    public virtual MuhasebeHesap? MuhasebeHesap { get; set; }

    // Arac Iliskisi (Arac satis/alis veya servis icin)
    public int? AracId { get; set; }
    public virtual Arac? Arac { get; set; }

    // Demirbas Iliskisi
    public int? DemirbasId { get; set; }

    // Foreign Key
    public int FaturaId { get; set; }

    // Navigation Property
    public virtual Fatura Fatura { get; set; } = null!;

    // Hesaplanan değerler
    [NotMapped]
    public decimal NetTutar => (Miktar * BirimFiyat) - IskontoTutar;
    [NotMapped]
    public decimal TevkifatliKdvTutar => KdvTutar - TevkifatTutar;
}

/// <summary>
/// Fatura kalemi ana tipi
/// </summary>
public enum FaturaKalemTipi
{
    Hizmet = 1,         // Hizmet satışı/alışı
    Mal = 2,            // Mal satışı/alışı (Ticari mal)
    Demirbas = 3,       // Demirbaş satışı/alışı
    Arac = 4,           // Araç satışı/alışı
    Servis = 5,         // Servis hizmeti
    Masraf = 6,         // Araç masrafları (yansıtma için)
    Diger = 99
}

/// <summary>
/// Fatura kalemi alt tipi (detay)
/// </summary>
public enum FaturaKalemAltTipi
{
    // Hizmet Alt Tipleri
    TasimaHizmeti = 101,
    KiralamaHizmeti = 102,
    DanismanlikHizmeti = 103,
    
    // Mal Alt Tipleri
    TicariMal = 201,
    YedekParca = 202,
    SarfMalzeme = 203,
    
    // Demirbaş Alt Tipleri
    AracDemirbas = 301,
    OfisEkipmani = 302,
    MakinaTechizat = 303,
    DigerDemirbas = 399,
    
    // Araç Alt Tipleri
    AracSatis = 401,
    AracAlis = 402,
    
    // Servis Alt Tipleri
    BakimOnarim = 501,
    Kasko = 502,
    Sigorta = 503,
    Muayene = 504,
    Lastik = 505,
    Yakit = 506,

    // Masraf Alt Tipleri (Yansıtma için)
    YansitmaBedeli = 601,       // Araca ait masrafın müşteriye yansıtılması
    AracKiraBedeli = 602,       // Araç kira bedeli
    YanitimYakit = 603,         // Yakıt yansıtma
    YansitimServis = 604,       // Servis yansıtma
    YansitimSigorta = 605,      // Sigorta yansıtma
    YansitimDiger = 699,        // Diğer yansıtma

    Diger = 999
}
