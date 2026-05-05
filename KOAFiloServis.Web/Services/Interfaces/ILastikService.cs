using KOAFiloServis.Shared.Entities;

namespace KOAFiloServis.Web.Services.Interfaces;

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

    // --- Değişim ---
    Task<List<LastikDegisim>> GetDegisimListAsync(int? aracId = null, DateTime? baslangic = null, DateTime? bitis = null);
    Task<LastikDegisim?> GetDegisimByIdAsync(int id);
    Task<LastikDegisim> CreateDegisimAsync(LastikDegisim degisim);
    Task<LastikDegisim> UpdateDegisimAsync(LastikDegisim degisim);
    Task DeleteDegisimAsync(int id);

    // --- Rapor / Araç Detay ---
    Task<List<LastikAracDonemOzet>> GetAracDonemOzetListAsync(DateTime? baslangic = null, DateTime? bitis = null);
    Task<LastikAracDetay?> GetAracDetayAsync(int aracId, DateTime? baslangic = null, DateTime? bitis = null);
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
    public bool YazEksik { get; set; }
    public bool KisEksik { get; set; }
    public int ToplamLastikSayisi { get; set; }
}
