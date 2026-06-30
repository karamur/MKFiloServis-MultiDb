using MKFiloServis.Shared.Entities;

namespace MKFiloServis.Web.Services.Interfaces;

public interface IAylikOdemeService
{
    // �deme Plan�
    Task<List<AylikOdemePlani>> GetTumPlanlariAsync(int firmaId);
    Task<List<AylikOdemePlani>> GetAktifPlanlariAsync(int firmaId);
    Task<AylikOdemePlani?> GetPlanByIdAsync(int id);
    Task<AylikOdemePlani> CreatePlanAsync(AylikOdemePlani plan);
    Task<AylikOdemePlani> UpdatePlanAsync(AylikOdemePlani plan);
    Task DeletePlanAsync(int id);

    // Ger�ekle�en �demeler
    Task<List<AylikOdemeGerceklesen>> GetAylikOdemeleriAsync(int firmaId, int yil, int ay);
    Task<List<AylikOdemeGerceklesen>> GetBekleyenOdemeleriAsync(int firmaId);
    Task<List<AylikOdemeGerceklesen>> GetGecikmiOdemeleriAsync(int firmaId);
    Task<AylikOdemeGerceklesen?> GetGerceklesenByIdAsync(int id);
    Task<AylikOdemeGerceklesen> OdemeKaydetAsync(int planId, int yil, int ay, decimal tutar, DateTime? odemeTarihi);
    Task<AylikOdemeGerceklesen> OdemeDurumGuncelleAsync(int id, OdemeDurumu durum);

    // Takvim ve Planlama
    Task<List<AylikOdemeGerceklesen>> GetTakvimOdemeleriAsync(int firmaId, int yil, int ay);
    Task OtomatikOdemeKayitlariOlusturAsync(int firmaId, int yil, int ay);
    Task<decimal> GetAylikToplamTutarAsync(int firmaId, int yil, int ay);
    Task<Dictionary<int, decimal>> GetYillikOdemeDagilimiAsync(int firmaId, int yil);

    // �statistikler
    Task<decimal> GetToplamAylikOdemeAsync(int firmaId);
    Task<int> GetBekleyenOdemeSayisiAsync(int firmaId);
    Task<decimal> GetBuAyOdenecekTutarAsync(int firmaId);
    
    // Excel Export
    Task<byte[]> ExportAylikOdemeTablosuAsync(int yil, int ay);
    Task<byte[]> ExportYillikOdemeTablosuAsync(int yil);
}



