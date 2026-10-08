using MKFiloServis.Web.Data;
using MKFiloServis.Web.Helpers;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Web.Services;

/// <summary>Verifies migrated legacy content and retains the old path for rollback and late references.</summary>
public sealed class LegacyFileCleanupService
{
    private const string CommonPrefix = "__legacy_cleanup_v1__/common/";
    private const string WebPrefix = "__legacy_cleanup_v1__/web/";
    private const string StorageUploadsPrefix = "__legacy_cleanup_v1__/storage-uploads/";
    private const string SupportUploadsPrefix = "__legacy_cleanup_v1__/support-uploads/";
    private const string InvoiceWebPrefix = "__legacy_cleanup_v1__/invoice-web/";
    private const string MigrationPrefix = "__legacy_cleanup_v2__/";

    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly FileService _fileService;
    private readonly FileCleanupJournal _journal;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<LegacyFileCleanupService> _logger;
    private readonly ISecureFileService _secureFiles;

    public LegacyFileCleanupService(
        IDbContextFactory<ApplicationDbContext> dbFactory,
        FileService fileService,
        FileCleanupJournal journal,
        IWebHostEnvironment environment,
        ILogger<LegacyFileCleanupService> logger,
        ISecureFileService secureFiles)
    {
        _dbFactory = dbFactory;
        _fileService = fileService;
        _journal = journal;
        _environment = environment;
        _logger = logger;
        _secureFiles = secureFiles;
    }

    public static bool IsLegacyCleanupKey(string key) =>
        key.StartsWith(CommonPrefix, StringComparison.Ordinal) ||
        key.StartsWith(WebPrefix, StringComparison.Ordinal) ||
        key.StartsWith(StorageUploadsPrefix, StringComparison.Ordinal) ||
        key.StartsWith(SupportUploadsPrefix, StringComparison.Ordinal) ||
        key.StartsWith(InvoiceWebPrefix, StringComparison.Ordinal) ||
        key.StartsWith(MigrationPrefix, StringComparison.Ordinal);

    public Task CompleteAsync(string cleanupKey, CancellationToken cancellationToken = default) =>
        _journal.CompleteAsync(cleanupKey, cancellationToken);

    public string CreateKey(string oldPath, bool storageUploadsRoot = false, bool supportUploadsRoot = false, bool invoiceWebRoot = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(oldPath);
        if (new[] { storageUploadsRoot, supportUploadsRoot, invoiceWebRoot }.Count(value => value) > 1)
            throw new ArgumentException("Bir eski dosya için yalnızca bir depolama kökü seçilebilir.");
        if (invoiceWebRoot)
            return InvoiceWebPrefix + GetInvoiceWebRelativePath(oldPath);
        if (supportUploadsRoot)
            return SupportUploadsPrefix + GetSupportRelativePath(oldPath);
        var normalized = oldPath.Replace('\\', '/');
        if (!Path.IsPathRooted(oldPath) && !normalized.Contains('/'))
            return CommonPrefix + normalized;
        if (normalized.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
            return (storageUploadsRoot ? StorageUploadsPrefix : WebPrefix) + normalized["/uploads/".Length..];
        if (normalized.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase))
            return (storageUploadsRoot ? StorageUploadsPrefix : WebPrefix) + normalized["uploads/".Length..];
        if (normalized.StartsWith("\\uploads\\", StringComparison.OrdinalIgnoreCase))
            return (storageUploadsRoot ? StorageUploadsPrefix : WebPrefix) + normalized["\\uploads\\".Length..];
        throw new InvalidOperationException("Eski dosya yolu temizleme köklerine uymuyor.");
    }

