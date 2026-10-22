namespace MKFiloServis.Web.Services;

/// <summary>
/// Oturumdaki (devre başına) son hata ve gezinme bilgisi. "Sorun Bildir" penceresi bu bilgiyle açılır:
/// yakalanmayan hatalar <c>AppErrorBoundary</c>'den, ekranda gösterilen hata mesajları <c>ToastContainer</c>'dan gelir.
/// </summary>
public class AppIssueStateService
{
    public AppIssueReport? LastReport { get; private set; }
    public string? CurrentPage { get; private set; }
    public string? PreviousPage { get; private set; }

    /// <summary>Ekranda gösterilen son hata mesajı (hata bildirimi kutusu).</summary>
    public AppIssueReport? LastVisibleError { get; private set; }

    /// <summary>"Sorun Bildir" penceresinin açılması istendi (hata kutusundaki ya da hata panelindeki düğme).</summary>
    public event Action<AppIssueReport?>? ReportFormRequested;

    public void TrackNavigation(string? relativeUri)
    {
        var normalized = Normalize(relativeUri);
        if (string.Equals(CurrentPage, normalized, StringComparison.OrdinalIgnoreCase))
            return;

        if (!string.IsNullOrWhiteSpace(CurrentPage))
            PreviousPage = CurrentPage;

        CurrentPage = normalized;
    }

    public void Report(Exception exception)
    {
        LastReport = new AppIssueReport
        {
            ErrorTime = DateTime.UtcNow,
            CurrentPage = CurrentPage,
            PreviousPage = PreviousPage,
            ErrorMessage = exception.Message,
            ErrorType = exception.GetType().FullName ?? exception.GetType().Name,
            StackTrace = exception.StackTrace
        };
    }

    public void ReportVisibleError(string message)
    {
        LastVisibleError = new AppIssueReport
        {
            ErrorTime = DateTime.UtcNow,
            CurrentPage = CurrentPage,
            PreviousPage = PreviousPage,
            ErrorMessage = message,
            ErrorType = "Ekranda gösterilen hata"
        };
    }

    /// <summary>Son <paramref name="pencere"/> içinde yaşanan en yeni hata (yakalanmayan ya da ekranda gösterilen).</summary>
    public AppIssueReport? RecentIssue(TimeSpan pencere)
    {
        var aday = LastVisibleError != null && (LastReport == null || LastVisibleError.ErrorTime > LastReport.ErrorTime)
            ? LastVisibleError
            : LastReport;
        return aday != null && DateTime.UtcNow - aday.ErrorTime <= pencere ? aday : null;
    }

    public void RequestReportForm(AppIssueReport? report = null) => ReportFormRequested?.Invoke(report);

    private static string Normalize(string? relativeUri)
    {
        if (string.IsNullOrWhiteSpace(relativeUri))
            return "/dashboard";

        return relativeUri.StartsWith('/') ? relativeUri : $"/{relativeUri}";
    }
}

public class AppIssueReport
{
    public DateTime ErrorTime { get; set; }
    public string? CurrentPage { get; set; }
    public string? PreviousPage { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public string ErrorType { get; set; } = string.Empty;
    public string? StackTrace { get; set; }
}
