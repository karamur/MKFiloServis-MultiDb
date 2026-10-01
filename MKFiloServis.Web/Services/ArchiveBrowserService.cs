using MKFiloServis.Web.Helpers;
using MKFiloServis.Web.Services.Security;

namespace MKFiloServis.Web.Services;

public sealed class ArchiveBrowserService
{
    private readonly string _storageRoot;
    private readonly string _repositoryRoot;
    private readonly string _archiveRoot;

    public ArchiveBrowserService(IWebHostEnvironment environment)
    {
        _storageRoot = Path.GetFullPath(AppStoragePaths.GetStorageRoot(environment.ContentRootPath));
        _repositoryRoot = Path.GetFullPath(AppStoragePaths.GetArchiveRepositoryRoot(environment.ContentRootPath));
        _archiveRoot = Path.GetFullPath(Path.Combine(_storageRoot, "Arsiv"));
        Directory.CreateDirectory(_repositoryRoot);
    }

    public string RepositoryRoot => _repositoryRoot;
    public string StorageRoot => _storageRoot;

    public IReadOnlyList<ArchiveDirectoryItem> GetDirectories(string? relativeDirectory = null)
    {
        if (string.IsNullOrWhiteSpace(relativeDirectory))
        {
            return new[] { _repositoryRoot, _archiveRoot }
                .Where(Directory.Exists)
                .Select(path => new ArchiveDirectoryItem(Path.GetFileName(path), Path.GetRelativePath(_storageRoot, path).Replace('\\', '/')))
                .ToList();
        }

        var directory = ResolveBrowsePath(relativeDirectory);
        return Directory.EnumerateDirectories(directory)
            .Where(path => !IsReparsePoint(path))
            .Select(path => new DirectoryInfo(path))
            .OrderBy(info => info.Name)
            .Select(info => new ArchiveDirectoryItem(
                info.Name,
                Path.GetRelativePath(_storageRoot, info.FullName).Replace('\\', '/')))
            .ToList();
    }

    public IReadOnlyList<ArchiveFileItem> GetFiles(string? relativeDirectory = null, bool recursive = true)
    {
        var roots = string.IsNullOrWhiteSpace(relativeDirectory)
            ? new[] { _repositoryRoot, _archiveRoot }.Where(Directory.Exists)
            : new[] { ResolveBrowsePath(relativeDirectory) }.AsEnumerable();

        return roots.SelectMany(root => EnumerateFilesWithoutLinks(root, recursive))
            .Select(path => new FileInfo(path))
            .OrderByDescending(info => info.LastWriteTimeUtc)
            .Select(info =>
            {
                var storageRelativePath = Path.GetRelativePath(_storageRoot, info.FullName).Replace('\\', '/');
                var repositoryRelativePath = storageRelativePath;
                var format = DetectFormat(info.FullName);
                var displayName = info.Name.EndsWith(".enc", StringComparison.OrdinalIgnoreCase)
                    ? info.Name[..^4]
                    : info.Name;

                return new ArchiveFileItem(
                    storageRelativePath,
                    repositoryRelativePath,
                    displayName,
                    Path.GetDirectoryName(repositoryRelativePath)?.Replace('\\', '/') ?? string.Empty,
                    Path.GetExtension(displayName).ToLowerInvariant(),
                    info.Length,
                    info.LastWriteTime,
                    format);
            })
            .ToList();
    }

    public string ValidateStorageRelativePath(string storageRelativePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storageRelativePath);
        var fullPath = Path.GetFullPath(Path.Combine(_storageRoot, storageRelativePath.Replace('/', Path.DirectorySeparatorChar)));
        EnsureBrowsablePath(fullPath);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException("Depo dosyası bulunamadı.", storageRelativePath);
        return fullPath;
    }

    private string ResolveBrowsePath(string relativeDirectory)
    {
        var fullPath = Path.GetFullPath(Path.Combine(_storageRoot, relativeDirectory.Replace('/', Path.DirectorySeparatorChar)));
        EnsureBrowsablePath(fullPath);
        if (!Directory.Exists(fullPath))
            throw new DirectoryNotFoundException("Seçilen arşiv dizini bulunamadı.");
        return fullPath;
    }

    private void EnsureBrowsablePath(string fullPath)
    {
        if (!IsUnderRoot(fullPath, _repositoryRoot) && !IsUnderRoot(fullPath, _archiveRoot))
            throw new UnauthorizedAccessException("Yalnız Depo ve Arşiv dizinlerine erişilebilir.");

        var relative = Path.GetRelativePath(_storageRoot, fullPath);
        var current = _storageRoot;
        foreach (var part in relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
        {
            current = Path.Combine(current, part);
            if ((Directory.Exists(current) || File.Exists(current)) && IsReparsePoint(current))
                throw new UnauthorizedAccessException("Bağlantı dizinleri üzerinden arşiv dışına çıkılamaz.");
        }
    }

    private static IEnumerable<string> EnumerateFilesWithoutLinks(string root, bool recursive)
    {
        var pending = new Stack<string>();
        pending.Push(root);
        while (pending.Count > 0)
        {
            var directory = pending.Pop();
            foreach (var file in Directory.EnumerateFiles(directory))
                if (!IsReparsePoint(file)) yield return file;
            if (recursive)
                foreach (var child in Directory.EnumerateDirectories(directory))
                    if (!IsReparsePoint(child)) pending.Push(child);
        }
    }

    private static bool IsReparsePoint(string path)
        => (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0;

    private static bool IsUnderRoot(string fullPath, string root)
    {
        var normalizedRoot = root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return fullPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase)
            || string.Equals(fullPath, root, StringComparison.OrdinalIgnoreCase);
    }

    private static ArchiveFileFormat DetectFormat(string fullPath)
    {
        using var stream = File.OpenRead(fullPath);
        Span<byte> header = stackalloc byte[4];
        var read = stream.Read(header);
        if (read == 4 && DataProtectionFileProtector.IsProtectedFormat(header))
            return ArchiveFileFormat.DataProtection;
        if (read == 4 && header.SequenceEqual("KOA1"u8))
            return ArchiveFileFormat.LegacyAes;
        if (fullPath.EndsWith(".enc", StringComparison.OrdinalIgnoreCase))
            return ArchiveFileFormat.LegacyEncrypted;
        return ArchiveFileFormat.Plain;
    }
}

public sealed record ArchiveDirectoryItem(string Name, string RelativePath);

public sealed record ArchiveFileItem(
    string StorageRelativePath,
    string RepositoryRelativePath,
    string DisplayName,
    string Directory,
    string Extension,
    long Size,
    DateTime ModifiedAt,
    ArchiveFileFormat Format);

public enum ArchiveFileFormat
{
    Plain,
    DataProtection,
    LegacyAes,
    LegacyEncrypted
}
