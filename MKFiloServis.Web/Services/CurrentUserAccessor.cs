using System.Collections.Concurrent;

namespace MKFiloServis.Web.Services;

/// <summary>
/// Blazor Server circuit'lerinden aktif kullanıcı bilgisini thread-safe şekilde tutar.
/// Singleton interceptor'ün scoped authentication state'e erişebilmesi için kullanılır.
/// </summary>
public interface ICurrentUserAccessor
{
    string? GetCurrentUserName();
    void SetCurrentUser(string? kullaniciAdi, string? adSoyad);
    void ClearCurrentUser();
}

public class CurrentUserAccessor : ICurrentUserAccessor
{
    // Her thread/circuit için ayrı kullanıcı bilgisi tutar
    private static readonly AsyncLocal<UserContext?> _currentUser = new();

    public string? GetCurrentUserName()
    {
        var ctx = _currentUser.Value;
        if (ctx == null)
            return null;

        return !string.IsNullOrWhiteSpace(ctx.AdSoyad) 
            ? ctx.AdSoyad 
            : ctx.KullaniciAdi;
    }

    public void SetCurrentUser(string? kullaniciAdi, string? adSoyad)
    {
        _currentUser.Value = new UserContext
        {
            KullaniciAdi = kullaniciAdi,
            AdSoyad = adSoyad
        };
    }

    public void ClearCurrentUser()
    {
        _currentUser.Value = null;
    }

    private class UserContext
    {
        public string? KullaniciAdi { get; set; }
        public string? AdSoyad { get; set; }
    }
}


