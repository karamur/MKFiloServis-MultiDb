using System.Security.Cryptography;
using System.Text;

namespace MKFiloServis.Web.Helpers;

public static class TwoFactorAuthenticatorHelper
{
    private const string Base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
    private const int SecretSize = 20;
    private const int TimeStepSeconds = 30;

    public static string GenerateSecretKey()
    {
        var bytes = new byte[SecretSize];
        RandomNumberGenerator.Fill(bytes);
        return Base32Encode(bytes);
    }

    public static string FormatManualEntryKey(string secretKey)
    {
        return string.Join(" ", secretKey.Chunk(4).Select(chunk => new string(chunk)));
    }

    public static string BuildSetupUri(string issuer, string accountName, string secretKey)
    {
        var encodedIssuer = Uri.EscapeDataString(issuer);
        var encodedAccount = Uri.EscapeDataString(accountName);
        return $"otpauth://totp/{encodedIssuer}:{encodedAccount}?secret={secretKey}&issuer={encodedIssuer}&algorithm=SHA1&digits=6&period={TimeStepSeconds}";
    }

    public static bool ValidateCode(string secretKey, string code)
    {
        if (string.IsNullOrWhiteSpace(secretKey) || string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        var normalizedCode = new string(code.Where(char.IsDigit).ToArray());
        if (normalizedCode.Length != 6)
        {
            return false;
        }

        var secretBytes = Base32Decode(secretKey);
        var currentStep = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / TimeStepSeconds;

        for (var offset = -1; offset <= 1; offset++)
        {
            var expected = ComputeTotp(secretBytes, currentStep + offset);
            if (string.Equals(expected, normalizedCode, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static string ComputeTotp(byte[] secretBytes, long timestepNumber)
    {
        Span<byte> timestepBytes = stackalloc byte[8];
        BitConverter.TryWriteBytes(timestepBytes, timestepNumber);
        if (BitConverter.IsLittleEndian)
        {
            timestepBytes.Reverse();
        }

        using var hmac = new HMACSHA1(secretBytes);
        var hash = hmac.ComputeHash(timestepBytes.ToArray());
        var offset = hash[^1] & 0x0F;
        var binaryCode = ((hash[offset] & 0x7F) << 24)
                         | (hash[offset + 1] << 16)
                         | (hash[offset + 2] << 8)
                         | hash[offset + 3];

        return (binaryCode % 1_000_000).ToString("D6");
    }

    private static string Base32Encode(byte[] data)
    {
        var output = new StringBuilder((data.Length + 4) / 5 * 8);
        var bitBuffer = 0;
        var bitsInBuffer = 0;

        foreach (var value in data)
        {
            bitBuffer = (bitBuffer << 8) | value;
            bitsInBuffer += 8;

            while (bitsInBuffer >= 5)
            {
                output.Append(Base32Alphabet[(bitBuffer >> (bitsInBuffer - 5)) & 0x1F]);
                bitsInBuffer -= 5;
            }
        }

        if (bitsInBuffer > 0)
        {
            output.Append(Base32Alphabet[(bitBuffer << (5 - bitsInBuffer)) & 0x1F]);
        }

        return output.ToString();
    }

    private static byte[] Base32Decode(string input)
    {
        var normalized = input.Trim().Replace(" ", string.Empty).TrimEnd('=').ToUpperInvariant();
        var output = new List<byte>();
        var bitBuffer = 0;
        var bitsInBuffer = 0;

        foreach (var c in normalized)
        {
            var value = Base32Alphabet.IndexOf(c);
            if (value < 0)
            {
                throw new FormatException("Geçersiz base32 anahtarı.");
            }

            bitBuffer = (bitBuffer << 5) | value;
            bitsInBuffer += 5;

            if (bitsInBuffer >= 8)
            {
                output.Add((byte)((bitBuffer >> (bitsInBuffer - 8)) & 0xFF));
                bitsInBuffer -= 8;
            }
        }

        return output.ToArray();
    }
}


