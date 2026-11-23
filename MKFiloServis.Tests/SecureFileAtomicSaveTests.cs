using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MKFiloServis.Web.Services;
using MKFiloServis.Web.Services.Security;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Tests;

public sealed class SecureFileAtomicSaveTests
{
    [Theory]
    [InlineData("complete")]
    [InlineData("missing-xml")]
    [InlineData("missing-koa1")]
    public async Task Recovery_backup_publishes_only_when_required_key_material_is_present(string scenario)
    {
        var previousRoot = Environment.GetEnvironmentVariable("CRMFILO_STORAGE_ROOT");
        var root = Path.Combine(Path.GetTempPath(), "mkfiloservis-recovery-" + Guid.NewGuid().ToString("N"));
        try
        {
            Environment.SetEnvironmentVariable("CRMFILO_STORAGE_ROOT", root);
            var keys = Directory.CreateDirectory(Path.Combine(root, "keys"));
            var writerKeys = scenario == "missing-xml" ? Directory.CreateDirectory(Path.Combine(root, "other-keys")) : keys;
            var provider = DataProtectionProvider.Create(keys, options => options.SetApplicationName("MKFiloServis"));
            var writer = DataProtectionProvider.Create(writerKeys, options => options.SetApplicationName("MKFiloServis"));
            var archiveDirectory = Directory.CreateDirectory(Path.Combine(root, "Arsiv"));
            var bytes = "archive content"u8.ToArray();
            var encrypted = scenario == "missing-koa1"
                ? new AesGcmFileProtector(new TestMasterKeyProvider()).Protect(bytes)
                : new DataProtectionFileProtector(writer).Protect(bytes);
            await File.WriteAllBytesAsync(Path.Combine(archiveDirectory.FullName, "document.pdf"), encrypted);
            var recoveryDirectory = RecoveryArchive.EnsureBackupDirectory(root, Path.Combine(root, "wwwroot"));
            var zip = Path.Combine(recoveryDirectory, "backup.zip");
            Task Create() => RecoveryArchive.CreateAsync(zip, root, provider, null, CancellationToken.None, verifyRecoveryKeys: true);

            if (scenario == "complete")
            {
                await Create();
                var prepared = await RecoveryArchive.PrepareAsync(zip, root, null, CancellationToken.None);
                Assert.True(prepared.KeyProbeVerified);
                Assert.Equal(encrypted, await File.ReadAllBytesAsync(Path.Combine(prepared.FolderPath!, "storage", "Arsiv", "document.pdf")));
            }
            else
            {
                await Assert.ThrowsAsync<InvalidDataException>(Create);
                Assert.Empty(Directory.EnumerateFileSystemEntries(recoveryDirectory));
            }
        }
        finally
        {
            Environment.SetEnvironmentVariable("CRMFILO_STORAGE_ROOT", previousRoot);
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Archive_reads_plain_and_encrypted_formats_after_reopening_the_same_key_ring()
    {
        var previousRoot = Environment.GetEnvironmentVariable("CRMFILO_STORAGE_ROOT");
        var root = Path.Combine(Path.GetTempPath(), "mkfiloservis-archive-" + Guid.NewGuid().ToString("N"));
        try
        {
            Environment.SetEnvironmentVariable("CRMFILO_STORAGE_ROOT", root);
            var environment = new TestWebHostEnvironment(root);
            var keys = new DirectoryInfo(Path.Combine(root, "keys"));
            var writer = DataProtectionProvider.Create(keys, options => options.SetApplicationName("MKFiloServis"));
            var masterKey = new TestMasterKeyProvider();
            var aes = new AesGcmFileProtector(masterKey);
            var aesWithVersion = aes.Protect("ARŞİV"u8);
            byte[] plain = "ARŞİV"u8.ToArray();
            (string Name, ArchiveFileFormat Format, byte[] Bytes)[] fixtures =
            [
                ("plain.pdf", ArchiveFileFormat.Plain, plain),
                ("current.pdf.enc", ArchiveFileFormat.DataProtection, new DataProtectionFileProtector(writer).Protect(plain)),
                ("current-no-enc.pdf", ArchiveFileFormat.DataProtection, new DataProtectionFileProtector(writer).Protect(plain)),
                ("old-aes.pdf.enc", ArchiveFileFormat.LegacyAes, aesWithVersion),
                ("old-aes-no-version.pdf", ArchiveFileFormat.LegacyAes, [.. aesWithVersion[..4], .. aesWithVersion[5..]]),
                ("old-dp.pdf", ArchiveFileFormat.LegacyEncrypted, writer.CreateProtector("MKFiloServis.SecureFileStorage.v1").Protect(plain))
            ];

            var directory = Path.Combine(root, "Arsiv", "Sifreli");
            Directory.CreateDirectory(directory);
            foreach (var (name, _, bytes) in fixtures)
                await File.WriteAllBytesAsync(Path.Combine(directory, name), bytes);
            await File.WriteAllBytesAsync(Path.Combine(directory, "broken.pdf"), "MKD1"u8.ToArray());

            using var services = new ServiceCollection().BuildServiceProvider();
            ArchiveBrowserService Open(DirectoryInfo keyDirectory)
            {
                var provider = DataProtectionProvider.Create(keyDirectory, options => options.SetApplicationName("MKFiloServis"));
                var secure = new SecureFileService(new DataProtectionFileProtector(provider), masterKey, provider, environment,
                    NullLogger<SecureFileService>.Instance, new TestRecoveryTracker(), new FileCleanupJournal(provider, environment),
                    services.GetRequiredService<IServiceScopeFactory>());
                return new ArchiveBrowserService(environment, secure);
            }

            var browser = Open(keys);
            var listed = browser.GetFiles("Arsiv").ToDictionary(file => file.StorageRelativePath, file => file.Format);
            foreach (var (name, format, bytes) in fixtures)
            {
                var path = $"Arsiv/Sifreli/{name}";
                var opened = await browser.ReadFileAsync(path);
                Assert.Equal(format, listed[path]);
                Assert.Equal(format, opened.Format);
                Assert.Equal(name.EndsWith(".enc", StringComparison.Ordinal) ? name[..^4] : name, opened.DisplayName);
                Assert.Equal(plain, opened.Content);
                Assert.Equal(bytes, await File.ReadAllBytesAsync(Path.Combine(directory, name)));
            }

            var missingKey = await Open(new DirectoryInfo(Path.Combine(root, "different-keys")))
                .ReadFileAsync("Arsiv/Sifreli/current-no-enc.pdf");
            Assert.Equal(ArchiveFileFormat.DataProtection, missingKey.Format);
            Assert.Null(missingKey.Content);
            var broken = await browser.ReadFileAsync("Arsiv/Sifreli/broken.pdf");
            Assert.Equal(ArchiveFileFormat.DataProtection, broken.Format);
            Assert.Null(broken.Content);
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => browser.ReadFileAsync("Arsiv/../keys/master.key"));
        }
        finally
        {
            Environment.SetEnvironmentVariable("CRMFILO_STORAGE_ROOT", previousRoot);
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task SaveEncryptedAsync_publishes_complete_file_without_temporary_files()
    {
        var originalStorageRoot = Environment.GetEnvironmentVariable("CRMFILO_STORAGE_ROOT");
        var root = Path.Combine(Path.GetTempPath(), "mkfiloservis-secure-save-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        Environment.SetEnvironmentVariable("CRMFILO_STORAGE_ROOT", root);
        try
        {
            var service = CreateService(root);
            byte[] content = [1, 2, 3, 4, 5];

            var relativePath = await service.SaveEncryptedAsync("test", "sample.pdf", content);

            var uploads = Path.Combine(root, "uploads");
            var files = Directory.GetFiles(uploads, "*", SearchOption.AllDirectories);
            Assert.Single(files);
            Assert.EndsWith(".enc", relativePath, StringComparison.Ordinal);
            Assert.Equal([9, 1, 2, 3, 4, 5], await File.ReadAllBytesAsync(files[0]));
        }
        finally
        {
            Environment.SetEnvironmentVariable("CRMFILO_STORAGE_ROOT", originalStorageRoot);
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task SaveEncryptedAsync_cancellation_leaves_no_published_or_temporary_file()
    {
        var originalStorageRoot = Environment.GetEnvironmentVariable("CRMFILO_STORAGE_ROOT");
        var root = Path.Combine(Path.GetTempPath(), "mkfiloservis-secure-save-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        Environment.SetEnvironmentVariable("CRMFILO_STORAGE_ROOT", root);
        try
        {
            var service = CreateService(root);
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                service.SaveEncryptedAsync("test", "sample.pdf", [1, 2, 3], cancellation.Token));

            var uploads = Path.Combine(root, "uploads");
            Assert.Empty(Directory.GetFiles(uploads, "*", SearchOption.AllDirectories));
        }
        finally
        {
            Environment.SetEnvironmentVariable("CRMFILO_STORAGE_ROOT", originalStorageRoot);
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Quarantined_encrypted_file_remains_readable_after_storage_root_changes()
    {
        var previousRoot = Environment.GetEnvironmentVariable("CRMFILO_STORAGE_ROOT");
        var firstRoot = Path.Combine(Path.GetTempPath(), "mkfiloservis-quarantine-a-" + Guid.NewGuid().ToString("N"));
        var restoredRoot = Path.Combine(Path.GetTempPath(), "mkfiloservis-quarantine-b-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(firstRoot);
        Directory.CreateDirectory(restoredRoot);
        try
        {
            Environment.SetEnvironmentVariable("CRMFILO_STORAGE_ROOT", firstRoot);
            var service = CreateService(firstRoot, useAes: true);
            byte[] content = [4, 3, 2, 1];
            var path = await service.SaveEncryptedAsync("test", "recover.pdf", content);
            await service.DeleteAsync(path);

            var original = Path.Combine(firstRoot, "uploads", path.Replace('/', Path.DirectorySeparatorChar));
            var quarantinedRelative = Path.Combine("uploads", ".deleted-file-quarantine-v1", "uploads",
                path.Replace('/', Path.DirectorySeparatorChar));
            var quarantined = Path.Combine(firstRoot, quarantinedRelative);
            Assert.False(File.Exists(original));
            Assert.True(File.Exists(quarantined));
            Assert.True(await service.ExistsAsync(path));
            Assert.Equal(content, await service.ReadDecryptedAsync(path));

            var keys = Path.Combine(firstRoot, "keys");
            Directory.CreateDirectory(keys);
            using var archiveServices = new ServiceCollection()
                .AddLogging()
                .AddDataProtection().SetApplicationName("MKFiloServis")
                .PersistKeysToFileSystem(new DirectoryInfo(keys)).Services.BuildServiceProvider();
            var backup = Path.Combine(firstRoot, "recovery.zip");
            await RecoveryArchive.CreateAsync(backup, firstRoot,
                archiveServices.GetRequiredService<IDataProtectionProvider>(), null, CancellationToken.None);
            var prepared = await RecoveryArchive.PrepareAsync(backup, firstRoot,
                Path.Combine(firstRoot, "wwwroot"), CancellationToken.None);
            Assert.True(prepared.Prepared);
            Assert.True(prepared.KeyProbeVerified);

            var restoredFile = Path.Combine(restoredRoot, quarantinedRelative);
            Directory.CreateDirectory(Path.GetDirectoryName(restoredFile)!);
            File.Copy(Path.Combine(prepared.FolderPath!, "storage", quarantinedRelative), restoredFile);
            Environment.SetEnvironmentVariable("CRMFILO_STORAGE_ROOT", restoredRoot);
            var restored = CreateService(restoredRoot, useAes: true);
            Assert.Equal(content, await restored.ReadDecryptedAsync(path));
        }
        finally
        {
            Environment.SetEnvironmentVariable("CRMFILO_STORAGE_ROOT", previousRoot);
            Directory.Delete(firstRoot, recursive: true);
            Directory.Delete(restoredRoot, recursive: true);
        }
    }

    [Fact]
    public async Task DeleteAsync_preserves_file_and_pending_request_when_reference_lookup_fails()
    {
        var originalStorageRoot = Environment.GetEnvironmentVariable("CRMFILO_STORAGE_ROOT");
        var root = Path.Combine(Path.GetTempPath(), "mkfiloservis-secure-delete-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        Environment.SetEnvironmentVariable("CRMFILO_STORAGE_ROOT", root);
        try
        {
            var environment = new TestWebHostEnvironment(root);
            var dataProtection = DataProtectionProvider.Create(new DirectoryInfo(Path.Combine(root, "dp-keys")));
            var journal = new FileCleanupJournal(dataProtection, environment);
            using var services = new ServiceCollection()
                .AddSingleton<ISecureFileReferenceChecker>(new FailingReferenceChecker())
                .BuildServiceProvider();
            var service = new SecureFileService(
                new PrefixFileProtector(), new TestMasterKeyProvider(), dataProtection,
                environment, NullLogger<SecureFileService>.Instance,
                new TestRecoveryTracker(), journal, services.GetRequiredService<IServiceScopeFactory>());
            var uploadRoot = Path.Combine(root, "uploads", "test");
            Directory.CreateDirectory(uploadRoot);
            var fullPath = Path.Combine(uploadRoot, "retained.enc");
            await File.WriteAllBytesAsync(fullPath, [1, 2, 3]);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.DeleteAsync("uploads/test/retained.enc"));

            Assert.True(File.Exists(fullPath));
            Assert.Contains(await journal.GetDueAsync(DateTime.UtcNow.AddDays(1)),
                item => item.RelativePath == "uploads/test/retained.enc");
        }
        finally
        {
            Environment.SetEnvironmentVariable("CRMFILO_STORAGE_ROOT", originalStorageRoot);
            Directory.Delete(root, recursive: true);
        }
    }

    private static SecureFileService CreateService(string root, bool useAes = false)
    {
        var dpDirectory = Path.Combine(root, "dp-keys");
        Directory.CreateDirectory(dpDirectory);
        var environment = new TestWebHostEnvironment(root);
        var dataProtection = DataProtectionProvider.Create(new DirectoryInfo(dpDirectory));
        var services = new ServiceCollection()
            .AddSingleton<ISecureFileReferenceChecker>(new NoReferenceChecker())
            .BuildServiceProvider();
        var masterKey = new TestMasterKeyProvider();
        return new SecureFileService(
            useAes ? new AesGcmFileProtector(masterKey) : new PrefixFileProtector(),
            masterKey,
            dataProtection,
            environment,
            NullLogger<SecureFileService>.Instance,
            new TestRecoveryTracker(),
            new FileCleanupJournal(dataProtection, environment),
            services.GetRequiredService<IServiceScopeFactory>());
    }

    private sealed class TestWebHostEnvironment(string root) : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "MKFiloServis.Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = root;
        public string EnvironmentName { get; set; } = "Testing";
        public string ContentRootPath { get; set; } = root;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class PrefixFileProtector : IFileProtector
    {
        public byte[] Protect(ReadOnlySpan<byte> plain) => [9, .. plain];
        public byte[] Unprotect(ReadOnlySpan<byte> cipher) => cipher[1..].ToArray();
        public void ProtectFile(string plainPath, string cipherPath) => throw new NotSupportedException();
        public void UnprotectFile(string cipherPath, string plainPath) => throw new NotSupportedException();
    }

    private sealed class TestMasterKeyProvider : IMasterKeyProvider
    {
        private static readonly byte[] Key = RandomNumberGenerator.GetBytes(32);
        public ReadOnlyMemory<byte> GetMasterKey() => Key;
    }

    private sealed class TestRecoveryTracker : IDecryptionRecoveryTracker
    {
        public void TrackDecryptionFailure(string relativePath, string reason) { }
        public void TrackDecryptionRecovery(string relativePath, string method) { }
        public (int FailureCount, int RecoveryCount) GetSessionStats() => (0, 0);
        public IReadOnlyList<DecryptionFailureRecord> GetRecentFailures(int limit = 10) => [];
    }

    private sealed class NoReferenceChecker : ISecureFileReferenceChecker
    {
        public Task<bool> IsReferencedAsync(string relativePath, CancellationToken cancellationToken = default)
            => Task.FromResult(false);
    }

    private sealed class FailingReferenceChecker : ISecureFileReferenceChecker
    {
        public Task<bool> IsReferencedAsync(string relativePath, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("DB sorgusu başarısız");
    }
}
