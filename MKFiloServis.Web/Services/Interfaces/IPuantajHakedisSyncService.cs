namespace MKFiloServis.Web.Services.Interfaces;

/// <summary>
/// PuantajKayit → Hakedis (yeni model) senkronizasyon servisi.
/// Grid'de kaydedilen her PuantajKayit için Hakedis + HakedisDetay oluşturur/günceller.
/// Sadece Taslak durumdaki hakedişleri günceller, onaylılara dokunmaz.
/// </summary>
public interface IPuantajHakedisSyncService
{
    /// <summary>
    /// Belirtilen dönem ve firma için PuantajKayit'lardan Hakedis oluşturur/günceller.
    /// </summary>
    Task SyncFromPuantajKayitAsync(int firmaId, int yil, int ay);

    /// <summary>
    /// Grid'den direkt veri alarak sync yapar (Mesai/EkSefer/FiyatCarpani bilgilerini korur).
    /// </summary>
    Task SyncFromGridAsync(int firmaId, int yil, int ay, List<Models.PuantajGridSatir> satirlar);
}




