using System.Globalization;

namespace MKFiloServis.Shared.Licensing;

/// <summary>Mevcut v2 imza protokolünün ortak, değişmez alan sırası.</summary>
public static class LicenseSignaturePayload
{
    public static string Create(string? company, string? machine, DateTime expiry, int days,
        bool demo, string? maximumVersion, DateTime created, string? phone)
    {
        static string Field(string? value)
            => (value?.Length ?? 0).ToString(CultureInfo.InvariantCulture) + ":" + value;
        return "MKFiloServis-License-v2" + Field(company) + Field(machine)
            + Field(expiry.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))
            + Field(days.ToString(CultureInfo.InvariantCulture))
            + Field(demo ? "True" : "False") + Field(maximumVersion)
            + Field(created.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)) + Field(phone);
    }
}
