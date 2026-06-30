using MKFiloServis.Shared.Entities;

namespace MKFiloServis.Web.Services.Interfaces;

public interface IPuantajService
{
    // Puantaj İşlemleri
    Task<List<PersonelPuantaj>> GetAylikPuantajAsync(int firmaId, int yil, int ay);
    Task<PersonelPuantaj?> GetPuantajByIdAsync(int id);
    Task<PersonelPuantaj?> GetPersonelAylikPuantajAsync(int personelId, int yil, int ay);
    Task<PersonelPuantaj> CreateOrUpdatePuantajAsync(PersonelPuantaj puantaj);
    Task<PersonelPuantaj> OnayaGonderAsync(int id, string? not = null);
    Task<PersonelPuantaj> OnaylaAsync(int id, string onaylayanKullanici, string? not = null);
    Task<PersonelPuantaj> ReddetAsync(int id, string onaylayanKullanici, string? not = null);
    Task<PersonelPuantaj> OnayGeriAlAsync(int id, string? not = null);
    Task DeletePuantajAsync(int id);

    // Günlük Puantaj
    Task<List<GunlukPuantaj>> GetGunlukPuantajlarAsync(int puantajId);
    Task<GunlukPuantaj> SaveGunlukPuantajAsync(GunlukPuantaj gunluk);
    Task OtomatikGunlukPuantajOlusturAsync(int puantajId, int yil, int ay, bool cumartesiCalisir = true, bool pazarCalisir = false, List<DateTime>? resmiTatiller = null);

    // Hesaplamalar
    Task<PersonelPuantaj> HesaplaAsync(int puantajId);
    Task<decimal> ToplamBrutMaasHesaplaAsync(int firmaId, int yil, int ay);
    Task<decimal> ToplamNetOdemeHesaplaAsync(int firmaId, int yil, int ay);

    // Excel Export
    Task<byte[]> ExportPuantajListesiAsync(int firmaId, int yil, int ay);
    Task<byte[]> ExportVakifbankOdemeListesiAsync(int firmaId, int yil, int ay);

    // İstatistikler
    Task<int> GetToplamPersonelSayisiAsync(int firmaId);
    Task<Dictionary<int, decimal>> GetAylikMaasGrafigiAsync(int firmaId, int yil);
}



