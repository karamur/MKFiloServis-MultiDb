using KOAFiloServis.Web.Components;
using KOAFiloServis.Web.Data;
using KOAFiloServis.Web.Helpers;
using KOAFiloServis.Web.Jobs;
using KOAFiloServis.Web.Services;
using KOAFiloServis.Web.Services.Interfaces;
using KOAFiloServis.Web.Services.Security;
using KOAFiloServis.Web.Hubs;
using KOAFiloServis.Shared.Entities;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.FileProviders;
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
using Microsoft.Extensions.Diagnostics.HealthChecks;
using KOAFiloServis.Web.HealthChecks;

// EPPlus lisans ayari (NonCommercial kullanim icin)
ExcelPackage.License.SetNonCommercialPersonal("KOAFiloServis");

var builder = WebApplication.CreateBuilder(args);

// Npgsql EnableLegacyTimestampBehavior: herhangi bir UseNpgsql() cagrisindan ONCE set edilmeli.
// Npgsql static constructor bu switch'i ilk kez UseNpgsql()'de okur ve cache'ler.
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

// Database Provider Secimi (dbsettings.json varsa onu oncele)
var dbProvider = builder.Configuration.GetValue<string>("DatabaseProvider") ?? "PostgreSQL";
var defaultConnectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
var dbSettingsPath = Path.Combine(builder.Environment.ContentRootPath, "dbsettings.json");

if (File.Exists(dbSettingsPath))
{
    try
    {
        var dbSettingsJson = await File.ReadAllTextAsync(dbSettingsPath);
        var dbSettings = JsonSerializer.Deserialize<DatabaseSettings>(dbSettingsJson);
        if (dbSettings != null)
        {
            dbProvider = dbSettings.Provider switch
            {
                DatabaseProvider.PostgreSQL => "PostgreSQL",
                DatabaseProvider.MySQL => "MySQL",
                DatabaseProvider.SQLServer => "SQLServer",
                _ => "SQLite"
            };
            defaultConnectionString = dbSettings.GetConnectionString();
        }
    }
    catch
    {
        // dbsettings.json okunamazsa appsettings ile devam et
    }
}

// URL baglama: IIS tarafi zaten adres/port yonetir; burada zorlama yapma.
// Sadece yerel calistirmada (Development) ve explicit URL verilmediyse otomatik port sec.
var hasExplicitUrls = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASPNETCORE_URLS"))
    || args.Any(a => a.StartsWith("--urls", StringComparison.OrdinalIgnoreCase));
var isIisHosted = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASPNETCORE_IIS_PHYSICAL_PATH"))
    || !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASPNETCORE_PORT"));

if (!hasExplicitUrls && !isIisHosted && builder.Environment.IsDevelopment())
{
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

    var baslangicPortu = 5190;
    var secilenPort = baslangicPortu;

    while (secilenPort < baslangicPortu + 10 && !PortKullanilabilirMi(secilenPort))
    {
        secilenPort++;
    }

    builder.WebHost.UseUrls($"http://0.0.0.0:{secilenPort}");
}

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSingleton<AktiviteLogInterceptor>();
builder.Services.AddSingleton<ICurrentUserAccessor, CurrentUserAccessor>();

// Database - Tek PostgreSQL veritabanı (Nihai Mimari 2026)
builder.Services.AddPooledDbContextFactory<ApplicationDbContext>((sp, options) =>
{
    var enableSensitiveDataLogging = builder.Environment.IsDevelopment() &&
        builder.Configuration.GetValue<bool>("EntityFramework:EnableSensitiveDataLogging");

    if (dbProvider == "PostgreSQL")
    {
        options.UseNpgsql(defaultConnectionString, npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorCodesToAdd: null);
            npgsqlOptions.CommandTimeout(30);
            npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "public");
        });
    }
    else // SQLite
    {
        options.UseSqlite(defaultConnectionString);
    }

    if (enableSensitiveDataLogging)
    {
        options.EnableSensitiveDataLogging();
    }

    // Pending migration ve model validation query-filter etkileşim uyarılarını devre dışı bırak
    options.ConfigureWarnings(w =>
    {
        w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning);
        w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning);
    });
    options.AddInterceptors(sp.GetRequiredService<AktiviteLogInterceptor>());
});

