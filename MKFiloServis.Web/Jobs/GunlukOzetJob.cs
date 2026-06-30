using Quartz;
using MKFiloServis.Web.Services;

namespace MKFiloServis.Web.Jobs;

[DisallowConcurrentExecution]
public class GunlukOzetJob : IJob
{
    private readonly GunlukOzetService _service;

    public GunlukOzetJob(GunlukOzetService service)
    {
        _service = service;
    }

    public Task Execute(IJobExecutionContext context)
    {
        return _service.GonderGunlukOzetAsync(context.CancellationToken);
    }
}


