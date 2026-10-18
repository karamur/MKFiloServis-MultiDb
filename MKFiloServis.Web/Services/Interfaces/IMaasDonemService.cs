using MKFiloServis.Web.Services.Calculation;

namespace MKFiloServis.Web.Services.Interfaces;

/// <summary>
/// Aylık maaş / ödeme listesi için personel finans kayıtlarını (avans, borç, cebinden harcama) seçilen aya böler.
/// Hesap kuralları <see cref="MaasDonemHesaplayici"/> içindedir.
/// </summary>
public interface IMaasDonemService
{
    /// <summary>
    /// Verilen personellerin seçilen ay için avans/harcama özetini ve önceki aylardan devreden bakiyesini döndürür.
    /// Kaydı olmayan personel için de sıfır değerli bir sonuç bulunur.
    /// <paramref name="tumFirmalar"/> true ise aktif firma filtresi kaldırılır ve yalnızca <paramref name="firmaIds"/>
    /// içindeki firmaların kayıtları okunur; false ise aktif firma filtresi geçerlidir.
    /// </summary>
    Task<Dictionary<int, DonemFinansSonuc>> GetDonemFinansAsync(
        IReadOnlyCollection<int> personelIds,
        int yil,
        int ay,
        bool tumFirmalar = false,
        IReadOnlyCollection<int>? firmaIds = null,
        CancellationToken cancellationToken = default);
}