// Scoped ApplicationDbContext — her Blazor circuit için bir context,
// IAktifFirmaProvider üzerinden firma izolasyonu sağlanır (Kural 6, Kural 7).
builder.Services.AddScoped<ApplicationDbContext>(sp =>
{
    var factory = sp.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
    var ctx = factory.CreateDbContext();
    ctx.SetServiceProvider(sp); // Scoped provider → IAktifFirmaProvider + Global Query Filter
    return ctx;
});

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
    .SetApplicationName("KOAFiloServis");
builder.Services.AddSingleton<IConfigureOptions<KeyManagementOptions>>(sp =>
    new ConfigureOptions<KeyManagementOptions>(options =>
    {
        options.XmlRepository = new FileSystemXmlRepository(dataProtectionKeysRoot, sp.GetRequiredService<ILoggerFactory>());
    }));
builder.Services.AddSingleton<IPortalProjectCatalogService, PortalProjectCatalogService>();
builder.Services.AddSingleton<ISecureFileService, SecureFileService>();
builder.Services.AddScoped<DosyaMigrasyonService>();

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
builder.Services.AddSingleton<ILisansService, LisansService>(); // Singleton - lisans cache
builder.Services.AddScoped<IKullaniciService, KullaniciService>(); // Scoped - her circuit kendi oturumunu yonetir
builder.Services.AddScoped<ICariService, CariService>();
builder.Services.AddScoped<ISoforService, SoforService>();
builder.Services.AddScoped<IAracService, AracService>();
builder.Services.AddScoped<IGuzergahService, GuzergahService>();
builder.Services.AddScoped<IGuzergahSeferService, GuzergahSeferService>();
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
builder.Services.AddScoped<IFaturaHazirlikService, FaturaHazirlikService>();
builder.Services.AddScoped<IMaliAnalizService, MaliAnalizService>();
builder.Services.AddScoped<IPersonelMaasIzinService, PersonelMaasIzinService>();
builder.Services.AddScoped<IBelgeUyariService, BelgeUyariService>();
builder.Services.AddScoped<IDashboardGrafikService, DashboardGrafikService>();
builder.Services.AddScoped<IDataExportService, DataExportService>();
builder.Services.AddScoped<IGlobalSearchService, GlobalSearchService>();
builder.Services.AddScoped<IToastService, ToastService>();
builder.Services.AddScoped<AppIssueStateService>();
builder.Services.AddScoped<IPdfService, PdfService>();
builder.Services.AddScoped<IBudgetService, BudgetService>();
builder.Services.AddScoped<NumaraSerisiService>(); // Kural 15: Firma bazlı numara serisi

// OpenRouter AI Integration
builder.Services.AddHttpClient<IOpenRouterService, OpenRouterService>();

// Ollama AI Integration (yerel LLM / embedding)
builder.Services.AddHttpClient("Ollama");
builder.Services.AddScoped<IOllamaService, OllamaService>();

// DeepSeek AI Integration (DeepSeek V3)
builder.Services.AddHttpClient<IDeepSeekService, DeepSeekService>();

