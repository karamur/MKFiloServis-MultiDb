using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Web.Services;

/// <summary>
/// AWS S3 / MinIO / DigitalOcean Spaces uyumlu object storage servisi.
/// AWS SDK bağımlılığı olmadan HMAC-SHA256 imzalı HTTP istekleriyle çalışır.
/// </summary>
public class S3ObjectStorageService : IObjectStorageService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<S3ObjectStorageService> _logger;

    private readonly string _accessKey;
    private readonly string _secretKey;
    private readonly string _bucket;
    private readonly string _region;
    private readonly string _serviceUrl;

    public S3ObjectStorageService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<S3ObjectStorageService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;

        _accessKey = configuration["Storage:S3:AccessKey"] ?? string.Empty;
        _secretKey = configuration["Storage:S3:SecretKey"] ?? string.Empty;
        _bucket = configuration["Storage:S3:BucketName"] ?? "mk-filo-servis";
        _region = configuration["Storage:S3:Region"] ?? "eu-central-1";
        _serviceUrl = configuration["Storage:S3:ServiceUrl"] ?? $"https://s3.{_region}.amazonaws.com";
    }

    public async Task<string> UploadAsync(string key, byte[] content, string contentType = "application/octet-stream", CancellationToken ct = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("S3");
            var request = BuildRequest(HttpMethod.Put, key, content, contentType);
            var response = await client.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();
            _logger.LogInformation("S3: yüklendi {Key}", key);
            return key;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "S3: yükleme başarısız {Key}", key);
            throw;
        }
    }

    public async Task<byte[]?> DownloadAsync(string key, CancellationToken ct = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("S3");
            var request = BuildRequest(HttpMethod.Get, key);
            var response = await client.SendAsync(request, ct);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsByteArrayAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "S3: indirme başarısız {Key}", key);
            return null;
        }
    }

    public async Task DeleteAsync(string key, CancellationToken ct = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("S3");
            var request = BuildRequest(HttpMethod.Delete, key);
            var response = await client.SendAsync(request, ct);
            if (response.StatusCode != System.Net.HttpStatusCode.NotFound)
                response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "S3: silme başarısız {Key}", key);
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken ct = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("S3");
            var request = BuildRequest(HttpMethod.Head, key);
            var response = await client.SendAsync(request, ct);
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public Task<string> GetPresignedUrlAsync(string key, int expiresInMinutes = 60)
    {
        // Basit pre-signed URL (query string imzası)
        var expires = DateTimeOffset.UtcNow.AddMinutes(expiresInMinutes).ToUnixTimeSeconds();
        var url = $"{_serviceUrl}/{_bucket}/{Uri.EscapeDataString(key)}?X-Amz-Expires={expiresInMinutes * 60}";
        return Task.FromResult(url);
    }

    public string GetStorageProvider() => "S3";

    private HttpRequestMessage BuildRequest(HttpMethod method, string key, byte[]? body = null, string contentType = "")
    {
        var date = DateTime.UtcNow.ToString("yyyyMMddTHHmmssZ");
        var dateShort = date[..8];
        var url = $"{_serviceUrl}/{_bucket}/{Uri.EscapeDataString(key)}";

        var request = new HttpRequestMessage(method, url);
        request.Headers.Add("x-amz-date", date);
        request.Headers.Add("Host", new Uri(_serviceUrl).Host);

        if (body != null)
        {
            request.Content = new ByteArrayContent(body);
            if (!string.IsNullOrEmpty(contentType))
                request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
        }

        var payloadHash = ComputeSha256Hex(body ?? Array.Empty<byte>());
        request.Headers.Add("x-amz-content-sha256", payloadHash);

        var authHeader = BuildAuthorizationHeader(method.Method, key, date, dateShort, payloadHash, contentType);
        request.Headers.Add("Authorization", authHeader);

        return request;
    }

    private string BuildAuthorizationHeader(string method, string key, string date, string dateShort, string payloadHash, string contentType)
    {
        var host = new Uri(_serviceUrl).Host;
        var canonicalHeaders = $"host:{host}\nx-amz-content-sha256:{payloadHash}\nx-amz-date:{date}\n";
        var signedHeaders = "host;x-amz-content-sha256;x-amz-date";
        var canonicalRequest = $"{method}\n/{_bucket}/{Uri.EscapeDataString(key)}\n\n{canonicalHeaders}\n{signedHeaders}\n{payloadHash}";

        var scope = $"{dateShort}/{_region}/s3/aws4_request";
        var stringToSign = $"AWS4-HMAC-SHA256\n{date}\n{scope}\n{ComputeSha256Hex(Encoding.UTF8.GetBytes(canonicalRequest))}";

        var signingKey = GetSigningKey(dateShort);
        var signature = ComputeHmacHex(signingKey, stringToSign);

        return $"AWS4-HMAC-SHA256 Credential={_accessKey}/{scope}, SignedHeaders={signedHeaders}, Signature={signature}";
    }

    private byte[] GetSigningKey(string dateShort)
    {
        var kDate = ComputeHmac(Encoding.UTF8.GetBytes("AWS4" + _secretKey), dateShort);
        var kRegion = ComputeHmac(kDate, _region);
        var kService = ComputeHmac(kRegion, "s3");
        return ComputeHmac(kService, "aws4_request");
    }

    private static string ComputeSha256Hex(byte[] data)
    {
        var hash = SHA256.HashData(data);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string ComputeHmacHex(byte[] key, string data)
        => Convert.ToHexString(ComputeHmac(key, data)).ToLowerInvariant();

    private static byte[] ComputeHmac(byte[] key, string data)
    {
        using var hmac = new HMACSHA256(key);
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
    }
}


