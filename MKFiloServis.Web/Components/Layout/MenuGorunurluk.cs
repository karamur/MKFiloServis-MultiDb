namespace MKFiloServis.Web.Components.Layout;

/// <summary>
/// Menü kurallarının değerlendirildiği bağlam. Yetki/lisans semantiği eski <c>NavMenu</c> ile aynıdır
/// (<c>HasYetki</c>, <c>HasAnyYetki</c>, <c>HasMenuYetki</c>); değişen tek şey, bu mantığın bileşenden ayrılıp
/// lisans servisi olmadan test edilebilmesidir.
/// </summary>
public sealed class MenuGorunurluk : IMenuYetkiBaglami
{
    private readonly Func<string, bool> _lisansli;
    private readonly bool _yonetici;
    private readonly bool _yetkilerYuklendi;
    private readonly IReadOnlySet<string> _yetkiler;
    private readonly IReadOnlySet<string> _yetkiOnekleri;
    private readonly string? _rolAdi;

    /// <param name="lisansli">Yetki/menü kodunun modül lisansı var mı (<c>LicenseService.HasLicensedPermission</c>).</param>
    /// <param name="yonetici">Rolü "Admin" olan kullanıcı: lisanslı her şeyi görür.</param>
    /// <param name="yetkilerYuklendi">Kullanıcı yetkileri okundu mu; okunmadıysa yönetici dışında hiçbir şey gösterilmez.</param>
    /// <param name="yetkiler">Kullanıcıya atanmış yetki kodları.</param>
    /// <param name="yetkiOnekleri">Yetki kodlarının noktadan önceki kısımları (<c>cari.oku</c> → <c>cari</c>).</param>
    /// <param name="rolAdi">Kullanıcının rol adı.</param>
    public MenuGorunurluk(
        Func<string, bool> lisansli,
        bool yonetici,
        bool yetkilerYuklendi,
        IReadOnlySet<string> yetkiler,
        IReadOnlySet<string> yetkiOnekleri,
        string? rolAdi)
    {
        _lisansli = lisansli;
        _yonetici = yonetici;
        _yetkilerYuklendi = yetkilerYuklendi;
        _yetkiler = yetkiler;
        _yetkiOnekleri = yetkiOnekleri;
        _rolAdi = rolAdi;
    }

    /// <summary>Kural yoksa görünür; varsa değerlendirir.</summary>
    public bool Gorunur(MenuKurali? kural) => kural is null || kural.Degerlendir(this);

    public bool Yetki(string kod)
    {
        if (!_lisansli(kod)) return false;
        // Admin her şeyi görebilir
        if (_yonetici) return true;

        // Yetkiler henüz yüklenmediyse gösterme
        if (!_yetkilerYuklendi) return false;

        return _yetkiler.Contains(kod);
    }

    public bool Herhangi(IReadOnlyList<string> kodlar)
    {
        if (_yonetici) return kodlar.Any(_lisansli);
        if (!_yetkilerYuklendi) return false;

        foreach (var kod in kodlar)
        {
            if (_lisansli(kod) && _yetkiler.Contains(kod)) return true;
        }

        return false;
    }

    public bool Menu(string menuKodu)
    {
        if (!_lisansli(menuKodu)) return false;
        if (_yonetici) return true;
        if (!_yetkilerYuklendi) return false;

        if (_yetkiler.Contains(menuKodu)) return true;

        if (MenuIlgiliYetkiler.TryGetValue(menuKodu, out var menuYetkileri))
        {
            // Kullanıcı yetkileri genelde menü yetkilerinden büyük olduğu için küçük seti gez
            foreach (var y in menuYetkileri)
            {
                if (_yetkiler.Contains(y)) return true;
            }
        }

        if (MenuOnekleri.TryGetValue(menuKodu, out var onekler))
        {
            foreach (var onek in onekler)
            {
                if (_yetkiOnekleri.Contains(onek))
                    return true;
            }
        }

        return false;
    }

    public bool Rol(string rolAdi) => string.Equals(_rolAdi, rolAdi, StringComparison.Ordinal);

    /// <summary>Ana menü kodunu açan yetki önekleri (kullanıcıda bu önekle başlayan herhangi bir yetki varsa menü açılır).</summary>
    internal static readonly IReadOnlyDictionary<string, HashSet<string>> MenuOnekleri = new Dictionary<string, HashSet<string>>
    {
        ["menu.crm"] = new() { "bildirim", "mesaj", "email", "hatirlatici" },
        ["menu.cari"] = new() { "cari", "cariler", "kesilenfatura", "gelenfatura" },
        ["menu.filoservis"] = new() { "arac", "araclar", "guzergah", "guzergahlar", "serviscalisma", "toplucalisma", "aracmasraf", "filo", "hakedis", "operasyonel-hakedis", "arac-maliyet-snapshot", "tedarikciservis", "tedarikciarac", "tedarikcipersonel", "tedarikciaracevrak" },
        ["menu.muhasebe"] = new() { "muhasebe", "hesapplani", "muhasebefis", "muhaseberapor" },
        ["menu.personel"] = new() { "personel", "maas", "izin" },
        ["menu.fatura"] = new() { "fatura", "faturalar", "faturahazirlik", "kesilenfatura", "gelenfatura" },
        ["menu.bankakasa"] = new() { "banka", "bankahesap", "bankahareket" },
        ["menu.butce"] = new() { "butce", "odemeyonetim", "tekrarlayanodem" },
        ["menu.holding"] = new() { "holding" },
        ["menu.planlama"] = new() { "planlama" },
        ["menu.checklist"] = new() { "checklist", "checklistmali" },
        ["menu.raporlar"] = new() { "rapor", "raporlar", "raporozmal", "raporkirala", "raporkomisyon", "raporservis", "raporfatura", "rapormasraf", "raporekstre" },
        ["menu.satis"] = new() { "satis", "piyasa", "ilan" },
        ["menu.ayarlar"] = new() { "firma", "ayarlar", "websayfasi", "carimuhasebeayar", "belgeuyariayar", "arsivgoruntuleyici", "kullanici", "rol", "veritabani", "sistemdurumu", "aktivitelog", "yedekleme", "guncelleme", "destektalebi", "piyasakaynak", "masrafkalem", "aracsase" },
        ["menu.ebys"] = new() { "ebys", "evrak", "belgeuyari" },
        ["menu.stok"] = new() { "stok" },
    };

    /// <summary>Ana menü kodunu doğrudan açan, menü adı taşımayan yetkiler.</summary>
    internal static readonly IReadOnlyDictionary<string, HashSet<string>> MenuIlgiliYetkiler = new Dictionary<string, HashSet<string>>
    {
        ["menu.filoservis"] = new() { "raporlar.oku", "serviscalisma.oku", "tedarikciservis.oku", "tedarikciarac.oku", "tedarikcipersonel.oku", "tedarikciaracevrak.oku" },
        ["menu.satis"] = new() { "satis.oku", "piyasa.oku", "raporlar.oku" },
        ["menu.holding"] = new() { "holding.dashboard.oku", "holding.karsilastirma.oku", "holding.butce.oku", "holding.odemeler.oku", "holding.aracmaliyet.oku", "holding.personelgider.oku", "holding.hakedis.oku" },
        ["menu.planlama"] = new() { "planlama.oku", "planlama.dashboard.oku", "planlama.yaz", "planlama.sablon", "planlama.cakisma", "planlama.kaydet" },
    };
}
