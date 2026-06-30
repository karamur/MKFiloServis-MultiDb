using MKFiloServis.Shared.Entities;

namespace MKFiloServis.Web.Services.Interfaces;

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
    Task<Hakedis> KurumHakedisiUretAsync(int kurumFirmaId, int yil, int ay);

    /// <summary>
    /// Tedarikçi bazlı gider hakedişi üretir.
    /// Aracı tedarikçiye ait olan FiloGunlukPuantaj kayıtlarını toplulaştırır.
    /// </summary>
    Task<Hakedis> TedarikciHakedisiUretAsync(int tasimaTedarikciId, int yil, int ay);

    /// <summary>
    /// Araç bazlı iç hakediş/karlılık özeti.
    /// Faturalanmaz; sadece raporlama amaçlıdır.
    /// </summary>
    Task<Hakedis> AracHakedisiUretAsync(int aracId, int yil, int ay);

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
    Task<TopluHakedisSonuc> TopluUretAsync(HakedisTipi tip, int yil, int ay);

    /// <summary>Verilen id listesindeki Taslak hakedişleri toplu onaylar.</summary>
    Task<TopluIslemSonuc> TopluOnaylaAsync(IEnumerable<int> hakedisIds, string onaylayanKisi);

    /// <summary>Verilen id listesindeki Taslak hakedişleri toplu siler (faturalanmamış olanlar).</summary>
    Task<TopluIslemSonuc> TopluSilAsync(IEnumerable<int> hakedisIds);

    /// <summary>
    /// Verilen id listesindeki Onaylı hakedişleri toplu olarak faturaya dönüştürür.
    /// Faturalanmış / Taslak / İptal kayıtlar atlanır.
    /// </summary>
    Task<TopluIslemSonuc> TopluFaturalaAsync(IEnumerable<int> hakedisIds, DateTime faturaTarihi);
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

/// <summary>Toplu onay/silme gibi basit toplu işlemler için sonuç.</summary>
public record TopluIslemSonuc(
    int ToplamSecili,
    int BasariliAdet,
    int AtlananAdet,
    int HataliAdet,
    List<TopluIslemSatir> Satirlar
);

public record TopluIslemSatir(
    int HakedisId,
    string Sonuc,        // "Tamam" | "Atlandı" | "Hata"
    string? Mesaj,
    int? FaturaId = null,
    string? FaturaNo = null
);




