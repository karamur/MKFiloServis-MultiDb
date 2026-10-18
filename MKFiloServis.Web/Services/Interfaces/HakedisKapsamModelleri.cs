using System.Text.Json;
using System.Text.Json.Serialization;
using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Helpers;

namespace MKFiloServis.Web.Services.Interfaces;

/// <summary>Hakediş satırlarının ekranda ve raporda hangi başlık altında toplanacağı.</summary>
public enum HakedisGruplama
{
    Yok = 0,
    Cari = 1,
    AracSahibi = 2,
    Guzergah = 3,
    Arac = 4,
    Sofor = 5,
    Eslestirme = 6
}

/// <summary>Kurum (müşteri) hangi kayıttan alınır: hakediş referansı ve "Cari / kurum" filtresi buna göre çalışır.</summary>
public enum HakedisMusteriKaynagi
{
    /// <summary>Güzergâh kartındaki cari (eski davranış; kayıt yoksa varsayılan).</summary>
    Guzergah = 0,

    /// <summary>Eşleştirmedeki "Müşteri Kurum"; eşleştirmesiz puantajda puantajın kendi müşteri kurumu.</summary>
    Eslestirme = 1
}

/// <summary>Araç hakedişinin birim fiyatının kaynağı.</summary>
public enum HakedisBirimFiyatKaynagi
{
    /// <summary>Eşleştirmedeki "Taşerona / Şoföre Ödenen" ücret; yoksa puantaj taşeron tahakkuku, o da yoksa maliyet kaydı.</summary>
    EslestirmeOdenen = 0,

    /// <summary>Eşleştirmedeki "Kuruma Kesilecek Ücret"; yoksa puantaj kurum tahakkuku, o da yoksa maliyet kaydı.</summary>
    EslestirmeKesilecek = 1,

    /// <summary>Yalnızca araç maliyet kaydından (snapshot) hesaplanan iç maliyet (eski davranış).</summary>
    MaliyetKaydi = 2
}

/// <summary>
/// Hakediş kapsamı: dönem + kullanıcının seçtiği filtreler.
/// Boş liste "tümü" demektir. Aynı kapsam hem canlı önizlemede hem hakediş üretiminde kullanılır;
/// böylece ekranda görülen satırlar ile üretilen hakediş birbirinden ayrışmaz.
/// </summary>
public sealed class HakedisKapsami
{
    public int Yil { get; set; }
    public int Ay { get; set; }

    /// <summary>Dönem içinde ilk gün (dahil). Boşsa dönem başı.</summary>
    public DateTime? BaslangicTarihi { get; set; }

    /// <summary>Dönem içinde son gün (dahil). Boşsa dönem sonu.</summary>
    public DateTime? BitisTarihi { get; set; }

    /// <summary>Güzergâh kartındaki cari (kurum / müşteri).</summary>
    public List<int> CariIdleri { get; set; } = new();
    public List<int> GuzergahIdleri { get; set; } = new();

    /// <summary>FiloGuzergahEslestirme kayıtları. 0 = eşleştirmeden türemeyen (elle girilen) puantaj.</summary>
    public List<int> EslestirmeIdleri { get; set; } = new();
    public List<int> AracIdleri { get; set; } = new();
    public List<int> SoforIdleri { get; set; } = new();
    public List<AracSahiplikTipi> SahiplikTipleri { get; set; } = new();

    /// <summary>Araç sahibi anahtarları: "O" özmal, "K:{cari}" kiralık, "M:{cari}" komisyon, "T:{tedarikçi}" tedarikçi, "D" diğer.</summary>
    public List<string> SahipAnahtarlari { get; set; } = new();
    public List<ServisTuru> ServisTurleri { get; set; } = new();

    /// <summary>
    /// Müşteri kurumun kaynağı. Bir filtre değil hesaplama biçimidir; bu yüzden taslak kimliğine (<see cref="Imza"/>)
    /// girmez: aynı kapsam ve referans için taslak yerinde yeniden hesaplanır.
    /// </summary>
    public HakedisMusteriKaynagi MusteriKaynagi { get; set; }

    /// <summary>Araç hakedişinin birim fiyat kaynağı. Taslak kimliğine girmez.</summary>
    public HakedisBirimFiyatKaynagi BirimFiyatKaynagi { get; set; }

    [JsonIgnore] public DateTime DonemBaslangici => new(Yil, Ay, 1);

    /// <summary>Dönemden sonraki ilk gün (hariç).</summary>
    [JsonIgnore] public DateTime DonemBitisi => DonemBaslangici.AddMonths(1);

