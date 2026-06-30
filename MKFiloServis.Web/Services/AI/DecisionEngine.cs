namespace MKFiloServis.Web.Services.AI;

/// <summary>
/// AI tabanlı karar motoru. Kaydet öncesi validasyon.
/// Block: kaydı ENGELLER. Warn: uyarır ama izin verir. Ok: geçer.
/// </summary>
public class DecisionEngine
{
    public DecisionResult Decide(int toplamSefer, int mesai, decimal birimFiyat, decimal ortalamaSefer = 5)
    {
        if (toplamSefer > 10)
            return DecisionResult.Block($"Sefer limiti aşıldı ({toplamSefer} > 10)");

        if (toplamSefer == 0 && mesai == 0 && birimFiyat == 0)
            return DecisionResult.Block("Tüm değerler sıfır — veri girilmemiş");

        if (toplamSefer > ortalamaSefer * 2)
            return DecisionResult.Warn($"Sefer sayısı ortalamanın çok üstünde ({toplamSefer} vs ortalama {ortalamaSefer})");

        if (mesai > 5)
            return DecisionResult.Warn($"Mesai sayısı yüksek ({mesai})");

        if (birimFiyat > 10000)
            return DecisionResult.Warn($"Birim fiyat anormal yüksek ({birimFiyat:N2})");

        return DecisionResult.Ok();
    }
}

public class DecisionResult
{
    public bool IsBlocked { get; set; }
    public bool IsWarning { get; set; }
    public string Message { get; set; } = string.Empty;

    public static DecisionResult Block(string msg) => new() { IsBlocked = true, Message = msg };
    public static DecisionResult Warn(string msg) => new() { IsWarning = true, Message = msg };
    public static DecisionResult Ok() => new();
}



