using System.Text.Json;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Web.Services;

public sealed class PortalProjectCatalogService : IPortalProjectCatalogService
{
    private readonly string _settingsPath;
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };
    private PortalProjectCatalogOptions _options;

    public PortalProjectCatalogService(IConfiguration configuration, IWebHostEnvironment environment)
    {
        _settingsPath = Path.Combine(environment.ContentRootPath, "portalsettings.json");

        var configuredOptions = configuration.GetSection("PortalProjects").Get<PortalProjectCatalogOptions>() ?? new PortalProjectCatalogOptions();
        EnsureDefaults(configuredOptions);

        if (File.Exists(_settingsPath))
        {
            try
            {
                var savedOptions = JsonSerializer.Deserialize<PortalProjectCatalogOptions>(File.ReadAllText(_settingsPath), _jsonOptions);
                _options = savedOptions ?? configuredOptions;
            }
            catch
            {
                _options = configuredOptions;
            }
        }
        else
        {
            _options = configuredOptions;
        }

        EnsureDefaults(_options);
    }

    public PortalProjectCatalogOptions GetCatalog() => _options;

    public IReadOnlyList<PortalProjectDefinition> GetProjects() => _options.Projects;

    public PortalProjectDefinition? GetProjectBySlug(string? slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return null;
        }

        return _options.Projects.FirstOrDefault(p => string.Equals(p.Slug, slug, StringComparison.OrdinalIgnoreCase));
    }

    public PortalProjectDefinition GetDefaultProject()
    {
        return _options.Projects.FirstOrDefault(p => p.LoginEnabled)
            ?? _options.Projects.First();
    }

    public PortalProjectCatalogOptions CreateEditableCopy()
    {
        return Clone(_options);
    }

    public async Task SaveAsync(PortalProjectCatalogOptions settings)
    {
        EnsureDefaults(settings);
        var normalized = Clone(settings);
        var json = JsonSerializer.Serialize(normalized, _jsonOptions);
        await File.WriteAllTextAsync(_settingsPath, json);
        _options = normalized;
    }

    private static PortalProjectCatalogOptions Clone(PortalProjectCatalogOptions options)
    {
        var json = JsonSerializer.Serialize(options);
        return JsonSerializer.Deserialize<PortalProjectCatalogOptions>(json) ?? new PortalProjectCatalogOptions();
    }

    private static void EnsureDefaults(PortalProjectCatalogOptions options)
    {
        options.CompanyName = Normalize(options.CompanyName, "Koa Teknoloji");
        options.SupportEmail = Normalize(options.SupportEmail, "info@koateknoloji.com");
        options.SupportPhone = Normalize(options.SupportPhone, "0850 000 00 00");
        options.SupportLocation = Normalize(options.SupportLocation, "İstanbul / Türkiye");
        options.TopbarMessage = Normalize(options.TopbarMessage, "Kurumsal danışmanlık, bordro ve dijital dönüşüm");
        options.HeroTitle = Normalize(options.HeroTitle, "İş süreçlerinizi tek merkezden yönetin");
        options.HeroSubtitle = Normalize(options.HeroSubtitle, "Kurumsal portal üzerinden projenizi seçin ve güvenli girişe devam edin.");

        options.Content ??= new PortalLandingContent();
        EnsureContentDefaults(options);

        if (options.Projects.Count == 0)
        {
            options.Projects.Add(CreateDefaultProject());
        }

        foreach (var project in options.Projects)
        {
            project.Slug = Normalize(project.Slug, Guid.NewGuid().ToString("N"));
            project.Name = Normalize(project.Name, "Yeni Proje");
            project.BrandHighlight = Normalize(project.BrandHighlight, project.Name);
            project.Category = Normalize(project.Category, "Genel");
            project.Subtitle = Normalize(project.Subtitle, project.Name);
            project.Description = Normalize(project.Description, project.Subtitle);
            project.Icon = Normalize(project.Icon, "bi bi-grid-1x2-fill");
            project.ThemeColor = Normalize(project.ThemeColor, "#1e3c72");
            project.Highlights = NormalizeStrings(project.Highlights, ["Örnek özellik"]);
        }
    }

    private static void EnsureContentDefaults(PortalProjectCatalogOptions options)
    {
        var content = options.Content!;

        content.BrowserTitle = Normalize(content.BrowserTitle, $"Portal - {options.CompanyName}");
        content.BrandTagline = Normalize(content.BrandTagline, "Kurumsal çözüm portalı");

        content.Navigation ??= new PortalNavigationContent();
        content.Navigation.AboutText = Normalize(content.Navigation.AboutText, "Hakkımızda");
        content.Navigation.ServicesText = Normalize(content.Navigation.ServicesText, "Hizmetler");
        content.Navigation.ProjectsText = Normalize(content.Navigation.ProjectsText, "Dijital Çözümler");
        content.Navigation.ContactText = Normalize(content.Navigation.ContactText, "İletişim");

        content.Hero ??= new PortalHeroContent();
        content.Hero.BadgeText = Normalize(content.Hero.BadgeText, "Kurumsal Çözüm Portalı");
        content.Hero.Title = Normalize(content.Hero.Title, options.HeroTitle);
        content.Hero.Subtitle = Normalize(content.Hero.Subtitle, options.HeroSubtitle);
        content.Hero.PrimaryButtonText = Normalize(content.Hero.PrimaryButtonText, "Programa Geç");
        content.Hero.SecondaryButtonText = Normalize(content.Hero.SecondaryButtonText, "Teklif Al");
        content.Hero.Highlights = NormalizeStrings(content.Hero.Highlights,
        [
            "Public web sayfası ve yönetim paneli aynı çözüm içinde kurgulanabilir",
            "Yetki kontrollü giriş, personel ve bordro yönetimi tek portalda toplanır",
            "Kurumsal tasarım, ürün seçimi ve login akışı marka bütünlüğüyle sunulur"
        ]);
        content.Hero.Stats = NormalizeStats(content.Hero.Stats,
        [
            new PortalStatItem { Value = "12+", Label = "Kurumsal modül yaklaşımı" },
            new PortalStatItem { Value = "7/24", Label = "Portal erişimi" },
            new PortalStatItem { Value = "%100", Label = "Yönetilebilir içerik kurgusu" }
        ]);
        content.Hero.FlowEyebrow = Normalize(content.Hero.FlowEyebrow, "Portal Akışı");
        content.Hero.FlowTitle = Normalize(content.Hero.FlowTitle, "3 adımda giriş");
        content.Hero.FlowSteps = NormalizeSteps(content.Hero.FlowSteps,
        [
            new PortalStepItem { Number = "01", Title = "Kurumsal çözümü seçin", Description = "Aktif projeler arasından ihtiyacınıza uygun ürünü seçin." },
            new PortalStepItem { Number = "02", Title = "Giriş ekranına ilerleyin", Description = "Seçtiğiniz ürün için markalanmış giriş sayfasına geçin." },
            new PortalStepItem { Number = "03", Title = "Sisteme devam edin", Description = "Kullanıcı hesabınızla oturum açıp operasyon ekranlarına geçin." }
        ]);

        content.About ??= new PortalAboutContent();
        content.About.Tag = Normalize(content.About.Tag, "Hakkımızda");
        content.About.Title = Normalize(content.About.Title, "Mali süreçleri, bordroyu ve kurumsal operasyonu tek çatı altında topluyoruz");
        content.About.Description = Normalize(content.About.Description, "Koa Teknoloji; mali danışmanlık, personel süreçleri, bordro operasyonları ve yönetim raporlarını dijitalleştiren çözümler üretir. Public tanıtım sayfası ile kurumsal uygulama girişini aynı yapıda birleştirerek marka bütünlüğü sağlar.");
        content.About.PanelTitle = Normalize(content.About.PanelTitle, "Neler sağlıyoruz?");
        content.About.Cards = NormalizeTextCards(content.About.Cards,
        [
            new PortalTextCard { Icon = "bi bi-briefcase", Title = "Operasyon ve finans birlikte", Description = "Muhasebe, bordro ve şirket süreçlerini ayrı ekranlar yerine tek kurgu üzerinden yönetin." },
            new PortalTextCard { Icon = "bi bi-shield-check", Title = "Yetki bazlı yönetim", Description = "Aktif/pasif kullanıcı kontrolü, menü görünürlüğü ve rol bazlı erişim merkezi olarak yönetilir." },
            new PortalTextCard { Icon = "bi bi-globe2", Title = "Public + uygulama entegrasyonu", Description = "Tanıtım sitesi ile kurumsal uygulama geçişi tek kök domain veya ayrı çalışan yapı ile sunulabilir." }
        ]);
        content.About.Features = NormalizeTextCards(content.About.Features,
        [
            new PortalTextCard { Title = "Kurumsal içerik yönetimi", Description = "Hizmetler, ekip, referanslar ve çağrı alanları markaya uygun olarak düzenlenir." },
            new PortalTextCard { Title = "Bordro ve personel odağı", Description = "Personel listesi, bordro türleri ve finansal süreçler uygulama tarafına entegre edilir." },
            new PortalTextCard { Title = "Test edilebilir akış", Description = "Kullanıcı senaryoları Playwright ile doğrulanarak yayın öncesi güvence sağlanır." }
        ]);

        content.Services ??= new PortalServicesContent();
        content.Services.Tag = Normalize(content.Services.Tag, "Hizmetler");
        content.Services.Title = Normalize(content.Services.Title, "Kurumsal ihtiyaçlara göre şekillenen çözüm başlıkları");
        content.Services.Description = Normalize(content.Services.Description, "Muhasebe ve bordrodan yazılım altyapısına kadar uçtan uca destek verin, aynı zamanda kullanıcıları tek bir giriş akışında toplayın.");
        content.Services.Cards = NormalizeServiceCards(content.Services.Cards,
        [
            new PortalServiceCard
            {
                Icon = "bi bi-calculator",
                Title = "Mali danışmanlık ve raporlama",
                Description = "Kurumsal müşteriler için finansal görünürlük oluşturan modüler ekranlar.",
                Bullets = ["Aylık finans özetleri", "Yönetim raporları", "Bütçe ve ödeme akışı"]
            },
            new PortalServiceCard
            {
                Icon = "bi bi-people",
                Title = "Personel ve bordro süreçleri",
                Description = "İşe girişten çıkışa kadar personel hareketlerini ve bordro hesaplamalarını tek panelden yönetin.",
                Bullets = ["Normal ve AR-GE bordro", "İzin ve maaş yönetimi", "Çıkış tarihine göre hesaplama"]
            },
            new PortalServiceCard
            {
                Icon = "bi bi-window-stack",
                Title = "Kurumsal portal ve giriş deneyimi",
                Description = "Public sayfa, proje seçimi ve kullanıcı login akışı markalı deneyimle sunulur.",
                Bullets = ["Anonim tanıtım sayfası", "Projeye özel login yönlendirme", "Aktif/pasif proje yayını"]
            }
        ]);

        content.Projects ??= new PortalProjectsContent();
        content.Projects.Tag = Normalize(content.Projects.Tag, "Projeler");
        content.Projects.Title = Normalize(content.Projects.Title, "Kurumsal uygulamalarınızı tek kapıda toplayın");
        content.Projects.Description = Normalize(content.Projects.Description, "Bu alana yeni projeler ekleyebilir, aktif olanları doğrudan login ekranına bağlayabilirsiniz.");
        content.Projects.ReadyBadgeText = Normalize(content.Projects.ReadyBadgeText, "Girişe Hazır");
        content.Projects.SoonBadgeText = Normalize(content.Projects.SoonBadgeText, "Yakında");
        content.Projects.OpenButtonText = Normalize(content.Projects.OpenButtonText, "Projeyi Seç ve Girişe Git");
        content.Projects.SoonButtonText = Normalize(content.Projects.SoonButtonText, "Yakında Aktif Olacak");

        content.Team ??= new PortalTeamContent();
        content.Team.Tag = Normalize(content.Team.Tag, "Ekip");
        content.Team.Title = Normalize(content.Team.Title, "Alan uzmanlığı ile teknolojiyi aynı masada buluşturan ekip");
        content.Team.Description = Normalize(content.Team.Description, "Yazılım, bordro, mali süreçler ve operasyon yönetimini birlikte ele alan disiplinler arası yapı.");
        content.Team.Members = NormalizeTeamMembers(content.Team.Members,
        [
            new PortalTeamMember { Initials = "KT", Name = "Koa Teknoloji Ekibi", Role = "Ürün Yönetimi", Description = "Kurumsal ihtiyaçları yazılım mimarisi ve kullanım senaryolarına dönüştürür." },
            new PortalTeamMember { Initials = "MY", Name = "Mali Operasyon", Role = "Mali Süreçler", Description = "Muhasebe, bordro ve raporlama ihtiyaçlarının uygulama tarafına doğru yansımasını sağlar." },
            new PortalTeamMember { Initials = "PY", Name = "Portal Yönetimi", Role = "Kullanıcı Deneyimi", Description = "Public sayfa, giriş ekranı ve yönetim paneli arasındaki akışı sadeleştirir." },
            new PortalTeamMember { Initials = "QA", Name = "Test Ekibi", Role = "Kalite Güvencesi", Description = "Playwright senaryoları ile kritik kullanıcı yolculuklarını otomatik doğrular." }
        ]);

        content.Contact ??= new PortalContactContent();
        content.Contact.Tag = Normalize(content.Contact.Tag, "İletişim");
        content.Contact.Title = Normalize(content.Contact.Title, "Kurumsal tanıtım sayfanızı ve yönetim panelinizi birlikte kuralım");
        content.Contact.Description = Normalize(content.Contact.Description, "İster sadece public tanıtım ekranı, ister login bağlantılı tam kurumsal portal kurgusu isteyin; aynı proje içinde yönetilebilir bir yapı sunuyoruz.");
        content.Contact.PrimaryButtonText = Normalize(content.Contact.PrimaryButtonText, "Login Akışını Aç");
        content.Contact.SecondaryButtonText = Normalize(content.Contact.SecondaryButtonText, "E-posta Gönder");
        content.Contact.SecondaryButtonUrl = Normalize(content.Contact.SecondaryButtonUrl, $"mailto:{options.SupportEmail}");
        content.Contact.Cards = NormalizeTextCards(content.Contact.Cards,
        [
            new PortalTextCard { Icon = "bi bi-layout-text-window", Title = "Public web yayını", Description = "Markanıza uygun ana sayfa, hizmet kartları ve iletişim alanlarıyla yayın alın." },
            new PortalTextCard { Icon = "bi bi-toggles", Title = "Yönetim ekranı", Description = "Kullanıcı, proje veya modül bazlı aktif/pasif kontrol mekanizmaları ekleyin." },
            new PortalTextCard { Icon = "bi bi-robot", Title = "Test otomasyonu", Description = "Selenium yerine Playwright temelli kullanıcı testleriyle yayın kalitesini yükseltin." },
            new PortalTextCard { Icon = "bi bi-git", Title = "Yayın ve versiyonlama", Description = "Kod gözden geçirme sonrası GitHub akışı ile değişiklikleri kontrollü yönetin." }
        ]);
    }

    private static string Normalize(string? value, string fallback)
        => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

    private static List<string> NormalizeStrings(List<string>? values, IEnumerable<string> defaults)
    {
        var normalized = (values ?? [])
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .ToList();

        return normalized.Count > 0
            ? normalized
            : defaults.Select(x => x.Trim()).ToList();
    }

    private static List<PortalStatItem> NormalizeStats(List<PortalStatItem>? values, IEnumerable<PortalStatItem> defaults)
    {
        var normalized = (values ?? [])
            .Select(item => new PortalStatItem
            {
                Value = Normalize(item.Value, string.Empty),
                Label = Normalize(item.Label, string.Empty)
            })
            .Where(item => !string.IsNullOrWhiteSpace(item.Value) || !string.IsNullOrWhiteSpace(item.Label))
            .ToList();

        if (normalized.Count > 0)
        {
            return normalized;
        }

        return defaults
            .Select(item => new PortalStatItem { Value = item.Value, Label = item.Label })
            .ToList();
    }

    private static List<PortalStepItem> NormalizeSteps(List<PortalStepItem>? values, IEnumerable<PortalStepItem> defaults)
    {
        var normalized = (values ?? [])
            .Select(item => new PortalStepItem
            {
                Number = Normalize(item.Number, string.Empty),
                Title = Normalize(item.Title, string.Empty),
                Description = Normalize(item.Description, string.Empty)
            })
            .Where(item => !string.IsNullOrWhiteSpace(item.Title) || !string.IsNullOrWhiteSpace(item.Description))
            .ToList();

        if (normalized.Count > 0)
        {
            return normalized;
        }

        return defaults
            .Select(item => new PortalStepItem { Number = item.Number, Title = item.Title, Description = item.Description })
            .ToList();
    }

    private static List<PortalTextCard> NormalizeTextCards(List<PortalTextCard>? values, IEnumerable<PortalTextCard> defaults)
    {
        var normalized = (values ?? [])
            .Select(item => new PortalTextCard
            {
                Icon = Normalize(item.Icon, string.Empty),
                Title = Normalize(item.Title, string.Empty),
                Description = Normalize(item.Description, string.Empty)
            })
            .Where(item => !string.IsNullOrWhiteSpace(item.Title) || !string.IsNullOrWhiteSpace(item.Description))
            .ToList();

        if (normalized.Count > 0)
        {
            return normalized;
        }

        return defaults
            .Select(item => new PortalTextCard { Icon = item.Icon, Title = item.Title, Description = item.Description })
            .ToList();
    }

    private static List<PortalServiceCard> NormalizeServiceCards(List<PortalServiceCard>? values, IEnumerable<PortalServiceCard> defaults)
    {
        var normalized = (values ?? [])
            .Select(item => new PortalServiceCard
            {
                Icon = Normalize(item.Icon, "bi bi-grid-1x2-fill"),
                Title = Normalize(item.Title, string.Empty),
                Description = Normalize(item.Description, string.Empty),
                Bullets = NormalizeStrings(item.Bullets, ["Örnek madde"])
            })
            .Where(item => !string.IsNullOrWhiteSpace(item.Title) || !string.IsNullOrWhiteSpace(item.Description))
            .ToList();

        if (normalized.Count > 0)
        {
            return normalized;
        }

        return defaults
            .Select(item => new PortalServiceCard
            {
                Icon = item.Icon,
                Title = item.Title,
                Description = item.Description,
                Bullets = item.Bullets.ToList()
            })
            .ToList();
    }

    private static List<PortalTeamMember> NormalizeTeamMembers(List<PortalTeamMember>? values, IEnumerable<PortalTeamMember> defaults)
    {
        var normalized = (values ?? [])
            .Select(item => new PortalTeamMember
            {
                Initials = Normalize(item.Initials, string.Empty),
                Name = Normalize(item.Name, string.Empty),
                Role = Normalize(item.Role, string.Empty),
                Description = Normalize(item.Description, string.Empty)
            })
            .Where(item => !string.IsNullOrWhiteSpace(item.Name) || !string.IsNullOrWhiteSpace(item.Description))
            .ToList();

        if (normalized.Count > 0)
        {
            return normalized;
        }

        return defaults
            .Select(item => new PortalTeamMember
            {
                Initials = item.Initials,
                Name = item.Name,
                Role = item.Role,
                Description = item.Description
            })
            .ToList();
    }

    private static PortalProjectDefinition CreateDefaultProject()
    {
        return new PortalProjectDefinition
        {
            Slug = "koa-filo-servis",
            Name = "Koa Filo Servis",
            BrandHighlight = "Koa Filo Servis",
            Category = "Filo Yönetimi",
            Subtitle = "Filo operasyonu, muhasebe ve servis süreçleri tek panelde.",
            Description = "Araç, sürücü, cari ve muhasebe operasyonlarını tek portal üzerinden yönetin.",
            Icon = "bi bi-truck",
            ThemeColor = "#1e3c72",
            LoginEnabled = true,
            Highlights =
            {
                "Operasyon ve araç yönetimi",
                "Muhasebe ve raporlama modülleri",
                "Kurumsal kullanıcı giriş akışı"
            }
        };
    }
}

