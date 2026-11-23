using MKFiloServis.Web.Helpers;

namespace MKFiloServis.Tests;

public sealed class PostgreSqlClientToolsTests
{
    [Theory]
    [InlineData("pg_dump (PostgreSQL) 18.6", 18, 6)]
    [InlineData("pg_dump (PostgreSQL) 16.4 (Ubuntu 16.4-1.pgdg22.04+1)", 16, 4)]
    [InlineData("pg_restore (PostgreSQL) 19beta1", 19, 0)]
    public void ParseVersion_reads_major_and_minor(string output, int major, int minor)
        => Assert.Equal(new Version(major, minor), PostgreSqlClientTools.ParseVersion(output));

    [Fact]
    public void Find_prefers_newest_installed_tool_over_directory_order()
    {
        var root = Path.Combine(Path.GetTempPath(), "mkfiloservis-pgtools-" + Guid.NewGuid().ToString("N"));
        var fileName = OperatingSystem.IsWindows() ? "pg_dump.exe" : "pg_dump";
        try
        {
            // 17 klasöründe araç yok; 18, eski sabit listede hiç yer almıyordu.
            var directories = new[] { "16", "17", "15", "18" }.Select(version => Path.Combine(root, version, "bin")).ToArray();
            foreach (var directory in directories)
                Directory.CreateDirectory(directory);
            foreach (var version in new[] { "16", "15", "18" })
                File.WriteAllBytes(Path.Combine(root, version, "bin", fileName), []);

            var found = PostgreSqlClientTools.Find("pg_dump", directories,
                path => new Version(int.Parse(Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(path))!)), 0));

            Assert.Equal(Path.Combine(root, "18", "bin", fileName), found);
            Assert.Null(PostgreSqlClientTools.Find("pg_restore", directories, _ => null));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Find_still_returns_a_tool_when_its_version_cannot_be_read()
    {
        var root = Path.Combine(Path.GetTempPath(), "mkfiloservis-pgtools-" + Guid.NewGuid().ToString("N"));
        var fileName = OperatingSystem.IsWindows() ? "psql.exe" : "psql";
        try
        {
            Directory.CreateDirectory(root);
            File.WriteAllBytes(Path.Combine(root, fileName), []);

            Assert.Equal(Path.Combine(root, fileName), PostgreSqlClientTools.Find("psql", [root], _ => null));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }
}
