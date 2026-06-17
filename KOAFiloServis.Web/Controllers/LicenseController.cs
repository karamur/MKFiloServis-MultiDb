using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using KOAFiloServis.Shared.Entities;
using KOAFiloServis.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KOAFiloServis.Web.Controllers;

/// <summary>
/// Lisans anahtari uretim API'si.
/// LisansDesktop ile ayni SECRET ve imza algoritmasini kullanir.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LicenseController : ControllerBase
{
    private readonly LicenseService _licenseService;
    private readonly ILogger<LicenseController> _logger;

    public LicenseController(LicenseService licenseService, ILogger<LicenseController> logger)
    {
        _licenseService = licenseService;
        _logger = logger;
    }

    /// <summary>
    /// POST /api/license/generate
    /// Lisans anahtari uretir. LicenseService.GenerateSignature() ile birebir ayni.
    /// </summary>
    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromBody] LicenseGenerateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FirmaKodu))
            return BadRequest(new { error = "FirmaKodu zorunludur." });

        if (string.IsNullOrWhiteSpace(request.MachineId))
            return BadRequest(new { error = "MachineId zorunludur." });

        try
        {
            var expire = request.ExpireDate ?? DateTime.UtcNow.AddYears(1);
            var created = DateTime.UtcNow;
            const string allowedVersion = "1.0.99";

            // AYNI SIGNATURE — LicenseService.GenerateSignature() ile birebir
            var signature = LicenseService.GenerateSignature(
                request.FirmaKodu, request.MachineId, expire,
                isDemo: false, allowedVersion, created);

            // JSON → Base64 (LicenseInfo entity deserialize edilebilir)
            var json = JsonSerializer.Serialize(new
            {
                FirmaKodu = request.FirmaKodu,
                MachineId = request.MachineId,
                ExpireDate = expire,
                AllowedVersion = allowedVersion,
                IsDemo = false,
                CreatedAt = created,
                Signature = signature
            });

            var key = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));

            // DB'ye kaydet (istege bagli — log olarak)
            var lic = new LicenseInfo
            {
                FirmaKodu = request.FirmaKodu,
                MachineId = request.MachineId,
                ExpireDate = expire,
                AllowedVersion = allowedVersion,
                IsDemo = false,
                CreatedAt = created,
                Signature = signature,
                IsActive = false // API ile uretilen lisanslar default AKTIF DEGIL
            };
            await _licenseService.SaveGeneratedLogAsync(lic);

            _logger.LogInformation(
                "Lisans uretildi (API): Firma={Firma}, Makine={Machine}, Bitis={Expire}",
                request.FirmaKodu, request.MachineId, expire.ToString("yyyy-MM-dd"));

            return Ok(new LicenseGenerateResponse
            {
                LicenseKey = key,
                FirmaKodu = request.FirmaKodu,
                MachineId = request.MachineId,
                ExpireDate = expire,
                AllowedVersion = allowedVersion
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lisans uretme hatasi (API)");
            return StatusCode(500, new { error = $"Lisans uretilemedi: {ex.Message}" });
        }
    }
}

// ══════════════════════════════════════════════
// REQUEST / RESPONSE MODELS
// ══════════════════════════════════════════════

public class LicenseGenerateRequest
{
    public string FirmaKodu { get; set; } = string.Empty;
    public string MachineId { get; set; } = string.Empty;
    public DateTime? ExpireDate { get; set; }
}

public class LicenseGenerateResponse
{
    public string LicenseKey { get; set; } = string.Empty;
    public string FirmaKodu { get; set; } = string.Empty;
    public string MachineId { get; set; } = string.Empty;
    public DateTime ExpireDate { get; set; }
    public string AllowedVersion { get; set; } = string.Empty;
}
