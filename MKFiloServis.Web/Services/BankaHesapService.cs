using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Web.Services;

public class BankaHesapService : IBankaHesapService
{
    private const string HesapKodPrefix = "HSP-";
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly NumaraSerisiService _numaraSerisi;
    private readonly CurrentPermissionGuard _permissionGuard;
    private readonly IAktifFirmaProvider _aktifFirmaProvider;

    public BankaHesapService(IDbContextFactory<ApplicationDbContext> contextFactory, NumaraSerisiService numaraSerisi, CurrentPermissionGuard permissionGuard, IAktifFirmaProvider aktifFirmaProvider)
    {
        _contextFactory = contextFactory;
        _numaraSerisi = numaraSerisi;
        _permissionGuard = permissionGuard;
        _aktifFirmaProvider = aktifFirmaProvider;
    }

    private async Task<T> WriteAccountAsync<T>(string permission, Func<ApplicationDbContext, Task<T>> write)
    {
        await using var strategyContext = await _contextFactory.CreateDbContextAsync();
        var strategy = strategyContext.Database.CreateExecutionStrategy();
        var commitStarted = false;
        return await strategy.ExecuteAsync(async () =>
        {
            if (commitStarted)
                throw new InvalidOperationException("Hesap işleminin commit sonucu belirsiz; listeyi yenileyip kaydı kontrol edin.");
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

    public async Task<List<BankaHesap>> GetAllAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await QueryBankaHesaplari(context)
            .OrderBy(b => b.HesapAdi)
            .ToListAsync();
    }

    public async Task<List<BankaHesap>> GetActiveAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await QueryBankaHesaplari(context)
            .Where(b => b.Aktif)
            .OrderBy(b => b.HesapAdi)
            .ToListAsync();
    }

    public async Task<List<BankaHesap>> GetByTipAsync(HesapTipi tip)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await QueryBankaHesaplari(context)
            .Where(b => b.HesapTipi == tip && b.Aktif)
            .OrderBy(b => b.HesapAdi)
            .ToListAsync();
    }

