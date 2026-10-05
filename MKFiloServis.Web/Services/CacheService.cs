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
    // CacheService Scoped kayitli ancak IDistributedCache deposu singleton'dir.
    // Key takibi instance bazli olursa RemoveByPrefixAsync baska scope'larda
    // yazilmis anahtarlari goremez ve firma-bazli listeler bayat kalir
    // (orn. arac firma transferi sonrasi eski firmada gorunme sorunu).
    // Bu nedenle tracker tum instance'lar arasinda paylasilir (static).
    private static readonly ConcurrentDictionary<string, bool> _keyTracker = new();
    // Factory bu kilidin dışında çalışır; yalnız yayınlama/temizleme sırası korunur.
    private static readonly SemaphoreSlim MutationGate = new(1, 1);
    private static long _invalidationVersion;
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

    public Task SetAsync<T>(string key, T value, TimeSpan absoluteExpiration, CancellationToken cancellationToken = default) where T : class
        => SetCoreAsync(key, value, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = absoluteExpiration
        }, null, cancellationToken);

    public Task SetWithSlidingAsync<T>(string key, T value, TimeSpan slidingExpiration, CancellationToken cancellationToken = default) where T : class
        => SetCoreAsync(key, value, new DistributedCacheEntryOptions
        {
            SlidingExpiration = slidingExpiration
        }, null, cancellationToken);

    private async Task SetCoreAsync<T>(string key, T value, DistributedCacheEntryOptions options,
        long? expectedVersion, CancellationToken cancellationToken) where T : class
    {
        string data;
        try
        {
            data = JsonSerializer.Serialize(value, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache serialize hatası: {Key}", key);
            return;
        }
        await MutationGate.WaitAsync(cancellationToken);
        try
        {
            if (expectedVersion.HasValue && expectedVersion.Value != _invalidationVersion)
            {
                _logger.LogDebug("Cache sonucu temizlik sonrası yayınlanmadı: {Key}", key);
                return;
            }
            await _cache.SetStringAsync(key, data, options, cancellationToken);
            _keyTracker.TryAdd(key, true);
            _logger.LogDebug("Cache SET: {Key}", key);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache SET hatası: {Key}", key);
        }
        finally
        {
            MutationGate.Release();
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        await MutationGate.WaitAsync(cancellationToken);
        try
        {
            Interlocked.Increment(ref _invalidationVersion);
            await _cache.RemoveAsync(key, cancellationToken);
            _keyTracker.TryRemove(key, out _);
            _logger.LogDebug("Cache REMOVE: {Key}", key);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache REMOVE hatası: {Key}", key);
        }
        finally
        {
            MutationGate.Release();
        }
    }

    public async Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        await MutationGate.WaitAsync(cancellationToken);
        try
        {
            // İlk kez hesaplanan anahtar tracker'da olmasa bile eski factory geçersiz olsun.
            Interlocked.Increment(ref _invalidationVersion);
            var keysToRemove = _keyTracker.Keys.Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToList();
            foreach (var key in keysToRemove)
            {
                try
                {
                    await _cache.RemoveAsync(key, cancellationToken);
                    _keyTracker.TryRemove(key, out _);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    // Başarısız anahtar takibi korunur; diğer anahtarları temizlemeye devam et.
                    _logger.LogWarning(ex, "Cache prefix anahtarı silinemedi: {Key}", key);
                }
            }
            _logger.LogDebug("Cache REMOVE BY PREFIX: {Prefix}, Attempted: {Count}", prefix, keysToRemove.Count);
        }
        finally
        {
            MutationGate.Release();
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
        cancellationToken.ThrowIfCancellationRequested();
        var version = Volatile.Read(ref _invalidationVersion);
        var cached = await GetAsync<T>(key, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        if (cached != null && version == Volatile.Read(ref _invalidationVersion))
            return cached;

        _logger.LogDebug("Cache MISS: {Key}, factory çağrılıyor", key);
        var value = await factory();
        cancellationToken.ThrowIfCancellationRequested();
        if (value != null)
        {
            await SetCoreAsync(key, value, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = absoluteExpiration ?? DefaultExpiration
            }, version, cancellationToken);
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