    public static HakedisKapsami Donem(int yil, int ay) => new() { Yil = yil, Ay = ay };

    /// <summary>Gün aralığını dönemin içine sıkıştırır; ters girilmişse yer değiştirir. İki uç da dahildir.</summary>
    public (DateTime Ilk, DateTime Son) GunAraligi()
    {
        var donemIlk = DonemBaslangici;
        var donemSon = DonemBitisi.AddDays(-1);
        var ilk = (BaslangicTarihi?.Date ?? donemIlk);
        var son = (BitisTarihi?.Date ?? donemSon);
        if (ilk > son) (ilk, son) = (son, ilk);
        if (ilk < donemIlk) ilk = donemIlk;
        if (son > donemSon) son = donemSon;
        if (ilk > son) ilk = son;
        return (ilk, son);
    }

    /// <summary>Gün aralığı ayın tamamını kapsıyorsa true.</summary>
    [JsonIgnore]
    public bool TumAyMi
    {
        get
        {
            var (ilk, son) = GunAraligi();
            return ilk == DonemBaslangici && son == DonemBitisi.AddDays(-1);
        }
    }

    /// <summary>Dönem dışında herhangi bir daraltma yapılmış mı?</summary>
    [JsonIgnore]
    public bool FiltreVarMi =>
        !TumAyMi || CariIdleri.Count > 0 || GuzergahIdleri.Count > 0 || EslestirmeIdleri.Count > 0
        || AracIdleri.Count > 0 || SoforIdleri.Count > 0 || SahiplikTipleri.Count > 0
        || SahipAnahtarlari.Count > 0 || ServisTurleri.Count > 0;

    /// <summary>
    /// Kapsamın kararlı kimliği. Aynı dönem ve aynı referans için farklı kapsamdaki taslaklar
    /// birbirinin üzerine yazılmasın diye taslağı bulmakta kullanılır. Filtre yoksa boş döner;
    /// bu sayede eski (kapsamsız) taslaklarla uyumludur.
    /// </summary>
    public string Imza()
    {
        var parcalar = new List<string>();

        void Ekle(string ad, IEnumerable<string> degerler)
        {
            var liste = degerler.Distinct().OrderBy(x => x, StringComparer.Ordinal).ToList();
            if (liste.Count > 0)
                parcalar.Add($"{ad}={string.Join(',', liste)}");
        }

        Ekle("cari", CariIdleri.Select(x => x.ToString()));
        Ekle("guz", GuzergahIdleri.Select(x => x.ToString()));
        Ekle("esl", EslestirmeIdleri.Select(x => x.ToString()));
        Ekle("arac", AracIdleri.Select(x => x.ToString()));
        Ekle("sofor", SoforIdleri.Select(x => x.ToString()));
        Ekle("tip", SahiplikTipleri.Select(x => ((int)x).ToString()));
        Ekle("sahip", SahipAnahtarlari);
        Ekle("servis", ServisTurleri.Select(x => ((int)x).ToString()));

        if (!TumAyMi)
        {
            var (ilk, son) = GunAraligi();
            parcalar.Add($"gun={ilk:yyyyMMdd}-{son:yyyyMMdd}");
        }

        return string.Join('|', parcalar);
    }

    public HakedisKapsami Kopyala() => new()
    {
        Yil = Yil,
        Ay = Ay,
        BaslangicTarihi = BaslangicTarihi,
        BitisTarihi = BitisTarihi,
        CariIdleri = new List<int>(CariIdleri),
        GuzergahIdleri = new List<int>(GuzergahIdleri),
        EslestirmeIdleri = new List<int>(EslestirmeIdleri),
        AracIdleri = new List<int>(AracIdleri),
        SoforIdleri = new List<int>(SoforIdleri),
        SahiplikTipleri = new List<AracSahiplikTipi>(SahiplikTipleri),
        SahipAnahtarlari = new List<string>(SahipAnahtarlari),
        ServisTurleri = new List<ServisTuru>(ServisTurleri),
        MusteriKaynagi = MusteriKaynagi,
        BirimFiyatKaynagi = BirimFiyatKaynagi
    };
}

/// <summary>
/// Hakedişin üretildiği kapsamı <c>Hakedis.GenerationParams</c> alanında saklayan kayıt.
/// Liste ekranında "bu hakediş neyi kapsıyor" bilgisini göstermek ve taslağı aynı kapsamla
/// yeniden üretmek için kullanılır.
/// </summary>
public sealed class HakedisUretimParametreleri
{
    private static readonly JsonSerializerOptions JsonSecenekleri = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public int Surum { get; set; } = 1;
    public string Imza { get; set; } = string.Empty;

