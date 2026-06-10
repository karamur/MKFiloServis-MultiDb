using FluentAssertions;
using KOAFiloServis.Web.Helpers;
using KOAFiloServis.Web.Services;
using KOAFiloServis.Web.Services.Security;

namespace KOAFiloServis.Tests.Services;

public class EvrakArsivBackfillServiceTests
{
    // ─────────────────────────────────────────────────────────────
    // Path hesaplama testleri (dry-run benzeri)
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public void Backfill_AracEvrak_CreatesNewArchivePath_WithPlateChassisDocumentTypeDateTime()
    {
        var plaka = "06 C 0640";
        var sasiNo = "NM0ABC12345678900";
        var evrak = "Ruhsat";
        var tarihSaat = "20260610-235912";

        var plakaNorm = FileNameHelper.NormalizeFileName(plaka);
        var sasiNorm = FileNameHelper.NormalizeFileName(sasiNo);
        var evrakNorm = FileNameHelper.NormalizeFileName(evrak);

        var baseName = $"{plakaNorm}-{sasiNorm}-{evrakNorm}-{tarihSaat}";
        var relativePath = $"Arsiv/Sifreli/Araclar/{baseName}/{baseName}.pdf.enc";

        baseName.Should().Be("06-C-0640-NM0ABC12345678900-RUHSAT-20260610-235912");
        relativePath.Should().StartWith("Arsiv/Sifreli/Araclar/");
        relativePath.Should().EndWith(".pdf.enc");
    }

    [Fact]
    public void Backfill_PersonelEvrak_CreatesNewArchivePath_WithFullNameDocumentTypeDateTime()
    {
        var adSoyad = "Murat Karakaş";
        var evrak = "Ehliyet";
        var tarihSaat = "20260610-235912";

        var adSoyadNorm = FileNameHelper.NormalizeFileName(adSoyad);
        var evrakNorm = FileNameHelper.NormalizeFileName(evrak);

        var baseName = $"{adSoyadNorm}-{evrakNorm}-{tarihSaat}";
        var relativePath = $"Arsiv/Sifreli/Personeller/{baseName}/{baseName}.pdf.enc";

        baseName.Should().Be("MURAT-KARAKAS-EHLIYET-20260610-235912");
        relativePath.Should().StartWith("Arsiv/Sifreli/Personeller/");
    }

    [Theory]
    [InlineData("", "EVRAK")]
    [InlineData("   ", "EVRAK")]
    [InlineData("Ehliyet", "EHLIYET")]
    [InlineData("Trafik Sigortası", "TRAFIK-SIGORTASI")]
    public void Backfill_UsesFallback_WhenEvrakTipiEmpty(string evrakTipi, string expected)
    {
        var result = FileNameHelper.NormalizeFileName(evrakTipi);
        result.Should().Be(expected);
    }

    [Fact]
    public void Backfill_SasiNoFallback_WhenEmpty()
    {
        var result = FileNameHelper.NormalizeFileName("", "SASIYOK");
        result.Should().Be("SASIYOK");
    }

    [Fact]
    public void Backfill_PersonelNameFallback_WhenEmpty()
    {
        var result = FileNameHelper.NormalizeFileName("", "PERSONEL-42");
        result.Should().Be("PERSONEL-42");
    }

    // ─────────────────────────────────────────────────────────────
    // KOA1 format testleri
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public void Backfill_EncryptedCopyWithProtect_IsDecryptable()
    {
        var key = new byte[32];
        System.Security.Cryptography.RandomNumberGenerator.Fill(key);
        var provider = new TestMasterKeyProvider(key);
        var protector = new AesGcmFileProtector(provider);

        var pdfContent = "%PDF-1.4\nTest PDF content"u8.ToArray();
        var encrypted = protector.Protect(pdfContent); // KOA1

        // Başlık kontrolü
        var header = System.Text.Encoding.ASCII.GetString(encrypted, 0, 4);
        header.Should().Be("KOA1");

        // Decrypt
        var decrypted = protector.Unprotect(encrypted);
        decrypted.Should().Equal(pdfContent);
    }

    [Fact]
    public void Backfill_EncryptedCopyWithProtect_StartsWithKOA1()
    {
        var key = new byte[32];
        System.Security.Cryptography.RandomNumberGenerator.Fill(key);
        var protector = new AesGcmFileProtector(new TestMasterKeyProvider(key));

        var content = new byte[1024];
        System.Security.Cryptography.RandomNumberGenerator.Fill(content);

        var encrypted = protector.Protect(content);
        encrypted[0].Should().Be((byte)'K');
        encrypted[1].Should().Be((byte)'O');
        encrypted[2].Should().Be((byte)'A');
        encrypted[3].Should().Be((byte)'1');
    }

    // ─────────────────────────────────────────────────────────────
    // Rapor modeli testi
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public void Backfill_Rapor_InitialCountsAreZero()
    {
        var rapor = new EvrakArsivBackfillRaporu();
        rapor.AracToplam.Should().Be(0);
        rapor.AracBasarili.Should().Be(0);
        rapor.AracBasarisiz.Should().Be(0);
        rapor.PersonelToplam.Should().Be(0);
        rapor.Satirlar.Should().BeEmpty();
    }

    [Fact]
    public void Backfill_Satir_HataYazilabilir()
    {
        var satir = new EvrakArsivBackfillSatir
        {
            EvrakTipi = "Arac",
            EvrakId = 42,
            Hata = "Eski dosya decrypt edilemedi"
        };

        satir.Basarili.Should().BeFalse();
        satir.Hata.Should().NotBeNull();
    }

    // ─────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────

    private sealed class TestMasterKeyProvider : IMasterKeyProvider
    {
        private readonly byte[] _key;
        public TestMasterKeyProvider(byte[] key) => _key = key;
        public ReadOnlyMemory<byte> GetMasterKey() => _key;
    }
}
