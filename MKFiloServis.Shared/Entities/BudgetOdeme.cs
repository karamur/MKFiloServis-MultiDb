using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MKFiloServis.Shared.Entities;

/// <summary>
/// Bütçe Ödeme Kaydı
/// </summary>
public class BudgetOdeme : BaseEntity
{
    [Required]
    public DateTime OdemeTarihi { get; set; }

    [Required]
    public int OdemeAy { get; set; } // 1-12

    [Required]
    public int OdemeYil { get; set; }

    [Required]
    public string MasrafKalemi { get; set; } = string.Empty;

    public string? Aciklama { get; set; }

    [Required]
    public decimal Miktar { get; set; }

    // Firma bilgisi
    public int? FirmaId { get; set; }
    public virtual Firma? Firma { get; set; }

    // Taksit bilgileri
    public bool TaksitliMi { get; set; } = false;
    public int ToplamTaksitSayisi { get; set; } = 1;
    public int KacinciTaksit { get; set; } = 1;
    public Guid? TaksitGrupId { get; set; } // Aynı taksit grubundaki ödemeleri bağlar

    public DateTime? TaksitBaslangicAy { get; set; }
    public DateTime? TaksitBitisAy { get; set; }

    public OdemeDurum Durum { get; set; } = OdemeDurum.Bekliyor;

    public string? Notlar { get; set; }

    // Ödeme bilgileri - Kasa/Banka hareketi
    public DateTime? GercekOdemeTarihi { get; set; }
    public int? OdemeYapildigiHesapId { get; set; } // BankaHesap ID
    public decimal? OdenenTutar { get; set; }
    public string? OdemeNotu { get; set; }
    public int? BankaKasaHareketId { get; set; } // İlişkili hareket

    // Kesinti bilgileri (masraf, ceza, komisyon vb.)
    public decimal MasrafKesintisi { get; set; } = 0;
    public decimal CezaKesintisi { get; set; } = 0;
    public decimal DigerKesinti { get; set; } = 0;
    public string? KesintiAciklamasi { get; set; }

    // Fatura ile eşleştirme
    public int? FaturaId { get; set; }
    public bool FaturaIleKapatildi { get; set; } = false;

    // Kısmi ödeme bilgileri
    public bool KismiOdemeMi { get; set; } = false;
    public decimal ToplamKismiOdenen { get; set; } = 0; // Şimdiye kadar ödenen toplam
    public bool KalanSonrakiDonemeAktarilsin { get; set; } = false; // Kalan tutar sonraki döneme aktarılsın mı
    public int? SonrakiDonemOdemeId { get; set; } // Sonraki dönemde oluşturulan ödeme ID'si
    public int? OncekiDonemOdemeId { get; set; } // Bu ödeme bir önceki dönemden aktarıldıysa, ana ödeme ID'si

    // Navigation
    public virtual BankaHesap? OdemeYapildigiHesap { get; set; }
    public virtual Fatura? Fatura { get; set; }
    public virtual BudgetOdeme? SonrakiDonemOdeme { get; set; }
    public virtual BudgetOdeme? OncekiDonemOdeme { get; set; }

    [NotMapped]
    public string? HareketKaynakGorunumu { get; set; }

    [NotMapped]
    public string? HareketCariUnvani { get; set; }

    [NotMapped]
    public string? HareketYonGorunumu { get; set; }

    [NotMapped]
    public string? HareketBelgeNo { get; set; }

    // Hesaplanan alanlar
    public int KalanTaksitSayisi => ToplamTaksitSayisi - KacinciTaksit;
    public decimal ToplamTaksitTutari => Miktar * ToplamTaksitSayisi;
    public bool OdenmisVeyaKapatilmis => Durum == OdemeDurum.Odendi || FaturaIleKapatildi;
    public decimal ToplamEkMasraf => MasrafKesintisi + CezaKesintisi + DigerKesinti;
    public decimal NetOdenenTutar => (OdenenTutar ?? Miktar) + ToplamEkMasraf;

    // Kısmi ödeme hesaplanan alanlar
    [NotMapped]
    public decimal KalanTutar => Miktar - ToplamKismiOdenen;
    [NotMapped]
    public bool TamamenOdendi => KalanTutar <= 0;
    [NotMapped]
    public decimal OdemeYuzdesi => Miktar > 0 ? Math.Round(ToplamKismiOdenen / Miktar * 100, 1) : 0;
}

public enum OdemeDurum
{
    Bekliyor = 1,
    Odendi = 2,
    Iptal = 3,
    Ertelendi = 4,
    KismiOdendi = 5 // Yeni: Kısmi ödeme yapıldı
}

/// <summary>
/// Bütçe Masraf Kalemleri
/// </summary>
public class BudgetMasrafKalemi : BaseEntity
{
    [Required]
    public string KalemAdi { get; set; } = string.Empty;

    public string? Kategori { get; set; }

    public string? Renk { get; set; } = "#007bff"; // Grafik rengi

    public string? Icon { get; set; } = "bi-cash";

    public bool Aktif { get; set; } = true;

    public int SiraNo { get; set; }
}

/// <summary>
/// Bütçe Hedef Kaydı - Kategori bazlı hedef tutarlar
/// </summary>
public class BudgetHedef : BaseEntity
{
    [Required]
    public int Yil { get; set; }

    [Required]
    public int Ay { get; set; } // 1-12, 0 = Yıllık hedef

    [Required]
    public string MasrafKalemi { get; set; } = string.Empty;

    [Required]
    public decimal HedefTutar { get; set; }

    public string? Aciklama { get; set; }

    // Firma bazlı hedef (opsiyonel)
    public int? FirmaId { get; set; }
    public virtual Firma? Firma { get; set; }
}

/// <summary>
/// Hedef vs Gerçekleşen Karşılaştırma DTO
/// </summary>
public class BudgetHedefGerceklesen
{
    public string MasrafKalemi { get; set; } = string.Empty;
    public int Ay { get; set; }
    public int Yil { get; set; }
    public decimal HedefTutar { get; set; }
    public decimal GerceklesenTutar { get; set; }
    public decimal Fark => GerceklesenTutar - HedefTutar;
    public decimal FarkYuzdesi => HedefTutar > 0 ? (Fark / HedefTutar) * 100 : 0;
    public string Durum => GerceklesenTutar <= HedefTutar ? "Basarili" : "Asim";
    public string? Renk { get; set; }
}

/// <summary>
/// Yıllık Hedef vs Gerçekleşen Özet
/// </summary>
public class BudgetYillikHedefOzet
{
    public int Yil { get; set; }
    public decimal ToplamHedef { get; set; }
    public decimal ToplamGerceklesen { get; set; }
    public decimal ToplamFark => ToplamGerceklesen - ToplamHedef;
    public decimal BasariOrani => ToplamHedef > 0 ? ((ToplamHedef - Math.Max(0, ToplamFark)) / ToplamHedef) * 100 : 100;
    public List<BudgetHedefGerceklesen> KategoriDetaylari { get; set; } = new();
    public List<BudgetAylikHedefOzet> AylikDetaylar { get; set; } = new();
}

/// <summary>
/// Aylık Hedef vs Gerçekleşen Özet
/// </summary>
public class BudgetAylikHedefOzet
{
    public int Ay { get; set; }
    public string AyAdi { get; set; } = string.Empty;
    public decimal HedefTutar { get; set; }
    public decimal GerceklesenTutar { get; set; }
    public decimal Fark => GerceklesenTutar - HedefTutar;
    public decimal BasariOrani => HedefTutar > 0 ? Math.Min(100, (GerceklesenTutar / HedefTutar) * 100) : 0;
}


