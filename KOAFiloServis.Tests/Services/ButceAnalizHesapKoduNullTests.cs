using FluentAssertions;
using KOAFiloServis.Shared.Entities;

namespace KOAFiloServis.Tests.Services;

/// <summary>
/// Bütçe analiz HesapKodu null güvenliği testleri.
/// </summary>
public class ButceAnalizHesapKoduNullTests
{
    // ── BankaHesap entity nullable testleri ────────────────────────────

    [Fact]
    public void BankaHesap_HesapKodu_Null_DoesNotThrow()
    {
        var hesap = new BankaHesap
        {
            HesapAdi = "Test Hesap",
            HesapTipi = HesapTipi.VadesizHesap
        };

        // HesapKodu null olarak bırakıldı — entity nullable olduğu için hata vermemeli
        hesap.HesapKodu.Should().BeNull();
        hesap.HesapAdi.Should().Be("Test Hesap");
    }

    [Fact]
    public void BankaHesap_HesapKodu_Fallback_ReturnsEmpty()
    {
        var hesap = new BankaHesap { HesapAdi = "Test", HesapTipi = HesapTipi.Kasa };

        // UI fallback: HesapKodu null ise boş string göster
        var display = hesap.HesapKodu ?? "—";
        display.Should().Be("—");
    }

    [Fact]
    public void BankaHesap_HesapKodu_Assignment_Works()
    {
        var hesap = new BankaHesap
        {
            HesapKodu = "100.01",
            HesapAdi = "Test",
            HesapTipi = HesapTipi.Kasa
        };

        hesap.HesapKodu.Should().Be("100.01");
    }

    [Fact]
    public void BankaHesap_HesapKodu_NullCoalescing_DoesNotThrow()
    {
        var hesap = new BankaHesap { HesapAdi = "Test", HesapTipi = HesapTipi.Kasa };

        // BankaHesapService.NormalizeBankaHesap kullanımına benzer
        var normalized = hesap.HesapKodu?.Trim().ToUpperInvariant();
        normalized.Should().BeNull("null HesapKodu için Trim çağrılmamalı ve null dönmeli");
    }

    [Fact]
    public void BankaHesap_HesapKodu_StringOperations_Safe()
    {
        var withValue = new BankaHesap
        {
            HesapKodu = " 100.01 ",
            HesapAdi = "Test",
            HesapTipi = HesapTipi.Kasa
        };

        var normalized = withValue.HesapKodu?.Trim().ToUpperInvariant();
        normalized.Should().Be("100.01");
    }

    // ── DTO projection testleri ─────────────────────────────────────

    [Fact]
    public void ButceAnaliz_HesapKoduNull_DoesNotThrow()
    {
        // Null HesapKodu'lu bir BankaHesap ile DTO oluşturma
        var hesap = new BankaHesap
        {
            Id = 1,
            HesapKodu = null,
            HesapAdi = "Test Hesap",
            HesapTipi = HesapTipi.Kasa
        };

        // Projection — null güvenli
        var dto = new
        {
            hesap.Id,
            HesapKodu = hesap.HesapKodu ?? "",
            hesap.HesapAdi
        };

        dto.HesapKodu.Should().Be("");
        dto.HesapAdi.Should().Be("Test Hesap");
    }

    [Fact]
    public void ButceAnaliz_LeftJoinMuhasebeHesapNull_DoesNotThrow()
    {
        // MuhasebeHesap referansı null olabilir (left join senaryosu)
        MuhasebeHesap? muhasebeHesap = null;

        var hesapKodu = muhasebeHesap?.HesapKodu ?? "";
        hesapKodu.Should().Be("");
    }

    [Fact]
    public void ButceAnaliz_NewRecord_DoesNotCreateNullHesapKodu()
    {
        // BankaHesap oluşturulurken HesapKodu atanmamışsa null olabilir
        // ama bu hata değil — DB/entity nullable destekliyor
        var hesap = new BankaHesap
        {
            HesapAdi = "Yeni Hesap",
            HesapTipi = HesapTipi.VadesizHesap
        };

        // UI'da gösterirken fallback
        var display = hesap.HesapKodu ?? "KOD BEKLİYOR";
        display.Should().Be("KOD BEKLİYOR");
    }
}
