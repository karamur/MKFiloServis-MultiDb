using KOAFiloServis.Shared.Entities;
using KOAFiloServis.Web.Services;

namespace KOAFiloServis.Tests.Services;

public class TenantDatabaseServiceTests
{
    // ── BuildTenantDbName ──────────────────────────────────────────────────

    [Fact]
    public void BuildTenantDbName_StandartKod_DogruFormat()
    {
        var firma = new Firma { Id = 1, FirmaKodu = "KOA", FirmaAdi = "Test" };
        var result = TenantDatabaseService.BuildTenantDbName(firma);
        Assert.Equal("Koa_KOA_001", result);
    }

    [Fact]
    public void BuildTenantDbName_TurkcKarakter_Donusturulur()
    {
        var firma = new Firma { Id = 5, FirmaKodu = "ŞEKER", FirmaAdi = "Test" };
        var result = TenantDatabaseService.BuildTenantDbName(firma);
        Assert.Equal("Koa_SEKER_005", result);
    }

    [Fact]
    public void BuildTenantDbName_TumTurkcKarakterler_Donusturulur()
    {
        var firma = new Firma { Id = 10, FirmaKodu = "ÜĞİŞÇÖ", FirmaAdi = "Test" };
        var result = TenantDatabaseService.BuildTenantDbName(firma);
        Assert.Equal("Koa_UGISCO_010", result);
    }

    [Fact]
    public void BuildTenantDbName_BosKod_FirmaIdKullanilir()
    {
        var firma = new Firma { Id = 42, FirmaKodu = null!, FirmaAdi = "Test" };
        var result = TenantDatabaseService.BuildTenantDbName(firma);
        Assert.Equal("Koa_F42_042", result);
    }

    [Fact]
    public void BuildTenantDbName_BoslukluKod_AltCizgiyeDonusur()
    {
        var firma = new Firma { Id = 3, FirmaKodu = "KOA TRANS", FirmaAdi = "Test" };
        var result = TenantDatabaseService.BuildTenantDbName(firma);
        Assert.Equal("Koa_KOA_TRANS_003", result);
    }

    [Fact]
    public void BuildTenantDbName_UcHaneliId_SifirBaslar()
    {
        var firma = new Firma { Id = 7, FirmaKodu = "ABC", FirmaAdi = "Test" };
        var result = TenantDatabaseService.BuildTenantDbName(firma);
        Assert.Equal("Koa_ABC_007", result);
    }

    [Fact]
    public void BuildTenantDbName_BuyukId_DogruFormatlanir()
    {
        var firma = new Firma { Id = 123, FirmaKodu = "ABC", FirmaAdi = "Test" };
        var result = TenantDatabaseService.BuildTenantDbName(firma);
        Assert.Equal("Koa_ABC_123", result);
    }

    [Fact]
    public void BuildTenantDbName_UzunKod_50KarakterdeLimitlenir()
    {
        var uzunKod = new string('A', 60);
        var firma = new Firma { Id = 1, FirmaKodu = uzunKod, FirmaAdi = "Test" };
        var result = TenantDatabaseService.BuildTenantDbName(firma);
        // Kod kismi 50 karakterde kesilir
        Assert.True(result.Length <= 65); // "Koa_" + 50 + "_001"
        Assert.StartsWith("Koa_", result);
        Assert.EndsWith("_001", result);
    }

    [Fact]
    public void BuildTenantDbName_OzelKarakter_Temizlenir()
    {
        var firma = new Firma { Id = 2, FirmaKodu = "KOA@#$LTD", FirmaAdi = "Test" };
        var result = TenantDatabaseService.BuildTenantDbName(firma);
        Assert.Equal("Koa_KOALTD_002", result);
    }

    [Fact]
    public void BuildTenantDbName_SadeceOzelKarakter_FirmaDonerDefault()
    {
        var firma = new Firma { Id = 9, FirmaKodu = "@@@", FirmaAdi = "Test" };
        var result = TenantDatabaseService.BuildTenantDbName(firma);
        Assert.Equal("Koa_FIRMA_009", result);
    }

    [Fact]
    public void BuildTenantDbName_KucukHarf_BuyukHaraDonuturulur()
    {
        var firma = new Firma { Id = 4, FirmaKodu = "koa", FirmaAdi = "Test" };
        var result = TenantDatabaseService.BuildTenantDbName(firma);
        Assert.Equal("Koa_KOA_004", result);
    }

    // ── Format dogrulama ──────────────────────────────────────────────────

    [Theory]
    [InlineData("F001", 1, "Koa_F001_001")]
    [InlineData("TEST", 99, "Koa_TEST_099")]
    [InlineData("ABCDE", 999, "Koa_ABCDE_999")]
    public void BuildTenantDbName_CesitliKodlar_BeklenenSonuc(
        string kod, int id, string beklenen)
    {
        var firma = new Firma { Id = id, FirmaKodu = kod, FirmaAdi = "Test" };
        var result = TenantDatabaseService.BuildTenantDbName(firma);
        Assert.Equal(beklenen, result);
    }
}
