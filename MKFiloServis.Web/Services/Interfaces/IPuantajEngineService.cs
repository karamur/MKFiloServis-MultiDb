using System.Threading;

namespace MKFiloServis.Web.Services.Interfaces;

/// <summary>
/// OperasyonKaydi → PuantajKayit dönüşüm motoru (V1).
/// HesapDonemi + PuantajDetay + revizyon zinciri ile çalışır.
/// </summary>
public interface IPuantajEngineService
{
    /// <summary>
    /// Belirtilen dönem için yeni PuantajHesapDonemi oluşturup tüm OperasyonKaydi'ları işler.
    /// Önceki Aktif hesap varsa Superseded yapar.
    /// </summary>
    Task<PuantajEngineSonucV1> ProcessDonemAsync(int yil, int ay, int? kurumId = null, string? hesaplayan = null, string? notlar = null, CancellationToken ct = default);
    Task IptalEtAsync(int hesapDonemiId, string? iptalEden = null, CancellationToken ct = default);
    Task<List<PuantajEngineDetayDto>> GetDetaylarAsync(int hesapDonemiId, CancellationToken ct = default);
}

public sealed class PuantajEngineSonucV1
{
    public int HesapDonemiId { get; init; }
    public int Versiyon { get; init; }
    public int IslenenOperasyonSayisi { get; init; }
    public int UretilenPuantajKayit { get; init; }
    public int SupersededKayit { get; init; }
    public int OlusturulanDetay { get; init; }
}

public sealed class PuantajEngineDetayDto
{
    public int Id { get; init; }
    public int OperasyonKaydiId { get; init; }
    public DateTime Tarih { get; init; }
    public string? Plaka { get; init; }
    public string? GuzergahAdi { get; init; }
    public string Slot { get; init; } = "";
    public int SeferSayisi { get; init; }
    public decimal BirimGelir { get; init; }
    public decimal BirimGider { get; init; }
    public decimal HesaplananTutar { get; init; }
}




