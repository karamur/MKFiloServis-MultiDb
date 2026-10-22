using System.Globalization;
using MKFiloServis.Web.Helpers;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Web.Services;

public enum HoldingSeriTuru
{
    Sutun = 0,
    Cizgi = 1
}

/// <summary>Sütun grafiğinin bir kategorisi (genellikle bir ay).</summary>
public sealed record HoldingKategori(string Etiket, string Aciklama, bool VeriVar = true, bool Vurgulu = false);

/// <summary>Bir seri: kategori sayısı kadar değer (null: o kategoride veri yok).</summary>
public sealed record HoldingSeri(string Ad, string Renk, IReadOnlyList<decimal?> Degerler, HoldingSeriTuru Tur = HoldingSeriTuru.Sutun);

/// <summary>Halka grafiğin dilimi.</summary>
public sealed record HoldingDilim(string Etiket, decimal Deger, string Renk);

/// <summary>Yatay çubuk listesinde bir satırın tek çubuğu.</summary>
public sealed record HoldingCubukDegeri(string Ad, decimal Deger, string Metin, string Renk);

/// <summary>Yatay çubuk listesinin satırı: etiket + bir ya da birkaç çubuk.</summary>
public sealed record HoldingCubukSatiri(string Etiket, string? AltMetin, IReadOnlyList<HoldingCubukDegeri> Degerler, string? Renk = null);

public enum HoldingRozetYonu
{
    Yok = 0,
    Yukari = 1,
    Asagi = 2,
    Ayni = 3
}

/// <summary>Önceki döneme göre değişim rozeti. <see cref="Iyi"/> true yeşil, false kırmızı, null nötr gösterilir.</summary>
public sealed record HoldingRozet(HoldingRozetYonu Yon, string Metin, bool? Iyi, string? Ipucu = null)
{
    public static HoldingRozet Yok { get; } = new(HoldingRozetYonu.Yok, string.Empty, null);

    public bool Var => Yon != HoldingRozetYonu.Yok;

    /// <summary>
    /// Yüzde değişim rozeti. <paramref name="yuksekIyi"/>: artış iyi haberse true (gelir, kâr), maliyette false.
    /// Önceki dönemde değer yoksa yüzde hesaplanamaz; rozet gösterilmez.
    /// </summary>
    public static HoldingRozet Degisim(OzetDegisim d, bool yuksekIyi, string? ipucu = null) => d.Yon switch
    {
        DegisimYonu.Artti => new HoldingRozet(HoldingRozetYonu.Yukari, $"%{d.Yuzde}", yuksekIyi, ipucu),
        DegisimYonu.Azaldi => new HoldingRozet(HoldingRozetYonu.Asagi, $"%{d.Yuzde}", !yuksekIyi, ipucu),
        DegisimYonu.Ayni => new HoldingRozet(HoldingRozetYonu.Ayni, "=", null, ipucu),
        _ => Yok
    };

    /// <summary>Yüzde puanı farkı (kâr marjı gibi oranlar için).</summary>
    public static HoldingRozet Puan(decimal fark, bool yuksekIyi, string? ipucu = null)
    {
        var yuvarlak = Math.Round(fark, 1, MidpointRounding.AwayFromZero);
        if (yuvarlak == 0m)
            return new HoldingRozet(HoldingRozetYonu.Ayni, "=", null, ipucu);

        var metin = Math.Abs(yuvarlak).ToString("0.#", CultureInfo.GetCultureInfo("tr-TR")) + " puan";
        return yuvarlak > 0m
            ? new HoldingRozet(HoldingRozetYonu.Yukari, metin, yuksekIyi, ipucu)
            : new HoldingRozet(HoldingRozetYonu.Asagi, metin, !yuksekIyi, ipucu);
    }
}

/// <summary>
/// Holding grafiklerinin renk belirteçleri. Anlamlı renkler (gelir, gider…) sabittir; firmalar sıra numarasına göre
/// ayrı bir paletten renk alır, böylece bir firma her sayfada aynı renkte görünür.
/// </summary>
public static class HoldingRenk
{
    public const string Gelir = "gelir";
    public const string Gider = "gider";
    public const string Kar = "kar";
    public const string Arac = "arac";
    public const string Personel = "personel";
    public const string Hakedis = "hakedis";
    public const string Butce = "butce";
    public const string Uyari = "uyari";
    public const string Notr = "notr";

    private static readonly string[] FirmaPaleti =
    [
        "#6366f1", "#0ea5e9", "#fb923c", "#ec4899", "#84cc16",
        "#14b8a6", "#f43f5e", "#a78bfa", "#eab308", "#64748b"
    ];

    public static string Firma(int sira) => "f" + (sira < 0 ? 0 : sira) % FirmaPaleti.Length;

