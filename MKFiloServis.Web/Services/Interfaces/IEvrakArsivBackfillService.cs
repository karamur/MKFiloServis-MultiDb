namespace MKFiloServis.Web.Services.Interfaces;

/// <summary>
/// Mevcut evrak kayıtlarını yeni arşiv yapısına taşıyan backfill servisi.
/// </summary>
public interface IEvrakArsivBackfillService
{
    /// <summary>
    /// Dry-run: Dosya yazmaz, DB güncellemez. Sadece rapor üretir.
    /// </summary>
    Task<EvrakArsivBackfillRaporu> DryRunAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gerçek backfill işlemini çalıştırır.
    /// </summary>
    /// <param name="updateDatabase">true ise DB DosyaYolu güncellenir.</param>
    /// <param name="overwriteExisting">true ise hedef dosya varsa üzerine yazar.</param>
    /// <param name="cancellationToken">İşlemi iptal etmek için kullanılan token.</param>
    Task<EvrakArsivBackfillRaporu> ExecuteAsync(
        bool updateDatabase,
        bool overwriteExisting,
        CancellationToken cancellationToken = default);
}

public sealed class EvrakArsivBackfillRaporu
{
    public int AracToplam { get; set; }
    public int AracBasarili { get; set; }
    public int AracBasarisiz { get; set; }
    public int AracAtlandi { get; set; }

    public int PersonelToplam { get; set; }
    public int PersonelBasarili { get; set; }
    public int PersonelBasarisiz { get; set; }
    public int PersonelAtlandi { get; set; }

    public DateTime Baslangic { get; set; }
    public DateTime Bitis { get; set; }
    public bool DryRun { get; set; }

    public List<EvrakArsivBackfillSatir> Satirlar { get; set; } = new();
}

public sealed class EvrakArsivBackfillSatir
{
    public string EvrakTipi { get; set; } = ""; // "Arac" veya "Personel"
    public int EvrakId { get; set; }
    public string Sahip { get; set; } = "";
    public string EvrakNiteligi { get; set; } = "";
    public string EskiDosyaYolu { get; set; } = "";
    public string YeniSifreliPath { get; set; } = "";
    public string YeniSifresizPath { get; set; } = "";
    public bool Basarili { get; set; }
    public string? Hata { get; set; }
}



