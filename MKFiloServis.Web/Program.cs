using MKFiloServis.Web.Components;
using MKFiloServis.Web.Data;
using MKFiloServis.Web.Helpers;
using MKFiloServis.Web.Jobs;
using MKFiloServis.Web.Services;
using MKFiloServis.Web.Services.Interfaces;
using MKFiloServis.Web.Services.Security;
using MKFiloServis.Web.Hubs;
using MKFiloServis.Shared.Entities;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OfficeOpenXml;
using Quartz;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Localization;

// EPPlus lisans ayari (NonCommercial kullanim icin)
ExcelPackage.License.SetNonCommercialPersonal("MKFiloServis");

var builder = WebApplication.CreateBuilder(args);

// Npgsql EnableLegacyTimestampBehavior: herhangi bir UseNpgsql() cagrisindan ONCE set edilmeli.
// Npgsql static constructor bu switch'i ilk kez UseNpgsql()'de okur ve cache'ler.
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

// Database Provider Secimi (dbsettings.json varsa onu oncele)
var databaseRuntime = await DatabaseRuntimeResolver.ResolveAsync(builder.Configuration, builder.Environment);
var dbProvider = databaseRuntime.Provider.ToString();
var defaultConnectionString = databaseRuntime.ConnectionString;

// URL baglama: IIS tarafi zaten adres/port yonetir; burada zorlama yapma.
// Explicit URL verilmediyse development ve self-hosted production icin cakismayan port sec.
var hasExplicitUrls = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASPNETCORE_URLS"))
    || args.Any(a => a.StartsWith("--urls", StringComparison.OrdinalIgnoreCase));
var isIisHosted = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASPNETCORE_IIS_PHYSICAL_PATH"))
    || !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASPNETCORE_PORT"));

static bool PortKullanilabilirMi(int port)
{
    try
    {
        using var listener = new TcpListener(IPAddress.Any, port);
        listener.Start();
        listener.Stop();
        return true;
    }
    catch (SocketException)
    {
        return false;
    }
}

static int UygunPortSec(int baslangicPortu, int denemeSayisi)
{
    var secilenPort = baslangicPortu;

    while (secilenPort < baslangicPortu + denemeSayisi && !PortKullanilabilirMi(secilenPort))
    {
        secilenPort++;
    }

    return secilenPort;
}

if (!hasExplicitUrls && !isIisHosted)
{
    var baslangicPortu = 5191;
    var secilenPort = UygunPortSec(baslangicPortu, 20);
    builder.WebHost.UseUrls($"http://0.0.0.0:{secilenPort}");
    Environment.SetEnvironmentVariable("MKFILOSERVIS_ACTIVE_PORT", secilenPort.ToString());
}

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSingleton<AktiviteLogInterceptor>();
builder.Services.AddSingleton<ICurrentUserAccessor, CurrentUserAccessor>();

// Database - Kanonik migration otoritesi PostgreSQL, desteklenen runtime: PostgreSQL + SQLite + SQL Server + MySQL
builder.Services.AddPooledDbContextFactory<ApplicationDbContext>((sp, options) =>
{
    var enableSensitiveDataLogging = builder.Environment.IsDevelopment() &&
        builder.Configuration.GetValue<bool>("EntityFramework:EnableSensitiveDataLogging");

    // Production: varsayılan NoTracking — salt-okunur sorgularda change tracker overhead'ini kaldırır.
    // CRUD işlemleri .AsTracking() ile explicit tracking kullanır.
    options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);

    switch (databaseRuntime.Provider)
    {
        case DatabaseProvider.SQLite:
            options.UseSqlite(defaultConnectionString, sqliteOptions =>
            {
                sqliteOptions.CommandTimeout(30);
                sqliteOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            });
            break;

        case DatabaseProvider.SQLServer:
            options.UseSqlServer(defaultConnectionString, sqlServerOptions =>
            {
                sqlServerOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorNumbersToAdd: null);
                sqlServerOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                sqlServerOptions.CommandTimeout(30);
            });
            break;

        case DatabaseProvider.MySQL:
            options.UseMySQL(defaultConnectionString, mySqlOptions =>
            {
                mySqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorNumbersToAdd: null);
                mySqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                mySqlOptions.CommandTimeout(30);
            });
            break;

        default:
            options.UseNpgsql(defaultConnectionString, npgsqlOptions =>
            {
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: null);
                // Birden fazla collection Include/projection içeren sorgularda kartesyen çoğalmayı azaltır.
                npgsqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                npgsqlOptions.CommandTimeout(30);
                npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "public");
            });
            break;
    }

    if (enableSensitiveDataLogging)
    {
        options.EnableSensitiveDataLogging();
    }

    // Query-filter etkileşim uyarısı: EF Core'un global query filter + required navigation
    // etkileşiminde verdiği false-positive uyarı. Model doğru, uyarı bastırılıyor.
    // Pending model değişiklik uyarısı startup'ta exception'a dönüşmemeli.
    options.ConfigureWarnings(w =>
    {
        w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning);
        w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.MultipleCollectionIncludeWarning);
        w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning);
    });
    options.AddInterceptors(sp.GetRequiredService<AktiviteLogInterceptor>());
});

// Scoped IDbContextFactory: her context oluşturulduğunda SetServiceProvider çağrılır.
// Pooled factory'yi sarar → IAktifFirmaProvider + Global Query Filter (Kural 6, Kural 7).
// Singleton tüketiciler (LisansService) IServiceScopeFactory üzerinden ApplicationDbContext alır.
builder.Services.AddScoped<IDbContextFactory<ApplicationDbContext>>(sp =>
{
    var options = sp.GetRequiredService<DbContextOptions<ApplicationDbContext>>();
    return new MKFiloServis.Web.Data.ScopedDbContextFactory(options, sp);
});
builder.Services.AddScoped(sp => sp.GetRequiredService<IDbContextFactory<ApplicationDbContext>>().CreateDbContext());

// Nihai Mimari: HoldingDbContext kaldırıldı — HoldingVeri ve HoldingRapor ApplicationDbContext'te

// Authentication - Her circuit (tarayici baglantisi) icin bagimsiz oturum yonetimi
// Scoped: Her Blazor circuit kendi oturumunu yonetir - farkli PC/tarayicilar birbirini etkilemez
builder.Services.AddScoped<AppAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => 
    sp.GetRequiredService<AppAuthenticationStateProvider>());
builder.Services.AddScoped<IUserStore<Kullanici>, KullaniciUserStore>();
builder.Services.AddScoped<IPasswordHasher<Kullanici>, KullaniciPasswordHasher>();
builder.Services.AddIdentityCore<Kullanici>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.User.RequireUniqueEmail = false;
})
    .AddUserStore<KullaniciUserStore>();
builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
var dataProtectionKeysRoot = new DirectoryInfo(AppStoragePaths.GetDataProtectionKeysRoot(builder.Environment.ContentRootPath));
dataProtectionKeysRoot.Create();
builder.Services.AddDataProtection()
    .SetApplicationName("MKFiloServis");
builder.Services.AddSingleton<IConfigureOptions<KeyManagementOptions>>(sp =>
    new ConfigureOptions<KeyManagementOptions>(options =>
    {
        options.XmlRepository = new FileSystemXmlRepository(dataProtectionKeysRoot, sp.GetRequiredService<ILoggerFactory>());
    }));
builder.Services.AddSingleton<IPortalProjectCatalogService, PortalProjectCatalogService>();
builder.Services.AddSingleton<ISecureFileService, SecureFileService>();
builder.Services.AddScoped<DosyaMigrasyonService>();
builder.Services.AddScoped<ArchiveMigrationService>();

// Distributed Cache - Redis veya Memory
var cacheProvider = builder.Configuration.GetValue<string>("Cache:Provider") ?? "Memory";
if (cacheProvider.Equals("Redis", StringComparison.OrdinalIgnoreCase))
{
    var redisConnection = builder.Configuration.GetValue<string>("Cache:Redis:ConnectionString") ?? "localhost:6379";
    var redisInstance = builder.Configuration.GetValue<string>("Cache:Redis:InstanceName") ?? "CRMFilo:";
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnection;
        options.InstanceName = redisInstance;
    });
}
else
{
    // Development/fallback için Memory Cache
    builder.Services.AddDistributedMemoryCache();
}
builder.Services.AddScoped<ICacheService, CacheService>();

// Multi-tenant Service (legacy ITenantService kaldirildi: Faz 5.3-B2)

