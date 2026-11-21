namespace MKFiloServis.Shared.Licensing;

/// <summary>Üretici giriş normalizasyonu. Doğrulamada imzalı alanlar yeniden yazılmaz.</summary>
public static class LicenseIdentity
{
    public static string CompanyCode(string? value) => (value ?? string.Empty).Trim().ToUpperInvariant();
    // Parmak izi (FP2-...) büyük harfe çevrilir; eski biçimler yalnız boşluktan arındırılır.
    public static string MachineId(string? value)
    {
        var cleaned = RemoveWhitespace(value);
        return MachineFingerprint.IsFingerprint(cleaned) ? MachineFingerprint.Format(cleaned) : cleaned;
    }
    public static string ContactPhone(string? value) => RemoveWhitespace(value);

    public static bool MatchesMachine(string? licensed, string? current)
    {
        var left = MachineId(licensed);
        var right = MachineId(current);
        return left.Length > 0 && right.Length > 0 && string.Equals(left, right, StringComparison.Ordinal);
    }

    private static string RemoveWhitespace(string? value)
        => string.Concat((value ?? string.Empty).Where(c => !char.IsWhiteSpace(c)));
}
