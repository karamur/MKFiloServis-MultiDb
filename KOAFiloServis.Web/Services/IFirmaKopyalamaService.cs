using KOAFiloServis.Shared.Entities;

namespace KOAFiloServis.Web.Services;

/// <summary>
/// Şirketler arası kopyalama (Karar K8) için modül tanımı.
/// </summary>
public enum FirmaKopyalamaModulu
{
    Cari = 1,
    Kurum = 2,
    Guzergah = 3,
    Arac = 4,
    Sofor = 5,
    MasrafKalemi = 6
}

/// <summary>
/// Kopyalama UI'ında listelenecek tek satır.
/// </summary>
public sealed class FirmaKopyalamaKayitOzeti
{
    public int Id { get; init; }
    public string Kod { get; init; } = string.Empty;
    public string Ad { get; init; } = string.Empty;
    public string? EkBilgi { get; init; }
    public bool HedefteVarMi { get; init; }
}

/// <summary>
/// Kopyalama sonucu özet bilgi.
/// </summary>
public sealed class FirmaKopyalamaSonucu
{
    public int KopyalananSayisi { get; init; }
    public int AtlananSayisi { get; init; } // hedefte zaten olan kodlar
    public List<string> AtlananKodlar { get; init; } = new();
    public List<string> Hatalar { get; init; } = new();
}

/// <summary>
/// Şirketler arası master kart kopyalama servisi (Karar K8).
/// <para>
/// Sadece master kartlar kopyalanır; hareketler (fatura, masraf, puantaj, plaka geçmişi vb.)
/// kopyalanmaz. Kopyalanan kayıtlarda <see cref="IKopyalanabilirTenant.KaynakFirmaId"/> ve
/// <see cref="IKopyalanabilirTenant.KaynakKayitId"/> doldurulur.
/// </para>
/// </summary>
public interface IFirmaKopyalamaService
{
    /// <summary>Verilen modül için kaynak firmadaki kayıtların listesini döndürür.</summary>
    Task<List<FirmaKopyalamaKayitOzeti>> ListeleAsync(
        FirmaKopyalamaModulu modul,
        int kaynakFirmaId,
        int hedefFirmaId);

    /// <summary>Seçili kayıtları kaynaktan hedefe kopyalar.</summary>
    Task<FirmaKopyalamaSonucu> KopyalaAsync(
        FirmaKopyalamaModulu modul,
        int kaynakFirmaId,
        int hedefFirmaId,
        IEnumerable<int> kayitIds);
}
