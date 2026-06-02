using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace KOAFiloServis.Web.Services;

/// <summary>
/// DeepSeek AI API servisi - DeepSeek V3 model desteği
/// https://platform.deepseek.com/api-docs/
/// </summary>
public interface IDeepSeekService
{
    IAsyncEnumerable<string> StreamChatCompletionAsync(
        string prompt, 
        string? systemPrompt = null,
        CancellationToken cancellationToken = default);

    Task<string> ChatCompletionAsync(
        string prompt, 
        string? systemPrompt = null,
        CancellationToken cancellationToken = default);
}

public class DeepSeekService : IDeepSeekService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly string _apiKey;
    private readonly string _model;
    private readonly ILogger<DeepSeekService> _logger;

    public DeepSeekService(
        HttpClient httpClient, 
        IConfiguration configuration,
        ILogger<DeepSeekService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;

        _apiKey = _configuration["DeepSeek:ApiKey"] 
                  ?? throw new ArgumentNullException("DeepSeek:ApiKey is missing in configuration.");

        _model = _configuration["DeepSeek:Model"] ?? "deepseek-chat"; // DeepSeek V3

        var baseUrl = _configuration["DeepSeek:BaseUrl"] ?? "https://api.deepseek.com/v1/";
        _httpClient.BaseAddress = new Uri(baseUrl);
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        _httpClient.Timeout = TimeSpan.FromMinutes(5); // DeepSeek reasoning can take time
    }

    /// <summary>
    /// Streaming chat completion - gerçek zamanlı yanıt alır
    /// </summary>
    public async IAsyncEnumerable<string> StreamChatCompletionAsync(
        string prompt, 
        string? systemPrompt = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var messages = new List<object>();

        if (!string.IsNullOrWhiteSpace(systemPrompt))
        {
            messages.Add(new { role = "system", content = systemPrompt });
        }

        messages.Add(new { role = "user", content = prompt });

        var requestBody = new
        {
            model = _model,
            messages = messages.ToArray(),
            stream = true,
            temperature = 1.0, // DeepSeek önerilen default
            max_tokens = 8000,  // DeepSeek V3 max output tokens
            // DeepSeek reasoning özellikleri
            reasoning_effort = "medium" // low, medium, high
        };

        var content = new StringContent(
            JsonSerializer.Serialize(requestBody), 
            Encoding.UTF8, 
            "application/json");

        using var request = new HttpRequestMessage(HttpMethod.Post, "chat/completions") 
        { 
            Content = content 
        };

        using var response = await _httpClient.SendAsync(
            request, 
            HttpCompletionOption.ResponseHeadersRead, 
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("DeepSeek API error: {StatusCode} - {Error}", 
                response.StatusCode, errorContent);
            throw new HttpRequestException(
                $"DeepSeek API request failed: {response.StatusCode} - {errorContent}");
        }

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

                DeepSeekChunk? chunk = null;
                try
                {
                    chunk = JsonSerializer.Deserialize<DeepSeekChunk>(data);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning("Failed to deserialize DeepSeek chunk: {Error}", ex.Message);
                    continue;
                }

                if (chunk?.Choices != null && chunk.Choices.Length > 0)
                {
                    var choice = chunk.Choices[0];

                    // Reasoning content (DeepSeek'in düşünme süreci)
                    if (!string.IsNullOrEmpty(choice.Delta?.ReasoningContent))
                    {
                        _logger.LogDebug("DeepSeek reasoning: {Reasoning}", 
                            choice.Delta.ReasoningContent);
                        // İsterseniz reasoning'i de stream edebilirsiniz:
                        // yield return $"[THINKING: {choice.Delta.ReasoningContent}]";
                    }

                    // Actual response content
                    var deltaContent = choice.Delta?.Content;
                    if (!string.IsNullOrEmpty(deltaContent))
                    {
                        yield return deltaContent;
                    }
                }

                // Usage bilgisi (son chunk'ta gelir)
                if (chunk?.Usage != null)
                {
                    _logger.LogInformation(
                        "DeepSeek usage - Prompt: {PromptTokens}, Completion: {CompletionTokens}, Total: {TotalTokens}, Cache: {CacheTokens}, Reasoning: {ReasoningTokens}",
                        chunk.Usage.PromptTokens,
                        chunk.Usage.CompletionTokens,
                        chunk.Usage.TotalTokens,
                        chunk.Usage.PromptCacheHitTokens + chunk.Usage.PromptCacheMissTokens,
                        chunk.Usage.CompletionTokensDetails?.ReasoningTokens ?? 0);
                }
            }
        }
    }

    /// <summary>
    /// Non-streaming chat completion - tam yanıt alır
    /// </summary>
    public async Task<string> ChatCompletionAsync(
        string prompt, 
        string? systemPrompt = null,
        CancellationToken cancellationToken = default)
    {
        var messages = new List<object>();

        if (!string.IsNullOrWhiteSpace(systemPrompt))
        {
            messages.Add(new { role = "system", content = systemPrompt });
        }

        messages.Add(new { role = "user", content = prompt });

        var requestBody = new
        {
            model = _model,
            messages = messages.ToArray(),
            stream = false,
            temperature = 1.0,
            max_tokens = 8000,
            reasoning_effort = "medium"
        };

        var content = new StringContent(
            JsonSerializer.Serialize(requestBody), 
            Encoding.UTF8, 
            "application/json");

        var response = await _httpClient.PostAsync("chat/completions", content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("DeepSeek API error: {StatusCode} - {Error}", 
                response.StatusCode, errorContent);
            throw new HttpRequestException(
                $"DeepSeek API request failed: {response.StatusCode}");
        }

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        var result = JsonSerializer.Deserialize<DeepSeekResponse>(responseBody);

        if (result?.Choices != null && result.Choices.Length > 0)
        {
            var message = result.Choices[0].Message;

            // Log reasoning if available
            if (!string.IsNullOrEmpty(message?.ReasoningContent))
            {
                _logger.LogDebug("DeepSeek reasoning: {Reasoning}", message.ReasoningContent);
            }

            return message?.Content ?? string.Empty;
        }

        return string.Empty;
    }

    #region DeepSeek API Models

    private class DeepSeekChunk
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("object")]
        public string? Object { get; set; }

        [JsonPropertyName("created")]
        public long Created { get; set; }

        [JsonPropertyName("model")]
        public string? Model { get; set; }

        [JsonPropertyName("choices")]
        public Choice[]? Choices { get; set; }

        [JsonPropertyName("usage")]
        public Usage? Usage { get; set; }
    }

    private class DeepSeekResponse
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("object")]
        public string? Object { get; set; }

        [JsonPropertyName("created")]
        public long Created { get; set; }

        [JsonPropertyName("model")]
        public string? Model { get; set; }

        [JsonPropertyName("choices")]
        public ResponseChoice[]? Choices { get; set; }

        [JsonPropertyName("usage")]
        public Usage? Usage { get; set; }
    }

    private class Choice
    {
        [JsonPropertyName("index")]
        public int Index { get; set; }

        [JsonPropertyName("delta")]
        public Delta? Delta { get; set; }

        [JsonPropertyName("finish_reason")]
        public string? FinishReason { get; set; }

        [JsonPropertyName("logprobs")]
        public object? Logprobs { get; set; }
    }

    private class ResponseChoice
    {
        [JsonPropertyName("index")]
        public int Index { get; set; }

        [JsonPropertyName("message")]
        public Message? Message { get; set; }

        [JsonPropertyName("finish_reason")]
        public string? FinishReason { get; set; }

        [JsonPropertyName("logprobs")]
        public object? Logprobs { get; set; }
    }

    private class Delta
    {
        [JsonPropertyName("role")]
        public string? Role { get; set; }

        [JsonPropertyName("content")]
        public string? Content { get; set; }

        [JsonPropertyName("reasoning_content")]
        public string? ReasoningContent { get; set; }
    }

    private class Message
    {
        [JsonPropertyName("role")]
        public string? Role { get; set; }

        [JsonPropertyName("content")]
        public string? Content { get; set; }

        [JsonPropertyName("reasoning_content")]
        public string? ReasoningContent { get; set; }
    }

    private class Usage
    {
        [JsonPropertyName("prompt_tokens")]
        public int PromptTokens { get; set; }

        [JsonPropertyName("completion_tokens")]
        public int CompletionTokens { get; set; }

        [JsonPropertyName("total_tokens")]
        public int TotalTokens { get; set; }

        [JsonPropertyName("prompt_cache_hit_tokens")]
        public int PromptCacheHitTokens { get; set; }

        [JsonPropertyName("prompt_cache_miss_tokens")]
        public int PromptCacheMissTokens { get; set; }

        [JsonPropertyName("completion_tokens_details")]
        public CompletionTokensDetails? CompletionTokensDetails { get; set; }
    }

    private class CompletionTokensDetails
    {
        [JsonPropertyName("reasoning_tokens")]
        public int ReasoningTokens { get; set; }
    }

    #endregion
}
