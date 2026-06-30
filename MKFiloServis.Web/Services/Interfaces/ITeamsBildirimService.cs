namespace MKFiloServis.Web.Services.Interfaces;

public interface ITeamsBildirimService
{
    Task<bool> GonderAsync(string baslik, string mesaj, string? renk = null, string? butonMetin = null, string? butonUrl = null);
    Task<bool> TestBaglantisiAsync();
}




