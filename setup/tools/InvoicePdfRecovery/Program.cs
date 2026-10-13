using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.DependencyInjection;

if (args.Length != 4)
{
    Console.Error.WriteLine("Usage: InvoicePdfRecovery <old-uploads-root> <current-storage-root> <old-raw-key-file> <db-path-list>");
    return 2;
}

var oldUploadsRoot = Path.GetFullPath(args[0]);
var currentStorageRoot = Path.GetFullPath(args[1]);
var currentUploadsRoot = Path.Combine(currentStorageRoot, "uploads");
var keyRing = Path.Combine(currentStorageRoot, "keys");
if (!Directory.Exists(oldUploadsRoot) || !Directory.Exists(currentUploadsRoot) ||
    !Directory.GetFiles(keyRing, "key-*.xml").Any())
    throw new InvalidOperationException("Kaynak, hedef veya mevcut uygulama anahtar halkası bulunamadı.");

var oldKey = Convert.FromHexString(File.ReadAllText(args[2]).Trim());
if (oldKey.Length != 32)
    throw new InvalidOperationException("Eski anahtar 32 bayt olmalıdır.");

var paths = File.ReadAllLines(args[3], System.Text.Encoding.UTF8)
    .Where(path => !string.IsNullOrWhiteSpace(path))
    .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

using var services = new ServiceCollection()
    .AddLogging()
    .AddDataProtection()
    .SetApplicationName("MKFiloServis")
    .PersistKeysToFileSystem(new DirectoryInfo(keyRing))
    .Services
    .Configure<KeyManagementOptions>(options => options.AutoGenerateKeys = false)
    .BuildServiceProvider();
var protector = services.GetRequiredService<IDataProtectionProvider>()
    .CreateProtector("MKFiloServis.SecureFileStorage.v2");

var recovered = 0;
var alreadyPresent = 0;
var missing = 0;
var failed = 0;
try
{
    foreach (var path in paths)
    {
        var relative = path.Replace('\\', '/');
        var segments = relative.Split('/');
        if (!relative.StartsWith("faturalar/", StringComparison.OrdinalIgnoreCase) ||
            !relative.EndsWith(".pdf.enc", StringComparison.OrdinalIgnoreCase) ||
            segments.Any(s => string.IsNullOrWhiteSpace(s) || s is "." or ".." || s.Contains(':')))
            throw new InvalidDataException("Veritabanında geçersiz PDF yolu var.");

        var source = Path.GetFullPath(Path.Combine(oldUploadsRoot, relative.Replace('/', Path.DirectorySeparatorChar)));
        var target = Path.GetFullPath(Path.Combine(currentUploadsRoot, relative.Replace('/', Path.DirectorySeparatorChar)));
        if (!source.StartsWith(oldUploadsRoot.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
            !target.StartsWith(currentUploadsRoot.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("PDF yolu depo dışına çıkıyor.");

        if (File.Exists(target)) { alreadyPresent++; continue; }
        if (!File.Exists(source)) { missing++; continue; }

        string? temporary = null;
        try
        {
            var encrypted = File.ReadAllBytes(source);
            if (encrypted.Length < 33 || !encrypted.AsSpan(0, 5).SequenceEqual("KOA1\x01"u8))
                throw new InvalidDataException("Eski PDF şifreleme biçimi beklenen KOA1 sürümü değil.");
            var plain = new byte[encrypted.Length - 33];
            using (var aes = new AesGcm(oldKey, 16))
                aes.Decrypt(encrypted.AsSpan(5, 12), encrypted.AsSpan(33), encrypted.AsSpan(17, 16), plain);
            if (!plain.AsSpan().StartsWith("%PDF-"u8))
                throw new InvalidDataException("Çözülen dosya PDF değil.");

            var payload = protector.Protect(plain);
            var converted = new byte[4 + payload.Length];
            "MKD1"u8.CopyTo(converted);
            payload.CopyTo(converted, 4);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            temporary = target + ".recovering-" + Guid.NewGuid().ToString("N") + ".tmp";
            File.WriteAllBytes(temporary, converted);
            if (!protector.Unprotect(converted.AsSpan(4).ToArray()).AsSpan().SequenceEqual(plain))
                throw new CryptographicException("Yeni şifreleme doğrulanamadı.");
            File.Move(temporary, target, overwrite: false);
            temporary = null;
            recovered++;
            CryptographicOperations.ZeroMemory(plain);
        }
        catch (Exception ex)
        {
            failed++;
            Console.Error.WriteLine($"PDF kurtarılamadı: {relative}: {ex.GetType().Name}: {ex.Message}");
        }
        finally
        {
            if (temporary != null && File.Exists(temporary)) File.Delete(temporary);
        }
    }
}
finally
{
    CryptographicOperations.ZeroMemory(oldKey);
}

Console.WriteLine($"PDF paths={paths.Length}, recovered={recovered}, present={alreadyPresent}, missing={missing}, failed={failed}");
return failed == 0 && missing == 0 ? 0 : 1;
