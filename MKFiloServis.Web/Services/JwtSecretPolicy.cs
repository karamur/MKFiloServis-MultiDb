using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.EnvironmentVariables;

namespace MKFiloServis.Web.Services;

/// <summary>Validates signing secrets identically at host startup and token issuance.</summary>
public static class JwtSecretPolicy
{
    private const string CompromisedAppSettingsFingerprint =
        "F96583977D82A77C0DC5DEE3B5EBB60911C9380D662EF7DC24EEC50828B397BC";
    private const string CompromisedPreProductionFingerprint =
        "0764D8425695267BDB5B744647385B32D7930B585788773DDA7FD29BCF069BBD";
    private const string CompromisedKoaAppSettingsFingerprint =
        "BB8A434D91A44C9B2B835A0F2595216A4182AEE6DA2C2B01622D4659EEBAE181";
    private const string CompromisedCrmAppSettingsFingerprint =
        "43E415844495C061C48EA2737D308C06A0DE643270E6BB9AA3FCBF1E3BF80865";

    public static string? GetValidationError(string? secret)
    {
        if (string.IsNullOrWhiteSpace(secret))
            return "JWT Secret yapılandırılmamış.";
        if (secret.StartsWith("REPLACE_", StringComparison.OrdinalIgnoreCase))
            return "JWT Secret örnek/yer tutucu değer olamaz.";
        if (Encoding.UTF8.GetByteCount(secret) < 32)
            return "JWT Secret en az 32 bayt olmalıdır.";

        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(secret)));
        if (IsCompromisedFingerprint(fingerprint))
            return "JWT Secret ifşa edilmiş eski anahtardır; yeni ve rastgele bir secret belirleyin.";

        return null;
    }

    public static bool IsCompromisedFingerprint(string fingerprint)
        => string.Equals(fingerprint, CompromisedAppSettingsFingerprint, StringComparison.Ordinal) ||
           string.Equals(fingerprint, CompromisedPreProductionFingerprint, StringComparison.Ordinal) ||
           string.Equals(fingerprint, CompromisedKoaAppSettingsFingerprint, StringComparison.Ordinal) ||
           string.Equals(fingerprint, CompromisedCrmAppSettingsFingerprint, StringComparison.Ordinal);

    public static string EnsureValid(string? secret)
    {
        var error = GetValidationError(secret);
        if (error != null)
            throw new InvalidOperationException(error);
        return secret!;
    }

    public static string GetProductionSecret(IConfiguration configuration)
    {
        if (configuration is not IConfigurationRoot root)
            throw new InvalidOperationException("Production JWT Secret kaynağı doğrulanamadı.");

        var providers = root.Providers.Reverse().ToArray();
        IConfigurationProvider? effectiveProvider = null;
        string? effectiveValue = null;

        foreach (var provider in providers)
        {
            if (!provider.TryGet("Jwt:Secret", out var value))
                continue;

            effectiveProvider = provider;
            effectiveValue = value;
            break;
        }

        if (effectiveProvider is not EnvironmentVariablesConfigurationProvider)
            throw new InvalidOperationException(
                "Production JWT Secret yalnız korumalı ortam değişkeninden alınabilir; JSON veya komut satırı kullanmayın.");

        // A higher-priority environment value must not conceal an old secret left in
        // appsettings or command-line configuration on the target machine.
        foreach (var provider in providers)
        {
            if (provider is EnvironmentVariablesConfigurationProvider ||
                !provider.TryGet("Jwt:Secret", out var shadowedValue) ||
                string.IsNullOrWhiteSpace(shadowedValue))
                continue;

            throw new InvalidOperationException(
                "Production yapılandırmasında ortam değişkeni dışında JWT Secret bulundu; dosya veya argümandaki değeri kaldırın.");
        }

        return EnsureValid(effectiveValue);
    }
}
