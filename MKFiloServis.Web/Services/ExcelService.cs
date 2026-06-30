using ClosedXML.Excel;
using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Models;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Web.Services;

public class ExcelService : IExcelService
{
    public byte[] ExportToExcel<T>(List<T> data, string sheetName = "Rapor")
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add(sheetName);

        // Header ve data ekle
        worksheet.Cell(1, 1).InsertTable(data);

        // Stil ayarları
        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public byte[] ExportSoforPerformansRaporu(SoforPerformansOzet data)
    {
        var headers = new[] { "Dönem", "Sefer Sayısı", "Çalışılan Gün", "Toplam Kazanç", "Ort. Günlük Kazanç" };
        var rows = data.AylikPerformans
            .Select(a => new object[]
            {
                a.AyAdi,
                a.SeferSayisi,
                a.CalistigiGun,
                a.ToplamKazanc,
                a.CalistigiGun > 0 ? a.ToplamKazanc / a.CalistigiGun : 0m
            })
            .ToList();

        return CreateExcel(headers, rows, string.IsNullOrWhiteSpace(data.SoforAdi) ? "Şoför Performans" : data.SoforAdi);
    }

    public byte[] ExportSoforKarsilastirmaRaporu(List<SoforKarsilastirmaOzeti> data)
    {
        var headers = new[] { "Sıra", "Şoför Adı", "Sefer Sayısı", "Çalıştığı Gün", "Toplam Kazanç", "Arıza Oranı %" };
        var rows = data
            .Select((k, i) => new object[]
            {
                i + 1,
                k.SoforAdi,
                k.SeferSayisi,
                k.CalistigiGun,
                k.ToplamKazanc,
                k.ArizaOrani
            })
            .ToList();

        return CreateExcel(headers, rows, "Şoför Karşılaştırma");
    }

    public byte[] ExportAracKarlilikRaporu(AracKarlilikOzet data)
    {
        var headers = new[]
        {
            "Plaka", "Marka", "Model", "Sahiplik", "Sefer Sayısı", "Toplam Gelir", "Toplam Masraf",
            "Kira Bedeli", "Komisyon", "Toplam Gider", "Net Kar", "Kar Marjı %", "Arıza Sayısı", "Arıza Oranı %"
        };

        var rows = new List<object[]>
        {
            new object[]
            {
                data.Plaka,
                data.Marka ?? string.Empty,
                data.Model ?? string.Empty,
                data.SahiplikTipi,
                data.ToplamSeferSayisi,
                data.ToplamGelir,
                data.ToplamMasraf,
                data.KiraBedeli,
                data.KomisyonTutari,
                data.ToplamGider,
                data.NetKar,
                data.KarMarji,
                data.ArizaSayisi,
                data.ArizaOrani
            }
        };

        return CreateExcel(headers, rows, "Araç Karlılık");
    }

    public byte[] ExportAracKarlilikKarsilastirmaRaporu(List<AracKarsilastirmaOzeti> data)
    {
        var headers = new[]
        {
            "Sıra", "Plaka", "Marka/Model", "Sahiplik", "Sefer Sayısı", "Toplam Gelir",
            "Toplam Gider", "Net Kar", "Kar Marjı %", "Arıza Sayısı", "Arıza Oranı %"
        };

        var rows = data
            .Select(k => new object[]
            {
                k.KarlilikSirasi,
                k.Plaka,
                k.MarkaModel ?? string.Empty,
                k.SahiplikTipi,
                k.SeferSayisi,
                k.ToplamGelir,
                k.ToplamGider,
                k.NetKar,
                k.KarMarji,
                k.ArizaSayisi,
                k.ArizaOrani
            })
            .ToList();

        return CreateExcel(headers, rows, "Araç Karşılaştırma");
    }

    public byte[] ExportServisCalismaRaporu(List<ServisCalismaRaporItem> data)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Servis Çalışma Raporu");

