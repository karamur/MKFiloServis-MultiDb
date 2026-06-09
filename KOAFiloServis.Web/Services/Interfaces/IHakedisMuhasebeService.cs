namespace KOAFiloServis.Web.Services;

public interface IHakedisMuhasebeService
{
    /// <summary>Onaylanmış hakedişi muhasebeye aktarır (2 fiş: GELIR + GIDER).</summary>
    Task MuhasebeyeAktarAsync(int hakedisId);
}
