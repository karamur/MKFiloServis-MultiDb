using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using MKFiloServis.Web.Services;
using MKFiloServis.Web.Services.Interfaces;
using MKFiloServis.Web.Services.Security;
using MKFiloServis.Shared.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace MKFiloServis.Web.Controllers;

/// <summary>
/// API Authentication Controller - JWT Token oluşturma ve doğrulama
/// </summary>
[ApiController]
[Microsoft.AspNetCore.Authorization.Authorize(AuthenticationSchemes = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IKullaniciService _kullaniciService;
    private readonly JwtSigningConfiguration _jwtSigning;

    public AuthController(
        IKullaniciService kullaniciService,
        JwtSigningConfiguration jwtSigning)
    {
        _kullaniciService = kullaniciService;
        _jwtSigning = jwtSigning;
    }

    /// <summary>
    /// Kullanıcı adı ve şifre ile JWT token alır
    /// </summary>
    /// <param name="request">Giriş bilgileri</param>
    /// <returns>JWT token ve kullanıcı bilgileri</returns>
    [HttpPost("login")]
    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrEmpty(request.KullaniciAdi) || string.IsNullOrEmpty(request.Sifre))
        {
            return BadRequest(new { Error = "Kullanıcı adı ve şifre gereklidir" });
        }

        var sonuc = await _kullaniciService.GirisYapAsync(request.KullaniciAdi, request.Sifre);
        if (!sonuc.Basarili || sonuc.Kullanici == null)
        {
            return Unauthorized(new { Error = "Kullanıcı adı veya şifre geçersiz ya da hesap kullanılamıyor." });
        }

        var kullanici = sonuc.Kullanici;
        if (!kullanici.Aktif)
        {
            return Unauthorized(new { Error = "Kullanıcı adı veya şifre geçersiz ya da hesap kullanılamıyor." });
        }

        var sessionStartedAt = DateTimeOffset.UtcNow;
        var token = GenerateJwtToken(kullanici, sessionStartedAt);

        return Ok(new LoginResponse
        {
            Token = token,
            KullaniciId = kullanici.Id,
            KullaniciAdi = kullanici.KullaniciAdi,
            AdSoyad = kullanici.AdSoyad,
            Rol = kullanici.Rol?.RolAdi ?? "Kullanici",
            ExpiresAt = sessionStartedAt.Add(AuthenticationSessionPolicy.MaximumLifetime).UtcDateTime
        });
    }

    /// <summary>
    /// Mevcut token'ı yenileyerek yeni bir token alır
    /// </summary>
    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken()
    {
        var kullaniciIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (kullaniciIdClaim == null || !int.TryParse(kullaniciIdClaim.Value, out var kullaniciId))
        {
            return Unauthorized(new { Error = "Geçersiz token" });
        }

        var startedValue = User.FindFirst("auth_started")?.Value;
        if (!long.TryParse(startedValue, out var startedUnixSeconds))
            return Unauthorized(new { Error = "Oturum süresi sona erdi. Yeniden giriş yapın." });
        var sessionStartedAt = DateTimeOffset.FromUnixTimeSeconds(startedUnixSeconds);
        if (!AuthenticationSessionPolicy.IsWithinLifetime(sessionStartedAt, DateTimeOffset.UtcNow))
            return Unauthorized(new { Error = "Oturum süresi sona erdi. Yeniden giriş yapın." });
        var kullanici = await _kullaniciService.GetByIdAsync(kullaniciId);
        var kilitli = kullanici?.Kilitli == true &&
            (kullanici.KilitlenmeBitisUtc is null || kullanici.KilitlenmeBitisUtc > DateTime.UtcNow);
        if (kullanici is not { Aktif: true, IsDeleted: false } || kilitli)
        {
            return Unauthorized(new { Error = "Oturum artık geçerli değil. Yeniden giriş yapın." });
        }

        var token = GenerateJwtToken(kullanici, sessionStartedAt);

        return Ok(new LoginResponse
        {
            Token = token,
            KullaniciId = kullanici.Id,
            KullaniciAdi = kullanici.KullaniciAdi,
            AdSoyad = kullanici.AdSoyad,
            Rol = kullanici.Rol?.RolAdi ?? "Kullanici",
            ExpiresAt = sessionStartedAt.Add(AuthenticationSessionPolicy.MaximumLifetime).UtcDateTime
        });
    }

    /// <summary>
    /// Token doğrulama - Token geçerli mi kontrol eder
    /// </summary>
    [HttpGet("verify")]
    public IActionResult VerifyToken()
    {
        if (!User.Identity?.IsAuthenticated ?? true)
        {
            return Unauthorized(new { Valid = false, Error = "Token geçersiz veya süresi dolmuş" });
        }

        return Ok(new
        {
            Valid = true,
            KullaniciId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
            KullaniciAdi = User.FindFirst(ClaimTypes.Name)?.Value,
            Rol = User.FindFirst(ClaimTypes.Role)?.Value
        });
    }

    private string GenerateJwtToken(Kullanici kullanici, DateTimeOffset sessionStartedAt)
    {
        var rolAdi = kullanici.Rol?.RolAdi ?? "Kullanici";

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, kullanici.Id.ToString()),
            new Claim(ClaimTypes.Name, kullanici.KullaniciAdi),
            new Claim(ClaimTypes.Role, rolAdi),
            new Claim("AdSoyad", kullanici.AdSoyad ?? ""),
            new Claim("Email", kullanici.Email ?? ""),
            new Claim("auth_stamp", _jwtSigning.CreateAuthStamp(kullanici.SifreHash)),
            new Claim("auth_started", sessionStartedAt.ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture)),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _jwtSigning.Issuer,
            audience: _jwtSigning.Audience,
            claims: claims,
            expires: sessionStartedAt.Add(AuthenticationSessionPolicy.MaximumLifetime).UtcDateTime,
            signingCredentials: _jwtSigning.SigningCredentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

/// <summary>
/// Giriş isteği modeli
/// </summary>
public class LoginRequest
{
    public string KullaniciAdi { get; set; } = "";
    public string Sifre { get; set; } = "";
}

/// <summary>
/// Giriş yanıt modeli
/// </summary>
public class LoginResponse
{
    public string Token { get; set; } = "";
    public int KullaniciId { get; set; }
    public string KullaniciAdi { get; set; } = "";
    public string? AdSoyad { get; set; }
    public string Rol { get; set; } = "";
    public DateTime ExpiresAt { get; set; }
}



