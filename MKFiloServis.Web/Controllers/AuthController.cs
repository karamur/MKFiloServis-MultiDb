using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using MKFiloServis.Web.Services;
using MKFiloServis.Web.Services.Interfaces;
using MKFiloServis.Shared.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MKFiloServis.Web.Controllers;

/// <summary>
/// API Authentication Controller - JWT Token oluşturma ve doğrulama
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IKullaniciService _kullaniciService;
    private readonly IConfiguration _configuration;

    public AuthController(
        IKullaniciService kullaniciService,
        IConfiguration configuration)
    {
        _kullaniciService = kullaniciService;
        _configuration = configuration;
    }

    /// <summary>
    /// Kullanıcı adı ve şifre ile JWT token alır
    /// </summary>
    /// <param name="request">Giriş bilgileri</param>
    /// <returns>JWT token ve kullanıcı bilgileri</returns>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrEmpty(request.KullaniciAdi) || string.IsNullOrEmpty(request.Sifre))
        {
            return BadRequest(new { Error = "Kullanıcı adı ve şifre gereklidir" });
        }

        var sonuc = await _kullaniciService.GirisYapAsync(request.KullaniciAdi, request.Sifre);
        if (!sonuc.Basarili || sonuc.Kullanici == null)
        {
            return Unauthorized(new { Error = sonuc.Mesaj ?? "Geçersiz kullanıcı adı veya şifre" });
        }

        var kullanici = sonuc.Kullanici;
        if (!kullanici.Aktif)
        {
            return Unauthorized(new { Error = "Kullanıcı hesabı devre dışı" });
        }

        var token = GenerateJwtToken(kullanici);

        return Ok(new LoginResponse
        {
            Token = token,
            KullaniciId = kullanici.Id,
            KullaniciAdi = kullanici.KullaniciAdi,
            AdSoyad = kullanici.AdSoyad,
            Rol = kullanici.Rol?.RolAdi ?? "Kullanici",
            ExpiresAt = DateTime.UtcNow.AddHours(24)
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

        var kullanici = await _kullaniciService.GetByIdAsync(kullaniciId);
        if (kullanici == null || !kullanici.Aktif)
        {
            return Unauthorized(new { Error = "Kullanıcı bulunamadı veya devre dışı" });
        }

        var token = GenerateJwtToken(kullanici);

        return Ok(new LoginResponse
        {
            Token = token,
            KullaniciId = kullanici.Id,
            KullaniciAdi = kullanici.KullaniciAdi,
            AdSoyad = kullanici.AdSoyad,
            Rol = kullanici.Rol?.RolAdi ?? "Kullanici",
            ExpiresAt = DateTime.UtcNow.AddHours(24)
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

    private string GenerateJwtToken(Kullanici kullanici)
    {
        var jwtSecret = _configuration["Jwt:Secret"];
        if (string.IsNullOrEmpty(jwtSecret) || jwtSecret.StartsWith("REPLACE_") || jwtSecret.Length < 32)
        {
            throw new InvalidOperationException(
                "JWT Secret yapılandırılmamış veya geçersiz. " +
                "appsettings.Production.json → Jwt:Secret alanına en az 32 karakterli güçlü bir değer girin.");
        }
        var jwtIssuer = _configuration["Jwt:Issuer"] ?? "KOAFiloServis";
        var jwtAudience = _configuration["Jwt:Audience"] ?? "KOAFiloServis-API";

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var rolAdi = kullanici.Rol?.RolAdi ?? "Kullanici";

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, kullanici.Id.ToString()),
            new Claim(ClaimTypes.Name, kullanici.KullaniciAdi),
            new Claim(ClaimTypes.Role, rolAdi),
            new Claim("AdSoyad", kullanici.AdSoyad ?? ""),
            new Claim("Email", kullanici.Email ?? ""),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: credentials
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



