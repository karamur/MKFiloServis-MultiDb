using MKFiloServis.Web.Services;

namespace MKFiloServis.Tests;

public sealed class SecureFileOrphanScannerTests
{
    [Fact]
    public void Reports_unreferenced_encrypted_files_across_storage_areas_without_mutating_them()
    {
        var root = Path.Combine(Path.GetTempPath(), "mkfiloservis-orphan-scan-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var uploads = Path.Combine(root, "uploads", "ebys");
            var archive = Path.Combine(root, "Arsiv", "Sifreli", "Personeller");
            var repository = Path.Combine(root, "Depo", "batch-1");
            Directory.CreateDirectory(uploads);
            Directory.CreateDirectory(archive);
            Directory.CreateDirectory(repository);

            var referenced = Path.Combine(uploads, "used.enc");
            var orphan = Path.Combine(uploads, "orphan.enc");
            var archiveOrphan = Path.Combine(archive, "old.enc");
            var repositoryOrphan = Path.Combine(repository, "recover.enc");
            var temporary = Path.Combine(uploads, "upload.tmp");
            var legacy = Path.Combine(uploads, "old.pdf");
            var referencedLegacy = Path.Combine(archive, "current.xml");
            File.WriteAllBytes(referenced, [1, 2]);
            File.WriteAllBytes(orphan, [3, 4, 5]);
            File.WriteAllBytes(archiveOrphan, [6]);
            File.WriteAllBytes(repositoryOrphan, [7, 8]);
            File.WriteAllBytes(temporary, [9]);
            File.WriteAllBytes(legacy, [10]);
            File.WriteAllBytes(referencedLegacy, [11]);

            var results = SecureFileOrphanScanner.FindOrphans(root,
                ["/uploads/ebys/used.enc", "Arsiv/Sifreli/Personeller/missing.enc"]);

            Assert.Equal(3, results.Count);
            Assert.Contains(results, item => item.StoragePath == "uploads/ebys/orphan.enc" && item.Size == 3);
            Assert.Contains(results, item => item.StoragePath == "Arsiv/Sifreli/Personeller/old.enc");
            Assert.Contains(results, item => item.StoragePath == "Depo/batch-1/recover.enc");
            Assert.Equal(new byte[] { 3, 4, 5 }, File.ReadAllBytes(orphan));
            Assert.True(File.Exists(archiveOrphan));
            Assert.True(File.Exists(repositoryOrphan));
            Assert.DoesNotContain(results, item => item.StoragePath.EndsWith("used.enc", StringComparison.Ordinal));
            Assert.DoesNotContain(results, item => item.StoragePath.EndsWith("upload.tmp", StringComparison.Ordinal));
            Assert.Contains("uploads/ebys/used.enc",
                SecureFileOrphanScanner.GetStorageKeyAliases(root, "\\uploads\\ebys\\used.enc"));
            Assert.Contains("uploads/ebys/used.enc",
                SecureFileOrphanScanner.GetStorageKeyAliases(root, referenced));

            var plaintextCandidates = SecureFileOrphanScanner.FindLegacyPlaintextCandidates(root,
                [referencedLegacy]);
            var plaintext = Assert.Single(plaintextCandidates);
            Assert.Equal("uploads/ebys/old.pdf", plaintext.StoragePath);
            Assert.True(File.Exists(legacy));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