// Application Services
// Aktif firma (tenant) state'i per-circuit tutulur; Blazor Server'da static state veri sizdiriyordu.
builder.Services.AddScoped<IAktifFirmaProvider, AktifFirmaProvider>();
builder.Services.AddScoped<IFirmaService, FirmaService>();
builder.Services.AddScoped<IFirmalarArasiTransferService, FirmalarArasiTransferService>();
builder.Services.AddScoped<IFirmaKopyalamaService, FirmaKopyalamaService>();
// Eski ILisansService kaldirildi — yerine LicenseService kullaniliyor (anti-bypass hardened)
builder.Services.AddScoped<IKullaniciService, KullaniciService>(); // Scoped - her circuit kendi oturumunu yonetir
builder.Services.AddScoped<ICariService, CariService>();
builder.Services.AddScoped<ISoforService, SoforService>();
builder.Services.AddScoped<IAracService, AracService>();
builder.Services.AddScoped<IGuzergahService, GuzergahService>();
builder.Services.AddScoped<IGuzergahSeferService, GuzergahSeferService>();
builder.Services.AddScoped<GuzergahSeferService>(); // GuzergahService constructor concrete ihtiyacı
builder.Services.AddScoped<IMasrafKalemiService, MasrafKalemiService>();
builder.Services.AddScoped<IKapasiteService, KapasiteService>();
builder.Services.AddScoped<IAracMasrafService, AracMasrafService>();
builder.Services.AddScoped<IServisCalismaService, ServisCalismaService>();
builder.Services.AddScoped<IFaturaService, FaturaService>();
builder.Services.AddScoped<IBankaHesapService, BankaHesapService>();
builder.Services.AddScoped<IBankaKasaHareketService, BankaKasaHareketService>();
builder.Services.AddScoped<IBankaHareketImportService, BankaHareketImportService>();
builder.Services.AddScoped<IOdemeEslestirmeService, OdemeEslestirmeService>();
builder.Services.AddScoped<IRaporService, RaporService>();
builder.Services.AddScoped<IExcelService, ExcelService>();
builder.Services.AddScoped<IMaliAnalizService, MaliAnalizService>();
builder.Services.AddScoped<IPersonelMaasIzinService, PersonelMaasIzinService>();
builder.Services.AddScoped<IMaasSnapshotService, MaasSnapshotService>();
builder.Services.AddScoped<MuhasebeSnapshotService>();
builder.Services.AddScoped<FinansDashboardService>();
builder.Services.AddScoped<BankaImportService>();
builder.Services.AddScoped<PuantajExcelService>();
builder.Services.AddScoped<IBelgeUyariService, BelgeUyariService>();
builder.Services.AddScoped<IDashboardGrafikService, DashboardGrafikService>();
builder.Services.AddScoped<IDataExportService, DataExportService>();
builder.Services.AddScoped<IGlobalSearchService, GlobalSearchService>();
builder.Services.AddScoped<IToastService, ToastService>();
builder.Services.AddScoped<AppIssueStateService>();
builder.Services.AddScoped<BrandingService>();
builder.Services.AddScoped<IPdfService, PdfService>();
builder.Services.AddScoped<IBudgetService, BudgetService>();
builder.Services.AddScoped<NumaraSerisiService>(); // Kural 15: Firma bazlı numara serisi

// OpenRouter AI Integration
builder.Services.AddHttpClient<IOpenRouterService, OpenRouterService>();

// Ollama AI — kaldırıldı, stub implementasyon kayıtlı
builder.Services.AddScoped<IOllamaService, OllamaService>();
builder.Services.AddScoped<IOllamaAIChatService, OllamaAIChatService>();

// DeepSeek AI Integration (DeepSeek V3)
builder.Services.AddHttpClient<IDeepSeekService, DeepSeekService>();

// Guvenlik: Master key (DPAPI) + AES-GCM dosya koruyucu
// Production'da key eksik/bozuk ise sessizce yeniden uretmez, kritik hata verir.
builder.Services.AddSingleton<IMasterKeyProvider>(sp =>
{
    var env = sp.GetRequiredService<IWebHostEnvironment>();
    var storageRoot = MKFiloServis.Web.Helpers.AppStoragePaths.GetDataProtectionKeysRoot(env.ContentRootPath);
    var keyPath = Path.Combine(storageRoot, "master.key");
    var logger = sp.GetRequiredService<ILogger<DpapiMasterKeyProvider>>();
    var isProduction = env.IsProduction();
    return new DpapiMasterKeyProvider(keyPath, logger, throwOnMissing: isProduction);
});
builder.Services.AddSingleton<IFileProtector, AesGcmFileProtector>();
builder.Services.AddSingleton<IDecryptionRecoveryTracker, InMemoryDecryptionRecoveryTracker>();
builder.Services.AddScoped<FileRecoveryService>(); // YENI: Eski anahtarla dosya kurtarma (recovery mode)
builder.Services.AddScoped<IEvrakArsivService, EvrakArsivService>();
builder.Services.AddScoped<IEvrakArsivBackfillService, EvrakArsivBackfillService>();
builder.Services.AddScoped<FileService>(); // YENI: Basit evrak dosya servisi (C:\MKFiloServis\uploads)
builder.Services.AddScoped<IEvrakDosyaMaintenanceService, EvrakDosyaMaintenanceService>(); // Kayip EvrakDosya bakim servisi
builder.Services.AddScoped<EvrakExpiryService>(); // YENI: EvrakDosya tabanli gecerlilik takibi
builder.Services.AddScoped<ITekrarlayanOdemeService, TekrarlayanOdemeService>(); // Kredi/Taksit Ynetimi
builder.Services.AddScoped<IBackupService, BackupService>();
builder.Services.AddScoped<IAktiviteLogService, AktiviteLogService>();
builder.Services.AddScoped<IDatabaseSettingsService, DatabaseSettingsService>();
builder.Services.AddScoped<IMuhasebeService, MuhasebeService>();
builder.Services.AddScoped<ISatisService, SatisService>();
builder.Services.AddSingleton<GuzergahDegisiklikUyariService>();
builder.Services.AddScoped<FirmaTransferService>();
builder.Services.AddScoped<IKurumService, KurumService>();
builder.Services.AddScoped<IPuantajService, PuantajService>();
builder.Services.AddScoped<IPuantajHakedisSyncService, PuantajHakedisSyncService>(); // Grid → Hakedis köprüsü
builder.Services.AddScoped<IFaturaGrupSablonuService, FaturaGrupSablonuService>(); // YENI: Agac gruplama şablonu CRUD
builder.Services.AddScoped<IPuantajIstisnaService, PuantajIstisnaService>(); // YENI: Puantaj istisna yönetimi
builder.Services.AddScoped<RebuildService>(); // RebuildAll motoru
builder.Services.AddScoped<DenetimService>(); // Finans denetim motoru
builder.Services.AddScoped<TestSessionService>(); // Güvenli test modu
builder.Services.AddScoped<MKFiloServis.Web.Services.Common.GenericUpdateService>(); // Ortak update pattern'i
builder.Services.AddHostedService<NightlyDenetimService>(); // Her gece 02:00 otomatik denetim
builder.Services.AddSingleton<MKFiloServis.Web.Services.AI.AnomalyDetectionService>(); // ML.NET offline AI
builder.Services.AddSingleton<MKFiloServis.Web.Services.AI.DecisionEngine>(); // AI karar motoru
builder.Services.AddSingleton<LicenseCache>(); // Singleton cache — static SSR (App.razor) dahil her yerden erişilebilir
builder.Services.AddScoped<LicenseService>(); // Lisans sistemi (Scoped — IDbContextFactory kullanır, cache'i günceller)
builder.Services.AddSignalR(); // Real-time (EvrakHub)
builder.Services.AddScoped<IHakedisRaporService, HakedisRaporService>(); // Hakediş Raporlama
builder.Services.AddScoped<IHakedisMuhasebeService, HakedisMuhasebeService>(); // Hakediş Muhasebe Entegrasyonu
builder.Services.AddScoped<OperasyonKaydiBusinessRules>();
builder.Services.AddScoped<MKFiloServis.Web.Services.Interfaces.IPuantajSyncService, PuantajSyncService>();
builder.Services.AddScoped<MKFiloServis.Web.Services.Interfaces.IDuplicateDetectionService, DuplicateDetectionService>();
builder.Services.AddScoped<MKFiloServis.Web.Services.Interfaces.IPreviewEngineService, PreviewEngineService>();
builder.Services.AddScoped<MKFiloServis.Web.Services.Interfaces.IPuantajEngineService, PuantajEngineService>();
builder.Services.AddScoped<MKFiloServis.Web.Services.Interfaces.IPuantajWorkflowService, PuantajWorkflowService>();
builder.Services.AddScoped<MKFiloServis.Web.Services.Interfaces.IPuantajFinansService, PuantajFinansService>();
builder.Services.AddScoped(typeof(MKFiloServis.Web.Services.Interfaces.IFiloKomisyonService), typeof(FiloKomisyonService));
builder.Services.AddScoped<IPiyasaKaynakService, PiyasaKaynakService>(); // Piyasa Kaynak Yonetimi (once kaydet)
builder.Services.AddScoped<IHttpScraperService, HttpScraperService>(); // HTTP Scraper (en hizli)
builder.Services.AddScoped<IPlaywrightScraperService, PlaywrightScraperService>(); // Playwright Web Scraper (yedek)
builder.Services.AddScoped<IMusteriKiralamaService, MusteriKiralamaService>();
builder.Services.AddScoped<ICRMService, CRMService>(); // CRM Servisi - Bildirim, Mesaj, Hatırlatıcı
builder.Services.AddScoped<MKFiloServis.Web.Services.Interfaces.IWhatsAppService, WhatsAppService>(); // WhatsApp Servisi
builder.Services.AddScoped<IStokService, StokService>(); // Stok/Envanter Servisi
builder.Services.AddScoped<IPersonelOzlukService, PersonelOzlukService>(); // Personel Özlük Evrak Servisi
builder.Services.AddScoped<IEbysService, EbysService>(); // EBYS Belge Merkezi Servisi
builder.Services.AddScoped<IPersonelFinansService, PersonelFinansService>(); // Personel Finans (Avans/Borç) Servisi
builder.Services.AddScoped<IBordroService, BordroService>(); // Bordro Servisi
builder.Services.AddScoped<IFiloOperasyonService, FiloOperasyonService>();
builder.Services.AddScoped<IKiralikPlakaTakipService, KiralikPlakaTakipService>();
 // Filo Operasyon (Komisyonculuk, Alım/Satım, Kiralık C Plaka Takip)
