using KOAFiloServis.Shared.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace KOAFiloServis.Web.Services;

public class PdfService : IPdfService
{
    public PdfService()
    {
        // QuestPDF Community License
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GenerateFaturaPdf(Fatura fatura)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(c => ComposeHeader(c, "FATURA"));
                page.Content().Element(c => ComposeFaturaContent(c, fatura));
                page.Footer().Element(ComposeFooter);
            });
        });

        return document.GeneratePdf();
    }

    public byte[] GenerateServisCalismaRaporuPdf(List<ServisCalisma> calismalar, DateTime baslangic, DateTime bitis)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Element(c => ComposeHeader(c, $"SERV�S �ALI�MA RAPORU\n{baslangic:dd.MM.yyyy} - {bitis:dd.MM.yyyy}"));
                page.Content().Element(c => ComposeServisCalismaContent(c, calismalar));
                page.Footer().Element(ComposeFooter);
            });
        });

        return document.GeneratePdf();
    }

    public byte[] GenerateBelgeUyariRaporuPdf(List<BelgeUyari> uyarilar)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(c => ComposeHeader(c, "BELGE S�RE UYARI RAPORU"));
                page.Content().Element(c => ComposeBelgeUyariContent(c, uyarilar));
                page.Footer().Element(ComposeFooter);
            });
        });

        return document.GeneratePdf();
    }

    public byte[] GenerateCariEkstresPdf(Cari cari, List<Fatura> faturalar, List<BankaKasaHareket> hareketler)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(c => ComposeHeader(c, $"CAR� EKSTRE\n{cari.Unvan}"));
                page.Content().Element(c => ComposeCariEkstreContent(c, cari, faturalar, hareketler));
                page.Footer().Element(ComposeFooter);
            });
        });

        return document.GeneratePdf();
    }

    private void ComposeHeader(IContainer container, string title)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(col =>
            {
                col.Item().Text("CRM F�LO SERV�S").Bold().FontSize(16);
                col.Item().Text("Filo Y�netim Sistemi").FontSize(10).FontColor(Colors.Grey.Medium);
            });

            row.RelativeItem().AlignRight().Column(col =>
            {
                col.Item().Text(title).Bold().FontSize(14).AlignRight();
                col.Item().Text($"Tarih: {DateTime.Now:dd.MM.yyyy HH:mm}").FontSize(9).AlignRight();
            });
        });

        container.PaddingBottom(15);
    }

    private void ComposeFooter(IContainer container)
    {
        container.AlignCenter().Text(text =>
        {
            text.Span("Sayfa ");
            text.CurrentPageNumber();
            text.Span(" / ");
            text.TotalPages();
        });
    }

    private void ComposeFaturaContent(IContainer container, Fatura fatura)
    {
        container.Column(col =>
        {
            // Fatura Bilgileri
            col.Item().Border(1).Padding(10).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("Fatura No:").Bold();
                    c.Item().Text(fatura.FaturaNo);
                    c.Item().PaddingTop(5).Text("Fatura Tarihi:").Bold();
                    c.Item().Text(fatura.FaturaTarihi.ToString("dd.MM.yyyy"));
                    c.Item().PaddingTop(5).Text("Vade Tarihi:").Bold();
                    c.Item().Text(fatura.VadeTarihi?.ToString("dd.MM.yyyy") ?? "-");
                });

                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("M��teri:").Bold();
                    c.Item().Text(fatura.Cari?.Unvan ?? "-");
                    c.Item().PaddingTop(5).Text("Vergi No:").Bold();
                    c.Item().Text(fatura.Cari?.VergiNo ?? "-");
                    c.Item().PaddingTop(5).Text("Adres:").Bold();
                    c.Item().Text(fatura.Cari?.Adres ?? "-");
                });
            });

            col.Item().PaddingVertical(15);

            // Fatura Kalemleri
            if (fatura.FaturaKalemleri?.Any() == true)
            {
                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(30);
                        columns.RelativeColumn(3);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(1);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("#").Bold();
                        header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("A��klama").Bold();
                        header.Cell().Background(Colors.Grey.Lighten2).Padding(5).AlignRight().Text("Miktar").Bold();
                        header.Cell().Background(Colors.Grey.Lighten2).Padding(5).AlignRight().Text("Birim Fiyat").Bold();
                        header.Cell().Background(Colors.Grey.Lighten2).Padding(5).AlignRight().Text("Tutar").Bold();
                    });

                    int sira = 1;
                    foreach (var kalem in fatura.FaturaKalemleri)
                    {
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(sira++.ToString());
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(kalem.Aciklama);
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text(kalem.Miktar.ToString("N2"));
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"{kalem.BirimFiyat:N2} ?");
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"{kalem.ToplamTutar:N2} ?");
                    }
                });
            }

            col.Item().PaddingVertical(10);

            // Toplam
            col.Item().AlignRight().Width(200).Column(totals =>
            {
                totals.Item().Row(r =>
                {
                    r.RelativeItem().Text("Ara Toplam:");
                    r.RelativeItem().AlignRight().Text($"{fatura.AraToplam:N2} ?");
                });
                totals.Item().Row(r =>
                {
                    r.RelativeItem().Text($"KDV (%{fatura.KdvOrani}):");
                    r.RelativeItem().AlignRight().Text($"{fatura.KdvTutar:N2} ?");
                });
                totals.Item().PaddingTop(5).BorderTop(1).Row(r =>
                {
                    r.RelativeItem().Text("Genel Toplam:").Bold();
                    r.RelativeItem().AlignRight().Text($"{fatura.GenelToplam:N2} ?").Bold();
                });
                if (fatura.OdenenTutar > 0)
                {
                    totals.Item().Row(r =>
                    {
                        r.RelativeItem().Text("�denen:");
                        r.RelativeItem().AlignRight().Text($"{fatura.OdenenTutar:N2} ?").FontColor(Colors.Green.Medium);
                    });
                    totals.Item().Row(r =>
                    {
                        r.RelativeItem().Text("Kalan:").Bold();
                        r.RelativeItem().AlignRight().Text($"{fatura.KalanTutar:N2} ?").Bold().FontColor(Colors.Red.Medium);
                    });
                }
            });

            // Notlar
            if (!string.IsNullOrEmpty(fatura.Notlar))
            {
                col.Item().PaddingTop(20).Text("Notlar:").Bold();
                col.Item().Text(fatura.Notlar);
            }
        });
    }

    private void ComposeServisCalismaContent(IContainer container, List<ServisCalisma> calismalar)
    {
        container.Column(col =>
        {
            // �zet
            col.Item().Row(row =>
            {
                row.RelativeItem().Background(Colors.Blue.Lighten4).Padding(10).Column(c =>
                {
                    c.Item().Text("Toplam Sefer").FontSize(9);
                    c.Item().Text(calismalar.Count.ToString()).Bold().FontSize(14);
                });
                row.ConstantItem(10);
                row.RelativeItem().Background(Colors.Green.Lighten4).Padding(10).Column(c =>
                {
                    c.Item().Text("Toplam Ciro").FontSize(9);
                    c.Item().Text($"{calismalar.Sum(c => c.Fiyat ?? 0):N0} ?").Bold().FontSize(14);
                });
                row.ConstantItem(10);
                row.RelativeItem().Background(Colors.Orange.Lighten4).Padding(10).Column(c =>
                {
                    c.Item().Text("Ara� Say�s�").FontSize(9);
                    c.Item().Text(calismalar.Select(c => c.AracId).Distinct().Count().ToString()).Bold().FontSize(14);
                });
            });

            col.Item().PaddingVertical(15);

            // Tablo
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(1);   // Tarih
                    columns.RelativeColumn(1);   // Plaka
                    columns.RelativeColumn(2);   // G�zergah
                    columns.RelativeColumn(2);   // Firma
                    columns.RelativeColumn(1);   // �of�r
                    columns.RelativeColumn(1);   // Fiyat
                });

                table.Header(header =>
                {
                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Tarih").Bold();
                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Plaka").Bold();
                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("G�zergah").Bold();
                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Firma").Bold();
                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("�of�r").Bold();
                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5).AlignRight().Text("Fiyat").Bold();
                });

                foreach (var c in calismalar.OrderBy(x => x.CalismaTarihi))
                {
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(c.CalismaTarihi.ToString("dd.MM.yyyy"));
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(c.Arac?.AktifPlaka ?? "-");
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(c.Guzergah?.GuzergahAdi ?? "-");
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(c.Guzergah?.Cari?.Unvan ?? "-");
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(c.Sofor?.TamAd ?? "-");
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).AlignRight().Text($"{c.Fiyat ?? 0:N0} ?");
                }
            });
        });
    }

    private void ComposeBelgeUyariContent(IContainer container, List<BelgeUyari> uyarilar)
    {
        container.Column(col =>
        {
            // �zet
            var kritik = uyarilar.Count(u => u.KalanGun < 0);
            var acil = uyarilar.Count(u => u.KalanGun >= 0 && u.KalanGun <= 7);
            var uyari = uyarilar.Count(u => u.KalanGun > 7 && u.KalanGun <= 30);

            col.Item().Row(row =>
            {
                row.RelativeItem().Background(Colors.Red.Lighten4).Padding(10).Column(c =>
                {
                    c.Item().Text("S�resi Ge�mi�").FontSize(9);
                    c.Item().Text(kritik.ToString()).Bold().FontSize(14).FontColor(Colors.Red.Medium);
                });
                row.ConstantItem(10);
                row.RelativeItem().Background(Colors.Orange.Lighten4).Padding(10).Column(c =>
                {
                    c.Item().Text("7 G�n ��inde").FontSize(9);
                    c.Item().Text(acil.ToString()).Bold().FontSize(14).FontColor(Colors.Orange.Medium);
                });
                row.ConstantItem(10);
                row.RelativeItem().Background(Colors.Blue.Lighten4).Padding(10).Column(c =>
                {
                    c.Item().Text("30 G�n ��inde").FontSize(9);
                    c.Item().Text(uyari.ToString()).Bold().FontSize(14).FontColor(Colors.Blue.Medium);
                });
                row.ConstantItem(10);
                row.RelativeItem().Background(Colors.Grey.Lighten3).Padding(10).Column(c =>
                {
                    c.Item().Text("Toplam").FontSize(9);
                    c.Item().Text(uyarilar.Count.ToString()).Bold().FontSize(14);
                });
            });

            col.Item().PaddingVertical(15);

            // Tablo
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2);   // Ad/Plaka
                    columns.RelativeColumn(2);   // Belge T�r�
                    columns.RelativeColumn(1);   // Biti� Tarihi
                    columns.RelativeColumn(1);   // Kalan G�n
                    columns.RelativeColumn(1);   // Durum
                });

                table.Header(header =>
                {
                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Ad / Plaka").Bold();
                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Belge T�r�").Bold();
                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Biti� Tarihi").Bold();
                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5).AlignCenter().Text("Kalan G�n").Bold();
                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Durum").Bold();
                });

                foreach (var u in uyarilar.OrderBy(x => x.KalanGun))
                {
                    var bgColor = u.KalanGun < 0 ? Colors.Red.Lighten4 : 
                                  u.KalanGun <= 7 ? Colors.Orange.Lighten4 : 
                                  Colors.White;

                    table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(u.Baslik);
                    table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(u.BelgeTuru);
                    table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(u.BitisTarihi.ToString("dd.MM.yyyy"));
                    table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).AlignCenter().Text(u.KalanGun < 0 ? $"{Math.Abs(u.KalanGun)} g�n ge�ti" : $"{u.KalanGun} g�n");
                    table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(u.Seviye switch
                    {
                        BelgeUyariSeviye.Kritik => "KR�T�K",
                        BelgeUyariSeviye.Acil => "AC�L",
                        BelgeUyariSeviye.Uyari => "UYARI",
                        _ => "B�LG�"
                    });
                }
            });
        });
    }

    private void ComposeCariEkstreContent(IContainer container, Cari cari, List<Fatura> faturalar, List<BankaKasaHareket> hareketler)
    {
        container.Column(col =>
        {
            // Cari Bilgileri
            col.Item().Border(1).Padding(10).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("Cari Kodu:").Bold();
                    c.Item().Text(cari.CariKodu);
                    c.Item().PaddingTop(5).Text("�nvan:").Bold();
                    c.Item().Text(cari.Unvan);
                });
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("Vergi No:").Bold();
                    c.Item().Text(cari.VergiNo ?? "-");
                    c.Item().PaddingTop(5).Text("Telefon:").Bold();
                    c.Item().Text(cari.Telefon ?? "-");
                });
            });

            col.Item().PaddingVertical(15);

            // Faturalar
            if (faturalar.Any())
            {
                col.Item().Text("FATURALAR").Bold().FontSize(12);
                col.Item().PaddingVertical(5);

                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(1);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Fatura No").Bold();
                        header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Tarih").Bold();
                        header.Cell().Background(Colors.Grey.Lighten2).Padding(5).AlignRight().Text("Tutar").Bold();
                        header.Cell().Background(Colors.Grey.Lighten2).Padding(5).AlignRight().Text("�denen").Bold();
                        header.Cell().Background(Colors.Grey.Lighten2).Padding(5).AlignRight().Text("Kalan").Bold();
                    });

                    foreach (var f in faturalar.OrderBy(x => x.FaturaTarihi))
                    {
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(f.FaturaNo);
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(f.FaturaTarihi.ToString("dd.MM.yyyy"));
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).AlignRight().Text($"{f.GenelToplam:N2} ?");
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).AlignRight().Text($"{f.OdenenTutar:N2} ?");
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).AlignRight().Text($"{f.KalanTutar:N2} ?");
                    }

                    // Toplam
                    table.Cell().ColumnSpan(2).Background(Colors.Grey.Lighten3).Padding(5).Text("TOPLAM").Bold();
                    table.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text($"{faturalar.Sum(f => f.GenelToplam):N2} ?").Bold();
                    table.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text($"{faturalar.Sum(f => f.OdenenTutar):N2} ?").Bold();
                    table.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text($"{faturalar.Sum(f => f.KalanTutar):N2} ?").Bold();
                });
            }
        });
    }

    public byte[] GenerateMutabakatPdf(MutabakatPdfModel model)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(c => ComposeHeader(c, $"CARİ MUTABAKAT\n{model.CariUnvan}"));
                page.Content().Element(c => ComposeMutabakatContent(c, model));
                page.Footer().Element(ComposeFooter);
            });
        });

        return document.GeneratePdf();
    }

    private void ComposeMutabakatContent(IContainer container, MutabakatPdfModel model)
    {
        container.Column(col =>
        {
            // Cari bilgileri
            col.Item().Border(1).Padding(10).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("Cari Kodu:").Bold();
                    c.Item().Text(model.CariKodu);
                    c.Item().PaddingTop(5).Text("Cari Ünvan:").Bold();
                    c.Item().Text(model.CariUnvan);
                    c.Item().PaddingTop(5).Text("Vergi No:").Bold();
                    c.Item().Text(string.IsNullOrWhiteSpace(model.VergiNo) ? "-" : model.VergiNo);
                });

                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("Dönem:").Bold();
                    c.Item().Text($"{model.BaslangicTarihi:dd.MM.yyyy} - {model.BitisTarihi:dd.MM.yyyy}");
                    c.Item().PaddingTop(5).Text("Dönem Başı Bakiye:").Bold();
                    c.Item().Text($"{model.DonemBasiBakiye:N2} TL");
                    c.Item().PaddingTop(5).Text("Dönem Sonu Bakiye:").Bold();
                    c.Item().Text($"{model.DonemSonuBakiye:N2} TL").Bold();
                });
            });

            col.Item().PaddingVertical(15);

            // Hareketler tablosu
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(3);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1);
                });

                table.Header(header =>
                {
                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Tarih").Bold();
                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Belge No").Bold();
                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Açıklama").Bold();
                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5).AlignRight().Text("Borç").Bold();
                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5).AlignRight().Text("Alacak").Bold();
                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5).AlignRight().Text("Bakiye").Bold();
                });

                foreach (var h in model.Hareketler)
                {
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(h.Tarih.ToString("dd.MM.yyyy"));
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(h.BelgeNo);
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(h.Aciklama);
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).AlignRight().Text($"{h.Borc:N2}");
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).AlignRight().Text($"{h.Alacak:N2}");
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).AlignRight().Text($"{h.Bakiye:N2}");
                }

                // Toplam
                table.Cell().ColumnSpan(3).Background(Colors.Grey.Lighten3).Padding(5).Text("TOPLAM").Bold();
                table.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text($"{model.ToplamBorc:N2}").Bold();
                table.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text($"{model.ToplamAlacak:N2}").Bold();
                table.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text($"{model.DonemSonuBakiye:N2}").Bold();
            });

            col.Item().PaddingTop(30).Row(row =>
            {
                row.RelativeItem().Border(1).Padding(15).Column(c =>
                {
                    c.Item().Text("Mutabakat Eden").Bold();
                    c.Item().PaddingTop(40).Text("İmza / Kaşe").FontSize(9).FontColor(Colors.Grey.Medium);
                });
                row.ConstantItem(20);
                row.RelativeItem().Border(1).Padding(15).Column(c =>
                {
                    c.Item().Text("Mutabık Kalınan").Bold();
                    c.Item().PaddingTop(40).Text("İmza / Kaşe").FontSize(9).FontColor(Colors.Grey.Medium);
                });
            });
        });
    }
}
