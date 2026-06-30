namespace MKFiloServis.Web.Services;

public interface IToastService
{
    event Action<ToastMessage>? OnShow;
    void ShowSuccess(string message, string? title = null);
    void ShowError(string message, string? title = null);
    void ShowWarning(string message, string? title = null);
    void ShowInfo(string message, string? title = null);
}

public class ToastService : IToastService
{
    public event Action<ToastMessage>? OnShow;

    public void ShowSuccess(string message, string? title = null)
    {
        OnShow?.Invoke(new ToastMessage(ToastType.Success, message, title ?? "Başarılı"));
    }

    public void ShowError(string message, string? title = null)
    {
        OnShow?.Invoke(new ToastMessage(ToastType.Error, message, title ?? "Hata"));
    }

    public void ShowWarning(string message, string? title = null)
    {
        OnShow?.Invoke(new ToastMessage(ToastType.Warning, message, title ?? "Uyarı"));
    }

    public void ShowInfo(string message, string? title = null)
    {
        OnShow?.Invoke(new ToastMessage(ToastType.Info, message, title ?? "Bilgi"));
    }
}

public class ToastMessage
{
    public Guid Id { get; } = Guid.NewGuid();
    public ToastType Type { get; }
    public string Message { get; }
    public string Title { get; }
    public DateTime CreatedAt { get; } = DateTime.Now;

    public ToastMessage(ToastType type, string message, string title)
    {
        Type = type;
        Message = message;
        Title = title;
    }

    public string IconClass => Type switch
    {
        ToastType.Success => "bi-check-circle-fill",
        ToastType.Error => "bi-x-circle-fill",
        ToastType.Warning => "bi-exclamation-triangle-fill",
        ToastType.Info => "bi-info-circle-fill",
        _ => "bi-bell-fill"
    };

    public string BgClass => Type switch
    {
        ToastType.Success => "bg-success",
        ToastType.Error => "bg-danger",
        ToastType.Warning => "bg-warning",
        ToastType.Info => "bg-info",
        _ => "bg-secondary"
    };

    public string TextClass => Type switch
    {
        ToastType.Warning => "text-dark",
        _ => "text-white"
    };
}

public enum ToastType
{
    Success,
    Error,
    Warning,
    Info
}


