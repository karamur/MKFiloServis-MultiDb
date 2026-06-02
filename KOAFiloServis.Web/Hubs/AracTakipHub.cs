using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using KOAFiloServis.Shared.Entities;

namespace KOAFiloServis.Web.Hubs;

/// <summary>
/// Araç takip için SignalR Hub'ı - Gerçek zamanlı konum güncellemeleri
/// </summary>
[Authorize]
public class AracTakipHub : Hub
{
    private readonly ILogger<AracTakipHub> _logger;

    public AracTakipHub(ILogger<AracTakipHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Client bağlandığında
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client bağlandı: {ConnectionId}", Context.ConnectionId);
        
        // Varsayılan olarak "TumAraclar" grubuna ekle
        await Groups.AddToGroupAsync(Context.ConnectionId, "TumAraclar");
        
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Client ayrıldığında
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client ayrıldı: {ConnectionId}, Hata: {Error}", 
            Context.ConnectionId, exception?.Message ?? "Yok");
        
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Belirli bir aracı takip etmeye başla
    /// </summary>
    public async Task TakipBaslat(int aracId)
    {
        var grupAdi = $"Arac_{aracId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, grupAdi);
        _logger.LogInformation("Client {ConnectionId} araç {AracId} takibine başladı", Context.ConnectionId, aracId);
    }

    /// <summary>
    /// Belirli bir aracın takibini bırak
    /// </summary>
    public async Task TakipDurdur(int aracId)
    {
        var grupAdi = $"Arac_{aracId}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, grupAdi);
        _logger.LogInformation("Client {ConnectionId} araç {AracId} takibini bıraktı", Context.ConnectionId, aracId);
    }

    /// <summary>
    /// Birden fazla aracı takip et
    /// </summary>
    public async Task CokluTakipBaslat(int[] aracIdler)
    {
        foreach (var aracId in aracIdler)
        {
            var grupAdi = $"Arac_{aracId}";
            await Groups.AddToGroupAsync(Context.ConnectionId, grupAdi);
        }
        _logger.LogInformation("Client {ConnectionId} {Count} araç takibine başladı", Context.ConnectionId, aracIdler.Length);
    }

    /// <summary>
    /// Tüm araç takiplerini bırak
    /// </summary>
    public async Task TumTakipleriDurdur()
    {
        // Sadece TumAraclar grubunda kal
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "TumAraclar");
        await Groups.AddToGroupAsync(Context.ConnectionId, "TumAraclar");
        _logger.LogInformation("Client {ConnectionId} tüm takipleri bıraktı", Context.ConnectionId);
    }

    /// <summary>
    /// Belirli bir bölgeyi takip et (Geofence)
    /// </summary>
    public async Task BolgeTakipBaslat(int bolgeId)
    {
        var grupAdi = $"Bolge_{bolgeId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, grupAdi);
        _logger.LogInformation("Client {ConnectionId} bölge {BolgeId} takibine başladı", Context.ConnectionId, bolgeId);
    }

    /// <summary>
    /// Bölge takibini bırak
    /// </summary>
    public async Task BolgeTakipDurdur(int bolgeId)
    {
        var grupAdi = $"Bolge_{bolgeId}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, grupAdi);
    }
}

/// <summary>
/// Araç konum güncelleme DTO'su (SignalR üzerinden gönderilecek)
/// </summary>
public class AracKonumGuncelleme
{
    public int AracId { get; set; }
    public string Plaka { get; set; } = string.Empty;
    public double Enlem { get; set; }
    public double Boylam { get; set; }
    public double? Hiz { get; set; }
    public double? Yon { get; set; }
    public bool KontakDurumu { get; set; }
    public bool MotorDurumu { get; set; }
    public int? YakitSeviyesi { get; set; }
    public DateTime ZamanDamgasi { get; set; }
    public string? Adres { get; set; }
    public string Durum { get; set; } = "Bilinmiyor"; // Hareket, Bekliyor, Park, Çevrimdışı
}

/// <summary>
/// Alarm bildirimi DTO'su
/// </summary>
public class AlarmBildirimi
{
    public int AlarmId { get; set; }
    public int AracId { get; set; }
    public string Plaka { get; set; } = string.Empty;
    public string AlarmTipi { get; set; } = string.Empty;
    public string Mesaj { get; set; } = string.Empty;
    public double? Enlem { get; set; }
    public double? Boylam { get; set; }
    public DateTime OlusturmaTarihi { get; set; }
    public string Oncelik { get; set; } = "Normal"; // Dusuk, Normal, Yuksek, Kritik
}

/// <summary>
/// Bölge olayı DTO'su (giriş/çıkış)
/// </summary>
public class BolgeOlayi
{
    public int AracId { get; set; }
    public string Plaka { get; set; } = string.Empty;
    public int BolgeId { get; set; }
    public string BolgeAdi { get; set; } = string.Empty;
    public string OlayTipi { get; set; } = string.Empty; // Giris, Cikis
    public DateTime OlayZamani { get; set; }
}
