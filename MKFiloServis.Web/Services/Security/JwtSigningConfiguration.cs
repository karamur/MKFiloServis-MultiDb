using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using MKFiloServis.Web.Services;

namespace MKFiloServis.Web.Services.Security;

/// <summary>
/// One immutable signing configuration per process. Issuance and validation must use
/// the same key until the process is restarted as part of a coordinated rotation.
/// </summary>
public sealed class JwtSigningConfiguration
{
    private readonly byte[] _secretBytes;

    public string Issuer { get; }
    public string Audience { get; }
    public SymmetricSecurityKey SigningKey { get; }
    public SigningCredentials SigningCredentials { get; }

    public JwtSigningConfiguration(string secret, string issuer, string audience)
    {
        _secretBytes = Encoding.UTF8.GetBytes(JwtSecretPolicy.EnsureValid(secret));
        Issuer = issuer;
        Audience = audience;
        SigningKey = new SymmetricSecurityKey(_secretBytes);
        SigningCredentials = new SigningCredentials(SigningKey, SecurityAlgorithms.HmacSha256);
    }

    public string CreateAuthStamp(string? passwordHash)
        => Base64UrlEncoder.Encode(HMACSHA256.HashData(
            _secretBytes, Encoding.UTF8.GetBytes(passwordHash ?? string.Empty)));
}
