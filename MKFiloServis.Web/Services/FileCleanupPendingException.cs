namespace MKFiloServis.Web.Services;

/// <summary>DB değişikliği tamamlandı; fiziksel dosyanın temizlenmesi başarısız oldu.</summary>
public sealed class FileCleanupPendingException : IOException
{
    public FileCleanupPendingException(Exception innerException)
        : base("Dosya kaydı kaldırıldı; fiziksel dosya temizliği tamamlanamadı. Sistem yöneticisine bildirin.", innerException)
    {
    }
}
