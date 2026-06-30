using MKFiloServis.Shared.Entities;

namespace MKFiloServis.Web.Helpers;

/// <summary>
/// Araç sahiplik tipleri için ortak UI yardımcı sınıfı.
/// Tüm Blazor sayfalarında tutarlı renk, metin ve ikon kullanımı sağlar.
/// </summary>
public static class SahiplikHelper
{
    // ---------- AracSahiplikTipi (5 değer: Özmal, Kiralık, Komisyon, Diğer, Tedarikçi) ----------

    public static string GetMetin(AracSahiplikTipi tip) => tip switch
    {
        AracSahiplikTipi.Ozmal => "Özmal",
        AracSahiplikTipi.Kiralik => "Kiralık",
        AracSahiplikTipi.Komisyon => "Komisyon",
        AracSahiplikTipi.Tedarikci => "Tedarikçi",
        AracSahiplikTipi.Diger => "Diğer",
        _ => "-"
    };

    public static string GetBadgeClass(AracSahiplikTipi tip) => tip switch
    {
        AracSahiplikTipi.Ozmal => "bg-success",
        AracSahiplikTipi.Kiralik => "bg-warning text-dark",
        AracSahiplikTipi.Komisyon => "bg-info text-dark",
        AracSahiplikTipi.Tedarikci => "bg-primary",
        AracSahiplikTipi.Diger => "bg-secondary",
        _ => "bg-light text-dark"
    };

    public static string GetIcon(AracSahiplikTipi tip) => tip switch
    {
        AracSahiplikTipi.Ozmal => "bi-house-check",
        AracSahiplikTipi.Kiralik => "bi-key",
        AracSahiplikTipi.Komisyon => "bi-people",
        AracSahiplikTipi.Tedarikci => "bi-truck",
        AracSahiplikTipi.Diger => "bi-three-dots",
        _ => "bi-question-circle"
    };

    public static string GetAlertClass(AracSahiplikTipi tip) => tip switch
    {
        AracSahiplikTipi.Ozmal => "alert-success",
        AracSahiplikTipi.Kiralik => "alert-warning",
        AracSahiplikTipi.Komisyon => "alert-info",
        AracSahiplikTipi.Tedarikci => "alert-primary",
        AracSahiplikTipi.Diger => "alert-secondary",
        _ => "alert-light"
    };

    public static string GetAciklama(AracSahiplikTipi tip) => tip switch
    {
        AracSahiplikTipi.Ozmal => "Plaka, şoför ve araç firmaya aittir. Taşeron ödemesi yapılmaz.",
        AracSahiplikTipi.Kiralik => "Plaka kiralık; firma araç sahibine C plaka kirası öder. Yakıt, bakım, şoför masrafları firmaya aittir. Belge takibi (sigorta/muayene/kasko/ruhsat) ve lastik değişimi her durumda firmaya aittir.",
        AracSahiplikTipi.Komisyon => "Plaka, şoför, araç ve operasyon masrafları komisyoncuya aittir. Taşeron ücreti aktif kullanılır.",
        AracSahiplikTipi.Tedarikci => "Araç, plaka ve şoför tedarikçiye aittir; operasyonel masraflar (yakıt, bakım, plaka, şoför) tedarikçiye yansır. Lastik değişimi ve belge takibi (sigorta/muayene/kasko/ruhsat) yine firmaya aittir.",
        AracSahiplikTipi.Diger => "Sahiplik tipi belirtilmemiş.",
        _ => "Ek kural tanımlanmamış."
    };

    // ---------- AracSahiplikKalem (3 değer: İhale modülü için) ----------

    public static string GetMetin(AracSahiplikKalem kalem) => kalem switch
    {
        AracSahiplikKalem.Ozmal => "Özmal",
        AracSahiplikKalem.Kiralik => "Kiralık",
        AracSahiplikKalem.Komisyon => "Komisyon",
        _ => "-"
    };

    public static string GetBadgeClass(AracSahiplikKalem kalem) => kalem switch
    {
        AracSahiplikKalem.Ozmal => "bg-success",
        AracSahiplikKalem.Kiralik => "bg-warning text-dark",
        AracSahiplikKalem.Komisyon => "bg-info text-dark",
        _ => "bg-secondary"
    };

    public static string GetIcon(AracSahiplikKalem kalem) => kalem switch
    {
        AracSahiplikKalem.Ozmal => "bi-house-check",
        AracSahiplikKalem.Kiralik => "bi-key",
        AracSahiplikKalem.Komisyon => "bi-people",
        _ => "bi-question-circle"
    };
}


