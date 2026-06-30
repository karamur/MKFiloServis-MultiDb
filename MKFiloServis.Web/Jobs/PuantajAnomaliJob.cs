using MKFiloServis.Web.Services.Interfaces;
using Quartz;

namespace MKFiloServis.Web.Jobs;

/// <summary>
/// Puantaj anomali tarama job'ı.
/// Her hafta Pazartesi sabah 06:00'da bir önceki ayın verilerini tarar.
/// </summary>
[DisallowConcurrentExecution]
public class PuantajAnomaliJob : IJob
{
    private readonly IPuantajAnomaliService _anomaliService;
    private readonly ILogger<PuantajAnomaliJob> _logger;

    public PuantajAnomaliJob(
        IPuantajAnomaliService anomaliService,
        ILogger<PuantajAnomaliJob> logger)
    {
        _anomaliService = anomaliService;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("PuantajAnomaliJob basladi.");

        try
        {
            // Bir önceki ayın verilerini tara
            var simdi = DateTime.UtcNow;
            var yil = simdi.Year;
            var ay = simdi.Month == 1 ? 12 : simdi.Month - 1;
            if (simdi.Month == 1) yil--;

            var count = await _anomaliService.TumTaramaAsync(yil, ay);

            _logger.LogInformation(
                "PuantajAnomaliJob tamamlandi: {Count} anomali tespit edildi ({Yil}-{Ay}).",
                count, yil, ay);

            // AI analizi de çalıştır (opsiyonel, Ollama yoksa sessizce geçer)
            try
            {
                var aiSonuc = await _anomaliService.AIAnalizAsync(yil, ay);
                if (!string.IsNullOrEmpty(aiSonuc) && !aiSonuc.Contains("gerekli"))
                    _logger.LogInformation("AI anomali analizi sonucu:\n{Sonuc}", aiSonuc);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "AI anomali analizi basarisiz (Ollama offline olabilir).");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PuantajAnomaliJob hata aldi.");
            throw new JobExecutionException(ex) { RefireImmediately = false };
        }
    }
}


