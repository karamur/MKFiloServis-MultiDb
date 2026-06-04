namespace KOAFiloServis.Shared.Entities;

/// <summary>
/// Proforma Fatura - Kesilmeden önce müşteriye gönderilen ön fatura
/// </summary>
public class ProformaFatura : BaseEntity, IFirmaTenant
{
    public string ProformaNo { get; set; } = string.Empty;
    public DateTime ProformaTarihi { get; set; } = DateTime.Now;
    public DateTime GecerlilikTarihi { get; set; } // Teklifin geçerlilik süresi
    
    public ProformaDurum Durum { get; set; } = ProformaDurum.Taslak;
    
    // Cari Bilgisi
    public int CariId { get; set; }
    public virtual Cari Cari { get; set; } = null!;
    
    // Firma Bilgisi
    public int? FirmaId { get; set; }
    public virtual Firma? Firma { get; set; }
    
    // Tutarlar
    public decimal AraToplam { get; set; }
    public decimal IskontoTutar { get; set; } = 0;
    public decimal IskontoOrani { get; set; } = 0;
    public decimal KdvOrani { get; set; } = 20;
    public decimal KdvTutar { get; set; }
    public decimal GenelToplam { get; set; }
    
    // Ödeme Koşulları
    public string? OdemeKosulu { get; set; } // Ör: "30 gün vadeli", "Peşin"
    public string? TeslimKosulu { get; set; } // Ör: "Fabrika teslim", "Kapıda teslim"
    public int? VadeGun { get; set; }
    
    // İletişim
    public string? IlgiliKisi { get; set; }
    public string? Telefon { get; set; }
    public string? Email { get; set; }
    
    // Notlar
    public string? Aciklama { get; set; }
    public string? OzelNotlar { get; set; } // Dahili notlar (müşteriye gösterilmez)
    
    // PDF
    public string? PdfDosyaYolu { get; set; }
    
    // Faturaya Dönüştürme
    public bool FaturayaDonusturuldu { get; set; } = false;
    public int? FaturaId { get; set; }
    public virtual Fatura? Fatura { get; set; }
    public DateTime? FaturaDonusumTarihi { get; set; }
    
    // Navigation
    public virtual ICollection<ProformaFaturaKalem> Kalemler { get; set; } = new List<ProformaFaturaKalem>();
}

/// <summary>
/// Proforma Fatura Kalemi
/// </summary>
public class ProformaFaturaKalem : BaseEntity, IFirmaTenant
{
    public int? FirmaId { get; set; }
    public virtual Firma? Firma { get; set; }

    public int ProformaFaturaId { get; set; }
    public virtual ProformaFatura ProformaFatura { get; set; } = null!;
    
    // Stok Kartı (varsa)
    public int? StokKartiId { get; set; }
    public virtual StokKarti? StokKarti { get; set; }
    
    // Kalem Bilgileri
    public int SiraNo { get; set; }
    public string UrunAdi { get; set; } = string.Empty;
    public string? UrunKodu { get; set; }
    public string? Aciklama { get; set; }
    
    public decimal Miktar { get; set; } = 1;
    public string Birim { get; set; } = "Adet";
    public decimal BirimFiyat { get; set; }
    
    // İskonto
    public decimal IskontoOrani { get; set; } = 0;
    public decimal IskontoTutar { get; set; } = 0;
    
    // KDV
    public decimal KdvOrani { get; set; } = 20;
    public decimal KdvTutar { get; set; }
    
    // Toplam
    public decimal AraToplam { get; set; } // Miktar * BirimFiyat
    public decimal NetTutar { get; set; } // AraToplam - İskonto
    public decimal ToplamTutar { get; set; } // NetTutar + KDV
}

public enum ProformaDurum
{
    Taslak = 1,
    Gonderildi = 2,
    Onaylandi = 3,
    Reddedildi = 4,
    FaturayaDonusturuldu = 5,
    SuresiDoldu = 6
}
