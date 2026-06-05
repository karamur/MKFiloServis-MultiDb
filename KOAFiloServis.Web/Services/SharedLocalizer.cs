using System.Globalization;
using System.Resources;

namespace KOAFiloServis.Web.Services;

/// <summary>
/// Paylaşılan resource (.resx) dosyalarından lokalize metin okumak için yardımcı servis.
/// Blazor sayfalarında @inject ile kullanılır.
/// </summary>
public class SharedLocalizer
{
    private static readonly Lazy<ResourceManager> _trManager = new(() =>
        new ResourceManager("KOAFiloServis.Web.Resources.SharedResources",
            typeof(SharedLocalizer).Assembly));

    /// <summary>Belirtilen key için lokalize metni döndürür.</summary>
    public string this[string key]
    {
        get
        {
            var culture = CultureInfo.CurrentUICulture;
            try
            {
                var value = _trManager.Value.GetString(key, culture);
                return value ?? $"[{key}]";
            }
            catch
            {
                return $"[{key}]";
            }
        }
    }

    /// <summary>Parametreli lokalize metin.</summary>
    public string Format(string key, params object[] args)
    {
        var template = this[key];
        return string.Format(template, args);
    }
}
