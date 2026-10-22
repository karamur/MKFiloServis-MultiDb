namespace MKFiloServis.Web.Components.Layout;

/// <summary>
/// Ana menü veri modeli. Menü kod yerine veri olarak tanımlanır (<see cref="AnaMenuYapisi"/>); <c>AnaMenuAgaci</c> çizer.
/// Böylece bağlantıların benzersizliği ve yetki koşulları testle doğrulanabilir.
/// </summary>
public interface IMenuDugumu
{
}

/// <summary>Tek menü bağlantısı. <see cref="Kural"/> yoksa (üst koşullar geçtiyse) herkese görünür.</summary>
/// <param name="Etiket">Menüde görünen ad.</param>
/// <param name="Href">Kök göreli adres (<c>/efatura/giden</c>); en çok bir <c>anahtar=değer</c> sorgusu olabilir.</param>
/// <param name="Ikon">Bootstrap Icons sınıfı.</param>
/// <param name="IkonRengi">İkonun renk sınıfı (<c>text-success</c> gibi).</param>
/// <param name="Kural">Görünürlük koşulu; üst grup/alt grup koşullarına ek olarak aranır.</param>
/// <param name="HtmlKimligi">Dışarıdan (test/otomasyon) erişim için sabit <c>id</c>.</param>
/// <param name="EkYollar">Aynı sayfanın başka adresleri; bu adreslerdeyken de bağlantı vurgulanır.</param>
/// <param name="VarsayilanSekme">Sorgulu bağlantılarda, sorgu verilmediğinde sayfanın açtığı sekme.</param>
public sealed record MenuOgesi(
    string Etiket,
    string Href,
    string Ikon,
    string? IkonRengi = null,
    MenuKurali? Kural = null,
    string? HtmlKimligi = null,
    IReadOnlyList<string>? EkYollar = null,
    bool VarsayilanSekme = false) : IMenuDugumu;

/// <summary>Grup içindeki alt grup. <see cref="Baslik"/> boşsa öğeler başlıksız (düz) listelenir.</summary>
public sealed record MenuAltGrubu(
    string? Baslik,
    string? Ikon,
    IReadOnlyList<MenuOgesi> Ogeler,
    MenuKurali? Kural = null,
    bool VarsayilanAcik = false);

/// <summary>
/// Açılıp kapanan ana grup. Görünür öğesi kalmayan grup (ve alt grup) hiç çizilmez;
/// bu yüzden birleşen eski gruplar için ayrıca "herhangi biri" koşulu gerekmez.
/// </summary>
public sealed record MenuGrubu(
    string Anahtar,
    string Baslik,
    string Ikon,
    IReadOnlyList<MenuAltGrubu> AltGruplar,
    MenuKurali? Kural = null,
    string? HtmlKimligi = null) : IMenuDugumu;

/// <summary>Kuralların değerlendirildiği bağlam: kullanıcının yetkileri, rolü ve modül lisansları.</summary>
public interface IMenuYetkiBaglami
{
    /// <summary>Tek yetki kodu (lisanslı ve kullanıcıda var).</summary>
    bool Yetki(string kod);

    /// <summary>Kodlardan en az biri (lisanslı ve kullanıcıda var).</summary>
    bool Herhangi(IReadOnlyList<string> kodlar);

    /// <summary>Ana menü erişim kodu (<c>menu.cari</c> gibi); modül lisansı ve ilgili yetki önekleri dahil.</summary>
    bool Menu(string menuKodu);

    /// <summary>Kullanıcının rolü.</summary>
    bool Rol(string rolAdi);

    /// <summary>Yetki istemeyen bağlantı: oturum açmış her kullanıcı, kodun modülü lisanslıysa.</summary>
    bool HerKullanici(string lisansKodu);
}

/// <summary>Menü öğesi/grubu için görünürlük koşulu.</summary>
public abstract class MenuKurali
{
    public abstract bool Degerlendir(IMenuYetkiBaglami baglam);

    public static MenuKurali Yetki(string kod) => new YetkiKurali(kod);

    public static MenuKurali Herhangi(params string[] kodlar) => new HerhangiKurali(kodlar);

    public static MenuKurali Menu(string menuKodu) => new MenuKoduKurali(menuKodu);

    public static MenuKurali Rol(string rolAdi) => new RolKurali(rolAdi);

    /// <summary>Oturum açmış her kullanıcıya açık (yalnız modül lisansına bakılır).</summary>
    public static MenuKurali HerKullanici(string lisansKodu) => new HerKullaniciKurali(lisansKodu);

    public static MenuKurali Ve(params MenuKurali[] kurallar) => new VeKurali(kurallar);

    public static MenuKurali Veya(params MenuKurali[] kurallar) => new VeyaKurali(kurallar);

    private sealed class YetkiKurali(string kod) : MenuKurali
    {
        public override bool Degerlendir(IMenuYetkiBaglami baglam) => baglam.Yetki(kod);

        public override string ToString() => $"Yetki({kod})";
    }

    private sealed class HerhangiKurali(string[] kodlar) : MenuKurali
    {
        public override bool Degerlendir(IMenuYetkiBaglami baglam) => baglam.Herhangi(kodlar);

        public override string ToString() => $"Herhangi({string.Join(", ", kodlar)})";
    }

    private sealed class MenuKoduKurali(string menuKodu) : MenuKurali
    {
        public override bool Degerlendir(IMenuYetkiBaglami baglam) => baglam.Menu(menuKodu);

        public override string ToString() => $"Menu({menuKodu})";
    }

    private sealed class RolKurali(string rolAdi) : MenuKurali
    {
        public override bool Degerlendir(IMenuYetkiBaglami baglam) => baglam.Rol(rolAdi);

        public override string ToString() => $"Rol({rolAdi})";
    }

    private sealed class HerKullaniciKurali(string lisansKodu) : MenuKurali
    {
        public override bool Degerlendir(IMenuYetkiBaglami baglam) => baglam.HerKullanici(lisansKodu);

        public override string ToString() => $"HerKullanici({lisansKodu})";
    }

    private sealed class VeKurali(MenuKurali[] kurallar) : MenuKurali
    {
        public override bool Degerlendir(IMenuYetkiBaglami baglam) => kurallar.All(k => k.Degerlendir(baglam));

        public override string ToString() => $"({string.Join(" VE ", kurallar.Select(k => k.ToString()))})";
    }

    private sealed class VeyaKurali(MenuKurali[] kurallar) : MenuKurali
    {
        public override bool Degerlendir(IMenuYetkiBaglami baglam) => kurallar.Any(k => k.Degerlendir(baglam));

        public override string ToString() => $"({string.Join(" VEYA ", kurallar.Select(k => k.ToString()))})";
    }
}