        // Başlıklar
        var headers = new[] { "Firma", "Güzergah", "Plaka", "Şoför", "Servis Türü", "Birim Fiyat", "Çalışılan Gün", "Toplam Tutar" };
        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cell(1, i + 1).Value = headers[i];
        }

        // Header stilini ayarla
        var headerRange = worksheet.Range(1, 1, 1, headers.Length);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;
        headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

        // Veriler
        int row = 2;
        foreach (var item in data)
        {
            worksheet.Cell(row, 1).Value = item.FirmaAdi;
            worksheet.Cell(row, 2).Value = item.GuzergahAdi;
            worksheet.Cell(row, 3).Value = item.Plaka;
            worksheet.Cell(row, 4).Value = item.SoforAdi;
            worksheet.Cell(row, 5).Value = item.ServisTuru;
            worksheet.Cell(row, 6).Value = item.BirimFiyat;
            worksheet.Cell(row, 7).Value = item.CalisilanGun;
            worksheet.Cell(row, 8).Value = item.ToplamTutar;
            row++;
        }

        // Toplam satırı
        worksheet.Cell(row, 6).Value = "TOPLAM:";
        worksheet.Cell(row, 6).Style.Font.Bold = true;
        worksheet.Cell(row, 7).FormulaA1 = $"SUM(G2:G{row - 1})";
        worksheet.Cell(row, 8).FormulaA1 = $"SUM(H2:H{row - 1})";
        worksheet.Cell(row, 7).Style.Font.Bold = true;
        worksheet.Cell(row, 8).Style.Font.Bold = true;

        // Para formatı
        worksheet.Column(6).Style.NumberFormat.Format = "#,##0.00 ?";
        worksheet.Column(8).Style.NumberFormat.Format = "#,##0.00 ?";

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public byte[] ExportFaturaOdemeRaporu(List<FaturaOdemeRaporItem> data)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Fatura Ödeme Raporu");

        // Başlıklar
        var headers = new[] { "Fatura No", "Fatura Tarihi", "Vade Tarihi", "Cari", "Fatura Tipi", "Durum", "Genel Toplam", "Ödenen", "Kalan", "Vade Günü" };
        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cell(1, i + 1).Value = headers[i];
        }

        var headerRange = worksheet.Range(1, 1, 1, headers.Length);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGreen;
        headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

        // Veriler
        int row = 2;
        foreach (var item in data)
        {
            worksheet.Cell(row, 1).Value = item.FaturaNo;
            worksheet.Cell(row, 2).Value = item.FaturaTarihi.ToString("dd.MM.yyyy");
            worksheet.Cell(row, 3).Value = item.VadeTarihi?.ToString("dd.MM.yyyy") ?? "-";
            worksheet.Cell(row, 4).Value = item.CariUnvan;
            worksheet.Cell(row, 5).Value = item.FaturaTipi;
            worksheet.Cell(row, 6).Value = item.Durum;
            worksheet.Cell(row, 7).Value = item.GenelToplam;
            worksheet.Cell(row, 8).Value = item.OdenenTutar;
            worksheet.Cell(row, 9).Value = item.KalanTutar;
            worksheet.Cell(row, 10).Value = item.VadeGunu;

            // Gecikmiş ödemeleri kırmızı yap
            if (item.VadeGunu < 0 && item.KalanTutar > 0)
            {
                worksheet.Row(row).Style.Fill.BackgroundColor = XLColor.LightPink;
            }
            row++;
        }

        // Toplam satırı
        worksheet.Cell(row, 6).Value = "TOPLAM:";
        worksheet.Cell(row, 6).Style.Font.Bold = true;
        worksheet.Cell(row, 7).FormulaA1 = $"SUM(G2:G{row - 1})";
        worksheet.Cell(row, 8).FormulaA1 = $"SUM(H2:H{row - 1})";
        worksheet.Cell(row, 9).FormulaA1 = $"SUM(I2:I{row - 1})";

        // Para formatı
        worksheet.Column(7).Style.NumberFormat.Format = "#,##0.00 ?";
        worksheet.Column(8).Style.NumberFormat.Format = "#,##0.00 ?";
        worksheet.Column(9).Style.NumberFormat.Format = "#,##0.00 ?";

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public byte[] ExportAracMasrafRaporu(List<AracMasrafRaporItem> data)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Araç Masraf Raporu");

        // Başlıklar
        var headers = new[] { "Tarih", "Plaka", "Masraf Kalemi", "Kategori", "Güzergah", "Tutar", "Belge No", "Açıklama", "Arıza Kaynaklı" };
        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cell(1, i + 1).Value = headers[i];
        }

        var headerRange = worksheet.Range(1, 1, 1, headers.Length);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightCoral;
        headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

        // Veriler
        int row = 2;
        foreach (var item in data)
        {
            worksheet.Cell(row, 1).Value = item.MasrafTarihi.ToString("dd.MM.yyyy");
            worksheet.Cell(row, 2).Value = item.Plaka;
            worksheet.Cell(row, 3).Value = item.MasrafKalemi;
            worksheet.Cell(row, 4).Value = item.Kategori;
            worksheet.Cell(row, 5).Value = item.GuzergahAdi ?? "-";
            worksheet.Cell(row, 6).Value = item.Tutar;
            worksheet.Cell(row, 7).Value = item.BelgeNo ?? "-";
            worksheet.Cell(row, 8).Value = item.Aciklama ?? "-";
            worksheet.Cell(row, 9).Value = item.ArizaKaynakli ? "Evet" : "Hayır";
            row++;
        }

        // Toplam satırı
        worksheet.Cell(row, 5).Value = "TOPLAM:";
        worksheet.Cell(row, 5).Style.Font.Bold = true;
        worksheet.Cell(row, 6).FormulaA1 = $"SUM(F2:F{row - 1})";
        worksheet.Cell(row, 6).Style.Font.Bold = true;

        // Para formatı
        worksheet.Column(6).Style.NumberFormat.Format = "#,##0.00 ?";

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public byte[] ExportCariler(List<Cari> data)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Cariler");

        var headers = new[] { "Cari Kodu", "Ünvan", "Tip", "Vergi Dairesi", "Vergi No", "Telefon", "Email", "Yetkili" };
        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cell(1, i + 1).Value = headers[i];
        }

        var headerRange = worksheet.Range(1, 1, 1, headers.Length);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;

        int row = 2;
        foreach (var item in data)
        {
            worksheet.Cell(row, 1).Value = item.CariKodu;
            worksheet.Cell(row, 2).Value = item.Unvan;
            worksheet.Cell(row, 3).Value = item.CariTipi.ToString();
            worksheet.Cell(row, 4).Value = item.VergiDairesi ?? "-";
            worksheet.Cell(row, 5).Value = item.VergiNo ?? "-";
            worksheet.Cell(row, 6).Value = item.Telefon ?? "-";
            worksheet.Cell(row, 7).Value = item.Email ?? "-";
            worksheet.Cell(row, 8).Value = item.YetkiliKisi ?? "-";
            row++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public byte[] ExportAraclar(List<Arac> data)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Araçlar");

        var headers = new[] { "Plaka", "Marka", "Model", "Yıl", "Tip", "Sahiplik", "Koltuk", "Muayene", "Kasko", "Trafik Sig.", "Durum" };
        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cell(1, i + 1).Value = headers[i];
        }

        var headerRange = worksheet.Range(1, 1, 1, headers.Length);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGreen;

        int row = 2;
        foreach (var item in data)
        {
            worksheet.Cell(row, 1).Value = item.AktifPlaka ?? item.SaseNo;
            worksheet.Cell(row, 2).Value = item.Marka ?? "-";
            worksheet.Cell(row, 3).Value = item.Model ?? "-";
            worksheet.Cell(row, 4).Value = item.ModelYili?.ToString() ?? "-";
            worksheet.Cell(row, 5).Value = item.AracTipi.ToString();
            worksheet.Cell(row, 6).Value = item.SahiplikTipi.ToString();
            worksheet.Cell(row, 7).Value = item.KoltukSayisi;
            worksheet.Cell(row, 8).Value = item.MuayeneBitisTarihi?.ToString("dd.MM.yyyy") ?? "-";
            worksheet.Cell(row, 9).Value = item.KaskoBitisTarihi?.ToString("dd.MM.yyyy") ?? "-";
            worksheet.Cell(row, 10).Value = item.TrafikSigortaBitisTarihi?.ToString("dd.MM.yyyy") ?? "-";
            worksheet.Cell(row, 11).Value = item.Aktif ? "Aktif" : "Pasif";
            row++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public byte[] ExportPersonel(List<Sofor> data)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Personel");

        var headers = new[] { "Kod", "Ad Soyad", "Görev", "TC Kimlik", "Telefon", "Email", "İşe Başlama", "Ehliyet Bitiş", "SRC Bitiş", "Net Maaş", "Durum" };
        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cell(1, i + 1).Value = headers[i];
        }

        var headerRange = worksheet.Range(1, 1, 1, headers.Length);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightYellow;

        int row = 2;
        foreach (var item in data)
        {
            worksheet.Cell(row, 1).Value = item.SoforKodu;
            worksheet.Cell(row, 2).Value = item.TamAd;
            worksheet.Cell(row, 3).Value = GetGorevAdi(item.Gorev);
            worksheet.Cell(row, 4).Value = item.TcKimlikNo ?? "-";
            worksheet.Cell(row, 5).Value = item.Telefon ?? "-";
            worksheet.Cell(row, 6).Value = item.Email ?? "-";
            worksheet.Cell(row, 7).Value = item.IseBaslamaTarihi?.ToString("dd.MM.yyyy") ?? "-";
            worksheet.Cell(row, 8).Value = item.EhliyetGecerlilikTarihi?.ToString("dd.MM.yyyy") ?? "-";
            worksheet.Cell(row, 9).Value = item.SrcBelgesiGecerlilikTarihi?.ToString("dd.MM.yyyy") ?? "-";
            worksheet.Cell(row, 10).Value = item.NetMaas;
            worksheet.Cell(row, 11).Value = item.Aktif ? "Aktif" : "Pasif";
            row++;
        }

        worksheet.Column(10).Style.NumberFormat.Format = "#,##0.00 ?";
        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public byte[] ExportBelgeUyarilari(List<BelgeUyari> data)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Belge Uyarıları");

        var headers = new[] { "Ad / Plaka", "Belge Türü", "Bitiş Tarihi", "Kalan Gün", "Durum" };
        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cell(1, i + 1).Value = headers[i];
        }

        var headerRange = worksheet.Range(1, 1, 1, headers.Length);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightCoral;

        int row = 2;
        foreach (var item in data)
        {
            worksheet.Cell(row, 1).Value = item.Baslik;
            worksheet.Cell(row, 2).Value = item.BelgeTuru;
            worksheet.Cell(row, 3).Value = item.BitisTarihi.ToString("dd.MM.yyyy");
            worksheet.Cell(row, 4).Value = item.KalanGun;
            worksheet.Cell(row, 5).Value = item.Seviye switch
            {
                BelgeUyariSeviye.Kritik => "Süresi Geçmiş",
                BelgeUyariSeviye.Acil => "Acil (7 gün)",
                BelgeUyariSeviye.Uyari => "Uyarı (30 gün)",
                _ => "Bilgi"
            };

            if (item.KalanGun < 0)
                worksheet.Row(row).Style.Fill.BackgroundColor = XLColor.LightPink;
            else if (item.KalanGun <= 7)
                worksheet.Row(row).Style.Fill.BackgroundColor = XLColor.LightYellow;
            
            row++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public byte[] ExportAracPerformans(List<AracPerformansData> data, int yil, int ay)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add($"Araç Performans {ay:D2}-{yil}");

        var headers = new[] { "Plaka", "Sefer Sayısı", "Toplam Ciro", "Toplam Masraf", "Net Kar" };
        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cell(1, i + 1).Value = headers[i];
        }

        var headerRange = worksheet.Range(1, 1, 1, headers.Length);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGreen;

        int row = 2;
        foreach (var item in data)
        {
            worksheet.Cell(row, 1).Value = item.Plaka;
            worksheet.Cell(row, 2).Value = item.SeferSayisi;
            worksheet.Cell(row, 3).Value = item.ToplamCiro;
            worksheet.Cell(row, 4).Value = item.ToplamMasraf;
            worksheet.Cell(row, 5).Value = item.NetKar;

            if (item.NetKar < 0)
                worksheet.Cell(row, 5).Style.Font.FontColor = XLColor.Red;
            else
                worksheet.Cell(row, 5).Style.Font.FontColor = XLColor.Green;
            
            row++;
        }

        // Toplam satırı
        worksheet.Cell(row, 1).Value = "TOPLAM";
        worksheet.Cell(row, 1).Style.Font.Bold = true;
        worksheet.Cell(row, 2).FormulaA1 = $"SUM(B2:B{row - 1})";
        worksheet.Cell(row, 3).FormulaA1 = $"SUM(C2:C{row - 1})";
        worksheet.Cell(row, 4).FormulaA1 = $"SUM(D2:D{row - 1})";
        worksheet.Cell(row, 5).FormulaA1 = $"SUM(E2:E{row - 1})";
        worksheet.Row(row).Style.Font.Bold = true;

        worksheet.Column(3).Style.NumberFormat.Format = "#,##0.00 ?";
        worksheet.Column(4).Style.NumberFormat.Format = "#,##0.00 ?";
        worksheet.Column(5).Style.NumberFormat.Format = "#,##0.00 ?";
        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public byte[] ExportCariPerformans(List<CariPerformansData> data, int yil, int ay)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add($"Müşteri Performans {ay:D2}-{yil}");

        var headers = new[] { "Müşteri", "Fatura Sayısı", "Toplam Ciro", "Ödenen Tutar", "Kalan Bakiye" };
        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cell(1, i + 1).Value = headers[i];
        }

        var headerRange = worksheet.Range(1, 1, 1, headers.Length);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;

        int row = 2;
        foreach (var item in data)
        {
            worksheet.Cell(row, 1).Value = item.CariUnvan;
            worksheet.Cell(row, 2).Value = item.SeferSayisi;
            worksheet.Cell(row, 3).Value = item.ToplamCiro;
            worksheet.Cell(row, 4).Value = item.OdenenTutar;
            worksheet.Cell(row, 5).Value = item.KalanBakiye;

            if (item.KalanBakiye > 0)
                worksheet.Cell(row, 5).Style.Font.FontColor = XLColor.Red;
            
            row++;
        }

        // Toplam satırı
        worksheet.Cell(row, 1).Value = "TOPLAM";
        worksheet.Cell(row, 1).Style.Font.Bold = true;
        worksheet.Cell(row, 2).FormulaA1 = $"SUM(B2:B{row - 1})";
        worksheet.Cell(row, 3).FormulaA1 = $"SUM(C2:C{row - 1})";
        worksheet.Cell(row, 4).FormulaA1 = $"SUM(D2:D{row - 1})";
        worksheet.Cell(row, 5).FormulaA1 = $"SUM(E2:E{row - 1})";
        worksheet.Row(row).Style.Font.Bold = true;

        worksheet.Column(3).Style.NumberFormat.Format = "#,##0.00 ?";
        worksheet.Column(4).Style.NumberFormat.Format = "#,##0.00 ?";
        worksheet.Column(5).Style.NumberFormat.Format = "#,##0.00 ?";
        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private string GetGorevAdi(PersonelGorev gorev)
    {
        return gorev switch
        {
            PersonelGorev.Sofor => "Sofor",
            PersonelGorev.OfisCalisani => "Ofis Calisani",
            PersonelGorev.Muhasebe => "Muhasebe",
            PersonelGorev.Yonetici => "Yonetici",
            PersonelGorev.Teknik => "Teknik",
            PersonelGorev.Diger => "Diger",
            _ => gorev.ToString()
        };
    }

    public byte[] CreateExcel(string[] headers, List<object[]> data, string sheetName = "Rapor")
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add(sheetName);

        // Basliklar
        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cell(1, i + 1).Value = headers[i];
        }

        // Header stili
        var headerRange = worksheet.Range(1, 1, 1, headers.Length);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;
        headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

        // Veriler
        int row = 2;
        foreach (var rowData in data)
        {
            for (int col = 0; col < rowData.Length; col++)
            {
                var value = rowData[col];
                if (value is decimal decimalVal)
                    worksheet.Cell(row, col + 1).Value = decimalVal;
                else if (value is int intVal)
                    worksheet.Cell(row, col + 1).Value = intVal;
                else if (value is double doubleVal)
                    worksheet.Cell(row, col + 1).Value = doubleVal;
                else
                    worksheet.Cell(row, col + 1).Value = value?.ToString() ?? "";
            }
            row++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}



