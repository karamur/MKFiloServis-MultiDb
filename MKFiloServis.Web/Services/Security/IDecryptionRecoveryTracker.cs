namespace MKFiloServis.Web.Services.Security;

/// <summary>
/// Şifreli dosya kurtarma (recovery) durumunu izler.
/// Eski master key problemi sonrası decrypt başarısızlıkları track eder.
/// </summary>
public interface IDecryptionRecoveryTracker
{
    /// <summary>
    /// Decrypt hatası kaydı (dosya başarısız oldukça)
    /// </summary>
    void TrackDecryptionFailure(string relativePath, string reason);

    /// <summary>
    /// Başarılı kurtarma kaydı (legacy format başarısı gibi)
    /// </summary>
    void TrackDecryptionRecovery(string relativePath, string method);

    /// <summary>
    /// Oturumda toplam hata ve kurtarma sayısı
    /// </summary>
    (int FailureCount, int RecoveryCount) GetSessionStats();

    /// <summary>
    /// Son N hatayı getir (dashboard/diagnostic için)
    /// </summary>
    IReadOnlyList<DecryptionFailureRecord> GetRecentFailures(int limit = 10);
}

public record DecryptionFailureRecord(
    string RelativePath,
    string Reason,
    DateTime OccurredAt);



