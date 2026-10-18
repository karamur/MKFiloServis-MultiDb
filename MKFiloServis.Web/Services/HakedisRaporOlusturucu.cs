using System.Globalization;
using ClosedXML.Excel;
using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Helpers;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Web.Services;

/// <summary>Gruplanmış önizleme satırları: ekranda başlık satırı, raporda grup başlığı ve ara toplam olur.</summary>
public sealed record HakedisSatirGrubu(string Anahtar, string Baslik, string? Alt, IReadOnlyList<HakedisKapsamSatiri> Satirlar)
{
    public int PuantajSayisi => Satirlar.Sum(s => s.PuantajSayisi);
    public decimal Sefer => Satirlar.Sum(s => s.Sefer);
    public decimal Gelir => Satirlar.Sum(s => s.Gelir);
    public decimal Gider => Satirlar.Sum(s => s.Gider);
    public decimal Maliyet => Satirlar.Sum(s => s.Maliyet);
    public decimal Net => Gelir - Gider - Maliyet;
    public decimal HakedisTutari => Satirlar.Sum(s => s.HakedisTutari);
}

/// <summary>Dışa aktarılacak (Excel / PDF) tablo. Ekrandaki tabloyla aynı satırlardan üretilir.</summary>
public sealed class HakedisRaporTablosu
{
    public string Baslik { get; init; } = string.Empty;
    public string AltBaslik { get; init; } = string.Empty;
    public string KapsamOzeti { get; init; } = string.Empty;
    public List<HakedisRaporKolonu> Kolonlar { get; init; } = new();
    public List<HakedisRaporGrubu> Gruplar { get; init; } = new();

    /// <summary>Kolonlarla aynı sırada; toplanmayan kolonlarda null.</summary>
    public object?[] GenelToplam { get; init; } = Array.Empty<object?>();
}

/// <summary>
/// Rapor kolonu. Sayısal kolonlar sağa hizalanır ve "N2" biçimiyle yazılır;
/// toplanır kolonlar ara toplam ve genel toplamda toplanır. Genişlik göreli bir orandır.
/// </summary>
public sealed record HakedisRaporKolonu(string Baslik, bool Sayisal, bool Toplanir, float Genislik);

public sealed class HakedisRaporGrubu
{
    /// <summary>Gruplama yoksa boş.</summary>
    public string? Baslik { get; init; }
    public List<object?[]> Satirlar { get; init; } = new();
    public object?[]? AraToplam { get; init; }
}

/// <summary>
/// Hakediş önizlemesini gruplar ve Excel / PDF için tek tablo modeline çevirir.
/// Ekran, Excel ve PDF aynı satırlardan beslendiği için birbirinden farklı sonuç göstermez.
/// </summary>
public static class HakedisRaporOlusturucu
{
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

    public static string GruplamaAdi(HakedisGruplama gruplama) => gruplama switch
    {
        HakedisGruplama.Cari => "Cari (kurum)",
        HakedisGruplama.AracSahibi => "Araç sahibi",
        HakedisGruplama.Guzergah => "Güzergâh",
        HakedisGruplama.Arac => "Araç (plaka)",
        HakedisGruplama.Sofor => "Şoför",
        HakedisGruplama.Eslestirme => "Eşleştirme",
        _ => "Gruplama yok"
    };

    public static string TutarBasligi(HakedisTipi tip) => tip switch
    {
        HakedisTipi.Kurum => "Kesilecek Fatura",
        HakedisTipi.Tedarikci or HakedisTipi.AracSahibi => "Ödenecek Tutar",
        HakedisTipi.Arac => "Araç Tutarı",
        _ => "Tutar"
    };

    public static string TipAdi(HakedisTipi tip) => tip switch
    {
        HakedisTipi.Kurum => "Kurum hakedişi (gelir)",
        HakedisTipi.Tedarikci => "Tedarikçi hakedişi (gider)",
        HakedisTipi.AracSahibi => "Araç sahibi hakedişi (komisyon / kiralık, gider)",
        HakedisTipi.Arac => "Araç hakedişi (faturalanmaz)",
        _ => tip.ToString()
    };

