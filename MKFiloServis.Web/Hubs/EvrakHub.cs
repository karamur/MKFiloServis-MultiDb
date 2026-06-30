using Microsoft.AspNetCore.SignalR;

namespace MKFiloServis.Web.Hubs;

/// <summary>Personel evrak değişikliklerini real-time yayınlar.</summary>
public class EvrakHub : Hub
{
    public async Task SubscribePersonel(int personelId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"personel-{personelId}");
    }
}


