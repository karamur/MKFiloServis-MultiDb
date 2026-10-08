using System.Security.Cryptography;
using System.Text.Json;

namespace MKFiloServis.Shared.Auditing;

/// <summary>Durable operation evidence outside databases replaced by restore tools.</summary>
public sealed class RestoreOperationJournal : IDisposable
{
    private readonly string _directory;
    private bool _completed;
    public string DirectoryPath => _directory;
    private RestoreOperationJournal(string directory) => _directory = directory;

    public static async Task<RestoreOperationJournal> StartAsync(string sourceFile, string target, string provider)
    {
        var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MKFiloServis", "OperationJournal");
        var directory = Path.Combine(root, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        await using var stream = File.OpenRead(sourceFile);
        var hash = Convert.ToHexString(await SHA256.HashDataAsync(stream));
        var journal = new RestoreOperationJournal(directory);
        await using var receipt = new FileStream(Path.Combine(directory, "started.json"), FileMode.CreateNew, FileAccess.Write, FileShare.Read);
        await JsonSerializer.SerializeAsync(receipt, new
        {
            Operation = "DatabaseRestore", StartedAtUtc = DateTime.UtcNow, Source = Path.GetFileName(sourceFile),
            SourceSha256 = hash, Target = target, Provider = provider, Actor = Environment.UserName,
            Status = "Started", FailureSemantics = "Absent success receipt means failed or unconfirmed; never assume rollback"
        });
        receipt.Flush(flushToDisk: true);
        return journal;
    }

    public void Complete()
    {
        // CreateNew: never overwrite a previously issued receipt.
        using var stream = new FileStream(Path.Combine(_directory, "completed.json"), FileMode.CreateNew, FileAccess.Write, FileShare.Read);
        JsonSerializer.Serialize(stream, new { CompletedAtUtc = DateTime.UtcNow, Status = "Succeeded" });
        stream.Flush(flushToDisk: true);
        _completed = true;
    }

    public void RollbackCompleted(string failure)
    {
        using var stream = new FileStream(Path.Combine(_directory, "rolled-back.json"), FileMode.CreateNew, FileAccess.Write, FileShare.Read);
        JsonSerializer.Serialize(stream, new
        {
            RecordedAtUtc = DateTime.UtcNow, Status = "RolledBack",
            Failure = failure.Length <= 2048 ? failure : failure[..2048]
        });
        stream.Flush(flushToDisk: true);
        _completed = true;
    }

    public void Dispose()
    {
        if (_completed) return;
        using var stream = new FileStream(Path.Combine(_directory, "unconfirmed.json"), FileMode.CreateNew, FileAccess.Write, FileShare.Read);
        JsonSerializer.Serialize(stream, new { RecordedAtUtc = DateTime.UtcNow, Status = "FailedOrUnconfirmed" });
        stream.Flush(flushToDisk: true);
    }
}
