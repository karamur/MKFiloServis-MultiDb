using Microsoft.AspNetCore.SignalR;

namespace MKFiloServis.Web.Hubs;

/// <summary>Personel evrak değişikliklerini real-time yayınlar.</summary>
[Microsoft.AspNetCore.Authorization.Authorize(AuthenticationSchemes = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)]
public class EvrakHub : Hub
{
    public async Task SubscribePersonel(int personelId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"personel-{personelId}");
    }
}


