using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Web.Services;

/// <summary>
/// Sütun/çizgi grafiği için "güzel" dikey ölçek: negatif değerleri de kapsar (kâr/zarar), yuvarlak adımlıdır
/// (1, 2, 2,5, 5 × 10ⁿ) ve en çok <see cref="AzamiAralik"/> eşit aralığa bölünür. Yalnız pozitif değerlerde
/// <see cref="GrafikOlcegi"/> ile aynı sonucu verir.
/// </summary>
public sealed class HoldingOlcek
{
    private static readonly decimal[] Carpanlar = [1m, 2m, 2.5m, 5m];
    private const int AzamiAralik = 5;

    private HoldingOlcek(decimal alt, decimal ust, decimal adim, IReadOnlyList<decimal> isaretler)
    {
        Alt = alt;
        Ust = ust;
        Adim = adim;
        Isaretler = isaretler;
    }

    public decimal Alt { get; }
    public decimal Ust { get; }
    public decimal Adim { get; }

    /// <summary>Alt sınırdan üst sınıra eşit aralıklı eksen değerleri (sıfır dahil).</summary>
    public IReadOnlyList<decimal> Isaretler { get; }

    public decimal Aralik => Ust - Alt;

    /// <summary>Değerin dikeydeki yeri: 0 (alt sınır) – 100 (üst sınır).</summary>
    public decimal Konum(decimal deger) => Aralik <= 0m ? 0m : (deger - Alt) / Aralik * 100m;

    /// <summary>Sıfır çizgisinin dikeydeki yeri (yüzde).</summary>
    public decimal Sifir => Konum(0m);

    public static HoldingOlcek Hesapla(decimal enKucuk, decimal enBuyuk, AylikBirim birim)
    {
        var alt = Math.Min(0m, enKucuk);
        var ust = Math.Max(0m, enBuyuk);
        if (alt == 0m && ust == 0m)
            return Olustur(0m, 1m, 1m);

        var enKucukAdim = birim == AylikBirim.Adet ? 1m : 0.01m;
        for (var taban = 0.01m; taban <= 1_000_000_000_000_000m; taban *= 10m)
        {
            foreach (var carpan in Carpanlar)
            {
                var adim = taban * carpan;
                if (adim < enKucukAdim || (birim == AylikBirim.Adet && adim != Math.Truncate(adim)))
                    continue;

                // Küçük adımlarda oran int'e sığmayabilir; sınama decimal olarak yapılır.
                var ustAralik = Math.Ceiling(ust / adim);
                var altAralik = Math.Ceiling(-alt / adim);
                if (ustAralik + altAralik <= AzamiAralik)
                    return Olustur(-altAralik * adim, Math.Max(ustAralik, alt < 0m ? 0m : 1m) * adim, adim);
            }
        }

        // Aşırı büyük değerler: tek aralık.
        return Olustur(alt, ust, Math.Max(ust - alt, 1m));
    }

    private static HoldingOlcek Olustur(decimal alt, decimal ust, decimal adim)
    {
        var isaretler = new List<decimal>();
        if (adim > 0m)
        {
            for (var d = alt; d <= ust + adim / 1000m; d += adim)
                isaretler.Add(d);
        }

        if (isaretler.Count < 2)
        {
            isaretler.Clear();
            isaretler.Add(alt);
            isaretler.Add(ust);
        }

        return new HoldingOlcek(alt, ust, adim, isaretler);
    }
}