// Guvenlik: Master key (DPAPI) + AES-GCM dosya koruyucu
builder.Services.AddSingleton<IMasterKeyProvider>(sp =>
{
    var env = sp.GetRequiredService<IWebHostEnvironment>();
    var storageRoot = KOAFiloServis.Web.Helpers.AppStoragePaths.GetDataProtectionKeysRoot(env.ContentRootPath);
    var keyPath = Path.Combine(storageRoot, "master.key");
    var logger = sp.GetRequiredService<ILogger<DpapiMasterKeyProvider>>();
    return new DpapiMasterKeyProvider(keyPath, logger);
});
builder.Services.AddSingleton<IFileProtector, AesGcmFileProtector>();
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
builder.Services.AddScoped<IKurumPuantajService, KurumPuantajService>();
builder.Services.AddScoped<OperasyonKaydiBusinessRules>();
builder.Services.AddScoped<KOAFiloServis.Web.Services.Interfaces.IOperasyonKaydiService, OperasyonKaydiService>();
builder.Services.AddScoped<KOAFiloServis.Web.Services.Interfaces.IPuantajSyncService, PuantajSyncService>();
builder.Services.AddScoped<KOAFiloServis.Web.Services.Interfaces.IDuplicateDetectionService, DuplicateDetectionService>();
builder.Services.AddScoped<KOAFiloServis.Web.Services.Interfaces.IPreviewEngineService, PreviewEngineService>();
builder.Services.AddScoped<KOAFiloServis.Web.Services.Interfaces.IPuantajEngineService, PuantajEngineService>();
builder.Services.AddScoped<KOAFiloServis.Web.Services.Interfaces.IPuantajWorkflowService, PuantajWorkflowService>();
builder.Services.AddScoped<KOAFiloServis.Web.Services.Interfaces.IPuantajFinansService, PuantajFinansService>();
builder.Services.AddScoped(typeof(KOAFiloServis.Web.Services.Interfaces.IFiloKomisyonService), typeof(FiloKomisyonService));
builder.Services.AddScoped<KOAFiloServis.Web.Services.Interfaces.IPuantajEslestirmeService, PuantajEslestirmeService>();
// Sprint 8: Puantaj engine automation
builder.Services.Configure<PuantajJobOptions>(
    builder.Configuration.GetSection(PuantajJobOptions.Section));
builder.Services.AddSingleton<IPuantajRetryPolicy, PuantajRetryPolicy>();
builder.Services.AddScoped<IPuantajJobService, PuantajJobService>();
builder.Services.AddScoped<IPuantajMutexService, PuantajMutexService>();
builder.Services.AddScoped<IPuantajReconciliationService, PuantajReconciliationService>();
builder.Services.AddScoped<IPiyasaKaynakService, PiyasaKaynakService>(); // Piyasa Kaynak Yonetimi (once kaydet)
builder.Services.AddScoped<IHttpScraperService, HttpScraperService>(); // HTTP Scraper (en hizli)
builder.Services.AddScoped<IPlaywrightScraperService, PlaywrightScraperService>(); // Playwright Web Scraper (yedek)
builder.Services.AddScoped<IMusteriKiralamaService, MusteriKiralamaService>();
builder.Services.AddScoped<ICRMService, CRMService>(); // CRM Servisi - Bildirim, Mesaj, Hatırlatıcı
builder.Services.AddScoped<KOAFiloServis.Web.Services.Interfaces.IWhatsAppService, WhatsAppService>(); // WhatsApp Servisi
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
builder.Services.AddScoped<IBildirimService, BildirimService>(); // Bildirim Sistemi Servisi
builder.Services.AddScoped<ISmsService, SmsService>(); // SMS Gönderim Servisi
builder.Services.AddScoped<IWebhookService, WebhookService>(); // Webhook Sistemi Servisi
builder.Services.AddScoped<TestDataSeeder>(); // Test/Demo Veri Oluşturma Servisi
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

    // Puantaj Engine - her ayın 1'inde saat 00:30'da geçen ayı otomatik hesaplar
    var puantajAutoEnabled = builder.Configuration.GetValue("PuantajEngine:AutoProcess:Enabled", true);
    if (puantajAutoEnabled)
    {
        q.AddJob<PuantajEngineJob>(opts => opts.WithIdentity("puantaj-engine-job"));
        q.AddTrigger(opts => opts
            .ForJob("puantaj-engine-job")
            .WithIdentity("puantaj-engine-trigger")
            .WithSchedule(CronScheduleBuilder.CronSchedule("0 30 0 1 * ?")
                .WithMisfireHandlingInstructionDoNothing()));
    }

    // Puantaj Reconciliation — her gün 04:00'da tutarsızlıkları tamir eder
    q.AddJob<PuantajReconciliationJob>(opts => opts.WithIdentity("puantaj-reconciliation-job"));
    q.AddTrigger(opts => opts
        .ForJob("puantaj-reconciliation-job")
        .WithIdentity("puantaj-reconciliation-trigger")
        .WithSchedule(CronScheduleBuilder.DailyAtHourAndMinute(4, 0)));
});
builder.Services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

