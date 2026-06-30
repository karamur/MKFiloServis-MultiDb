using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Web.Services;

public class AylikOdemeService : IAylikOdemeService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IFirmaService _firmaService;

    public AylikOdemeService(IDbContextFactory<ApplicationDbContext> contextFactory, IFirmaService firmaService)
    {
        _contextFactory = contextFactory;
        _firmaService = firmaService;
    }

    public async Task<List<AylikOdemePlani>> GetTumPlanlariAsync(int firmaId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.AylikOdemePlanlari
            .Include(p => p.Cari)
            .Include(p => p.BankaHesap)
            .Include(p => p.MasrafKalemi)
            .Where(p => p.FirmaId == firmaId && !p.IsDeleted)
            .OrderBy(p => p.OdemeGunu)
            .ToListAsync();
    }

    public async Task<List<AylikOdemePlani>> GetAktifPlanlariAsync(int firmaId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var bugun = DateTime.Today;

        return await context.AylikOdemePlanlari
            .Include(p => p.Cari)
            .Include(p => p.BankaHesap)
            .Include(p => p.MasrafKalemi)
            .Where(p => p.FirmaId == firmaId && 
                       p.Aktif && 
                       !p.IsDeleted &&
                       p.BaslangicTarihi <= bugun &&
                       (!p.BitisTarihi.HasValue || p.BitisTarihi.Value >= bugun))
            .OrderBy(p => p.OdemeGunu)
            .ToListAsync();
    }

    public async Task<AylikOdemePlani?> GetPlanByIdAsync(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.AylikOdemePlanlari
            .Include(p => p.Cari)
            .Include(p => p.BankaHesap)
            .Include(p => p.MasrafKalemi)
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
    }

    public async Task<AylikOdemePlani> CreatePlanAsync(AylikOdemePlani plan)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        plan.CreatedAt = DateTime.UtcNow;
        context.AylikOdemePlanlari.Add(plan);
        await context.SaveChangesAsync();
        return plan;
    }

    public async Task<AylikOdemePlani> UpdatePlanAsync(AylikOdemePlani plan)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        plan.UpdatedAt = DateTime.UtcNow;
        context.AylikOdemePlanlari.Update(plan);
        await context.SaveChangesAsync();
        return plan;
    }

    public async Task DeletePlanAsync(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var plan = await context.AylikOdemePlanlari.FindAsync(id);
        if (plan != null)
        {
            plan.IsDeleted = true;
            plan.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }
    }

    public async Task<List<AylikOdemeGerceklesen>> GetAylikOdemeleriAsync(int firmaId, int yil, int ay)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.AylikOdemeGerceklesenler
            .Include(o => o.Plan)
            .Include(o => o.BankaKasaHareket)
            .Where(o => o.FirmaId == firmaId && o.Yil == yil && o.Ay == ay && !o.IsDeleted)
            .OrderBy(o => o.Plan!.OdemeGunu)
            .ToListAsync();
    }

    public async Task<List<AylikOdemeGerceklesen>> GetBekleyenOdemeleriAsync(int firmaId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.AylikOdemeGerceklesenler
            .Include(o => o.Plan)
            .Where(o => o.FirmaId == firmaId && 
                       o.Durum == OdemeDurumu.Bekleniyor && 
                       !o.IsDeleted)
            .OrderBy(o => o.Yil).ThenBy(o => o.Ay).ThenBy(o => o.Plan!.OdemeGunu)
            .ToListAsync();
    }

    public async Task<List<AylikOdemeGerceklesen>> GetGecikmiOdemeleriAsync(int firmaId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var bugun = DateTime.Today;
        
        var veriler = await context.AylikOdemeGerceklesenler
            .Include(o => o.Plan)
            .Where(o => o.FirmaId == firmaId && 
                       o.Durum == OdemeDurumu.Bekleniyor &&
                       !o.IsDeleted)
            .ToListAsync();
            
        return veriler
            .Where(o => new DateTime(o.Yil, o.Ay, o.Plan?.OdemeGunu ?? 1) < bugun)
            .OrderBy(o => o.Yil).ThenBy(o => o.Ay)
            .ToList();
    }

    public async Task<AylikOdemeGerceklesen?> GetGerceklesenByIdAsync(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.AylikOdemeGerceklesenler
            .Include(o => o.Plan)
            .Include(o => o.BankaKasaHareket)
            .FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted);
    }

    public async Task<AylikOdemeGerceklesen> OdemeKaydetAsync(int planId, int yil, int ay, decimal tutar, DateTime? odemeTarihi)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        var plan = await context.AylikOdemePlanlari.FindAsync(planId);
        if (plan == null) throw new Exception("Plan bulunamad�");

        var gerceklesen = await context.AylikOdemeGerceklesenler
            .FirstOrDefaultAsync(o => o.AylikOdemePlaniId == planId && o.Yil == yil && o.Ay == ay);

        if (gerceklesen == null)
        {
            gerceklesen = new AylikOdemeGerceklesen
            {
                AylikOdemePlaniId = planId,
                FirmaId = plan.FirmaId,
                Yil = yil,
                Ay = ay,
                PlanlananTutar = plan.AylikTutar,
                OdenenTutar = 0,
                Durum = OdemeDurumu.Bekleniyor,
                CreatedAt = DateTime.UtcNow
            };
            context.AylikOdemeGerceklesenler.Add(gerceklesen);
        }

        gerceklesen.OdenenTutar += tutar;
        gerceklesen.OdemeTarihi = odemeTarihi ?? DateTime.Now;
        gerceklesen.Durum = gerceklesen.OdenenTutar >= gerceklesen.PlanlananTutar 
            ? OdemeDurumu.Odendi 
            : OdemeDurumu.KismiOdendi;
        gerceklesen.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
        return gerceklesen;
    }

    public async Task<AylikOdemeGerceklesen> OdemeDurumGuncelleAsync(int id, OdemeDurumu durum)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var gerceklesen = await context.AylikOdemeGerceklesenler.FindAsync(id);

        if (gerceklesen == null) throw new Exception("�deme bulunamad�");

        gerceklesen.Durum = durum;
        gerceklesen.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        return gerceklesen;
    }

    public async Task<List<AylikOdemeGerceklesen>> GetTakvimOdemeleriAsync(int firmaId, int yil, int ay)
    {
        // �nce otomatik kay�tlar� olu�tur
        await OtomatikOdemeKayitlariOlusturAsync(firmaId, yil, ay);

        // Sonra listeyi getir
        return await GetAylikOdemeleriAsync(firmaId, yil, ay);
    }

    public async Task OtomatikOdemeKayitlariOlusturAsync(int firmaId, int yil, int ay)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        var aktifPlanlar = await GetAktifPlanlariAsync(firmaId);
        var tarih = new DateTime(yil, ay, 1);

        foreach (var plan in aktifPlanlar.Where(p => p.OtomatikKayitOlustur))
        {
            // Bu ay i�in kay�t var m� kontrol et
            var mevcutKayit = await context.AylikOdemeGerceklesenler
                .AnyAsync(o => o.AylikOdemePlaniId == plan.Id && o.Yil == yil && o.Ay == ay);

            if (!mevcutKayit)
            {
                var gerceklesen = new AylikOdemeGerceklesen
                {
                    AylikOdemePlaniId = plan.Id,
                    FirmaId = firmaId,
                    Yil = yil,
                    Ay = ay,
                    PlanlananTutar = plan.AylikTutar,
                    OdenenTutar = 0,
                    Durum = OdemeDurumu.Bekleniyor,
                    CreatedAt = DateTime.UtcNow
                };
                context.AylikOdemeGerceklesenler.Add(gerceklesen);
            }
        }

        await context.SaveChangesAsync();
    }

    public async Task<decimal> GetAylikToplamTutarAsync(int firmaId, int yil, int ay)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.AylikOdemeGerceklesenler
            .Where(o => o.FirmaId == firmaId && o.Yil == yil && o.Ay == ay && !o.IsDeleted)
            .SumAsync(o => o.PlanlananTutar);
    }

    public async Task<Dictionary<int, decimal>> GetYillikOdemeDagilimiAsync(int firmaId, int yil)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        var odemeler = await context.AylikOdemeGerceklesenler
            .Where(o => o.FirmaId == firmaId && o.Yil == yil && !o.IsDeleted)
            .GroupBy(o => o.Ay)
            .Select(g => new { Ay = g.Key, Toplam = g.Sum(o => o.PlanlananTutar) })
            .ToListAsync();

        var sonuc = new Dictionary<int, decimal>();
        for (int ay = 1; ay <= 12; ay++)
        {
            sonuc[ay] = odemeler.FirstOrDefault(o => o.Ay == ay)?.Toplam ?? 0;
        }

        return sonuc;
    }

    public async Task<decimal> GetToplamAylikOdemeAsync(int firmaId)
    {
        var planlar = await GetAktifPlanlariAsync(firmaId);
        return planlar.Sum(p => p.AylikTutar);
    }

    public async Task<int> GetBekleyenOdemeSayisiAsync(int firmaId)
    {
        var bekleyenler = await GetBekleyenOdemeleriAsync(firmaId);
        return bekleyenler.Count;
    }

    public async Task<decimal> GetBuAyOdenecekTutarAsync(int firmaId)
    {
        var bugun = DateTime.Today;
        return await GetAylikToplamTutarAsync(firmaId, bugun.Year, bugun.Month);
    }

    public async Task<byte[]> ExportAylikOdemeTablosuAsync(int yil, int ay)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        
        // T�m firmalar�n �deme planlar�n� al
        var planlar = await context.AylikOdemePlanlari
            .Include(p => p.Firma)
            .Include(p => p.Cari)
            .Where(p => p.Aktif && !p.IsDeleted)
            .OrderBy(p => p.Firma!.FirmaAdi).ThenBy(p => p.OdemeGunu)
            .ToListAsync();

        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add($"{ay:00}-{yil}");

        // Ba�l�k
        worksheet.Cells["A1"].Value = "AYLIK �DEME TABLOSU";
        worksheet.Cells["A1:H1"].Merge = true;
        worksheet.Cells["A1"].Style.Font.Size = 14;
        worksheet.Cells["A1"].Style.Font.Bold = true;
        worksheet.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

        worksheet.Cells["A2"].Value = $"{GetAyAdi(ay)} {yil}";
        worksheet.Cells["A2:H2"].Merge = true;
        worksheet.Cells["A2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

        // Kolon ba�l�klar�
        int row = 4;
        worksheet.Cells[row, 1].Value = "Firma";
        worksheet.Cells[row, 2].Value = "�deme T�r�";
        worksheet.Cells[row, 3].Value = "�deme Ad�";
        worksheet.Cells[row, 4].Value = "G�n";
        worksheet.Cells[row, 5].Value = "Planlanan";
        worksheet.Cells[row, 6].Value = "�denen";
        worksheet.Cells[row, 7].Value = "Kalan";
        worksheet.Cells[row, 8].Value = "Durum";

        worksheet.Cells[row, 1, row, 8].Style.Font.Bold = true;
        worksheet.Cells[row, 1, row, 8].Style.Fill.PatternType = ExcelFillStyle.Solid;
        worksheet.Cells[row, 1, row, 8].Style.Fill.BackgroundColor.SetColor(Color.LightBlue);

        // Veriler
        row = 5;
        decimal toplamPlanlanan = 0;
        decimal toplamOdenen = 0;

        foreach (var plan in planlar)
        {
            var gerceklesen = await context.AylikOdemeGerceklesenler
                .FirstOrDefaultAsync(o => o.AylikOdemePlaniId == plan.Id && o.Yil == yil && o.Ay == ay);

            worksheet.Cells[row, 1].Value = plan.Firma?.FirmaAdi;
            worksheet.Cells[row, 2].Value = plan.Turu.ToString();
            worksheet.Cells[row, 3].Value = plan.OdemeAdi;
            worksheet.Cells[row, 4].Value = plan.OdemeGunu;
            worksheet.Cells[row, 5].Value = plan.AylikTutar;
            worksheet.Cells[row, 6].Value = gerceklesen?.OdenenTutar ?? 0;
            worksheet.Cells[row, 7].Value = plan.AylikTutar - (gerceklesen?.OdenenTutar ?? 0);
            worksheet.Cells[row, 8].Value = gerceklesen?.Durum.ToString() ?? "Bekleniyor";

            toplamPlanlanan += plan.AylikTutar;
            toplamOdenen += gerceklesen?.OdenenTutar ?? 0;

            for (int col = 5; col <= 7; col++)
            {
                worksheet.Cells[row, col].Style.Numberformat.Format = "#,##0.00";
            }

            row++;
        }

        // Toplam
        worksheet.Cells[row, 1].Value = "TOPLAM";
        worksheet.Cells[row, 1, row, 4].Merge = true;
        worksheet.Cells[row, 5].Value = toplamPlanlanan;
        worksheet.Cells[row, 6].Value = toplamOdenen;
        worksheet.Cells[row, 7].Value = toplamPlanlanan - toplamOdenen;

        worksheet.Cells[row, 1, row, 8].Style.Font.Bold = true;
        worksheet.Cells[row, 1, row, 8].Style.Fill.PatternType = ExcelFillStyle.Solid;
        worksheet.Cells[row, 1, row, 8].Style.Fill.BackgroundColor.SetColor(Color.Yellow);

        for (int col = 5; col <= 7; col++)
        {
            worksheet.Cells[row, col].Style.Numberformat.Format = "#,##0.00";
        }

        worksheet.Cells.AutoFitColumns();

        return package.GetAsByteArray();
    }

    public async Task<byte[]> ExportYillikOdemeTablosuAsync(int yil)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        
        var planlar = await context.AylikOdemePlanlari
            .Include(p => p.Firma)
            .Where(p => p.Aktif && !p.IsDeleted)
            .OrderBy(p => p.Firma!.FirmaAdi).ThenBy(p => p.OdemeAdi)
            .ToListAsync();

        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add(yil.ToString());

        // Ba�l�k
        worksheet.Cells["A1"].Value = $"{yil} YILI AYLIK �DEME TABLOSU";
        worksheet.Cells["A1:O1"].Merge = true;
        worksheet.Cells["A1"].Style.Font.Size = 16;
        worksheet.Cells["A1"].Style.Font.Bold = true;
        worksheet.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

        // Kolon ba�l�klar�
        int row = 3;
        worksheet.Cells[row, 1].Value = "Firma";
        worksheet.Cells[row, 2].Value = "�deme Ad�";
        for (int ay = 1; ay <= 12; ay++)
        {
            worksheet.Cells[row, ay + 2].Value = GetAyAdi(ay);
        }
        worksheet.Cells[row, 15].Value = "Toplam";

        worksheet.Cells[row, 1, row, 15].Style.Font.Bold = true;
        worksheet.Cells[row, 1, row, 15].Style.Fill.PatternType = ExcelFillStyle.Solid;
        worksheet.Cells[row, 1, row, 15].Style.Fill.BackgroundColor.SetColor(Color.LightBlue);

        // Veriler
        row = 4;
        var aylikToplamlar = new decimal[12];

        foreach (var plan in planlar)
        {
            worksheet.Cells[row, 1].Value = plan.Firma?.FirmaAdi;
            worksheet.Cells[row, 2].Value = plan.OdemeAdi;

            decimal yillikToplam = 0;
            for (int ay = 1; ay <= 12; ay++)
            {
                var gerceklesen = await context.AylikOdemeGerceklesenler
                    .FirstOrDefaultAsync(o => o.AylikOdemePlaniId == plan.Id && o.Yil == yil && o.Ay == ay);

                var tutar = gerceklesen?.PlanlananTutar ?? 0;
                worksheet.Cells[row, ay + 2].Value = tutar;
                worksheet.Cells[row, ay + 2].Style.Numberformat.Format = "#,##0.00";
                
                yillikToplam += tutar;
                aylikToplamlar[ay - 1] += tutar;
            }

            worksheet.Cells[row, 15].Value = yillikToplam;
            worksheet.Cells[row, 15].Style.Numberformat.Format = "#,##0.00";
            worksheet.Cells[row, 15].Style.Font.Bold = true;

            row++;
        }

        // Toplam sat�r�
        worksheet.Cells[row, 1].Value = "TOPLAM";
        worksheet.Cells[row, 1, row, 2].Merge = true;
        
        decimal grandTotal = 0;
        for (int ay = 1; ay <= 12; ay++)
        {
            worksheet.Cells[row, ay + 2].Value = aylikToplamlar[ay - 1];
            worksheet.Cells[row, ay + 2].Style.Numberformat.Format = "#,##0.00";
            grandTotal += aylikToplamlar[ay - 1];
        }

        worksheet.Cells[row, 15].Value = grandTotal;
        worksheet.Cells[row, 15].Style.Numberformat.Format = "#,##0.00";

        worksheet.Cells[row, 1, row, 15].Style.Font.Bold = true;
        worksheet.Cells[row, 1, row, 15].Style.Fill.PatternType = ExcelFillStyle.Solid;
        worksheet.Cells[row, 1, row, 15].Style.Fill.BackgroundColor.SetColor(Color.Yellow);

        worksheet.Cells.AutoFitColumns();

        return package.GetAsByteArray();
    }

    private string GetAyAdi(int ay) => ay switch
    {
        1 => "Ocak", 2 => "�ubat", 3 => "Mart", 4 => "Nisan",
        5 => "May�s", 6 => "Haziran", 7 => "Temmuz", 8 => "A�ustos",
        9 => "Eyl�l", 10 => "Ekim", 11 => "Kas�m", 12 => "Aral�k",
        _ => ""
    };
}



