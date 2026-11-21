using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;

namespace MKFiloServis.Shared.Licensing;

/// <summary>
/// Makine kimliği v2: "FP2-AAAAAAAA-BBBBBBBB-CCCCCCCC-DDDDDDDD".
/// Dört bağımsız bileşen (işletim sistemi kimliği, işlemci, ana kart/BIOS, sistem diski) ayrı ayrı özetlenir.
/// PC adı ve Windows kullanıcı adı kullanılmaz; tek bir donanım değişiminde lisans geçerli kalır.
/// </summary>
public static class MachineFingerprint
{
    public const string Prefix = "FP2";
    public const int ComponentCount = 4;
    private const string Missing = "00000000";
    private static readonly string[] Tags = { "OS", "CPU", "BOARD", "DISK" };
    private static readonly Lazy<string> Cached = new(Compute, LazyThreadSafetyMode.ExecutionAndPublication);

    private static readonly HashSet<string> JunkValues = new(StringComparer.OrdinalIgnoreCase)
    {
        "none", "default string", "to be filled by o.e.m.", "to be filled by oem", "system serial number",
        "system product name", "not applicable", "not specified", "unknown", "n/a", "0", "00000000",
        "123456789", "1234567890", "ffffffff-ffff-ffff-ffff-ffffffffffff", "00000000-0000-0000-0000-000000000000"
    };

    public static string Current => Cached.Value;

    public static bool IsFingerprint(string? value) => TryParse(value, out _);

    public static string Build(string? osId, string? cpu, string? board, string? disk)
        => Prefix + "-" + string.Join("-", new[] { osId, cpu, board, disk }.Select((v, i) => Hash(Tags[i], v)));

    public static bool TryParse(string? value, out string[] components)
    {
        components = Array.Empty<string>();
        var normalized = new string((value ?? string.Empty).Where(c => !char.IsWhiteSpace(c)).ToArray()).ToUpperInvariant();
        var parts = normalized.Split('-');
        if (parts.Length != ComponentCount + 1 || parts[0] != Prefix) return false;

        var values = parts.Skip(1).ToArray();
        if (values.Any(p => p.Length != 8 || !p.All(Uri.IsHexDigit))) return false;
        components = values;
        return true;
    }

    /// <summary>
    /// Ortak ve boş olmayan bileşenlerden en fazla biri farklıysa eşleşir.
    /// İki veya daha az ortak bileşende tüm bileşenlerin eşleşmesi gerekir.
    /// </summary>
    public static bool Matches(string? licensed, string? current)
    {
        if (!TryParse(licensed, out var left) || !TryParse(current, out var right)) return false;

        int common = 0, equal = 0;
        for (var i = 0; i < ComponentCount; i++)
        {
            if (left[i] == Missing || right[i] == Missing) continue;
            common++;
            if (string.Equals(left[i], right[i], StringComparison.Ordinal)) equal++;
        }

        if (common == 0) return false;
        var required = common <= 2 ? common : common - 1;
        return equal >= required;
    }

    public static string Format(string? value)
        => TryParse(value, out var parts) ? Prefix + "-" + string.Join("-", parts) : (value ?? string.Empty).Trim();

    private static string Hash(string tag, string? value)
    {
        var cleaned = Clean(value);
        if (cleaned is null) return Missing;
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes("MKFiloServis.FP2|" + tag + "|" + cleaned.ToUpperInvariant()));
        return Convert.ToHexString(bytes, 0, 4);
    }

    private static string? Clean(string? value)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed) || JunkValues.Contains(trimmed)) return null;
        if (trimmed.All(c => c is '0' or '-' or 'F' or 'f' or ' ')) return null;
        return trimmed;
    }

    private static string Compute()
    {
        string? os = null, cpu = null, board = null, disk = null;
        try
        {
            if (OperatingSystem.IsWindows())
                CollectWindows(out os, out cpu, out board, out disk);
            else
                CollectUnix(out os, out board);
        }
        catch
        {
            // Bileşen okunamazsa boş bırakılır; kalan bileşenler eşleşmeye yeter.
        }

        // Hiçbir kimlik okunamayan nadir ortamlarda en azından sabit bir bileşen üret.
        if (Clean(os) is null && Clean(cpu) is null && Clean(board) is null && Clean(disk) is null)
            os = "HOST:" + Environment.MachineName;

        return Build(os, cpu, board, disk);
    }

    [SupportedOSPlatform("windows")]
    private static void CollectWindows(out string? os, out string? cpu, out string? board, out string? disk)
    {
        os = cpu = board = disk = null;

        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            using var key = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography");
            os = key?.GetValue("MachineGuid")?.ToString();
        }
        catch { }

        cpu = Wmi("SELECT ProcessorId FROM Win32_Processor", "ProcessorId");
        board = Wmi("SELECT SerialNumber FROM Win32_BaseBoard", "SerialNumber");
        if (Clean(board) is null)
            board = Wmi("SELECT UUID FROM Win32_ComputerSystemProduct", "UUID");
        disk = WindowsSystemDiskSerial();
    }

    [SupportedOSPlatform("windows")]
    private static string? Wmi(string query, string property)
    {
        try
        {
            using var searcher = new System.Management.ManagementObjectSearcher(query);
            searcher.Options.Timeout = TimeSpan.FromSeconds(8);
            foreach (System.Management.ManagementBaseObject item in searcher.Get())
            {
                var value = item[property]?.ToString();
                if (Clean(value) is not null) return value;
            }
        }
        catch { }
        return null;
    }

    // USB bellekler takılıp çıkarılabildiği için yalnız USB olmayan en düşük indeksli disk kullanılır.
    [SupportedOSPlatform("windows")]
    private static string? WindowsSystemDiskSerial()
    {
        try
        {
            using var searcher = new System.Management.ManagementObjectSearcher(
                "SELECT Index, InterfaceType, SerialNumber FROM Win32_DiskDrive");
            searcher.Options.Timeout = TimeSpan.FromSeconds(8);
            return searcher.Get().Cast<System.Management.ManagementBaseObject>()
                .Where(d => !string.Equals(d["InterfaceType"]?.ToString(), "USB", StringComparison.OrdinalIgnoreCase)
                    && Clean(d["SerialNumber"]?.ToString()) is not null)
                .OrderBy(d => Convert.ToInt32(d["Index"] ?? int.MaxValue))
                .Select(d => d["SerialNumber"]!.ToString())
                .FirstOrDefault();
        }
        catch { return null; }
    }

    private static void CollectUnix(out string? os, out string? board)
    {
        os = Read("/etc/machine-id") ?? Read("/var/lib/dbus/machine-id");
        board = Read("/sys/class/dmi/id/product_uuid") ?? Read("/sys/class/dmi/id/board_serial");
    }

    private static string? Read(string path)
    {
        try { return File.Exists(path) ? File.ReadAllText(path).Trim() : null; }
        catch { return null; }
    }
}
