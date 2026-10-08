namespace MKFiloServis.Web.Services;

/// <summary>DB değişikliği tamamlandı; fiziksel dosyanın temizlenmesi başarısız oldu.</summary>
public sealed class FileCleanupPendingException : IOException
{
    public FileCleanupPendingException(Exception innerException)
        : base("Veritabanı değişikliği kaydedildi; fiziksel dosya temizliği tamamlanamadı. Sistem yöneticisine bildirin.", innerException)
    {
    }
}
