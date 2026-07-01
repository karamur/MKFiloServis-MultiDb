using System.Text;
using System.Text.Json;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Web.Services;

public class SlackBildirimService : ISlackBildirimService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SlackBildirimService> _logger;

    public SlackBildirimService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<SlackBildirimService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> GonderAsync(string mesaj, string? kanal = null, string? emoji = null)
    {
        var webhookUrl = _configuration["Slack:WebhookUrl"];
        if (string.IsNullOrWhiteSpace(webhookUrl))
        {
            _logger.LogWarning("Slack WebhookUrl yapılandırılmamış.");
            return false;
        }

        try
        {
            var payload = new Dictionary<string, object>
            {
                ["text"] = (emoji != null ? $"{emoji} " : "") + mesaj
            };
            if (!string.IsNullOrWhiteSpace(kanal))
                payload["channel"] = kanal;

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var client = _httpClientFactory.CreateClient("Slack");
            var response = await client.PostAsync(webhookUrl, content);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Slack mesajı gönderilemedi.");
            return false;
        }
    }

    public async Task<bool> TestBaglantisiAsync()
        => await GonderAsync("MK Filo Servis bağlantı testi başarılı.", emoji: ":white_check_mark:");
}


