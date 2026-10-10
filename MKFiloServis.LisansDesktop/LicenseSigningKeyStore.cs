using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using MKFiloServis.Shared.Licensing;

/// <summary>İmzalama anahtarının tüm işlemleri lisans uygulaması tarafından yürütülür.</summary>
internal static class LicenseSigningKeyStore
{
    private static readonly string DirectoryPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MKFiloServis", "licensing");
    private static readonly string ActivePath = Path.Combine(DirectoryPath, "signing-key.dpapi");
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("MKFiloServis.LisansDesktop.SigningKey.v1");
    private const int MaxKeyFileBytes = 64 * 1024;

    public static RSA OpenSigningKey()
    {
        if (!File.Exists(ActivePath))
        {
            // Önceki kurulumun mevcut anahtarını, program içi şifreli depoya bir kez taşı.
            var previousPath = Path.Combine(DirectoryPath, "license-signing-private.pem");
            if (!File.Exists(previousPath))
                throw new InvalidOperationException("İmzalama anahtarı bulunamadı. İmza Anahtarı menüsünden mevcut anahtarı veya yedeğini içe aktarın.");
            Import(previousPath, []);
        }
        var encrypted = ReadKeyFile(ActivePath);
        var plain = ProtectedData.Unprotect(encrypted, Entropy, DataProtectionScope.CurrentUser);
        try
        {
            var activeKey = ReadPrivateKey(plain);
            try
            {
                RetireLegacyPrivateKey(activeKey);
                return activeKey;
            }
            catch
            {
                activeKey.Dispose();
                throw;
            }
        }
        finally { CryptographicOperations.ZeroMemory(plain); }
    }

    public static string GetFingerprint()
    {
        using var rsa = OpenSigningKey();
        return Convert.ToHexString(SHA256.HashData(rsa.ExportSubjectPublicKeyInfo()));
    }

    public static void ExportBackup(string path, ReadOnlySpan<char> password, bool overwrite = false)
    {
        if (password.Length < 16) throw new ArgumentException("Yedek parolası en az 16 karakter olmalıdır.");
        EnsureOutsideRepository(path);
        EnsureBackupPathIsNotKeyStore(path);
        using var rsa = OpenSigningKey();
        var encrypted = rsa.ExportEncryptedPkcs8PrivateKey(password,
            new PbeParameters(PbeEncryptionAlgorithm.Aes256Cbc, HashAlgorithmName.SHA256, 600_000));
        // Dosya başarı bildirilmeden yeniden okunup şifre çözme ve imza eşleşmesi denetlenir.
        using var verified = RSA.Create();
        verified.ImportEncryptedPkcs8PrivateKey(password, encrypted, out var consumed);
        if (consumed != encrypted.Length) throw new CryptographicException("Yedek içeriği geçersiz.");
        VerifyPrivateKey(verified);
        WriteAtomic(path, encrypted, overwrite);
        var saved = ReadKeyFile(path);
        using var savedKey = RSA.Create();
        savedKey.ImportEncryptedPkcs8PrivateKey(password, saved, out consumed);
        if (consumed != saved.Length) throw new CryptographicException("Yedek dosyası geçersiz.");
        VerifyPrivateKey(savedKey);
    }

    public static void VerifyBackup(string path, ReadOnlySpan<char> password)
    {
        var encrypted = ReadKeyFile(path);
        using var rsa = RSA.Create();
        rsa.ImportEncryptedPkcs8PrivateKey(password, encrypted, out var consumed);
        if (consumed != encrypted.Length) throw new CryptographicException("Yedek dosyası geçersiz.");
        VerifyPrivateKey(rsa);
    }

    public static void Import(string path, ReadOnlySpan<char> password)
    {
        var input = ReadKeyFile(path);
        byte[]? plain = null;
        using var rsa = RSA.Create();
        try
        {
            if (Path.GetExtension(path).Equals(".pem", StringComparison.OrdinalIgnoreCase))
                rsa.ImportFromPem(Encoding.UTF8.GetString(input));
            else if (Path.GetExtension(path).Equals(".json", StringComparison.OrdinalIgnoreCase))
            {
                // Önceden oluşturulmuş DPAPI yedeği programdan geri yüklenebilir.
                using var document = JsonDocument.Parse(input);
                var root = document.RootElement;
                if (root.GetProperty("format").GetString() != "MKFiloServis-LicenseSigningBackup-v1"
                    || root.GetProperty("protection").GetString() != "Windows-DPAPI-CurrentUser")
                    throw new CryptographicException("Desteklenmeyen yedek biçimi.");
                plain = ProtectedData.Unprotect(Convert.FromBase64String(root.GetProperty("ciphertext").GetString()!),
                    Encoding.UTF8.GetBytes("MKFiloServis-LicenseSigningBackup-v1"), DataProtectionScope.CurrentUser);
                rsa.ImportFromPem(Encoding.UTF8.GetString(plain));
            }
            else
            {
                rsa.ImportEncryptedPkcs8PrivateKey(password, input, out var consumed);
                if (consumed != input.Length) throw new CryptographicException("Yedek dosyası geçersiz.");
            }
            VerifyPrivateKey(rsa);
            SaveActiveKey(rsa);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(input);
            if (plain != null) CryptographicOperations.ZeroMemory(plain);
        }
    }

