using KOAFiloServis.Web.Models;

namespace KOAFiloServis.Web.Services.Interfaces;

/// <summary>
/// Puantaj sonrası fatura hazırlık için READONLY rapor servisi.
/// PuantajKayit ve HakedisPuantaj tablolarından okur, hiçbir kayıt oluşturmaz/güncellemez/silmez.
/// </summary>
public interface IPuantajFaturaRaporService
{
    /// <summary>Özet istatistikler (KPI kartları için).</summary>
    Task<PuantajFaturaOzetDto> GetOzetAsync(PuantajFaturaRaporRequest request, CancellationToken cancellationToken = default);

    /// <summary>Düz satır listesi (sayfalamalı).</summary>
    Task<List<PuantajFaturaSatirDto>> GetSatirlarAsync(PuantajFaturaRaporRequest request, CancellationToken cancellationToken = default);

    /// <summary>Seçili ağaç yapısına göre hiyerarşik gruplu liste.</summary>
    Task<List<PuantajFaturaAgacNodeDto>> GetAgacAsync(PuantajFaturaRaporRequest request, CancellationToken cancellationToken = default);

    /// <summary>Toplam kayıt sayısı (sayfalama için).</summary>
    Task<int> GetCountAsync(PuantajFaturaRaporRequest request, CancellationToken cancellationToken = default);
}
