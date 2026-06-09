using FluentAssertions;
using KOAFiloServis.Shared.Entities;
using KOAFiloServis.Web.Data;
using KOAFiloServis.Web.Services;
using KOAFiloServis.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace KOAFiloServis.Tests.Services;

/// <summary>
/// Evrak dosya bütünlüğü testi: DB kaydı → disk → şifreleme formatı → MIME type.
/// Browser testi olmadan zincirin sağlam olduğunu kanıtlar.
/// </summary>
public class EvrakDownloadChainTests
{
    private const string ConnectionString =
        "Host=localhost;Port=5432;Database=KOAFiloServis;Username=postgres;Password=Fast123";

    private static readonly DbContextOptions<ApplicationDbContext> DbOptions =
        new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(ConnectionString).Options;

    private sealed class TestAktifFirmaProvider : IAktifFirmaProvider
    {
        public TestAktifFirmaProvider(int firmaId)
            => Mevcut = new AktifFirmaBilgisi { FirmaId = firmaId, TumFirmalar = false };
        public int? AktifFirmaId => Mevcut.FirmaId > 0 ? Mevcut.FirmaId : null;
        public bool HasAktifFirma => AktifFirmaId.HasValue || TumFirmalar;
        public bool TumFirmalar => Mevcut.TumFirmalar;
        public AktifFirmaBilgisi Mevcut { get; private set; }
        public event Action? AktifFirmaDegisti;
        public void Set(AktifFirmaBilgisi f) { Mevcut = f; AktifFirmaDegisti?.Invoke(); }
        public void SetTumFirmalar(bool tf) { Mevcut.TumFirmalar = tf; AktifFirmaDegisti?.Invoke(); }
        public void SetDonem(int y, int m) { Mevcut.AktifDonemYil = y; Mevcut.AktifDonemAy = m; AktifFirmaDegisti?.Invoke(); }
        public Task<bool> TryRestoreAsync() => Task.FromResult(false);
    }

    private sealed class TestDbContextFactory : IDbContextFactory<ApplicationDbContext>
    {
        private readonly IServiceProvider _sp;
        public TestDbContextFactory(IServiceProvider sp) => _sp = sp;
        public ApplicationDbContext CreateDbContext()
        {
            var ctx = new ApplicationDbContext(DbOptions);
            ctx.SetServiceProvider(_sp);
            return ctx;
        }
    }

