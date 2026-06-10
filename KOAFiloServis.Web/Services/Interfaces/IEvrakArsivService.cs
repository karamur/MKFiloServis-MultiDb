namespace KOAFiloServis.Web.Services;

/// <summary>
/// Evrak arşivleme servisi: Yüklenen evrakların şifreli ve şifresiz
/// kopyalarını <c>Arsiv\Sifreli</c> ve <c>Arsiv\Sifresiz</c> dizinlerinde saklar.
/// </summary>
public interface IEvrakArsivService
{
    /// <summary>
    /// Personel evrakını arşivler. Klasör/dosya adı:
    /// <c>{AD-SOYAD}-{EVRAK_NITELIGI}-{yyyyMMdd-HHmmss}</c>
    /// </summary>
    Task ArsivlePersonelEvrakAsync(
        string adSoyad,
        string evrakNiteligi,
        byte[] icerik,
        string uzanti,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Araç evrakını arşivler. Klasör/dosya adı:
    /// <c>{PLAKA}-{SASI_NO}-{EVRAK_NITELIGI}-{yyyyMMdd-HHmmss}</c>
    /// </summary>
    Task ArsivleAracEvrakAsync(
        string plaka,
        string sasiNo,
        string evrakNiteligi,
        byte[] icerik,
        string uzanti,
        CancellationToken cancellationToken = default);
}
