using System.Security.Cryptography;

namespace KOAFiloServis.Web.Services.Security;

/// <summary>
/// AES-256-GCM tabanli dosya koruyucu.
///
/// Cikti formati (basit, ileri-uyumlu):
///   MAGIC (4) = "KOA1" | VERSION (1) = 0x01 | NONCE (12) | TAG (16) | CIPHER (n)
///
/// - Her cagri icin yeni, kriptografik rastgele nonce (12 B) uretilir.
/// - Authentication tag (16 B) otomatik olarak tutulur; bozuk/degistirilmis
///   dosyalarda <see cref="CryptographicException"/> atilir.
/// - Master key <see cref="IMasterKeyProvider"/>'dan gelir (32 B).
/// </summary>
public sealed class AesGcmFileProtector : IFileProtector
{
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private const int HeaderSize = 4 + 1 + NonceSize + TagSize; // MAGIC + VER + NONCE + TAG
    private static readonly byte[] Magic = "KOA1"u8.ToArray();
    private const byte Version = 0x01;

    private readonly IMasterKeyProvider _keyProvider;

    public AesGcmFileProtector(IMasterKeyProvider keyProvider)
    {
        _keyProvider = keyProvider;
    }

    public byte[] Protect(ReadOnlySpan<byte> plain)
    {
        var key = _keyProvider.GetMasterKey().Span;
        var output = new byte[HeaderSize + plain.Length];
        var span = output.AsSpan();

        // Header
        Magic.CopyTo(span[..4]);
        span[4] = Version;
        var nonce = span.Slice(5, NonceSize);
        RandomNumberGenerator.Fill(nonce);
        var tag = span.Slice(5 + NonceSize, TagSize);
        var cipher = span[HeaderSize..];

        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, plain, cipher, tag);
        return output;
    }

    public byte[] Unprotect(ReadOnlySpan<byte> cipher)
    {
        const int MinPayload = 4 + NonceSize + TagSize + 1;

        if (cipher.Length < MinPayload)
            throw new CryptographicException("Sifreli veri cok kucuk (header eksik).");

        if (!cipher[..4].SequenceEqual(Magic))
            throw new CryptographicException("Gecersiz dosya formati (magic number uyusmuyor).");

        var key = _keyProvider.GetMasterKey().Span;

        static byte[] DecryptOrThrow(ReadOnlySpan<byte> key, ReadOnlySpan<byte> nonce, ReadOnlySpan<byte> body, ReadOnlySpan<byte> tag)
        {
            var plain = new byte[body.Length];
            using var aes = new AesGcm(key, TagSize);
            aes.Decrypt(nonce, body, tag, plain);
            return plain;
        }

        // 1) Yeni format (v1): KOA1 | VER | NONCE | TAG | CIPHER
        if (cipher.Length >= HeaderSize && cipher[4] == Version)
        {
            try
            {
                var nonce = cipher.Slice(5, NonceSize);
                var tag = cipher.Slice(5 + NonceSize, TagSize);
                var body = cipher[HeaderSize..];
                return DecryptOrThrow(key, nonce, body, tag);
            }
            catch (CryptographicException)
            {
                // Legacy format denemelerine devam
            }
        }

        // 2) Legacy KOA1 (versiyonsuz): KOA1 | NONCE | TAG | CIPHER
        try
        {
            var nonce = cipher.Slice(4, NonceSize);
            var tag = cipher.Slice(4 + NonceSize, TagSize);
            var body = cipher[(4 + NonceSize + TagSize)..];
            return DecryptOrThrow(key, nonce, body, tag);
        }
        catch (CryptographicException)
        {
            // Sonraki legacy düzeni dene
        }

        // 3) Legacy KOA1 (versiyonsuz): KOA1 | NONCE | CIPHER | TAG
        try
        {
            var nonce = cipher.Slice(4, NonceSize);
            var tag = cipher[^TagSize..];
            var body = cipher.Slice(4 + NonceSize, cipher.Length - (4 + NonceSize + TagSize));
            return DecryptOrThrow(key, nonce, body, tag);
        }
        catch (CryptographicException)
        {
            // Sonraki legacy düzeni dene
        }

        // 4) KOA1+VER varyantı: KOA1 | VER | NONCE | CIPHER | TAG
        if (cipher.Length >= HeaderSize && cipher[4] == Version)
        {
            try
            {
                var nonce = cipher.Slice(5, NonceSize);
                var tag = cipher[^TagSize..];
                var body = cipher.Slice(5 + NonceSize, cipher.Length - (5 + NonceSize + TagSize));
                return DecryptOrThrow(key, nonce, body, tag);
            }
            catch (CryptographicException)
            {
                // tum formatlar basarisiz
            }
        }

        throw new CryptographicException("Dosya decrypt edilemedi: desteklenen KOA1 formatları veya anahtar eşleşmiyor.");
    }

    public void ProtectFile(string plainPath, string cipherPath)
    {
        var plain = File.ReadAllBytes(plainPath);
        var cipher = Protect(plain);
        var dir = Path.GetDirectoryName(cipherPath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }
        File.WriteAllBytes(cipherPath, cipher);
    }

    public void UnprotectFile(string cipherPath, string plainPath)
    {
        var cipher = File.ReadAllBytes(cipherPath);
        var plain = Unprotect(cipher);
        var dir = Path.GetDirectoryName(plainPath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }
        File.WriteAllBytes(plainPath, plain);
    }
}
