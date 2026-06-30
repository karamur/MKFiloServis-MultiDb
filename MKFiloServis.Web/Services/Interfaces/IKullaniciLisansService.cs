using MKFiloServis.Shared.Entities;

namespace MKFiloServis.Web.Services.Interfaces;

public interface ILisansService
{
    // Lisans Yonetimi
    Task<Lisans?> GetAktifLisansAsync();
    Task<bool> LisansGecerliMiAsync();
    Task<Lisans> AktiveLisansAsync(string lisansAnahtari);
    Task<int> KalanGunAsync();
    Task<string> GetMakineKoduAsync();

    // Trial
    Task<Lisans> OlusturTrialLisansAsync();

    // Kontroller
    Task<bool> KullanicLimitiKontrolAsync();
    Task<bool> ModulIzniVarMiAsync(string modulAdi);
}



