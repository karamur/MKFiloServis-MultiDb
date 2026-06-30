using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using MKFiloServis.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MKFiloServis.Web.Services;

/// <summary>
/// Webhook yönetim ve gönderim servisi implementasyonu
/// </summary>
public class WebhookService : IWebhookService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WebhookService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public WebhookService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IHttpClientFactory httpClientFactory,
        ILogger<WebhookService> logger)
    {
        _contextFactory = contextFactory;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    #region Endpoint CRUD

    public async Task<List<WebhookEndpoint>> GetAllEndpointsAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.WebhookEndpointler
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.Ad)
            .ToListAsync();
    }

    public async Task<WebhookEndpoint?> GetEndpointByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.WebhookEndpointler
            .Include(x => x.Loglar.OrderByDescending(l => l.CreatedAt).Take(10))
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
    }

    public async Task<WebhookEndpoint> CreateEndpointAsync(WebhookEndpoint endpoint)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        endpoint.CreatedAt = DateTime.Now;

        // Secret yoksa otomatik oluştur
        if (string.IsNullOrEmpty(endpoint.Secret))
        {
            endpoint.Secret = GenerateSecret();
        }

        context.WebhookEndpointler.Add(endpoint);
        await context.SaveChangesAsync();

        _logger.LogInformation("Webhook endpoint oluşturuldu: {Ad} ({Url})", endpoint.Ad, endpoint.Url);
        return endpoint;
    }

    public async Task<WebhookEndpoint> UpdateEndpointAsync(WebhookEndpoint endpoint)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var existing = await context.WebhookEndpointler.FindAsync(endpoint.Id);
        if (existing == null)
            throw new InvalidOperationException("Webhook endpoint bulunamadı");

        existing.Ad = endpoint.Ad;
        existing.Aciklama = endpoint.Aciklama;
        existing.Url = endpoint.Url;
        existing.Aktif = endpoint.Aktif;
        existing.MaxRetry = endpoint.MaxRetry;
        existing.RetryDelaySaniye = endpoint.RetryDelaySaniye;
        existing.OlayFiltresi = endpoint.OlayFiltresi;
        existing.HttpMethod = endpoint.HttpMethod;
        existing.Headers = endpoint.Headers;
        existing.UpdatedAt = DateTime.Now;

        // Secret değiştiyse güncelle
        if (!string.IsNullOrEmpty(endpoint.Secret) && endpoint.Secret != existing.Secret)
        {
            existing.Secret = endpoint.Secret;
        }

        await context.SaveChangesAsync();

        _logger.LogInformation("Webhook endpoint güncellendi: {Ad}", endpoint.Ad);
        return existing;
    }

    public async Task DeleteEndpointAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var endpoint = await context.WebhookEndpointler.FindAsync(id);
        if (endpoint != null)
        {
            endpoint.IsDeleted = true;
            endpoint.UpdatedAt = DateTime.Now;
            await context.SaveChangesAsync();

            _logger.LogInformation("Webhook endpoint silindi: {Ad}", endpoint.Ad);
        }
    }

    public async Task<bool> TestEndpointAsync(int endpointId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var endpoint = await GetEndpointByIdAsync(endpointId);
        if (endpoint == null)
            return false;

        var testPayload = new
        {
            test = true,
            timestamp = DateTime.Now,
            message = "Koa Filo Servis webhook test mesajı"
        };

        try
        {
            var result = await SendWebhookAsync(context, endpoint, "Test.Ping", testPayload);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Webhook test başarısız: {Url}", endpoint.Url);
            return false;
        }
    }

    #endregion

    #region Webhook Tetikleme

    public async Task TriggerWebhookAsync(string olayTipi, object payload, string? iliskiliTablo = null, int? iliskiliKayitId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var endpoints = await GetActiveEndpointsForEventAsync(context, olayTipi);
        if (!endpoints.Any())
        {
            _logger.LogDebug("'{OlayTipi}' olayı için aktif webhook bulunamadı", olayTipi);
            return;
        }

        var payloadJson = JsonSerializer.Serialize(payload, _jsonOptions);

        foreach (var endpoint in endpoints)
        {
            var log = new WebhookLog
            {
                WebhookEndpointId = endpoint.Id,
                OlayTipi = olayTipi,
                Payload = payloadJson,
                Durum = WebhookLogDurum.Bekliyor,
                IliskiliTablo = iliskiliTablo,
                IliskiliKayitId = iliskiliKayitId,
                CreatedAt = DateTime.Now
            };

            context.WebhookLoglar.Add(log);
            await context.SaveChangesAsync();

            // Asenkron gönderim (fire and forget)
            _ = Task.Run(async () => await ProcessWebhookLogAsync(context, log.Id));
        }
    }

    public async Task TriggerWebhookAsync<T>(string olayTipi, T payload, string? iliskiliTablo = null, int? iliskiliKayitId = null) where T : class
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        await TriggerWebhookAsync(olayTipi, (object)payload, iliskiliTablo, iliskiliKayitId);
    }

    private async Task<List<WebhookEndpoint>> GetActiveEndpointsForEventAsync(ApplicationDbContext context, string olayTipi)
    {
        var endpoints = await context.WebhookEndpointler
            .Where(x => x.Aktif && !x.IsDeleted)
            .ToListAsync();

        return endpoints.Where(e => IsEventAllowed(e, olayTipi)).ToList();
    }

    private bool IsEventAllowed(WebhookEndpoint endpoint, string olayTipi)
    {
        if (string.IsNullOrEmpty(endpoint.OlayFiltresi))
            return true; // Filtre yoksa tüm olayları al

        try
        {
            var allowedEvents = JsonSerializer.Deserialize<string[]>(endpoint.OlayFiltresi);
            return allowedEvents?.Contains(olayTipi) ?? true;
        }
        catch
        {
            return true;
        }
    }

    private async Task ProcessWebhookLogAsync(ApplicationDbContext context, int logId)
    {
        // Process webhook log
        
        var log = await context.WebhookLoglar
            .Include(x => x.WebhookEndpoint)
            .FirstOrDefaultAsync(x => x.Id == logId);

        if (log == null || log.WebhookEndpoint == null)
            return;

        var endpoint = log.WebhookEndpoint;
        var success = false;

        for (int retry = 0; retry <= endpoint.MaxRetry && !success; retry++)
        {
            if (retry > 0)
            {
                log.Durum = WebhookLogDurum.YenidenDeneniyor;
                log.RetryCount = retry;
                await context.SaveChangesAsync();

                await Task.Delay(TimeSpan.FromSeconds(endpoint.RetryDelaySaniye * retry));
            }

            success = await SendWebhookWithLoggingAsync(context, log, endpoint);
        }

        // İstatistikleri güncelle
        endpoint.ToplamGonderim++;
        if (success)
        {
            endpoint.BasariliGonderim++;
            endpoint.SonBasariliTarih = DateTime.Now;
        }
        else
        {
            endpoint.BasarisizGonderim++;
        }
        endpoint.SonGonderimTarihi = DateTime.Now;

        await context.SaveChangesAsync();
    }

    private async Task<bool> SendWebhookWithLoggingAsync(ApplicationDbContext context, WebhookLog log, WebhookEndpoint endpoint)
    {
        log.Durum = WebhookLogDurum.Gonderiliyor;
        log.GonderimTarihi = DateTime.Now;
        await context.SaveChangesAsync();

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(30);

            var request = new HttpRequestMessage(
                endpoint.HttpMethod.ToUpper() == "POST" ? HttpMethod.Post : HttpMethod.Put,
                endpoint.Url);

            // Payload
            var webhookPayload = new
            {
                @event = log.OlayTipi,
                timestamp = DateTime.UtcNow.ToString("o"),
                data = log.Payload != null ? JsonSerializer.Deserialize<object>(log.Payload) : null
            };
            var content = JsonSerializer.Serialize(webhookPayload, _jsonOptions);
            request.Content = new StringContent(content, Encoding.UTF8, "application/json");

            // HMAC imza
            if (!string.IsNullOrEmpty(endpoint.Secret))
            {
                var signature = ComputeHmacSignature(content, endpoint.Secret);
                request.Headers.Add("X-Webhook-Signature", signature);
            }

            // Ek headerlar
            if (!string.IsNullOrEmpty(endpoint.Headers))
            {
                try
                {
                    var headers = JsonSerializer.Deserialize<Dictionary<string, string>>(endpoint.Headers);
                    if (headers != null)
                    {
                        foreach (var header in headers)
                        {
                            request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                        }
                    }
                }
                catch { }
            }

            request.Headers.Add("X-Webhook-Event", log.OlayTipi);
            request.Headers.Add("X-Webhook-Delivery", log.Id.ToString());

            var response = await client.SendAsync(request);

            stopwatch.Stop();
            log.SureMilisaniye = (int)stopwatch.ElapsedMilliseconds;
            log.HttpStatusCode = (int)response.StatusCode;
            log.YanitTarihi = DateTime.Now;

            try
            {
                log.ResponseBody = await response.Content.ReadAsStringAsync();
                if (log.ResponseBody?.Length > 2000)
                    log.ResponseBody = log.ResponseBody.Substring(0, 2000) + "...";
            }
            catch { }

            if (response.IsSuccessStatusCode)
            {
                log.Durum = WebhookLogDurum.Basarili;
                await context.SaveChangesAsync();

                _logger.LogInformation("Webhook gönderildi: {OlayTipi} -> {Url} ({StatusCode})",
                    log.OlayTipi, endpoint.Url, log.HttpStatusCode);
                return true;
            }
            else
            {
                log.Durum = WebhookLogDurum.Basarisiz;
                log.HataMesaji = $"HTTP {log.HttpStatusCode}: {response.ReasonPhrase}";
                await context.SaveChangesAsync();

                _logger.LogWarning("Webhook başarısız: {OlayTipi} -> {Url} ({StatusCode})",
                    log.OlayTipi, endpoint.Url, log.HttpStatusCode);
                return false;
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            log.SureMilisaniye = (int)stopwatch.ElapsedMilliseconds;
            log.Durum = WebhookLogDurum.Basarisiz;
            log.HataMesaji = ex.Message;
            log.YanitTarihi = DateTime.Now;
            await context.SaveChangesAsync();

            _logger.LogError(ex, "Webhook gönderim hatası: {OlayTipi} -> {Url}", log.OlayTipi, endpoint.Url);
            return false;
        }
    }

    private async Task<bool> SendWebhookAsync(ApplicationDbContext context, WebhookEndpoint endpoint, string olayTipi, object payload)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(10);

            var webhookPayload = new
            {
                @event = olayTipi,
                timestamp = DateTime.UtcNow.ToString("o"),
                data = payload
            };
            var content = JsonSerializer.Serialize(webhookPayload, _jsonOptions);

            var request = new HttpRequestMessage(HttpMethod.Post, endpoint.Url);
            request.Content = new StringContent(content, Encoding.UTF8, "application/json");

            if (!string.IsNullOrEmpty(endpoint.Secret))
            {
                var signature = ComputeHmacSignature(content, endpoint.Secret);
                request.Headers.Add("X-Webhook-Signature", signature);
            }

            var response = await client.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private static string ComputeHmacSignature(string payload, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return "sha256=" + Convert.ToHexString(hash).ToLower();
    }

    private static string GenerateSecret()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }

    #endregion

    #region Log İşlemleri

    public async Task<List<WebhookLog>> GetLogsAsync(int? endpointId = null, int sayfa = 1, int sayfaBoyutu = 50)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.WebhookLoglar
            .Include(x => x.WebhookEndpoint)
            .AsQueryable();

        if (endpointId.HasValue)
            query = query.Where(x => x.WebhookEndpointId == endpointId.Value);

        return await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((sayfa - 1) * sayfaBoyutu)
            .Take(sayfaBoyutu)
            .ToListAsync();
    }

    public async Task<WebhookLog?> GetLogByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.WebhookLoglar
            .Include(x => x.WebhookEndpoint)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<int> GetPendingLogCountAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.WebhookLoglar
            .CountAsync(x => x.Durum == WebhookLogDurum.Bekliyor || x.Durum == WebhookLogDurum.YenidenDeneniyor);
    }

    public async Task RetryFailedLogsAsync(int endpointId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var failedLogs = await context.WebhookLoglar
            .Where(x => x.WebhookEndpointId == endpointId &&
                       x.Durum == WebhookLogDurum.Basarisiz)
            .OrderBy(x => x.CreatedAt)
            .Take(100)
            .ToListAsync();

        foreach (var log in failedLogs)
        {
            log.Durum = WebhookLogDurum.Bekliyor;
            log.RetryCount = 0;
            log.HataMesaji = null;
        }

        await context.SaveChangesAsync();

        // Yeniden işle
        foreach (var log in failedLogs)
        {
            _ = Task.Run(async () => await ProcessWebhookLogAsync(context, log.Id));
        }

        _logger.LogInformation("{Count} webhook logu yeniden denemeye alındı", failedLogs.Count);
    }

    #endregion

    #region İstatistikler

    public async Task<WebhookIstatistik> GetIstatistiklerAsync(int? endpointId = null, DateTime? baslangic = null, DateTime? bitis = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.WebhookLoglar.AsQueryable();

        if (endpointId.HasValue)
            query = query.Where(x => x.WebhookEndpointId == endpointId.Value);

        if (baslangic.HasValue)
            query = query.Where(x => x.CreatedAt >= baslangic.Value);

        if (bitis.HasValue)
            query = query.Where(x => x.CreatedAt <= bitis.Value);

        var logs = await query.ToListAsync();

        var istatistik = new WebhookIstatistik
        {
            ToplamGonderim = logs.Count,
            BasariliGonderim = logs.Count(x => x.Durum == WebhookLogDurum.Basarili),
            BasarisizGonderim = logs.Count(x => x.Durum == WebhookLogDurum.Basarisiz),
            BekleyenGonderim = logs.Count(x => x.Durum == WebhookLogDurum.Bekliyor || x.Durum == WebhookLogDurum.YenidenDeneniyor),
            OrtalamaSureMilisaniye = logs.Where(x => x.SureMilisaniye > 0).DefaultIfEmpty().Average(x => x?.SureMilisaniye ?? 0),
            OlayTipiDagilimi = logs.GroupBy(x => x.OlayTipi).ToDictionary(g => g.Key, g => g.Count()),
            HttpStatusDagilimi = logs.Where(x => x.HttpStatusCode > 0).GroupBy(x => x.HttpStatusCode).ToDictionary(g => g.Key, g => g.Count())
        };

        return istatistik;
    }

    #endregion
}


