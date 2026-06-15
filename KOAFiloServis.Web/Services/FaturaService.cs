using KOAFiloServis.Shared.Entities;
using KOAFiloServis.Web.Data;
using KOAFiloServis.Web.Helpers;
using KOAFiloServis.Web.Models;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

namespace KOAFiloServis.Web.Services;

public class FaturaService : IFaturaService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IMuhasebeService _muhasebeService;
    private readonly NumaraSerisiService _numaraSerisi;
    private readonly IWebHostEnvironment _env;
    private readonly ISecureFileService _secureFileService;

    public FaturaService(IDbContextFactory<ApplicationDbContext> contextFactory, IMuhasebeService muhasebeService, NumaraSerisiService numaraSerisi, IWebHostEnvironment env, ISecureFileService secureFileService)
    {
        _contextFactory = contextFactory;
        _muhasebeService = muhasebeService;
        _numaraSerisi = numaraSerisi;
        _env = env;
        _secureFileService = secureFileService;
    }

    public async Task<List<Fatura>> GetAllAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Faturalar
            .AsNoTracking()
            .Include(f => f.Cari)
            .Include(f => f.KarsiFirma)
            .OrderByDescending(f => f.FaturaTarihi)
            .ToListAsync();
    }

    public async Task<PagedResult<Fatura>> GetPagedAsync(FaturaFilterParams filter)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.Faturalar
            .AsNoTracking()
            .Include(f => f.Cari)
            .Include(f => f.Firma)
            .Include(f => f.KarsiFirma)
            .AsQueryable();

        // Arama filtresi
        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var searchLower = filter.SearchTerm.ToLower();
            query = query.Where(f =>
                (f.FaturaNo != null && f.FaturaNo.ToLower().Contains(searchLower)) ||
                (f.Cari != null && f.Cari.Unvan != null && f.Cari.Unvan.ToLower().Contains(searchLower)));
        }

        // Tip filtresi
        if (filter.FaturaTipi.HasValue)
        {
            query = query.Where(f => f.FaturaTipi == filter.FaturaTipi.Value);
        }

        // Durum filtresi
        if (filter.Durum.HasValue)
        {
            query = query.Where(f => f.Durum == filter.Durum.Value);
        }

        // Yön filtresi
        if (filter.Yon.HasValue)
        {
            query = query.Where(f => f.FaturaYonu == filter.Yon.Value);
        }

        if (filter.FirmaId.HasValue)
        {
            query = query.Where(f => f.FirmaId == filter.FirmaId.Value);
        }

        // Cari filtresi
        if (filter.CariId.HasValue)
        {
            query = query.Where(f => f.CariId == filter.CariId.Value);
        }

        // Tarih aralığı
        if (filter.BaslangicTarih.HasValue)
        {
            query = query.Where(f => f.FaturaTarihi >= filter.BaslangicTarih.Value);
        }
        if (filter.BitisTarih.HasValue)
        {
            query = query.Where(f => f.FaturaTarihi <= filter.BitisTarih.Value);
        }

        // Toplam kayıt sayısı
        var totalCount = await query.CountAsync();

        // Sayfalama uygula
        var items = await query
            .OrderByDescending(f => f.FaturaTarihi)
            .Skip(filter.Skip)
            .Take(filter.PageSize)
            .ToListAsync();

        return new PagedResult<Fatura>(items, totalCount, filter.PageNumber, filter.PageSize);
    }

    public async Task<List<Fatura>> GetByCariIdAsync(int cariId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Faturalar
            .AsNoTracking()
            .Include(f => f.Cari)
            .Include(f => f.KarsiFirma)
            .Where(f => f.CariId == cariId)
            .OrderByDescending(f => f.FaturaTarihi)
            .ToListAsync();
    }

    public async Task<List<Fatura>> GetByTipAsync(FaturaTipi tip)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Faturalar
            .AsNoTracking()
            .Include(f => f.Cari)
            .Include(f => f.KarsiFirma)
            .Where(f => f.FaturaTipi == tip)
            .OrderByDescending(f => f.FaturaTarihi)
            .ToListAsync();
    }

    public async Task<List<Fatura>> GetByDurumAsync(FaturaDurum durum)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Faturalar
            .AsNoTracking()
            .Include(f => f.Cari)
            .Include(f => f.KarsiFirma)
            .Where(f => f.Durum == durum)
            .OrderByDescending(f => f.FaturaTarihi)
            .ToListAsync();
    }

    public async Task<List<Fatura>> GetOdenmemisFaturalarAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Faturalar
            .AsNoTracking()
            .Include(f => f.Cari)
            .Include(f => f.KarsiFirma)
            .Where(f => f.Durum == FaturaDurum.Beklemede || f.Durum == FaturaDurum.KismiOdendi)
            .OrderBy(f => f.VadeTarihi)
            .ToListAsync();
    }

    public async Task<List<Fatura>> GetOdenmisFaturalarAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Faturalar
            .AsNoTracking()
            .Include(f => f.Cari)
            .Include(f => f.KarsiFirma)
            .Where(f => f.Durum == FaturaDurum.Odendi)
            .OrderByDescending(f => f.FaturaTarihi)
            .ToListAsync();
    }

    public async Task<List<Fatura>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Faturalar
            .AsNoTracking()
            .Include(f => f.Cari)
            .Include(f => f.KarsiFirma)
            .Where(f => f.FaturaTarihi >= startDate && f.FaturaTarihi <= endDate)
            .OrderByDescending(f => f.FaturaTarihi)
            .ToListAsync();
    }

    public async Task<Fatura?> GetByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Faturalar
            .Include(f => f.Cari)
            .Include(f => f.Firma)
            .Include(f => f.KarsiFirma)
            .FirstOrDefaultAsync(f => f.Id == id);
    }

    public async Task<Fatura?> GetByIdWithKalemlerAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Faturalar
            .Include(f => f.Cari)
            .Include(f => f.Firma)
            .Include(f => f.KarsiFirma)
            .Include(f => f.FaturaKalemleri)
            .Include(f => f.OdemeEslestirmeleri)
                .ThenInclude(o => o.BankaKasaHareket)
                    .ThenInclude(h => h.BankaHesap)
            .FirstOrDefaultAsync(f => f.Id == id);
    }

    public async Task<Fatura> CreateAsync(Fatura fatura)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        try
        {
            await PrepareFaturaForSaveAsync(context, fatura);

            // Tutarları hesapla
            CalculateTotals(fatura);

            context.Faturalar.Add(fatura);
            await context.SaveChangesAsync();

            // Firmalar arası fatura ise karşı firmada eşleşen fatura oluştur
            if (fatura.FirmalarArasiFatura && fatura.KarsiFirmaId.HasValue)
            {
                await CreateKarsiFirmaFaturasiAsync(context, fatura);
            }

            // Otomatik muhasebe fişi oluştur (ayarlara göre)
            await TryCreateMuhasebeFisiAsync(context, fatura);

            return fatura;
        }
        catch (DbUpdateException ex)
        {
            throw CreateFriendlyFaturaSaveException(ex, fatura);
        }
    }

    public async Task<Fatura> UpdateAsync(Fatura fatura)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        try
        {
            var existing = await context.Faturalar
                .Include(f => f.FaturaKalemleri)
                .FirstOrDefaultAsync(f => f.Id == fatura.Id);

            if (existing == null) throw new Exception("Fatura bulunamadi");

            // Mevcut entity'yi guncelle
            await PrepareFaturaForSaveAsync(context, fatura);

            existing.FaturaNo = fatura.FaturaNo;
            existing.FaturaTarihi = fatura.FaturaTarihi;
            existing.VadeTarihi = fatura.VadeTarihi;
            existing.FaturaTipi = fatura.FaturaTipi;
            existing.EFaturaTipi = fatura.EFaturaTipi;
            existing.FaturaYonu = fatura.FaturaYonu;
            existing.CariId = fatura.CariId;
            existing.FirmaId = fatura.FirmaId;
            existing.AraToplam = fatura.AraToplam;
            existing.IskontoTutar = fatura.IskontoTutar;
            existing.KdvOrani = fatura.KdvOrani;
            existing.KdvTutar = fatura.KdvTutar;
            existing.GenelToplam = fatura.GenelToplam;
            existing.OdenenTutar = fatura.OdenenTutar;
            existing.Durum = fatura.Durum;
            existing.Aciklama = fatura.Aciklama;
            existing.Notlar = fatura.Notlar;
            existing.EttnNo = fatura.EttnNo;
            existing.GibKodu = fatura.GibKodu;
            existing.GibDurumu = fatura.GibDurumu;
            existing.GibGonderimTarihi = fatura.GibGonderimTarihi;
            existing.GibDurumGuncellemeTarihi = fatura.GibDurumGuncellemeTarihi;
            existing.GibDurumMesaji = fatura.GibDurumMesaji;
            existing.GibOnayTarihi = fatura.GibOnayTarihi;
            existing.TevkifatliMi = fatura.TevkifatliMi;
            existing.TevkifatOrani = fatura.TevkifatOrani;
            existing.TevkifatKodu = fatura.TevkifatKodu;
            existing.TevkifatTutar = fatura.TevkifatTutar;
            existing.MuhasebeFisiOlusturuldu = fatura.MuhasebeFisiOlusturuldu;
            existing.MuhasebeFisId = fatura.MuhasebeFisId;
            existing.AracId = fatura.AracId;
            existing.AracFaturasi = fatura.AracFaturasi;
            existing.UpdatedAt = DateTime.UtcNow;

            // Tutarlari yeniden hesapla
            CalculateTotals(existing);

            // Context tracking için attach değil, zaten track ediliyor
            context.Entry(existing).State = EntityState.Modified;
            await context.SaveChangesAsync();
            return existing;
        }
        catch (DbUpdateException ex)
        {
            throw CreateFriendlyFaturaSaveException(ex, fatura);
        }
    }

    private static InvalidOperationException CreateFriendlyFaturaSaveException(DbUpdateException ex, Fatura fatura)
    {
        var dbMessage = ex.InnerException?.Message ?? ex.Message;

        if (dbMessage.Contains("IX_Faturalar_FirmaId_FaturaYonu_FaturaNo", StringComparison.OrdinalIgnoreCase)
            || dbMessage.Contains("Faturalar_FirmaId_FaturaYonu_FaturaNo", StringComparison.OrdinalIgnoreCase)
            || dbMessage.Contains("UNIQUE constraint failed: Faturalar.FirmaId, Faturalar.FaturaYonu, Faturalar.FaturaNo", StringComparison.OrdinalIgnoreCase))
        {
            var firmaText = fatura.FirmaId.HasValue ? $"firma #{fatura.FirmaId.Value}" : "seçili firma";
            var yonText = fatura.FaturaYonu == FaturaYonu.Giden ? "kesilen" : "gelen";
            var faturaNo = string.IsNullOrWhiteSpace(fatura.FaturaNo) ? "(boş)" : fatura.FaturaNo;
            return new InvalidOperationException($"{firmaText} için {yonText} yönde '{faturaNo}' numaralı fatura zaten kayıtlı.", ex);
        }

        return new InvalidOperationException("Fatura kaydı sırasında veritabanı hatası oluştu.", ex);
    }

    public async Task DeleteAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var fatura = await context.Faturalar.FindAsync(id);
        if (fatura != null)
        {
            fatura.IsDeleted = true;
            await context.SaveChangesAsync();
        }
    }

    public async Task<string> GenerateNextFaturaNoAsync(FaturaTipi tip, FaturaYonu? yon = null, int? firmaId = null)
    {
        var prefix = tip switch
        {
            FaturaTipi.SatisFaturasi => "SF",
            FaturaTipi.AlisFaturasi => "AF",
            FaturaTipi.SatisIadeFaturasi => "SIF",
            FaturaTipi.AlisIadeFaturasi => "AIF",
            _ => "FTR"
        };

        // FaturaYonu prefix'e eklenir (unique constraint: FirmaId + FaturaYonu + FaturaNo)
        if (yon.HasValue)
            prefix += yon.Value == FaturaYonu.Giden ? "_GIDEN" : "_GELEN";

        // Kural 15: FirmaId bazlı atomik numara üretimi
        return await _numaraSerisi.GenerateFormattedAsync(prefix, firmaId ?? 0, 6);
    }

    public async Task UpdateOdenenTutarAsync(int faturaId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var fatura = await context.Faturalar
            .Include(f => f.OdemeEslestirmeleri)
            .FirstOrDefaultAsync(f => f.Id == faturaId);

        if (fatura != null)
        {
            fatura.OdenenTutar = fatura.OdemeEslestirmeleri.Sum(o => o.EslestirilenTutar);

            // Durumu güncelle
            if (fatura.OdenenTutar >= fatura.GenelToplam)
            {
                fatura.Durum = FaturaDurum.Odendi;
            }
            else if (fatura.OdenenTutar > 0)
            {
                fatura.Durum = FaturaDurum.KismiOdendi;
            }
            else
            {
                fatura.Durum = FaturaDurum.Beklemede;
            }

            await context.SaveChangesAsync();
        }
    }

    public async Task<DashboardFaturaStats> GetDashboardStatsAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var stats = new DashboardFaturaStats();
        var today = DateTime.Today;
        var buAyBaslangic = new DateTime(today.Year, today.Month, 1);

        // Single optimized query for invoices needed for dashboard
        var relevantFaturalar = await context.Faturalar
            .Include(f => f.Cari)
            .Where(f => f.Durum != FaturaDurum.IptalEdildi)
            .Select(f => new
            {
                f.Id,
                f.FaturaNo,
                f.FaturaTarihi,
                f.FaturaTipi,
                f.VadeTarihi,
                f.GenelToplam,
                f.OdenenTutar,
                f.Durum,
                KalanTutar = f.GenelToplam - f.OdenenTutar,
                CariUnvan = f.Cari.Unvan
            })
            .ToListAsync();

        // Count pending invoices
        stats.BekleyenFaturaSayisi = relevantFaturalar
            .Count(f => f.KalanTutar > 0);

        // Calculate this month's income/expense
        var buAyFaturalar = relevantFaturalar
            .Where(f => f.FaturaTarihi >= buAyBaslangic);

        stats.BuAyGelir = buAyFaturalar
            .Where(f => f.FaturaTipi == FaturaTipi.SatisFaturasi)
            .Sum(f => f.GenelToplam);

        stats.BuAyGider = buAyFaturalar
            .Where(f => f.FaturaTipi == FaturaTipi.AlisFaturasi)
            .Sum(f => f.GenelToplam);

        // Overdue invoices - need full entity for display
        var vadeGecmisIds = relevantFaturalar
            .Where(f => f.KalanTutar > 0 && f.VadeTarihi.HasValue && f.VadeTarihi.Value < today)
            .OrderBy(f => f.VadeTarihi)
            .Take(10)
            .Select(f => f.Id)
            .ToList();

        if (vadeGecmisIds.Count > 0)
        {
            stats.VadeGecmisFaturalar = await context.Faturalar
                .Include(f => f.Cari)
                .Where(f => vadeGecmisIds.Contains(f.Id))
                .OrderBy(f => f.VadeTarihi)
                .ToListAsync();
        }

        // Upcoming due invoices
        var vadeYaklasanIds = relevantFaturalar
            .Where(f => f.KalanTutar > 0 && f.VadeTarihi.HasValue &&
                   f.VadeTarihi.Value >= today && f.VadeTarihi.Value <= today.AddDays(7))
            .OrderBy(f => f.VadeTarihi)
            .Take(10)
            .Select(f => f.Id)
            .ToList();

        if (vadeYaklasanIds.Count > 0)
        {
            stats.VadeYaklasanFaturalar = await context.Faturalar
                .Include(f => f.Cari)
                .Where(f => vadeYaklasanIds.Contains(f.Id))
                .OrderBy(f => f.VadeTarihi)
                .ToListAsync();
        }

        return stats;
    }

    #region E-Fatura / E-Arsiv Metodlari

    public async Task<List<Fatura>> GetByYonAsync(FaturaYonu yon, int? firmaId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.Faturalar
            .Include(f => f.Cari)
            .Where(f => f.FaturaYonu == yon);

        if (firmaId.HasValue)
            query = query.Where(f => f.FirmaId == firmaId.Value);

        return await query.OrderByDescending(f => f.FaturaTarihi).ToListAsync();
    }

    public async Task<List<Fatura>> GetByYonAndDateRangeAsync(FaturaYonu yon, DateTime? baslangic, DateTime? bitis, int? firmaId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.Faturalar
            .Include(f => f.Cari)
            .Where(f => f.FaturaYonu == yon);

        if (firmaId.HasValue)
            query = query.Where(f => f.FirmaId == firmaId.Value);

        if (baslangic.HasValue)
            query = query.Where(f => f.FaturaTarihi >= baslangic.Value);

        if (bitis.HasValue)
            query = query.Where(f => f.FaturaTarihi <= bitis.Value);

        return await query.OrderByDescending(f => f.FaturaTarihi).ToListAsync();
    }

    public async Task<List<Fatura>> GetByEFaturaTipiAsync(EFaturaTipi tip)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Faturalar
            .Include(f => f.Cari)
            .Where(f => f.EFaturaTipi == tip)
            .OrderByDescending(f => f.FaturaTarihi)
            .ToListAsync();
    }

    /// <summary>
    /// Excel Import - Ornek dosya formatina gore:
    /// A: Unvani/Adi Soyadi, B: Vkn/Tckn, C: Fatura Tipi, D: Fatura Tarihi, E: Fatura No
    /// F: Iskonto, G: Kdv Matrahi %0, H: Kdv Matrahi %1, I: Kdv Matrahi %10, J: Kdv Matrahi %20
    /// K: Kdv%1, L: Kdv%10, M: Kdv%20, N: Odenecek Tutar Turk Lirasi
    /// </summary>
    public async Task<EFaturaImportResult> ImportFromExcelAsync(byte[] fileContent, FaturaYonu yon, int? firmaId = null, EFaturaTipi? eFaturaTipi = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var result = new EFaturaImportResult();

        // Varsayilan E-Fatura tipi
        var defaultEFaturaTipi = eFaturaTipi ?? EFaturaTipi.EArsiv;

        try
        {
            using var stream = new MemoryStream(fileContent);
            using var package = new OfficeOpenXml.ExcelPackage(stream);
            var worksheet = package.Workbook.Worksheets.FirstOrDefault();

            if (worksheet == null)
            {
                result.Errors.Add("Excel dosyasinda sayfa bulunamadi.");
                return result;
            }

            var rowCount = worksheet.Dimension?.Rows ?? 0;

            if (rowCount < 2)
            {
                result.Errors.Add("Excel dosyasinda veri bulunamadi. (Sadece baslik satiri var)");
                return result;
            }

            int nextCariNum = await GetNextCariNumAsync(context);

            // Bu importta olusturulan carileri takip et
            var importCarileri = new Dictionary<string, Cari>(StringComparer.OrdinalIgnoreCase);

            for (int row = 2; row <= rowCount; row++)
            {
                try
                {
                    var cariUnvan = worksheet.Cells[row, 1].Text?.Trim();
                    var cariVkn = worksheet.Cells[row, 2].Text?.Trim();
                    var tarihStr = worksheet.Cells[row, 4].Text?.Trim();
                    var faturaNo = NormalizeFaturaNo(worksheet.Cells[row, 5].Text);

                    // Bos satiri atla
                    if (string.IsNullOrEmpty(faturaNo) && string.IsNullOrEmpty(cariUnvan))
                        continue;

                    // Zorunlu alan kontrolu
                    if (string.IsNullOrEmpty(faturaNo))
                    {
                        result.Errors.Add($"Satir {row}: Fatura No bos.");
                        result.ErrorCount++;
                        continue;
                    }

                    if (string.IsNullOrEmpty(cariUnvan))
                    {
                        result.Errors.Add($"Satir {row}: Cari Unvan bos.");
                        result.ErrorCount++;
                        continue;
                    }

                    var existingFatura = await FindExistingFaturaAsync(context, faturaNo, yon, firmaId);
                    if (existingFatura != null)
                    {
                        result.SkippedCount++;
                        result.Errors.Add($"Satir {row}: '{faturaNo}' zaten mevcut.");
                        continue;
                    }

                    var iskonto = ParseDecimal(worksheet.Cells[row, 6].Text);
                    var kdvMatrah0 = ParseDecimal(worksheet.Cells[row, 7].Text);
                    var kdvMatrah1 = ParseDecimal(worksheet.Cells[row, 8].Text);
                    var kdvMatrah10 = ParseDecimal(worksheet.Cells[row, 9].Text);
                    var kdvMatrah20 = ParseDecimal(worksheet.Cells[row, 10].Text);
                    var kdv1 = ParseDecimal(worksheet.Cells[row, 11].Text);
                    var kdv10 = ParseDecimal(worksheet.Cells[row, 12].Text);
                    var kdv20 = ParseDecimal(worksheet.Cells[row, 13].Text);
                    var odenecekTutar = ParseDecimal(worksheet.Cells[row, 14].Text);

                    // Tarihi parse et
                    DateTime faturaTarihi = DateTime.UtcNow;
                    if (!string.IsNullOrEmpty(tarihStr))
                    {
                        // Turkce tarih formatlari
                        var formats = new[] {
                            "dd.MM.yyyy", "d.MM.yyyy", "dd.M.yyyy", "d.M.yyyy",
                            "dd/MM/yyyy", "d/MM/yyyy", "dd/M/yyyy", "d/M/yyyy",
                            "yyyy-MM-dd"
                        };

                        if (DateTime.TryParseExact(tarihStr, formats,
                            new System.Globalization.CultureInfo("tr-TR"),
                            System.Globalization.DateTimeStyles.None, out var parsedTarih))
                        {
                            faturaTarihi = DateTime.SpecifyKind(parsedTarih.Date, DateTimeKind.Utc);
                        }
                        else if (DateTime.TryParse(tarihStr, new System.Globalization.CultureInfo("tr-TR"), out parsedTarih))
                        {
                            faturaTarihi = DateTime.SpecifyKind(parsedTarih.Date, DateTimeKind.Utc);
                        }
                        else
                        {
                            // Excel sayisal tarih formati
                            if (double.TryParse(tarihStr, out var excelDate) && excelDate > 1)
                            {
                                faturaTarihi = DateTime.SpecifyKind(DateTime.FromOADate(excelDate).Date, DateTimeKind.Utc);
                            }
                        }
                    }

                    // CARİ KONTROL
                    Cari? cari = null;
                    var cariKey = !string.IsNullOrWhiteSpace(cariVkn) ? cariVkn : cariUnvan.ToLowerInvariant();

                    // Once bu importta olusturulmus carilere bak
                    if (!string.IsNullOrEmpty(cariKey) && importCarileri.TryGetValue(cariKey, out var mevcutCari))
                    {
                        cari = mevcutCari;
                    }

                    // Veritabaninda VKN ile ara
                    if (cari == null && !string.IsNullOrWhiteSpace(cariVkn) && cariVkn.Length >= 10)
                    {
                        cari = await context.Cariler.FirstOrDefaultAsync(c => c.VergiNo == cariVkn && (firmaId == null || c.FirmaId == null || c.FirmaId == firmaId));
                    }

                    // Veritabaninda Unvan ile ara
                    if (cari == null && !string.IsNullOrWhiteSpace(cariUnvan))
                    {
                        cari = await context.Cariler.FirstOrDefaultAsync(c =>
                            c.Unvan.ToLower() == cariUnvan.ToLower() && (firmaId == null || c.FirmaId == null || c.FirmaId == firmaId));
                    }

                    // Hala bulunamadiysa yeni olustur
                    if (cari == null)
                    {
                        // Benzersiz CariKodu - timestamp ile garantili
                        var uniqueCode = await GetUniqueCariCodeAsync(context, nextCariNum);

                        cari = new Cari
                        {
                            CariKodu = uniqueCode,
                            Unvan = cariUnvan,
                            VergiNo = cariVkn ?? string.Empty,
                            FirmaId = firmaId,
                            CariTipi = yon == FaturaYonu.Giden ? CariTipi.Musteri : CariTipi.Tedarikci,
                            Aktif = true,
                            CreatedAt = DateTime.UtcNow
                        };

                        context.Cariler.Add(cari);
                        await context.SaveChangesAsync();

                        if (!string.IsNullOrEmpty(cariKey))
                        {
                            importCarileri[cariKey] = cari;
                        }
                        nextCariNum++;
                    }

                    // Toplam matrah ve KDV hesapla
                    var toplamMatrah = kdvMatrah0 + kdvMatrah1 + kdvMatrah10 + kdvMatrah20;
                    var toplamKdv = kdv1 + kdv10 + kdv20;
                    var genelToplam = odenecekTutar > 0 ? odenecekTutar : (toplamMatrah + toplamKdv - iskonto);

                    // Tutar kontrolu
                    if (genelToplam <= 0 && toplamMatrah <= 0)
                    {
                        result.Errors.Add($"Satir {row}: Tutar bilgisi eksik.");
                        result.ErrorCount++;
                        continue;
                    }

                    var fatura = new Fatura
                    {
                        FaturaNo = faturaNo,
                        FaturaTarihi = faturaTarihi,
                        CariId = cari.Id,
                        FirmaId = firmaId,
                        FaturaYonu = yon,
                        FaturaTipi = yon == FaturaYonu.Giden ? FaturaTipi.SatisFaturasi : FaturaTipi.AlisFaturasi,
                        EFaturaTipi = defaultEFaturaTipi,
                        AraToplam = toplamMatrah > 0 ? toplamMatrah : genelToplam,
                        IskontoTutar = iskonto,
                        KdvTutar = toplamKdv,
                        GenelToplam = genelToplam,
                        ImportKaynak = "Excel",
                        Durum = FaturaDurum.Beklemede,
                        CreatedAt = DateTime.UtcNow
                    };

                    context.Faturalar.Add(fatura);

                    // Her faturayı tek tek kaydet - hata durumunda diger faturalar etkilenmesin
                    await context.SaveChangesAsync();

                    // Otomatik muhasebe fişi oluştur
                    await TryCreateMuhasebeFisiAsync(context, fatura);

                    // Stok hareketi oluştur (Gelen: Giriş, Giden: Çıkış)
                    await CreateStokHareketleriFromFaturaAsync(context, fatura, yon);

                    result.ImportedItems.Add(fatura);
                    result.ImportedCount++;
                }
                catch (Exception ex)
                {
                    // Detayli hata mesaji
                    var errorMessage = ex.InnerException?.Message ?? ex.Message;
                    result.Errors.Add($"Satir {row}: {errorMessage}");
                    result.ErrorCount++;

                    // Context'i temizle - hatali entity'leri kaldir
                    foreach (var entry in context.ChangeTracker.Entries().ToList())
                    {
                        if (entry.State == EntityState.Added)
                        {
                            entry.State = EntityState.Detached;
                        }
                    }
                }
            }

            result.Success = result.ImportedCount > 0;
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Excel okuma hatasi: {ex.InnerException?.Message ?? ex.Message}");
        }

        return result;
    }

    public async Task<EFaturaImportResult> ImportFromXmlAsync(List<XmlFileContent> xmlFiles, FaturaYonu yon, int? firmaId = null, EFaturaTipi? eFaturaTipi = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var result = new EFaturaImportResult();
        var defaultEFaturaTipi = eFaturaTipi ?? EFaturaTipi.EFatura;
        int nextCariNum = await GetNextCariNumAsync(context);
        var importCarileri = new Dictionary<string, Cari>(StringComparer.OrdinalIgnoreCase);

        // Ayarları al
        var ayar = await context.MuhasebeAyarlari.FirstOrDefaultAsync();
        var otomatikCariOlustur = ayar?.XmlImportOtomatikCariOlustur ?? true;
        var otomatikHesapKoduOlustur = ayar?.XmlImportOtomatikHesapKoduOlustur ?? true;

        // Kayıtlı firmaları al (firmalar arası fatura kontrolü için)
        var kayitliFirmalar = await context.Firmalar.Where(f => !f.IsDeleted && f.Aktif).ToListAsync();

        foreach (var file in xmlFiles)
        {
            try
            {
                using var ms = new MemoryStream(file.Content);
                var xdoc = System.Xml.Linq.XDocument.Load(ms);

                // Helper to find elements safely ignoring namespaces
                string GetValue(System.Xml.Linq.XElement? parent, string localName) =>
                    parent?.Descendants().FirstOrDefault(x => x.Name.LocalName == localName)?.Value ?? string.Empty;

                decimal GetDecimalValue(System.Xml.Linq.XElement? parent, string localName) =>
                    ParseDecimal(GetValue(parent, localName));

                var invoice = xdoc.Descendants().FirstOrDefault(x => x.Name.LocalName == "Invoice");
                if (invoice == null)
                {
                    result.Errors.Add($"{file.FileName}: Geçerli bir UBL Fatura formatı değil.");
                    result.ErrorCount++;
                    continue;
                }

                var faturaNo = NormalizeFaturaNo(GetValue(invoice, "ID"));
                var issueDateStr = GetValue(invoice, "IssueDate");

                if (string.IsNullOrWhiteSpace(faturaNo))
                {
                    result.Errors.Add($"{file.FileName}: Fatura No (ID) bulunamadı.");
                    result.ErrorCount++;
                    continue;
                }

                var ettn = GetValue(invoice, "UUID");
                var profileId = GetValue(invoice, "ProfileID");
                var invoiceTypeCode = GetValue(invoice, "InvoiceTypeCode");

                var fatTip = !string.IsNullOrEmpty(profileId) && profileId.ToUpperInvariant().Contains("EARSIV")
                    ? EFaturaTipi.EArsiv
                    : defaultEFaturaTipi;

                if (!DateTime.TryParse(issueDateStr, out var faturaTarihi))
                {
                    faturaTarihi = DateTime.Today;
                }
                faturaTarihi = DateTime.SpecifyKind(faturaTarihi.Date, DateTimeKind.Utc);

                string cariUnvan = string.Empty;
                string cariVkn = string.Empty;
                string cariVergiDairesi = string.Empty;
                string cariAdres = string.Empty;
                string cariTelefon = string.Empty;
                string cariTelefon2 = string.Empty;
                string cariFax = string.Empty;
                string cariEmail = string.Empty;
                string cariIlce = string.Empty;
                string cariIl = string.Empty;
                string cariUlke = string.Empty;
                string cariTcKimlikNo = string.Empty;
                string cariPostaKodu = string.Empty;
                string cariWebSitesi = string.Empty;
                string cariNotlar = string.Empty;

                System.Xml.Linq.XElement? supplierNode = invoice.Descendants().FirstOrDefault(x => x.Name.LocalName == "AccountingSupplierParty");
                System.Xml.Linq.XElement? customerNode = invoice.Descendants().FirstOrDefault(x => x.Name.LocalName == "AccountingCustomerParty");

                // Firmalar arası fatura kontrolü: Satıcı ve Alıcı VKN'lerini al
                string supplierVkn = ExtractVknFromParty(supplierNode?.Descendants().FirstOrDefault(x => x.Name.LocalName == "Party"), GetValue);
                string customerVkn = ExtractVknFromParty(customerNode?.Descendants().FirstOrDefault(x => x.Name.LocalName == "Party"), GetValue);

                // Satıcı ve alıcı firmayı bul
                Firma? saticiFirma = null;
                Firma? aliciFirma = null;
                bool firmalarArasiFatura = false;
                int? karsiFirmaId = null;

                if (!string.IsNullOrWhiteSpace(supplierVkn) && supplierVkn.Length >= 10)
                {
                    saticiFirma = kayitliFirmalar.FirstOrDefault(f => f.VergiNo == supplierVkn);
                }
                if (!string.IsNullOrWhiteSpace(customerVkn) && customerVkn.Length >= 10)
                {
                    aliciFirma = kayitliFirmalar.FirstOrDefault(f => f.VergiNo == customerVkn);
                }

                // Her iki taraf da kayıtlı firma ise, firmalar arası fatura
                if (saticiFirma != null && aliciFirma != null)
                {
                    firmalarArasiFatura = true;
                    // Gelen fatura: Satıcı = karşı firma, firmaId = alıcı firma
                    // Giden fatura: Alıcı = karşı firma, firmaId = satıcı firma
                    if (yon == FaturaYonu.Gelen)
                    {
                        karsiFirmaId = saticiFirma.Id;
                        firmaId = firmaId ?? aliciFirma.Id; // Parametre verilmediyse alıcı firmayı kullan
                    }
                    else
                    {
                        karsiFirmaId = aliciFirma.Id;
                        firmaId = firmaId ?? saticiFirma.Id; // Parametre verilmediyse satıcı firmayı kullan
                    }
                }
                // Tek taraf kayıtlı firma ise, onu firmaId olarak kullan (parametre verilmediyse)
                else if (saticiFirma != null && yon == FaturaYonu.Giden && firmaId == null)
                {
                    firmaId = saticiFirma.Id;
                }
                else if (aliciFirma != null && yon == FaturaYonu.Gelen && firmaId == null)
                {
                    firmaId = aliciFirma.Id;
                }

                var existingFatura = await FindExistingFaturaAsync(context, faturaNo, yon, firmaId);
                if (existingFatura != null)
                {
                    result.Errors.Add($"{file.FileName}: {faturaNo} no'lu {(yon == FaturaYonu.Giden ? "kesilen" : "gelen")} fatura seçilen firma için zaten var.");
                    result.SkippedCount++;
                    continue;
                }

                var targetNode = yon == FaturaYonu.Giden ? customerNode : supplierNode;
                var partyNode = targetNode?.Descendants().FirstOrDefault(x => x.Name.LocalName == "Party");

                if (partyNode != null)
                {
                    // Vergi No / TC Kimlik No - önce bunu al
                    var partyIdentification = partyNode.Descendants().FirstOrDefault(x => x.Name.LocalName == "PartyIdentification");
                    if (partyIdentification != null)
                    {
                        cariVkn = GetValue(partyIdentification, "ID").Trim();
                    }

                    // 11 haneli ise TCKN - şahıs faturası
                    bool sahisFaturasi = !string.IsNullOrEmpty(cariVkn) && cariVkn.Length == 11;

                    // Şahıs faturası ise önce Person'dan ad soyad al
                    if (sahisFaturasi)
                    {
                        var personNode = partyNode.Descendants().FirstOrDefault(x => x.Name.LocalName == "Person");
                        if (personNode != null)
                        {
                            var firstName = GetValue(personNode, "FirstName").Trim();
                            var familyName = GetValue(personNode, "FamilyName").Trim();
                            if (!string.IsNullOrWhiteSpace(firstName) || !string.IsNullOrWhiteSpace(familyName))
                            {
                                cariUnvan = $"{firstName} {familyName}".Trim();
                                cariTcKimlikNo = cariVkn; // TCKN olarak kaydet
                                cariVkn = string.Empty; // Vergi no boş
                            }
                        }
                    }

                    // Şahıs değilse veya Person'dan alınamadıysa PartyName'den al (Ticari Firma)
                    if (string.IsNullOrWhiteSpace(cariUnvan))
                    {
                        var partyNameNode = partyNode.Descendants().FirstOrDefault(x => x.Name.LocalName == "PartyName");
                        if (partyNameNode != null)
                        {
                            var nameValue = GetValue(partyNameNode, "Name").Trim();
                            // Ülke adı değilse kullan
                            if (!IsCountryName(nameValue))
                            {
                                cariUnvan = nameValue;
                            }
                        }
                    }

                    // Hala boşsa eski yöntemle dene
                    if (string.IsNullOrWhiteSpace(cariUnvan))
                    {
                        var nameValue = GetValue(partyNode, "Name").Trim();
                        if (!IsCountryName(nameValue))
                        {
                            cariUnvan = nameValue;
                        }
                    }

                    // Vergi Dairesi - PartyTaxScheme içinden al
                    var taxScheme = partyNode.Descendants().FirstOrDefault(x => x.Name.LocalName == "PartyTaxScheme");
                    if (taxScheme != null)
                    {
                        // RegistrationName (Ticaret sicil adı varsa)
                        var registrationName = GetValue(taxScheme, "RegistrationName").Trim();

                        // TaxOffice veya Name alanından vergi dairesi
                        cariVergiDairesi = GetValue(taxScheme, "TaxOffice").Trim();
                        if (string.IsNullOrWhiteSpace(cariVergiDairesi))
                        {
                            var taxSchemeNode = taxScheme.Descendants().FirstOrDefault(x => x.Name.LocalName == "TaxScheme");
                            if (taxSchemeNode != null)
                            {
                                cariVergiDairesi = GetValue(taxSchemeNode, "Name").Trim();
                            }
                        }
                    }

                    // Web Sitesi
                    var websiteUri = partyNode.Descendants().FirstOrDefault(x => x.Name.LocalName == "WebsiteURI");
                    if (websiteUri != null)
                    {
                        cariWebSitesi = websiteUri.Value.Trim();
                    }

                    // Adres bilgileri - Detaylı
                    var postalAddress = partyNode.Descendants().FirstOrDefault(x => x.Name.LocalName == "PostalAddress");
                    if (postalAddress != null)
                    {
                        var streetName = GetValue(postalAddress, "StreetName").Trim();
                        var buildingName = GetValue(postalAddress, "BuildingName").Trim();
                        var buildingNumber = GetValue(postalAddress, "BuildingNumber").Trim();
                        var room = GetValue(postalAddress, "Room").Trim();
                        var floor = GetValue(postalAddress, "Floor").Trim();
                        var blockName = GetValue(postalAddress, "BlockName").Trim();
                        var region = GetValue(postalAddress, "Region").Trim();
                        var district = GetValue(postalAddress, "District").Trim();
                        var citySubdivisionName = GetValue(postalAddress, "CitySubdivisionName").Trim();
                        var cityName = GetValue(postalAddress, "CityName").Trim();
                        var postalZone = GetValue(postalAddress, "PostalZone").Trim();
                        var countryName = GetValue(postalAddress.Descendants().FirstOrDefault(x => x.Name.LocalName == "Country"), "Name").Trim();

                        // Tam adres oluştur
                        var adresParcalari = new List<string>();
                        if (!string.IsNullOrWhiteSpace(streetName)) adresParcalari.Add(streetName);
                        if (!string.IsNullOrWhiteSpace(buildingName)) adresParcalari.Add(buildingName);
                        if (!string.IsNullOrWhiteSpace(blockName)) adresParcalari.Add($"Blok:{blockName}");
                        if (!string.IsNullOrWhiteSpace(buildingNumber)) adresParcalari.Add($"No:{buildingNumber}");
                        if (!string.IsNullOrWhiteSpace(floor)) adresParcalari.Add($"Kat:{floor}");
                        if (!string.IsNullOrWhiteSpace(room)) adresParcalari.Add($"Daire:{room}");

                        cariAdres = string.Join(" ", adresParcalari);
                        cariIlce = !string.IsNullOrWhiteSpace(citySubdivisionName) ? citySubdivisionName : district;
                        cariIl = cityName;
                        cariUlke = countryName;
                        cariPostaKodu = postalZone;
                    }

                    // İletişim bilgileri - Detaylı
                    var contact = partyNode.Descendants().FirstOrDefault(x => x.Name.LocalName == "Contact");
                    if (contact != null)
                    {
                        cariTelefon = GetValue(contact, "Telephone").Trim();
                        cariFax = GetValue(contact, "Telefax").Trim();
                        cariEmail = GetValue(contact, "ElectronicMail").Trim();

                        // Note alanı varsa notlara ekle
                        var note = GetValue(contact, "Note").Trim();
                        if (!string.IsNullOrWhiteSpace(note))
                        {
                            cariNotlar = note;
                        }
                    }

                    // Fatura notları varsa al
                    var invoiceNotes = invoice.Descendants().Where(x => x.Name.LocalName == "Note").ToList();
                    if (invoiceNotes.Any() && string.IsNullOrWhiteSpace(cariNotlar))
                    {
                        // İlk 500 karakteri al
                        var notlar = string.Join(" | ", invoiceNotes.Select(n => n.Value.Trim()).Where(n => !string.IsNullOrWhiteSpace(n)));
                        if (notlar.Length > 500) notlar = notlar.Substring(0, 500);
                        // Cari notlarına ekleme (fatura notları değil, cari hakkında bilgi içeriyorsa)
                    }
                }

                if (string.IsNullOrWhiteSpace(cariUnvan))
                {
                    result.Errors.Add($"{file.FileName}: Cari unvan bilgisi XML içinden çıkarılamadı.");
                    result.ErrorCount++;
                    continue;
                }

                Cari? cari = null;
                // TCKN varsa ona göre ara, yoksa VKN'ye göre
                var aramaNo = !string.IsNullOrWhiteSpace(cariTcKimlikNo) ? cariTcKimlikNo : cariVkn;
                var cariKey = !string.IsNullOrWhiteSpace(aramaNo) ? aramaNo : cariUnvan.ToLowerInvariant();

                if (!string.IsNullOrEmpty(cariKey) && importCarileri.TryGetValue(cariKey, out var mevcutCari))
                {
                    cari = mevcutCari;
                }

                // TCKN ile ara
                if (cari == null && !string.IsNullOrWhiteSpace(cariTcKimlikNo))
                {
                    cari = await context.Cariler.Include(c => c.MuhasebeHesap).FirstOrDefaultAsync(c => c.TcKimlikNo == cariTcKimlikNo && (firmaId == null || c.FirmaId == null || c.FirmaId == firmaId));
                }

                // VKN ile ara
                if (cari == null && !string.IsNullOrWhiteSpace(cariVkn) && cariVkn.Length >= 10)
                {
                    cari = await context.Cariler.Include(c => c.MuhasebeHesap).FirstOrDefaultAsync(c => c.VergiNo == cariVkn && (firmaId == null || c.FirmaId == null || c.FirmaId == firmaId));
                }

                // Unvan ile ara
                if (cari == null)
                {
                    cari = await context.Cariler.Include(c => c.MuhasebeHesap).FirstOrDefaultAsync(c => c.Unvan.ToLower() == cariUnvan.ToLower() && (firmaId == null || c.FirmaId == null || c.FirmaId == firmaId));
                }

                // Mevcut cari varsa eksik bilgileri güncelle
                if (cari != null)
                {
                    bool guncellendi = false;

                    // Vergi Dairesi
                    if (string.IsNullOrWhiteSpace(cari.VergiDairesi) && !string.IsNullOrWhiteSpace(cariVergiDairesi))
                    {
                        cari.VergiDairesi = cariVergiDairesi;
                        guncellendi = true;
                    }
                    // Vergi No
                    if (string.IsNullOrWhiteSpace(cari.VergiNo) && !string.IsNullOrWhiteSpace(cariVkn))
                    {
                        cari.VergiNo = cariVkn;
                        guncellendi = true;
                    }
                    // TCKN
                    if (string.IsNullOrWhiteSpace(cari.TcKimlikNo) && !string.IsNullOrWhiteSpace(cariTcKimlikNo))
                    {
                        cari.TcKimlikNo = cariTcKimlikNo;
                        guncellendi = true;
                    }
                    // Adres
                    if (string.IsNullOrWhiteSpace(cari.Adres) && !string.IsNullOrWhiteSpace(cariAdres))
                    {
                        cari.Adres = cariAdres;
                        guncellendi = true;
                    }
                    // İl
                    if (string.IsNullOrWhiteSpace(cari.Il) && !string.IsNullOrWhiteSpace(cariIl))
                    {
                        cari.Il = cariIl;
                        guncellendi = true;
                    }
                    // İlçe
                    if (string.IsNullOrWhiteSpace(cari.Ilce) && !string.IsNullOrWhiteSpace(cariIlce))
                    {
                        cari.Ilce = cariIlce;
                        guncellendi = true;
                    }
                    // Posta Kodu
                    if (string.IsNullOrWhiteSpace(cari.PostaKodu) && !string.IsNullOrWhiteSpace(cariPostaKodu))
                    {
                        cari.PostaKodu = cariPostaKodu;
                        guncellendi = true;
                    }
                    // Telefon
                    if (string.IsNullOrWhiteSpace(cari.Telefon) && !string.IsNullOrWhiteSpace(cariTelefon))
                    {
                        cari.Telefon = cariTelefon;
                        guncellendi = true;
                    }
                    // Fax
                    if (string.IsNullOrWhiteSpace(cari.Fax) && !string.IsNullOrWhiteSpace(cariFax))
                    {
                        cari.Fax = cariFax;
                        guncellendi = true;
                    }
                    // Email
                    if (string.IsNullOrWhiteSpace(cari.Email) && !string.IsNullOrWhiteSpace(cariEmail))
                    {
                        cari.Email = cariEmail;
                        guncellendi = true;
                    }
                    // Web Sitesi
                    if (string.IsNullOrWhiteSpace(cari.WebSitesi) && !string.IsNullOrWhiteSpace(cariWebSitesi))
                    {
                        cari.WebSitesi = cariWebSitesi;
                        guncellendi = true;
                    }
                    // Notlar
                    if (string.IsNullOrWhiteSpace(cari.Notlar) && !string.IsNullOrWhiteSpace(cariNotlar))
                    {
                        cari.Notlar = cariNotlar;
                        guncellendi = true;
                    }

                    if (guncellendi)
                    {
                        cari.UpdatedAt = DateTime.UtcNow;
                        await context.SaveChangesAsync();
                    }
                }

                if (cari == null && otomatikCariOlustur)
                {
                    var uniqueCode = await GetUniqueCariCodeAsync(context, nextCariNum);
                    var cariTipi = yon == FaturaYonu.Giden ? CariTipi.Musteri : CariTipi.Tedarikci;

                    cari = new Cari
                    {
                        CariKodu = uniqueCode,
                        Unvan = cariUnvan,
                        VergiNo = cariVkn ?? string.Empty,
                        TcKimlikNo = cariTcKimlikNo ?? string.Empty,
                        FirmaId = firmaId,
                        VergiDairesi = cariVergiDairesi,
                        Adres = cariAdres,
                        Il = cariIl,
                        Ilce = cariIlce,
                        PostaKodu = cariPostaKodu,
                        Telefon = cariTelefon,
                        Fax = cariFax,
                        Email = cariEmail,
                        WebSitesi = cariWebSitesi,
                        Notlar = cariNotlar,
                        CariTipi = cariTipi,
                        Aktif = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    context.Cariler.Add(cari);
                    await context.SaveChangesAsync();
                    nextCariNum++;

                    // Otomatik muhasebe hesap kodu oluştur
                    if (otomatikHesapKoduOlustur && ayar != null)
                    {
                        var prefix = cariTipi == CariTipi.Musteri ? ayar.MusteriPrefix : ayar.TedarikciPrefix;
                        var yeniHesapKodu = await GetSonrakiHesapKoduAsync(context, prefix);

                        var yeniHesap = new MuhasebeHesap
                        {
                            HesapKodu = yeniHesapKodu,
                            HesapAdi = cariUnvan,
                            HesapTuru = cariTipi == CariTipi.Musteri ? HesapTuru.Aktif : HesapTuru.Pasif,
                            HesapGrubu = cariTipi == CariTipi.Musteri ? HesapGrubu.DonenVarliklar : HesapGrubu.KisaVadeliYabanciKaynaklar,
                            Aktif = true,
                            CreatedAt = DateTime.UtcNow
                        };

                        context.MuhasebeHesaplari.Add(yeniHesap);
                        await context.SaveChangesAsync();

                        cari.MuhasebeHesapId = yeniHesap.Id;
                        await context.SaveChangesAsync();
                    }
                }

                if (cari == null)
                {
                    result.Errors.Add($"{file.FileName}: Cari bulunamadı ve otomatik oluşturma kapalı.");
                    result.ErrorCount++;
                    continue;
                }

                if (!string.IsNullOrEmpty(cariKey))
                {
                    importCarileri[cariKey] = cari;
                }

                var legalMonetaryTotal = invoice.Descendants().FirstOrDefault(x => x.Name.LocalName == "LegalMonetaryTotal");
                var dAraToplam = GetDecimalValue(legalMonetaryTotal, "TaxExclusiveAmount");
                var dGenelToplam = GetDecimalValue(legalMonetaryTotal, "PayableAmount");
                if (dGenelToplam == 0)
                    dGenelToplam = GetDecimalValue(legalMonetaryTotal, "TaxInclusiveAmount");

                var dKdvTutar = dGenelToplam - dAraToplam;

                // Tevikifat bilgilerini oku
                var tevikifatliMi = false;
                decimal tevikifatOrani = 0;
                decimal tevikifatTutar = 0;
                string? tevikifatKodu = null;

                var withholdingTaxTotal = invoice.Descendants().FirstOrDefault(x => x.Name.LocalName == "WithholdingTaxTotal");
                if (withholdingTaxTotal != null)
                {
                    tevikifatliMi = true;
                    tevikifatTutar = GetDecimalValue(withholdingTaxTotal, "TaxAmount");

                    var taxSubtotal = withholdingTaxTotal.Descendants().FirstOrDefault(x => x.Name.LocalName == "TaxSubtotal");
                    if (taxSubtotal != null)
                    {
                        tevikifatOrani = GetDecimalValue(taxSubtotal, "Percent");
                        var taxCategory = taxSubtotal.Descendants().FirstOrDefault(x => x.Name.LocalName == "TaxCategory");
                        if (taxCategory != null)
                        {
                            var taxScheme = taxCategory.Descendants().FirstOrDefault(x => x.Name.LocalName == "TaxScheme");
                            tevikifatKodu = GetValue(taxScheme, "TaxTypeCode");
                        }
                    }
                }

                if (dGenelToplam <= 0 && dAraToplam <= 0)
                {
                    result.Errors.Add($"{file.FileName}: Tutar bilgisi eksik.");
                    result.ErrorCount++;
                    continue;
                }

                // UUID/EttnNo uniqueness check — mükerrer fatura engelle
                if (!string.IsNullOrWhiteSpace(ettn))
                {
                    var duplicateExists = await context.Faturalar
                        .IgnoreQueryFilters()
                        .AsNoTracking()
                        .AnyAsync(f => f.EttnNo == ettn);
                    if (duplicateExists)
                    {
                        result.Errors.Add($"{file.FileName}: Bu UUID ({ettn}) zaten kayıtlı — atlandı.");
                        result.SkippedCount++;
                        continue;
                    }
                }

                var fatura = new Fatura
                {
                    FaturaNo = faturaNo,
                    FaturaTarihi = faturaTarihi,
                    CariId = cari.Id,
                    FirmaId = firmaId,
                    FaturaYonu = yon,
                    FaturaTipi = tevikifatliMi ? FaturaTipi.TevkifatliFatura : (yon == FaturaYonu.Giden ? FaturaTipi.SatisFaturasi : FaturaTipi.AlisFaturasi),
                    EFaturaTipi = fatTip,
                    EttnNo = ettn,
                    AraToplam = dAraToplam > 0 ? dAraToplam : dGenelToplam,
                    IskontoTutar = 0,
                    KdvTutar = dKdvTutar,
                    GenelToplam = dGenelToplam,
                    TevkifatliMi = tevikifatliMi,
                    TevkifatOrani = tevikifatOrani,
                    TevkifatKodu = tevikifatKodu,
                    TevkifatTutar = tevikifatTutar,
                    Durum = FaturaDurum.Beklemede,
                    ImportKaynak = "XML",
                    FirmalarArasiFatura = firmalarArasiFatura,
                    KarsiFirmaId = karsiFirmaId,
                    CreatedAt = DateTime.UtcNow
                };

                // Fatura kalemlerini oku
                var invoiceLines = invoice.Descendants().Where(x => x.Name.LocalName == "InvoiceLine").ToList();
                var siraNo = 1;

                foreach (var line in invoiceLines)
                {
                    var urunKodu = GetValue(line, "ID");
                    var aciklama = GetInvoiceLineAciklama(line, cari?.Unvan, urunKodu);

                    var kalem = new FaturaKalem
                    {
                        SiraNo = siraNo++,
                        UrunKodu = urunKodu,
                        Aciklama = aciklama,
                        Miktar = GetDecimalValue(line, "InvoicedQuantity"),
                        BirimFiyat = GetDecimalValue(line.Descendants().FirstOrDefault(x => x.Name.LocalName == "Price"), "PriceAmount"),
                        KdvOrani = GetDecimalValue(line.Descendants().FirstOrDefault(x => x.Name.LocalName == "TaxTotal")?.Descendants().FirstOrDefault(x => x.Name.LocalName == "TaxSubtotal")?.Descendants().FirstOrDefault(x => x.Name.LocalName == "TaxCategory"), "Percent"),
                        KdvTutar = GetDecimalValue(line.Descendants().FirstOrDefault(x => x.Name.LocalName == "TaxTotal"), "TaxAmount"),
                        ToplamTutar = GetDecimalValue(line, "LineExtensionAmount"),
                        CreatedAt = DateTime.UtcNow
                    };

                    // Birim bilgisini al
                    var quantityElement = line.Descendants().FirstOrDefault(x => x.Name.LocalName == "InvoicedQuantity");
                    if (quantityElement != null)
                    {
                        var unitCode = quantityElement.Attribute("unitCode")?.Value;
                        kalem.Birim = unitCode ?? "Adet";
                    }

                    // İskonto bilgisini al
                    var allowanceCharge = line.Descendants().FirstOrDefault(x => x.Name.LocalName == "AllowanceCharge");
                    if (allowanceCharge != null)
                    {
                        var chargeIndicator = GetValue(allowanceCharge, "ChargeIndicator");
                        if (chargeIndicator == "false")
                        {
                            kalem.IskontoTutar = GetDecimalValue(allowanceCharge, "Amount");
                            kalem.IskontoOrani = GetDecimalValue(allowanceCharge, "MultiplierFactorNumeric") * 100;
                        }
                    }

                    // Kalem bazında tevikifat
                    var lineWithholdingTax = line.Descendants().FirstOrDefault(x => x.Name.LocalName == "WithholdingTaxTotal");
                    if (lineWithholdingTax != null)
                    {
                        kalem.TevkifatTutar = GetDecimalValue(lineWithholdingTax, "TaxAmount");
                        var lineTaxSubtotal = lineWithholdingTax.Descendants().FirstOrDefault(x => x.Name.LocalName == "TaxSubtotal");
                        if (lineTaxSubtotal != null)
                        {
                            kalem.TevkifatOrani = GetDecimalValue(lineTaxSubtotal, "Percent");
                        }
                    }

                    // Kalem tipini belirle (açıklamaya göre)
                    kalem.KalemTipi = DetermineKalemTipi(aciklama, urunKodu);
                    kalem.AltTipi = DetermineKalemAltTipi(aciklama, urunKodu, kalem.KalemTipi);

                    // Araç ilişkisi - açıklamada şase/plaka varsa
                    var aracBilgisi = ExtractAracBilgisi(aciklama);
                    if (aracBilgisi.HasValue && cari != null)
                    {
                        var arac = await FindOrCreateAracAsync(context, aracBilgisi.Value, yon, cari.Id);
                        if (arac != null)
                        {
                            kalem.AracId = arac.Id;
                            kalem.KalemTipi = FaturaKalemTipi.Arac;
                            kalem.AltTipi = yon == FaturaYonu.Giden ? FaturaKalemAltTipi.AracSatis : FaturaKalemAltTipi.AracAlis;

                            // Ana faturaya da araç ilişkisi ekle
                            fatura.AracId = arac.Id;
                            fatura.AracFaturasi = true;
                        }
                    }

                    // Varsayılan muhasebe hesabı ata (kalem tipine göre)
                    if (otomatikHesapKoduOlustur && ayar != null)
                    {
                        var hesapKodu = GetMuhasebeHesapKoduByKalemTipi(kalem.KalemTipi, yon, ayar);
                        var hesap = await context.MuhasebeHesaplari.FirstOrDefaultAsync(h => h.HesapKodu == hesapKodu);
                        if (hesap != null)
                        {
                            kalem.MuhasebeHesapId = hesap.Id;
                        }
                    }

                    fatura.FaturaKalemleri.Add(kalem);
                }

                context.Faturalar.Add(fatura);
                await context.SaveChangesAsync();

                await SaveInvoiceXmlAsync(context, fatura, file.FileName, file.Content);

                // Otomatik muhasebe fişi oluştur
                await TryCreateMuhasebeFisiAsync(context, fatura);

                // Stok hareketi oluştur (Gelen: Giriş, Giden: Çıkış)
                await CreateStokHareketleriFromFaturaAsync(context, fatura, yon);

                result.ImportedItems.Add(fatura);
                result.FaturaXmlMapping[fatura.Id] = file.FileName; // XML dosya adını kaydet
                result.ImportedCount++;
            }
            catch (Exception ex)
            {
                result.Errors.Add($"{file.FileName} parse hatası: {ex.InnerException?.Message ?? ex.Message}");
                result.ErrorCount++;

                foreach (var entry in context.ChangeTracker.Entries().ToList())
                {
                    if (entry.State == EntityState.Added)
                    {
                        entry.State = EntityState.Detached;
                    }
                }
            }
        }

        result.Success = result.ImportedCount > 0;
        return result;
    }

    public async Task<EFaturaImportResult> ImportFromXmlWithPdfAsync(List<XmlPdfFileContent> files, FaturaYonu yon, int? firmaId = null, EFaturaTipi? eFaturaTipi = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        // Önce XML'leri import et
        var xmlContents = files.Select(f => new XmlFileContent { FileName = f.XmlFileName, Content = f.XmlContent }).ToList();
        var result = await ImportFromXmlAsync(xmlContents, yon, firmaId, eFaturaTipi);

        // XML dosya adı -> XmlPdfFileContent eşleştirmesi (doğrudan PDF içeriğini içeriyor)
        var xmlFileMap = files.ToDictionary(
            f => f.XmlFileName.ToLowerInvariant(),
            f => f
        );

        // Başarıyla import edilen faturalara PDF'leri kaydet
        foreach (var fatura in result.ImportedItems)
        {
            // FaturaXmlMapping'den bu faturanın hangi XML'den geldiğini bul
            if (result.FaturaXmlMapping.TryGetValue(fatura.Id, out var xmlFileName))
            {
                // Bu XML'e ait XmlPdfFileContent'i bul
                if (xmlFileMap.TryGetValue(xmlFileName.ToLowerInvariant(), out var xmlPdfFile))
                {
                    // Bu XML'in kendi PDF'i var mı?
                    if (!string.IsNullOrEmpty(xmlPdfFile.PdfFileName) && xmlPdfFile.PdfContent != null)
                    {
                        await UploadFaturaPdfAsync(fatura.Id, xmlPdfFile.PdfFileName, xmlPdfFile.PdfContent);
                    }
                }
            }
        }

        return result;
    }

    public async Task<bool> UploadFaturaPdfAsync(int faturaId, string fileName, byte[] pdfContent)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var fatura = await context.Faturalar.FindAsync(faturaId);
        if (fatura == null) return false;

        try
        {
            ValidateStoredFileExtension(fileName, ".pdf");

            if (!string.IsNullOrWhiteSpace(fatura.PdfDosyaYolu) && !IsLegacyUploadPath(fatura.PdfDosyaYolu))
                await _secureFileService.DeleteAsync(fatura.PdfDosyaYolu);

            fatura.PdfDosyaYolu = await _secureFileService.SaveEncryptedAsync(
                Path.Combine("faturalar", fatura.FaturaYonu.ToString().ToLowerInvariant(), "pdf"),
                fileName,
                pdfContent);
            fatura.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();

            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<FaturaStoredFile?> GetFaturaDosyaAsync(int faturaId, FaturaDosyaTuru dosyaTuru)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var fatura = await context.Faturalar.FindAsync(faturaId);
        if (fatura == null)
            return null;

        var storedPath = dosyaTuru == FaturaDosyaTuru.Pdf ? fatura.PdfDosyaYolu : fatura.XmlDosyaYolu;
        if (string.IsNullOrWhiteSpace(storedPath))
            return null;

        byte[]? content;
        if (IsLegacyUploadPath(storedPath))
        {
            content = await ReadLegacyUploadAsync(context, storedPath);
        }
        else
        {
            content = await _secureFileService.ReadDecryptedAsync(storedPath);
        }

        if (content == null)
            return null;

        var extension = dosyaTuru == FaturaDosyaTuru.Pdf ? ".pdf" : ".xml";
        return new FaturaStoredFile
        {
            FileName = $"{NormalizeFaturaNo(fatura.FaturaNo)}{extension}",
            ContentType = dosyaTuru == FaturaDosyaTuru.Pdf ? "application/pdf" : "application/xml",
            Content = content
        };
    }

    private async Task PrepareFaturaForSaveAsync(ApplicationDbContext context, Fatura fatura)
    {
        fatura.FaturaNo = NormalizeFaturaNo(fatura.FaturaNo);
        fatura.FaturaTarihi = DateTime.SpecifyKind(fatura.FaturaTarihi, DateTimeKind.Utc);

        if (fatura.VadeTarihi.HasValue)
            fatura.VadeTarihi = DateTime.SpecifyKind(fatura.VadeTarihi.Value, DateTimeKind.Utc);

        if (!fatura.FirmaId.HasValue && fatura.CariId > 0)
        {
            fatura.FirmaId = await context.Cariler
                .Where(c => c.Id == fatura.CariId)
                .Select(c => c.FirmaId)
                .FirstOrDefaultAsync();
        }

        if (fatura.FirmalarArasiFatura)
        {
            if (!fatura.FirmaId.HasValue)
                throw new InvalidOperationException("Firmalar arası faturada kaynak firma seçilmelidir.");

            if (!fatura.KarsiFirmaId.HasValue)
                throw new InvalidOperationException("Firmalar arası faturada karşı firma seçilmelidir.");

            if (fatura.FirmaId == fatura.KarsiFirmaId)
                throw new InvalidOperationException("Kaynak firma ile karşı firma aynı olamaz.");
        }
        else
        {
            fatura.KarsiFirmaId = null;
        }

        var ayniNumara = await FindExistingFaturaAsync(context, fatura.FaturaNo, fatura.FaturaYonu, fatura.FirmaId, fatura.Id > 0 ? fatura.Id : null);
        if (ayniNumara != null)
            throw new InvalidOperationException($"'{fatura.FaturaNo}' numaralı {(fatura.FaturaYonu == FaturaYonu.Giden ? "kesilen" : "gelen")} fatura seçilen firma için zaten kayıtlı.");

        if (fatura.CreatedAt == default)
            fatura.CreatedAt = DateTime.UtcNow;

        fatura.UpdatedAt = DateTime.UtcNow;
    }

    private async Task SaveInvoiceXmlAsync(ApplicationDbContext context, Fatura fatura, string fileName, byte[] xmlContent)
    {
        ValidateStoredXmlFileExtension(fileName);

        var normalizedFileName = NormalizeXmlFileName(fileName);

        if (!string.IsNullOrWhiteSpace(fatura.XmlDosyaYolu) && !IsLegacyUploadPath(fatura.XmlDosyaYolu))
            await _secureFileService.DeleteAsync(fatura.XmlDosyaYolu);

        fatura.XmlDosyaYolu = await _secureFileService.SaveEncryptedAsync(
            Path.Combine("faturalar", fatura.FaturaYonu.ToString().ToLowerInvariant(), "xml"),
            normalizedFileName,
            xmlContent);

        fatura.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
    }

    private static void ValidateStoredFileExtension(string fileName, string expectedExtension)
    {
        var extension = Path.GetExtension(fileName);
        if (!string.Equals(extension, expectedExtension, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Sadece {expectedExtension} uzantılı dosyalar kabul edilir.");
    }

    private static void ValidateStoredXmlFileExtension(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        if (!string.Equals(extension, ".xml", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(extension, ".xlm", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Sadece .xml veya .xlm uzantılı dosyalar kabul edilir.");
        }
    }

    private static string NormalizeXmlFileName(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        return string.Equals(extension, ".xlm", StringComparison.OrdinalIgnoreCase)
            ? Path.ChangeExtension(fileName, ".xml")
            : fileName;
    }

    private static bool IsLegacyUploadPath(string path)
        => path.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase) || path.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase);

    private async Task<byte[]?> ReadLegacyUploadAsync(ApplicationDbContext context, string legacyPath)
    {
        var relativePath = legacyPath
            .Replace("/uploads/", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("uploads/", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace('/', Path.DirectorySeparatorChar);

        var uploadsRoot = AppStoragePaths.GetUploadsRoot(_env.ContentRootPath);
        var fullPath = Path.GetFullPath(Path.Combine(uploadsRoot, relativePath));
        var normalizedRoot = Path.GetFullPath(uploadsRoot);

        if (!fullPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase) || !File.Exists(fullPath))
            return null;

        return await File.ReadAllBytesAsync(fullPath);
    }

    private async Task<string> GetSonrakiHesapKoduAsync(ApplicationDbContext context, string prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
            return string.Empty;

        var sonKod = await context.MuhasebeHesaplari
            .Where(h => h.HesapKodu.StartsWith(prefix + "."))
            .OrderByDescending(h => h.HesapKodu)
            .Select(h => h.HesapKodu)
            .FirstOrDefaultAsync();

        if (string.IsNullOrWhiteSpace(sonKod))
            return $"{prefix}.001";

        var sonParca = sonKod.Split('.').LastOrDefault();
        if (!int.TryParse(sonParca, out var sonNumara))
            return $"{prefix}.001";

        return $"{prefix}.{sonNumara + 1:D3}";
    }

    private static bool IsCountryName(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;

        var upper = value.ToUpperInvariant().Trim();
        var countryNames = new[]
        {
            "TÜRKİYE", "TURKIYE", "TURKEY", "TR",
            "ALMANYA", "GERMANY", "FRANSA", "FRANCE",
            "İNGİLTERE", "INGILTERE", "ENGLAND", "UK",
            "ABD", "USA", "AMERIKA", "AMERICA",
            "HOLLANDA", "NETHERLANDS", "BELÇİKA", "BELGIUM"
        };

        return countryNames.Contains(upper);
    }

    private async Task<Fatura?> FindExistingFaturaAsync(ApplicationDbContext context, string faturaNo, FaturaYonu? yon = null, int? firmaId = null, int? excludeId = null)
    {
        var normalizedFaturaNo = NormalizeFaturaNo(faturaNo);
        if (string.IsNullOrWhiteSpace(normalizedFaturaNo))
            return null;

        var query = context.Faturalar.Where(f => !f.IsDeleted);

        if (yon.HasValue)
            query = query.Where(f => f.FaturaYonu == yon.Value);

        if (firmaId.HasValue)
            query = query.Where(f => f.FirmaId == firmaId.Value);

        if (excludeId.HasValue)
            query = query.Where(f => f.Id != excludeId.Value);

        return await query.FirstOrDefaultAsync(f => f.FaturaNo == normalizedFaturaNo || f.FaturaNo == faturaNo);
    }

    private async Task<string> GetUniqueCariCodeAsync(ApplicationDbContext context, int startNum)
    {
        var year = DateTime.UtcNow.Year % 100;
        string code;
        int num = startNum;

        do
        {
            code = $"C{year}{num:D4}";
            num++;
        } while (await context.Cariler.AnyAsync(c => c.CariKodu == code));

        return code;
    }

    private static string NormalizeFaturaNo(string faturaNo)
    {
        if (string.IsNullOrWhiteSpace(faturaNo)) return faturaNo;
        return faturaNo.Trim().ToUpperInvariant();
    }

    private static decimal ParseDecimal(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return 0;

        value = value.Trim();

        // XML'den gelen değerlerde nokta ondalık ayracı olarak kullanılır
        // Türkçe formatı düzelt: virgülü noktaya çevir, binlik ayracı olan noktayı kaldır

        // Eğer hem nokta hem virgül varsa, hangisi ondalık ayracı?
        var dotIndex = value.LastIndexOf('.');
        var commaIndex = value.LastIndexOf(',');

        if (dotIndex > -1 && commaIndex > -1)
        {
            // Her ikisi de var - sonuncusu ondalık ayracı
            if (commaIndex > dotIndex)
            {
                // Virgül ondalık ayracı (Türkçe format: 1.234,56)
                value = value.Replace(".", "").Replace(",", ".");
            }
            else
            {
                // Nokta ondalık ayracı (İngilizce format: 1,234.56)
                value = value.Replace(",", "");
            }
        }
        else if (commaIndex > -1)
        {
            // Sadece virgül var - ondalık ayracı olarak kabul et
            value = value.Replace(",", ".");
        }
        // Sadece nokta varsa olduğu gibi bırak (İngilizce format)

        if (decimal.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var result))
            return result;

        return 0;
    }

    private async Task TryCreateMuhasebeFisiAsync(ApplicationDbContext context, Fatura fatura)
    {
        try
        {
            // Muhasebe fişi otomatik oluşturma devre dışı veya firma yoksa çık
            if (fatura.FirmaId == null) return;

            // İlgili ayar var mı kontrol et
            var ayar = await context.MuhasebeAyarlari.FirstOrDefaultAsync();
            if (ayar == null || !ayar.FaturaOtomatikMuhasebeFisi) return;

            // Fatura kalemlerini yükle
            await context.Entry(fatura).Collection(f => f.FaturaKalemleri).LoadAsync();

            // Cari bilgisini yükle
            if (fatura.Cari == null)
                await context.Entry(fatura).Reference(f => f.Cari).LoadAsync();

            // Muhasebe fişini oluştur (MuhasebeService tam muhasebe kaydını oluşturur)
            await _muhasebeService.CreateFaturaFisiAsync(fatura);
        }
        catch (Exception ex)
        {
            // Muhasebe fişi oluşturma hatası fatura kaydını engellemesin, loglansın
            System.Diagnostics.Debug.WriteLine($"[UYARI] Otomatik muhasebe fişi oluşturulamadı (Fatura: {fatura.FaturaNo}): {ex.Message}");
        }
    }

    private void CalculateTotals(Fatura fatura)
    {
        // Fatura toplamlarını hesapla
        if (fatura.FaturaKalemleri != null && fatura.FaturaKalemleri.Any())
        {
            fatura.AraToplam = fatura.FaturaKalemleri.Sum(k => k.ToplamTutar);
            fatura.KdvTutar = fatura.FaturaKalemleri.Sum(k => k.KdvTutar);
            fatura.GenelToplam = fatura.AraToplam + fatura.KdvTutar - fatura.IskontoTutar;
        }
    }

    private async Task<int> GetNextCariNumAsync(ApplicationDbContext context)
    {
        var year = DateTime.UtcNow.Year % 100;
        var prefix = $"C{year}";

        var sonCari = await context.Cariler
            .Where(c => c.CariKodu.StartsWith(prefix))
            .OrderByDescending(c => c.CariKodu)
            .FirstOrDefaultAsync();

        if (sonCari == null)
            return 1;

        var numStr = sonCari.CariKodu.Replace(prefix, "");
        if (int.TryParse(numStr, out var num))
            return num + 1;

        return 1;
    }

    #endregion

    #region Fatura Kalemleri - Stok Türü Eşleştirme

    public async Task<List<FaturaKalem>> GetFaturaKalemleriAsync(DateTime? baslangic = null, DateTime? bitis = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.FaturaKalemleri
            .Include(k => k.Fatura)
                .ThenInclude(f => f.Cari)
            .Where(k => !k.IsDeleted && !k.Fatura.IsDeleted)
            .Where(k => !context.Guzergahlar.Any(g => !g.IsDeleted && g.FaturaKalemId == k.Id));

        if (baslangic.HasValue)
        {
            var start = DateTime.SpecifyKind(baslangic.Value.Date, DateTimeKind.Utc);
            query = query.Where(k => k.Fatura.FaturaTarihi >= start);
        }

        if (bitis.HasValue)
        {
            var end = DateTime.SpecifyKind(bitis.Value.Date.AddDays(1), DateTimeKind.Utc);
            query = query.Where(k => k.Fatura.FaturaTarihi < end);
        }

        return await query
            .OrderByDescending(k => k.Fatura.FaturaTarihi)
            .ThenBy(k => k.Fatura.FaturaNo)
            .ThenBy(k => k.SiraNo)
            .ToListAsync();
    }

    public async Task<List<FaturaKalem>> GetEslesmemisKalemleriAsync(DateTime? baslangic = null, DateTime? bitis = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        // Mevcut stok kartlarının adlarını ve kodlarını al
        var stokKartlari = await context.StokKartlari
            .Where(s => !s.IsDeleted)
            .Select(s => new { Adi = s.StokAdi.ToLower().Trim(), Kodu = s.StokKodu.ToUpper().Trim() })
            .ToListAsync();
        
        var stokAdlariSet = stokKartlari.Select(s => s.Adi).ToHashSet();
        var stokKodlariSet = stokKartlari.Where(s => !string.IsNullOrWhiteSpace(s.Kodu)).Select(s => s.Kodu).ToHashSet();

        var query = context.FaturaKalemleri
            .Include(k => k.Fatura)
                .ThenInclude(f => f.Cari)
            .Where(k => !k.IsDeleted && !k.Fatura.IsDeleted)
            .Where(k => !context.Guzergahlar.Any(g => !g.IsDeleted && g.FaturaKalemId == k.Id));

        if (baslangic.HasValue)
        {
            var start = DateTime.SpecifyKind(baslangic.Value.Date, DateTimeKind.Utc);
            query = query.Where(k => k.Fatura.FaturaTarihi >= start);
        }

        if (bitis.HasValue)
        {
            var end = DateTime.SpecifyKind(bitis.Value.Date.AddDays(1), DateTimeKind.Utc);
            query = query.Where(k => k.Fatura.FaturaTarihi < end);
        }

        var kalemler = await query
            .OrderByDescending(k => k.Fatura.FaturaTarihi)
            .ThenBy(k => k.Fatura.FaturaNo)
            .ThenBy(k => k.SiraNo)
            .ToListAsync();

        // Eşleşmemiş olanları filtrele (stok kartı olmayan) ve tekrarlayanları çıkar
        var eslesmemisler = kalemler.Where(k =>
            string.IsNullOrWhiteSpace(k.Aciklama) ||
            (!stokAdlariSet.Contains(k.Aciklama.ToLower().Trim()) &&
             (string.IsNullOrWhiteSpace(k.UrunKodu) || !stokKodlariSet.Contains(k.UrunKodu.ToUpper().Trim())))
        ).ToList();

        // Tekrarlayan açıklamaları filtrele - sadece benzersiz açıklamaları göster
        var benzersizAciklamalar = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var filtrelenmis = new List<FaturaKalem>();
        
        foreach (var kalem in eslesmemisler)
        {
            var key = (kalem.Aciklama ?? "").Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(key))
            {
                filtrelenmis.Add(kalem);
                continue;
            }
            
            if (!benzersizAciklamalar.Contains(key))
            {
                benzersizAciklamalar.Add(key);
                filtrelenmis.Add(kalem);
            }
        }

        return filtrelenmis;
    }

    public async Task<List<FaturaKalem>> GetEslesmisKalemleriAsync(DateTime? baslangic = null, DateTime? bitis = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        // Mevcut stok kartlarının adlarını ve kodlarını al
        var stokKartlari = await context.StokKartlari
            .Where(s => !s.IsDeleted)
            .Select(s => new { Adi = s.StokAdi.ToLower().Trim(), Kodu = s.StokKodu.ToUpper().Trim() })
            .ToListAsync();
        
        var stokAdlariSet = stokKartlari.Select(s => s.Adi).ToHashSet();
        var stokKodlariSet = stokKartlari.Where(s => !string.IsNullOrWhiteSpace(s.Kodu)).Select(s => s.Kodu).ToHashSet();

        var query = context.FaturaKalemleri
            .Include(k => k.Fatura)
                .ThenInclude(f => f.Cari)
            .Where(k => !k.IsDeleted && !k.Fatura.IsDeleted);

        if (baslangic.HasValue)
        {
            var start = DateTime.SpecifyKind(baslangic.Value.Date, DateTimeKind.Utc);
            query = query.Where(k => k.Fatura.FaturaTarihi >= start);
        }

        if (bitis.HasValue)
        {
            var end = DateTime.SpecifyKind(bitis.Value.Date.AddDays(1), DateTimeKind.Utc);
            query = query.Where(k => k.Fatura.FaturaTarihi < end);
        }

        var kalemler = await query
            .OrderByDescending(k => k.Fatura.FaturaTarihi)
            .ThenBy(k => k.Fatura.FaturaNo)
            .ThenBy(k => k.SiraNo)
            .ToListAsync();

        // Eşleşmiş olanları filtrele (stok kartı olan - ad veya kod eşleşmesi)
        var eslesmisler = kalemler.Where(k =>
            !string.IsNullOrWhiteSpace(k.Aciklama) &&
            (stokAdlariSet.Contains(k.Aciklama.ToLower().Trim()) ||
             (!string.IsNullOrWhiteSpace(k.UrunKodu) && stokKodlariSet.Contains(k.UrunKodu.ToUpper().Trim())))
        ).ToList();

        // Tekrarlayan açıklamaları filtrele - sadece benzersiz stok kartlarını göster
        var benzersizAciklamalar = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var filtrelenmis = new List<FaturaKalem>();
        
        foreach (var kalem in eslesmisler)
        {
            var key = (kalem.Aciklama ?? "").Trim().ToLowerInvariant();
            if (!benzersizAciklamalar.Contains(key))
            {
                benzersizAciklamalar.Add(key);
                filtrelenmis.Add(kalem);
            }
        }

        return filtrelenmis;
    }

    public async Task UpdateFaturaKalemleriAsync(List<FaturaKalem> kalemler)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        if (kalemler == null || !kalemler.Any())
            return;

        var kalemIds = kalemler.Select(k => k.Id).ToList();
        var mevcutKalemler = await context.FaturaKalemleri
            .AsTracking()
            .Include(k => k.Fatura)
                .ThenInclude(f => f.Cari)
            .Where(k => kalemIds.Contains(k.Id))
            .ToListAsync();

        foreach (var kalem in kalemler)
        {
            var existing = mevcutKalemler.FirstOrDefault(k => k.Id == kalem.Id);
            if (existing != null)
            {
                var yeniUrunKodu = string.IsNullOrWhiteSpace(kalem.UrunKodu) ? null : kalem.UrunKodu.Trim();
                existing.Aciklama = NormalizeImportedAciklama(existing.Aciklama, existing.Fatura?.Cari?.Unvan, yeniUrunKodu);
                existing.UrunKodu = yeniUrunKodu;
                existing.KalemTipi = kalem.KalemTipi;
                existing.AltTipi = kalem.AltTipi;
                existing.UpdatedAt = DateTime.UtcNow;
            }
        }
        
        await context.SaveChangesAsync();

        var faturaIds = mevcutKalemler.Select(k => k.FaturaId).Distinct().ToList();
        await RebuildDerivedRecordsForInvoicesAsync(context, faturaIds);
    }

    public async Task<StokKartiOlusturSonuc> UpdateFaturaKalemleriVeStokKartiOlusturAsync(List<FaturaKalem> kalemler, bool stokKartiOlustur = true)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var sonuc = new StokKartiOlusturSonuc();

        if (kalemler == null || !kalemler.Any())
            return sonuc;

        // Muhasebe ayarlarını al
        var ayar = await context.MuhasebeAyarlari.AsNoTracking().FirstOrDefaultAsync();
        // Mevcut stok kartlarını al
        var mevcutStoklar = await context.StokKartlari
            .AsTracking()
            .Where(s => !s.IsDeleted)
            .ToListAsync();

        var mevcutStokOzetleri = mevcutStoklar
            .Select(s => new { s.Id, s.StokKodu, s.StokAdi })
            .ToList();

        var mevcutStokAdlari = mevcutStokOzetleri.Select(s => s.StokAdi.ToLowerInvariant().Trim()).ToHashSet();
        var mevcutStokKodlari = mevcutStokOzetleri.Select(s => s.StokKodu.ToUpperInvariant().Trim()).ToHashSet();
        var stokAdToId = mevcutStokOzetleri.ToDictionary(s => s.StokAdi.ToLowerInvariant().Trim(), s => s.Id);
        var stokKodToId = mevcutStokOzetleri
            .Where(s => !string.IsNullOrWhiteSpace(s.StokKodu))
            .ToDictionary(s => s.StokKodu.ToUpperInvariant().Trim(), s => s.Id);

        var sonStokKodu = await context.StokKartlari
            .Where(s => s.StokKodu.StartsWith("STK"))
            .OrderByDescending(s => s.StokKodu)
            .Select(s => s.StokKodu)
            .FirstOrDefaultAsync();

        int stokSayac = 1;
        if (!string.IsNullOrEmpty(sonStokKodu))
        {
            var numStr = sonStokKodu.Replace("STK", "");
            if (int.TryParse(numStr, out var num))
                stokSayac = num + 1;
        }

        var kalemIds = kalemler.Select(k => k.Id).ToList();
        var mevcutKalemler = await context.FaturaKalemleri
            .AsTracking()
            .Include(k => k.Fatura)
                .ThenInclude(f => f.Cari)
            .Where(k => kalemIds.Contains(k.Id))
            .ToListAsync();

        var olusturulacakStoklar = new List<StokKarti>();
        var stokHareketleri = new List<StokHareket>();
        var giderKayitlari = new List<(FaturaKalem kalem, StokKarti stok, decimal tutar)>();

        foreach (var kalem in kalemler)
        {
            try
            {
                var existing = mevcutKalemler.FirstOrDefault(k => k.Id == kalem.Id);
                if (existing != null)
                {
                    var yeniUrunKodu = string.IsNullOrWhiteSpace(kalem.UrunKodu) ? null : kalem.UrunKodu.Trim();
                    existing.Aciklama = NormalizeImportedAciklama(existing.Aciklama, existing.Fatura?.Cari?.Unvan, yeniUrunKodu);
                    existing.UrunKodu = yeniUrunKodu;
                    existing.KalemTipi = kalem.KalemTipi;
                    existing.AltTipi = kalem.AltTipi;
                    existing.UpdatedAt = DateTime.UtcNow;
                    sonuc.GuncellenenKalemSayisi++;

                    if (stokKartiOlustur && !string.IsNullOrWhiteSpace(existing.Aciklama))
                    {
                        var stokAdi = existing.Aciklama.Trim();
                        var stokAdiLower = stokAdi.ToLowerInvariant();

                        var stokKodu = !string.IsNullOrWhiteSpace(existing.UrunKodu) 
                            ? existing.UrunKodu.ToUpperInvariant().Trim() 
                            : $"STK{stokSayac:D5}";

                        // Stok kartı zaten var mı kontrol et
                        bool stokZatenVar = mevcutStokKodlari.Contains(stokKodu.ToUpperInvariant()) || 
                                           mevcutStokAdlari.Contains(stokAdiLower);

                        var stokTipi = KalemTipindenStokTipi(kalem.KalemTipi);
                        var stokAltTipi = KalemAltTipindenStokAltTipi(kalem.AltTipi);

                        StokKarti? yeniStok = null;
                        int stokKartiId = 0;

                        if (stokZatenVar)
                        {
                            // Mevcut stok kartı ID'sini bul
                            if (stokKodToId.TryGetValue(stokKodu.ToUpperInvariant(), out var stokKodId))
                            {
                                stokKartiId = stokKodId;
                            }
                            else if (stokAdToId.TryGetValue(stokAdiLower, out var mevcutId))
                            {
                                stokKartiId = mevcutId;
                            }

                            var mevcutStok = mevcutStoklar.FirstOrDefault(s => s.Id == stokKartiId);
                            if (mevcutStok != null)
                            {
                                mevcutStok.StokTipi = stokTipi;
                                mevcutStok.AltTipi = stokAltTipi;
                                mevcutStok.Birim = existing.Birim ?? mevcutStok.Birim;
                                mevcutStok.KdvOrani = existing.KdvOrani;
                                if (existing.BirimFiyat > 0)
                                    mevcutStok.AlisFiyati = existing.BirimFiyat;
                                mevcutStok.StokTakibiYapilsin = stokTipi == StokTipi.Mal || stokTipi == StokTipi.YedekParca || stokTipi == StokTipi.SarfMalzeme;
                                mevcutStok.UpdatedAt = DateTime.UtcNow;
                            }

                            sonuc.AtlananStokKartiSayisi++;
                        }
                        else
                        {
                            // Yeni stok kartı oluştur
                            yeniStok = new StokKarti
                            {
                                StokKodu = stokKodu,
                                StokAdi = stokAdi.Length > 200 ? stokAdi.Substring(0, 200) : stokAdi,
                                StokTipi = stokTipi,
                                AltTipi = stokAltTipi,
                                Birim = existing.Birim ?? "Adet",
                                KdvOrani = existing.KdvOrani,
                                AlisFiyati = existing.BirimFiyat,
                                Aktif = true,
                                StokTakibiYapilsin = stokTipi == StokTipi.Mal || stokTipi == StokTipi.YedekParca || stokTipi == StokTipi.SarfMalzeme,
                                CreatedAt = DateTime.UtcNow
                            };

                            olusturulacakStoklar.Add(yeniStok);
                            mevcutStokAdlari.Add(stokAdiLower);
                            mevcutStokKodlari.Add(stokKodu.ToUpperInvariant());
                            stokSayac++;
                            sonuc.OlusturulanStokKartiSayisi++;
                        }

                        // Mal veya Sarf Malzeme ise gider kaydı için işaretle
                        if (stokTipi == StokTipi.Mal || stokTipi == StokTipi.SarfMalzeme)
                        {
                            if (yeniStok != null)
                            {
                                giderKayitlari.Add((existing, yeniStok, existing.ToplamTutar));
                            }
                            else if (stokKartiId > 0)
                            {
                                var mevcutStok = await context.StokKartlari.FindAsync(stokKartiId);
                                if (mevcutStok != null)
                                {
                                    giderKayitlari.Add((existing, mevcutStok, existing.ToplamTutar));
                                }
                            }
                        }
                    }
                }
                else
                {
                    sonuc.Hatalar.Add($"Kalem ID {kalem.Id} bulunamadı.");
                }
            }
            catch (Exception ex)
            {
                sonuc.Hatalar.Add($"Kalem {kalem.Id}: {ex.Message}");
            }
        }

        if (olusturulacakStoklar.Any())
        {
            context.StokKartlari.AddRange(olusturulacakStoklar);
        }

        try
        {
            await context.SaveChangesAsync();
            var faturaIds = mevcutKalemler.Select(k => k.FaturaId).Distinct().ToList();
            var rebuildSonuc = await RebuildDerivedRecordsForInvoicesAsync(context, faturaIds);
            sonuc.OlusturulanStokHareketSayisi = rebuildSonuc.StokHareketSayisi;
            sonuc.OlusturulanGiderKayitSayisi = rebuildSonuc.MuhasebeFisSayisi;
        }
        catch (Exception ex)
        {
            sonuc.Hatalar.Add($"Kayıt hatası: {ex.Message}");
            if (ex.InnerException != null)
                sonuc.Hatalar.Add($"Detay: {ex.InnerException.Message}");
        }

        return sonuc;
    }

    /// <summary>
    /// Mal/Sarf Malzeme gider aktarımı için muhasebe kaydı oluşturur
    /// Borç: 770.99.999 (Genel Yönetim Giderleri)
    /// Alacak: 153 (Ticari Mallar)
    /// </summary>
    private async Task CreateGiderMuhasebeKaydiAsync(ApplicationDbContext context, Fatura fatura, decimal tutar, string giderHesapKodu)
    {
        try
        {
            // Gider hesabını bul veya oluştur
            var giderHesap = await context.MuhasebeHesaplari.FirstOrDefaultAsync(h => h.HesapKodu == giderHesapKodu);
            if (giderHesap == null)
            {
                var ustKod = "770.99";
                var ustHesap = await context.MuhasebeHesaplari.FirstOrDefaultAsync(h => h.HesapKodu == ustKod);

                giderHesap = new MuhasebeHesap
                {
                    HesapKodu = giderHesapKodu,
                    HesapAdi = "Genel Yönetim Giderleri - Mal/Sarf Malzeme",
                    HesapTuru = HesapTuru.Gider,
                    HesapGrubu = HesapGrubu.MaliyetHesaplari,
                    UstHesapId = ustHesap?.Id,
                    Aktif = true,
                    CreatedAt = DateTime.UtcNow
                };
                context.MuhasebeHesaplari.Add(giderHesap);
                await context.SaveChangesAsync();
            }

            // Stok hesabı (153)
            var stokHesap = await context.MuhasebeHesaplari.FirstOrDefaultAsync(h => h.HesapKodu == "153");
            if (stokHesap == null)
            {
                var ustHesap = await context.MuhasebeHesaplari.FirstOrDefaultAsync(h => h.HesapKodu == "15");

                stokHesap = new MuhasebeHesap
                {
                    HesapKodu = "153",
                    HesapAdi = "Ticari Mallar",
                    HesapTuru = HesapTuru.Aktif,
                    HesapGrubu = HesapGrubu.DonenVarliklar,
                    UstHesapId = ustHesap?.Id,
                    Aktif = true,
                    CreatedAt = DateTime.UtcNow
                };
                context.MuhasebeHesaplari.Add(stokHesap);
                await context.SaveChangesAsync();
            }

            // Muhasebe fişi oluştur
            var fisNo = await GenerateNextFisNoAsync(context);
            var fis = new MuhasebeFis
            {
                FisNo = fisNo,
                FisTarihi = DateTime.SpecifyKind(fatura.FaturaTarihi, DateTimeKind.Utc),
                FisTipi = FisTipi.Mahsup,
                Aciklama = $"Fatura {fatura.FaturaNo} - Mal/Sarf Malzeme Gider Kaydı",
                ToplamBorc = tutar,
                ToplamAlacak = tutar,
                Durum = FisDurum.Onaylandi,
                Kaynak = FisKaynak.Fatura,
                KaynakId = fatura.Id,
                KaynakTip = "GiderAktarim",
                CreatedAt = DateTime.UtcNow
            };

            fis.Kalemler.Add(new MuhasebeFisKalem
            {
                HesapId = giderHesap.Id,
                SiraNo = 1,
                Borc = tutar,
                Alacak = 0,
                Tarih = fatura.FaturaTarihi,
                Aciklama = $"Gider Kaydı - {fatura.FaturaNo}",
                CreatedAt = DateTime.UtcNow
            });

            fis.Kalemler.Add(new MuhasebeFisKalem
            {
                HesapId = stokHesap.Id,
                SiraNo = 2,
                Borc = 0,
                Alacak = tutar,
                Tarih = fatura.FaturaTarihi,
                Aciklama = $"Stok Çıkışı - {fatura.FaturaNo}",
                CreatedAt = DateTime.UtcNow
            });

            context.MuhasebeFisleri.Add(fis);
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Gider muhasebe kaydı hatası: {ex.Message}");
        }
    }

    private string GetInvoiceLineAciklama(System.Xml.Linq.XElement line, string? cariUnvani, string? urunKodu)
    {
        var item = line.Descendants().FirstOrDefault(x => x.Name.LocalName == "Item");
        var name = item?.Descendants().FirstOrDefault(x => x.Name.LocalName == "Name")?.Value?.Trim() ?? string.Empty;
        var description = item?.Descendants().FirstOrDefault(x => x.Name.LocalName == "Description")?.Value?.Trim();
        var note = line.Descendants().FirstOrDefault(x => x.Name.LocalName == "Note")?.Value?.Trim();

        var aciklama = !string.IsNullOrWhiteSpace(description)
            ? description
            : !string.IsNullOrWhiteSpace(note)
                ? note
                : name;

        return NormalizeImportedAciklama(aciklama, cariUnvani, urunKodu);
    }

    private static string NormalizeImportedAciklama(string? aciklama, string? cariUnvani, string? urunKodu)
    {
        var temizAciklama = string.IsNullOrWhiteSpace(aciklama) ? "" : aciklama.Trim();
        var cariLower = (cariUnvani ?? "").ToLowerInvariant();
        var kod = urunKodu?.Trim();

        if (cariLower.Contains("başkent elektrik") || cariLower.Contains("baskent elektrik"))
        {
            if (kod == "1")
                return "Elektrik";

            if (kod == "2")
                return "Elektrik Çarpan";

            if (temizAciklama.Contains("çarpan", StringComparison.OrdinalIgnoreCase))
                return "Elektrik Çarpan";
        }

        return temizAciklama;
    }

    private async Task<(int StokHareketSayisi, int MuhasebeFisSayisi)> RebuildDerivedRecordsForInvoicesAsync(ApplicationDbContext context, List<int> faturaIds)
    {
        if (faturaIds == null || !faturaIds.Any())
            return (0, 0);

        var invoiceIdSet = faturaIds.Distinct().ToList();

        var etkilenmisStokIds = await context.StokHareketler
            .Where(h => h.FaturaId.HasValue && invoiceIdSet.Contains(h.FaturaId.Value))
            .Select(h => h.StokKartiId)
            .Distinct()
            .ToListAsync();

        var silinecekHareketler = await context.StokHareketler
            .Where(h => h.FaturaId.HasValue && invoiceIdSet.Contains(h.FaturaId.Value))
            .ToListAsync();

        if (silinecekHareketler.Any())
            context.StokHareketler.RemoveRange(silinecekHareketler);

        var silinecekFisler = await context.MuhasebeFisleri
            .Include(f => f.Kalemler)
            .Where(f => f.Kaynak == FisKaynak.Fatura &&
                        f.KaynakId.HasValue && invoiceIdSet.Contains(f.KaynakId.Value) &&
                        (f.KaynakTip == "GiderAktarim" || f.KaynakTip == "MasrafAktarim" || f.KaynakTip == "Fatura"))
            .ToListAsync();

        if (silinecekFisler.Any())
            context.MuhasebeFisleri.RemoveRange(silinecekFisler);

        await context.SaveChangesAsync();

        if (etkilenmisStokIds.Any())
        {
            foreach (var stokId in etkilenmisStokIds)
            {
                var stok = await context.StokKartlari.AsTracking().FirstOrDefaultAsync(s => s.Id == stokId);
                if (stok == null)
                    continue;

                stok.MevcutStok = await context.StokHareketler
                    .Where(h => h.StokKartiId == stokId)
                    .SumAsync(h => h.Miktar);
                stok.UpdatedAt = DateTime.UtcNow;
            }

            await context.SaveChangesAsync();
        }

        var faturalar = await context.Faturalar
            .AsTracking()
            .Include(f => f.Cari)
            .Include(f => f.FaturaKalemleri)
            .Where(f => invoiceIdSet.Contains(f.Id))
            .ToListAsync();

        foreach (var fatura in faturalar)
        {
            await CreateStokHareketleriFromFaturaAsync(context, fatura, fatura.FaturaYonu);
            await TryCreateMuhasebeFisiAsync(context, fatura);
        }

        var stokHareketSayisi = await context.StokHareketler
            .CountAsync(h => h.FaturaId.HasValue && invoiceIdSet.Contains(h.FaturaId.Value));

        var muhasebeFisSayisi = await context.MuhasebeFisleri
            .CountAsync(f => f.Kaynak == FisKaynak.Fatura && f.KaynakId.HasValue && invoiceIdSet.Contains(f.KaynakId.Value));

        return (stokHareketSayisi, muhasebeFisSayisi);
    }

    private static StokTipi KalemTipindenStokTipi(FaturaKalemTipi kalemTipi) => kalemTipi switch
    {
        FaturaKalemTipi.Arac => StokTipi.Arac,
        FaturaKalemTipi.Demirbas => StokTipi.Demirbas,
        FaturaKalemTipi.Mal => StokTipi.Mal,
        FaturaKalemTipi.Hizmet => StokTipi.Hizmet,
        FaturaKalemTipi.Servis => StokTipi.Hizmet,
        _ => StokTipi.Diger
    };

    private static StokAltTipi? KalemAltTipindenStokAltTipi(FaturaKalemAltTipi? altTipi) => altTipi switch
    {
        null => null,
        FaturaKalemAltTipi.TasimaHizmeti => StokAltTipi.TasimaHizmeti,
        FaturaKalemAltTipi.KiralamaHizmeti => StokAltTipi.KiralamaHizmeti,
        FaturaKalemAltTipi.DanismanlikHizmeti => StokAltTipi.DanismanlikHizmeti,
        FaturaKalemAltTipi.TicariMal => StokAltTipi.TicariMal,
        FaturaKalemAltTipi.YedekParca => StokAltTipi.MotorParcasi,
        FaturaKalemAltTipi.SarfMalzeme => StokAltTipi.Yakit,
        FaturaKalemAltTipi.BakimOnarim => StokAltTipi.ServisHizmeti,
        _ => null
    };

    #endregion

    #region Muhasebe Fişi ve Excel

    public async Task<MuhasebeFis> CreateMuhasebeFisiAsync(int faturaId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var fatura = await context.Faturalar
            .Include(f => f.Cari)
            .Include(f => f.FaturaKalemleri)
            .FirstOrDefaultAsync(f => f.Id == faturaId);

        if (fatura == null)
            throw new Exception("Fatura bulunamadı.");

        if (fatura.MuhasebeFisiOlusturuldu)
            throw new InvalidOperationException($"Fatura {fatura.FaturaNo} zaten muhasebeleştirilmiş.");

        // Tam muhasebe kaydını (120/320/600/770/191/391 kalemleri dahil) MuhasebeService oluşturur
        return await _muhasebeService.CreateFaturaFisiAsync(fatura);
    }

    private static async Task<string> GenerateNextFisNoAsync(ApplicationDbContext context)
    {
        var yilAy = $"{DateTime.UtcNow.Year}{DateTime.UtcNow.Month:D2}";
        var sonNo = await MuhasebeService.NextFisNoCounterAsync(context, "MF", yilAy);
        return $"MF{yilAy}{sonNo:D4}";
    }

    public async Task<byte[]> GetExcelSablonAsync(FaturaYonu yon)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        using var package = new ExcelPackage();
        var ws = package.Workbook.Worksheets.Add(yon == FaturaYonu.Giden ? "Satış Faturaları" : "Alış Faturaları");

        var headers = new[] { "Ünvanı", "Vkn/Tckn", "Fatura Tipi", "Fatura Tarihi", "Fatura No",
            "İskonto", "%0 Matrah", "%1 Matrah", "%10 Matrah", "%20 Matrah",
            "%1 KDV", "%10 KDV", "%20 KDV", "Ödenecek Tutar" };

        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cells[1, i + 1].Value = headers[i];
            ws.Cells[1, i + 1].Style.Font.Bold = true;
            ws.Cells[1, i + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            ws.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGreen);
        }

        ws.Cells[2, 1].Value = "ÖRNEK FİRMA A.Ş.";
        ws.Cells[2, 2].Value = "1234567890";
        ws.Cells[2, 3].Value = yon == FaturaYonu.Giden ? "SATIS" : "ALIS";
        ws.Cells[2, 4].Value = DateTime.Today.ToString("dd.MM.yyyy");
        ws.Cells[2, 5].Value = $"FTR{DateTime.Now:yyyyMM}000001";
        ws.Cells[2, 10].Value = "1000,00";
        ws.Cells[2, 13].Value = "200,00";
        ws.Cells[2, 14].Value = "1200,00";

        ws.Cells.AutoFitColumns();
        return await Task.FromResult(package.GetAsByteArray());
    }

    public async Task<byte[]> ExportToExcelAsync(List<Fatura> faturalar)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        using var package = new ExcelPackage();
        var ws = package.Workbook.Worksheets.Add("Faturalar");

        var headers = new[] { "Fatura No", "Tarih", "Vade", "Cari", "VKN", "Matrah", "KDV", "Toplam", "Ödenen", "Kalan", "Durum", "Tip", "ETTN" };
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cells[1, i + 1].Value = headers[i];
            ws.Cells[1, i + 1].Style.Font.Bold = true;
            ws.Cells[1, i + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            ws.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGreen);
        }

        int row = 2;
        foreach (var f in faturalar)
        {
            ws.Cells[row, 1].Value = f.FaturaNo;
            ws.Cells[row, 2].Value = f.FaturaTarihi.ToString("dd.MM.yyyy");
            ws.Cells[row, 3].Value = f.VadeTarihi?.ToString("dd.MM.yyyy");
            ws.Cells[row, 4].Value = f.Cari?.Unvan;
            ws.Cells[row, 5].Value = f.Cari?.VergiNo ?? f.Cari?.TcKimlikNo;
            ws.Cells[row, 6].Value = f.AraToplam;
            ws.Cells[row, 7].Value = f.KdvTutar;
            ws.Cells[row, 8].Value = f.GenelToplam;
            ws.Cells[row, 9].Value = f.OdenenTutar;
            ws.Cells[row, 10].Value = f.KalanTutar;
            ws.Cells[row, 11].Value = f.Durum.ToString();
            ws.Cells[row, 12].Value = f.FaturaTipi.ToString();
            ws.Cells[row, 13].Value = f.EttnNo;
            row++;
        }

        ws.Cells.AutoFitColumns();
        return await Task.FromResult(package.GetAsByteArray());
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// XML Party node'undan VKN/TCKN bilgisini çıkarır
    /// </summary>
    private static string ExtractVknFromParty(System.Xml.Linq.XElement? partyNode, Func<System.Xml.Linq.XElement?, string, string> getValue)
    {
        if (partyNode == null) return string.Empty;

        var partyIdentification = partyNode.Descendants().FirstOrDefault(x => x.Name.LocalName == "PartyIdentification");
        if (partyIdentification != null)
        {
            var vkn = getValue(partyIdentification, "ID").Trim();
            if (!string.IsNullOrWhiteSpace(vkn) && vkn.Length >= 10)
                return vkn;
        }

        return string.Empty;
    }

    private static FaturaKalemTipi DetermineKalemTipi(string? aciklama, string? urunKodu)
    {
        if (string.IsNullOrWhiteSpace(urunKodu) && string.IsNullOrWhiteSpace(aciklama))
            return FaturaKalemTipi.Hizmet;

        var lower = (aciklama ?? "").ToLowerInvariant();

        if (lower.Contains("araç") || lower.Contains("otomobil") || lower.Contains("minibüs") ||
            lower.Contains("otobüs") || lower.Contains("midibüs") || lower.Contains("panelvan") ||
            lower.Contains("şase") || lower.Contains("plaka") || lower.Contains("arac"))
            return FaturaKalemTipi.Arac;

        if (lower.Contains("servis") || lower.Contains("bakım") || lower.Contains("onarım") ||
            lower.Contains("tamir") || lower.Contains("muayene") || lower.Contains("sigorta") ||
            lower.Contains("kasko") || lower.Contains("bakim") || lower.Contains("onarim"))
            return FaturaKalemTipi.Servis;

        if (lower.Contains("demirbaş") || lower.Contains("demirbas") || lower.Contains("ofis") ||
            lower.Contains("makina") || lower.Contains("makine") || lower.Contains("ekipman"))
            return FaturaKalemTipi.Demirbas;

        if (!string.IsNullOrWhiteSpace(urunKodu) && urunKodu.Length > 3)
        {
            if (!lower.Contains("hizmet") && !lower.Contains("iş") && !lower.Contains("işçilik"))
                return FaturaKalemTipi.Mal;
        }

        if (lower.Contains("mal") || lower.Contains("ürün") || lower.Contains("parça") ||
            lower.Contains("yedek") || lower.Contains("malzeme"))
            return FaturaKalemTipi.Mal;

        return FaturaKalemTipi.Hizmet;
    }

    private static FaturaKalemAltTipi? DetermineKalemAltTipi(string? aciklama, string? urunKodu, FaturaKalemTipi kalemTipi)
    {
        if (string.IsNullOrWhiteSpace(aciklama))
            return null;

        var lower = aciklama.ToLowerInvariant();

        return kalemTipi switch
        {
            FaturaKalemTipi.Hizmet => lower switch
            {
                var s when s.Contains("taşıma") || s.Contains("nakil") => FaturaKalemAltTipi.TasimaHizmeti,
                var s when s.Contains("kiralama") || s.Contains("kira") => FaturaKalemAltTipi.KiralamaHizmeti,
                var s when s.Contains("danışmanlık") => FaturaKalemAltTipi.DanismanlikHizmeti,
                _ => null
            },
            FaturaKalemTipi.Mal => lower switch
            {
                var s when s.Contains("yedek") || s.Contains("parça") => FaturaKalemAltTipi.YedekParca,
                var s when s.Contains("sarf") || s.Contains("malzeme") => FaturaKalemAltTipi.SarfMalzeme,
                _ => FaturaKalemAltTipi.TicariMal
            },
            FaturaKalemTipi.Demirbas => FaturaKalemAltTipi.DigerDemirbas,
            FaturaKalemTipi.Servis => lower switch
            {
                var s when s.Contains("bakım") || s.Contains("onarım") => FaturaKalemAltTipi.BakimOnarim,
                var s when s.Contains("kasko") => FaturaKalemAltTipi.Kasko,
                var s when s.Contains("sigorta") => FaturaKalemAltTipi.Sigorta,
                var s when s.Contains("muayene") => FaturaKalemAltTipi.Muayene,
                var s when s.Contains("lastik") => FaturaKalemAltTipi.Lastik,
                var s when s.Contains("yakıt") => FaturaKalemAltTipi.Yakit,
                _ => FaturaKalemAltTipi.BakimOnarim
            },
            _ => null
        };
    }

    private static (string? SaseNo, string? Plaka)? ExtractAracBilgisi(string? aciklama)
    {
        if (string.IsNullOrWhiteSpace(aciklama))
            return null;

        var saseMatch = System.Text.RegularExpressions.Regex.Match(aciklama, @"\b[A-HJ-NPR-Z0-9]{17}\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        var plakaMatch = System.Text.RegularExpressions.Regex.Match(aciklama, @"\b\d{2}\s*[A-Z]{1,3}\s*\d{2,4}\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        if (saseMatch.Success || plakaMatch.Success)
        {
            return (
                saseMatch.Success ? saseMatch.Value.ToUpperInvariant() : null,
                plakaMatch.Success ? plakaMatch.Value.Replace(" ", "").ToUpperInvariant() : null
            );
        }

        return null;
    }

    private async Task<Arac?> FindOrCreateAracAsync(ApplicationDbContext context, (string? SaseNo, string? Plaka) aracBilgisi, FaturaYonu yon, int cariId)
    {
        Arac? arac = null;

        if (!string.IsNullOrWhiteSpace(aracBilgisi.SaseNo))
        {
            arac = await context.Araclar
                .Include(a => a.PlakaGecmisi)
                .FirstOrDefaultAsync(a => a.SaseNo == aracBilgisi.SaseNo && !a.IsDeleted);
        }

        if (arac == null && !string.IsNullOrWhiteSpace(aracBilgisi.Plaka))
        {
            var plakaKaydi = await context.AracPlakalar
                .Include(p => p.Arac)
                .FirstOrDefaultAsync(p => p.Plaka == aracBilgisi.Plaka && !p.IsDeleted);

            if (plakaKaydi != null)
            {
                arac = plakaKaydi.Arac;
            }
        }

        return arac;
    }

    private string GetMuhasebeHesapKoduByKalemTipi(FaturaKalemTipi kalemTipi, FaturaYonu yon, MuhasebeAyar ayar)
    {
        if (yon == FaturaYonu.Giden)
        {
            return kalemTipi switch
            {
                FaturaKalemTipi.Arac => "253",
                FaturaKalemTipi.Demirbas => "255",
                FaturaKalemTipi.Mal => "600.01",
                FaturaKalemTipi.Hizmet => ayar.SatisGelirHesabi,
                FaturaKalemTipi.Servis => ayar.SatisGelirHesabi,
                _ => ayar.SatisGelirHesabi
            };
        }
        else
        {
            return kalemTipi switch
            {
                FaturaKalemTipi.Arac => "253",
                FaturaKalemTipi.Demirbas => "255",
                FaturaKalemTipi.Mal => "153",
                FaturaKalemTipi.Hizmet => ayar.AlisGiderHesabi,
                FaturaKalemTipi.Servis => "770.07",
                _ => ayar.AlisGiderHesabi
            };
        }
    }

    /// <summary>
    /// Fatura için stok hareketleri oluşturur ve muhasebe kaydı yapar
    /// Gelen Fatura: Stok Girişi (Mal/Sarf Malzeme ise masrafa aktarım)
    /// Giden Fatura: Stok Çıkışı
    /// </summary>
    private async Task CreateStokHareketleriFromFaturaAsync(ApplicationDbContext context, Fatura fatura, FaturaYonu yon)
    {
        try
        {
            if (fatura.FaturaKalemleri == null || !fatura.FaturaKalemleri.Any())
                return;

            // Tüm verileri önce çek - tek sorguda
            var ayar = await context.MuhasebeAyarlari.AsNoTracking().FirstOrDefaultAsync();
            if (ayar == null || !ayar.StokMasrafAktarimiOtomatik)
                return;

            var stokKartlari = await context.StokKartlari
                .AsNoTracking()
                .Where(s => !s.IsDeleted && s.Aktif)
                .ToListAsync();

            if (!stokKartlari.Any())
                return;

            // Dictionary'ler oluştur (memory içi işlem)
            var stokAdSozluk = stokKartlari
                .GroupBy(s => s.StokAdi.ToLower().Trim())
                .ToDictionary(g => g.Key, g => g.First());

            var stokKodSozluk = stokKartlari
                .Where(s => !string.IsNullOrWhiteSpace(s.StokKodu))
                .GroupBy(s => s.StokKodu.ToUpper().Trim())
                .ToDictionary(g => g.Key, g => g.First());

            var stokHareketleri = new List<StokHareket>();
            decimal toplamMalMasrafTutar = 0;
            decimal toplamSarfMasrafTutar = 0;
            var guncellenecekStoklar = new Dictionary<int, decimal>();

            // Kalemleri işle (DB sorgusu yok)
            foreach (var kalem in fatura.FaturaKalemleri)
            {
                if (string.IsNullOrWhiteSpace(kalem.Aciklama))
                    continue;

                StokKarti? stokKarti = null;

                if (!string.IsNullOrWhiteSpace(kalem.UrunKodu))
                {
                    stokKodSozluk.TryGetValue(kalem.UrunKodu.ToUpper().Trim(), out stokKarti);
                }

                if (stokKarti == null)
                {
                    stokAdSozluk.TryGetValue(kalem.Aciklama.ToLower().Trim(), out stokKarti);
                }

                if (stokKarti != null && stokKarti.StokTakibiYapilsin)
                {
                    bool isMal = stokKarti.StokTipi == StokTipi.Mal;
                    bool isSarfMalzeme = stokKarti.StokTipi == StokTipi.SarfMalzeme;
                    bool masrafaAktar = yon == FaturaYonu.Gelen && (isMal || isSarfMalzeme);

                    var masrafHesapKodu = isMal ? ayar.MalMasrafHesabi : ayar.SarfMalzemeMasrafHesabi;

                    if (masrafaAktar)
                    {
                        stokHareketleri.Add(new StokHareket
                        {
                            StokKartiId = stokKarti.Id,
                            HareketTipi = StokHareketTipi.Giris,
                            IslemTarihi = DateTime.SpecifyKind(fatura.FaturaTarihi, DateTimeKind.Utc),
                            Miktar = kalem.Miktar,
                            BirimFiyat = kalem.BirimFiyat,
                            Aciklama = $"Fatura Girişi: {fatura.FaturaNo} - {kalem.Aciklama}",
                            BelgeNo = fatura.FaturaNo,
                            FaturaId = fatura.Id,
                            CariId = fatura.CariId,
                            FaturaKalemId = kalem.Id,
                            CreatedAt = DateTime.UtcNow
                        });

                        stokHareketleri.Add(new StokHareket
                        {
                            StokKartiId = stokKarti.Id,
                            HareketTipi = StokHareketTipi.Cikis,
                            IslemTarihi = DateTime.SpecifyKind(fatura.FaturaTarihi, DateTimeKind.Utc),
                            Miktar = kalem.Miktar,
                            BirimFiyat = kalem.BirimFiyat,
                            Aciklama = $"Masrafa Aktarım: {fatura.FaturaNo} - {kalem.Aciklama} -> {masrafHesapKodu}",
                            BelgeNo = fatura.FaturaNo,
                            FaturaId = fatura.Id,
                            CariId = fatura.CariId,
                            FaturaKalemId = kalem.Id,
                            CreatedAt = DateTime.UtcNow
                        });

                        if (isMal)
                            toplamMalMasrafTutar += kalem.ToplamTutar;
                        else
                            toplamSarfMasrafTutar += kalem.ToplamTutar;
                    }
                    else
                    {
                        stokHareketleri.Add(new StokHareket
                        {
                            StokKartiId = stokKarti.Id,
                            HareketTipi = yon == FaturaYonu.Giden ? StokHareketTipi.Cikis : StokHareketTipi.Giris,
                            IslemTarihi = DateTime.SpecifyKind(fatura.FaturaTarihi, DateTimeKind.Utc),
                            Miktar = kalem.Miktar,
                            BirimFiyat = kalem.BirimFiyat,
                            Aciklama = $"Fatura: {fatura.FaturaNo} - {kalem.Aciklama}",
                            BelgeNo = fatura.FaturaNo,
                            FaturaId = fatura.Id,
                            CariId = fatura.CariId,
                            FaturaKalemId = kalem.Id,
                            CreatedAt = DateTime.UtcNow
                        });

                        var miktarDegisimi = yon == FaturaYonu.Giden ? -kalem.Miktar : kalem.Miktar;
                        if (guncellenecekStoklar.ContainsKey(stokKarti.Id))
                            guncellenecekStoklar[stokKarti.Id] += miktarDegisimi;
                        else
                            guncellenecekStoklar[stokKarti.Id] = miktarDegisimi;
                    }
                }
            }

            // Eğer işlenecek bir şey yoksa çık
            if (!stokHareketleri.Any() && toplamMalMasrafTutar == 0 && toplamSarfMasrafTutar == 0)
                return;

            // Stok hareketlerini ekle
            if (stokHareketleri.Any())
            {
                context.StokHareketler.AddRange(stokHareketleri);
            }

            // Stok miktarlarını güncelle - tek sorguda ID'leri al
            if (guncellenecekStoklar.Any())
            {
                var stokIds = guncellenecekStoklar.Keys.ToList();
                var stoklarToUpdate = await context.StokKartlari
                    .Where(s => stokIds.Contains(s.Id))
                    .ToListAsync();

                foreach (var stok in stoklarToUpdate)
                {
                    if (guncellenecekStoklar.TryGetValue(stok.Id, out var miktar))
                    {
                        stok.MevcutStok += miktar;
                        stok.UpdatedAt = DateTime.UtcNow;
                    }
                }
            }

            // İlk kaydet
            await context.SaveChangesAsync();

            // Muhasebe kayıtlarını ayrı ayrı oluştur (her biri kendi SaveChanges'ini yapacak)
            if (toplamMalMasrafTutar > 0)
            {
                await CreateMasrafMuhasebeKaydiInternalAsync(context, fatura, toplamMalMasrafTutar, ayar.MalMasrafHesabi, ayar.StokCikisHesabi, "Ticari Mal");
            }

            if (toplamSarfMasrafTutar > 0)
            {
                await CreateMasrafMuhasebeKaydiInternalAsync(context, fatura, toplamSarfMasrafTutar, ayar.SarfMalzemeMasrafHesabi, ayar.StokCikisHesabi, "Sarf Malzeme");
            }
        }
        catch (Exception ex)
        {
            // Hata durumunda sessizce devam et - fatura importu başarılı olsun
            System.Diagnostics.Debug.WriteLine($"Stok hareketi oluşturma hatası: {ex.Message}");
        }
    }

    /// <summary>
    /// Masraf aktarımı için muhasebe fişi oluşturur (internal - SaveChanges yapar)
    /// </summary>
    private async Task CreateMasrafMuhasebeKaydiInternalAsync(ApplicationDbContext context, Fatura fatura, decimal tutar, string masrafHesapKodu, string stokCikisHesapKodu, string aciklamaTipi)
    {
        try
        {
            // Hesapları bul
            var masrafHesap = await context.MuhasebeHesaplari.FirstOrDefaultAsync(h => h.HesapKodu == masrafHesapKodu);
            var stokHesap = await context.MuhasebeHesaplari.FirstOrDefaultAsync(h => h.HesapKodu == stokCikisHesapKodu);

            // Masraf hesabı yoksa oluştur
            if (masrafHesap == null)
            {
                var ustKod = masrafHesapKodu.Contains('.') 
                    ? string.Join(".", masrafHesapKodu.Split('.').Take(masrafHesapKodu.Split('.').Length - 1))
                    : masrafHesapKodu.Substring(0, Math.Min(3, masrafHesapKodu.Length));
                var ustHesap = await context.MuhasebeHesaplari.FirstOrDefaultAsync(h => h.HesapKodu == ustKod);

                masrafHesap = new MuhasebeHesap
                {
                    HesapKodu = masrafHesapKodu,
                    HesapAdi = $"Masraflar - {aciklamaTipi}",
                    HesapTuru = HesapTuru.Gider,
                    HesapGrubu = HesapGrubu.MaliyetHesaplari,
                    UstHesapId = ustHesap?.Id,
                    Aktif = true,
                    CreatedAt = DateTime.UtcNow
                };
                context.MuhasebeHesaplari.Add(masrafHesap);
                await context.SaveChangesAsync();
            }

            // Stok hesabı yoksa oluştur
            if (stokHesap == null)
            {
                var ustKod = stokCikisHesapKodu.Length >= 2 ? stokCikisHesapKodu.Substring(0, 2) : stokCikisHesapKodu;
                var ustHesap = await context.MuhasebeHesaplari.FirstOrDefaultAsync(h => h.HesapKodu == ustKod);

                stokHesap = new MuhasebeHesap
                {
                    HesapKodu = stokCikisHesapKodu,
                    HesapAdi = "Ticari Mallar",
                    HesapTuru = HesapTuru.Aktif,
                    HesapGrubu = HesapGrubu.DonenVarliklar,
                    UstHesapId = ustHesap?.Id,
                    Aktif = true,
                    CreatedAt = DateTime.UtcNow
                };
                context.MuhasebeHesaplari.Add(stokHesap);
                await context.SaveChangesAsync();
            }

            // Fiş numarası al
            var fisNo = await GenerateNextFisNoAsync(context);

            // Muhasebe fişi oluştur
            var fis = new MuhasebeFis
            {
                FisNo = fisNo,
                FisTarihi = DateTime.SpecifyKind(fatura.FaturaTarihi, DateTimeKind.Utc),
                FisTipi = FisTipi.Mahsup,
                Aciklama = $"Fatura {fatura.FaturaNo} - {aciklamaTipi} Masraf Aktarımı",
                ToplamBorc = tutar,
                ToplamAlacak = tutar,
                Durum = FisDurum.Onaylandi,
                Kaynak = FisKaynak.Fatura,
                KaynakId = fatura.Id,
                KaynakTip = "MasrafAktarim",
                CreatedAt = DateTime.UtcNow
            };

            fis.Kalemler.Add(new MuhasebeFisKalem
            {
                HesapId = masrafHesap.Id,
                SiraNo = 1,
                Borc = tutar,
                Alacak = 0,
                Tarih = fatura.FaturaTarihi,
                Aciklama = $"Masraf Aktarımı ({aciklamaTipi}) - {fatura.FaturaNo}",
                CreatedAt = DateTime.UtcNow
            });

            fis.Kalemler.Add(new MuhasebeFisKalem
            {
                HesapId = stokHesap.Id,
                SiraNo = 2,
                Borc = 0,
                Alacak = tutar,
                Tarih = fatura.FaturaTarihi,
                Aciklama = $"Stok Çıkışı ({aciklamaTipi}) - {fatura.FaturaNo}",
                CreatedAt = DateTime.UtcNow
            });

            context.MuhasebeFisleri.Add(fis);
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Muhasebe kaydı oluşturma hatası: {ex.Message}");
        }
    }
    #endregion

    #region Firmalar Arası Fatura

    /// <summary>
    /// Firmalar arası fatura için karşı firmada eşleşen fatura oluşturur
    /// Giden fatura = Karşı firmanın gelen faturası
    /// Gelen fatura = Karşı firmanın giden faturası
    /// </summary>
    private async Task CreateKarsiFirmaFaturasiAsync(ApplicationDbContext context, Fatura anaFatura)
    {
        try
        {
            // Karşı firma için cari var mı kontrol et (ana firmanın karşı firmadaki carisi)
            var anaFirma = await context.Firmalar.FirstOrDefaultAsync(f => f.Id == anaFatura.FirmaId);
            var karsiFirma = await context.Firmalar.FirstOrDefaultAsync(f => f.Id == anaFatura.KarsiFirmaId);

            if (anaFirma == null || karsiFirma == null) return;

            // Karşı firmada ana firmayı temsil eden cari bul veya oluştur
            var mevcutKarsiFatura = await context.Faturalar
                .FirstOrDefaultAsync(f => !f.IsDeleted &&
                                          f.FirmaId == anaFatura.KarsiFirmaId &&
                                          f.FaturaYonu == (anaFatura.FaturaYonu == FaturaYonu.Giden ? FaturaYonu.Gelen : FaturaYonu.Giden) &&
                                          f.FaturaNo == anaFatura.FaturaNo);

            if (mevcutKarsiFatura != null)
            {
                anaFatura.EslesenFaturaId = mevcutKarsiFatura.Id;
                mevcutKarsiFatura.EslesenFaturaId = anaFatura.Id;
                await context.SaveChangesAsync();
                return;
            }

            var karsiFirmadakiCari = await context.Cariler
                .FirstOrDefaultAsync(c => c.FirmaId == anaFatura.KarsiFirmaId && c.VergiNo == anaFirma.VergiNo && !c.IsDeleted);

            if (karsiFirmadakiCari == null)
            {
                // Cari yoksa oluştur
                var cariKodu = await GetUniqueCariCodeAsync(context, await GetNextCariNumAsync(context));
                karsiFirmadakiCari = new Cari
                {
                    CariKodu = cariKodu,
                    Unvan = anaFirma.FirmaAdi,
                    VergiNo = anaFirma.VergiNo ?? string.Empty,
                    FirmaId = anaFatura.KarsiFirmaId,
                    VergiDairesi = anaFirma.VergiDairesi,
                    Adres = anaFirma.Adres,
                    Il = anaFirma.Il,
                    Ilce = anaFirma.Ilce,
                    Telefon = anaFirma.Telefon,
                    Email = anaFirma.Email,
                    CariTipi = anaFatura.FaturaYonu == FaturaYonu.Giden ? CariTipi.Tedarikci : CariTipi.Musteri,
                    Aktif = true,
                    CreatedAt = DateTime.UtcNow
                };
                context.Cariler.Add(karsiFirmadakiCari);
                await context.SaveChangesAsync();
            }

            // Karşı firmada eşleşen fatura oluştur
            var karsiFatura = new Fatura
            {
                FaturaNo = anaFatura.FaturaNo, // Aynı fatura numarası
                FaturaTarihi = anaFatura.FaturaTarihi,
                VadeTarihi = anaFatura.VadeTarihi,
                FaturaTipi = anaFatura.FaturaYonu == FaturaYonu.Giden ? FaturaTipi.AlisFaturasi : FaturaTipi.SatisFaturasi,
                FaturaYonu = anaFatura.FaturaYonu == FaturaYonu.Giden ? FaturaYonu.Gelen : FaturaYonu.Giden,
                EFaturaTipi = anaFatura.EFaturaTipi,
                EttnNo = anaFatura.EttnNo,
                FirmaId = anaFatura.KarsiFirmaId, // Karşı firma
                CariId = karsiFirmadakiCari.Id,
                FirmalarArasiFatura = true,
                KarsiFirmaId = anaFatura.FirmaId, // Ana firma karşı firma oluyor
                EslesenFaturaId = anaFatura.Id, // Ana fatura ile eşleş
                AraToplam = anaFatura.AraToplam,
                IskontoTutar = anaFatura.IskontoTutar,
                KdvOrani = anaFatura.KdvOrani,
                KdvTutar = anaFatura.KdvTutar,
                GenelToplam = anaFatura.GenelToplam,
                TevkifatliMi = anaFatura.TevkifatliMi,
                TevkifatOrani = anaFatura.TevkifatOrani,
                TevkifatKodu = anaFatura.TevkifatKodu,
                TevkifatTutar = anaFatura.TevkifatTutar,
                XmlDosyaYolu = anaFatura.XmlDosyaYolu,
                PdfDosyaYolu = anaFatura.PdfDosyaYolu,
                ImportKaynak = "FirmalarArasi",
                Durum = FaturaDurum.Beklemede,
                Aciklama = $"Firmalar arası fatura - {anaFirma.FirmaAdi}",
                CreatedAt = DateTime.UtcNow
            };

            // Fatura kalemlerini kopyala
            if (anaFatura.FaturaKalemleri != null)
            {
                foreach (var kalem in anaFatura.FaturaKalemleri)
                {
                    karsiFatura.FaturaKalemleri.Add(new FaturaKalem
                    {
                        SiraNo = kalem.SiraNo,
                        UrunKodu = kalem.UrunKodu,
                        Aciklama = kalem.Aciklama,
                        Birim = kalem.Birim,
                        Miktar = kalem.Miktar,
                        BirimFiyat = kalem.BirimFiyat,
                        IskontoOrani = kalem.IskontoOrani,
                        IskontoTutar = kalem.IskontoTutar,
                        KdvOrani = kalem.KdvOrani,
                        KdvTutar = kalem.KdvTutar,
                        ToplamTutar = kalem.ToplamTutar,
                        KalemTipi = kalem.KalemTipi,
                        AltTipi = kalem.AltTipi,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            context.Faturalar.Add(karsiFatura);
            await context.SaveChangesAsync();

            // Ana faturayı karşı fatura ile eşleştir
            anaFatura.EslesenFaturaId = karsiFatura.Id;
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Karşı firma faturası oluşturma hatası: {ex.Message}");
        }
    }

    /// <summary>
    /// Firmalar arası faturaları mahsup ile kapatır
    /// </summary>
    public async Task<bool> MahsupKapatAsync(int faturaId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var fatura = await context.Faturalar
            .Include(f => f.EslesenFatura)
            .FirstOrDefaultAsync(f => f.Id == faturaId);

        if (fatura == null || !fatura.FirmalarArasiFatura || fatura.EslesenFaturaId == null)
            return false;

        var eslesenFatura = fatura.EslesenFatura;
        if (eslesenFatura == null) return false;

        // Her iki faturayı da ödenmiş olarak işaretle
        var mahsupTarihi = DateTime.UtcNow;

        fatura.OdenenTutar = fatura.GenelToplam;
        fatura.Durum = FaturaDurum.Odendi;
        fatura.MahsupKapatildi = true;
        fatura.MahsupTarihi = mahsupTarihi;
        fatura.UpdatedAt = mahsupTarihi;

        eslesenFatura.OdenenTutar = eslesenFatura.GenelToplam;
        eslesenFatura.Durum = FaturaDurum.Odendi;
        eslesenFatura.MahsupKapatildi = true;
        eslesenFatura.MahsupTarihi = mahsupTarihi;
        eslesenFatura.UpdatedAt = mahsupTarihi;

        await context.SaveChangesAsync();

        // Mahsup muhasebe fişi oluştur
        await CreateMahsupMuhasebeFisiAsync(context, fatura, eslesenFatura);

        return true;
    }

    /// <summary>
    /// Mahsup için muhasebe fişi oluşturur
    /// </summary>
    private async Task CreateMahsupMuhasebeFisiAsync(ApplicationDbContext context, Fatura fatura1, Fatura fatura2)
    {
        try
        {
            var fisNo = await GenerateNextFisNoAsync(context);

            var fis = new MuhasebeFis
            {
                FisNo = fisNo,
                FisTarihi = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
                FisTipi = FisTipi.Mahsup,
                Aciklama = $"Firmalar Arası Fatura Mahsubu - {fatura1.FaturaNo}",
                ToplamBorc = fatura1.GenelToplam,
                ToplamAlacak = fatura1.GenelToplam,
                Durum = FisDurum.Onaylandi,
                Kaynak = FisKaynak.Fatura,
                KaynakId = fatura1.Id,
                KaynakTip = "FirmalarArasiMahsup",
                CreatedAt = DateTime.UtcNow
            };

            // Alıcılar hesabı (120) - Giden fatura için borç
            // Satıcılar hesabı (320) - Gelen fatura için alacak
            var alicilarHesap = await context.MuhasebeHesaplari.FirstOrDefaultAsync(h => h.HesapKodu == "120");
            var saticilarHesap = await context.MuhasebeHesaplari.FirstOrDefaultAsync(h => h.HesapKodu == "320");

            if (alicilarHesap != null && saticilarHesap != null)
            {
                fis.Kalemler.Add(new MuhasebeFisKalem
                {
                    HesapId = saticilarHesap.Id,
                    SiraNo = 1,
                    Borc = fatura1.GenelToplam,
                    Alacak = 0,
                    Tarih = fis.FisTarihi,
                    Aciklama = $"Satıcı Mahsup - {fatura1.FaturaNo}",
                    CreatedAt = DateTime.UtcNow
                });

                fis.Kalemler.Add(new MuhasebeFisKalem
                {
                    HesapId = alicilarHesap.Id,
                    SiraNo = 2,
                    Borc = 0,
                    Alacak = fatura1.GenelToplam,
                    Tarih = fis.FisTarihi,
                    Aciklama = $"Alıcı Mahsup - {fatura1.FaturaNo}",
                    CreatedAt = DateTime.UtcNow
                });

                context.MuhasebeFisleri.Add(fis);
                await context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Mahsup muhasebe fişi hatası: {ex.Message}");
        }
    }

    /// <summary>
    /// Firmalar arası eşleşmemiş faturaları getirir
    /// </summary>
    public async Task<List<Fatura>> GetFirmalarArasiEslesmemisFaturalarAsync(int? firmaId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.Faturalar
            .Include(f => f.Cari)
            .Include(f => f.KarsiFirma)
            .Where(f => f.FirmalarArasiFatura && !f.MahsupKapatildi && f.EslesenFaturaId == null);

        if (firmaId.HasValue)
            query = query.Where(f => f.FirmaId == firmaId);

        return await query.OrderByDescending(f => f.FaturaTarihi).ToListAsync();
    }

    /// <summary>
    /// Firmalar arası faturaları manuel eşleştirir
    /// </summary>
    public async Task<bool> FaturalariEslestirAsync(int fatura1Id, int fatura2Id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var fatura1 = await context.Faturalar.FindAsync(fatura1Id);
        var fatura2 = await context.Faturalar.FindAsync(fatura2Id);

        if (fatura1 == null || fatura2 == null) return false;
        if (!fatura1.FirmalarArasiFatura || !fatura2.FirmalarArasiFatura) return false;
        if (fatura1.FaturaYonu == fatura2.FaturaYonu) return false; // Biri gelen, biri giden olmalı

        // Eşleştir
        fatura1.EslesenFaturaId = fatura2.Id;
        fatura2.EslesenFaturaId = fatura1.Id;
        fatura1.UpdatedAt = DateTime.UtcNow;
        fatura2.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
        return true;
    }

    #endregion
}
