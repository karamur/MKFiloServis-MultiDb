using MKFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace MKFiloServis.Web.Services;

/// <summary>
/// Her gece 02:00'da otomatik denetim çalıştırır.
/// Hata varsa IncidentLog oluşturur, Critical ise SystemHealth'i günceller.
/// </summary>
public class NightlyDenetimService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NightlyDenetimService> _logger;

    public NightlyDenetimService(IServiceScopeFactory scopeFactory, ILogger<NightlyDenetimService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // İlk çalıştırmayı 02:00'a kadar bekle
        var now = DateTime.Now;
        var nextRun = new DateTime(now.Year, now.Month, now.Day, 2, 0, 0);
        if (now > nextRun) nextRun = nextRun.AddDays(1);
        var delay = nextRun - now;

        _logger.LogInformation("Gece denetimi planlandı: {NextRun}", nextRun);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(delay, stoppingToken);
                await RunDenetimAsync(stoppingToken);
            }
            catch (TaskCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gece denetimi hatası");
            }
            delay = TimeSpan.FromHours(24);
        }
    }

    private async Task RunDenetimAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
        var denetimService = scope.ServiceProvider.GetRequiredService<DenetimService>();

        await using var db = await dbFactory.CreateDbContextAsync(ct);

        // Tüm aktif firmalar için çalıştır
        var firmalar = await db.Firmalar.Where(f => !f.IsDeleted).Select(f => f.Id).ToListAsync(ct);
        var now = DateTime.Now;
        int ay = now.Month, yil = now.Year;

        _logger.LogInformation("Gece denetimi başladı: {FirmaCount} firma, {Yil}/{Ay}", firmalar.Count, yil, ay);

        foreach (var firmaId in firmalar)
        {
            if (ct.IsCancellationRequested) break;
            try
            {
                var rapor = await denetimService.DenetleAsync(firmaId, yil, ay);
                if (!rapor.Temiz)
                {
                    _logger.LogWarning("Denetim HATALI: Firma={FirmaId} Skor={Skor} Hata={Hata}",
                        firmaId, rapor.Skor, rapor.KalanKontrol);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Denetim hatası: Firma={FirmaId}", firmaId);
            }
        }

        _logger.LogInformation("Gece denetimi tamamlandı");
    }
}


