using KOAFiloServis.Web.Services;
using Quartz;

namespace KOAFiloServis.Web.Jobs;

[DisallowConcurrentExecution]
public class PuantajReconciliationJob : IJob
{
    private readonly IPuantajReconciliationService _reconciliation;

    public PuantajReconciliationJob(IPuantajReconciliationService reconciliation)
        => _reconciliation = reconciliation;

    public async Task Execute(IJobExecutionContext context)
    {
        await _reconciliation.RunAsync(context.CancellationToken);
    }
}
