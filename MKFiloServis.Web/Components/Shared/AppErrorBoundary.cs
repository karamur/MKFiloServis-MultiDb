using MKFiloServis.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace MKFiloServis.Web.Components.Shared;

public class AppErrorBoundary : ErrorBoundary
{
    [Inject] public NavigationManager Navigation { get; set; } = null!;
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

        if (!Navigation.Uri.Contains("/ters-giden-bir-sey", StringComparison.OrdinalIgnoreCase) &&
            !Navigation.Uri.Contains("/error", StringComparison.OrdinalIgnoreCase))
        {
            Navigation.NavigateTo("/ters-giden-bir-sey", forceLoad: false);
        }

        return Task.CompletedTask;
    }
}



