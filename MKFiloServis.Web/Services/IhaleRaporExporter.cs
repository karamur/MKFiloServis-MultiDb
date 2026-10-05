using System.Globalization;
using ClosedXML.Excel;
using MKFiloServis.Web.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace MKFiloServis.Web.Services;

/// <summary>İhale ekranında yüklenmiş rapor verisinin Excel ve PDF sunumu.</summary>
public static class IhaleRaporExporter
{
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");
    public sealed record RaporTablosu(string Ad, string[] Basliklar, List<object[]> Satirlar);

    public static List<RaporTablosu> ProjeksiyonTablolari(IhaleProjeOzet ozet)
    {
        var aylar = ozet.AylikProjeksiyonlar;
        var ozetRows = new List<object[]>
        {
            new object[] { "Sözleşme süresi (ay)", ozet.SozlesmeSuresiAy },
            new object[] { "Hat sayısı", ozet.GuzergahSayisi },
            new object[] { "Toplam proje maliyeti (TL)", aylar.Sum(a => a.ToplamMaliyet) },
            new object[] { "Toplam proje kârı (TL)", aylar.Sum(a => a.ToplamKar) },
            new object[] { "Toplam teklif bedeli (TL)", aylar.Sum(a => a.ToplamTeklif) },
            new object[] { "Enflasyon toplam etkisi (TL)", aylar.Sum(a => a.EnflasyonEtkisi) }
        };
        if (aylar.Count > 0)
        {
            var artis = aylar[^1].ToplamMaliyet - aylar[0].ToplamMaliyet;
            ozetRows.Add(new object[] { "İlk ay maliyeti (TL)", aylar[0].ToplamMaliyet });
            ozetRows.Add(new object[] { "Son ay tahmini maliyeti (TL)", aylar[^1].ToplamMaliyet });
            ozetRows.Add(new object[] { "Süre sonu maliyet artışı (TL)", artis });
            ozetRows.Add(new object[] { "Maliyet artışı (%)", aylar[0].ToplamMaliyet == 0 ? 0m : artis / aylar[0].ToplamMaliyet * 100 });
        }
        var rows = aylar.Select(a => new object[] { a.DonemAdi, a.ToplamMaliyet, a.ToplamKar,
            a.ToplamTeklif, a.EnflasyonEtkisi, a.KumulatifMaliyet, a.KumulatifKar }).ToList();
        rows.Add(new object[] { "TOPLAM", aylar.Sum(a => a.ToplamMaliyet), aylar.Sum(a => a.ToplamKar),
            aylar.Sum(a => a.ToplamTeklif), aylar.Sum(a => a.EnflasyonEtkisi),
            aylar.LastOrDefault()?.KumulatifMaliyet ?? 0m, aylar.LastOrDefault()?.KumulatifKar ?? 0m });
        return new()
        {
            new("Proje Özeti", new[] { "Gösterge", "Değer" }, ozetRows),
            new("Aylık Projeksiyon", new[] { "Dönem", "Maliyet (TL)", "Kâr (TL)", "Teklif (TL)",
                "Enflasyon Etkisi (TL)", "Küm. Maliyet (TL)", "Küm. Kâr (TL)" }, rows),
            new("Hat Detayları", new[] { "Hat", "Sahiplik", "Mesafe (km)", "Aylık Maliyet (TL)",
                "Aylık Teklif (TL)", "Kâr (%)", "Sefer Başı Teklif (TL)" },
                ozet.KalemOzetleri.Select(k => new object[] { k.HatAdi, k.SahiplikDurumu, k.MesafeKm,
                    k.AylikMaliyet, k.AylikTeklifFiyati, k.KarMarji, k.SeferBasiTeklif }).ToList())
        };
    }

