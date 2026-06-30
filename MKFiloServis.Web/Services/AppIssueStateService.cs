namespace MKFiloServis.Web.Services;

public class AppIssueStateService
{
    public AppIssueReport? LastReport { get; private set; }
    public string? CurrentPage { get; private set; }
    public string? PreviousPage { get; private set; }

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
            ErrorTime = DateTime.Now,
            CurrentPage = CurrentPage,
            PreviousPage = PreviousPage,
            ErrorMessage = exception.Message,
            ErrorType = exception.GetType().FullName ?? exception.GetType().Name,
            StackTrace = exception.StackTrace
        };
    }

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


