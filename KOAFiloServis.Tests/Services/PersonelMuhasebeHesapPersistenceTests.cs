using FluentAssertions;
using KOAFiloServis.Shared.Entities;
using KOAFiloServis.Web.Data;
using KOAFiloServis.Web.Services;
using KOAFiloServis.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace KOAFiloServis.Tests.Services;

public class PersonelMuhasebeHesapPersistenceTests
{
    private static string DbName() => $"personel-muhasebe-{Guid.NewGuid():N}";

    private static IDbContextFactory<ApplicationDbContext> CreateFactory(string dbName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite($"Data Source={dbName}.db")
            .Options;

        using var setupContext = new ApplicationDbContext(options);
        setupContext.Database.EnsureDeleted();
        setupContext.Database.EnsureCreated();

        // Audit log FirmaId FK'si için test taban verisini raw SQL ile oluştur
        setupContext.Database.ExecuteSqlRaw("INSERT INTO \"Organizasyonlar\" (\"Id\", \"Adi\", \"Kod\", \"Aciklama\", \"CreatedAt\", \"UpdatedAt\", \"DeletedAt\", \"IsDeleted\") VALUES (1, 'Test Organizasyon', 'TESTORG', NULL, CURRENT_TIMESTAMP, NULL, NULL, 0)");
        setupContext.Database.ExecuteSqlRaw("INSERT INTO \"Firmalar\" (\"Id\", \"FirmaKodu\", \"FirmaAdi\", \"UnvanTam\", \"VergiNo\", \"VergiDairesi\", \"Adres\", \"Il\", \"Ilce\", \"Telefon\", \"Email\", \"WebSite\", \"Logo\", \"Aktif\", \"VarsayilanFirma\", \"SiraNo\", \"OrganizasyonId\", \"CariId\", \"AktifDonemYil\", \"AktifDonemAy\", \"DatabaseName\", \"CreatedAt\", \"UpdatedAt\", \"DeletedAt\", \"IsDeleted\") VALUES (1, 'TST-FRM', 'Test Firma', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 1, 0, 0, 1, NULL, 2026, 1, NULL, CURRENT_TIMESTAMP, NULL, NULL, 0)");

        var services = new ServiceCollection();
        services.AddSingleton<IAktifFirmaProvider>(new TestAktifFirmaProvider(1));
        var sp = services.BuildServiceProvider();

        return new ScopedDbContextFactory(options, sp);
    }

