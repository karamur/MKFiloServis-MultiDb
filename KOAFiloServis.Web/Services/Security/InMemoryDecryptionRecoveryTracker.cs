namespace KOAFiloServis.Web.Services.Security;

/// <summary>
/// In-memory decrypt hatası ve recovery sayaçları.
/// Eski master key probleminin yönetim dashboard'ında takip edilmesi için.
/// </summary>
public sealed class InMemoryDecryptionRecoveryTracker : IDecryptionRecoveryTracker
{
    private int _failureCount;
    private int _recoveryCount;
    private readonly Queue<DecryptionFailureRecord> _recentFailures = new(100);
    private readonly Lock _lock = new();

    public void TrackDecryptionFailure(string relativePath, string reason)
    {
        lock (_lock)
        {
            _failureCount++;
            var record = new DecryptionFailureRecord(relativePath, reason, DateTime.UtcNow);
            _recentFailures.Enqueue(record);

            // En eski kayıtları sil (max 100 saklı tut)
            while (_recentFailures.Count > 100)
                _recentFailures.Dequeue();
        }
    }

    public void TrackDecryptionRecovery(string relativePath, string method)
    {
        lock (_lock)
        {
            _recoveryCount++;
        }
    }

    public (int FailureCount, int RecoveryCount) GetSessionStats()
    {
        lock (_lock)
        {
            return (_failureCount, _recoveryCount);
        }
    }

    public IReadOnlyList<DecryptionFailureRecord> GetRecentFailures(int limit = 10)
    {
        lock (_lock)
        {
            return _recentFailures.TakeLast(limit).ToList().AsReadOnly();
        }
    }
}