// ── Health Checks ──────────────────────────────────────────────────
builder.Services.AddHealthChecks()
    .AddCheck<PuantajJobHealthCheck>("puantaj_job", tags: ["job"]);

// API Controller ve JWT Authentication
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

// JWT Authentication - API için
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "KOAFiloServis-Super-Secret-Key-2025-Minimum-32-Chars";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "KOAFiloServis";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "KOAFiloServis-API";
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
        Title = "KOAFiloServis API",
        Version = "v1",
        Description = "Kurumsal Filo Yönetimi & ERP Platformu — REST API dokümantasyonu.\n\n" +
                      "🚚 Araç, sürücü, muhasebe, bordro, EBYS, CRM ve ihale modülleri için uçtan uca endpoint'ler.\n\n" +
                      "🔐 Korumalı endpoint'leri çağırmak için önce `/api/auth/login` ile token alıp sağ üstteki **Authorize** butonu ile `Bearer {token}` formatında girin.",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Allbatros Global Teknoloji",
            Url = new Uri("https://github.com/karamur/KOAFiloServis")
        },
        License = new Microsoft.OpenApi.Models.OpenApiLicense
        {
            Name = "Ticari Lisans",
            Url = new Uri("https://github.com/karamur/KOAFiloServis/blob/main/LICENSE")
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

var app = builder.Build();

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
    await KOAFiloServis.Web.Data.Migrations.PersonelTableMigrationHelper.ApplyPersonelTableMigrationAsync(context);
});

await RunScopedSafeAsync(app, "PersonelMaasHesaplamaMigration", async services =>
{
    var context = services.GetRequiredService<ApplicationDbContext>();
    await KOAFiloServis.Web.Data.Migrations.PersonelMaasHesaplamaMigrationHelper.ApplyPersonelMaasHesaplamaAsync(context);
});

await RunScopedSafeAsync(app, "SoforMaasMigration", async services =>
{
    var context = services.GetRequiredService<ApplicationDbContext>();
    await KOAFiloServis.Web.Data.Migrations.SoforMaasMigrationHelper.ApplySoforMaasAlanlariAsync(context);
});

// PersonelPuantajlar ve GunlukPuantajlar tablolarini olustur
await RunScopedSafeAsync(app, "PersonelPuantajTableMigration", async services =>
{
    var context = services.GetRequiredService<ApplicationDbContext>();
    await KOAFiloServis.Web.Data.Migrations.PersonelPuantajTableMigrationHelper.EnsurePersonelPuantajTablesAsync(context);
});

await RunScopedSafeAsync(app, "PersonelPuantajOnayMigration", async services =>
{
    var context = services.GetRequiredService<ApplicationDbContext>();
    await KOAFiloServis.Web.Data.Migrations.PersonelPuantajOnayMigrationHelper.ApplyPersonelPuantajOnayAsync(context);
});

