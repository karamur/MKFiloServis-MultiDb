namespace KOAFiloServis.Web.Services.Interfaces;

/// <summary>
/// Puantaj verilerinde anomali tespit servisi.
/// </summary>
public interface IPuantajAnomaliService
{
    /// <summary>Belirtilen dönem için tüm kural-tabanlı taramaları çalıştırır.</summary>
    Task<int> TumTaramaAsync(int yil, int ay, int? firmaId = null);

    /// <summary>AI (Ollama) ile derinlemesine pattern analizi yapar.</summary>
    Task<string> AIAnalizAsync(int yil, int ay, int? firmaId = null);
}