    /// <summary>Belirteci (gelir, f3…) CSS renk koduna çevirir; bilinmeyen belirteç nötr gri olur.</summary>
    public static string Hex(string? belirtec)
    {
        switch (belirtec)
        {
            case Gelir: return "#10b981";
            case Gider: return "#ef4444";
            case Kar: return "#2563eb";
            case Arac: return "#f59e0b";
            case Personel: return "#8b5cf6";
            case Hakedis: return "#06b6d4";
            case Butce: return "#6366f1";
            case Uyari: return "#f97316";
            case Notr: return "#94a3b8";
        }

        if (belirtec is { Length: > 1 } && belirtec[0] == 'f' && int.TryParse(belirtec.AsSpan(1), NumberStyles.None, CultureInfo.InvariantCulture, out var sira))
            return FirmaPaleti[sira % FirmaPaleti.Length];

        return "#94a3b8";
    }
}

/// <summary>Holding ekranlarının ortak metin/sayı biçimleri. Kullanıcıya görünen sayılar tr-TR; SVG/CSS sayıları sabit kültürdür.</summary>
public static class HoldingBicim
{
    private static readonly CultureInfo Tr = new("tr-TR");

    public static readonly IReadOnlyList<string> AyAdlari =
        ["Ocak", "Şubat", "Mart", "Nisan", "Mayıs", "Haziran", "Temmuz", "Ağustos", "Eylül", "Ekim", "Kasım", "Aralık"];

    public static readonly IReadOnlyList<string> AyKisaltmalari =
        ["Oca", "Şub", "Mar", "Nis", "May", "Haz", "Tem", "Ağu", "Eyl", "Eki", "Kas", "Ara"];

    public static string AyAdi(int ay) => ay is >= 1 and <= 12 ? AyAdlari[ay - 1] : "Tüm Yıl";

    public static string AyKisa(int ay) => ay is >= 1 and <= 12 ? AyKisaltmalari[ay - 1] : "Yıl";

    /// <summary>Ay listesini okunur yazar: ardışıksa "Ocak–Mart", değilse "Ocak, Mart, Mayıs".</summary>
    public static string AyListesi(IReadOnlyList<int> aylar)
    {
        var sirali = aylar.Where(a => a is >= 1 and <= 12).Distinct().OrderBy(a => a).ToList();
        if (sirali.Count == 0)
            return string.Empty;
        if (sirali.Count == 1)
            return AyAdi(sirali[0]);

        var ardisik = sirali[^1] - sirali[0] == sirali.Count - 1;
        return ardisik && sirali.Count > 2
            ? $"{AyAdi(sirali[0])}–{AyAdi(sirali[^1])}"
            : string.Join(", ", sirali.Select(AyAdi));
    }

    /// <summary>"Nisan 2026" ya da (ay = 0) "2026 yılı".</summary>
    public static string Donem(int yil, int ay) => ay is >= 1 and <= 12 ? $"{AyAdlari[ay - 1]} {yil}" : $"{yil} yılı";

    /// <summary>Kartlara sığan kısa tutar: "13,17 Mn TL", "250.000 TL".</summary>
    public static string ParaKisa(decimal tutar) => YoneticiOzetMetni.ParaKisalt(tutar);

    /// <summary>Tablolarda tam tutar (kuruşsuz): "13.166.594 TL".</summary>
    public static string Para(decimal tutar) => ParaFormat.FormatKisa(tutar);

    /// <summary>Fare ipucunda kuruşlu tutar: "13.166.594,37 TL".</summary>
    public static string ParaTam(decimal tutar) => ParaFormat.Format(tutar);

    /// <summary>"%43,3"; negatifte eksi işareti yüzdenin önünde ("-%4,4"). Yuvarlayınca sıfır olan değer eksi almaz.</summary>
    public static string Yuzde(decimal deger, int basamak = 1)
    {
        var yuvarlak = Math.Round(deger, basamak, MidpointRounding.AwayFromZero);
        return (yuvarlak < 0m ? "-" : string.Empty) + "%" + Math.Abs(yuvarlak).ToString("N" + basamak, Tr);
    }

    public static string Sayi(decimal deger) => YoneticiOzetMetni.Sayi(deger);

    /// <summary>Eksen etiketi: "250 Bin", "1,2 Mn".</summary>
    public static string Eksen(decimal deger) => GrafikOlcegi.EksenMetni(deger);

    /// <summary>CSS/SVG için sabit kültürlü sayı ("12.5").</summary>
    public static string Css(decimal deger) => deger.ToString("0.##", CultureInfo.InvariantCulture);

    /// <summary>UTC bir anı iş günü saatine çevirip "21 Eki 2026 14:20" biçiminde yazar.</summary>
    public static string Zaman(DateTime utc)
    {
        var yerel = MKFiloServis.Shared.Time.BusinessTime.LocalAt(utc);
        return $"{yerel.Day} {AyKisa(yerel.Month)} {yerel.Year} {yerel:HH:mm}";
    }
}
