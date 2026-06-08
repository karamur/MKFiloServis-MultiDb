namespace KOAFiloServis.Web.Services;

/// <summary>Ollama kaldırıldı — stub implementasyon.</summary>
public class OllamaService : IOllamaService
{
    public string ModelAdi => "kapali";
    public string EmbeddingModelAdi => "kapali";
    public Task<bool> BaglantiKontrolAsync() => Task.FromResult(false);
    public Task<string> AnalizYapAsync(string prompt, string? sistemPrompt = null) => Task.FromResult("AI analizi devre dışı.");
    public Task<string> RaporYorumlaAsync(string veri) => Task.FromResult("AI yorumlama devre dışı.");
    public Task<float[]> EmbeddingOlusturAsync(string metin) => Task.FromResult(Array.Empty<float>());
}
