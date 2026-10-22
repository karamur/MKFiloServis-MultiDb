using Microsoft.AspNetCore.WebUtilities;

namespace MKFiloServis.Web.Components.Layout;

/// <summary>
/// Adres çubuğundaki sayfaya karşılık gelen TEK menü bağlantısını bulur: en uzun yol eşleşmesi kazanır.
/// Böylece "/butce/odemeler" adresinde yalnız "Ödeme Yönetimi" vurgulanır ("Bütçe Analiz" de değil);
/// menüde bağlantısı olmayan alt sayfalar (örn. "/cariler/yeni") en yakın üst bağlantıyı vurgular.
/// </summary>
public static class MenuEslestirici
{
    /// <param name="ogeler">Görünür menü bağlantıları.</param>
    /// <param name="goreliAdres">Uygulama köküne göre adres (<c>NavigationManager.ToBaseRelativePath</c>): <c>efatura/giden?yil=2026</c>.</param>
    /// <returns>Eşleşen bağlantı; hiçbiri uymuyorsa null.</returns>
    public static MenuOgesi? EnIyiEslesme(IEnumerable<MenuOgesi> ogeler, string goreliAdres)
    {
        var (yol, sorgu) = AdresiAyir(goreliAdres);
        var liste = ogeler as IReadOnlyList<MenuOgesi> ?? ogeler.ToList();

        MenuOgesi? en = null;
        var enPuan = 0;
        foreach (var oge in liste)
        {
            var puan = Puan(oge, liste, yol, sorgu);
            if (puan > enPuan)
            {
                en = oge;
                enPuan = puan;
            }
        }

        return en;
    }

    /// <summary>Adresi yol ve sorguya ayırır; yol küçük/büyük harfe duyarsız karşılaştırılmak üzere "/a/b" biçimine getirilir.</summary>
    public static (string Yol, IReadOnlyDictionary<string, string> Sorgu) AdresiAyir(string adres)
    {
        var temiz = adres ?? string.Empty;

        var kare = temiz.IndexOf('#');
        if (kare >= 0)
            temiz = temiz[..kare];

        var soru = temiz.IndexOf('?');
        var yol = soru >= 0 ? temiz[..soru] : temiz;
        var sorguMetni = soru >= 0 ? temiz[(soru + 1)..] : string.Empty;

        // Adres çubuğunda Türkçe karakterler yüzde kodlu gelir ("kiral%C4%B1k"); bağlantı tanımlarıyla aynı biçime getirilir.
        yol = "/" + Uri.UnescapeDataString(yol).Trim('/');

        var sorgu = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (sorguMetni.Length > 0)
        {
            foreach (var cift in QueryHelpers.ParseQuery(sorguMetni))
                sorgu[cift.Key] = cift.Value.ToString();
        }

        return (yol, sorgu);
    }

    private static int Puan(MenuOgesi oge, IReadOnlyList<MenuOgesi> tumu, string yol, IReadOnlyDictionary<string, string> sorgu)
    {
        var (ogeYolu, ogeSorgusu) = AdresiAyir(oge.Href);
        var adaylar = new List<string> { ogeYolu };
        if (oge.EkYollar is not null)
            adaylar.AddRange(oge.EkYollar.Select(y => AdresiAyir(y).Yol));

        var enIyi = 0;
        foreach (var aday in adaylar)
        {
            if (!YolEslesir(aday, yol, out var tam))
                continue;

            var puan = aday.Length * 4 + (tam ? 2 : 0) + 1;

            // Sorgulu bağlantı (rent-a-car?sekme=araclar): yalnız sorgu da uyuyorsa eşleşir; sorgusuz bağlantıya göre öncelikli.
            if (ogeSorgusu.Count > 0 && aday == ogeYolu)
            {
                if (!SorguUyar(oge, ogeSorgusu, tumu, ogeYolu, sorgu))
                    continue;
                puan += 10;
            }

            enIyi = Math.Max(enIyi, puan);
        }

        return enIyi;
    }

    private static bool YolEslesir(string aday, string yol, out bool tam)
    {
        tam = string.Equals(aday, yol, StringComparison.OrdinalIgnoreCase);
        if (tam)
            return true;

        // Kök ("/") her adresin öneki sayılmaz.
        return aday.Length > 1 && yol.StartsWith(aday + "/", StringComparison.OrdinalIgnoreCase);
    }

    private static bool SorguUyar(
        MenuOgesi oge,
        IReadOnlyDictionary<string, string> ogeSorgusu,
        IReadOnlyList<MenuOgesi> tumu,
        string ogeYolu,
        IReadOnlyDictionary<string, string> simdikiSorgu)
    {
        var (anahtar, deger) = ogeSorgusu.First();
        if (simdikiSorgu.TryGetValue(anahtar, out var simdiki) && string.Equals(simdiki, deger, StringComparison.OrdinalIgnoreCase))
            return true;

        if (!oge.VarsayilanSekme)
            return false;

        // Sorgu verilmemiş ya da tanımsız bir değer: sayfa varsayılan sekmesini açar; kardeş bağlantılardan hiçbiri uymuyorsa bu uyar.
        var kardesUydu = tumu.Any(k =>
        {
            var (kYol, kSorgu) = AdresiAyir(k.Href);
            return kSorgu.Count > 0
                   && string.Equals(kYol, ogeYolu, StringComparison.OrdinalIgnoreCase)
                   && kSorgu.TryGetValue(anahtar, out var kDeger)
                   && simdikiSorgu.TryGetValue(anahtar, out var s)
                   && string.Equals(s, kDeger, StringComparison.OrdinalIgnoreCase);
        });
        return !kardesUydu;
    }
}