public sealed class PortalProjectCatalogOptions
{
    public bool LandingPageEnabled { get; set; } = true;
    public string CompanyName { get; set; } = "Koa Teknoloji";
    public string SupportEmail { get; set; } = "info@koateknoloji.com";
    public string SupportPhone { get; set; } = "0850 000 00 00";
    public string SupportLocation { get; set; } = "İstanbul / Türkiye";
    public string TopbarMessage { get; set; } = "Kurumsal danışmanlık, bordro ve dijital dönüşüm";
    public string HeroTitle { get; set; } = "İş süreçlerinizi tek merkezden yönetin";
    public string HeroSubtitle { get; set; } = "Kurumsal portal üzerinden projenizi seçin ve güvenli girişe devam edin.";
    public PortalLandingContent Content { get; set; } = new();
    public List<PortalProjectDefinition> Projects { get; set; } = new();
}

public sealed class PortalLandingContent
{
    public string BrowserTitle { get; set; } = "Portal - Koa Teknoloji";
    public string BrandTagline { get; set; } = "Kurumsal çözüm portalı";
    public PortalNavigationContent Navigation { get; set; } = new();
    public PortalHeroContent Hero { get; set; } = new();
    public PortalAboutContent About { get; set; } = new();
    public PortalServicesContent Services { get; set; } = new();
    public PortalProjectsContent Projects { get; set; } = new();
    public PortalTeamContent Team { get; set; } = new();
    public PortalContactContent Contact { get; set; } = new();
}