builder.Services.AddScoped<ITasimaTedarikciService, TasimaTedarikciService>(); // Personel Taşıma Tedarikçi (alt yüklenici) Modülü
builder.Services.AddScoped<IServisKontratService, ServisKontratService>(); // Servis Operasyon (Özmal/Kiralık/Tedarikçi Kontrat + Puantaj)
builder.Services.AddScoped<IIlanYayinService, IlanYayinService>(); // Araç İlan Yayın ve Kullanıcı Tercihleri
builder.Services.AddScoped<IHakedisService, HakedisService>(); // Hakedis/Puantaj Excel Import ve Takip
builder.Services.AddScoped<IOperasyonelHakedisService, OperasyonelHakedisService>(); // Yeni Hakediş entity tabanlı operasyonel hakediş üretimi (Faz 2)
builder.Services.AddScoped<IAracMaliyetService, AracMaliyetService>(); // Aylık araç maliyet snapshot servisi (Faz 2)
builder.Services.AddScoped<IProformaFaturaService, ProformaFaturaService>(); // Proforma Fatura Servisi
builder.Services.AddScoped<ICariHareketTakipService, CariHareketTakipService>(); // Cari Borç/Alacak Takip Servisi
builder.Services.AddScoped<UpdateService>(); // Güncelleme Yönetimi Servisi
builder.Services.AddScoped<IEmailService, EmailService>(); // E-posta Bildirim Servisi
builder.Services.AddScoped<ISystemHealthService, SystemHealthService>(); // Sistem Sağlık Kontrolü
builder.Services.AddScoped<DatabaseBackupService>(); // Quartz job tarafından tetiklenen veritabanı yedek servisi
builder.Services.AddScoped<BelgeUyariBackgroundService>(); // Quartz job tarafından tetiklenen belge uyarı servisi
builder.Services.AddHttpClient("Scraper"); // Scraper icin HttpClient
builder.Services.AddScoped<IIhaleHazirlikService, IhaleHazirlikService>();
builder.Services.AddScoped<ICariRiskService, CariRiskService>(); // Cari Risk Analizi Servisi
builder.Services.AddScoped<IKolayMuhasebeService, KolayMuhasebeService>(); // Kolay Muhasebe Girişi Servisi
builder.Services.AddScoped<ITopluFaturaService, TopluFaturaService>(); // Toplu Fatura Oluşturma Servisi
builder.Services.AddScoped<IEFaturaXmlService, EFaturaXmlService>(); // E-Fatura XML (GİB UBL-TR) Servisi
builder.Services.AddScoped<IIhaleTeklifVersiyonService, IhaleTeklifVersiyonService>(); // İhale teklif versiyonlama servisi
builder.Services.AddScoped<IIhaleTeklifKarsilastirmaService, IhaleTeklifKarsilastirmaService>(); // İhale teklif karşılaştırma servisi
builder.Services.AddScoped<IIhaleTeklifExportService, IhaleTeklifExportService>(); // İhale teklif export servisi
builder.Services.AddScoped<ILucaPortalService, LucaPortalService>(); // Luca Portal E-Fatura/E-Arşiv Entegrasyonu
builder.Services.AddScoped<ICariHatirlatmaService, CariHatirlatmaService>(); // Cari Otomatik Hatırlatma Servisi
builder.Services.AddScoped<BelgeUyariAyarlariService>(); // Belge uyarı ayarları (JSON) + önizleme sorguları
builder.Services.AddScoped<CariHatirlatmaBackgroundService>(); // Quartz job tarafından tetiklenen cari hatırlatma servisi
builder.Services.AddScoped<IFaturaSablonService, FaturaSablonService>(); // Fatura Şablon Yönetimi Servisi
builder.Services.AddScoped<IDestekTalebiService, DestekTalebiService>(); // Destek Talebi (Ticket) Servisi - osTicket benzeri
builder.Services.AddScoped<IEbysEvrakService, EbysEvrakService>(); // EBYS Gelen/Giden Evrak Servisi
builder.Services.AddScoped<IBelgeVersiyonService, BelgeVersiyonService>(); // EBYS Belge Versiyon Yönetimi Servisi
builder.Services.AddScoped<IEbysBelgeAramaService, EbysBelgeAramaService>(); // EBYS Gelişmiş Belge Arama Servisi
builder.Services.AddScoped<ISemanticSearchService, SemanticSearchService>(); // EBYS Semantik Arama Servisi
builder.Services.AddScoped<IBildirimService, BildirimService>(); // Bildirim Sistemi Servisi
builder.Services.AddScoped<ISmsService, SmsService>(); // SMS Gönderim Servisi
builder.Services.AddScoped<IWebhookService, WebhookService>(); // Webhook Sistemi Servisi
builder.Services.AddScoped<TestDataSeeder>(); // Test/Demo Veri Oluşturma Servisi
builder.Services.AddScoped<DemoDataService>(); // Demo Veri Yönetim Servisi (Reset/Seed/Remove)
builder.Services.AddScoped<IAracTakipService, AracTakipService>(); // Araç GPS Takip Servisi
builder.Services.AddScoped<IAracTakipBildirimService, AracTakipBildirimService>(); // SignalR Araç Takip Bildirim Servisi
builder.Services.AddScoped<IAuditLogService, AuditLogService>(); // Audit Log (Tüm İşlem Takibi) Servisi
builder.Services.AddSingleton<GpsSimulasyonService>(); // GPS Simülasyon Servisi (Singleton - state tutar)
builder.Services.AddHostedService(sp => sp.GetRequiredService<GpsSimulasyonService>()); // BackgroundService olarak çalıştır
builder.Services.AddSignalR(); // SignalR Hub'ları için
builder.Services.AddHttpClient("SMS"); // SMS provider'lar için HttpClient
builder.Services.AddHttpClient("Webhook"); // Webhook gönderimi için HttpClient
builder.Services.AddScoped<AutoBackupService>(); // Quartz job tarafından tetiklenen otomatik yedek servisi
builder.Services.AddScoped<GunlukOzetService>(); // Quartz job tarafından tetiklenen günlük WhatsApp özet servisi
builder.Services.AddScoped<IBakimPeriyotService, BakimPeriyotService>();
builder.Services.AddScoped<ILastikService, LastikService>();
builder.Services.AddScoped<ZamanliRaporService>(); // Zamanlanmış e-posta rapor servisi
builder.Services.AddScoped<IHoldingService, HoldingService>(); // Holding konsolidasyon servisi
builder.Services.AddScoped<LegacyDataTransferService>(); // Legacy DB veri aktarım servisi
builder.Services.AddScoped<IPuantajAnomaliService, PuantajAnomaliService>(); // AI Puantaj Anomali Tespiti
builder.Services.AddScoped<SharedLocalizer>(); // Çoklu dil desteği (i18n)

