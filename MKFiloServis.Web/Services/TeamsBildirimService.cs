using System.Text;
using System.Text.Json;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Web.Services;

public class TeamsBildirimService : ITeamsBildirimService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TeamsBildirimService> _logger;

    public TeamsBildirimService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<TeamsBildirimService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> GonderAsync(string baslik, string mesaj, string? renk = null, string? butonMetin = null, string? butonUrl = null)
    {
        var webhookUrl = _configuration["Teams:WebhookUrl"];
        if (string.IsNullOrWhiteSpace(webhookUrl))
        {
            _logger.LogWarning("Teams WebhookUrl yapılandırılmamış.");
            return false;
        }

        try
        {
            var card = new
            {
                type = "message",
                attachments = new[]
                {
                    new
                    {
                        contentType = "application/vnd.microsoft.card.adaptive",
                        content = new
                        {
                            type = "AdaptiveCard",
                            version = "1.4",
                            body = new object[]
                            {
                                new { type = "TextBlock", size = "Medium", weight = "Bolder", text = baslik },
                                new { type = "TextBlock", text = mesaj, wrap = true, color = renk ?? "Default" }
                            },
                            actions = butonMetin != null && butonUrl != null
                                ? new object[] { new { type = "Action.OpenUrl", title = butonMetin, url = butonUrl } }
                                : Array.Empty<object>()
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(card);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var client = _httpClientFactory.CreateClient("Teams");
            var response = await client.PostAsync(webhookUrl, content);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Teams mesajı gönderilemedi.");
            return false;
        }
    }

    public async Task<bool> TestBaglantisiAsync()
        => await GonderAsync("KOA Filo Servis", "Bağlantı testi başarılı.", "Good");
}


