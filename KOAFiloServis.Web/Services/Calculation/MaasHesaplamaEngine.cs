namespace KOAFiloServis.Web.Services.Calculation;

/// <summary>
/// TEK MERKEZLİ maaş / ödeme hesaplama motoru.
/// Tüm maaş, puantaj ve banka ödeme hesaplamaları buradan yapılır.
/// UI sadece GÖSTERİM yapar — hesaplama yapmaz.
/// </summary>
public static class MaasHesaplamaEngine
{
    /// <summary>
    /// Elden ödenecek tutarı hesaplar.
    /// Formül: GercekMaas - BankayaYatan - Avans - Kesinti + Harcama
    /// </summary>
    public static MaasHesapSonuc Hesapla(MaasInput input)
    {
        var gercekMaas = input.GercekMaas ?? 0;
        var bankayaYatan = input.BankayaYatan ?? 0;
        var avans = input.Avans ?? 0;
        var kesinti = input.Kesinti ?? 0;
        var harcama = input.Harcama ?? 0;

        var odenecek =
            gercekMaas
            - bankayaYatan
            - avans
            - kesinti
            + harcama;

        return new MaasHesapSonuc
        {
            GercekMaas = gercekMaas,
            BankayaYatan = bankayaYatan,
            Avans = avans,
            Kesinti = kesinti,
            Harcama = harcama,
            Odenecek = odenecek
        };
    }

    /// <summary>
    /// Birden çok satır için toplu hesaplama.
    /// </summary>
    public static List<MaasHesapSonuc> Hesapla(IEnumerable<MaasInput> inputs)
    {
        return inputs.Select(Hesapla).ToList();
    }
}