// Object Storage (Local veya S3-uyumlu)
var storageProvider = builder.Configuration.GetValue<string>("Storage:Provider") ?? "Local";
if (storageProvider.Equals("S3", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddScoped<IObjectStorageService, S3ObjectStorageService>();
    builder.Services.AddHttpClient("S3");
}
else
{
    builder.Services.AddScoped<IObjectStorageService, LocalObjectStorageService>();
}
builder.Services.AddScoped<ITeamsBildirimService, TeamsBildirimService>(); // Microsoft Teams Webhook Bildirimleri
builder.Services.AddScoped<ISlackBildirimService, SlackBildirimService>(); // Slack Webhook Bildirimleri
builder.Services.AddScoped<ILokalizasyonService, LokalizasyonService>(); // i18n Lokalizasyon
builder.Services.AddHttpClient("Teams"); // Teams webhook için
builder.Services.AddHttpClient("Slack");  // Slack webhook için
builder.Services.AddHttpContextAccessor();

var belgeUyariCheckIntervalHours = Math.Max(1, builder.Configuration.GetValue("BelgeUyari:CheckIntervalHours", 24));
var belgeUyariEmailEnabled = builder.Configuration.GetValue("BelgeUyari:EmailEnabled", false);

builder.Services.AddQuartz(q =>
{
    q.AddJob<AutoBackupJob>(opts => opts.WithIdentity("auto-backup-job"));
    q.AddTrigger(opts => opts
        .ForJob("auto-backup-job")
        .WithIdentity("auto-backup-trigger")
        .StartAt(DateBuilder.FutureDate(2, IntervalUnit.Minute))
        .WithSimpleSchedule(x => x.WithIntervalInMinutes(30).RepeatForever()));

    q.AddJob<CariHatirlatmaJob>(opts => opts.WithIdentity("cari-hatirlatma-job"));
    q.AddTrigger(opts => opts
        .ForJob("cari-hatirlatma-job")
        .WithIdentity("cari-hatirlatma-trigger")
        .StartAt(DateBuilder.FutureDate(3, IntervalUnit.Minute))
        .WithSimpleSchedule(x => x.WithIntervalInMinutes(30).RepeatForever()));

    q.AddJob<DatabaseBackupJob>(opts => opts.WithIdentity("database-backup-job"));
    q.AddTrigger(opts => opts
        .ForJob("database-backup-job")
        .WithIdentity("database-backup-trigger")
        .WithSchedule(CronScheduleBuilder.DailyAtHourAndMinute(3, 0)));

    if (belgeUyariEmailEnabled)
    {
        q.AddJob<BelgeUyariJob>(opts => opts.WithIdentity("belge-uyari-job"));
        q.AddTrigger(opts => opts
            .ForJob("belge-uyari-job")
            .WithIdentity("belge-uyari-trigger")
            .StartAt(DateBuilder.FutureDate(1, IntervalUnit.Minute))
            .WithSimpleSchedule(x => x.WithInterval(TimeSpan.FromHours(belgeUyariCheckIntervalHours)).RepeatForever()));
    }
    var gunlukOzetEnabled = builder.Configuration.GetValue("GunlukOzet:Enabled", false);
    if (gunlukOzetEnabled)
    {
        q.AddJob<GunlukOzetJob>(opts => opts.WithIdentity("gunluk-ozet-job"));
        var gunlukOzetSaat = builder.Configuration.GetValue("GunlukOzet:GonderimSaati", 8);
        q.AddTrigger(opts => opts
            .ForJob("gunluk-ozet-job")
            .WithIdentity("gunluk-ozet-trigger")
            .WithSchedule(CronScheduleBuilder.DailyAtHourAndMinute(gunlukOzetSaat, 0)));
    }
    var zamanliRaporEnabled = builder.Configuration.GetValue("ZamanlıRapor:Enabled",
        builder.Configuration.GetValue("ZamanliRapor:Enabled", false));
    if (zamanliRaporEnabled)
    {
        q.AddJob<ZamanliRaporJob>(opts => opts.WithIdentity("zamanli-rapor-job"));
        var raporSaat = builder.Configuration.GetValue("ZamanlıRapor:GonderimSaati",
            builder.Configuration.GetValue("ZamanliRapor:GonderimSaati", 7));
        q.AddTrigger(opts => opts
            .ForJob("zamanli-rapor-job")
            .WithIdentity("zamanli-rapor-trigger")
            .WithSchedule(CronScheduleBuilder.DailyAtHourAndMinute(raporSaat, 0)));
    }

    var bakimEnabled = builder.Configuration.GetValue("BakimPeriyot:Enabled", true);
    if (bakimEnabled)
    {
        q.AddJob<BakimPeriyotJob>(opts => opts.WithIdentity("bakim-periyot-job"));
        var bakimSaat = builder.Configuration.GetValue("BakimPeriyot:GunlukKontrolSaati", 9);
        q.AddTrigger(opts => opts
            .ForJob("bakim-periyot-job")
            .WithIdentity("bakim-periyot-trigger")
            .WithSchedule(CronScheduleBuilder.DailyAtHourAndMinute(bakimSaat, 30)));
    }

    // Holding veri toplama - her ayın 1'inde saat 02:00'de çalışır
    q.AddJob<HoldingVeriToplamaJob>(opts => opts.WithIdentity("holding-veri-toplama-job"));
    q.AddTrigger(opts => opts
        .ForJob("holding-veri-toplama-job")
        .WithIdentity("holding-veri-toplama-trigger")
        .WithSchedule(CronScheduleBuilder.MonthlyOnDayAndHourAndMinute(1, 2, 7)));

    // Luca Portal e-Fatura/e-Arsiv otomatik senkronizasyon - her gece 03:00
    q.AddJob<LucaPortalSenkronJob>(opts => opts.WithIdentity("luca-portal-senkron-job"));
    q.AddTrigger(opts => opts
        .ForJob("luca-portal-senkron-job")
        .WithIdentity("luca-portal-senkron-trigger")
        .WithSchedule(CronScheduleBuilder.DailyAtHourAndMinute(3, 0)));

    // Puantaj Anomali Tarama - her Pazartesi sabah 06:00
    q.AddJob<PuantajAnomaliJob>(opts => opts.WithIdentity("puantaj-anomali-job"));
    q.AddTrigger(opts => opts
        .ForJob("puantaj-anomali-job")
        .WithIdentity("puantaj-anomali-trigger")
        .WithSchedule(CronScheduleBuilder.WeeklyOnDayAndHourAndMinute(DayOfWeek.Monday, 6, 0)));

});
builder.Services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

// ── Health Checks ──────────────────────────────────────────────────
builder.Services.AddHealthChecks();

// API Controller ve JWT Authentication
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

// JWT Authentication - API için
var jwtSecret = builder.Configuration["Jwt:Secret"];
if (string.IsNullOrEmpty(jwtSecret) || jwtSecret.StartsWith("REPLACE_") || jwtSecret.Length < 32)
{
    throw new InvalidOperationException(
        "JWT Secret yapılandırılmamış veya geçersiz. " +
        "appsettings.Production.json → Jwt:Secret alanına en az 32 karakterli güçlü bir değer girin. " +
        "Örnek: openssl rand -base64 48");
}
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "MKFiloServis";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "MKFiloServis-API";
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "Cookies";
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ClockSkew = TimeSpan.Zero
    };
});

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "MKFiloServis API",
        Version = "v1",
        Description = "Kurumsal Filo Yönetimi & ERP Platformu — REST API dokümantasyonu.\n\n" +
                      "🚚 Araç, sürücü, muhasebe, bordro, EBYS, CRM ve ihale modülleri için uçtan uca endpoint'ler.\n\n" +
                      "🔐 Korumalı endpoint'leri çağırmak için önce `/api/auth/login` ile token alıp sağ üstteki **Authorize** butonu ile `Bearer {token}` formatında girin.",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Allbatros Global Teknoloji",
            Url = new Uri("https://github.com/karamur/MKFiloServis")
        },
        License = new Microsoft.OpenApi.Models.OpenApiLicense
        {
            Name = "Ticari Lisans",
            Url = new Uri("https://github.com/karamur/MKFiloServis/blob/main/LICENSE")
        }
    });

    // JWT Bearer auth UI
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "JWT Authorization header. Örnek: \"Authorization: Bearer {token}\""
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // XML yorum dosyalarını yükle (varsa)
    try
    {
        var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
        {
            options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
        }
    }
    catch
    {
        // XML dosyası eksikse Swagger açılmaya devam etsin
    }
});

// ── Çoklu Dil Desteği (i18n) ──────────────────────────────────────
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.AddControllersWithViews()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization();

var app = builder.Build();

var supportedCultures = new[] { "tr", "en" };
var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture(supportedCultures[0])
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);
localizationOptions.RequestCultureProviders.Insert(0,
    new Microsoft.AspNetCore.Localization.CookieRequestCultureProvider
    {
        CookieName = ".KOA.Culture",
        Options = localizationOptions
    });
app.UseRequestLocalization(localizationOptions);

// Culture query-string handler: ?culture=tr|en → cookie set → redirect
app.Use(async (context, next) =>
{
    var cultureQuery = context.Request.Query["culture"].FirstOrDefault();
    if (!string.IsNullOrEmpty(cultureQuery) &&
        supportedCultures.Contains(cultureQuery, StringComparer.OrdinalIgnoreCase))
    {
        context.Response.Cookies.Append(
            ".KOA.Culture",
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(cultureQuery)),
            new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), HttpOnly = false });
    }
    await next();
});

static async Task RunScopedSafeAsync(WebApplication app, string taskName, Func<IServiceProvider, Task> action)
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogInformation("Startup gorevi basliyor: {TaskName}", taskName);
        await action(scope.ServiceProvider);
        logger.LogInformation("Startup gorevi tamamlandi: {TaskName}", taskName);
    }
    catch (Exception ex)
    {
        // Nihai mimari: eksik tablolar/kolonlar idempotent helper'larla duzeltilir.
        // Kritik olmayan hatalarda uygulama devam eder.
        logger.LogWarning(ex, "Startup gorevi hatasi (kritik degil, devam): {TaskName}", taskName);
    }
}

// Master DB: olustur ve tablolari hazirla (raw SQL ile, EF migration cascade sorununu bypass eder)
await RunScopedSafeAsync(app, "MasterDatabase", async services =>
{
    var configuration = services.GetRequiredService<IConfiguration>();
    await DbInitializer.EnsureMasterDatabaseAsync(configuration);
});

