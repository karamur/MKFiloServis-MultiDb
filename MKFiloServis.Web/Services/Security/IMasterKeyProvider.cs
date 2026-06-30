namespace MKFiloServis.Web.Services.Security;

/// <summary>
/// Uygulama icin kalici bir 32-bytelik ana anahtar (master key) saglar.
/// Anahtar diskte sifreli sekilde saklanir (Windows'ta DPAPI ile).
/// </summary>
public interface IMasterKeyProvider
{
    /// <summary>
    /// AES-256 icin kullanilan 32-bytelik ana anahtar. Yoksa olusturur ve kaydeder.
    /// </summary>
    ReadOnlyMemory<byte> GetMasterKey();
}



