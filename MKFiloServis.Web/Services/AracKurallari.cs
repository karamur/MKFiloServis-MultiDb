using MKFiloServis.Shared.Entities;

namespace MKFiloServis.Web.Services;

/// <summary>
/// Araç kartı iş kuralları. Araç formu, servis, API ve Excel aktarımı aynı kuralı kullanır.
/// </summary>
public static class AracKurallari
{
    public const string KiralikSahipZorunluMesaji = "Kiralık araçlarda araç (plaka) sahibi cari seçimi zorunludur.";

    /// <summary>Kiralık araçta sahip (cari) seçilmemişse true döner.</summary>
    public static bool KiralikSahipEksik(AracSahiplikTipi sahiplikTipi, int? kiralikCariId)
        => sahiplikTipi == AracSahiplikTipi.Kiralik && kiralikCariId is not > 0;

    /// <summary>
    /// Güncelleme sahibi olmayan bir kiralık araç bırakıyor mu? Kayıt zaten sahipsiz kiralıksa ve sahiplik
    /// alanlarına dokunulmuyorsa (km, belge tarihi, şase düzeltmesi) engellenmez; eski kayıtlar kartı açılıp
    /// sahibi seçilene kadar kullanılabilir kalır.
    /// </summary>
    public static bool GuncellemeSahipsizKiralikBirakiyor(
        AracSahiplikTipi eskiTip, int? eskiCariId, AracSahiplikTipi yeniTip, int? yeniCariId)
        => KiralikSahipEksik(yeniTip, yeniCariId) &&
           !(eskiTip == yeniTip && SahipId(eskiCariId) == SahipId(yeniCariId));

    private static int? SahipId(int? cariId) => cariId is > 0 ? cariId : null;
}
