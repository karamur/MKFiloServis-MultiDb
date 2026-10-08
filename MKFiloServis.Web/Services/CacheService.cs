using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Web.Services;

/// <summary>
/// Cache values belong to a generation held in the shared backend. Invalidations
/// rotate that generation, so factories on other processes cannot revive old data.
/// </summary>
public class CacheService : ICacheService
{
    private const string GenerationKey = "__MKFiloServis.Cache.v2.generation";
    private readonly IDistributedCache _cache;
    private readonly ILogger<CacheService> _logger;
    private static readonly TimeSpan DefaultExpiration = TimeSpan.FromMinutes(5);
    // Business lists are changed by several services and external import paths. Until
    // invalidation is committed with the database write, serving a cached copy can
    // return stale or cross-tenant data after an invalidation failure.
    private static bool RequiresFreshRead(string key) =>
        key.StartsWith(CacheKeys.Prefix, StringComparison.Ordinal);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = null,
        WriteIndented = false,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
    };

    public CacheService(IDistributedCache cache, ILogger<CacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    private async Task<string> GetGenerationAsync(CancellationToken ct)
    {
        var generation = await _cache.GetStringAsync(GenerationKey, ct);
        if (!string.IsNullOrEmpty(generation))
            return generation;

        // A missing/evicted marker must never reuse an old namespace. Concurrent
        // initializers may cause a cache miss, but cannot make old values visible.
        generation = Guid.NewGuid().ToString("N");
        await _cache.SetStringAsync(GenerationKey, generation, new DistributedCacheEntryOptions(), ct);
        return generation;
    }

    private static string PhysicalKey(string key, string generation)
        => "__MKFiloServis.Cache.v2." + generation + "." +
           Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key)));

    private async Task<T?> ReadAsync<T>(string key, string generation, CancellationToken ct) where T : class
    {
        var data = await _cache.GetStringAsync(PhysicalKey(key, generation), ct);
        if (string.IsNullOrEmpty(data) || generation != await GetGenerationAsync(ct))
            return null;
        return JsonSerializer.Deserialize<T>(data, JsonOptions);
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (RequiresFreshRead(key)) return null;
        try
        {
            return await ReadAsync<T>(key, await GetGenerationAsync(cancellationToken), cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache GET hatası: {Key}", key);
            return null;
        }
    }

    public Task SetAsync<T>(string key, T value, CancellationToken cancellationToken = default) where T : class
        => SetAsync(key, value, DefaultExpiration, cancellationToken);

    public Task SetAsync<T>(string key, T value, TimeSpan absoluteExpiration, CancellationToken cancellationToken = default) where T : class
        => SetCoreAsync(key, value, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = absoluteExpiration
        }, null, cancellationToken);

    public Task SetWithSlidingAsync<T>(string key, T value, TimeSpan slidingExpiration, CancellationToken cancellationToken = default) where T : class
        => SetCoreAsync(key, value, new DistributedCacheEntryOptions
        {
            SlidingExpiration = slidingExpiration,
            AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1)
        }, null, cancellationToken);

    private async Task SetCoreAsync<T>(string key, T value, DistributedCacheEntryOptions options,
        string? expectedGeneration, CancellationToken ct) where T : class
    {
        ct.ThrowIfCancellationRequested();
        if (RequiresFreshRead(key)) return;
        try
        {
            var generation = await GetGenerationAsync(ct);
            if (expectedGeneration is not null && expectedGeneration != generation)
                return;
            var data = JsonSerializer.Serialize(value, JsonOptions);
            // If invalidation races this write, it remains in the old namespace.
            // Future readers use the new generation and cannot observe this value.
            await _cache.SetStringAsync(PhysicalKey(key, generation), data, options, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache SET hatası: {Key}", key);
        }
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        => InvalidateAsync(key, cancellationToken);

    public Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
        => InvalidateAsync(prefix, cancellationToken);

    private async Task InvalidateAsync(string reason, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (RequiresFreshRead(reason)) return;
        try
        {
            // IDistributedCache cannot enumerate keys or atomically maintain a
            // prefix index. Conservatively invalidate the whole application cache.
            // Old namespace entries expire with their existing TTL.
            await _cache.SetStringAsync(GenerationKey, Guid.NewGuid().ToString("N"),
                new DistributedCacheEntryOptions(), ct);
            _logger.LogDebug("Ortak cache nesli yenilendi: {Reason}", reason);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache geçersizleştirme başarısız: {Reason}", reason);
            // A failed invalidation must not be reported as a successful removal.
            throw;
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (RequiresFreshRead(key)) return false;
        try
        {
            var generation = await GetGenerationAsync(cancellationToken);
            var data = await _cache.GetAsync(PhysicalKey(key, generation), cancellationToken);
            return data is { Length: > 0 } && generation == await GetGenerationAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache EXISTS hatası: {Key}", key);
            return false;
        }
    }

    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? absoluteExpiration = null,
        CancellationToken cancellationToken = default) where T : class
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (RequiresFreshRead(key))
        {
            var fresh = await factory();
            cancellationToken.ThrowIfCancellationRequested();
            return fresh;
        }
        string? generation = null;
        try
        {
            generation = await GetGenerationAsync(cancellationToken);
            var cached = await ReadAsync<T>(key, generation, cancellationToken);
            if (cached is not null)
                return cached;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            generation = null;
            _logger.LogWarning(ex, "Cache okunamadı, veri kaynağı kullanılacak: {Key}", key);
        }

        var value = await factory();
        cancellationToken.ThrowIfCancellationRequested();
        if (value is not null && generation is not null)
            await SetCoreAsync(key, value, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = absoluteExpiration ?? DefaultExpiration
            }, generation, cancellationToken);
        return value!;
    }

    public async Task RefreshAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (RequiresFreshRead(key)) return;
        try
        {
            var generation = await GetGenerationAsync(cancellationToken);
            await _cache.RefreshAsync(PhysicalKey(key, generation), cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache REFRESH hatası: {Key}", key);
        }
    }
}
