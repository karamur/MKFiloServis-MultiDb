using Quartz;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Web.Jobs;

[DisallowConcurrentExecution]
public class BakimPeriyotJob : IJob
{
    private readonly IBakimPeriyotService _service;
    private readonly ILogger<BakimPeriyotJob> _logger;

    public BakimPeriyotJob(IBakimPeriyotService service, ILogger<BakimPeriyotJob> logger)
    {
        _service = service;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("BakimPeriyotJob başlatıldı");
        await _service.TumAraclariBakimKontrolAsync(context.CancellationToken);
        _logger.LogInformation("BakimPeriyotJob tamamlandı");
    }
}