    public async Task<BankaHesap?> GetByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await QueryBankaHesaplari(context)
            .FirstOrDefaultAsync(b => b.Id == id);
    }

    public async Task<BankaHesap> CreateAsync(BankaHesap bankaHesap)
    {
        await _permissionGuard.RequireAnyAsync(Yetkiler.BankaHesaplariYaz);
        return await WriteAccountAsync(Yetkiler.BankaHesaplariYaz, async context =>
        {
            if (bankaHesap.Id != 0 || bankaHesap.IsDeleted)
                throw new InvalidOperationException("Yeni hesap aktif ve kimliksiz olmalıdır.");
            var firmaId = _aktifFirmaProvider.AktifFirmaId;
            if (_aktifFirmaProvider.TumFirmalar || firmaId is not > 0 ||
                (bankaHesap.FirmaId.HasValue && bankaHesap.FirmaId != firmaId))
                throw new UnauthorizedAccessException("Banka hesabı yalnız seçili firmada oluşturulabilir.");
            bankaHesap.FirmaId = firmaId;
            NormalizeBankaHesap(bankaHesap);
            await ValidateBankaHesapAsync(context, bankaHesap);

            context.BankaHesaplari.Add(bankaHesap);
            return bankaHesap;
        });
    }

    public async Task<BankaHesap> UpdateAsync(BankaHesap bankaHesap)
    {
        await _permissionGuard.RequireAnyAsync(Yetkiler.BankaHesaplariDuzenle);
        return await WriteAccountAsync(Yetkiler.BankaHesaplariDuzenle, async context =>
        {
            var existing = await QueryBankaHesaplari(context, asNoTracking: false)
                .FirstOrDefaultAsync(b => b.Id == bankaHesap.Id);

            if (existing == null)
                throw new InvalidOperationException($"Banka hesabı bulunamadı. Id: {bankaHesap.Id}");

            if (bankaHesap.IsDeleted || bankaHesap.FirmaId != existing.FirmaId)
                throw new InvalidOperationException("Hesabın firma/silinme bilgisi düzenlenemez.");
            if (bankaHesap.UpdatedAt != existing.UpdatedAt)
                throw new InvalidOperationException("Hesap başka bir işlemde güncellendi; listeyi yenileyin.");
            if ((existing.ParaBirimi != bankaHesap.ParaBirimi || existing.HesapTipi != bankaHesap.HesapTipi || existing.AcilisBakiye != bankaHesap.AcilisBakiye) &&
                await context.BankaKasaHareketleri.IgnoreQueryFilters().AnyAsync(h => h.BankaHesapId == existing.Id))
                throw new InvalidOperationException("Hareket geçmişi bulunan hesabın para birimi, tipi ve açılış bakiyesi doğrudan değiştirilemez.");

            NormalizeBankaHesap(bankaHesap);
            await ValidateBankaHesapAsync(context, bankaHesap);

            existing.HesapKodu = bankaHesap.HesapKodu;
            existing.HesapAdi = bankaHesap.HesapAdi;
            existing.HesapTipi = bankaHesap.HesapTipi;
            existing.BankaAdi = bankaHesap.BankaAdi;
            existing.SubeAdi = bankaHesap.SubeAdi;
            existing.SubeKodu = bankaHesap.SubeKodu;
            existing.HesapNo = bankaHesap.HesapNo;
            existing.Iban = bankaHesap.Iban;
            existing.ParaBirimi = bankaHesap.ParaBirimi;
            existing.AcilisBakiye = bankaHesap.AcilisBakiye;
            existing.Aktif = bankaHesap.Aktif;
            existing.Notlar = bankaHesap.Notlar;
            existing.KrediTaksitGrupId = bankaHesap.KrediTaksitGrupId;
            existing.VarsayilanMuhasebeKodu = bankaHesap.VarsayilanMuhasebeKodu;
            existing.VarsayilanKostMerkezi = bankaHesap.VarsayilanKostMerkezi;
            existing.UpdatedAt = DateTime.UtcNow;

            return existing;
        });
    }

    public async Task DeleteAsync(int id)
    {
        await _permissionGuard.RequireAnyAsync(Yetkiler.BankaHesaplariSil);
        await WriteAccountAsync(Yetkiler.BankaHesaplariSil, async context =>
        {
            var bankaHesap = await QueryBankaHesaplari(context, asNoTracking: false)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (bankaHesap != null)
            {
                bankaHesap.DeletedAt = DateTime.UtcNow;
                bankaHesap.IsDeleted = true;
                bankaHesap.Aktif = false;
                bankaHesap.UpdatedAt = DateTime.UtcNow;
            }
            return 0;
        });
    }

    public async Task<string> GenerateNextKodAsync()
    {
        var nextNumber = await _numaraSerisi.GenerateNextAsync("HSP", 0, "GLOBAL");
        return $"HSP-{nextNumber:D4}";
    }

    public async Task<decimal> GetBakiyeAsync(int hesapId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var hesap = await QueryBankaHesaplari(context)
            .Where(h => h.Id == hesapId)
            .Select(h => new { h.AcilisBakiye })
            .FirstOrDefaultAsync();

        if (hesap == null) return 0;

        var girisler = await QueryBankaKasaHareketleri(context)
            .Where(h => h.BankaHesapId == hesapId && h.HareketTipi == HareketTipi.Giris)
            .SumAsync(h => h.Tutar);

        var cikislar = await QueryBankaKasaHareketleri(context)
            .Where(h => h.BankaHesapId == hesapId && h.HareketTipi == HareketTipi.Cikis)
            .SumAsync(h => h.Tutar);

        return hesap.AcilisBakiye + girisler - cikislar;
    }

    public async Task<Dictionary<int, decimal>> GetTumHesapBakiyeleriAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var hesaplar = await QueryBankaHesaplari(context)
            .Where(h => h.Aktif)
            .Select(h => new
            {
                h.Id,
                h.AcilisBakiye,
                Girisler = QueryBankaKasaHareketleri(context)
                    .Where(hr => hr.BankaHesapId == h.Id && hr.HareketTipi == HareketTipi.Giris)
                    .Sum(hr => (decimal?)hr.Tutar) ?? 0,
                Cikislar = QueryBankaKasaHareketleri(context)
                    .Where(hr => hr.BankaHesapId == h.Id && hr.HareketTipi == HareketTipi.Cikis)
                    .Sum(hr => (decimal?)hr.Tutar) ?? 0
            })
            .ToListAsync();

        return hesaplar.ToDictionary(h => h.Id, h => h.AcilisBakiye + h.Girisler - h.Cikislar);
    }

    public async Task<List<BankaHesap>> GetFirmasizHesaplarAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.BankaHesaplari
            .IgnoreQueryFilters()
            .Where(b => !b.IsDeleted && b.FirmaId == null)
            .AsNoTracking()
            .OrderBy(b => b.HesapAdi)
            .ToListAsync();
    }

    public async Task AssignFirmaAsync(int hesapId, int firmaId)
    {
        await _permissionGuard.RequireAnyAsync(Yetkiler.BankaHesaplariDuzenle);
        if (firmaId <= 0)
            throw new ArgumentException("Geçerli bir firma seçilmedi.", nameof(firmaId));
        if (_aktifFirmaProvider.TumFirmalar || _aktifFirmaProvider.AktifFirmaId != firmaId)
            throw new UnauthorizedAccessException("Firma ataması yalnız seçili firmaya yapılabilir.");

        await WriteAccountAsync(Yetkiler.BankaHesaplariDuzenle, async context =>
        {

            var firmaVar = await context.Firmalar.IgnoreQueryFilters()
                .AnyAsync(f => f.Id == firmaId && !f.IsDeleted);
            if (!firmaVar)
                throw new InvalidOperationException($"Id={firmaId} olan firma bulunamadı.");

            var hesap = await context.BankaHesaplari
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(b => b.Id == hesapId && !b.IsDeleted);

            if (hesap == null)
                throw new InvalidOperationException("Banka/Kasa hesabı bulunamadı.");
            if (hesap.FirmaId.HasValue)
                throw new InvalidOperationException("Firması bulunan hesap bu akıştan başka firmaya taşınamaz.");
            if (await context.BankaKasaHareketleri.IgnoreQueryFilters().AnyAsync(h => h.BankaHesapId == hesap.Id && h.FirmaId != firmaId))
                throw new InvalidOperationException("Hesabın hareket firmaları hedef firmayla uyuşmuyor; veri onarım akışını kullanın.");

            hesap.FirmaId = firmaId;
            hesap.UpdatedAt = DateTime.UtcNow;
            return 0;
        });
    }

    public async Task<int> GetFirmaIdYokSayisiAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.BankaHesaplari
            .IgnoreQueryFilters()
            .CountAsync(b => !b.IsDeleted && b.FirmaId == null);
    }

    private IQueryable<BankaHesap> QueryBankaHesaplari(ApplicationDbContext context, bool asNoTracking = true)
    {
        var query = context.BankaHesaplari
            .Where(b => !b.IsDeleted);

        return asNoTracking ? query.AsNoTracking() : query;
    }

    private IQueryable<BankaKasaHareket> QueryBankaKasaHareketleri(ApplicationDbContext context)
    {
        return context.BankaKasaHareketleri
            .Where(h => !h.IsDeleted);
    }

    private async Task ValidateBankaHesapAsync(ApplicationDbContext context, BankaHesap bankaHesap)
    {
        if (string.IsNullOrWhiteSpace(bankaHesap.HesapKodu))
            throw new InvalidOperationException("Hesap kodu zorunludur.");

        if (string.IsNullOrWhiteSpace(bankaHesap.HesapAdi))
            throw new InvalidOperationException("Hesap adı zorunludur.");

        var hesapKoduVar = await QueryBankaHesaplari(context)
            .AnyAsync(b => b.Id != bankaHesap.Id && b.HesapKodu == bankaHesap.HesapKodu);

        if (hesapKoduVar)
            throw new InvalidOperationException($"'{bankaHesap.HesapKodu}' hesap kodu zaten kullanımda.");

    }

    private static void NormalizeBankaHesap(BankaHesap bankaHesap)
    {
        bankaHesap.HesapKodu = bankaHesap.HesapKodu?.Trim().ToUpperInvariant();
        bankaHesap.HesapAdi = bankaHesap.HesapAdi.Trim();
        bankaHesap.BankaAdi = NormalizeNullableText(bankaHesap.BankaAdi);
        bankaHesap.SubeAdi = NormalizeNullableText(bankaHesap.SubeAdi);
        bankaHesap.SubeKodu = NormalizeNullableText(bankaHesap.SubeKodu);
        bankaHesap.HesapNo = NormalizeNullableText(bankaHesap.HesapNo);
        bankaHesap.Iban = NormalizeIban(bankaHesap.Iban);
        bankaHesap.ParaBirimi = string.IsNullOrWhiteSpace(bankaHesap.ParaBirimi)
            ? "TRY"
            : bankaHesap.ParaBirimi.Trim().ToUpperInvariant();
        bankaHesap.Notlar = NormalizeNullableText(bankaHesap.Notlar);
        bankaHesap.VarsayilanMuhasebeKodu = NormalizeNullableText(bankaHesap.VarsayilanMuhasebeKodu);
        bankaHesap.VarsayilanKostMerkezi = NormalizeNullableText(bankaHesap.VarsayilanKostMerkezi);
    }

    private static string? NormalizeNullableText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string? NormalizeIban(string? iban)
    {
        if (string.IsNullOrWhiteSpace(iban))
            return null;

        return new string(iban.Where(ch => !char.IsWhiteSpace(ch)).ToArray()).ToUpperInvariant();
    }

}



