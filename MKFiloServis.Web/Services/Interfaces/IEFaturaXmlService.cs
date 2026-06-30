using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Models;

namespace MKFiloServis.Web.Services.Interfaces;

/// <summary>
/// E-Fatura XML oluşturma servisi (GİB UBL-TR 1.2)
/// </summary>
public interface IEFaturaXmlService
{
    /// <summary>
    /// Fatura için E-Fatura XML oluşturur
    /// </summary>
    Task<EFaturaXmlSonuc> XmlOlusturAsync(EFaturaXmlRequest request);

    /// <summary>
    /// Fatura verisini UBL-TR formatına dönüştürür
    /// </summary>
    Task<UblInvoice?> UblDonusturAsync(int faturaId);

    /// <summary>
    /// E-Fatura XML'ini doğrular
    /// </summary>
    Task<EFaturaDogrulamaRapor> DogrulaAsync(string xmlIcerik);

    /// <summary>
    /// E-Fatura XML'ini dosyaya kaydeder
    /// </summary>
    Task<string?> DosyayaKaydetAsync(int faturaId, string xmlIcerik);

    /// <summary>
    /// Kaydedilmiş E-Fatura XML'ini okur
    /// </summary>
    Task<string?> XmlOkuAsync(int faturaId);

    /// <summary>
    /// Toplu E-Fatura XML oluşturur
    /// </summary>
    Task<List<EFaturaXmlSonuc>> TopluXmlOlusturAsync(List<int> faturaIdler, EFaturaSenaryo senaryo);

    /// <summary>
    /// GİB gönderim durumunu günceller
    /// </summary>
    Task<bool> GibDurumGuncelleAsync(int faturaId, GibGonderimDurumu durum, string? gibKodu = null, string? mesaj = null);

    /// <summary>
    /// ETTN (UUID) numarası oluşturur
    /// </summary>
    string YeniEttnOlustur();

    /// <summary>
    /// Birim kodunu UBL-TR formatına dönüştürür
    /// </summary>
    string BirimKoduDonustur(string birim);
}




