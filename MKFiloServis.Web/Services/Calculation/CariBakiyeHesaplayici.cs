using MKFiloServis.Shared.Entities;

namespace MKFiloServis.Web.Services.Calculation;

/// <summary>
/// Cari listesindeki Borç / Alacak hesabı. Bakiye = Alacak − Borç; pozitif değer carinin bize borcunu,
/// negatif değer bizim cariye borcumuzu gösterir.
/// Faturanın yönü hangi cari tipine bağlı olursa olsun hesaba girer: kesilen (giden) fatura ve cariye yapılan ödeme Alacak,
/// alınan (gelen) fatura ve cariden yapılan tahsilat Borç sütununa yazılır. Böylece müşteri olarak açılmış bir cariden
/// alınan fatura (ör. araç alımı) ya da tedarikçi carisine kesilen fatura Cari Listesi'nde de görünür ve
/// Borç/Alacak Takip ile aynı sonucu verir.
/// </summary>
public static class CariBakiyeHesaplayici
{
    public static (decimal Borc, decimal Alacak) Hesapla(
        CariTipi tip,
        decimal gidenFatura,
        decimal gelenFatura,
        decimal cikisOdemeleri,
        decimal girisTahsilatlari)
    {
        // Personel carisinde yalnız alınan belgeler (borç) ve yapılan ödemeler izlenir.
        if (tip == CariTipi.Personel)
            return (gelenFatura, cikisOdemeleri);

        return (gelenFatura + girisTahsilatlari, gidenFatura + cikisOdemeleri);
    }

    public static void Uygula(Cari cari, decimal gidenFatura, decimal gelenFatura, decimal cikisOdemeleri, decimal girisTahsilatlari)
        => (cari.Borc, cari.Alacak) = Hesapla(cari.CariTipi, gidenFatura, gelenFatura, cikisOdemeleri, girisTahsilatlari);
}
