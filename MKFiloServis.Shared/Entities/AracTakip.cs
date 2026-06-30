namespace MKFiloServis.Shared.Entities;

/// <summary>
/// Araç GPS takip cihazı tanımı
/// </summary>
public class AracTakipCihaz : BaseEntity
{
    public int AracId { get; set; }
    public virtual Arac Arac { get; set; } = null!;
    
    /// <summary>
    /// Cihaz seri numarası / IMEI
    /// </summary>
    public string CihazId { get; set; } = string.Empty;
    
    /// <summary>
    /// Cihaz markası (örn: Teltonika, Queclink, Concox)
    /// </summary>
    public string? CihazMarka { get; set; }
    
    /// <summary>
    /// Cihaz modeli
    /// </summary>
    public string? CihazModel { get; set; }
    
    /// <summary>
    /// SIM kart numarası
    /// </summary>
    public string? SimKartNo { get; set; }
    
    /// <summary>
    /// Cihaz aktif mi?
    /// </summary>
    public bool Aktif { get; set; } = true;
    
    /// <summary>
    /// Kurulum tarihi
    /// </summary>
    public DateTime? KurulumTarihi { get; set; }
    
    /// <summary>
    /// Son iletişim zamanı
    /// </summary>
    public DateTime? SonIletisimZamani { get; set; }
    
    /// <summary>
    /// Batarya seviyesi (%)
    /// </summary>
    public int? BataryaSeviyesi { get; set; }
    
    /// <summary>
    /// GSM sinyal gücü (0-5)
    /// </summary>
    public int? SinyalGucu { get; set; }
    
    /// <summary>
    /// Notlar
    /// </summary>
    public string? Notlar { get; set; }
    
    // Navigation
    public virtual ICollection<AracKonum> Konumlar { get; set; } = new List<AracKonum>();
}

/// <summary>
/// Araç konum kaydı (GPS verisi)
/// </summary>
public class AracKonum : BaseEntity
{
    public int AracTakipCihazId { get; set; }
    public virtual AracTakipCihaz AracTakipCihaz { get; set; } = null!;
    
    /// <summary>
    /// GPS enlem koordinatı
    /// </summary>
    public double Latitude { get; set; }
    
    /// <summary>
    /// GPS boylam koordinatı
    /// </summary>
    public double Longitude { get; set; }
    
    /// <summary>
    /// Hız (km/s)
    /// </summary>
    public double? Hiz { get; set; }
    
    /// <summary>
    /// Yön (derece, 0-360)
    /// </summary>
    public double? Yon { get; set; }
    
    /// <summary>
    /// Rakım (metre)
    /// </summary>
    public double? Rakım { get; set; }
    
    /// <summary>
    /// GPS hassasiyeti (metre)
    /// </summary>
    public double? Hassasiyet { get; set; }
    
    /// <summary>
    /// Konum kaydedilme zamanı (cihazdan gelen)
    /// </summary>
    public DateTime KayitZamani { get; set; }
    
    /// <summary>
    /// Kontak durumu (açık/kapalı)
    /// </summary>
    public bool? KontakDurumu { get; set; }
    
    /// <summary>
    /// Motor durumu (çalışıyor/durmuş)
    /// </summary>
    public bool? MotorDurumu { get; set; }
    
    /// <summary>
    /// Yakıt seviyesi (%)
    /// </summary>
    public int? YakitSeviyesi { get; set; }
    
    /// <summary>
    /// Kilometre sayacı
    /// </summary>
    public int? Kilometre { get; set; }
    
    /// <summary>
    /// Sıcaklık (°C)
    /// </summary>
    public double? Sicaklik { get; set; }
    
    /// <summary>
    /// Olay tipi (normal konum, alarm, durak, vb.)
    /// </summary>
    public KonumOlayTipi OlayTipi { get; set; } = KonumOlayTipi.Normal;
    
    /// <summary>
    /// Adres (ters geocoding ile doldurulabilir)
    /// </summary>
    public string? Adres { get; set; }
}

/// <summary>
/// Araç için tanımlanan bölgeler (Geofence)
/// </summary>
public class AracBolge : BaseEntity
{
    public string BolgeAdi { get; set; } = string.Empty;
    
    /// <summary>
    /// Bölge tipi
    /// </summary>
    public BolgeTipi Tip { get; set; } = BolgeTipi.Daire;
    
    /// <summary>
    /// Merkez enlem (daire için)
    /// </summary>
    public double? MerkezLatitude { get; set; }
    
    /// <summary>
    /// Merkez boylam (daire için)
    /// </summary>
    public double? MerkezLongitude { get; set; }
    