public sealed class PortalNavigationContent
{
    public string AboutText { get; set; } = "Hakkımızda";
    public string ServicesText { get; set; } = "Hizmetler";
    public string ProjectsText { get; set; } = "Dijital Çözümler";
    public string ContactText { get; set; } = "İletişim";
}

public sealed class PortalHeroContent
{
    public string BadgeText { get; set; } = "Kurumsal Çözüm Portalı";
    public string Title { get; set; } = "İş süreçlerinizi tek merkezden yönetin";
    public string Subtitle { get; set; } = "Kurumsal portal üzerinden projenizi seçin ve güvenli girişe devam edin.";
    public string PrimaryButtonText { get; set; } = "Programa Geç";
    public string SecondaryButtonText { get; set; } = "Teklif Al";
    public List<string> Highlights { get; set; } = new();
    public List<PortalStatItem> Stats { get; set; } = new();
    public string FlowEyebrow { get; set; } = "Portal Akışı";
    public string FlowTitle { get; set; } = "3 adımda giriş";
    public List<PortalStepItem> FlowSteps { get; set; } = new();
}

public sealed class PortalAboutContent
{
    public string Tag { get; set; } = "Hakkımızda";
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string PanelTitle { get; set; } = "Neler sağlıyoruz?";
    public List<PortalTextCard> Cards { get; set; } = new();
    public List<PortalTextCard> Features { get; set; } = new();
}

