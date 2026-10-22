using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using MKFiloServis.Web.Data;

namespace MKFiloServis.Web.Services.Destek;

/// <summary>Destek işlemini yapan kullanıcıyı ve güncel yetkilerini verir; oturum yoksa null (zamanlanmış işler).</summary>
public interface IDestekKullaniciSaglayici
{
    Task<DestekKullanici?> GetirAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Kimliği oturumdan, hesap ve yetki durumunu her çağrıda veritabanından alır
/// (<see cref="CurrentPermissionGuard"/> ile aynı kurallar: pasif, silinmiş ya da kilitli hesap yetkisizdir).
/// </summary>
public sealed class DestekKullaniciSaglayici(
    IDbContextFactory<ApplicationDbContext> contextFactory,
    IHttpContextAccessor httpContextAccessor,
    AppAuthenticationStateProvider authenticationStateProvider) : IDestekKullaniciSaglayici
{
    public async Task<DestekKullanici?> GetirAsync(CancellationToken cancellationToken = default)
    {
        var principal = httpContextAccessor.HttpContext?.User;
        if (principal?.Identity?.IsAuthenticated != true)
            principal = (await authenticationStateProvider.GetAuthenticationStateAsync()).User;

        if (principal.Identity?.IsAuthenticated != true ||
            !int.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var kullaniciId) || kullaniciId <= 0)
            return null;

        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var kullanici = await context.Kullanicilar.AsNoTracking()
            .Include(k => k.Rol).ThenInclude(r => r.Yetkiler)
            .FirstOrDefaultAsync(k => k.Id == kullaniciId && k.Aktif && !k.IsDeleted, cancellationToken);

        if (kullanici == null) return null;
        var kilitli = kullanici.Kilitli && (kullanici.KilitlenmeBitisUtc is null || kullanici.KilitlenmeBitisUtc > DateTime.UtcNow);
        if (kilitli) return null;

        var rolGecerli = kullanici.Rol != null && !kullanici.Rol.IsDeleted;
        var yonetici = rolGecerli && string.Equals(kullanici.Rol!.RolAdi, "Admin", StringComparison.Ordinal);
        var yetkiler = rolGecerli
            ? kullanici.Rol!.Yetkiler.Where(y => !y.IsDeleted && y.Izin).Select(y => y.YetkiKodu).ToHashSet(StringComparer.Ordinal)
            : new HashSet<string>(StringComparer.Ordinal);

        return new DestekKullanici(kullanici.Id, kullanici.AdSoyad, kullanici.Email, yonetici, yetkiler);
    }
}
