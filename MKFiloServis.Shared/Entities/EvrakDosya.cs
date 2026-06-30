namespace MKFiloServis.Shared.Entities;

/// <summary>
/// TEK GERÇEK KAYNAK — Personel ve Araç evrakları için ORTAK dosya tablosu.
/// Tüm checklist, UI ve kontroller SADECE bu tablodan okunur.
/// Eski sistemler (PersonelOzlukEvrak, AracEvrak vb.) KULLANILMAZ.
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

    /// <summary>İlk yükleme tarihi.</summary>
    public DateTime YuklenmeTarihi { get; set; } = DateTime.UtcNow;

    /// <summary>Son güncelleme tarihi (aynı evrak tekrar yüklenince).</summary>
    public DateTime? GuncellenmeTarihi { get; set; }

    /// <summary>Evrak geçerlilik bitiş tarihi. Boşsa süresiz.</summary>
    public DateTime? GecerlilikTarihi { get; set; }

    public bool IsDeleted { get; set; } = false;
}


