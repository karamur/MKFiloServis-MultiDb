using MKFiloServis.Shared.Entities;

namespace MKFiloServis.Web.Services.Interfaces;

/// <summary>
/// EBYS Gelişmiş Belge Arama Servisi Interface
/// </summary>
public interface IEbysBelgeAramaService
{
    #region Ana Arama

    /// <summary>
    /// Gelişmiş arama yapar
    /// </summary>
    Task<EbysAramaSonuc> AraAsync(EbysGelismisAramaFiltre filtre);

    /// <summary>
    /// Hızlı arama yapar (sadece metin bazlı)
    /// </summary>
    Task<EbysAramaSonuc> HizliAraAsync(string aramaMetni, int maxSonuc = 10);

    /// <summary>
    /// Belirli bir kaynak tipinde arama yapar
    /// </summary>
    Task<EbysAramaSonuc> KaynaktaAraAsync(EbysAramaKaynak kaynak, EbysGelismisAramaFiltre filtre);

    #endregion

    #region Arama Önerileri

    /// <summary>
    /// Arama önerileri getirir
    /// </summary>
    Task<List<EbysAramaOnerisi>> GetAramaOnerileriAsync(string metin, int kullaniciId);

    /// <summary>
    /// Popüler aramaları getirir
    /// </summary>
    Task<List<string>> GetPopulerAramalarAsync(int adet = 10);

    /// <summary>
    /// İlgili aramaları getirir
    /// </summary>
    Task<List<string>> GetIlgiliAramalarAsync(string aramaMetni);

    #endregion

    #region Arama Geçmişi

    /// <summary>
    /// Kullanıcının arama geçmişini getirir
    /// </summary>
    Task<List<EbysAramaGecmisi>> GetAramaGecmisiAsync(int kullaniciId, int adet = 20);

    /// <summary>
    /// Arama geçmişine kayıt ekler
    /// </summary>
    Task KaydetAramaGecmisiAsync(int kullaniciId, EbysGelismisAramaFiltre filtre, int sonucSayisi);

    /// <summary>
    /// Arama geçmişini temizler
    /// </summary>
    Task TemizleAramaGecmisiAsync(int kullaniciId);

    #endregion

    #region Kayıtlı Aramalar

    /// <summary>
    /// Kullanıcının kayıtlı aramalarını getirir
    /// </summary>
    Task<List<EbysKayitliArama>> GetKayitliAramalarAsync(int kullaniciId);

    /// <summary>
    /// Aramayı kaydeder
    /// </summary>
    Task<EbysKayitliArama> AramaKaydetAsync(int kullaniciId, string aramaAdi, EbysGelismisAramaFiltre filtre);

    /// <summary>
    /// Kayıtlı aramayı siler
    /// </summary>
    Task SilKayitliAramaAsync(int id);

    /// <summary>
    /// Kayıtlı arama günceller
    /// </summary>
    Task GuncelleKayitliAramaAsync(EbysKayitliArama arama);

    #endregion

    #region İstatistikler

    /// <summary>
    /// Tüm belgeler için istatistik getirir
    /// </summary>
    Task<EbysAramaIstatistik> GetGenelIstatistiklerAsync();

    /// <summary>
    /// Kategori bazlı istatistikler getirir
    /// </summary>
    Task<Dictionary<string, int>> GetKategoriBazliSayilarAsync();

    /// <summary>
    /// Riskli belge sayısını getirir
    /// </summary>
    Task<int> GetRiskliBelgeSayisiAsync(int yaklasanGunSayisi = 30);

    #endregion

    #region Yardımcı Metodlar

    /// <summary>
    /// Mevcut kategorileri getirir
    /// </summary>
    Task<List<string>> GetTumKategorilerAsync();

    /// <summary>
    /// Arama filtresi oluşturur (JSON'dan)
    /// </summary>
    EbysGelismisAramaFiltre? FiltreOlustur(string filtreJson);

    /// <summary>
    /// Filtreyi JSON'a çevirir
    /// </summary>
    string FiltreToJson(EbysGelismisAramaFiltre filtre);

    #endregion
}



