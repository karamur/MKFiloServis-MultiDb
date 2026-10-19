using MKFiloServis.Web.Services;

namespace MKFiloServis.Web.Services.Interfaces;

/// <summary>Aylık grafik raporu isteği: yıl, yetki kapsamı ve (testte) sabit "bugün".</summary>
public sealed class AylikGrafikIstegi
{
    /// <summary>Raporlanacak yıl; verilmezse içinde bulunulan yıl.</summary>
    public int? Yil { get; init; }

    public YoneticiOzetKapsami Kapsam { get; init; } = YoneticiOzetKapsami.Tumu;

    /// <summary>Test/önizleme için sabit "bugün"; verilmezse iş günü (<c>BusinessTime.Today</c>).</summary>
    public DateTime? Bugun { get; init; }

    /// <summary>Seçilebilir yıl aralığı: geçmişe doğru <see cref="GeriYilSayisi"/> yıl, bir sonraki yıl (bütçe/planlı sefer için).</summary>
    public const int GeriYilSayisi = 4;

    public static (int EnErken, int EnGec) YilSiniri(DateTime bugun) => (bugun.Year - GeriYilSayisi, bugun.Year + 1);
}

/// <summary>Kart değerlerinin birimi: biçimleme ve eksen ölçeği buna göre seçilir.</summary>
public enum AylikBirim
{
    Adet = 1,
    Para = 2
}

/// <summary>Grafik sütun rengi (renk sınıfı arayüzde eşlenir; anlam rengi serviste belirlenir).</summary>
public enum GrafikRengi
{
    Mavi = 1,
    Yesil = 2,
    Kirmizi = 3,
    Turuncu = 4,
    Mor = 5,
    Gri = 6
}

/// <summary>
/// Seçilen yılın 12 ayı için bölüm bölüm grafik raporu. Kapsam dışı bölümler kart üretmez;
/// yüklenemeyen bölümlerin adı <see cref="Hatalar"/>'a yazılır.
/// </summary>
public sealed class AylikGrafikRaporu
{
    public required int Yil { get; init; }

    /// <summary>Raporun "bugün"ü (içinde bulunulan ayı vurgulamak ve kesim günü için).</summary>
    public DateTime Bugun { get; init; }

    /// <summary>Toplamların ve karşılaştırmanın kesim günü: içinde bulunulan yılda bugün, geçmiş yılda 31 Aralık, gelecek yılda yok.</summary>
    public DateTime? KesimGunu { get; init; }

    /// <summary>Yıl toplamlarının karşılaştırıldığı geçen yılın aynı dönemi (gelecek yılda yok).</summary>
    public string KarsilastirmaMetni { get; init; } = string.Empty;

    public DateTime UretimZamani { get; init; }

    /// <summary>Aktif firma adı veya "Tüm Firmalar".</summary>
    public string KapsamAdi { get; init; } = string.Empty;

    public IReadOnlyList<AylikGrafikKarti> Kartlar { get; init; } = Array.Empty<AylikGrafikKarti>();

    public IReadOnlyList<string> Hatalar { get; init; } = Array.Empty<string>();

    /// <summary>Hiçbir kartta grafik verisi yok.</summary>
    public bool Bos => Kartlar.All(k => k.Bos);
}

/// <summary>Tek bölümün 12 aylık grafik kartı: bir veya iki değer serisi, yıl toplamları ve geçen yılla karşılaştırma.</summary>
public sealed class AylikGrafikKarti
{
    public string Anahtar { get; init; } = string.Empty;
    public string Baslik { get; init; } = string.Empty;
    public string AltBaslik { get; init; } = string.Empty;
    public string Ikon { get; init; } = "bi-bar-chart-line";

    /// <summary>Simge zemini için Bootstrap renk adı (primary, success, …).</summary>
    public string Renk { get; init; } = "primary";

    public string Link { get; init; } = "/";

    public AylikBirim Birim { get; init; } = AylikBirim.Adet;

    /// <summary>Adet kartlarında sayının yanına yazılan birim ("sefer", "kayıt", "evrak").</summary>
    public string AdetEki { get; init; } = string.Empty;

    /// <summary>İkinci serinin birinciyle aynı sütunda üst üste (yığılı) çizilmesi: yapılan + planlı sefer.</summary>
    public bool Yigili { get; init; }

    public GrafikRengi Renk1 { get; init; } = GrafikRengi.Mavi;
    public GrafikRengi Renk2 { get; init; } = GrafikRengi.Gri;

    /// <summary>Birinci / ikinci seride artış iyi haberse true (aksi halde değişim rozeti nötr gösterilir).</summary>
    public bool YuksekIyi1 { get; init; }
    public bool YuksekIyi2 { get; init; }

    /// <summary>12 aylık seri. Deger1/Deger2 sırasıyla <see cref="Seri"/> etiketlerine karşılık gelir.</summary>
    public ZamanSerisi Seri { get; init; } = ZamanSerisi.Bos;

    /// <summary>Yıl başından kesim gününe kadar toplam (planlı/gelecek değerler dahil değildir).</summary>
    public decimal Toplam1 { get; init; }
    public decimal Toplam2 { get; init; }

    /// <summary>Geçen yılın aynı dönemi toplamları.</summary>
    public decimal OncekiYilToplam1 { get; init; }
    public decimal OncekiYilToplam2 { get; init; }

    /// <summary>Gelecek yılda karşılaştırılacak gerçekleşen bir dönem yoktur.</summary>
    public bool KarsilastirmaVar { get; init; }

    public bool IkiSeri => !string.IsNullOrEmpty(Seri.Etiket2);

    public bool Bos => !Seri.VeriVar;

    public OzetDegisim Degisim1 => KarsilastirmaVar ? OzetDegisim.Hesapla(Toplam1, OncekiYilToplam1) : default;

    public OzetDegisim Degisim2 => KarsilastirmaVar ? OzetDegisim.Hesapla(Toplam2, OncekiYilToplam2) : default;
}