    /// <summary>
    /// Yarıçap metre (daire için)
    /// </summary>
    public double? YaricapMetre { get; set; }
    
    /// <summary>
    /// Poligon koordinatları (JSON formatında) - çokgen için
    /// </summary>
    public string? PoligonKoordinatlari { get; set; }
    
    /// <summary>
    /// Bölge rengi (hex)
    /// </summary>
    public string? Renk { get; set; }
    
    /// <summary>
    /// Bölgeye giriş bildirimini aç
    /// </summary>
    public bool GirisBildirimi { get; set; } = true;
    
    /// <summary>
    /// Bölgeden çıkış bildirimini aç
    /// </summary>
    public bool CikisBildirimi { get; set; } = true;
    
    /// <summary>
    /// Aktif mi?
    /// </summary>
    public bool Aktif { get; set; } = true;
    
    public string? Notlar { get; set; }
    
    // Navigation - hangi araçlara uygulanacak
    public virtual ICollection<AracBolgeAtama> Atamalar { get; set; } = new List<AracBolgeAtama>();
}

/// <summary>
/// Bölge-Araç ataması
/// </summary>
public class AracBolgeAtama : BaseEntity
{
    public int AracBolgeId { get; set; }
    public virtual AracBolge AracBolge { get; set; } = null!;
    
    public int AracId { get; set; }
    public virtual Arac Arac { get; set; } = null!;
}

/// <summary>
/// Araç takip alarm kaydı
/// </summary>
public class AracTakipAlarm : BaseEntity
{
    public int AracTakipCihazId { get; set; }
    public virtual AracTakipCihaz AracTakipCihaz { get; set; } = null!;
    
    public AlarmTipi AlarmTipi { get; set; }
    
    /// <summary>
    /// Alarm zamanı
    /// </summary>
    public DateTime AlarmZamani { get; set; }
    
    /// <summary>
    /// Alarm konumu - enlem
    /// </summary>
    public double? Latitude { get; set; }
    
    /// <summary>
    /// Alarm konumu - boylam
    /// </summary>
    public double? Longitude { get; set; }
    
    /// <summary>
    /// Alarm mesajı
    /// </summary>
    public string? Mesaj { get; set; }
    
    /// <summary>
    /// Alarm değeri (hız, sıcaklık vb.)
    /// </summary>
    public double? Deger { get; set; }
    
    /// <summary>
    /// Okundu mu?
    /// </summary>
    public bool Okundu { get; set; } = false;
    
    /// <summary>
    /// İşlendi mi?
    /// </summary>
    public bool Islendi { get; set; } = false;
    
    public string? Notlar { get; set; }
}

/// <summary>
/// Konum kayıt tipi
/// </summary>
public enum KonumOlayTipi
{
    Normal = 1,           // Normal periyodik konum
    KontakAcildi = 2,     // Kontak açıldı
    KontakKapandi = 3,    // Kontak kapandı
    HizAlarm = 4,         // Hız aşımı
    BolgeGiris = 5,       // Bölgeye giriş
    BolgeCikis = 6,       // Bölgeden çıkış
    DurakBasla = 7,       // Araç durdu
    DurakBit = 8,         // Araç hareket etti
    SOS = 9,              // SOS butonu basıldı
    Cekme = 10,           // Araç çekildi/hareket (kontak kapalı)
    DusukBatarya = 11,    // Düşük batarya
    Diger = 99
}

/// <summary>
/// Bölge tipi
/// </summary>
public enum BolgeTipi
{
    Daire = 1,      // Dairesel bölge (merkez + yarıçap)
    Cokgen = 2,     // Çokgen bölge (poligon)
    Koridor = 3     // Koridor/Rota bölgesi
}

/// <summary>
/// Alarm tipi
/// </summary>
public enum AlarmTipi
{
    HizAsimi = 1,           // Hız limiti aşıldı
    BolgeGiris = 2,         // Yasak bölgeye giriş
    BolgeCikis = 3,         // İzin verilen bölgeden çıkış
    KontakAcildi = 4,       // Mesai dışı kontak
    UzunSureliBekleme = 5,  // Belirlenen sürenin üzerinde bekleme
    DusukYakit = 6,         // Düşük yakıt
    YuksekSicaklik = 7,     // Yüksek sıcaklık
    DusukBatarya = 8,       // Düşük cihaz bataryası
    SinyalKaybi = 9,        // GPS/GSM sinyal kaybı
    Cekme = 10,             // Araç çekildi (kontak kapalı hareket)
    SOS = 11,               // SOS butonu
    Carpisma = 12,          // Çarpışma algılandı
    Diger = 99
}


