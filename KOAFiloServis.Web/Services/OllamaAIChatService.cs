using System.Runtime.CompilerServices;

namespace KOAFiloServis.Web.Services;

/// <summary>Ollama AI Chat kaldırıldı — stub implementasyon.</summary>
public class OllamaAIChatService : IOllamaAIChatService
{
    public string CurrentModel => "kapali";
    public Task<bool> IsAvailableAsync() => Task.FromResult(false);
    public Task<string> SendMessageAsync(string message) => Task.FromResult("AI asistan devre dışı.");
    public Task<string> SendMessageWithHistoryAsync(string message, List<(string role, string content)> history) => Task.FromResult("AI asistan devre dışı.");

    public async IAsyncEnumerable<string> ChatStreamAsync(string message, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        yield return "AI asistan devre dışı.";
        await Task.CompletedTask;
    }

    public void ClearHistory() { }
    public void SetModel(string modelName) { }
    public Task<List<string>> GetAvailableModelsAsync() => Task.FromResult(new List<string>());
}
