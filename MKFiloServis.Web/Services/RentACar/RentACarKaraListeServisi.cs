using System.Data;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;

namespace MKFiloServis.Web.Services.RentACar;

public sealed class RentACarKaraListeServisi(
    IDbContextFactory<ApplicationDbContext> factory,
    IAktifFirmaProvider firmaProvider,
    AuthenticationStateProvider auth)
{
    private async Task<int> YetkiliKullaniciAsync(int firmaId)
    {
        if (firmaId <= 0 || firmaProvider.TumFirmalar || firmaProvider.AktifFirmaId != firmaId)
            throw new InvalidOperationException("Firma değişti veya seçilmedi. Sayfayı yenileyin.");
        var user = (await auth.GetAuthenticationStateAsync()).User;
        if (user.Identity?.IsAuthenticated != true || !user.IsInRole(SistemRolleri.Admin)
            || !int.TryParse(user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var id))
            throw new UnauthorizedAccessException("Kara liste yönetimi için yönetici yetkisi gerekir.");
        return id;
    }

    public async Task<List<RentACarKaraListeKaydi>> ListeleAsync(int firmaId)
    {
        await YetkiliKullaniciAsync(firmaId);
        await using var context = await factory.CreateDbContextAsync();
        return await context.RentACarKaraListeKayitlari.AsNoTracking()
            .Where(x => x.FirmaId == firmaId && !x.IsDeleted)
            .OrderByDescending(x => x.CreatedAt).ToListAsync();
    }

    public async Task EkleAsync(int firmaId, int musteriId, string neden)
    {
        var kullaniciId = await YetkiliKullaniciAsync(firmaId);
        NedeniDogrula(neden);
        await IslemAsync(async context =>
        {
            if (!await context.Cariler.AnyAsync(x => x.Id == musteriId && x.FirmaId == firmaId && !x.IsDeleted
                && (x.CariTipi == CariTipi.Musteri || x.CariTipi == CariTipi.MusteriTedarikci)))
                throw new InvalidOperationException("Müşteri aktif firmaya ait değil.");
            if (await context.RentACarKaraListeKayitlari.AnyAsync(x => x.FirmaId == firmaId && x.MusteriId == musteriId
                && !x.IsDeleted && x.KaldirmaTarihi == null))
                throw new InvalidOperationException("Müşteri zaten kara listede.");
            context.RentACarKaraListeKayitlari.Add(new RentACarKaraListeKaydi
            {
                FirmaId = firmaId, MusteriId = musteriId, Neden = neden.Trim(), EkleyenKullaniciId = kullaniciId
            });
        });
    }

    public async Task KaldirAsync(int firmaId, int kayitId, string neden)
    {
        var kullaniciId = await YetkiliKullaniciAsync(firmaId);
        NedeniDogrula(neden);
        await IslemAsync(async context =>
        {
            var kayit = await context.RentACarKaraListeKayitlari.SingleOrDefaultAsync(x => x.Id == kayitId
                && x.FirmaId == firmaId && !x.IsDeleted && x.KaldirmaTarihi == null)
                ?? throw new InvalidOperationException("Aktif kara liste kaydı bulunamadı.");
            kayit.KaldirmaTarihi = DateTime.UtcNow;
            kayit.KaldiranKullaniciId = kullaniciId;
            kayit.KaldirmaNedeni = neden.Trim();
        });
    }

    private async Task IslemAsync(Func<ApplicationDbContext, Task> islem)
    {
        await using var strategyContext = await factory.CreateDbContextAsync();
        await strategyContext.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var context = await factory.CreateDbContextAsync();
            await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            await islem(context);
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        });
    }

    private static void NedeniDogrula(string neden)
    {
        if (string.IsNullOrWhiteSpace(neden) || neden.Trim().Length > 500)
            throw new InvalidOperationException("Gerekçe 1–500 karakter olmalıdır.");
    }
}
