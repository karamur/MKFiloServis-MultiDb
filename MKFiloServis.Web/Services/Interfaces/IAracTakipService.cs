using MKFiloServis.Shared.Entities;

namespace MKFiloServis.Web.Services.Interfaces;

/// <summary>
/// Araç GPS takip sistemi servisi
/// </summary>
public interface IAracTakipService
{
    // Cihaz Yönetimi
    Task<List<AracTakipCihaz>> GetAllCihazlarAsync();
    Task<List<AracTakipCihaz>> GetAktifCihazlarAsync();
    Task<AracTakipCihaz?> GetCihazByIdAsync(int id);
    Task<AracTakipCihaz?> GetCihazByAracIdAsync(int aracId);
    Task<AracTakipCihaz?> GetCihazByCihazIdAsync(string cihazId);
    Task<AracTakipCihaz> CreateCihazAsync(AracTakipCihaz cihaz);
    Task<AracTakipCihaz> UpdateCihazAsync(AracTakipCihaz cihaz);
    Task DeleteCihazAsync(int id);
    
    // Konum Yönetimi
    Task<AracKonum?> GetSonKonumByAracIdAsync(int aracId);
    Task<AracKonum?> GetSonKonumByCihazIdAsync(string cihazId);
    Task<List<AracKonumDto>> GetTumAraclarinSonKonumlariAsync();
    Task<List<AracKonum>> GetKonumGecmisiAsync(int aracId, DateTime baslangic, DateTime bitis);
    Task<List<AracKonum>> GetKonumGecmisiByCihazAsync(int cihazId, DateTime baslangic, DateTime bitis);
    Task<AracKonum> KaydetKonumAsync(AracKonum konum);
    Task<int> KaydetKonumlarAsync(List<AracKonum> konumlar);
    
    // Bölge (Geofence) Yönetimi
    Task<List<AracBolge>> GetAllBolgelerAsync();
    Task<AracBolge?> GetBolgeByIdAsync(int id);
    Task<List<AracBolge>> GetBolgelerByAracIdAsync(int aracId);
    Task<AracBolge> CreateBolgeAsync(AracBolge bolge);
    Task<AracBolge> UpdateBolgeAsync(AracBolge bolge);
    Task DeleteBolgeAsync(int id);
    Task AtaBolgeToAracAsync(int bolgeId, int aracId);
    Task KaldirBolgeFromAracAsync(int bolgeId, int aracId);
    
    // Alarm Yönetimi
    Task<List<AracTakipAlarm>> GetAktifAlarmlarAsync();
    Task<List<AracTakipAlarm>> GetAlarmlarByAracIdAsync(int aracId, DateTime? baslangic = null, DateTime? bitis = null);
    Task<AracTakipAlarm> CreateAlarmAsync(AracTakipAlarm alarm);
    Task OkunduIsaretle(int alarmId);
    Task IslendiIsaretle(int alarmId, string? notlar = null);
    Task<int> GetOkunmamisAlarmSayisiAsync();
    
    // İstatistik ve Raporlama
    Task<AracTakipIstatistik> GetIstatistikAsync(int aracId, DateTime baslangic, DateTime bitis);
    Task<List<DurakNoktasi>> GetDurakNoktalariAsync(int aracId, DateTime baslangic, DateTime bitis, int minDurakDakika = 5);
    Task<double> HesaplaToplamMesafeAsync(int aracId, DateTime baslangic, DateTime bitis);
    
    // API Entegrasyonu
    Task<bool> TestBaglantisiAsync(string apiUrl, string apiKey);
    Task SenkronizeEtAsync();
}

/// <summary>
/// Araç son konum DTO'su
/// </summary>
public class AracKonumDto
{
    public int AracId { get; set; }
    public string Plaka { get; set; } = string.Empty;
    public string? AracMarka { get; set; }
    public string? AracModel { get; set; }
    public string? SoforAdi { get; set; }
    
    // Konum Bilgileri
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double? Hiz { get; set; }
    public double? Yon { get; set; }
    public DateTime KayitZamani { get; set; }
    public string? Adres { get; set; }
    
    // Durum Bilgileri
    public bool? KontakDurumu { get; set; }
    public bool? MotorDurumu { get; set; }
    public int? YakitSeviyesi { get; set; }
    public int? BataryaSeviyesi { get; set; }
    public int? SinyalGucu { get; set; }
    
    // Cihaz Bilgileri
    public string? CihazId { get; set; }
    public DateTime? SonIletisimZamani { get; set; }
    
    // Durum
    public AracTakipDurum Durum { get; set; }
    public string DurumAciklama { get; set; } = string.Empty;
}

/// <summary>
/// Araç takip durumu
/// </summary>
public enum AracTakipDurum
{
    Cevrimdisi = 0,      // Cihazla bağlantı yok
    Hareket = 1,         // Araç hareket halinde
    Bekliyor = 2,        // Araç durmuş, kontak açık
    Park = 3,            // Araç park halinde, kontak kapalı
    Bilinmiyor = 99
}

/// <summary>
/// Araç takip istatistikleri
/// </summary>
public class AracTakipIstatistik
{
    public int AracId { get; set; }
    public DateTime Baslangic { get; set; }
    public DateTime Bitis { get; set; }
    
    public double ToplamMesafeKm { get; set; }
    public TimeSpan ToplamHareketSuresi { get; set; }
    public TimeSpan ToplamBeklemeSuresi { get; set; }
    public double OrtalamaHizKmSaat { get; set; }
    public double MaxHizKmSaat { get; set; }
    public int DurakSayisi { get; set; }
    public int AlarmSayisi { get; set; }
    public int HizAsimiSayisi { get; set; }
    
    // Yakıt
    public int? BaslangicYakit { get; set; }
    public int? BitisYakit { get; set; }
    public int? TahminiYakitTuketimiLitre { get; set; }
}

/// <summary>
/// Durak noktası bilgisi
/// </summary>
public class DurakNoktasi
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? Adres { get; set; }
    public DateTime BaslangicZamani { get; set; }
    public DateTime? BitisZamani { get; set; }
    public TimeSpan Sure { get; set; }
    public bool KontakAcik { get; set; }
}




