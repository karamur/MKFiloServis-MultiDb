using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace KOAFiloServis.Maui.Services;

/// <summary>
/// Merkezi HTTP client — JWT token yönetimi ve API çağrıları.
/// </summary>
public class ApiClientService
{
    private readonly HttpClient _http;
    private readonly AuthService _auth;
    private readonly JsonSerializerOptions _jsonOptions;

    public ApiClientService(HttpClient http, AuthService auth)
    {
        _http = http;
        _auth = auth;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = null
        };
    }

    /// <summary>JWT token ile yetkilendirilmiş GET isteği.</summary>
    public async Task<T?> GetAsync<T>(string endpoint)
    {
        await EnsureAuthHeaderAsync();
        var response = await _http.GetAsync(endpoint);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(_jsonOptions);
    }

    /// <summary>JWT token ile yetkilendirilmiş POST isteği.</summary>
    public async Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest data)
    {
        await EnsureAuthHeaderAsync();
        var response = await _http.PostAsJsonAsync(endpoint, data, _jsonOptions);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TResponse>(_jsonOptions);
    }

    /// <summary>JWT token ile yetkilendirilmiş POST (yanıt gerektirmeyen).</summary>
    public async Task<bool> PostAsync<TRequest>(string endpoint, TRequest data)
    {
        await EnsureAuthHeaderAsync();
        var response = await _http.PostAsJsonAsync(endpoint, data, _jsonOptions);
        return response.IsSuccessStatusCode;
    }

    /// <summary>Login isteği (token header'sız).</summary>
    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        var response = await _http.PostAsJsonAsync("/api/auth/login", request, _jsonOptions);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<LoginResponse>(_jsonOptions);
    }

    private async Task EnsureAuthHeaderAsync()
    {
        var token = await _auth.GetTokenAsync();
        if (!string.IsNullOrEmpty(token))
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
    }
}

// ── Auth DTOs ──────────────────────────────────────────────────────

public class LoginRequest
{
    public string KullaniciAdi { get; set; } = string.Empty;
    public string Sifre { get; set; } = string.Empty;
}

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public string KullaniciAdi { get; set; } = string.Empty;
    public string AdSoyad { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
    public int KullaniciId { get; set; }
    public DateTime Expiration { get; set; }
}

// ── Puantaj DTOs ───────────────────────────────────────────────────

public class GunlukPuantajDto
{
    public DateTime Tarih { get; set; }
    public string? GuzergahAdi { get; set; }
    public string? Plaka { get; set; }
    public int SeferSayisi { get; set; }
    public string? Yon { get; set; }
    public string? Slot { get; set; }
    public decimal? BirimGelir { get; set; }
    public decimal? BirimGider { get; set; }
    public string? Notlar { get; set; }
}

public class SoforGorevDto
{
    public int Id { get; set; }
    public DateTime Tarih { get; set; }
    public string GuzergahAdi { get; set; } = string.Empty;
    public string Plaka { get; set; } = string.Empty;
    public string Slot { get; set; } = string.Empty;
    public string Yon { get; set; } = string.Empty;
    public int SeferSayisi { get; set; }
    public string Durum { get; set; } = string.Empty;
}

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }
}
