using KOAFiloServis.Web.Services.Interfaces;
using Quartz;

namespace KOAFiloServis.Web.Jobs;

/// <summary>
/// Luca Portal e-Fatura / e-Arşiv otomatik senkronizasyon job'ı.
/// Her gece 03:00'te son 24 saatlik belgeleri çeker.
/// </summary>
[DisallowConcurrentExecution]
public class LucaPortalSenkronJob : IJob
{
    private readonly ILucaPortalService _lucaPortalService;
    private readonly ILogger<LucaPortalSenkronJob> _logger;

    public LucaPortalSenkronJob(
        ILucaPortalService lucaPortalService,
        ILogger<LucaPortalSenkronJob> logger)
    {
        _lucaPortalService = lucaPortalService;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("LucaPortalSenkronJob basladi.");

        try
        {
            var simdi = DateTime.UtcNow;
            var baslangic = simdi.AddDays(-1);
            var bitis = simdi;

            var imported = await _lucaPortalService.TumBelgeleriSenkronizeEtAsync(
                baslangic, bitis, null);

            _logger.LogInformation(
                "LucaPortalSenkronJob tamamlandi: {Count} belge aktarildi ({Start:yyyy-MM-dd} - {End:yyyy-MM-dd}).",
                imported, baslangic, bitis);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LucaPortalSenkronJob hata aldi.");
            throw new JobExecutionException(ex) { RefireImmediately = false };
        }
    }
}
