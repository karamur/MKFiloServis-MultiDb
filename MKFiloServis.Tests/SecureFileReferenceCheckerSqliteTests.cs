using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using System.Security.Cryptography;
using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using MKFiloServis.Web.Services;
using MKFiloServis.Web.Services.Interfaces;
using MKFiloServis.Web.Services.Security;

namespace MKFiloServis.Tests;

public sealed class SecureFileReferenceCheckerSqliteTests(Xunit.Abstractions.ITestOutputHelper output)
{
    [Fact]
    public async Task Preserves_referenced_file_and_releases_unreferenced_file_in_full_sqlite_model()
    {
        var root = Path.Combine(Path.GetTempPath(), "mkfiloservis-reference-check-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var previousStorageRoot = Environment.GetEnvironmentVariable("CRMFILO_STORAGE_ROOT");
        Environment.SetEnvironmentVariable("CRMFILO_STORAGE_ROOT", root);
        try
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite($"Data Source={Path.Combine(root, "acceptance.db")}")
                .Options;
            await using (var db = new ApplicationDbContext(options))
            {
                await db.Database.EnsureCreatedAsync();
                db.EvrakDosyalari.Add(new EvrakDosya
                {
                    EvrakTipi = "Kimlik",
                    DosyaAdi = "owned.enc",
                    DosyaYolu = OperatingSystem.IsWindows()
                        ? "  \\uploads\\ebys\\OWNED.enc  "
                        : "  \\uploads\\ebys\\owned.enc  ",
                    IsDeleted = true
                });
                db.EvrakDosyalari.Add(new EvrakDosya
                {
                    EvrakTipi = "Kimlik", DosyaAdi = "özlük.enc",
                    DosyaYolu = OperatingSystem.IsWindows()
                        ? "  \\uploads\\ebys\\ÖZLÜK.enc  "
                        : "  \\uploads\\ebys\\özlük.enc  ", IsDeleted = true
                });
                await db.SaveChangesAsync();
            }

            var checker = new SecureFileReferenceChecker(new TestFactory(options), new TestEnvironment(root));
            Assert.True(await checker.IsReferencedAsync("uploads/ebys/owned.enc"));
            Assert.True(await checker.IsReferencedAsync("uploads/ebys/özlük.enc"));
            Assert.False(await checker.IsReferencedAsync("uploads/ebys/unreferenced.enc"));
        }
        finally
        {
            Environment.SetEnvironmentVariable("CRMFILO_STORAGE_ROOT", previousStorageRoot);
            SqliteConnection.ClearAllPools();
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Worker_keeps_referenced_file_and_deletes_unreferenced_file_in_full_sqlite_model()
    {
        var root = Path.Combine(Path.GetTempPath(), "mkfiloservis-cleanup-worker-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var previousStorageRoot = Environment.GetEnvironmentVariable("CRMFILO_STORAGE_ROOT");
        Environment.SetEnvironmentVariable("CRMFILO_STORAGE_ROOT", root);
        try
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite($"Data Source={Path.Combine(root, "worker.db")}")
                .Options;
            await using (var db = new ApplicationDbContext(options))
            {
                await db.Database.EnsureCreatedAsync();
                db.EvrakDosyalari.Add(new EvrakDosya
                {
                    EvrakTipi = "Kimlik",
                    DosyaAdi = "owned.enc",
                    DosyaYolu = OperatingSystem.IsWindows()
                        ? "/uploads/ebys/OWNED.enc"
                        : "/uploads/ebys/owned.enc",
                    IsDeleted = true
                });
                await db.SaveChangesAsync();
            }

            var uploadRoot = Path.Combine(root, "uploads", "ebys");
            Directory.CreateDirectory(uploadRoot);
            var ownedFile = Path.Combine(uploadRoot, "owned.enc");
            var freeFile = Path.Combine(uploadRoot, "free.enc");
            await File.WriteAllBytesAsync(ownedFile, [1]);
            await File.WriteAllBytesAsync(freeFile, [2]);

            var environment = new TestEnvironment(root);
            var protectionProvider = DataProtectionProvider.Create(new DirectoryInfo(Path.Combine(root, "dp-keys")));
            var journal = new FileCleanupJournal(protectionProvider, environment);
            await journal.EnqueueAsync("uploads/ebys/owned.enc");
            await journal.EnqueueAsync("uploads/ebys/free.enc");

            var masterKey = new TestMasterKeyProvider();
            using var services = new ServiceCollection()
                .AddSingleton<ISecureFileReferenceChecker>(new SecureFileReferenceChecker(new TestFactory(options), environment))
                .AddSingleton<ISecureFileService>(sp => new SecureFileService(
                    new AesGcmFileProtector(masterKey), masterKey,
                    protectionProvider, environment, NullLogger<SecureFileService>.Instance,
                    new TestRecoveryTracker(), journal, sp.GetRequiredService<IServiceScopeFactory>()))
                .BuildServiceProvider();

            var files = services.GetRequiredService<ISecureFileService>();
            await files.DeleteAsync("uploads/ebys/owned.enc");
            Assert.True(File.Exists(ownedFile));
            Assert.DoesNotContain(await journal.GetDueAsync(DateTime.UtcNow.AddDays(1)),
                item => item.RelativePath == "uploads/ebys/owned.enc");

            await journal.EnqueueAsync("uploads/ebys/owned.enc", waitForReferenceRemoval: true);
            await files.DeleteAsync("uploads/ebys/owned.enc");
            Assert.Contains(await journal.GetDueAsync(DateTime.UtcNow.AddDays(1)),
                item => item.RelativePath == "uploads/ebys/owned.enc" && item.WaitForReferenceRemoval);
            await journal.CompleteAsync("uploads/ebys/owned.enc");

            // Worker kendi kuyruğunda da geri alınabilir kayda bağlı dosyayı korumalı.
            await journal.EnqueueAsync("uploads/ebys/owned.enc");

            var worker = new FileCleanupRetryWorker(journal,
                services.GetRequiredService<IServiceScopeFactory>(), NullLogger<FileCleanupRetryWorker>.Instance);
            await worker.ProcessDueOnceAsync();

            Assert.True(File.Exists(ownedFile));
            Assert.False(File.Exists(freeFile));
            Assert.Empty(await journal.GetDueAsync(DateTime.UtcNow.AddDays(1)));

            // Restore the soft-deleted record after a cleanup pass: the original
            // object must still be present and referenced by the restored row.
            await using (var db = new ApplicationDbContext(options))
            {
                var document = await db.EvrakDosyalari.SingleAsync();
                document.IsDeleted = false;
                await db.SaveChangesAsync();
            }
            Assert.True(await services.GetRequiredService<ISecureFileReferenceChecker>()
                .IsReferencedAsync("uploads/ebys/owned.enc"));
            Assert.True(File.Exists(ownedFile));
        }
        finally
        {
            Environment.SetEnvironmentVariable("CRMFILO_STORAGE_ROOT", previousStorageRoot);
            SqliteConnection.ClearAllPools();
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Legacy_cleanup_requires_a_readable_identical_replacement_before_removing_source()
    {
        var root = Path.Combine(Path.GetTempPath(), "mkfiloservis-migration-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var previousStorageRoot = Environment.GetEnvironmentVariable("CRMFILO_STORAGE_ROOT");
        Environment.SetEnvironmentVariable("CRMFILO_STORAGE_ROOT", root);
        try
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite($"Data Source={Path.Combine(root, "migration.db")}").Options;
            await using (var db = new ApplicationDbContext(options))
                await db.Database.EnsureCreatedAsync();

            var environment = new TestEnvironment(root);
            var provider = DataProtectionProvider.Create(new DirectoryInfo(Path.Combine(root, "dp-keys")));
            var journal = new FileCleanupJournal(provider, environment);
            var masterKey = new TestMasterKeyProvider();
            using var services = new ServiceCollection()
                .AddSingleton<ISecureFileReferenceChecker>(new SecureFileReferenceChecker(new TestFactory(options), environment))
                .BuildServiceProvider();
            var files = new SecureFileService(new AesGcmFileProtector(masterKey), masterKey, provider,
                environment, NullLogger<SecureFileService>.Instance, new TestRecoveryTracker(), journal,
                services.GetRequiredService<IServiceScopeFactory>());
            byte[] content = [1, 2, 3, 4, 5];
            var newPath = await files.SaveEncryptedAsync("migration", "document.pdf", content);
            var encryptedPath = Path.Combine(root, "uploads", newPath.Replace('/', Path.DirectorySeparatorChar));
            var ciphertext = await File.ReadAllBytesAsync(encryptedPath);
            var oldPath = Path.Combine(root, "uploads", "legacy.pdf");
            await File.WriteAllBytesAsync(oldPath, content);
            await using (var db = new ApplicationDbContext(options))
            {
                db.EvrakDosyalari.Add(new EvrakDosya
                {
                    EvrakTipi = "Kimlik", DosyaAdi = "document.pdf", DosyaYolu = newPath, IsDeleted = true
                });
                await db.SaveChangesAsync();
            }
            var legacy = new LegacyFileCleanupService(new TestFactory(options), new FileService(), journal,
                environment, NullLogger<LegacyFileCleanupService>.Instance, files);
            var key = await legacy.EnqueueMigrationAsync("/uploads/legacy.pdf", newPath);

            File.Delete(encryptedPath);
            await Assert.ThrowsAsync<InvalidOperationException>(() => legacy.ProcessAsync(key));
            Assert.True(File.Exists(oldPath));
            Assert.Single(await journal.GetDueAsync(DateTime.UtcNow.AddDays(1)));

            await File.WriteAllBytesAsync(encryptedPath, ciphertext);
            await File.WriteAllBytesAsync(oldPath, [9, 8, 7]);
            await Assert.ThrowsAsync<InvalidOperationException>(() => legacy.ProcessAsync(key));
            Assert.True(File.Exists(oldPath));

            await File.WriteAllBytesAsync(oldPath, content);
            await legacy.ProcessAsync(key);
            Assert.True(File.Exists(oldPath));
            Assert.Equal(content, await File.ReadAllBytesAsync(oldPath));
            // A late reference after cleanup must still find the original plaintext file.
            await using (var late = new ApplicationDbContext(options))
            {
                late.EvrakDosyalari.Add(new EvrakDosya { EvrakTipi = "Late", DosyaAdi = "legacy.pdf", DosyaYolu = "/uploads/legacy.pdf" });
                await late.SaveChangesAsync();
                Assert.Equal(content, await File.ReadAllBytesAsync(oldPath));
                // Remove this isolated probe row before exercising the migration iterator below.
                late.EvrakDosyalari.Remove(await late.EvrakDosyalari.SingleAsync(e => e.EvrakTipi == "Late"));
                await late.SaveChangesAsync();
            }
            Assert.Equal(content, await File.ReadAllBytesAsync(oldPath));
            Assert.Equal(content, await files.ReadDecryptedAsync(newPath));
            Assert.Empty(await journal.GetDueAsync(DateTime.UtcNow.AddDays(1)));

            // Exercise the real migration iterator and restore a migrated soft-delete row.
            var ebysSource = Path.Combine(root, "uploads", "ebys-old.pdf");
            await File.WriteAllBytesAsync(ebysSource, content);
            await using (var db = new ApplicationDbContext(options))
            {
                db.EbysEvrakDosyalar.Add(new EbysEvrakDosya
                {
                    Evrak = new EbysEvrak { EvrakNo = "A10-ACCEPTANCE", Konu = "Isolated fixture" },
                    DosyaAdi = "ebys-old.pdf", DosyaYolu = "/uploads/ebys-old.pdf", IsDeleted = true
                });
                await db.SaveChangesAsync();
            }
            var migration = new DosyaMigrasyonService(new TestFactory(options), files, new FileService(), legacy,
                new TestActiveFirm(), environment, new AesGcmFileProtector(masterKey),
                NullLogger<DosyaMigrasyonService>.Instance);
            Assert.Equal(1, (await migration.OnizlemeAsync()).SilinmisEbysAnaCount);
            var steps = new List<DosyaMigrasyonAdim>();
            await foreach (var step in migration.MigrateAsync()) steps.Add(step);
            Assert.Equal(MigrasyonDurum.Basarili, Assert.Single(steps).Durum);
            Assert.True(File.Exists(ebysSource));
            await using (var db = new ApplicationDbContext(options))
            {
                var restored = await db.EbysEvrakDosyalar.IgnoreQueryFilters().SingleAsync();
                Assert.True(restored.IsDeleted);
                Assert.Equal(content, await files.ReadDecryptedAsync(restored.DosyaYolu));
                restored.IsDeleted = false;
                await db.SaveChangesAsync();
                await files.DeleteAsync(restored.DosyaYolu);
                Assert.Equal(content, await files.ReadDecryptedAsync(restored.DosyaYolu));
            }
            Assert.Equal(0, (await migration.OnizlemeAsync()).Toplam);
            await foreach (var step in migration.MigrateAsync())
                Assert.Fail("Completed migration must not produce another step.");
        }
        finally
        {
            Environment.SetEnvironmentVariable("CRMFILO_STORAGE_ROOT", previousStorageRoot);
            SqliteConnection.ClearAllPools();
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Reference_lookup_handles_one_hundred_thousand_rows_without_loading_the_inventory()
    {
        var root = Path.Combine(Path.GetTempPath(), "mkfiloservis-volume-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var previousRoot = Environment.GetEnvironmentVariable("CRMFILO_STORAGE_ROOT");
        Environment.SetEnvironmentVariable("CRMFILO_STORAGE_ROOT", root);
        try
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite($"Data Source={Path.Combine(root, "volume.db")}").Options;
            await using (var db = new ApplicationDbContext(options))
            {
                await db.Database.EnsureCreatedAsync();
                await db.Database.ExecuteSqlRawAsync("""
                    WITH RECURSIVE numbers(n) AS (SELECT 1 UNION ALL SELECT n + 1 FROM numbers WHERE n < 100000)
                    INSERT INTO EvrakDosyalari (EvrakTipi, DosyaAdi, DosyaYolu, YuklenmeTarihi, IsDeleted)
                    SELECT 'Kimlik', 'volume.enc', '/uploads/volume/' || n || '.enc', '2026-10-07', n % 2
                    FROM numbers;
                    """);
                Assert.Equal(100000, await db.EvrakDosyalari.IgnoreQueryFilters().CountAsync());
            }
            var checker = new SecureFileReferenceChecker(new TestFactory(options), new TestEnvironment(root));
            var timer = System.Diagnostics.Stopwatch.StartNew();
            Assert.True(await checker.IsReferencedAsync("uploads/volume/99999.enc"));
            output.WriteLine($"100000 rows, exact reference: {timer.Elapsed.TotalMilliseconds:F1} ms");
            timer.Restart();
            await using (var db = new ApplicationDbContext(options))
                Assert.True(await DatabaseFilePathInventory.IsReferencedNormalizedAsync(db,
                    ["/UPLOADS/VOLUME/99999.ENC"], true, CancellationToken.None));
            output.WriteLine($"100000 rows, normalized reference: {timer.Elapsed.TotalMilliseconds:F1} ms");
            timer.Restart();
            Assert.False(await checker.IsReferencedAsync("uploads/volume/absent.enc"));
            output.WriteLine($"100000 rows, absent reference: {timer.Elapsed.TotalMilliseconds:F1} ms");
        }
        finally
        {
            Environment.SetEnvironmentVariable("CRMFILO_STORAGE_ROOT", previousRoot);
            SqliteConnection.ClearAllPools();
            Directory.Delete(root, recursive: true);
        }
    }

    private sealed class TestFactory(DbContextOptions<ApplicationDbContext> options) : IDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext() => new(options);
    }

    private sealed class TestActiveFirm : IAktifFirmaProvider
    {
        public int? AktifFirmaId => 1;
        public bool HasAktifFirma => true;
        public bool TumFirmalar => false;
        public AktifFirmaBilgisi Mevcut => new();
        public event Action? AktifFirmaDegisti { add { } remove { } }
        public void Set(AktifFirmaBilgisi firma) => throw new NotSupportedException();
        public void SetTumFirmalar(bool tumFirmalar) => throw new NotSupportedException();
        public void SetDonem(int yil, int ay) => throw new NotSupportedException();
        public Task<bool> TryRestoreAsync() => Task.FromResult(false);
    }

    private sealed class TestEnvironment(string root) : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "MKFiloServis.Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = root;
        public string EnvironmentName { get; set; } = "Testing";
        public string ContentRootPath { get; set; } = root;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class TestMasterKeyProvider : IMasterKeyProvider
    {
        private readonly byte[] _key = RandomNumberGenerator.GetBytes(32);
        public ReadOnlyMemory<byte> GetMasterKey() => _key;
    }

    private sealed class TestRecoveryTracker : IDecryptionRecoveryTracker
    {
        public void TrackDecryptionFailure(string relativePath, string reason) { }
        public void TrackDecryptionRecovery(string relativePath, string method) { }
        public (int FailureCount, int RecoveryCount) GetSessionStats() => (0, 0);
        public IReadOnlyList<DecryptionFailureRecord> GetRecentFailures(int limit = 10) => [];
    }
}
