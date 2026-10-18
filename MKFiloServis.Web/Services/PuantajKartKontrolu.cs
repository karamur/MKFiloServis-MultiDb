using System.Globalization;
using MKFiloServis.Shared.Entities;

namespace MKFiloServis.Web.Services;

public enum KartKontrolDurumu
{
    /// <summary>Puantaj, karttaki saat/gün ile aynı.</summary>
    Uyumlu,

    /// <summary>Puantajdaki değer karttakinden az.</summary>
    Az,

    /// <summary>Puantajdaki değer karttakinden fazla.</summary>
    Fazla,

    /// <summary>Kartta karşılaştırılacak saat/gün yok (Manuel/Aylık ücret ya da değer girilmemiş).</summary>
    Tanimsiz
}

public enum KartKontrolBirimi
{
    Yok,
    Saat,
    Gun
}

/// <summary>Bir personelin puantajının, personel kartında yazılan saat/gün ile karşılaştırması.</summary>
public sealed record KartKontrolSonucu(
    BrutMaasHesaplamaTipi Tip,
    KartKontrolBirimi Birim,
    decimal KartDegeri,
    decimal PuantajDegeri,
    decimal FazlaMesai,
    KartKontrolDurumu Durum)
{
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

    public decimal Fark => PuantajDegeri - KartDegeri;
    public bool Uyarili => Durum is KartKontrolDurumu.Az or KartKontrolDurumu.Fazla;
    public string BirimAdi => Birim switch { KartKontrolBirimi.Saat => "saat", KartKontrolBirimi.Gun => "gün", _ => "" };
    public string BirimKisa => Birim switch { KartKontrolBirimi.Saat => "s", KartKontrolBirimi.Gun => "g", _ => "" };

    /// <summary>Tablo hücresi için kısa metin: "165 / 195 s".</summary>
    public string Ozet => Birim == KartKontrolBirimi.Yok
        ? "-"
        : $"{Sayi(PuantajDegeri)} / {Sayi(KartDegeri)} {BirimKisa}";

    /// <summary>Kullanıcıya gösterilecek açıklayıcı cümle.</summary>
    public string Mesaj => Durum switch
    {
        KartKontrolDurumu.Uyumlu => $"Kart ile uyumlu: {Sayi(PuantajDegeri)} {BirimAdi}.",
        KartKontrolDurumu.Az => $"Kartta {Sayi(KartDegeri)} {BirimAdi} yazıyor, puantajda {Sayi(PuantajDegeri)} {BirimAdi} var ({Sayi(-Fark)} {BirimAdi} eksik).",
        KartKontrolDurumu.Fazla => $"Kartta {Sayi(KartDegeri)} {BirimAdi} yazıyor, puantajda {Sayi(PuantajDegeri)} {BirimAdi} var ({Sayi(Fark)} {BirimAdi} fazla"
            + (FazlaMesai > 0 ? $"; {Sayi(FazlaMesai)} saati fazla mesai" : "") + ").",
        _ => Tip switch
        {
            BrutMaasHesaplamaTipi.Manuel => "Ücret türü Manuel: kartta saat/gün tanımı yok, kontrol yapılmadı.",
            BrutMaasHesaplamaTipi.Aylik => "Aylık ücretli personelin kartında saat/gün tanımı yok, kontrol yapılmadı.",
            _ => "Kartta çalışılan saat/gün girilmemiş, kontrol yapılmadı."
        }
    };

    private static string Sayi(decimal deger) => deger.ToString("0.##", Tr);
}

/// <summary>Uyarı penceresinde listelenen satır.</summary>
public sealed record KartUyariSatiri(string Personel, KartKontrolSonucu Sonuc);

/// <summary>
/// Puantajdaki saat/günü personel kartında yazılan değerle karşılaştırır.
/// Kart değeri, personel kartındaki "Çalışılan Saat" (Saatlik ücret) veya "Çalışılan Gün" (Günlük ücret) alanıdır;
/// brüt ücret = bu miktar × birim ücret olduğundan aylık toplamı ifade eder. Aylık ve Manuel ücretli personelin
/// kartında saat/gün tutulmadığı için bunlar karşılaştırılmaz.
/// Fark ücret hesabını değiştirmez; yalnızca uyarı üretir ve kullanıcı isterse devam edebilir.
/// </summary>
public static class PuantajKartKontrolu
{
    /// <summary>Günlük standart çalışma süresi; fazlası fazla mesai sayılır.</summary>
    public const decimal StandartGunlukSaat = 7.5m;

