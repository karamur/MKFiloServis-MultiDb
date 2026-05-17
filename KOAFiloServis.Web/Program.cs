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

// EPPlus lisans ayari (NonCommercial kullanim icin)
ExcelPackage.License.SetNonCommercialPersonal("KOAFiloServis");

var builder = WebApplication.CreateBuilder(args);

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

// Database - Pooled DbContextFactory kullan (thread-safe)
builder.Services.AddPooledDbContextFactory<ApplicationDbContext>((sp, options) =>
{
    var enableSensitiveDataLogging = builder.Environment.IsDevelopment() &&
        builder.Configuration.GetValue<bool>("EntityFramework:EnableSensitiveDataLogging");

    if (dbProvider == "PostgreSQL")
    {
        // PostgreSQL timestamp ayari
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

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

    // Pending migration uyarisini devre disi birak
    options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
    options.AddInterceptors(sp.GetRequiredService<AktiviteLogInterceptor>());
});

// DbContext - Factory'den olustur ve scoped IServiceProvider'ı bağla
// ITenantService sorgu zamanında lazy olarak çözümlenir (döngüsel bağımlılık önlenir)
builder.Services.AddScoped<ApplicationDbContext>(sp =>
{
    var context = sp.GetRequiredService<IDbContextFactory<ApplicationDbContext>>().CreateDbContext();
    context.SetServiceProvider(sp);
    return context;
});

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

// Multi-tenant Service
builder.Services.AddScoped<ITenantService, TenantService>();

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

// OpenRouter AI Integration
builder.Services.AddHttpClient<IOpenRouterService, OpenRouterService>();

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
builder.Services.AddScoped<IKurumService, KurumService>();
builder.Services.AddScoped<IPuantajService, PuantajService>();
builder.Services.AddScoped(typeof(KOAFiloServis.Web.Services.Interfaces.IFiloKomisyonService), typeof(FiloKomisyonService));
builder.Services.AddScoped<KOAFiloServis.Web.Services.Interfaces.IPuantajEslestirmeService, PuantajEslestirmeService>();
builder.Services.AddScoped<IAracDegerlemeAIService, AracDegerlemeAIService>(); // AI Arac Degerleme
builder.Services.AddScoped<IPiyasaKaynakService, PiyasaKaynakService>(); // Piyasa Kaynak Yonetimi (once kaydet)
builder.Services.AddScoped<IHttpScraperService, HttpScraperService>(); // HTTP Scraper (en hizli)
builder.Services.AddScoped<IPlaywrightScraperService, PlaywrightScraperService>(); // Playwright Web Scraper (yedek)
builder.Services.AddScoped<IAracPiyasaArastirmaService, AracPiyasaArastirmaService>(); // AI Piyasa Arastirma
builder.Services.AddScoped<IMusteriKiralamaService, MusteriKiralamaService>(); // Musteri Kiralama Servisi
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
builder.Services.AddHttpClient("OpenAI"); // OpenAI icin HttpClient
builder.Services.AddHttpClient("Scraper"); // Scraper icin HttpClient
builder.Services.AddScoped<IOllamaService, OllamaService>(); // Ollama AI Rapor Yorumlama
builder.Services.AddScoped<IFaturaAIImportService, FaturaAIImportService>(); // AI Fatura Import Servisi
builder.Services.AddScoped<IIhaleHazirlikService, IhaleHazirlikService>(); // İhale Hazırlık Servisi
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
builder.Services.AddScoped<IEbysAIService, EbysAIService>(); // EBYS AI Servisi (OCR, Belge Sınıflandırma)
builder.Services.AddScoped<ISemanticSearchService, SemanticSearchService>(); // EBYS Semantic Search (Akıllı Belge Arama) Servisi
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
});
builder.Services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

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
        logger.LogCritical(ex, "Startup gorevi basarisiz. Uygulama durduruluyor: {TaskName}", taskName);
        throw;
    }
}

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

await RunScopedSafeAsync(app, "SeedDefaultEvrakTanimlari", async services =>
{
    var ozlukService = services.GetRequiredService<IPersonelOzlukService>();
    await ozlukService.SeedDefaultEvrakTanimlariAsync();
});

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

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


