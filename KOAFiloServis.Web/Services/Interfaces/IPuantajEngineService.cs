namespace KOAFiloServis.Web.Services.Interfaces;

/// <summary>
/// OperasyonKaydi → PuantajKayit dönüşüm motoru.
/// Günlük ham operasyon kayıtlarını işleyip aylık finansal puantaj çıktısı üretir.
/// </summary>
public interface IPuantajEngineService
{
    /// <summary>
    /// Belirtilen dönemdeki işlenmemiş OperasyonKaydi'ları PuantajKayit'a dönüştürür.
    /// </summary>
    /// <returns>İşlenen grup sayısı ve üretilen/güncellenen PuantajKayit adedi.</returns>
    Task<PuantajEngineSonuc> ProcessDonemAsync(int yil, int ay, int? kurumId = null);

    /// <summary>
    /// Tek bir OperasyonKaydi'dan ilgili PuantajKayit'ı günceller.
    /// </summary>
    Task ProcessSingleAsync(int operasyonKaydiId);

    /// <summary>
    /// Mevcut PuantajKayit'ları silip tüm OperasyonKaydi'lardan yeniden üretir.
    /// </summary>
    Task<PuantajEngineSonuc> ReprocessDonemAsync(int yil, int ay, int? kurumId = null);
}

public sealed class PuantajEngineSonuc
{
    public int IslenenOperasyonKaydi { get; init; }
    public int UretilenPuantajKayit { get; init; }
    public int GuncellenenPuantajKayit { get; init; }
    public int ToplamPuantaj => UretilenPuantajKayit + GuncellenenPuantajKayit;
}
