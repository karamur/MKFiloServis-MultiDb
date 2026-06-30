namespace MKFiloServis.Web.Models;

/// <summary>
/// Dashboard grafikleri için aylık gelir/gider verisi
/// </summary>
public class AylikGelirGiderVeri
{
    public int Yil { get; set; }
    public int Ay { get; set; }
    public string AyAdi { get; set; } = string.Empty;
    public decimal Gelir { get; set; }
    public decimal Gider { get; set; }
    public decimal Net => Gelir - Gider;
}

/// <summary>
/// Dashboard grafikleri için cari tip dağılımı
/// </summary>
public class CariTipDagilimi
{
    public string TipAdi { get; set; } = string.Empty;
    public int Adet { get; set; }
    public decimal ToplamBakiye { get; set; }
}

/// <summary>
/// Dashboard grafikleri için masraf kategori dağılımı
/// </summary>
public class MasrafKategoriDagilimi
{
    public string KategoriAdi { get; set; } = string.Empty;
    public decimal Tutar { get; set; }
    public int Adet { get; set; }
}

/// <summary>
/// Dashboard grafikleri için aylık bütçe özeti
/// </summary>
public class AylikButceVeri
{
    public int Yil { get; set; }
    public int Ay { get; set; }
    public string AyAdi { get; set; } = string.Empty;
    public decimal PlanlananOdeme { get; set; }
    public decimal GerceklesenOdeme { get; set; }
}

/// <summary>
/// Dashboard tüm grafik verilerini içeren özet
/// </summary>
public class DashboardChartData
{
    public List<AylikGelirGiderVeri> AylikGelirGider { get; set; } = [];
    public List<CariTipDagilimi> CariDagilimi { get; set; } = [];
    public List<MasrafKategoriDagilimi> MasrafDagilimi { get; set; } = [];
    public List<AylikButceVeri> AylikButce { get; set; } = [];
}



