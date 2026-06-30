using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using WebDriverManager;
using WebDriverManager.DriverConfigs.Impl;
using MKFiloServis.Shared.Entities;
using System.Text.Json;
using System.Text.RegularExpressions;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Web.Services;

public interface ISeleniumScraperService
{
    Task<List<PiyasaArastirmaIlan>> TumKaynaklardanTaraAsync(AracPiyasaArastirmaRequest request, IProgress<string>? progress = null, CancellationToken cancellationToken = default);
    Task<List<string>> IlanFotograflariniCekAsync(string ilanUrl, string kaynak);
    void DurdurTarama();
    List<PiyasaKaynakBilgi> GetDesteklenenKaynaklar();
}

public class SeleniumScraperService : ISeleniumScraperService
{
    private readonly ILogger<SeleniumScraperService> _logger;
    private readonly IConfiguration _configuration;
    private ChromeDriver? _activeDriver;
    private volatile bool _stopRequested = false;

    // Statik kaynak listesi - veritabani bagimliligi yok
    private static readonly List<KaynakTanim> TumKaynaklar = new()
    {
        // Genel Pazaryerleri
        new KaynakTanim("sahibinden", "Sahibinden.com", "https://www.sahibinden.com", "Genel", 1),
        new KaynakTanim("arabam", "Arabam.com", "https://www.arabam.com", "Genel", 2),

        // Galeri Siteleri
        new KaynakTanim("otosor", "Otosor", "https://www.otosor.com", "Galeri", 3),
        new KaynakTanim("otoplus", "Otoplus", "https://www.otoplus.com", "Galeri", 4),
        new KaynakTanim("vavacars", "VavaCars", "https://www.vavacars.com", "Galeri", 5),
        new KaynakTanim("otocars", "Otocars", "https://www.otocars.com.tr", "Galeri", 6),

        // Yetkili Bayi Siteleri
        new KaynakTanim("spoticar", "Spoticar", "https://www.spoticar.com.tr", "YetkiliBayi", 7, "Peugeot,Citroen,Opel,DS"),
        new KaynakTanim("borusan", "Borusan Ikinci El", "https://www.borusanikinciel.com", "YetkiliBayi", 8, "BMW,Mini,Land Rover,Jaguar"),
        new KaynakTanim("dod", "DOD", "https://www.dod.com.tr", "YetkiliBayi", 9, "Volkswagen,Audi,Seat,Skoda,Porsche"),
        new KaynakTanim("ford2el", "Ford Ikinci El", "https://www.fordikinciel.com", "YetkiliBayi", 10, "Ford"),
        new KaynakTanim("otokoc", "Otokoc 2. El", "https://www.otokoc2el.com.tr", "YetkiliBayi", 11, "Toyota,Lexus"),
        new KaynakTanim("dogusoto", "Dogus Oto 2. El", "https://www.dogusotomotiv2el.com", "YetkiliBayi", 12, "Volkswagen,Audi,Seat,Skoda,Porsche")
    };

