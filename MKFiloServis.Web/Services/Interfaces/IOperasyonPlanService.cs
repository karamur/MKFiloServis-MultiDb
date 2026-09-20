using MKFiloServis.Shared.Entities;

namespace MKFiloServis.Web.Services.Interfaces;

public interface IOperasyonPlanService
{
    Task<(int Olusan, int Atlanan)> PlanUretAsync(DateTime tarih);
    Task<(int Olusan, int Atlanan, int IslenmeyenGun)> AyPlaniUretAsync(int yil, int ay);
    Task<(int Olusan, int Atlanan)> HaftaSonuPlanlariniUretAsync(int yil, int ay);
    Task<List<OperasyonPlanSatiri>> GetPlanlarAsync(DateTime tarih);
    Task<List<OperasyonPlanSatiri>> GetAylikPlanlarAsync(int yil, int ay);
    Task<OperasyonPlanSatiri> EkSeferPlanSatiriEkleAsync(
        DateTime tarih,
        int guzergahId,
        int aracId,
        int soforId,
        decimal seferSayisi,
        decimal? kurumSeferUcreti,
        ServisTuru servisTuru = ServisTuru.SabahAksam);
    Task<int> PlanlariGuncelleAsync(List<OperasyonPlanSatiri> planlar);
    Task DuzenlemeIcinGeriAlAsync(int planId);
    Task SilTekGunlukPlanAsync(int planId);
    Task<(int Plan, int Puantaj, int Hakedis)> AylikPlaniTemizleAsync(int yil, int ay);
    Task<OperasyonTakvimGunu?> GetTakvimGunuAsync(DateTime tarih);
    Task<int> PlanTeyitEtAsync(List<int> planIdleri);
    Task<List<OperasyonKontrat>> GetKontratlarAsync();
    Task KontratKaydetAsync(OperasyonKontrat kontrat);
    Task TakvimGunuKaydetAsync(OperasyonTakvimGunu gun);
}
