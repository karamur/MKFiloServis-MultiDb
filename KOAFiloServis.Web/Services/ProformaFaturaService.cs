using KOAFiloServis.Shared.Entities;
using KOAFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace KOAFiloServis.Web.Services;

public class ProformaFaturaService : IProformaFaturaService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IFaturaService _faturaService;
    private readonly NumaraSerisiService _numaraSerisi;
    private readonly ILogger<ProformaFaturaService> _logger;

    public ProformaFaturaService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IFaturaService faturaService,
        NumaraSerisiService numaraSerisi,
        ILogger<ProformaFaturaService> logger)
    {
        _contextFactory = contextFactory;
        _faturaService = faturaService;
        _numaraSerisi = numaraSerisi;
        _logger = logger;
    }

    #region CRUD

    public async Task<List<ProformaFatura>> GetAllAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.ProformaFaturalar
            .AsNoTracking()
            .Include(p => p.Cari)
            .Include(p => p.Firma)
            .OrderByDescending(p => p.ProformaTarihi)
            .ToListAsync();
    }

    public async Task<List<ProformaFatura>> GetByCariIdAsync(int cariId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.ProformaFaturalar
            .AsNoTracking()
            .Include(p => p.Firma)
            .Where(p => p.CariId == cariId)
            .OrderByDescending(p => p.ProformaTarihi)
            .ToListAsync();
    }

    public async Task<List<ProformaFatura>> GetByDurumAsync(ProformaDurum durum)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.ProformaFaturalar
            .AsNoTracking()
            .Include(p => p.Cari)
            .Include(p => p.Firma)
            .Where(p => p.Durum == durum)
            .OrderByDescending(p => p.ProformaTarihi)
            .ToListAsync();
    }

    public async Task<List<ProformaFatura>> GetByDateRangeAsync(DateTime baslangic, DateTime bitis)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.ProformaFaturalar
            .AsNoTracking()
            .Include(p => p.Cari)
            .Include(p => p.Firma)
            .Where(p => p.ProformaTarihi >= baslangic && p.ProformaTarihi <= bitis)
            .OrderByDescending(p => p.ProformaTarihi)
            .ToListAsync();
    }

    public async Task<ProformaFatura?> GetByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.ProformaFaturalar
            .AsNoTracking()
            .Include(p => p.Cari)
            .Include(p => p.Firma)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<ProformaFatura?> GetByIdWithKalemlerAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.ProformaFaturalar
            .AsNoTracking()
            .Include(p => p.Cari)
            .Include(p => p.Firma)
            .Include(p => p.Kalemler.OrderBy(k => k.SiraNo))
                .ThenInclude(k => k.StokKarti)
            .Include(p => p.Fatura)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<ProformaFatura> CreateAsync(ProformaFatura proforma)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        if (string.IsNullOrEmpty(proforma.ProformaNo))
        {
            proforma.ProformaNo = await GenerateNextProformaNoAsync();
        }

        proforma.CreatedAt = DateTime.UtcNow;
        proforma = await HesaplaAsync(proforma);

        context.ProformaFaturalar.Add(proforma);
        await context.SaveChangesAsync();

        _logger.LogInformation("Proforma fatura oluşturuldu: {ProformaNo}", proforma.ProformaNo);
        return proforma;
    }

    public async Task<ProformaFatura> UpdateAsync(ProformaFatura proforma)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var existing = await context.ProformaFaturalar
            .Include(p => p.Kalemler)
            .FirstOrDefaultAsync(p => p.Id == proforma.Id)
            ?? throw new InvalidOperationException("Proforma fatura bulunamadı.");

        // Faturaya dönüştürülmüşse güncelleme yapılmaz
        if (existing.FaturayaDonusturuldu)
        {
            throw new InvalidOperationException("Faturaya dönüştürülmüş proforma güncellenemez.");
        }

        // Temel alanları güncelle
        existing.ProformaTarihi = proforma.ProformaTarihi;
        existing.GecerlilikTarihi = proforma.GecerlilikTarihi;
        existing.CariId = proforma.CariId;
        existing.FirmaId = proforma.FirmaId;
        existing.OdemeKosulu = proforma.OdemeKosulu;
        existing.TeslimKosulu = proforma.TeslimKosulu;
        existing.VadeGun = proforma.VadeGun;
        existing.IlgiliKisi = proforma.IlgiliKisi;
        existing.Telefon = proforma.Telefon;
        existing.Email = proforma.Email;
        existing.Aciklama = proforma.Aciklama;
        existing.OzelNotlar = proforma.OzelNotlar;
        existing.IskontoOrani = proforma.IskontoOrani;
        existing.KdvOrani = proforma.KdvOrani;
        existing.UpdatedAt = DateTime.UtcNow;

        // Yeniden hesapla
        existing = await HesaplaAsync(existing);

        await context.SaveChangesAsync();

        _logger.LogInformation("Proforma fatura güncellendi: {ProformaNo}", existing.ProformaNo);
        return existing;
    }

    public async Task DeleteAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var proforma = await context.ProformaFaturalar
            .FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new InvalidOperationException("Proforma fatura bulunamadı.");

        if (proforma.FaturayaDonusturuldu)
        {
            throw new InvalidOperationException("Faturaya dönüştürülmüş proforma silinemez.");
        }

        proforma.IsDeleted = true;
        proforma.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        _logger.LogInformation("Proforma fatura silindi: {ProformaNo}", proforma.ProformaNo);
    }

    #endregion

    #region Numara Üretimi

    public async Task<string> GenerateNextProformaNoAsync(int firmaId = 0)
    {
        // Kural 15: FirmaId bazlı atomik numara üretimi
        return await _numaraSerisi.GenerateFormattedAsync("PRF", firmaId, 5);
    }

    #endregion

    #region Kalem İşlemleri

    public async Task<ProformaFaturaKalem> AddKalemAsync(ProformaFaturaKalem kalem)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var proforma = await context.ProformaFaturalar
            .Include(p => p.Kalemler)
            .FirstOrDefaultAsync(p => p.Id == kalem.ProformaFaturaId)
            ?? throw new InvalidOperationException("Proforma fatura bulunamadı.");

        if (proforma.FaturayaDonusturuldu)
        {
            throw new InvalidOperationException("Faturaya dönüştürülmüş proformaya kalem eklenemez.");
        }

        // Sıra numarası ata
        kalem.SiraNo = proforma.Kalemler.Count + 1;
        kalem.CreatedAt = DateTime.UtcNow;

        // Kalem hesapla
        kalem = HesaplaKalem(kalem);

        context.ProformaFaturaKalemler.Add(kalem);
        
        // Proformayı yeniden hesapla
        proforma = await HesaplaAsync(proforma);
        
        await context.SaveChangesAsync();
        return kalem;
    }

    public async Task<ProformaFaturaKalem> UpdateKalemAsync(ProformaFaturaKalem kalem)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var existing = await context.ProformaFaturaKalemler
            .Include(k => k.ProformaFatura)
            .FirstOrDefaultAsync(k => k.Id == kalem.Id)
            ?? throw new InvalidOperationException("Kalem bulunamadı.");

        if (existing.ProformaFatura.FaturayaDonusturuldu)
        {
            throw new InvalidOperationException("Faturaya dönüştürülmüş proformanın kalemi güncellenemez.");
        }

        existing.UrunAdi = kalem.UrunAdi;
        existing.UrunKodu = kalem.UrunKodu;
        existing.Aciklama = kalem.Aciklama;
        existing.Miktar = kalem.Miktar;
        existing.Birim = kalem.Birim;
        existing.BirimFiyat = kalem.BirimFiyat;
        existing.IskontoOrani = kalem.IskontoOrani;
        existing.KdvOrani = kalem.KdvOrani;
        existing.StokKartiId = kalem.StokKartiId;
        existing.UpdatedAt = DateTime.UtcNow;

        // Kalem hesapla
        existing = HesaplaKalem(existing);

        // Proformayı yeniden hesapla
        var proforma = await context.ProformaFaturalar
            .Include(p => p.Kalemler)
            .FirstAsync(p => p.Id == existing.ProformaFaturaId);
        await HesaplaAsync(proforma);

        await context.SaveChangesAsync();
        return existing;
    }

    public async Task DeleteKalemAsync(int kalemId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var kalem = await context.ProformaFaturaKalemler
            .Include(k => k.ProformaFatura)
            .FirstOrDefaultAsync(k => k.Id == kalemId)
            ?? throw new InvalidOperationException("Kalem bulunamadı.");

        if (kalem.ProformaFatura.FaturayaDonusturuldu)
        {
            throw new InvalidOperationException("Faturaya dönüştürülmüş proformanın kalemi silinemez.");
        }

        kalem.IsDeleted = true;
        kalem.UpdatedAt = DateTime.UtcNow;

        // Proformayı yeniden hesapla
        var proforma = await context.ProformaFaturalar
            .Include(p => p.Kalemler)
            .FirstAsync(p => p.Id == kalem.ProformaFaturaId);
        await HesaplaAsync(proforma);

        await context.SaveChangesAsync();
    }

    public async Task<List<ProformaFaturaKalem>> GetKalemlerAsync(int proformaId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.ProformaFaturaKalemler
            .AsNoTracking()
            .Include(k => k.StokKarti)
            .Where(k => k.ProformaFaturaId == proformaId)
            .OrderBy(k => k.SiraNo)
            .ToListAsync();
    }

    private ProformaFaturaKalem HesaplaKalem(ProformaFaturaKalem kalem)
    {
        kalem.AraToplam = kalem.Miktar * kalem.BirimFiyat;
        kalem.IskontoTutar = kalem.AraToplam * (kalem.IskontoOrani / 100);
        kalem.NetTutar = kalem.AraToplam - kalem.IskontoTutar;
        kalem.KdvTutar = kalem.NetTutar * (kalem.KdvOrani / 100);
        kalem.ToplamTutar = kalem.NetTutar + kalem.KdvTutar;
        return kalem;
    }

    #endregion

    #region Hesaplama

    public Task<ProformaFatura> HesaplaAsync(ProformaFatura proforma)
    {
        if (proforma.Kalemler == null || !proforma.Kalemler.Any())
        {
            proforma.AraToplam = 0;
            proforma.IskontoTutar = 0;
            proforma.KdvTutar = 0;
            proforma.GenelToplam = 0;
            return Task.FromResult(proforma);
        }

        // Aktif kalemler
        var kalemler = proforma.Kalemler.Where(k => !k.IsDeleted).ToList();

        // Ara toplam (iskonto öncesi)
        proforma.AraToplam = kalemler.Sum(k => k.AraToplam);

        // Genel iskonto
        if (proforma.IskontoOrani > 0)
        {
            proforma.IskontoTutar = proforma.AraToplam * (proforma.IskontoOrani / 100);
        }
        else
        {
            proforma.IskontoTutar = kalemler.Sum(k => k.IskontoTutar);
        }

        // KDV hesapla
        var netTutar = proforma.AraToplam - proforma.IskontoTutar;
        proforma.KdvTutar = netTutar * (proforma.KdvOrani / 100);

        // Genel toplam
        proforma.GenelToplam = netTutar + proforma.KdvTutar;

        return Task.FromResult(proforma);
    }

    #endregion

    #region Durum Değişiklikleri

    public async Task<ProformaFatura> DurumDegistirAsync(int id, ProformaDurum yeniDurum)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var proforma = await context.ProformaFaturalar
            .FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new InvalidOperationException("Proforma fatura bulunamadı.");

        if (proforma.FaturayaDonusturuldu && yeniDurum != ProformaDurum.FaturayaDonusturuldu)
        {
            throw new InvalidOperationException("Faturaya dönüştürülmüş proformanın durumu değiştirilemez.");
        }

        proforma.Durum = yeniDurum;
        proforma.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        _logger.LogInformation("Proforma durumu değiştirildi: {ProformaNo} -> {Durum}", proforma.ProformaNo, yeniDurum);
        return proforma;
    }

    public async Task<ProformaFatura> GonderildiOlarakIsaretle(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await DurumDegistirAsync(id, ProformaDurum.Gonderildi);
    }

    public async Task<ProformaFatura> OnaylandiOlarakIsaretle(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await DurumDegistirAsync(id, ProformaDurum.Onaylandi);
    }

    public async Task<ProformaFatura> ReddedildiOlarakIsaretle(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await DurumDegistirAsync(id, ProformaDurum.Reddedildi);
    }

    #endregion

    #region Faturaya Dönüştürme

    public async Task<Fatura> FaturayaDonusturAsync(int proformaId, DateTime? faturaTarihi = null, DateTime? vadeTarihi = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var proforma = await context.ProformaFaturalar
            .Include(p => p.Kalemler.Where(k => !k.IsDeleted))
            .Include(p => p.Cari)
            .FirstOrDefaultAsync(p => p.Id == proformaId)
            ?? throw new InvalidOperationException("Proforma fatura bulunamadı.");

        if (proforma.FaturayaDonusturuldu)
        {
            throw new InvalidOperationException("Bu proforma zaten faturaya dönüştürülmüş.");
        }

        // Fatura oluştur
        var fatura = new Fatura
        {
            FaturaNo = await _faturaService.GenerateNextFaturaNoAsync(FaturaTipi.SatisFaturasi),
            FaturaTarihi = faturaTarihi ?? DateTime.UtcNow,
            VadeTarihi = vadeTarihi ?? DateTime.UtcNow.AddDays(proforma.VadeGun ?? 30),
            FaturaTipi = FaturaTipi.SatisFaturasi,
            FaturaYonu = FaturaYonu.Giden,
            EFaturaTipi = EFaturaTipi.EArsiv,
            Durum = FaturaDurum.Beklemede,
            CariId = proforma.CariId,
            FirmaId = proforma.FirmaId,
            AraToplam = proforma.AraToplam,
            IskontoTutar = proforma.IskontoTutar,
            KdvOrani = proforma.KdvOrani,
            KdvTutar = proforma.KdvTutar,
            GenelToplam = proforma.GenelToplam,
            Aciklama = $"Proforma No: {proforma.ProformaNo} - {proforma.Aciklama}",
            CreatedAt = DateTime.UtcNow
        };

        // Fatura kalemlerini ekle
        int siraNo = 1;
        foreach (var kalem in proforma.Kalemler.OrderBy(k => k.SiraNo))
        {
            fatura.FaturaKalemleri.Add(new FaturaKalem
            {
                SiraNo = siraNo++,
                UrunKodu = kalem.UrunKodu ?? "",
                Aciklama = kalem.UrunAdi + (string.IsNullOrEmpty(kalem.Aciklama) ? "" : $" - {kalem.Aciklama}"),
                Miktar = kalem.Miktar,
                Birim = kalem.Birim,
                BirimFiyat = kalem.BirimFiyat,
                IskontoOrani = kalem.IskontoOrani,
                IskontoTutar = kalem.IskontoTutar,
                KdvOrani = kalem.KdvOrani,
                KdvTutar = kalem.KdvTutar,
                ToplamTutar = kalem.ToplamTutar,
                CreatedAt = DateTime.UtcNow
            });
        }

        // Faturayı kaydet
        fatura = await _faturaService.CreateAsync(fatura);

        // Proformayı güncelle
        proforma.FaturayaDonusturuldu = true;
        proforma.FaturaId = fatura.Id;
        proforma.FaturaDonusumTarihi = DateTime.UtcNow;
        proforma.Durum = ProformaDurum.FaturayaDonusturuldu;
        proforma.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        _logger.LogInformation("Proforma faturaya dönüştürüldü: {ProformaNo} -> {FaturaNo}", 
            proforma.ProformaNo, fatura.FaturaNo);

        return fatura;
    }

    public async Task<bool> FaturayaDonusturulmusMu(int proformaId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.ProformaFaturalar
            .AsNoTracking()
            .AnyAsync(p => p.Id == proformaId && p.FaturayaDonusturuldu);
    }

    #endregion

    #region Export

    public async Task<byte[]> ExportToPdfAsync(int proformaId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        // Bu metod QuestPDF veya benzeri bir kütüphane ile implemente edilebilir
        // Şimdilik basit bir placeholder
        var proforma = await GetByIdWithKalemlerAsync(proformaId)
            ?? throw new InvalidOperationException("Proforma fatura bulunamadı.");

        // TODO: PDF oluşturma implementasyonu
        throw new NotImplementedException("PDF export henüz implemente edilmedi.");
    }

    public async Task<byte[]> ExportToExcelAsync(List<ProformaFatura> proformalars)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        using var workbook = new ClosedXML.Excel.XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Proforma Faturalar");

        // Başlıklar
        worksheet.Cell(1, 1).Value = "Proforma No";
        worksheet.Cell(1, 2).Value = "Tarih";
        worksheet.Cell(1, 3).Value = "Geçerlilik";
        worksheet.Cell(1, 4).Value = "Cari";
        worksheet.Cell(1, 5).Value = "Durum";
        worksheet.Cell(1, 6).Value = "Ara Toplam";
        worksheet.Cell(1, 7).Value = "KDV";
        worksheet.Cell(1, 8).Value = "Genel Toplam";

        var headerRange = worksheet.Range(1, 1, 1, 8);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightBlue;

        // Veriler
        int row = 2;
        foreach (var p in proformalars)
        {
            worksheet.Cell(row, 1).Value = p.ProformaNo;
            worksheet.Cell(row, 2).Value = p.ProformaTarihi.ToString("dd.MM.yyyy");
            worksheet.Cell(row, 3).Value = p.GecerlilikTarihi.ToString("dd.MM.yyyy");
            worksheet.Cell(row, 4).Value = p.Cari?.Unvan ?? "";
            worksheet.Cell(row, 5).Value = p.Durum.ToString();
            worksheet.Cell(row, 6).Value = p.AraToplam;
            worksheet.Cell(row, 7).Value = p.KdvTutar;
            worksheet.Cell(row, 8).Value = p.GenelToplam;
            row++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    #endregion

    #region Dashboard

    public async Task<ProformaDashboardStats> GetDashboardStatsAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var stats = new ProformaDashboardStats();

        var proformalars = await context.ProformaFaturalar
            .AsNoTracking()
            .Include(p => p.Cari)
            .ToListAsync();

        stats.TaslakSayisi = proformalars.Count(p => p.Durum == ProformaDurum.Taslak);
        stats.GonderilmisSayisi = proformalars.Count(p => p.Durum == ProformaDurum.Gonderildi);
        stats.OnayliSayisi = proformalars.Count(p => p.Durum == ProformaDurum.Onaylandi);
        stats.ReddedilenSayisi = proformalars.Count(p => p.Durum == ProformaDurum.Reddedildi);
        stats.ToplamTutar = proformalars.Sum(p => p.GenelToplam);
        stats.OnayliTutar = proformalars.Where(p => p.Durum == ProformaDurum.Onaylandi).Sum(p => p.GenelToplam);

        // Süresi dolacaklar (3 gün içinde)
        var ucGunSonra = DateTime.UtcNow.AddDays(3);
        stats.SuresiDolacaklar = proformalars
            .Where(p => p.Durum == ProformaDurum.Gonderildi && 
                       p.GecerlilikTarihi <= ucGunSonra && 
                       p.GecerlilikTarihi >= DateTime.UtcNow)
            .OrderBy(p => p.GecerlilikTarihi)
            .ToList();

        return stats;
    }

    #endregion
}
