using MKFiloServis.Shared.Entities;

namespace MKFiloServis.Web.Services.Interfaces;

/// <summary>
/// SMS gönderim servisi interface
/// </summary>
public interface ISmsService
{
    // SMS Ayarları
    Task<SmsAyar?> GetAktifAyarAsync();
    Task<List<SmsAyar>> GetTumAyarlarAsync();
    Task<SmsAyar> SaveAyarAsync(SmsAyar ayar);
    Task<bool> TestBaglantisiAsync(int ayarId);
    Task<decimal?> BakiyeSorgulaAsync(int ayarId);

    // SMS Gönderimi
    Task<SmsGonderimSonuc> GonderAsync(string telefon, string mesaj, SmsTipi tip = SmsTipi.Bildirim, string? iliskiliTablo = null, int? iliskiliKayitId = null);
    Task<List<SmsGonderimSonuc>> TopluGonderAsync(List<SmsGonderimIstek> istekler);
    
    // Şablonlu Gönderim
    Task<SmsGonderimSonuc> SablonlaGonderAsync(string telefon, int sablonId, Dictionary<string, string> degiskenler, string? iliskiliTablo = null, int? iliskiliKayitId = null);

    // SMS Logları
    Task<List<SmsLog>> GetLoglarAsync(DateTime? baslangic = null, DateTime? bitis = null, SmsGonderimDurum? durum = null, int? limit = 100);
    Task<SmsLog?> GetLogByIdAsync(int id);
    Task<SmsIstatistik> GetIstatistikAsync(DateTime? baslangic = null, DateTime? bitis = null);

    // SMS Şablonları
    Task<List<SmsSablon>> GetSablonlarAsync(SmsTipi? tip = null);
    Task<SmsSablon?> GetSablonByIdAsync(int id);
    Task<SmsSablon> SaveSablonAsync(SmsSablon sablon);
    Task DeleteSablonAsync(int id);
    Task<SmsSablon?> GetVarsayilanSablonAsync(SmsTipi tip);

    // Bildirim Entegrasyonu
    Task<int> BildirimSmsGonderAsync(); // Bildirim ayarlarına göre SMS gönder
}

/// <summary>
/// SMS gönderim sonucu
/// </summary>
public class SmsGonderimSonuc
{
    public bool Basarili { get; set; }
    public string? MesajId { get; set; }
    public string? HataMesaji { get; set; }
    public int? LogId { get; set; }
    public decimal? KalanBakiye { get; set; }
}

/// <summary>
/// Toplu SMS gönderim isteği
/// </summary>
public class SmsGonderimIstek
{
    public string Telefon { get; set; } = string.Empty;
    public string Mesaj { get; set; } = string.Empty;
    public SmsTipi Tip { get; set; } = SmsTipi.Bildirim;
    public string? IliskiliTablo { get; set; }
    public int? IliskiliKayitId { get; set; }
}

/// <summary>
/// SMS istatistikleri
/// </summary>
public class SmsIstatistik
{
    public int ToplamGonderen { get; set; }
    public int Basarili { get; set; }
    public int Basarisiz { get; set; }
    public int Bekleyen { get; set; }
    public decimal? KalanBakiye { get; set; }
    public Dictionary<SmsTipi, int> TipeBazliSayilar { get; set; } = new();
    public Dictionary<string, int> GunlukGonderim { get; set; } = new(); // Son 7 gün
}




