namespace KOAFiloServis.Shared;

/// <summary>
/// Global uygulama modu.
/// Test modunda tüm yeni kayıtlar TestSessionLog'a eklenir.
/// </summary>
public static class AppMode
{
    /// <summary>Test modu aktif mi?</summary>
    public static bool IsTestMode { get; set; }

    /// <summary>Aktif test oturum etiketi. Boşsa test modu kapalı.</summary>
    public static string? CurrentTestTag { get; set; }

    /// <summary>Aktif test oturum ID'si.</summary>
    public static int? CurrentSessionId { get; set; }

    /// <summary>Test moduna geç</summary>
    public static void EnterTestMode(string tag)
    {
        IsTestMode = true;
        CurrentTestTag = tag;
    }

    /// <summary>Test modundan çık</summary>
    public static void ExitTestMode()
    {
        IsTestMode = false;
        CurrentTestTag = null;
        CurrentSessionId = null;
    }
}
