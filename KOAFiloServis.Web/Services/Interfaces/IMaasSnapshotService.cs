using KOAFiloServis.Shared.Entities;

namespace KOAFiloServis.Web.Services.Interfaces;

public interface IMaasSnapshotService
{
    /// <summary>Ay/Yıl için snapshot var mı?</summary>
    Task<bool> VarMiAsync(int yil, int ay, int firmaId);

    /// <summary>Mevcut snapshot'ı getir.</summary>
    Task<List<MaasOdemeSnapshot>> GetAsync(int yil, int ay, int firmaId);

    /// <summary>Snapshots oluştur (zaten varsa tekrar oluşturmaz).</summary>
    Task<List<MaasOdemeSnapshot>> OlusturAsync(int yil, int ay, int firmaId, List<(int PersonelId, string AdSoyad, string? PersonelKodu, string? GorevAdi, string? AracPlakasi, decimal GercekMaas, decimal BankayaYatan, decimal Avans, decimal Kesinti, decimal Harcama, decimal Odenecek)> data);

    /// <summary>Snapshots güncelle — sadece kilitli olmayan dönemler için.</summary>
    Task<List<MaasOdemeSnapshot>> GuncelleAsync(int yil, int ay, int firmaId, List<(int PersonelId, string AdSoyad, string? PersonelKodu, string? GorevAdi, string? AracPlakasi, decimal GercekMaas, decimal BankayaYatan, decimal Avans, decimal Kesinti, decimal Harcama, decimal Odenecek)> data);

    /// <summary>Belirli bir ay için snapshot'ı sil (yeniden oluşturmak için).</summary>
    Task SilAsync(int yil, int ay, int firmaId);
}
