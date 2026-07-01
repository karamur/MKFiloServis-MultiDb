using System.Diagnostics;
using System.Net.Http;
using System.Text.RegularExpressions;
using Microsoft.Playwright;

Console.OutputEncoding = System.Text.Encoding.UTF8;

var baseUrl = args.FirstOrDefault()
    ?? Environment.GetEnvironmentVariable("CRMFILO_BASE_URL")
    ?? "http://127.0.0.1:5190";
var username = Environment.GetEnvironmentVariable("CRMFILO_TEST_USER") ?? "admin";
var password = Environment.GetEnvironmentVariable("CRMFILO_TEST_PASSWORD") ?? "admin123";

var runner = new DestekSmokeRunner(baseUrl, username, password);
return await runner.RunAsync();

internal sealed class DestekSmokeRunner(string baseUrl, string username, string password)
{
    private readonly string _baseUrl = baseUrl.TrimEnd('/');
    private readonly string _username = username;
    private readonly string _password = password;
    private readonly List<SmokeCheckResult> _results = [];

    public async Task<int> RunAsync()
    {
        Console.WriteLine($"[INFO] Smoke test base URL: {_baseUrl}");

        using var host = new LocalWebAppHost(_baseUrl);
        host.Start();

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });

        var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            IgnoreHTTPSErrors = true,
            ViewportSize = new ViewportSize { Width = 1600, Height = 1200 }
        });

        var page = await context.NewPageAsync();

        await ExecuteAsync("Anonim kullanıcı destek listesinde login ekranına yönlenir", () => VerifyAnonymousRedirectAsync(page));
        await ExecuteAsync("Geçerli kullanıcı ile giriş yapılabilir", () => LoginAsync(page));
        await ExecuteAsync("Destek listesi açılır", () => VerifySupportListAsync(page));
        await ExecuteAsync("Destek detay ekranı açılır", () => VerifySupportDetailAsync(page));
        await ExecuteAsync("Bilgi bankası açılır", () => VerifyKnowledgeBaseAsync(page));
        await ExecuteAsync("Destek ayarları açılır", () => VerifySettingsAsync(page));

        Console.WriteLine();
        Console.WriteLine("=== Smoke Test Özeti ===");
        foreach (var result in _results)
        {
            Console.WriteLine($"[{result.Status}] {result.Name}{(string.IsNullOrWhiteSpace(result.Message) ? string.Empty : $" - {result.Message}")}");
        }

        return _results.Any(r => r.Status == "FAIL") ? 1 : 0;
    }

    private async Task ExecuteAsync(string name, Func<Task> action)
    {
        try
        {
            await action();
            _results.Add(new SmokeCheckResult(name, "PASS", null));
            Console.WriteLine($"[PASS] {name}");
        }
        catch (SmokeSkippedException ex)
        {
            _results.Add(new SmokeCheckResult(name, "SKIP", ex.Message));
            Console.WriteLine($"[SKIP] {name} - {ex.Message}");
        }
        catch (Exception ex)
        {
            var message = ex.GetBaseException().Message;
            _results.Add(new SmokeCheckResult(name, "FAIL", message));
            Console.WriteLine($"[FAIL] {name} - {message}");
        }
    }

    private async Task VerifyAnonymousRedirectAsync(IPage page)
    {
        await page.GotoAsync($"{_baseUrl}/destek-talepleri");
        await page.WaitForURLAsync(new Regex(@".*/login.*", RegexOptions.IgnoreCase));
        await page.WaitForSelectorAsync("#kullaniciAdi");
    }

    private async Task LoginAsync(IPage page)
    {
        await page.GotoAsync($"{_baseUrl}/login");
        await page.FillAsync("#kullaniciAdi", _username);
        await page.FillAsync("#sifre", _password);
        await page.ClickAsync("button.btn-login");
        await page.WaitForURLAsync(url => !url.Contains("/login", StringComparison.OrdinalIgnoreCase), new PageWaitForURLOptions
        {
            Timeout = 20000
        });
        await page.WaitForSelectorAsync("#logout-button", new PageWaitForSelectorOptions { Timeout = 20000 });
    }

    private async Task VerifySupportListAsync(IPage page)
    {
        await page.GotoAsync($"{_baseUrl}/destek-talepleri");
        await WaitForPageSettledAsync(page);
        await EnsureAnyVisibleAsync(page,
            "text=Destek Talepleri",
            "text=Hiç destek talebi bulunamadı.",
            "text=Destek modülü verileri yüklenirken sorun oluştu.");
    }

    private async Task VerifySupportDetailAsync(IPage page)
    {
        await page.GotoAsync($"{_baseUrl}/destek-talepleri");
        await WaitForPageSettledAsync(page);

        var detailLinks = page.Locator("a[href^='destek-talepleri/'], a[href^='/destek-talepleri/']");
        var linkCount = await detailLinks.CountAsync();
        if (linkCount == 0)
        {
            throw new SmokeSkippedException("Detay ekranı için listede talep kaydı bulunamadı.");
        }

        await detailLinks.First.ClickAsync();
        await page.WaitForURLAsync(new Regex(@".*/destek-talepleri/\d+$", RegexOptions.IgnoreCase), new PageWaitForURLOptions
        {
            Timeout = 20000
        });
        await WaitForPageSettledAsync(page);
        await EnsureAnyVisibleAsync(page,
            "text=Geri",
            "text=Talep bulunamadı.",
            "text=Destek talebi verileri yüklenirken sorun oluştu.");
    }

    private async Task VerifyKnowledgeBaseAsync(IPage page)
    {
        await page.GotoAsync($"{_baseUrl}/destek-talepleri/bilgi-bankasi");
        await WaitForPageSettledAsync(page);
        await EnsureAnyVisibleAsync(page,
            "text=Bilgi Bankası",
            "text=Bilgi bankası verileri yüklenirken sorun oluştu.");
    }

    private async Task VerifySettingsAsync(IPage page)
    {
        await page.GotoAsync($"{_baseUrl}/destek-talepleri/ayarlar");
        await WaitForPageSettledAsync(page);
        await EnsureAnyVisibleAsync(page,
            "text=Destek Ayarları",
            "text=Destek ayarları yüklenirken sorun oluştu.");
    }

    private static async Task WaitForPageSettledAsync(IPage page)
    {
        await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        await page.WaitForTimeoutAsync(1200);
    }

    private static async Task EnsureAnyVisibleAsync(IPage page, params string[] selectors)
    {
        var timeoutAt = DateTime.UtcNow.AddSeconds(20);

        while (DateTime.UtcNow < timeoutAt)
        {
            foreach (var selector in selectors)
            {
                if (await page.Locator(selector).First.IsVisibleAsync())
                {
                    return;
                }
            }

            await page.WaitForTimeoutAsync(250);
        }

        throw new InvalidOperationException($"Beklenen görünüm bulunamadı. Kontroller: {string.Join(", ", selectors)}");
    }
}

