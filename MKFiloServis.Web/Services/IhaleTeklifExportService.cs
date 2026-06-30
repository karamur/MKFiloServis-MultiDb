using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using MKFiloServis.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace MKFiloServis.Web.Services;

public class IhaleTeklifExportService : IIhaleTeklifExportService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IExcelService _excelService;
    private readonly IKullaniciService _kullaniciService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<IhaleTeklifExportService> _logger;

    public IhaleTeklifExportService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IExcelService excelService,
        IKullaniciService kullaniciService,
        IAuditLogService auditLogService,
        ILogger<IhaleTeklifExportService> logger)
    {
        _contextFactory = contextFactory;
        _excelService = excelService;
        _kullaniciService = kullaniciService;
        _auditLogService = auditLogService;
        _logger = logger;

        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<byte[]> ExportPdfAsync(int versiyonId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var (versiyon, proje, kalemler) = await GetExportDataAsync(context, versiyonId);
        await ValidateExportAsync(context, versiyon);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(24);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Text($"İhale Teklif Özeti - {proje.ProjeAdi}").Bold().FontSize(16);
                    col.Item().Text($"Proje Kodu: {proje.ProjeKodu} | Versiyon: {versiyon.RevizyonKodu} | Durum: {versiyon.Durum}").FontSize(10).FontColor(Colors.Grey.Darken1);
                });

                page.Content().Column(col =>
                {
                    col.Spacing(12);

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Background(Colors.Grey.Lighten3).Padding(8).Column(c =>
                        {
                            c.Item().Text("Toplam Maliyet").FontSize(9);
                            c.Item().Text($"{versiyon.ToplamMaliyet:N0} ₺").Bold().FontSize(14);
                        });
                        row.ConstantItem(8);
                        row.RelativeItem().Background(Colors.Grey.Lighten3).Padding(8).Column(c =>
                        {
                            c.Item().Text("Teklif Tutarı").FontSize(9);
                            c.Item().Text($"{versiyon.TeklifTutari:N0} ₺").Bold().FontSize(14);
                        });
                        row.ConstantItem(8);
                        row.RelativeItem().Background(Colors.Grey.Lighten3).Padding(8).Column(c =>
                        {
                            c.Item().Text("Kâr Marjı").FontSize(9);
                            c.Item().Text($"{versiyon.KarMarjiTutari:N0} ₺ (%{versiyon.KarMarjiOrani:N1})").Bold().FontSize(14);
                        });
                    });

                    if (!string.IsNullOrWhiteSpace(versiyon.RevizyonNotu))
                    {
                        col.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Column(c =>
                        {
                            c.Item().Text("Revizyon Notu").Bold();
                            c.Item().Text(versiyon.RevizyonNotu);
                        });
                    }

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Hat").Bold();
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(5).AlignRight().Text("Aylık Maliyet").Bold();
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(5).AlignRight().Text("Aylık Teklif").Bold();
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(5).AlignRight().Text("Kâr %").Bold();
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Sahiplik").Bold();
                        });

                        foreach (var kalem in kalemler)
                        {
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(4).Text(kalem.HatAdi);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(4).AlignRight().Text($"{kalem.AylikMaliyet:N0}");
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(4).AlignRight().Text($"{kalem.AylikTeklifFiyati:N0}");
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(4).AlignRight().Text($"%{kalem.KarMarjiOrani:N1}");
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(4).Text(kalem.SahiplikDurumu.ToString());
                        }
                    });
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Sayfa ");
                    text.CurrentPageNumber();
                    text.Span(" / ");
                    text.TotalPages();
                });
            });
        });

        await TryAuditAsync(context, versiyon.Id, "PDF");
        return document.GeneratePdf();
    }

    public async Task<byte[]> ExportExcelAsync(int versiyonId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var (versiyon, proje, kalemler) = await GetExportDataAsync(context, versiyonId);
        await ValidateExportAsync(context, versiyon);

        var headers = new[]
        {
            "Versiyon", "Hat", "Sahiplik", "Mesafe Km", "Günlük Sefer", "Aylık Gün", "Aylık Maliyet", "Aylık Teklif", "Kâr Tutarı", "Kâr %"
        };

        var rows = kalemler
            .Select(k => new object[]
            {
                versiyon.RevizyonKodu,
                k.HatAdi,
                k.SahiplikDurumu.ToString(),
                k.MesafeKm,
                k.GunlukSeferSayisi,
                k.AylikSeferGunu,
                k.AylikMaliyet,
                k.AylikTeklifFiyati,
                k.AylikKarTutari,
                k.KarMarjiOrani
            })
            .ToList();

        rows.Add(new object[]
        {
            versiyon.RevizyonKodu,
            "TOPLAM",
            string.Empty,
            0m,
            0,
            0,
            versiyon.ToplamMaliyet,
            versiyon.TeklifTutari,
            versiyon.KarMarjiTutari,
            versiyon.KarMarjiOrani
        });

        await TryAuditAsync(context, versiyon.Id, "Excel");
        return _excelService.CreateExcel(headers, rows, SanitizeSheetName($"{proje.ProjeKodu}-{versiyon.RevizyonKodu}"));
    }

    private async Task<(IhaleTeklifVersiyon versiyon, IhaleProje proje, List<IhaleGuzergahKalem> kalemler)> GetExportDataAsync(ApplicationDbContext context, int versiyonId)
    {
        var versiyon = await context.IhaleTeklifVersiyonlari
            .FirstOrDefaultAsync(x => x.Id == versiyonId);

        if (versiyon == null)
            throw new InvalidOperationException("Teklif versiyonu bulunamadı.");

        var proje = await context.IhaleProjeleri
            .Include(p => p.Cari)
            .Include(p => p.Firma)
            .FirstOrDefaultAsync(p => p.Id == versiyon.IhaleProjeId);

        if (proje == null)
            throw new InvalidOperationException("İhale projesi bulunamadı.");

        var kalemler = await context.IhaleGuzergahKalemleri
            .Where(k => k.IhaleProjeId == proje.Id)
            .OrderBy(k => k.HatAdi)
            .ToListAsync();

        return (versiyon, proje, kalemler);
    }

    private async Task ValidateExportAsync(ApplicationDbContext context, IhaleTeklifVersiyon versiyon)
    {
        var kullanici = await _kullaniciService.GetAktifKullaniciAsync();
        var rolAdi = kullanici?.Rol?.RolAdi;
        var yetkili = string.Equals(rolAdi, "Admin", StringComparison.OrdinalIgnoreCase)
            || string.Equals(rolAdi, "Yönetici", StringComparison.OrdinalIgnoreCase)
            || string.Equals(rolAdi, "Yonetici", StringComparison.OrdinalIgnoreCase);

        if (versiyon.Durum != IhaleTeklifVersiyonDurum.Onaylandi && !yetkili)
            throw new InvalidOperationException("Sadece onaylı teklifler export edilebilir.");
    }

    private async Task TryAuditAsync(ApplicationDbContext context, int versiyonId, string format)
    {
        try
        {
            await _auditLogService.LogExportAsync(nameof(IhaleTeklifVersiyon), 1, format, $"Teklif export oluşturuldu. VersiyonId: {versiyonId}");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Teklif export audit kaydı oluşturulamadı. VersiyonId: {VersiyonId}", versiyonId);
        }
    }

    private static string SanitizeSheetName(string value)
    {
        var invalidChars = new[] { '\\', '/', '?', '*', '[', ']', ':' };
        foreach (var invalid in invalidChars)
            value = value.Replace(invalid, '-');

        return value.Length > 31 ? value[..31] : value;
    }
}


