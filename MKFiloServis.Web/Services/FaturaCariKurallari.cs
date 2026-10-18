using MKFiloServis.Shared.Entities;

namespace MKFiloServis.Web.Services;

/// <summary>Faturalardan cariye yansıyan kurallar (veritabanından bağımsız).</summary>
public static class FaturaCariKurallari
{
    /// <summary>
    /// Carinin faturalarının yönüne göre olması gereken tipi verir. Tip yalnızca genişletilir, hiçbir zaman daraltılmaz:
    /// kesilen fatura müşteri, alınan fatura tedarikçi rolü ekler; iki yön birlikte varsa Müşteri+Tedarikçi olur.
    /// Personel carisi ve faturası olmayan cari değişmez.
    /// </summary>
    public static CariTipi GerekenTip(CariTipi mevcut, bool gidenFaturaVar, bool gelenFaturaVar)
    {
        if (mevcut == CariTipi.Personel)
            return mevcut;

        var roller = mevcut switch
        {
            CariTipi.Musteri => 1,
            CariTipi.Tedarikci => 2,
            CariTipi.MusteriTedarikci => 3,
            _ => 0 // eski kayıtlarda tip belirsiz (0) olabilir; fatura yönüne göre belirlenir
        };

        if (gidenFaturaVar) roller |= 1;
        if (gelenFaturaVar) roller |= 2;

        return roller switch
        {
            1 => CariTipi.Musteri,
            2 => CariTipi.Tedarikci,
            3 => CariTipi.MusteriTedarikci,
            _ => mevcut
        };
    }

    public static string TipAdi(CariTipi tip) => tip switch
    {
        CariTipi.Musteri => "Müşteri",
        CariTipi.Tedarikci => "Tedarikçi",
        CariTipi.MusteriTedarikci => "Müşteri + Tedarikçi",
        CariTipi.Personel => "Personel",
        _ => "Belirsiz"
    };
}
