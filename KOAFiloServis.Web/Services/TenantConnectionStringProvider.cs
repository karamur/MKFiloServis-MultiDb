using KOAFiloServis.Shared.Entities;
using KOAFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace KOAFiloServis.Web.Services;

/// <summary>
/// [OBSOLETE] Nihai mimari (2026) ile kullanımdan kaldırılmıştır.
/// Tenant-per-DB yaklaşımı terk edilmiş, tek veritabanı kullanılmaktadır.
/// </summary>
[Obsolete("Nihai mimari: TenantConnectionStringProvider kaldırıldı. Tek connection string kullanın.")]
public sealed class TenantConnectionStringProvider : ITenantConnectionStringProvider
{
    private readonly IAktifFirmaProvider _firmaProvider;
    private readonly IDbContextFactory<ApplicationDbContext> _masterFactory;
    private readonly string _masterConnectionString;
    private readonly string _dbHost;
    private readonly int _dbPort;
    private readonly string _dbUsername;
    private readonly string _dbPassword;
    private bool _defaultFirmaResolved;

    public TenantConnectionStringProvider(
        IAktifFirmaProvider firmaProvider,
        IDbContextFactory<ApplicationDbContext> masterFactory,
        IConfiguration configuration)
    {
        _firmaProvider = firmaProvider;
        _masterFactory = masterFactory;
        _masterConnectionString = configuration.GetConnectionString("MasterConnection")
            ?? throw new InvalidOperationException("MasterConnection bulunamadı.appsettings.json içinde 'MasterConnection' tanımlı olmalıdır.");

        var builder = new NpgsqlConnectionStringBuilder(_masterConnectionString);
        _dbHost = builder.Host ?? "localhost";
        _dbPort = builder.Port;
        _dbUsername = builder.Username ?? "postgres";
        _dbPassword = builder.Password ?? "";
    }

    public string? GetTenantConnectionString()
    {
        // DatabaseName eksikse (varsayılan firma henüz çözümlenmemişse)
        // Master DB'den firma bilgisini çekip AktifFirmaProvider'ı güncelle
        if (string.IsNullOrWhiteSpace(_firmaProvider.Mevcut.DatabaseName))
        {
            var firmaId = _firmaProvider.AktifFirmaId ?? 1;
            ResolveFirmaFromMasterDb(firmaId);
        }

        var aktifFirmaId = _firmaProvider.AktifFirmaId;
        if (aktifFirmaId == null || aktifFirmaId.Value == 0)
            throw new InvalidOperationException(
                "Aktif firma bulunamadı. Lütfen Master DB'de en az bir aktif firma tanımlayın.");

        var databaseName = _firmaProvider.Mevcut.DatabaseName;
        return GetConnectionStringForFirma(aktifFirmaId.Value, databaseName);
    }

    public string? GetConnectionStringForFirma(int firmaId, string? databaseName)
    {
        if (string.IsNullOrWhiteSpace(databaseName))
            throw new InvalidOperationException(
                $"Firma (Id={firmaId}) için DatabaseName tanımlı değil. " +
                "Lütfen önce tenant veritabanını oluşturun (Ayarlar → Firmalar → Tenant DB Oluştur).");

        return new NpgsqlConnectionStringBuilder
        {
            Host = _dbHost,
            Port = _dbPort,
            Database = databaseName,
            Username = _dbUsername,
            Password = _dbPassword,
            Pooling = true,
            MinPoolSize = 1,
            MaxPoolSize = 10,
            CommandTimeout = 30
        }.ConnectionString;
    }

    public string GetMasterConnectionString() => _masterConnectionString;

    /// <summary>
    /// Master DB'den belirtilen ID'ye sahip firmayı (veya varsayılanı) çözümleyip
    /// <see cref="IAktifFirmaProvider"/>'a set eder. Her scope'ta yalnızca bir kez çalışır.
    /// </summary>
    private void ResolveFirmaFromMasterDb(int firmaId)
    {
        if (_defaultFirmaResolved) return;
        _defaultFirmaResolved = true;

        try
        {
            using var masterCtx = _masterFactory.CreateDbContext();
            var firma = masterCtx.Firmalar
                .FirstOrDefault(f => f.Id == firmaId && f.Aktif && !f.IsDeleted)
                ?? masterCtx.Firmalar
                    .FirstOrDefault(f => f.VarsayilanFirma && f.Aktif && !f.IsDeleted)
                ?? masterCtx.Firmalar
                    .Where(f => f.Aktif && !f.IsDeleted)
                    .OrderBy(f => f.SiraNo)
                    .ThenBy(f => f.FirmaAdi)
                    .FirstOrDefault();

            if (firma == null)
                throw new InvalidOperationException(
                    $"Master DB'de Id={firmaId} veya varsayılan aktif firma bulunamadı. " +
                    "Lütfen en az bir firma tanımlayın.");

            _firmaProvider.Set(new AktifFirmaBilgisi
            {
                FirmaId = firma.Id,
                FirmaKodu = firma.FirmaKodu,
                FirmaAdi = firma.FirmaAdi,
                AktifDonemYil = firma.AktifDonemYil,
                AktifDonemAy = firma.AktifDonemAy,
                DatabaseName = firma.DatabaseName,
                TumFirmalar = false
            });
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Firma (Id={firmaId}) çözümlenirken hata oluştu: {ex.Message}", ex);
        }
    }
}
