using MKFiloServis.Shared.Entities;

namespace MKFiloServis.Web.Services;

/// <summary>Saatlik ücretli personelin aylık tutarının, fiilen çalışılan saatlerden hesabı.</summary>
public sealed record PuantajUcretSonucu(bool Hesaplandi, decimal ToplamSaat, decimal BirimUcret, decimal Brut, decimal? Net)
{
    /// <summary>Saatlik ücretli olmayan ya da birim ücreti girilmemiş personel: tutar puantajdan hesaplanmaz.</summary>
    public static readonly PuantajUcretSonucu Hesaplanmadi = new(false, 0, 0, 0, null);
}

/// <summary>
/// Saatlik ücretli personelin puantaj tutarı: brüt = toplam çalışma saati (normal + fazla mesai) × saatlik ücret.
/// Net, personel kartındaki net maaşın "puantaj brütü / kart brütü" oranıyla ölçeklenir (kart brütü = kartta yazan aylık saat × saatlik ücret);
/// kartta aylık saat ya da net maaş yoksa net hesaplanamaz ve değişmez. Aylık, günlük ve manuel ücretli personel etkilenmez.
/// </summary>
public static class PuantajUcretHesabi
{
    public static PuantajUcretSonucu Hesapla(Sofor? personel, IEnumerable<GunlukPuantaj> gunler)
    {
        if (personel is null || personel.BrutMaasHesaplamaTipi != BrutMaasHesaplamaTipi.Saatlik || personel.BirimUcret <= 0)
            return PuantajUcretSonucu.Hesaplanmadi;

        var saat = PuantajKartKontrolu.ToplamSaat(gunler);
        var brut = Yuvarla(saat * personel.BirimUcret);
        var kartBrut = Yuvarla(personel.CalismaMiktari * personel.BirimUcret);
        decimal? net = kartBrut > 0 && personel.NetMaas > 0
            ? Yuvarla(personel.NetMaas * brut / kartBrut)
            : null;

        return new PuantajUcretSonucu(true, saat, personel.BirimUcret, brut, net);
    }

    private static decimal Yuvarla(decimal deger) => Math.Round(deger, 2, MidpointRounding.AwayFromZero);
}

/// <summary>Günlük kayıtlardan aylık puantaj özetini (gün sayıları, fazla mesai, saatlik tutar) günceller.</summary>
public static class PuantajOzet
{
    public static void Uygula(PersonelPuantaj puantaj, Sofor? personel, IReadOnlyCollection<GunlukPuantaj> gunler)
    {
        puantaj.CalisilanGun = gunler.Count(g => g.Calisti);
        puantaj.IzinGunu = gunler.Count(g => g.Izinli);
        puantaj.MazeretGunu = gunler.Count(g => g.Mazeret);
        puantaj.FazlaMesaiSaat = gunler.Sum(g => g.FazlaMesaiSaat);

        // Hiç günlük kayıt yoksa tutar bozulmasın (kart değerinden gelen ilk tutar kalır).
        if (gunler.Count == 0)
            return;

        var ucret = PuantajUcretHesabi.Hesapla(personel, gunler);
        if (!ucret.Hesaplandi)
            return;

        puantaj.BrutMaas = ucret.Brut;
        if (ucret.Net is { } net)
            puantaj.NetOdeme = net;
    }
}
