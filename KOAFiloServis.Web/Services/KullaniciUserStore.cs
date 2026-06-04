using KOAFiloServis.Shared.Entities;
using KOAFiloServis.Web.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace KOAFiloServis.Web.Services;

public class KullaniciUserStore : IUserPasswordStore<Kullanici>
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public KullaniciUserStore(IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public void Dispose()
    {
    }

    public async Task<IdentityResult> CreateAsync(Kullanici user, CancellationToken cancellationToken)
    {
        using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        context.Kullanicilar.Add(user);
        await context.SaveChangesAsync(cancellationToken);
        return IdentityResult.Success;
    }

    public async Task<IdentityResult> DeleteAsync(Kullanici user, CancellationToken cancellationToken)
    {
        using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        user.IsDeleted = true;
        user.UpdatedAt = DateTime.UtcNow;
        context.Kullanicilar.Update(user);
        await context.SaveChangesAsync(cancellationToken);
        return IdentityResult.Success;
    }

    public async Task<Kullanici?> FindByIdAsync(string userId, CancellationToken cancellationToken)
    {
        if (!int.TryParse(userId, out var id))
            return null;

        using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Kullanicilar.FirstOrDefaultAsync(x => !x.IsDeleted && x.Id == id, cancellationToken);
    }

    public async Task<Kullanici?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
    {
        using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Kullanicilar.FirstOrDefaultAsync(
            x => !x.IsDeleted && x.KullaniciAdi.ToUpper() == normalizedUserName,
            cancellationToken);
    }

    public Task<string?> GetNormalizedUserNameAsync(Kullanici user, CancellationToken cancellationToken)
    {
        return Task.FromResult<string?>(user.KullaniciAdi.ToUpperInvariant());
    }

    public Task<string> GetUserIdAsync(Kullanici user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.Id.ToString());
    }

    public Task<string?> GetUserNameAsync(Kullanici user, CancellationToken cancellationToken)
    {
        return Task.FromResult<string?>(user.KullaniciAdi);
    }

    public Task SetNormalizedUserNameAsync(Kullanici user, string? normalizedName, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task SetUserNameAsync(Kullanici user, string? userName, CancellationToken cancellationToken)
    {
        user.KullaniciAdi = userName ?? string.Empty;
        return Task.CompletedTask;
    }

    public async Task<IdentityResult> UpdateAsync(Kullanici user, CancellationToken cancellationToken)
    {
        using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        context.Kullanicilar.Update(user);
        await context.SaveChangesAsync(cancellationToken);
        return IdentityResult.Success;
    }

    public Task SetPasswordHashAsync(Kullanici user, string? passwordHash, CancellationToken cancellationToken)
    {
        user.SifreHash = passwordHash ?? string.Empty;
        return Task.CompletedTask;
    }

    public Task<string?> GetPasswordHashAsync(Kullanici user, CancellationToken cancellationToken)
    {
        return Task.FromResult<string?>(user.SifreHash);
    }

    public Task<bool> HasPasswordAsync(Kullanici user, CancellationToken cancellationToken)
    {
        return Task.FromResult(!string.IsNullOrWhiteSpace(user.SifreHash));
    }
}
