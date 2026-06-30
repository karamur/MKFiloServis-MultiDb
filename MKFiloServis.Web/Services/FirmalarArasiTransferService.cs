using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Web.Services;

public interface IFirmalarArasiTransferService
{
    Task<List<FirmalarArasiTransfer>> ListeleAsync(int? firmaId = null);
    Task<FirmalarArasiTransfer> CreateAsync(FirmalarArasiTransfer transfer);
    Task<FirmalarArasiTransfer?> GetByIdAsync(int id);
    Task IptalEtAsync(int id);
}

/// <summary>
/// Şirketler arası kasa/banka transferi servisi. (Karar K6)
/// <para>
/// Tek bir transfer kaydından iki <see cref="BankaKasaHareket"/> üretir:
/// kaynakta çıkış (FirmaId = KaynakFirmaId), hedefte giriş (FirmaId = HedefFirmaId).
/// Hareketlere atanacak <c>FirmaId</c> elle set edilir; bu sayede SaveChanges'in
/// otomatik tenant ataması (aktif firma) hedef hareketi yanlış firmaya iliştirmez.
/// </para>
/// </summary>
public sealed class FirmalarArasiTransferService : IFirmalarArasiTransferService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IAktifFirmaProvider _firmaProvider;

    public FirmalarArasiTransferService(IDbContextFactory<ApplicationDbContext> contextFactory, IAktifFirmaProvider firmaProvider)
    {
        _contextFactory = contextFactory;
        _firmaProvider = firmaProvider;
    }

    public async Task<List<FirmalarArasiTransfer>> ListeleAsync(int? firmaId = null)
    {
        using var ctx = await _contextFactory.CreateDbContextAsync();
        var q = ctx.FirmalarArasiTransferler
            .Include(t => t.KaynakFirma)
            .Include(t => t.HedefFirma)
            .Include(t => t.KaynakHesap)
            .Include(t => t.HedefHesap)
            .AsQueryable();

        if (firmaId.HasValue && firmaId.Value > 0)
        {
            q = q.Where(t => t.KaynakFirmaId == firmaId.Value || t.HedefFirmaId == firmaId.Value);
        }

        return await q.OrderByDescending(t => t.TransferTarihi).ThenByDescending(t => t.Id).ToListAsync();
    }

    public async Task<FirmalarArasiTransfer?> GetByIdAsync(int id)
    {
        using var ctx = await _contextFactory.CreateDbContextAsync();
        return await ctx.FirmalarArasiTransferler
            .Include(t => t.KaynakFirma)
            .Include(t => t.HedefFirma)
            .Include(t => t.KaynakHesap)
            .Include(t => t.HedefHesap)
            .Include(t => t.KaynakHareket)
            .Include(t => t.HedefHareket)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<FirmalarArasiTransfer> CreateAsync(FirmalarArasiTransfer transfer)
    {
        ArgumentNullException.ThrowIfNull(transfer);
        if (transfer.KaynakFirmaId <= 0 || transfer.HedefFirmaId <= 0)
            throw new InvalidOperationException("Kaynak ve hedef firma seçilmelidir.");
        if (transfer.KaynakFirmaId == transfer.HedefFirmaId)
            throw new InvalidOperationException("Kaynak ve hedef firma aynı olamaz; bunun için banka virmanı kullanın.");
        if (transfer.Tutar <= 0)
            throw new InvalidOperationException("Tutar 0'dan büyük olmalıdır.");

        using var ctx = await _contextFactory.CreateDbContextAsync();
        var strategy = ctx.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            using var ctx = await _contextFactory.CreateDbContextAsync();

            // Tenant filter "TumFirmalar" senaryosu dışında diğer firmanın hesabını göremez;
            // bu yüzden hesap doğrulamalarını IgnoreQueryFilters üzerinden yapıyoruz (sadece sahiplik kontrolü).
            var kaynakHesap = await ctx.BankaHesaplari.IgnoreQueryFilters().AsNoTracking()
                .FirstOrDefaultAsync(h => h.Id == transfer.KaynakHesapId)
                ?? throw new InvalidOperationException("Kaynak hesap bulunamadı.");
            var hedefHesap = await ctx.BankaHesaplari.IgnoreQueryFilters().AsNoTracking()
                .FirstOrDefaultAsync(h => h.Id == transfer.HedefHesapId)
                ?? throw new InvalidOperationException("Hedef hesap bulunamadı.");

            if (kaynakHesap.FirmaId.HasValue && kaynakHesap.FirmaId.Value != transfer.KaynakFirmaId)
                throw new InvalidOperationException("Kaynak hesap, seçilen kaynak firmaya ait değil.");
            if (hedefHesap.FirmaId.HasValue && hedefHesap.FirmaId.Value != transfer.HedefFirmaId)
                throw new InvalidOperationException("Hedef hesap, seçilen hedef firmaya ait değil.");

            await using var tx = await ctx.Database.BeginTransactionAsync();

            // Önce başlık (hareket id'leri sonra atanacak)
            transfer.CreatedAt = DateTime.UtcNow;
            ctx.FirmalarArasiTransferler.Add(transfer);
            await ctx.SaveChangesAsync();

            var islemNoBase = $"TRF-{transfer.Id:000000}-{transfer.TransferTarihi:yyyyMMdd}";

            var kaynakHareket = new BankaKasaHareket
            {
                FirmaId = transfer.KaynakFirmaId,
                BankaHesapId = transfer.KaynakHesapId,
                IslemNo = islemNoBase + "-OUT",
                IslemTarihi = transfer.TransferTarihi,
                HareketTipi = HareketTipi.Cikis,
                Tutar = transfer.Tutar,
                Aciklama = $"Firmalar arası transfer (giden) → Firma#{transfer.HedefFirmaId} / Hesap#{transfer.HedefHesapId}. {transfer.Aciklama}".Trim(),
                BelgeNo = transfer.BelgeNo,
                IslemKaynak = IslemKaynak.Manuel,
                CreatedAt = DateTime.UtcNow
            };

            var hedefHareket = new BankaKasaHareket
            {
                FirmaId = transfer.HedefFirmaId,
                BankaHesapId = transfer.HedefHesapId,
                IslemNo = islemNoBase + "-IN",
                IslemTarihi = transfer.TransferTarihi,
                HareketTipi = HareketTipi.Giris,
                Tutar = transfer.Tutar,
                Aciklama = $"Firmalar arası transfer (gelen) ← Firma#{transfer.KaynakFirmaId} / Hesap#{transfer.KaynakHesapId}. {transfer.Aciklama}".Trim(),
                BelgeNo = transfer.BelgeNo,
                IslemKaynak = IslemKaynak.Manuel,
                CreatedAt = DateTime.UtcNow
            };

            ctx.BankaKasaHareketleri.Add(kaynakHareket);
            ctx.BankaKasaHareketleri.Add(hedefHareket);
            await ctx.SaveChangesAsync();

            transfer.KaynakHareketId = kaynakHareket.Id;
            transfer.HedefHareketId = hedefHareket.Id;
            await ctx.SaveChangesAsync();

            await tx.CommitAsync();
            return transfer;
        });
    }

    public async Task IptalEtAsync(int id)
    {
        using var ctx = await _contextFactory.CreateDbContextAsync();
        var strategy = ctx.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            using var ctx = await _contextFactory.CreateDbContextAsync();
            var transfer = await ctx.FirmalarArasiTransferler.FirstOrDefaultAsync(t => t.Id == id)
                ?? throw new InvalidOperationException("Transfer bulunamadı.");

            await using var tx = await ctx.Database.BeginTransactionAsync();

            if (transfer.KaynakHareketId.HasValue)
            {
                var k = await ctx.BankaKasaHareketleri.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(h => h.Id == transfer.KaynakHareketId.Value);
                if (k != null)
                {
                    k.IsDeleted = true;
                    k.UpdatedAt = DateTime.UtcNow;
                }
            }

            if (transfer.HedefHareketId.HasValue)
            {
                var h = await ctx.BankaKasaHareketleri.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(x => x.Id == transfer.HedefHareketId.Value);
                if (h != null)
                {
                    h.IsDeleted = true;
                    h.UpdatedAt = DateTime.UtcNow;
                }
            }

            transfer.IsDeleted = true;
            transfer.UpdatedAt = DateTime.UtcNow;

            await ctx.SaveChangesAsync();
            await tx.CommitAsync();
        });
    }
}



