using System.Net;
using System.Text.Json;

namespace MKFiloServis.Web.Middleware;

public class IpGuvenlikMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<IpGuvenlikMiddleware> _logger;
    private readonly bool _enabled;
    private readonly HashSet<string> _beyazListe;
    private readonly HashSet<string> _karaListe;

    private static readonly HashSet<string> _atlanacakYollar = new(StringComparer.OrdinalIgnoreCase)
    {
        "/_blazor", "/_framework", "/favicon", "/app.css", "/lib/"
    };

    public IpGuvenlikMiddleware(RequestDelegate next, IConfiguration configuration, ILogger<IpGuvenlikMiddleware> logger)
    {
        _next = next;
        _logger = logger;
        _enabled = configuration.GetValue("IpGuvenlik:Enabled", false);
        _beyazListe = new HashSet<string>(
            configuration.GetSection("IpGuvenlik:BeyazListe").Get<string[]>() ?? Array.Empty<string>(),
            StringComparer.OrdinalIgnoreCase);
        _karaListe = new HashSet<string>(
            configuration.GetSection("IpGuvenlik:KaraListe").Get<string[]>() ?? Array.Empty<string>(),
            StringComparer.OrdinalIgnoreCase);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_enabled)
        {
            await _next(context);
            return;
        }

        var path = context.Request.Path.Value ?? string.Empty;
        if (_atlanacakYollar.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            await _next(context);
            return;
        }

        var ip = GetClientIp(context);

        // Kara liste kontrolü - önce kontrol et
        if (_karaListe.Count > 0 && (_karaListe.Contains(ip) || EslestirCidr(ip, _karaListe)))
        {
            _logger.LogWarning("Kara listeden engellendi: {IP} -> {Path}", ip, path);
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { hata = "Erişim engellendi." }));
            return;
        }

        // Beyaz liste aktifse ve IP listede değilse engelle
        if (_beyazListe.Count > 0 && !_beyazListe.Contains(ip) && !EslestirCidr(ip, _beyazListe))
        {
            _logger.LogWarning("Beyaz liste dışı engellendi: {IP} -> {Path}", ip, path);
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { hata = "Erişim yetkisi yok." }));
            return;
        }

        await _next(context);
    }

    private static string GetClientIp(HttpContext context)
    {
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            var firstIp = forwardedFor.Split(',')[0].Trim();
            if (IPAddress.TryParse(firstIp, out _))
                return firstIp;
        }
        return context.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "0.0.0.0";
    }

    private static bool EslestirCidr(string ip, HashSet<string> liste)
    {
        if (!IPAddress.TryParse(ip, out var hedefIp)) return false;
        foreach (var girdis in liste)
        {
            if (!girdis.Contains('/')) continue;
            var parcalar = girdis.Split('/');
            if (parcalar.Length != 2) continue;
            if (!IPAddress.TryParse(parcalar[0], out var agIp)) continue;
            if (!int.TryParse(parcalar[1], out var prefix)) continue;
            if (AgIcindeMi(hedefIp, agIp, prefix)) return true;
        }
        return false;
    }

    private static bool AgIcindeMi(IPAddress ip, IPAddress agIp, int prefix)
    {
        var ipBytes = ip.GetAddressBytes();
        var agBytes = agIp.GetAddressBytes();
        if (ipBytes.Length != agBytes.Length) return false;

        var maskBytes = OlusturMask(prefix, ipBytes.Length);
        for (int i = 0; i < ipBytes.Length; i++)
        {
            if ((ipBytes[i] & maskBytes[i]) != (agBytes[i] & maskBytes[i])) return false;
        }
        return true;
    }

    private static byte[] OlusturMask(int prefix, int length)
    {
        var mask = new byte[length];
        for (int i = 0; i < length; i++)
        {
            if (prefix >= 8) { mask[i] = 0xFF; prefix -= 8; }
            else if (prefix > 0) { mask[i] = (byte)(0xFF << (8 - prefix)); prefix = 0; }
        }
        return mask;
    }
}



