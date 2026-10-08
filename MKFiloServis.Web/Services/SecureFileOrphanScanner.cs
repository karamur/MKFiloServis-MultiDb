namespace MKFiloServis.Web.Services;

/// <summary>Reports encrypted payloads on disk that are absent from the DB path inventory.</summary>
public static class SecureFileOrphanScanner
{
    public static IReadOnlyList<SecureFileOrphan> FindOrphans(
        string storageRoot,
        IEnumerable<string> databasePaths)
    {
        var root = Path.GetFullPath(storageRoot);
        var comparison = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        var referenced = new HashSet<string>(comparison);
        foreach (var path in databasePaths)
        {
            var key = TryNormalizeStorageKey(root, path);
            if (key is not null)
                referenced.Add(key);
        }

        var candidates = new List<SecureFileOrphan>();
        foreach (var candidateRoot in new[]
                 {
                     Path.Combine(root, "uploads"),
                     Path.Combine(root, "Arsiv"),
                     Path.Combine(root, "Depo")
                 })
        {
            if (!Directory.Exists(candidateRoot))
                continue;

            foreach (var file in EnumerateFilesWithoutLinks(candidateRoot))
            {
                if (Path.GetRelativePath(root, file).Replace('\\', '/').StartsWith(
                        "uploads/.deleted-file-quarantine-v1/", StringComparison.OrdinalIgnoreCase))
                    continue; // Retained cleanup payloads are tracked by their original path.
                if (!file.EndsWith(".enc", StringComparison.OrdinalIgnoreCase))
                    continue;

                var key = Path.GetRelativePath(root, file).Replace('\\', '/');
                if (referenced.Contains(key))
                    continue;

                var info = new FileInfo(file);
                candidates.Add(new SecureFileOrphan(key, info.Length, info.LastWriteTimeUtc));
            }
        }

        return candidates.OrderBy(x => x.StoragePath, comparison).ToArray();
    }

    public static IReadOnlyList<SecureFileOrphan> FindLegacyPlaintextCandidates(
        string storageRoot,
        IEnumerable<string> databasePaths)
    {
        var root = Path.GetFullPath(storageRoot);
        var comparison = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        var referenced = new HashSet<string>(comparison);
        foreach (var path in databasePaths)
        {
            var key = TryNormalizeStorageKey(root, path);
            if (key is not null)
                referenced.Add(key);
        }

        var candidates = new List<SecureFileOrphan>();
        foreach (var candidateRoot in new[] { Path.Combine(root, "uploads"), Path.Combine(root, "Arsiv") })
        {
            if (!Directory.Exists(candidateRoot))
                continue;

            foreach (var file in EnumerateFilesWithoutLinks(candidateRoot))
            {
                var name = Path.GetFileName(file);
                if (name.EndsWith(".enc", StringComparison.OrdinalIgnoreCase) ||
                    name.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains(".uploading-", StringComparison.OrdinalIgnoreCase))
                    continue;

                var key = Path.GetRelativePath(root, file).Replace('\\', '/');
                if (referenced.Contains(key))
                    continue;

                var info = new FileInfo(file);
                candidates.Add(new SecureFileOrphan(key, info.Length, info.LastWriteTimeUtc));
            }
        }

        return candidates.OrderBy(x => x.StoragePath, comparison).ToArray();
    }

    public static IReadOnlyList<SecureFileOrphan> FindLegacyUploadCandidates(
        string uploadRoot,
        IEnumerable<string> databasePaths,
        bool fileServiceRoot)
    {
        var root = Path.GetFullPath(uploadRoot);
        if (!Directory.Exists(root) || IsReparsePoint(root))
            return [];

        var comparison = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        var referenced = new HashSet<string>(databasePaths
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => path.Trim().Replace('\\', '/')), comparison);
        var candidates = new List<SecureFileOrphan>();

        foreach (var file in EnumerateFilesWithoutLinks(root))
        {
            var name = Path.GetFileName(file);
            if (name.EndsWith(".enc", StringComparison.OrdinalIgnoreCase) ||
                name.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase) ||
                name.Contains(".uploading-", StringComparison.OrdinalIgnoreCase))
                continue;

