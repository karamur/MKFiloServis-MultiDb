using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MKFiloServis.DataSync.Exporters;

/// <summary>
/// Hedef PostgreSQL şemasını MKFiloServis.Web uygulamasının kendi EF modeliyle hazırlar.
/// Web uygulamasını <c>--ensure-schema</c> anahtarıyla başsız çalıştırır; bağlantı dizesi
/// ortam değişkeniyle aktarılır. Böylece DataSync, uygulamanın ağır bağımlılıklarını
/// taşımadan uygulamanın açılışta ürettiği şemanın aynısını üretir.
/// </summary>
public sealed class WebSchemaProvisioner
{
    private const string Switch = "--ensure-schema";
    private const string ConnectionEnvironmentVariable = "MKFILO_SCHEMA_CONNECTION";
    private const string WebAssemblyName = "MKFiloServis.Web.dll";

    private readonly string _pgConnectionString;
    private readonly string? _webAppDirectory;
    private readonly Action<string> _progress;

    public WebSchemaProvisioner(string pgConnectionString, string? webAppDirectory, Action<string>? progress = null)
    {
        _pgConnectionString = pgConnectionString;
        _webAppDirectory = webAppDirectory;
        _progress = progress ?? (_ => { });
    }

    /// <summary>
    /// Kurulu/yayınlanmış Web uygulamasının klasörünü bulur. Kurulum düzeninde DataSync
    /// <c>C:\MKFiloServis\tools\datasync</c>, Web ise <c>C:\MKFiloServis\app</c> altındadır.
    /// </summary>
    public static string? LocateWebAppDirectory()
    {
        var baseDir = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
        var candidates = new List<string>
        {
            Path.GetFullPath(Path.Combine(baseDir, "..", "..", "app")),
            Path.GetFullPath(Path.Combine(baseDir, "..", "app")),
            Path.GetFullPath(Path.Combine(baseDir, "app")),
            Path.Combine(Path.GetPathRoot(baseDir) ?? @"C:\", "MKFiloServis", "app"),
        };

        // Kurulum kaydi (HKLM\SOFTWARE\MK Yazilim\MKFiloServis\InstallPath) dogrudan app klasorudur.
        var installPath = ReadInstallPathFromRegistry();
        if (!string.IsNullOrWhiteSpace(installPath))
            candidates.Insert(0, installPath);

        // Geliştirme ortamı: depo içinden çalışırken Web projesinin build çıktısı.
        var dir = new DirectoryInfo(baseDir);
        for (var i = 0; i < 6 && dir is not null; i++, dir = dir.Parent)
        {
            var webBin = Path.Combine(dir.FullName, "MKFiloServis.Web", "bin");
            if (Directory.Exists(webBin))
            {
                candidates.AddRange(Directory.EnumerateFiles(webBin, WebAssemblyName, SearchOption.AllDirectories)
                    .Select(Path.GetDirectoryName)
                    .Where(p => p is not null)
                    .OrderByDescending(p => File.GetLastWriteTimeUtc(Path.Combine(p!, WebAssemblyName)))
                    .Select(p => p!));
                break;
            }
        }

        return candidates.FirstOrDefault(IsWebAppDirectory);
    }

    public static bool IsWebAppDirectory(string? directory) =>
        !string.IsNullOrWhiteSpace(directory) && File.Exists(Path.Combine(directory, WebAssemblyName));

    public async Task RunAsync()
    {
        var webDir = _webAppDirectory;
        if (!IsWebAppDirectory(webDir))
            throw new InvalidOperationException(
                "MKFiloServis.Web uygulaması bulunamadı; hedef şema otomatik oluşturulamıyor. " +
                "Web uygulamasının kurulu olduğu klasörü (MKFiloServis.Web.dll içeren) seçin veya " +
                "uygulamayı bir kez çalıştırıp şemanın oluşmasını sağlayın.");

        var (fileName, arguments) = ResolveLauncher(webDir!);
        _progress($"▸ Hedef şema Web uygulaması modeliyle hazırlanıyor ({webDir})...");

        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = webDir,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        psi.Environment[ConnectionEnvironmentVariable] = _pgConnectionString;
        // Web açılış yapılandırmasının bu makinedeki dbsettings/appsettings dosyalarıyla karışmaması için.
        psi.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";
        psi.Environment["DOTNET_ENVIRONMENT"] = "Production";

        using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
        var errorLines = new List<string>();
        process.OutputDataReceived += (_, e) => { if (!string.IsNullOrWhiteSpace(e.Data)) _progress("    " + e.Data); };
        process.ErrorDataReceived += (_, e) =>
        {
            if (string.IsNullOrWhiteSpace(e.Data)) return;
            lock (errorLines) errorLines.Add(e.Data);
            _progress("    ! " + e.Data);
        };

        if (!process.Start())
            throw new InvalidOperationException("Web uygulaması şema modu başlatılamadı.");
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            string detail;
            lock (errorLines)
                detail = errorLines.FirstOrDefault(l => l.StartsWith("HATA", StringComparison.OrdinalIgnoreCase)) ?? errorLines.FirstOrDefault() ?? "";
            throw new InvalidOperationException(
                $"Hedef şema hazırlanamadı (çıkış kodu {process.ExitCode}). {detail}".TrimEnd());
        }

        _progress("▸ Hedef şema hazır.");
    }

    private static (string FileName, string Arguments) ResolveLauncher(string webDir)
    {
        var exe = Path.Combine(webDir, "MKFiloServis.Web.exe");
        if (File.Exists(exe))
            return (exe, Switch);

        // AppHost yoksa paylaşılan çalışma zamanı üzerinden.
        return ("dotnet", $"\"{Path.Combine(webDir, WebAssemblyName)}\" {Switch}");
    }

    private static string? ReadInstallPathFromRegistry()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\MK Yazilim\MKFiloServis");
            return key?.GetValue("InstallPath") as string;
        }
        catch
        {
            return null;
        }
    }
}