    /// <summary>
    /// Aynı cari / güzergâh / araç / şoför için gelen satırları (ör. eşleştirmeden türeyen ve elle girilen ek seferler)
    /// tek satırda toplar. Eşleştirmeye göre gruplanıyorsa eşleştirmeler ayrı tutulur.
    /// </summary>
    public static List<HakedisKapsamSatiri> Birlestir(IEnumerable<HakedisKapsamSatiri> satirlar, bool eslestirmeyiAyir)
    {
        return satirlar
            .GroupBy(s => (s.ReferansId, s.CariId, s.GuzergahId, s.AracId, s.SoforId, s.Uygun, s.HakedisteMi,
                Eslestirme: eslestirmeyiAyir ? s.EslestirmeId : -1))
            .Select(g =>
            {
                var ilk = g.First();
                if (g.Count() == 1)
                    return ilk;

                var tekEslestirme = g.Select(x => x.EslestirmeId).Distinct().Count() == 1;
                return ilk with
                {
                    EslestirmeId = tekEslestirme ? ilk.EslestirmeId : -1,
                    EslestirmeAd = tekEslestirme ? ilk.EslestirmeAd : "Birden fazla eşleştirme",
                    PuantajSayisi = g.Sum(x => x.PuantajSayisi),
                    Sefer = g.Sum(x => x.Sefer),
                    Gelir = g.Sum(x => x.Gelir),
                    Gider = g.Sum(x => x.Gider),
                    Maliyet = g.Sum(x => x.Maliyet),
                    HakedisTutari = g.Sum(x => x.HakedisTutari)
                };
            })
            .OrderBy(s => s.ReferansAd, StringComparer.Create(Tr, ignoreCase: true))
            .ThenBy(s => s.GuzergahAd, StringComparer.Create(Tr, ignoreCase: true))
            .ThenBy(s => s.Plaka, StringComparer.Create(Tr, ignoreCase: true))
            .ThenBy(s => s.SoforAd, StringComparer.Create(Tr, ignoreCase: true))
            .ToList();
    }

    public static List<HakedisSatirGrubu> Grupla(IEnumerable<HakedisKapsamSatiri> satirlar, HakedisGruplama gruplama)
    {
        var liste = satirlar.ToList();
        if (gruplama == HakedisGruplama.Yok)
            return new List<HakedisSatirGrubu> { new(string.Empty, string.Empty, null, liste) };

        static string Sahiplik(HakedisKapsamSatiri s) => SahiplikHelper.GetMetin(s.SahiplikTipi);

        IEnumerable<IGrouping<(string Anahtar, string Baslik, string? Alt), HakedisKapsamSatiri>> gruplar = gruplama switch
        {
            HakedisGruplama.Cari => liste.GroupBy(s => (s.CariId.ToString(), s.CariAd, (string?)null)),
            HakedisGruplama.AracSahibi => liste.GroupBy(s => (s.SahipAnahtari, s.SahipAd, (string?)Sahiplik(s))),
            HakedisGruplama.Guzergah => liste.GroupBy(s => (s.GuzergahId.ToString(), s.GuzergahAd, (string?)s.CariAd)),
            HakedisGruplama.Arac => liste.GroupBy(s => (s.AracId.ToString(), s.Plaka, (string?)s.SahipAd)),
            HakedisGruplama.Sofor => liste.GroupBy(s => (s.SoforId.ToString(), s.SoforAd, (string?)null)),
            HakedisGruplama.Eslestirme => liste.GroupBy(s => (s.EslestirmeId.ToString(), s.EslestirmeAd, (string?)null)),
            _ => liste.GroupBy(s => (string.Empty, string.Empty, (string?)null))
        };

        return gruplar
            .OrderBy(g => g.Key.Baslik, StringComparer.Create(Tr, ignoreCase: true))
            .Select(g => new HakedisSatirGrubu(g.Key.Anahtar, g.Key.Baslik, g.Key.Alt, g.ToList()))
            .ToList();
    }

    private static string GrupOnEki(HakedisGruplama gruplama) => gruplama switch
    {
        HakedisGruplama.Cari => "CARİ",
        HakedisGruplama.AracSahibi => "ARAÇ SAHİBİ",
        HakedisGruplama.Guzergah => "GÜZERGÂH",
        HakedisGruplama.Arac => "ARAÇ",
        HakedisGruplama.Sofor => "ŞOFÖR",
        HakedisGruplama.Eslestirme => "EŞLEŞTİRME",
        _ => string.Empty
    };

    private sealed record Kolon(string Baslik, bool Sayisal, bool Toplanir, float Genislik, Func<HakedisKapsamSatiri, object?> Deger)
    {
        public HakedisRaporKolonu Tanim => new(Baslik, Sayisal, Toplanir, Genislik);
    }

