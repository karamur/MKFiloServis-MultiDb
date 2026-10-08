using System.Security.Cryptography;
using System.Text;
using MKFiloServis.Shared.Licensing;

namespace MKFiloServis.Tests;

public sealed class LicenseProtocolTests
{
    [Fact]
    public void Module_envelope_uses_canonical_order_and_deduplicates_selection()
    {
        var envelope = LicenseModules.Envelope("personel,cari,personel", "signature");

        Assert.True(LicenseModules.TryRead(envelope, out var modules, out var signature));
        Assert.Equal("cari,personel", modules);
        Assert.Equal("signature", signature);
    }

    [Theory]
    [InlineData("v3:cGVyc29uZWwsY2FyaQ==:signature")] // noncanonical order
    [InlineData("v3:Y2FyaSxjYXJp:signature")] // duplicate module
    [InlineData("v3:dW5rbm93bg==:signature")] // unknown module
    [InlineData("v2:Y2FyaQ==:signature")]
    [InlineData("v3:broken:signature")]
    public void Invalid_module_envelopes_are_rejected(string envelope)
    {
        Assert.False(LicenseModules.TryRead(envelope, out _, out _));
    }

    [Fact]
    public void Rsa_signature_binds_selected_modules_to_license_payload()
    {
        var licensePayload = LicenseSignaturePayload.Create(
            "FIRMA", "MACHINE", new DateTime(2027, 1, 1), 365,
            false, "2.3.0", new DateTime(2026, 1, 1), "5551234567");
        using var rsa = RSA.Create(2048);
        var signedPayload = LicenseModules.Payload(licensePayload, "cari,personel");
        var signature = rsa.SignData(Encoding.UTF8.GetBytes(signedPayload),
            HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
        var envelope = LicenseModules.Envelope("personel,cari", Convert.ToBase64String(signature));

        Assert.True(LicenseModules.TryRead(envelope, out var modules, out var encodedSignature));
        Assert.True(rsa.VerifyData(Encoding.UTF8.GetBytes(LicenseModules.Payload(licensePayload, modules)),
            Convert.FromBase64String(encodedSignature), HashAlgorithmName.SHA256, RSASignaturePadding.Pss));
        Assert.False(rsa.VerifyData(Encoding.UTF8.GetBytes(LicenseModules.Payload(licensePayload, "cari,personel,stok")),
            Convert.FromBase64String(encodedSignature), HashAlgorithmName.SHA256, RSASignaturePadding.Pss));
    }

    [Theory]
    [InlineData("1.2.3", "1.2.3", true)]
    [InlineData("1.2.3", "1.2.4", false)]
    [InlineData("0.0.0", "99.0.0", true)]
    [InlineData("1.2.3", "1.2-beta", false)]
    [InlineData("1.2-beta", "1.2.3", false)]
    public void Signed_version_limit_controls_requested_release(string maximum, string requested, bool expected)
    {
        Assert.Equal(expected, LicenseVersionPolicy.Allows(maximum, requested));
    }
}
