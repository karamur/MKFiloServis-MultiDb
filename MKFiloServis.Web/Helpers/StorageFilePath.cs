namespace MKFiloServis.Web.Helpers;

internal static class StorageFilePath
{
    public static void DeleteIdempotently(string fullPath)
    {
        var parentPath = Path.GetDirectoryName(fullPath)
            ?? throw new InvalidOperationException("Dosya için üst klasör belirlenemedi.");
        try
        {
            _ = File.GetAttributes(parentPath);
        }
        catch (DirectoryNotFoundException)
        {
            return;
        }

        // File.Delete is idempotent. Avoid File.Exists, which may hide IO errors.
        File.Delete(fullPath);
    }

    public static string Resolve(string storageRoot, string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath) || Path.IsPathRooted(relativePath))
            throw new InvalidOperationException("Dosya yolu depolama klasörüne göreli olmalıdır.");

        var root = Path.GetFullPath(storageRoot);
        var fullPath = Path.GetFullPath(Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        var pathFromRoot = Path.GetRelativePath(root, fullPath);

        // Klasör adı öneki yeterli değildir: uploads-extra, uploads altında değildir.
        if (pathFromRoot == "." || pathFromRoot == ".." ||
            pathFromRoot.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal) ||
            Path.IsPathRooted(pathFromRoot))
            throw new InvalidOperationException("Dosya yolu izin verilen depolama klasörünün dışında.");

        EnsureNoSymbolicLinkTraversal(root, fullPath);

        return fullPath;
    }

    private static void EnsureNoSymbolicLinkTraversal(string root, string fullPath)
    {
        var relativePath = Path.GetRelativePath(root, fullPath);
        var current = root;
        foreach (var segment in relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
        {
            if (segment is "" or ".")
                continue;

            current = Path.Combine(current, segment);
            FileSystemInfo info = Directory.Exists(current) ? new DirectoryInfo(current) : new FileInfo(current);
            if (info.LinkTarget is not null)
                throw new InvalidOperationException("Dosya yolu depolama klasörü içindeki sembolik bağlantıdan geçemez.");
        }
    }
}
