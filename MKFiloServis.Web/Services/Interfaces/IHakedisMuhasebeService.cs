namespace MKFiloServis.Web.Services.Interfaces;

public interface IHakedisMuhasebeService
{
    /// <summary>Onaylanmış hakedişi muhasebeye aktarır (2 fiş: GELIR + GIDER).</summary>
    Task MuhasebeyeAktarAsync(int hakedisId);
}



