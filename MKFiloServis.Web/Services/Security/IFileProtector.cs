namespace MKFiloServis.Web.Services.Security;

/// <summary>
/// Dosya/byte icerigini simetrik olarak sifreler/cozer.
/// Amac: hassas PDF'ler ve yuklenen belgeler diskte sifreli saklansin.
/// </summary>
public interface IFileProtector
{
    /// <summary>Duz byte icerigini sifreli cikti olarak dondurur.</summary>
    byte[] Protect(ReadOnlySpan<byte> plain);

    /// <summary>Sifreli ciktiyi duz byte'a cevirir.</summary>
    byte[] Unprotect(ReadOnlySpan<byte> cipher);

    /// <summary>Bir dosyayi okuyup sifreli sekilde hedefe yazar.</summary>
    void ProtectFile(string plainPath, string cipherPath);

    /// <summary>Sifreli dosyayi okuyup duz hedefe yazar.</summary>
    void UnprotectFile(string cipherPath, string plainPath);
}



