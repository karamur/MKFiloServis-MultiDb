namespace MKFiloServis.Web.Helpers;

internal static class StorageFilePath
{
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

        return fullPath;
    }
}
