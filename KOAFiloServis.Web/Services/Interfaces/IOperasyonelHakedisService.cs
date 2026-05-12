using KOAFiloServis.Shared.Entities;

namespace KOAFiloServis.Web.Services.Interfaces;

/// <summary>
/// Yeni Hakediş entity'si üzerinden çalışan operasyonel hakediş servisi.
/// FiloGunlukPuantaj ve ServisPuantaj kayıtlarından kurum / tedarikçi / araç bazlı
/// dönemsel hakediş üretir, onaylar ve faturaya dönüştürmek için altyapı sağlar.
/// (Eski PuantajKayit tabanlı IHakedisService bozulmaz; bu servis paralel akıştır.)
/// </summary>
public interface IOperasyonelHakedisService
{
    /// <summary>Belirli dönem ve filtrelere göre hakediş listesi.</summary>
    Task<List<Hakedis>> GetHakedislerAsync(int? yil = null, int? ay = null, HakedisTipi? tip = null, int? referansId = null, HakedisDurum? durum = null);

    Task<Hakedis?> GetByIdAsync(int id);

    /// <summary>
    /// Kurum bazlı gelir hakedişi üretir.
    /// FiloGunlukPuantaj kayıtlarından (KurumFirmaId == kurumFirmaId) toplulaştırır.
    /// </summary>
    Task<Hakedis> KurumHakedisiUretAsync(int kurumFirmaId, int yil, int ay, int? sirketId = null);

    /// <summary>
    /// Tedarikçi bazlı gider hakedişi üretir.
    /// Aracı tedarikçiye ait olan FiloGunlukPuantaj kayıtlarını toplulaştırır.
    /// </summary>
    Task<Hakedis> TedarikciHakedisiUretAsync(int tasimaTedarikciId, int yil, int ay, int? sirketId = null);

    /// <summary>
    /// Araç bazlı iç hakediş/karlılık özeti.
    /// Faturalanmaz; sadece raporlama amaçlıdır.
    /// </summary>
    Task<Hakedis> AracHakedisiUretAsync(int aracId, int yil, int ay, int? sirketId = null);

    Task<Hakedis> OnaylaAsync(int hakedisId, string onaylayanKisi);
    Task<Hakedis> IptalEtAsync(int hakedisId, string? aciklama = null);

    /// <summary>
    /// Onaylı hakedişten Fatura oluşturur.
    /// Kurum hakedişinden Gelen/Giden(Giden) gelir faturası, Tedarikçi hakedişinden gider faturası üretilir.
    /// </summary>
    Task<Fatura> FaturayaDonustureAsync(int hakedisId, DateTime faturaTarihi, string? faturaNo = null);

    Task<bool> SilAsync(int hakedisId);

    /// <summary>
    /// Hakediş üretmeden önce tahmini değerleri döndürür (puantaj sayısı, sefer, tutar, çakışma uyarısı).
    /// </summary>
    Task<HakedisOnizleme> OnizleAsync(HakedisTipi tip, int referansId, int yil, int ay);

    /// <summary>
    /// Verilen dönem ve tip için, dönem puantajlarında geçen tüm referansları (Kurum/Tedarikçi/Araç)
    /// tespit edip her biri için ayrı hakediş üretir. Mevcut Onaylı/Faturalı kayıtlar atlanır,
    /// Taslak kayıtların üzerine yazılır.
    /// </summary>
    Task<TopluHakedisSonuc> TopluUretAsync(HakedisTipi tip, int yil, int ay, int? sirketId = null);
}

/// <summary>Hakediş üretim önizleme sonucu.</summary>
public record HakedisOnizleme(
    int PuantajSayisi,
    decimal ToplamSefer,
    decimal TahminiTutar,
    bool MevcutTaslakVar,
    bool MevcutOnayliVar,
    int? MevcutHakedisId,
    HakedisDurum? MevcutHakedisDurum
);

/// <summary>Toplu hakediş üretim sonucu.</summary>
public record TopluHakedisSonuc(
    int ToplamReferans,
    int UretilenAdet,
    int AtlananAdet,
    int HataliAdet,
    decimal ToplamTutar,
    List<TopluHakedisSatir> Satirlar
);

public record TopluHakedisSatir(
    int ReferansId,
    string ReferansAd,
    string Sonuc,        // "Üretildi" | "Atlandı" | "Hata"
    string? Mesaj,
    int? HakedisId,
    decimal Tutar,
    decimal Sefer
);
