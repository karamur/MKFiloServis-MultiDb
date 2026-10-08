using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using MKFiloServis.Web.Services;

namespace MKFiloServis.Tests;

public sealed class PayrollWriteSqliteTests
{
    [Fact]
    public async Task Stale_update_is_rejected_and_current_deduction_remains()
    {
        await using var fixture = await Fixture.CreateAsync();
        var stale = (await fixture.Service.GetMaasByIdAsync(fixture.SalaryId))!;
        var current = (await fixture.Service.GetMaasByIdAsync(fixture.SalaryId))!;
        current.Avans = 125;
        await fixture.Service.UpdateMaasAsync(current);
        stale.NetMaas = 1500;
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.UpdateMaasAsync(stale));
        Assert.Equal(125, (await fixture.Service.GetMaasByIdAsync(fixture.SalaryId))!.Avans);
    }

    [Fact]
    public async Task Payment_replay_is_safe_and_paid_salary_cannot_be_changed_or_removed()
    {
        await using var fixture = await Fixture.CreateAsync();
        var date = new DateTime(2026, 10, 8);
        await fixture.Service.MaasOdemeYapAsync(fixture.SalaryId, date, "Test payment");
        await fixture.Service.MaasOdemeYapAsync(fixture.SalaryId, date, "Test payment");
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.MaasOdemeYapAsync(fixture.SalaryId, date, "Different"));
        var paid = (await fixture.Service.GetMaasByIdAsync(fixture.SalaryId))!;
        Assert.Equal("Test payment", paid.OdemeAciklama);
        paid.NetMaas++;
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.UpdateMaasAsync(paid));
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.DeleteMaasAsync(fixture.SalaryId));
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.RecalculateMaaslarAsync([fixture.SalaryId]));
    }

    [Fact]
    public async Task Unpaid_salary_removal_preserves_row_as_soft_deleted()
    {
        await using var fixture = await Fixture.CreateAsync();
        await fixture.Service.DeleteMaasAsync(fixture.SalaryId);
        Assert.Null(await fixture.Service.GetMaasByIdAsync(fixture.SalaryId));
        await using var db = fixture.Factory.CreateDbContext();
        var row = await db.PersonelMaaslari.IgnoreQueryFilters().SingleAsync(m => m.Id == fixture.SalaryId);
        Assert.True(row.IsDeleted);
        Assert.NotNull(row.DeletedAt);
    }

    internal sealed class Fixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection = new("Data Source=:memory:");
        private readonly ServiceProvider _services = new ServiceCollection().AddSingleton<IAktifFirmaProvider>(new Firm()).BuildServiceProvider();
        public Factory Factory { get; private set; } = null!;
        public PersonelMaasIzinService Service { get; private set; } = null!;
        public CurrentPermissionGuard Guard { get; private set; } = null!;
        public IAktifFirmaProvider ActiveFirm => _services.GetRequiredService<IAktifFirmaProvider>();
        public int SalaryId { get; private set; }
        public static async Task<Fixture> CreateAsync()
        {
            var fixture = new Fixture();
            await fixture._connection.OpenAsync();
            fixture.Factory = new Factory(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(fixture._connection).Options, fixture._services);
            await using var db = fixture.Factory.CreateDbContext();
            await db.Database.EnsureCreatedAsync();
            var firma = new Firma { Id = 1, FirmaKodu = "TEST", FirmaAdi = "Test", Organizasyon = new Organizasyon { Id = 1, Adi = "Test Organization" } };
            var sofor = new Sofor { SoforKodu = "TEST", Ad = "Test", Soyad = "Person", Firma = firma, FirmaId = 1 };
            var salary = new PersonelMaas { Sofor = sofor, Firma = firma, FirmaId = 1, Yil = 2026, Ay = 10, NetMaas = 1000 };
            var user = new Kullanici { KullaniciAdi = "payroll-test", SifreHash = "test-only", AdSoyad = "Test", Rol = new Rol { RolAdi = "Admin" } };
            db.PersonelMaaslari.Add(salary);
            db.Kullanicilar.Add(user);
            await db.SaveChangesAsync();
            fixture.SalaryId = salary.Id;
            var http = new FixedHttpAccessor { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())], "Test")) } };
            fixture.Guard = new CurrentPermissionGuard(fixture.Factory, http, null!);
            fixture.Service = new PersonelMaasIzinService(fixture.Factory, fixture.Guard);
            return fixture;
        }
        public async ValueTask DisposeAsync()
        {
            ApplicationDbContext.AmbientServiceProvider.Value = null;
            await _connection.DisposeAsync();
            await _services.DisposeAsync();
        }
    }
    public sealed class Factory(DbContextOptions<ApplicationDbContext> options, IServiceProvider services) : IDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext()
        {
            var db = new ApplicationDbContext(options);
            db.SetServiceProvider(services);
            return db;
        }
        public Task<ApplicationDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) => Task.FromResult(CreateDbContext());
    }
    private sealed class FixedHttpAccessor : IHttpContextAccessor
    {
        public HttpContext? HttpContext { get; set; }
    }
    private sealed class Firm : IAktifFirmaProvider
    {
        public int? AktifFirmaId => 1;
        public bool HasAktifFirma => true;
        public bool TumFirmalar => false;
        public AktifFirmaBilgisi Mevcut => new();
        public event Action? AktifFirmaDegisti { add { } remove { } }
        public void Set(AktifFirmaBilgisi firma) => throw new NotSupportedException();
        public void SetTumFirmalar(bool value) => throw new NotSupportedException();
        public void SetDonem(int yil, int ay) => throw new NotSupportedException();
        public Task<bool> TryRestoreAsync() => Task.FromResult(false);
    }
}
