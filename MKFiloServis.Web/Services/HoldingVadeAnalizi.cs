namespace MKFiloServis.Web.Services;

/// <summary>
/// Ödenmemiş (kısmen/hiç ödenmemiş, iptal olmayan) bir faturanın kalan tutarı ve vadesi.
/// <c>Borc</c> true ise alış faturası (ödenecek), false ise satış faturası (tahsil edilecek).
/// </summary>
public sealed record HoldingVadeKalemi(int FirmaId, string FirmaAdi, bool Borc, DateTime? Vade, decimal Kalan);

public enum HoldingVadeKovasi
{
    Gecikmis = 0,
    YediGun = 1,
    OtuzGun = 2,
    DoksanGun = 3,
    DahaSonra = 4,
    Vadesiz = 5
}

public sealed record HoldingVadeKovaToplami(HoldingVadeKovasi Kova, decimal Tutar, int Adet);

/// <summary>Bir yönün (borç ya da alacak) vade kovalarına göre dökümü.</summary>
public sealed class HoldingVadeYonu
{
    public IReadOnlyList<HoldingVadeKovaToplami> Kovalar { get; init; } = Array.Empty<HoldingVadeKovaToplami>();

    public decimal Toplam => Kovalar.Sum(k => k.Tutar);
    public int Adet => Kovalar.Sum(k => k.Adet);

    public HoldingVadeKovaToplami Kova(HoldingVadeKovasi kova)
        => Kovalar.FirstOrDefault(k => k.Kova == kova) ?? new HoldingVadeKovaToplami(kova, 0m, 0);

    public decimal Gecikmis => Kova(HoldingVadeKovasi.Gecikmis).Tutar;
    public int GecikmisAdet => Kova(HoldingVadeKovasi.Gecikmis).Adet;

    /// <summary>Bugünden itibaren 7 gün içinde vadesi gelenler (bugün dahil).</summary>
    public decimal YediGunIcinde => Kova(HoldingVadeKovasi.YediGun).Tutar;
}

public sealed class HoldingVadeFirmasi
{
    public int FirmaId { get; init; }
    public string FirmaAdi { get; init; } = string.Empty;
    public HoldingVadeYonu Borc { get; init; } = new();
    public HoldingVadeYonu Alacak { get; init; } = new();
}

/// <summary>Vade takviminin bir sütunu: "Gecikmiş", takvim ayları ve "Sonrası".</summary>
public sealed record HoldingVadeAyi(string Etiket, string Aciklama, decimal Borc, decimal Alacak, bool Gecikmis);

/// <summary>
/// Güncel vade analizi: açık faturalar bugüne göre kovalara (gecikmiş, 7/30/90 gün, sonrası, vadesiz) ve
/// önümüzdeki aylara dağıtılır. Borç = ödenecek alış faturaları, alacak = tahsil edilecek satış faturaları.
/// </summary>
public sealed class HoldingVadeOzeti
{
    public DateTime Bugun { get; init; }
    public HoldingVadeYonu Borc { get; init; } = new();
    public HoldingVadeYonu Alacak { get; init; } = new();

    /// <summary>Firma başına döküm; toplam borç + alacağa göre azalan.</summary>
    public IReadOnlyList<HoldingVadeFirmasi> Firmalar { get; init; } = Array.Empty<HoldingVadeFirmasi>();

    /// <summary>Gecikmiş + önümüzdeki aylar + sonrası (vadesizler dahil değildir).</summary>
    public IReadOnlyList<HoldingVadeAyi> Takvim { get; init; } = Array.Empty<HoldingVadeAyi>();

    public bool Bos => Borc.Adet == 0 && Alacak.Adet == 0;

    /// <summary>Alacaklar borçlardan çıkarılınca önümüzdeki dönemin net nakit durumu (toplam alacak − toplam borç).</summary>
    public decimal Net => Alacak.Toplam - Borc.Toplam;
}

public static class HoldingVadeAnalizi
{
    /// <summary>Vade takviminde gösterilen ay sayısı (bu ay dahil).</summary>
    public const int TakvimAySayisi = 6;