    public async Task<string> EnqueueMigrationAsync(string oldPath, string newPath, CancellationToken ct = default, bool storageUploadsRoot = false, bool supportUploadsRoot = false, bool invoiceWebRoot = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newPath);
        var oldKey = CreateKey(oldPath, storageUploadsRoot, supportUploadsRoot, invoiceWebRoot);
        _ = Resolve(oldKey);
        var request = new MigrationCleanupRequest(oldKey, newPath);
        var payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request)))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var key = MigrationPrefix + payload;
        await _journal.EnqueueAsync(key, ct);
        return key;
    }

    public async Task ProcessAsync(string key, CancellationToken ct = default, bool completeJournal = true)
    {
        MigrationCleanupRequest? migration = null;
        var oldKey = key;
        if (key.StartsWith(MigrationPrefix, StringComparison.Ordinal))
        {
            var payload = key[MigrationPrefix.Length..].Replace('-', '+').Replace('_', '/');
            payload = payload.PadRight((payload.Length + 3) / 4 * 4, '=');
            migration = JsonSerializer.Deserialize<MigrationCleanupRequest>(
                Encoding.UTF8.GetString(Convert.FromBase64String(payload)))
                ?? throw new InvalidOperationException("Geçiş temizleme isteği okunamadı.");
            oldKey = migration.OldKey;
            if (string.IsNullOrWhiteSpace(migration.NewPath))
                throw new InvalidOperationException("Geçiş temizleme isteğinde yeni dosya yolu eksik.");
        }

        var (fullPath, aliases) = Resolve(oldKey);
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var newReferenced = migration is null || await HasReferenceAsync(db, [migration.NewPath], ct);
        if (!newReferenced)
            throw new InvalidOperationException("Yeni yol henüz DB'de doğrulanmadı; eski dosya temizliği ertelendi.");

        var oldReferenced = await HasReferenceAsync(db, aliases, ct);
        if (oldReferenced)
        {
            if (completeJournal)
                await _journal.CompleteAsync(key, ct);
            _logger.LogInformation("Eski dosya hâlâ DB kaydında; geri alma için korundu: {Key}", key);
            return;
        }

        if (migration is not null)
            await VerifyReplacementAsync(fullPath, migration.NewPath, ct);
        ct.ThrowIfCancellationRequested();
        // A new DB reference can commit after the check above, including from another server.
        // Plain legacy readers do not have a quarantine fallback. Never unlink this path online.
        // Physical removal requires a separately approved offline cutover and recovery evidence.
        if (completeJournal)
            await _journal.CompleteAsync(key, ct);
        _logger.LogInformation("Eski dosya geçiş kontrolü tamamlandı; dosya geri alma ve geç gelen referanslar için yerinde korundu: {Key}", key);
    }

    private sealed record MigrationCleanupRequest(string OldKey, string NewPath);

    private async Task VerifyReplacementAsync(string sourcePath, string newPath, CancellationToken ct)
    {
        FileStream source;
        try
        {
            source = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read,
                81920, FileOptions.Asynchronous | FileOptions.SequentialScan);
        }
        catch (FileNotFoundException) { return; }
        catch (DirectoryNotFoundException) { return; }

        await using (source)
        {
            var replacement = await _secureFiles.ReadDecryptedAsync(newPath, ct)
                ?? throw new InvalidOperationException("Yeni şifreli kopya okunamadı; eski dosya korundu.");
            var sourceHash = await SHA256.HashDataAsync(source, ct);
            if (!CryptographicOperations.FixedTimeEquals(sourceHash, SHA256.HashData(replacement)))
                throw new InvalidOperationException("Yeni kopya eski dosyanın içeriğiyle eşleşmiyor; eski dosya korundu.");
        }
    }

    private static async Task<bool> HasReferenceAsync(ApplicationDbContext db, string[] aliases, CancellationToken ct)
        => await DatabaseFilePathInventory.IsReferencedAsync(db, aliases, ct) ||
           await DatabaseFilePathInventory.IsReferencedNormalizedAsync(db, aliases, OperatingSystem.IsWindows(), ct);

    private (string FullPath, string[] Aliases) Resolve(string key)
    {
        string root;
        string relative;
        string[] aliases;
        if (key.StartsWith(CommonPrefix, StringComparison.Ordinal))
        {
            root = _fileService.UploadRootPath;
            relative = key[CommonPrefix.Length..];
            if (relative.Contains('/') || relative.Contains('\\'))
                throw new InvalidOperationException("Ortak evrak eski dosya adı geçersiz.");
            var commonFullPath = StorageFilePath.Resolve(root, relative);
            aliases = [relative, commonFullPath.Replace('\\', '/')];
            return (commonFullPath, aliases);
        }

        if (key.StartsWith(InvoiceWebPrefix, StringComparison.Ordinal))
        {
            relative = key[InvoiceWebPrefix.Length..];
            if (!(relative.StartsWith("efatura/", StringComparison.OrdinalIgnoreCase) ||
                  relative.StartsWith("belgeler/efatura/", StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("Eski e-fatura web yolu izin verilen dizinin dışında.");
            root = _environment.WebRootPath;
            var invoiceFullPath = StorageFilePath.Resolve(root, relative);
            var canonical = invoiceFullPath.Replace('\\', '/');
            aliases = [relative, "/" + relative, canonical, Path.GetFullPath(invoiceFullPath)];
            return (invoiceFullPath, aliases);
        }

        var storageUploads = key.StartsWith(StorageUploadsPrefix, StringComparison.Ordinal);
        var supportUploads = key.StartsWith(SupportUploadsPrefix, StringComparison.Ordinal);
        if (!storageUploads && !supportUploads && !key.StartsWith(WebPrefix, StringComparison.Ordinal))
            throw new InvalidOperationException("Eski dosya temizleme kuyruğu anahtarı geçersiz.");

        root = storageUploads
            ? AppStoragePaths.GetUploadsRoot(_environment.ContentRootPath)
            : Path.Combine(_environment.WebRootPath, "uploads", supportUploads ? "destek" : string.Empty);
        relative = key[(storageUploads ? StorageUploadsPrefix : supportUploads ? SupportUploadsPrefix : WebPrefix).Length..];
        var uploadFullPath = StorageFilePath.Resolve(root, relative);
        var webKey = relative.Replace('\\', '/');
        aliases = supportUploads
            ? [uploadFullPath.Replace('\\', '/'), Path.GetFullPath(uploadFullPath)]
            : ["/uploads/" + webKey, "uploads/" + webKey, uploadFullPath.Replace('\\', '/')];
        return (uploadFullPath, aliases);
    }

    private string GetSupportRelativePath(string path)
    {
        var root = Path.GetFullPath(Path.Combine(_environment.WebRootPath, "uploads", "destek"));
        var fullPath = Path.GetFullPath(path);
        var rootWithSeparator = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        if (!Path.IsPathRooted(path) || !fullPath.StartsWith(rootWithSeparator, comparison))
            throw new InvalidOperationException("Destek eki yolu izin verilen uploads/destek kökünün dışında.");
        return Path.GetRelativePath(root, fullPath);
    }

    private static string GetInvoiceWebRelativePath(string path)
    {
        var normalized = path.Trim().Replace('\\', '/').TrimStart('/');
        if (!(normalized.StartsWith("efatura/", StringComparison.OrdinalIgnoreCase) ||
              normalized.StartsWith("belgeler/efatura/", StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Eski e-fatura yolu izin verilen wwwroot dizinlerinin dışında.");
        return normalized;
    }
}
