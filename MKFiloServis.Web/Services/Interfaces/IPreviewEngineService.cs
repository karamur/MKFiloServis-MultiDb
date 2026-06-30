using System.Threading;

namespace MKFiloServis.Web.Services.Interfaces;

/// <summary>
/// Dry-run puantaj hesaplama motoru. DB'ye yazmaz, sadece önizleme sonucu üretir.
/// </summary>
public interface IPreviewEngineService
{
    /// <summary>Dönem + kurum için kuru çalıştırma önizlemesi yapar.</summary>
    Task<PreviewResult> PreviewAsync(int yil, int ay, int? kurumId = null, CancellationToken ct = default);

    /// <summary>İki hesap dönemi arasındaki farkları karşılaştırır.</summary>
    Task<ComparisonResult> CompareAsync(int hesapDonemiId1, int hesapDonemiId2, CancellationToken ct = default);

    /// <summary>Bir preview grubunun altındaki operasyon detaylarını döner (drill-down).</summary>
    Task<List<DrillDownOperasyon>> DrillDownAsync(int guzergahId, int aracId, int slot, int yil, int ay, CancellationToken ct = default);
}

public sealed class PreviewResult
{
    /// <summary>Bu dönem için kaç operasyon kaydı var</summary>
    public int OperasyonSayisi { get; init; }

    /// <summary>Kaç farklı Guzergah+Arac+Slot grubu oluştu</summary>
    public int GrupSayisi { get; init; }

    /// <summary>Üretilecek PuantajKayit sayısı</summary>
    public int UretilecekPuantajKayit { get; init; }

    /// <summary>Önceki aktif hesap varsa onun versiyonu, yoksa 0</summary>
    public int OncekiVersiyon { get; init; }

    /// <summary>Bu hesap hangi versiyon olacak</summary>
    public int YeniVersiyon { get; init; }

    /// <summary>Önceki aktif hesap var mı (revizyon mu?)</summary>
    public bool RevizyonYapilacak => OncekiVersiyon > 0;
    public int? OncekiHesapDonemiId { get; init; }
    public string? OncekiHesaplayan { get; init; }
    public DateTime? OncekiHesaplamaTarihi { get; init; }

    // ── Finansal özet ────────────────────────────────────────────────────
    public decimal ToplamGelir { get; init; }
    public decimal ToplamGider { get; init; }
    public decimal NetKar => ToplamGelir - ToplamGider;
    public int ToplamSeferGunu { get; init; }
    public decimal OrtalamaBirimGelir { get; init; }

    // ── Grup detayları ───────────────────────────────────────────────────
    public List<PreviewGrupDetay> Gruplar { get; init; } = new();

    // ── Uygunluk ─────────────────────────────────────────────────────────
    public bool HesaplamaYapilabilir => OperasyonSayisi > 0;
    public bool AktifHesapVar => OncekiVersiyon > 0;
    public List<string> UyariMesajlari { get; init; } = new();
}

public sealed class PreviewGrupDetay
{
    public int GuzergahId { get; init; }
    public string GuzergahAdi { get; init; } = "";
    public int AracId { get; init; }
    public string Plaka { get; init; } = "";
    public string? SoforAdi { get; init; }
    public string Slot { get; init; } = "";
    public int SeferGunuToplami { get; init; }
    public decimal BirimGelir { get; init; }
    public decimal BirimGider { get; init; }
    public decimal ToplamGelir { get; init; }
    public decimal ToplamGider { get; init; }
    public int OperasyonSayisi { get; init; }
}

public sealed class ComparisonResult
{
    public int Hesap1Id { get; init; }
    public int Hesap1Versiyon { get; init; }
    public string Hesap1Tarih { get; init; } = "";
    public int Hesap2Id { get; init; }
    public int Hesap2Versiyon { get; init; }
    public string Hesap2Tarih { get; init; } = "";

    public decimal Gelir1 { get; init; }
    public decimal Gelir2 { get; init; }
    public decimal Gider1 { get; init; }
    public decimal Gider2 { get; init; }
    public int Sefer1 { get; init; }
    public int Sefer2 { get; init; }

    public List<ComparisonDelta> Farklar { get; init; } = new();

    // Operasyon bazlı delta
    public int EklenenOperasyon { get; init; }
    public int CikarilanOperasyon { get; init; }
    public int DegisenOperasyon { get; init; }
    public int AyniOperasyon { get; init; }
    public List<OperasyonDelta> OperasyonFarklari { get; init; } = new();
}

public sealed class ComparisonDelta
{
    public string GuzergahAdi { get; init; } = "";
    public string Plaka { get; init; } = "";
    public string Slot { get; init; } = "";
    public int SeferOnceki { get; init; }
    public int SeferYeni { get; init; }
    public decimal GelirOnceki { get; init; }
    public decimal GelirYeni { get; init; }
    public int FarkSefer => SeferYeni - SeferOnceki;
    public decimal FarkGelir => GelirYeni - GelirOnceki;
}

public sealed class OperasyonDelta
{
    public DateTime Tarih { get; init; }
    public string Plaka { get; init; } = "";
    public string GuzergahAdi { get; init; } = "";
    public string Slot { get; init; } = "";
    public string? SoforAdi { get; init; }
    public int SeferOnceki { get; init; }
    public int SeferYeni { get; init; }
    public string DegisimTipi { get; init; } = ""; // "Eklendi", "Çıkarıldı", "Değişti", "Aynı"
    public decimal? GelirOnceki { get; init; }
    public decimal? GelirYeni { get; init; }
}

public sealed class DrillDownOperasyon
{
    public int Id { get; init; }
    public DateTime Tarih { get; init; }
    public string Plaka { get; init; } = "";
    public string? SoforAdi { get; init; }
    public string Slot { get; init; } = "";
    public int SeferSayisi { get; init; }
    public string Durum { get; init; } = "";
    public bool Gitti => Durum == "Gitti";
}




