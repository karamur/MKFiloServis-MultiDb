using System.Security.Cryptography;
using System.Text;
using MKFiloServis.Shared.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace MKFiloServis.Web.Services;

public class KullaniciPasswordHasher : IPasswordHasher<Kullanici>
{
    private readonly PasswordHasher<Kullanici> _innerHasher;

    public KullaniciPasswordHasher(IOptions<PasswordHasherOptions> options)
    {
        _innerHasher = new PasswordHasher<Kullanici>(options);
    }

    public string HashPassword(Kullanici user, string password)
    {
        return _innerHasher.HashPassword(user, password);
    }

    public PasswordVerificationResult VerifyHashedPassword(Kullanici user, string hashedPassword, string providedPassword)
    {
        if (string.IsNullOrWhiteSpace(hashedPassword))
            return PasswordVerificationResult.Failed;

        byte[] storedBytes;
        try
        {
            storedBytes = Convert.FromBase64String(hashedPassword);
        }
        catch (FormatException)
        {
            return PasswordVerificationResult.Failed;
        }

        // ASP.NET Identity V2/V3 hashes start with a format marker byte (0/1).
        // Other correctly encoded values are the application's legacy raw SHA-256 hashes.
        if (storedBytes.Length > 32 && storedBytes[0] is 0x00 or 0x01)
            return _innerHasher.VerifyHashedPassword(user, hashedPassword, providedPassword);

        var expectedBytes = SHA256.HashData(Encoding.UTF8.GetBytes(providedPassword + "KOAFiloServisSalt"));
        if (storedBytes.Length != expectedBytes.Length ||
            !CryptographicOperations.FixedTimeEquals(storedBytes, expectedBytes))
            return PasswordVerificationResult.Failed;

        return PasswordVerificationResult.SuccessRehashNeeded;
    }
}


