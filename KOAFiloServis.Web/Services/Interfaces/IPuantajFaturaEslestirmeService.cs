using KOAFiloServis.Web.Models;

namespace KOAFiloServis.Web.Services.Interfaces;

/// <summary>
/// PuantajKayit (B1) ↔ Fatura eşleştirme ve fark raporu servisi.
/// </summary>
public interface IPuantajFaturaEslestirmeService
{
    /// <summary>Otomatik eşleştirme çalıştırır, sonuçları döner.</summary>
    Task<PuantajFaturaEslesmeRaporu> EslesmeAnaliziYapAsync(int yil, int ay, int? kurumId = null, CancellationToken ct = default);

    /// <summary>Manuel eşleştirme: belirtilen PuantajKayit ile Fatura'yı bağlar.</summary>
    Task<bool> ManuelEslestirAsync(int puantajKayitId, int faturaId, CancellationToken ct = default);

    /// <summary>Eşleştirmeyi kaldır.</summary>
    Task<bool> EslesmeKaldirAsync(int puantajKayitId, CancellationToken ct = default);

    /// <summary>Sadece eşleşmeyen / farklı olanları getirir.</summary>
    Task<List<PuantajFaturaFarkDto>> FarkRaporuGetirAsync(int yil, int ay, int? kurumId = null, CancellationToken ct = default);
}
