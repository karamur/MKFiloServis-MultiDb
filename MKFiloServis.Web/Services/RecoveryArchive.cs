using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using MKFiloServis.Web.Helpers;
using MKFiloServis.Web.Services.Interfaces;
using System.Security.AccessControl;
using System.Security.Principal;

namespace MKFiloServis.Web.Services;

/// <summary>Dosya ve DB ZIP yedeklerinin ortak kurtarma kapsamı.</summary>
internal static class RecoveryArchive
{
    private const string ManifestName = "recovery-manifest.json";
    private const string ProbePurpose = "MKFiloServis.RecoveryArchive.Probe.v1";
    private const string ProbeValue = "MKFiloServis-RecoveryArchive-v1";
    private sealed record FileRecord(string Path, long Length, string Sha256);
    private sealed record Manifest(string Format, DateTime CreatedAtUtc, string ProtectedProbe,
        string KeyRecoveryRequirement, List<FileRecord> Files);
    private const long MaxExpandedBytes = 100L * 1024 * 1024 * 1024;
    private const int MaxFileCount = 100_000;

    /// <summary>Güvenilir yedeği yeni, kapalı bir kurtarma klasörüne hazırlar; canlı hedeflere yazmaz.</summary>
    public static async Task<RecoveryPreparationResult> PrepareAsync(string path, string contentRoot, string? webRoot, CancellationToken ct)
    {
        var root = Path.GetFullPath(Path.Combine(AppStoragePaths.GetStorageRoot(contentRoot), "RecoveryStaging"));
        var publicRoot = Path.GetFullPath(webRoot ?? Path.Combine(contentRoot, "wwwroot")).TrimEnd(Path.DirectorySeparatorChar);
        if (root.Equals(publicRoot, PathComparison) || root.StartsWith(publicRoot + Path.DirectorySeparatorChar, PathComparison))
            throw new IOException("Kurtarma dosyaları web kökünde hazırlanamaz. Depolama kökünü düzeltin.");
        EnsureNoLinks(root);
        Directory.CreateDirectory(root);
        RestrictDirectory(root);
        var id = $"recovery-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}";
        var staging = Path.Combine(root, "." + id + ".partial");
        var destination = Path.Combine(root, id);
        EnsureNoLinks(path);
        // Tek dosya tutamacı boyunca doğrula ve aç; doğrulama sonrası ZIP değiştirme yarışını azaltır.
        using var source = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var archive = new ZipArchive(source, ZipArchiveMode.Read);
        var manifest = await VerifyArchiveAsync(archive, ct);
        Directory.CreateDirectory(staging);
        RestrictDirectory(staging);
        try
        {
            foreach (var file in manifest.Files)
            {
                ct.ThrowIfCancellationRequested();
                var target = Path.GetFullPath(Path.Combine(staging, file.Path.Replace('/', Path.DirectorySeparatorChar)));
                if (!target.StartsWith(staging + Path.DirectorySeparatorChar, PathComparison))
                    throw new InvalidDataException("Kurtarma yolu hedef dizin dışında.");
                EnsureNoLinks(target);
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                await using var input = archive.GetEntry(file.Path)!.Open();
                await using var output = new FileStream(target, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                var hash = await HashBoundedAsync(input, file.Length, ct, output);
                if (!Convert.ToHexString(hash).Equals(file.Sha256, StringComparison.Ordinal))
                    throw new InvalidDataException("Kurtarma sırasında yedek bütünlüğü değişti.");
                await output.FlushAsync(ct);
            }
            // Manifest de kurtarma dosyalarıyla kalır; prob/sürüm incelemesinde kullanılır.
            var manifestPath = Path.Combine(staging, ManifestName);
            await using (var output = new FileStream(manifestPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                await JsonSerializer.SerializeAsync(output, manifest, cancellationToken: ct);
            var keys = Path.Combine(staging, "storage", "keys");
            bool probeVerified;
            try
            {
                // Ayrı DI/anahtar halkası: çalışan uygulamanın anahtarlarıyla yanlış pozitif oluşmaz.
                var services = new ServiceCollection();
                services.AddLogging();
                services.AddDataProtection().SetApplicationName("MKFiloServis")
                    .PersistKeysToFileSystem(new DirectoryInfo(keys)).DisableAutomaticKeyGeneration();
                using var isolated = services.BuildServiceProvider();
                var protector = isolated.GetRequiredService<IDataProtectionProvider>().CreateProtector(ProbePurpose);
                probeVerified = protector.Unprotect(manifest.ProtectedProbe) == ProbeValue;
            }
            catch (Exception ex) when (ex is CryptographicException or InvalidOperationException or IOException or UnauthorizedAccessException or System.Xml.XmlException or ArgumentException)
            {
                probeVerified = false;
            }
            ct.ThrowIfCancellationRequested();
            Directory.Move(staging, destination);
            return new RecoveryPreparationResult
            {
                Prepared = true, FolderPath = destination, FileCount = manifest.Files.Count,
                KeyProbeVerified = probeVerified, HasDatabaseDump = manifest.Files.Any(x => x.Path == "database.backup"),
                Message = probeVerified
                    ? "Dosyalar hazırlandı; yedekteki anahtarlarla kurtarma probu çözüldü. Belge/credential ve DB ilişkisi kabulü ayrıca yapılmalıdır."
                    : "Dosyalar hazırlandı ancak kurtarma probu çözülemedi. Sertifika/DPAPI hesabı veya özgün anahtar malzemesi gerekir; anahtar kurtarması tamamlanmadı."
            };
        }
        finally
        {
            // Yalnız bu çağrının ürettiği, root içindeki geçici dizin temizlenir.
            if (Directory.Exists(staging))
            {
                EnsureNoLinks(staging);
                Directory.Delete(staging, recursive: true);
            }
        }
    }

    private static StringComparison PathComparison => OperatingSystem.IsWindows()
        ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    private static void EnsureNoLinks(string path)
    {
        for (var current = Path.GetFullPath(path); !string.IsNullOrEmpty(current); current = Path.GetDirectoryName(current))
            if ((File.Exists(current) || Directory.Exists(current)) && (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                throw new IOException("Kurtarma kaynağı/hedefi sembolik bağlantı içeremez.");
    }

    private static void RestrictDirectory(string path)
    {
        if (OperatingSystem.IsWindows())
        {
            var security = new DirectorySecurity();
            security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
            var inheritance = InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit;
            using var identity = WindowsIdentity.GetCurrent();
            foreach (var sid in new[] { identity.User!, new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null),
                new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null) })
                security.AddAccessRule(new FileSystemAccessRule(sid, FileSystemRights.FullControl,
                    inheritance, PropagationFlags.None, AccessControlType.Allow));
            new DirectoryInfo(path).SetAccessControl(security);
        }
        else File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
    }

    private static bool IsAllowedPath(string path)
    {
        if (string.IsNullOrEmpty(path) || path.Length > 2048 || path.Contains('\\') || path.Contains(':') || path.StartsWith('/')) return false;
        var segments = path.Split('/');
        foreach (var segment in segments)
        {
            var stem = segment.Split('.')[0];
            if (string.IsNullOrWhiteSpace(segment) || segment is "." or ".." || segment.EndsWith('.') || segment.EndsWith(' ')
                || segment.Any(c => c < 32 || "<>\"|?*".Contains(c))
                || new[] { "CON", "PRN", "AUX", "NUL", "COM¹", "COM²", "COM³", "LPT¹", "LPT²", "LPT³", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
                    "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9" }.Contains(stem, StringComparer.OrdinalIgnoreCase)) return false;
        }
        return path == "database.backup" || new[] { "appsettings.json", "appsettings.Production.json", "dbsettings.json",
                "portalsettings.json", "backup_settings.json" }.Any(x => path == "application/" + x)
            || new[] { "storage/uploads/", "storage/Arsiv/", "storage/Depo/", "storage/data/", "storage/logs/", "storage/keys/",
                "application/Data/LucaSettings/", "application/wwwroot/belgeler/", "application/wwwroot/uploads/" }
                .Any(prefix => path.StartsWith(prefix, StringComparison.Ordinal));
    }

    public static async Task CreateAsync(string destination, string contentRoot,
        IDataProtectionProvider protection, string? databaseDump, CancellationToken ct)
    {
        var storageRoot = AppStoragePaths.GetStorageRoot(contentRoot);
        var target = Path.GetFullPath(destination);
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        var selectedRoots = new[] { "uploads", "Arsiv", "Depo", "data", "logs", "keys" }
            .Select(folder => Path.Combine(storageRoot, folder))
            .Concat(new[] { Path.Combine(contentRoot, "Data", "LucaSettings"),
                Path.Combine(contentRoot, "wwwroot", "belgeler"), Path.Combine(contentRoot, "wwwroot", "uploads") });
        foreach (var root in selectedRoots)
            if (target.StartsWith(Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, comparison))
                throw new IOException("Yedek hedefi yedeklenen kaynak klasörünün içinde olamaz.");
        var temporary = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
        // Anahtar halkasını ihtiyaç anında oluşturur; legacy master.key üretmez.
        var probe = protection.CreateProtector(ProbePurpose).Protect(ProbeValue);
        var files = new List<FileRecord>();
        try
        {
            using (var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                using (var archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
                {
                    if (databaseDump != null)
                        await AddFileAsync(archive, databaseDump, "database.backup", files, ct);
                    // Anahtarlar en son kopyalanır: veri kopyası sırasında üretilen anahtarlar da alınır.
                    foreach (var folder in new[] { "uploads", "Arsiv", "Depo", "data", "logs", "keys" })
                        await AddDirectoryAsync(archive, Path.Combine(storageRoot, folder),
                            "storage/" + folder, files, ct);
                    await AddDirectoryAsync(archive, Path.Combine(contentRoot, "Data", "LucaSettings"),
                        "application/Data/LucaSettings", files, ct);
                    await AddDirectoryAsync(archive, Path.Combine(contentRoot, "wwwroot", "belgeler"),
                        "application/wwwroot/belgeler", files, ct);
                    // Eski kurulumların web root altında kalan ekleri de korunur.
                    await AddDirectoryAsync(archive, Path.Combine(contentRoot, "wwwroot", "uploads"),
                        "application/wwwroot/uploads", files, ct);
                    foreach (var name in new[] { "appsettings.json", "appsettings.Production.json",
                        "dbsettings.json", "portalsettings.json", "backup_settings.json" })
                    {
                        var source = Path.Combine(contentRoot, name);
                        if (File.Exists(source))
                            await AddFileAsync(archive, source, "application/" + name, files, ct);
                    }
                    if (!files.Any(f => f.Path.StartsWith("storage/keys/key-", StringComparison.Ordinal)
                        && f.Path.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)))
                        throw new InvalidDataException("DataProtection anahtar halkası yedeğe alınamadı.");
                    var manifest = new Manifest("MKFiloServis-RecoveryArchive-v1", DateTime.UtcNow, probe,
                        "Key ring XML ile birlikte, varsa anahtarları koruyan sertifika/DPAPI hesabı da gerekir. " +
                        "Legacy master.key mevcutsa kopyalanır; DPAPI koruması başka makinede tek başına açılamayabilir.", files);
                    var entry = archive.CreateEntry(ManifestName, CompressionLevel.Optimal);
                    await using var manifestStream = entry.Open();
                    await JsonSerializer.SerializeAsync(manifestStream, manifest, cancellationToken: ct);
                }
                output.Flush(flushToDisk: true);
            }
            await VerifyAsync(temporary, ct);
            File.Move(temporary, destination, overwrite: false);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    public static async Task VerifyAsync(string path, CancellationToken ct)
    {
        using var archive = ZipFile.OpenRead(path);
        await VerifyArchiveAsync(archive, ct);
    }

    private static async Task<Manifest> VerifyArchiveAsync(ZipArchive archive, CancellationToken ct)
    {
        if (archive.Entries.Count > MaxFileCount + 1)
            throw new InvalidDataException("Yedekte çok fazla dosya var.");
        var duplicates = archive.Entries.GroupBy(e => e.FullName, StringComparer.OrdinalIgnoreCase)
            .Any(g => g.Count() > 1);
        if (duplicates) throw new InvalidDataException("Yedekte yinelenen dosya adı var.");
        var manifestEntry = archive.GetEntry(ManifestName)
            ?? throw new InvalidDataException("Kurtarma manifesti bulunamadı.");
        if (manifestEntry.Length <= 0 || manifestEntry.Length > 16 * 1024 * 1024)
            throw new InvalidDataException("Kurtarma manifesti boyutu geçersiz.");
        var manifest = await ReadManifestAsync(manifestEntry, ct);
        if (manifest.Format != "MKFiloServis-RecoveryArchive-v1" || manifest.Files == null || manifest.Files.Count > MaxFileCount
            || manifest.Files.Any(f => f == null)
            || string.IsNullOrEmpty(manifest.ProtectedProbe) || manifest.ProtectedProbe.Length > 65536
            || manifest.Files.Select(f => f.Path).Distinct(StringComparer.OrdinalIgnoreCase).Count() != manifest.Files.Count
            || archive.Entries.Count != manifest.Files.Count + 1)
            throw new InvalidDataException("Yedek kapsamı manifest ile uyuşmuyor.");
        long total = 0;
        foreach (var file in manifest.Files)
        {
            ct.ThrowIfCancellationRequested();
            if (file == null || !IsAllowedPath(file.Path) || file.Length < 0 || file.Length > MaxExpandedBytes - total)
                throw new InvalidDataException("Yedek yolu veya açılmış boyutu geçersiz.");
            total += file.Length;
            var entry = archive.GetEntry(file.Path) ?? throw new InvalidDataException("Yedek dosyası eksik: " + file.Path);
            if (((entry.ExternalAttributes >> 16) & 0xF000) == 0xA000 || (entry.ExternalAttributes & (int)FileAttributes.ReparsePoint) != 0)
                throw new InvalidDataException("Yedek sembolik bağlantı içeremez.");
            if (entry.Length != file.Length) throw new InvalidDataException("Yedek boyutu uyuşmuyor: " + file.Path);
            await using var input = entry.Open();
            var hash = await HashBoundedAsync(input, file.Length, ct);
            if (!Convert.ToHexString(hash).Equals(file.Sha256, StringComparison.Ordinal))
                throw new InvalidDataException("Yedek bütünlüğü uyuşmuyor: " + file.Path);
        }
        if (!manifest.Files.Any(x => x.Path.StartsWith("storage/keys/key-", StringComparison.Ordinal) && x.Path.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)))
            throw new InvalidDataException("Yedekte DataProtection anahtar halkası yok.");
        return manifest;
    }

    private static async Task<Manifest> ReadManifestAsync(ZipArchiveEntry entry, CancellationToken ct)
    {
        await using var input = entry.Open();
        using var json = new MemoryStream();
        var buffer = new byte[81920];
        try
        {
            int read;
            while ((read = await input.ReadAsync(buffer, ct)) > 0)
            {
                if (read > entry.Length - json.Length)
                    throw new InvalidDataException("Açılan manifest belirtilen boyutu aşıyor.");
                await json.WriteAsync(buffer.AsMemory(0, read), ct);
            }
            if (json.Length != entry.Length) throw new InvalidDataException("Manifest eksik.");
            json.Position = 0;
            return await JsonSerializer.DeserializeAsync<Manifest>(json, cancellationToken: ct)
                ?? throw new InvalidDataException("Kurtarma manifesti geçersiz.");
        }
        finally { CryptographicOperations.ZeroMemory(buffer); }
    }

    private static async Task<byte[]> HashBoundedAsync(Stream input, long expectedLength, CancellationToken ct, Stream? output = null)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var buffer = new byte[81920];
        long length = 0;
        try
        {
            int read;
            while ((read = await input.ReadAsync(buffer, ct)) > 0)
            {
                if (read > expectedLength - length)
                    throw new InvalidDataException("Açılan dosya manifest boyutunu aşıyor.");
                length += read;
                hash.AppendData(buffer, 0, read);
                if (output != null) await output.WriteAsync(buffer.AsMemory(0, read), ct);
            }
            if (length != expectedLength) throw new InvalidDataException("Açılan dosya eksik.");
            return hash.GetHashAndReset();
        }
        finally { CryptographicOperations.ZeroMemory(buffer); }
    }

    private static async Task AddDirectoryAsync(ZipArchive archive, string source, string prefix,
        List<FileRecord> files, CancellationToken ct)
    {
        if (!Directory.Exists(source)) return;
        if ((File.GetAttributes(source) & FileAttributes.ReparsePoint) != 0)
            throw new IOException("Yedek kaynak dizini sembolik bağlantı olamaz: " + source);
        foreach (var file in Directory.EnumerateFiles(source))
            await AddFileAsync(archive, file, prefix + "/" + Path.GetFileName(file), files, ct);
        foreach (var directory in Directory.EnumerateDirectories(source))
        {
            ct.ThrowIfCancellationRequested();
            if (Path.GetFileName(directory).Equals("Backups", StringComparison.OrdinalIgnoreCase)) continue;
            await AddDirectoryAsync(archive, directory, prefix + "/" + Path.GetFileName(directory), files, ct);
        }
    }

    private static async Task AddFileAsync(ZipArchive archive, string source, string name,
        List<FileRecord> files, CancellationToken ct)
    {
        if ((File.GetAttributes(source) & FileAttributes.ReparsePoint) != 0)
            throw new IOException("Yedek kaynak dosyası sembolik bağlantı olamaz: " + source);
        var entry = archive.CreateEntry(name, CompressionLevel.Optimal);
        await using var output = entry.Open();
        await using var input = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var buffer = new byte[81920];
        long length = 0;
        try
        {
            int read;
            while ((read = await input.ReadAsync(buffer, ct)) > 0)
            {
                await output.WriteAsync(buffer.AsMemory(0, read), ct);
                hash.AppendData(buffer, 0, read);
                length += read;
            }
            files.Add(new FileRecord(name, length, Convert.ToHexString(hash.GetHashAndReset())));
        }
        finally { CryptographicOperations.ZeroMemory(buffer); }
    }
}
