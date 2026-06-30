using Microsoft.EntityFrameworkCore;
using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Web.Services;

/// <summary>
/// Cari hatırlatmaları için arka plan servisi
/// Her gün belirlenen saatte otomatik kontrol yapar
/// </summary>
public class CariHatirlatmaBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CariHatirlatmaBackgroundService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(30); // Her 30 dakikada kontrol
    private readonly TimeSpan _initialDelay = TimeSpan.FromMinutes(3); // Uygulama başladıktan 3 dk sonra

    public CariHatirlatmaBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<CariHatirlatmaBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Cari hatırlatma servisi başlatıldı");

        // Uygulama tam başlayana kadar bekle
        await Task.Delay(_initialDelay, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await KontrolVeHatirlatAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cari hatırlatma kontrolü hatası");
            }

            try
            {
                await Task.Delay(_checkInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    public Task RunOnceAsync(CancellationToken cancellationToken = default)
    {
        return KontrolVeHatirlatAsync(cancellationToken);
    }

    private async Task KontrolVeHatirlatAsync(CancellationToken stoppingToken)
    {
        if (stoppingToken.IsCancellationRequested)
            return;

        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var hatirlatmaService = scope.ServiceProvider.GetRequiredService<ICariHatirlatmaService>();

        // Tüm firmaları kontrol et
        var firmalar = await context.Firmalar
            .Where(f => !f.IsDeleted && f.Aktif)
            .Select(f => f.Id)
            .ToListAsync(stoppingToken);

        foreach (var firmaId in firmalar)
        {
            try
            {
                await KontrolFirmaAsync(hatirlatmaService, firmaId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Firma hatırlatma kontrolü hatası: FirmaId={FirmaId}", firmaId);
            }
        }
    }

    private async Task KontrolFirmaAsync(ICariHatirlatmaService hatirlatmaService, int firmaId)
    {
        var ayarlar = await hatirlatmaService.GetAyarlarAsync(firmaId);

        if (!ayarlar.HatirlatmaAktif)
            return;

        // Bugün kontrolün yapılma saati geldi mi?
        var simdi = DateTime.Now;
        var kontrolSaati = new TimeSpan(ayarlar.KontrolSaati, 0, 0);
        var bugunKontrolZamani = DateTime.Today.Add(kontrolSaati);

        // Bugün zaten kontrol yapıldı mı?
        if (ayarlar.SonKontrolTarihi.HasValue && ayarlar.SonKontrolTarihi.Value.Date == DateTime.Today)
            return;

        // Kontrol saati henüz gelmedi mi?
        if (simdi.TimeOfDay < kontrolSaati)
            return;

        _logger.LogInformation("Cari hatırlatma kontrolü başlatılıyor: FirmaId={FirmaId}", firmaId);

        var rapor = await hatirlatmaService.HatirlatmaKontroluYapAsync(
            firmaId, 
            emailGonder: ayarlar.EmailGonder, 
            bildirimOlustur: ayarlar.SistemBildirimiOlustur);

        _logger.LogInformation(
            "Cari hatırlatma kontrolü tamamlandı: FirmaId={FirmaId}, UyarıSayısı={UyariSayisi}", 
            firmaId, 
            rapor.ToplamUyariSayisi);
    }
}


