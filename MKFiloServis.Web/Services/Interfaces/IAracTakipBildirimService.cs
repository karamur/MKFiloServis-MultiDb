using MKFiloServis.Web.Hubs;

namespace MKFiloServis.Web.Services.Interfaces;

/// <summary>
/// SignalR üzerinden gerçek zamanlı araç takip bildirimleri gönderen servis
/// </summary>
public interface IAracTakipBildirimService
{
    /// <summary>
    /// Tek bir aracın konum güncellemesini tüm ilgili client'lara gönder
    /// </summary>
    Task KonumGuncellemesiGonderAsync(AracKonumGuncelleme guncelleme);

    /// <summary>
    /// Birden fazla aracın konum güncellemesini toplu gönder
    /// </summary>
    Task TopluKonumGuncellemesiGonderAsync(IEnumerable<AracKonumGuncelleme> guncellemeler);

    /// <summary>
    /// Alarm bildirimini ilgili client'lara gönder
    /// </summary>
    Task AlarmBildirimiGonderAsync(AlarmBildirimi alarm);

    /// <summary>
    /// Bölge giriş/çıkış olayını gönder
    /// </summary>
    Task BolgeOlayiGonderAsync(BolgeOlayi olay);

    /// <summary>
    /// Belirli bir araca özel mesaj gönder
    /// </summary>
    Task AracaMesajGonderAsync(int aracId, string mesajTipi, object data);

    /// <summary>
    /// Tüm bağlı client'lara sistem mesajı gönder
    /// </summary>
    Task SistemMesajiGonderAsync(string mesaj, string tip = "info");
}




