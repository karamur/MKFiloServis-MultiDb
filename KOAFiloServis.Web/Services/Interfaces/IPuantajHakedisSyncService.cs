namespace KOAFiloServis.Web.Services.Interfaces;

/// <summary>
/// PuantajKayit → HakedisPuantaj senkronizasyon servisi.
/// Grid'de kaydedilen her PuantajKayit için HakedisPuantaj + Detay oluşturur.
/// Sadece Taslak durumdaki hakedişleri günceller, onaylılara dokunmaz.
/// </summary>
public interface IPuantajHakedisSyncService
{
    /// <summary>
    /// Belirtilen dönem ve firma için PuantajKayit'lardan HakedisPuantaj oluşturur/günceller.
    /// </summary>
    Task SyncFromPuantajKayitAsync(int firmaId, int yil, int ay);

    /// <summary>
    /// Grid'den direkt veri alarak sync yapar (Mesai/EkSefer/FiyatCarpani bilgilerini korur).
    /// </summary>
    Task SyncFromGridAsync(int firmaId, int yil, int ay, List<Models.PuantajGridSatir> satirlar);
}
