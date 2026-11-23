using MKFiloServis.DataSync.Exporters;
using System;
using System.Threading.Tasks;

namespace MKFiloServis.DataSync;

internal static class CliRunner
{
    public static async Task<int> RunAsync(string[] args)
    {
        try
        {
            AllocConsole();

            if (args[0] is "--help" or "-h" or "/?")
            {
                PrintUsage();
                return 0;
            }

            string? command = args[0].ToLowerInvariant();
            string? source = GetArg(args, "--source");
            string? target = GetArg(args, "--target");

            return command switch
            {
                "export" => await ExportAsync(source, target),
                "import" => await ImportAsync(source, target, GetArg(args, "--web-app")),
                "inventory" => await LegacyInventoryRunner.RunAsync(
                    source, GetArg(args, "--provider"), GetArg(args, "--output"),
                    GetArg(args, "--source-env")),
                _ => Fail($"Bilinmeyen komut: {command}")
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"HATA: {ex.Message}");
            Console.Error.WriteLine(ex.ToString());
            return 1;
        }
    }

    private static async Task<int> ExportAsync(string? source, string? target)
    {
        if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(target))
        {
            return Fail("--source ve --target gerekli.");
        }

        var exporter = new PostgresToSqliteExporter(
            source!,
            target!,
            progress: msg => Console.WriteLine(msg));

        await exporter.RunAsync();
        Console.WriteLine("✔ Aktarim tamamlandi.");
        return 0;
    }

    private static async Task<int> ImportAsync(string? source, string? target, string? webAppDirectory)
    {
        if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(target))
        {
            return Fail("--source ve --target gerekli.");
        }

        var webDir = webAppDirectory ?? WebSchemaProvisioner.LocateWebAppDirectory();
        if (webDir is not null) Console.WriteLine($"▸ Sema kaynagi (Web uygulamasi): {webDir}");
        else Console.WriteLine("▸ Web uygulamasi bulunamadi; hedefte eksik tablo olursa aktarim durur (--web-app ile belirtin).");

        var importer = new SqliteToPostgresImporter(
            source!,
            target!,
            progress: msg => Console.WriteLine(msg),
            ensureSchema: webDir is null ? null : () => new WebSchemaProvisioner(target!, webDir, msg => Console.WriteLine(msg)).RunAsync());

        await importer.RunAsync();
        Console.WriteLine("✔ Aktarim tamamlandi.");
        return 0;
    }

    private static string? GetArg(string[] args, string name)
    {
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i].Equals(name, StringComparison.OrdinalIgnoreCase))
                return args[i + 1];
        return null;
    }

    private static int Fail(string message)
    {
        Console.Error.WriteLine(message);
        PrintUsage();
        return 2;
    }

    private static void PrintUsage()
    {
        Console.WriteLine("""
MKFiloServis.DataSync — PostgreSQL -> SQLite veri aktarim araci

KULLANIM:
  MKFiloServis.DataSync.exe                                  (UI modu)
  MKFiloServis.DataSync.exe export --source "<PG>" --target "<sqlite.db>"
  MKFiloServis.DataSync.exe import --source "<sqlite.db>" --target "<PG>" [--web-app "<MKFiloServis.Web klasoru>"]
  MKFiloServis.DataSync.exe inventory --provider sqlite --source "<sqlite.db>" [--output "<rapor.json>"]
  MKFiloServis.DataSync.exe inventory --provider postgresql --source-env "<baglanti-degiskeni>" [--output "<rapor.json>"]

ORNEK:
  MKFiloServis.DataSync.exe export ^
    --source "Host=localhost;Port=5432;Database=MKFiloServis;Username=<kullanici>;Password=<parola>" ^
    --target "C:\MKFiloServis\koa.db"
""");
    }

    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    private static extern bool AllocConsole();
}


