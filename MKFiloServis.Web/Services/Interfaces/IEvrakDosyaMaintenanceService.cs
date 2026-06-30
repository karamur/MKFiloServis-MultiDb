namespace MKFiloServis.Web.Services.Interfaces;

/// <summary>
/// Kayıp/kırık EvrakDosya kayıtlarını tespit ve temizlik servisi.
/// Eski bozuk hızlı upload sisteminden kalma, fiziksel dosyası diskte olmayan kayıtları yönetir.
/// </summary>
public interface IEvrakDosyaMaintenanceService
{
    /// <summary>
    /// Rapor modu: hiçbir DB değişikliği yapmaz, sadece durumu analiz eder.
    /// </summary>
    Task<EvrakDosyaMaintenanceReport> AnalyzeAsync(CancellationToken ct = default);

    /// <summary>
    /// Temizlik modu: kayıp EvrakDosya kayıtlarını soft-delete yapar,
    /// ilgili PersonelOzlukEvrak kayıtlarını temizler.
    /// </summary>
    Task<EvrakDosyaMaintenanceReport> CleanupAsync(EvrakDosyaMaintenanceReport? previewReport = null, CancellationToken ct = default);
}

public sealed record EvrakDosyaMaintenanceReport
{
    public DateTime OlusturmaTarihi { get; init; } = DateTime.Now;
    public int ToplamEvrakDosya { get; init; }
    public int KayipSayisi { get; init; }
    public int SaglamSayisi { get; init; }
    public int TemizlenenEvrakDosya { get; init; }
    public int TemizlenenOzlukEvrak { get; init; }
    public List<EvrakDosyaKayipOgesi> Kayiplar { get; init; } = new();
    public List<EvrakDosyaKayipOgesi> Saglamlar { get; init; } = new();
    public bool DryRun { get; init; } = true;
    public string? HataMesaji { get; init; }
}

public sealed class EvrakDosyaKayipOgesi
{
    public int EvrakDosyaId { get; init; }
    public string EvrakTipi { get; init; } = string.Empty;
    public string DosyaAdi { get; init; } = string.Empty;
    public string DosyaYolu { get; init; } = string.Empty;
    public int? PersonelId { get; init; }
    public string? PersonelAdi { get; init; }
    public string? CanonicalTip { get; init; }
    public string? TamYol { get; init; }
    public bool DosyaVar { get; init; }
    public int? EslesenOzlukEvrakId { get; init; }
    public string? OzlukEvrakAdi { get; init; }
    public bool OzlukEvrakTamamlandi { get; init; }
}




