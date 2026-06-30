using Microsoft.Playwright;
using MKFiloServis.Shared.Entities;
using System.Text.RegularExpressions;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Web.Services;

public interface IPlaywrightScraperService
{
    Task<List<PiyasaArastirmaIlan>> TumKaynaklardanTaraAsync(AracPiyasaArastirmaRequest request, IProgress<string>? progress = null, CancellationToken cancellationToken = default);
    Task<List<string>> IlanFotograflariniCekAsync(string ilanUrl, string kaynak);
    void DurdurTarama();
    List<PiyasaKaynakBilgi> GetDesteklenenKaynaklar();
}

public class PlaywrightScraperService : IPlaywrightScraperService
{
    private readonly ILogger<PlaywrightScraperService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IPiyasaKaynakService _kaynakService;
    private volatile bool _stopRequested = false;
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    // Varsayilan kaynak listesi (veritabaninda kaynak yoksa kullanilir)
    private static readonly List<KaynakTanim> VarsayilanKaynaklar = new()
    {
        new KaynakTanim("sahibinden", "Sahibinden.com", "https://www.sahibinden.com", "Genel", 1),
        new KaynakTanim("arabam", "Arabam.com", "https://www.arabam.com", "Genel", 2),
    };

    public PlaywrightScraperService(
        ILogger<PlaywrightScraperService> logger, 
        IConfiguration configuration,
        IPiyasaKaynakService kaynakService)
    {
        _logger = logger;
        _configuration = configuration;
        _kaynakService = kaynakService;
    }

    public List<PiyasaKaynakBilgi> GetDesteklenenKaynaklar()
    {
        return VarsayilanKaynaklar.Select(k => new PiyasaKaynakBilgi
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
            _browser?.CloseAsync().GetAwaiter().GetResult();
            _browser = null;
        }
        catch { }
    }

