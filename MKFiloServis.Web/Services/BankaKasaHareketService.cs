using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using MKFiloServis.Web.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Web.Services;

public class BankaKasaHareketService : IBankaKasaHareketService
{
    private const string IslemNoPrefix = "HRK";
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IMuhasebeService _muhasebeService;
    private readonly IBankaHesapService _bankaHesapService;
    private readonly NumaraSerisiService _numaraSerisi;
    private readonly IAktifFirmaProvider _aktifFirmaProvider;
    private readonly CurrentPermissionGuard _permissionGuard;

    public BankaKasaHareketService(IDbContextFactory<ApplicationDbContext> contextFactory, IMuhasebeService muhasebeService, IBankaHesapService bankaHesapService, NumaraSerisiService numaraSerisi, IAktifFirmaProvider aktifFirmaProvider, CurrentPermissionGuard permissionGuard)
    {
        _contextFactory = contextFactory;
        _muhasebeService = muhasebeService;
        _bankaHesapService = bankaHesapService;
        _numaraSerisi = numaraSerisi;
        _aktifFirmaProvider = aktifFirmaProvider;
        _permissionGuard = permissionGuard;
    }

    private async Task<T> WriteBankAsync<T>(string permission, Func<ApplicationDbContext, Task<T>> write)
    {
        await using var strategyContext = await _contextFactory.CreateDbContextAsync();
        var strategy = strategyContext.Database.CreateExecutionStrategy();
        var commitStarted = false;
        return await strategy.ExecuteAsync(async () =>
        {
            if (commitStarted)
                throw new InvalidOperationException("Banka işleminin commit sonucu belirsiz; listeyi yenileyip kaydı kontrol edin.");
            await _permissionGuard.RequireAnyAsync(permission);
            await using var context = await _contextFactory.CreateDbContextAsync();
            await using var transaction = await context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            var result = await write(context);
            await context.SaveChangesAsync();
            commitStarted = true;
            await transaction.CommitAsync();
            return result;
        });
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

        if (filter.IslemKaynak.HasValue)
            query = query.Where(h => h.IslemKaynak == filter.IslemKaynak.Value);

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
        await _permissionGuard.RequireAnyAsync(Yetkiler.BankaHareketleriYaz);
        await using var context = await _contextFactory.CreateDbContextAsync();
        if (hareket.IslemKimligi != null || hareket.IslemOzeti != null)
            throw new InvalidOperationException("İşlem anahtarı yalnız kaynak mali işlemden atanabilir.");
        if (hareket.Id != 0 || hareket.IsDeleted || hareket.MuhasebeFisId.HasValue || hareket.MahsupGrupId.HasValue || hareket.MahsupHareketId.HasValue ||
            hareket.PersoneleOdendi || hareket.PersonelGeriOdemeHareketId.HasValue || hareket.PersonelOdemeTarihi.HasValue || hareket.PersonelOdemeHesapId.HasValue)
            throw new InvalidOperationException("Yeni hareket silinmiş/işlenmiş olamaz veya bağlı mali işlem kimliği taşıyamaz.");
        NormalizeHareket(hareket);
        await ValidateHareketAsync(context, hareket);
        await ApplyMuhasebeDefaultsAsync(context, hareket);
        hareket.BankaHesap = null!;
        hareket.Cari = null;
        context.BankaKasaHareketleri.Add(hareket);
        await context.SaveChangesAsync();
        return hareket;
    }

    public async Task<BankaKasaHareket> CreateImportedAsync(BankaKasaHareket hareket, string islemKimligi)
    {
        var result = await CreateImportedBatchAsync(new[] { (hareket, islemKimligi) });
        return result[0];
    }

    public Task<IReadOnlyList<BankaKasaHareket>> CreateImportedBatchAsync(
        IReadOnlyCollection<(BankaKasaHareket Hareket, string IslemKimligi)> hareketler)
    {
        if (hareketler == null || hareketler.Count == 0)
            throw new ArgumentException("İçe aktarılacak hareket bulunamadı.", nameof(hareketler));
        var entries = hareketler.Select(item => (item.Hareket, Key: NormalizeBankOperationKey(item.IslemKimligi))).ToList();
        if (entries.Select(item => item.Key).Distinct(StringComparer.Ordinal).Count() != entries.Count)
            throw new InvalidOperationException("Aktarım paketinde yinelenen işlem kimliği var.");

        return WriteBankAsync(Yetkiler.BankaHareketleriYaz, async context =>
        {
            RequireSelectedFinanceFirma(_aktifFirmaProvider.AktifFirmaId);
            var firmaId = _aktifFirmaProvider.AktifFirmaId!.Value;
            var results = new List<BankaKasaHareket>(entries.Count);
            foreach (var (hareket, key) in entries)
            {
                if (hareket.Id != 0 || hareket.IsDeleted || hareket.MuhasebeFisId.HasValue ||
                    hareket.MahsupGrupId.HasValue || hareket.MahsupHareketId.HasValue ||
                    hareket.PersoneleOdendi || hareket.PersonelGeriOdemeHareketId.HasValue ||
                    hareket.PersonelOdemeTarihi.HasValue || hareket.PersonelOdemeHesapId.HasValue)
                    throw new InvalidOperationException("İçe aktarılan yeni hareket işlenmiş/bağlı olamaz.");

                var summary = BankImportOperationSummary(firmaId, hareket);
                var existing = await context.BankaKasaHareketleri.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(h => h.FirmaId == firmaId && h.IslemKimligi == key);
                if (existing != null)
                {
                    if (existing.IsDeleted || existing.IslemOzeti != summary)
                        throw new InvalidOperationException("İçe aktarma işlem kimliği daha önce farklı içerikle kullanılmış; yeni hareket oluşturulmadı.");
                    results.Add(existing);
                    continue;
                }

                hareket.IslemKimligi = key;
                hareket.IslemOzeti = summary;
                hareket.FirmaId = firmaId;
                hareket.IslemKaynak = IslemKaynak.Manuel;
                NormalizeHareket(hareket);
                await ValidateHareketAsync(context, hareket);
                await ApplyMuhasebeDefaultsAsync(context, hareket);
                hareket.BankaHesap = null!;
                hareket.Cari = null;
                context.BankaKasaHareketleri.Add(hareket);
                results.Add(hareket);
            }
            return (IReadOnlyList<BankaKasaHareket>)results;
        });
    }

