using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using MKFiloServis.Shared.Entities;
using MKFiloServis.Shared.Licensing;
using MKFiloServis.Web.Data;
using MKFiloServis.Web.Services;

namespace MKFiloServis.Tests;

public sealed class LicenseActivationSqliteTests
{
    [Theory]
    [InlineData("company")]
    [InlineData("clock")]
    [InlineData("stored-signature")]
    public async Task Rejected_activation_preserves_previous_license_and_cache(string failure)
    {
        await using var fixture = await Fixture.CreateAsync();
        if (failure == "company") fixture.Configuration["FirmaKodu"] = "OTHER";
        await using (var db = fixture.Factory.CreateDbContext())
        {
            if (failure == "clock")
            {
                (await db.LicenseInfos.IgnoreQueryFilters().SingleAsync()).LastValidatedAt = DateTime.UtcNow.AddDays(2);
                await db.SaveChangesAsync();
            }
            if (failure == "stored-signature")
                await db.Database.ExecuteSqlRawAsync("""
                    CREATE TRIGGER CorruptNewLicense AFTER INSERT ON LicenseInfos
                    BEGIN UPDATE LicenseInfos SET Signature = 'corrupted' WHERE Id = NEW.Id; END;
                    """);
        }

        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.ActivateFromKeyAsync(fixture.NewKey));

