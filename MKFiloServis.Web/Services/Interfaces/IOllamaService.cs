namespace MKFiloServis.Web.Services.Interfaces;

/// <summary>AI analiz stub — Ollama kaldırıldı.</summary>
public interface IOllamaService
{
    string ModelAdi { get; }
    string EmbeddingModelAdi { get; }
    Task<bool> BaglantiKontrolAsync();
    Task<string> AnalizYapAsync(string prompt, string? sistemPrompt = null);
    Task<string> RaporYorumlaAsync(string veri);
    Task<float[]> EmbeddingOlusturAsync(string metin);
}

/// <summary>AI chat stub — Ollama kaldırıldı.</summary>
public interface IOllamaAIChatService
{
    Task<bool> IsAvailableAsync();
    Task<string> SendMessageAsync(string message);
    Task<string> SendMessageWithHistoryAsync(string message, List<(string role, string content)> history);
    IAsyncEnumerable<string> ChatStreamAsync(string message, CancellationToken cancellationToken = default);
    void ClearHistory();
    string CurrentModel { get; }
    void SetModel(string modelName);
    Task<List<string>> GetAvailableModelsAsync();
}



