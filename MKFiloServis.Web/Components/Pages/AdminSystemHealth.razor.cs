using MKFiloServis.Web.Services;
using Microsoft.AspNetCore.Components;
using System.Net.Http.Json;

namespace MKFiloServis.Web.Components.Pages;

/// <summary>
/// Admin dashboard sayfası - Recovery ve system health monitoring.
/// </summary>
public partial class AdminSystemHealth
{
    [Inject] public HttpClient HttpClient { get; set; } = null!;
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
            var request = new
            {
                oldMasterKeyHex = oldMasterKeyHex.Trim(),
                targetDirectory = (string?)null
            };

            var response = await HttpClient.PostAsJsonAsync(
                "api/system/recover-encrypted-files",
                request);

            if (response.IsSuccessStatusCode)
            {
                recoveryResult = await response.Content.ReadFromJsonAsync<RecoveryResultSimple>();
                statusMessage = recoveryResult?.IsSuccess ?? false
                    ? $"✅ Recovery başarılı: {recoveryResult?.SuccessCount} dosya kurtarıldı, {recoveryResult?.FailedCount} başarısız"
                    : $"⚠️ Recovery kısmi başarılı: {recoveryResult?.SuccessCount}✓ / {recoveryResult?.FailedCount}❌";
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                statusMessage = $"❌ Recovery hatası: {error}";
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