    public SeleniumScraperService(ILogger<SeleniumScraperService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public List<PiyasaKaynakBilgi> GetDesteklenenKaynaklar()
    {
        return TumKaynaklar.Select(k => new PiyasaKaynakBilgi
        {
            Kod = k.Kod,
            Ad = k.Ad,
            Url = k.BaseUrl,
            Aktif = true,
            Sira = k.Sira
        }).ToList();
    }

    public void DurdurTarama()
    {
        _stopRequested = true;
        try
        {
            _activeDriver?.Quit();
            _activeDriver?.Dispose();
            _activeDriver = null;
        }
        catch { }
    }

    private ChromeDriver CreateDriver()
    {
        new DriverManager().SetUpDriver(new ChromeConfig());

        var options = new ChromeOptions();
        options.AddArgument("--headless=new");
        options.AddArgument("--no-sandbox");
        options.AddArgument("--disable-dev-shm-usage");
        options.AddArgument("--disable-gpu");
        options.AddArgument("--window-size=1920,1080");
        options.AddArgument("--disable-blink-features=AutomationControlled");
        options.AddArgument("--user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        options.AddArgument("--disable-extensions");
        options.AddArgument("--disable-images"); // Hızlı yükleme için resimleri devre dışı bırak
        options.AddExcludedArgument("enable-automation");
        options.AddAdditionalOption("useAutomationExtension", false);

        var driver = new ChromeDriver(options);
        driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(5);
        driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(30);

        _activeDriver = driver;
        return driver;
    }

    public async Task<List<PiyasaArastirmaIlan>> TumKaynaklardanTaraAsync(
        AracPiyasaArastirmaRequest request, 
        IProgress<string>? progress = null, 
        CancellationToken cancellationToken = default)
    {
        var tumIlanlar = new List<PiyasaArastirmaIlan>();
        _stopRequested = false;

        // Marka filtresi uygula
        var filtrelenmisKaynaklar = TumKaynaklar
            .Where(k => string.IsNullOrEmpty(k.DesteklenenMarkalar) || 
                        k.DesteklenenMarkalar.Split(',').Any(m => 
                            m.Trim().Equals(request.Marka, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(k => k.Sira)
            .ToList();

        ChromeDriver? driver = null;

        try
        {
            progress?.Report("Chrome baslatiliyor...");
            driver = CreateDriver();

            int sayac = 0;
            foreach (var kaynak in filtrelenmisKaynaklar)
            {
                if (_stopRequested || cancellationToken.IsCancellationRequested)
                {
                    progress?.Report("Tarama durduruldu!");
                    break;
                }

                sayac++;
                progress?.Report($"[{sayac}/{filtrelenmisKaynaklar.Count}] {kaynak.Ad} taraniyor...");

                try
                {
                    var ilanlar = await TaraKaynak(kaynak, request, driver, cancellationToken);
                    if (ilanlar.Any())
                    {
                        tumIlanlar.AddRange(ilanlar);
                        progress?.Report($"{kaynak.Ad}: {ilanlar.Count} ilan bulundu");
                    }
                    else
                    {
                        progress?.Report($"{kaynak.Ad}: Ilan bulunamadi");
                    }
                }
                catch (OperationCanceledException)
                {
                    progress?.Report("Tarama iptal edildi!");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "{Kaynak} tarama hatasi", kaynak.Ad);
                    progress?.Report($"{kaynak.Ad}: Hata - {ex.Message}");
                }

                if (!_stopRequested && !cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(500, cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Tarama genel hatasi");
            progress?.Report($"Genel hata: {ex.Message}");
        }
        finally
        {
            SafeQuitDriver(driver);
            _activeDriver = null;
        }

        progress?.Report($"Toplam {tumIlanlar.Count} ilan bulundu");
        return tumIlanlar;
    }

    private async Task<List<PiyasaArastirmaIlan>> TaraKaynak(KaynakTanim kaynak, AracPiyasaArastirmaRequest request, ChromeDriver driver, CancellationToken ct)
    {
        return kaynak.Kod switch
        {
            "sahibinden" => await TaraSahibinden(kaynak, request, driver, ct),
            "arabam" => await TaraArabam(kaynak, request, driver, ct),
            "otosor" => await TaraOtosor(kaynak, request, driver, ct),
            "otoplus" => await TaraOtoplus(kaynak, request, driver, ct),
            "vavacars" => await TaraVavaCars(kaynak, request, driver, ct),
            "otocars" => await TaraOtocars(kaynak, request, driver, ct),
            _ => await TaraGenelKaynak(kaynak, request, driver, ct)
        };
    }

    #region Sahibinden.com

    private async Task<List<PiyasaArastirmaIlan>> TaraSahibinden(KaynakTanim kaynak, AracPiyasaArastirmaRequest request, ChromeDriver driver, CancellationToken ct)
    {
        var ilanlar = new List<PiyasaArastirmaIlan>();
        if (_stopRequested || ct.IsCancellationRequested) return ilanlar;

        try
        {
            var marka = Slugify(request.Marka);
            var model = Slugify(request.Model);
            var kategori = request.VasitaTuru switch
            {
                "arazi-suv-pickup" => "arazi-suv-pickup",
                "ticari-arac" => "ticari-araclar",
                "minivan-panelvan" => "minivan-panelvan",
                _ => "otomobil"
            };

            var url = $"https://www.sahibinden.com/{kategori}-{marka}-{model}?sorting=date_desc";
            if (request.YilBaslangic.HasValue) url += $"&a5_min={request.YilBaslangic}";
            if (request.YilBitis.HasValue) url += $"&a5_max={request.YilBitis}";

            driver.Navigate().GoToUrl(url);
            await Task.Delay(3000, ct);

            // Cookie kabul
            TryClick(driver, By.Id("onetrust-accept-btn-handler"));
            await Task.Delay(500, ct);

            var elements = driver.FindElements(By.CssSelector("tr.searchResultsItem, tbody tr[data-id]"));
            _logger.LogInformation("Sahibinden: {Count} element bulundu", elements.Count);

            foreach (var el in elements.Take(30))
            {
                if (_stopRequested || ct.IsCancellationRequested) break;

                try
                {
                    var ilan = new PiyasaArastirmaIlan
                    {
                        Kaynak = kaynak.Ad,
                        ToplanmaTarihi = DateTime.Now,
                        AktifMi = true,
                        Marka = request.Marka,
                        Model = request.Model
                    };

                    // İlan ID
                    ilan.IlanNo = el.GetAttribute("data-id") ?? "";

                    // Link ve Başlık
                    try
                    {
                        var linkEl = el.FindElement(By.CssSelector("td.searchResultsTitleValue a"));
                        var href = linkEl.GetAttribute("href") ?? "";
                        ilan.IlanUrl = href.StartsWith("http") ? href : $"https://www.sahibinden.com{href}";
                        ilan.IlanBasligi = linkEl.Text.Trim();
                    }
                    catch { continue; }

                    // Fiyat
                    try
                    {
                        var fiyatEl = el.FindElement(By.CssSelector("td.searchResultsPriceValue"));
                        ilan.Fiyat = ParseFiyat(fiyatEl.Text);
                    }
                    catch { }

                    // Yıl, KM, Renk
                    try
                    {
                        var attrs = el.FindElements(By.CssSelector("td.searchResultsAttributeValue"));
                        if (attrs.Count >= 1) ilan.ModelYili = ParseYil(attrs[0].Text);
                        if (attrs.Count >= 2) ilan.Kilometre = ParseKilometre(attrs[1].Text);
                        if (attrs.Count >= 3) ilan.Renk = attrs[2].Text.Trim();
                    }
                    catch { }

                    // Resim - Sahibinden.com icin ozel islem
                    try
                    {
                        var imgEl = el.FindElement(By.CssSelector("td.searchResultsLargeThumbnail img"));
                        var imgSrc = imgEl.GetAttribute("src") ?? imgEl.GetAttribute("data-src") ?? "";
                        if (!string.IsNullOrEmpty(imgSrc) && !imgSrc.Contains("placeholder") && !imgSrc.Contains("logo"))
                        {
                            // Sahibinden.com resim URL formati:
                            // Kucuk: https://i.sahibinden.com/photos/xx/xx/xx/thumbs/xxx_thmb.jpg
                            // Buyuk: https://i.sahibinden.com/photos/xx/xx/xx/xxx_x5.jpg
                            
                            // Kucuk resmi buyuk resme cevir
                            imgSrc = imgSrc.Replace("_thmb.", "_x5.")
                                           .Replace("/thmb/", "/x5/")
                                           .Replace("/thumbs/", "/")
                                           .Replace("_s.", "_x5.")
                                           .Replace("_m.", "_x5.");
                            
                            ilan.ResimUrl = imgSrc.StartsWith("http") ? imgSrc : $"https:{imgSrc}";
                        }
                    }
                    catch { }

                    // Konum
                    try
                    {
                        var locEl = el.FindElement(By.CssSelector("td.searchResultsLocationValue"));
                        var locParts = locEl.Text.Split('\n');
                        ilan.Sehir = locParts.FirstOrDefault()?.Trim();
                        if (locParts.Length > 1) ilan.Ilce = locParts[1].Trim();
                    }
                    catch { }

                    // Tarih
                    try
                    {
                        var dateEl = el.FindElement(By.CssSelector("td.searchResultsDateValue"));
                        ilan.IlanTarihi = ParseTarih(dateEl.Text);
                    }
                    catch { ilan.IlanTarihi = DateTime.Today; }

                    if (ilan.Fiyat > 0 && !string.IsNullOrEmpty(ilan.IlanUrl))
                        ilanlar.Add(ilan);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Sahibinden ilan parse hatası");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Sahibinden tarama hatası");
        }

        return ilanlar;
    }

    #endregion

    #region Arabam.com

    private async Task<List<PiyasaArastirmaIlan>> TaraArabam(KaynakTanim kaynak, AracPiyasaArastirmaRequest request, ChromeDriver driver, CancellationToken ct)
    {
        var ilanlar = new List<PiyasaArastirmaIlan>();
        if (_stopRequested || ct.IsCancellationRequested) return ilanlar;

        try
        {
            // Arabam.com arama URL'si
            var marka = Slugify(request.Marka);
            var model = Slugify(request.Model);
            var url = $"https://www.arabam.com/ikinci-el/{marka}-{model}?sort=3";
            if (request.YilBaslangic.HasValue) url += $"&minYear={request.YilBaslangic}";
            if (request.YilBitis.HasValue) url += $"&maxYear={request.YilBitis}";

            _logger.LogInformation("Arabam.com URL: {Url}", url);
            driver.Navigate().GoToUrl(url);
            await Task.Delay(3000, ct);

            // Cookie kabul
            TryClick(driver, By.CssSelector("button[data-testid='accept-all-cookies']"));
            TryClick(driver, By.CssSelector(".onetrust-accept-btn-handler"));
            TryClick(driver, By.CssSelector("#onetrust-accept-btn-handler"));
            await Task.Delay(1000, ct);

            // Sayfanin tamamen yuklenmesini bekle
            await Task.Delay(2000, ct);

            // Ilan linklerini topla - once tum linkleri al
            var allLinks = driver.FindElements(By.CssSelector("a[href*='/ilan/']"));
            _logger.LogInformation("Arabam: {Count} link bulundu", allLinks.Count);

            // Benzersiz ilan URL'lerini topla
            var processedUrls = new HashSet<string>();
            
            foreach (var linkEl in allLinks.Take(50))
            {
                if (_stopRequested || ct.IsCancellationRequested) break;

                try
                {
                    var href = linkEl.GetAttribute("href") ?? "";
                    if (string.IsNullOrEmpty(href) || !href.Contains("/ilan/")) continue;
                    
                    // URL'yi normalize et
                    if (!href.StartsWith("http"))
                    {
                        href = $"https://www.arabam.com{href}";
                    }
                    
                    // Zaten islenmis mi?
                    if (processedUrls.Contains(href)) continue;
                    
                    // Ilan numarasini cek
                    // Arabam URL formati: /ilan/xxx-xxx-xxx/sehir-xxx/38132099
                    // veya: /ilan/38132099/detay
                    var ilanNoMatch = Regex.Match(href, @"/(\d{7,})(?:/|$|\?)");
                    if (!ilanNoMatch.Success) continue;
                    
                    var ilanNo = ilanNoMatch.Groups[1].Value;
                    
                    // /detay'i kaldir
                    href = Regex.Replace(href, @"/detay$", "");
                    
                    // Zaten bu ilan numarasi var mi?
                    if (ilanlar.Any(x => x.IlanNo == ilanNo)) continue;
                    
                    processedUrls.Add(href);
                    
                    var ilan = new PiyasaArastirmaIlan
                    {
                        Kaynak = kaynak.Ad,
                        ToplanmaTarihi = DateTime.Now,
                        AktifMi = true,
                        Marka = request.Marka,
                        Model = request.Model,
                        IlanNo = ilanNo,
                        IlanUrl = href
                    };

                    // Link elementinin parent container'ini bul
                    IWebElement? container = null;
                    try
                    {
                        // Parent tr veya listing item'i bul
                        container = linkEl.FindElement(By.XPath("./ancestor::tr[contains(@class, 'listing')]"))
                                    ?? linkEl.FindElement(By.XPath("./ancestor::*[contains(@class, 'listing')]"));
                    }
                    catch
                    {
                        try
                        {
                            container = linkEl.FindElement(By.XPath("./.."));
                        }
                        catch { }
                    }

                    var textSource = container?.Text ?? linkEl.Text ?? "";

                    // Baslik
                    try
                    {
                        var titleEl = container?.FindElement(By.CssSelector("h3, .listing-title, [class*='title']"));
                        ilan.IlanBasligi = titleEl?.Text.Trim() ?? $"{request.Marka} {request.Model}";
                    }
                    catch 
                    { 
                        ilan.IlanBasligi = linkEl?.Text?.Trim() ?? $"{request.Marka} {request.Model}";
                        if (string.IsNullOrEmpty(ilan.IlanBasligi))
                            ilan.IlanBasligi = $"{request.Marka} {request.Model}"; 
                    }

                    // Fiyat
                    try
                    {
                        var fiyatEl = container?.FindElement(By.CssSelector("[class*='price'], .listing-price"));
                        ilan.Fiyat = ParseFiyat(fiyatEl?.Text ?? textSource);
                    }
                    catch
                    {
                        ilan.Fiyat = ParseFiyat(textSource);
                    }

                    // Yil, KM
                    ilan.ModelYili = ParseYil(textSource);
                    ilan.Kilometre = ParseKilometre(textSource);

                    // Resim - Arabam.com resim URL'si ilan numarasindan olusturulabilir
                    try
                    {
                        var imgEl = container?.FindElement(By.CssSelector("img"));
                        var imgSrc = imgEl?.GetAttribute("src") ?? imgEl?.GetAttribute("data-src") ?? "";
                        
                        if (!string.IsNullOrEmpty(imgSrc) && !imgSrc.Contains("placeholder") && !imgSrc.Contains("logo"))
                        {
                            // Kucuk resmi buyuk resme cevir: _120x90 -> _580x435
                            imgSrc = Regex.Replace(imgSrc, @"_\d+x\d+\.", "_580x435.");
                            ilan.ResimUrl = imgSrc.StartsWith("http") ? imgSrc : $"https:{imgSrc}";
                        }
                    }
                    catch { }

                    ilan.IlanTarihi = DateTime.Today;

                    if (ilan.Fiyat > 0)
                    {
                        _logger.LogDebug("Arabam ilan: No={IlanNo}, URL={Url}, Fiyat={Fiyat}", 
                            ilan.IlanNo, ilan.IlanUrl, ilan.Fiyat);
                        ilanlar.Add(ilan);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Arabam link parse hatasi");
                }
            }
            
            _logger.LogInformation("Arabam: {Count} ilan eklendi", ilanlar.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Arabam tarama hatasi");
        }

        return ilanlar;
    }

    #endregion

    #region Otosor

    private async Task<List<PiyasaArastirmaIlan>> TaraOtosor(KaynakTanim kaynak, AracPiyasaArastirmaRequest request, ChromeDriver driver, CancellationToken ct)
    {
        var ilanlar = new List<PiyasaArastirmaIlan>();
        if (_stopRequested || ct.IsCancellationRequested) return ilanlar;

        try
        {
            var url = $"https://www.otosor.com/ikinci-el-arac?marka={Slugify(request.Marka)}&model={Slugify(request.Model)}";
            driver.Navigate().GoToUrl(url);
            await Task.Delay(3000, ct);

            TryClick(driver, By.CssSelector("[class*='cookie'] button, .cookie-accept"));
            await Task.Delay(500, ct);

            var elements = driver.FindElements(By.CssSelector(".car-card, .vehicle-card, [class*='listing-item']"));

            foreach (var el in elements.Take(20))
            {
                if (_stopRequested || ct.IsCancellationRequested) break;
                var ilan = ParseGenelElement(el, kaynak, request);
                if (ilan != null) ilanlar.Add(ilan);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Otosor tarama hatası");
        }

        return ilanlar;
    }

    #endregion

    #region Otoplus

    private async Task<List<PiyasaArastirmaIlan>> TaraOtoplus(KaynakTanim kaynak, AracPiyasaArastirmaRequest request, ChromeDriver driver, CancellationToken ct)
    {
        var ilanlar = new List<PiyasaArastirmaIlan>();
        if (_stopRequested || ct.IsCancellationRequested) return ilanlar;

        try
        {
            var url = $"https://www.otoplus.com/arac-listesi?marka={Slugify(request.Marka)}";
            driver.Navigate().GoToUrl(url);
            await Task.Delay(3000, ct);

            TryClick(driver, By.CssSelector("[class*='cookie'] button, .cookie-accept"));
            await Task.Delay(500, ct);

            var elements = driver.FindElements(By.CssSelector(".car-card, .vehicle-card, [class*='vehicle-item']"));

            foreach (var el in elements.Take(20))
            {
                if (_stopRequested || ct.IsCancellationRequested) break;
                var ilan = ParseGenelElement(el, kaynak, request);
                if (ilan != null) ilanlar.Add(ilan);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Otoplus tarama hatası");
        }

        return ilanlar;
    }

    #endregion

    #region VavaCars

    private async Task<List<PiyasaArastirmaIlan>> TaraVavaCars(KaynakTanim kaynak, AracPiyasaArastirmaRequest request, ChromeDriver driver, CancellationToken ct)
    {
        var ilanlar = new List<PiyasaArastirmaIlan>();
        if (_stopRequested || ct.IsCancellationRequested) return ilanlar;

        try
        {
            var url = $"https://www.vavacars.com/arac-satin-al?marka={Slugify(request.Marka)}";
            driver.Navigate().GoToUrl(url);
            await Task.Delay(3000, ct);

            TryClick(driver, By.CssSelector("[class*='cookie'] button, .cookie-accept"));
            await Task.Delay(500, ct);

            var elements = driver.FindElements(By.CssSelector(".car-card, .vehicle-card, [class*='car-item']"));

            foreach (var el in elements.Take(20))
            {
                if (_stopRequested || ct.IsCancellationRequested) break;
                var ilan = ParseGenelElement(el, kaynak, request);
                if (ilan != null) ilanlar.Add(ilan);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "VavaCars tarama hatası");
        }

        return ilanlar;
    }

    #endregion

    #region Otocars

    private async Task<List<PiyasaArastirmaIlan>> TaraOtocars(KaynakTanim kaynak, AracPiyasaArastirmaRequest request, ChromeDriver driver, CancellationToken ct)
    {
        var ilanlar = new List<PiyasaArastirmaIlan>();
        if (_stopRequested || ct.IsCancellationRequested) return ilanlar;

        try
        {
            var url = $"https://www.otocars.com.tr/araclar?marka={Slugify(request.Marka)}";
            driver.Navigate().GoToUrl(url);
            await Task.Delay(3000, ct);

            TryClick(driver, By.CssSelector("[class*='cookie'] button, .cookie-accept"));
            await Task.Delay(500, ct);

            var elements = driver.FindElements(By.CssSelector(".car-card, .vehicle-card, [class*='car-item']"));

            foreach (var el in elements.Take(20))
            {
                if (_stopRequested || ct.IsCancellationRequested) break;
                var ilan = ParseGenelElement(el, kaynak, request);
                if (ilan != null) ilanlar.Add(ilan);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Otocars tarama hatası");
        }

        return ilanlar;
    }

    #endregion

    #region Genel Kaynak

    private async Task<List<PiyasaArastirmaIlan>> TaraGenelKaynak(KaynakTanim kaynak, AracPiyasaArastirmaRequest request, ChromeDriver driver, CancellationToken ct)
    {
        var ilanlar = new List<PiyasaArastirmaIlan>();
        if (_stopRequested || ct.IsCancellationRequested) return ilanlar;

        try
        {
            var url = $"{kaynak.BaseUrl}/arac-listesi?marka={Slugify(request.Marka)}";
            driver.Navigate().GoToUrl(url);
            await Task.Delay(3000, ct);

            TryClick(driver, By.CssSelector("[class*='cookie'] button, .cookie-accept, #accept-cookies"));
            await Task.Delay(500, ct);

            var elements = driver.FindElements(By.CssSelector(".car-card, .vehicle-card, .listing-item, [class*='vehicle-item'], [class*='car-item']"));

            foreach (var el in elements.Take(15))
            {
                if (_stopRequested || ct.IsCancellationRequested) break;
                var ilan = ParseGenelElement(el, kaynak, request);
                if (ilan != null) ilanlar.Add(ilan);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "{Kaynak} genel tarama hatası", kaynak.Ad);
        }

        return ilanlar;
    }

    private PiyasaArastirmaIlan? ParseGenelElement(IWebElement element, KaynakTanim kaynak, AracPiyasaArastirmaRequest request)
    {
        try
        {
            var ilan = new PiyasaArastirmaIlan
            {
                Kaynak = kaynak.Ad,
                ToplanmaTarihi = DateTime.Now,
                AktifMi = true,
                Marka = request.Marka,
                Model = request.Model,
                SaticiTipi = kaynak.Tip == "YetkiliBayi" ? "Yetkili Bayi" : kaynak.Tip == "Galeri" ? "Galeri" : "Bireysel"
            };

            // Link
            try
            {
                var linkEl = element.FindElement(By.CssSelector("a[href*='/arac'], a[href*='/ilan'], a[href*='/detay'], a"));
                var href = linkEl.GetAttribute("href") ?? "";
                if (!string.IsNullOrEmpty(href))
                {
                    ilan.IlanUrl = href.StartsWith("http") ? href : $"{kaynak.BaseUrl}{(href.StartsWith("/") ? "" : "/")}{href}";
                    var match = Regex.Match(href, @"[/-](\d{4,})");
                    if (match.Success) ilan.IlanNo = match.Groups[1].Value;
                }
            }
            catch { return null; }

            // Başlık
            try
            {
                var titleEl = element.FindElement(By.CssSelector("h2, h3, h4, .title, [class*='title']"));
                ilan.IlanBasligi = titleEl.Text.Trim();
            }
            catch { ilan.IlanBasligi = $"{request.Marka} {request.Model}"; }

            // Fiyat
            var text = element.Text;
            ilan.Fiyat = ParseFiyat(text);
            ilan.ModelYili = ParseYil(text);
            ilan.Kilometre = ParseKilometre(text);

            // Resim
            try
            {
                var imgEl = element.FindElement(By.CssSelector("img"));
                var imgSrc = imgEl.GetAttribute("src") ?? imgEl.GetAttribute("data-src") ?? "";
                if (!string.IsNullOrEmpty(imgSrc) && !imgSrc.Contains("placeholder") && !imgSrc.Contains("logo"))
                {
                    ilan.ResimUrl = imgSrc.StartsWith("http") ? imgSrc : $"https:{imgSrc}";
                }
            }
            catch { }

            ilan.IlanTarihi = DateTime.Today;

            return ilan.Fiyat > 0 ? ilan : null;
        }
        catch
        {
            return null;
        }
    }

    #endregion

    #region Fotoğraf Çekme

    public async Task<List<string>> IlanFotograflariniCekAsync(string ilanUrl, string kaynak)
    {
        var fotograflar = new List<string>();
        if (string.IsNullOrEmpty(ilanUrl)) return fotograflar;

        ChromeDriver? driver = null;

        try
        {
            // Resim yükleme aktif driver oluştur
            var options = new ChromeOptions();
            options.AddArgument("--headless=new");
            options.AddArgument("--no-sandbox");
            options.AddArgument("--disable-dev-shm-usage");
            options.AddArgument("--disable-gpu");
            options.AddArgument("--window-size=1920,1080");
            options.AddArgument("--user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

            new DriverManager().SetUpDriver(new ChromeConfig());
            driver = new ChromeDriver(options);
            driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(30);

            driver.Navigate().GoToUrl(ilanUrl);
            await Task.Delay(3000);

            // Cookie kabul
            TryClick(driver, By.Id("onetrust-accept-btn-handler"));
            TryClick(driver, By.CssSelector("button[data-testid='accept-all-cookies']"));
            TryClick(driver, By.CssSelector("[class*='cookie'] button"));
            await Task.Delay(500);

            // Sahibinden.com için özel selector'lar
            if (ilanUrl.Contains("sahibinden.com"))
            {
                var selectors = new[]
                {
                    ".classifiedDetailMainPhoto img",
                    ".classified-gallery img",
                    "[data-fancybox] img",
                    ".galleria-thumbnails img",
                    ".classifiedDetailThumbList img"
                };

                foreach (var selector in selectors)
                {
                    try
                    {
                        var imgs = driver.FindElements(By.CssSelector(selector));
                        foreach (var img in imgs)
                        {
                            var src = img.GetAttribute("src") ?? img.GetAttribute("data-src") ?? img.GetAttribute("data-original") ?? "";
                            if (IsValidImageUrl(src))
                            {
                                // Büyük versiyona çevir
                                src = src.Replace("_thmb.", "_x5.").Replace("/thmb/", "/x5/").Replace("_s.", "_x5.");
                                if (!src.StartsWith("http")) src = $"https:{src}";
                                if (!fotograflar.Contains(src)) fotograflar.Add(src);
                            }
                        }
                    }
                    catch { }
                }
            }
            // Arabam.com için
            else if (ilanUrl.Contains("arabam.com"))
            {
                var selectors = new[]
                {
                    ".gallery-thumbs img",
                    ".swiper-slide img",
                    "[class*='gallery'] img",
                    ".detail-slider img"
                };

                foreach (var selector in selectors)
                {
                    try
                    {
                        var imgs = driver.FindElements(By.CssSelector(selector));
                        foreach (var img in imgs)
                        {
                            var src = img.GetAttribute("src") ?? img.GetAttribute("data-src") ?? "";
                            if (IsValidImageUrl(src))
                            {
                                if (!src.StartsWith("http")) src = $"https:{src}";
                                if (!fotograflar.Contains(src)) fotograflar.Add(src);
                            }
                        }
                    }
                    catch { }
                }
            }
            // Diğer siteler için genel
            else
            {
                var selectors = new[]
                {
                    ".gallery img",
                    ".slider img",
                    ".swiper-slide img",
                    "[class*='gallery'] img",
                    "[class*='photo'] img",
                    ".vehicle-images img"
                };

                foreach (var selector in selectors)
                {
                    try
                    {
                        var imgs = driver.FindElements(By.CssSelector(selector));
                        foreach (var img in imgs)
                        {
                            var src = img.GetAttribute("src") ?? img.GetAttribute("data-src") ?? "";
                            if (IsValidImageUrl(src))
                            {
                                if (!src.StartsWith("http")) src = $"https:{src}";
                                if (!fotograflar.Contains(src)) fotograflar.Add(src);
                            }
                        }
                    }
                    catch { }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Fotoğraf çekme hatası: {Url}", ilanUrl);
        }
        finally
        {
            SafeQuitDriver(driver);
        }

        return fotograflar.Distinct().Take(15).ToList();
    }

    private bool IsValidImageUrl(string src)
    {
        if (string.IsNullOrEmpty(src)) return false;
        if (src.Contains("placeholder")) return false;
        if (src.Contains("logo")) return false;
        if (src.Contains("icon")) return false;
        if (src.Contains("avatar")) return false;
        if (src.Contains("loading")) return false;
        if (src.Length < 20) return false;
        return true;
    }

    #endregion

    #region Helper Methods

    private decimal ParseFiyat(string text)
    {
        if (string.IsNullOrEmpty(text)) return 0;

        // "1.850.000 TL" veya "1.850.000?" formatı
        var match = Regex.Match(text, @"([\d\.]+)\s*(?:TL|?|tl)", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            var fiyatStr = match.Groups[1].Value.Replace(".", "");
            if (decimal.TryParse(fiyatStr, out var fiyat)) return fiyat;
        }

        // Sadece rakam grupları
        match = Regex.Match(text, @"(\d{1,3}(?:\.\d{3})+)");
        if (match.Success)
        {
            var fiyatStr = match.Value.Replace(".", "");
            if (decimal.TryParse(fiyatStr, out var fiyat) && fiyat > 50000) return fiyat;
        }

        return 0;
    }

    private int ParseYil(string text)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        var match = Regex.Match(text, @"\b(20[0-2]\d|19[89]\d)\b");
        return match.Success && int.TryParse(match.Value, out var yil) ? yil : 0;
    }

    private int ParseKilometre(string text)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        var match = Regex.Match(text, @"(\d{1,3}(?:[.,]\d{3})*)\s*km", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            var kmStr = match.Groups[1].Value.Replace(".", "").Replace(",", "");
            if (int.TryParse(kmStr, out var km)) return km;
        }
        return 0;
    }

    private DateTime ParseTarih(string text)
    {
        if (string.IsNullOrEmpty(text)) return DateTime.Today;

        var lowerText = text.ToLower().Trim();

        if (lowerText.Contains("bugün") || lowerText.Contains("bugun"))
            return DateTime.Today;

        if (lowerText.Contains("dün") || lowerText.Contains("dun"))
            return DateTime.Today.AddDays(-1);

        var gunMatch = Regex.Match(lowerText, @"(\d+)\s*gün\s*önce");
        if (gunMatch.Success && int.TryParse(gunMatch.Groups[1].Value, out var gun))
            return DateTime.Today.AddDays(-gun);

        var tarihMatch = Regex.Match(text, @"(\d{2})[./](\d{2})[./](\d{4})");
        if (tarihMatch.Success)
        {
            try
            {
                var g = int.Parse(tarihMatch.Groups[1].Value);
                var a = int.Parse(tarihMatch.Groups[2].Value);
                var y = int.Parse(tarihMatch.Groups[3].Value);
                return new DateTime(y, a, g);
            }
            catch { }
        }

        return DateTime.Today;
    }

    private void TryClick(ChromeDriver driver, By selector)
    {
        try
        {
            var el = driver.FindElement(selector);
            if (el != null && el.Displayed)
            {
                el.Click();
                Thread.Sleep(300);
            }
        }
        catch { }
    }

    private void SafeQuitDriver(ChromeDriver? driver)
    {
        try
        {
            if (driver != null)
            {
                driver.Quit();
                driver.Dispose();
            }
        }
        catch { }
    }

    private string Slugify(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        return text.ToLower()
            .Replace("ı", "i").Replace("ö", "o").Replace("ü", "u")
            .Replace("ş", "s").Replace("ğ", "g").Replace("ç", "c")
            .Replace(" ", "-").Replace(".", "").Replace(",", "")
            .Replace("--", "-").Trim('-');
    }

    #endregion
}

#region Models

public class PiyasaKaynakBilgi
{
    public string Kod { get; set; } = "";
    public string Ad { get; set; } = "";
    public string Url { get; set; } = "";
    public bool Aktif { get; set; } = true;
    public int Sira { get; set; }
}

public class KaynakTanim
{
    public string Kod { get; set; }
    public string Ad { get; set; }
    public string BaseUrl { get; set; }
    public string Tip { get; set; }
    public int Sira { get; set; }
    public string DesteklenenMarkalar { get; set; }

    public KaynakTanim(string kod, string ad, string baseUrl, string tip, int sira, string desteklenenMarkalar = "")
    {
        Kod = kod;
        Ad = ad;
        BaseUrl = baseUrl;
        Tip = tip;
        Sira = sira;
        DesteklenenMarkalar = desteklenenMarkalar;
    }
}

#endregion