public sealed class PortalServicesContent
{
    public string Tag { get; set; } = "Hizmetler";
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<PortalServiceCard> Cards { get; set; } = new();
}

public sealed class PortalProjectsContent
{
    public string Tag { get; set; } = "Projeler";
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ReadyBadgeText { get; set; } = "Girişe Hazır";
    public string SoonBadgeText { get; set; } = "Yakında";
    public string OpenButtonText { get; set; } = "Projeyi Seç ve Girişe Git";
    public string SoonButtonText { get; set; } = "Yakında Aktif Olacak";
}

public sealed class PortalTeamContent
{
    public string Tag { get; set; } = "Ekip";
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<PortalTeamMember> Members { get; set; } = new();
}

public sealed class PortalContactContent
{
    public string Tag { get; set; } = "İletişim";
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string PrimaryButtonText { get; set; } = "Login Akışını Aç";
    public string SecondaryButtonText { get; set; } = "E-posta Gönder";
    public string SecondaryButtonUrl { get; set; } = string.Empty;
    public List<PortalTextCard> Cards { get; set; } = new();
}

public sealed class PortalStatItem
{
    public string Value { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
}

public sealed class PortalStepItem
{
    public string Number { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public sealed class PortalTextCard
{
    public string Icon { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public sealed class PortalServiceCard
{
    public string Icon { get; set; } = "bi bi-grid-1x2-fill";
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Bullets { get; set; } = new();
}

public sealed class PortalTeamMember
{
    public string Initials { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public sealed class PortalProjectDefinition
{
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string BrandHighlight { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = "bi bi-grid-1x2-fill";
    public string ThemeColor { get; set; } = "#1e3c72";
    public bool LoginEnabled { get; set; } = true;
    public List<string> Highlights { get; set; } = new();
}



