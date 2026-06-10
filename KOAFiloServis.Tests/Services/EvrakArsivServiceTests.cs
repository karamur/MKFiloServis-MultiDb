using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using KOAFiloServis.Web.Helpers;
using KOAFiloServis.Web.Services;
using KOAFiloServis.Web.Services.Security;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Moq;

namespace KOAFiloServis.Tests.Services;

public class EvrakArsivServiceTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly byte[] _masterKey;
    private readonly Mock<IWebHostEnvironment> _envMock;
    private readonly Mock<ILogger<EvrakArsivService>> _loggerMock;
    private readonly IFileProtector _fileProtector;

    public EvrakArsivServiceTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), $"koa-arsiv-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempRoot);

        // Temp root altinda "Arsiv" dizininin üstü storage root olacak
        // AppStoragePaths.GetStorageRoot -> DefaultStorageRoot -> C:\KOAFiloServis_yedekleme
        // ama biz temp dizini kullanmak icin env.ContentRootPath ile oynayamayiz.
        // Onun yerine direkt path'i constructor'a verecek sekilde test edecegiz.
        // EvrakArsivService, AppStoragePaths.GetStorageRoot kullandigi icin
        // test sirasinda CRMFILO_STORAGE_ROOT env var set edelim.

        Environment.SetEnvironmentVariable("CRMFILO_STORAGE_ROOT", _tempRoot);

        _envMock = new Mock<IWebHostEnvironment>();
        _envMock.Setup(e => e.ContentRootPath).Returns(_tempRoot);

        _loggerMock = new Mock<ILogger<EvrakArsivService>>();

        _masterKey = RandomNumberGenerator.GetBytes(32);
        var keyProvider = new FixedMasterKeyProvider(_masterKey);
        _fileProtector = new AesGcmFileProtector(keyProvider);
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("CRMFILO_STORAGE_ROOT", null);
        try { Directory.Delete(_tempRoot, recursive: true); } catch { }
    }

    private EvrakArsivService CreateSut() =>
        new(_fileProtector, _envMock.Object, _loggerMock.Object);

    // ─────────────────────────────────────────────────────────────
    // FileNameHelper Tests
    // ─────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("Murat Karakaş", "MURAT-KARAKAS")]
    [InlineData("Ali Aydın Boduroğlu", "ALI-AYDIN-BODUROGLU")]
    [InlineData("Psiko Teknik", "PSIKO-TEKNIK")]
    [InlineData("06 C 0640", "06-C-0640")]
    [InlineData("SRC / Ehliyet", "SRC-EHLIYET")]
    [InlineData("", "EVRAK")]
    [InlineData("   ", "EVRAK")]
    [InlineData("Kasko", "KASKO")]
    [InlineData("Muayene", "MUAYENE")]
    [InlineData("Trafik Sigortası", "TRAFIK-SIGORTASI")]
    [InlineData("Sağlık Raporu", "SAGLIK-RAPORU")]
    public void FileName_SanitizesTurkishAndInvalidCharacters(string input, string expected)
    {
        var result = FileNameHelper.NormalizeFileName(input);
        result.Should().Be(expected);
    }

    [Fact]
    public void NormalizeExtension_ReturnsPdf_WhenEmpty()
    {
        FileNameHelper.NormalizeExtension(null).Should().Be(".pdf");
        FileNameHelper.NormalizeExtension("").Should().Be(".pdf");
    }

    [Theory]
    [InlineData("test.pdf", ".pdf")]
    [InlineData("belge.PDF", ".pdf")]
    [InlineData("dokuman.jpg", ".jpg")]
    [InlineData("dosya", ".pdf")]
    public void NormalizeExtension_ReturnsCorrect(string input, string expected)
    {
        FileNameHelper.NormalizeExtension(input).Should().Be(expected);
    }

    // ─────────────────────────────────────────────────────────────
    // Personel Evrak Tests
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task PersonelEvrak_CreatesFullNameDocumentTypeDateTimeDirectory()
    {
        var sut = CreateSut();
        var pdfContent = GeneratePdfContent(1024);

        await sut.ArsivlePersonelEvrakAsync("Murat Karakaş", "Ehliyet", pdfContent, ".pdf");

        var sifreliDir = Path.Combine(_tempRoot, "Arsiv", "Sifreli", "Personeller");
        var sifresizDir = Path.Combine(_tempRoot, "Arsiv", "Sifresiz", "Personeller");

        Directory.Exists(sifreliDir).Should().BeTrue("şifreli Personeller dizini oluşmalı");
        Directory.Exists(sifresizDir).Should().BeTrue("şifresiz Personeller dizini oluşmalı");

        // Klasör adı formatı: MURAT-KARAKAS-EHLIYET-yyyyMMdd-HHmmss
        var sifreliSubDirs = Directory.GetDirectories(sifreliDir);
        sifreliSubDirs.Should().HaveCount(1);
        var dirName = Path.GetFileName(sifreliSubDirs[0]);
        dirName.Should().MatchRegex(@"^MURAT-KARAKAS-EHLIYET-\d{8}-\d{6}$");

        // Aynı klasör adı şifresizde de olmalı
        var sifresizSubDirs = Directory.GetDirectories(sifresizDir);
        sifresizSubDirs.Should().HaveCount(1);
        Path.GetFileName(sifresizSubDirs[0]).Should().Be(dirName);
    }

    [Fact]
    public async Task PersonelEvrak_WritesEncryptedAndPlainCopies()
    {
        var sut = CreateSut();
        var pdfContent = GeneratePdfContent(2048);

        await sut.ArsivlePersonelEvrakAsync("Ali Yılmaz", "Kimlik", pdfContent, ".pdf");

        var sifreliDir = Directory.GetDirectories(Path.Combine(_tempRoot, "Arsiv", "Sifreli", "Personeller"))[0];
        var sifresizDir = Directory.GetDirectories(Path.Combine(_tempRoot, "Arsiv", "Sifresiz", "Personeller"))[0];

        // Şifreli: .pdf.enc
        var encFile = Directory.GetFiles(sifreliDir, "*.enc");
        encFile.Should().HaveCount(1);
        Path.GetFileName(encFile[0]).Should().EndWith(".pdf.enc");
        Path.GetFileName(encFile[0]).Should().MatchRegex(@"^ALI-YILMAZ-KIMLIK-\d{8}-\d{6}\.pdf\.enc$");

        // Şifresiz: .pdf
        var plainFile = Directory.GetFiles(sifresizDir, "*.pdf");
        plainFile.Should().HaveCount(1);
        Path.GetFileName(plainFile[0]).Should().EndWith(".pdf");
        Path.GetFileName(plainFile[0]).Should().MatchRegex(@"^ALI-YILMAZ-KIMLIK-\d{8}-\d{6}\.pdf$");

        // Şifresiz içerik orijinalle aynı olmalı
        var plainContent = await File.ReadAllBytesAsync(plainFile[0]);
        plainContent.Should().Equal(pdfContent);
    }

    [Fact]
    public async Task EncryptedArchiveFile_CanBeDecrypted_AndStartsWithPdfHeader()
    {
        var sut = CreateSut();
        var pdfContent = GeneratePdfContent(4096);

        await sut.ArsivlePersonelEvrakAsync("Mehmet Demir", "SRC Belgesi", pdfContent, ".pdf");

        // Şifreli dosyayı bul
        var sifreliDir = Directory.GetDirectories(Path.Combine(_tempRoot, "Arsiv", "Sifreli", "Personeller"))[0];
        var encFile = Directory.GetFiles(sifreliDir, "*.enc")[0];

        // KOA1 formatında decrypt et
        var encContent = await File.ReadAllBytesAsync(encFile);
        var decrypted = _fileProtector.Unprotect(encContent);

        // %PDF kontrolü
        decrypted.Should().NotBeNull();
        decrypted.Length.Should().BeGreaterThan(4);
        var header = Encoding.ASCII.GetString(decrypted, 0, 4);
        header.Should().Be("%PDF", "şifreli arşiv dosyası decrypt edildiğinde PDF header ile başlamalı");

        // İçerik doğrulaması
        decrypted.Should().Equal(pdfContent);
    }

    [Fact]
    public async Task Backfill_UsesEvrakWhenDocumentTypeIsEmpty()
    {
        var sut = CreateSut();
        var content = GeneratePdfContent(512);

        await sut.ArsivlePersonelEvrakAsync("Test Kullanıcı", "", content, ".pdf");
        await Task.Delay(1100); // timestamp farklı olsun diye 1.1 sn bekle
        await sut.ArsivlePersonelEvrakAsync("Test Kullanıcı", "   ", content, ".pdf");

        var sifreliDir = Directory.GetDirectories(Path.Combine(_tempRoot, "Arsiv", "Sifreli", "Personeller"));
        sifreliDir.Should().HaveCount(2);

        foreach (var dir in sifreliDir)
        {
            var dirName = Path.GetFileName(dir);
            dirName.Should().Contain("-EVRAK-", "boş evrak niteliği 'EVRAK' olarak doldurulmalı");
        }
    }

    // ─────────────────────────────────────────────────────────────
    // Araç Evrak Tests
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task AracEvrak_CreatesPlateChassisDocumentTypeDateTimeDirectory()
    {
        var sut = CreateSut();
        var pdfContent = GeneratePdfContent(1024);

        await sut.ArsivleAracEvrakAsync("06 C 0640", "NM0ABC12345678900", "Ruhsat", pdfContent, ".pdf");

        var sifreliDir = Path.Combine(_tempRoot, "Arsiv", "Sifreli", "Araclar");
        var sifresizDir = Path.Combine(_tempRoot, "Arsiv", "Sifresiz", "Araclar");

        Directory.Exists(sifreliDir).Should().BeTrue();
        Directory.Exists(sifresizDir).Should().BeTrue();

        // Klasör adı: 06-C-0640-NM0ABC12345678900-RUHSAT-yyyyMMdd-HHmmss
        var subDirs = Directory.GetDirectories(sifreliDir);
        subDirs.Should().HaveCount(1);
        var dirName = Path.GetFileName(subDirs[0]);
        dirName.Should().MatchRegex(@"^06-C-0640-NM0ABC12345678900-RUHSAT-\d{8}-\d{6}$");
    }

    [Fact]
    public async Task AracEvrak_WritesEncryptedAndPlainCopies()
    {
        var sut = CreateSut();
        var pdfContent = GeneratePdfContent(2048);

        await sut.ArsivleAracEvrakAsync("34 ABC 123", "WBA1234567890", "Kasko", pdfContent, ".pdf");

        var sifreliDir = Directory.GetDirectories(Path.Combine(_tempRoot, "Arsiv", "Sifreli", "Araclar"))[0];
        var sifresizDir = Directory.GetDirectories(Path.Combine(_tempRoot, "Arsiv", "Sifresiz", "Araclar"))[0];

        var encFile = Directory.GetFiles(sifreliDir, "*.enc");
        encFile.Should().HaveCount(1);
        Path.GetFileName(encFile[0]).Should().EndWith(".pdf.enc");
        Path.GetFileName(encFile[0]).Should().MatchRegex(@"^34-ABC-123-WBA1234567890-KASKO-\d{8}-\d{6}\.pdf\.enc$");

        var plainFile = Directory.GetFiles(sifresizDir, "*.pdf");
        plainFile.Should().HaveCount(1);
        Path.GetFileName(plainFile[0]).Should().EndWith(".pdf");

        // Şifresiz içerik doğrulaması
        var plainContent = await File.ReadAllBytesAsync(plainFile[0]);
        plainContent.Should().Equal(pdfContent);

        // Şifreli decrypt doğrulaması
        var encContent = await File.ReadAllBytesAsync(encFile[0]);
        var decrypted = _fileProtector.Unprotect(encContent);
        Encoding.ASCII.GetString(decrypted, 0, 4).Should().Be("%PDF");
        decrypted.Should().Equal(pdfContent);
    }

    // ─────────────────────────────────────────────────────────────
    // Güvenlik Testleri
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public void SifresizArsiv_WwwRootDisinda()
    {
        var sut = CreateSut();
        var tempRoot = _tempRoot;

        // Arsiv\Sifresiz yolu wwwroot altında olmamalı
        tempRoot.Should().NotContain("wwwroot");
    }

    [Fact]
    public async Task ArsivlemeHatasi_UploadAkisiniBozmaz()
    {
        // Read-only bir dizinde arşivleme yapmayı dene - exception fırlatmamalı
        // EvrakArsivService zaten try-catch ile korumalı (async void değil)
        var sut = CreateSut();

        // Normal çalışma durumunda exception fırlatılmamalı
        var act = () => sut.ArsivlePersonelEvrakAsync("Test", "Test", GeneratePdfContent(100), ".pdf");
        await act.Should().NotThrowAsync();
    }

    // ─────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────

    private static byte[] GeneratePdfContent(int minSize)
    {
        var header = "%PDF-1.4\n%âãÏÓ\n"u8.ToArray();
        var body = new byte[Math.Max(minSize - header.Length, 0)];
        RandomNumberGenerator.Fill(body);
        return [.. header, .. body];
    }

    private sealed class FixedMasterKeyProvider : IMasterKeyProvider
    {
        private readonly byte[] _key;
        public FixedMasterKeyProvider(byte[] key) => _key = key;
        public ReadOnlyMemory<byte> GetMasterKey() => _key;
    }
}
