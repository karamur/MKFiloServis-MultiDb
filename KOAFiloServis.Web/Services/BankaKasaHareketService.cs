using KOAFiloServis.Shared.Entities;
using KOAFiloServis.Web.Data;
using KOAFiloServis.Web.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace KOAFiloServis.Web.Services;

public class BankaKasaHareketService : IBankaKasaHareketService
{
    private const string IslemNoPrefix = "HRK";
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IMuhasebeService _muhasebeService;
    private readonly IBankaHesapService _bankaHesapService;
    private readonly NumaraSerisiService _numaraSerisi;

    public BankaKasaHareketService(IDbContextFactory<ApplicationDbContext> contextFactory, IMuhasebeService muhasebeService, IBankaHesapService bankaHesapService, NumaraSerisiService numaraSerisi)
    {
        _contextFactory = contextFactory;
        _muhasebeService = muhasebeService;
        _bankaHesapService = bankaHesapService;
        _numaraSerisi = numaraSerisi;
    }

    public async Task<List<BankaKasaHareket>> GetAllAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await QueryHareketler(context)
            .Include(h => h.BankaHesap)
            .Include(h => h.Cari)
            .Include(h => h.PersonelCebinden)
            .Include(h => h.Arac)
            .OrderByDescending(h => h.IslemTarihi)
            .ToListAsync();
    }

    public async Task<PagedResult<BankaKasaHareket>> GetPagedAsync(BankaHareketFilterParams filter)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = QueryHareketler(context)
            .Include(h => h.BankaHesap)
            .Include(h => h.Cari)
            .Include(h => h.PersonelCebinden)
            .Include(h => h.Arac)
            .AsQueryable();

        // Arama filtresi
        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var searchLower = filter.SearchTerm.ToLower();
            query = query.Where(h =>
                (h.IslemNo != null && h.IslemNo.ToLower().Contains(searchLower)) ||
                (h.Aciklama != null && h.Aciklama.ToLower().Contains(searchLower)) ||
                (h.BelgeNo != null && h.BelgeNo.ToLower().Contains(searchLower)) ||
                (h.BankaHesap != null && h.BankaHesap.HesapAdi.ToLower().Contains(searchLower)) ||
                (h.Cari != null && h.Cari.Unvan.ToLower().Contains(searchLower)));
        }

        // Hesap filtresi
        if (filter.HesapId.HasValue && filter.HesapId.Value > 0)
        {
            query = query.Where(h => h.BankaHesapId == filter.HesapId.Value);
        }

        // Cari filtresi
        if (filter.CariId.HasValue && filter.CariId.Value > 0)
        {
            query = query.Where(h => h.CariId == filter.CariId.Value);
        }

        // Hareket tipi filtresi
        if (filter.HareketTipi.HasValue)
        {
            query = query.Where(h => h.HareketTipi == filter.HareketTipi.Value);
        }

        // Tarih aralığı filtresi
        if (filter.BaslangicTarihi.HasValue)
        {
            query = query.Where(h => h.IslemTarihi >= filter.BaslangicTarihi.Value);
        }

        if (filter.BitisTarihi.HasValue)
        {
            query = query.Where(h => h.IslemTarihi <= filter.BitisTarihi.Value);
        }

        var totalItems = await query.CountAsync();

        var items = await query
            .OrderByDescending(h => h.IslemTarihi)
            .ThenByDescending(h => h.Id)
            .Skip(filter.Skip)
            .Take(filter.PageSize)
            .ToListAsync();

        return new PagedResult<BankaKasaHareket>(items, totalItems, filter.PageNumber, filter.PageSize);
    }

    public async Task<List<BankaKasaHareket>> GetRecentAsync(int count = 5)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await QueryHareketler(context)
            .Include(h => h.BankaHesap)
            .Include(h => h.Cari)
            .OrderByDescending(h => h.IslemTarihi)
            .Take(count)
            .ToListAsync();
    }

    public async Task<List<BankaKasaHareket>> GetByHesapIdAsync(int hesapId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await QueryHareketler(context)
            .Include(h => h.Cari)
            .Where(h => h.BankaHesapId == hesapId)
            .OrderByDescending(h => h.IslemTarihi)
            .ToListAsync();
    }

    public async Task<List<BankaKasaHareket>> GetByCariIdAsync(int cariId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await QueryHareketler(context)
            .Include(h => h.BankaHesap)
            .Where(h => h.CariId == cariId)
            .OrderByDescending(h => h.IslemTarihi)
            .ToListAsync();
    }

    public async Task<List<BankaKasaHareket>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await QueryHareketler(context)
            .Include(h => h.BankaHesap)
            .Include(h => h.Cari)
            .Where(h => h.IslemTarihi >= startDate && h.IslemTarihi <= endDate)
            .OrderByDescending(h => h.IslemTarihi)
            .ToListAsync();
    }

    public async Task<List<BankaKasaHareket>> GetByTipAsync(HareketTipi tip)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await QueryHareketler(context)
            .Include(h => h.BankaHesap)
            .Include(h => h.Cari)
            .Where(h => h.HareketTipi == tip)
            .OrderByDescending(h => h.IslemTarihi)
            .ToListAsync();
    }

    public async Task<List<BankaKasaHareket>> GetEslestirmeyeUygunHareketlerAsync(int cariId, HareketTipi tip)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        // Tamamen eşleştirilmemiş hareketleri getir
        var hareketler = await QueryHareketler(context)
            .Include(h => h.BankaHesap)
            .Include(h => h.OdemeEslestirmeleri)
            .Where(h => h.CariId == cariId && h.HareketTipi == tip)
            .OrderByDescending(h => h.IslemTarihi)
            .ToListAsync();

        // Henüz tam eşleştirilmemiş olanları filtrele
        return hareketler
            .Where(h => h.Tutar > h.OdemeEslestirmeleri.Sum(e => e.EslestirilenTutar))
            .ToList();
    }

    public async Task<BankaKasaHareket?> GetByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await QueryHareketler(context)
            .Include(h => h.BankaHesap)
            .Include(h => h.Cari)
            .Include(h => h.PersonelCebinden)
            .Include(h => h.Arac)
            .Include(h => h.OdemeEslestirmeleri)
            .ThenInclude(e => e.Fatura)
            .FirstOrDefaultAsync(h => h.Id == id);
    }

    public async Task<BankaKasaHareket> CreateAsync(BankaKasaHareket hareket)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        NormalizeHareket(hareket);
        await ValidateHareketAsync(context, hareket);
        await ApplyMuhasebeDefaultsAsync(context, hareket);
        hareket.BankaHesap = null!;
        hareket.Cari = null;
        context.BankaKasaHareketleri.Add(hareket);
        await context.SaveChangesAsync();
        return hareket;
    }

    public async Task<BankaKasaHareket> UpdateAsync(BankaKasaHareket hareket)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        NormalizeHareket(hareket);
        await ValidateHareketAsync(context, hareket);
        await ApplyMuhasebeDefaultsAsync(context, hareket);

        var existing = await QueryHareketler(context, asNoTracking: false)
            .FirstOrDefaultAsync(h => h.Id == hareket.Id);
        if (existing == null)
            throw new InvalidOperationException($"Banka/Kasa hareketi bulunamadı. Id: {hareket.Id}");

        existing.IslemNo = hareket.IslemNo;
        existing.IslemTarihi = hareket.IslemTarihi;
        existing.HareketTipi = hareket.HareketTipi;
        existing.Tutar = hareket.Tutar;
        existing.Aciklama = hareket.Aciklama;
        existing.BelgeNo = hareket.BelgeNo;
        existing.IslemKaynak = hareket.IslemKaynak;
        existing.MahsupHareketId = hareket.MahsupHareketId;
        existing.MahsupGrupId = hareket.MahsupGrupId;
        existing.MuhasebeHesapKodu = hareket.MuhasebeHesapKodu;
        existing.MuhasebeAltHesapKodu = hareket.MuhasebeAltHesapKodu;
        existing.KostMerkeziKodu = hareket.KostMerkeziKodu;
        existing.ProjeKodu = hareket.ProjeKodu;
        existing.MuhasebeAciklama = hareket.MuhasebeAciklama;
        existing.BankaHesapId = hareket.BankaHesapId;
        existing.CariId = hareket.CariId;
        existing.IsDeleted = hareket.IsDeleted;
        existing.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
        return existing;
    }

    private async Task ApplyMuhasebeDefaultsAsync(ApplicationDbContext context, BankaKasaHareket hareket)
    {
        if (string.IsNullOrWhiteSpace(hareket.MuhasebeAciklama) && !string.IsNullOrWhiteSpace(hareket.Aciklama))
        {
            hareket.MuhasebeAciklama = hareket.Aciklama;
        }

        if (hareket.BankaHesapId == 0)
            return;

        var hesap = await _bankaHesapService.GetByIdAsync(hareket.BankaHesapId);

        if (hesap == null)
            return;

        if (string.IsNullOrWhiteSpace(hareket.MuhasebeHesapKodu))
            hareket.MuhasebeHesapKodu = hesap.VarsayilanMuhasebeKodu;

        if (string.IsNullOrWhiteSpace(hareket.KostMerkeziKodu))
            hareket.KostMerkeziKodu = hesap.VarsayilanKostMerkezi;
    }

    private static IQueryable<BankaKasaHareket> QueryHareketler(ApplicationDbContext context, bool asNoTracking = true)
    {
        var query = context.BankaKasaHareketleri
            .Where(h => !h.IsDeleted);

        return asNoTracking ? query.AsNoTracking() : query;
    }

    private static IQueryable<Cari> QueryCariler(ApplicationDbContext context, bool asNoTracking = true)
    {
        var query = context.Cariler
            .Where(c => !c.IsDeleted);

        return asNoTracking ? query.AsNoTracking() : query;
    }

    private async Task ValidateHareketAsync(ApplicationDbContext context, BankaKasaHareket hareket)
    {
        if (string.IsNullOrWhiteSpace(hareket.IslemNo))
            throw new InvalidOperationException("İşlem no zorunludur.");

        if (hareket.IslemTarihi == default)
            throw new InvalidOperationException("İşlem tarihi zorunludur.");

        if (hareket.BankaHesapId <= 0)
            throw new InvalidOperationException("Geçerli bir hesap seçiniz.");

        if (hareket.Tutar <= 0)
            throw new InvalidOperationException("Tutar sıfırdan büyük olmalıdır.");

        var islemNoKullanimda = await QueryHareketler(context)
            .AnyAsync(h => h.Id != hareket.Id && h.IslemNo == hareket.IslemNo);

        if (islemNoKullanimda)
            throw new InvalidOperationException($"'{hareket.IslemNo}' işlem numarası zaten kullanımda.");

        var hesapVar = await _bankaHesapService.GetByIdAsync(hareket.BankaHesapId);
        if (hesapVar == null)
            throw new InvalidOperationException("Seçilen hesap bulunamadı.");

        if (hareket.CariId.HasValue)
        {
            var cariVar = await QueryCariler(context)
                .AnyAsync(c => c.Id == hareket.CariId.Value);

            if (!cariVar)
                throw new InvalidOperationException("Seçilen cari bulunamadı.");
        }
    }

    private static void NormalizeHareket(BankaKasaHareket hareket)
    {
        hareket.IslemNo = string.IsNullOrWhiteSpace(hareket.IslemNo)
            ? string.Empty
            : hareket.IslemNo.Trim().ToUpperInvariant();
        hareket.Aciklama = NormalizeNullableText(hareket.Aciklama);
        hareket.BelgeNo = NormalizeNullableText(hareket.BelgeNo);
        hareket.MuhasebeHesapKodu = NormalizeNullableText(hareket.MuhasebeHesapKodu);
        hareket.MuhasebeAltHesapKodu = NormalizeNullableText(hareket.MuhasebeAltHesapKodu);
        hareket.KostMerkeziKodu = NormalizeNullableText(hareket.KostMerkeziKodu);
        hareket.ProjeKodu = NormalizeNullableText(hareket.ProjeKodu);
        hareket.MuhasebeAciklama = NormalizeNullableText(hareket.MuhasebeAciklama);
        hareket.CariId = hareket.CariId <= 0 ? null : hareket.CariId;
        hareket.MahsupHareketId = hareket.MahsupHareketId <= 0 ? null : hareket.MahsupHareketId;
    }

    private static string? NormalizeNullableText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    public async Task DeleteAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var strategy = context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync();

            var hareket = await QueryHareketler(context, asNoTracking: false)
                .Include(h => h.OdemeEslestirmeleri)
                .FirstOrDefaultAsync(h => h.Id == id);

            if (hareket == null)
            {
                await transaction.CommitAsync();
                return;
            }

            // İlişkili bütçe ödemesini bul ve durumunu geri al
            var iliskiliOdeme = await context.BudgetOdemeler
                .FirstOrDefaultAsync(o => o.BankaKasaHareketId == id);

            if (iliskiliOdeme != null)
            {
                // Ödeme durumunu geri al
                iliskiliOdeme.Durum = OdemeDurum.Bekliyor;
                iliskiliOdeme.GercekOdemeTarihi = null;
                iliskiliOdeme.OdenenTutar = null;
                iliskiliOdeme.BankaKasaHareketId = null;
                iliskiliOdeme.OdemeYapildigiHesapId = null;
                iliskiliOdeme.OdemeNotu = null;
                iliskiliOdeme.MasrafKesintisi = 0;
                iliskiliOdeme.CezaKesintisi = 0;
                iliskiliOdeme.DigerKesinti = 0;
                iliskiliOdeme.KesintiAciklamasi = null;
                iliskiliOdeme.UpdatedAt = DateTime.UtcNow;
            }

            if (hareket.OdemeEslestirmeleri.Any())
            {
                context.OdemeEslestirmeleri.RemoveRange(hareket.OdemeEslestirmeleri);
            }

            context.BankaKasaHareketleri.Remove(hareket);
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        });
    }

    public async Task<string> GenerateNextIslemNoAsync(int firmaId = 0)
    {
        // Kural 15: FirmaId bazlı atomik numara üretimi
        return await _numaraSerisi.GenerateFormattedAsync(IslemNoPrefix, firmaId, 4);
    }

    // BankaHesap (Kasa/Banka) işlemleri
    public async Task<List<BankaHesap>> GetHesaplarAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await _bankaHesapService.GetAllAsync();
    }

    public async Task<List<BankaHesap>> GetAktifHesaplarAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await _bankaHesapService.GetActiveAsync();
    }

    public async Task<BankaHesap?> GetHesapByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await _bankaHesapService.GetByIdAsync(id);
    }

    public async Task<BankaHesap> CreateHesapAsync(BankaHesap hesap)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await _bankaHesapService.CreateAsync(hesap);
    }

    public async Task<BankaHesap> UpdateHesapAsync(BankaHesap hesap)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await _bankaHesapService.UpdateAsync(hesap);
    }

    public async Task DeleteHesapAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        await _bankaHesapService.DeleteAsync(id);
    }

    public async Task<DashboardBankaStats> GetDashboardStatsAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        try
        {
            // Calculate balances using a single optimized query
            var hesapBakiyeleri = await context.BankaHesaplari
                .AsNoTracking()
                .Where(h => !h.IsDeleted)
                .Select(h => new
                {
                    h.HesapTipi,
                    h.AcilisBakiye,
                    Girisler = QueryHareketler(context)
                        .Where(hr => hr.BankaHesapId == h.Id && hr.HareketTipi == HareketTipi.Giris)
                        .Sum(hr => (decimal?)hr.Tutar) ?? 0,
                    Cikislar = QueryHareketler(context)
                        .Where(hr => hr.BankaHesapId == h.Id && hr.HareketTipi == HareketTipi.Cikis)
                        .Sum(hr => (decimal?)hr.Tutar) ?? 0
                })
                .ToListAsync();

            return new DashboardBankaStats
            {
                ToplamKasa = hesapBakiyeleri
                    .Where(h => h.HesapTipi == HesapTipi.Kasa)
                    .Sum(h => h.AcilisBakiye + h.Girisler - h.Cikislar),
                ToplamBanka = hesapBakiyeleri
                    .Where(h => h.HesapTipi != HesapTipi.Kasa)
                    .Sum(h => h.AcilisBakiye + h.Girisler - h.Cikislar)
            };
        }
        catch (PostgresException ex) when (ex.SqlState == "42703")
        {
            // Eski tenant şemasında bazı kolonlar eksikse dashboard finans kartı sıfır değerle devam eder.
            return new DashboardBankaStats();
        }
    }

    // Mahsup İşlemleri
    public async Task<MahsupSonuc> HesaplarArasiTransferAsync(int kaynakHesapId, int hedefHesapId, decimal tutar, DateTime tarih, string aciklama, string? belgeNo = null, string? muhasebeHesapKodu = null, string? kostMerkeziKodu = null, string? projeKodu = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        if (kaynakHesapId == hedefHesapId)
            return new MahsupSonuc { Basarili = false, Hata = "Kaynak ve hedef hesap aynı olamaz." };

        if (tutar <= 0)
            return new MahsupSonuc { Basarili = false, Hata = "Tutar sıfırdan büyük olmalıdır." };

        var kaynakHesap = await _bankaHesapService.GetByIdAsync(kaynakHesapId);
        var hedefHesap = await _bankaHesapService.GetByIdAsync(hedefHesapId);

        if (kaynakHesap == null || hedefHesap == null)
            return new MahsupSonuc { Basarili = false, Hata = "Hesap bulunamadı." };

        if (!kaynakHesap.Aktif || !hedefHesap.Aktif)
            return new MahsupSonuc { Basarili = false, Hata = "Transfer için hesapların aktif olması gerekir." };

        var bakiye = await GetHesapBakiyeAsync(kaynakHesapId);
        if (bakiye < tutar)
            return new MahsupSonuc { Basarili = false, Hata = $"Yetersiz bakiye. Mevcut: {bakiye:N2} ₺" };

        // ExecutionStrategy ile transaction sarmalama (NpgsqlRetryingExecutionStrategy uyumluluğu)
        var strategy = context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await context.Database.BeginTransactionAsync();

            try
            {
                var mahsupGrupId = Guid.NewGuid();
                var islemNo = await GenerateNextIslemNoAsync();

            // Kaynak hesaptan çıkış
            var cikisHareket = new BankaKasaHareket
            {
                IslemNo = islemNo,
                IslemTarihi = tarih,
                HareketTipi = HareketTipi.Cikis,
                Tutar = tutar,
                BankaHesapId = kaynakHesapId,
                BelgeNo = string.IsNullOrWhiteSpace(belgeNo) ? null : belgeNo.Trim(),
                Aciklama = $"[TRANSFER] {hedefHesap.HesapAdi}'na transfer - {aciklama}",
                MuhasebeHesapKodu = string.IsNullOrWhiteSpace(muhasebeHesapKodu) ? kaynakHesap.VarsayilanMuhasebeKodu : muhasebeHesapKodu.Trim(),
                KostMerkeziKodu = string.IsNullOrWhiteSpace(kostMerkeziKodu) ? kaynakHesap.VarsayilanKostMerkezi : kostMerkeziKodu.Trim(),
                ProjeKodu = string.IsNullOrWhiteSpace(projeKodu) ? null : projeKodu.Trim(),
                MuhasebeAciklama = string.IsNullOrWhiteSpace(aciklama) ? null : aciklama.Trim(),
                IslemKaynak = IslemKaynak.Mahsup,
                MahsupGrupId = mahsupGrupId
            };
            NormalizeHareket(cikisHareket);
            await ValidateHareketAsync(context, cikisHareket);
            context.BankaKasaHareketleri.Add(cikisHareket);
            await context.SaveChangesAsync();

            // Hedef hesaba giriş
            var girisHareket = new BankaKasaHareket
            {
                IslemNo = await GenerateNextIslemNoAsync(),
                IslemTarihi = tarih,
                HareketTipi = HareketTipi.Giris,
                Tutar = tutar,
                BankaHesapId = hedefHesapId,
                BelgeNo = string.IsNullOrWhiteSpace(belgeNo) ? null : belgeNo.Trim(),
                Aciklama = $"[TRANSFER] {kaynakHesap.HesapAdi}'ndan transfer - {aciklama}",
                MuhasebeHesapKodu = string.IsNullOrWhiteSpace(muhasebeHesapKodu) ? hedefHesap.VarsayilanMuhasebeKodu : muhasebeHesapKodu.Trim(),
                KostMerkeziKodu = string.IsNullOrWhiteSpace(kostMerkeziKodu) ? hedefHesap.VarsayilanKostMerkezi : kostMerkeziKodu.Trim(),
                ProjeKodu = string.IsNullOrWhiteSpace(projeKodu) ? null : projeKodu.Trim(),
                MuhasebeAciklama = string.IsNullOrWhiteSpace(aciklama) ? null : aciklama.Trim(),
                IslemKaynak = IslemKaynak.Mahsup,
                MahsupGrupId = mahsupGrupId,
                MahsupHareketId = cikisHareket.Id
            };
            NormalizeHareket(girisHareket);
            await ValidateHareketAsync(context, girisHareket);
            context.BankaKasaHareketleri.Add(girisHareket);
            await context.SaveChangesAsync();

            // Çıkış hareketine de karşı hareket ID'si ekle
            cikisHareket.MahsupHareketId = girisHareket.Id;
            await context.SaveChangesAsync();

            // Muhasebe fişi oluştur
            try
            {
                await _muhasebeService.CreateHesapTransferFisiAsync(cikisHareket, girisHareket, kaynakHesap, hedefHesap);
            }
            catch
            {
                // Muhasebe entegrasyonu başarısız olsa bile işlem devam eder
                // Loglama eklenebilir
            }

            await transaction.CommitAsync();

            return new MahsupSonuc
            {
                Basarili = true,
                MahsupGrupId = mahsupGrupId,
                KaynakHareket = cikisHareket,
                HedefHareket = girisHareket
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return new MahsupSonuc { Basarili = false, Hata = ex.Message };
        }
        }); // ExecutionStrategy lambda sonu
    }

    public async Task<MahsupSonuc> CariMahsupAsync(int cariId, int hesapId, decimal tutar, DateTime tarih, string aciklama, bool caridenHesaba, string? belgeNo = null, string? muhasebeHesapKodu = null, string? kostMerkeziKodu = null, string? projeKodu = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        if (tutar <= 0)
            return new MahsupSonuc { Basarili = false, Hata = "Tutar sıfırdan büyük olmalıdır." };

        var cari = await QueryCariler(context)
            .FirstOrDefaultAsync(c => c.Id == cariId);
        var hesap = await _bankaHesapService.GetByIdAsync(hesapId);

        if (cari == null || hesap == null)
            return new MahsupSonuc { Basarili = false, Hata = "Cari veya hesap bulunamadı." };

        if (!hesap.Aktif)
            return new MahsupSonuc { Basarili = false, Hata = "Cari mahsup için hesabın aktif olması gerekir." };

        var strategy = context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync();

            try
            {
                var mahsupGrupId = Guid.NewGuid();
                var islemNo = await GenerateNextIslemNoAsync();

                // caridenHesaba = true: Cari bize borçlu, biz tahsil ediyoruz (Hesaba Giriş)
                // caridenHesaba = false: Biz cariye borçluyuz, ödeme yapıyoruz (Hesaptan Çıkış)
                var hareket = new BankaKasaHareket
                {
                    IslemNo = islemNo,
                    IslemTarihi = tarih,
                    HareketTipi = caridenHesaba ? HareketTipi.Giris : HareketTipi.Cikis,
                    Tutar = tutar,
                    BankaHesapId = hesapId,
                    CariId = cariId,
                    BelgeNo = string.IsNullOrWhiteSpace(belgeNo) ? null : belgeNo.Trim(),
                    Aciklama = $"[CARİ MAHSUP] {cari.Unvan} - {aciklama}",
                    MuhasebeHesapKodu = string.IsNullOrWhiteSpace(muhasebeHesapKodu) ? hesap.VarsayilanMuhasebeKodu : muhasebeHesapKodu.Trim(),
                    KostMerkeziKodu = string.IsNullOrWhiteSpace(kostMerkeziKodu) ? hesap.VarsayilanKostMerkezi : kostMerkeziKodu.Trim(),
                    ProjeKodu = string.IsNullOrWhiteSpace(projeKodu) ? null : projeKodu.Trim(),
                    MuhasebeAciklama = string.IsNullOrWhiteSpace(aciklama) ? null : aciklama.Trim(),
                    IslemKaynak = IslemKaynak.CariMahsup,
                    MahsupGrupId = mahsupGrupId
                };

                NormalizeHareket(hareket);
                await ValidateHareketAsync(context, hareket);
                context.BankaKasaHareketleri.Add(hareket);
                await context.SaveChangesAsync();

                try
                {
                    await _muhasebeService.CreateCariMahsupFisiAsync(hareket, cari, hesap, caridenHesaba);
                }
                catch
                {
                }

                await transaction.CommitAsync();

                return new MahsupSonuc
                {
                    Basarili = true,
                    MahsupGrupId = mahsupGrupId,
                    KaynakHareket = hareket
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new MahsupSonuc { Basarili = false, Hata = ex.Message };
            }
        });
    }

    public async Task<List<BankaKasaHareket>> GetMahsupHareketleriAsync(DateTime? baslangic = null, DateTime? bitis = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = QueryHareketler(context)
            .Include(h => h.BankaHesap)
            .Include(h => h.Cari)
            .Where(h => h.IslemKaynak == IslemKaynak.Mahsup || h.IslemKaynak == IslemKaynak.CariMahsup);

        if (baslangic.HasValue)
            query = query.Where(h => h.IslemTarihi >= baslangic.Value);

        if (bitis.HasValue)
            query = query.Where(h => h.IslemTarihi <= bitis.Value);

        var hareketler = await query
            .OrderByDescending(h => h.IslemTarihi)
            .ToListAsync();

        if (!hareketler.Any())
            return hareketler;

        var hareketIds = hareketler.Select(h => h.Id).ToHashSet();
        var mahsupGrupIds = hareketler.Where(h => h.MahsupGrupId.HasValue).Select(h => h.MahsupGrupId!.Value).ToHashSet();

        var fisler = await context.MuhasebeFisleri
            .AsNoTracking()
            .Where(f => (f.KaynakTip == "HesapTransfer" || f.KaynakTip == "CariMahsup") && f.KaynakId.HasValue && hareketIds.Contains(f.KaynakId.Value))
            .Select(f => new { f.Id, f.FisNo, f.Durum, f.KaynakId, f.KaynakTip })
            .ToListAsync();

        var iptalFisleri = await context.MuhasebeFisleri
            .AsNoTracking()
            .Where(f => f.KaynakTip == "IptalKaydi" && f.KaynakId.HasValue)
            .Select(f => new { f.FisNo, f.KaynakId })
            .ToListAsync();

        var fisByKaynakId = fisler
            .Where(f => f.KaynakId.HasValue)
            .ToDictionary(f => f.KaynakId!.Value, f => f);

        var iptalFisNoByFisId = iptalFisleri
            .Where(f => f.KaynakId.HasValue)
            .GroupBy(f => f.KaynakId!.Value)
            .ToDictionary(g => g.Key, g => g.Select(x => x.FisNo).FirstOrDefault());

        foreach (var hareket in hareketler)
        {
            var kaynakHareketId = hareket.Id;

            if (!fisByKaynakId.TryGetValue(kaynakHareketId, out var fis)
                && hareket.MahsupGrupId.HasValue
                && mahsupGrupIds.Contains(hareket.MahsupGrupId.Value))
            {
                var grupKaynakHareket = hareketler.FirstOrDefault(h => h.MahsupGrupId == hareket.MahsupGrupId && fisByKaynakId.ContainsKey(h.Id));
                if (grupKaynakHareket != null)
                {
                    fis = fisByKaynakId[grupKaynakHareket.Id];
                }
            }

            if (fis == null)
                continue;

            hareket.MuhasebeFisNo = fis.FisNo;
            hareket.MuhasebeFisDurumu = fis.Durum switch
            {
                FisDurum.Onaylandi => "Onaylandı",
                FisDurum.IptalEdildi => "İptal Edildi",
                _ => "Taslak"
            };

            if (iptalFisNoByFisId.TryGetValue(fis.Id, out var iptalFisNo) && !string.IsNullOrWhiteSpace(iptalFisNo))
            {
                hareket.IptalFisNo = iptalFisNo;
            }
        }

        return hareketler;
    }

    public async Task MahsupIptalAsync(Guid mahsupGrupId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var strategy = context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync();

            var hareketler = await QueryHareketler(context, asNoTracking: false)
                .Include(h => h.OdemeEslestirmeleri)
                .Where(h => h.MahsupGrupId == mahsupGrupId)
                .ToListAsync();

            try
            {
                await _muhasebeService.IptalFisiOlusturAsync(mahsupGrupId);
            }
            catch
            {
            }

            var eslestirmeler = hareketler.SelectMany(h => h.OdemeEslestirmeleri).ToList();
            if (eslestirmeler.Any())
            {
                context.OdemeEslestirmeleri.RemoveRange(eslestirmeler);
            }

            context.BankaKasaHareketleri.RemoveRange(hareketler);
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        });
    }

    public async Task<decimal> GetHesapBakiyeAsync(int hesapId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var hesap = await _bankaHesapService.GetByIdAsync(hesapId);
        if (hesap == null) return 0;

        var girisler = await QueryHareketler(context)
            .Where(h => h.BankaHesapId == hesapId && h.HareketTipi == HareketTipi.Giris)
            .SumAsync(h => (decimal?)h.Tutar) ?? 0;

        var cikislar = await QueryHareketler(context)
            .Where(h => h.BankaHesapId == hesapId && h.HareketTipi == HareketTipi.Cikis)
            .SumAsync(h => (decimal?)h.Tutar) ?? 0;

        return hesap.AcilisBakiye + girisler - cikislar;
    }

    public async Task<Dictionary<int, decimal>> GetTumHesapBakiyeleriAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var hesaplar = await context.BankaHesaplari
            .AsNoTracking()
            .Where(h => !h.IsDeleted && h.Aktif)
            .Select(h => new
            {
                h.Id,
                h.AcilisBakiye,
                Girisler = QueryHareketler(context)
                    .Where(hr => hr.BankaHesapId == h.Id && hr.HareketTipi == HareketTipi.Giris)
                    .Sum(hr => (decimal?)hr.Tutar) ?? 0,
                Cikislar = QueryHareketler(context)
                    .Where(hr => hr.BankaHesapId == h.Id && hr.HareketTipi == HareketTipi.Cikis)
                    .Sum(hr => (decimal?)hr.Tutar) ?? 0
            })
            .ToListAsync();

        return hesaplar.ToDictionary(h => h.Id, h => h.AcilisBakiye + h.Girisler - h.Cikislar);
    }

    public async Task<PersonelGeriOdemeSonuc> PersonelGeriOdemeYapAsync(int personelId, IEnumerable<int> cebindenHareketIds, int? hesapId, DateTime odemeTarihi, string? aciklama = null)
    {
        if (personelId <= 0)
            return new PersonelGeriOdemeSonuc { Basarili = false, Hata = "Personel seçilmelidir." };

        var idListesi = cebindenHareketIds?.Distinct().ToList() ?? new List<int>();
        if (idListesi.Count == 0)
            return new PersonelGeriOdemeSonuc { Basarili = false, Hata = "Kapatılacak hareket seçilmedi." };

        await using var context = await _contextFactory.CreateDbContextAsync();
        var strategy = context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync();
            try
            {
                var cebindenler = await context.BankaKasaHareketleri
                    .Where(h => idListesi.Contains(h.Id) && h.PersonelCebindenId == personelId && !h.PersoneleOdendi)
                    .ToListAsync();

                if (cebindenler.Count == 0)
                    return new PersonelGeriOdemeSonuc { Basarili = false, Hata = "Geçerli (ödenmemiş) cebinden kaydı bulunamadı." };

                var toplam = cebindenler.Sum(x => x.Tutar);

                BankaKasaHareket? odemeHareketi = null;

                // Hesap seçildiyse karşı çıkış hareketi oluştur (kasa/banka düşülecek).
                // Hesap seçilmediyse: sadece "ödendi" işaretle (elden / nakit dışı senaryo).
                if (hesapId.HasValue && hesapId.Value > 0)
                {
                    var hesap = await _bankaHesapService.GetByIdAsync(hesapId.Value);
                    if (hesap == null)
                        return new PersonelGeriOdemeSonuc { Basarili = false, Hata = "Seçilen hesap bulunamadı." };
                    if (!hesap.Aktif)
                        return new PersonelGeriOdemeSonuc { Basarili = false, Hata = "Hesap aktif değil." };

                    var personelAd = cebindenler.First().PersonelCebindenId.HasValue
                        ? (await context.Soforler.AsNoTracking().Where(s => s.Id == personelId).Select(s => s.Ad + " " + s.Soyad).FirstOrDefaultAsync() ?? "Personel")
                        : "Personel";

                    odemeHareketi = new BankaKasaHareket
                    {
                        IslemNo = await GenerateNextIslemNoAsync(),
                        IslemTarihi = odemeTarihi,
                        HareketTipi = HareketTipi.Cikis,
                        Tutar = toplam,
                        BankaHesapId = hesapId.Value,
                        IslemKaynak = IslemKaynak.PersonelGeriOdeme,
                        Aciklama = string.IsNullOrWhiteSpace(aciklama)
                            ? $"[PERSONEL GERİ ÖDEME] {personelAd} - {cebindenler.Count} kayıt"
                            : $"[PERSONEL GERİ ÖDEME] {personelAd} - {aciklama.Trim()}",
                        MuhasebeHesapKodu = hesap.VarsayilanMuhasebeKodu,
                        KostMerkeziKodu = hesap.VarsayilanKostMerkezi
                    };

                    NormalizeHareket(odemeHareketi);
                    await ValidateHareketAsync(context, odemeHareketi);
                    context.BankaKasaHareketleri.Add(odemeHareketi);
                    await context.SaveChangesAsync();
                }

                // Cebinden kayıtlarını kapat
                foreach (var c in cebindenler)
                {
                    c.PersoneleOdendi = true;
                    c.PersonelOdemeTarihi = odemeTarihi;
                    c.PersonelOdemeHesapId = hesapId;
                    c.PersonelGeriOdemeHareketId = odemeHareketi?.Id;
                }
                await context.SaveChangesAsync();

                await transaction.CommitAsync();

                return new PersonelGeriOdemeSonuc
                {
                    Basarili = true,
                    OdemeHareketi = odemeHareketi,
                    KapatilanKayitSayisi = cebindenler.Count,
                    ToplamTutar = toplam
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new PersonelGeriOdemeSonuc { Basarili = false, Hata = ex.Message };
            }
        });
    }

    public async Task PersonelGeriOdemeIptalAsync(int cebindenHareketId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var strategy = context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync();

            var cebinden = await context.BankaKasaHareketleri
                .FirstOrDefaultAsync(h => h.Id == cebindenHareketId);
            if (cebinden == null)
            {
                await transaction.CommitAsync();
                return;
            }

            var odemeId = cebinden.PersonelGeriOdemeHareketId;

            cebinden.PersoneleOdendi = false;
            cebinden.PersonelOdemeTarihi = null;
            cebinden.PersonelOdemeHesapId = null;
            cebinden.PersonelGeriOdemeHareketId = null;
            await context.SaveChangesAsync();

            // Eğer ödeme hareketi başka cebinden kaydını kapatmıyorsa, ödeme hareketini de sil
            if (odemeId.HasValue)
            {
                var hala = await context.BankaKasaHareketleri
                    .AnyAsync(h => h.PersonelGeriOdemeHareketId == odemeId.Value);
                if (!hala)
                {
                    var odeme = await context.BankaKasaHareketleri.FirstOrDefaultAsync(h => h.Id == odemeId.Value);
                    if (odeme != null && odeme.IslemKaynak == IslemKaynak.PersonelGeriOdeme)
                    {
                        context.BankaKasaHareketleri.Remove(odeme);
                        await context.SaveChangesAsync();
                    }
                }
            }

            await transaction.CommitAsync();
        });
    }

}
