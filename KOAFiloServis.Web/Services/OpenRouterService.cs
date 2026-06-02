using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace KOAFiloServis.Web.Services;

public interface IOpenRouterService
{
    IAsyncEnumerable<string> StreamChatCompletionAsync(string prompt, CancellationToken cancellationToken = default);
}

public class OpenRouterService : IOpenRouterService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly string _apiKey;

    public OpenRouterService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _apiKey = _configuration["OpenRouter:ApiKey"] 
                  ?? throw new ArgumentNullException("OpenRouter:ApiKey is missing in configuration.");
        
        _httpClient.BaseAddress = new Uri("https://openrouter.ai/api/v1/");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        
        // OpenRouter recommended headers
        var siteUrl = _configuration["OpenRouter:SiteUrl"] ?? "http://localhost:5000";
        var siteName = _configuration["OpenRouter:SiteName"] ?? "KOAFiloServis";
        
        _httpClient.DefaultRequestHeaders.Add("HTTP-Referer", siteUrl);
        _httpClient.DefaultRequestHeaders.Add("X-Title", siteName);
    }

    public async IAsyncEnumerable<string> StreamChatCompletionAsync(string prompt, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var requestBody = new
        {
            model = "anthropic/claude-opus-4.7",
            messages = new[]
            {
                new { role = "user", content = prompt }
            },
            stream = true
        };

        var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        using var request = new HttpRequestMessage(HttpMethod.Post, "chat/completions") { Content = content };
        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line is null) break;
            if (string.IsNullOrWhiteSpace(line)) continue;

            if (line.StartsWith("data: "))
            {
                var data = line.Substring(6);
                if (data == "[DONE]") break;

                OpenRouterChunk? chunk = null;
                try
                {
                    chunk = JsonSerializer.Deserialize<OpenRouterChunk>(data);
                }
                catch (JsonException) { continue; }

                if (chunk?.Choices != null && chunk.Choices.Length > 0)
                {
                    var deltaContent = chunk.Choices[0].Delta?.Content;
                    if (!string.IsNullOrEmpty(deltaContent))
                    {
                        yield return deltaContent;
                    }
                }
                
                // Reasoning tokens are typically sent in the final chunk's usage object
                if (chunk?.Usage != null && chunk.Usage.ReasoningTokens > 0)
                {
                    // For debugging or logging purposes
                    Console.WriteLine($"\nReasoning tokens: {chunk.Usage.ReasoningTokens}");
                }
            }
        }
    }

    private class OpenRouterChunk
    {
        [JsonPropertyName("choices")]
        public Choice[]? Choices { get; set; }

        [JsonPropertyName("usage")]
        public UsageInfo? Usage { get; set; }
    }

    private class Choice
    {
        [JsonPropertyName("delta")]
        public Delta? Delta { get; set; }
    }

    private class Delta
    {
        [JsonPropertyName("content")]
        public string? Content { get; set; }
    }

    private class UsageInfo
    {
        [JsonPropertyName("reasoning_tokens")]
        public int ReasoningTokens { get; set; }
    }
}

