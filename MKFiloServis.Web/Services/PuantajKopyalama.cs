using MKFiloServis.Shared.Entities;

namespace MKFiloServis.Web.Services;

public enum PuantajKopyaHizalama
{
    /// <summary>Haftanın günleri korunur: önceki ayın salısı bu ayın salısına gider (haftalık çalışma düzeni).</summary>
    HaftaninGunu = 0,

    /// <summary>Ayın gün numarası korunur: önceki ayın 5'i bu ayın 5'ine gider.</summary>
    AyinGunu = 1
}

public sealed record PuantajKopyaSecenekleri(
    PuantajKopyaHizalama Hizalama = PuantajKopyaHizalama.HaftaninGunu,
    bool IzinMazeretKopyala = false,
    bool FazlaMesaiKopyala = true,
    bool ResmiTatilleriAtla = true);

/// <summary>Kopyalamanın kaynağı olan ay: günlük kayıtlar ve o aydaki resmi tatiller.</summary>
public sealed record PuantajKaynakAy(int Yil, int Ay, IReadOnlyList<GunlukPuantaj> Gunler, IReadOnlyCollection<DateTime> ResmiTatiller);

/// <summary>Geçen aydan kopyala penceresinde bir personelin kaynak ve hedef durumu.</summary>
public sealed record PuantajKopyaAday(
    Sofor Personel,
    int? MevcutPuantajId,
    bool Onayli,
    IReadOnlyList<GunlukPuantaj> MevcutGunler,
    PuantajKaynakAy? Kaynak)
{
    public bool MevcutDolu => MevcutGunler.Any(g => g.Calisti || g.Izinli || g.Mazeret);
}

/// <summary>Kopyalama sonucu: kaydedilecek günlük kayıtlar (hedef ayın bütün günleri).</summary>
public sealed record PuantajKopyaSonucu(PuantajKopyaAday Aday, IReadOnlyList<GunlukPuantaj> YeniGunler);

/// <summary>
/// Önceki ayın puantajını yeni aya taşır. Yalnız "çalıştı" günleri (saat ve fazla mesaiyle) taşınır; izin, mazeret ve resmi tatil
/// gibi o aya özgü istisnalar varsayılan olarak kopyalanmaz: istisna olan kaynak gün yerine aynı haftanın gününün başka bir haftadaki
/// normal günü esas alınır. Saf mantıktır, veritabanına dokunmaz.
/// </summary>
public static class PuantajKopyalama
{
    /// <summary>Hedef ayın bir gününe karşılık gelen kaynak ay günü; yoksa null.</summary>
    public static int? KaynakGun(int hedefYil, int hedefAy, int hedefGun, int kaynakYil, int kaynakAy, PuantajKopyaHizalama hizalama)
    {
        var kaynakGunSayisi = DateTime.DaysInMonth(kaynakYil, kaynakAy);

        if (hizalama == PuantajKopyaHizalama.AyinGunu)
            return hedefGun <= kaynakGunSayisi ? hedefGun : null;

        // Haftanın günü korunur: iki ayın 1'i arasındaki hafta günü farkı kadar kaydırılır, en yakın eşleşme seçilir.
        var kaynakIlk = HaftaIndeksi(new DateTime(kaynakYil, kaynakAy, 1).DayOfWeek);
        var hedefIlk = HaftaIndeksi(new DateTime(hedefYil, hedefAy, 1).DayOfWeek);
        var kaydirma = ((hedefIlk - kaynakIlk) % 7 + 7) % 7;
        if (kaydirma > 3)
            kaydirma -= 7;

        var gun = hedefGun + kaydirma;
        while (gun < 1) gun += 7;
        while (gun > kaynakGunSayisi) gun -= 7;
        return gun;
    }

