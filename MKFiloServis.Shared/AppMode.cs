namespace MKFiloServis.Shared;

/// <summary>
/// Global uygulama modu.
/// Demo modda uygulama acilir ama lisans yoktur.
/// Test modunda tum yeni kayitlar TestSessionLog'a eklenir.
/// </summary>
public static class AppMode
{
    /// <summary>Demo mod aktif mi? (lisans yok veya gecersiz)</summary>
    public static bool IsDemoMode { get; set; }

    /// <summary>Demo moda gecis sebebi (UI'da gosterilir).</summary>
    public static string? DemoReason { get; set; }

    /// <summary>Demo moda gec.</summary>
    public static void EnterDemoMode(string? reason = null)
    {
        IsDemoMode = true;
        DemoReason = reason;
    }

    /// <summary>Demo moddan cik (lisans yuklenince).</summary>
    public static void ExitDemoMode()
    {
        IsDemoMode = false;
        DemoReason = null;
    }

    /// <summary>Test modu aktif mi?</summary>
    public static bool IsTestMode { get; set; }

    /// <summary>Aktif test oturum etiketi. Bos sa test modu kapali.</summary>
    public static string? CurrentTestTag { get; set; }

    /// <summary>Aktif test oturum ID'si.</summary>
    public static int? CurrentSessionId { get; set; }

    /// <summary>Test moduna gec</summary>
    public static void EnterTestMode(string tag)
    {
        IsTestMode = true;
        CurrentTestTag = tag;
    }

    /// <summary>Test modundan cik</summary>
    public static void ExitTestMode()
    {
        IsTestMode = false;
        CurrentTestTag = null;
        CurrentSessionId = null;
    }
}


