using System.Globalization;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Web.Services;

/// <summary>Aylık rapora girdi olan tek kayıt: iş günü tarihi ve iki seriye ait değerler (kullanılmayan seri 0).</summary>
public readonly record struct AylikSatir(DateTime Tarih, decimal Deger1, decimal Deger2 = 0m);

/// <summary>Bir grafik kartının sabit tanımı (başlık, simge, renkler, hangi serinin artışı iyi).</summary>
public sealed record AylikKartTanimi(
    string Anahtar,
    string Baslik,
    string AltBaslik,
    string Ikon,
    string Renk,
    string Link,
    AylikBirim Birim,
    string Etiket1,
    string Etiket2,
    GrafikRengi Renk1,
    GrafikRengi Renk2,
    bool YuksekIyi1,
    bool YuksekIyi2,
    string AdetEki = "",
    bool Yigili = false);

/// <summary>
/// Aylık rapor için saf hesap: kayıtları 12 aylık kovalara dağıtır, yıl toplamlarını ve geçen yılın aynı dönemini bulur.
/// Veritabanına dokunmaz; tüm tarihler iş günü saatiyle gün olarak verilir.
/// <para>
/// Toplamlar "kesim gününe" kadardır: içinde bulunulan yılda bugün (planlı/gelecek değerler dahil olmaz),
/// geçmiş yılda 31 Aralık. Karşılaştırma geçen yılın aynı gününe kadarki toplamla yapılır; böylece 19 Ekim'de
/// 10 aylık veri geçen yılın tamamıyla kıyaslanıp yanıltmaz.
/// </para>
/// </summary>
public static class AylikSeriHesaplayici
{
    private static readonly CultureInfo Tr = new("tr-TR");

    /// <summary>Toplamların son günü; gelecek yılda gerçekleşen bir dönem olmadığından null.</summary>
    public static DateTime? KesimGunu(int yil, DateTime bugun)
    {
        bugun = bugun.Date;
        if (yil > bugun.Year)
            return null;
        return yil == bugun.Year ? bugun : new DateTime(yil, 12, 31);
    }

    /// <summary>Karşılaştırılan geçen yıl kesimi (29 Şubat → 28 Şubat; geçmiş yıllarda geçen yılın tamamı).</summary>
    public static DateTime OncekiYilKesimi(int yil, DateTime bugun)
    {
        bugun = bugun.Date;
        return yil == bugun.Year ? bugun.AddYears(-1) : new DateTime(yil - 1, 12, 31);
    }

    /// <summary>Seçilen yılın 12 ayı; yalnız o yıla ait kayıtlar toplanır. İçinde bulunulan aydan sonraki aylar "gelecek"tir.</summary>
    public static ZamanSerisi Seri(int yil, DateTime bugun, IEnumerable<AylikSatir> satirlar, string etiket1, string etiket2)
    {
        var v1 = new decimal[12];
        var v2 = new decimal[12];
        foreach (var s in satirlar)
        {
            if (s.Tarih.Year != yil)
                continue;
            v1[s.Tarih.Month - 1] += s.Deger1;
            v2[s.Tarih.Month - 1] += s.Deger2;
        }

        var buAy = new DateTime(bugun.Year, bugun.Month, 1);
        var noktalar = new List<SeriNoktasi>(12);
        for (var ay = 1; ay <= 12; ay++)
        {
            var bas = new DateTime(yil, ay, 1);
            noktalar.Add(new SeriNoktasi(bas, bas.ToString("MMM", Tr), bas.ToString("MMMM yyyy", Tr), v1[ay - 1], v2[ay - 1], bas > buAy));
        }

        return new ZamanSerisi { Noktalar = noktalar, Aylik = true, Etiket1 = etiket1, Etiket2 = etiket2 };
    }

    /// <summary>[bas, bit] gün aralığındaki (uçlar dahil) kayıtların iki toplamı.</summary>
    public static (decimal Deger1, decimal Deger2) Toplam(IEnumerable<AylikSatir> satirlar, DateTime bas, DateTime bit)
    {
        decimal t1 = 0, t2 = 0;
        foreach (var s in satirlar)
        {
            var gun = s.Tarih.Date;
            if (gun < bas.Date || gun > bit.Date)
                continue;
            t1 += s.Deger1;
            t2 += s.Deger2;
        }

        return (t1, t2);
    }

    /// <summary>
    /// Kayıtlar yıl öncesini de içerebilir (geçen yıl karşılaştırması için): seri yalnız <paramref name="yil"/>'ı,
    /// karşılaştırma bir önceki yılın kesim gününe kadarki kısmı kullanır.
    /// </summary>
    public static AylikGrafikKarti KartKur(AylikKartTanimi tanim, int yil, DateTime bugun, IReadOnlyCollection<AylikSatir> satirlar)
    {
        var seri = Seri(yil, bugun, satirlar, tanim.Etiket1, tanim.Etiket2);
        decimal t1 = 0, t2 = 0, o1 = 0, o2 = 0;

        var kesim = KesimGunu(yil, bugun);
        if (kesim is { } son)
        {
            (t1, t2) = Toplam(satirlar, new DateTime(yil, 1, 1), son);
            (o1, o2) = Toplam(satirlar, new DateTime(yil - 1, 1, 1), OncekiYilKesimi(yil, bugun));
        }

        return new AylikGrafikKarti
        {
            Anahtar = tanim.Anahtar,
            Baslik = tanim.Baslik,
            AltBaslik = tanim.AltBaslik,
            Ikon = tanim.Ikon,
            Renk = tanim.Renk,
            Link = tanim.Link,
            Birim = tanim.Birim,
            AdetEki = tanim.AdetEki,
            Yigili = tanim.Yigili,
            Renk1 = tanim.Renk1,
            Renk2 = tanim.Renk2,
            YuksekIyi1 = tanim.YuksekIyi1,
            YuksekIyi2 = tanim.YuksekIyi2,
            Seri = seri,
            Toplam1 = t1,
            Toplam2 = t2,
            OncekiYilToplam1 = o1,
            OncekiYilToplam2 = o2,
            KarsilastirmaVar = kesim.HasValue
        };
    }
}
