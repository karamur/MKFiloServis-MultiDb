using System.Runtime.CompilerServices;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Web.Services;

/// <summary>Yerel AI sohbeti. Konusma gecmisi veritabanina veya buluta yazilmaz.</summary>
public sealed class OllamaAIChatService : IOllamaAIChatService
{
    private readonly IOllamaService _ollama;
    public OllamaAIChatService(IOllamaService ollama) => _ollama = ollama;

    public string CurrentModel => _ollama.ModelAdi;
    public Task<bool> IsAvailableAsync() => _ollama.BaglantiKontrolAsync();
    public Task<string> SendMessageAsync(string message) => _ollama.AnalizYapAsync(message, SystemPrompt);
    public Task<string> SendMessageWithHistoryAsync(string message, List<(string role, string content)> history)
    {
        var recent = history.TakeLast(8).Select(item => $"{(item.role == "assistant" ? "Asistan" : "Kullanici")}: {item.content}");
        var prompt = string.Join("\n", recent) + "\nKullanici: " + message;
        return _ollama.AnalizYapAsync(prompt, SystemPrompt);
    }

    public async IAsyncEnumerable<string> ChatStreamAsync(string message, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        yield return await SendMessageAsync(message);
    }

    public void ClearHistory() { }
    public void SetModel(string modelName) { }
    public async Task<List<string>> GetAvailableModelsAsync() =>
        await IsAvailableAsync() ? new List<string> { CurrentModel } : new List<string>();

    private const string SystemPrompt = "Sen MK Filo Servis'in yerel rapor asistanisin. Yalnizca kullanicinin sagladigi verileri yorumla. Verilmeyen sayilari ve kayitlari uydurma. Finansal kararlari otomatik verme. Turkce yanitla.";
}



