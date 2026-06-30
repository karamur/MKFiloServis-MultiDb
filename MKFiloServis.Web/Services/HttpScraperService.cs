using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Web;
using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Web.Services;

/// <summary>
/// HTTP API kullanarak Sahibinden, Arabam ve diger kaynaklardan veri ceker
/// </summary>
public interface IHttpScraperService
{
    Task<List<PiyasaArastirmaIlan>> TaraAsync(AracPiyasaArastirmaRequest request, IProgress<string>? progress = null, CancellationToken ct = default);
}

public class HttpScraperService : IHttpScraperService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HttpScraperService> _logger;
    private readonly IPiyasaKaynakService _kaynakService;

    public HttpScraperService(
        IHttpClientFactory httpClientFactory,
        ILogger<HttpScraperService> logger,
        IPiyasaKaynakService kaynakService)
    {
        _httpClient = httpClientFactory.CreateClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        _httpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
        _httpClient.DefaultRequestHeaders.Add("Accept-Language", "tr-TR,tr;q=0.9,en-US;q=0.8,en;q=0.7");
        _logger = logger;
        _kaynakService = kaynakService;
    }

    public async Task<List<PiyasaArastirmaIlan>> TaraAsync(AracPiyasaArastirmaRequest request, IProgress<string>? progress = null, CancellationToken ct = default)
    {
        var tumIlanlar = new List<PiyasaArastirmaIlan>();

        // Aktif kaynaklari al
        var aktifKaynaklar = await _kaynakService.GetAktifKaynaklarAsync();
        var kaynakYok = !aktifKaynaklar.Any();

        try
        {
            // Sahibinden
            if (kaynakYok || aktifKaynaklar.Any(k => k.Kod.ToLower() == "sahibinden" && k.Aktif))
            {
                progress?.Report("Sahibinden.com taraniyor...");
                var ilanlar = await TaraSahibinden(request, ct);
                tumIlanlar.AddRange(ilanlar);
                progress?.Report($"Sahibinden: {ilanlar.Count} ilan bulundu");
            }

            // Arabam
            if (kaynakYok || aktifKaynaklar.Any(k => k.Kod.ToLower() == "arabam" && k.Aktif))
            {
                progress?.Report("Arabam.com taraniyor...");
                var ilanlar = await TaraArabam(request, ct);
                tumIlanlar.AddRange(ilanlar);
                progress?.Report($"Arabam: {ilanlar.Count} ilan bulundu");
            }

            // Otoshops
            if (aktifKaynaklar.Any(k => k.Kod.ToLower() == "otoshops" && k.Aktif))
            {
                progress?.Report("Otoshops taraniyor...");
                var ilanlar = await TaraOtoshops(request, ct);
                tumIlanlar.AddRange(ilanlar);
                progress?.Report($"Otoshops: {ilanlar.Count} ilan bulundu");
            }

            // Cardata (opsiyonel)
            if (aktifKaynaklar.Any(k => k.Kod.ToLower() == "cardata" && k.Aktif))
            {
                progress?.Report("Cardata taraniyor...");
                var ilanlar = await TaraCardata(request, ct);
                tumIlanlar.AddRange(ilanlar);
                progress?.Report($"Cardata: {ilanlar.Count} ilan bulundu");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HTTP Scraper hatasi");
            progress?.Report($"Hata: {ex.Message}");
        }

        progress?.Report($"Toplam {tumIlanlar.Count} ilan bulundu");
        return tumIlanlar;
    }

    #region Sahibinden

    private async Task<List<PiyasaArastirmaIlan>> TaraSahibinden(AracPiyasaArastirmaRequest request, CancellationToken ct)
    {
        var ilanlar = new List<PiyasaArastirmaIlan>();

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

            if (request.IlanTarihGun > 0)
            {
                if (request.IlanTarihGun <= 1) url += "&date=1day";
                else if (request.IlanTarihGun <= 3) url += "&date=3days";
                else if (request.IlanTarihGun <= 7) url += "&date=7days";
                else if (request.IlanTarihGun <= 15) url += "&date=15days";
                else if (request.IlanTarihGun <= 30) url += "&date=30days";
            }

            _logger.LogInformation("Sahibinden URL: {Url}", url);

            var response = await _httpClient.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode) return ilanlar;

            var html = await response.Content.ReadAsStringAsync(ct);

            // TR satirlerini bul - daha genis pattern
            var trPattern = @"<tr\s+class=""searchResultsItem""[^>]*data-id=""(\d+)""[^>]*>(.*?)</tr>";
            var trMatches = Regex.Matches(html, trPattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);

            _logger.LogInformation("Sahibinden: {Count} TR satiri bulundu", trMatches.Count);

            foreach (Match trMatch in trMatches)
            {
                if (ct.IsCancellationRequested) break;

                try
                {
                    var ilanId = trMatch.Groups[1].Value;
                    var trContent = trMatch.Groups[2].Value;

                    // Gercek URL'yi HTML'den cek
                    var realUrlMatch = Regex.Match(trContent, @"href=""(/ilan/[^""]+)""", RegexOptions.IgnoreCase);
                    var ilanUrl = realUrlMatch.Success 
                        ? $"https://www.sahibinden.com{realUrlMatch.Groups[1].Value}"
                        : $"https://www.sahibinden.com/ilan/{ilanId}";

                    var ilan = new PiyasaArastirmaIlan
                    {
                        Kaynak = "Sahibinden.com",
                        IlanNo = ilanId,
                        IlanUrl = ilanUrl,
                        Marka = request.Marka,
                        Model = request.Model,
                        ToplanmaTarihi = DateTime.Now,
                        AktifMi = true,
                        IlanTarihi = DateTime.Today
                    };

                    // Baslik - classifiedTitle icindeki text
                    var titleMatch = Regex.Match(trContent, @"class=""[^""]*classifiedTitle[^""]*""[^>]*>([^<]+)", RegexOptions.Singleline);
                    if (titleMatch.Success)
                    {
                        ilan.IlanBasligi = titleMatch.Groups[1].Value.Trim();
                    }
                    else
                    {
                        // Alternatif: a tag icinde title attribute
                        var altTitle = Regex.Match(trContent, @"title=""([^""]+)""");
                        if (altTitle.Success)
                            ilan.IlanBasligi = altTitle.Groups[1].Value.Trim();
                        else
                            ilan.IlanBasligi = $"{request.Marka} {request.Model}";
                    }

                    // Fiyat - searchResultsPriceValue
                    var priceMatch = Regex.Match(trContent, @"searchResultsPriceValue[^>]*>.*?(\d[\d\.,]+)\s*(?:TL|?)?", RegexOptions.Singleline | RegexOptions.IgnoreCase);
                    if (priceMatch.Success)
                    {
                        ilan.Fiyat = ParseFiyat(priceMatch.Groups[1].Value);
                    }

                    // Attribute'lar - td.searchResultsAttributeValue
                    var attrPattern = @"searchResultsAttributeValue[^>]*>\s*([^<]+?)\s*</td>";
                    var attrMatches = Regex.Matches(trContent, attrPattern, RegexOptions.Singleline);
                    
                    if (attrMatches.Count >= 1)
                    {
                        ilan.ModelYili = ParseYil(attrMatches[0].Groups[1].Value);
                    }
                    if (attrMatches.Count >= 2)
                    {
                        ilan.Kilometre = ParseKilometre(attrMatches[1].Groups[1].Value);
                    }
                    if (attrMatches.Count >= 3)
                    {
                        ilan.Renk = attrMatches[2].Groups[1].Value.Trim();
                    }

                    // Koltuk sayisi - basliktan veya icerikten cek
                    ilan.TasimaKapasitesi = ParseKoltukSayisi(ilan.IlanBasligi + " " + trContent);

                    // Konum - searchResultsLocationValue
                    var locMatch = Regex.Match(trContent, @"searchResultsLocationValue[^>]*>(.*?)</td>", RegexOptions.Singleline);
                    if (locMatch.Success)
                    {
                        var locText = StripHtml(locMatch.Groups[1].Value);
                        var locParts = locText.Split(new[] { '\n', '\r', '/' }, StringSplitOptions.RemoveEmptyEntries);
                        if (locParts.Length > 0) ilan.Sehir = locParts[0].Trim();
                        if (locParts.Length > 1) ilan.Ilce = locParts[1].Trim();
                    }

                    // Tarih - searchResultsDateValue
                    var dateMatch = Regex.Match(trContent, @"searchResultsDateValue[^>]*>(.*?)</td>", RegexOptions.Singleline);
                    if (dateMatch.Success)
                    {
                        ilan.IlanTarihi = ParseTarih(StripHtml(dateMatch.Groups[1].Value));
                    }

                    // Resim
                    var imgMatch = Regex.Match(trContent, @"<img[^>]*(?:data-src|src)=""([^""]+)""", RegexOptions.Singleline);
                    if (imgMatch.Success)
                    {
                        var imgSrc = imgMatch.Groups[1].Value;
                        if (!imgSrc.Contains("placeholder") && !imgSrc.Contains("logo") && imgSrc.Length > 20)
                        {
                            imgSrc = imgSrc.Replace("_thmb.", "_x5.").Replace("/thumbs/", "/");
                            ilan.ResimUrl = imgSrc.StartsWith("http") ? imgSrc : $"https:{imgSrc}";
                        }
                    }

                    // Satici tipi
                    if (trContent.Contains("fromStore") || trContent.Contains("galeri"))
                    {
                        ilan.SaticiTipi = "Galeri";
                    }
                    else
                    {
                        ilan.SaticiTipi = "Bireysel";
                    }

                    if (ilan.Fiyat > 0)
                    {
                        ilanlar.Add(ilan);
                        _logger.LogDebug("Sahibinden ilan: {IlanNo} - {Baslik} - {Fiyat} TL - {Yil} - {Km} km - Koltuk: {Koltuk}", 
                            ilan.IlanNo, ilan.IlanBasligi, ilan.Fiyat, ilan.ModelYili, ilan.Kilometre, ilan.TasimaKapasitesi);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Sahibinden ilan parse hatasi");
                }

                if (ilanlar.Count >= 30) break;
            }

            _logger.LogInformation("Sahibinden: {Count} ilan eklendi", ilanlar.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sahibinden tarama hatasi");
        }

        return ilanlar;
    }

    #endregion

    #region Arabam

    private async Task<List<PiyasaArastirmaIlan>> TaraArabam(AracPiyasaArastirmaRequest request, CancellationToken ct)
    {
        var ilanlar = new List<PiyasaArastirmaIlan>();

        try
        {
            var marka = Slugify(request.Marka);
            var model = Slugify(request.Model);

            var url = $"https://www.arabam.com/ikinci-el/{marka}-{model}?sort=3";
            if (request.YilBaslangic.HasValue) url += $"&minYear={request.YilBaslangic}";
            if (request.YilBitis.HasValue) url += $"&maxYear={request.YilBitis}";

            if (request.IlanTarihGun > 0)
            {
                if (request.IlanTarihGun <= 1) url += "&listingDate=1";
                else if (request.IlanTarihGun <= 3) url += "&listingDate=3";
                else if (request.IlanTarihGun <= 7) url += "&listingDate=7";
                else if (request.IlanTarihGun <= 15) url += "&listingDate=15";
                else if (request.IlanTarihGun <= 30) url += "&listingDate=30";
            }

            _logger.LogInformation("Arabam URL: {Url}", url);

            var response = await _httpClient.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode) return ilanlar;

            var html = await response.Content.ReadAsStringAsync(ct);

            // __NEXT_DATA__ JSON'unu dene (Next.js)
            var nextDataMatch = Regex.Match(html, @"<script\s+id=""__NEXT_DATA__""[^>]*>(.*?)</script>", RegexOptions.Singleline);
            if (nextDataMatch.Success)
            {
                try
                {
                    var jsonData = nextDataMatch.Groups[1].Value;
                    using var doc = JsonDocument.Parse(jsonData);
                    var root = doc.RootElement;

                    // props.pageProps.searchResult.listings
                    if (root.TryGetProperty("props", out var props) &&
                        props.TryGetProperty("pageProps", out var pageProps))
                    {
                        JsonElement listings = default;
                        
                        if (pageProps.TryGetProperty("searchResult", out var sr) && sr.TryGetProperty("listings", out listings))
                        { }
                        else if (pageProps.TryGetProperty("listings", out listings))
                        { }

                        if (listings.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var item in listings.EnumerateArray())
                            {
                                if (ct.IsCancellationRequested) break;
                                
                                var ilan = ParseArabamJsonItem(item, request);
                                if (ilan != null && ilan.Fiyat > 0)
                                {
                                    ilanlar.Add(ilan);
                                    _logger.LogDebug("Arabam ilan: {IlanNo} - {Baslik} - {Fiyat} TL - {Yil} - {Km} km", 
                                        ilan.IlanNo, ilan.IlanBasligi, ilan.Fiyat, ilan.ModelYili, ilan.Kilometre);
                                }
                                if (ilanlar.Count >= 30) break;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Arabam JSON parse hatasi, HTML'e geciliyor");
                }
            }

            // JSON calismadiysa HTML'den parse et
            if (!ilanlar.Any())
            {
                ilanlar = ParseArabamHtml(html, request);
            }

            _logger.LogInformation("Arabam: {Count} ilan eklendi", ilanlar.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Arabam tarama hatasi");
        }

        return ilanlar;
    }

    private PiyasaArastirmaIlan? ParseArabamJsonItem(JsonElement item, AracPiyasaArastirmaRequest request)
    {
        try
        {
            var ilan = new PiyasaArastirmaIlan
            {
                Kaynak = "Arabam.com",
                Marka = request.Marka,
                Model = request.Model,
                ToplanmaTarihi = DateTime.Now,
                AktifMi = true,
                IlanTarihi = DateTime.Today
            };

            // ID - farkli property isimleri dene
            ilan.IlanNo = GetJsonString(item, "id", "listingId", "classifiedId") ?? "";
            if (string.IsNullOrEmpty(ilan.IlanNo)) return null;

            // Baslik - once baslik al, URL icin gerekli
            ilan.IlanBasligi = GetJsonString(item, "title", "modelName", "name", "headline") 
                               ?? $"{request.Marka} {request.Model}";

            // Konum bilgisi - URL icin gerekli
            string sehir = "";
            if (item.TryGetProperty("location", out var locProp))
            {
                if (locProp.ValueKind == JsonValueKind.Object)
                {
                    sehir = GetJsonString(locProp, "cityName", "city", "il", "sehir") ?? "";
                    ilan.Sehir = sehir;
                    ilan.Ilce = GetJsonString(locProp, "townName", "town", "ilce", "district");
                }
                else if (locProp.ValueKind == JsonValueKind.String)
                {
                    sehir = locProp.GetString() ?? "";
                    ilan.Sehir = sehir;
                }
            }
            else
            {
                sehir = GetJsonString(item, "city", "cityName", "sehir") ?? "";
                ilan.Sehir = sehir;
                ilan.Ilce = GetJsonString(item, "town", "townName", "ilce");
            }

            // URL - Arabam.com'da ilanlar su formatta oluyor:
            // /ilan/galeriden-satilik-mercedes-benz-sprinter-416-cdi/gultekin-otomotiv-den-hatasiz-sprinter-iklim-stil/38117039
            // /ilan/sahibinden-satilik-toyota-corolla-1-4-d-4d-elegant/sahibinden-temiz-corolla/32174820
            
            // Format 1: /ilan/{satici-tipi-satilik-marka-model-versiyon}/{ilan-basligi}/{id}
            
            var saticiSlug = !string.IsNullOrEmpty(ilan.SaticiTipi) && ilan.SaticiTipi.ToLower() == "galeri" ? "galeriden" : "sahibinden";
            var aracSlug = Slugify($"{request.Marka}-{request.Model}");
            var baslikSlug = Slugify(ilan.IlanBasligi);
            
            // Ornek: galeriden-satilik-mercedes-benz-sprinter
            var kategoriSlug = $"{saticiSlug}-satilik-{aracSlug}";
            
            if (!string.IsNullOrEmpty(baslikSlug))
            {
                ilan.IlanUrl = $"https://www.arabam.com/ilan/{kategoriSlug}/{baslikSlug}/{ilan.IlanNo}";
            }
            else
            {
                // Fallback: Sadece ID ve Marka/Model
                ilan.IlanUrl = $"https://www.arabam.com/ilan/{kategoriSlug}/{ilan.IlanNo}";
            }

            // Fiyat
            if (item.TryGetProperty("price", out var priceProp))
            {
                if (priceProp.ValueKind == JsonValueKind.Number)
                    ilan.Fiyat = priceProp.GetDecimal();
                else if (priceProp.ValueKind == JsonValueKind.Object)
                {
                    ilan.Fiyat = GetJsonDecimal(priceProp, "value", "amount", "price") ?? 0;
                }
                else
                    ilan.Fiyat = ParseFiyat(priceProp.ToString());
            }
            else
            {
                var priceStr = GetJsonString(item, "priceFormatted", "formattedPrice", "priceText");
                if (!string.IsNullOrEmpty(priceStr))
                    ilan.Fiyat = ParseFiyat(priceStr);
            }

            // Yil
            ilan.ModelYili = GetJsonInt(item, "year", "modelYear", "productionYear") ?? 0;

            // KM
            ilan.Kilometre = GetJsonInt(item, "km", "mileage", "kilometer", "odometer") ?? 0;

            // Yakit
            ilan.YakitTipi = GetJsonString(item, "fuelType", "fuel", "fuelTypeName", "yakitTipi");

            // Vites
            ilan.VitesTipi = GetJsonString(item, "gearType", "gear", "transmission", "gearTypeName", "vitesTipi");

            // Koltuk sayisi - cok onemli!
            var koltuk = GetJsonInt(item, "seatCount", "seats", "numberOfSeats", "koltukSayisi");
            if (koltuk.HasValue && koltuk > 0)
            {
                ilan.TasimaKapasitesi = koltuk.ToString();
            }
            else
            {
                // Kapi sayisindan veya basliktan cikarmaya calis
                var kapi = GetJsonInt(item, "doorCount", "doors", "numberOfDoors");
                if (kapi.HasValue && kapi > 0)
                {
                    // 4 kapi = genellikle 5 koltuk, 2 kapi = genellikle 4 koltuk
                    ilan.TasimaKapasitesi = kapi >= 4 ? "5" : "4";
                }
                else
                {
                    ilan.TasimaKapasitesi = ParseKoltukSayisi(ilan.IlanBasligi ?? "");
                }
            }

            // Renk
            ilan.Renk = GetJsonString(item, "color", "colorName", "exteriorColor", "renk");

            // Konum zaten yukarida URL icin cekildi, tekrar cekmeye gerek yok

            // Resim
            var imgUrl = GetJsonString(item, "photo", "image", "photoUrl", "imageUrl", "thumbnail", "mainPhoto");
            if (!string.IsNullOrEmpty(imgUrl) && !imgUrl.Contains("placeholder"))
            {
                imgUrl = Regex.Replace(imgUrl, @"_\d+x\d+\.", "_580x435.");
                ilan.ResimUrl = imgUrl.StartsWith("http") ? imgUrl : $"https:{imgUrl}";
            }

            // Satici tipi
            var userType = GetJsonString(item, "userType", "sellerType", "advertiserType");
            ilan.SaticiTipi = userType?.ToLower().Contains("galeri") == true || 
                              userType?.ToLower().Contains("dealer") == true 
                              ? "Galeri" : "Bireysel";

            // Motor hacmi
            ilan.MotorHacmi = GetJsonString(item, "engineVolume", "engineSize", "cc", "motorHacmi");

            // Motor gucu
            ilan.MotorGucu = GetJsonString(item, "horsePower", "hp", "power", "motorGucu");

            // Kasa tipi
            ilan.KasaTipi = GetJsonString(item, "bodyType", "category", "kasaTipi");

            return ilan;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Arabam JSON item parse hatasi");
            return null;
        }
    }

    // JSON helper metodlari
    private string? GetJsonString(JsonElement element, params string[] propertyNames)
    {
        foreach (var name in propertyNames)
        {
            if (element.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.String)
            {
                var val = prop.GetString();
                if (!string.IsNullOrWhiteSpace(val))
                    return val;
            }
        }
        return null;
    }

    private int? GetJsonInt(JsonElement element, params string[] propertyNames)
    {
        foreach (var name in propertyNames)
        {
            if (element.TryGetProperty(name, out var prop))
            {
                if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt32(out var val))
                    return val;
                if (prop.ValueKind == JsonValueKind.String && int.TryParse(prop.GetString(), out var parsed))
                    return parsed;
            }
        }
        return null;
    }

    private decimal? GetJsonDecimal(JsonElement element, params string[] propertyNames)
    {
        foreach (var name in propertyNames)
        {
            if (element.TryGetProperty(name, out var prop))
            {
                if (prop.ValueKind == JsonValueKind.Number && prop.TryGetDecimal(out var val))
                    return val;
                if (prop.ValueKind == JsonValueKind.String)
                    return ParseFiyat(prop.GetString() ?? "");
            }
        }
        return null;
    }

    private List<PiyasaArastirmaIlan> ParseArabamHtml(string html, AracPiyasaArastirmaRequest request)
    {
        var ilanlar = new List<PiyasaArastirmaIlan>();

        try
        {
            // Ilan ID'lerini ve URL'lerini bul
            // href="/ilan/satilik-toyota-corolla/istanbul/12345678" formatinda
            // ONEMLI: detay icermeyen ve en az 2 slash iceren URL'leri yakala
            var linkMatches = Regex.Matches(html, @"href=""(/ilan/[a-z0-9-]+/[a-z0-9-]+/(\d{7,}))""", RegexOptions.IgnoreCase);
            var processedIds = new HashSet<string>();

            foreach (Match match in linkMatches)
            {
                var fullPath = match.Groups[1].Value; // /ilan/satilik-toyota-corolla/istanbul/12345678
                var ilanId = match.Groups[2].Value;   // 12345678
                
                // detay iceren URL'leri atla (case insensitive)
                if (fullPath.Contains("detay", StringComparison.OrdinalIgnoreCase))
                    continue;
                
                if (processedIds.Contains(ilanId)) continue;
                processedIds.Add(ilanId);

                // Bu ID etrafindaki HTML blogundan bilgileri cek
                var blockPattern = $@"(?:data-id=""{ilanId}""|href=""[^""]*{ilanId}[^""]*"")[^>]*>.*?(?=data-id=""\d|href=""/ilan/|$)";
                var blockMatch = Regex.Match(html, blockPattern, RegexOptions.Singleline);
                var blockContent = blockMatch.Success ? blockMatch.Value : "";

                var ilan = new PiyasaArastirmaIlan
                {
                    Kaynak = "Arabam.com",
                    IlanNo = ilanId,
                    IlanUrl = $"https://www.arabam.com{fullPath}", // Gercek URL'yi kullan
                    Marka = request.Marka,
                    Model = request.Model,
                    IlanBasligi = $"{request.Marka} {request.Model}",
                    ToplanmaTarihi = DateTime.Now,
                    AktifMi = true,
                    IlanTarihi = DateTime.Today,
                    Fiyat = ParseFiyat(blockContent),
                    ModelYili = ParseYil(blockContent),
                    Kilometre = ParseKilometre(blockContent),
                    TasimaKapasitesi = ParseKoltukSayisi(blockContent + " " + fullPath) // Koltuk sayisi
                };

                if (ilan.Fiyat > 0)
                    ilanlar.Add(ilan);

                if (ilanlar.Count >= 30) break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Arabam HTML parse hatasi");
        }

        return ilanlar;
    }

    #endregion

    #region Otoshops

    private async Task<List<PiyasaArastirmaIlan>> TaraOtoshops(AracPiyasaArastirmaRequest request, CancellationToken ct)
    {
        var ilanlar = new List<PiyasaArastirmaIlan>();

        try
        {
            var marka = Slugify(request.Marka);
            var model = Slugify(request.Model);

            var url = $"https://www.otoshops.com/ikinci-el-arac/{marka}/{model}";
            
            _logger.LogInformation("Otoshops URL: {Url}", url);

            var response = await _httpClient.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode) return ilanlar;

            var html = await response.Content.ReadAsStringAsync(ct);

            // Ilan kartlarini bul
            var cardPattern = @"<div[^>]*class=""[^""]*listing-card[^""]*""[^>]*>(.*?)</div>\s*</div>";
            var cardMatches = Regex.Matches(html, cardPattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);

            foreach (Match cardMatch in cardMatches)
            {
                if (ct.IsCancellationRequested) break;

                try
                {
                    var cardContent = cardMatch.Groups[1].Value;

                    var ilan = new PiyasaArastirmaIlan
                    {
                        Kaynak = "Otoshops",
                        Marka = request.Marka,
                        Model = request.Model,
                        ToplanmaTarihi = DateTime.Now,
                        AktifMi = true,
                        IlanTarihi = DateTime.Today
                    };

                    // URL ve ID
                    var linkMatch = Regex.Match(cardContent, @"href=""(/ilan/[^""]+)""");
                    if (linkMatch.Success)
                    {
                        ilan.IlanUrl = $"https://www.otoshops.com{linkMatch.Groups[1].Value}";
                        var idMatch = Regex.Match(linkMatch.Groups[1].Value, @"/(\d+)");
                        if (idMatch.Success)
                            ilan.IlanNo = idMatch.Groups[1].Value;
                    }

                    // Baslik
                    var titleMatch = Regex.Match(cardContent, @"<h[23][^>]*>(.*?)</h[23]>", RegexOptions.Singleline);
                    if (titleMatch.Success)
                        ilan.IlanBasligi = StripHtml(titleMatch.Groups[1].Value).Trim();
                    else
                        ilan.IlanBasligi = $"{request.Marka} {request.Model}";

                    // Fiyat, Yil, KM
                    ilan.Fiyat = ParseFiyat(cardContent);
                    ilan.ModelYili = ParseYil(cardContent);
                    ilan.Kilometre = ParseKilometre(cardContent);

                    // Resim
                    var imgMatch = Regex.Match(cardContent, @"<img[^>]*(?:data-src|src)=""([^""]+)""");
                    if (imgMatch.Success)
                    {
                        var imgUrl = imgMatch.Groups[1].Value;
                        if (!imgUrl.Contains("placeholder"))
                            ilan.ResimUrl = imgUrl.StartsWith("http") ? imgUrl : $"https://www.otoshops.com{imgUrl}";
                    }

                    if (ilan.Fiyat > 0 && !string.IsNullOrEmpty(ilan.IlanNo))
                        ilanlar.Add(ilan);
                }
                catch { }

                if (ilanlar.Count >= 20) break;
            }

            _logger.LogInformation("Otoshops: {Count} ilan eklendi", ilanlar.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Otoshops tarama hatasi");
        }

        return ilanlar;
    }

    #endregion

    #region Cardata

    private async Task<List<PiyasaArastirmaIlan>> TaraCardata(AracPiyasaArastirmaRequest request, CancellationToken ct)
    {
        var ilanlar = new List<PiyasaArastirmaIlan>();

        try
        {
            var marka = HttpUtility.UrlEncode(request.Marka);
            var model = HttpUtility.UrlEncode(request.Model);

            var url = $"https://www.cardata.com.tr/arac-ara?marka={marka}&model={model}";
            
            _logger.LogInformation("Cardata URL: {Url}", url);

            var response = await _httpClient.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode) return ilanlar;

            var html = await response.Content.ReadAsStringAsync(ct);

            // Ilan kartlarini bul
            var cardPattern = @"<div[^>]*class=""[^""]*car-card[^""]*""[^>]*>(.*?)</div>\s*</div>";
            var cardMatches = Regex.Matches(html, cardPattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);

            foreach (Match cardMatch in cardMatches)
            {
                if (ct.IsCancellationRequested) break;

                try
                {
                    var cardContent = cardMatch.Groups[1].Value;

                    var ilan = new PiyasaArastirmaIlan
                    {
                        Kaynak = "Cardata",
                        Marka = request.Marka,
                        Model = request.Model,
                        ToplanmaTarihi = DateTime.Now,
                        AktifMi = true,
                        IlanTarihi = DateTime.Today,
                        SaticiTipi = "Galeri"
                    };

                    // URL ve ID
                    var linkMatch = Regex.Match(cardContent, @"href=""([^""]+)""");
                    if (linkMatch.Success)
                    {
                        var href = linkMatch.Groups[1].Value;
                        ilan.IlanUrl = href.StartsWith("http") ? href : $"https://www.cardata.com.tr{href}";
                        var idMatch = Regex.Match(href, @"/(\d+)");
                        if (idMatch.Success)
                            ilan.IlanNo = idMatch.Groups[1].Value;
                    }

                    // Baslik
                    var titleMatch = Regex.Match(cardContent, @"<h[234][^>]*>(.*?)</h[234]>", RegexOptions.Singleline);
                    ilan.IlanBasligi = titleMatch.Success ? StripHtml(titleMatch.Groups[1].Value).Trim() : $"{request.Marka} {request.Model}";

                    // Fiyat, Yil, KM
                    ilan.Fiyat = ParseFiyat(cardContent);
                    ilan.ModelYili = ParseYil(cardContent);
                    ilan.Kilometre = ParseKilometre(cardContent);

                    // Resim
                    var imgMatch = Regex.Match(cardContent, @"<img[^>]*(?:data-src|src)=""([^""]+)""");
                    if (imgMatch.Success)
                    {
                        var imgUrl = imgMatch.Groups[1].Value;
                        if (!imgUrl.Contains("placeholder"))
                            ilan.ResimUrl = imgUrl.StartsWith("http") ? imgUrl : $"https://www.cardata.com.tr{imgUrl}";
                    }

                    if (ilan.Fiyat > 0 && !string.IsNullOrEmpty(ilan.IlanNo))
                        ilanlar.Add(ilan);
                }
                catch { }

                if (ilanlar.Count >= 20) break;
            }

            _logger.LogInformation("Cardata: {Count} ilan eklendi", ilanlar.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cardata tarama hatasi");
        }

        return ilanlar;
    }

    #endregion

    #region Helper Methods

    private string Slugify(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        return text.ToLower()
            .Replace("�", "i").Replace("�", "o").Replace("�", "u")
            .Replace("�", "s").Replace("�", "g").Replace("�", "c")
            .Replace(" ", "-").Replace(".", "").Replace(",", "")
            .Replace("--", "-").Trim('-');
    }

    private string StripHtml(string html)
    {
        if (string.IsNullOrEmpty(html)) return "";
        return Regex.Replace(html, "<[^>]*>", " ").Trim();
    }

    private decimal ParseFiyat(string text)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        
        // "1.850.000 TL" veya "1,850,000" formatlar�
        var match = Regex.Match(text, @"(\d{1,3}(?:[\.,]\d{3})+)(?:\s*(?:TL|?))?", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            var fiyatStr = match.Groups[1].Value.Replace(".", "").Replace(",", "");
            if (decimal.TryParse(fiyatStr, out var fiyat) && fiyat >= 50000)
                return fiyat;
        }

        // Tek sayi
        match = Regex.Match(text, @"(\d{6,})");
        if (match.Success && decimal.TryParse(match.Groups[1].Value, out var f) && f >= 50000)
            return f;

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
        
        // "125.000 km" veya "125,000 km" formatlar�
        var match = Regex.Match(text, @"(\d{1,3}(?:[\.,]\d{3})*)\s*km", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            var kmStr = match.Groups[1].Value.Replace(".", "").Replace(",", "");
            if (int.TryParse(kmStr, out var km) && km >= 100)
                return km;
        }
        return 0;
    }

    private string? ParseKoltukSayisi(string text)
    {
        if (string.IsNullOrEmpty(text)) return null;

        // "5 koltuk", "7 ki�ilik", "5+2", "5 kisilik" gibi formatlar
        var patterns = new[]
        {
            @"(\d)\s*(?:\+\s*\d)?\s*koltuk",           // 5 koltuk, 5+2 koltuk
            @"(\d)\s*(?:\+\s*\d)?\s*ki�ilik",          // 5 ki�ilik
            @"(\d)\s*(?:\+\s*\d)?\s*kisilik",          // 5 kisilik
            @"(\d)\s*\+\s*(\d)\s*koltuk",              // 5+2 koltuk -> 7
            @"(\d)\s*kap�",                            // 5 kap�
            @"(\d)\s*kapi",                            // 5 kapi
            @"\b([2-9])\s*(?:ki�i|kisi)\b",           // 5 ki�i
            @"(?:sedan|hatchback|station).+?(\d)\s*k", // sedan 5 kap�
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                // 5+2 format� i�in toplam hesapla
                if (match.Groups.Count > 2 && !string.IsNullOrEmpty(match.Groups[2].Value))
                {
                    if (int.TryParse(match.Groups[1].Value, out var a) && 
                        int.TryParse(match.Groups[2].Value, out var b))
                    {
                        return $"{a + b}";
                    }
                }
                return match.Groups[1].Value;
            }
        }

        // Arac tipine gore varsayilan koltuk sayisi
        var lowerText = text.ToLower();
        if (lowerText.Contains("suv") || lowerText.Contains("crossover"))
            return "5";
        if (lowerText.Contains("minivan") || lowerText.Contains("mpv"))
            return "7";
        if (lowerText.Contains("pickup") || lowerText.Contains("kamyonet"))
            return "5";
        if (lowerText.Contains("sedan") || lowerText.Contains("hatchback") || lowerText.Contains("coupe"))
            return "5";

        return null;
    }

    private DateTime ParseTarih(string text)
    {
        if (string.IsNullOrEmpty(text)) return DateTime.Today;

        var lowerText = text.ToLower().Trim();

        if (lowerText.Contains("bug�n") || lowerText.Contains("bugun"))
            return DateTime.Today;

        if (lowerText.Contains("d�n") || lowerText.Contains("dun"))
            return DateTime.Today.AddDays(-1);

        var gunMatch = Regex.Match(lowerText, @"(\d+)\s*(?:g�n|gun)\s*(?:�nce|once)");
        if (gunMatch.Success && int.TryParse(gunMatch.Groups[1].Value, out var gun))
            return DateTime.Today.AddDays(-gun);

        var tarihMatch = Regex.Match(text, @"(\d{2})[\./](\d{2})[\./](\d{4})");
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

    #endregion
}



