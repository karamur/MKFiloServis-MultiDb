using MKFiloServis.Shared.Entities;

namespace MKFiloServis.Web.Services.Interfaces;

public interface IAktiviteLogService
{
    Task LogAsync(string islemTipi, string modul, string? aciklama = null, 
                  string? entityTipi = null, int? entityId = null, string? entityAdi = null,
                  string? eskiDeger = null, string? yeniDeger = null,
                  AktiviteSeviye seviye = AktiviteSeviye.Bilgi);

    Task LogEklemeAsync(string modul, string entityTipi, int entityId, string entityAdi);
    Task LogGuncellemeAsync(string modul, string entityTipi, int entityId, string entityAdi, object? eskiDeger = null, object? yeniDeger = null);
    Task LogSilmeAsync(string modul, string entityTipi, int entityId, string entityAdi);
    Task LogGoruntulemeAsync(string modul, string entityTipi, int entityId, string entityAdi, string? aciklama = null);
    Task LogHataAsync(string modul, string aciklama, Exception? ex = null);

    Task<List<AktiviteLogItem>> GetLogsAsync(AktiviteLogFilter? filter = null);
    Task<AktiviteLogDetay?> GetLogByIdAsync(int id);
    Task<AktiviteLogOzet> GetOzetAsync(int gunSayisi = 7);
    Task<int> GetLogCountAsync(DateTime? baslangic = null, DateTime? bitis = null);
    Task CleanupOldLogsAsync(int gunSakla = 90);

    // Geri alma işlemleri
    Task<GeriAlmaSonuc> GeriAlAsync(int logId);
    bool GeriAlinabilirMi(AktiviteLogDetay log);
}

public class AktiviteLogItem
{
    public int Id { get; set; }
    public DateTime IslemZamani { get; set; }
    public string IslemTipi { get; set; } = string.Empty;
    public string Modul { get; set; } = string.Empty;
    public string? EntityTipi { get; set; }
    public int? EntityId { get; set; }
    public string? EntityAdi { get; set; }
    public string? Aciklama { get; set; }
    public AktiviteSeviye Seviye { get; set; }
    public string? KullaniciAdi { get; set; }

    public string SeviyeClass => Seviye switch
    {
        AktiviteSeviye.Bilgi => "bg-info text-dark",
        AktiviteSeviye.Uyari => "bg-warning text-dark",
        AktiviteSeviye.Hata => "bg-danger",
        AktiviteSeviye.Kritik => "bg-dark",
        _ => "bg-secondary"
    };

    public string IslemTipiIcon => IslemTipi switch
    {
        "Ekleme" => "bi-plus-circle text-success",
        "Güncelleme" => "bi-pencil text-primary",
        "Silme" => "bi-trash text-danger",
        "Giriş" => "bi-box-arrow-in-right text-info",
        "Çıkış" => "bi-box-arrow-right text-secondary",
        "Görüntüleme" => "bi-eye text-secondary",
        "Hata" => "bi-exclamation-triangle text-danger",
        "Yedekleme" => "bi-database text-success",
        "Geri Yükleme" => "bi-arrow-counterclockwise text-warning",
        _ => "bi-activity text-muted"
    };
}

public class AktiviteLogDetay : AktiviteLogItem
{
    public string? IpAdresi { get; set; }
    public string? Tarayici { get; set; }
    public string? EskiDeger { get; set; }
    public string? YeniDeger { get; set; }
}

public class AktiviteLogFilter
{
    public DateTime? BaslangicTarihi { get; set; }
    public DateTime? BitisTarihi { get; set; }
    public string? Modul { get; set; }
    public string? IslemTipi { get; set; }
    public AktiviteSeviye? Seviye { get; set; }
    public string? AramaMetni { get; set; }
    public string? KullaniciAdi { get; set; }
    public string? EntityTipi { get; set; }
    public int Sayfa { get; set; } = 1;
    public int SayfaBoyutu { get; set; } = 50;
}

public class AktiviteLogOzet
{
    public int ToplamLog { get; set; }
    public int BugunLog { get; set; }
    public int EklemeAdet { get; set; }
    public int GuncellemeAdet { get; set; }
    public int SilmeAdet { get; set; }
    public int HataAdet { get; set; }
    public List<ModulAktivite> ModulAktiviteleri { get; set; } = new();
    public List<GunlukAktivite> GunlukAktiviteler { get; set; } = new();
}

public class ModulAktivite
{
    public string Modul { get; set; } = string.Empty;
    public int Adet { get; set; }
}

public class GunlukAktivite
{
    public DateTime Tarih { get; set; }
    public int Adet { get; set; }
}

public class GeriAlmaSonuc
{
    public bool Basarili { get; set; }
    public string Mesaj { get; set; } = string.Empty;
    public string? EntityTipi { get; set; }
    public int? EntityId { get; set; }
    public string? IslemTipi { get; set; }
    public int? OrijinalLogId { get; set; }
}