// Otomatik schema sync: EF modelindeki TÜM eksik kolonları tespit edip ekler
// Not: DeletedAtColumnMigrationHelper kaldırıldı — SchemaSyncHelper tüm kolon + DeletedAt işlemlerini kapsar
await RunScopedSafeAsync(app, "SchemaSync", async services =>
{
    var context = services.GetRequiredService<ApplicationDbContext>();
    // 1) Model bazlı eksik kolon taraması
    await MKFiloServis.Web.Data.Migrations.SchemaSyncHelper.EnsureAllColumnsExistAsync(context);
    // 2) FisNoCounters: eski 2-kolon PK → 3-kolon unique index geçişi (42P10 hatasını önler)
    await MKFiloServis.Web.Data.Migrations.SchemaSyncHelper.EnsureFisNoCountersSchemaAsync(context);
    // Not: Pending migration'ları otomatik "uygulandı" işaretlemek bazı ortamlarda tablo oluşturmayı engelleyip
    // runtime'da relation does not exist hatalarına yol açabiliyor. Migration yönetimi DbInitializer tarafında yapılır.
});

// Legacy veri aktarımı (Talimat Bölüm 16-23): LegacySourceConnection (örn. eski kaynak DB) → MKFiloServis
await RunScopedSafeAsync(app, "LegacyDataTransfer", async services =>
{
    var transferService = services.GetRequiredService<LegacyDataTransferService>();
    await transferService.EnsureSchemaAsync();
    var result = await transferService.TransferAllAsync();
    var logger = services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Veri aktarimi tamamlandi: {Count} kayit aktarildi", result.TotalTransferred);
});

// Seed Database
await RunScopedSafeAsync(app, "DbInitializer", async services =>
{
    var context = services.GetRequiredService<ApplicationDbContext>();
    var configuration = services.GetRequiredService<IConfiguration>();
    await DbInitializer.InitializeAsync(context, configuration);
});

await RunScopedSafeAsync(app, "PersonelTableMigration", async services =>
{
    var context = services.GetRequiredService<ApplicationDbContext>();
    await MKFiloServis.Web.Data.Migrations.PersonelTableMigrationHelper.ApplyPersonelTableMigrationAsync(context);
});

await RunScopedSafeAsync(app, "PersonelMaasHesaplamaMigration", async services =>
{
    var context = services.GetRequiredService<ApplicationDbContext>();
    await MKFiloServis.Web.Data.Migrations.PersonelMaasHesaplamaMigrationHelper.ApplyPersonelMaasHesaplamaAsync(context);
});

await RunScopedSafeAsync(app, "SoforMaasMigration", async services =>
{
    var context = services.GetRequiredService<ApplicationDbContext>();
    await MKFiloServis.Web.Data.Migrations.SoforMaasMigrationHelper.ApplySoforMaasAlanlariAsync(context);
});

// PersonelPuantajlar ve GunlukPuantajlar tablolarini olustur
await RunScopedSafeAsync(app, "PersonelPuantajTableMigration", async services =>
{
    var context = services.GetRequiredService<ApplicationDbContext>();
    await MKFiloServis.Web.Data.Migrations.PersonelPuantajTableMigrationHelper.EnsurePersonelPuantajTablesAsync(context);
});

await RunScopedSafeAsync(app, "PersonelPuantajOnayMigration", async services =>
{
    var context = services.GetRequiredService<ApplicationDbContext>();
    await MKFiloServis.Web.Data.Migrations.PersonelPuantajOnayMigrationHelper.ApplyPersonelPuantajOnayAsync(context);
});

// BudgetOdemeler tablosuna KalanSonrakiDonemeAktarilsin kolonunu ekle
await RunScopedSafeAsync(app, "BudgetOdemeKalanMigration", async services =>
{
    var context = services.GetRequiredService<ApplicationDbContext>();
    await MKFiloServis.Web.Data.Migrations.BudgetOdemeKalanMigrationHelper.EnsureBudgetOdemeKalanColumnAsync(context);
});

await RunScopedSafeAsync(app, "BudgetHedefMigration", async services =>
{
    var context = services.GetRequiredService<ApplicationDbContext>();
    await MKFiloServis.Web.Data.Migrations.BudgetHedefMigrationHelper.EnsureBudgetHedefTableAsync(context);
});

await RunScopedSafeAsync(app, "FaturaGibDurumMigration", async services =>
{
    var context = services.GetRequiredService<ApplicationDbContext>();
    await MKFiloServis.Web.Data.Migrations.FaturaGibDurumMigrationHelper.ApplyFaturaGibDurumAsync(context);
});

await RunScopedSafeAsync(app, "TwoFactorMigration", async services =>
{
    var context = services.GetRequiredService<ApplicationDbContext>();
    await MKFiloServis.Web.Data.Migrations.TwoFactorMigrationHelper.ApplyTwoFactorColumnsAsync(context);
});

await RunScopedSafeAsync(app, "SmsMigration", async services =>
{
    var context = services.GetRequiredService<ApplicationDbContext>();
    var logger = services.GetRequiredService<ILogger<Program>>();
    await MKFiloServis.Web.Data.Migrations.SmsMigrationHelper.EnsureSmsTablesAsync(context, logger);
});

await RunScopedSafeAsync(app, "GuzergahKoordinatMigration", async services =>
{
    var context = services.GetRequiredService<ApplicationDbContext>();
    if (dbProvider == "PostgreSQL")
    {
        await MKFiloServis.Web.Data.Migrations.GuzergahKoordinatMigrationHelper.ApplyGuzergahKoordinatMigrationPostgresAsync(context);
    }
    else
    {
        await MKFiloServis.Web.Data.Migrations.GuzergahKoordinatMigrationHelper.ApplyGuzergahKoordinatMigrationAsync(context);
    }
});

await RunScopedSafeAsync(app, "DbSeeder", async services =>
{
    var context = services.GetRequiredService<ApplicationDbContext>();
    await DbSeeder.SeedAsync(context);
});

// Tenant Aşama C2: IFirmaTenant tablolarındaki NULL FirmaId değerlerini varsayılan firma ile doldur.
await RunScopedSafeAsync(app, "TenantC2_FirmaIdBackfill", async services =>
{
    var context = services.GetRequiredService<ApplicationDbContext>();
    var logger = services.GetRequiredService<ILogger<Program>>();
    await MKFiloServis.Web.Data.Migrations.TenantFirmaIdBackfillMigrationHelper.BackfillAsync(context, logger);
});

await RunScopedSafeAsync(app, "SeedAdmin", async services =>
{
    var kullaniciService = services.GetRequiredService<IKullaniciService>();
    await kullaniciService.SeedAdminAsync();
});

// LisansSeed kaldirildi — LicenseService.ValidateAsync() ilk cagrildiginda otomatik demo olusturur

await RunScopedSafeAsync(app, "MarkaModelSeed", async services =>
{
    var satisService = services.GetRequiredService<ISatisService>();
    await satisService.SeedMarkaModelAsync();
});

await RunScopedSafeAsync(app, "MuhasebeHesapPlaniSeed", async services =>
{
    var muhasebeService = services.GetRequiredService<IMuhasebeService>();
    await muhasebeService.SeedVarsayilanHesapPlaniAsync();
});

await RunScopedSafeAsync(app, "PiyasaKaynakSeed", async services =>
{
    var piyasaKaynakService = services.GetRequiredService<IPiyasaKaynakService>();
    await piyasaKaynakService.SeedDefaultKaynaklarAsync();
});

await RunScopedSafeAsync(app, "BudgetMasrafKalemleriSeed", async services =>
{
    var budgetService = services.GetRequiredService<IBudgetService>();
    await budgetService.SeedMasrafKalemleriAsync();
});

await RunScopedSafeAsync(app, "CariAlanGenisletmeMigration", async services =>
{
    var context = services.GetRequiredService<ApplicationDbContext>();
    await MKFiloServis.Web.Data.Migrations.CariMigrationHelper.ApplyCariAlanGenisletmeAsync(context);
});

await RunScopedSafeAsync(app, "BordroMigration", async services =>
{
    var context = services.GetRequiredService<ApplicationDbContext>();
    await MKFiloServis.Web.Data.Migrations.BordroMigrationHelper.ApplyBordroTablolariAsync(context);
});

await RunScopedSafeAsync(app, "PersonelFinansMigration", async services =>
{
    var context = services.GetRequiredService<ApplicationDbContext>();
    await MKFiloServis.Web.Data.Migrations.PersonelFinansMigrationHelper.ApplyPersonelFinansTablolariAsync(context);
});

await RunScopedSafeAsync(app, "AracMasrafMuhasebeMigration", async services =>
{
    var context = services.GetRequiredService<ApplicationDbContext>();
    await MKFiloServis.Web.Data.Migrations.AracMasrafMuhasebeMigrationHelper.ApplyAracMasrafMuhasebeAlanlariAsync(context);
});

await RunScopedSafeAsync(app, "OzlukEvrakMigration", async services =>
{
    var context = services.GetRequiredService<ApplicationDbContext>();
    await MKFiloServis.Web.Data.Migrations.OzlukEvrakMigrationHelper.ApplyOzlukEvrakMigrationAsync(context);
});

