using MKFiloServis.Web.Services;

namespace MKFiloServis.Tests;

public sealed class JwtSecretPolicyTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("REPLACE_WITH_A_REAL_SECRET_VALUE_0123456789")]
    [InlineData("too-short")]
    public void Missing_placeholder_or_short_signing_secret_is_rejected(string? secret)
        => Assert.NotNull(JwtSecretPolicy.GetValidationError(secret));

    [Fact]
    public void Random_production_length_secret_is_accepted()
    {
        var secret = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(48));
        Assert.Null(JwtSecretPolicy.GetValidationError(secret));
    }

    [Fact]
    public void Known_compromised_key_fingerprint_is_blocked_without_storing_the_key()
    {
        Assert.True(JwtSecretPolicy.IsCompromisedFingerprint(
            "F96583977D82A77C0DC5DEE3B5EBB60911C9380D662EF7DC24EEC50828B397BC"));
        Assert.False(JwtSecretPolicy.IsCompromisedFingerprint(new string('0', 64)));
    }
}
