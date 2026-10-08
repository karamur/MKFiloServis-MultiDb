using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using MKFiloServis.Web.Services;

namespace MKFiloServis.Tests;

public sealed class LocalObjectStorageSafetyTests
{
    [Fact]
    public async Task Upload_is_atomic_and_stays_inside_upload_root()
    {
        var (root, originalStorageRoot, service) = CreateService();
        try
        {
            byte[] content = [7, 8, 9];
            await service.UploadAsync("case/attachment.bin", content);

            var uploads = Path.Combine(root, "uploads");
            Assert.Equal(content, await File.ReadAllBytesAsync(Path.Combine(uploads, "case", "attachment.bin")));
            Assert.Single(Directory.GetFiles(uploads, "*", SearchOption.AllDirectories));
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.UploadAsync("../outside.bin", content));
            Assert.False(File.Exists(Path.Combine(root, "outside.bin")));
        }
        finally
        {
            Environment.SetEnvironmentVariable("CRMFILO_STORAGE_ROOT", originalStorageRoot);
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Delete_of_missing_local_object_is_idempotent()
    {
        var (root, originalStorageRoot, service) = CreateService();
        try
        {
            Directory.CreateDirectory(Path.Combine(root, "uploads"));
            await service.DeleteAsync("missing.bin");
            Assert.False(File.Exists(Path.Combine(root, "uploads", "missing.bin")));
        }
        finally
        {
            Environment.SetEnvironmentVariable("CRMFILO_STORAGE_ROOT", originalStorageRoot);
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Delete_of_object_under_missing_parent_is_idempotent()
    {
        var (root, originalStorageRoot, service) = CreateService();
        try
        {
            await service.DeleteAsync("never-created/nested/object.bin");
            Assert.False(Directory.Exists(Path.Combine(root, "uploads", "never-created")));
        }
        finally
        {
            Environment.SetEnvironmentVariable("CRMFILO_STORAGE_ROOT", originalStorageRoot);
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Read_and_exists_distinguish_missing_files_and_reject_unsigned_links()
    {
        var (root, originalStorageRoot, service) = CreateService();
        try
        {
            Assert.Null(await service.DownloadAsync("missing/object.bin"));
            Assert.False(await service.ExistsAsync("missing/object.bin"));
            await service.UploadAsync("case/object.bin", [1, 2, 3]);
            Assert.Equal([1, 2, 3], await service.DownloadAsync("case/object.bin"));
            Assert.True(await service.ExistsAsync("case/object.bin"));
            Assert.False(await service.ExistsAsync("case"));
            await Assert.ThrowsAsync<NotSupportedException>(() =>
                service.GetPresignedUrlAsync("case/object.bin"));
        }
        finally
        {
            Environment.SetEnvironmentVariable("CRMFILO_STORAGE_ROOT", originalStorageRoot);
            Directory.Delete(root, recursive: true);
        }
    }

    private static (string Root, string? PreviousStorageRoot, LocalObjectStorageService Service) CreateService()
    {
        var root = Path.Combine(Path.GetTempPath(), "mkfiloservis-local-object-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var previousStorageRoot = Environment.GetEnvironmentVariable("CRMFILO_STORAGE_ROOT");
        Environment.SetEnvironmentVariable("CRMFILO_STORAGE_ROOT", root);
        var environment = new TestWebHostEnvironment(root);
        var service = new LocalObjectStorageService(environment, NullLogger<LocalObjectStorageService>.Instance);
        return (root, previousStorageRoot, service);
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
