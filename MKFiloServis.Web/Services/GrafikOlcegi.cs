using System.Globalization;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Web.Services;

/// <summary>
/// Sütun grafiği için "güzel" dikey ölçek: en yüksek değeri aşan, 2–5 eşit aralıklı, yuvarlak adımlı üst sınır
/// (1, 2, 2,5, 5 × 10ⁿ). Adet ölçeğinde adım en az 1'dir ("0,5 sefer" gibi eksen çıkmaz).
/// </summary>
public readonly record struct GrafikOlcegi(decimal Ust, decimal Adim, int AralikSayisi)
{
    private static readonly CultureInfo Tr = new("tr-TR");
    private static readonly decimal[] Carpanlar = [1m, 2m, 2.5m, 5m];
    private const int AzamiAralik = 5;

    /// <summary>0, adım, 2×adım, … üst sınır.</summary>
    public IEnumerable<decimal> Isaretler
    {
        get
        {
            var adim = Adim;
            return Enumerable.Range(0, AralikSayisi + 1).Select(i => i * adim);
        }
    }

    public static GrafikOlcegi Hesapla(decimal azami, AylikBirim birim)
    {
        if (azami <= 0)
            return new GrafikOlcegi(1m, 1m, 1);

        var enKucukAdim = birim == AylikBirim.Adet ? 1m : 0.01m;

        for (var taban = 0.01m; taban <= 1_000_000_000_000_000m; taban *= 10m)
        {
            foreach (var carpan in Carpanlar)
            {
                var adim = taban * carpan;
                if (adim < enKucukAdim || (birim == AylikBirim.Adet && adim != Math.Truncate(adim)))
                    continue;

                // Küçük adımlarda oran int'e sığmayabilir (100 milyon / 0,01); önce decimal olarak sınanır.
                var aralik = Math.Ceiling(azami / adim);
                if (aralik <= AzamiAralik)
                {
                    var sayi = (int)Math.Max(aralik, 1m);
                    return new GrafikOlcegi(sayi * adim, adim, sayi);
                }
            }
        }

        // Aşırı büyük değerler: tek aralık.
        return new GrafikOlcegi(azami, azami, 1);
    }

    /// <summary>Eksen etiketi: kısa ve para birimsiz ("250 Bin", "1,2 Mn", "3 Mr").</summary>
    public static string EksenMetni(decimal deger)
    {
        var mutlak = Math.Abs(deger);
        if (mutlak >= 1_000_000_000m)
            return $"{(deger / 1_000_000_000m).ToString("0.##", Tr)} Mr";
        if (mutlak >= 1_000_000m)
            return $"{(deger / 1_000_000m).ToString("0.##", Tr)} Mn";
        if (mutlak >= 1_000m)
            return $"{(deger / 1_000m).ToString("0.##", Tr)} Bin";
        return deger.ToString("0.##", Tr);
    }
}
