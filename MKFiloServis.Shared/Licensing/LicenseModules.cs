using System.Text;

namespace MKFiloServis.Shared.Licensing;

/// <summary>Modül kimlikleri imza protokolünün parçasıdır; mevcut kimlikler değiştirilmez.</summary>
public static class LicenseModules
{
    public static IReadOnlyDictionary<string, string> All { get; } = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["cari"] = "Cari", ["filoservis"] = "Filo ve Servis", ["rentacar"] = "Rent a Car",
        ["muhasebe"] = "Muhasebe", ["personel"] = "Personel", ["fatura"] = "Fatura ve E-Fatura",
        ["bankakasa"] = "Banka ve Kasa", ["butce"] = "Bütçe", ["crm"] = "CRM",
        ["ebys"] = "EBYS ve Belgeler", ["satis"] = "Satış ve Galeri", ["stok"] = "Stok",
        ["holding"] = "Holding", ["raporlar"] = "Raporlar", ["planlama"] = "Checklist"
    };

    public static string Canonical(IEnumerable<string> modules)
    {
        var values = modules.Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal).ToArray();
        if (values.Length == 0 || values.Any(x => !All.ContainsKey(x)))
            throw new ArgumentException("En az bir geçerli lisans modülü seçilmelidir.");
        return string.Join(",", values);
    }

    // Haklar ayrı, değiştirilebilir bir DB sütunundan değil imzalı zarftan okunur.
    public static bool TryRead(string? signature, out string modules, out string rsaSignature)
    {
        modules = rsaSignature = string.Empty;
        if (signature is null || signature.Length > 8192) return false;
        var parts = signature.Split(':');
        if (parts.Length != 3 || parts[0] != "v3") return false;
        try
        {
            modules = Encoding.UTF8.GetString(Convert.FromBase64String(parts[1]));
            if (modules != Canonical(modules.Split(','))) return false;
            rsaSignature = parts[2];
            return true;
        }
        catch (ArgumentException) { return false; }
        catch (FormatException) { return false; }
    }

    public static string Payload(string v2Payload, string modules)
        => "MKFiloServis-License-v3:" + v2Payload + ":" + Canonical(modules.Split(','));

    public static string Envelope(string modules, string rsaSignature)
        => "v3:" + Convert.ToBase64String(Encoding.UTF8.GetBytes(Canonical(modules.Split(',')))) + ":" + rsaSignature;
}
