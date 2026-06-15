namespace KOAFiloServis.Web.Services.Calculation;

/// <summary>
/// Puantaj hesaplama motoru — Excel ile %100 uyumlu.
/// GÜN kolonları: S=1, A=1, S-A=2. Toplam Sefer = gün toplamı.
/// Toplam = Sefer × BirimFiyat. Net = Toplam - Kesinti.
/// </summary>
public static class PuantajEngine
{
    /// <summary>S-A veya A-S pattern'ini tespit eder.</summary>
    public static bool IsGidisDonus(string yon) =>
        !string.IsNullOrWhiteSpace(yon) && yon.Trim().ToUpperInvariant() is "S-A" or "A-S" or "S+A" or "GİDİŞ-DÖNÜŞ";

    /// <summary>S veya A tek yön tespiti.</summary>
    public static bool IsTekYon(string yon) =>
        !string.IsNullOrWhiteSpace(yon) && yon.Trim().ToUpperInvariant() is "S" or "A" or "GİDİŞ" or "DÖNÜŞ";

    /// <summary>Gün hücresini sefer sayısına çevirir.</summary>
    public static int GunToSefer(string cellValue)
    {
        if (string.IsNullOrWhiteSpace(cellValue)) return 0;
        var v = cellValue.Trim().ToUpperInvariant();
        if (v is "1" or "S" or "A" or "X" or "✓") return 1;
        if (v is "S-A" or "A-S" or "2") return 2;
        if (int.TryParse(v, out var n) && n > 0) return n;
        return 0;
    }

    /// <summary>
    /// Excel puantaj satırı hesaplar.
    /// </summary>
    public static PuantajSonuc Hesapla(PuantajInput input)
    {
        int sefer = 0;
        if (input.Gunler != null)
        {
            foreach (var g in input.Gunler)
                sefer += GunToSefer(g);
        }

        var birimFiyat = input.BirimFiyat;

        // S-A (gidiş-dönüş) ise birim fiyat 2'ye bölünür (her yön için ayrı)
        if (IsGidisDonus(input.Yon))
            birimFiyat = birimFiyat / 2;

        var toplam = sefer * birimFiyat;
        var net = toplam - input.Kesinti;

        return new PuantajSonuc
        {
            Sefer = sefer,
            BirimFiyat = birimFiyat,
            Toplam = toplam,
            Kesinti = input.Kesinti,
            Net = net
        };
    }

    /// <summary>Toplu hesaplama.</summary>
    public static List<PuantajSonuc> Hesapla(IEnumerable<PuantajInput> inputs)
        => inputs.Select(Hesapla).ToList();
}

public class PuantajInput
{
    public string? Kurum { get; set; }
    public string? Guzergah { get; set; }
    public string? Yon { get; set; }
    public string? Sofor { get; set; }
    public string? Plaka { get; set; }
    public decimal BirimFiyat { get; set; }
    public decimal Kesinti { get; set; }
    public List<string> Gunler { get; set; } = new(); // 1-31 gün değerleri ("S", "A", "S-A", "1", "0", "")
}

public class PuantajSonuc
{
    public int Sefer { get; set; }
    public decimal BirimFiyat { get; set; }
    public decimal Toplam { get; set; }
    public decimal Kesinti { get; set; }
    public decimal Net { get; set; }
}
