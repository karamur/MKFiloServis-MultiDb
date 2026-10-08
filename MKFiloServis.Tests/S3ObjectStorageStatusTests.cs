using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using MKFiloServis.Web.Services;

namespace MKFiloServis.Tests;

public sealed class S3ObjectStorageStatusTests
{
    [Fact]
    public async Task Only_not_found_is_treated_as_a_missing_object()
    {
        using var notFound = CreateService(HttpStatusCode.NotFound);
        Assert.Null(await notFound.Service.DownloadAsync("document.enc"));
        Assert.False(await notFound.Service.ExistsAsync("document.enc"));
        await notFound.Service.DeleteAsync("document.enc");

        foreach (var status in new[] { HttpStatusCode.Forbidden, HttpStatusCode.InternalServerError })
        {
            using var failure = CreateService(status);
            await Assert.ThrowsAsync<HttpRequestException>(() => failure.Service.DownloadAsync("document.enc"));
            await Assert.ThrowsAsync<HttpRequestException>(() => failure.Service.ExistsAsync("document.enc"));
            await Assert.ThrowsAsync<HttpRequestException>(() => failure.Service.DeleteAsync("document.enc"));
        }
    }

    [Fact]
    public async Task Successful_download_returns_bytes_and_unsigned_url_is_rejected()
    {
        using var success = CreateService(HttpStatusCode.OK);
        Assert.Equal([1, 2, 3], await success.Service.DownloadAsync("document.enc"));
        Assert.True(await success.Service.ExistsAsync("document.enc"));
        await Assert.ThrowsAsync<NotSupportedException>(() =>
            success.Service.GetPresignedUrlAsync("document.enc"));
    }

    private static Fixture CreateService(HttpStatusCode status)
    {
        var client = new HttpClient(new StatusHandler(status));
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Storage:S3:AccessKey"] = "test-access",
            ["Storage:S3:SecretKey"] = "test-secret",
            ["Storage:S3:BucketName"] = "test-bucket",
            ["Storage:S3:ServiceUrl"] = "https://s3.example.invalid"
        }).Build();
        var service = new S3ObjectStorageService(new StaticClientFactory(client), config,
            NullLogger<S3ObjectStorageService>.Instance);
        return new Fixture(client, service);
    }

    private sealed record Fixture(HttpClient Client, S3ObjectStorageService Service) : IDisposable
    {
        public void Dispose() => Client.Dispose();
    }

    private sealed class StaticClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class StatusHandler(HttpStatusCode status) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken) => Task.FromResult(new HttpResponseMessage(status)
        {
            Content = new ByteArrayContent([1, 2, 3])
        });
    }
}
