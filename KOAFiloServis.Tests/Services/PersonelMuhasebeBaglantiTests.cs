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
/// Personel 335/195 muhasebe bağlantısı izole persistence testi.
/// ExecuteUpdateAsync ile doğrudan DB yazma yöntemini kanıtlar.
/// </summary>
public class PersonelMuhasebeBaglantiTests
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

    private SoforService CreateSoforService(int firmaId = 1)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IAktifFirmaProvider>(new TestAktifFirmaProvider(firmaId));
        var sp = services.BuildServiceProvider();
        var factory = new TestDbContextFactory(sp);
        var numara = new NumaraSerisiService(factory);
        return new SoforService(factory, Mock.Of<IMuhasebeService>(), Mock.Of<ICacheService>(), numara, Mock.Of<IMaasSnapshotService>());
    }

    [Fact]
    public async Task UpdatePersonel_WithNullFkModel_DoesNotClear335195Links()
    {
        // Arrange: Var olan bir personel al (Id=5 veya herhangi bir aktif)
        await using var ctx = new TestDbContextFactory(
            new ServiceCollection().AddSingleton<IAktifFirmaProvider>(new TestAktifFirmaProvider(1)).BuildServiceProvider())
            .CreateDbContext();

        var personel = await ctx.Soforler
            .FirstOrDefaultAsync(s => !s.IsDeleted && s.Aktif && s.FirmaId == 1 && s.Id == 5);
        personel.Should().NotBeNull("PersonelId=5 DB'de bulunmalı");

        // Kaydetmeden önceki durumu hatırla
        var onceki335 = personel!.MuhasebeHesapId;
        var onceki195 = personel.PersonelAvansHesapId;

        // Act: UI'dan gelen model — MuhasebeHesapId ve PersonelAvansHesapId null
        var svc = CreateSoforService(1);
        personel.MuhasebeHesapId = null;   // UI modelinde null
        personel.PersonelAvansHesapId = null; // UI modelinde null

        var updated = await svc.UpdateAsync(personel);

        // Assert: Update dönen entity'de FK'lar dolu olmalı
        updated.MuhasebeHesapId.Should().NotBeNull("UpdateAsync 335 bağlantısını garanti etmeli");
        updated.PersonelAvansHesapId.Should().NotBeNull("UpdateAsync 195 bağlantısını garanti etmeli");

        // Assert: DB'den tekrar oku — FK'lar gerçekten yazılmış mı?
        await using var ctx2 = new TestDbContextFactory(
            new ServiceCollection().AddSingleton<IAktifFirmaProvider>(new TestAktifFirmaProvider(1)).BuildServiceProvider())
            .CreateDbContext();

        var dbKayit = await ctx2.Soforler
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == personel.Id);

        dbKayit.Should().NotBeNull();
        dbKayit!.MuhasebeHesapId.Should().NotBeNull("DB'de 335 hesap Id dolu olmalı");
        dbKayit.PersonelAvansHesapId.Should().NotBeNull("DB'de 195 hesap Id dolu olmalı");

        // Hesap kodlarını doğrula
        var hesap335 = await ctx2.MuhasebeHesaplari
            .FirstOrDefaultAsync(h => h.Id == dbKayit.MuhasebeHesapId);
        hesap335.Should().NotBeNull();
        hesap335!.HesapKodu.Should().Be($"335.01.{personel.Id:D4}");

        var hesap195 = await ctx2.MuhasebeHesaplari
            .FirstOrDefaultAsync(h => h.Id == dbKayit.PersonelAvansHesapId);
        hesap195.Should().NotBeNull();
        hesap195!.HesapKodu.Should().Be($"195.01.{personel.Id:D4}");
    }

    [Fact]
    public async Task RepeatedUpdate_DoesNotCreateDuplicate335195Accounts()
    {
        await using var ctx = new TestDbContextFactory(
            new ServiceCollection().AddSingleton<IAktifFirmaProvider>(new TestAktifFirmaProvider(1)).BuildServiceProvider())
            .CreateDbContext();

        var personel = await ctx.Soforler
            .FirstOrDefaultAsync(s => !s.IsDeleted && s.Aktif && s.FirmaId == 1 && s.Id == 5);
        personel.Should().NotBeNull();

        // İlk update
        var svc = CreateSoforService(1);
        personel!.MuhasebeHesapId = null;
        personel.PersonelAvansHesapId = null;
        await svc.UpdateAsync(personel);

        // 335 hesaplarını say
        var onceki335Count = await ctx.MuhasebeHesaplari
            .CountAsync(h => h.HesapKodu == $"335.01.{personel.Id:D4}" && !h.IsDeleted);

        // İkinci update
        personel.MuhasebeHesapId = null;
        personel.PersonelAvansHesapId = null;
        await svc.UpdateAsync(personel);

        // Aynı sayıda 335 hesap olmalı (mükerrer oluşmamalı)
        var sonraki335Count = await ctx.MuhasebeHesaplari
            .CountAsync(h => h.HesapKodu == $"335.01.{personel.Id:D4}" && !h.IsDeleted);

        sonraki335Count.Should().Be(onceki335Count,
            "Tekrarlı update mükerrer 335 hesap oluşturmamalı");
    }
}