    public static byte[] Excel(string baslik, string bilgi, IReadOnlyList<RaporTablosu> tablolar)
    {
        using var book = new XLWorkbook();
        book.Properties.Title = baslik;
        book.Properties.Author = "MKFiloServis";
        foreach (var tablo in tablolar)
        {
            var ws = book.Worksheets.Add(tablo.Ad);
            var cols = tablo.Basliklar.Length;
            ws.Style.Font.FontName = "Calibri";
            ws.Style.Font.FontSize = 11;
            ws.Range(1, 1, 1, cols).Merge().Value = baslik;
            ws.Range(1, 1, 1, cols).Style.Font.Bold = true;
            ws.Range(1, 1, 1, cols).Style.Font.FontSize = 18;
            ws.Range(1, 1, 1, cols).Style.Font.FontColor = XLColor.FromHtml("#17365D");
            ws.Range(2, 1, 2, cols).Merge().Value = bilgi;
            ws.Range(2, 1, 2, cols).Style.Alignment.WrapText = true;
            ws.Row(2).Height = 34;
            ws.Range(3, 1, 3, cols).Merge().Value = $"{tablo.Ad} · {DateTime.Now:dd.MM.yyyy HH:mm} · Para birimi: TL";
            for (int c = 0; c < cols; c++) ws.Cell(5, c + 1).Value = tablo.Basliklar[c];
            for (int r = 0; r < tablo.Satirlar.Count; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    var cell = ws.Cell(r + 6, c + 1);
                    var value = tablo.Satirlar[r][c];
                    if (value is decimal number) { cell.Value = number; cell.Style.NumberFormat.Format = "#,##0.00;[Red]-#,##0.00"; }
                    else if (value is int count) cell.Value = count;
                    else cell.Value = Convert.ToString(value, Tr) ?? "";
                }
            }
            var last = Math.Max(6, tablo.Satirlar.Count + 5);
            var table = ws.Range(5, 1, last, cols).CreateTable();
            table.Theme = XLTableTheme.TableStyleMedium2;
            for (int r = 0; r < tablo.Satirlar.Count; r++)
                if (Equals(tablo.Satirlar[r][0], "TOPLAM"))
                    ws.Range(r + 6, 1, r + 6, cols).Style.Font.Bold = true;
            ws.SheetView.FreezeRows(5);
            ws.Columns().AdjustToContents();
            foreach (var column in ws.ColumnsUsed()) column.Width = Math.Clamp(column.Width, 16, 38);
            ws.Range(5, 1, last, cols).Style.Alignment.WrapText = true;
            ws.Rows(5, last).AdjustToContents();
            ws.PageSetup.PageOrientation = XLPageOrientation.Landscape;
            ws.PageSetup.PaperSize = XLPaperSize.A4Paper;
            ws.PageSetup.PagesWide = 1;
            ws.PageSetup.PagesTall = 0;
            ws.PageSetup.SetRowsToRepeatAtTop(1, 5);
            ws.PageSetup.PrintAreas.Add(1, 1, last, cols);
        }
        using var stream = new MemoryStream();
        book.SaveAs(stream);
        return stream.ToArray();
    }

    public static byte[] Pdf(string baslik, string bilgi, IReadOnlyList<RaporTablosu> tablolar)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        return Document.Create(doc =>
        {
            foreach (var tablo in tablolar)
                doc.Page(page =>
                {
                    page.Size(tablo.Basliklar.Length > 9 ? PageSizes.A3.Landscape() : PageSizes.A4.Landscape());
                    page.Margin(28);
                    page.DefaultTextStyle(x => x.FontSize(9));
                    page.Header().PaddingBottom(12).Column(col =>
                    {
                        col.Item().Text("MKFiloServis | İhale Hazırlık").FontSize(9).FontColor(Colors.Grey.Darken1);
                        col.Item().Text(baslik).FontSize(17).Bold().FontColor("#17365D");
                        col.Item().Text(bilgi).FontSize(9);
                        col.Item().Text($"{tablo.Ad} · Para birimi: TL").FontSize(12).Bold();
                    });
                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            for (int c = 0; c < tablo.Basliklar.Length; c++) cols.RelativeColumn(c == 0 ? 1.5f : 1);
                        });
                        table.Header(header =>
                        {
                            foreach (var h in tablo.Basliklar)
                                header.Cell().Background("#17365D").Padding(6).Text(h).FontColor(Colors.White).Bold();
                        });
                        for (int r = 0; r < tablo.Satirlar.Count; r++)
                        {
                            bool toplam = Equals(tablo.Satirlar[r][0], "TOPLAM");
                            foreach (var value in tablo.Satirlar[r])
                            {
                                var cell = table.Cell().Background(toplam ? "#E2EAF4" : r % 2 == 0 ? "#F3F6FA" : "#FFFFFF")
                                    .BorderBottom(0.5f).BorderColor("#D9E2EF").Padding(5);
                                if (value is decimal or int) cell = cell.AlignRight();
                                var text = cell.Text(value is decimal n ? n.ToString("N2", Tr) : Convert.ToString(value, Tr) ?? "");
                                if (toplam) text.Bold();
                            }
                        }
                    });
                    page.Footer().PaddingTop(10).Row(row =>
                    {
                        row.RelativeItem().Text($"Oluşturma: {DateTime.Now:dd.MM.yyyy HH:mm} · MKFiloServis · İhale raporu").FontSize(8).FontColor(Colors.Grey.Darken1);
                        row.ConstantItem(95).AlignRight().Text(t => { t.Span("Sayfa "); t.CurrentPageNumber(); t.Span(" / "); t.TotalPages(); });
                    });
                });
        }).GeneratePdf();
    }
}
