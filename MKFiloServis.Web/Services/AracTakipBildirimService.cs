using Microsoft.AspNetCore.SignalR;
using MKFiloServis.Web.Hubs;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Web.Services;

/// <summary>
/// SignalR üzerinden gerçek zamanlı araç takip bildirimleri gönderen servis implementasyonu
/// </summary>
public class AracTakipBildirimService : IAracTakipBildirimService
{
    private readonly IHubContext<AracTakipHub> _hubContext;
    private readonly ILogger<AracTakipBildirimService> _logger;

    public AracTakipBildirimService(
        IHubContext<AracTakipHub> hubContext,
        ILogger<AracTakipBildirimService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    /// <summary>
    /// Tek bir aracın konum güncellemesini tüm ilgili client'lara gönder
    /// </summary>
    public async Task KonumGuncellemesiGonderAsync(AracKonumGuncelleme guncelleme)
    {
        try
        {
            // 1. Tüm araçları takip edenlere gönder
            await _hubContext.Clients.Group("TumAraclar")
                .SendAsync("KonumGuncellendi", guncelleme);

            // 2. Bu aracı özellikle takip edenlere gönder
            var aracGrubu = $"Arac_{guncelleme.AracId}";
            await _hubContext.Clients.Group(aracGrubu)
                .SendAsync("AracKonumGuncellendi", guncelleme);

            _logger.LogDebug("Konum güncellemesi gönderildi: Araç {AracId}, Plaka {Plaka}", 
                guncelleme.AracId, guncelleme.Plaka);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Konum güncellemesi gönderilemedi: Araç {AracId}", guncelleme.AracId);
        }
    }

    /// <summary>
    /// Birden fazla aracın konum güncellemesini toplu gönder
    /// </summary>
    public async Task TopluKonumGuncellemesiGonderAsync(IEnumerable<AracKonumGuncelleme> guncellemeler)
    {
        try
        {
            var guncellemelerList = guncellemeler.ToList();
            
            // Tüm güncellemeleri tek seferde gönder (daha verimli)
            await _hubContext.Clients.Group("TumAraclar")
                .SendAsync("TopluKonumGuncellendi", guncellemelerList);

            // Her araç için ayrı grup güncellemesi
            foreach (var guncelleme in guncellemelerList)
            {
                var aracGrubu = $"Arac_{guncelleme.AracId}";
                await _hubContext.Clients.Group(aracGrubu)
                    .SendAsync("AracKonumGuncellendi", guncelleme);
            }

            _logger.LogDebug("Toplu konum güncellemesi gönderildi: {Count} araç", guncellemelerList.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Toplu konum güncellemesi gönderilemedi");
        }
    }

    /// <summary>
    /// Alarm bildirimini ilgili client'lara gönder
    /// </summary>
    public async Task AlarmBildirimiGonderAsync(AlarmBildirimi alarm)
    {
        try
        {
            // Tüm client'lara alarm gönder
            await _hubContext.Clients.Group("TumAraclar")
                .SendAsync("AlarmOlustu", alarm);

            // Bu aracı takip edenlere özel alarm
            var aracGrubu = $"Arac_{alarm.AracId}";
            await _hubContext.Clients.Group(aracGrubu)
                .SendAsync("AracAlarmOlustu", alarm);

            _logger.LogInformation("Alarm bildirimi gönderildi: {AlarmTipi} - Araç {AracId} ({Plaka})", 
                alarm.AlarmTipi, alarm.AracId, alarm.Plaka);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Alarm bildirimi gönderilemedi: Alarm {AlarmId}", alarm.AlarmId);
        }
    }

    /// <summary>
    /// Bölge giriş/çıkış olayını gönder
    /// </summary>
    public async Task BolgeOlayiGonderAsync(BolgeOlayi olay)
    {
        try
        {
            // Tüm client'lara bölge olayı gönder
            await _hubContext.Clients.Group("TumAraclar")
                .SendAsync("BolgeOlayiOlustu", olay);

            // Bu bölgeyi takip edenlere özel bildirim
            var bolgeGrubu = $"Bolge_{olay.BolgeId}";
            await _hubContext.Clients.Group(bolgeGrubu)
                .SendAsync("BolgeyeAracOlayi", olay);

            // Bu aracı takip edenlere
            var aracGrubu = $"Arac_{olay.AracId}";
            await _hubContext.Clients.Group(aracGrubu)
                .SendAsync("AracBolgeOlayi", olay);

            _logger.LogInformation("Bölge olayı gönderildi: {OlayTipi} - Araç {Plaka} -> Bölge {BolgeAdi}", 
                olay.OlayTipi, olay.Plaka, olay.BolgeAdi);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bölge olayı gönderilemedi");
        }
    }

    /// <summary>
    /// Belirli bir araca özel mesaj gönder
    /// </summary>
    public async Task AracaMesajGonderAsync(int aracId, string mesajTipi, object data)
    {
        try
        {
            var aracGrubu = $"Arac_{aracId}";
            await _hubContext.Clients.Group(aracGrubu)
                .SendAsync(mesajTipi, data);

            _logger.LogDebug("Araca mesaj gönderildi: Araç {AracId}, Tip {MesajTipi}", aracId, mesajTipi);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Araca mesaj gönderilemedi: Araç {AracId}", aracId);
        }
    }

    /// <summary>
    /// Tüm bağlı client'lara sistem mesajı gönder
    /// </summary>
    public async Task SistemMesajiGonderAsync(string mesaj, string tip = "info")
    {
        try
        {
            await _hubContext.Clients.All.SendAsync("SistemMesaji", new { mesaj, tip, zaman = DateTime.Now });
            _logger.LogInformation("Sistem mesajı gönderildi: {Mesaj}", mesaj);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sistem mesajı gönderilemedi");
        }
    }
}


