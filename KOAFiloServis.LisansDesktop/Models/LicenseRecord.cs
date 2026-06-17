namespace KOAFiloServis.LisansDesktop.Models;

/// <summary>
/// Web LicenseService.LicenseInfo ile %100 uyumlu model.
/// </summary>
public class LicenseRecord
{
    public int Id { get; set; }
    public string FirmaKodu { get; set; } = string.Empty;
    public string FirmaAdi { get; set; } = string.Empty;
    public string MachineId { get; set; } = string.Empty;
    public DateTime ExpireDate { get; set; }
    public string AllowedVersion { get; set; } = "1.0.99";
    public bool IsDemo { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string Signature { get; set; } = string.Empty;
    public string LisansTipi { get; set; } = "Standard";
    public int MaxKullanici { get; set; } = 10;
    public string? YetkiliKisi { get; set; }
    public string? Email { get; set; }
    public string? Notlar { get; set; }
    public string KayitTarihi { get; set; } = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm");

    public int KalanGun => Math.Max(0, (ExpireDate.Date - DateTime.UtcNow.Date).Days);
    public bool SuresiDoldu => DateTime.UtcNow > ExpireDate;
}
