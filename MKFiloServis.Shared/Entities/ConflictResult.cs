namespace MKFiloServis.Shared.Entities;

/// <summary>
/// Cakisma kontrol sonucu - Blocking: kayit engellenir, Warning: onayla devam edilebilir
/// </summary>
public sealed class ConflictResult
{
    public List<ConflictItem> Conflicts { get; init; } = new();
    public bool HasBlocking => Conflicts.Any(c => c.Severity == ConflictSeverity.Blocking);
    public bool HasWarning => Conflicts.Any(c => c.Severity == ConflictSeverity.Warning);
    public bool IsClean => Conflicts.Count == 0;
}

public sealed class ConflictItem
{
    public ConflictSeverity Severity { get; init; }
    public string Kural { get; init; } = "";
    public string Mesaj { get; init; } = "";
    public int Gun { get; init; }
    public SeferSlot Slot { get; init; }
    public int? EtkilenenKayitId { get; init; }
    public string? EtkilenenAciklama { get; init; }
}

public enum ConflictSeverity
{
    Blocking,
    Warning
}


