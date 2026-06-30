namespace MKFiloServis.Web.Helpers;

/// <summary>
/// Para birimi ve sayı formatlama yardımcı sınıfı
/// </summary>
public static class ParaFormat
{
    private const string ParaBirimi = "TL";
    private const string ParaBirimiKodu = "TL";
    private static readonly System.Globalization.CultureInfo TrCulture = new("tr-TR");

    /// <summary>
    /// Decimal değeri para formatında döner (1.234,56 TL)
    /// </summary>
    public static string Format(decimal tutar, bool birimGoster = true)
    {
        var formatli = tutar.ToString("N2", TrCulture);
        return birimGoster ? $"{formatli} {ParaBirimi}" : formatli;
    }

    /// <summary>
    /// Decimal değeri kısa para formatında döner (1.234 TL)
    /// </summary>
    public static string FormatKisa(decimal tutar, bool birimGoster = true)
    {
        var formatli = tutar.ToString("N0", TrCulture);
        return birimGoster ? $"{formatli} {ParaBirimi}" : formatli;
    }

    /// <summary>
    /// Decimal değeri TL koduyla döner (1.234,56 TL)
    /// </summary>
    public static string FormatTL(decimal tutar)
    {
        var formatli = tutar.ToString("N2", TrCulture);
        return $"{formatli} {ParaBirimiKodu}";
    }

    /// <summary>
    /// Decimal değeri kısa TL koduyla döner (1.234 TL)
    /// </summary>
    public static string FormatTLKisa(decimal tutar)
    {
        var formatli = tutar.ToString("N0", TrCulture);
        return $"{formatli} {ParaBirimiKodu}";
    }

    /// <summary>
    /// Nullable decimal değeri formatlar
    /// </summary>
    public static string Format(decimal? tutar, bool birimGoster = true, string bostaDeger = "-")
    {
        if (!tutar.HasValue) return bostaDeger;
        return Format(tutar.Value, birimGoster);
    }
}


