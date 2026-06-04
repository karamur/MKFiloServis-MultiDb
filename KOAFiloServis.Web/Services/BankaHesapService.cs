using KOAFiloServis.Shared.Entities;
using KOAFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace KOAFiloServis.Web.Services;

public class BankaHesapService : IBankaHesapService
{
    private const string HesapKodPrefix = "HSP-";
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly NumaraSerisiService _numaraSerisi;

    public BankaHesapService(IDbContextFactory<ApplicationDbContext> contextFactory, NumaraSerisiService numaraSerisi)
    {
        _contextFactory = contextFactory;
        _numaraSerisi = numaraSerisi;
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
        await using var context = await _contextFactory.CreateDbContextAsync();
        NormalizeBankaHesap(bankaHesap);
        await ValidateBankaHesapAsync(context, bankaHesap);

        context.BankaHesaplari.Add(bankaHesap);
        await context.SaveChangesAsync();
        return bankaHesap;
    }

    public async Task<BankaHesap> UpdateAsync(BankaHesap bankaHesap)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var existing = await QueryBankaHesaplari(context, asNoTracking: false)
            .FirstOrDefaultAsync(b => b.Id == bankaHesap.Id);

        if (existing == null)
            throw new InvalidOperationException($"Banka hesabı bulunamadı. Id: {bankaHesap.Id}");

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
        existing.IsDeleted = bankaHesap.IsDeleted;
        existing.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
        return existing;
    }

    public async Task DeleteAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var bankaHesap = await QueryBankaHesaplari(context, asNoTracking: false)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (bankaHesap != null)
        {
            bankaHesap.IsDeleted = true;
            bankaHesap.Aktif = false;
            bankaHesap.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }
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
        if (firmaId <= 0)
            throw new ArgumentException("Geçerli bir firma seçilmedi.", nameof(firmaId));

        await using var context = await _contextFactory.CreateDbContextAsync();

        var firmaVar = await context.Firmalar.IgnoreQueryFilters()
            .AnyAsync(f => f.Id == firmaId && !f.IsDeleted);
        if (!firmaVar)
            throw new InvalidOperationException($"Id={firmaId} olan firma bulunamadı.");

        var hesap = await context.BankaHesaplari
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(b => b.Id == hesapId && !b.IsDeleted);

        if (hesap == null)
            throw new InvalidOperationException("Banka/Kasa hesabı bulunamadı.");

        hesap.FirmaId = firmaId;
        hesap.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
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
        bankaHesap.HesapKodu = bankaHesap.HesapKodu.Trim().ToUpperInvariant();
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

    private static int? TryParseGeneratedKodNumber(string? hesapKodu)
    {
        if (string.IsNullOrWhiteSpace(hesapKodu))
            return null;

        var normalizedKod = hesapKodu.Trim().ToUpperInvariant();
        if (!normalizedKod.StartsWith(HesapKodPrefix, StringComparison.Ordinal))
            return null;

        var numberPart = normalizedKod[HesapKodPrefix.Length..];
        return int.TryParse(numberPart, out var number) ? number : null;
    }
}
