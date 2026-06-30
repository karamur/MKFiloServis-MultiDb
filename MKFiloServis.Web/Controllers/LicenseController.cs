using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Services;
using MKFiloServis.Web.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MKFiloServis.Web.Controllers;

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
            var created = DateTime.UtcNow; // 🔥 CRITICAL: UTC
            const string allowedVersion = "1.0.99";

            // AYNI SIGNATURE — LicenseService.GenerateSignature() + Desktop MainForm.Uret() ile birebir
            var signature = LicenseService.GenerateSignature(
                request.FirmaKodu, request.MachineId, expire,
                isDemo: false, allowedVersion, created,
                request.DurationDays, request.ContactPhone);

            // JSON → Base64 (LicenseInfo entity deserialize edilebilir)
            var json = JsonSerializer.Serialize(new
            {
                FirmaKodu = request.FirmaKodu,
                MachineId = request.MachineId,
                ExpireDate = expire,
                DurationDays = request.DurationDays,
                AllowedVersion = allowedVersion,
                IsDemo = false,
                CreatedAt = created,
                ContactPhone = request.ContactPhone,
                Signature = signature
            });

            var key = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));

            // DB'ye kaydet (istege bagli — log olarak)
            var lic = new LicenseInfo
            {
                FirmaKodu = request.FirmaKodu,
                MachineId = request.MachineId,
                ExpireDate = expire,
                DurationDays = request.DurationDays,
                AllowedVersion = allowedVersion,
                IsDemo = false,
                CreatedAt = created,
                ContactPhone = request.ContactPhone,
                Signature = signature,
                IsActive = false // API ile uretilen lisanslar default AKTIF DEGIL
            };
            await _licenseService.SaveGeneratedLogAsync(lic);

            _logger.LogInformation(
                "Lisans uretildi (API): Firma={Firma}, Makine={Machine}, Bitis={Expire}, Gun={Days}",
                request.FirmaKodu, request.MachineId, expire.ToString("yyyy-MM-dd"), request.DurationDays);

            return Ok(new LicenseGenerateResponse
            {
                LicenseKey = key,
                FirmaKodu = request.FirmaKodu,
                MachineId = request.MachineId,
                ExpireDate = expire,
                DurationDays = request.DurationDays,
                AllowedVersion = allowedVersion,
                ContactPhone = request.ContactPhone
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
    public int DurationDays { get; set; } = 365;
    public string ContactPhone { get; set; } = string.Empty;
}

public class LicenseGenerateResponse
{
    public string LicenseKey { get; set; } = string.Empty;
    public string FirmaKodu { get; set; } = string.Empty;
    public string MachineId { get; set; } = string.Empty;
    public DateTime ExpireDate { get; set; }
    public int DurationDays { get; set; }
    public string AllowedVersion { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
}



