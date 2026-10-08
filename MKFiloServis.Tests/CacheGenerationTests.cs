using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MKFiloServis.Web.Services;

namespace MKFiloServis.Tests;

public sealed class CacheGenerationTests
{
    [Fact]
    public async Task Another_instance_invalidates_values_it_did_not_write()
    {
        var backend = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        var writer = new CacheService(backend, NullLogger<CacheService>.Instance);
        await writer.SetAsync("vehicles:firm:1", "before-transfer");
        var otherNode = new CacheService(backend, NullLogger<CacheService>.Instance);
        Assert.Equal("before-transfer", await otherNode.GetAsync<string>("vehicles:firm:1"));

        await otherNode.RemoveByPrefixAsync("vehicles:");
        Assert.Null(await writer.GetAsync<string>("vehicles:firm:1"));
        Assert.False(await writer.ExistsAsync("vehicles:firm:1"));
        await otherNode.SetAsync("vehicles:firm:1", "after-transfer");
        Assert.Equal("after-transfer", await writer.GetAsync<string>("vehicles:firm:1"));
    }

    [Fact]
    public async Task Factory_started_before_remote_invalidation_cannot_republish_stale_value()
    {
        var backend = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        var first = new CacheService(backend, NullLogger<CacheService>.Instance);
        var second = new CacheService(backend, NullLogger<CacheService>.Instance);
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var resume = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var pending = first.GetOrSetAsync("vehicles:firm:1", async () =>
        {
            started.SetResult();
            await resume.Task.WaitAsync(TimeSpan.FromSeconds(10));
            return "stale";
        });
        try
        {
            await started.Task.WaitAsync(TimeSpan.FromSeconds(10));
            await second.RemoveAsync("vehicles:firm:1");
        }
        finally { resume.TrySetResult(); }
        Assert.Equal("stale", await pending);
        Assert.Null(await second.GetAsync<string>("vehicles:firm:1"));
        Assert.Equal("fresh", await second.GetOrSetAsync("vehicles:firm:1", () => Task.FromResult("fresh")));
        Assert.Equal("fresh", await first.GetAsync<string>("vehicles:firm:1"));
    }

    [Fact]
    public async Task Backend_failure_falls_back_to_factory_but_does_not_claim_invalidation_succeeded()
    {
        var cache = new CacheService(new UnavailableCache(), NullLogger<CacheService>.Instance);
        Assert.Null(await cache.GetAsync<string>("key"));
        Assert.Equal("database-result", await cache.GetOrSetAsync("key", () => Task.FromResult("database-result")));
        await Assert.ThrowsAsync<IOException>(() => cache.RemoveByPrefixAsync("key"));
    }

    [Fact]
    public async Task Cancellation_is_not_converted_into_a_cache_miss()
    {
        var backend = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        var cache = new CacheService(backend, NullLogger<CacheService>.Instance);
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cache.GetAsync<string>("key", cts.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cache.ExistsAsync("key", cts.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cache.RefreshAsync("key", cts.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cache.GetOrSetAsync<string>("key",
            () => throw new InvalidOperationException("Factory must not run"), cancellationToken: cts.Token));
    }

    private sealed class UnavailableCache : IDistributedCache
    {
        public byte[]? Get(string key) => throw new IOException("Backend unavailable");
        public Task<byte[]?> GetAsync(string key, CancellationToken token = default) => Task.FromException<byte[]?>(new IOException("Backend unavailable"));
        public void Set(string key, byte[] value, DistributedCacheEntryOptions options) => throw new IOException("Backend unavailable");
        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default) => Task.FromException(new IOException("Backend unavailable"));
        public void Refresh(string key) => throw new IOException("Backend unavailable");
        public Task RefreshAsync(string key, CancellationToken token = default) => Task.FromException(new IOException("Backend unavailable"));
        public void Remove(string key) => throw new IOException("Backend unavailable");
        public Task RemoveAsync(string key, CancellationToken token = default) => Task.FromException(new IOException("Backend unavailable"));
    }
}
