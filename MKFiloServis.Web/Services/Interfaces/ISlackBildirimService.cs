namespace MKFiloServis.Web.Services.Interfaces;

public interface ISlackBildirimService
{
    Task<bool> GonderAsync(string mesaj, string? kanal = null, string? emoji = null);
    Task<bool> TestBaglantisiAsync();
}




