using System.Security.Cryptography;
using System.Text;
using MKFiloServis.Shared.Entities;
using MKFiloServis.Shared.Licensing;
using MKFiloServis.Web.Services;

namespace MKFiloServis.Tests;

public sealed class LicenseValidatorTests : IDisposable
{
    private static readonly DateTime Now = new(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);
    private static readonly string Machine = MachineFingerprint.Build("os-guid", "cpu-1", "board-1", "disk-1");
    private readonly RSA _rsa = RSA.Create(2048);

    public void Dispose() => _rsa.Dispose();

    private string PublicPem => _rsa.ExportSubjectPublicKeyInfoPem();

    private LicenseInfo Signed(string machine, string modules = "cari,filoservis", DateTime? expire = null,
        string version = "0.0.0", string company = "F001", DateTime? created = null)
    {
        var lic = new LicenseInfo
        {
            FirmaKodu = company,
            MachineId = machine,
            ExpireDate = expire ?? new DateTime(2027, 6, 15),
            CreatedAt = created ?? Now.AddDays(-1),
            DurationDays = 365,
            AllowedVersion = version,
            ContactPhone = string.Empty,
            IsActive = true
        };
        var payload = LicenseModules.Payload(LicenseSignatureVerifier.Payload(lic), modules);
        var signature = _rsa.SignData(Encoding.UTF8.GetBytes(payload), HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
        lic.Signature = LicenseModules.Envelope(modules, Convert.ToBase64String(signature));
        return lic;
    }

    private LicenseValidationContext Context(string? machine = null,
        DateTime? now = null, DateTime? lastSeen = null, string? company = null, Version? version = null)
    {
        return new LicenseValidationContext
        {
            NowUtc = now ?? Now,
            MachineIds = new[] { machine ?? Machine },
            AppVersion = version ?? new Version(2, 0, 0),
            ExpectedCompanyCode = company,
            LastSeenUtc = lastSeen,
            VerifySignature = lic => LicenseSignatureVerifier.VerifyRsa(lic, new[] { PublicPem })
        };
    }

    [Fact]
    public void Valid_license_returns_signed_modules()
    {
        var result = LicenseValidator.Evaluate(Signed(Machine), Context());

        Assert.Equal(LicenseStatusCode.Ok, result.Code);
        Assert.True(result.IsValid);
        Assert.Equal(new[] { "cari", "filoservis" }, result.Modules.OrderBy(m => m));
    }

    [Fact]
    public void Missing_license_reports_not_found()
        => Assert.Equal(LicenseStatusCode.NotFound, LicenseValidator.Evaluate(null, Context()).Code);

    [Fact]
    public void Inactive_license_is_rejected()
    {
        var lic = Signed(Machine);
        lic.IsActive = false;
        Assert.Equal(LicenseStatusCode.Inactive, LicenseValidator.Evaluate(lic, Context()).Code);
    }

    [Fact]
    public void Tampered_modules_invalidate_signature()
    {
        var lic = Signed(Machine, "cari");
        var rsaPart = lic.Signature.Split(':')[2];
        lic.Signature = LicenseModules.Envelope("cari,muhasebe", rsaPart);

        Assert.Equal(LicenseStatusCode.SignatureInvalid, LicenseValidator.Evaluate(lic, Context()).Code);
    }

    [Fact]
    public void Tampered_expiry_invalidates_signature()
    {
        var lic = Signed(Machine);
        lic.ExpireDate = lic.ExpireDate.AddYears(10);
        Assert.Equal(LicenseStatusCode.SignatureInvalid, LicenseValidator.Evaluate(lic, Context()).Code);
    }

    [Fact]
    public void Signature_from_another_key_is_rejected()
    {
        using var other = RSA.Create(2048);
        var lic = Signed(Machine);
        var ctx = new LicenseValidationContext
        {
            NowUtc = Now,
            MachineIds = new[] { Machine },
            VerifySignature = l => LicenseSignatureVerifier.VerifyRsa(l, new[] { other.ExportSubjectPublicKeyInfoPem() })
        };
        Assert.Equal(LicenseStatusCode.SignatureInvalid, LicenseValidator.Evaluate(lic, ctx).Code);
    }

    [Fact]
    public void Key_rotation_accepts_signature_from_any_listed_key()
    {
        using var newer = RSA.Create(2048);
        var lic = Signed(Machine);
        var keys = new[] { newer.ExportSubjectPublicKeyInfoPem(), PublicPem };
        Assert.True(LicenseSignatureVerifier.VerifyRsa(lic, keys));
    }

    [Fact]
    public void Garbage_public_key_does_not_throw_and_next_key_is_tried()
    {
        var lic = Signed(Machine);
        Assert.True(LicenseSignatureVerifier.VerifyRsa(lic, new[] { "not a pem", PublicPem }));
        Assert.False(LicenseSignatureVerifier.VerifyRsa(lic, new[] { "not a pem" }));
    }

    [Fact]
    public void License_for_different_hardware_is_rejected()
    {
        var other = MachineFingerprint.Build("other-os", "other-cpu", "other-board", "other-disk");
        Assert.Equal(LicenseStatusCode.MachineMismatch,
            LicenseValidator.Evaluate(Signed(other), Context()).Code);
    }

    [Fact]
    public void Single_hardware_change_keeps_license_valid()
    {
        var upgraded = MachineFingerprint.Build("os-guid", "cpu-1", "board-1", "new-disk");
        Assert.True(LicenseValidator.Evaluate(Signed(Machine), Context(machine: upgraded)).IsValid);
    }

    [Fact]
    public void Legacy_machine_id_binding_is_still_accepted_with_exact_match()
    {
        const string legacy = "PC01_user_ABC123";
        var lic = Signed(legacy);

        var ctx = new LicenseValidationContext
        {
            NowUtc = Now,
            MachineIds = new[] { Machine, legacy },
            VerifySignature = l => LicenseSignatureVerifier.VerifyRsa(l, new[] { PublicPem })
        };
        Assert.True(LicenseValidator.Evaluate(lic, ctx).IsValid);
        Assert.Equal(LicenseStatusCode.MachineMismatch,
            LicenseValidator.Evaluate(lic, Context()).Code);
    }

    [Fact]
    public void Expired_license_is_rejected_after_end_of_expiry_day()
    {
        var lic = Signed(Machine, expire: new DateTime(2026, 6, 1));
        var result = LicenseValidator.Evaluate(lic, Context(now: new DateTime(2026, 6, 20, 0, 0, 0, DateTimeKind.Utc)));
        Assert.Equal(LicenseStatusCode.Expired, result.Code);
    }

    [Fact]
    public void License_is_valid_through_the_whole_expiry_day()
    {
        var lic = Signed(Machine, expire: new DateTime(2026, 6, 15));
        Assert.Equal(LicenseStatusCode.Ok,
            LicenseValidator.Evaluate(lic, Context(now: new DateTime(2026, 6, 15, 23, 30, 0, DateTimeKind.Utc))).Code);
    }

    [Fact]
    public void Renewal_grace_requires_previous_successful_validation()
    {
        var lic = Signed(Machine, expire: new DateTime(2026, 6, 10));

        Assert.Equal(LicenseStatusCode.Expired, LicenseValidator.Evaluate(lic, Context()).Code);

        lic.LastValidatedAt = Now.AddDays(-6);
        var graced = LicenseValidator.Evaluate(lic, Context());
        Assert.Equal(LicenseStatusCode.Grace, graced.Code);
        Assert.True(graced.IsValid);
    }

    [Fact]
    public void Renewal_grace_ends_after_fourteen_days()
    {
        var lic = Signed(Machine, expire: new DateTime(2026, 5, 20));
        lic.LastValidatedAt = Now.AddDays(-20);
        Assert.Equal(LicenseStatusCode.Expired, LicenseValidator.Evaluate(lic, Context()).Code);
    }

    [Fact]
    public void Clock_set_before_last_seen_time_is_detected_but_small_drift_is_tolerated()
    {
        var lic = Signed(Machine);
        Assert.Equal(LicenseStatusCode.ClockTampered,
            LicenseValidator.Evaluate(lic, Context(lastSeen: Now.AddDays(3))).Code);
        Assert.True(LicenseValidator.Evaluate(lic, Context(lastSeen: Now.AddMinutes(20))).IsValid);
    }

    [Fact]
    public void Creation_date_slightly_in_future_is_tolerated_but_far_future_is_not()
    {
        Assert.True(LicenseValidator.Evaluate(Signed(Machine, created: Now.AddHours(5)), Context()).IsValid);
        Assert.Equal(LicenseStatusCode.ClockTampered,
            LicenseValidator.Evaluate(Signed(Machine, created: Now.AddDays(10)), Context()).Code);
    }

    [Fact]
    public void Application_version_above_signed_limit_is_rejected()
    {
        var lic = Signed(Machine, version: "1.5.0");
        Assert.Equal(LicenseStatusCode.VersionNotAllowed,
            LicenseValidator.Evaluate(lic, Context(version: new Version(2, 0, 0))).Code);
        Assert.True(LicenseValidator.Evaluate(lic, Context(version: new Version(1, 4, 9))).IsValid);
    }

    [Fact]
    public void Company_code_mismatch_is_rejected_only_when_configured()
    {
        var lic = Signed(Machine, company: "F001");
        Assert.True(LicenseValidator.Evaluate(lic, Context()).IsValid);
        Assert.True(LicenseValidator.Evaluate(lic, Context(company: "f001")).IsValid);
        Assert.Equal(LicenseStatusCode.CompanyMismatch,
            LicenseValidator.Evaluate(lic, Context(company: "F002")).Code);
    }

    [Fact]
    public void Demo_license_grants_all_modules_until_it_expires()
    {
        var demo = new LicenseInfo
        {
            FirmaKodu = "DEMO", MachineId = Machine, IsDemo = true, IsActive = true,
            CreatedAt = Now.AddDays(-5), ExpireDate = Now.AddDays(10), Signature = "demo:x"
        };
        var ctx = new LicenseValidationContext { NowUtc = Now, MachineIds = new[] { Machine }, VerifySignature = _ => true };

        var ok = LicenseValidator.Evaluate(demo, ctx);
        Assert.True(ok.IsValid);
        Assert.Equal(LicenseModules.All.Count, ok.Modules.Count);

        demo.CreatedAt = Now.AddDays(-30);
        Assert.Equal(LicenseStatusCode.DemoExpired, LicenseValidator.Evaluate(demo, ctx).Code);
    }

    [Theory]
    [InlineData("  eyJ4Ijox\r\nfQ==  ", "eyJ4IjoxfQ==")]
    [InlineData("\"eyJ4IjoxfQ==\"", "eyJ4IjoxfQ==")]
    [InlineData("FirmaKodu=F001\r\nMachineId=FP2-AAAA\r\nLicenseKey=eyJ4Ijox\r\nfQ==", "eyJ4IjoxfQ==")]
    [InlineData("\uFEFFabc", "abc")]
    public void Pasted_license_text_is_normalized(string input, string expected)
        => Assert.Equal(expected, LicenseService.NormalizeKeyInput(input));
}

public sealed class MachineFingerprintTests
{
    private static string Fp(string os = "os", string cpu = "cpu", string board = "board", string disk = "disk")
        => MachineFingerprint.Build(os, cpu, board, disk);

