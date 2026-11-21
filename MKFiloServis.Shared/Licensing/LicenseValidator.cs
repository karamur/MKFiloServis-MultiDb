using MKFiloServis.Shared.Entities;

namespace MKFiloServis.Shared.Licensing;

public enum LicenseStatusCode
{
    Ok,
    Grace,
    NotFound,
    Inactive,
    SignatureInvalid,
    MachineMismatch,
    ClockTampered,
    Expired,
    DemoExpired,
    CompanyMismatch,
    VersionNotAllowed,
    Unavailable
}

public sealed class LicenseEvaluation
{
    public LicenseStatusCode Code { get; init; }
    public string Message { get; init; } = string.Empty;
    public IReadOnlySet<string> Modules { get; init; } = new HashSet<string>();

    public bool IsValid => Code is LicenseStatusCode.Ok or LicenseStatusCode.Grace;
    public bool InGrace => Code == LicenseStatusCode.Grace;
}

public sealed class LicenseValidationContext
{
    public DateTime NowUtc { get; init; } = DateTime.UtcNow;
    /// <summary>Bu makinenin kabul edilen kimlikleri (yeni parmak izi ve varsa eski biçim).</summary>
    public IEnumerable<string> MachineIds { get; init; } = Array.Empty<string>();
    public Version AppVersion { get; init; } = new(1, 0);
    public string? ExpectedCompanyCode { get; init; }
    /// <summary>Bu kurulumda görülen en son güvenilir zaman (DB ve yerel durum dosyasının büyüğü).</summary>
    public DateTime? LastSeenUtc { get; init; }
    public required Func<LicenseInfo, bool> VerifySignature { get; init; }
    public int DemoMaxDays { get; init; } = 15;
    public TimeSpan RenewalGrace { get; init; } = TimeSpan.FromDays(14);
    public TimeSpan ClockTolerance { get; init; } = TimeSpan.FromHours(1);
    /// <summary>Lisansı üreten bilgisayarın saati ileride olabilir; oluşturma tarihi için daha geniş tolerans.</summary>
    public TimeSpan CreatedAtTolerance { get; init; } = TimeSpan.FromDays(1);
}

/// <summary>
/// Lisans politikası: veritabanı, dosya veya HTTP bağımlılığı olmayan saf karar mantığı.
/// Kontrol sırası güvenilirlik sırasıdır; imza geçerli olmadan diğer alanlara güvenilmez.
/// </summary>
public static class LicenseValidator
{
    public static LicenseEvaluation Evaluate(LicenseInfo? lic, LicenseValidationContext ctx)
    {
        if (lic is null)
            return Fail(LicenseStatusCode.NotFound, "Lisans bulunamadı. Lisans anahtarını yükleyin.");
        if (!lic.IsActive || lic.IsDeleted)
            return Fail(LicenseStatusCode.Inactive, "Lisans aktif değil. Lisans anahtarını yeniden yükleyin.");

        if (!ctx.VerifySignature(lic))
            return Fail(LicenseStatusCode.SignatureInvalid,
                "Lisans imzası geçersiz. Lisans bozulmuş veya değiştirilmiş olabilir; anahtarı yeniden yükleyin.");

        if (!MachineAllowed(lic.MachineId, ctx.MachineIds))
            return Fail(LicenseStatusCode.MachineMismatch,
                "Lisans bu bilgisayar için geçerli değil. Bu bilgisayarın makine koduyla yeni lisans alın.");

        var now = ctx.NowUtc;
        if (now < lic.CreatedAt - ctx.CreatedAtTolerance)
            return Fail(LicenseStatusCode.ClockTampered,
                "Sistem saati lisans oluşturma tarihinden önce görünüyor. Tarih ve saat ayarlarını düzeltin.");
        if (ctx.LastSeenUtc is { } lastSeen && lastSeen - now > ctx.ClockTolerance)
            return Fail(LicenseStatusCode.ClockTampered,
                "Sistem saati son lisans kontrolünden önceye alınmış. Tarih ve saat ayarlarını düzeltin.");

        if (lic.IsDemo)
        {
            if ((now - lic.CreatedAt).TotalDays > ctx.DemoMaxDays || now > lic.ExpireDate)
                return Fail(LicenseStatusCode.DemoExpired, $"Demo süresi doldu ({lic.ExpireDate:yyyy-MM-dd}). Lisans anahtarı yükleyin.");
        }

        var expiry = EffectiveExpiry(lic);
        var inGrace = false;
        if (now > expiry)
        {
            // Yalnız ticari ve daha önce başarıyla doğrulanmış lisanslar yenileme toleransı alır.
            inGrace = !lic.IsDemo && lic.LastValidatedAt.HasValue && now - expiry <= ctx.RenewalGrace;
            if (!inGrace)
                return Fail(LicenseStatusCode.Expired, $"Lisans süresi doldu ({lic.ExpireDate:yyyy-MM-dd}). Yenileme için lisans sağlayıcınıza başvurun.");
        }

        if (!lic.IsDemo && !string.IsNullOrWhiteSpace(ctx.ExpectedCompanyCode) &&
            !string.Equals(lic.FirmaKodu, ctx.ExpectedCompanyCode, StringComparison.OrdinalIgnoreCase))
            return Fail(LicenseStatusCode.CompanyMismatch,
                $"Firma kodu uyuşmuyor: lisans '{lic.FirmaKodu}', yapılandırma '{ctx.ExpectedCompanyCode}'.");

        if (!LicenseVersionPolicy.Allows(lic.AllowedVersion, ctx.AppVersion))
            return Fail(LicenseStatusCode.VersionNotAllowed,
                $"Uygulama sürümü ({ctx.AppVersion}) lisansa dahil değil. İzin verilen en yüksek sürüm: {lic.AllowedVersion}.");

        return new LicenseEvaluation
        {
            Code = inGrace ? LicenseStatusCode.Grace : LicenseStatusCode.Ok,
            Message = inGrace ? "Lisans süresi doldu; yenileme toleransı sürüyor." : string.Empty,
            Modules = ResolveModules(lic)
        };
    }

    /// <summary>Ticari lisans bitiş gününün sonuna kadar geçerlidir; demo tam zaman damgasına bağlıdır.</summary>
    public static DateTime EffectiveExpiry(LicenseInfo lic)
        => lic.IsDemo ? lic.ExpireDate : lic.ExpireDate.Date.AddDays(1);

    /// <summary>Makine kimliği yeni parmak izi ise toleranslı, eski biçimdeyse birebir karşılaştırılır.</summary>
    public static bool MachineAllowed(string? licensedMachineId, IEnumerable<string> currentMachineIds)
    {
        foreach (var current in currentMachineIds)
        {
            if (MachineFingerprint.IsFingerprint(licensedMachineId))
            {
                if (MachineFingerprint.Matches(licensedMachineId, current)) return true;
            }
            else if (LicenseIdentity.MatchesMachine(licensedMachineId, current))
            {
                return true;
            }
        }
        return false;
    }

    private static IReadOnlySet<string> ResolveModules(LicenseInfo lic)
    {
        if (lic.IsDemo) return new HashSet<string>(LicenseModules.All.Keys, StringComparer.Ordinal);
        return LicenseSignatureVerifier.ReadModules(lic) ?? new HashSet<string>(StringComparer.Ordinal);
    }

    private static LicenseEvaluation Fail(LicenseStatusCode code, string message)
        => new() { Code = code, Message = message };
}
