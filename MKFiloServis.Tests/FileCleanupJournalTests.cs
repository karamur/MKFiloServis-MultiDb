using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.FileProviders;
using MKFiloServis.Web.Services;

namespace MKFiloServis.Tests;

public sealed class FileCleanupJournalTests
{
    [Fact]
    public async Task Journal_persists_encrypted_entries_retries_and_completes_them()
    {
        var root = Path.Combine(Path.GetTempPath(), "mkfiloservis-cleanup-journal-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var previousStorageRoot = Environment.GetEnvironmentVariable("CRMFILO_STORAGE_ROOT");
        Environment.SetEnvironmentVariable("CRMFILO_STORAGE_ROOT", root);
        try
        {
            var dpKeys = Path.Combine(root, "dp-keys");
            var provider = DataProtectionProvider.Create(new DirectoryInfo(dpKeys));
            var environment = new TestWebHostEnvironment(root);
            var journal = new FileCleanupJournal(provider, environment);
            const string path = "private-company/employee-record.enc";

            await journal.EnqueueAsync(path);
            var journalFile = Path.Combine(root, ".file-cleanup-journal.v1");
            var journalText = System.Text.Encoding.UTF8.GetString(await File.ReadAllBytesAsync(journalFile));
            Assert.DoesNotContain(path, journalText, StringComparison.Ordinal);

            var restartedJournal = new FileCleanupJournal(provider, environment);
            Assert.Contains(await restartedJournal.GetDueAsync(DateTime.UtcNow.AddSeconds(1)), item => item.RelativePath == path);

            var claimTime = DateTime.UtcNow.AddSeconds(1);
            var claimed = Assert.Single(await restartedJournal.ClaimDueAsync(claimTime, TimeSpan.FromMinutes(5), maxCount: 1));
            Assert.NotNull(claimed.LeaseId);
            Assert.Empty(await journal.ClaimDueAsync(claimTime.AddSeconds(1), TimeSpan.FromMinutes(5)));
            Assert.True(await journal.RenewLeaseAsync(path, claimed.LeaseId!, claimTime.AddMinutes(4), TimeSpan.FromMinutes(5)));
            Assert.Empty(await restartedJournal.ClaimDueAsync(claimTime.AddMinutes(6), TimeSpan.FromMinutes(5)));

            var reclaimed = Assert.Single(await journal.ClaimDueAsync(claimTime.AddMinutes(10), TimeSpan.FromMinutes(5)));
            Assert.NotEqual(claimed.LeaseId, reclaimed.LeaseId);
            Assert.False(await journal.RenewLeaseAsync(path, claimed.LeaseId!, claimTime.AddMinutes(10), TimeSpan.FromMinutes(5)));

            Assert.False(await restartedJournal.CompleteClaimAsync(path, claimed.LeaseId!, claimed.Revision));
            await restartedJournal.DeferAsync(path, "stale worker", claimed.LeaseId!);
            var stillOwned = Assert.Single(await journal.GetDueAsync(claimTime.AddDays(1)));
            Assert.Equal(reclaimed.LeaseId, stillOwned.LeaseId);
            Assert.Equal(0, stillOwned.Attempts);

            await restartedJournal.DeferAsync(path, "temporary disk error");
            var retried = await journal.GetDueAsync(DateTime.UtcNow.AddSeconds(16));
            var entry = Assert.Single(retried);
            Assert.Equal(1, entry.Attempts);
            Assert.Equal("temporary disk error", entry.LastError);

            await journal.CompleteAsync(path);
            Assert.Empty(await restartedJournal.GetDueAsync(DateTime.UtcNow.AddDays(1)));

            await journal.EnqueueAsync(path);
            Assert.False(await restartedJournal.CompleteClaimAsync(path, reclaimed.LeaseId!, reclaimed.Revision));
            Assert.Single(await journal.GetDueAsync(DateTime.UtcNow.AddDays(1)));
        }
        finally
        {
            Environment.SetEnvironmentVariable("CRMFILO_STORAGE_ROOT", previousStorageRoot);
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Independent_workers_cannot_claim_the_same_due_file()
    {
        var root = Path.Combine(Path.GetTempPath(), "mkfiloservis-cleanup-race-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var previousStorageRoot = Environment.GetEnvironmentVariable("CRMFILO_STORAGE_ROOT");
        Environment.SetEnvironmentVariable("CRMFILO_STORAGE_ROOT", root);
        try
        {
            var provider = DataProtectionProvider.Create(new DirectoryInfo(Path.Combine(root, "dp-keys")));
            var environment = new TestWebHostEnvironment(root);
            var workerA = new FileCleanupJournal(provider, environment);
            var workerB = new FileCleanupJournal(provider, environment);
            await workerA.EnqueueAsync("uploads/shared.enc");

            var claimTime = DateTime.UtcNow.AddSeconds(1);
            var claims = await Task.WhenAll(
                workerA.ClaimDueAsync(claimTime, TimeSpan.FromMinutes(2), maxCount: 1),
                workerB.ClaimDueAsync(claimTime, TimeSpan.FromMinutes(2), maxCount: 1));

            Assert.Equal(1, claims.Sum(batch => batch.Count));
            var owner = Assert.Single(claims.SelectMany(batch => batch));
            Assert.NotNull(owner.LeaseId);
            Assert.Empty(await workerA.ClaimDueAsync(claimTime.AddSeconds(1), TimeSpan.FromMinutes(2), maxCount: 1));

            var upgraded = await workerB.EnqueueAsync(owner.RelativePath, waitForReferenceRemoval: true);
            Assert.False(await workerA.CompleteClaimAsync(owner.RelativePath, owner.LeaseId!, owner.Revision));
            Assert.True(Assert.Single(await workerB.GetDueAsync(claimTime)).WaitForReferenceRemoval);

            await workerB.CompleteAsync(owner.RelativePath);
            await workerB.EnqueueAsync(owner.RelativePath);
            Assert.False(await workerA.CompleteRequestAsync(upgraded));
            Assert.Single(await workerB.GetDueAsync(claimTime));
        }
        finally
        {
            Environment.SetEnvironmentVariable("CRMFILO_STORAGE_ROOT", previousStorageRoot);
            Directory.Delete(root, recursive: true);
        }
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
}
