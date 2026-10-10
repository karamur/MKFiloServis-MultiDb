namespace MKFiloServis.Web.Services;

/// <summary>Authentication session lifetime and firm-scope rules shared by circuits and API requests.</summary>
public static class AuthenticationSessionPolicy
{
    public static readonly TimeSpan MaximumLifetime = TimeSpan.FromHours(12);

    public static bool IsWithinLifetime(DateTimeOffset startedAtUtc, DateTimeOffset nowUtc)
    {
        var age = nowUtc - startedAtUtc;
        return age >= TimeSpan.Zero && age < MaximumLifetime;
    }

    public static bool AllowsFirmScopeRestore(bool isAdmin, bool allFirms, int selectedFirmId, int? defaultFirmId)
        => isAdmin || (!allFirms && (selectedFirmId == 0 || selectedFirmId == defaultFirmId));

    public static bool AllowsFirmScopeSelection(bool authenticated, bool isAdmin, int selectedFirmId, int? defaultFirmId)
        => selectedFirmId == 0
            ? true
            : authenticated && (isAdmin || selectedFirmId == defaultFirmId);
}
