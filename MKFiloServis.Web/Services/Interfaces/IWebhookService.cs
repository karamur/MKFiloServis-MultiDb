using MKFiloServis.Shared.Entities;

namespace MKFiloServis.Web.Services.Interfaces;

/// <summary>
/// Webhook yönetim ve gönderim servisi
/// </summary>
public interface IWebhookService
{
    // Endpoint CRUD
    Task<List<WebhookEndpoint>> GetAllEndpointsAsync();
    Task<WebhookEndpoint?> GetEndpointByIdAsync(int id);
    Task<WebhookEndpoint> CreateEndpointAsync(WebhookEndpoint endpoint);
    Task<WebhookEndpoint> UpdateEndpointAsync(WebhookEndpoint endpoint);
    Task DeleteEndpointAsync(int id);
    Task<bool> TestEndpointAsync(int endpointId);

    // Webhook tetikleme
    Task TriggerWebhookAsync(string olayTipi, object payload, string? iliskiliTablo = null, int? iliskiliKayitId = null);
    Task TriggerWebhookAsync<T>(string olayTipi, T payload, string? iliskiliTablo = null, int? iliskiliKayitId = null) where T : class;

    // Log işlemleri
    Task<List<WebhookLog>> GetLogsAsync(int? endpointId = null, int sayfa = 1, int sayfaBoyutu = 50);
    Task<WebhookLog?> GetLogByIdAsync(int id);
    Task<int> GetPendingLogCountAsync();
    Task RetryFailedLogsAsync(int endpointId);

    // İstatistikler
    Task<WebhookIstatistik> GetIstatistiklerAsync(int? endpointId = null, DateTime? baslangic = null, DateTime? bitis = null);
}

/// <summary>
/// Webhook istatistik DTO
/// </summary>
public class WebhookIstatistik
{
    public int ToplamGonderim { get; set; }
    public int BasariliGonderim { get; set; }
    public int BasarisizGonderim { get; set; }
    public int BekleyenGonderim { get; set; }
    public double BasariOrani => ToplamGonderim > 0 ? (double)BasariliGonderim / ToplamGonderim * 100 : 0;
    public double OrtalamaSureMilisaniye { get; set; }
    public Dictionary<string, int> OlayTipiDagilimi { get; set; } = new();
    public Dictionary<int, int> HttpStatusDagilimi { get; set; } = new();
}




