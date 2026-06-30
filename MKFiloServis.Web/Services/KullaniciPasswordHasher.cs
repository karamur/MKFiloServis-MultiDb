using System.Security.Cryptography;
using System.Text;
using MKFiloServis.Shared.Entities;
using Microsoft.AspNetCore.Identity;

namespace MKFiloServis.Web.Services;

public class KullaniciPasswordHasher : IPasswordHasher<Kullanici>
{
    private readonly PasswordHasher<Kullanici> _innerHasher = new();

    public string HashPassword(Kullanici user, string password)
    {
        return _innerHasher.HashPassword(user, password);
    }

    public PasswordVerificationResult VerifyHashedPassword(Kullanici user, string hashedPassword, string providedPassword)
    {
        if (string.IsNullOrWhiteSpace(hashedPassword))
            return PasswordVerificationResult.Failed;

        if (hashedPassword.StartsWith("AQAAAA", StringComparison.Ordinal))
            return _innerHasher.VerifyHashedPassword(user, hashedPassword, providedPassword);

        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(providedPassword + "KOAFiloServisSalt"));
        var legacyHash = Convert.ToBase64String(bytes);

        return legacyHash == hashedPassword
            ? PasswordVerificationResult.SuccessRehashNeeded
            : PasswordVerificationResult.Failed;
    }
}