    public async Task<BankaKasaHareket> UpdateAsync(BankaKasaHareket hareket)
    {
        return await WriteBankAsync(Yetkiler.BankaHareketleriDuzenle, async context =>
        {
            var existing = await QueryHareketler(context, asNoTracking: false)
                .FirstOrDefaultAsync(h => h.Id == hareket.Id);
            if (existing == null)
                throw new InvalidOperationException($"Banka/Kasa hareketi bulunamadı. Id: {hareket.Id}");

            if (hareket.UpdatedAt != existing.UpdatedAt)
                throw new InvalidOperationException("Hareket başka bir işlemde güncellendi; listeyi yenileyin.");
            if (hareket.IslemKimligi != existing.IslemKimligi || hareket.IslemOzeti != existing.IslemOzeti ||
                hareket.IsDeleted || hareket.IslemKaynak != existing.IslemKaynak ||
                hareket.MahsupGrupId != existing.MahsupGrupId || hareket.MahsupHareketId != existing.MahsupHareketId ||
                hareket.MuhasebeFisId != existing.MuhasebeFisId || hareket.PersonelCebindenId != existing.PersonelCebindenId ||
                hareket.PersoneleOdendi != existing.PersoneleOdendi || hareket.PersonelGeriOdemeHareketId != existing.PersonelGeriOdemeHareketId ||
                hareket.PersonelOdemeTarihi != existing.PersonelOdemeTarihi || hareket.PersonelOdemeHesapId != existing.PersonelOdemeHesapId ||
                hareket.AracMasrafId != existing.AracMasrafId)
                throw new InvalidOperationException("Kaynak, silinme ve bağlı işlem alanları genel düzenlemeden değiştirilemez.");
            await EnsureGeneralMutationAllowedAsync(context, existing);

            if (hareket.FirmaId.HasValue && hareket.FirmaId != existing.FirmaId)
                throw new UnauthorizedAccessException("Banka/Kasa hareketinin firma kapsamı değiştirilemez.");
            hareket.FirmaId = existing.FirmaId;
            NormalizeHareket(hareket);
            await ValidateHareketAsync(context, hareket);
            await ApplyMuhasebeDefaultsAsync(context, hareket);

            if (existing.IslemKaynak == IslemKaynak.RentACar)
            {
                if (hareket.IslemKaynak != IslemKaynak.RentACar || hareket.IslemNo != existing.IslemNo || hareket.IsDeleted)
                    throw new InvalidOperationException("Rent a Car hareketinin kaynak türü ve işlem numarası değiştirilemez.");
                if (hareket.HareketTipi != existing.HareketTipi)
                    throw new InvalidOperationException("Rent a Car tahsilat/iade yönü Banka/Kasa üzerinden değiştirilemez.");

                await RentOdemeHareketiniGuncelleAsync(context, existing, hareket);
                BankaHareketAlanlariniGuncelle(existing, hareket);
                return existing;
            }

            BankaHareketAlanlariniGuncelle(existing, hareket);
            return existing;
        });
    }

    private static async Task EnsureGeneralMutationAllowedAsync(ApplicationDbContext context, BankaKasaHareket hareket)
    {
        if (hareket.MahsupGrupId.HasValue || hareket.MahsupHareketId.HasValue || hareket.AracMasrafId.HasValue ||
            hareket.PersoneleOdendi || hareket.PersonelGeriOdemeHareketId.HasValue || hareket.IslemKaynak == IslemKaynak.PersonelGeriOdeme)
            throw new InvalidOperationException("Bağlı transfer/masraf/personel ödemesi kaynak işlemin akışından yönetilmelidir.");
        if (hareket.MuhasebeFisId.HasValue || await context.MuhasebeFisleri.AnyAsync(f => f.KaynakId == hareket.Id &&
            (f.KaynakTip == "BankaKasaHareket" || f.KaynakTip == "HesapTransfer" || f.KaynakTip == "CariMahsup")))
            throw new InvalidOperationException("Muhasebe fişine bağlı banka hareketi genel düzenleme/kaldırma üzerinden değiştirilemez.");
        if (await context.OdemeEslestirmeleri.AnyAsync(e => e.BankaKasaHareketId == hareket.Id) ||
            await context.BudgetOdemeler.AnyAsync(o => o.BankaKasaHareketId == hareket.Id) ||
            await context.BankaKasaHareketleri.AnyAsync(h => h.PersonelGeriOdemeHareketId == hareket.Id))
            throw new InvalidOperationException("Ödeme eşleştirmesi veya bağlı ödeme olan hareket kaynak işlemin akışından yönetilmelidir.");
    }

