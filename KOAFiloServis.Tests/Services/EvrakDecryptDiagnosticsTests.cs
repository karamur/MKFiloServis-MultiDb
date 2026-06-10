using FluentAssertions;
using KOAFiloServis.Web.Helpers;
using KOAFiloServis.Web.Services.Security;
using Microsoft.Extensions.Logging.Abstractions;

namespace KOAFiloServis.Tests.Services;

public class EvrakDecryptDiagnosticsTests
{
    private static string KeysRoot => @"C:\KOAFiloServis_yedekleme\keys";
    private static string UploadsRoot => @"C:\KOAFiloServis_yedekleme\uploads";

    /// <summary>
    /// Tüm master key dosyalarını dener, çalışan key'i bulur, gerçek .enc dosyayı decrypt eder.
    /// </summary>
    [Fact]
    public async Task FindWorkingKey_AndDecryptRealFile()
    {
        // Key dosyaları — en derin backup (orijinal April 22 key'i) önce dene
        var keyFiles = Directory.GetFiles(KeysRoot, "master.key*")
            .OrderByDescending(f => f.Length) // en uzun isim = en derin backup zinciri = orijinal key
            .ThenBy(f => new FileInfo(f).LastWriteTime) // en eski tarih önce
            .ToList();
        keyFiles.Should().NotBeEmpty();

        // İlk enc dosyayı bul
        var encFiles = Directory.GetFiles(UploadsRoot, "*.pdf.enc", SearchOption.AllDirectories).Take(5).ToList();
        encFiles.Should().NotBeEmpty();

        string? workingKey = null;
        byte[]? decryptedPdf = null;

        foreach (var kf in keyFiles)
        {
            try
            {
                var kp = new DpapiMasterKeyProvider(kf, NullLogger<DpapiMasterKeyProvider>.Instance);
                var protector = new AesGcmFileProtector(kp);
                var testFile = encFiles.First();
                var rawBytes = await File.ReadAllBytesAsync(testFile);
                var plain = protector.Unprotect(rawBytes);
                var header = System.Text.Encoding.ASCII.GetString(plain, 0, 4);
                if (header == "%PDF")
                {
                    workingKey = kf;
                    decryptedPdf = plain;
                    break;
                }
            }
            catch (System.Security.Cryptography.CryptographicException) { /* bu key çalışmadı */ }
        }

        workingKey.Should().NotBeNull(
            $"Çalışan bir master key bulunmalı. Denenenler:\n{string.Join("\n", keyFiles.Select(f => $"  {Path.GetFileName(f)} ({new FileInfo(f).LastWriteTime})"))}");

        decryptedPdf.Should().NotBeNull();
        decryptedPdf!.Length.Should().BeGreaterThan(100);
        System.Text.Encoding.ASCII.GetString(decryptedPdf, 0, 4).Should().Be("%PDF");

        Console.WriteLine($"\nÇALIŞAN KEY: {workingKey}");
        Console.WriteLine($"DECRYPT BOYUT: {decryptedPdf.Length} bytes");
        Console.WriteLine($"PDF HEADER: %PDF ✅");
    }

    /// <summary>
    /// Mevcut master.key ile tüm KOA1 dosyalarını test eder.
    /// Hangi key'in çalıştığını tespit edip raporlar.
    /// </summary>
    [Fact]
    public async Task ReportAllKeyStatuses()
    {
        var encFiles = Directory.GetFiles(UploadsRoot, "*.pdf.enc", SearchOption.AllDirectories).Take(30).ToList();
        var keyFiles = Directory.GetFiles(KeysRoot, "master.key*").OrderBy(f => f).ToList();

        Console.WriteLine($"\nKey dosyaları ({keyFiles.Count}):");
        foreach (var kf in keyFiles)
            Console.WriteLine($"  {Path.GetFileName(kf)}: {new FileInfo(kf).Length}B, Modified={new FileInfo(kf).LastWriteTime}");

        Console.WriteLine($"\nEnc dosyalar (ilk {encFiles.Count}):");

        // Her key için kaç dosya decrypt edilebiliyor?
        foreach (var kf in keyFiles)
        {
            int ok = 0, fail = 0, notKoa1 = 0;
            try
            {
                var kp = new DpapiMasterKeyProvider(kf, NullLogger<DpapiMasterKeyProvider>.Instance);
                var protector = new AesGcmFileProtector(kp);

                foreach (var ef in encFiles)
                {
                    var rawBytes = await File.ReadAllBytesAsync(ef);
                    if (rawBytes.Length < 33) continue;
                    if (System.Text.Encoding.ASCII.GetString(rawBytes, 0, 4) != "KOA1") { notKoa1++; continue; }
                    try { protector.Unprotect(rawBytes); ok++; }
                    catch (System.Security.Cryptography.CryptographicException) { fail++; }
                }
            }
            catch { }

            Console.WriteLine($"  Key={Path.GetFileName(kf)}: OK={ok}, FAIL={fail}, NotKOA1={notKoa1}");
        }
    }
}