    private static void SaveActiveKey(RSA rsa)
    {
        EnsurePrivateDirectory();
        var plain = rsa.ExportPkcs8PrivateKey();
        byte[]? roundtrip = null;
        try
        {
            var encrypted = ProtectedData.Protect(plain, Entropy, DataProtectionScope.CurrentUser);
            roundtrip = ProtectedData.Unprotect(encrypted, Entropy, DataProtectionScope.CurrentUser);
            using var restored = ReadPrivateKey(roundtrip);
            WriteAtomic(ActivePath, encrypted, overwrite: true);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plain);
            if (roundtrip != null) CryptographicOperations.ZeroMemory(roundtrip);
        }
    }

    private static RSA ReadPrivateKey(byte[] plain)
    {
        var rsa = RSA.Create();
        try
        {
            rsa.ImportPkcs8PrivateKey(plain, out var consumed);
            if (consumed != plain.Length) throw new CryptographicException("İmzalama anahtarı geçersiz.");
            VerifyPrivateKey(rsa);
            return rsa;
        }
        catch { rsa.Dispose(); throw; }
    }

    private static void VerifyPrivateKey(RSA rsa)
    {
        using var verifier = RSA.Create();
        verifier.ImportFromPem(LicenseSigningPublicKey.Pem);
        var nonce = RandomNumberGenerator.GetBytes(32);
        var signature = rsa.SignData(nonce, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
        if (!verifier.VerifyData(nonce, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pss))
            throw new CryptographicException("İmzalama anahtarı bu sürümün açık anahtarıyla eşleşmiyor. Mevcut anahtar değiştirilmedi.");
    }

    private static void RetireLegacyPrivateKey(RSA activeKey)
    {
        var legacyPath = Path.Combine(DirectoryPath, "license-signing-private.pem");
        if (!File.Exists(legacyPath)) return;
        if ((File.GetAttributes(legacyPath) & FileAttributes.ReparsePoint) != 0)
            throw new IOException("Eski imzalama anahtarı sembolik bağlantı; otomatik temizleme durduruldu.");

        var legacyBytes = ReadKeyFile(legacyPath);
        var legacyChars = new UTF8Encoding(false, true).GetChars(legacyBytes);
        try
        {
            using var legacyKey = RSA.Create();
            legacyKey.ImportFromPem(legacyChars);
            VerifyPrivateKey(legacyKey);
            var activePublicKey = activeKey.ExportSubjectPublicKeyInfo();
            var legacyPublicKey = legacyKey.ExportSubjectPublicKeyInfo();
            try
            {
                if (!CryptographicOperations.FixedTimeEquals(activePublicKey, legacyPublicKey))
                    throw new CryptographicException("Eski ve etkin imzalama anahtarları farklı; eski dosya korunarak işlem durduruldu.");
            }
            finally
            {
                CryptographicOperations.ZeroMemory(activePublicKey);
                CryptographicOperations.ZeroMemory(legacyPublicKey);
            }

            File.Delete(legacyPath);
            if (File.Exists(legacyPath))
                throw new IOException("Eski düz metin imzalama anahtarı kaldırılamadı.");
        }
        finally
        {
            CryptographicOperations.ZeroMemory(legacyBytes);
            Array.Clear(legacyChars);
        }
    }

    private static byte[] ReadKeyFile(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (stream.Length <= 0 || stream.Length > MaxKeyFileBytes)
            throw new InvalidDataException("Anahtar dosyasının boyutu geçersiz.");
        var bytes = new byte[(int)stream.Length];
        stream.ReadExactly(bytes);
        return bytes;
    }

    private static void EnsurePrivateDirectory()
    {
        Directory.CreateDirectory(DirectoryPath);
        var directory = new DirectoryInfo(DirectoryPath);
        if ((directory.Attributes & FileAttributes.ReparsePoint) != 0)
            throw new IOException("Anahtar dizini sembolik bağlantı olamaz.");
        using var identity = WindowsIdentity.GetCurrent();
        var acl = new DirectorySecurity(DirectoryPath, AccessControlSections.Access);
        acl.SetAccessRuleProtection(true, false);
        foreach (FileSystemAccessRule rule in acl.GetAccessRules(true, false, typeof(SecurityIdentifier)))
            acl.RemoveAccessRuleSpecific(rule);
        foreach (var sid in new[] { identity.User!, new SecurityIdentifier("S-1-5-18"), new SecurityIdentifier("S-1-5-32-544") })
            acl.AddAccessRule(new FileSystemAccessRule(sid, FileSystemRights.FullControl,
                InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));
        directory.SetAccessControl(acl);
    }

    private static void WriteAtomic(string path, byte[] content, bool overwrite)
    {
        var fullPath = Path.GetFullPath(path);
        if (File.Exists(fullPath) && (File.GetAttributes(fullPath) & FileAttributes.ReparsePoint) != 0)
            throw new IOException("Anahtar hedefi sembolik bağlantı olamaz.");
        var temporary = Path.Combine(Path.GetDirectoryName(fullPath)!, ".mkkey-" + Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                stream.Write(content);
                stream.Flush(flushToDisk: true);
            }
            File.Move(temporary, fullPath, overwrite);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    private static void EnsureOutsideRepository(string path)
    {
        var directory = new DirectoryInfo(Path.GetDirectoryName(Path.GetFullPath(path))!);
        for (var current = directory; current != null; current = current.Parent)
            if (Directory.Exists(Path.Combine(current.FullName, ".git")) || File.Exists(Path.Combine(current.FullName, ".git")))
                throw new IOException("Anahtar yedeğini Git çalışma alanına kaydetmeyin.");
    }

    private static void EnsureBackupPathIsNotKeyStore(string path)
    {
        var target = Path.GetFullPath(path);
        if (PathEquals(target, ActivePath)
            || PathEquals(target, Path.Combine(DirectoryPath, "license-signing-private.pem")))
            throw new IOException("Yedek hedefi etkin imzalama anahtarı deposuyla aynı olamaz.");
    }

    private static bool PathEquals(string left, string right)
        => string.Equals(Path.TrimEndingDirectorySeparator(Path.GetFullPath(left)),
            Path.TrimEndingDirectorySeparator(Path.GetFullPath(right)),
            OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
}
