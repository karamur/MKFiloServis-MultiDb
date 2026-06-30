using MKFiloServis.Shared.Entities;

namespace MKFiloServis.Web.Services.Interfaces;

public interface ILastikService
{
    // --- Depo ---
    Task<List<LastikDepo>> GetDepoListAsync();
    Task<LastikDepo?> GetDepoByIdAsync(int id);
    Task<LastikDepo> CreateDepoAsync(LastikDepo depo);
    Task<LastikDepo> UpdateDepoAsync(LastikDepo depo);
    Task DeleteDepoAsync(int id);

    // --- Stok ---
    /// <summary>Bireysel lastikleri listeler. aktif=true → aktif, false → pasif, null → tümü</summary>
    Task<List<LastikStok>> GetStokListAsync(int? depoId = null, bool? aktif = true);
    Task<LastikStok?> GetStokByIdAsync(int id);
    Task<LastikStok> CreateStokAsync(LastikStok stok);
    /// <summary>Aynı özelliklerde (marka/ebat/sezon/depo/araç vb.) belirtilen adette lastik kaydı oluşturur.</summary>
    Task<List<LastikStok>> CreateStokToplualAsync(LastikStok sablon, int adet);
    Task<LastikStok> UpdateStokAsync(LastikStok stok);
    Task DeleteStokAsync(int id);
    /// <summary>Lastiği pasife alır (hurda / atıldı)</summary>
    Task PasifAlAsync(int id);

    // --- Plaka Bazlı Envanter ve Eksik Sezon Raporu ---
    /// <summary>Her araç için, o araca atanmış (takılı veya yedek) lastiklerin listesini döner.</summary>
    Task<List<LastikPlakaEnvanteri>> GetPlakaBazliEnvanterAsync();
    /// <summary>Yaz ve/veya kış lastiği takılı olmayan plakaların listesini döner.</summary>
    Task<List<LastikEksikSezonSatiri>> GetEksikSezonRaporuAsync();

    /// <summary>
    /// Sökülüp depoya teslim edilmemiş (kayıp) lastiklerin raporu.
    /// LastikDurum.Kayip olan tüm aktif lastik kayıtlarını döner.
    /// </summary>
    Task<List<LastikKayipSatiri>> GetKayipLastikRaporuAsync();

    /// <summary>
    /// Kayıp lastiği depoya alır ve durumunu Kullanılabilir olarak günceller.
    /// </summary>
    Task<LastikStok?> KayipLastigiDepoyaAlAsync(int stokId, int depoId, string? not = null);

    /// <summary>
    /// Kayıp lastiği çöpe atar (hurda/imha). Plaka ve şoför bilgisi geçmişte saklanır.
    /// </summary>
    Task KayipLastigiCopeyAtAsync(int stokId, string? not, string? plaka, string? soforAdi);

    /// <summary>
    /// Çöpe atılan lastiklerin listesi (plaka/şoför geçmişiyle birlikte).
    /// </summary>
    Task<List<LastikCopeyAtilanSatiri>> GetCopeyAtilanLastiklerAsync();

    // --- Değişim ---
    Task<List<LastikDegisim>> GetDegisimListAsync(int? aracId = null, DateTime? baslangic = null, DateTime? bitis = null);
    Task<LastikDegisim?> GetDegisimByIdAsync(int id);
    Task<LastikDegisim> CreateDegisimAsync(LastikDegisim degisim);
    Task<LastikDegisim> UpdateDegisimAsync(LastikDegisim degisim);
    Task DeleteDegisimAsync(int id);

    // --- Rapor / Araç Detay ---
    Task<List<LastikAracDonemOzet>> GetAracDonemOzetListAsync(DateTime? baslangic = null, DateTime? bitis = null);
    Task<LastikAracDetay?> GetAracDetayAsync(int aracId, DateTime? baslangic = null, DateTime? bitis = null);

    // --- Sezon Ayarları ---
    Task<List<LastikSezonAyar>> GetSezonAyarlarAsync();
    Task<LastikSezonAyar> UpsertSezonAyarAsync(LastikSezonAyar ayar);
    Task<LastikSezonDurum> GetSezonDurumAsync();
}

public sealed class LastikAracDonemOzet
{
    public int AracId { get; set; }
    public string Plaka { get; set; } = string.Empty;
    public string AracBilgisi { get; set; } = string.Empty;
    public int DonemDegisimSayisi { get; set; }
    public bool DonemdeDegisti { get; set; }
    public int TakiliLastikSayisi { get; set; }
    public bool DortLastikAyniMi { get; set; }
    public string TakiliLastikOzeti { get; set; } = string.Empty;
    /// <summary>Aktif sezon ayarına göre doğru sezon lastiği takılı mı (null = aktif sezon tanımlı değil)</summary>
    public bool? BuSezonDegisimYapildi { get; set; }
    /// <summary>Aktif sezon adı (ör: "Yaz Dönemi")</summary>
    public string? AktifSezonAdi { get; set; }
}

public sealed class LastikAracHareketSatiri
{
    public int DegisimId { get; set; }
    public DateTime Tarih { get; set; }
    public LastikDegisimTipi DegisimTipi { get; set; }
    public int? KmDurumu { get; set; }
    public string TakilanAciklama { get; set; } = string.Empty;
    public string SokulenAciklama { get; set; } = string.Empty;
    public string YapilanYer { get; set; } = string.Empty;
    public decimal? Ucret { get; set; }
}

