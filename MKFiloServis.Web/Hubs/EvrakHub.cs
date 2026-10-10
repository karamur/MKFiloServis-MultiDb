using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MKFiloServis.Web.Data;

namespace MKFiloServis.Web.Hubs;

/// <summary>Personel evrak değişikliklerini real-time yayınlar.</summary>
[Microsoft.AspNetCore.Authorization.Authorize(AuthenticationSchemes = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)]
[Microsoft.AspNetCore.Authorization.Authorize(Policy = "Licensed:personel")]
public class EvrakHub(IDbContextFactory<ApplicationDbContext> contextFactory) : Hub
{
    public async Task SubscribePersonel(int personelId)
    {
        if (personelId <= 0 ||
            !int.TryParse(Context.User?.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) || userId <= 0)
            throw new HubException("Bu personelin bildirimlerine abone olma yetkiniz yok.");

        await using var context = await contextFactory.CreateDbContextAsync(Context.ConnectionAborted);
        var linkedPersonnelId = await context.Kullanicilar.AsNoTracking()
            .Where(user => user.Id == userId && user.Aktif && !user.IsDeleted && !user.Kilitli)
            .Select(user => user.SoforId)
            .SingleOrDefaultAsync(Context.ConnectionAborted);

        if (linkedPersonnelId != personelId ||
            !await context.Soforler.IgnoreQueryFilters().AsNoTracking().AnyAsync(personnel =>
                personnel.Id == personelId && personnel.Aktif && !personnel.IsDeleted,
                Context.ConnectionAborted))
            throw new HubException("Bu personelin bildirimlerine abone olma yetkiniz yok.");

        await Groups.AddToGroupAsync(Context.ConnectionId, $"personel-{personelId}");
    }
}


