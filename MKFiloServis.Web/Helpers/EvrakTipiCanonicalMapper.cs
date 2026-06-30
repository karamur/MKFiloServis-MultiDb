namespace MKFiloServis.Web.Helpers;

/// <summary>
/// Merkezi evrak tipi alias mapping.
/// Hızlı yükleme kartları ile detay checklist arasındaki evrak isim farklılıklarını
/// gidermek için canonical (standart kısa) isimlere mapping yapar.
///
/// Kullanım: Tüm evrak adı karşılaştırmaları bu mapper üzerinden yapılmalıdır.
/// Razor içinde dağınık if/contains yazılmamalıdır.
/// </summary>
public static class EvrakTipiCanonicalMapper
{
    /// <summary>
    /// Canonical (standart kısa) isim → alias listesi mapping.
    /// Key: Hızlı kartlarda gösterilen kısa isim.
    /// Value: SADECE bu belgenin varyasyonları. Ayrı zorunlu evraklar eklenmez!
    ///
    /// ÖNEMLİ KURAL: Her alias listesi yalnızca AYNI BELGENİN farklı yazılışlarını içerir.
    /// Örn: "Kimlik" kartı sadece kimlik belgesi varyasyonlarını kapsar;
    /// İkametgah, Vesikalık Fotoğraf, Nüfus Kayıt Örneği ayrı evraklardır — Kimlik'e eklenmez.
    /// </summary>
    public static readonly Dictionary<string, string[]> CategoryAliases = new()
    {
        ["Kimlik"] = new[] {
            "Kimlik", "Kimlik Fotokopisi", "Nüfus Cüzdanı", "T.C. Kimlik", "Kimlik Belgesi"
        },
        ["Ehliyet"] = new[] {
            "Ehliyet", "Ehliyet Fotokopisi", "Sürücü Belgesi"
        },
        ["Diploma"] = new[] {
            "Diploma", "Diploma Fotokopisi"
        },
        ["Saglik Raporu"] = new[] {
            "Sağlık Raporu", "Saglik Raporu"
        },
        ["Sertifika"] = new[] {
            "Sertifika", "SRC Belgesi", "Psikoteknik Belgesi",
            "ADR Belgesi", "Mesleki Yeterlilik Belgesi", "MYK"
        },
        ["Is Sozlesmesi"] = new[] {
            "İş Sözleşmesi", "Is Sozlesmesi", "Sözleşme"
            // NOT: İş Başvuru, Özgeçmiş, Sabıka, Askerlik, KVKK ayrı zorunlu evraklardır.
        },
        ["Diger"] = new[] {
            "Diger", "Diğer"
            // NOT: Banka Hesap (IBAN), AGİ, Engellilik ayrı zorunlu evraklardır.
        },
    };

    /// <summary>
    /// Verilen bir evrak adının hangi canonical kategoriye ait olduğunu döner.
    /// Eşleşme bulunamazsa null döner.
    /// </summary>
    public static string? GetCanonicalName(string? evrakAdi)
    {
        if (string.IsNullOrWhiteSpace(evrakAdi))
            return null;

        foreach (var (canonical, aliases) in CategoryAliases)
        {
            if (aliases.Any(a =>
                    string.Equals(a, evrakAdi, StringComparison.OrdinalIgnoreCase) ||
                    evrakAdi.Contains(a, StringComparison.OrdinalIgnoreCase) ||
                    a.Contains(evrakAdi, StringComparison.OrdinalIgnoreCase)))
            {
                return canonical;
            }
        }

        return null;
    }

    /// <summary>
    /// İki evrak adının aynı canonical kategoriye ait olup olmadığını kontrol eder.
    /// </summary>
    public static bool AreSameCategory(string? name1, string? name2)
    {
        var c1 = GetCanonicalName(name1);
        var c2 = GetCanonicalName(name2);
        return c1 != null && c1 == c2;
    }

    /// <summary>
    /// Bir evrak adının, verilen canonical kategoriye ait olup olmadığını kontrol eder.
    /// </summary>
    public static bool BelongsToCategory(string? evrakAdi, string canonicalCategory)
    {
        var c = GetCanonicalName(evrakAdi);
        return c == canonicalCategory;
    }

    /// <summary>
    /// Hızlı kart canonical ismi → detay checklist'teki en olası gerçek evrak tip adını döner.
    /// Upload sırasında doğru EvrakTipi ile ilişkilendirmek için kullanılır.
    /// </summary>
    public static string? GetPrimaryAlias(string canonicalName)
    {
        return canonicalName switch
        {
            "Kimlik" => "Kimlik Fotokopisi",
            "Ehliyet" => "Ehliyet Fotokopisi",
            "Diploma" => "Diploma Fotokopisi",
            "Saglik Raporu" => "Sağlık Raporu",
            "Sertifika" => "Sertifika",
            "Is Sozlesmesi" => "İş Sözleşmesi",
            _ => null
        };
    }

    /// <summary>
    /// Hızlı kart canonical ismine karşılık gelen OzlukEvrakKategori değerini döner.
    /// </summary>
    public static Shared.Entities.OzlukEvrakKategori? GetKategori(string canonicalName)
    {
        return canonicalName switch
        {
            "Kimlik" => Shared.Entities.OzlukEvrakKategori.KimlikBelgeleri,
            "Ehliyet" => Shared.Entities.OzlukEvrakKategori.SoforBelgeleri,
            "Diploma" => Shared.Entities.OzlukEvrakKategori.EgitimBelgeleri,
            "Saglik Raporu" => Shared.Entities.OzlukEvrakKategori.SaglikBelgeleri,
            "Sertifika" => Shared.Entities.OzlukEvrakKategori.SoforBelgeleri,
            "Is Sozlesmesi" => Shared.Entities.OzlukEvrakKategori.IseGirisBelgeleri,
            "Diger" => Shared.Entities.OzlukEvrakKategori.Diger,
            _ => null
        };
    }
}


