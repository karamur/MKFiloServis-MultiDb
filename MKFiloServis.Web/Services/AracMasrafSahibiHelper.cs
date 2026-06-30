using MKFiloServis.Shared.Entities;

namespace MKFiloServis.Web.Services;

/// <summary>
/// Araç masraflarının ekonomik sahibini belirler. (Karar K5)
/// <para>
/// İş kuralları:
/// </para>
/// <list type="bullet">
///   <item><b>Özmal:</b> tüm masraf + C plaka + şoför firmaya aittir.</item>
///   <item><b>Kiralık:</b> C plaka kirası firma tarafından ödenir; yakıt, bakım, şoför masrafı firmaya aittir. (Kullanıcı kararı: "3 kiralık araçta c plaka kirası ödenir.")</item>
///   <item><b>Tedarikçi:</b> yakıt/bakım/şoför/plaka masrafı <b>tedarikçiye</b> aittir.</item>
///   <item><b>Her durumda</b> <see cref="MasrafKategori.Lastik"/> ve belge takibi (ruhsat/sigorta/muayene/kasko) <b>firmaya</b> aittir.</item>
/// </list>
/// </summary>
public static class AracMasrafSahibiHelper
{
    /// <summary>
    /// Verilen araç + masraf kategorisi için ekonomik sahibi döner.
    /// </summary>
    public static MasrafSahibi GetMasrafSahibi(Arac arac, MasrafKategori? kategori)
    {
        ArgumentNullException.ThrowIfNull(arac);

        // Lastik + belge masrafları her zaman firmaya ait (sahiplikten bağımsız).
        if (kategori == MasrafKategori.Lastik)
            return MasrafSahibi.Firma;

        return arac.SahiplikTipi switch
        {
            AracSahiplikTipi.Ozmal => MasrafSahibi.Firma,
            AracSahiplikTipi.Kiralik => MasrafSahibi.Firma, // Kiralık → C plaka kirası ve operasyonel masraf firmada
            AracSahiplikTipi.Komisyon => MasrafSahibi.Firma,
            AracSahiplikTipi.Tedarikci => MasrafSahibi.Tedarikci,
            AracSahiplikTipi.Diger => MasrafSahibi.Firma,
            _ => MasrafSahibi.Firma,
        };
    }

    /// <summary>
    /// Belge takibi (ruhsat/sigorta/muayene/kasko) her zaman firmaya aittir.
    /// K5: "tüm durumda da personel ve araçlar ile ilgili lastik değişimi evrakları ve belgelerin
    /// kontrol ve takibi şirkete aittir."
    /// </summary>
    public static MasrafSahibi GetBelgeTakipSahibi(Arac _) => MasrafSahibi.Firma;

    /// <summary>
    /// Şoför / personel masrafının (maaş, taksi, ulaşım fişleri) sahibi.
    /// Özmal &amp; Kiralık → firma. Tedarikçi → tedarikçi.
    /// </summary>
    public static MasrafSahibi GetSoforMasrafSahibi(Arac arac)
    {
        ArgumentNullException.ThrowIfNull(arac);
        return arac.SahiplikTipi == AracSahiplikTipi.Tedarikci
            ? MasrafSahibi.Tedarikci
            : MasrafSahibi.Firma;
    }

    /// <summary>
    /// Kiralık araçta firmanın araç sahibine ödediği C plaka kirası.
    /// Sadece <see cref="AracSahiplikTipi.Kiralik"/> için pozitif tutar döner.
    /// </summary>
    public static decimal? HesaplaCPlakaKirasi(Arac arac, int? aylikGunSayisi = null, int? seferSayisi = null)
    {
        ArgumentNullException.ThrowIfNull(arac);
        if (arac.SahiplikTipi != AracSahiplikTipi.Kiralik) return null;

        return arac.KiraHesaplamaTipi switch
        {
            KiraHesaplamaTipi.Aylik => arac.AylikKiraBedeli,
            KiraHesaplamaTipi.Gunluk => aylikGunSayisi.HasValue && arac.GunlukKiraBedeli.HasValue
                ? arac.GunlukKiraBedeli.Value * aylikGunSayisi.Value
                : null,
            KiraHesaplamaTipi.SeferBasina => seferSayisi.HasValue && arac.SeferBasinaKiraBedeli.HasValue
                ? arac.SeferBasinaKiraBedeli.Value * seferSayisi.Value
                : null,
            _ => arac.AylikKiraBedeli ?? arac.GunlukKiraBedeli
        };
    }
}

/// <summary>
/// Bir maliyet kaleminin ekonomik sahibi.
/// </summary>
public enum MasrafSahibi
{
    /// <summary>Masraf operatör firmaya aittir (özmal/kiralık).</summary>
    Firma = 1,
    /// <summary>Masraf taşıma tedarikçisine aittir (tedarikçi aracı).</summary>
    Tedarikci = 2
}


