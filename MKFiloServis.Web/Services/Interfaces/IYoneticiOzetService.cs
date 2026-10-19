using MKFiloServis.Web.Services;

namespace MKFiloServis.Web.Services.Interfaces;

/// <summary>
/// Yönetici özeti: seçilen dönemde sistemde yapılan işlerin (sefer, hakediş, fatura, tahsilat/ödeme,
/// bütçe, puantaj/maaş, ekip hareketleri) tek bakışta okunabilir raporu.
/// Her bölüm bağımsız üretilir; biri hata verirse diğerleri yine gelir (<see cref="YoneticiOzetRapor.Hatalar"/>).
/// </summary>
public interface IYoneticiOzetService
{
    Task<YoneticiOzetRapor> GetRaporAsync(YoneticiOzetIstegi istek, CancellationToken cancellationToken = default);

    /// <summary>
    /// Aylık grafik raporu: seçilen yılın 12 ayı için her bölümün (sefer, hakediş, fatura, tahsilat/ödeme, bütçe, maaş,
    /// masraf, kiralama, yeni kayıt, evrak, ekip işlemi) aylık serisi ve geçen yılın aynı dönemiyle karşılaştırması.
    /// </summary>
    Task<AylikGrafikRaporu> GetAylikGrafikAsync(AylikGrafikIstegi istek, CancellationToken cancellationToken = default);
}

public enum YoneticiOzetDonemTuru
{
    Bugun = 1,
    BuHafta = 2,
    BuAy = 3,
    GecenAy = 4,
    BuYil = 5,
    Ozel = 6
}

/// <summary>
/// Hangi bölümlerin üretileceği. Çağıran (Dashboard) kullanıcı yetkisi ve modül lisansına göre doldurur;
/// kapalı bölümün sorgusu hiç çalışmaz ve rapordaki karşılığı null kalır.
/// </summary>
public sealed record YoneticiOzetKapsami(
    bool Servis,
    bool Hakedis,
    bool Fatura,
    bool Finans,
    bool Personel,
    bool Cari,
    bool Arac,
    bool Evrak,
    bool RentACar,
    bool Ekip)
{
    public static YoneticiOzetKapsami Tumu { get; } = new(true, true, true, true, true, true, true, true, true, true);

    public static YoneticiOzetKapsami Hicbiri { get; } = new(false, false, false, false, false, false, false, false, false, false);

    public bool HerhangiBiri => Servis || Hakedis || Fatura || Finans || Personel || Cari || Arac || Evrak || RentACar || Ekip;
}

public sealed class YoneticiOzetIstegi
{
    public YoneticiOzetDonemTuru DonemTuru { get; init; } = YoneticiOzetDonemTuru.BuAy;

    /// <summary><see cref="YoneticiOzetDonemTuru.Ozel"/> için aralık başı.</summary>
    public DateTime? OzelBaslangic { get; init; }

    /// <summary><see cref="YoneticiOzetDonemTuru.Ozel"/> için aralık sonu (dahil).</summary>
    public DateTime? OzelBitis { get; init; }

    public YoneticiOzetKapsami Kapsam { get; init; } = YoneticiOzetKapsami.Tumu;

    /// <summary>Test/önizleme için sabit "bugün"; verilmezse iş günü (<c>BusinessTime.Today</c>).</summary>
    public DateTime? Bugun { get; init; }

    /// <summary>
    /// Dashboard'un belge uyarısı servisinden zaten yüklediği kritik belge sayısı; verilirse
    /// "Dikkat gerektirenler" listesine eklenir (aynı sorgu ikinci kez çalıştırılmaz).
    /// </summary>
    public int? KritikBelgeSayisi { get; init; }
}