// BudgetOdemeler tablosuna KalanSonrakiDonemeAktarilsin kolonunu ekle
await RunScopedSafeAsync(app, "BudgetOdemeKalanMigration", async services =>
{
    var context = services.GetRequiredService<ApplicationDbContext>();
    await KOAFiloServis.Web.Data.Migrations.BudgetOdemeKalanMigrationHelper.EnsureBudgetOdemeKalanColumnAsync(context);
});

await RunScopedSafeAsync(app, "BudgetHedefMigration", async services =>
{
    var context = services.GetRequiredService<ApplicationDbContext>();
    await KOAFiloServis.Web.Data.Migrations.BudgetHedefMigrationHelper.EnsureBudgetHedefTableAsync(context);
});

await RunScopedSafeAsync(app, "FaturaGibDurumMigration", async services =>
{
    var context = services.GetRequiredService<ApplicationDbContext>();
    await KOAFiloServis.Web.Data.Migrations.FaturaGibDurumMigrationHelper.ApplyFaturaGibDurumAsync(context);
});

await RunScopedSafeAsync(app, "TwoFactorMigration", async services =>
{
    var context = services.GetRequiredService<ApplicationDbContext>();
    await KOAFiloServis.Web.Data.Migrations.TwoFactorMigrationHelper.ApplyTwoFactorColumnsAsync(context);
});

await RunScopedSafeAsync(app, "SmsMigration", async services =>
{
    var context = services.GetRequiredService<ApplicationDbContext>();
    var logger = services.GetRequiredService<ILogger<Program>>();
    await KOAFiloServis.Web.Data.Migrations.SmsMigrationHelper.EnsureSmsTablesAsync(context, logger);
});

await RunScopedSafeAsync(app, "GuzergahKoordinatMigration", async services =>
{
    var context = services.GetRequiredService<ApplicationDbContext>();
    if (dbProvider == "PostgreSQL")
    {
        await KOAFiloServis.Web.Data.Migrations.GuzergahKoordinatMigrationHelper.ApplyGuzergahKoordinatMigrationPostgresAsync(context);
    }
    else
    {
        await KOAFiloServis.Web.Data.Migrations.GuzergahKoordinatMigrationHelper.ApplyGuzergahKoordinatMigrationAsync(context);
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
    await KOAFiloServis.Web.Data.Migrations.TenantFirmaIdBackfillMigrationHelper.BackfillAsync(context, logger);
});

await RunScopedSafeAsync(app, "SeedAdmin", async services =>
{
    var kullaniciService = services.GetRequiredService<IKullaniciService>();
    await kullaniciService.SeedAdminAsync();
});

await RunScopedSafeAsync(app, "LisansSeed", async services =>
{
    var lisansService = services.GetRequiredService<ILisansService>();
    await lisansService.GetAktifLisansAsync(); // Trial lisans olusturur
});

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
    await KOAFiloServis.Web.Data.Migrations.CariMigrationHelper.ApplyCariAlanGenisletmeAsync(context);
});

await RunScopedSafeAsync(app, "BordroMigration", async services =>
{
    var context = services.GetRequiredService<ApplicationDbContext>();
    await KOAFiloServis.Web.Data.Migrations.BordroMigrationHelper.ApplyBordroTablolariAsync(context);
});

await RunScopedSafeAsync(app, "PersonelFinansMigration", async services =>
{
    var context = services.GetRequiredService<ApplicationDbContext>();
    await KOAFiloServis.Web.Data.Migrations.PersonelFinansMigrationHelper.ApplyPersonelFinansTablolariAsync(context);
});

await RunScopedSafeAsync(app, "AracMasrafMuhasebeMigration", async services =>
{
    var context = services.GetRequiredService<ApplicationDbContext>();
    await KOAFiloServis.Web.Data.Migrations.AracMasrafMuhasebeMigrationHelper.ApplyAracMasrafMuhasebeAlanlariAsync(context);
});

await RunScopedSafeAsync(app, "OzlukEvrakMigration", async services =>
{
    var context = services.GetRequiredService<ApplicationDbContext>();
    await KOAFiloServis.Web.Data.Migrations.OzlukEvrakMigrationHelper.ApplyOzlukEvrakMigrationAsync(context);
});

