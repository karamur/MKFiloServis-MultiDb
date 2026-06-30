using Quartz;
using MKFiloServis.Web.Services;

namespace MKFiloServis.Web.Jobs;

[DisallowConcurrentExecution]
public class ZamanliRaporJob : IJob
{
    private readonly ZamanliRaporService _service;
    private readonly ILogger<ZamanliRaporJob> _logger;

    public ZamanliRaporJob(ZamanliRaporService service, ILogger<ZamanliRaporJob> logger)
    {
        _service = service;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("ZamanliRaporJob başlatıldı");
        await _service.GonderGunlukRaporAsync(context.CancellationToken);
        _logger.LogInformation("ZamanliRaporJob tamamlandı");
    }
}