    /// <summary>
    /// Önizleme satırlarından dışa aktarılacak tabloyu üretir. <c>karlilikKolonlari</c> açıksa
    /// gelir / gider / iç maliyet / net kolonları ayrı ayrı eklenir; kapalıysa seçili türün tutarı yazılır.
    /// </summary>
    public static HakedisRaporTablosu TabloOlustur(
        IReadOnlyCollection<HakedisKapsamSatiri> satirlar,
        HakedisTipi tip,
        HakedisGruplama gruplama,
        bool karlilikKolonlari,
        int yil,
        int ay,
        string kapsamOzeti)
    {
        var hakedistekiVar = satirlar.Any(s => s.HakedisteMi);

        var kolonlar = new List<Kolon>();
        // Gruplanan boyutun kolonu, grup başlığında zaten yazdığı için tabloda tekrarlanmaz.
        void Ekle(HakedisGruplama gizle, Kolon kolon)
        {
            if (gruplama != gizle)
                kolonlar.Add(kolon);
        }

        Ekle(HakedisGruplama.Cari, new Kolon("Cari", false, false, 2.2f, s => s.CariAd));
        Ekle(HakedisGruplama.Guzergah, new Kolon("Güzergâh", false, false, 2.2f, s => s.GuzergahAd));
        Ekle(HakedisGruplama.Arac, new Kolon("Plaka", false, false, 1.1f, s => s.Plaka));
        Ekle(HakedisGruplama.AracSahibi, new Kolon("Araç Sahibi", false, false, 2.0f,
            s => s.SahiplikTipi == AracSahiplikTipi.Ozmal ? "Özmal" : $"{SahiplikHelper.GetMetin(s.SahiplikTipi)}: {s.SahipAd}"));
        Ekle(HakedisGruplama.Sofor, new Kolon("Şoför", false, false, 1.6f, s => s.SoforAd));
        kolonlar.Add(new Kolon("Sefer", true, true, 0.8f, s => s.Sefer));

        if (karlilikKolonlari)
        {
            kolonlar.Add(new Kolon("Gelir", true, true, 1.2f, s => s.Gelir));
            kolonlar.Add(new Kolon("Gider", true, true, 1.2f, s => s.Gider));
            kolonlar.Add(new Kolon("İç Maliyet", true, true, 1.2f, s => s.Maliyet));
            kolonlar.Add(new Kolon("Net", true, true, 1.2f, s => s.Net));
        }
        else
        {
            kolonlar.Add(new Kolon("Birim Fiyat", true, false, 1.1f, s => s.Sefer > 0 ? s.HakedisTutari / s.Sefer : 0m));
            kolonlar.Add(new Kolon(TutarBasligi(tip), true, true, 1.5f, s => s.HakedisTutari));
        }

        if (hakedistekiVar)
            kolonlar.Add(new Kolon("Durum", false, false, 1.0f, s => s.HakedisteMi ? "Hakedişte" : "Yeni"));

        object?[] SatirDegerleri(HakedisKapsamSatiri s) => kolonlar.Select(k => k.Deger(s)).ToArray();

        object?[] Toplamlar(string etiket, IReadOnlyCollection<HakedisKapsamSatiri> kaynak)
        {
            var sonuc = new object?[kolonlar.Count];
            for (var i = 0; i < kolonlar.Count; i++)
            {
                var kolon = kolonlar[i];
                if (kolon.Toplanir)
                    sonuc[i] = kaynak.Sum(s => Convert.ToDecimal(kolon.Deger(s), CultureInfo.InvariantCulture));
            }

            // Etiket, toplanmayan ilk metin kolonuna yazılır.
            var etiketKolonu = kolonlar.FindIndex(k => !k.Sayisal);
            sonuc[etiketKolonu < 0 ? 0 : etiketKolonu] = etiket;
            return sonuc;
        }

        var tablo = new HakedisRaporTablosu
        {
            Baslik = "HAKEDİŞ RAPORU",
            AltBaslik = $"{ay:D2}/{yil} · {TipAdi(tip)}" + (gruplama == HakedisGruplama.Yok ? string.Empty : $" · Gruplama: {GruplamaAdi(gruplama)}"),
            KapsamOzeti = $"Kapsam: {kapsamOzeti}",
            Kolonlar = kolonlar.Select(k => k.Tanim).ToList(),
            GenelToplam = Toplamlar("GENEL TOPLAM", satirlar)
        };

        foreach (var grup in Grupla(satirlar, gruplama))
        {
            var baslik = gruplama == HakedisGruplama.Yok
                ? null
                : $"{GrupOnEki(gruplama)}: {grup.Baslik}" + (string.IsNullOrWhiteSpace(grup.Alt) ? string.Empty : $" ({grup.Alt})");

            tablo.Gruplar.Add(new HakedisRaporGrubu
            {
                Baslik = baslik,
                Satirlar = grup.Satirlar.Select(SatirDegerleri).ToList(),
                AraToplam = gruplama == HakedisGruplama.Yok ? null : Toplamlar($"{grup.Baslik} Ara Toplam", grup.Satirlar.ToList())
            });
        }

        return tablo;
    }