await RunScopedSafeAsync(app, "MuhasebeAyarMigration", async services =>
{
    var context = services.GetRequiredService<ApplicationDbContext>();
    await KOAFiloServis.Web.Data.Migrations.MuhasebeAyarMigrationHelper.ApplyStokMasrafAyarlariAsync(context);
});

await RunScopedSafeAsync(app, "BankaHareketPersonelCebindenMigration", async services =>
{
    var context = services.GetRequiredService<ApplicationDbContext>();
    var logger = services.GetRequiredService<ILogger<Program>>();
    await KOAFiloServis.Web.Data.Migrations.BankaHareketPersonelCebindenMigrationHelper.EnsurePersonelCebindenColumnsAsync(context, logger);
});

await RunScopedSafeAsync(app, "PersonelAvansHesapMigration", async services =>
{
    var context = services.GetRequiredService<ApplicationDbContext>();
    await KOAFiloServis.Web.Data.Migrations.PersonelAvansHesapMigrationHelper.ApplyPersonelAvansHesaplariAsync(context);
});

await RunScopedSafeAsync(app, "PersonelBelgeTarihleriMigration", async services =>
{
    var context = services.GetRequiredService<ApplicationDbContext>();
    var logger = services.GetRequiredService<ILogger<Program>>();
    await KOAFiloServis.Web.Data.Migrations.PersonelBelgeTarihleriMigrationHelper.ApplyPersonelBelgeTarihleriAsync(context, logger);
});

await RunScopedSafeAsync(app, "LastikSezonAyarMigration", async services =>
{
    var context = services.GetRequiredService<ApplicationDbContext>();
    await KOAFiloServis.Web.Data.Migrations.LastikSezonAyarMigrationHelper.ApplyLastikSezonAyarAsync(context);
});

// Sprint S1: PuantajKayitlar tablosuna Slot, KurumId, IsverenFirmaId kolonlarini ekle
await RunScopedSafeAsync(app, "PuantajSlotMigration", async services =>
{
    var context = services.GetRequiredService<ApplicationDbContext>();
    var logger = services.GetRequiredService<ILogger<Program>>();
    await KOAFiloServis.Web.Data.Migrations.PuantajSlotMigrationHelper.ApplyAsync(context, logger);
});

await RunScopedSafeAsync(app, "GuzergahSeferFirmaIdConstraint", async services =>
{
    var context = services.GetRequiredService<ApplicationDbContext>();
    var logger = services.GetRequiredService<ILogger<Program>>();
    await KOAFiloServis.Web.Data.Migrations.GuzergahSeferFirmaIdConstraintHelper.ApplyAsync(context, logger);
});

await RunScopedSafeAsync(app, "KiralikPlakaFaturaMigration", async services =>
{
    var context = services.GetRequiredService<ApplicationDbContext>();
    var logger = services.GetRequiredService<ILogger<Program>>();
    await KOAFiloServis.Web.Data.Migrations.KiralikPlakaFaturaMigrationHelper.ApplyAsync(context, logger);
});

await RunScopedSafeAsync(app, "SeedDefaultEvrakTanimlari", async services =>
{
    var ozlukService = services.GetRequiredService<IPersonelOzlukService>();
    await ozlukService.SeedDefaultEvrakTanimlariAsync();
});

// Nihai Mimari: Tenant DB oluşturma ve migration blokları kaldırıldı.
// Tüm firmalar tek KOAFiloServis veritabanında çalışır.
// Migration helper'lar tek veritabanında ApplyMigrations ile uygulanır (aşağıya bakın).

