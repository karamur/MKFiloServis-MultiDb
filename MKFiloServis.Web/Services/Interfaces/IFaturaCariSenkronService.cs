using MKFiloServis.Shared.Entities;

namespace MKFiloServis.Web.Services.Interfaces;

/// <summary>
/// Faturaların cariye ve Borç/Alacak hesaplarına yansımasını belirli bir tarih aralığı için yeniden kurar.
/// Cari Listesi ve Borç/Alacak Takip bakiyeleri faturalardan canlı hesaplanır; bu servis, bu hesabın doğru
/// carilere (ve doğru cari tipine) bakmasını ve eksik muhasebe fişlerinin tamamlanmasını sağlar. Tekrar çalıştırmak güvenlidir.
/// </summary>
public interface IFaturaCariSenkronService
{
    /// <summary>Verilen tarih aralığındaki faturaları tarar; <see cref="FaturaCariSenkronIstegi.Uygula"/> false ise yalnızca ne yapılacağını bildirir.</summary>
    Task<FaturaCariSenkronSonucu> GuncelleAsync(FaturaCariSenkronIstegi istek, CancellationToken cancellationToken = default);

    /// <summary>
    /// Aktarma sonrası: verilen carilerin tipini faturalarının yönüne göre genişletir.
    /// Güncellenen carilerin adlarını <paramref name="guncellenenler"/> listesine ekler ve güncellenen cari sayısını döndürür.
    /// </summary>
    Task<int> CariTipleriniGuncelleAsync(IReadOnlyCollection<int> cariIds, List<string>? guncellenenler = null, CancellationToken cancellationToken = default);
}

public sealed class FaturaCariSenkronIstegi
{
    public DateTime Baslangic { get; set; }
    public DateTime Bitis { get; set; }

    /// <summary>null: kesilen ve gelen faturaların ikisi de.</summary>
    public FaturaYonu? Yon { get; set; }

    /// <summary>Carinin tipini faturalarının yönüne göre genişletir (müşteri + tedarikçi).</summary>
    public bool CariTipiGuncelle { get; set; } = true;

    /// <summary>Muhasebe fişi oluşmamış faturalar için fiş üretir (muhasebe ayarlarında otomatik fiş açıksa).</summary>
    public bool EksikMuhasebeFisiOlustur { get; set; }

    /// <summary>false: hiçbir şey yazmadan yalnızca önizleme üretir.</summary>
    public bool Uygula { get; set; } = true;
}

public enum FaturaCariSenkronIslemi
{
    CariTipiGuncellendi = 1,
    MuhasebeFisiOlusturuldu = 2,
    MuhasebeFisiOlusturulamadi = 3
}

public sealed record FaturaCariSenkronSatiri(FaturaCariSenkronIslemi Islem, int? CariId, string Baslik, string Aciklama);

public sealed class FaturaCariSenkronSonucu
{
    public bool Uygulandi { get; set; }
    public int TaranFatura { get; set; }
    public int TaranCari { get; set; }
    public decimal GidenToplam { get; set; }
    public decimal GelenToplam { get; set; }

    /// <summary>Tipi güncellenen (önizlemede güncellenecek) cari sayısı.</summary>
    public int CariTipiGuncellenen { get; set; }

    /// <summary>Muhasebe fişi olmayan fatura sayısı.</summary>
    public int EksikFisSayisi { get; set; }

    public int OlusturulanFis { get; set; }
    public List<FaturaCariSenkronSatiri> Satirlar { get; } = new();
    public List<string> Uyarilar { get; } = new();

    public bool DegisiklikVar => CariTipiGuncellenen > 0 || OlusturulanFis > 0;
}
