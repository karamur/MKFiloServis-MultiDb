using Microsoft.AspNetCore.DataProtection;
using System.Security.Cryptography;

namespace MKFiloServis.Web.Services.Security;

/// <summary>
/// Dosyalari ASP.NET Core Data Protection anahtar halkasi ile korur.
/// Anahtar yonetimi uygulama tarafindan otomatik yapilir; ayri master.key gerekmez.
/// Format: MKD1 | DataProtection payload.
/// </summary>
public sealed class DataProtectionFileProtector : IFileProtector
{
    private static readonly byte[] Magic = "MKD1"u8.ToArray();
    private readonly IDataProtector _protector;

    public DataProtectionFileProtector(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("MKFiloServis.SecureFileStorage.v2");
    }

    public byte[] Protect(ReadOnlySpan<byte> plain)
    {
        var payload = _protector.Protect(plain.ToArray());
        var output = new byte[Magic.Length + payload.Length];
        Magic.CopyTo(output, 0);
        payload.CopyTo(output, Magic.Length);
        return output;
    }

    public byte[] Unprotect(ReadOnlySpan<byte> cipher)
    {
        if (cipher.Length <= Magic.Length || !IsProtectedFormat(cipher))
            throw new CryptographicException("Gecersiz Data Protection dosya formati.");

        return _protector.Unprotect(cipher[Magic.Length..].ToArray());
    }

    public void ProtectFile(string plainPath, string cipherPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plainPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(cipherPath);
        var directory = Path.GetDirectoryName(cipherPath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);
        File.WriteAllBytes(cipherPath, Protect(File.ReadAllBytes(plainPath)));
    }

    public void UnprotectFile(string cipherPath, string plainPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cipherPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(plainPath);
        var directory = Path.GetDirectoryName(plainPath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);
        File.WriteAllBytes(plainPath, Unprotect(File.ReadAllBytes(cipherPath)));
    }

    public static bool IsProtectedFormat(ReadOnlySpan<byte> content)
        => content.StartsWith(Magic);
}