await RunScopedSafeAsync(app, "MuhasebeAyarMigration", async services =>
{
    var context = services.GetRequiredService<ApplicationDbContext>();
    await MKFiloServis.Web.Data.Migrations.MuhasebeAyarMigrationHelper.ApplyStokMasrafAyarlariAsync(context);
});

await RunScopedSafeAsync(app, "BankaHareketPersonelCebindenMigration", async services =>
{
    var context = services.GetRequiredService<ApplicationDbContext>();
    var logger = services.GetRequiredService<ILogger<Program>>();
    await MKFiloServis.Web.Data.Migrations.BankaHareketPersonelCebindenMigrationHelper.EnsurePersonelCebindenColumnsAsync(context, logger);
});

await RunScopedSafeAsync(app, "PersonelAvansHesapMigration", async services =>
{
    var context = services.GetRequiredService<ApplicationDbContext>();
    await MKFiloServis.Web.Data.Migrations.PersonelAvansHesapMigrationHelper.ApplyPersonelAvansHesaplariAsync(context);
});

await RunScopedSafeAsync(app, "PersonelBelgeTarihleriMigration", async services =>
{
    var context = services.GetRequiredService<ApplicationDbContext>();
    var logger = services.GetRequiredService<ILogger<Program>>();
    await MKFiloServis.Web.Data.Migrations.PersonelBelgeTarihleriMigrationHelper.ApplyPersonelBelgeTarihleriAsync(context, logger);
});

await RunScopedSafeAsync(app, "LastikSezonAyarMigration", async services =>
{
    var context = services.GetRequiredService<ApplicationDbContext>();
    await MKFiloServis.Web.Data.Migrations.LastikSezonAyarMigrationHelper.ApplyLastikSezonAyarAsync(context);
});

// Sprint S1: PuantajKayitlar tablosuna Slot, KurumId, IsverenFirmaId kolonlarini ekle
await RunScopedSafeAsync(app, "PuantajSlotMigration", async services =>
{
    var context = services.GetRequiredService<ApplicationDbContext>();
    var logger = services.GetRequiredService<ILogger<Program>>();
    await MKFiloServis.Web.Data.Migrations.PuantajSlotMigrationHelper.ApplyAsync(context, logger);
});

await RunScopedSafeAsync(app, "GuzergahSeferFirmaIdConstraint", async services =>
{
    var context = services.GetRequiredService<ApplicationDbContext>();
    var logger = services.GetRequiredService<ILogger<Program>>();
    await MKFiloServis.Web.Data.Migrations.GuzergahSeferFirmaIdConstraintHelper.ApplyAsync(context, logger);
});

await RunScopedSafeAsync(app, "KiralikPlakaFaturaMigration", async services =>
{
    var context = services.GetRequiredService<ApplicationDbContext>();
    var logger = services.GetRequiredService<ILogger<Program>>();
    await MKFiloServis.Web.Data.Migrations.KiralikPlakaFaturaMigrationHelper.ApplyAsync(context, logger);
});

await RunScopedSafeAsync(app, "KiralikPlakaTakipFaturaPlanMigration", async services =>
{
    var context = services.GetRequiredService<ApplicationDbContext>();
    var logger = services.GetRequiredService<ILogger<Program>>();
    await MKFiloServis.Web.Data.Migrations.KiralikPlakaTakipFaturaPlanMigrationHelper.ApplyAsync(context, logger);
});

await RunScopedSafeAsync(app, "SeedDefaultEvrakTanimlari", async services =>
{
    var ozlukService = services.GetRequiredService<IPersonelOzlukService>();
    await ozlukService.SeedDefaultEvrakTanimlariAsync();
});

// Nihai Mimari: Tenant DB oluşturma ve migration blokları kaldırıldı.
// Tüm firmalar tek MKFiloServis veritabanında çalışır.
// Migration helper'lar tek veritabanında ApplyMigrations ile uygulanır (aşağıya bakın).

