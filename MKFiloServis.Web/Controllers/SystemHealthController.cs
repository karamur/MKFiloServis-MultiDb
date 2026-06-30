using MKFiloServis.Web.Services;
using MKFiloServis.Web.Services.Interfaces;
using MKFiloServis.Web.Services.Security;
using Microsoft.AspNetCore.Mvc;

namespace MKFiloServis.Web.Controllers;

/// <summary>
/// Sistem sağlığı ve DPAPI recovery durumu API'ı.
/// Yönetim dashboard'ında kullanılır.
/// </summary>
[ApiController]
[Route("api/system")]
public class SystemHealthController : ControllerBase
{
    private readonly IDecryptionRecoveryTracker _recoveryTracker;
    private readonly FileRecoveryService _fileRecoveryService;
    private readonly ILogger<SystemHealthController> _logger;

    public SystemHealthController(
        IDecryptionRecoveryTracker recoveryTracker,
        FileRecoveryService fileRecoveryService,
        ILogger<SystemHealthController> logger)
    {
        _recoveryTracker = recoveryTracker;
        _fileRecoveryService = fileRecoveryService;
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

    /// <summary>
    /// Eski anahtarla şifrelenmiş dosyaları kurtarma (batch recovery).
    /// 
    /// Giriş: { "oldMasterKeyHex": "...", "targetDirectory": "optional" }
    /// 
    /// Recovery işlemi:
    ///   1. Eski key ile .enc dosyaları decrypt eder
    ///   2. Yeni key ile yeniden şifreler (re-encrypt)
    ///   3. Başarılı/başarısız sayısını döner
    /// 
    /// ⚠️ YETKİLİ KULLANICI TARAFINDAN ÇAĞRILMALIDIR
    /// </summary>
    [HttpPost("recover-encrypted-files")]
    public async Task<ActionResult<FileRecoveryResultDto>> RecoverEncryptedFilesAsync(
        [FromBody] RecoverEncryptedFilesRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.OldMasterKeyHex))
        {
            _logger.LogWarning("❌ Recovery isteği eksik: OldMasterKeyHex boş");
            return BadRequest(new { error = "OldMasterKeyHex gerekli ve boş olamaz" });
        }

        try
        {
            // Hex string'i byte array'e dönüştür
            var oldKeyBytes = ConvertHexToBytes(request.OldMasterKeyHex);
            if (oldKeyBytes == null || oldKeyBytes.Length != 32)
            {
                _logger.LogWarning("❌ Geçersiz eski key: uzunluk {Len} (32 bekleniyor)", oldKeyBytes?.Length ?? 0);
                return BadRequest(new { error = "Eski key hex string 64 karakter (32 byte) olmalıdır" });
            }

            _logger.LogInformation("🔄 Batch recovery başlatılıyor: targetDir={Dir}", request.TargetDirectory ?? "default");

            var result = await _fileRecoveryService.RecoverEncryptedFilesAsync(
                oldKeyBytes,
                request.TargetDirectory,
                cancellationToken);

            if (!string.IsNullOrEmpty(result.ErrorMessage))
            {
                _logger.LogError("❌ Recovery başarısız: {Error}", result.ErrorMessage);
                return BadRequest(new { error = result.ErrorMessage });
            }

            _logger.LogInformation(
                "✅ Recovery tamamlandı: {Success} başarı, {Failed} hata",
                result.SuccessCount, result.FailedCount);

            return Ok(new FileRecoveryResultDto
            {
                SuccessCount = result.SuccessCount,
                FailedCount = result.FailedCount,
                SkippedCount = result.SkippedCount,
                RecoveredFiles = result.RecoveredFiles,
                FailedFiles = result.FailedFiles.Select(f => new { f.Path, f.Error }).ToList<object>(),
                IsSuccess = result.IsSuccess
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Recovery endpoint hatası");
            return StatusCode(500, new { error = $"Recovery hatası: {ex.Message}" });
        }
    }

    private byte[]? ConvertHexToBytes(string hex)
    {
        try
        {
            hex = hex.Trim().Replace(" ", "");
            if (hex.Length % 2 != 0)
                return null;

            var bytes = new byte[hex.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            }
            return bytes;
        }
        catch
        {
            return null;
        }
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

public class RecoverEncryptedFilesRequestDto
{
    /// <summary>
    /// Eski master key'i 64 karakterlik HEX string olarak sağla.
    /// Örn: "A1B2C3D4E5F6...0102" (32 byte = 64 hex char)
    /// </summary>
    public string OldMasterKeyHex { get; set; } = "";

    /// <summary>
    /// Opsiyonel: Taranacak hedef dizin (Arsiv/ relative).
    /// Boş bırakılırsa varsayılan Personel + Arac dizinleri taranır.
    /// </summary>
    public string? TargetDirectory { get; set; }
}

public class FileRecoveryResultDto
{
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public int SkippedCount { get; set; }
    public List<string> RecoveredFiles { get; set; } = new();
    public List<object> FailedFiles { get; set; } = new();
    public bool IsSuccess { get; set; }
}



