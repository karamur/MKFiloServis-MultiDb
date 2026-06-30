using MKFiloServis.Shared.Entities;

namespace MKFiloServis.Web.Services.Interfaces;

public interface IPuantajFinansService
{
    // Finansal kayıt
    Task FinansalKayitOlusturAsync(int hesapDonemiId, CancellationToken ct = default);
    Task<List<PuantajFinansalKayit>> FinansalKayitlariGetirAsync(int hesapDonemiId, CancellationToken ct = default);

    // Fatura üretimi
    Task<Fatura> GelirFaturasiUretAsync(int finansalKayitId, CancellationToken ct = default);
    Task<Fatura> GiderFaturasiUretAsync(int finansalKayitId, CancellationToken ct = default);
    Task<int> TopluFaturaUretAsync(int hesapDonemiId, CancellationToken ct = default);

    // Durum
    Task<bool> FaturaUretilebilirMiAsync(int hesapDonemiId, CancellationToken ct = default);
    Task<bool> FinansalKayitOlusturulabilirMiAsync(int hesapDonemiId, CancellationToken ct = default);

    // YENİ: HakedisPuantaj → Fatura → Muhasebe → Snapshot
    Task<PuantajFinansSonuc> IsleAsync(HakedisPuantaj puantaj);
}

public class PuantajFinansSonuc
{
    public bool Basarili { get; set; }
    public string? Mesaj { get; set; }
    public decimal GelirTutar { get; set; }
    public decimal GiderTutar { get; set; }
    public decimal Kar { get; set; }
    public int? GelirFaturaId { get; set; }
    public int? GiderFaturaId { get; set; }
}




