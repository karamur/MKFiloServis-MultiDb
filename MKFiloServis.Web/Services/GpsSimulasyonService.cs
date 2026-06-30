using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using MKFiloServis.Web.Hubs;
using MKFiloServis.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace MKFiloServis.Web.Services;

/// <summary>
/// GPS Cihaz Simülasyon Servisi
/// Test ve demo amaçlı gerçekçi GPS verileri üretir
/// </summary>
public class GpsSimulasyonService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<GpsSimulasyonService> _logger;
    
    // Simülasyon durumu
    private bool _aktif = false;
    private int _guncellemeAraligiSaniye = 10;
    private readonly Random _random = new();
    private bool _eksikTabloUyarisiLoglandi;
    
    // Simüle edilen araçların durumları
    private readonly Dictionary<int, SimulasyonAracDurumu> _aracDurumlari = new();

    public GpsSimulasyonService(
        IServiceScopeFactory scopeFactory,
        ILogger<GpsSimulasyonService> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        
        // Yapılandırmadan oku
        _aktif = configuration.GetValue<bool>("GpsSimulasyon:Aktif", false);
        _guncellemeAraligiSaniye = configuration.GetValue<int>("GpsSimulasyon:GuncellemeAraligiSaniye", 10);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("GPS Simülasyon Servisi başlatıldı. Aktif: {Aktif}", _aktif);
        
        while (!stoppingToken.IsCancellationRequested)
        {
            if (_aktif)
            {
                try
                {
                    await SimulasyonDongusuAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "GPS simülasyon hatası");
                }
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(_guncellemeAraligiSaniye), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task SimulasyonDongusuAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var bildirimService = scope.ServiceProvider.GetRequiredService<IAracTakipBildirimService>();

        if (!await GerekliTablolarHazirMiAsync(context, stoppingToken))
        {
            if (!_eksikTabloUyarisiLoglandi)
            {
                _logger.LogWarning("GPS simülasyonu atlandı: araç takip tabloları henüz hazır değil.");
                _eksikTabloUyarisiLoglandi = true;
            }

            return;
        }

        _eksikTabloUyarisiLoglandi = false;
        
        // Aktif cihazları al
        var cihazlar = await context.AracTakipCihazlar
            .AsNoTracking()
            .Include(c => c.Arac)
            .Where(c => !c.IsDeleted && c.Aktif)
            .ToListAsync(stoppingToken);

        if (!cihazlar.Any())
        {
            _logger.LogDebug("Simüle edilecek aktif cihaz bulunamadı");
            return;
        }

        var cihazIdleri = cihazlar.Select(c => c.Id).ToList();
        var sonKonumListesi = await context.AracKonumlar
            .AsNoTracking()
            .Where(k => cihazIdleri.Contains(k.AracTakipCihazId))
            .OrderByDescending(k => k.KayitZamani)
            .ToListAsync(stoppingToken);

        var sonKonumlar = sonKonumListesi
            .GroupBy(k => k.AracTakipCihazId)
            .ToDictionary(g => g.Key, g => g.First());

        var cihazEntityMap = await context.AracTakipCihazlar
            .Where(c => cihazIdleri.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, stoppingToken);

        var guncellemeler = new List<AracKonumGuncelleme>();

        foreach (var cihaz in cihazlar)
        {
            // Araç için simülasyon durumu al veya oluştur
            if (!_aracDurumlari.TryGetValue(cihaz.AracId, out var durum))
            {
                sonKonumlar.TryGetValue(cihaz.Id, out var sonKonum);
                durum = YeniSimulasyonDurumuOlustur(cihaz, sonKonum);
                _aracDurumlari[cihaz.AracId] = durum;
            }

            // Durumu güncelle
            GuncelleDurum(durum);

            // Konum kaydı oluştur
            var konum = new AracKonum
            {
                AracTakipCihazId = cihaz.Id,
                Latitude = durum.Enlem,
                Longitude = durum.Boylam,
                Hiz = durum.Hiz,
                Yon = durum.Yon,
                KontakDurumu = durum.KontakAcik,
                MotorDurumu = durum.MotorCalisiyor,
                YakitSeviyesi = durum.YakitSeviyesi,
                OlayTipi = KonumOlayTipi.Normal,
                KayitZamani = DateTime.Now,
                CreatedAt = DateTime.Now
            };

            // Veritabanına kaydet
            context.AracKonumlar.Add(konum);

            // Cihazın son iletişim zamanını güncelle
            if (cihazEntityMap.TryGetValue(cihaz.Id, out var cihazEntity))
            {
                cihazEntity.SonIletisimZamani = DateTime.Now;
                cihazEntity.BataryaSeviyesi = durum.BataryaSeviyesi;
                cihazEntity.SinyalGucu = durum.SinyalGucu;
            }

            // SignalR güncellemesi hazırla
            guncellemeler.Add(new AracKonumGuncelleme
            {
                AracId = cihaz.AracId,
                Plaka = cihaz.Arac?.AktifPlaka ?? "",
                Enlem = durum.Enlem,
                Boylam = durum.Boylam,
                Hiz = durum.Hiz,
                Yon = durum.Yon,
                KontakDurumu = durum.KontakAcik,
                MotorDurumu = durum.MotorCalisiyor,
                YakitSeviyesi = durum.YakitSeviyesi,
                ZamanDamgasi = DateTime.Now,
                Durum = BelirleAracDurumu(durum)
            });
        }

        await context.SaveChangesAsync(stoppingToken);

        // SignalR ile toplu güncelleme gönder
        if (guncellemeler.Any())
        {
            await bildirimService.TopluKonumGuncellemesiGonderAsync(guncellemeler);
            _logger.LogDebug("GPS simülasyonu: {Count} araç güncellendi", guncellemeler.Count);
        }
    }

    private SimulasyonAracDurumu YeniSimulasyonDurumuOlustur(AracTakipCihaz cihaz, AracKonum? sonKonum)
    {
        // Türkiye sınırları içinde rastgele başlangıç noktası
        // veya son bilinen konumdan devam
        double baslangicEnlem, baslangicBoylam;
        
        if (sonKonum != null)
        {
            baslangicEnlem = sonKonum.Latitude;
            baslangicBoylam = sonKonum.Longitude;
        }
        else
        {
            // İstanbul merkez civarı rastgele nokta
            baslangicEnlem = 40.9 + (_random.NextDouble() * 0.3);
            baslangicBoylam = 28.8 + (_random.NextDouble() * 0.5);
        }

        // Rastgele hareket modu seç
        var modlar = new[] { SimulasyonModu.Hareket, SimulasyonModu.Bekliyor, SimulasyonModu.Park };
        var mod = modlar[_random.Next(modlar.Length)];

        return new SimulasyonAracDurumu
        {
            AracId = cihaz.AracId,
            Enlem = baslangicEnlem,
            Boylam = baslangicBoylam,
            Hiz = mod == SimulasyonModu.Hareket ? 30 + _random.Next(70) : 0,
            Yon = _random.Next(360),
            KontakAcik = mod != SimulasyonModu.Park,
            MotorCalisiyor = mod == SimulasyonModu.Hareket,
            YakitSeviyesi = 30 + _random.Next(70),
            BataryaSeviyesi = 70 + _random.Next(30),
            SinyalGucu = 3 + _random.Next(3),
            Mod = mod,
            SonDurumDegisikligi = DateTime.Now,
            HedefEnlem = baslangicEnlem + ((_random.NextDouble() - 0.5) * 0.1),
            HedefBoylam = baslangicBoylam + ((_random.NextDouble() - 0.5) * 0.1)
        };
    }

    private void GuncelleDurum(SimulasyonAracDurumu durum)
    {
        // Belirli aralıklarla mod değiştir
        var modSuresi = (DateTime.Now - durum.SonDurumDegisikligi).TotalMinutes;
        if (modSuresi > 5 + _random.Next(10))
        {
            // Mod değiştir
            var modlar = new[] { SimulasyonModu.Hareket, SimulasyonModu.Bekliyor, SimulasyonModu.Park };
            durum.Mod = modlar[_random.Next(modlar.Length)];
            durum.SonDurumDegisikligi = DateTime.Now;
            
            // Yeni hedef belirle
            if (durum.Mod == SimulasyonModu.Hareket)
            {
                durum.HedefEnlem = durum.Enlem + ((_random.NextDouble() - 0.5) * 0.1);
                durum.HedefBoylam = durum.Boylam + ((_random.NextDouble() - 0.5) * 0.1);
                durum.Yon = HesaplaYon(durum.Enlem, durum.Boylam, durum.HedefEnlem, durum.HedefBoylam);
            }
        }

        // Moda göre güncelle
        switch (durum.Mod)
        {
            case SimulasyonModu.Hareket:
                GuncelleHareketModu(durum);
                break;
            case SimulasyonModu.Bekliyor:
                GuncelleBekliyorModu(durum);
                break;
            case SimulasyonModu.Park:
                GuncelleParkModu(durum);
                break;
        }

        // Yakıt tüketimi (hareket halinde)
        if (durum.Mod == SimulasyonModu.Hareket && durum.YakitSeviyesi > 5)
        {
            if (_random.Next(10) == 0) // %10 ihtimalle
            {
                durum.YakitSeviyesi--;
            }
        }

        // Batarya (şarj oluyor mu?)
        if (durum.MotorCalisiyor && durum.BataryaSeviyesi < 100)
        {
            if (_random.Next(5) == 0) // %20 ihtimalle
            {
                durum.BataryaSeviyesi++;
            }
        }
    }

    private void GuncelleHareketModu(SimulasyonAracDurumu durum)
    {
        durum.KontakAcik = true;
        durum.MotorCalisiyor = true;

        // Hız değişimi (30-120 km/s arası)
        var hizDegisimi = (_random.NextDouble() - 0.5) * 10;
        durum.Hiz = Math.Clamp(durum.Hiz + hizDegisimi, 20, 120);

        // Hedefe doğru hareket et
        var mesafe = HesaplaMesafe(durum.Enlem, durum.Boylam, durum.HedefEnlem, durum.HedefBoylam);
        
        if (mesafe < 0.5) // 500 metre yaklaştıysa yeni hedef
        {
            durum.HedefEnlem = durum.Enlem + ((_random.NextDouble() - 0.5) * 0.1);
            durum.HedefBoylam = durum.Boylam + ((_random.NextDouble() - 0.5) * 0.1);
            durum.Yon = HesaplaYon(durum.Enlem, durum.Boylam, durum.HedefEnlem, durum.HedefBoylam);
        }

        // Konumu güncelle (hıza göre)
        var hareketMiktari = (durum.Hiz / 3600) * _guncellemeAraligiSaniye / 111; // derece cinsinden
        var yonRadyan = durum.Yon * Math.PI / 180;
        
        durum.Enlem += hareketMiktari * Math.Cos(yonRadyan);
        durum.Boylam += hareketMiktari * Math.Sin(yonRadyan);

        // Yön sapması ekle (gerçekçilik için)
        durum.Yon += (_random.NextDouble() - 0.5) * 10;
        durum.Yon = ((durum.Yon % 360) + 360) % 360;
    }

    private void GuncelleBekliyorModu(SimulasyonAracDurumu durum)
    {
        durum.KontakAcik = true;
        durum.MotorCalisiyor = false;
        durum.Hiz = 0;
        
        // Küçük GPS sapması ekle (gerçekçilik için)
        durum.Enlem += (_random.NextDouble() - 0.5) * 0.00001;
        durum.Boylam += (_random.NextDouble() - 0.5) * 0.00001;
    }

    private void GuncelleParkModu(SimulasyonAracDurumu durum)
    {
        durum.KontakAcik = false;
        durum.MotorCalisiyor = false;
        durum.Hiz = 0;
        
        // Çok küçük GPS sapması (sabit pozisyon)
        durum.Enlem += (_random.NextDouble() - 0.5) * 0.000001;
        durum.Boylam += (_random.NextDouble() - 0.5) * 0.000001;
    }

    private double HesaplaMesafe(double lat1, double lon1, double lat2, double lon2)
    {
        // Haversine formülü ile km cinsinden mesafe
        var R = 6371; // Dünya yarıçapı km
        var dLat = (lat2 - lat1) * Math.PI / 180;
        var dLon = (lon2 - lon1) * Math.PI / 180;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private double HesaplaYon(double lat1, double lon1, double lat2, double lon2)
    {
        var dLon = (lon2 - lon1) * Math.PI / 180;
        var y = Math.Sin(dLon) * Math.Cos(lat2 * Math.PI / 180);
        var x = Math.Cos(lat1 * Math.PI / 180) * Math.Sin(lat2 * Math.PI / 180) -
                Math.Sin(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) * Math.Cos(dLon);
        var yon = Math.Atan2(y, x) * 180 / Math.PI;
        return ((yon + 360) % 360);
    }

    private string BelirleAracDurumu(SimulasyonAracDurumu durum)
    {
        if (durum.Hiz > 5)
            return "Hareket";
        if (durum.KontakAcik)
            return "Bekliyor";
        return "Park";
    }

    #region Public API

    /// <summary>
    /// Simülasyonu başlat
    /// </summary>
    public void Baslat()
    {
        _aktif = true;
        _logger.LogInformation("GPS simülasyonu başlatıldı");
    }

    /// <summary>
    /// Simülasyonu durdur
    /// </summary>
    public void Durdur()
    {
        _aktif = false;
        _logger.LogInformation("GPS simülasyonu durduruldu");
    }

    /// <summary>
    /// Simülasyon durumunu al
    /// </summary>
    public bool AktifMi => _aktif;

    /// <summary>
    /// Güncelleme aralığını ayarla
    /// </summary>
    public void GuncellemeAraligiAyarla(int saniye)
    {
        _guncellemeAraligiSaniye = Math.Max(1, Math.Min(60, saniye));
        _logger.LogInformation("GPS simülasyon güncelleme aralığı: {Saniye} saniye", _guncellemeAraligiSaniye);
    }

    /// <summary>
    /// Tüm araç durumlarını sıfırla
    /// </summary>
    public void Sifirla()
    {
        _aracDurumlari.Clear();
        _logger.LogInformation("GPS simülasyon araç durumları sıfırlandı");
    }

    private async Task<bool> GerekliTablolarHazirMiAsync(ApplicationDbContext context, CancellationToken cancellationToken)
    {
        return await TabloVarMiAsync(context, "AracTakipCihazlar", cancellationToken)
            && await TabloVarMiAsync(context, "AracKonumlar", cancellationToken);
    }

    private static async Task<bool> TabloVarMiAsync(ApplicationDbContext context, string tableName, CancellationToken cancellationToken)
    {
        var connection = context.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;

        if (shouldClose)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using var command = connection.CreateCommand();

            if (context.Database.IsNpgsql())
            {
                command.CommandText = $"SELECT to_regclass('\"{tableName}\"') IS NOT NULL";
            }
            else if (context.Database.IsSqlite())
            {
                command.CommandText = $"SELECT EXISTS(SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = '{tableName}')";
            }
            else
            {
                return true;
            }

            var result = await command.ExecuteScalarAsync(cancellationToken);
            return result != null && Convert.ToBoolean(result);
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }

    #endregion
}

/// <summary>
/// Simülasyon araç durumu
/// </summary>
public class SimulasyonAracDurumu
{
    public int AracId { get; set; }
    public double Enlem { get; set; }
    public double Boylam { get; set; }
    public double Hiz { get; set; }
    public double Yon { get; set; }
    public bool KontakAcik { get; set; }
    public bool MotorCalisiyor { get; set; }
    public int YakitSeviyesi { get; set; }
    public int BataryaSeviyesi { get; set; }
    public int SinyalGucu { get; set; }
    public SimulasyonModu Mod { get; set; }
    public DateTime SonDurumDegisikligi { get; set; }
    public double HedefEnlem { get; set; }
    public double HedefBoylam { get; set; }
}

/// <summary>
/// Simülasyon modu
/// </summary>
public enum SimulasyonModu
{
    Hareket,
    Bekliyor,
    Park
}