    /// <summary>İnsan okuyabilir özet (ör. "Araç sahibi: Tedarikçi · 01.10 – 15.10").</summary>
    public string? Ozet { get; set; }
    public HakedisKapsami? Kapsam { get; set; }

    public string Serilestir() => JsonSerializer.Serialize(this, JsonSecenekleri);

    /// <summary>Bozuk veya boş içerik hata fırlatmaz; eski (kapsamsız) kayıtlar için null döner.</summary>
    public static HakedisUretimParametreleri? Coz(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize<HakedisUretimParametreleri>(json, JsonSecenekleri);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static string ImzaOku(string? json) => Coz(json)?.Imza ?? string.Empty;

    /// <summary>Hakedişin müşteri kurumu eşleştirmeden alınarak hazırlandıysa true (ReferansId doğrudan cari id'sidir).</summary>
    public static bool MusteriEslestirmedenMi(string? json) =>
        Coz(json)?.Kapsam?.MusteriKaynagi == HakedisMusteriKaynagi.Eslestirme;
}

/// <summary>Filtre kutularında gösterilecek bir seçenek.</summary>
public sealed record HakedisSecenek(
    string Anahtar,
    string Ad,
    string? Alt = null,
    string? UstAnahtar = null,
    AracSahiplikTipi? SahiplikTipi = null);

/// <summary>Seçili dönemdeki puantajlarda gerçekten bulunan değerler; filtre kutuları buradan beslenir.</summary>
public sealed class HakedisFiltreSecenekleri
{
    public List<HakedisSecenek> Cariler { get; init; } = new();

    /// <summary><see cref="HakedisSecenek.UstAnahtar"/> = güzergâhın cari id'si.</summary>
    public List<HakedisSecenek> Guzergahlar { get; init; } = new();
    public List<HakedisSecenek> Eslestirmeler { get; init; } = new();
    public List<HakedisSecenek> Araclar { get; init; } = new();
    public List<HakedisSecenek> Soforler { get; init; } = new();
    public List<HakedisSecenek> Sahipler { get; init; } = new();
    public List<AracSahiplikTipi> SahiplikTipleri { get; init; } = new();
    public List<ServisTuru> ServisTurleri { get; init; } = new();
}

/// <summary>
/// Önizlemedeki bir toplu satır: (cari, güzergâh, eşleştirme, araç, şoför) kırılımında dönem toplamları.
/// </summary>
public sealed record HakedisKapsamSatiri
{
    /// <summary>Hakediş türüne göre referans: Kurum → cari, Tedarikçi → tedarikçi, Araç → araç.</summary>
    public int ReferansId { get; init; }
    public string ReferansAd { get; init; } = string.Empty;

    public int CariId { get; init; }
    public string CariAd { get; init; } = string.Empty;
    public int GuzergahId { get; init; }
    public string GuzergahAd { get; init; } = string.Empty;

    /// <summary>0 = eşleştirmeden türemeyen puantaj.</summary>
    public int EslestirmeId { get; init; }
    public string EslestirmeAd { get; init; } = string.Empty;
    public int AracId { get; init; }
    public string Plaka { get; init; } = string.Empty;
    public AracSahiplikTipi SahiplikTipi { get; init; }
    public string SahipAnahtari { get; init; } = string.Empty;
    public string SahipAd { get; init; } = string.Empty;
    public int SoforId { get; init; }
    public string SoforAd { get; init; } = string.Empty;

    public int PuantajSayisi { get; init; }
    public decimal Sefer { get; init; }

    /// <summary>Kuruma kesilecek tutar (tahakkuk eden kurum ücreti).</summary>
    public decimal Gelir { get; init; }

    /// <summary>Taşerona / araç sahibine ödenecek tutar (komisyon ve tedarikçi araçlarında dolu).</summary>
    public decimal Gider { get; init; }

    /// <summary>Özmal / kiralık araçlar için iç maliyet.</summary>
    public decimal Maliyet { get; init; }

    /// <summary>Hakedişe girebilir mi? Girmiyorsa nedeni <see cref="UygunsuzlukNedeni"/> içindedir.</summary>
    public bool Uygun { get; init; } = true;
    public string? UygunsuzlukNedeni { get; init; }

    /// <summary>Bu satırdaki puantajlar aynı türdeki onaylı/faturalı bir hakedişte zaten var.</summary>
    public bool HakedisteMi { get; init; }

