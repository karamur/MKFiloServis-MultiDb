using MKFiloServis.Shared.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using MKFiloServis.Web.Services;

namespace MKFiloServis.Tests;

public sealed class DatabaseProviderScopeTests
{
    [Theory]
    [InlineData(DatabaseProvider.PostgreSQL, true)]
    [InlineData(DatabaseProvider.SQLite, true)]
    [InlineData(DatabaseProvider.SQLServer, false)]
    [InlineData(DatabaseProvider.MySQL, false)]
    public void Database_settings_only_advertises_supported_runtime_providers(
        DatabaseProvider provider,
        bool expected)
    {
        Assert.Equal(expected, new DatabaseSettings { Provider = provider }.UsesSupportedRuntimeProvider());
    }

    [Theory]
    [InlineData(DatabaseProvider.SQLServer)]
    [InlineData(DatabaseProvider.MySQL)]
    public async Task Settings_service_rejects_unsupported_provider_without_writing_settings(
        DatabaseProvider provider)
    {
        var root = Path.Combine(Path.GetTempPath(), "mkfiloservis-provider-scope-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var service = new DatabaseSettingsService(
                new ConfigurationBuilder().Build(),
                new TestEnvironment(root));
            var settings = new DatabaseSettings { Provider = provider };

            var connection = await service.TestConnectionAsync(settings);
            var apply = await service.ApplyConnectionAsync(settings);

            Assert.False(connection.Success);
            Assert.Contains("migration desteği", connection.Message, StringComparison.OrdinalIgnoreCase);
            Assert.False(apply.Success);
            Assert.False(File.Exists(Path.Combine(root, "dbsettings.json")));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private sealed class TestEnvironment(string root) : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "MKFiloServis.Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = Path.Combine(root, "wwwroot");
        public string EnvironmentName { get; set; } = "Test";
        public string ContentRootPath { get; set; } = root;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
