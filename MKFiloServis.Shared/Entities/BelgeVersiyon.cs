namespace MKFiloServis.Shared.Entities;

/// <summary>
/// EBYS Evrak Dosyası Versiyon Geçmişi
/// Her dosya güncellemesinde önceki versiyon buraya arşivlenir
/// </summary>
public class EbysEvrakDosyaVersiyon : BaseEntity
{
    public int EvrakDosyaId { get; set; }
    public int VersiyonNo { get; set; }
    public string DosyaAdi { get; set; } = string.Empty;
    public string DosyaYolu { get; set; } = string.Empty;
    public string? DosyaTipi { get; set; }
    public long DosyaBoyutu { get; set; }
    public string? Aciklama { get; set; }
    public string? DegisiklikNotu { get; set; }
    
    // Kim oluşturdu
    public int? OlusturanKullaniciId { get; set; }
    public DateTime OlusturmaTarihi { get; set; } = DateTime.Now;
    
    // Navigation
    public virtual EbysEvrakDosya? EvrakDosya { get; set; }
    public virtual Kullanici? OlusturanKullanici { get; set; }
}

/// <summary>
/// Araç Evrak Dosyası Versiyon Geçmişi
/// Her dosya güncellemesinde önceki versiyon buraya arşivlenir
/// </summary>
public class AracEvrakDosyaVersiyon : BaseEntity
{
    public int AracEvrakDosyaId { get; set; }
    public int VersiyonNo { get; set; }
    public string DosyaAdi { get; set; } = string.Empty;
    public string DosyaYolu { get; set; } = string.Empty;
    public string? DosyaTipi { get; set; }
    public long DosyaBoyutu { get; set; }
    public string? Aciklama { get; set; }
    public string? DegisiklikNotu { get; set; }
    
    // Kim oluşturdu
    public int? OlusturanKullaniciId { get; set; }
    public DateTime OlusturmaTarihi { get; set; } = DateTime.Now;
    
    // Navigation
    public virtual AracEvrakDosya? AracEvrakDosya { get; set; }
    public virtual Kullanici? OlusturanKullanici { get; set; }
}

/// <summary>
/// Personel Özlük Evrak Versiyon Geçmişi
/// Her dosya güncellemesinde önceki versiyon buraya arşivlenir
/// </summary>
public class PersonelOzlukEvrakVersiyon : BaseEntity
{
    public int PersonelOzlukEvrakId { get; set; }
    public int VersiyonNo { get; set; }
    public string? DosyaYolu { get; set; }
    public string? DosyaAdi { get; set; }
    public string? DosyaTipi { get; set; }
    public long? DosyaBoyutu { get; set; }
    public string? Aciklama { get; set; }
    public string? DegisiklikNotu { get; set; }
    
    // Kim oluşturdu
    public int? OlusturanKullaniciId { get; set; }
    public DateTime OlusturmaTarihi { get; set; } = DateTime.Now;
    
    // Navigation
    public virtual PersonelOzlukEvrak? PersonelOzlukEvrak { get; set; }
    public virtual Kullanici? OlusturanKullanici { get; set; }
}

/// <summary>
/// Versiyon karşılaştırma için DTO
/// </summary>
public class BelgeVersiyonKarsilastirma
{
    public int EskiVersiyonNo { get; set; }
    public int YeniVersiyonNo { get; set; }
    public string EskiDosyaAdi { get; set; } = string.Empty;
    public string YeniDosyaAdi { get; set; } = string.Empty;
    public DateTime EskiTarih { get; set; }
    public DateTime YeniTarih { get; set; }
    public long BoyutFarki { get; set; }
    public string? DegisiklikNotu { get; set; }
}