            var relative = Path.GetRelativePath(root, file).Replace('\\', '/');
            var absolute = Path.GetFullPath(file).Replace('\\', '/');
            var hasReference = referenced.Contains(absolute) ||
                (fileServiceRoot
                    ? referenced.Contains(relative)
                    : referenced.Contains("/uploads/" + relative) || referenced.Contains("uploads/" + relative));
            if (hasReference)
                continue;

            var info = new FileInfo(file);
            candidates.Add(new SecureFileOrphan(file, info.Length, info.LastWriteTimeUtc));
        }

        return candidates.OrderBy(x => x.StoragePath, comparison).ToArray();
    }

    public static IReadOnlyCollection<string> GetStorageKeyAliases(string storageRoot, string path)
    {
        var key = TryNormalizeStorageKey(Path.GetFullPath(storageRoot), path);
        if (key is null)
            return Array.Empty<string>();

        var aliases = new HashSet<string>(OperatingSystem.IsWindows()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal)
        {
            key,
            "/" + key,
            key.Replace('/', '\\'),
            "\\" + key.Replace('/', '\\')
        };

        // Eski kayıtlarda aynı storage nesnesi mutlak disk yolu olarak tutulmuş
        // olabilir. Bu biçimleri de SQL tarafındaki normalize edilmiş karşılaştırma
        // için alias listesine ekle; DB'deki tüm yolları uygulamaya çekme.
        var absolutePath = Path.GetFullPath(Path.Combine(
            Path.GetFullPath(storageRoot), key.Replace('/', Path.DirectorySeparatorChar)));
        aliases.Add(absolutePath);
        aliases.Add(absolutePath.Replace('\\', '/'));
        aliases.Add(absolutePath.Replace('/', '\\'));

        if (key.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase))
        {
            var withoutPrefix = key["uploads/".Length..];
            aliases.Add(withoutPrefix);
            aliases.Add("/" + withoutPrefix);
            aliases.Add(withoutPrefix.Replace('/', '\\'));
            aliases.Add("/uploads/" + withoutPrefix);
        }

        return aliases;
    }

    private static string? TryNormalizeStorageKey(string storageRoot, string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        var normalized = path.Trim().Replace('\\', '/');
        if (normalized.Contains("://", StringComparison.Ordinal))
            return null;

        var trimmed = normalized.TrimStart('/');
        var hasStoragePrefix = trimmed.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase) ||
                               trimmed.StartsWith("Arsiv/", StringComparison.OrdinalIgnoreCase) ||
                               trimmed.StartsWith("Depo/", StringComparison.OrdinalIgnoreCase);
        if (!hasStoragePrefix && Path.IsPathRooted(normalized))
        {
            var fullPath = Path.GetFullPath(normalized);
            var relative = Path.GetRelativePath(storageRoot, fullPath);
            if (relative == ".." || relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal) || Path.IsPathRooted(relative))
                return null;
            normalized = relative.Replace('\\', '/');
        }

        normalized = normalized.TrimStart('/');
        if (!normalized.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase) &&
            !normalized.StartsWith("Arsiv/", StringComparison.OrdinalIgnoreCase) &&
            !normalized.StartsWith("Depo/", StringComparison.OrdinalIgnoreCase))
            normalized = "uploads/" + normalized;

        var root = storageRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var fullCandidate = Path.GetFullPath(Path.Combine(root, normalized.Replace('/', Path.DirectorySeparatorChar)));
        var relativeCandidate = Path.GetRelativePath(root, fullCandidate);
        if (relativeCandidate == ".." || relativeCandidate.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal) || Path.IsPathRooted(relativeCandidate))
            return null;

        return relativeCandidate.Replace('\\', '/');
    }

    private static IEnumerable<string> EnumerateFilesWithoutLinks(string root)
    {
        var pending = new Stack<string>();
        pending.Push(root);
        while (pending.Count > 0)
        {
            var directory = pending.Pop();
            foreach (var file in Directory.EnumerateFiles(directory))
            {
                if (!IsReparsePoint(file))
                    yield return file;
            }

            foreach (var child in Directory.EnumerateDirectories(directory))
            {
                if (!IsReparsePoint(child))
                    pending.Push(child);
            }
        }
    }

    private static bool IsReparsePoint(string path)
        => (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0;
}

public sealed record SecureFileOrphan(string StoragePath, long Size, DateTime LastWriteTimeUtc);
