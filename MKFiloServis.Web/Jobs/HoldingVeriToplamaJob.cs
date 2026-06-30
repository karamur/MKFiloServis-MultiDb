using MKFiloServis.Web.Services;
using Quartz;

namespace MKFiloServis.Web.Jobs;

[DisallowConcurrentExecution]
public class HoldingVeriToplamaJob : IJob
{
    private readonly IHoldingService _holdingService;

    public HoldingVeriToplamaJob(IHoldingService holdingService)
    {
        _holdingService = holdingService;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var now = DateTime.UtcNow;
        var yil = now.Year;
        var ay = now.Month == 1 ? 12 : now.Month - 1;
        if (now.Month == 1) yil--;

        await _holdingService.ToplaVeKaydetAsync(yil, ay);
    }
}


