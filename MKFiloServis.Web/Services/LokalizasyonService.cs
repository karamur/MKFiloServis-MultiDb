using MKFiloServis.Web.Services.Interfaces;
using System.Collections.Concurrent;
using System.Text.Json;

namespace MKFiloServis.Web.Services;

/// <summary>
/// Lightweight i18n service. Dil dosyaları wwwroot/i18n/{kod}.json'dan yüklenir.
/// localStorage'da 'crm-lang' anahtarıyla saklanır (JS interop ile).
/// </summary>
public class LokalizasyonService : ILokalizasyonService
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<LokalizasyonService> _logger;
    private static readonly ConcurrentDictionary<string, Dictionary<string, string>> _cache = new();
    private Dictionary<string, string> _aktifSozluk = new();

    public string AktifDil { get; private set; } = "tr";

    public IReadOnlyList<(string Kod, string Ad)> DesteklenenDiller { get; } = new List<(string, string)>
    {
        ("tr", "Türkçe"),
        ("en", "English"),
    };

    public LokalizasyonService(IWebHostEnvironment env, ILogger<LokalizasyonService> logger)
    {
        _env = env;
        _logger = logger;
        _ = YukleDilAsync("tr");
    }

    public string T(string anahtar)
    {
        if (_aktifSozluk.TryGetValue(anahtar, out var deger))
            return deger;
        return anahtar;
    }

    public async Task DilDegistirAsync(string dilKodu)
    {
        if (!DesteklenenDiller.Any(d => d.Kod == dilKodu)) return;
        AktifDil = dilKodu;
        await YukleDilAsync(dilKodu);
    }

    private async Task YukleDilAsync(string dilKodu)
    {
        if (_cache.TryGetValue(dilKodu, out var cached))
        {
            _aktifSozluk = cached;
            return;
        }

        try
        {
            var dosya = Path.Combine(_env.WebRootPath, "i18n", $"{dilKodu}.json");
            if (!File.Exists(dosya))
            {
                _aktifSozluk = new();
                return;
            }
            var json = await File.ReadAllTextAsync(dosya);
            var sozluk = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new();
            _cache[dilKodu] = sozluk;
            _aktifSozluk = sozluk;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Dil dosyası yüklenemedi: {Dil}", dilKodu);
            _aktifSozluk = new();
        }
    }
}