    private async Task<IBrowser> GetBrowserAsync()
    {
        if (_browser == null || !_browser.IsConnected)
        {
            _playwright = await Playwright.CreateAsync();
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true,
                Args = new[]
                {
                    "--no-sandbox",
                    "--disable-dev-shm-usage",
                    "--disable-gpu",
                    "--disable-setuid-sandbox",
                    "--disable-web-security"
                }
            });
        }
        return _browser;
    }

    public async Task<List<PiyasaArastirmaIlan>> TumKaynaklardanTaraAsync(
        AracPiyasaArastirmaRequest request,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var tumIlanlar = new List<PiyasaArastirmaIlan>();
        _stopRequested = false;

        // Veritabanindan AKTIF kaynaklari al
        var aktifKaynaklar = await _kaynakService.GetAktifKaynaklarAsync();
        
        // Eger veritabaninda kaynak yoksa varsayilanlari kullan
        List<KaynakTanim> taranacakKaynaklar;
        if (aktifKaynaklar.Any())
        {
            taranacakKaynaklar = aktifKaynaklar
                .Where(k => k.Aktif) // Sadece aktif olanlar
                .Select(k => new KaynakTanim(k.Kod, k.Ad, k.BaseUrl, k.KaynakTipi ?? "Genel", k.Sira, k.DesteklenenMarkalar ?? string.Empty))
                .ToList();
            
            _logger.LogInformation("Veritabanindan {Count} aktif kaynak yuklendi", taranacakKaynaklar.Count);
        }
        else
        {
            taranacakKaynaklar = VarsayilanKaynaklar;
            _logger.LogInformation("Varsayilan kaynaklar kullaniliyor");
        }

        // Marka filtresi uygula
        var filtrelenmisKaynaklar = taranacakKaynaklar
            .Where(k => string.IsNullOrEmpty(k.DesteklenenMarkalar) ||
                        k.DesteklenenMarkalar.Split(',').Any(m =>
                            m.Trim().Equals(request.Marka, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(k => k.Sira)
            .ToList();

        if (!filtrelenmisKaynaklar.Any())
        {
            progress?.Report("Aktif kaynak bulunamadi!");
            return tumIlanlar;
        }

        IBrowser? browser = null;
        IPage? page = null;

        try
        {
            progress?.Report("Tarayici baslatiliyor...");
            browser = await GetBrowserAsync();

            var context = await browser.NewContextAsync(new BrowserNewContextOptions
            {
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
                ViewportSize = new ViewportSize { Width = 1920, Height = 1080 }
            });

            page = await context.NewPageAsync();

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
                    var ilanlar = await TaraKaynak(kaynak, request, page, cancellationToken);
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
            if (page != null) await page.CloseAsync();
        }

        progress?.Report($"Toplam {tumIlanlar.Count} ilan bulundu");
        return tumIlanlar;
    }

    private async Task<List<PiyasaArastirmaIlan>> TaraKaynak(KaynakTanim kaynak, AracPiyasaArastirmaRequest request, IPage page, CancellationToken ct)
    {
        return kaynak.Kod.ToLower() switch
        {
            "sahibinden" => await TaraSahibinden(kaynak, request, page, ct),
            "arabam" => await TaraArabam(kaynak, request, page, ct),
            _ => new List<PiyasaArastirmaIlan>()
        };
    }

    #region Sahibinden.com

    private async Task<List<PiyasaArastirmaIlan>> TaraSahibinden(KaynakTanim kaynak, AracPiyasaArastirmaRequest request, IPage page, CancellationToken ct)
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

            // Sahibinden URL - tarihe gore sirala
            var url = $"https://www.sahibinden.com/{kategori}-{marka}-{model}?sorting=date_desc";
            if (request.YilBaslangic.HasValue) url += $"&a5_min={request.YilBaslangic}";
            if (request.YilBitis.HasValue) url += $"&a5_max={request.YilBitis}";
            
            // Ilan tarihi filtresi - Sahibinden icin
            if (request.IlanTarihGun > 0 && request.IlanTarihGun <= 1)
            {
                url += "&date=1day"; // Son 24 saat
            }
            else if (request.IlanTarihGun <= 3)
            {
                url += "&date=3days"; // Son 3 gun
            }
            else if (request.IlanTarihGun <= 7)
            {
                url += "&date=7days"; // Son 1 hafta
            }
            else if (request.IlanTarihGun <= 15)
            {
                url += "&date=15days"; // Son 15 gun
            }
            else if (request.IlanTarihGun <= 30)
            {
                url += "&date=30days"; // Son 30 gun
            }

            _logger.LogInformation("Sahibinden URL: {Url}", url);
            await page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
            await Task.Delay(2000, ct);

            // Cookie kabul
            try
            {
                var cookieBtn = page.Locator("#onetrust-accept-btn-handler");
                if (await cookieBtn.IsVisibleAsync())
                {
                    await cookieBtn.ClickAsync();
                    await Task.Delay(500, ct);
                }
            }
            catch { }

            // Ilan satirlarini al
            var rows = await page.Locator("tr.searchResultsItem, tbody tr[data-id]").AllAsync();
            _logger.LogInformation("Sahibinden: {Count} satir bulundu", rows.Count);
            
            // Tarih filtresi icin min tarih
            var minTarih = request.IlanTarihGun > 0 ? DateTime.Today.AddDays(-request.IlanTarihGun) : DateTime.MinValue;

            foreach (var row in rows.Take(30))
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

                    // Ilan ID
                    ilan.IlanNo = await row.GetAttributeAsync("data-id") ?? "";

                    // Link ve Baslik
                    try
                    {
                        var link = row.Locator("td.searchResultsTitleValue a").First;
                        var href = await link.GetAttributeAsync("href") ?? "";
                        ilan.IlanUrl = href.StartsWith("http") ? href : $"https://www.sahibinden.com{href}";
                        ilan.IlanBasligi = (await link.TextContentAsync() ?? "").Trim();
                    }
                    catch { continue; }

                    // Fiyat
                    try
                    {
                        var fiyatText = await row.Locator("td.searchResultsPriceValue").TextContentAsync() ?? "";
                        ilan.Fiyat = ParseFiyat(fiyatText);
                    }
                    catch { }

                    // Yil, KM, Renk
                    try
                    {
                        var attrs = await row.Locator("td.searchResultsAttributeValue").AllAsync();
                        if (attrs.Count >= 1) ilan.ModelYili = ParseYil(await attrs[0].TextContentAsync() ?? "");
                        if (attrs.Count >= 2) ilan.Kilometre = ParseKilometre(await attrs[1].TextContentAsync() ?? "");
                        if (attrs.Count >= 3) ilan.Renk = (await attrs[2].TextContentAsync() ?? "").Trim();
                    }
                    catch { }

                    // Resim
                    try
                    {
                        var img = row.Locator("td.searchResultsLargeThumbnail img").First;
                        var imgSrc = await img.GetAttributeAsync("src") ?? await img.GetAttributeAsync("data-src") ?? "";
                        if (!string.IsNullOrEmpty(imgSrc) && !imgSrc.Contains("placeholder"))
                        {
                            // Buyuk resme cevir
                            imgSrc = imgSrc.Replace("_thmb.", "_x5.").Replace("/thumbs/", "/");
                            ilan.ResimUrl = imgSrc.StartsWith("http") ? imgSrc : $"https:{imgSrc}";
                        }
                    }
                    catch { }

                    // Konum
                    try
                    {
                        var locText = await row.Locator("td.searchResultsLocationValue").TextContentAsync() ?? "";
                        var locParts = locText.Split('\n');
                        ilan.Sehir = locParts.FirstOrDefault()?.Trim();
                        if (locParts.Length > 1) ilan.Ilce = locParts[1].Trim();
                    }
                    catch { }

                    // Tarih
                    try
                    {
                        var dateText = await row.Locator("td.searchResultsDateValue").TextContentAsync() ?? "";
                        ilan.IlanTarihi = ParseTarih(dateText);
                    }
                    catch { ilan.IlanTarihi = DateTime.Today; }

                    // Tarih filtresi uygula
                    if (ilan.IlanTarihi < minTarih)
                    {
                        _logger.LogInformation("Ilan tarihi filtre nedeniyle atlandi: {IlanNo} - {Tarih}", ilan.IlanNo, ilan.IlanTarihi);
                        continue;
                    }

                    if (ilan.Fiyat > 0 && !string.IsNullOrEmpty(ilan.IlanUrl))
                    {
                        _logger.LogDebug("Sahibinden ilan: {IlanNo} - {Baslik}", ilan.IlanNo, ilan.IlanBasligi);
                        ilanlar.Add(ilan);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Sahibinden ilan parse hatasi");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Sahibinden tarama hatasi");
        }

        return ilanlar;
    }

    #endregion

    #region Arabam.com

    private async Task<List<PiyasaArastirmaIlan>> TaraArabam(KaynakTanim kaynak, AracPiyasaArastirmaRequest request, IPage page, CancellationToken ct)
    {
        var ilanlar = new List<PiyasaArastirmaIlan>();
        if (_stopRequested || ct.IsCancellationRequested) return ilanlar;

        try
        {
            var marka = Slugify(request.Marka);
            var model = Slugify(request.Model);
            
            // Arabam.com arama sayfasi
            var url = $"https://www.arabam.com/ikinci-el/{marka}-{model}?sort=3";
            if (request.YilBaslangic.HasValue) url += $"&minYear={request.YilBaslangic}";
            if (request.YilBitis.HasValue) url += $"&maxYear={request.YilBitis}";
            
            // Ilan tarihi filtresi
            if (request.IlanTarihGun > 0 && request.IlanTarihGun <= 1)
                url += "&listingDate=1";
            else if (request.IlanTarihGun <= 3)
                url += "&listingDate=3";
            else if (request.IlanTarihGun <= 7)
                url += "&listingDate=7";
            else if (request.IlanTarihGun <= 15)
                url += "&listingDate=15";
            else if (request.IlanTarihGun <= 30)
                url += "&listingDate=30";

            _logger.LogInformation("Arabam URL: {Url}", url);

            await page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
            await Task.Delay(3000, ct);

            // Cookie kabul
            try
            {
                var cookieBtn = page.Locator("button[data-testid='accept-all-cookies'], #onetrust-accept-btn-handler").First;
                if (await cookieBtn.IsVisibleAsync())
                {
                    await cookieBtn.ClickAsync();
                    await Task.Delay(1000, ct);
                }
            }
            catch { }

            await Task.Delay(2000, ct);

            // YONTEM: Ilan satirlarini bul ve her birine tiklayarak gercek URL'yi al
            var ilanRows = await page.Locator("tr.listing-list-item").AllAsync();
            _logger.LogInformation("Arabam: {Count} ilan satiri bulundu", ilanRows.Count);

            if (ilanRows.Count == 0)
            {
                // Alternatif selector dene
                ilanRows = await page.Locator("[data-testid='listing-item'], .classified-list-item").AllAsync();
                _logger.LogInformation("Arabam alternatif: {Count} ilan satiri bulundu", ilanRows.Count);
            }

            var processedIlanNos = new HashSet<string>();
            var minTarih = request.IlanTarihGun > 0 ? DateTime.Today.AddDays(-request.IlanTarihGun) : DateTime.MinValue;

            foreach (var row in ilanRows.Take(30))
            {
                if (_stopRequested || ct.IsCancellationRequested) break;

                try
                {
                    // Row'dan data attribute'lari ve text bilgilerini al
                    var dataId = await row.GetAttributeAsync("data-id") ?? "";
                    var rowText = await row.TextContentAsync() ?? "";
                    
                    // Ilan numarasini bul
                    string ilanNo = "";
                    if (!string.IsNullOrEmpty(dataId) && dataId.All(char.IsDigit) && dataId.Length >= 7)
                    {
                        ilanNo = dataId;
                    }
                    else
                    {
                        // Link'ten ilan numarasini cek
                        var linkEl = row.Locator("a[href*='/ilan/']").First;
                        var href = await linkEl.GetAttributeAsync("href") ?? "";
                        var match = Regex.Match(href, @"/(\d{7,})");
                        if (match.Success)
                        {
                            ilanNo = match.Groups[1].Value;
                        }
                    }
                    
                    if (string.IsNullOrEmpty(ilanNo)) continue;
                    if (processedIlanNos.Contains(ilanNo)) continue;
                    processedIlanNos.Add(ilanNo);

                    // Baslik ve diger bilgileri al
                    string baslik = "";
                    try
                    {
                        var titleEl = row.Locator("h3, .listing-title, [class*='title'] span").First;
                        baslik = (await titleEl.TextContentAsync() ?? "").Trim();
                    }
                    catch { }
                    
                    if (string.IsNullOrEmpty(baslik))
                    {
                        baslik = $"{request.Marka} {request.Model}";
                    }

                    // Resim URL'sini al
                    string resimUrl = "";
                    try
                    {
                        var img = row.Locator("img").First;
                        resimUrl = await img.GetAttributeAsync("src") ?? await img.GetAttributeAsync("data-src") ?? "";
                        if (!string.IsNullOrEmpty(resimUrl) && !resimUrl.Contains("placeholder") && !resimUrl.Contains("logo"))
                        {
                            resimUrl = Regex.Replace(resimUrl, @"_\d+x\d+\.", "_580x435.");
                            if (!resimUrl.StartsWith("http")) resimUrl = $"https:{resimUrl}";
                        }
                    }
                    catch { }

                    // GERCEK URL'yi almak icin: Link'e tikla ve URL'yi al
                    // Alternatif: Sabit URL formati kullan (calisir ama SEO-friendly degil)
                    // Arabam.com ilan numarasi ile direkt erisim destekliyor:
                    // https://www.arabam.com/ilan/detay/38132099 -> Dogru sayfaya yonlendirir
                    
                    // En guvenilir yontem: Ilan numarasi ile standart URL
                    var ilanUrl = $"https://www.arabam.com/ilan/detay/{ilanNo}";

                    var ilan = new PiyasaArastirmaIlan
                    {
                        Kaynak = kaynak.Ad,
                        ToplanmaTarihi = DateTime.Now,
                        AktifMi = true,
                        Marka = request.Marka,
                        Model = request.Model,
                        IlanNo = ilanNo,
                        IlanUrl = ilanUrl,
                        IlanBasligi = baslik,
                        ResimUrl = resimUrl,
                        Fiyat = ParseFiyat(rowText),
                        ModelYili = ParseYil(rowText),
                        Kilometre = ParseKilometre(rowText),
                        IlanTarihi = DateTime.Today
                    };

                    if (ilan.Fiyat > 0)
                    {
                        _logger.LogDebug("Arabam ilan: No={IlanNo}, URL={Url}, Fiyat={Fiyat}", 
                            ilan.IlanNo, ilan.IlanUrl, ilan.Fiyat);
                        ilanlar.Add(ilan);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Arabam ilan parse hatasi");
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

    #region Genel Kaynak

    private async Task<List<PiyasaArastirmaIlan>> TaraGenelKaynak(KaynakTanim kaynak, AracPiyasaArastirmaRequest request, IPage page, CancellationToken ct)
    {
        var ilanlar = new List<PiyasaArastirmaIlan>();
        if (_stopRequested || ct.IsCancellationRequested) return ilanlar;

        try
        {
            var url = $"{kaynak.BaseUrl}/arac-listesi?marka={Slugify(request.Marka)}";

            await page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
            await Task.Delay(2000, ct);

            // Cookie kabul
            try
            {
                var cookieBtn = page.Locator("[class*='cookie'] button, .cookie-accept, #accept-cookies").First;
                if (await cookieBtn.IsVisibleAsync())
                {
                    await cookieBtn.ClickAsync();
                    await Task.Delay(500, ct);
                }
            }
            catch { }

            var elements = await page.Locator(".car-card, .vehicle-card, .listing-item, [class*='vehicle-item'], [class*='car-item']").AllAsync();

            foreach (var el in elements.Take(15))
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
                        Model = request.Model,
                        SaticiTipi = kaynak.Tip == "YetkiliBayi" ? "Yetkili Bayi" : kaynak.Tip == "Galeri" ? "Galeri" : "Bireysel"
                    };

                    // Link
                    try
                    {
                        var linkEl = el.Locator("a[href*='/arac'], a[href*='/ilan'], a[href*='/detay'], a").First;
                        var href = await linkEl.GetAttributeAsync("href") ?? "";
                        if (!string.IsNullOrEmpty(href))
                        {
                            ilan.IlanUrl = href.StartsWith("http") ? href : $"{kaynak.BaseUrl}{(href.StartsWith("/") ? "" : "/")}{href}";
                            var match = Regex.Match(href, @"[/-](\d{4,})");
                            if (match.Success) ilan.IlanNo = match.Groups[1].Value;
                        }
                    }
                    catch { continue; }

                    // Baslik
                    try
                    {
                        var titleEl = el.Locator("h2, h3, h4, .title, [class*='title']").First;
                        ilan.IlanBasligi = (await titleEl.TextContentAsync() ?? "").Trim();
                    }
                    catch { ilan.IlanBasligi = $"{request.Marka} {request.Model}"; }

                    // Fiyat
                    var text = await el.TextContentAsync() ?? "";
                    ilan.Fiyat = ParseFiyat(text);
                    ilan.ModelYili = ParseYil(text);
                    ilan.Kilometre = ParseKilometre(text);

                    // Resim
                    try
                    {
                        var imgEl = el.Locator("img").First;
                        var imgSrc = await imgEl.GetAttributeAsync("src") ?? await imgEl.GetAttributeAsync("data-src") ?? "";
                        if (!string.IsNullOrEmpty(imgSrc) && !imgSrc.Contains("placeholder") && !imgSrc.Contains("logo"))
                        {
                            ilan.ResimUrl = imgSrc.StartsWith("http") ? imgSrc : $"https:{imgSrc}";
                        }
                    }
                    catch { }

                    ilan.IlanTarihi = DateTime.Today;

                    if (ilan.Fiyat > 0)
                        ilanlar.Add(ilan);
                }
                catch { }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "{Kaynak} genel tarama hatasi", kaynak.Ad);
        }

        return ilanlar;
    }

    #endregion

    #region Fotograf Cekme

    public async Task<List<string>> IlanFotograflariniCekAsync(string ilanUrl, string kaynak)
    {
        var fotograflar = new List<string>();
        if (string.IsNullOrEmpty(ilanUrl)) return fotograflar;

        try
        {
            var browser = await GetBrowserAsync();
            var context = await browser.NewContextAsync(new BrowserNewContextOptions
            {
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36"
            });
            var page = await context.NewPageAsync();

            await page.GotoAsync(ilanUrl, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
            await Task.Delay(2000);

            // Cookie kabul
            try
            {
                var cookieBtn = page.Locator("#onetrust-accept-btn-handler, button[data-testid='accept-all-cookies']").First;
                if (await cookieBtn.IsVisibleAsync())
                {
                    await cookieBtn.ClickAsync();
                    await Task.Delay(500);
                }
            }
            catch { }

            // Kaynak bazli selector'lar
            var selectors = new List<string>();

            if (ilanUrl.Contains("sahibinden.com"))
            {
                selectors.AddRange(new[]
                {
                    ".classifiedDetailMainPhoto img",
                    ".classified-gallery img",
                    "[data-fancybox] img",
                    ".galleria-thumbnails img"
                });
            }
            else if (ilanUrl.Contains("arabam.com"))
            {
                selectors.AddRange(new[]
                {
                    ".gallery-thumbs img",
                    ".swiper-slide img",
                    "[class*='gallery'] img",
                    ".detail-slider img"
                });
            }
            else
            {
                selectors.AddRange(new[]
                {
                    ".gallery img",
                    ".slider img",
                    ".swiper-slide img",
                    "[class*='gallery'] img",
                    "[class*='photo'] img"
                });
            }

            foreach (var selector in selectors)
            {
                try
                {
                    var imgs = await page.Locator(selector).AllAsync();
                    foreach (var img in imgs)
                    {
                        var src = await img.GetAttributeAsync("src") ?? 
                                  await img.GetAttributeAsync("data-src") ?? 
                                  await img.GetAttributeAsync("data-original") ?? "";

                        if (IsValidImageUrl(src))
                        {
                            // Buyuk versiyona cevir
                            if (ilanUrl.Contains("sahibinden.com"))
                            {
                                src = src.Replace("_thmb.", "_x5.").Replace("/thumbs/", "/");
                            }
                            else if (ilanUrl.Contains("arabam.com"))
                            {
                                src = Regex.Replace(src, @"_\d+x\d+\.", "_580x435.");
                            }

                            if (!src.StartsWith("http")) src = $"https:{src}";
                            if (!fotograflar.Contains(src)) fotograflar.Add(src);
                        }
                    }
                }
                catch { }
            }

            await page.CloseAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Fotograf cekme hatasi: {Url}", ilanUrl);
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

        // "1.850.000 TL" veya "1.850.000?" formati
        var match = Regex.Match(text, @"([\d\.]+)\s*(?:TL|?|tl)", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            var fiyatStr = match.Groups[1].Value.Replace(".", "");
            if (decimal.TryParse(fiyatStr, out var fiyat)) return fiyat;
        }

        // Sadece rakam gruplari
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

        if (lowerText.Contains("bugun") || lowerText.Contains("bugün"))
            return DateTime.Today;

        if (lowerText.Contains("dun") || lowerText.Contains("dün"))
            return DateTime.Today.AddDays(-1);

        var gunMatch = Regex.Match(lowerText, @"(\d+)\s*gun\s*once");
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



