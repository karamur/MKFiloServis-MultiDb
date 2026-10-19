using System.Globalization;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Web.Services;

/// <summary>
/// Yönetici özeti dönemi: seçilen aralık, "bugüne kadar" kırpılmış etkin aralık ve önceki dönemle
/// karşılaştırma aralığı. Saf hesap; veritabanına dokunmaz. Tüm tarihler gün (saat 00:00), bitişler dahildir.
/// <para>
/// Karşılaştırma adildir: ayın 19. günündeyken "Bu Ay", geçen ayın tamamıyla değil
/// geçen ayın ilk 19 günüyle kıyaslanır (hafta ve yıl için de aynı).
/// </para>
/// </summary>
public sealed class YoneticiOzetDonemi
{
    private static readonly CultureInfo Tr = new("tr-TR");

    /// <summary>Özel aralığın sorgu yükünü sınırlayan üst sınırı (gün).</summary>
    public const int AzamiOzelGun = 731;

    private const int GunlukGrafikAzamiGun = 62;
    private const int BugunGrafikGunu = 14;

    private YoneticiOzetDonemi(
        YoneticiOzetDonemTuru tur,
        DateTime bugun,
        DateTime baslangic,
        DateTime bitis,
        DateTime oncekiBaslangic,
        DateTime oncekiBitis)
    {
        Tur = tur;
        Bugun = bugun;
        Baslangic = baslangic;
        Bitis = bitis;
        EtkinBitis = bitis < bugun ? bitis : bugun;
        OncekiBaslangic = oncekiBaslangic;
        OncekiBitis = oncekiBitis;

        // "Bugün" tek sütunluk grafik vermez; son iki haftayı gösterir.
        GrafikBaslangic = tur == YoneticiOzetDonemTuru.Bugun ? bugun.AddDays(-(BugunGrafikGunu - 1)) : baslangic;
        GrafikBitis = bitis;
        GrafikAylik = (GrafikBitis - GrafikBaslangic).Days + 1 > GunlukGrafikAzamiGun;
    }

    public YoneticiOzetDonemTuru Tur { get; }

    public DateTime Bugun { get; }

    /// <summary>Seçilen dönemin ilk günü.</summary>
    public DateTime Baslangic { get; }

    /// <summary>Seçilen dönemin son günü (ay/hafta/yıl sonu olabilir; bugünden sonrası "planlı"dır).</summary>
    public DateTime Bitis { get; }

    /// <summary>Gerçekleşen işlerin son günü: dönem sonu ile bugünden küçük olanı.</summary>
    public DateTime EtkinBitis { get; }

    public DateTime OncekiBaslangic { get; }

    public DateTime OncekiBitis { get; }

    public DateTime GrafikBaslangic { get; }

    public DateTime GrafikBitis { get; }

    /// <summary>Grafik kovaları aylık mı (uzun aralık) günlük mü.</summary>
    public bool GrafikAylik { get; }

    /// <summary>Bugüne kadar geçen gün sayısı (dönemin tamamı geçmişteyse tüm gün sayısı).</summary>
    public int GecenGunSayisi => EtkinBitis >= Baslangic ? (EtkinBitis - Baslangic).Days + 1 : 0;

    public int ToplamGunSayisi => (Bitis - Baslangic).Days + 1;

    /// <summary>Dönemin bugünden sonraya uzanan günleri var mı.</summary>
    public bool GelecekGunVar => Bitis > Bugun;

    /// <summary>Başlıkta gösterilen kısa ad ("Ekim 2026", "Bu Hafta", "Bugün"…).</summary>
    public string Etiket => Tur switch
    {
        YoneticiOzetDonemTuru.Bugun => "Bugün",
        YoneticiOzetDonemTuru.BuHafta => "Bu Hafta",
        YoneticiOzetDonemTuru.BuAy or YoneticiOzetDonemTuru.GecenAy => Baslangic.ToString("MMMM yyyy", Tr),
        YoneticiOzetDonemTuru.BuYil => Baslangic.ToString("yyyy", Tr),
        _ => AralikMetni
    };

    /// <summary>Seçilen aralığın okunur hali ("1 – 31 Ekim 2026").</summary>
    public string AralikMetni => AralikEtiketi(Baslangic, Bitis);

    /// <summary>Raporun gerçekten kapsadığı gün aralığı (bugüne kırpılmış).</summary>
    public string EtkinAralikMetni => EtkinBitis >= Baslangic ? AralikEtiketi(Baslangic, EtkinBitis) : AralikMetni;

    /// <summary>Karşılaştırılan önceki aralık ("1 – 19 Eylül 2026").</summary>
    public string KarsilastirmaMetni => AralikEtiketi(OncekiBaslangic, OncekiBitis);