    /// <summary>Hücre değerini ekran/PDF için metne çevirir (sayılar tr-TR "N2").</summary>
    public static string Metin(object? deger) => deger switch
    {
        null => string.Empty,
        decimal d => d.ToString("N2", Tr),
        string s => s,
        _ => Convert.ToString(deger, Tr) ?? string.Empty
    };

    public static byte[] ExcelOlustur(HakedisRaporTablosu tablo, string sayfaAdi = "Hakediş")
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add(sayfaAdi);
        var kolonSayisi = Math.Max(tablo.Kolonlar.Count, 1);

        ws.Cell(1, 1).Value = tablo.Baslik;
        ws.Range(1, 1, 1, kolonSayisi).Merge();
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 14;

        ws.Cell(2, 1).Value = tablo.AltBaslik;
        ws.Range(2, 1, 2, kolonSayisi).Merge();
        ws.Cell(2, 1).Style.Font.Bold = true;

        ws.Cell(3, 1).Value = tablo.KapsamOzeti;
        ws.Range(3, 1, 3, kolonSayisi).Merge();
        ws.Cell(3, 1).Style.Font.Italic = true;
        ws.Cell(3, 1).Style.Font.FontColor = XLColor.Gray;

        const int baslikSatiri = 5;
        for (var i = 0; i < tablo.Kolonlar.Count; i++)
        {
            var hucre = ws.Cell(baslikSatiri, i + 1);
            hucre.Value = tablo.Kolonlar[i].Baslik;
            hucre.Style.Font.Bold = true;
            hucre.Style.Fill.BackgroundColor = XLColor.LightGray;
            hucre.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            if (tablo.Kolonlar[i].Sayisal)
                hucre.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        }

        var satir = baslikSatiri + 1;

        void SatirYaz(object?[] degerler, bool kalin, XLColor? arkaPlan)
        {
            for (var i = 0; i < degerler.Length; i++)
            {
                var hucre = ws.Cell(satir, i + 1);
                switch (degerler[i])
                {
                    case null:
                        break;
                    case decimal d:
                        hucre.Value = d;
                        hucre.Style.NumberFormat.Format = "#,##0.00";
                        break;
                    default:
                        hucre.Value = Metin(degerler[i]);
                        break;
                }
            }

            if (kalin || arkaPlan != null)
            {
                var aralik = ws.Range(satir, 1, satir, kolonSayisi);
                aralik.Style.Font.Bold = kalin;
                if (arkaPlan != null)
                    aralik.Style.Fill.BackgroundColor = arkaPlan;
            }

            satir++;
        }

        foreach (var grup in tablo.Gruplar)
        {
            if (!string.IsNullOrWhiteSpace(grup.Baslik))
            {
                ws.Cell(satir, 1).Value = grup.Baslik;
                ws.Range(satir, 1, satir, kolonSayisi).Merge();
                ws.Range(satir, 1, satir, kolonSayisi).Style.Font.Bold = true;
                ws.Range(satir, 1, satir, kolonSayisi).Style.Fill.BackgroundColor = XLColor.LightBlue;
                satir++;
            }

            foreach (var degerler in grup.Satirlar)
                SatirYaz(degerler, false, null);

            if (grup.AraToplam != null)
                SatirYaz(grup.AraToplam, true, XLColor.WhiteSmoke);
        }

        SatirYaz(tablo.GenelToplam, true, XLColor.LightGray);

        for (var i = 0; i < tablo.Kolonlar.Count; i++)
        {
            if (tablo.Kolonlar[i].Sayisal)
                ws.Column(i + 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        }

        // Birleştirilmiş başlık satırları sütun genişliğini bozmasın diye yalnızca tablo gövdesine göre ayarlanır.
        ws.Columns(1, kolonSayisi).AdjustToContents(baslikSatiri, satir - 1, 8d, 50d);
        ws.SheetView.FreezeRows(baslikSatiri);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
