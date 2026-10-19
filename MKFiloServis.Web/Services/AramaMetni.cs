using System.Text;

namespace MKFiloServis.Web.Services;

/// <summary>
/// Genel arama için Türkçe harf duyarsız eşleştirme yardımcıları.
/// <para>
/// Neden gerekli: PostgreSQL <c>lower()</c> bu veritabanında 'İ' (noktalı büyük i) harfini küçültmez
/// (<c>lower('ALİ') = 'alİ'</c>), C# tarafında ise tr-TR kültüründe 'I' → 'ı' olur. "ali", "filo", "demir" gibi
/// aramalar ALİ / FİLO / DEMİREZ kayıtlarını bu yüzden bulamıyordu. Çözüm iki aşamalıdır: veritabanı yalnızca
/// "kaba" bir LIKE deseniyle adayları getirir (duyarsız harfler '_' joker olur), kesin eşleştirme bellekte
/// <see cref="Katla"/> ile yapılır.
/// </para>
/// </summary>
public static class AramaMetni
{
    /// <summary>LIKE ifadelerinde joker karakterleri kaçırmak için kullanılan kaçış karakteri.</summary>
    public const string KacisKarakteri = "\\";

    /// <summary>
    /// Türkçe harfleri ASCII karşılığına indirip küçültür: "ALİ", "Ali", "ali", "ALI", "alı" aynı olur;
    /// ş→s, ğ→g, ü→u, ö→o, ç→c. Birleşik nokta işareti (i + U+0307) atılır.
    /// </summary>
    public static string Katla(string? metin)
    {
        if (string.IsNullOrEmpty(metin))
            return string.Empty;

        var sb = new StringBuilder(metin.Length);
        foreach (var ch in metin)
        {
            switch (ch)
            {
                case 'İ' or 'I' or 'ı' or 'i':
                    sb.Append('i');
                    break;
                case 'Ş' or 'ş':
                    sb.Append('s');
                    break;
                case 'Ğ' or 'ğ':
                    sb.Append('g');
                    break;
                case 'Ü' or 'ü':
                    sb.Append('u');
                    break;
                case 'Ö' or 'ö':
                    sb.Append('o');
                    break;
                case 'Ç' or 'ç':
                    sb.Append('c');
                    break;
                case '̇': // "İ".ToLowerInvariant() sonrası kalan birleşik nokta
                    break;
                default:
                    sb.Append(char.ToLowerInvariant(ch));
                    break;
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Aramayı kelimelere ayırır ve her kelimeyi katlar. Boşluk, virgül ve noktalı virgül ayraçtır.
    /// Çok kelimeli aramalar ("ali aydın") kelimelerin hepsinin kayıtta geçmesini ister.
    /// </summary>
    public static IReadOnlyList<string> Kelimeler(string? arama)
    {
        if (string.IsNullOrWhiteSpace(arama))
            return Array.Empty<string>();

        return arama
            .Split([' ', '\t', ',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(Katla)
            .Where(k => k.Length > 0)
            .Distinct()
            .ToList();
    }

    /// <summary>Veritabanı sorgusu için en seçici (en uzun) kelime.</summary>
    public static string EnUzun(IReadOnlyList<string> kelimeler)
        => kelimeler.Count == 0 ? string.Empty : kelimeler.OrderByDescending(k => k.Length).First();

    /// <summary>
    /// Katlanmış kelimeden kaba LIKE deseni üretir ("%...%"): i/s/g/u/o/c ailesi harfleri tek karakterlik '_' joker
    /// olur (İ, I, ı, Ş, ş, … hepsini yakalar), diğer harfler küçültülür, <c>%</c> <c>_</c> <c>\</c> kaçırılır.
    /// Kullanım: <c>EF.Functions.Like(kolon.ToLower(), desen, AramaMetni.KacisKarakteri)</c>.
    /// </summary>
    public static string KabaDesen(string katlanmisKelime)
    {
        var sb = new StringBuilder(katlanmisKelime.Length + 2);
        sb.Append('%');
        foreach (var ch in katlanmisKelime)
        {
            switch (ch)
            {
                case 'i' or 's' or 'g' or 'u' or 'o' or 'c':
                    sb.Append('_');
                    break;
                case '%' or '_' or '\\':
                    sb.Append('\\').Append(ch);
                    break;
                default:
                    sb.Append(char.ToLowerInvariant(ch));
                    break;
            }
        }

        sb.Append('%');
        return sb.ToString();
    }

    /// <summary>
    /// Tüm kelimeler, verilen alanların birleşiminde (katlanarak) geçiyor mu. Boşluksuz yazım da kabul edilir:
    /// "34abc123" araması "34 ABC 123" plakasını bulur.
    /// </summary>
    public static bool HepsiEslesir(IReadOnlyList<string> kelimeler, params string?[] alanlar)
    {
        if (kelimeler.Count == 0)
            return false;

        var birlesik = string.Join(' ', alanlar.Select(Katla));
        var bosluksuz = birlesik.Replace(" ", string.Empty);
        return kelimeler.All(k =>
            birlesik.Contains(k, StringComparison.Ordinal) || bosluksuz.Contains(k, StringComparison.Ordinal));
    }

    /// <summary>
    /// Eşleşme puanı: başlık tam eşleşirse en yüksek, başlık kelimeyle başlarsa orta, içeriyorsa düşük.
    /// İkincil alan (kod/plaka) için ayrı puan eklenir. Çok kelimede ilk kelime baz alınır.
    /// </summary>
    public static int Puan(IReadOnlyList<string> kelimeler, string? baslik, string? ikincil)
    {
        if (kelimeler.Count == 0)
            return 0;

        var tamArama = string.Join(' ', kelimeler);
        var b = Katla(baslik);
        var i = Katla(ikincil);
        var puan = 0;

        if (b == tamArama) puan += 100;
        else if (b.StartsWith(tamArama, StringComparison.Ordinal)) puan += 80;
        else if (b.Contains(tamArama, StringComparison.Ordinal)) puan += 50;
        else if (kelimeler.All(k => b.Contains(k, StringComparison.Ordinal))) puan += 30;

        if (i == tamArama) puan += 60;
        else if (i.StartsWith(tamArama, StringComparison.Ordinal)) puan += 40;
        else if (i.Contains(tamArama, StringComparison.Ordinal)) puan += 20;

        return puan;
    }
}
