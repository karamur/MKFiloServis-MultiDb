using MKFiloServis.Web.Services;
using Microsoft.AspNetCore.Components;
using System.Security.Cryptography;

namespace MKFiloServis.Web.Components.Pages;

/// <summary>
/// Admin dashboard sayfası - Recovery ve system health monitoring.
/// </summary>
public partial class AdminSystemHealth
{
    [Inject] public FileRecoveryService FileRecoveryService { get; set; } = null!;
    [Inject] public ILogger<AdminSystemHealth> Logger { get; set; } = null!;

    private string oldMasterKeyHex = "";
    private string statusMessage = "";
    private bool isRecoveryInProgress = false;
    private RecoveryResultSimple? recoveryResult;

    private class RecoveryResultSimple
    {
        public int SuccessCount { get; set; }
        public int FailedCount { get; set; }
        public int SkippedCount { get; set; }
        public List<string> RecoveredFiles { get; set; } = new();
        public List<object> FailedFiles { get; set; } = new();
        public bool IsSuccess { get; set; }
    }

    private async Task TriggerRecoveryAsync()
    {
        if (string.IsNullOrWhiteSpace(oldMasterKeyHex))
        {
            statusMessage = "❌ Eski master key HEX string'i boş olamaz";
            return;
        }

        isRecoveryInProgress = true;
        statusMessage = "🔄 Recovery başlatılıyor...";
        recoveryResult = null;

        try
        {
            byte[] oldKeyBytes;
            try
            {
                oldKeyBytes = Convert.FromHexString(oldMasterKeyHex.Trim().Replace(" ", string.Empty));
            }
            catch (FormatException)
            {
                statusMessage = "❌ Eski master key geçerli hex biçiminde olmalıdır.";
                return;
            }

            if (oldKeyBytes.Length != 32)
            {
                CryptographicOperations.ZeroMemory(oldKeyBytes);
                statusMessage = "❌ Eski master key 64 hex karakter (32 bayt) olmalıdır.";
                return;
            }

            try
            {
                var result = await FileRecoveryService.RecoverEncryptedFilesAsync(oldKeyBytes);
                recoveryResult = new RecoveryResultSimple
                {
                    SuccessCount = result.SuccessCount,
                    FailedCount = result.FailedCount,
                    SkippedCount = result.SkippedCount,
                    RecoveredFiles = result.RecoveredFiles,
                    FailedFiles = result.FailedFiles.Cast<object>().ToList(),
                    IsSuccess = result.IsSuccess
                };
                statusMessage = !string.IsNullOrEmpty(result.ErrorMessage)
                    ? $"❌ Recovery hatası: {result.ErrorMessage}"
                    : result.IsSuccess
                        ? $"✅ Recovery başarılı: {result.SuccessCount} dosya kurtarıldı, {result.FailedCount} başarısız"
                        : $"⚠️ Recovery kısmi başarılı: {result.SuccessCount}✓ / {result.FailedCount}❌";
            }
            finally
            {
                CryptographicOperations.ZeroMemory(oldKeyBytes);
            }
        }
        catch (Exception ex)
        {
            statusMessage = $"❌ İstek hatası: {ex.Message}";
            Logger.LogError(ex, "Recovery endpoint çağırırken hata");
        }
        finally
        {
            isRecoveryInProgress = false;
        }
    }
}