    [Fact]
    public void Build_is_deterministic_and_well_formed()
    {
        Assert.Equal(Fp(), Fp());
        Assert.True(MachineFingerprint.TryParse(Fp(), out var parts));
        Assert.Equal(MachineFingerprint.ComponentCount, parts.Length);
        Assert.StartsWith("FP2-", Fp());
    }

    [Fact]
    public void Component_hashes_are_independent_per_slot()
    {
        Assert.NotEqual(Fp("x", "x", "x", "x").Split('-')[1], Fp("x", "x", "x", "x").Split('-')[2]);
    }

    [Fact]
    public void Parsing_ignores_case_and_whitespace()
    {
        var value = Fp();
        Assert.True(MachineFingerprint.Matches(value, " " + value.ToLowerInvariant().Replace("-", " - ") + " "));
    }

    [Fact]
    public void One_changed_component_still_matches_two_do_not()
    {
        Assert.True(MachineFingerprint.Matches(Fp(), Fp(disk: "other")));
        Assert.True(MachineFingerprint.Matches(Fp(), Fp(cpu: "other")));
        Assert.False(MachineFingerprint.Matches(Fp(), Fp(cpu: "other", disk: "other")));
    }

    [Fact]
    public void Missing_components_are_ignored_but_never_count_as_matches()
    {
        Assert.True(MachineFingerprint.Matches(Fp(), Fp(board: "")));
        Assert.False(MachineFingerprint.Matches(Fp(os: "", cpu: "", board: "", disk: ""), Fp(os: "", cpu: "", board: "", disk: "")));
        Assert.False(MachineFingerprint.Matches(Fp(board: "", disk: ""), Fp(os: "other", board: "", disk: "")));
    }

    [Fact]
    public void Placeholder_hardware_values_are_treated_as_missing()
    {
        Assert.Equal(Fp(board: ""), Fp(board: "To Be Filled By O.E.M."));
        Assert.Equal(Fp(disk: ""), Fp(disk: "00000000"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("PC_user_code")]
    [InlineData("FP2-AAAA")]
    [InlineData("FP2-GGGGGGGG-00000000-00000000-00000000")]
    public void Malformed_values_are_not_fingerprints(string value)
    {
        Assert.False(MachineFingerprint.IsFingerprint(value));
        Assert.False(MachineFingerprint.Matches(value, Fp()));
    }

    [Fact]
    public void Current_fingerprint_is_stable_within_a_process()
    {
        Assert.True(MachineFingerprint.IsFingerprint(MachineFingerprint.Current));
        Assert.Equal(MachineFingerprint.Current, MachineFingerprint.Current);
    }

    [Fact]
    public void License_identity_canonicalizes_fingerprint_but_leaves_legacy_ids()
    {
        var value = Fp();
        Assert.Equal(value, LicenseIdentity.MachineId(" " + value.ToLowerInvariant() + "\r\n"));
        Assert.Equal("PC_user_code", LicenseIdentity.MachineId(" PC_user_code "));
    }
}
