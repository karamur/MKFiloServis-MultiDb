namespace KOAFiloServis.Shared.Entities;

/// <summary>
/// Personel ve Araç evrakları için ORTAK dosya tablosu.
/// Her evrak bir dosyadır. Diske yazılır, DB'ye kaydedilir.
/// Eski karmaşık sistemin yerine: tek tablo, basit mantık.
/// </summary>
public class EvrakDosya
{
    public int Id { get; set; }

    public int? PersonelId { get; set; }
    public int? AracId { get; set; }

    /// <summary>Evrak tipi: "Kimlik", "Ehliyet", "Ruhsat", "Sigorta" vs.</summary>
    public string EvrakTipi { get; set; } = string.Empty;

    /// <summary>Orijinal dosya adı (kullanıcının gördüğü).</summary>
    public string DosyaAdi { get; set; } = string.Empty;

    /// <summary>Diskteki GUID'li dosya adı.</summary>
    public string DosyaYolu { get; set; } = string.Empty;

    public DateTime YuklenmeTarihi { get; set; } = DateTime.UtcNow;

    public bool IsDeleted { get; set; } = false;
}
