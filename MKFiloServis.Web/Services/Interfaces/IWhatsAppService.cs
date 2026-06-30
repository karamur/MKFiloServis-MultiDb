using MKFiloServis.Shared.Entities;

namespace MKFiloServis.Web.Services.Interfaces;

public interface IWhatsAppService
{
    // Kişiler
    Task<List<WhatsAppKisi>> GetKisilerAsync();
    Task<WhatsAppKisi?> GetKisiByIdAsync(int id);
    Task<WhatsAppKisi> CreateKisiAsync(WhatsAppKisi kisi);
    Task<WhatsAppKisi> UpdateKisiAsync(WhatsAppKisi kisi);
    Task DeleteKisiAsync(int id);
    Task SeciliCarilerdenKisiOlustur(List<int> cariIds);
    Task<int> KisileriSenkronizeEtAsync(List<WhatsAppKisi> kisiler);

    // Gruplar
    Task<List<WhatsAppGrup>> GetGruplarAsync();
    Task<WhatsAppGrup?> GetGrupByIdAsync(int id);
    Task<WhatsAppGrup> CreateGrupAsync(WhatsAppGrup grup);
    Task<WhatsAppGrup> UpdateGrupAsync(WhatsAppGrup grup);
    Task DeleteGrupAsync(int id);
    Task GrubaKisiEkleAsync(int grupId, int kisiId);
    Task GruptanKisiCikarAsync(int grupId, int kisiId);

    // Şablonlar
    Task<List<WhatsAppSablon>> GetSablonlarAsync();
    Task<WhatsAppSablon?> GetSablonByIdAsync(int id);
    Task<WhatsAppSablon> CreateSablonAsync(WhatsAppSablon sablon);
    Task<WhatsAppSablon> UpdateSablonAsync(WhatsAppSablon sablon);
    Task DeleteSablonAsync(int id);

    // Mesajlar
    Task<List<WhatsAppMesaj>> GetMesajlarByKisiAsync(int kisiId);
    Task<List<WhatsAppMesaj>> GetMesajlarByGrupAsync(int grupId);
    Task<WhatsAppMesaj> SendMesajToKisiAsync(int kisiId, string icerik, int? gonderenId = null);
    Task<WhatsAppMesaj> SendMesajToGrupAsync(int grupId, string icerik, int? gonderenId = null);
    Task<int> GetOkunmamisMesajSayisiAsync();
    Task MesajlariOkunduIsaretleAsync(int? kisiId = null, int? grupId = null);
}




