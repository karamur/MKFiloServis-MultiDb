using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KOAFiloServis.Shared.Entities;

public class KiralikPlakaTakip : BaseEntity
{
    public int? AracId { get; set; }
    public virtual Arac? Arac { get; set; }
    public virtual ICollection<KiralikPlakaTakipFatura> FaturaDetaylari { get; set; } = new List<KiralikPlakaTakipFatura>();

    [Required, StringLength(15)]
    public string Plaka { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string IsimSoyisim { get; set; } = string.Empty;

    public DateTime BaslamaTarihi { get; set; } = DateTime.Today;
    public DateTime BitisTarihi { get; set; } = DateTime.Today.AddYears(1);

    [StringLength(50)]
    public string Durum { get; set; } = "ÖNÜ AÇIK";

    [StringLength(50)]
    public string KasaDurumu { get; set; } = "PLAKA";

    public decimal FaturaOdemesi { get; set; } = 0;
    
    [StringLength(20)]
    public string Periyot { get; set; } = "AYLIK";

    public int OdemeSayisi { get; set; } = 12;

    public decimal AylikVeyaYillikTutar { get; set; } = 0;

    public decimal EkTutar { get; set; } = 0;

    // Fatura takip alanları
    [StringLength(50)]
    public string? KesilenFaturaNo { get; set; }
    public DateTime? KesilenFaturaTarih { get; set; }
    public decimal KesilenFaturaTutar { get; set; } = 0;
    public decimal KalanFaturaTutar { get; set; } = 0;
    public int? GelenFaturaId { get; set; }

    // Ödeme takip alanları (Faz 3'te kullanılacak)
    public decimal ToplamOdeme { get; set; } = 0;
    public decimal OdenenTutar { get; set; } = 0;
    public decimal KalanOdeme => ToplamOdeme - OdenenTutar;
    public DateTime? SonOdemeTarihi { get; set; }

    [NotMapped]
    public decimal Toplam => FaturaOdemesi + EkTutar;
}

