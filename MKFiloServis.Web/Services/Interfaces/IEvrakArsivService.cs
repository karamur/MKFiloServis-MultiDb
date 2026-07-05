namespace MKFiloServis.Web.Services.Interfaces;

/// <summary>
/// Evrak arşivleme servisi: Yüklenen evrakların şifreli ve şifresiz
/// kopyalarını <c>Arsiv\Sifreli</c> ve <c>Arsiv\Sifresiz</c> dizinlerinde saklar.
/// </summary>
public interface IEvrakArsivService
{
    /// <summary>
    /// Personel evrakını tekil arşiv düzenine kaydeder.
    /// Şifreli kopya:  Arsiv\Sifreli\Personeller\{AD SOYAD - FIRMA}\{EVRAK_NITELIGI}.ext.enc
    /// Şifresiz kopya: Arsiv\Sifresiz\Personeller\{AD SOYAD - FIRMA}\{EVRAK_NITELIGI}.ext
    /// Dönüş: şifreli relative path (DB DosyaYolu alanına yazılır).
    /// </summary>
    Task<string> ArsivlePersonelEvrakAsync(
        string adSoyad,
        string firmaAdi,
        string evrakNiteligi,
        byte[] icerik,
        string uzanti,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Araç evrakını tekil arşiv düzenine kaydeder.
    /// Şifreli kopya:  Arsiv\Sifreli\Araclar\{PLAKA - FIRMA}\{EVRAK_NITELIGI}.ext.enc
    /// Şifresiz kopya: Arsiv\Sifresiz\Araclar\{PLAKA - FIRMA}\{EVRAK_NITELIGI}.ext
    /// Dönüş: şifreli relative path (DB DosyaYolu alanına yazılır).
    /// </summary>
    Task<string> ArsivleAracEvrakAsync(
        string plaka,
        string firmaAdi,
        string evrakNiteligi,
        byte[] icerik,
        string uzanti,
        CancellationToken cancellationToken = default);
}



