using System;
using System.Management;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;
using System.Runtime.Versioning;
using MKFiloServis.Shared.Licensing;

namespace MKFiloServis.Shared;

[SupportedOSPlatform("windows")]
public static class LisansHelper
{
    private static readonly Lazy<string> CachedMachineCode = new(GenerateMachineCode);

    /// <summary>
    /// Bilgisayarın benzersiz makine kodunu üretir
    /// (CPU ID + Ana Kart Seri No + Disk Seri No)
    /// </summary>
    public static string GetMachineCode()
    {
        return CachedMachineCode.Value;
    }

    public static string NormalizeMachineCode(string? machineCode)
    {
        return (machineCode ?? string.Empty)
            .Trim()
            .Replace("-", string.Empty)
            .Replace(" ", string.Empty)
            .ToUpperInvariant();
    }

    private static string GenerateMachineCode()
    {
        try
        {
            var cpuId = GetCpuId();
            var motherboardSerial = GetMotherboardSerial();
            var diskSerial = GetDiskSerial();

            var hasHardwareFingerprint =
                !cpuId.StartsWith("CPU-UNKNOWN", StringComparison.OrdinalIgnoreCase) ||
                !motherboardSerial.StartsWith("MB-UNKNOWN", StringComparison.OrdinalIgnoreCase) ||
                !diskSerial.StartsWith("DISK-UNKNOWN", StringComparison.OrdinalIgnoreCase);

            var combined = hasHardwareFingerprint
                ? $"{cpuId}-{motherboardSerial}-{diskSerial}"
                : BuildDeterministicFallbackSource();

            // Hash'le daha kısa ve standart hale getir
            return HashMachineCode(combined);
        }
        catch
        {
            return HashMachineCode(BuildDeterministicFallbackSource());
        }
    }

    private static string GetCpuId()
    {
        try
        {
            var searcher = new ManagementObjectSearcher("SELECT ProcessorId FROM Win32_Processor");
            foreach (ManagementObject obj in searcher.Get())
            {
                return obj["ProcessorId"]?.ToString() ?? "UNKNOWN";
            }
        }
        catch { }
        return "CPU-UNKNOWN";
    }

    private static string GetMotherboardSerial()
    {
        try
        {
            var searcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_BaseBoard");
            foreach (ManagementObject obj in searcher.Get())
            {
                return obj["SerialNumber"]?.ToString() ?? "UNKNOWN";
            }
        }
        catch { }
        return "MB-UNKNOWN";
    }

    private static string GetDiskSerial()
    {
        try
        {
            // USB bellek/harici diskler takılıp çıkarılabilir ve WMI listesinde dahili diskten önce gelebilir;
            // atlanmazsa USB takılınca makine kodu (dolayısıyla lisans eşleşmesi) değişir.
            var serial = DiskSerialSelector.Pick(ReadPhysicalMedia(), GetUsbDriveIds());
            if (serial != null)
                return serial;
        }
        catch { }
        return "DISK-UNKNOWN";
    }

    private static IEnumerable<(string? Tag, string? Serial)> ReadPhysicalMedia()
    {
        var searcher = new ManagementObjectSearcher("SELECT SerialNumber, Tag FROM Win32_PhysicalMedia");
        foreach (ManagementObject obj in searcher.Get())
        {
            yield return (obj["Tag"]?.ToString(), obj["SerialNumber"]?.ToString());
        }
    }

    /// <summary>USB veri yolundaki fiziksel disklerin kimlikleri (örn. \\.\PHYSICALDRIVE1); Win32_PhysicalMedia.Tag ile eşleşir.</summary>
    private static HashSet<string> GetUsbDriveIds()
    {
        var kimlikler = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            var searcher = new ManagementObjectSearcher("SELECT DeviceID, InterfaceType, PNPDeviceID FROM Win32_DiskDrive");
            foreach (ManagementObject obj in searcher.Get())
            {
                var deviceId = obj["DeviceID"]?.ToString();
                if (!string.IsNullOrEmpty(deviceId) &&
                    DiskSerialSelector.IsUsbDrive(obj["InterfaceType"]?.ToString(), obj["PNPDeviceID"]?.ToString()))
                    kimlikler.Add(deviceId);
            }
        }
        catch { }
        return kimlikler;
    }

    private static string BuildDeterministicFallbackSource()
    {
        var machineGuid = GetMachineGuid();
        var machineName = Environment.MachineName;
        var userDomain = Environment.UserDomainName;
        var osVersion = Environment.OSVersion.VersionString;

        return $"{machineGuid}-{machineName}-{userDomain}-{osVersion}";
    }

    private static string GetMachineGuid()
    {
        try
        {
            if (!OperatingSystem.IsWindows())
                return "MACHINE-GUID-UNAVAILABLE";

            return Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Cryptography", "MachineGuid", null)?.ToString()
                ?? "MACHINE-GUID-UNAVAILABLE";
        }
        catch
        {
            return "MACHINE-GUID-UNAVAILABLE";
        }
    }

    private static string HashMachineCode(string source)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(source));
        return Convert.ToBase64String(hash).Substring(0, 32).Replace("/", "").Replace("+", "");
    }

    /// <summary>
    /// Makine kodunu görsel formatta göster
    /// </summary>
    public static string FormatMachineCode(string machineCode)
    {
        if (string.IsNullOrEmpty(machineCode) || machineCode.Length < 16)
            return machineCode;

        // XXXX-XXXX-XXXX-XXXX formatında göster
        var formatted = "";
        for (int i = 0; i < Math.Min(machineCode.Length, 16); i++)
        {
            if (i > 0 && i % 4 == 0)
                formatted += "-";
            formatted += machineCode[i];
        }
        return formatted;
    }
}


