namespace MKFiloServis.Web.Services.Interfaces;

public interface ILokalizasyonService
{
    string AktifDil { get; }
    string T(string anahtar);
    Task DilDegistirAsync(string dilKodu);
    IReadOnlyList<(string Kod, string Ad)> DesteklenenDiller { get; }
}




