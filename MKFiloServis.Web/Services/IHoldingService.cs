using MKFiloServis.Shared.Entities;

namespace MKFiloServis.Web.Services;

public interface IHoldingService
{
    Task ToplaVeKaydetAsync(int yil, int ay);
    Task<List<HoldingVeri>> GetFirmaKarsilastirmaAsync(int yil, int? ay = null);
    Task<List<HoldingVeri>> GetButceKonsolidasyonAsync(int yil);
    Task<List<HoldingVeri>> GetAracMaliyetOzetiAsync(int yil, int? ay = null);
    Task<List<HoldingVeri>> GetPersonelGiderOzetiAsync(int yil, int? ay = null);
    Task<List<HoldingVeri>> GetHakedisOzetiAsync(int yil, int? ay = null);
    Task<List<HoldingRapor>> GetKayitliRaporlarAsync();
    Task<HoldingRapor> RaporKaydetAsync(HoldingRapor rapor);

    /// <summary>Kâr/zarar anlık görüntüsü olan en son dönem (ekranların açılış dönemi); hiç veri yoksa null.</summary>
    Task<HoldingDonemSecimi?> GetSonVeriDonemiAsync();

    /// <summary>
    /// Aktif firmaların ödenmemiş (iptal ve tam ödenmiş olmayan) alış/satış faturalarının güncel kalan tutarı ve vadesi.
    /// Aktif firma seçiminden bağımsızdır; anlık görüntü değil canlı veridir.
    /// </summary>
    Task<List<HoldingVadeKalemi>> GetAcikFaturalarAsync();
}