    /// <summary>Ondalık yuvarlama hatalarını fark saymamak için eşik.</summary>
    public const decimal Tolerans = 0.01m;

    /// <summary>Saatlik personelin puantajdaki toplam çalışma süresi (normal saat + fazla mesai).</summary>
    public static decimal ToplamSaat(IEnumerable<GunlukPuantaj> gunler)
        => gunler.Where(g => g.Calisti).Sum(g => g.CalismaSaati + g.FazlaMesaiSaat);

    public static decimal ToplamFazlaMesai(IEnumerable<GunlukPuantaj> gunler)
        => gunler.Where(g => g.Calisti).Sum(g => g.FazlaMesaiSaat);

    public static int CalisilanGun(IEnumerable<GunlukPuantaj> gunler) => gunler.Count(g => g.Calisti);

    /// <summary>Günlük kayıtlardan gerçekleşen toplamı hesaplayıp karşılaştırır.</summary>
    public static KartKontrolSonucu Hesapla(Sofor? personel, IEnumerable<GunlukPuantaj> gunler)
    {
        var liste = gunler as IReadOnlyCollection<GunlukPuantaj> ?? gunler.ToList();
        return Degerlendir(personel, ToplamSaat(liste), ToplamFazlaMesai(liste), CalisilanGun(liste));
    }

    /// <summary>Henüz oluşturulmamış çalışma düzenini (seçili gün × günlük saat) karşılaştırır.</summary>
    public static KartKontrolSonucu Plan(Sofor? personel, int seciliGunSayisi, decimal gunlukSaat)
    {
        var toplam = seciliGunSayisi * Math.Max(0, gunlukSaat);
        var fazlaMesai = seciliGunSayisi * Math.Max(0, gunlukSaat - StandartGunlukSaat);
        return Degerlendir(personel, toplam, fazlaMesai, seciliGunSayisi);
    }

    public static KartKontrolSonucu Degerlendir(Sofor? personel, decimal toplamSaat, decimal fazlaMesai, int calisilanGun)
    {
        var tip = personel?.BrutMaasHesaplamaTipi ?? BrutMaasHesaplamaTipi.Manuel;
        var kart = personel?.CalismaMiktari ?? 0;

        var (birim, gerceklesen, fm) = tip switch
        {
            BrutMaasHesaplamaTipi.Saatlik when kart > 0 => (KartKontrolBirimi.Saat, toplamSaat, fazlaMesai),
            BrutMaasHesaplamaTipi.Gunluk when kart > 0 => (KartKontrolBirimi.Gun, (decimal)calisilanGun, 0m),
            _ => (KartKontrolBirimi.Yok, 0m, 0m)
        };

        if (birim == KartKontrolBirimi.Yok)
            return new KartKontrolSonucu(tip, birim, 0, 0, 0, KartKontrolDurumu.Tanimsiz);

        var fark = gerceklesen - kart;
        var durum = Math.Abs(fark) < Tolerans
            ? KartKontrolDurumu.Uyumlu
            : fark < 0 ? KartKontrolDurumu.Az : KartKontrolDurumu.Fazla;
        return new KartKontrolSonucu(tip, birim, kart, gerceklesen, fm, durum);
    }

    /// <summary>
    /// Saatlik personele toplu saat atarken önerilecek günlük saat. Kart değeri aylık toplam olduğundan
    /// (örn. 195) günlük sayılmaz; 24 saati aşan değerlerde standart 7,5 saat kullanılır.
    /// </summary>
    public static decimal VarsayilanGunlukSaat(Sofor? personel)
    {
        var kart = personel?.CalismaMiktari ?? 0;
        return kart > 0 && kart <= 24 ? kart : StandartGunlukSaat;
    }

    /// <summary>Günlük saati standart süreye göre normal saat ve fazla mesaiye böler.</summary>
    public static (decimal Normal, decimal FazlaMesai) Bol(decimal saat)
    {
        var pozitif = Math.Max(0, saat);
        return (Math.Min(pozitif, StandartGunlukSaat), Math.Max(0, pozitif - StandartGunlukSaat));
    }
}
