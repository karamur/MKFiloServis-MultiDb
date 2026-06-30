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
    public decimal Odenecek { get; set; }
}



