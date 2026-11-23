using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace MKFiloServis.Web.Helpers;

/// <summary>pg_dump, pg_restore ve psql araçlarını bu makinede kurulu sürümler arasından bulur.</summary>
internal static class PostgreSqlClientTools
{
    /// <summary>
    /// En yeni kurulu sürümdeki aracı döndürür. pg_dump kendinden yeni bir sunucuyu yedekleyemez
    /// ama eski sunucuları yedekleyebilir; bu yüzden sabit sürüm listesi yerine en yeni kurulum seçilir.
    /// </summary>
    public static string? Find(string toolName) => Find(toolName, CandidateDirectories(), ReadVersion);

    internal static string? Find(string toolName, IEnumerable<string> directories, Func<string, Version?> readVersion)
    {
        var fileName = OperatingSystem.IsWindows() ? toolName + ".exe" : toolName;
        return directories
            .Select(directory => Path.Combine(directory, fileName))
            .Where(File.Exists)
            .Distinct(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal)
            .Select((path, order) => (path, order, version: readVersion(path) ?? new Version(0, 0)))
            .OrderByDescending(candidate => candidate.version)
            .ThenBy(candidate => candidate.order)
            .Select(candidate => candidate.path)
            .FirstOrDefault();
    }

    internal static Version? ParseVersion(string versionOutput)
    {
        // Örnek: "pg_dump (PostgreSQL) 18.6" veya "pg_dump (PostgreSQL) 16.4 (Ubuntu 16.4-1)".
        var match = Regex.Match(versionOutput, @"\)\s*(\d+)(?:\.(\d+))?");
        return match.Success
            ? new Version(int.Parse(match.Groups[1].Value), match.Groups[2].Success ? int.Parse(match.Groups[2].Value) : 0)
            : null;
    }

    private static IEnumerable<string> CandidateDirectories()
    {
        var roots = OperatingSystem.IsWindows()
            ? new[] { "ProgramW6432", "ProgramFiles", "ProgramFiles(x86)" }
                .Select(Environment.GetEnvironmentVariable)
                .Where(root => !string.IsNullOrWhiteSpace(root))
                .Select(root => Path.Combine(root!, "PostgreSQL"))
            : ["/usr/lib/postgresql", "/usr"];
        foreach (var root in roots)
            foreach (var installation in SubDirectories(root))
                yield return Path.Combine(installation, "bin");

        foreach (var directory in (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
                     .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            yield return directory;

        if (!OperatingSystem.IsWindows())
        {
            yield return "/usr/bin";
            yield return "/usr/local/bin";
        }
    }

    private static string[] SubDirectories(string root)
    {
        try { return Directory.Exists(root) ? Directory.GetDirectories(root) : []; }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { return []; }
    }

    private static Version? ReadVersion(string path)
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo(path, "--version")
            {
                UseShellExecute = false, RedirectStandardOutput = true, CreateNoWindow = true
            });
            if (process is null) return null;
            var output = process.StandardOutput.ReadToEndAsync();
            if (!process.WaitForExit(5000))
            {
                process.Kill();
                return null;
            }
            return ParseVersion(output.GetAwaiter().GetResult());
        }
        catch (Exception ex) when (ex is Win32Exception or IOException or InvalidOperationException)
        {
            return null;
        }
    }
}