        await using var verify = fixture.Factory.CreateDbContext();
        var previous = await verify.LicenseInfos.IgnoreQueryFilters().SingleAsync();
        Assert.True(previous.IsActive);
        Assert.Equal(fixture.Previous.Signature, previous.Signature);
        Assert.Same(fixture.Previous, fixture.Cache.GetValidated());
        Assert.True(fixture.Cache.HasValidLicense());
    }

    [Fact]
    public async Task Activation_is_idempotent_and_revalidates_after_restart_without_local_state_writes()
    {
        await using var fixture = await Fixture.CreateAsync();
        var first = await fixture.Service.ActivateFromKeyAsync(fixture.NewKey);
        var second = await fixture.Service.ActivateFromKeyAsync(fixture.NewKey);
        Assert.Equal(first.Id, second.Id);
        Assert.Equal(first.Signature, fixture.Cache.GetValidated()!.Signature);
        await using (var db = fixture.Factory.CreateDbContext())
        {
            Assert.Equal(2, await db.LicenseInfos.IgnoreQueryFilters().CountAsync());
            Assert.Equal(1, await db.LicenseInfos.IgnoreQueryFilters().CountAsync(l => l.IsActive));
        }

        var restartedCache = new LicenseCache();
        var restarted = fixture.CreateService(restartedCache);
        var result = await restarted.ValidateAsync(persistValidation: false);
        Assert.True(result.IsValid);
        Assert.Equal(first.Signature, restartedCache.GetValidated()!.Signature);
        Assert.Empty(restartedCache.GetSnapshot()!.Modules); // Legacy rights still require an explicit v3 reissue.
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task Development_module_authorization_respects_enforcement_setting(bool enforce, bool expected)
    {
        await using var fixture = await Fixture.CreateAsync();
        fixture.Environment.EnvironmentName = "Development";
        fixture.Configuration["Licensing:EnforceInDevelopment"] = enforce.ToString();
        fixture.Cache.Clear();
        var requirement = new LicenseModuleRequirement("cari");
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "test") }, "Test"));
        var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);
        await new LicenseModuleAuthorizationHandler(fixture.Service).HandleAsync(context);
        Assert.Equal(expected, context.HasSucceeded);
        if (!enforce)
        {
            await fixture.Service.ValidateAsync();
            Assert.True(fixture.Cache.HasValidLicense());
            Assert.Equal(LicenseModules.All.Count, fixture.Cache.GetSnapshot()!.Modules.Count);
        }
    }

    [Fact]
    public async Task Database_revocation_is_applied_on_next_explicit_validation()
    {
        await using var fixture = await Fixture.CreateAsync();
        await using (var db = fixture.Factory.CreateDbContext())
        {
            (await db.LicenseInfos.IgnoreQueryFilters().SingleAsync()).IsActive = false;
            await db.SaveChangesAsync();
        }
        var notifications = 0;
        fixture.Cache.Changed += () => notifications++;

        Assert.True(fixture.Service.HasModulePermission("cari"));
        Assert.Equal(LicenseStatusCode.Ok, fixture.Service.EvaluateCachedLicense().Code);
        Assert.Equal(0, notifications);

        var result = await fixture.Service.ValidateAsync(persistValidation: false);
        Assert.Equal(LicenseStatusCode.Inactive, result.Code);
        Assert.False(fixture.Cache.HasValidLicense());
        Assert.False(fixture.Service.HasModulePermission("cari"));
        Assert.Null(fixture.Cache.GetSnapshot());
        await fixture.Service.GetCurrentLicenseAsync();
        Assert.Equal(LicenseStatusCode.Inactive, fixture.Service.EvaluateCachedLicense().Code);
        Assert.Equal(1, notifications);
    }

    [Fact]
    public async Task Activation_and_revalidation_do_not_race()
    {
        await using var fixture = await Fixture.CreateAsync();
        await Task.WhenAll(
            fixture.Service.ActivateFromKeyAsync(fixture.NewKey),
            fixture.Service.ValidateAsync(persistValidation: false),
            fixture.Service.ActivateFromKeyAsync(fixture.NewKey));
        await using var db = fixture.Factory.CreateDbContext();
        var active = await db.LicenseInfos.IgnoreQueryFilters().SingleAsync(l => l.IsActive);
        Assert.Equal(active.Signature, fixture.Cache.GetValidated()!.Signature);
        Assert.Equal(2, await db.LicenseInfos.IgnoreQueryFilters().CountAsync());
    }

    [Fact]
    public void Cache_only_notifies_when_validation_result_changes()
    {
        var cache = new LicenseCache();
        var notifications = 0;
        cache.Changed += () => notifications++;
        var license = new LicenseInfo { Signature = "same" };
        cache.Set(license, new LicenseEvaluation { Code = LicenseStatusCode.Ok });
        cache.Set(license, new LicenseEvaluation { Code = LicenseStatusCode.Ok });
        Assert.Equal(1, notifications);
        cache.Set(license, new LicenseEvaluation { Code = LicenseStatusCode.Grace });
        Assert.Equal(2, notifications);
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private const string Secret = "isolated-test-only-legacy-verification";
        private readonly SqliteConnection connection = new("Data Source=:memory:");
        public ConfigurationManager Configuration { get; } = new();
        public TestEnvironment Environment { get; } = new();
        public LicenseCache Cache { get; } = new();
        public Factory Factory { get; private set; } = null!;
        public LicenseService Service { get; private set; } = null!;
        public LicenseInfo Previous { get; private set; } = null!;
        public string NewKey { get; private set; } = string.Empty;

        public static async Task<Fixture> CreateAsync()
        {
            var fixture = new Fixture();
            await fixture.connection.OpenAsync();
            fixture.Factory = new Factory(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(fixture.connection).Options);
            fixture.Configuration["Licensing:LegacyVerificationSecret"] = Secret;
            fixture.Configuration["Licensing:LegacyAcceptUntilUtc"] = DateTimeOffset.UtcNow.AddDays(10).ToString("O");
            fixture.Service = fixture.CreateService(fixture.Cache);
            fixture.Previous = CreateLicense("old");
            await using var db = fixture.Factory.CreateDbContext();
            await db.Database.EnsureCreatedAsync();
            var organization = new Organizasyon { Adi = "Test", Kod = "TEST" };
            db.Organizasyonlar.Add(organization);
            await db.SaveChangesAsync();
            var company = new Firma { FirmaKodu = "F001", FirmaAdi = "Test", Aktif = true, OrganizasyonId = organization.Id };
            db.Firmalar.Add(company);
            await db.SaveChangesAsync();
            fixture.Previous.FirmaId = company.Id;
            db.LicenseInfos.Add(fixture.Previous);
            await db.SaveChangesAsync();
            fixture.Cache.Set(fixture.Previous, new LicenseEvaluation
            {
                Code = LicenseStatusCode.Ok,
                Modules = new HashSet<string> { "cari" }
            });
            fixture.NewKey = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(CreateLicense("new"))));
            return fixture;
        }

        public LicenseService CreateService(LicenseCache cache) => new(Factory, Configuration, NullLogger<LicenseService>.Instance,
            cache, new DatabaseRuntimeInfo { Provider = DatabaseProvider.SQLite }, new UnavailableStateProtection(), Environment);

        private static LicenseInfo CreateLicense(string phone)
        {
            var license = new LicenseInfo
            {
                FirmaKodu = "F001", MachineId = LicenseService.GetMachineId(),
                CreatedAt = DateTime.UtcNow.AddDays(-1), ExpireDate = DateTime.UtcNow.AddDays(30).Date,
                DurationDays = 31, AllowedVersion = "0.0.0", ContactPhone = phone, LastValidatedAt = DateTime.UtcNow
            };
            var payload = $"{license.FirmaKodu}|{license.MachineId}|{license.ExpireDate:yyyy-MM-dd}|{license.DurationDays}|{license.IsDemo}|{license.AllowedVersion}|{license.CreatedAt:yyyy-MM-dd}|{license.ContactPhone}";
            license.Signature = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(payload + "|" + Secret)));
            return license;
        }

        public ValueTask DisposeAsync() => connection.DisposeAsync();
    }

    private sealed class Factory(DbContextOptions<ApplicationDbContext> options) : IDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext() => new(options);
        public Task<ApplicationDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(CreateDbContext());
    }

    private sealed class TestEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "MKFiloServis.Tests";
        public string EnvironmentName { get; set; } = "Production";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class UnavailableStateProtection : IDataProtectionProvider, IDataProtector
    {
        public IDataProtector CreateProtector(string purpose) => this;
        public byte[] Protect(byte[] plaintext) => throw new CryptographicException("Isolated test: no host state writes.");
        public byte[] Unprotect(byte[] protectedData) => throw new CryptographicException("Isolated test: no host state reads.");
    }
}
