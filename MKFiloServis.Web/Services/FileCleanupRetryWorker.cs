using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Web.Services;

public sealed class FileCleanupRetryWorker : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(5);
    private readonly FileCleanupJournal _journal;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<FileCleanupRetryWorker> _logger;

    public FileCleanupRetryWorker(
        FileCleanupJournal journal,
        IServiceScopeFactory scopeFactory,
        ILogger<FileCleanupRetryWorker> logger)
    {
        _journal = journal;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessDueOnceAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Kalıcı dosya temizleme kuyruğu okunamadı/işlenemedi; sonraki turda yeniden denenecek.");
                }

                await Task.Delay(PollInterval, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal host shutdown.
        }
    }

    internal async Task ProcessDueOnceAsync(CancellationToken ct = default)
    {
        while (!ct.IsCancellationRequested)
        {
            var pending = await _journal.ClaimDueAsync(DateTime.UtcNow, LeaseDuration, maxCount: 1, cancellationToken: ct);
            if (pending.Count == 0)
                break;
            await ProcessAsync(pending[0], ct);
        }
    }

    private async Task ProcessAsync(PendingFileCleanup item, CancellationToken ct)
    {
        using var leaseCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var renewTask = RenewUntilFinishedAsync(item, leaseCts);
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            if (!await _journal.RenewLeaseAsync(item.RelativePath, item.LeaseId!, DateTime.UtcNow, LeaseDuration, leaseCts.Token))
                throw new InvalidOperationException("Dosya temizleme lease'i kaybedildi.");

            if (LegacyFileCleanupService.IsLegacyCleanupKey(item.RelativePath))
            {
                var legacy = scope.ServiceProvider.GetRequiredService<LegacyFileCleanupService>();
                await legacy.ProcessAsync(item.RelativePath, leaseCts.Token, completeJournal: false);
            }
            else
            {
                var files = scope.ServiceProvider.GetRequiredService<ISecureFileService>();
                var deleted = await files.TryDeleteIfUnreferencedAsync(item.RelativePath, leaseCts.Token);
                if (!deleted && item.WaitForReferenceRemoval)
                {
                    await _journal.DeferAsync(item.RelativePath,
                        "Veritabanında dosya başvurusu sürüyor; silme kaydı korunarak yeniden denenecek.",
                        item.LeaseId, ct);
                    return;
                }
            }
            if (!await _journal.CompleteClaimAsync(item.RelativePath, item.LeaseId!, item.Revision, leaseCts.Token))
                throw new InvalidOperationException("Dosya temizleme sonucu kaydedilemedi; iş sahipliği değişti.");
            _logger.LogInformation("Kuyruktaki dosya temizleme isteği işlendi; şifreli içerik geri alınabilir karantinada korunur: {RelativePath}", item.RelativePath);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Kuyruktaki dosya bu turda temizlenemedi; yeniden denenecek: {RelativePath}", item.RelativePath);
            await _journal.DeferAsync(item.RelativePath, ex.Message, item.LeaseId, ct);
        }
        finally
        {
            leaseCts.Cancel();
            await renewTask;
        }
    }

    private async Task RenewUntilFinishedAsync(PendingFileCleanup item, CancellationTokenSource leaseCts)
    {
        try
        {
            while (!leaseCts.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMinutes(1), leaseCts.Token);
                if (!await _journal.RenewLeaseAsync(item.RelativePath, item.LeaseId!, DateTime.UtcNow, LeaseDuration, leaseCts.Token))
                {
                    _logger.LogWarning("Dosya temizleme lease'i başka worker tarafından alındı: {RelativePath}", item.RelativePath);
                    leaseCts.Cancel();
                    return;
                }
            }
        }
        catch (OperationCanceledException) when (leaseCts.IsCancellationRequested)
        {
            // Processing completed or host stopped.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dosya temizleme lease'i yenilenemedi: {RelativePath}", item.RelativePath);
            leaseCts.Cancel();
        }
    }
}