    private static SoforService CreateService(IDbContextFactory<ApplicationDbContext> factory)
    {
        var muhasebeMock = new Mock<IMuhasebeService>();
        var cacheMock = new Mock<ICacheService>();
        cacheMock.Setup(c => c.RemoveByPrefixAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var numaraSerisi = new NumaraSerisiService(factory);
        return new SoforService(factory, muhasebeMock.Object, cacheMock.Object, numaraSerisi, Mock.Of<IMaasSnapshotService>());
    }

    [Fact]
    public async Task Create_Persists_335_To_MuhasebeHesapId_And_195_To_PersonelAvansHesapId()
    {
        var dbName = DbName();
        var factory = CreateFactory(dbName);
        var service = CreateService(factory);

        var personel = new Sofor
        {
            SoforKodu = "SFR-T1",
            Ad = "Test",
            Soyad = "Personel",
            Aktif = true,
            Gorev = PersonelGorev.Sofor,
            FirmaId = 1
        };

        var created = await service.CreateAsync(personel);

        await using var verifyContext = await factory.CreateDbContextAsync();
        var dbPersonel = await verifyContext.Soforler
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == created.Id);

        dbPersonel.Should().NotBeNull();
        dbPersonel!.MuhasebeHesapId.Should().NotBeNull();
        dbPersonel.PersonelAvansHesapId.Should().NotBeNull();

        var hesap335 = await verifyContext.MuhasebeHesaplari
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.Id == dbPersonel.MuhasebeHesapId);
        var hesap195 = await verifyContext.MuhasebeHesaplari
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.Id == dbPersonel.PersonelAvansHesapId);

        hesap335.Should().NotBeNull();
        hesap195.Should().NotBeNull();
        hesap335!.HesapKodu.Should().StartWith("335.01.");
        hesap195!.HesapKodu.Should().StartWith("195.01.");
    }

    [Fact]
    public async Task SaveTwice_DoesNotCreate_New_335_195_Accounts_And_Keeps_Fks()
    {
        var dbName = DbName();
        var factory = CreateFactory(dbName);
        var service = CreateService(factory);

        var personel = new Sofor
        {
            SoforKodu = "SFR-T2",
            Ad = "Tekrar",
            Soyad = "Kaydet",
            Aktif = true,
            Gorev = PersonelGorev.Sofor,
            FirmaId = 1
        };

        var created = await service.CreateAsync(personel);

        await using var ctx1 = await factory.CreateDbContextAsync();
        var first = await ctx1.Soforler.AsNoTracking().FirstAsync(s => s.Id == created.Id);
        var first335 = first.MuhasebeHesapId;
        var first195 = first.PersonelAvansHesapId;
        var hesapSayisiBefore = await ctx1.MuhasebeHesaplari.AsNoTracking().CountAsync();

        var forUpdate = await service.GetByIdAsync(created.Id);
        forUpdate.Should().NotBeNull();
        forUpdate!.Telefon = "05001112233";
        await service.UpdateAsync(forUpdate);

        await using var ctx2 = await factory.CreateDbContextAsync();
        var second = await ctx2.Soforler.AsNoTracking().FirstAsync(s => s.Id == created.Id);
        var hesapSayisiAfter = await ctx2.MuhasebeHesaplari.AsNoTracking().CountAsync();

        second.MuhasebeHesapId.Should().Be(first335);
        second.PersonelAvansHesapId.Should().Be(first195);
        hesapSayisiAfter.Should().Be(hesapSayisiBefore);
    }

    [Fact]
    public async Task SoftDelete_DoesNotDelete_335_195_Accounts()
    {
        var dbName = DbName();
        var factory = CreateFactory(dbName);
        var service = CreateService(factory);

        var personel = new Sofor
        {
            SoforKodu = "SFR-T3",
            Ad = "Silinen",
            Soyad = "Personel",
            Aktif = true,
            Gorev = PersonelGorev.Sofor,
            FirmaId = 1
        };

        var created = await service.CreateAsync(personel);

        await using var ctxBefore = await factory.CreateDbContextAsync();
        var before = await ctxBefore.Soforler.AsNoTracking().FirstAsync(s => s.Id == created.Id);
        var hesap335Id = before.MuhasebeHesapId;
        var hesap195Id = before.PersonelAvansHesapId;

        await service.DeleteAsync(created.Id);

        await using var ctxAfter = await factory.CreateDbContextAsync();
        var deletedPersonel = await ctxAfter.Soforler.IgnoreQueryFilters().AsNoTracking().FirstAsync(s => s.Id == created.Id);
        deletedPersonel.IsDeleted.Should().BeTrue();

        var hesap335 = await ctxAfter.MuhasebeHesaplari.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(h => h.Id == hesap335Id);
        var hesap195 = await ctxAfter.MuhasebeHesaplari.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(h => h.Id == hesap195Id);

        hesap335.Should().NotBeNull();
        hesap195.Should().NotBeNull();
        hesap335!.IsDeleted.Should().BeFalse();
        hesap195!.IsDeleted.Should().BeFalse();
    }

    private sealed class TestAktifFirmaProvider : IAktifFirmaProvider
    {
        public TestAktifFirmaProvider(int firmaId)
        {
            Mevcut = new AktifFirmaBilgisi { FirmaId = firmaId, TumFirmalar = false };
        }

        public int? AktifFirmaId => Mevcut.FirmaId > 0 ? Mevcut.FirmaId : null;
        public bool HasAktifFirma => AktifFirmaId.HasValue || TumFirmalar;
        public bool TumFirmalar => Mevcut.TumFirmalar;
        public AktifFirmaBilgisi Mevcut { get; private set; }
        public event Action? AktifFirmaDegisti;

        public void Set(AktifFirmaBilgisi firma)
        {
            Mevcut = firma;
            AktifFirmaDegisti?.Invoke();
        }

        public void SetTumFirmalar(bool tumFirmalar)
        {
            Mevcut.TumFirmalar = tumFirmalar;
            AktifFirmaDegisti?.Invoke();
        }

        public void SetDonem(int yil, int ay)
        {
            Mevcut.AktifDonemYil = yil;
            Mevcut.AktifDonemAy = ay;
            AktifFirmaDegisti?.Invoke();
        }

        public Task<bool> TryRestoreAsync() => Task.FromResult(false);
    }
}
