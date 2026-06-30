using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Web.Services;

public interface IPythonScraperService
{
    Task<List<PiyasaArastirmaIlan>> TaraAsync(AracPiyasaArastirmaRequest request, IProgress<string>? progress = null, CancellationToken ct = default);
    Task<bool> SaglikKontroluAsync();
}

public class PythonScraperService : IPythonScraperService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PythonScraperService> _logger;
    private readonly string _baseUrl;

    public PythonScraperService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<PythonScraperService> logger)
    {
        _httpClient = httpClientFactory.CreateClient();
        _httpClient.Timeout = TimeSpan.FromMinutes(5); // Uzun timeout
        _logger = logger;
        _baseUrl = configuration["PythonScraper:BaseUrl"] ?? "http://localhost:5050";
    }

    public async Task<bool> SaglikKontroluAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/health");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<PiyasaArastirmaIlan>> TaraAsync(AracPiyasaArastirmaRequest request, IProgress<string>? progress = null, CancellationToken ct = default)
    {
        var ilanlar = new List<PiyasaArastirmaIlan>();

        try
        {
            progress?.Report("Python Scraper'a baglaniliyor...");

            // Saglik kontrolu
            if (!await SaglikKontroluAsync())
            {
                progress?.Report("Python Scraper servisi çalışmıyor! Lütfen start_scraper.bat çalıştırın.");
                _logger.LogWarning("Python Scraper servisi çalışmıyor");
                return ilanlar;
            }

            progress?.Report("Sahibinden ve Arabam taraniyor...");

            var requestData = new PythonScraperRequest
            {
                Marka = request.Marka,
                Model = request.Model,
                IlanTarihGun = request.IlanTarihGun,
                YilMin = request.YilBaslangic,
                YilMax = request.YilBitis,
                Kaynaklar = new List<string> { "sahibinden", "arabam" }
            };

            var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/api/tara", requestData, ct);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<PythonScraperResponse>(cancellationToken: ct);

                if (result?.Ilanlar != null)
                {
                    foreach (var pyIlan in result.Ilanlar)
                    {
                        var ilan = new PiyasaArastirmaIlan
                        {
                            Kaynak = pyIlan.Kaynak ?? "",
                            IlanNo = pyIlan.IlanNo ?? "",
                            IlanBasligi = pyIlan.Baslik ?? "",
                            IlanUrl = pyIlan.IlanUrl ?? "",
                            ResimUrl = pyIlan.ResimUrl ?? "",
                            Marka = pyIlan.Marka ?? request.Marka,
                            Model = pyIlan.Model ?? request.Model,
                            ModelYili = pyIlan.Yil,
                            Kilometre = pyIlan.Kilometre,
                            Fiyat = pyIlan.Fiyat,
                            Sehir = pyIlan.Sehir ?? "",
                            Ilce = pyIlan.Ilce ?? "",
                            Renk = pyIlan.Renk ?? "",
                            ToplanmaTarihi = DateTime.Now,
                            AktifMi = true
                        };

                        if (DateTime.TryParse(pyIlan.IlanTarihi, out var ilanTarihi))
                        {
                            ilan.IlanTarihi = ilanTarihi;
                        }
                        else
                        {
                            ilan.IlanTarihi = DateTime.Today;
                        }

                        ilanlar.Add(ilan);
                    }

                    progress?.Report($"Toplam {ilanlar.Count} ilan bulundu (Ort: {result.OrtalamaFiyat:N0} TL)");
                }
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError("Python Scraper hatasi: {Error}", error);
                progress?.Report($"Hata: {error}");
            }
        }
        catch (TaskCanceledException)
        {
            progress?.Report("Tarama iptal edildi");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Python Scraper cagri hatasi");
            progress?.Report($"Hata: {ex.Message}");
        }

        return ilanlar;
    }
}

// Python API Request/Response modelleri
public class PythonScraperRequest
{
    [JsonPropertyName("marka")]
    public string Marka { get; set; } = "";

    [JsonPropertyName("model")]
    public string Model { get; set; } = "";

    [JsonPropertyName("ilan_tarih_gun")]
    public int IlanTarihGun { get; set; } = 2;

    [JsonPropertyName("yil_min")]
    public int? YilMin { get; set; }

    [JsonPropertyName("yil_max")]
    public int? YilMax { get; set; }

    [JsonPropertyName("kaynaklar")]
    public List<string> Kaynaklar { get; set; } = new();
}

public class PythonScraperResponse
{
    [JsonPropertyName("toplam_ilan")]
    public int ToplamIlan { get; set; }

    [JsonPropertyName("ortalama_fiyat")]
    public decimal OrtalamaFiyat { get; set; }

    [JsonPropertyName("en_dusuk_fiyat")]
    public decimal EnDusukFiyat { get; set; }

    [JsonPropertyName("en_yuksek_fiyat")]
    public decimal EnYuksekFiyat { get; set; }

    [JsonPropertyName("ilanlar")]
    public List<PythonIlan>? Ilanlar { get; set; }
}

public class PythonIlan
{
    [JsonPropertyName("kaynak")]
    public string? Kaynak { get; set; }

    [JsonPropertyName("ilan_no")]
    public string? IlanNo { get; set; }

    [JsonPropertyName("baslik")]
    public string? Baslik { get; set; }

    [JsonPropertyName("ilan_url")]
    public string? IlanUrl { get; set; }

    [JsonPropertyName("resim_url")]
    public string? ResimUrl { get; set; }

    [JsonPropertyName("marka")]
    public string? Marka { get; set; }

    [JsonPropertyName("model")]
    public string? Model { get; set; }

    [JsonPropertyName("yil")]
    public int Yil { get; set; }

    [JsonPropertyName("kilometre")]
    public int Kilometre { get; set; }

    [JsonPropertyName("fiyat")]
    public decimal Fiyat { get; set; }

    [JsonPropertyName("sehir")]
    public string? Sehir { get; set; }

    [JsonPropertyName("ilce")]
    public string? Ilce { get; set; }

    [JsonPropertyName("renk")]
    public string? Renk { get; set; }

    [JsonPropertyName("ilan_tarihi")]
    public string? IlanTarihi { get; set; }
}



