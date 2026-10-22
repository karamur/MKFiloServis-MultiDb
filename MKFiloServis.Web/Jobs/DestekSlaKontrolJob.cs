using Quartz;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Web.Jobs;

/// <summary>
/// Destek talepleri: SLA süresi aşılan açık talepleri işaretleyip ilgililere bildirir, çözüldükten sonra
/// ayarlanan gün kadar yanıt gelmeyen talepleri kapatır (Destek Ayarları → otomatik SLA / otomatik kapatma).
/// </summary>
[DisallowConcurrentExecution]
public class DestekSlaKontrolJob : IJob
{
    private readonly IDestekTalebiService _destekService;
    private readonly ILogger<DestekSlaKontrolJob> _logger;

    public DestekSlaKontrolJob(IDestekTalebiService destekService, ILogger<DestekSlaKontrolJob> logger)
    {
        _destekService = destekService;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            var asilan = await _destekService.CheckAndUpdateSlaViolationsAsync();
            var kapanan = await _destekService.CozulenTalepleriOtomatikKapatAsync();
            if (asilan > 0 || kapanan > 0)
                _logger.LogInformation("Destek SLA kontrolü: {Asilan} SLA aşımı işaretlendi, {Kapanan} çözülmüş talep kapatıldı", asilan, kapanan);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Destek SLA kontrolü başarısız");
        }
    }
}
