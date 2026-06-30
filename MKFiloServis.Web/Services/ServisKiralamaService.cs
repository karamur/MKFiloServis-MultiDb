using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Web.Services;

public class ServisKiralamaService : IServisKiralamaService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public ServisKiralamaService(IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    #region Kiralama Araç İşlemleri

    public async Task<List<KiralamaArac>> GetTumKiralamaAraclarAsync(int firmaId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.KiralamaAraclar
            .Include(k => k.KiralayiciCari)
            .Where(k => k.FirmaId == firmaId && !k.IsDeleted)
            .OrderByDescending(k => k.KiralamaBaslangic)
            .ToListAsync();
    }

    public async Task<List<KiralamaArac>> GetAktifKiralamaAraclarAsync(int firmaId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var bugun = DateTime.Today;

        return await context.KiralamaAraclar
            .Include(k => k.KiralayiciCari)
            .Where(k => k.FirmaId == firmaId && 
                       k.Aktif && 
                       !k.IsDeleted &&
                       k.KiralamaBaslangic <= bugun &&
                       (!k.KiralamaBitis.HasValue || k.KiralamaBitis.Value >= bugun))
            .OrderBy(k => k.Plaka)
            .ToListAsync();
    }

    public async Task<KiralamaArac?> GetKiralamaAracByIdAsync(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.KiralamaAraclar
            .Include(k => k.KiralayiciCari)
            .FirstOrDefaultAsync(k => k.Id == id && !k.IsDeleted);
    }

    public async Task<KiralamaArac> CreateKiralamaAracAsync(KiralamaArac arac)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        arac.CreatedAt = DateTime.UtcNow;
        context.KiralamaAraclar.Add(arac);
        await context.SaveChangesAsync();
        return arac;
    }

    public async Task<KiralamaArac> UpdateKiralamaAracAsync(KiralamaArac arac)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        arac.UpdatedAt = DateTime.UtcNow;
        context.KiralamaAraclar.Update(arac);
        await context.SaveChangesAsync();
        return arac;
    }

    public async Task DeleteKiralamaAracAsync(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var arac = await context.KiralamaAraclar.FindAsync(id);
        if (arac != null)
        {
            arac.IsDeleted = true;
            arac.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }
    }

    #endregion

    #region Servis Çalışma İşlemleri

    public async Task<List<ServisCalismaKiralama>> GetServisCalismalariAsync(int firmaId, DateTime baslangic, DateTime bitis)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.ServisCalismaKiralamalar
            .Include(s => s.Arac)
            .Include(s => s.KiralamaArac).ThenInclude(k => k!.KiralayiciCari)
            .Include(s => s.Sofor)
            .Include(s => s.Guzergah)
            .Include(s => s.MusteriFirma)
            .Where(s => s.FirmaId == firmaId && 
                       s.CalismaTarihi >= baslangic && 
                       s.CalismaTarihi <= bitis &&
                       !s.IsDeleted)
            .OrderBy(s => s.CalismaTarihi).ThenBy(s => s.Guzergah!.GuzergahAdi)
            .ToListAsync();
    }

    public async Task<ServisCalismaKiralama?> GetServisCalismaByIdAsync(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.ServisCalismaKiralamalar
            .Include(s => s.Arac)
            .Include(s => s.KiralamaArac)
            .Include(s => s.Sofor)
            .Include(s => s.Guzergah)
            .Include(s => s.MusteriFirma)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);
    }

    public async Task<ServisCalismaKiralama> CreateServisCalismaAsync(ServisCalismaKiralama calisma)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        // Km hesaplama
        if (calisma.KmBaslangic.HasValue && calisma.KmBitis.HasValue)
        {
            calisma.ToplamKm = calisma.KmBitis.Value - calisma.KmBaslangic.Value;
        }

        calisma.CreatedAt = DateTime.UtcNow;
        context.ServisCalismaKiralamalar.Add(calisma);
        await context.SaveChangesAsync();

        // Hesapla
        return await HesaplaAsync(calisma.Id);
    }

    public async Task<ServisCalismaKiralama> UpdateServisCalismaAsync(ServisCalismaKiralama calisma)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        // Km hesaplama
        if (calisma.KmBaslangic.HasValue && calisma.KmBitis.HasValue)
        {
            calisma.ToplamKm = calisma.KmBitis.Value - calisma.KmBaslangic.Value;
        }

        calisma.UpdatedAt = DateTime.UtcNow;
        context.ServisCalismaKiralamalar.Update(calisma);
        await context.SaveChangesAsync();

        // Hesapla
        return await HesaplaAsync(calisma.Id);
    }

    public async Task DeleteServisCalismaAsync(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var calisma = await context.ServisCalismaKiralamalar.FindAsync(id);
        if (calisma != null)
        {
            calisma.IsDeleted = true;
            calisma.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }
    }

    public async Task<ServisCalismaKiralama> HesaplaAsync(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var calisma = await context.ServisCalismaKiralamalar
            .Include(s => s.KiralamaArac)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (calisma == null) throw new Exception("Servis çalışması bulunamadı");

        // Kiralık araç ise kira bedelini hesapla
        if (calisma.AracSahiplikTuru == AracSahiplikTuru.KiralikArac && calisma.KiralamaArac != null)
        {
            // Sefer başına bedel varsa
            if (calisma.KiralamaArac.SeferBasinaKiraBedeli.HasValue)
            {
                calisma.AracKiraBedeli = calisma.KiralamaArac.SeferBasinaKiraBedeli.Value;
            }
            // Günlük bedel varsa
            else if (calisma.KiralamaArac.GunlukKiraBedeli.HasValue)
            {
                calisma.AracKiraBedeli = calisma.KiralamaArac.GunlukKiraBedeli.Value;
            }

            // Komisyon hesapla
            if (calisma.KiralamaArac.SabitKomisyonTutari.HasValue)
            {
                calisma.KomisyonTutari = calisma.KiralamaArac.SabitKomisyonTutari.Value;
            }
            else if (calisma.KiralamaArac.KomisyonOrani.HasValue && calisma.CalismaBedeli.HasValue)
            {
                calisma.KomisyonTutari = calisma.CalismaBedeli.Value * (calisma.KiralamaArac.KomisyonOrani.Value / 100m);
            }
        }

        // Net kazanç hesapla
        var gelir = calisma.CalismaBedeli ?? 0;
        var gider = (calisma.AracKiraBedeli ?? 0) + (calisma.KomisyonTutari ?? 0);
        calisma.NetKazanc = gelir - gider;

        calisma.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        return calisma;
    }

    public async Task<List<ServisCalismaKiralama>> HaftalikPlanOlusturAsync(int firmaId, DateTime haftaBaslangic)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var sonuclar = new List<ServisCalismaKiralama>();

        // Önceki hafta çalışmalarını bul (tekrar eden güzergahlar için)
        var oncekiHafta = haftaBaslangic.AddDays(-7);
        var oncekiCalismalanlar = await context.ServisCalismaKiralamalar
            .Where(s => s.FirmaId == firmaId &&
                       s.CalismaTarihi >= oncekiHafta &&
                       s.CalismaTarihi < haftaBaslangic &&
                       !s.IsDeleted)
            .ToListAsync();

        // 7 gün için plan oluştur
        for (int i = 0; i < 7; i++)
        {
            var tarih = haftaBaslangic.AddDays(i);
            var gunCalismalari = oncekiCalismalanlar.Where(o => o.CalismaTarihi.DayOfWeek == tarih.DayOfWeek);

            foreach (var onceki in gunCalismalari)
            {
                var yeniCalisma = new ServisCalismaKiralama
                {
                    FirmaId = firmaId,
                    CalismaTarihi = tarih,
                    ServisTuru = onceki.ServisTuru,
                    AracSahiplikTuru = onceki.AracSahiplikTuru,
                    AracId = onceki.AracId,
                    KiralamaAracId = onceki.KiralamaAracId,
                    SoforId = onceki.SoforId,
                    GuzergahId = onceki.GuzergahId,
                    MusteriFirmaId = onceki.MusteriFirmaId,
                    CalismaBedeli = onceki.CalismaBedeli,
                    Durum = CalismaDurum.Planli,
                    CreatedAt = DateTime.UtcNow
                };

                context.ServisCalismaKiralamalar.Add(yeniCalisma);
                sonuclar.Add(yeniCalisma);
            }
        }

        await context.SaveChangesAsync();
        return sonuclar;
    }

    #endregion

    #region Raporlar

    public async Task<List<ServisCalismaRapor>> GetServisCalismaRaporuAsync(
        int? firmaId, DateTime baslangic, DateTime bitis, AracSahiplikTuru? sahiplikTuru = null)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        var query = context.ServisCalismaKiralamalar
            .Include(s => s.Arac)
            .Include(s => s.KiralamaArac)
            .Include(s => s.Sofor)
            .Include(s => s.Guzergah)
            .Include(s => s.MusteriFirma)
            .Where(s => s.CalismaTarihi >= baslangic && s.CalismaTarihi <= bitis && !s.IsDeleted)
            .AsQueryable();

        if (firmaId.HasValue)
        {
            query = query.Where(s => s.FirmaId == firmaId.Value);
        }

        if (sahiplikTuru.HasValue)
        {
            query = query.Where(s => s.AracSahiplikTuru == sahiplikTuru.Value);
        }

        var veriler = await query.OrderBy(s => s.CalismaTarihi).ToListAsync();

        return veriler.Select(s => new ServisCalismaRapor
        {
            Tarih = s.CalismaTarihi,
            Plaka = s.AracSahiplikTuru == AracSahiplikTuru.KendiArac 
                ? s.Arac?.AktifPlaka 
                : s.KiralamaArac?.Plaka,
            AracSahiplik = s.AracSahiplikTuru == AracSahiplikTuru.KendiArac ? "Kendi" : "Kiralık",
            SoforAdi = $"{s.Sofor?.Ad} {s.Sofor?.Soyad}",
            GuzergahAdi = s.Guzergah?.GuzergahAdi,
            MusteriFirma = s.MusteriFirma?.FirmaAdi,
            ServisTuru = s.ServisTuru.ToString(),
            CalismaBedeli = s.CalismaBedeli,
            AracKiraBedeli = s.AracKiraBedeli,
            KomisyonTutari = s.KomisyonTutari,
            NetKazanc = s.NetKazanc,
            ToplamKm = s.ToplamKm,
            BaslangicSaati = s.BaslangicSaati?.ToString(@"hh\:mm"),
            BitisSaati = s.BitisSaati?.ToString(@"hh\:mm"),
            Durum = s.Durum.ToString(),
            Notlar = s.Notlar
        }).ToList();
    }

    public async Task<Dictionary<string, decimal>> GetAracBazindaKazancAsync(int firmaId, DateTime baslangic, DateTime bitis)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        var veriler = await context.ServisCalismaKiralamalar
            .Include(s => s.Arac)
            .Include(s => s.KiralamaArac)
            .Where(s => s.FirmaId == firmaId && 
                       s.CalismaTarihi >= baslangic && 
                       s.CalismaTarihi <= bitis &&
                       !s.IsDeleted)
            .ToListAsync();

        return veriler
            .GroupBy(s => s.AracSahiplikTuru == AracSahiplikTuru.KendiArac 
                ? s.Arac?.AktifPlaka ?? "Bilinmeyen"
                : s.KiralamaArac?.Plaka ?? "Bilinmeyen")
            .ToDictionary(
                g => g.Key,
                g => g.Sum(s => s.NetKazanc ?? 0)
            );
    }

    public async Task<Dictionary<string, decimal>> GetGuzergahBazindaKazancAsync(int firmaId, DateTime baslangic, DateTime bitis)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        return await context.ServisCalismaKiralamalar
            .Include(s => s.Guzergah)
            .Where(s => s.FirmaId == firmaId && 
                       s.CalismaTarihi >= baslangic && 
                       s.CalismaTarihi <= bitis &&
                       !s.IsDeleted)
            .GroupBy(s => s.Guzergah!.GuzergahAdi)
            .Select(g => new { Guzergah = g.Key, Toplam = g.Sum(s => s.NetKazanc ?? 0) })
            .ToDictionaryAsync(x => x.Guzergah, x => x.Toplam);
    }

    public async Task<Dictionary<int, int>> GetAylikServisSayisiAsync(int firmaId, int yil)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        var sonuc = new Dictionary<int, int>();
        for (int ay = 1; ay <= 12; ay++)
        {
            var baslangic = new DateTime(yil, ay, 1);
            var bitis = baslangic.AddMonths(1).AddDays(-1);

            var sayi = await context.ServisCalismaKiralamalar
                .Where(s => s.FirmaId == firmaId &&
                           s.CalismaTarihi >= baslangic &&
                           s.CalismaTarihi <= bitis &&
                           !s.IsDeleted)
                .CountAsync();

            sonuc[ay] = sayi;
        }

        return sonuc;
    }

    #endregion

    #region Excel Export

    public async Task<byte[]> ExportServisCalismaRaporuAsync(int? firmaId, DateTime baslangic, DateTime bitis)
    {
        var veriler = await GetServisCalismaRaporuAsync(firmaId, baslangic, bitis);

        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("Servis Çalışma Raporu");

        // Başlık
        worksheet.Cells["A1"].Value = "SERVİS ÇALIŞMA RAPORU";
        worksheet.Cells["A1:O1"].Merge = true;
        worksheet.Cells["A1"].Style.Font.Size = 16;
        worksheet.Cells["A1"].Style.Font.Bold = true;
        worksheet.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

        worksheet.Cells["A2"].Value = $"{baslangic:dd.MM.yyyy} - {bitis:dd.MM.yyyy}";
        worksheet.Cells["A2:O2"].Merge = true;
        worksheet.Cells["A2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

        // Kolon başlıkları
        int row = 4;
        var basliklar = new[]
        {
            "Tarih", "Plaka", "Sahiplik", "Şoför", "Güzergah", "Müşteri Firma",
            "Servis Türü", "Çalışma Bedeli", "Kira Bedeli", "Komisyon",
            "Net Kazanç", "Km", "Başlangıç", "Bitiş", "Durum"
        };

        for (int col = 0; col < basliklar.Length; col++)
        {
            worksheet.Cells[row, col + 1].Value = basliklar[col];
        }

        worksheet.Cells[row, 1, row, basliklar.Length].Style.Font.Bold = true;
        worksheet.Cells[row, 1, row, basliklar.Length].Style.Fill.PatternType = ExcelFillStyle.Solid;
        worksheet.Cells[row, 1, row, basliklar.Length].Style.Fill.BackgroundColor.SetColor(Color.LightBlue);

        // Veriler
        row = 5;
        foreach (var v in veriler)
        {
            worksheet.Cells[row, 1].Value = v.Tarih.ToString("dd.MM.yyyy");
            worksheet.Cells[row, 2].Value = v.Plaka;
            worksheet.Cells[row, 3].Value = v.AracSahiplik;
            worksheet.Cells[row, 4].Value = v.SoforAdi;
            worksheet.Cells[row, 5].Value = v.GuzergahAdi;
            worksheet.Cells[row, 6].Value = v.MusteriFirma;
            worksheet.Cells[row, 7].Value = v.ServisTuru;
            worksheet.Cells[row, 8].Value = v.CalismaBedeli;
            worksheet.Cells[row, 9].Value = v.AracKiraBedeli;
            worksheet.Cells[row, 10].Value = v.KomisyonTutari;
            worksheet.Cells[row, 11].Value = v.NetKazanc;
            worksheet.Cells[row, 12].Value = v.ToplamKm;
            worksheet.Cells[row, 13].Value = v.BaslangicSaati;
            worksheet.Cells[row, 14].Value = v.BitisSaati;
            worksheet.Cells[row, 15].Value = v.Durum;

            // Para formatı
            for (int col = 8; col <= 11; col++)
            {
                worksheet.Cells[row, col].Style.Numberformat.Format = "#,##0.00";
            }

            // Renklendirme: Kiralık araçlar
            if (v.AracSahiplik == "Kiralık")
            {
                worksheet.Cells[row, 1, row, basliklar.Length].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheet.Cells[row, 1, row, basliklar.Length].Style.Fill.BackgroundColor.SetColor(Color.LightYellow);
            }

            row++;
        }

        // Toplam satırı
        worksheet.Cells[row, 1].Value = "TOPLAM";
        worksheet.Cells[row, 1, row, 7].Merge = true;
        worksheet.Cells[row, 8].Formula = $"SUM(H5:H{row-1})";
        worksheet.Cells[row, 9].Formula = $"SUM(I5:I{row-1})";
        worksheet.Cells[row, 10].Formula = $"SUM(J5:J{row-1})";
        worksheet.Cells[row, 11].Formula = $"SUM(K5:K{row-1})";

        worksheet.Cells[row, 1, row, basliklar.Length].Style.Font.Bold = true;
        worksheet.Cells[row, 1, row, basliklar.Length].Style.Fill.PatternType = ExcelFillStyle.Solid;
        worksheet.Cells[row, 1, row, basliklar.Length].Style.Fill.BackgroundColor.SetColor(Color.Yellow);

        for (int col = 8; col <= 11; col++)
        {
            worksheet.Cells[row, col].Style.Numberformat.Format = "#,##0.00";
        }

        worksheet.Cells.AutoFitColumns();

        return package.GetAsByteArray();
    }

    public async Task<byte[]> ExportKiralamaAracListesiAsync(int firmaId)
    {
        var araclar = await GetTumKiralamaAraclarAsync(firmaId);

        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("Kiralama Araçlar");

        // Başlık
        worksheet.Cells["A1"].Value = "KİRALIK ARAÇ LİSTESİ";
        worksheet.Cells["A1:J1"].Merge = true;
        worksheet.Cells["A1"].Style.Font.Size = 14;
        worksheet.Cells["A1"].Style.Font.Bold = true;
        worksheet.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

        // Kolon başlıkları
        int row = 3;
        var basliklar = new[] { "Plaka", "Marka/Model", "Araç Tipi", "Kiralayan", 
            "Başlangıç", "Bitiş", "Günlük Kira", "Sefer Kira", "Aylık Kira", "Durum" };

        for (int col = 0; col < basliklar.Length; col++)
        {
            worksheet.Cells[row, col + 1].Value = basliklar[col];
        }

        worksheet.Cells[row, 1, row, basliklar.Length].Style.Font.Bold = true;
        worksheet.Cells[row, 1, row, basliklar.Length].Style.Fill.PatternType = ExcelFillStyle.Solid;
        worksheet.Cells[row, 1, row, basliklar.Length].Style.Fill.BackgroundColor.SetColor(Color.LightGray);

        // Veriler
        row = 4;
        foreach (var a in araclar)
        {
            worksheet.Cells[row, 1].Value = a.Plaka;
            worksheet.Cells[row, 2].Value = $"{a.Marka} {a.Model}";
            worksheet.Cells[row, 3].Value = a.AracTipi.ToString();
            worksheet.Cells[row, 4].Value = a.KiralayiciCari?.Unvan;
            worksheet.Cells[row, 5].Value = a.KiralamaBaslangic.ToString("dd.MM.yyyy");
            worksheet.Cells[row, 6].Value = a.KiralamaBitis?.ToString("dd.MM.yyyy");
            worksheet.Cells[row, 7].Value = a.GunlukKiraBedeli;
            worksheet.Cells[row, 8].Value = a.SeferBasinaKiraBedeli;
            worksheet.Cells[row, 9].Value = a.AylikKiraBedeli;
            worksheet.Cells[row, 10].Value = a.Aktif ? "Aktif" : "Pasif";

            for (int col = 7; col <= 9; col++)
            {
                worksheet.Cells[row, col].Style.Numberformat.Format = "#,##0.00";
            }

            row++;
        }

        worksheet.Cells.AutoFitColumns();

        return package.GetAsByteArray();
    }

    public async Task<byte[]> ExportAylikOzetAsync(int firmaId, int yil, int ay)
    {
        var baslangic = new DateTime(yil, ay, 1);
        var bitis = baslangic.AddMonths(1).AddDays(-1);

        var veriler = await GetServisCalismaRaporuAsync(firmaId, baslangic, bitis);

        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add($"{ay:00}-{yil} Özet");

        // Başlık
        worksheet.Cells["A1"].Value = $"{GetAyAdi(ay)} {yil} - SERVİS ÇALIŞMA ÖZETİ";
        worksheet.Cells["A1:E1"].Merge = true;
        worksheet.Cells["A1"].Style.Font.Size = 14;
        worksheet.Cells["A1"].Style.Font.Bold = true;

        // Özet bilgiler
        int row = 3;
        worksheet.Cells[row, 1].Value = "Toplam Servis Sayısı:";
        worksheet.Cells[row, 2].Value = veriler.Count;

        row++;
        worksheet.Cells[row, 1].Value = "Kendi Araç Sayısı:";
        worksheet.Cells[row, 2].Value = veriler.Count(v => v.AracSahiplik == "Kendi");

        row++;
        worksheet.Cells[row, 1].Value = "Kiralık Araç Sayısı:";
        worksheet.Cells[row, 2].Value = veriler.Count(v => v.AracSahiplik == "Kiralık");

        row++;
        worksheet.Cells[row, 1].Value = "Toplam Gelir:";
        worksheet.Cells[row, 2].Value = veriler.Sum(v => v.CalismaBedeli ?? 0);
        worksheet.Cells[row, 2].Style.Numberformat.Format = "#,##0.00";

        row++;
        worksheet.Cells[row, 1].Value = "Toplam Kira Gideri:";
        worksheet.Cells[row, 2].Value = veriler.Sum(v => v.AracKiraBedeli ?? 0);
        worksheet.Cells[row, 2].Style.Numberformat.Format = "#,##0.00";

        row++;
        worksheet.Cells[row, 1].Value = "Toplam Net Kazanç:";
        worksheet.Cells[row, 2].Value = veriler.Sum(v => v.NetKazanc ?? 0);
        worksheet.Cells[row, 2].Style.Numberformat.Format = "#,##0.00";
        worksheet.Cells[row, 1, row, 2].Style.Font.Bold = true;

        worksheet.Cells.AutoFitColumns();

        return package.GetAsByteArray();
    }

    #endregion

    #region İstatistikler

    public async Task<int> GetToplamKiralamaAracSayisiAsync(int firmaId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.KiralamaAraclar.CountAsync(k => k.FirmaId == firmaId && k.Aktif && !k.IsDeleted);
    }

    public async Task<int> GetAylikServisSayisiAsync(int firmaId, int yil, int ay)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var baslangic = new DateTime(yil, ay, 1);
        var bitis = baslangic.AddMonths(1).AddDays(-1);

        return await context.ServisCalismaKiralamalar
            .Where(s => s.FirmaId == firmaId &&
                       s.CalismaTarihi >= baslangic &&
                       s.CalismaTarihi <= bitis &&
                       !s.IsDeleted)
            .CountAsync();
    }

    public async Task<decimal> GetAylikToplamKazancAsync(int firmaId, int yil, int ay)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var baslangic = new DateTime(yil, ay, 1);
        var bitis = baslangic.AddMonths(1).AddDays(-1);

        return await context.ServisCalismaKiralamalar
            .Where(s => s.FirmaId == firmaId &&
                       s.CalismaTarihi >= baslangic &&
                       s.CalismaTarihi <= bitis &&
                       !s.IsDeleted)
            .SumAsync(s => s.NetKazanc ?? 0);
    }

    public async Task<decimal> GetAylikKiraBedeliAsync(int firmaId, int yil, int ay)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var baslangic = new DateTime(yil, ay, 1);
        var bitis = baslangic.AddMonths(1).AddDays(-1);

        return await context.ServisCalismaKiralamalar
            .Where(s => s.FirmaId == firmaId &&
                       s.CalismaTarihi >= baslangic &&
                       s.CalismaTarihi <= bitis &&
                       s.AracSahiplikTuru == AracSahiplikTuru.KiralikArac &&
                       !s.IsDeleted)
            .SumAsync(s => s.AracKiraBedeli ?? 0);
    }

    #endregion

    private string GetAyAdi(int ay) => ay switch
    {
        1 => "Ocak", 2 => "Şubat", 3 => "Mart", 4 => "Nisan",
        5 => "Mayıs", 6 => "Haziran", 7 => "Temmuz", 8 => "Ağustos",
        9 => "Eylül", 10 => "Ekim", 11 => "Kasım", 12 => "Aralık",
        _ => ""
    };
}



