using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MKFiloServis.Web.Data;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Web.Services;

/// <summary>Yalnizca bilgisayardaki Ollama sureciyle konusur; model indirme veya uzak API yoktur.</summary>
public sealed class OllamaService : IOllamaService
{
    private readonly HttpClient _client;
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly bool _enabled;
    public string ModelAdi { get; }
    public string EmbeddingModelAdi { get; }

    public OllamaService(HttpClient client, IConfiguration configuration, IDbContextFactory<ApplicationDbContext> dbFactory)
    {
        var address = configuration["Ollama:BaseUrl"] ?? "http://127.0.0.1:11434";
        if (!Uri.TryCreate(address, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttp ||
            !(IPAddress.TryParse(uri.Host, out var ip) && IPAddress.IsLoopback(ip)) ||
            uri.UserInfo.Length != 0 || uri.Query.Length != 0 || uri.Fragment.Length != 0 ||
            uri.AbsolutePath != "/")
            throw new InvalidOperationException("Yerel AI adresi yalnizca bilgisayarin loopback adresi olabilir.");

        _client = client;
        _dbFactory = dbFactory;
        _enabled = configuration.GetValue<bool>("Ollama:Enabled");
        _client.BaseAddress = uri;
        _client.Timeout = TimeSpan.FromMinutes(3);
        ModelAdi = configuration["Ollama:Model"] ?? "llama3.2";
        EmbeddingModelAdi = configuration["Ollama:EmbeddingModel"] ?? "nomic-embed-text";
    }

    public async Task<bool> BaglantiKontrolAsync()
    {
        if (!_enabled) return false;
        await using var db = await _dbFactory.CreateDbContextAsync();
        if (!db.Database.IsSqlite()) return false;
        var dataSource = db.Database.GetDbConnection().DataSource;
        if (string.IsNullOrWhiteSpace(dataSource) || dataSource.StartsWith(@"\\", StringComparison.Ordinal) ||
            dataSource.StartsWith("file:", StringComparison.OrdinalIgnoreCase)) return false;
        try
        {
            var root = Path.GetPathRoot(Path.GetFullPath(dataSource));
            if (string.IsNullOrWhiteSpace(root) || new DriveInfo(root).DriveType != DriveType.Fixed) return false;
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or NotSupportedException)
        {
            return false;
        }
        try
        {
            using var response = await _client.GetAsync("api/tags");
            if (!response.IsSuccessStatusCode) return false;
            using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
            return document.RootElement.TryGetProperty("models", out var models) &&
                models.EnumerateArray().Any(item => item.TryGetProperty("name", out var name) &&
                    (name.GetString()?.Equals(ModelAdi, StringComparison.OrdinalIgnoreCase) == true ||
                     name.GetString()?.StartsWith(ModelAdi + ":", StringComparison.OrdinalIgnoreCase) == true));
        }
        catch (HttpRequestException) { return false; }
        catch (TaskCanceledException) { return false; }
        catch (JsonException) { return false; }
    }

    public async Task<string> AnalizYapAsync(string prompt, string? sistemPrompt = null)
    {
        if (!_enabled) throw new InvalidOperationException("Yerel AI guvenli kurulumu tamamlanmadi.");
        if (string.IsNullOrWhiteSpace(prompt)) return string.Empty;
        if (prompt.Length > 32000) throw new ArgumentException("AI istegi cok uzun.", nameof(prompt));
        if (!await BaglantiKontrolAsync())
            throw new InvalidOperationException("Yerel AI modeli kurulu veya calisir durumda degil. Internetten model indirilmez.");
        var messages = new List<object>();
        if (!string.IsNullOrWhiteSpace(sistemPrompt))
            messages.Add(new { role = "system", content = sistemPrompt });
        messages.Add(new { role = "user", content = prompt });
        using var response = await _client.PostAsJsonAsync("api/chat", new
        {
            model = ModelAdi,
            messages,
            stream = false,
            options = new { temperature = 0.2, num_predict = 1024, num_ctx = 8192 }
        });
        response.EnsureSuccessStatusCode();
        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        return document.RootElement.GetProperty("message").GetProperty("content").GetString() ?? string.Empty;
    }

    public Task<string> RaporYorumlaAsync(string veri) => AnalizYapAsync(veri,
        "Yalnizca verilen yerel rapor verisini yorumla. Sayilari degistirme ve eksik bilgiyi uydurma. Yaniti Turkce ver.");

    public async Task<float[]> EmbeddingOlusturAsync(string metin)
    {
        if (!_enabled) throw new InvalidOperationException("Yerel AI guvenli kurulumu tamamlanmadi.");
        if (string.IsNullOrWhiteSpace(metin)) return Array.Empty<float>();
        if (metin.Length > 16000) throw new ArgumentException("Embedding istegi cok uzun.", nameof(metin));
        if (!await BaglantiKontrolAsync())
            throw new InvalidOperationException("Yerel AI modeli kurulu veya calisir durumda degil. Internetten model indirilmez.");
        using var response = await _client.PostAsJsonAsync("api/embeddings", new { model = EmbeddingModelAdi, prompt = metin });
        response.EnsureSuccessStatusCode();
        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        return document.RootElement.GetProperty("embedding").EnumerateArray().Select(value => value.GetSingle()).ToArray();
    }
}


