using MKFiloServis.Shared.Entities;

namespace MKFiloServis.Web.Services.Interfaces;

/// <summary>
/// Luca Portal entegrasyon servisi
/// E-Fatura ve E-Arsiv belgelerine erisim saglar
/// </summary>
public interface ILucaPortalService
{
    // Ayarlar
    Task<LucaPortalSettings?> GetAyarlarAsync(int? firmaId = null);
    Task<bool> AyarKaydetAsync(LucaPortalSettings ayarlar);
    
    // Kimlik Dogrulama
    Task<LucaLoginSonuc> GirisYapAsync(string kullaniciAdi, string sifre);
    Task<LucaLoginSonuc> TokenYenileAsync(string refreshToken);
    Task<bool> CikisYapAsync();
    Task<bool> BaglantiTestiAsync();
    
    // Belge Sorgulama
    Task<LucaSorguSonuc> EFaturaListeleAsync(LucaSorguFiltre filtre);
    Task<LucaSorguSonuc> EArsivListeleAsync(LucaSorguFiltre filtre);
    Task<LucaBelge?> BelgeDetayGetirAsync(string belgeId, LucaBelgeTipi belgeTipi);
    
    // Belge Indirme
    Task<LucaBelgeIndirmeSonuc> XmlIndirAsync(string belgeId, LucaBelgeTipi belgeTipi);
    Task<LucaBelgeIndirmeSonuc> PdfIndirAsync(string belgeId, LucaBelgeTipi belgeTipi);
    Task<List<LucaBelgeIndirmeSonuc>> TopluXmlIndirAsync(List<string> belgeIdler, LucaBelgeTipi belgeTipi);
    Task<List<LucaBelgeIndirmeSonuc>> TopluPdfIndirAsync(List<string> belgeIdler, LucaBelgeTipi belgeTipi);
    
    // Sisteme Aktarma
    Task<int> BelgeleriSistemeAktarAsync(List<LucaBelge> belgeler, bool xmlIndir = true, bool pdfIndir = true);
    Task<int> TumBelgeleriSenkronizeEtAsync(DateTime baslangic, DateTime bitis, IProgress<string>? progress = null);
}




