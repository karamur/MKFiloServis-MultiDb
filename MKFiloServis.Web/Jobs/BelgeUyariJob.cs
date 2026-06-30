using Quartz;
using MKFiloServis.Web.Services;

namespace MKFiloServis.Web.Jobs;

[DisallowConcurrentExecution]
public class BelgeUyariJob : IJob
{
    private readonly BelgeUyariBackgroundService _service;

    public BelgeUyariJob(BelgeUyariBackgroundService service)
    {
        _service = service;
    }

    public Task Execute(IJobExecutionContext context)
    {
        return _service.RunOnceAsync(context.CancellationToken);
    }
}