    public decimal Net => Gelir - Gider - Maliyet;

    /// <summary>
    /// Önizlemenin hakediş türüne göre hakedişe yazılacak tutar: Kurum → gelir, Tedarikçi / Araç sahibi → ödenecek gider,
    /// Araç → seçilen birim fiyat kaynağına göre araç tutarı.
    /// </summary>
    public decimal HakedisTutari { get; init; }
}

/// <summary>Kapsamı tek satırlık, okunabilir bir metne çevirir (rapor başlığı, hakediş listesi, onay mesajları).</summary>
public static class HakedisKapsamAciklayici
{
    public static string Ozet(HakedisKapsami kapsam, HakedisFiltreSecenekleri secenekler, int enFazlaAd = 3)
    {
        var parcalar = new List<string>();

        void Ekle(string baslik, IEnumerable<string> adlar)
        {
            var liste = adlar.Where(a => !string.IsNullOrWhiteSpace(a)).Distinct().ToList();
            if (liste.Count == 0)
                return;

            var gorunen = string.Join(", ", liste.Take(enFazlaAd));
            parcalar.Add(liste.Count > enFazlaAd
                ? $"{baslik}: {gorunen} +{liste.Count - enFazlaAd}"
                : $"{baslik}: {gorunen}");
        }

        string Ad(IEnumerable<HakedisSecenek> kaynak, string anahtar, string yedek) =>
            kaynak.FirstOrDefault(s => s.Anahtar == anahtar)?.Ad ?? yedek;

        Ekle("Sahiplik", kapsam.SahiplikTipleri.Select(t => SahiplikHelper.GetMetin(t)));
        Ekle("Araç sahibi", kapsam.SahipAnahtarlari.Select(a => Ad(secenekler.Sahipler, a, a)));
        Ekle("Cari", kapsam.CariIdleri.Select(id => Ad(secenekler.Cariler, id.ToString(), $"Cari #{id}")));
        Ekle("Güzergâh", kapsam.GuzergahIdleri.Select(id => Ad(secenekler.Guzergahlar, id.ToString(), $"Güzergâh #{id}")));
        Ekle("Eşleştirme", kapsam.EslestirmeIdleri.Select(id => Ad(secenekler.Eslestirmeler, id.ToString(), $"Eşleştirme #{id}")));
        Ekle("Araç", kapsam.AracIdleri.Select(id => Ad(secenekler.Araclar, id.ToString(), $"Araç #{id}")));
        Ekle("Şoför", kapsam.SoforIdleri.Select(id => Ad(secenekler.Soforler, id.ToString(), $"Şoför #{id}")));
        Ekle("Servis", kapsam.ServisTurleri.Select(ServisTuruMetni));

        if (!kapsam.TumAyMi)
        {
            var (ilk, son) = kapsam.GunAraligi();
            parcalar.Add(ilk == son ? $"Gün: {ilk:dd.MM.yyyy}" : $"Günler: {ilk:dd.MM} – {son:dd.MM.yyyy}");
        }

        return parcalar.Count == 0 ? "Tüm dönem" : string.Join(" · ", parcalar);
    }

    public static string ServisTuruMetni(ServisTuru tur) => tur switch
    {
        ServisTuru.Sabah => "Sabah",
        ServisTuru.Aksam => "Akşam",
        ServisTuru.SabahAksam => "Sabah-Akşam",
        ServisTuru.Ozel => "Özel",
        ServisTuru.YardaMesai => "Yarda mesai",
        _ => tur.ToString()
    };
}

/// <summary>Kapsama göre canlı önizleme sonucu. Veritabanına hiçbir şey yazılmaz.</summary>
public sealed class HakedisKapsamOnizleme
{
    public HakedisTipi Tip { get; init; }
    public HakedisKapsami Kapsam { get; init; } = new();
    public List<HakedisKapsamSatiri> Satirlar { get; init; } = new();
    public HakedisFiltreSecenekleri Secenekler { get; init; } = new();

    /// <summary>Kapsamdaki puantajlardan bu hakediş türüne hiç girmeyecek olanlar (ör. tedarikçi dışı araçlar).</summary>
    public int TurDisiPuantajSayisi { get; init; }
    public decimal TurDisiSefer { get; init; }

    /// <summary>Hakediş oluşturulursa kaç hakediş (referans) üretileceği.</summary>
    public int OlusacakHakedisSayisi => Satirlar
        .Where(s => s.Uygun && !s.HakedisteMi)
        .Select(s => s.ReferansId)
        .Distinct()
        .Count();
}
