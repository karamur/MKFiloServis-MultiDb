using MKFiloServis.Shared.Entities;

namespace MKFiloServis.Web.Services.Interfaces;

public interface IBakimPeriyotService
{
    Task<List<BakimPeriyot>> GetByAracIdAsync(int aracId);
    Task<List<BakimPeriyot>> GetAllActiveAsync();
    Task<BakimPeriyot?> GetByIdAsync(int id);
    Task<BakimPeriyot> CreateAsync(BakimPeriyot periyot);
    Task<BakimPeriyot> UpdateAsync(BakimPeriyot periyot);
    Task DeleteAsync(int id);

    /// <summary>
    /// Araç km güncellendikten sonra bu aracın bakım durumlarını kontrol et
    /// Uyarı eşiği geçildiyse bildirim gönder
    /// </summary>
    Task KmGuncellemeKontrolAsync(int aracId, int yeniKm);

    /// <summary>
    /// Tüm aktif araçlar için bakım kontrolü (Quartz job tarafından çağrılır)
    /// </summary>
    Task TumAraclariBakimKontrolAsync(CancellationToken ct = default);

    /// <summary>Araç için yaklaşan/aşılan bakımların özet listesi</summary>
    Task<List<BakimDurumOzet>> GetBakimDurumOzetAsync(int? aracId = null);
}

public class BakimDurumOzet
{
    public int AracId { get; set; }
    public string Plaka { get; set; } = string.Empty;
    public int BakimPeriyotId { get; set; }
    public string BakimAdi { get; set; } = string.Empty;
    public int? KalanKm { get; set; }
    public int? KalanGun { get; set; }
    public BakimDurumSeviye Seviye { get; set; }
}

public enum BakimDurumSeviye
{
    Normal = 0,
    Uyari = 1,   // Eşikte
    Kritik = 2   // Aşıldı
}




