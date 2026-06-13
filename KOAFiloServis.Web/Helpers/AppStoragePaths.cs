namespace KOAFiloServis.Web.Helpers;

public static class AppStoragePaths
{
    public const string DefaultInstallRoot = @"C:\KOAFiloServis";
    public const string DefaultStorageRoot = @"C:\KOAFiloServis_yedekleme";

    /// <summary>
    /// Personel evrakları için arşiv dizin yolu: Arsiv/Sifreli/Personeller
    /// (SecureFileService tarafından {StorageRoot}/Arsiv/Sifreli/Personeller olarak çözümlenir)
    /// </summary>
    public const string PersonelEvrakRelativeRoot = "Arsiv/Sifreli/Personeller";

    public static string GetStorageRoot(string contentRootPath)
    {
        var configured = Environment.GetEnvironmentVariable("CRMFILO_STORAGE_ROOT");
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return Path.GetFullPath(configured);
        }

        return DefaultStorageRoot;
    }

    public static string GetUploadsRoot(string contentRootPath)
        => Path.Combine(GetStorageRoot(contentRootPath), "uploads");

    public static string GetDatabaseBackupRoot(string contentRootPath)
        => Path.Combine(GetStorageRoot(contentRootPath), "database");

    public static string GetDataProtectionKeysRoot(string contentRootPath)
        => Path.Combine(GetStorageRoot(contentRootPath), "keys");

    /// <summary>
    /// Personel arşiv klasör adını üretir: "{Ad} {Soyad} - {FirmaAdi}"
    /// Türkçe karakterler ASCII'ye dönüştürülür, Windows için sakıncalı karakterler temizlenir.
    /// </summary>
    public static string BuildPersonelArsivKlasoru(string ad, string soyad, string? firmaAdi)
    {
        var adSoyad = NormalizeFolderName($"{ad} {soyad}");
        var firma = NormalizeFolderName(firmaAdi ?? "FIRMA_YOK");
        return $"{adSoyad} - {firma}";
    }

    /// <summary>
    /// Klasör/dosya adı için Türkçe karakterleri ASCII karşılıklarına dönüştürür,
    /// Windows için sakıncalı karakterleri tire ile değiştirir, fazla boşlukları temizler.
    /// </summary>
    public static string NormalizeFolderName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "BILINMIYOR";

        var normalized = value.Trim()
            .Replace('Ç', 'C')
            .Replace('Ğ', 'G')
            .Replace('İ', 'I')
            .Replace('Ö', 'O')
            .Replace('Ş', 'S')
            .Replace('Ü', 'U')
            .Replace('ç', 'c')
            .Replace('ğ', 'g')
            .Replace('ı', 'i')
            .Replace('ö', 'o')
            .Replace('ş', 's')
            .Replace('ü', 'u');

        foreach (var c in Path.GetInvalidFileNameChars())
            normalized = normalized.Replace(c, '-');

        // Birden fazla boşluğu tek boşluğa indir
        while (normalized.Contains("  "))
            normalized = normalized.Replace("  ", " ");

        return normalized.Trim();
    }
}