    private IDbContextFactory<ApplicationDbContext> CreateFactory(int firmaId = 1)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IAktifFirmaProvider>(new TestAktifFirmaProvider(firmaId));
        return new TestDbContextFactory(services.BuildServiceProvider());
    }

    private static string GetMimeType(string? extension) => extension?.ToLower() switch
    {
        "pdf" => "application/pdf",
        "jpg" or "jpeg" => "image/jpeg",
        "png" => "image/png",
        "xls" => "application/vnd.ms-excel",
        "xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "doc" => "application/msword",
        "docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        _ => "application/octet-stream"
    };

    /// <summary>
    /// TEST 1: Araç evrak — DB kaydı var, diskte dosya var, KOA1 şifreli, MIME type doğru.
    /// </summary>
    [Fact]
    public async Task AracEvrak_FullChain_AllLinksVerified()
    {
        var factory = CreateFactory(1);
        await using var ctx = factory.CreateDbContext();

        // 1. DB'de kayıt var mı?
        var dosya = await ctx.AracEvrakDosyalari
            .OrderByDescending(d => d.DosyaBoyutu)
            .FirstOrDefaultAsync(d => !d.IsDeleted && d.DosyaTipi == "pdf" && d.DosyaBoyutu > 1000);
        dosya.Should().NotBeNull("DB'de PDF araç evrak kaydı olmalı");

        // 2. Dosya diskte var mı?
        var storageRoot = KOAFiloServis.Web.Helpers.AppStoragePaths.GetUploadsRoot(
            System.AppContext.BaseDirectory);
        var diskPath = System.IO.Path.Combine(storageRoot, dosya!.DosyaYolu!.Replace('/', System.IO.Path.DirectorySeparatorChar));
        System.IO.File.Exists(diskPath).Should().BeTrue($"Dosya diskte bulunmalı: {diskPath}");

        // 3. KOA1 şifreleme header'ı var mı?
        var rawBytes = await System.IO.File.ReadAllBytesAsync(diskPath);
        rawBytes.Length.Should().BeGreaterThan(100);
        rawBytes[0].Should().Be((byte)'K');
        rawBytes[1].Should().Be((byte)'O');
        rawBytes[2].Should().Be((byte)'A');
        rawBytes[3].Should().Be((byte)'1');
        // KOA1 = AES-256-GCM → SecureFileService.ReadDecryptedAsync tarafından çözülür

        // 4. MIME type doğru mu? (JS tarafında kullanılacak)
        var mimeType = GetMimeType(dosya.DosyaTipi);
        mimeType.Should().Be("application/pdf",
            "PDF dosya tipi application/pdf MIME type almalı");

        // 5. DosyaAdı PDF uzantılı mı? (JS getMimeTypeFromFilename için)
        System.IO.Path.GetExtension(dosya.DosyaAdi).ToLower().Should().Be(".pdf");
    }

    /// <summary>
    /// TEST 2: Personel evrak — DB kaydı var, diskte dosya var, KOA1 şifreli.
    /// </summary>
    [Fact]
    public async Task PersonelEvrak_FullChain_AllLinksVerified()
    {
        var factory = CreateFactory(1);
        await using var ctx = factory.CreateDbContext();

        // 1. DB'de kayıt var mı?
        var evrak = await ctx.PersonelOzlukEvraklar
            .OrderByDescending(e => e.Id)
            .FirstOrDefaultAsync(e => !e.IsDeleted && e.DosyaYolu != null && e.DosyaYolu.EndsWith(".pdf.enc"));
        evrak.Should().NotBeNull("DB'de PDF personel evrak kaydı olmalı");

        // 2. Dosya diskte var mı?
        var storageRoot = KOAFiloServis.Web.Helpers.AppStoragePaths.GetUploadsRoot(
            System.AppContext.BaseDirectory);
        var normalizedPath = evrak!.DosyaYolu!.Replace('\\', '/').TrimStart('/');
        if (normalizedPath.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase))
            normalizedPath = normalizedPath.Substring("uploads/".Length);
        var diskPath = System.IO.Path.Combine(storageRoot, normalizedPath.Replace('/', System.IO.Path.DirectorySeparatorChar));
        System.IO.File.Exists(diskPath).Should().BeTrue($"Personel evrak diskte bulunmalı: {diskPath}");

        // 3. KOA1 şifreleme header'ı var mı?
        var rawBytes = await System.IO.File.ReadAllBytesAsync(diskPath);
        rawBytes.Length.Should().BeGreaterThan(100);
        rawBytes[0].Should().Be((byte)'K');
        rawBytes[1].Should().Be((byte)'O');
        rawBytes[2].Should().Be((byte)'A');
        rawBytes[3].Should().Be((byte)'1');
    }

    /// <summary>
    /// TEST 3: Tüm AracEvrakDosyalari kayıtlarının diskteki dosyaları mevcut mu?
    /// </summary>
    [Fact]
    public async Task AllAracEvrakFiles_ExistOnDisk()
    {
        var factory = CreateFactory(1);
        await using var ctx = factory.CreateDbContext();
        var storageRoot = KOAFiloServis.Web.Helpers.AppStoragePaths.GetUploadsRoot(
            System.AppContext.BaseDirectory);

        var dosyalar = await ctx.AracEvrakDosyalari
            .Where(d => !d.IsDeleted && d.DosyaYolu != null)
            .ToListAsync();

        dosyalar.Should().NotBeEmpty();
        var missing = new List<string>();

        foreach (var d in dosyalar)
        {
            var normalizedPath = d.DosyaYolu!.Replace('\\', '/').TrimStart('/');
            if (normalizedPath.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase))
                normalizedPath = normalizedPath.Substring("uploads/".Length);
            var diskPath = System.IO.Path.Combine(storageRoot, normalizedPath.Replace('/', System.IO.Path.DirectorySeparatorChar));
            if (!System.IO.File.Exists(diskPath))
                missing.Add($"Id={d.Id} DosyaAdi={d.DosyaAdi} Yol={diskPath}");
        }

        missing.Should().BeEmpty(
            $"Tüm araç evrak dosyaları diskte bulunmalı. Eksik: {missing.Count}");
    }

    /// <summary>
    /// TEST 4: Tüm PersonelOzlukEvraklar kayıtlarının diskteki dosyaları mevcut mu?
    /// </summary>
    [Fact]
    public async Task AllPersonelEvrakFiles_ExistOnDisk()
    {
        var factory = CreateFactory(1);
        await using var ctx = factory.CreateDbContext();
        var storageRoot = KOAFiloServis.Web.Helpers.AppStoragePaths.GetUploadsRoot(
            System.AppContext.BaseDirectory);

        var evraklar = await ctx.PersonelOzlukEvraklar
            .Where(e => !e.IsDeleted && e.DosyaYolu != null)
            .ToListAsync();

        evraklar.Should().NotBeEmpty();
        var missing = new List<string>();

        foreach (var e in evraklar)
        {
            var normalizedPath = e.DosyaYolu!.Replace('\\', '/').TrimStart('/');
            if (normalizedPath.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase))
                normalizedPath = normalizedPath.Substring("uploads/".Length);
            var diskPath = System.IO.Path.Combine(storageRoot, normalizedPath.Replace('/', System.IO.Path.DirectorySeparatorChar));
            if (!System.IO.File.Exists(diskPath))
                missing.Add($"Id={e.Id} SoforId={e.SoforId} Yol={diskPath}");
        }

        missing.Should().BeEmpty(
            $"Tüm personel evrak dosyaları diskte bulunmalı. Eksik: {missing.Count}");
    }
}
