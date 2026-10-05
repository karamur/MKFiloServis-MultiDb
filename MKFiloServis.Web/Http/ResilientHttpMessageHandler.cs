using System.Net;
using System.Net.Sockets;

namespace MKFiloServis.Web.Http;

/// <summary>
/// Dış servislere giden HTTP çağrıları için üstel geri çekilmeli yeniden deneme.
/// Yalnız geçici hatalar (ağ hatası, 5xx, 429) yeniden denenir; 4xx isteğin kendisinden
/// kaynaklandığı için tekrar edilmez. Ek paket bağımlılığı getirmemesi için yazıldı.
/// </summary>
public sealed class ResilientHttpMessageHandler : DelegatingHandler
{
    private readonly int _maxRetries;
    private readonly TimeSpan _baseDelay;
    private readonly bool _retryNonIdempotent;
    private readonly ILogger<ResilientHttpMessageHandler> _logger;

    /// <param name="maxRetries">Bir istek için, ilk deneme dışında yapılacak en fazla ek deneme sayısı.</param>
    /// <param name="baseDelay">İlk geri çekilme aralığı; sonraki denemelerde üstel olarak büyür.</param>
    /// <param name="logger">Yeniden deneme kararlarını kaydedecek günlükçü.</param>
    /// <param name="retryNonIdempotent">
    /// POST/PATCH gibi idempotent olmayan metotlar da yeniden denensin mi? Yanlışlıkla çift
    /// gönderim (çift webhook, çift bildirim) riski taşır. Bu bayrak açıksa, yalnızca isteğin
    /// sunucuya ulaşmadan BAŞARISIZ olduğu durumlar (DNS/bağlantı reddi) yeniden denenir;
    /// belirsiz durumlar (zaman aşımı, bağlantı sıfırlanması) tekrar edilmez.
    /// </param>
    public ResilientHttpMessageHandler(
        int maxRetries,
        TimeSpan baseDelay,
        ILogger<ResilientHttpMessageHandler> logger,
        bool retryNonIdempotent = false)
    {
        _maxRetries = Math.Max(0, maxRetries);
        _baseDelay = baseDelay;
        _retryNonIdempotent = retryNonIdempotent;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // HttpClient, bir HttpRequestMessage örneğini yalnızca BİR kez gönderebilir; ikinci
        // SendAsync çağrısı InvalidOperationException ("already sent") ile çöker. Bu yüzden gövde
        // bir kez belleğe alınır ve her deneme için YENİ bir istek örneği oluşturulur.
        var govde = request.Content is null
            ? null
            : await request.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);

        for (var attempt = 0; ; attempt++)
        {
            var deneme = Kopyala(request, govde);
            try
            {
                var response = await base.SendAsync(deneme, cancellationToken).ConfigureAwait(false);

                if (attempt < _maxRetries && IsTransient(response.StatusCode) && !cancellationToken.IsCancellationRequested
                    && YenidenDenemeyeIzinVerili(request, null))
                {
                    _logger.LogWarning(
                        "Gecici HTTP hatasi ({StatusCode}); {Attempt}/{Max} denemede tekrar deneniyor. Url: {Url}",
                        (int)response.StatusCode, attempt + 1, _maxRetries, request.RequestUri);
                    response.Dispose();
                    await Task.Delay(BeklemeSuresi(attempt), cancellationToken).ConfigureAwait(false);
                    continue;
                }

                // Başarılı yanıt: deneme kopyasının yaşam döngüsü çağırana bırakılır, çünkü
                // dönen HttpResponseMessage bu isteğe bağlıdır.
                return response;
            }
            catch (HttpRequestException ex) when (attempt < _maxRetries && !cancellationToken.IsCancellationRequested)
            {
                if (!YenidenDenemeyeIzinVerili(request, ex))
                {
                    deneme.Dispose();
                    throw;
                }

                _logger.LogWarning(ex,
                    "Ag hatasi; {Attempt}/{Max} denemede tekrar deneniyor. Url: {Url}",
                    attempt + 1, _maxRetries, request.RequestUri);
                await Task.Delay(BeklemeSuresi(attempt), cancellationToken).ConfigureAwait(false);
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                // İstek iptali değil, istemcinin kendi zaman aşımı.
                // Zaman aşımında sunucu isteği işlemiş OLABİLİR; idempotent olmayan metotlarda bu
                // belirsiz durum yeniden denenmez (çift gönderim riski).
                if (attempt >= _maxRetries || !YenidenDenemeyeIzinVerili(request, null))
                {
                    deneme.Dispose();
                    throw;
                }

                _logger.LogWarning(
                    "Istek zaman asimina ugradi; {Attempt}/{Max} denemede tekrar deneniyor. Url: {Url}",
                    attempt + 1, _maxRetries, request.RequestUri);
                await Task.Delay(BeklemeSuresi(attempt), cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                deneme.Dispose();
                throw;
            }
        }
    }

