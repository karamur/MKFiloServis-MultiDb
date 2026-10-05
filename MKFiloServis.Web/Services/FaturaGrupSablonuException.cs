namespace MKFiloServis.Web.Services;

public enum FaturaGrupSablonuHata
{
    GecersizIstek,
    Bulunamadi
}

/// <summary>Yalnız kullanıcıya bildirilebilir şablon doğrulama ve kayıt hataları.</summary>
public sealed class FaturaGrupSablonuException : Exception
{
    public FaturaGrupSablonuHata Hata { get; }

    public FaturaGrupSablonuException(FaturaGrupSablonuHata hata, string message) : base(message)
    {
        Hata = hata;
    }
}