// Migration helper'ları tek veritabanında uygula (idempotent, race-condition safe)
await RunScopedSafeAsync(app, "ApplyMigrations", async services =>
{
    var ctx = services.GetRequiredService<ApplicationDbContext>();
    var logger = services.GetRequiredService<ILogger<Program>>();

    await KOAFiloServis.Web.Data.Migrations.GuzergahKoordinatMigrationHelper.ApplyGuzergahKoordinatMigrationPostgresAsync(ctx);
    await KOAFiloServis.Web.Data.Migrations.PuantajSlotMigrationHelper.ApplyAsync(ctx, logger);
    await KOAFiloServis.Web.Data.Migrations.KiralikPlakaFaturaMigrationHelper.ApplyAsync(ctx, logger);
    await KOAFiloServis.Web.Data.Migrations.GuzergahSeferFirmaIdConstraintHelper.ApplyAsync(ctx, logger);
    await KOAFiloServis.Web.Data.Migrations.SyncPuantajSchemaMigrationHelper.ApplyAsync(ctx, logger);
    await KOAFiloServis.Web.Data.Migrations.PuantajCarpaniMigrationHelper.ApplyAsync(ctx, logger);
    await KOAFiloServis.Web.Data.Migrations.PuantajSyncMigrationHelper.ApplyAsync(ctx, logger);
    await KOAFiloServis.Web.Data.Migrations.BudgetOdemeKalanMigrationHelper.EnsureBudgetOdemeKalanColumnAsync(ctx);
    await KOAFiloServis.Web.Data.Migrations.BudgetHedefMigrationHelper.EnsureBudgetHedefTableAsync(ctx);

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
    ");

    // Kural 15: FisNoCounters'a FirmaId + composite PK (idempotent)
    await ctx.Database.ExecuteSqlRawAsync(@"
        DO $$ BEGIN
            ALTER TABLE ""FisNoCounters"" ADD COLUMN IF NOT EXISTS ""FirmaId"" integer NOT NULL DEFAULT 0;
        EXCEPTION WHEN duplicate_column THEN END; $$;
        DO $$ BEGIN
            ALTER TABLE ""FisNoCounters"" DROP CONSTRAINT IF EXISTS ""PK_FisNoCounters"";
        EXCEPTION WHEN undefined_object THEN END; $$;
        DO $$ BEGIN
            ALTER TABLE ""FisNoCounters"" ADD PRIMARY KEY (""Prefix"", ""FirmaId"", ""YilAy"");
        EXCEPTION WHEN duplicate_table THEN END; $$;
    ");

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
                    SELECT setval(
                        pg_get_serial_sequence('"{table}"', 'Id'),
                        COALESCE((SELECT MAX("Id") FROM "{table}"), 0) + 1,
                        false
                    );
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
app.MapHealthChecks("/health/puantaj-job", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("job"),
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var json = System.Text.Json.JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            results = report.Entries.ToDictionary(
                e => e.Key,
                e => new { e.Value.Status, e.Value.Description, e.Value.Duration })
        });
        await context.Response.WriteAsync(json);
    }
});

// Swagger UI - tüm ortamlarda aktif (API dokümantasyonu)
app.UseSwagger(c =>
{
    c.RouteTemplate = "swagger/{documentName}/swagger.json";
});
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "KOAFiloServis API v1");
    c.RoutePrefix = "swagger";
    c.DocumentTitle = "KOAFiloServis API Dokümantasyonu";
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
app.UseMiddleware<KOAFiloServis.Web.Middleware.IpGuvenlikMiddleware>();

// Authentication & Authorization - API için
app.UseAuthentication();
app.UseAuthorization();

var externalUploadsPath = AppStoragePaths.GetUploadsRoot(app.Environment.ContentRootPath);
Directory.CreateDirectory(externalUploadsPath);

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(externalUploadsPath),
    RequestPath = "/uploads"
});

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
app.MapControllers(); // API Controller'larini haritalandir
app.MapHub<AracTakipHub>("/hubs/aractakip"); // SignalR Araç Takip Hub'ı

app.Run();



