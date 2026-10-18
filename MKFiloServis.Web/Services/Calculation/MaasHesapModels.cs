namespace MKFiloServis.Web.Services.Calculation;

/// <summary>
/// Maaş / ödeme hesaplama motoru için giriş parametreleri.
/// </summary>
public class MaasInput
{
    public decimal? GercekMaas { get; set; }
    public decimal? BankayaYatan { get; set; }
    public decimal? Avans { get; set; }
    public decimal? Kesinti { get; set; }
    public decimal? Harcama { get; set; }

    /// <summary>
    /// Önceki aylardan devreden net bakiye (pozitif: personelin alacağı, negatif: personelin borcu).
    /// Boş bırakılırsa 0 kabul edilir; mevcut çağıranların sonucu değişmez.
    /// </summary>
    public decimal? GecenAydanKalan { get; set; }
}

/// <summary>
/// Maaş / ödeme hesaplama motoru çıktısı.
/// </summary>
public class MaasHesapSonuc
{
    public decimal GercekMaas { get; set; }
    public decimal BankayaYatan { get; set; }
    public decimal Avans { get; set; }
    public decimal Kesinti { get; set; }
    public decimal Harcama { get; set; }
    public decimal GecenAydanKalan { get; set; }

    /// <summary>Geçen aydan kalan dahil, personele ödenecek toplam tutar.</summary>
    public decimal Odenecek { get; set; }

    /// <summary>Yalnızca o aya ait tutar (geçen aydan kalan hariç). Muhasebe tahakkuku bu tutarla çalışır.</summary>
    public decimal BuAyOdenecek { get; set; }
}



