using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using MKFiloServis.Web.Services;

namespace MKFiloServis.Tests;

public sealed class CurrentPermissionGuardSqliteTests
{
    [Theory]
    [InlineData("permission")]
    [InlineData("user")]
    [InlineData("role")]
    [InlineData("admin")]
    public async Task Existing_session_cannot_keep_revoked_database_authority(string revoke)
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        int userId;
        int roleId;
        await using (var db = new ApplicationDbContext(options))
        {
            await db.Database.EnsureCreatedAsync();
            var role = new Rol { RolAdi = revoke == "admin" ? "Admin" : "FinancialTest" };
            role.Yetkiler.Add(new RolYetki { YetkiKodu = Yetkiler.MaasDuzenle, Izin = revoke != "admin" });
            var user = new Kullanici { KullaniciAdi = "permission-test", SifreHash = "test-only", AdSoyad = "Test", Rol = role };
            db.Kullanicilar.Add(user);
            await db.SaveChangesAsync();
            userId = user.Id;
            roleId = role.Id;
        }
        var http = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                    new Claim(ClaimTypes.Role, "Admin") // Token rolü DB yetkisini değiştiremez.
                }, "Test"))
            }
        };
        var guard = new CurrentPermissionGuard(new Factory(options), http, null!);
        await guard.RequireAnyAsync(Yetkiler.MaasDuzenle);
        await using (var db = new ApplicationDbContext(options))
        {
            if (revoke == "user")
                (await db.Kullanicilar.SingleAsync(k => k.Id == userId)).Aktif = false;
            else if (revoke == "role")
                (await db.Roller.SingleAsync(r => r.Id == roleId)).IsDeleted = true;
            else if (revoke == "admin")
                (await db.Roller.SingleAsync(r => r.Id == roleId)).RolAdi = "NoLongerAdmin";
            else
                (await db.Set<RolYetki>().SingleAsync(y => y.RolId == roleId)).Izin = false;
            await db.SaveChangesAsync();
        }
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => guard.RequireAnyAsync(Yetkiler.MaasDuzenle));
    }

    private sealed class Factory(DbContextOptions<ApplicationDbContext> options) : IDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext() => new(options);
        public Task<ApplicationDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(CreateDbContext());
    }
}
