using KOAFiloServis.Web.Services.Security;
using Microsoft.AspNetCore.Mvc;

namespace KOAFiloServis.Web.Controllers;

/// <summary>
/// Sistem sağlığı ve DPAPI recovery durumu API'ı.
/// Yönetim dashboard'ında kullanılır.
/// </summary>
[ApiController]
[Route("api/system")]
public class SystemHealthController : ControllerBase
{
    private readonly IDecryptionRecoveryTracker _recoveryTracker;
    private readonly ILogger<SystemHealthController> _logger;

    public SystemHealthController(
        IDecryptionRecoveryTracker recoveryTracker,
        ILogger<SystemHealthController> logger)
    {
        _recoveryTracker = recoveryTracker;
        _logger = logger;
    }

    /// <summary>
    /// Decrypt recovery durumunu rapor et
    /// </summary>
    [HttpGet("decryption-recovery-status")]
    public ActionResult<DecryptionRecoveryStatusDto> GetDecryptionRecoveryStatus()
    {
        var (failures, recoveries) = _recoveryTracker.GetSessionStats();
        var recentFailures = _recoveryTracker.GetRecentFailures(5);

        var dto = new DecryptionRecoveryStatusDto
        {
            TotalDecryptionFailures = failures,
            TotalDecryptionRecoveries = recoveries,
            RecentFailures = recentFailures.Select(f => new DecryptionFailureDto
            {
                RelativePath = f.RelativePath,
                Reason = f.Reason,
                OccurredAt = f.OccurredAt
            }).ToList()
        };

        return Ok(dto);
    }

    /// <summary>
    /// Sistem sağlığı özeti
    /// </summary>
    [HttpGet("health-summary")]
    public ActionResult<HealthSummaryDto> GetHealthSummary()
    {
        var (failures, recoveries) = _recoveryTracker.GetSessionStats();

        var summary = new HealthSummaryDto
        {
            Status = "OK",
            EncryptedFileDecryptionIssues = failures > 0 ? $"⚠️ {failures} dosya açılamadı (master key değişti?)" : "✓ Tüm dosyalar açılabilir",
            DecryptionRecoveryAttempts = recoveries,
            Message = failures > 0 
                ? $"Eski master key problemi: {failures} dosya decrypt başarısız. Bkz: dashboard recovery raporu."
                : "DPAPI ve dosya şifreleme normal çalışıyor."
        };

        return Ok(summary);
    }
}

public class DecryptionRecoveryStatusDto
{
    public int TotalDecryptionFailures { get; set; }
    public int TotalDecryptionRecoveries { get; set; }
    public List<DecryptionFailureDto> RecentFailures { get; set; } = new();
}

public class DecryptionFailureDto
{
    public string RelativePath { get; set; } = "";
    public string Reason { get; set; } = "";
    public DateTime OccurredAt { get; set; }
}

public class HealthSummaryDto
{
    public string Status { get; set; } = "OK";
    public string EncryptedFileDecryptionIssues { get; set; } = "";
    public int DecryptionRecoveryAttempts { get; set; }
    public string Message { get; set; } = "";
}