// Migration helper'ları tek veritabanında uygula (idempotent, race-condition safe)
await RunScopedSafeAsync(app, "ApplyMigrations", async services =>
{
    var ctx = services.GetRequiredService<ApplicationDbContext>();
    var logger = services.GetRequiredService<ILogger<Program>>();

    if (databaseRuntime.IsSqlite)
        await MKFiloServis.Web.Data.Migrations.GuzergahKoordinatMigrationHelper.ApplyGuzergahKoordinatMigrationAsync(ctx);
    else
        await MKFiloServis.Web.Data.Migrations.GuzergahKoordinatMigrationHelper.ApplyGuzergahKoordinatMigrationPostgresAsync(ctx);
    await MKFiloServis.Web.Data.Migrations.PuantajSlotMigrationHelper.ApplyAsync(ctx, logger);
    await MKFiloServis.Web.Data.Migrations.KiralikPlakaFaturaMigrationHelper.ApplyAsync(ctx, logger);
    await MKFiloServis.Web.Data.Migrations.KiralikPlakaTakipFaturaPlanMigrationHelper.ApplyAsync(ctx, logger);
    await MKFiloServis.Web.Data.Migrations.GuzergahSeferFirmaIdConstraintHelper.ApplyAsync(ctx, logger);
    await MKFiloServis.Web.Data.Migrations.SyncPuantajSchemaMigrationHelper.ApplyAsync(ctx, logger);
    await MKFiloServis.Web.Data.Migrations.PuantajCarpaniMigrationHelper.ApplyAsync(ctx, logger);
    await MKFiloServis.Web.Data.Migrations.PuantajSyncMigrationHelper.ApplyAsync(ctx, logger);
    await MKFiloServis.Web.Data.Migrations.BudgetOdemeKalanMigrationHelper.EnsureBudgetOdemeKalanColumnAsync(ctx);
    await MKFiloServis.Web.Data.Migrations.BudgetHedefMigrationHelper.EnsureBudgetHedefTableAsync(ctx);

    // Aşağıdaki raw SQL blokları PostgreSQL'e özgü (DO $$ ... $$, ADD COLUMN IF NOT EXISTS)
    // SQLite için bu bloklar atlanır — EF migrations ve yukarıdaki helper'lar gerekli sütunları yönetir
    if (dbProvider == "PostgreSQL")
    {
    // Kural 14: AktiviteLog'a FirmaId + KullaniciId (idempotent)
    await ctx.Database.ExecuteSqlRawAsync(@"
        DO $$ BEGIN
            ALTER TABLE ""AktiviteLoglar"" ADD COLUMN IF NOT EXISTS ""FirmaId"" integer NULL;
            ALTER TABLE ""AktiviteLoglar"" ADD COLUMN IF NOT EXISTS ""KullaniciId"" integer NULL;
            CREATE INDEX IF NOT EXISTS ""IX_AktiviteLoglar_FirmaId"" ON ""AktiviteLoglar"" (""FirmaId"");
        EXCEPTION WHEN duplicate_column THEN END; $$;
    ");

    // Kural 4: AracMasraf + PersonelMaas FirmaId (idempotent DDL + backfill)
    await ctx.Database.ExecuteSqlRawAsync(@"
        DO $$ BEGIN
            ALTER TABLE ""AracMasraflari"" ADD COLUMN IF NOT EXISTS ""FirmaId"" integer NULL;
            ALTER TABLE ""PersonelMaaslari"" ADD COLUMN IF NOT EXISTS ""FirmaId"" integer NULL;
            CREATE INDEX IF NOT EXISTS ""IX_AracMasraflari_FirmaId"" ON ""AracMasraflari"" (""FirmaId"");
            CREATE INDEX IF NOT EXISTS ""IX_PersonelMaaslari_FirmaId"" ON ""PersonelMaaslari"" (""FirmaId"");
        EXCEPTION WHEN duplicate_column THEN END; $$;

        -- Backfill: AracMasraf.FirmaId ← Arac.FirmaId
        UPDATE ""AracMasraflari"" am
        SET ""FirmaId"" = a.""FirmaId""
        FROM ""Araclar"" a
        WHERE am.""AracId"" = a.""Id"" AND am.""FirmaId"" IS NULL AND a.""FirmaId"" IS NOT NULL;

        -- Backfill: PersonelMaas.FirmaId ← Sofor.FirmaId
        UPDATE ""PersonelMaaslari"" pm
        SET ""FirmaId"" = s.""FirmaId""
        FROM ""Personeller"" s
        WHERE pm.""SoforId"" = s.""Id"" AND pm.""FirmaId"" IS NULL AND s.""FirmaId"" IS NOT NULL;

        -- Kural 4: AracEvrak + AracEvrakDosya FirmaId (idempotent DDL + backfill)
        ALTER TABLE ""AracEvraklari"" ADD COLUMN IF NOT EXISTS ""FirmaId"" integer NULL;
        ALTER TABLE ""AracEvrakDosyalari"" ADD COLUMN IF NOT EXISTS ""FirmaId"" integer NULL;
        CREATE INDEX IF NOT EXISTS ""IX_AracEvraklari_FirmaId"" ON ""AracEvraklari"" (""FirmaId"");
        CREATE INDEX IF NOT EXISTS ""IX_AracEvrakDosyalari_FirmaId"" ON ""AracEvrakDosyalari"" (""FirmaId"");

        -- Backfill: AracEvrak.FirmaId ← Arac.FirmaId
        UPDATE ""AracEvraklari"" ae
        SET ""FirmaId"" = a.""FirmaId""
        FROM ""Araclar"" a
        WHERE ae.""AracId"" = a.""Id"" AND ae.""FirmaId"" IS NULL AND a.""FirmaId"" IS NOT NULL;

        -- Backfill: AracEvrakDosya.FirmaId ← AracEvrak.FirmaId
        UPDATE ""AracEvrakDosyalari"" aed
        SET ""FirmaId"" = ae.""FirmaId""
        FROM ""AracEvraklari"" ae
        WHERE aed.""AracEvrakId"" = ae.""Id"" AND aed.""FirmaId"" IS NULL AND ae.""FirmaId"" IS NOT NULL;

        -- Kural 4: PersonelIzin + PersonelIzinHakki FirmaId (idempotent DDL + backfill)
        ALTER TABLE ""PersonelIzinleri"" ADD COLUMN IF NOT EXISTS ""FirmaId"" integer NULL;
        ALTER TABLE ""PersonelIzinHaklari"" ADD COLUMN IF NOT EXISTS ""FirmaId"" integer NULL;
        CREATE INDEX IF NOT EXISTS ""IX_PersonelIzinleri_FirmaId"" ON ""PersonelIzinleri"" (""FirmaId"");
        CREATE INDEX IF NOT EXISTS ""IX_PersonelIzinHaklari_FirmaId"" ON ""PersonelIzinHaklari"" (""FirmaId"");

        -- Backfill: PersonelIzin.FirmaId ← Sofor.FirmaId
        UPDATE ""PersonelIzinleri"" pi
        SET ""FirmaId"" = s.""FirmaId""
        FROM ""Personeller"" s
        WHERE pi.""SoforId"" = s.""Id"" AND pi.""FirmaId"" IS NULL AND s.""FirmaId"" IS NOT NULL;

        -- Backfill: PersonelIzinHakki.FirmaId ← Sofor.FirmaId
        UPDATE ""PersonelIzinHaklari"" pih
        SET ""FirmaId"" = s.""FirmaId""
        FROM ""Personeller"" s
        WHERE pih.""SoforId"" = s.""Id"" AND pih.""FirmaId"" IS NULL AND s.""FirmaId"" IS NOT NULL;

        -- Kural 4: FaturaKalem FirmaId (idempotent DDL + backfill)
        ALTER TABLE ""FaturaKalemleri"" ADD COLUMN IF NOT EXISTS ""FirmaId"" integer NULL;
        CREATE INDEX IF NOT EXISTS ""IX_FaturaKalemleri_FirmaId"" ON ""FaturaKalemleri"" (""FirmaId"");

        -- Backfill: FaturaKalem.FirmaId ← Fatura.FirmaId
        UPDATE ""FaturaKalemleri"" fk
        SET ""FirmaId"" = f.""FirmaId""
        FROM ""Faturalar"" f
        WHERE fk.""FaturaId"" = f.""Id"" AND fk.""FirmaId"" IS NULL AND f.""FirmaId"" IS NOT NULL;

        -- Kural 4: ProformaFaturaKalem + AracBakimUyari + GunlukPuantaj FirmaId
        ALTER TABLE ""ProformaFaturaKalemler"" ADD COLUMN IF NOT EXISTS ""FirmaId"" integer NULL;
        ALTER TABLE ""AracBakimUyarilari"" ADD COLUMN IF NOT EXISTS ""FirmaId"" integer NULL;
        ALTER TABLE ""GunlukPuantajlar"" ADD COLUMN IF NOT EXISTS ""FirmaId"" integer NULL;
        CREATE INDEX IF NOT EXISTS ""IX_ProformaFaturaKalemler_FirmaId"" ON ""ProformaFaturaKalemler"" (""FirmaId"");
        CREATE INDEX IF NOT EXISTS ""IX_AracBakimUyarilari_FirmaId"" ON ""AracBakimUyarilari"" (""FirmaId"");
        CREATE INDEX IF NOT EXISTS ""IX_GunlukPuantajlar_FirmaId"" ON ""GunlukPuantajlar"" (""FirmaId"");

        -- Backfill: ProformaFaturaKalem ← ProformaFatura.FirmaId
        UPDATE ""ProformaFaturaKalemler"" pfk SET ""FirmaId"" = pf.""FirmaId""
        FROM ""ProformaFaturalar"" pf WHERE pfk.""ProformaFaturaId"" = pf.""Id"" AND pfk.""FirmaId"" IS NULL AND pf.""FirmaId"" IS NOT NULL;

        -- Backfill: AracBakimUyari ← Arac.FirmaId
        UPDATE ""AracBakimUyarilari"" abu SET ""FirmaId"" = a.""FirmaId""
        FROM ""Araclar"" a WHERE abu.""AracId"" = a.""Id"" AND abu.""FirmaId"" IS NULL AND a.""FirmaId"" IS NOT NULL;

        -- Backfill: GunlukPuantaj ← PersonelPuantaj.FirmaId
        UPDATE ""GunlukPuantajlar"" gp SET ""FirmaId"" = pp.""FirmaId""
        FROM ""PersonelPuantajlar"" pp WHERE gp.""PersonelPuantajId"" = pp.""Id"" AND gp.""FirmaId"" IS NULL AND pp.""FirmaId"" IS NOT NULL;

        -- Kural 4: AracIslem + ServisKaydi + ServisParca FirmaId
        ALTER TABLE ""AracIslemler"" ADD COLUMN IF NOT EXISTS ""FirmaId"" integer NULL;
        ALTER TABLE ""ServisKayitlari"" ADD COLUMN IF NOT EXISTS ""FirmaId"" integer NULL;
        ALTER TABLE ""ServisParcalar"" ADD COLUMN IF NOT EXISTS ""FirmaId"" integer NULL;
        CREATE INDEX IF NOT EXISTS ""IX_AracIslemler_FirmaId"" ON ""AracIslemler"" (""FirmaId"");
        CREATE INDEX IF NOT EXISTS ""IX_ServisKayitlari_FirmaId"" ON ""ServisKayitlari"" (""FirmaId"");
        CREATE INDEX IF NOT EXISTS ""IX_ServisParcalar_FirmaId"" ON ""ServisParcalar"" (""FirmaId"");

        UPDATE ""AracIslemler"" SET ""FirmaId"" = a.""FirmaId"" FROM ""Araclar"" a WHERE ""AracIslemler"".""AracId"" = a.""Id"" AND ""AracIslemler"".""FirmaId"" IS NULL AND a.""FirmaId"" IS NOT NULL;
        UPDATE ""ServisKayitlari"" SET ""FirmaId"" = a.""FirmaId"" FROM ""Araclar"" a WHERE ""ServisKayitlari"".""AracId"" = a.""Id"" AND ""ServisKayitlari"".""FirmaId"" IS NULL AND a.""FirmaId"" IS NOT NULL;
        UPDATE ""ServisParcalar"" sp SET ""FirmaId"" = sk.""FirmaId"" FROM ""ServisKayitlari"" sk WHERE sp.""ServisKaydiId"" = sk.""Id"" AND sp.""FirmaId"" IS NULL AND sk.""FirmaId"" IS NOT NULL;

        -- Kural 4: AylikChecklist + ChecklistKalem FirmaId
        ALTER TABLE ""AylikChecklistler"" ADD COLUMN IF NOT EXISTS ""FirmaId"" integer NULL;
        ALTER TABLE ""ChecklistKalemleri"" ADD COLUMN IF NOT EXISTS ""FirmaId"" integer NULL;
        CREATE INDEX IF NOT EXISTS ""IX_AylikChecklistler_FirmaId"" ON ""AylikChecklistler"" (""FirmaId"");
        CREATE INDEX IF NOT EXISTS ""IX_ChecklistKalemleri_FirmaId"" ON ""ChecklistKalemleri"" (""FirmaId"");

        UPDATE ""AylikChecklistler"" ac SET ""FirmaId"" = COALESCE(a.""FirmaId"", s.""FirmaId"", g.""FirmaId"")
        FROM ""AylikChecklistler"" ac2
        LEFT JOIN ""Araclar"" a ON ac2.""AracId"" = a.""Id""
        LEFT JOIN ""Personeller"" s ON ac2.""SoforId"" = s.""Id""
        LEFT JOIN ""Guzergahlar"" g ON ac2.""GuzergahId"" = g.""Id""
        WHERE ac.""Id"" = ac2.""Id"" AND ac.""FirmaId"" IS NULL;

        UPDATE ""ChecklistKalemleri"" ck SET ""FirmaId"" = ac.""FirmaId""
        FROM ""AylikChecklistler"" ac WHERE ck.""AylikChecklistId"" = ac.""Id"" AND ck.""FirmaId"" IS NULL AND ac.""FirmaId"" IS NOT NULL;
    ");

    } // if (dbProvider == "PostgreSQL")

    // Kural 15: FisNoCounters schema sync → SchemaSyncHelper.EnsureFisNoCountersSchemaAsync (yukarıda çağrıldı)

    logger.LogInformation("Migration helper'lar tek veritabaninda uygulandi.");
});

// Nihai Mimari: HoldingVeri ve HoldingRapor ApplicationDbContext'te (Kural 13).
// Ayrı HoldingDbContext kaldırıldı — EnsureCreated gerekmez (ana DB zaten var).
// Holding verisi: ilk kez calisiyorsa (bos tablo) otomatik doldur
await RunScopedSafeAsync(app, "EnsureHoldingInitialData", async services =>
{
    var appFactory = services.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
    using var ctx = await appFactory.CreateDbContextAsync();
    var hasData = await ctx.HoldingVeriler.AnyAsync();
    if (!hasData)
    {
        var holdingService = services.GetRequiredService<IHoldingService>();
        var logger = services.GetRequiredService<ILogger<IHoldingService>>();
        var now = DateTime.UtcNow;
        logger.LogInformation("Holding verisi bos, ilk veri toplama baslatiliyor: {Yil}-{Ay}", now.Year, now.Month);
        await holdingService.ToplaVeKaydetAsync(now.Year, now.Month);
        logger.LogInformation("Ilk holding veri toplama tamamlandi");
    }
});

// PostgreSQL sequence desync düzeltmesi: raw SQL veya explicit Id ile eklenen kayıtlardan sonra
// sequence'lar MAX(Id)'nin gerisinde kalabilir. Tüm SERIAL kolonları MAX(Id)'ye resetle.
if (dbProvider == "PostgreSQL")
{
    await RunScopedSafeAsync(app, "FixPostgresSequences", async services =>
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var logger = services.GetRequiredService<ILogger<Program>>();
        var tables = new[]
        {
            "Cariler", "Firmalar", "Personeller", "Araclar", "Kurumlar", "Guzergahlar",
            "Roller", "Kullanicilar", "MuhasebeHesaplari", "BankaHesaplari",
            "PuantajKayitlar", "PuantajHesapDonemleri", "FiloGuzergahEslestirmeleri",
            "GuzergahSeferleri", "AracMasraflar", "Faturalar", "CariHareketler"
        };

        int fixed_ = 0;
        foreach (var table in tables)
        {
            try
            {
                var sql = $"""
                    DO $$
                    DECLARE seq_name text;
                    BEGIN
                        IF to_regclass('"{table}"') IS NOT NULL THEN
                            SELECT pg_get_serial_sequence('"{table}"', 'Id') INTO seq_name;
                            IF seq_name IS NOT NULL THEN
                                EXECUTE format(
                                    'SELECT setval(%L, COALESCE((SELECT MAX("Id") FROM "{table}"), 0) + 1, false)',
                                    seq_name
                                );
                            END IF;
                        END IF;
                    EXCEPTION
                        WHEN undefined_table OR undefined_column THEN
                            NULL;
                    END $$;
                    """;
                await context.Database.ExecuteSqlRawAsync(sql);
                fixed_++;
            }
            catch
            {
                // Tablo yoksa veya serial sequence yoksa sessizce geç
            }
        }
        logger.LogInformation("PostgreSQL sequence reset tamamlandi: {Count} tablo.", fixed_);
    });
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

// Health check endpoints
app.MapHealthChecks("/healthz", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/readyz", new HealthCheckOptions { Predicate = _ => true });

// Swagger UI - tüm ortamlarda aktif (API dokümantasyonu)
app.UseSwagger(c =>
{
    c.RouteTemplate = "swagger/{documentName}/swagger.json";
});
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "MKFiloServis API v1");
    c.RoutePrefix = "swagger";
    c.DocumentTitle = "MKFiloServis API Dokümantasyonu";
    c.DefaultModelsExpandDepth(-1); // Models bölümünü kapalı başlat
    c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
    c.EnableDeepLinking();
    c.DisplayRequestDuration();
    c.EnableFilter();
    c.EnableTryItOutByDefault();
});

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