    /// <summary>Her deneme için, aynı gövdeyi paylaşan taze bir istek örneği üretir.</summary>
    private static HttpRequestMessage Kopyala(HttpRequestMessage kaynak, byte[]? govde)
    {
        var kopya = new HttpRequestMessage(kaynak.Method, kaynak.RequestUri)
        {
            Version = kaynak.Version
        };

        if (govde is not null)
        {
            var icerik = new ByteArrayContent(govde);
            if (kaynak.Content?.Headers is not null)
            {
                foreach (var baslik in kaynak.Content.Headers)
                {
                    icerik.Headers.TryAddWithoutValidation(baslik.Key, baslik.Value);
                }
            }
            kopya.Content = icerik;
        }

        foreach (var baslik in kaynak.Headers)
        {
            kopya.Headers.TryAddWithoutValidation(baslik.Key, baslik.Value);
        }

        foreach (var secenek in kaynak.Options)
        {
            ((IDictionary<string, object?>)kopya.Options)[secenek.Key] = secenek.Value;
        }

        return kopya;
    }

    /// <summary>İdempotent olmayan metotlarda hangi hâllerin yeniden denemeye uygun olduğunu belirler.</summary>
    private bool YenidenDenemeyeIzinVerili(HttpRequestMessage request, HttpRequestException? hata)
    {
        if (request.Method == HttpMethod.Get || request.Method == HttpMethod.Head
            || request.Method == HttpMethod.Options || request.Method == HttpMethod.Put
            || request.Method == HttpMethod.Delete || request.Method == HttpMethod.Trace)
        {
            return true;
        }

        if (!_retryNonIdempotent)
        {
            return false;
        }

        // Idempotent olmayan metot: yalnızca isteğin sunucuya hiç ulaşmadığı kesin hatalarda
        // tekrar dene. DNS çözülememesi / bağlantı reddi / ağ erişilemez → sunucu görmedi.
        // Bağlantı sıfırlanması veya zaman aşımı belirsizdir → tekrar edilmez.
        return SocketHatasi(hata) is SocketError.HostNotFound
            or SocketError.HostDown
            or SocketError.HostUnreachable
            or SocketError.NetworkUnreachable
            or SocketError.NetworkDown
            or SocketError.ConnectionRefused
            or SocketError.NoRecovery
            or SocketError.TryAgain;
    }

    /// <summary>İstisna zincirindeki ilk <see cref="SocketException"/> hata kodunu döndürür.</summary>
    private static SocketError? SocketHatasi(Exception? hata)
    {
        for (var e = hata; e is not null; e = e.InnerException)
        {
            if (e is SocketException sokul) return sokul.SocketErrorCode;
        }

        return null;
    }

    private TimeSpan BeklemeSuresi(int attempt)
    {
        // 200ms, 400ms, 800ms ... üstel geri çekilme.
        var ms = _baseDelay.TotalMilliseconds * Math.Pow(2, attempt);
        return TimeSpan.FromMilliseconds(Math.Min(ms, 10_000));
    }

    private static bool IsTransient(HttpStatusCode status) =>
        status == HttpStatusCode.RequestTimeout          // 408
        || status == HttpStatusCode.TooManyRequests        // 429
        || (int)status >= 500;                            // 5xx
}