internal sealed record SmokeCheckResult(string Name, string Status, string? Message);

internal sealed class SmokeSkippedException(string message) : Exception(message);

internal sealed class LocalWebAppHost(string baseUrl) : IDisposable
{
    private readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(2) };
    private readonly string _baseUrl = baseUrl.TrimEnd('/');
    private Process? _process;
    private bool _ownsProcess;

    public string GetUrl(string relativePath) => $"{_baseUrl}{relativePath}";

    public void Start()
    {
        if (IsApplicationResponsive())
        {
            return;
        }

        var projectPath = FindWebProjectPath();
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{projectPath}\" --no-launch-profile --urls {_baseUrl}",
            WorkingDirectory = Path.GetDirectoryName(projectPath)!,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";

        _process = Process.Start(startInfo) ?? throw new InvalidOperationException("Web uygulaması başlatılamadı.");
        _ownsProcess = true;

        _ = Task.Run(() => DrainAsync(_process.StandardOutput));
        _ = Task.Run(() => DrainAsync(_process.StandardError));

        var started = SpinWait.SpinUntil(IsApplicationResponsive, TimeSpan.FromSeconds(90));
        if (!started)
        {
            throw new TimeoutException("Web uygulaması belirlenen sürede hazır olmadı.");
        }
    }

    public void Dispose()
    {
        _httpClient.Dispose();

        if (!_ownsProcess || _process is null)
        {
            return;
        }

        try
        {
            if (!_process.HasExited)
            {
                _process.Kill(entireProcessTree: true);
                _process.WaitForExit(5000);
            }
        }
        catch
        {
        }
        finally
        {
            _process.Dispose();
        }
    }

    private bool IsApplicationResponsive()
    {
        try
        {
            using var response = _httpClient.GetAsync(GetUrl("/login")).GetAwaiter().GetResult();
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private static async Task DrainAsync(StreamReader reader)
    {
        while (await reader.ReadLineAsync() is not null) { }
    }

    private static string FindWebProjectPath()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, "MKFiloServis.Web", "MKFiloServis.Web.csproj");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        throw new FileNotFoundException("MKFiloServis.Web.csproj bulunamadı.");
    }
}