// HTTPS yonlendirme - sadece HTTPS portu aktifse calistir
// Ag uzerinden HTTP ile erisme sorun cikarmasin diye kontrol eklendi
var httpsPort = app.Configuration.GetValue<int?>("HTTPS_PORT") ?? 
                (app.Urls.Any(u => u.StartsWith("https")) ? 7113 : (int?)null);
if (httpsPort.HasValue)
{
    app.UseHttpsRedirection();
}

app.UseAntiforgery();

// IP Güvenlik Middleware (beyaz/kara liste)
app.UseMiddleware<MKFiloServis.Web.Middleware.IpGuvenlikMiddleware>();

// Global exception logging — tüm yakalanmayan hataları AppErrorLog'a kaydeder
app.UseMiddleware<MKFiloServis.Web.Middleware.ErrorLoggingMiddleware>();

// Authentication & Authorization - API için
app.UseAuthentication();
app.UseAuthorization();

// Uploads klasörünü oluştur (SecureFileService kullanır)
var externalUploadsPath = AppStoragePaths.GetUploadsRoot(app.Environment.ContentRootPath);
Directory.CreateDirectory(externalUploadsPath);

// NOT: /uploads static files middleware KALDIRILDI — dosyalar sadece
// SecureFileService üzerinden Blazor auth devresinde erişilebilir.
// Doğrudan URL erişimi yasak (Kural 16: Dosya Güvenliği).

// Setup otomasyon: publish klasorundeki license.auto.key varsa ilk acilista otomatik uygula.
using (var scope = app.Services.CreateScope())
{
    var licSvc = scope.ServiceProvider.GetRequiredService<LicenseService>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        var autoLicensePath = Path.Combine(app.Environment.ContentRootPath, "license.auto.key");
        if (File.Exists(autoLicensePath))
        {
            var autoKey = await File.ReadAllTextAsync(autoLicensePath);
            if (!string.IsNullOrWhiteSpace(autoKey))
            {
                await licSvc.ActivateFromKeyAsync(autoKey.Trim());
                File.Delete(autoLicensePath);
                logger.LogInformation("✅ Auto lisans aktivasyonu tamamlandi: {Path}", autoLicensePath);
            }
            else
            {
                logger.LogWarning("⚠️ Auto lisans dosyasi bos: {Path}", autoLicensePath);
            }
        }
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "⚠️ Auto lisans aktivasyonu basarisiz oldu.");
    }
}

// ══════════════════════════════════════════════
// LISANS KONTROL — DEMO MODE (block YOK)
// Lisans yoksa uygulama AÇILIR, demo modda çalışır.
// Kullanıcı içeriden lisans yükleyince FULL MODE'a geçer.
// ══════════════════════════════════════════════
using (var scope = app.Services.CreateScope())
{
    var licSvc = scope.ServiceProvider.GetRequiredService<LicenseService>();
    var licCache = scope.ServiceProvider.GetRequiredService<LicenseCache>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    var overrideKey = config["DevSettings:OverrideKey"];

    // Her uygulama başlangıcında cache temizlenir ve lisans DB üzerinden yeniden doğrulanır.
    licCache.Clear();

    try
    {
        var val = await licSvc.ValidateAsync(overrideKey);
        if (!val.IsValid)
        {
            MKFiloServis.Shared.AppMode.EnterDemoMode(val.Message);
            logger.LogWarning("🔶 DEMO MODE: {Reason}", val.Message);
        }
        else
        {
            MKFiloServis.Shared.AppMode.ExitDemoMode();
            logger.LogInformation("✅ FULL MODE: Lisans geçerli (DB doğrulandı).");
        }
    }
    catch (Exception ex)
    {
        MKFiloServis.Shared.AppMode.EnterDemoMode($"Lisans kontrol hatası: {ex.Message}");
        logger.LogWarning(ex, "🔶 DEMO MODE (hata): {Reason}", ex.Message);
    }
}

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
app.MapControllers(); // API Controller'larini haritalandir
app.MapHub<AracTakipHub>("/hubs/aractakip"); // SignalR Araç Takip Hub'ı
app.MapHub<MKFiloServis.Web.Hubs.EvrakHub>("/hubs/evrak"); // SignalR Evrak Hub'ı

// Admin: Evrak arşiv backfill endpoint'i (sadece Development'da aktif)
if (app.Environment.IsDevelopment())
{
    app.MapGet("/admin/evrak-arsiv-backfill/dry-run", async (
        IEvrakArsivBackfillService backfillService,
        CancellationToken ct) =>
    {
        var rapor = await backfillService.DryRunAsync(ct);
        return Results.Ok(rapor);
    }).WithTags("Admin");

    app.MapPost("/admin/evrak-arsiv-backfill/execute", async (
        IEvrakArsivBackfillService backfillService,
        bool updateDatabase,
        bool overwriteExisting,
        CancellationToken ct) =>
    {
        var rapor = await backfillService.ExecuteAsync(updateDatabase, overwriteExisting, ct);
        return Results.Ok(rapor);
    }).WithTags("Admin");
}

app.Run();






