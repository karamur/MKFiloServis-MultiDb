using MKFiloServis.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace MKFiloServis.Web.Components.Shared;

public class AppErrorBoundary : ErrorBoundary
{
    [Inject] public AppIssueStateService AppIssueState { get; set; } = null!;
    [Inject] public ILogger<AppErrorBoundary> Logger { get; set; } = null!;

    protected override Task OnErrorAsync(Exception exception)
    {
        AppIssueState.Report(exception);
        Logger.LogError(exception, "AppErrorBoundary yakaladı: {Message}", exception.Message);

        // JSException (downloadFile vb.) veya NavigationException ise navigate etme — sadece logla
        if (exception is JSException || exception is NavigationException || exception is JSDisconnectedException || exception is OperationCanceledException || exception is ObjectDisposedException)
        {
            Recover();
            return Task.CompletedTask;
        }

        // Hata kayda geçer; kullanıcıyı başka bir adrese otomatik taşıma.
        // ErrorBoundary aynı adreste hata durumunu gösterir.
        return Task.CompletedTask;
    }
}



