using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using MKFiloServis.Web.Services;

namespace MKFiloServis.Tests;

public sealed class AuthenticationSessionPolicyTests
{
    [Theory]
    [InlineData(0, true)]
    [InlineData(11, true)]
    [InlineData(12, false)]
    [InlineData(13, false)]
    public void Absolute_session_lifetime_is_enforced(int hoursElapsed, bool expected)
    {
        var now = DateTimeOffset.UtcNow;
        Assert.Equal(expected, AuthenticationSessionPolicy.IsWithinLifetime(
            now.AddHours(-hoursElapsed), now));
    }

    [Fact]
    public void Future_session_start_is_rejected()
    {
        var now = DateTimeOffset.UtcNow;
        Assert.False(AuthenticationSessionPolicy.IsWithinLifetime(now.AddSeconds(1), now));
    }

    [Theory]
    [InlineData(true, true, 44, 7, true)]
    [InlineData(true, false, 44, 7, true)]
    [InlineData(false, false, 7, 7, true)]
    [InlineData(false, false, 0, 7, true)]
    [InlineData(false, true, 7, 7, false)]
    [InlineData(false, false, 44, 7, false)]
    public void Tenant_restore_is_limited_to_admin_or_default_firm(
        bool isAdmin, bool allFirms, int selectedFirmId, int defaultFirmId, bool expected)
        => Assert.Equal(expected, AuthenticationSessionPolicy.AllowsFirmScopeRestore(
            isAdmin, allFirms, selectedFirmId, defaultFirmId));

    [Theory]
    [InlineData(false, false, 0, 7, true)]
    [InlineData(false, true, 7, 7, false)]
    [InlineData(true, false, 44, 7, false)]
    [InlineData(true, true, 44, 7, true)]
    [InlineData(true, false, 7, 7, true)]
    [InlineData(true, false, 0, 7, true)]
    public void Tenant_selection_is_limited_to_authenticated_admin_or_default_firm(
        bool authenticated, bool isAdmin, int selectedFirmId, int defaultFirmId, bool expected)
        => Assert.Equal(expected, AuthenticationSessionPolicy.AllowsFirmScopeSelection(
            authenticated, isAdmin, selectedFirmId, defaultFirmId));

    [Fact]
    public async Task Failed_attempt_counter_is_atomic_and_locks_after_five_attempts()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        int userId;
        await using (var setup = new ApplicationDbContext(options))
        {
            await setup.Database.EnsureCreatedAsync();
            var role = new Rol { RolAdi = "AuthTest" };
            var user = new Kullanici
            {
                KullaniciAdi = "lockout-test",
                SifreHash = "test-only",
                AdSoyad = "Lockout Test",
                Rol = role
            };
            setup.Kullanicilar.Add(user);
            await setup.SaveChangesAsync();
            userId = user.Id;
        }

        var now = DateTime.UtcNow;
        for (var attempt = 1; attempt <= 5; attempt++)
        {
            await using var db = new ApplicationDbContext(options);
            Assert.Equal(1, await AuthenticationLockoutPolicy.RegisterFailedAttemptAsync(
                db, userId, now, TimeSpan.FromMinutes(15)));
        }

        await using (var verify = new ApplicationDbContext(options))
        {
            var locked = await verify.Kullanicilar.SingleAsync(user => user.Id == userId);
            Assert.Equal(5, locked.BasarisizGirisSayisi);
            Assert.True(locked.Kilitli);
            Assert.Equal(now.AddMinutes(15), locked.KilitlenmeBitisUtc);
        }

        await using var afterLock = new ApplicationDbContext(options);
        Assert.Equal(0, await AuthenticationLockoutPolicy.RegisterFailedAttemptAsync(
            afterLock, userId, now.AddMinutes(1), TimeSpan.FromMinutes(15)));
    }
}
