namespace MKFiloServis.Shared.Exceptions;

// ═══════════════════════════════════════════════════════════════════
// Base marker — tüm puantaj exception'ları buradan türer
// ═══════════════════════════════════════════════════════════════════

public abstract class PuantajException : Exception
{
    protected PuantajException(string message, Exception? inner = null)
        : base(message, inner) { }
}

// ═══════════════════════════════════════════════════════════════════
// BUSINESS EXCEPTIONS — retry YOK, iş kuralı ihlali
// ═══════════════════════════════════════════════════════════════════

public abstract class PuantajBusinessException : PuantajException
{
    protected PuantajBusinessException(string message, Exception? inner = null)
        : base(message, inner) { }
}

/// <summary>Hesap dönemi kilitli — revizyon için önce kilit açılmalı.</summary>
public sealed class PuantajDonemKilitliException : PuantajBusinessException
{
    public int HesapDonemiId { get; }
    public int Versiyon { get; }

    public PuantajDonemKilitliException(int hesapDonemiId, int versiyon)
        : base($"Dönem kilitli: HesapDonemiId={hesapDonemiId}, V{versiyon}")
        => (HesapDonemiId, Versiyon) = (hesapDonemiId, versiyon);
}

/// <summary>İşlenecek operasyon kaydı bulunamadı.</summary>
public sealed class PuantajOperasyonBulunamadiException : PuantajBusinessException
{
    public int Yil { get; }
    public int Ay { get; }

    public PuantajOperasyonBulunamadiException(int yil, int ay)
        : base($"{yil}/{ay:D2} döneminde işlenecek operasyon bulunamadı.")
        => (Yil, Ay) = (yil, ay);
}

/// <summary>Dönem zaten hesaplanmış — idempotency check.</summary>
public sealed class PuantajDonemZatenHesaplanmisException : PuantajBusinessException
{
    public int HesapDonemiId { get; }
    public int Versiyon { get; }

    public PuantajDonemZatenHesaplanmisException(int hesapDonemiId, int versiyon)
        : base($"Dönem zaten hesaplanmış: V{versiyon}")
        => (HesapDonemiId, Versiyon) = (hesapDonemiId, versiyon);
}

// ═══════════════════════════════════════════════════════════════════
// INFRASTRUCTURE EXCEPTIONS — retry VAR (transient)
// ═══════════════════════════════════════════════════════════════════

public abstract class PuantajInfrastructureException : PuantajException
{
    /// <summary>Bu hata geçici mi? (retry uygun mu?)</summary>
    public bool IsTransientFailure { get; set; } = true;

    protected PuantajInfrastructureException(string message, Exception? inner = null)
        : base(message, inner) { }
}

/// <summary>Tenant veritabanına erişilemiyor.</summary>
public sealed class PuantajTenantOfflineException : PuantajInfrastructureException
{
    public int FirmaId { get; }
    public string? DatabaseName { get; }

    public PuantajTenantOfflineException(int firmaId, string? databaseName, Exception inner)
        : base($"Firma {firmaId} tenant DB offline: {databaseName ?? "shared"}", inner)
        => (FirmaId, DatabaseName) = (firmaId, databaseName);
}

/// <summary>Veritabanı bağlantı hatası — transient.</summary>
public sealed class PuantajDatabaseConnectionException : PuantajInfrastructureException
{
    public PuantajDatabaseConnectionException(string message, Exception inner)
        : base($"DB connection failed: {message}", inner) { }
}

/// <summary>Mutex alınamadı — başka bir worker işliyor.</summary>
public sealed class PuantajMutexAcquireFailedException : PuantajInfrastructureException
{
    public int FirmaId { get; }
    public int Yil { get; }
    public int Ay { get; }

    public PuantajMutexAcquireFailedException(int firmaId, int yil, int ay, string reason)
        : base($"Mutex alınamadı: Firma {firmaId}, {yil}/{ay:D2} — {reason}")
    {
        (FirmaId, Yil, Ay) = (firmaId, yil, ay);
        IsTransientFailure = false; // retry anlamsız — başkası işliyor
    }
}

// ═══════════════════════════════════════════════════════════════════
// FATAL EXCEPTIONS — retry YOK, manual intervention gerekli
// ═══════════════════════════════════════════════════════════════════

public abstract class PuantajFatalException : PuantajException
{
    protected PuantajFatalException(string message, Exception? inner = null)
        : base(message, inner) { }
}

/// <summary>Konfigürasyon eksik/hatalı — uygulama başlatılamaz.</summary>
public sealed class PuantajConfigurationException : PuantajFatalException
{
    public PuantajConfigurationException(string key)
        : base($"Kritik config eksik: '{key}'. PuantajEngine:AutoProcess:{key} ayarlanmalı.") { }
}

/// <summary>DI container çözümleme hatası.</summary>
public sealed class PuantajDependencyResolutionException : PuantajFatalException
{
    public Type ServiceType { get; }

    public PuantajDependencyResolutionException(Type serviceType, Exception inner)
        : base($"DI resolution failed: {serviceType.Name}", inner)
        => ServiceType = serviceType;
}

/// <summary>Firma listesi alınamadı — Master DB erişilemez.</summary>
public sealed class PuantajMasterDbOfflineException : PuantajFatalException
{
    public PuantajMasterDbOfflineException(Exception inner)
        : base("Master veritabanına erişilemiyor. Hiçbir tenant işlenemez.", inner) { }
}


