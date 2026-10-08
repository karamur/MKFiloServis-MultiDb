using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using MKFiloServis.Web.Helpers;

namespace MKFiloServis.Web.Services;

/// <summary>Encrypted, atomic, cross-process journal for post-DB file cleanup work.</summary>
public sealed class FileCleanupJournal
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly string _journalPath;
    private readonly string _lockPath;
    private readonly IDataProtector _protector;

    public FileCleanupJournal(
        IDataProtectionProvider protectionProvider,
        IWebHostEnvironment environment)
    {
        var storageRoot = Path.GetFullPath(AppStoragePaths.GetStorageRoot(environment.ContentRootPath));
        Directory.CreateDirectory(storageRoot);
        _journalPath = Path.Combine(storageRoot, ".file-cleanup-journal.v1");
        _lockPath = Path.Combine(storageRoot, ".file-cleanup-journal.lock");
        _protector = protectionProvider.CreateProtector("MKFiloServis.FileCleanupJournal.v1");
    }

    public async Task<PendingFileCleanup> EnqueueAsync(
        string relativePath,
        CancellationToken cancellationToken = default,
        bool waitForReferenceRemoval = false)
    {
        var path = NormalizePath(relativePath);
        return await MutateAndReturnAsync(items =>
        {
            var index = items.FindIndex(x => PathComparer.Equals(x.RelativePath, path));
            if (index < 0)
            {
                items.Add(new PendingFileCleanup(path, 0, DateTime.UtcNow, null, WaitForReferenceRemoval: waitForReferenceRemoval));
                index = items.Count - 1;
            }
            else
                items[index] = items[index] with
                {
                    WaitForReferenceRemoval = items[index].WaitForReferenceRemoval || waitForReferenceRemoval,
                    Revision = checked(items[index].Revision + 1)
                };
            return (items, items[index]);
        }, cancellationToken);
    }

    public Task<IReadOnlyList<PendingFileCleanup>> GetDueAsync(
        DateTime utcNow,
        CancellationToken cancellationToken = default)
        => ReadLockedAsync(items => (IReadOnlyList<PendingFileCleanup>)items
            .Where(x => x.NextAttemptUtc <= utcNow)
            .OrderBy(x => x.NextAttemptUtc)
            .ToArray(), cancellationToken);

    public Task<IReadOnlyList<PendingFileCleanup>> ClaimDueAsync(
        DateTime utcNow,
        TimeSpan leaseDuration,
        int maxCount = int.MaxValue,
        CancellationToken cancellationToken = default)
        => MutateAndReturnAsync(items =>
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxCount);
            if (leaseDuration <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(leaseDuration));

            var claimed = new List<PendingFileCleanup>();
            for (var i = 0; i < items.Count; i++)
            {
                if (claimed.Count == maxCount)
                    break;
                var item = items[i];
                var leaseActive = item.LeaseId is not null && item.LeaseUntilUtc > utcNow;
                if (item.NextAttemptUtc > utcNow || leaseActive)
                    continue;

                var updated = item with
                {
                    LeaseId = Guid.NewGuid().ToString("N"),
                    LeaseUntilUtc = utcNow.Add(leaseDuration)
                };
                items[i] = updated;
                claimed.Add(updated);
            }

            return (items, (IReadOnlyList<PendingFileCleanup>)claimed);
        }, cancellationToken);

    public Task<bool> RenewLeaseAsync(
        string relativePath,
        string leaseId,
        DateTime utcNow,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken = default)
    {
        var path = NormalizePath(relativePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(leaseId);
        if (leaseDuration <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(leaseDuration));

        return MutateAndReturnAsync(items =>
        {
            var index = items.FindIndex(x => PathComparer.Equals(x.RelativePath, path));
            if (index < 0 || !string.Equals(items[index].LeaseId, leaseId, StringComparison.Ordinal)
                || items[index].LeaseUntilUtc <= utcNow)
                return (items, false);

            items[index] = items[index] with { LeaseUntilUtc = utcNow.Add(leaseDuration) };
            return (items, true);
        }, cancellationToken);
    }

    public Task CompleteAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var path = NormalizePath(relativePath);
        return MutateAsync(items =>
        {
            items.RemoveAll(x => PathComparer.Equals(x.RelativePath, path));
            return items;
        }, cancellationToken);
    }

    /// <summary>A worker may acknowledge only its current, unexpired claim.</summary>
    public Task<bool> CompleteClaimAsync(string relativePath, string leaseId, long revision, CancellationToken cancellationToken = default)
    {
        var path = NormalizePath(relativePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(leaseId);
        return MutateAndReturnAsync(items =>
        {
            var index = items.FindIndex(x => PathComparer.Equals(x.RelativePath, path));
            if (index < 0 || items[index].LeaseId != leaseId || items[index].Revision != revision
                || items[index].LeaseUntilUtc <= DateTime.UtcNow)
                return (items, false);
            items.RemoveAt(index);
            return (items, true);
        }, cancellationToken);
    }

    public Task<bool> CompleteRequestAsync(PendingFileCleanup request, CancellationToken cancellationToken = default)
        => MutateAndReturnAsync(items =>
        {
            var index = items.FindIndex(x => PathComparer.Equals(x.RelativePath, request.RelativePath));
            if (index < 0 || items[index].RequestId != request.RequestId || items[index].Revision != request.Revision)
                return (items, false);
            items.RemoveAt(index);
            return (items, true);
        }, cancellationToken);

    public Task DeferAsync(string relativePath, string error, CancellationToken cancellationToken = default)
        => DeferAsync(relativePath, error, leaseId: null, cancellationToken);

    public Task DeferAsync(string relativePath, string error, string? leaseId, CancellationToken cancellationToken = default)
    {
        var path = NormalizePath(relativePath);
        return MutateAsync(items =>
        {
            var index = items.FindIndex(x => PathComparer.Equals(x.RelativePath, path));
            var previous = index >= 0 ? items[index] : new PendingFileCleanup(path, 0, DateTime.UtcNow, null);
            if (leaseId is not null && (index < 0 || !string.Equals(items[index].LeaseId, leaseId, StringComparison.Ordinal)
                || items[index].LeaseUntilUtc <= DateTime.UtcNow))
                return items;
            var attempts = checked(previous.Attempts + 1);
            var delaySeconds = Math.Min(3600d, 15d * Math.Pow(2d, Math.Min(attempts - 1, 8)));
            var updated = previous with
            {
                Attempts = attempts,
                NextAttemptUtc = DateTime.UtcNow.AddSeconds(delaySeconds),
                LastError = error.Length > 1000 ? error[..1000] : error,
                LeaseId = null,
                LeaseUntilUtc = null
            };

            if (index >= 0) items[index] = updated;
            else items.Add(updated);
            return items;
        }, cancellationToken);
    }

    private async Task<T> ReadLockedAsync<T>(Func<List<PendingFileCleanup>, T> read, CancellationToken ct)
    {
        await using var lease = await AcquireLockAsync(ct);
        var items = await ReadCoreAsync(ct);
        return read(items);
    }

    private async Task MutateAsync(Func<List<PendingFileCleanup>, List<PendingFileCleanup>> update, CancellationToken ct)
    {
        await using var lease = await AcquireLockAsync(ct);
        var items = update(await ReadCoreAsync(ct));
        await WriteCoreAsync(items, ct);
    }

    private async Task<T> MutateAndReturnAsync<T>(Func<List<PendingFileCleanup>, (List<PendingFileCleanup> Items, T Result)> update, CancellationToken ct)
    {
        await using var lease = await AcquireLockAsync(ct);
        var (items, result) = update(await ReadCoreAsync(ct));
        await WriteCoreAsync(items, ct);
        return result;
    }

    private async Task<List<PendingFileCleanup>> ReadCoreAsync(CancellationToken ct)
    {
        if (!File.Exists(_journalPath))
            return new List<PendingFileCleanup>();

        var protectedBytes = await File.ReadAllBytesAsync(_journalPath, ct);
        var json = _protector.Unprotect(protectedBytes);
        return JsonSerializer.Deserialize<List<PendingFileCleanup>>(json, JsonOptions)
               ?? throw new InvalidDataException("Dosya temizleme günlüğü boş/bozuk biçimde.");
    }

    private async Task WriteCoreAsync(List<PendingFileCleanup> items, CancellationToken ct)
    {
        var protectedBytes = _protector.Protect(JsonSerializer.SerializeToUtf8Bytes(items, JsonOptions));
        var temporaryPath = _journalPath + ".tmp-" + Guid.NewGuid().ToString("N");
        try
        {
            await using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
            {
                await stream.WriteAsync(protectedBytes, ct);
                await stream.FlushAsync(ct);
                stream.Flush(flushToDisk: true);
            }
            File.Move(temporaryPath, _journalPath, overwrite: true);
        }
        catch
        {
            try { File.Delete(temporaryPath); } catch { }
            throw;
        }
    }

    private async Task<FileStream> AcquireLockAsync(CancellationToken ct)
    {
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                return new FileStream(_lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 1, FileOptions.Asynchronous);
            }
            catch (IOException)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(50), ct);
            }
        }
    }

    private static string NormalizePath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return path.Trim().Replace('\\', '/');
    }

    private static StringComparer PathComparer => OperatingSystem.IsWindows()
        ? StringComparer.OrdinalIgnoreCase
        : StringComparer.Ordinal;
}

public sealed record PendingFileCleanup(
    string RelativePath,
    int Attempts,
    DateTime NextAttemptUtc,
    string? LastError,
    string? LeaseId = null,
    DateTime? LeaseUntilUtc = null,
    bool WaitForReferenceRemoval = false,
    long Revision = 0)
{
    // Distinguishes removal followed by a new request for the same path (ABA).
    public string RequestId { get; init; } = Guid.NewGuid().ToString("N");
}