    public static List<GunlukPuantaj> Uret(
        int hedefYil,
        int hedefAy,
        PuantajKaynakAy kaynak,
        PuantajKopyaSecenekleri secenek,
        IReadOnlyCollection<DateTime>? hedefResmiTatiller = null)
    {
        var hedefGunSayisi = DateTime.DaysInMonth(hedefYil, hedefAy);
        var kaynakGunSayisi = DateTime.DaysInMonth(kaynak.Yil, kaynak.Ay);
        var satirlar = kaynak.Gunler
            .Where(g => g.Tarih.Year == kaynak.Yil && g.Tarih.Month == kaynak.Ay)
            .GroupBy(g => g.Tarih.Day)
            .ToDictionary(g => g.Key, g => g.First());
        var kaynakTatilleri = kaynak.ResmiTatiller
            .Where(t => t.Year == kaynak.Yil && t.Month == kaynak.Ay)
            .Select(t => t.Day)
            .ToHashSet();

        var sonuc = new List<GunlukPuantaj>(hedefGunSayisi);
        for (var gun = 1; gun <= hedefGunSayisi; gun++)
        {
            var tarih = new DateTime(hedefYil, hedefAy, gun);
            var hedef = new GunlukPuantaj { Gun = gun, Tarih = tarih };
            sonuc.Add(hedef);

            if (secenek.ResmiTatilleriAtla && hedefResmiTatiller?.Any(t => t.Date == tarih) == true)
                continue;

            var aday = KaynakGun(hedefYil, hedefAy, gun, kaynak.Yil, kaynak.Ay, secenek.Hizalama);
            if (aday is null)
                continue;

            var secilen = Sec(aday.Value, kaynakGunSayisi, satirlar, kaynakTatilleri, secenek);
            if (secilen is null)
                continue;

            var (tur, satir) = secilen.Value;
            switch (tur)
            {
                case KaynakGunTuru.Calisti:
                    hedef.Calisti = true;
                    hedef.Durum = 1;
                    hedef.CalismaSaati = satir!.CalismaSaati;
                    hedef.FazlaMesaiSaat = secenek.FazlaMesaiKopyala ? satir.FazlaMesaiSaat : 0;
                    break;
                case KaynakGunTuru.Izinli:
                    hedef.Izinli = true;
                    hedef.Durum = 2;
                    break;
                case KaynakGunTuru.Mazeret:
                    hedef.Mazeret = true;
                    hedef.Durum = 3;
                    break;
            }
        }

        return sonuc;
    }

    private enum KaynakGunTuru { Bos, Calisti, Izinli, Mazeret, IstisnaBos }

    private static (KaynakGunTuru Tur, GunlukPuantaj? Satir)? Sec(
        int gun,
        int kaynakGunSayisi,
        IReadOnlyDictionary<int, GunlukPuantaj> satirlar,
        IReadOnlySet<int> tatiller,
        PuantajKopyaSecenekleri secenek)
    {
        var (tur, satir) = Siniflandir(gun, satirlar, tatiller);

        switch (tur)
        {
            case KaynakGunTuru.Calisti:
            case KaynakGunTuru.Bos:
                return (tur, satir);

            case KaynakGunTuru.Izinli:
            case KaynakGunTuru.Mazeret:
                if (secenek.IzinMazeretKopyala)
                    return (tur, satir);
                break;
        }

        // Kaynak gün o aya özgü bir istisna (izin, mazeret, resmi tatil): aynı haftanın gününün başka bir haftadaki normal günü esas alınır.
        if (secenek.Hizalama != PuantajKopyaHizalama.HaftaninGunu)
            return null;

        for (var adim = 7; adim <= 28; adim += 7)
        {
            foreach (var alternatif in new[] { gun + adim, gun - adim })
            {
                if (alternatif < 1 || alternatif > kaynakGunSayisi)
                    continue;

                var (altTur, altSatir) = Siniflandir(alternatif, satirlar, tatiller);
                if (altTur is KaynakGunTuru.Calisti or KaynakGunTuru.Bos)
                    return (altTur, altSatir);
            }
        }

        return null;
    }

    private static (KaynakGunTuru Tur, GunlukPuantaj? Satir) Siniflandir(
        int gun, IReadOnlyDictionary<int, GunlukPuantaj> satirlar, IReadOnlySet<int> tatiller)
    {
        satirlar.TryGetValue(gun, out var satir);

        if (satir is { Calisti: true })
            return (KaynakGunTuru.Calisti, satir);
        if (satir is { Izinli: true })
            return (KaynakGunTuru.Izinli, satir);
        if (satir is { Mazeret: true })
            return (KaynakGunTuru.Mazeret, satir);

        return tatiller.Contains(gun)
            ? (KaynakGunTuru.IstisnaBos, satir)
            : (KaynakGunTuru.Bos, satir);
    }

    /// <summary>Pazartesi = 0 ... Pazar = 6.</summary>
    private static int HaftaIndeksi(DayOfWeek gun) => ((int)gun + 6) % 7;
}