    public static HoldingVadeOzeti Olustur(IEnumerable<HoldingVadeKalemi> kalemler, DateTime bugun)
    {
        bugun = bugun.Date;
        var liste = kalemler.Where(k => k.Kalan > 0m).ToList();

        var firmalar = liste
            .GroupBy(k => k.FirmaId)
            .Select(g => new HoldingVadeFirmasi
            {
                FirmaId = g.Key,
                FirmaAdi = g.First().FirmaAdi,
                Borc = Yon(g.Where(k => k.Borc), bugun),
                Alacak = Yon(g.Where(k => !k.Borc), bugun)
            })
            .OrderByDescending(f => f.Borc.Toplam + f.Alacak.Toplam)
            .ThenBy(f => f.FirmaAdi, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        return new HoldingVadeOzeti
        {
            Bugun = bugun,
            Borc = Yon(liste.Where(k => k.Borc), bugun),
            Alacak = Yon(liste.Where(k => !k.Borc), bugun),
            Firmalar = firmalar,
            Takvim = TakvimOlustur(liste, bugun)
        };
    }

    /// <summary>Vadesi bugüne göre hangi kovaya düşer (bugün = 0. gün).</summary>
    public static HoldingVadeKovasi KovaBul(DateTime? vade, DateTime bugun)
    {
        if (vade is not { } v)
            return HoldingVadeKovasi.Vadesiz;

        var gun = (v.Date - bugun.Date).Days;
        if (gun < 0) return HoldingVadeKovasi.Gecikmis;
        if (gun <= 7) return HoldingVadeKovasi.YediGun;
        if (gun <= 30) return HoldingVadeKovasi.OtuzGun;
        if (gun <= 90) return HoldingVadeKovasi.DoksanGun;
        return HoldingVadeKovasi.DahaSonra;
    }

    public static string KovaAdi(HoldingVadeKovasi kova) => kova switch
    {
        HoldingVadeKovasi.Gecikmis => "Vadesi geçmiş",
        HoldingVadeKovasi.YediGun => "7 gün içinde",
        HoldingVadeKovasi.OtuzGun => "8–30 gün",
        HoldingVadeKovasi.DoksanGun => "31–90 gün",
        HoldingVadeKovasi.DahaSonra => "90 günden sonra",
        _ => "Vadesiz"
    };

    private static HoldingVadeYonu Yon(IEnumerable<HoldingVadeKalemi> kalemler, DateTime bugun)
    {
        var gruplar = kalemler
            .GroupBy(k => KovaBul(k.Vade, bugun))
            .ToDictionary(g => g.Key, g => (Tutar: g.Sum(k => k.Kalan), Adet: g.Count()));

        var kovalar = Enum.GetValues<HoldingVadeKovasi>()
            .Select(kova => gruplar.TryGetValue(kova, out var t)
                ? new HoldingVadeKovaToplami(kova, t.Tutar, t.Adet)
                : new HoldingVadeKovaToplami(kova, 0m, 0))
            .ToList();

        return new HoldingVadeYonu { Kovalar = kovalar };
    }

    private static List<HoldingVadeAyi> TakvimOlustur(IReadOnlyCollection<HoldingVadeKalemi> liste, DateTime bugun)
    {
        var ilkAy = new DateTime(bugun.Year, bugun.Month, 1);
        var sonrasi = ilkAy.AddMonths(TakvimAySayisi);

        decimal Toplam(Func<HoldingVadeKalemi, bool> secici) => liste.Where(secici).Sum(k => k.Kalan);

        var takvim = new List<HoldingVadeAyi>
        {
            new("Gecikmiş", "Vadesi geçmiş",
                Toplam(k => k.Borc && k.Vade is { } v && v.Date < bugun),
                Toplam(k => !k.Borc && k.Vade is { } v && v.Date < bugun),
                true)
        };

        for (var i = 0; i < TakvimAySayisi; i++)
        {
            var ay = ilkAy.AddMonths(i);
            var sonraki = ay.AddMonths(1);
            bool AyIcinde(DateTime? vade) => vade is { } v && v.Date >= bugun && v.Date >= ay && v.Date < sonraki;

            takvim.Add(new HoldingVadeAyi(
                HoldingBicim.AyKisa(ay.Month),
                HoldingBicim.Donem(ay.Year, ay.Month),
                Toplam(k => k.Borc && AyIcinde(k.Vade)),
                Toplam(k => !k.Borc && AyIcinde(k.Vade)),
                false));
        }

        takvim.Add(new HoldingVadeAyi(
            "Sonrası",
            $"{HoldingBicim.Donem(sonrasi.Year, sonrasi.Month)} ve sonrası",
            Toplam(k => k.Borc && k.Vade is { } v && v.Date >= sonrasi),
            Toplam(k => !k.Borc && k.Vade is { } v && v.Date >= sonrasi),
            false));

        return takvim;
    }
}
