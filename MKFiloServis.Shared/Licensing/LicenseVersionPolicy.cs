namespace MKFiloServis.Shared.Licensing;

/// <summary>Üretici ve doğrulayıcı için ortak, imzalı sürüm hakkı kuralları.</summary>
public static class LicenseVersionPolicy
{
    // Mevcut sözleşme: yalnız bu açık değer sınırsız sürüm hakkıdır.
    public const string Unlimited = "0.0.0";
    // LicenseInfo.AllowedVersion is persisted as varchar(20) by EF Core.
    public const int MaximumLength = 20;

    public static bool IsValid(string? value)
        => TryRead(value, out _);

    public static bool Allows(string? value, Version applicationVersion)
    {
        if (!TryRead(value, out var maximum)) return false;
        return value == Unlimited || applicationVersion <= maximum!;
    }

    /// <summary>Checks a parsed release string against the signed maximum version.</summary>
    public static bool Allows(string? maximumVersion, string? requestedVersion)
    {
        if (!TryRead(maximumVersion, out var maximum)) return false;
        if (!TryRead(requestedVersion, out var requested)) return false;
        if (maximumVersion == Unlimited) return true;
        return requested! <= maximum!;
    }

    private static bool TryRead(string? value, out Version? version)
    {
        version = null;
        if (string.IsNullOrWhiteSpace(value) || value.Length > MaximumLength) return false;
        var parts = value.Split('.');
        if (parts.Length is < 2 or > 4 || parts.Any(part => part.Length == 0
            || part.Any(character => character is < '0' or > '9'))) return false;
        return Version.TryParse(value, out version);
    }
}