public sealed class LastikAracDetay
{
    public int AracId { get; set; }
    public string Plaka { get; set; } = string.Empty;
    public string AracBilgisi { get; set; } = string.Empty;
    public List<LastikAracPlakaSatiri> PlakaGecmisi { get; set; } = new();
    public List<LastikStok> TakiliLastikler { get; set; } = new();
    public List<LastikStok> DepoLastikler { get; set; } = new();
    public List<LastikAracHareketSatiri> Hareketler { get; set; } = new();
}

public sealed class LastikAracPlakaSatiri
{
    public string Plaka { get; set; } = string.Empty;
    public DateTime GirisTarihi { get; set; }
    public DateTime? CikisTarihi { get; set; }
    public bool Aktif { get; set; }
}

/// <summary>Plaka bazlı lastik envanteri (araç + ona atanmış aktif lastikler).</summary>
public sealed class LastikPlakaEnvanteri
{
    public int AracId { get; set; }
    public string Plaka { get; set; } = string.Empty;
    public string AracBilgisi { get; set; } = string.Empty;
    public DateTime? SonDegisimTarihi { get; set; }
    public List<LastikPlakaEnvanterSatiri> Lastikler { get; set; } = new();
    public int TakiliSayisi { get; set; }
    public int YedekSayisi { get; set; }
    public bool YazVar { get; set; }
    public bool KisVar { get; set; }
}

public sealed class LastikPlakaEnvanterSatiri
{
    public int StokId { get; set; }
    public string? Marka { get; set; }
    public string Ebat { get; set; } = string.Empty;
    public LastikSezon Sezon { get; set; }
    public LastikDurum Durum { get; set; }
    public string? SeriNo { get; set; }
    public bool Takili { get; set; }
    public bool Yedek { get; set; }
    public string? DepoAdi { get; set; }
}

/// <summary>Yaz ve/veya kış lastiği eksik olan plakalar için rapor satırı.</summary>
public sealed class LastikEksikSezonSatiri
{
    public int AracId { get; set; }
    public string Plaka { get; set; } = string.Empty;
    public string AracBilgisi { get; set; } = string.Empty;
    public DateTime? SonDegisimTarihi { get; set; }
    public bool YazEksik { get; set; }
    public bool KisEksik { get; set; }
    public int ToplamLastikSayisi { get; set; }
}

/// <summary>
/// Sökülüp depoya teslim edilmediği için kayıp sayılan lastik kaydı.
/// </summary>
public sealed class LastikKayipSatiri
{
    public int StokId { get; set; }
    public string? Marka { get; set; }
    public string Ebat { get; set; } = string.Empty;
    public LastikSezon Sezon { get; set; }
    public string? SeriNo { get; set; }
    public DateTime? KayipTarihi { get; set; }
    public int? KaynaklandigiAracId { get; set; }
    public string? KaynaklandigiPlaka { get; set; }
    public int? DegisimId { get; set; }
    public string? Notlar { get; set; }
}

/// <summary>
/// Çöpe atılan lastik kaydı; plaka ve şoför bilgisi atılma anındaki geçmişle birlikte tutulur.
/// </summary>
public sealed class LastikCopeyAtilanSatiri
{
    public int StokId { get; set; }
    public string? Marka { get; set; }
    public string Ebat { get; set; } = string.Empty;
    public LastikSezon Sezon { get; set; }
    public string? SeriNo { get; set; }
    /// <summary>Çöpe atılma tarihi</summary>
    public DateTime? CopeyAtmaTarihi { get; set; }
    /// <summary>Atılma anındaki araç plakası (araç değişse bile saklanır)</summary>
    public string? Plaka { get; set; }
    /// <summary>Atılma anındaki şoför adı (şoför değişse bile saklanır)</summary>
    public string? SoforAdi { get; set; }
    public string? Notlar { get; set; }
}

/// <summary>Aktif sezon durumu ve değişim yapılması gereken araç listesi.</summary>
public sealed class LastikSezonDurum
{
    /// <summary>Bugünün içinde bulunduğu aktif sezon ayarı (null = ayarsız)</summary>
    public LastikSezonAyar? AktifSezon { get; set; }

    /// <summary>Bir sonraki sezon ayarı</summary>
    public LastikSezonAyar? SonrakiSezon { get; set; }

    /// <summary>Sonraki sezon başlangıcına kalan gün (null = bilinmiyor)</summary>
    public int? SonrakiSezonKalanGun { get; set; }

    /// <summary>Uyarı eşiğine girildi mi (kalan gün ≤ UyariOncesiGun)</summary>
    public bool UyariAktif { get; set; }

    /// <summary>Aktif sezonda lastik değişimi yapması gereken araçlar</summary>
    public List<LastikSezonDegisimGereken> DegisimGerekenler { get; set; } = new();
}

/// <summary>Sezon lastik değişimi yapması gereken araç satırı.</summary>
public sealed class LastikSezonDegisimGereken
{
    public int AracId { get; set; }
    public string Plaka { get; set; } = string.Empty;
    public string AracBilgisi { get; set; } = string.Empty;
    /// <summary>Şu an takılı lastiklerin sezon özeti</summary>
    public string TakiliSezonOzeti { get; set; } = string.Empty;
    /// <summary>Bu sezon içinde mevsimlik değişim yapıldı mı</summary>
    public bool BuSezonDegisimYapildi { get; set; }
    /// <summary>En son değişim tarihi</summary>
    public DateTime? SonDegisimTarihi { get; set; }
    /// <summary>Aracın sahiplik tipi (filtreleme amaçlı)</summary>
    public MKFiloServis.Shared.Entities.AracSahiplikTipi SahiplikTipi { get; set; }
}




