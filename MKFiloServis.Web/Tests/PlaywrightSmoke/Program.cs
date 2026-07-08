using System.Diagnostics;
using System.Net.Http;
using System.Text.RegularExpressions;
using Microsoft.Playwright;

Console.OutputEncoding = System.Text.Encoding.UTF8;

var baseUrl = args.FirstOrDefault(a => !a.StartsWith("--", StringComparison.Ordinal))
    ?? Environment.GetEnvironmentVariable("CRMFILO_BASE_URL")
    ?? "http://127.0.0.1:5190";
var username = Environment.GetEnvironmentVariable("CRMFILO_TEST_USER") ?? "admin";
var password = Environment.GetEnvironmentVariable("CRMFILO_TEST_PASSWORD") ?? "admin123";
var allowMutationArg = args.Any(a => string.Equals(a, "--allow-mutation", StringComparison.OrdinalIgnoreCase));
var allowMutationEnv = string.Equals(Environment.GetEnvironmentVariable("CRMFILO_SMOKE_ALLOW_MUTATION"), "true", StringComparison.OrdinalIgnoreCase);
var prepareDemoArg = args.Any(a => string.Equals(a, "--prepare-demo-data", StringComparison.OrdinalIgnoreCase));
var prepareDemoEnv = string.Equals(Environment.GetEnvironmentVariable("CRMFILO_SMOKE_PREPARE_DEMO"), "true", StringComparison.OrdinalIgnoreCase);

var yearArg = args.FirstOrDefault(a => a.StartsWith("--hakedis-year=", StringComparison.OrdinalIgnoreCase));
var monthArg = args.FirstOrDefault(a => a.StartsWith("--hakedis-month=", StringComparison.OrdinalIgnoreCase));
var tipArg = args.FirstOrDefault(a => a.StartsWith("--hakedis-tip=", StringComparison.OrdinalIgnoreCase));

var yearRaw = yearArg?.Split('=', 2).ElementAtOrDefault(1) ?? Environment.GetEnvironmentVariable("CRMFILO_SMOKE_HAKEDIS_YEAR");
var monthRaw = monthArg?.Split('=', 2).ElementAtOrDefault(1) ?? Environment.GetEnvironmentVariable("CRMFILO_SMOKE_HAKEDIS_MONTH");
var tipRaw = tipArg?.Split('=', 2).ElementAtOrDefault(1) ?? Environment.GetEnvironmentVariable("CRMFILO_SMOKE_HAKEDIS_TIP");

var targetYear = int.TryParse(yearRaw, out var parsedYear) && parsedYear >= 2000 ? parsedYear : DateTime.Today.Year;
var targetMonth = int.TryParse(monthRaw, out var parsedMonth) && parsedMonth is >= 1 and <= 12 ? parsedMonth : DateTime.Today.Month;
var targetTip = string.IsNullOrWhiteSpace(tipRaw) ? "Kurum" : tipRaw.Trim();

var runner = new DestekSmokeRunner(baseUrl, username, password, allowMutationArg || allowMutationEnv, prepareDemoArg || prepareDemoEnv, targetYear, targetMonth, targetTip);
return await runner.RunAsync();

internal sealed class DestekSmokeRunner(string baseUrl, string username, string password, bool allowStateMutations, bool prepareDemoData, int targetYear, int targetMonth, string targetTip)
{
    private readonly string _baseUrl = baseUrl.TrimEnd('/');
    private readonly string _username = username;
    private readonly string _password = password;
    private readonly bool _allowStateMutations = allowStateMutations;
    private readonly bool _prepareDemoData = prepareDemoData;
    private readonly int _targetYear = targetYear;
    private readonly int _targetMonth = targetMonth;
    private readonly string _targetTip = targetTip;
    private int? _newHakedisMinIdExclusive;
    private readonly List<SmokeCheckResult> _results = [];

