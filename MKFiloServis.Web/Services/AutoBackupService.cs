namespace MKFiloServis.Web.Services;

using MKFiloServis.Web.Services.Interfaces;

public class AutoBackupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AutoBackupService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(30);
    private readonly TimeSpan _initialDelay = TimeSpan.FromMinutes(2); // Uygulama basladiktan 2 dk sonra kontrol

    public AutoBackupService(IServiceProvider serviceProvider, ILogger<AutoBackupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Otomatik yedekleme servisi baslatildi.");

        // Uygulama tam baslayana kadar bekle
        await Task.Delay(_initialDelay, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAndBackupAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Normal kapanis
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Otomatik yedekleme kontrolu hatasi");
            }

            try
            {
                await Task.Delay(_checkInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    public Task RunOnceAsync(CancellationToken cancellationToken = default)
    {
        return CheckAndBackupAsync(cancellationToken);
    }

    private async Task CheckAndBackupAsync(CancellationToken stoppingToken)
    {
        if (stoppingToken.IsCancellationRequested)
            return;

        using var scope = _serviceProvider.CreateScope();
        var backupService = scope.ServiceProvider.GetRequiredService<IBackupService>();

        var settings = backupService.GetSettings();

        if (!settings.AutoBackupEnabled)
            return;

        var shouldBackup = settings.ShouldRun();

        if (shouldBackup && !stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Otomatik yedekleme baslatiliyor...");
            var result = await backupService.CreateBackupAsync();

            if (result.Success)
            {
                _logger.LogInformation("Otomatik yedekleme tamamlandi: {FileName}", result.FileName);
            }
            else
            {
                _logger.LogError("Otomatik yedekleme basarisiz: {Error}", result.ErrorMessage);
            }
        }
    }
}


