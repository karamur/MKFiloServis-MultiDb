using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using MKFiloServis.Web.Data;

namespace MKFiloServis.Web.Services;

/// <summary>Kimliği oturumdan, hesap ve izin durumunu her yazımda veritabanından alır.</summary>
public sealed class CurrentPermissionGuard(
    IDbContextFactory<ApplicationDbContext> contextFactory,
    IHttpContextAccessor httpContextAccessor,
    AppAuthenticationStateProvider authenticationStateProvider)
{
    public async Task<bool> HasAnyAsync(params string[] permissions)
    {
        try
        {
            await RequireAnyAsync(permissions);
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    public async Task RequireAnyAsync(params string[] permissions)
    {
        var principal = httpContextAccessor.HttpContext?.User;
        if (principal?.Identity?.IsAuthenticated != true)
            principal = (await authenticationStateProvider.GetAuthenticationStateAsync()).User;

        if (principal.Identity?.IsAuthenticated != true ||
            !int.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) || userId <= 0)
            throw new UnauthorizedAccessException("Bu işlem için oturum açmalısınız.");

        await using var context = await contextFactory.CreateDbContextAsync();
        var user = await context.Kullanicilar.AsNoTracking()
            .Include(k => k.Rol).ThenInclude(r => r.Yetkiler)
            .FirstOrDefaultAsync(k => k.Id == userId && k.Aktif && !k.IsDeleted);

        var lockedNow = user?.Kilitli == true &&
            (user.KilitlenmeBitisUtc is null || user.KilitlenmeBitisUtc > DateTime.UtcNow);
        if (permissions.Length == 0 || user?.Rol == null || user.Rol.IsDeleted || lockedNow ||
            (!string.Equals(user.Rol.RolAdi, "Admin", StringComparison.Ordinal) &&
             !user.Rol.Yetkiler.Any(y => !y.IsDeleted && y.Izin && permissions.Contains(y.YetkiKodu))))
            throw new UnauthorizedAccessException("Bu işlem için güncel yetkiniz bulunmuyor.");
    }
}
