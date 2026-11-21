using System.Security.Cryptography;
using System.Text;
using MKFiloServis.Shared.Entities;

namespace MKFiloServis.Shared.Licensing;

/// <summary>RSA-PSS imza doğrulaması. Anahtar rotasyonu için birden fazla genel anahtar denenir.</summary>
public static class LicenseSignatureVerifier
{
    public static string Payload(LicenseInfo lic)
        => LicenseSignaturePayload.Create(lic.FirmaKodu, lic.MachineId, lic.ExpireDate, lic.DurationDays,
            lic.IsDemo, lic.AllowedVersion, lic.CreatedAt, lic.ContactPhone);

    /// <summary>
    /// v3 (modüllü) ve v2 (modülsüz, eski) RSA imzalarını doğrular.
    /// Demo ve süreli eski imzalar bu sınıfın kapsamı dışındadır.
    /// </summary>
    public static bool VerifyRsa(LicenseInfo lic, IEnumerable<string> publicKeys)
    {
        var signature = lic.Signature ?? string.Empty;
        if (lic.IsDemo) return false;

        byte[] data;
        byte[] signed;
        try
        {
            if (LicenseModules.TryRead(signature, out var modules, out var encoded))
            {
                data = Encoding.UTF8.GetBytes(LicenseModules.Payload(Payload(lic), modules));
                signed = Convert.FromBase64String(encoded);
            }
            else if (signature.StartsWith("v2:", StringComparison.Ordinal))
            {
                data = Encoding.UTF8.GetBytes(Payload(lic));
                signed = Convert.FromBase64String(signature[3..]);
            }
            else
            {
                return false;
            }
        }
        catch (Exception ex) when (ex is FormatException or ArgumentException)
        {
            return false;
        }

        foreach (var pem in publicKeys)
        {
            try
            {
                using var rsa = RSA.Create();
                rsa.ImportFromPem(pem);
                if (rsa.VerifyData(data, signed, HashAlgorithmName.SHA256, RSASignaturePadding.Pss))
                    return true;
            }
            catch (Exception ex) when (ex is CryptographicException or ArgumentException or FormatException)
            {
                // Bu anahtar okunamadı; sıradaki anahtarı dene.
            }
        }

        return false;
    }

    /// <summary>İmzalı zarftan modül listesini okur. v2 ve demo için null döner.</summary>
    public static IReadOnlySet<string>? ReadModules(LicenseInfo lic)
    {
        if (lic.IsDemo || !LicenseModules.TryRead(lic.Signature, out var modules, out _)) return null;
        return new HashSet<string>(modules.Split(','), StringComparer.Ordinal);
    }
}