    public async Task<int> RunAsync()
    {
        Console.WriteLine($"[INFO] Smoke test base URL: {_baseUrl}");
        Console.WriteLine($"[INFO] State-mutation senaryoları: {(_allowStateMutations ? "Açık" : "Kapalı")}");
        Console.WriteLine($"[INFO] Demo veri hazırlığı: {(_prepareDemoData ? "Açık" : "Kapalı")}");
        Console.WriteLine($"[INFO] Deterministik hakediş filtresi: Tip={_targetTip}, Yıl={_targetYear}, Ay={_targetMonth}");

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
        await ExecuteAsync("Opsiyonel demo veri hazırlığı çalıştırılır", () => PrepareDemoDataIfRequestedAsync(page));
        await ExecuteAsync("Demo seed sonrası hakediş dağılımı doğrulanır (deterministik)", () => VerifyDeterministicHakedisDistributionAsync(page));
        await ExecuteAsync("Destek listesi açılır", () => VerifySupportListAsync(page));
        await ExecuteAsync("Destek detay ekranı açılır", () => VerifySupportDetailAsync(page));
        await ExecuteAsync("Bilgi bankası açılır", () => VerifyKnowledgeBaseAsync(page));
        await ExecuteAsync("Destek ayarları açılır", () => VerifySettingsAsync(page));
        await ExecuteAsync("Operasyonel puantaj ekranı açılır", () => VerifyOperationalPuantajAsync(page));
        await ExecuteAsync("Operasyonel puantaj filtreleri çalışır", () => VerifyOperationalPuantajFiltersAsync(page));
        await ExecuteAsync("Operasyonel puantaj toplu aksiyon butonları varsayılan durumda pasiftir", () => VerifyOperationalPuantajBulkActionsDefaultStateAsync(page));
        await ExecuteAsync("Operasyonel puantaj toplu sefer aksiyonu seçimle çalışır", () => VerifyOperationalPuantajBulkApplyFlowAsync(page));
        await ExecuteAsync("Operasyonel puantaj seçimle kaydet butonu aktifleşir", () => VerifyOperationalPuantajBulkSaveButtonEnableFlowAsync(page));
        await ExecuteAsync("Operasyonel hakediş ekranı açılır", () => VerifyOperationalHakedisAsync(page));
        await ExecuteAsync("Operasyonel hakediş dönem validasyonu çalışır", () => VerifyOperationalHakedisPeriodValidationAsync(page));
        await ExecuteAsync("Operasyonel hakediş toplu aksiyon butonları varsayılan durumda pasiftir", () => VerifyOperationalHakedisBulkActionsDefaultStateAsync(page));
        await ExecuteAsync("Operasyonel hakediş tümünü seç aksiyonu çalışır", () => VerifyOperationalHakedisSelectAllFlowAsync(page));
        await ExecuteAsync("Operasyonel hakedişte seçimle en az bir toplu aksiyon aktifleşir", () => VerifyOperationalHakedisBulkActionEnableFlowAsync(page));
        await ExecuteAsync("Operasyonel hakediş toplu aksiyon state-transition akışı (opsiyonel)", () => VerifyOperationalHakedisBulkStateTransitionFlowAsync(page));

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
        await WaitForPageSettledAsync(page);
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

    private async Task VerifyDeterministicHakedisDistributionAsync(IPage page)
    {
        if (!_prepareDemoData)
        {
            throw new SmokeSkippedException("Deterministik dağılım kontrolü demo seed kapalıyken atlanır.");
        }

        await page.GotoAsync($"{_baseUrl}/operasyonel-hakedis");
        await WaitForPageSettledAsync(page);

        var hedefYil = _targetYear.ToString();
        var hedefAy = _targetMonth.ToString();

        var yilInput = page.Locator("input[type='number']").First;
        var ayInput = page.Locator("input[type='number']").Nth(1);
        var tipSelect = page.Locator("select.form-select").First;

        await yilInput.FillAsync(hedefYil);
        await ayInput.FillAsync(hedefAy);
        await tipSelect.SelectOptionAsync(new[] { _targetTip });

        await page.ClickAsync("button:has-text('Listele')");
        await WaitForPageSettledAsync(page);

        var oncekiIds = await ReadHakedisIdsAsync(page);
        var oncekiMaxId = oncekiIds.Count > 0 ? oncekiIds.Max() : 0;

        await page.ClickAsync("button:has-text('Toplu Hakediş Üret')");
        await EnsureAnyVisibleAsync(page,
            "text=Toplu üretim tamamlandı.",
            "text=Toplu üretim sırasında");

        var hataMesaji = page.Locator(".alert-danger").First;
        if (await hataMesaji.IsVisibleAsync())
        {
            var hataText = (await hataMesaji.InnerTextAsync()).Trim();
            throw new InvalidOperationException($"Toplu Hakediş Üret adımı hata döndü: {hataText}");
        }

        var basariMesaji = page.Locator(".alert-success").First;
        var basariText = await basariMesaji.InnerTextAsync();
        var uretilenMatch = Regex.Match(basariText, @"Üretilen:\s*(\d+)", RegexOptions.IgnoreCase);
        if (!uretilenMatch.Success)
        {
            throw new InvalidOperationException($"Toplu Hakediş Üret sonucu parse edilemedi: {basariText}");
        }

        var uretilenAdet = int.Parse(uretilenMatch.Groups[1].Value);
        if (uretilenAdet <= 0)
        {
            throw new InvalidOperationException($"Deterministik dağılım için Toplu Hakediş Üret adımında yeni kayıt bekleniyordu. Üretilen: {uretilenAdet}");
        }

        await page.ClickAsync("button:has-text('Listele')");
        await WaitForPageSettledAsync(page);

        if (await page.Locator("text=Kayıt bulunamadı").First.IsVisibleAsync())
        {
            throw new InvalidOperationException($"Demo seed sonrası {_targetTip} + {hedefYil}/{hedefAy} filtresinde Toplu Hakediş Üret sonrası kayıt bulunamadı.");
        }

        var sonrakiIds = await ReadHakedisIdsAsync(page);
        if (uretilenAdet > 0 && !sonrakiIds.Any(id => id > oncekiMaxId))
        {
            throw new InvalidOperationException($"Toplu Hakediş Üret 'Üretilen={uretilenAdet}' döndürdü ancak {_targetTip} + {hedefYil}/{hedefAy} filtresinde yeni hakediş ID'si bulunamadı.");
        }

        _newHakedisMinIdExclusive = oncekiMaxId;

        var durumBadges = page.Locator("tbody tr td:nth-child(9) span.badge");
        var badgeCount = await durumBadges.CountAsync();
        if (badgeCount == 0)
        {
            throw new InvalidOperationException($"{_targetTip} + {hedefYil}/{hedefAy} filtresinde durum badge verisi üretmedi; dağılım doğrulanamadı.");
        }

        var taslakCount = 0;
        var onayliCount = 0;

        for (var i = 0; i < badgeCount; i++)
        {
            var durumText = (await durumBadges.Nth(i).InnerTextAsync()).Trim();
            if (string.Equals(durumText, "Taslak", StringComparison.OrdinalIgnoreCase))
            {
                taslakCount++;
            }
            else if (string.Equals(durumText, "Onaylandi", StringComparison.OrdinalIgnoreCase))
            {
                onayliCount++;
            }
        }

        if (taslakCount == 0 && onayliCount == 0)
        {
            throw new InvalidOperationException($"{_targetTip} + {hedefYil}/{hedefAy} filtresinde Taslak/Onaylandi durumları bulunamadı.");
        }

        Console.WriteLine($"[INFO] Demo seed dağılım kontrolü ({_targetTip}/{hedefYil}-{hedefAy}, Üretilen={uretilenAdet}, MaxIdÖnce={oncekiMaxId}, SatırSonra={sonrakiIds.Count}): Taslak={taslakCount}, Onaylandi={onayliCount}, Badge={badgeCount}");
    }

    private static async Task<List<int>> ReadHakedisIdsAsync(IPage page)
    {
        var idCells = page.Locator("tbody tr td:nth-child(2)");
        var count = await idCells.CountAsync();
        var result = new List<int>(count);

        for (var i = 0; i < count; i++)
        {
            var text = (await idCells.Nth(i).InnerTextAsync()).Trim();
            if (int.TryParse(text, out var id) && id > 0)
            {
                result.Add(id);
            }
        }

        return result;
    }

    private async Task PrepareDemoDataIfRequestedAsync(IPage page)
    {
        if (!_prepareDemoData)
        {
            throw new SmokeSkippedException("Demo veri hazırlığı kapalı. Çalıştırmak için CRMFILO_SMOKE_PREPARE_DEMO=true veya --prepare-demo-data kullanın.");
        }

        await page.GotoAsync($"{_baseUrl}/admin/test");
        await WaitForPageSettledAsync(page);

        await EnsureAnyVisibleAsync(page,
            "text=Guvenli Test Modu",
            "text=Test modu kapali. Canli veri korunuyor.",
            "text=TEST AKTIF!");

        var seedButton = page.Locator("button:has-text('Veri Tabanini Sifirla + Demo Ekle')").First;
        await seedButton.ClickAsync();

        await EnsureAnyVisibleAsync(page,
            "text=Database sıfırlandı ve",
            "text=Demo veri ekleme başarısız:",
            "text=ResetAndSeed başarısız:");
    }

    private async Task VerifySupportDetailAsync(IPage page)
    {
        await page.GotoAsync($"{_baseUrl}/destek-talepleri");
        await WaitForPageSettledAsync(page);

        var detailLinks = page.Locator("a[href*='destek-talepleri/']");
        var linkCount = await detailLinks.CountAsync();
        if (linkCount == 0)
        {
            throw new SmokeSkippedException("Detay ekranı için listede talep kaydı bulunamadı.");
        }

        string? detailHref = null;
        for (var i = 0; i < linkCount; i++)
        {
            var href = await detailLinks.Nth(i).GetAttributeAsync("href");
            if (!string.IsNullOrWhiteSpace(href) && Regex.IsMatch(href, @"destek-talepleri/\d+$", RegexOptions.IgnoreCase))
            {
                detailHref = href;
                break;
            }
        }

        if (string.IsNullOrWhiteSpace(detailHref))
        {
            throw new SmokeSkippedException("Detay ekranı için uygun talep bağlantısı bulunamadı.");
        }

        var normalizedHref = detailHref.StartsWith("http", StringComparison.OrdinalIgnoreCase)
            ? detailHref
            : detailHref.StartsWith("/", StringComparison.Ordinal)
                ? $"{_baseUrl}{detailHref}"
                : $"{_baseUrl}/{detailHref}";

        await page.GotoAsync(normalizedHref);
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

    private async Task VerifyOperationalPuantajAsync(IPage page)
    {
        await page.GotoAsync($"{_baseUrl}/operasyon/puantaj");
        await WaitForPageSettledAsync(page);
        await EnsureAnyVisibleAsync(page,
            "text=Operasyonel Puantaj",
            "text=Bugün için satırları oluştur",
            "text=Aktif firma bulunamadı. Lütfen önce firma seçiniz.");
    }

    private async Task VerifyOperationalPuantajFiltersAsync(IPage page)
    {
        await page.GotoAsync($"{_baseUrl}/operasyon/puantaj");
        await WaitForPageSettledAsync(page);

        var searchInput = page.Locator("input[placeholder='Arama yazın...']");
        await searchInput.WaitForAsync();
        await searchInput.FillAsync("demo-arama");

        var clearButton = page.Locator("button:has-text('Temizle')").First;
        await clearButton.ClickAsync();

        var currentValue = await searchInput.InputValueAsync();
        if (!string.IsNullOrEmpty(currentValue))
        {
            throw new InvalidOperationException("Operasyonel puantaj filtre temizleme işlemi arama alanını sıfırlamadı.");
        }
    }

    private async Task VerifyOperationalHakedisAsync(IPage page)
    {
        await page.GotoAsync($"{_baseUrl}/operasyonel-hakedis");
        await WaitForPageSettledAsync(page);
        await EnsureAnyVisibleAsync(page,
            "text=Operasyonel Hakediş",
            "text=Toplu Hakediş Üret",
            "text=Geçersiz dönem seçimi.");
    }

    private async Task VerifyOperationalHakedisPeriodValidationAsync(IPage page)
    {
        await page.GotoAsync($"{_baseUrl}/operasyonel-hakedis");
        await WaitForPageSettledAsync(page);

        var ayInput = page.Locator("input[type='number']").Nth(1);
        await ayInput.FillAsync("13");
        await page.ClickAsync("button:has-text('Listele')");

        await EnsureAnyVisibleAsync(page, "text=Geçersiz dönem seçimi.");
    }

    private async Task VerifyOperationalPuantajBulkActionsDefaultStateAsync(IPage page)
    {
        await page.GotoAsync($"{_baseUrl}/operasyon/puantaj");
        await WaitForPageSettledAsync(page);

        var topluSeferUygulaButton = page.Locator("button:has-text('Toplu Sefer Uygula')").First;
        var secilenleriKaydetButton = page.Locator("button:has-text('Seçilenleri Kaydet')").First;

        if (await topluSeferUygulaButton.IsEnabledAsync())
        {
            throw new InvalidOperationException("Operasyonel puantaj ekranında toplu sefer uygula butonu başlangıçta pasif olmalı.");
        }

        if (await secilenleriKaydetButton.IsEnabledAsync())
        {
            throw new InvalidOperationException("Operasyonel puantaj ekranında seçilenleri kaydet butonu başlangıçta pasif olmalı.");
        }
    }

    private async Task VerifyOperationalHakedisBulkActionsDefaultStateAsync(IPage page)
    {
        await page.GotoAsync($"{_baseUrl}/operasyonel-hakedis");
        await WaitForPageSettledAsync(page);

        var topluOnaylaButton = page.Locator("button:has-text('Toplu Onayla')").First;
        var topluFaturalaButton = page.Locator("button:has-text('Toplu Faturala')").First;
        var topluSilButton = page.Locator("button:has-text('Toplu Sil')").First;

        if (await topluOnaylaButton.IsEnabledAsync())
        {
            throw new InvalidOperationException("Operasyonel hakediş ekranında toplu onayla butonu başlangıçta pasif olmalı.");
        }

        if (await topluFaturalaButton.IsEnabledAsync())
        {
            throw new InvalidOperationException("Operasyonel hakediş ekranında toplu faturalama butonu başlangıçta pasif olmalı.");
        }

        if (await topluSilButton.IsEnabledAsync())
        {
            throw new InvalidOperationException("Operasyonel hakediş ekranında toplu sil butonu başlangıçta pasif olmalı.");
        }
    }

    private async Task VerifyOperationalPuantajBulkApplyFlowAsync(IPage page)
    {
        await page.GotoAsync($"{_baseUrl}/operasyon/puantaj");
        await WaitForPageSettledAsync(page);

        if (await page.Locator("text=Aktif firma bulunamadı. Lütfen önce firma seçiniz.").First.IsVisibleAsync())
        {
            throw new SmokeSkippedException("Operasyonel puantaj için aktif firma bulunamadığından toplu aksiyon akışı atlandı.");
        }

        var rowCheckboxes = page.Locator("tbody input.form-check-input");
        var rowCount = await rowCheckboxes.CountAsync();
        if (rowCount == 0)
        {
            throw new SmokeSkippedException("Operasyonel puantajda seçim yapılacak satır bulunamadı.");
        }

        await rowCheckboxes.First.ClickAsync();

        var seferInput = page.Locator("input[placeholder='Sefer']").First;
        await seferInput.FillAsync("1.5");

        var topluSeferUygulaButton = page.Locator("button:has-text('Toplu Sefer Uygula')").First;
        if (!await topluSeferUygulaButton.IsEnabledAsync())
        {
            throw new InvalidOperationException("Operasyonel puantajda seçim + sefer değeri sonrası toplu sefer butonu aktifleşmedi.");
        }

        await topluSeferUygulaButton.ClickAsync();
        await EnsureAnyVisibleAsync(page, "text=satıra toplu sefer değeri uygulandı.");
    }

    private async Task VerifyOperationalHakedisSelectAllFlowAsync(IPage page)
    {
        await page.GotoAsync($"{_baseUrl}/operasyonel-hakedis");
        await WaitForPageSettledAsync(page);

        var rowCheckboxes = page.Locator("tbody input.form-check-input");
        var rowCount = await rowCheckboxes.CountAsync();
        if (rowCount == 0)
        {
            HandleDeterministicExpectation("Operasyonel hakedişte tümünü seç akışı için kayıt bulunamadı.");
            return;
        }

        var toggleButton = page.Locator("button:has-text('Tümünü Seç'), button:has-text('Seçimi Temizle')").First;

        await toggleButton.ClickAsync();
        await EnsureAnyVisibleAsync(page, "text=Seçimi Temizle", "text=Seçili: ");

        await toggleButton.ClickAsync();
        await EnsureAnyVisibleAsync(page, "text=Tümünü Seç", "text=Seçili: 0");
    }

    private async Task VerifyOperationalPuantajBulkSaveButtonEnableFlowAsync(IPage page)
    {
        await page.GotoAsync($"{_baseUrl}/operasyon/puantaj");
        await WaitForPageSettledAsync(page);

        if (await page.Locator("text=Aktif firma bulunamadı. Lütfen önce firma seçiniz.").First.IsVisibleAsync())
        {
            throw new SmokeSkippedException("Operasyonel puantaj için aktif firma bulunamadığından kaydet butonu aktifleşme akışı atlandı.");
        }

        var rowCheckboxes = page.Locator("tbody input.form-check-input");
        var rowCount = await rowCheckboxes.CountAsync();
        if (rowCount == 0)
        {
            throw new SmokeSkippedException("Operasyonel puantajda seçim yapılacak satır bulunamadı.");
        }

        await rowCheckboxes.First.ClickAsync();

        var secilenleriKaydetButton = page.Locator("button:has-text('Seçilenleri Kaydet')").First;
        if (!await secilenleriKaydetButton.IsEnabledAsync())
        {
            throw new InvalidOperationException("Operasyonel puantajda satır seçimi sonrası Seçilenleri Kaydet butonu aktifleşmedi.");
        }
    }

    private async Task VerifyOperationalHakedisBulkActionEnableFlowAsync(IPage page)
    {
        await page.GotoAsync($"{_baseUrl}/operasyonel-hakedis");
        await WaitForPageSettledAsync(page);

        var rowCheckboxes = page.Locator("tbody input.form-check-input");
        var rowCount = await rowCheckboxes.CountAsync();
        if (rowCount == 0)
        {
            HandleDeterministicExpectation("Operasyonel hakedişte toplu aksiyon aktifleşme akışı için kayıt bulunamadı.");
            return;
        }

        if (_prepareDemoData && _newHakedisMinIdExclusive.HasValue)
        {
            var selectedId = await SelectFirstHakedisRowAsync(page, id => id > _newHakedisMinIdExclusive.Value);
            if (!selectedId.HasValue)
            {
                HandleDeterministicExpectation($"Operasyonel hakedişte yeni üretilen (ID>{_newHakedisMinIdExclusive}) satır bulunamadı.");
                return;
            }
        }
        else
        {
            await rowCheckboxes.First.ClickAsync();
        }

        var topluOnaylaButton = page.Locator("button:has-text('Toplu Onayla')").First;
        var topluFaturalaButton = page.Locator("button:has-text('Toplu Faturala')").First;
        var topluSilButton = page.Locator("button:has-text('Toplu Sil')").First;

        var onayEnabled = await topluOnaylaButton.IsEnabledAsync();
        var faturalaEnabled = await topluFaturalaButton.IsEnabledAsync();
        var silEnabled = await topluSilButton.IsEnabledAsync();

        if (!onayEnabled && !faturalaEnabled && !silEnabled)
        {
            HandleDeterministicExpectation("Seçilen hakediş durumu toplu aksiyonlara uygun değil; aktifleşen buton bulunamadı.");
        }
    }

    private async Task VerifyOperationalHakedisBulkStateTransitionFlowAsync(IPage page)
    {
        if (!_allowStateMutations)
        {
            throw new SmokeSkippedException("Mutasyon içeren state-transition senaryosu kapalı. Çalıştırmak için CRMFILO_SMOKE_ALLOW_MUTATION=true ayarlayın.");
        }

        await page.GotoAsync($"{_baseUrl}/operasyonel-hakedis");
        await WaitForPageSettledAsync(page);

        var rowCheckboxes = page.Locator("tbody input.form-check-input");
        var rowCount = await rowCheckboxes.CountAsync();
        if (rowCount == 0)
        {
            HandleDeterministicExpectation("Operasyonel hakedişte state-transition akışı için kayıt bulunamadı.");
            return;
        }

        var topluOnaylaButton = page.Locator("button:has-text('Toplu Onayla')").First;
        var topluFaturalaButton = page.Locator("button:has-text('Toplu Faturala')").First;
        var topluSilButton = page.Locator("button:has-text('Toplu Sil')").First;

        if (_prepareDemoData && _newHakedisMinIdExclusive.HasValue)
        {
            var selectedId = await SelectFirstHakedisRowAsync(page, id => id > _newHakedisMinIdExclusive.Value);
            if (!selectedId.HasValue)
            {
                HandleDeterministicExpectation($"State-transition için yeni üretilen (ID>{_newHakedisMinIdExclusive}) hakediş satırı bulunamadı.");
                return;
            }

            if (await topluOnaylaButton.IsEnabledAsync())
            {
                await topluOnaylaButton.ClickAsync();
                await EnsureAnyVisibleAsync(page, "text=Toplu onay tamamlandı.");
                return;
            }

            if (await topluFaturalaButton.IsEnabledAsync())
            {
                await topluFaturalaButton.ClickAsync();
                await EnsureAnyVisibleAsync(page, "text=Toplu faturalama tamamlandı.");
                return;
            }

            if (await topluSilButton.IsEnabledAsync())
            {
                await topluSilButton.ClickAsync();
                await EnsureAnyVisibleAsync(page, "text=Toplu silme tamamlandı.");
                return;
            }

            HandleDeterministicExpectation($"Yeni üretilen (ID>{_newHakedisMinIdExclusive}) hakediş satırında state-transition aksiyonu aktifleşmedi.");
            return;
        }

        var maxTry = Math.Min(rowCount, 10);
        for (var i = 0; i < maxTry; i++)
        {
            await rowCheckboxes.Nth(i).ClickAsync();

            if (await topluOnaylaButton.IsEnabledAsync())
            {
                await topluOnaylaButton.ClickAsync();
                await EnsureAnyVisibleAsync(page, "text=Toplu onay tamamlandı.");
                return;
            }

            if (await topluFaturalaButton.IsEnabledAsync())
            {
                await topluFaturalaButton.ClickAsync();
                await EnsureAnyVisibleAsync(page, "text=Toplu faturalama tamamlandı.");
                return;
            }

            if (await topluSilButton.IsEnabledAsync())
            {
                await topluSilButton.ClickAsync();
                await EnsureAnyVisibleAsync(page, "text=Toplu silme tamamlandı.");
                return;
            }

            await rowCheckboxes.Nth(i).ClickAsync();
        }

        HandleDeterministicExpectation("İncelenen kayıtlar için state-transition aksiyonuna uygun satır bulunamadı.");
    }

    private void HandleDeterministicExpectation(string defaultSkipMessage)
    {
        if (_prepareDemoData)
        {
            throw new InvalidOperationException($"{defaultSkipMessage} Demo veri hazırlığı açıkken bu durum beklenmiyor.");
        }

        throw new SmokeSkippedException(defaultSkipMessage);
    }

    private static async Task<int?> SelectFirstHakedisRowAsync(IPage page, Func<int, bool> predicate)
    {
        var rowCheckboxes = page.Locator("tbody input.form-check-input");
        var idCells = page.Locator("tbody tr td:nth-child(2)");
        var count = Math.Min(await rowCheckboxes.CountAsync(), await idCells.CountAsync());

        for (var i = 0; i < count; i++)
        {
            var text = (await idCells.Nth(i).InnerTextAsync()).Trim();
            if (!int.TryParse(text, out var id) || !predicate(id))
            {
                continue;
            }

            await rowCheckboxes.Nth(i).ClickAsync();
            return id;
        }

        return null;
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


