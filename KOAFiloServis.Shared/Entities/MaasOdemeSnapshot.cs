using KOAFiloServis.Shared.Entities;

namespace KOAFiloServis.Shared.Entities;

/// <summary>
/// Aylık maaş/ödeme snapshot kaydı.
/// Oluşturulduktan sonra değişmez (immutable). Geçmiş aylar finansal doğruluk için korunur.
/// </summary>
public class MaasOdemeSnapshot : BaseEntity, IFirmaTenant
{
    /// <summary>Tenant izolasyonu (Kural 6)</summary>
    public int? FirmaId { get; set; }

    public int Yil { get; set; }
    public int Ay { get; set; }

    public int PersonelId { get; set; }
    public string PersonelAdSoyad { get; set; } = string.Empty;
    public string? PersonelKodu { get; set; }
    public string? GorevAdi { get; set; }
    public string? AracPlakasi { get; set; }

    public decimal GercekMaas { get; set; }
    public decimal BankayaYatan { get; set; }
    public decimal Avans { get; set; }
    public decimal Kesinti { get; set; }
    public decimal Harcama { get; set; }
    public decimal Odenecek { get; set; }

    public DateTime HesaplamaTarihi { get; set; } = DateTime.UtcNow;

    /// <summary>Kilitli snapshotslar düzenlenemez.</summary>
    public bool Kilitli { get; set; } = true;

    /// <summary>Normal muhasebe fişi ID'si. Null ise fiş oluşturulmamış.</summary>
    public int? MuhasebeFisId { get; set; }

    /// <summary>İptal/ters fiş ID'si. Null ise iptal edilmemiş.</summary>
    public int? IptalFisId { get; set; }
}