    private static void BankaHareketAlanlariniGuncelle(BankaKasaHareket existing, BankaKasaHareket hareket)
    {
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
        existing.UpdatedAt = DateTime.UtcNow;
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
        var activeFirmaId = _aktifFirmaProvider.AktifFirmaId;
        if (!_aktifFirmaProvider.TumFirmalar && activeFirmaId is not > 0)
            throw new InvalidOperationException("Banka/Kasa hareketi için önce firma seçilmelidir.");

        if (activeFirmaId is > 0)
        {
            if (!_aktifFirmaProvider.TumFirmalar && hareket.FirmaId.HasValue && hareket.FirmaId.Value != activeFirmaId.Value)
                throw new UnauthorizedAccessException("Banka/Kasa hareketi seçili firma dışında kaydedilemez.");
            hareket.FirmaId ??= activeFirmaId.Value;
        }

        if (hareket.FirmaId is not > 0)
            throw new InvalidOperationException("Banka/Kasa hareketinin firma kimliği zorunludur.");

        if (string.IsNullOrWhiteSpace(hareket.IslemNo))
            throw new InvalidOperationException("İşlem no zorunludur.");

        if (hareket.IslemTarihi == default)
            throw new InvalidOperationException("İşlem tarihi zorunludur.");

        if (hareket.BankaHesapId <= 0)
            throw new InvalidOperationException("Geçerli bir hesap seçiniz.");

        if (hareket.Tutar <= 0)
            throw new InvalidOperationException("Tutar sıfırdan büyük olmalıdır.");

        if (!Enum.IsDefined(typeof(HareketTipi), hareket.HareketTipi) || !Enum.IsDefined(typeof(IslemKaynak), hareket.IslemKaynak))
            throw new InvalidOperationException("Hareket yönü veya kaynak türü geçersiz.");

        var islemNoKullanimda = await QueryHareketler(context)
            .AnyAsync(h => h.Id != hareket.Id && h.IslemNo == hareket.IslemNo);

        if (islemNoKullanimda)
            throw new InvalidOperationException($"'{hareket.IslemNo}' işlem numarası zaten kullanımda.");

        var hesapVar = await context.BankaHesaplari
            .AnyAsync(h => h.Id == hareket.BankaHesapId && h.FirmaId == hareket.FirmaId && !h.IsDeleted && h.Aktif);
        if (!hesapVar)
            throw new InvalidOperationException("Seçilen hesap bulunamadı veya hareketle aynı firmaya ait değil.");

        if (hareket.CariId.HasValue)
        {
            var cariVar = await QueryCariler(context)
                .AnyAsync(c => c.Id == hareket.CariId.Value && c.FirmaId == hareket.FirmaId);

            if (!cariVar)
                throw new InvalidOperationException("Seçilen cari bulunamadı veya hareketle aynı firmaya ait değil.");
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
        await WriteBankAsync(Yetkiler.BankaHareketleriSil, async context =>
        {
            var hareket = await QueryHareketler(context, asNoTracking: false)
                .FirstOrDefaultAsync(h => h.Id == id);
            if (hareket == null)
                throw new InvalidOperationException("Banka hareketi aktif firma kapsamında bulunamadı.");
            await EnsureGeneralMutationAllowedAsync(context, hareket);
            if (hareket.IslemKaynak == IslemKaynak.RentACar)
                await RentOdemeHareketiniSilAsync(context, hareket);
            hareket.IsDeleted = true;
            hareket.DeletedAt = DateTime.UtcNow;
            hareket.UpdatedAt = hareket.DeletedAt;
            return hareket;
        });
    }

    private static async Task RentOdemeHareketiniGuncelleAsync(
        ApplicationDbContext context, BankaKasaHareket mevcutBankaHareketi, BankaKasaHareket yeniBankaHareketi)
    {
        var odeme = await BagliRentOdemeBulAsync(context, mevcutBankaHareketi)
            ?? throw new InvalidOperationException("Banka/Kasa hareketine bağlı Rent a Car ödeme kaydı bulunamadı.");

        if (yeniBankaHareketi.Tutar <= 0)
            throw new InvalidOperationException("Rent a Car ödeme tutarı sıfırdan büyük olmalıdır.");

        var hesap = await context.BankaHesaplari
            .FirstOrDefaultAsync(h => h.Id == yeniBankaHareketi.BankaHesapId && h.FirmaId == odeme.FirmaId)
            ?? throw new InvalidOperationException("Seçilen Banka/Kasa hesabı bulunamadı.");

        odeme.Tutar = decimal.Round(yeniBankaHareketi.Tutar, 2);
        odeme.IslemTarihi = yeniBankaHareketi.IslemTarihi;
        odeme.BelgeNo = yeniBankaHareketi.BelgeNo?.Trim();
        odeme.OdemeYontemi = hesap.HesapTipi switch
        {
            HesapTipi.Kasa => RentACarOdemeYontemi.Nakit,
            HesapTipi.KrediKarti => RentACarOdemeYontemi.KrediKarti,
            _ when odeme.OdemeYontemi is RentACarOdemeYontemi.BankaKarti or RentACarOdemeYontemi.Havale or RentACarOdemeYontemi.EFT
                => odeme.OdemeYontemi,
            _ => RentACarOdemeYontemi.Havale
        };

        if (!string.Equals(yeniBankaHareketi.Aciklama, mevcutBankaHareketi.Aciklama, StringComparison.Ordinal))
            odeme.Aciklama = RentAciklamasiniAyikla(yeniBankaHareketi.Aciklama, odeme);

        if (odeme.BelgeNo?.Length > 100 || odeme.Aciklama?.Length > 500)
            throw new InvalidOperationException("Rent a Car belge numarası en fazla 100, açıklaması en fazla 500 karakter olabilir.");

        odeme.UpdatedAt = DateTime.UtcNow;
        await KiralamaOdemeDurumunuYenileAsync(context, odeme.MusteriKiralamaId, odeme.FirmaId);
    }

    private static async Task RentOdemeHareketiniSilAsync(ApplicationDbContext context, BankaKasaHareket bankaHareketi)
    {
        var odeme = await BagliRentOdemeBulAsync(context, bankaHareketi);
        if (odeme is null)
            return;

        odeme.IsDeleted = true;
        odeme.DeletedAt = DateTime.UtcNow;
        odeme.UpdatedAt = DateTime.UtcNow;
        await KiralamaOdemeDurumunuYenileAsync(context, odeme.MusteriKiralamaId, odeme.FirmaId, odeme.Id);
    }

    private static async Task<RentACarOdemeHareketi?> BagliRentOdemeBulAsync(
        ApplicationDbContext context, BankaKasaHareket bankaHareketi)
    {
        if (!bankaHareketi.IslemNo.StartsWith("RAC-", StringComparison.OrdinalIgnoreCase)
            || !int.TryParse(bankaHareketi.IslemNo[4..], out var odemeId))
            return null;

        return await context.RentACarOdemeHareketleri
            .Include(o => o.MusteriKiralama)
            .FirstOrDefaultAsync(o =>
            o.Id == odemeId && o.FirmaId == bankaHareketi.FirmaId && !o.IsDeleted);
    }

    private static string? RentAciklamasiniAyikla(string? bankaAciklamasi, RentACarOdemeHareketi odeme)
    {
        if (string.IsNullOrWhiteSpace(bankaAciklamasi))
            return null;

        var aciklama = bankaAciklamasi.Trim();
        var onEk = $"Rent a Car {odeme.MusteriKiralama?.SozlesmeNo} — ";
        if (aciklama.StartsWith(onEk, StringComparison.OrdinalIgnoreCase))
        {
            var ayirici = aciklama.IndexOf(": ", onEk.Length, StringComparison.Ordinal);
            return ayirici < 0 ? null : aciklama[(ayirici + 2)..].Trim();
        }

        return aciklama;
    }

    private static async Task KiralamaOdemeDurumunuYenileAsync(
        ApplicationDbContext context, int kiralamaId, int firmaId, int? haricOdemeId = null)
    {
        var kiralama = await context.MusteriKiralamalar.FirstOrDefaultAsync(k =>
            k.Id == kiralamaId && k.FirmaId == firmaId && !k.IsDeleted)
            ?? throw new InvalidOperationException("Rent a Car kiralama kaydı bulunamadı.");

        var odemeler = await context.RentACarOdemeHareketleri
            .Where(o => o.MusteriKiralamaId == kiralamaId && o.FirmaId == firmaId && !o.IsDeleted)
            .ToListAsync();
        if (haricOdemeId.HasValue)
            odemeler.RemoveAll(o => o.Id == haricOdemeId.Value);

        var netKira = odemeler.Where(o => o.HareketTuru == RentACarOdemeHareketTuru.KiraTahsilati).Sum(o => o.Tutar)
            - odemeler.Where(o => o.HareketTuru == RentACarOdemeHareketTuru.KiraIadesi).Sum(o => o.Tutar);
        var netDepozito = odemeler.Where(o => o.HareketTuru == RentACarOdemeHareketTuru.DepozitoTahsilati).Sum(o => o.Tutar)
            - odemeler.Where(o => o.HareketTuru == RentACarOdemeHareketTuru.DepozitoIadesi).Sum(o => o.Tutar);

        if (netKira < 0 || netKira > kiralama.ToplamTutar || netDepozito < 0 || netDepozito > (kiralama.Depozito ?? 0))
            throw new InvalidOperationException("Bu değişiklik kira/depozito tahsilat ve iade dengesini bozuyor. Önce ilişkili iade hareketini düzenleyin veya silin.");

        kiralama.OdemeDurumu = netKira <= 0 ? KiralamaOdemeDurumu.Beklemede
            : netKira >= kiralama.ToplamTutar ? KiralamaOdemeDurumu.Odendi : KiralamaOdemeDurumu.KismiOdendi;
        kiralama.UpdatedAt = DateTime.UtcNow;
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
    public async Task<MahsupSonuc> HesaplarArasiTransferAsync(int kaynakHesapId, int hedefHesapId, decimal tutar, DateTime tarih, string aciklama, string? belgeNo = null, string? muhasebeHesapKodu = null, string? kostMerkeziKodu = null, string? projeKodu = null, string? islemKimligi = null)
    {
        await _permissionGuard.RequireAnyAsync(Yetkiler.BankaHareketleriYaz);
        if (kaynakHesapId == hedefHesapId || tutar <= 0)
            return new MahsupSonuc { Basarili = false, Hata = "Hesaplar farklı, tutar sıfırdan büyük olmalıdır." };
        var key = NormalizeBankOperationKey(islemKimligi);
        try
        {
            return await WriteBankAsync(Yetkiler.BankaHareketleriYaz, async context =>
            {
                RequireSelectedFinanceFirma(_aktifFirmaProvider.AktifFirmaId);
                var summary = BankOperationSummary("Transfer", _aktifFirmaProvider.AktifFirmaId!.Value,
                    kaynakHesapId, hedefHesapId, true, tutar, tarih, aciklama, belgeNo, muhasebeHesapKodu, kostMerkeziKodu, projeKodu);
                var previous = await FindBankOperationAsync(context, key, summary);
                if (previous != null) return previous;
                var kaynakHesap = await context.BankaHesaplari.FirstOrDefaultAsync(h => h.Id == kaynakHesapId && h.Aktif)
                    ?? throw new InvalidOperationException("Aktif kaynak hesap bulunamadı.");
                var hedefHesap = await context.BankaHesaplari.FirstOrDefaultAsync(h => h.Id == hedefHesapId && h.Aktif)
                    ?? throw new InvalidOperationException("Aktif hedef hesap bulunamadı.");
                RequireSelectedFinanceFirma(kaynakHesap.FirmaId);
                if (kaynakHesap.FirmaId != hedefHesap.FirmaId || kaynakHesap.ParaBirimi != hedefHesap.ParaBirimi)
                    throw new InvalidOperationException("Transfer hesapları aynı firma ve para biriminde olmalıdır.");
                var girisler = await QueryHareketler(context).Where(h => h.BankaHesapId == kaynakHesapId && h.HareketTipi == HareketTipi.Giris)
                    .SumAsync(h => (decimal?)h.Tutar) ?? 0;
                var cikislar = await QueryHareketler(context).Where(h => h.BankaHesapId == kaynakHesapId && h.HareketTipi == HareketTipi.Cikis)
                    .SumAsync(h => (decimal?)h.Tutar) ?? 0;
                if (kaynakHesap.AcilisBakiye + girisler - cikislar < tutar)
                    throw new InvalidOperationException("Transfer için yetersiz bakiye.");
                var mahsupGrupId = Guid.NewGuid();
                var islemNo = await GenerateNextIslemNoInContextAsync(context, kaynakHesap.FirmaId!.Value);
                // Kaynak hesaptan çıkış
                var cikisHareket = new BankaKasaHareket
                {
                    IslemKimligi = key,
                    IslemOzeti = summary,
                    IslemNo = islemNo,
                    IslemTarihi = tarih,
                    HareketTipi = HareketTipi.Cikis,
                    Tutar = tutar,
                    BankaHesapId = kaynakHesapId,
                    FirmaId = kaynakHesap.FirmaId,
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
                    IslemNo = await GenerateNextIslemNoInContextAsync(context, kaynakHesap.FirmaId!.Value),
                    IslemTarihi = tarih,
                    HareketTipi = HareketTipi.Giris,
                    Tutar = tutar,
                    BankaHesapId = hedefHesapId,
                    FirmaId = kaynakHesap.FirmaId,
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

                var fis = await _muhasebeService.CreateHesapTransferFisiAsync(cikisHareket, girisHareket, kaynakHesap, hedefHesap, context)
                    ?? throw new InvalidOperationException("Transfer için muhasebe hesap eşleştirmeleri eksik; işlem kaydedilmedi.");
                cikisHareket.MuhasebeFisId = fis.Id;
                girisHareket.MuhasebeFisId = fis.Id;
                return new MahsupSonuc { Basarili = true, MahsupGrupId = mahsupGrupId, KaynakHareket = cikisHareket, HedefHareket = girisHareket };
            });
        }
        catch (Exception ex)
        {
            return new MahsupSonuc { Basarili = false, Hata = ex.Message };
        }
    }

    public async Task<MahsupSonuc> CariMahsupAsync(int cariId, int hesapId, decimal tutar, DateTime tarih, string aciklama, bool caridenHesaba, string? belgeNo = null, string? muhasebeHesapKodu = null, string? kostMerkeziKodu = null, string? projeKodu = null, string? islemKimligi = null)
    {
        await _permissionGuard.RequireAnyAsync(Yetkiler.BankaHareketleriYaz);
        if (tutar <= 0)
            return new MahsupSonuc { Basarili = false, Hata = "Tutar sıfırdan büyük olmalıdır." };
        var key = NormalizeBankOperationKey(islemKimligi);
        try
        {
            return await WriteBankAsync(Yetkiler.BankaHareketleriYaz, async context =>
            {
                RequireSelectedFinanceFirma(_aktifFirmaProvider.AktifFirmaId);
                var summary = BankOperationSummary("CariMahsup", _aktifFirmaProvider.AktifFirmaId!.Value,
                    cariId, hesapId, caridenHesaba, tutar, tarih, aciklama, belgeNo, muhasebeHesapKodu, kostMerkeziKodu, projeKodu);
                var previous = await FindBankOperationAsync(context, key, summary);
                if (previous != null) return previous;
                var hesap = await context.BankaHesaplari.FirstOrDefaultAsync(h => h.Id == hesapId && h.Aktif)
                    ?? throw new InvalidOperationException("Aktif hesap bulunamadı.");
                var cari = await QueryCariler(context).FirstOrDefaultAsync(c => c.Id == cariId)
                    ?? throw new InvalidOperationException("Cari bulunamadı.");
                RequireSelectedFinanceFirma(hesap.FirmaId);
                if (hesap.FirmaId != cari.FirmaId)
                    throw new InvalidOperationException("Cari ve hesap aynı firmaya ait olmalıdır.");
                var mahsupGrupId = Guid.NewGuid();
                var islemNo = await GenerateNextIslemNoInContextAsync(context, hesap.FirmaId!.Value);
                // caridenHesaba = true: Cari bize borçlu, biz tahsil ediyoruz (Hesaba Giriş)
                // caridenHesaba = false: Biz cariye borçluyuz, ödeme yapıyoruz (Hesaptan Çıkış)
                var hareket = new BankaKasaHareket
                {
                    IslemKimligi = key,
                    IslemOzeti = summary,
                    IslemNo = islemNo,
                    IslemTarihi = tarih,
                    HareketTipi = caridenHesaba ? HareketTipi.Giris : HareketTipi.Cikis,
                    Tutar = tutar,
                    BankaHesapId = hesapId,
                    FirmaId = hesap.FirmaId,
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

                var fis = await _muhasebeService.CreateCariMahsupFisiAsync(hareket, cari, hesap, caridenHesaba, context)
                    ?? throw new InvalidOperationException("Cari mahsup için muhasebe hesap eşleştirmeleri eksik; işlem kaydedilmedi.");
                hareket.MuhasebeFisId = fis.Id;
                return new MahsupSonuc { Basarili = true, MahsupGrupId = mahsupGrupId, KaynakHareket = hareket };
            });
        }
        catch (Exception ex)
        {
            return new MahsupSonuc { Basarili = false, Hata = ex.Message };
        }
    }

    private static string NormalizeBankOperationKey(string? value)
    {
        if (!Guid.TryParse(value, out var key) || key == Guid.Empty)
            throw new InvalidOperationException("Geçerli işlem kimliği zorunludur.");
        return key.ToString("N");
    }

    private static string BankOperationSummary(string operation, int firma, int first, int second, bool direction,
        decimal amount, DateTime date, params string?[] texts)
    {
        var payload = JsonSerializer.Serialize(new
        {
            operation, firma, first, second, direction,
            amount = amount.ToString("G29", CultureInfo.InvariantCulture),
            date = DateTime.SpecifyKind(date, DateTimeKind.Utc).ToString("O", CultureInfo.InvariantCulture),
            texts = texts.Select(t => t?.Trim() ?? "").ToArray()
        });
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
    }

    private static string BankImportOperationSummary(int firma, BankaKasaHareket movement)
    {
        var payload = JsonSerializer.Serialize(new
        {
            operation = "BankaImport",
            firma,
            movement.BankaHesapId,
            date = DateTime.SpecifyKind(movement.IslemTarihi, DateTimeKind.Utc).ToString("O", CultureInfo.InvariantCulture),
            movement.HareketTipi,
            amount = movement.Tutar.ToString("G29", CultureInfo.InvariantCulture),
            movement.CariId,
            movement.AracId,
            movement.MuhasebeHesapKodu,
            movement.KostMerkeziKodu,
            texts = new[] { movement.Aciklama?.Trim() ?? "", movement.BelgeNo?.Trim() ?? "" }
        });
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
    }

    private async Task<MahsupSonuc?> FindBankOperationAsync(ApplicationDbContext context, string key, string summary)
    {
        var original = await context.BankaKasaHareketleri.IgnoreQueryFilters().FirstOrDefaultAsync(h =>
            h.FirmaId == _aktifFirmaProvider.AktifFirmaId && h.IslemKimligi == key);
        if (original == null) return null;
        if (original.IsDeleted || original.IslemOzeti != summary || !original.MahsupGrupId.HasValue || !original.MuhasebeFisId.HasValue)
            throw new InvalidOperationException("İşlem kimliği iptal edilmiş veya farklı içerikle kullanılmış; yeni kayıt oluşturulmadı.");
        var group = await context.BankaKasaHareketleri.IgnoreQueryFilters().Where(h =>
            h.FirmaId == original.FirmaId && h.MahsupGrupId == original.MahsupGrupId).ToListAsync();
        var transfer = original.IslemKaynak == IslemKaynak.Mahsup;
        if (group.Count != (transfer ? 2 : 1) || group.Any(h => h.IsDeleted || h.MuhasebeFisId != original.MuhasebeFisId) ||
            !await context.MuhasebeFisleri.AnyAsync(f => f.Id == original.MuhasebeFisId && f.Durum == FisDurum.Onaylandi) ||
            await context.MuhasebeFisleri.AnyAsync(f => f.KaynakTip == "IptalKaydi" && f.KaynakId == original.MuhasebeFisId))
            throw new InvalidOperationException("Önceki işlemin fiş/hareket bağlantısı tutarsız; yeniden kayıt oluşturulmadı.");
        var target = transfer ? group.Single(h => h.Id != original.Id) : null;
        if (target != null && (original.MahsupHareketId != target.Id || target.MahsupHareketId != original.Id ||
            original.Tutar != target.Tutar || original.HareketTipi == target.HareketTipi))
            throw new InvalidOperationException("Önceki transferin karşı hareket bağlantısı tutarsız.");
        return new MahsupSonuc { Basarili = true, MahsupGrupId = original.MahsupGrupId,
            KaynakHareket = original, HedefHareket = target };
    }

    private void RequireSelectedFinanceFirma(int? firmaId)
    {
        if (_aktifFirmaProvider.TumFirmalar || firmaId is not > 0 || firmaId != _aktifFirmaProvider.AktifFirmaId)
            throw new UnauthorizedAccessException("Mali işlem yalnız tek seçili firmada yürütülebilir.");
    }

    private static async Task<string> GenerateNextIslemNoInContextAsync(ApplicationDbContext context, int firmaId)
    {
        var yilAy = DateTime.UtcNow.ToString("yyyyMM");
        var number = await MuhasebeService.NextFisNoCounterAsync(context, IslemNoPrefix, yilAy, firmaId);
        return $"{IslemNoPrefix}-{yilAy}-{number:D4}";
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
        if (mahsupGrupId == Guid.Empty)
            throw new InvalidOperationException("Geçerli mahsup grubu seçilmelidir.");
        await WriteBankAsync(Yetkiler.BankaHareketleriSil, async context =>
        {
            var hareketler = await QueryHareketler(context, asNoTracking: false)
                .Where(h => h.MahsupGrupId == mahsupGrupId).ToListAsync();
            if (hareketler.Count == 0)
                throw new InvalidOperationException("Aktif mahsup bulunamadı; işlem daha önce iptal edilmiş olabilir.");
            foreach (var hareket in hareketler) RequireSelectedFinanceFirma(hareket.FirmaId);
            var ids = hareketler.Select(h => h.Id).ToArray();
            var transfer = hareketler.All(h => h.IslemKaynak == IslemKaynak.Mahsup);
            var cari = hareketler.Count == 1 && hareketler[0].IslemKaynak == IslemKaynak.CariMahsup;
            if (!(transfer && hareketler.Count == 2 &&
                  hareketler[0].MahsupHareketId == hareketler[1].Id &&
                  hareketler[1].MahsupHareketId == hareketler[0].Id &&
                  hareketler[0].Tutar == hareketler[1].Tutar &&
                  hareketler[0].HareketTipi != hareketler[1].HareketTipi) && !cari)
                throw new InvalidOperationException("Mahsup bağlantıları eksik veya kaynak türü geçersiz; iptal durduruldu.");
            if (hareketler.Any(h => h.AracMasrafId.HasValue || h.PersoneleOdendi || h.PersonelGeriOdemeHareketId.HasValue) ||
                await context.OdemeEslestirmeleri.AnyAsync(e => ids.Contains(e.BankaKasaHareketId)) ||
                await context.BudgetOdemeler.AnyAsync(o => o.BankaKasaHareketId.HasValue && ids.Contains(o.BankaKasaHareketId.Value)) ||
                await context.BankaKasaHareketleri.AnyAsync(h => h.PersonelGeriOdemeHareketId.HasValue && ids.Contains(h.PersonelGeriOdemeHareketId.Value)))
                throw new InvalidOperationException("Bağlı ödemeler kaynak akıştan kaldırılmadan mahsup iptal edilemez.");
            await _muhasebeService.IptalFisiOlusturAsync(mahsupGrupId, context);
            var now = DateTime.UtcNow;
            foreach (var hareket in hareketler)
            {
                hareket.IsDeleted = true;
                hareket.DeletedAt = now;
                hareket.UpdatedAt = now;
            }
            return mahsupGrupId;
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
        await _permissionGuard.RequireAnyAsync(Yetkiler.BankaHareketleriYaz);
        var ids = cebindenHareketIds?.Distinct().ToArray() ?? [];
        if (personelId <= 0 || ids.Length == 0 || ids.Any(id => id <= 0) || odemeTarihi == default || hesapId is <= 0)
            return new PersonelGeriOdemeSonuc { Basarili = false, Hata = "Personel, hareketler, ödeme tarihi ve varsa hesap geçerli olmalıdır." };
        try
        {
            return await WriteBankAsync(Yetkiler.BankaHareketleriYaz, async context =>
            {
                var personel = await context.Soforler.AsNoTracking().FirstOrDefaultAsync(p => p.Id == personelId)
                    ?? throw new InvalidOperationException("Seçili firmanın personeli bulunamadı.");
                RequireSelectedFinanceFirma(personel.FirmaId);
                var expenses = await QueryHareketler(context, asNoTracking: false)
                    .Where(h => ids.Contains(h.Id)).ToListAsync();
                if (expenses.Count != ids.Length || expenses.Any(h => h.PersonelCebindenId != personelId ||
                    h.FirmaId != personel.FirmaId || h.PersoneleOdendi || h.PersonelGeriOdemeHareketId.HasValue ||
                    h.PersonelOdemeTarihi.HasValue || h.PersonelOdemeHesapId.HasValue || h.Tutar <= 0 ||
                    h.HareketTipi != HareketTipi.Cikis || h.MahsupGrupId.HasValue || h.MahsupHareketId.HasValue))
                    throw new InvalidOperationException("Seçilen kayıtların tamamı aynı personelin ödenmemiş, geçerli masrafları olmalıdır; ödeme kaydedilmedi.");
                var total = expenses.Sum(h => h.Tutar);
                BankaKasaHareket? payment = null;
                if (hesapId.HasValue)
                {
                    var account = await context.BankaHesaplari.FirstOrDefaultAsync(h => h.Id == hesapId && h.Aktif && h.FirmaId == personel.FirmaId)
                        ?? throw new InvalidOperationException("Seçili firmanın aktif ödeme hesabı bulunamadı.");
                    var expenseAccounts = expenses.Select(h => h.BankaHesapId).Distinct().ToArray();
                    if (await context.BankaHesaplari.Where(h => expenseAccounts.Contains(h.Id)).CountAsync() != expenseAccounts.Length ||
                        await context.BankaHesaplari.AnyAsync(h => expenseAccounts.Contains(h.Id) && h.ParaBirimi != account.ParaBirimi))
                        throw new InvalidOperationException("Masraf ve ödeme hesaplarının para birimleri eşleşmelidir.");
                    payment = new BankaKasaHareket
                    {
                        FirmaId = personel.FirmaId,
                        IslemNo = await GenerateNextIslemNoInContextAsync(context, personel.FirmaId!.Value),
                        IslemTarihi = odemeTarihi, HareketTipi = HareketTipi.Cikis, Tutar = total,
                        BankaHesapId = account.Id, IslemKaynak = IslemKaynak.PersonelGeriOdeme,
                        Aciklama = $"[PERSONEL GERİ ÖDEME] {personel.Ad} {personel.Soyad} - " +
                            (string.IsNullOrWhiteSpace(aciklama) ? $"{expenses.Count} kayıt" : aciklama.Trim()),
                        MuhasebeHesapKodu = account.VarsayilanMuhasebeKodu,
                        KostMerkeziKodu = account.VarsayilanKostMerkezi
                    };
                    NormalizeHareket(payment);
                    await ValidateHareketAsync(context, payment);
                    context.BankaKasaHareketleri.Add(payment);
                    await context.SaveChangesAsync();
                }
                foreach (var expense in expenses)
                {
                    expense.PersoneleOdendi = true;
                    expense.PersonelOdemeTarihi = odemeTarihi;
                    expense.PersonelOdemeHesapId = hesapId;
                    expense.PersonelGeriOdemeHareketId = payment?.Id;
                    expense.UpdatedAt = DateTime.UtcNow;
                }
                return new PersonelGeriOdemeSonuc { Basarili = true, OdemeHareketi = payment,
                    KapatilanKayitSayisi = expenses.Count, ToplamTutar = total };
            });
        }
        catch (Exception ex)
        {
            return new PersonelGeriOdemeSonuc { Basarili = false, Hata = ex.Message };
        }
    }

    public async Task PersonelGeriOdemeIptalAsync(int cebindenHareketId)
    {
        await WriteBankAsync(Yetkiler.BankaHareketleriSil, async context =>
        {
            var expense = await QueryHareketler(context, asNoTracking: false).FirstOrDefaultAsync(h => h.Id == cebindenHareketId)
                ?? throw new InvalidOperationException("Aktif personel masrafı bulunamadı.");
            RequireSelectedFinanceFirma(expense.FirmaId);
            if (!expense.PersonelCebindenId.HasValue || !expense.PersoneleOdendi)
                throw new InvalidOperationException("Ödenmiş personel masrafı seçilmelidir; ödeme daha önce iptal edilmiş olabilir.");
            var expenses = new List<BankaKasaHareket> { expense };
            if (expense.PersonelGeriOdemeHareketId is int paymentId)
            {
                var payment = await QueryHareketler(context, asNoTracking: false).FirstOrDefaultAsync(h => h.Id == paymentId)
                    ?? throw new InvalidOperationException("Bağlı ödeme hareketi bulunamadı; veri bağlantısı onarılmalıdır.");
                expenses = await QueryHareketler(context, asNoTracking: false)
                    .Where(h => h.PersonelGeriOdemeHareketId == paymentId).ToListAsync();
                // Detect hidden/deleted/cross-firm references, without mutating those rows.
                var referenceCount = await context.BankaKasaHareketleri.IgnoreQueryFilters()
                    .CountAsync(h => h.PersonelGeriOdemeHareketId == paymentId);
                if (referenceCount != expenses.Count || expenses.Any(h => !h.PersoneleOdendi || h.Tutar <= 0 ||
                    h.PersonelCebindenId != expense.PersonelCebindenId || h.FirmaId != expense.FirmaId ||
                    h.PersonelOdemeHesapId != payment.BankaHesapId) ||
                    payment.FirmaId != expense.FirmaId || payment.IslemKaynak != IslemKaynak.PersonelGeriOdeme ||
                    payment.HareketTipi != HareketTipi.Cikis || payment.Tutar != expenses.Sum(h => h.Tutar))
                    throw new InvalidOperationException("Ortak ödeme bağlantıları veya toplamı tutarsız; iptal durduruldu.");
                if (payment.MuhasebeFisId.HasValue || payment.MahsupGrupId.HasValue || payment.MahsupHareketId.HasValue ||
                    await context.MuhasebeFisleri.IgnoreQueryFilters().AnyAsync(f => f.KaynakId == paymentId && f.KaynakTip == "BankaKasaHareket") ||
                    await context.OdemeEslestirmeleri.IgnoreQueryFilters().AnyAsync(e => e.BankaKasaHareketId == paymentId) ||
                    await context.BudgetOdemeler.IgnoreQueryFilters().AnyAsync(o => o.BankaKasaHareketId == paymentId))
                    throw new InvalidOperationException("Fişe veya başka ödemeye bağlı geri ödeme kaynak akışından çözülmeden iptal edilemez.");
                payment.IsDeleted = true;
                payment.DeletedAt = DateTime.UtcNow;
                payment.UpdatedAt = DateTime.UtcNow;
            }
            else if (expense.PersonelOdemeHesapId.HasValue)
                throw new InvalidOperationException("Ödeme hesabı var ancak ödeme hareketi eksik; veri bağlantısı onarılmalıdır.");
            foreach (var item in expenses)
            {
                item.PersoneleOdendi = false;
                item.PersonelOdemeTarihi = null;
                item.PersonelOdemeHesapId = null;
                item.PersonelGeriOdemeHareketId = null;
                item.UpdatedAt = DateTime.UtcNow;
            }
            return cebindenHareketId;
        });
    }

}
