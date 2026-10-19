namespace MKFiloServis.Shared.Licensing;

/// <summary>
/// Makine kodunda kullanılacak disk seri numarasını seçer. USB bellek/harici diskler takılıp çıkarılabilir ve WMI
/// listesinde dahili diskten önce gelebilir; seri numaraları makine kodunu (dolayısıyla lisansı) değiştirmemeli.
/// </summary>
public static class DiskSerialSelector
{
    /// <summary>USB veri yolundaki (bellek, harici disk) fiziksel disk mi?</summary>
    /// <remarks>
    /// Yalnız veri yoluna bakılır. "Removable/External" ortam türü tek başına yeterli sayılmaz: bazı masaüstü
    /// kasalarında hot-swap dahili diskler de öyle görünür ve onların seri numarası lisansta kullanılıyor olabilir.
    /// </remarks>
    public static bool IsUsbDrive(string? interfaceType, string? pnpDeviceId)
        => string.Equals(interfaceType?.Trim(), "USB", StringComparison.OrdinalIgnoreCase)
           || (pnpDeviceId?.TrimStart().StartsWith("USBSTOR", StringComparison.OrdinalIgnoreCase) ?? false);

    /// <summary>
    /// Verilen sırayla ilk boş olmayan seri numarasını (kırpılmış) döndürür; USB disklerin ortamlarını atlar.
    /// Seçilecek seri numarası yoksa null.
    /// </summary>
    /// <param name="media">Win32_PhysicalMedia sırasıyla (Tag, SerialNumber) çiftleri.</param>
    /// <param name="usbDriveIds">USB disklerin kimlikleri (Win32_DiskDrive.DeviceID, örn. \\.\PHYSICALDRIVE1).</param>
    public static string? Pick(IEnumerable<(string? Tag, string? Serial)> media, ISet<string> usbDriveIds)
    {
        foreach (var (tag, serial) in media)
        {
            if (!string.IsNullOrEmpty(tag) && usbDriveIds.Contains(tag))
                continue;

            var temiz = serial?.Trim();
            if (!string.IsNullOrEmpty(temiz))
                return temiz;
        }

        return null;
    }
}