    /// <summary>Cümle başında kullanılan giriş ("Bu ay", "Bugün", "Bu aralıkta"); aralık metni başlıkta zaten görünür.</summary>
    public string CumleGirisi => Tur switch
    {
        YoneticiOzetDonemTuru.Bugun => "Bugün",
        YoneticiOzetDonemTuru.BuHafta => "Bu hafta",
        YoneticiOzetDonemTuru.BuAy => "Bu ay",
        YoneticiOzetDonemTuru.GecenAy => "Geçen ay",
        YoneticiOzetDonemTuru.BuYil => "Bu yıl",
        _ => "Bu aralıkta"
    };

    public static YoneticiOzetDonemi Hesapla(
        YoneticiOzetDonemTuru tur,
        DateTime bugun,
        DateTime? ozelBaslangic = null,
        DateTime? ozelBitis = null)
    {
        bugun = bugun.Date;
        DateTime bas, bit;

        switch (tur)
        {
            case YoneticiOzetDonemTuru.Bugun:
                bas = bit = bugun;
                break;
            case YoneticiOzetDonemTuru.BuHafta:
                bas = bugun.AddDays(-(((int)bugun.DayOfWeek + 6) % 7)); // pazartesi
                bit = bas.AddDays(6);
                break;
            case YoneticiOzetDonemTuru.GecenAy:
                bas = new DateTime(bugun.Year, bugun.Month, 1).AddMonths(-1);
                bit = bas.AddMonths(1).AddDays(-1);
                break;
            case YoneticiOzetDonemTuru.BuYil:
                bas = new DateTime(bugun.Year, 1, 1);
                bit = new DateTime(bugun.Year, 12, 31);
                break;
            case YoneticiOzetDonemTuru.Ozel:
                bas = (ozelBaslangic ?? bugun.AddDays(-29)).Date;
                bit = (ozelBitis ?? bugun).Date;
                if (bit < bas)
                    (bas, bit) = (bit, bas);
                if ((bit - bas).Days + 1 > AzamiOzelGun)
                    bas = bit.AddDays(-(AzamiOzelGun - 1));
                break;
            default:
                tur = YoneticiOzetDonemTuru.BuAy;
                bas = new DateTime(bugun.Year, bugun.Month, 1);
                bit = bas.AddMonths(1).AddDays(-1);
                break;
        }

        var etkinBitis = bit < bugun ? bit : bugun;
        var gecen = etkinBitis >= bas ? (etkinBitis - bas).Days + 1 : 0;
        var uzunluk = Math.Max(gecen, 1);

        DateTime oncekiBas, oncekiBit;
        switch (tur)
        {
            case YoneticiOzetDonemTuru.Bugun:
                oncekiBas = oncekiBit = bas.AddDays(-1);
                break;
            case YoneticiOzetDonemTuru.BuHafta:
                oncekiBas = bas.AddDays(-7);
                oncekiBit = oncekiBas.AddDays(uzunluk - 1);
                break;
            case YoneticiOzetDonemTuru.BuAy:
                oncekiBas = bas.AddMonths(-1);
                oncekiBit = Kucuk(bas.AddDays(-1), oncekiBas.AddDays(uzunluk - 1));
                break;
            case YoneticiOzetDonemTuru.GecenAy:
                oncekiBas = bas.AddMonths(-1);
                oncekiBit = bas.AddDays(-1);
                break;
            case YoneticiOzetDonemTuru.BuYil:
                oncekiBas = bas.AddYears(-1);
                oncekiBit = Kucuk(bas.AddDays(-1), oncekiBas.AddDays(uzunluk - 1));
                break;
            default:
                oncekiBit = bas.AddDays(-1);
                oncekiBas = bas.AddDays(-uzunluk);
                break;
        }

        return new YoneticiOzetDonemi(tur, bugun, bas, bit, oncekiBas, oncekiBit);
    }

    /// <summary>"19 Ekim 2026", "1 – 19 Ekim 2026", "28 Eylül – 4 Ekim 2026" biçiminde aralık metni.</summary>
    public static string AralikEtiketi(DateTime bas, DateTime bit)
    {
        if (bas.Date == bit.Date)
            return bas.ToString("d MMMM yyyy", Tr);
        if (bas.Year == bit.Year && bas.Month == bit.Month)
            return $"{bas.Day} – {bit.ToString("d MMMM yyyy", Tr)}";
        if (bas.Year == bit.Year)
            return $"{bas.ToString("d MMMM", Tr)} – {bit.ToString("d MMMM yyyy", Tr)}";
        return $"{bas.ToString("d MMMM yyyy", Tr)} – {bit.ToString("d MMMM yyyy", Tr)}";
    }

    private static DateTime Kucuk(DateTime a, DateTime b) => a < b ? a : b;
}
