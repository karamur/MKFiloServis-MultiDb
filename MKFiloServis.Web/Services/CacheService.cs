using System;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MKFiloServis.Web.Services.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace MKFiloServis.Web.Services;

/// <summary>
/// Distributed cache servisi implementasyonu
/// IDistributedCache üzerinden Redis veya Memory cache ile çalışır
/// </summary>
public class CacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<CacheService> _logger;
    private readonly ConcurrentDictionary<string, bool> _keyTracker = new();
    private static readonly TimeSpan DefaultExpiration = TimeSpan.FromMinutes(5);
    
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

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var data = await _cache.GetStringAsync(key, cancellationToken);
            if (string.IsNullOrEmpty(data))
            {
                return null;
            }
            
            var result = JsonSerializer.Deserialize<T>(data, JsonOptions);
            _logger.LogDebug("Cache HIT: {Key}", key);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache GET hatası: {Key}", key);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, CancellationToken cancellationToken = default) where T : class
    {
        await SetAsync(key, value, DefaultExpiration, cancellationToken);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan absoluteExpiration, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = absoluteExpiration
            };
            
            var data = JsonSerializer.Serialize(value, JsonOptions);
            await _cache.SetStringAsync(key, data, options, cancellationToken);
            
            // Key tracking for prefix-based removal
            _keyTracker.TryAdd(key, true);
            
            _logger.LogDebug("Cache SET: {Key}, TTL: {TTL}", key, absoluteExpiration);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache SET hatası: {Key}", key);
        }
    }

    public async Task SetWithSlidingAsync<T>(string key, T value, TimeSpan slidingExpiration, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var options = new DistributedCacheEntryOptions
            {
                SlidingExpiration = slidingExpiration
            };
            
            var data = JsonSerializer.Serialize(value, JsonOptions);
            await _cache.SetStringAsync(key, data, options, cancellationToken);
            
            _keyTracker.TryAdd(key, true);
            
            _logger.LogDebug("Cache SET (sliding): {Key}, Sliding: {Sliding}", key, slidingExpiration);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache SET (sliding) hatası: {Key}", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await _cache.RemoveAsync(key, cancellationToken);
            _keyTracker.TryRemove(key, out _);
            _logger.LogDebug("Cache REMOVE: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache REMOVE hatası: {Key}", key);
        }
    }

    public async Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        try
        {
            var keysToRemove = _keyTracker.Keys.Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToList();
            
            foreach (var key in keysToRemove)
            {
                await _cache.RemoveAsync(key, cancellationToken);
                _keyTracker.TryRemove(key, out _);
            }
            
            _logger.LogDebug("Cache REMOVE BY PREFIX: {Prefix}, Count: {Count}", prefix, keysToRemove.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache REMOVE BY PREFIX hatası: {Prefix}", prefix);
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var data = await _cache.GetAsync(key, cancellationToken);
            return data != null && data.Length > 0;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache EXISTS hatası: {Key}", key);
            return false;
        }
    }

    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? absoluteExpiration = null, CancellationToken cancellationToken = default) where T : class
    {
        // Önce cache'den dene
        var cached = await GetAsync<T>(key, cancellationToken);
        if (cached != null)
        {
            return cached;
        }
        
        // Cache'de yoksa factory ile oluştur
        _logger.LogDebug("Cache MISS: {Key}, factory çağrılıyor", key);
        var value = await factory();

        // Cache'e yaz
        if (value != null)
        {
            await SetAsync(key, value, absoluteExpiration ?? DefaultExpiration, cancellationToken);
        }

        return value!;
    }

    public async Task RefreshAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await _cache.RefreshAsync(key, cancellationToken);
            _logger.LogDebug("Cache REFRESH: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache REFRESH hatası: {Key}", key);
        }
    }
}


