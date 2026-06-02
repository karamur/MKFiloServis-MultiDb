using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KOAFiloServis.Web.Services.Interfaces;
using KOAFiloServis.Web.Hubs;
using KOAFiloServis.Shared.Entities;

namespace KOAFiloServis.Web.Controllers;

/// <summary>
/// Araç GPS Takip API endpoint'leri
/// GPS cihazlarından konum verisi almak ve gerçek zamanlı takip için kullanılır
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AracTakipController : ControllerBase
{
    private readonly IAracTakipService _aracTakipService;
    private readonly IAracTakipBildirimService _bildirimService;
    private readonly ILogger<AracTakipController> _logger;
    private readonly string? _gpsApiKey;

    public AracTakipController(
        IAracTakipService aracTakipService,
        IAracTakipBildirimService bildirimService,
        ILogger<AracTakipController> logger,
        IConfiguration configuration)
    {
        _aracTakipService = aracTakipService;
        _bildirimService = bildirimService;
        _logger = logger;
        _gpsApiKey = configuration["GpsApi:ApiKey"];
    }

    #region Konum Endpoint'leri (GPS Cihazları İçin)

    /// <summary>
    /// GPS cihazından konum verisi alır ve kaydeder
    /// Bu endpoint kimlik doğrulama gerektirmez (cihazlar için)
    /// Güvenlik: API Key veya Cihaz ID doğrulaması yapılır
    /// </summary>
    [HttpPost("konum")]
    [AllowAnonymous]
    public async Task<IActionResult> KonumGonder([FromBody] KonumGonderRequest request, [FromHeader(Name = "X-API-Key")] string? apiKey = null)
    {
        try
        {
            // API Key doğrulaması
            if (!string.IsNullOrEmpty(_gpsApiKey) && !string.Equals(apiKey, _gpsApiKey, StringComparison.Ordinal))
            {
                _logger.LogWarning("Geçersiz API Key ile konum gönderme denemesi");
                return Unauthorized(new { Error = "Geçersiz API Key" });
            }

            // CihazId doğrulaması
            if (string.IsNullOrEmpty(request.CihazId))
            {
                return BadRequest(new { Error = "CihazId zorunludur" });
            }

            // Cihazı bul
            var cihaz = await _aracTakipService.GetCihazByCihazIdAsync(request.CihazId);
            if (cihaz == null)
            {
                _logger.LogWarning("Bilinmeyen cihaz ID: {CihazId}", request.CihazId);
                return NotFound(new { Error = "Cihaz bulunamadı" });
            }

            if (!cihaz.Aktif)
            {
                return BadRequest(new { Error = "Cihaz aktif değil" });
            }

            // Konum kaydı oluştur
            var konum = new AracKonum
            {
                AracTakipCihazId = cihaz.Id,
                Latitude = request.Enlem,
                Longitude = request.Boylam,
                Hiz = request.Hiz,
                Yon = request.Yon,
                Rakım = request.Irtifa,
                KontakDurumu = request.KontakDurumu,
                MotorDurumu = request.MotorDurumu,
                YakitSeviyesi = request.YakitSeviyesi,
                Kilometre = request.KmSayaci,
                Hassasiyet = request.HDOP,
                OlayTipi = request.OlayTipi ?? KonumOlayTipi.Normal,
                KayitZamani = request.ZamanDamgasi ?? DateTime.Now,
                CreatedAt = DateTime.Now
            };

            // Kaydet
            await _aracTakipService.KaydetKonumAsync(konum);

            // SignalR ile gerçek zamanlı bildirim gönder
            var guncelleme = new AracKonumGuncelleme
            {
                AracId = cihaz.AracId,
                Plaka = cihaz.Arac?.AktifPlaka ?? "",
                Enlem = request.Enlem,
                Boylam = request.Boylam,
                Hiz = request.Hiz,
                Yon = request.Yon,
                KontakDurumu = request.KontakDurumu ?? false,
                MotorDurumu = request.MotorDurumu ?? false,
                YakitSeviyesi = request.YakitSeviyesi,
                ZamanDamgasi = konum.KayitZamani,
                Durum = BelirleAracDurumu(request)
            };

            await _bildirimService.KonumGuncellemesiGonderAsync(guncelleme);

            _logger.LogDebug("Konum kaydedildi: Cihaz {CihazId}, Araç {AracId}", request.CihazId, cihaz.AracId);

            return Ok(new { Success = true, Message = "Konum kaydedildi" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Konum kaydetme hatası: {CihazId}", request.CihazId);
            return StatusCode(500, new { Error = "Konum kaydedilemedi" });
        }
    }

    /// <summary>
    /// GPS cihazından toplu konum verisi alır (batch)
    /// Ağ kesintisi sonrası birikmiş verileri göndermek için kullanılır
    /// </summary>
    [HttpPost("konum/toplu")]
    [AllowAnonymous]
    public async Task<IActionResult> TopluKonumGonder([FromBody] TopluKonumGonderRequest request, [FromHeader(Name = "X-API-Key")] string? apiKey = null)
    {
        try
        {
            // API Key doğrulaması
            if (!string.IsNullOrEmpty(_gpsApiKey) && !string.Equals(apiKey, _gpsApiKey, StringComparison.Ordinal))
            {
                _logger.LogWarning("Geçersiz API Key ile toplu konum gönderme denemesi");
                return Unauthorized(new { Error = "Geçersiz API Key" });
            }

            if (string.IsNullOrEmpty(request.CihazId))
            {
                return BadRequest(new { Error = "CihazId zorunludur" });
            }

            if (request.Konumlar == null || !request.Konumlar.Any())
            {
                return BadRequest(new { Error = "En az bir konum verisi gerekli" });
            }

            // Cihazı bul
            var cihaz = await _aracTakipService.GetCihazByCihazIdAsync(request.CihazId);
            if (cihaz == null)
            {
                return NotFound(new { Error = "Cihaz bulunamadı" });
            }

            if (!cihaz.Aktif)
            {
                return BadRequest(new { Error = "Cihaz aktif değil" });
            }

            // Konumları dönüştür
            var konumlar = request.Konumlar.Select(k => new AracKonum
            {
                AracTakipCihazId = cihaz.Id,
                Latitude = k.Enlem,
                Longitude = k.Boylam,
                Hiz = k.Hiz,
                Yon = k.Yon,
                Rakım = k.Irtifa,
                KontakDurumu = k.KontakDurumu,
                MotorDurumu = k.MotorDurumu,
                YakitSeviyesi = k.YakitSeviyesi,
                Kilometre = k.KmSayaci,
                Hassasiyet = k.HDOP,
                OlayTipi = k.OlayTipi ?? KonumOlayTipi.Normal,
                KayitZamani = k.ZamanDamgasi ?? DateTime.Now,
                CreatedAt = DateTime.Now
            }).ToList();

            // Toplu kaydet
            var kaydedilen = await _aracTakipService.KaydetKonumlarAsync(konumlar);

            // Son konumu SignalR ile bildir
            var sonKonum = request.Konumlar.OrderByDescending(k => k.ZamanDamgasi).First();
            var guncelleme = new AracKonumGuncelleme
            {
                AracId = cihaz.AracId,
                Plaka = cihaz.Arac?.AktifPlaka ?? "",
                Enlem = sonKonum.Enlem,
                Boylam = sonKonum.Boylam,
                Hiz = sonKonum.Hiz,
                Yon = sonKonum.Yon,
                KontakDurumu = sonKonum.KontakDurumu ?? false,
                MotorDurumu = sonKonum.MotorDurumu ?? false,
                YakitSeviyesi = sonKonum.YakitSeviyesi,
                ZamanDamgasi = sonKonum.ZamanDamgasi ?? DateTime.Now,
                Durum = BelirleAracDurumu(sonKonum)
            };

            await _bildirimService.KonumGuncellemesiGonderAsync(guncelleme);

            _logger.LogInformation("Toplu konum kaydedildi: Cihaz {CihazId}, {Count} kayıt", request.CihazId, kaydedilen);

            return Ok(new { Success = true, KaydedilenSayisi = kaydedilen });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Toplu konum kaydetme hatası: {CihazId}", request.CihazId);
            return StatusCode(500, new { Error = "Konumlar kaydedilemedi" });
        }
    }

    #endregion

    #region Kimlik Doğrulamalı API Endpoint'leri

    /// <summary>
    /// Tüm araçların son konumlarını getirir
    /// </summary>
    [HttpGet("konumlar")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public async Task<IActionResult> GetTumKonumlar()
    {
        var konumlar = await _aracTakipService.GetTumAraclarinSonKonumlariAsync();
        return Ok(konumlar);
    }

    /// <summary>
    /// Belirli bir aracın son konumunu getirir
    /// </summary>
    [HttpGet("konum/{aracId}")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public async Task<IActionResult> GetAracKonum(int aracId)
    {
        var konum = await _aracTakipService.GetSonKonumByAracIdAsync(aracId);
        if (konum == null)
        {
            return NotFound(new { Error = "Araç konumu bulunamadı" });
        }

        return Ok(konum);
    }

    /// <summary>
    /// Aracın konum geçmişini getirir
    /// </summary>
    [HttpGet("gecmis/{aracId}")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public async Task<IActionResult> GetKonumGecmisi(int aracId, [FromQuery] DateTime baslangic, [FromQuery] DateTime bitis)
    {
        if (bitis < baslangic)
        {
            return BadRequest(new { Error = "Bitiş tarihi başlangıç tarihinden önce olamaz" });
        }

        if ((bitis - baslangic).TotalDays > 31)
        {
            return BadRequest(new { Error = "En fazla 31 günlük geçmiş sorgulanabilir" });
        }

        var konumlar = await _aracTakipService.GetKonumGecmisiAsync(aracId, baslangic, bitis);
        return Ok(konumlar.Select(k => new
        {
            k.Latitude,
            k.Longitude,
            k.Hiz,
            k.Yon,
            k.KayitZamani,
            k.KontakDurumu,
            k.MotorDurumu,
            OlayTipi = k.OlayTipi.ToString()
        }));
    }

    /// <summary>
    /// Aracın belirli dönem istatistiklerini getirir
    /// </summary>
    [HttpGet("istatistik/{aracId}")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public async Task<IActionResult> GetIstatistik(int aracId, [FromQuery] DateTime baslangic, [FromQuery] DateTime bitis)
    {
        var istatistik = await _aracTakipService.GetIstatistikAsync(aracId, baslangic, bitis);
        return Ok(istatistik);
    }

    /// <summary>
    /// Aracın durak noktalarını getirir
    /// </summary>
    [HttpGet("duraklar/{aracId}")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public async Task<IActionResult> GetDurakNoktaları(int aracId, [FromQuery] DateTime baslangic, [FromQuery] DateTime bitis, [FromQuery] int minDakika = 5)
    {
        var duraklar = await _aracTakipService.GetDurakNoktalariAsync(aracId, baslangic, bitis, minDakika);
        return Ok(duraklar);
    }

    #endregion

    #region Cihaz Yönetimi

    /// <summary>
    /// Tüm GPS cihazlarını listeler
    /// </summary>
    [HttpGet("cihazlar")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public async Task<IActionResult> GetCihazlar([FromQuery] bool? aktif = null)
    {
        var cihazlar = aktif.HasValue && aktif.Value
            ? await _aracTakipService.GetAktifCihazlarAsync()
            : await _aracTakipService.GetAllCihazlarAsync();

        return Ok(cihazlar.Select(c => new
        {
            c.Id,
            c.CihazId,
            c.AracId,
            Plaka = c.Arac?.AktifPlaka,
            c.CihazMarka,
            c.CihazModel,
            c.SimKartNo,
            c.Aktif,
            c.SonIletisimZamani,
            c.BataryaSeviyesi,
            c.SinyalGucu
        }));
    }

    /// <summary>
    /// Yeni GPS cihazı ekler
    /// </summary>
    [HttpPost("cihazlar")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public async Task<IActionResult> CreateCihaz([FromBody] CihazEkleRequest request)
    {
        try
        {
            // Cihaz ID kontrolü
            var mevcutCihaz = await _aracTakipService.GetCihazByCihazIdAsync(request.CihazId);
            if (mevcutCihaz != null)
            {
                return BadRequest(new { Error = "Bu Cihaz ID zaten kullanımda" });
            }

            var cihaz = new AracTakipCihaz
            {
                CihazId = request.CihazId,
                AracId = request.AracId,
                CihazMarka = request.Marka,
                CihazModel = request.Model,
                SimKartNo = request.SIMKartNo,
                Notlar = request.Notlar,
                Aktif = true,
                CreatedAt = DateTime.Now
            };

            var sonuc = await _aracTakipService.CreateCihazAsync(cihaz);
            return CreatedAtAction(nameof(GetCihazlar), new { id = sonuc.Id }, new { Success = true, Id = sonuc.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cihaz ekleme hatası");
            return StatusCode(500, new { Error = "Cihaz eklenemedi" });
        }
    }

    /// <summary>
    /// GPS cihazını günceller
    /// </summary>
    [HttpPut("cihazlar/{id}")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public async Task<IActionResult> UpdateCihaz(int id, [FromBody] CihazGuncelleRequest request)
    {
        try
        {
            var cihaz = await _aracTakipService.GetCihazByIdAsync(id);
            if (cihaz == null)
            {
                return NotFound(new { Error = "Cihaz bulunamadı" });
            }

            cihaz.AracId = request.AracId ?? cihaz.AracId;
            cihaz.CihazMarka = request.Marka ?? cihaz.CihazMarka;
            cihaz.CihazModel = request.Model ?? cihaz.CihazModel;
            cihaz.SimKartNo = request.SIMKartNo ?? cihaz.SimKartNo;
            cihaz.Notlar = request.Notlar ?? cihaz.Notlar;
            cihaz.Aktif = request.Aktif ?? cihaz.Aktif;
            cihaz.UpdatedAt = DateTime.Now;

            await _aracTakipService.UpdateCihazAsync(cihaz);
            return Ok(new { Success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cihaz güncelleme hatası: {Id}", id);
            return StatusCode(500, new { Error = "Cihaz güncellenemedi" });
        }
    }

    /// <summary>
    /// GPS cihazını siler
    /// </summary>
    [HttpDelete("cihazlar/{id}")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public async Task<IActionResult> DeleteCihaz(int id)
    {
        try
        {
            await _aracTakipService.DeleteCihazAsync(id);
            return Ok(new { Success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cihaz silme hatası: {Id}", id);
            return StatusCode(500, new { Error = "Cihaz silinemedi" });
        }
    }

    #endregion

    #region Alarm Endpoint'leri

    /// <summary>
    /// Aktif alarmları listeler
    /// </summary>
    [HttpGet("alarmlar")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public async Task<IActionResult> GetAlarmlar([FromQuery] int? aracId = null, [FromQuery] DateTime? baslangic = null, [FromQuery] DateTime? bitis = null)
    {
        List<AracTakipAlarm> alarmlar;

        if (aracId.HasValue)
        {
            alarmlar = await _aracTakipService.GetAlarmlarByAracIdAsync(aracId.Value, baslangic, bitis);
        }
        else
        {
            alarmlar = await _aracTakipService.GetAktifAlarmlarAsync();
        }

        return Ok(alarmlar.Select(a => new
        {
            a.Id,
            a.AracTakipCihazId,
            Plaka = a.AracTakipCihaz?.Arac?.AktifPlaka,
            AlarmTipi = a.AlarmTipi.ToString(),
            a.Mesaj,
            a.Latitude,
            a.Longitude,
            a.Okundu,
            a.Islendi,
            a.CreatedAt
        }));
    }

    /// <summary>
    /// Alarmı okundu olarak işaretler
    /// </summary>
    [HttpPut("alarmlar/{id}/okundu")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public async Task<IActionResult> AlarmOkundu(int id)
    {
        try
        {
            await _aracTakipService.OkunduIsaretle(id);
            return Ok(new { Success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Alarm okundu işaretleme hatası: {Id}", id);
            return StatusCode(500, new { Error = "İşlem başarısız" });
        }
    }

    /// <summary>
    /// Alarmı işlendi olarak işaretler
    /// </summary>
    [HttpPut("alarmlar/{id}/islendi")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public async Task<IActionResult> AlarmIslendi(int id, [FromBody] AlarmIslendiRequest? request = null)
    {
        try
        {
            await _aracTakipService.IslendiIsaretle(id, request?.Notlar);
            return Ok(new { Success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Alarm işlendi işaretleme hatası: {Id}", id);
            return StatusCode(500, new { Error = "İşlem başarısız" });
        }
    }

    #endregion

    #region Helper Methods

    private string BelirleAracDurumu(KonumVerisi konum)
    {
        if (konum.Hiz.HasValue && konum.Hiz.Value > 5)
            return "Hareket";
        if (konum.KontakDurumu == true)
            return "Bekliyor";
        return "Park";
    }

    #endregion
}

#region Request/Response DTOs

/// <summary>
/// Tek konum gönderme isteği (GPS cihazından)
/// </summary>
public class KonumGonderRequest : KonumVerisi
{
    /// <summary>GPS Cihaz ID (zorunlu)</summary>
    public string CihazId { get; set; } = string.Empty;
}

/// <summary>
/// Toplu konum gönderme isteği
/// </summary>
public class TopluKonumGonderRequest
{
    /// <summary>GPS Cihaz ID (zorunlu)</summary>
    public string CihazId { get; set; } = string.Empty;
    
    /// <summary>Konum verileri listesi</summary>
    public List<KonumVerisi> Konumlar { get; set; } = new();
}

/// <summary>
/// Konum verisi
/// </summary>
public class KonumVerisi
{
    /// <summary>Enlem (latitude)</summary>
    public double Enlem { get; set; }
    
    /// <summary>Boylam (longitude)</summary>
    public double Boylam { get; set; }
    
    /// <summary>Hız (km/saat)</summary>
    public double? Hiz { get; set; }
    
    /// <summary>Yön (derece, 0-360)</summary>
    public double? Yon { get; set; }
    
    /// <summary>İrtifa (metre)</summary>
    public double? Irtifa { get; set; }
    
    /// <summary>Kontak durumu</summary>
    public bool? KontakDurumu { get; set; }
    
    /// <summary>Motor durumu</summary>
    public bool? MotorDurumu { get; set; }
    
    /// <summary>Yakıt seviyesi (%)</summary>
    public int? YakitSeviyesi { get; set; }
    
    /// <summary>Kilometre sayacı</summary>
    public int? KmSayaci { get; set; }
    
    /// <summary>GSM sinyal gücü (0-100)</summary>
    public int? SinyalGucu { get; set; }
    
    /// <summary>Görülen uydu sayısı</summary>
    public int? UyduSayisi { get; set; }
    
    /// <summary>GPS hassasiyet değeri</summary>
    public double? HDOP { get; set; }
    
    /// <summary>Olay tipi</summary>
    public KonumOlayTipi? OlayTipi { get; set; }
    
    /// <summary>Zaman damgası (cihaz zamanı)</summary>
    public DateTime? ZamanDamgasi { get; set; }
}

/// <summary>
/// Cihaz ekleme isteği
/// </summary>
public class CihazEkleRequest
{
    public string CihazId { get; set; } = string.Empty;
    public int AracId { get; set; }
    public string? Marka { get; set; }
    public string? Model { get; set; }
    public string? SIMKartNo { get; set; }
    public string? Notlar { get; set; }
}

/// <summary>
/// Cihaz güncelleme isteği
/// </summary>
public class CihazGuncelleRequest
{
    public int? AracId { get; set; }
    public string? Marka { get; set; }
    public string? Model { get; set; }
    public string? SIMKartNo { get; set; }
    public string? Notlar { get; set; }
    public bool? Aktif { get; set; }
}

/// <summary>
/// Alarm işlendi isteği
/// </summary>
public class AlarmIslendiRequest
{
    public string? Notlar { get; set; }
}

#endregion
