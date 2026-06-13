using KOAFiloServis.Shared.Entities;
using KOAFiloServis.Web.Data;
using KOAFiloServis.Web.Helpers;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;

namespace KOAFiloServis.Tests.Services;

/// <summary>
/// GERÇEK ARŞİV TAŞIMA: Eski evrakları yeni dizin yapısına COPY ile taşır, DB path'leri günceller.
/// Eski dosyalar SİLİNMEZ. Transaction ile çalışır.
/// </summary>
public class ArchiveMigrationExecuteTests
{
    private readonly ITestOutputHelper _output;
    private const string ConnectionString =
        "Host=localhost;Port=5432;Database=KOAFiloServis;Username=postgres;Password=Fast123";
    private static readonly DbContextOptions<ApplicationDbContext> DbOptions =
        new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(ConnectionString).Options;

    public ArchiveMigrationExecuteTests(ITestOutputHelper output) => _output = output;

    private ApplicationDbContext CreateContext() => new(DbOptions);
    private static readonly string StorageRoot = AppStoragePaths.DefaultStorageRoot;
    private readonly List<string> _createdDirs = new();
    private readonly List<string> _copiedFiles = new();

    private static string Norm(string value, bool removeSpaces = false)
    {
        if (string.IsNullOrWhiteSpace(value)) return "BILINMIYOR";
        var result = value.Trim()
            .Replace('Ç', 'C').Replace('Ğ', 'G').Replace('İ', 'I')
            .Replace('Ö', 'O').Replace('Ş', 'S').Replace('Ü', 'U')
            .Replace('ç', 'c').Replace('ğ', 'g').Replace('ı', 'i')
            .Replace('ö', 'o').Replace('ş', 's').Replace('ü', 'u');
        foreach (var c in Path.GetInvalidFileNameChars()) result = result.Replace(c, '-');
        while (result.Contains("  ")) result = result.Replace("  ", " ");
        result = result.Trim();
        if (removeSpaces) result = result.Replace(" ", "");
        return result;
    }

    private static string ResolveSourceFullPath(string relativePath)
    {
        var n = relativePath.Replace('\\', '/').TrimStart('/');
        if (n.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase))
            n = n["uploads/".Length..];

        string root;
        if (n.StartsWith("Arsiv/", StringComparison.OrdinalIgnoreCase))
            root = StorageRoot;
        else
            root = Path.Combine(StorageRoot, "uploads");

        return Path.GetFullPath(Path.Combine(root, n.Replace('/', Path.DirectorySeparatorChar)));
    }

    private static string ResolveTargetFullPath(string relativePath)
    {
        var n = relativePath.Replace('\\', '/').TrimStart('/');
        string root;
        if (n.StartsWith("Arsiv/", StringComparison.OrdinalIgnoreCase))
            root = StorageRoot;
        else
            root = Path.Combine(StorageRoot, "uploads");
        return Path.GetFullPath(Path.Combine(root, n.Replace('/', Path.DirectorySeparatorChar)));
    }

    /// <summary>
    /// EXECUTE: Personel ve araç evraklarını yeni dizinlere COPY ile taşır, DB günceller.
    /// </summary>
    [Fact]
    public async Task Execute_MigrateAllEvrak()
    {
        _output.WriteLine("═══════════════════════════════════════════");
        _output.WriteLine("  ARŞİV TAŞIMA EXECUTE BAŞLADI");
        _output.WriteLine($"  {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        _output.WriteLine("═══════════════════════════════════════════");
        _output.WriteLine("");

        var personelCopied = 0;
        var personelSkipped = 0;
        var personelFailed = 0;
        var aracCopied = 0;
        var aracSkipped = 0;
        var aracFailed = 0;
        var errors = new List<string>();

        // ─── PERSONEL ──────────────────────────────────────────────
        _output.WriteLine("─── PERSONEL EVRAK TAŞIMA ───");
        await using (var ctx = CreateContext())
        {
            var evraklar = await ctx.PersonelOzlukEvraklar
                .IgnoreQueryFilters()
                .Include(e => e.Sofor).ThenInclude(s => s.Firma)
                .Include(e => e.EvrakTanim)
                .Where(e => !string.IsNullOrWhiteSpace(e.DosyaYolu) && !e.IsDeleted)
                .ToListAsync();

            _output.WriteLine($"  Toplam kayıt: {evraklar.Count}");

            foreach (var evrak in evraklar)
            {
                try
                {
                    // Hedef path hesapla
                    var ad = evrak.Sofor?.Ad ?? "PERSONEL";
                    var soyad = evrak.Sofor?.Soyad ?? evrak.SoforId.ToString();
                    var firma = evrak.Sofor?.Firma?.FirmaAdi;
                    var klasor = AppStoragePaths.BuildPersonelArsivKlasoru(ad, soyad, firma);
                    var evrakAdi = evrak.EvrakTanim?.EvrakAdi ?? "Evrak";
                    var uzanti = Path.GetExtension((evrak.DosyaYolu ?? ".pdf").Replace(".enc", ""));
                    if (string.IsNullOrEmpty(uzanti)) uzanti = ".pdf";
                    var ts = (evrak.CreatedAt == default ? DateTime.Now : evrak.CreatedAt).ToString("yyyyMMdd_HHmmss");
                    var dosyaAdi = $"{Norm(ad, true)}{Norm(soyad, true)}{Norm(evrakAdi, true)}_{ts}{uzanti}.enc";
                    var targetRel = $"{AppStoragePaths.PersonelEvrakRelativeRoot}/{klasor}/{dosyaAdi}";

                    var oldRel = evrak.DosyaYolu!;
                    var oldRelNorm = oldRel.Replace('\\', '/').TrimStart('/');
                    var newRelNorm = targetRel.Replace('\\', '/').TrimStart('/');

                    if (string.Equals(oldRelNorm, newRelNorm, StringComparison.OrdinalIgnoreCase))
                    {
                        personelSkipped++;
                        continue;
                    }

                    // Kaynak dosyayı bul
                    var sourceFull = ResolveSourceFullPath(oldRel);
                    if (!File.Exists(sourceFull))
                    {
                        // Fallback
                        sourceFull = Path.Combine(StorageRoot, "uploads", oldRel.Replace('\\', '/').TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                    }
                    if (!File.Exists(sourceFull))
                    {
                        personelFailed++;
                        errors.Add($"Personel Id={evrak.Id}: Kaynak bulunamadı: {oldRel}");
                        continue;
                    }

                    // Hedef dizini oluştur
                    var targetFull = ResolveTargetFullPath(targetRel);
                    var targetDir = Path.GetDirectoryName(targetFull)!;
                    if (!Directory.Exists(targetDir))
                    {
                        Directory.CreateDirectory(targetDir);
                        _createdDirs.Add(targetDir);
                    }

                    // Çakışma çöz
                    var finalTargetFull = targetFull;
                    var finalTargetRel = targetRel;
                    var counter = 1;
                    var nameNoExt = Path.GetFileNameWithoutExtension(targetFull);
                    var ext = Path.GetExtension(targetFull);
                    while (File.Exists(finalTargetFull))
                    {
                        finalTargetFull = Path.Combine(targetDir, $"{nameNoExt}_{counter:D3}{ext}");
                        finalTargetRel = targetRel.Replace(dosyaAdi, $"{nameNoExt}_{counter:D3}{ext}");
                        counter++;
                    }

                    // KOPYALA
                    File.Copy(sourceFull, finalTargetFull, overwrite: false);
                    _copiedFiles.Add(finalTargetFull);

                    // DB güncelle (transaction)
                    await using var uCtx = CreateContext();
                    await using var tx = await uCtx.Database.BeginTransactionAsync();
                    try
                    {
                        var db = await uCtx.PersonelOzlukEvraklar.IgnoreQueryFilters()
                            .FirstOrDefaultAsync(e => e.Id == evrak.Id);
                        if (db != null)
                        {
                            db.DosyaYolu = finalTargetRel;
                            db.DosyaAdi = Path.GetFileName(finalTargetRel);
                            db.UpdatedAt = DateTime.UtcNow;
                            await uCtx.SaveChangesAsync();
                        }
                        await tx.CommitAsync();
                        personelCopied++;
                    }
                    catch
                    {
                        await tx.RollbackAsync();
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    personelFailed++;
                    errors.Add($"Personel Id={evrak.Id}: {ex.Message}");
                }
            }
        }

        _output.WriteLine($"  Kopyalanan: {personelCopied}");
        _output.WriteLine($"  Atlanan (zaten yeni): {personelSkipped}");
        _output.WriteLine($"  Başarısız: {personelFailed}");
        _output.WriteLine("");

        // ─── ARAÇ ──────────────────────────────────────────────────
        _output.WriteLine("─── ARAÇ EVRAK TAŞIMA ───");
        await using (var ctx = CreateContext())
        {
            var evraklar = await ctx.AracEvrakDosyalari
                .IgnoreQueryFilters()
                .Include(d => d.AracEvrak).ThenInclude(e => e.Arac).ThenInclude(a => a.Firma)
                .Where(d => !string.IsNullOrWhiteSpace(d.DosyaYolu) && !d.IsDeleted)
                .ToListAsync();

            _output.WriteLine($"  Toplam kayıt: {evraklar.Count}");

            foreach (var dosya in evraklar)
            {
                try
                {
                    var plaka = dosya.AracEvrak?.Arac?.AktifPlaka
                                ?? dosya.AracEvrak?.Arac?.SaseNo
                                ?? dosya.AracEvrak?.AracId.ToString()
                                ?? "ARAC";
                    var firma = dosya.AracEvrak?.Arac?.Firma?.FirmaAdi;
                    var klasor = AppStoragePaths.BuildAracArsivKlasoru(plaka, firma);
                    var kategori = dosya.AracEvrak?.EvrakKategorisi ?? "Evrak";
                    var uzanti = Path.GetExtension((dosya.DosyaYolu ?? ".pdf").Replace(".enc", ""));
                    if (string.IsNullOrEmpty(uzanti)) uzanti = ".pdf";
                    var ts = (dosya.CreatedAt == default ? DateTime.Now : dosya.CreatedAt).ToString("yyyyMMdd_HHmmss");
                    var dosyaAdi = $"{Norm(plaka, true)}{Norm(kategori, true)}_{ts}{uzanti}.enc";
                    var targetRel = $"{AppStoragePaths.AracEvrakRelativeRoot}/{klasor}/{dosyaAdi}";

                    var oldRel = dosya.DosyaYolu!;
                    var oldRelNorm = oldRel.Replace('\\', '/').TrimStart('/');
                    var newRelNorm = targetRel.Replace('\\', '/').TrimStart('/');

                    if (string.Equals(oldRelNorm, newRelNorm, StringComparison.OrdinalIgnoreCase))
                    {
                        aracSkipped++;
                        continue;
                    }

                    var sourceFull = ResolveSourceFullPath(oldRel);
                    if (!File.Exists(sourceFull))
                    {
                        sourceFull = Path.Combine(StorageRoot, "uploads", oldRel.Replace('\\', '/').TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                    }
                    if (!File.Exists(sourceFull))
                    {
                        aracFailed++;
                        errors.Add($"Araç Id={dosya.Id}: Kaynak bulunamadı: {oldRel}");
                        continue;
                    }

                    var targetFull = ResolveTargetFullPath(targetRel);
                    var targetDir = Path.GetDirectoryName(targetFull)!;
                    if (!Directory.Exists(targetDir))
                    {
                        Directory.CreateDirectory(targetDir);
                        _createdDirs.Add(targetDir);
                    }

                    var finalTargetFull = targetFull;
                    var finalTargetRel = targetRel;
                    var counter = 1;
                    var nameNoExt = Path.GetFileNameWithoutExtension(targetFull);
                    var ext = Path.GetExtension(targetFull);
                    while (File.Exists(finalTargetFull))
                    {
                        finalTargetFull = Path.Combine(targetDir, $"{nameNoExt}_{counter:D3}{ext}");
                        finalTargetRel = targetRel.Replace(dosyaAdi, $"{nameNoExt}_{counter:D3}{ext}");
                        counter++;
                    }

                    File.Copy(sourceFull, finalTargetFull, overwrite: false);
                    _copiedFiles.Add(finalTargetFull);

                    await using var uCtx = CreateContext();
                    await using var tx = await uCtx.Database.BeginTransactionAsync();
                    try
                    {
                        var db = await uCtx.AracEvrakDosyalari.IgnoreQueryFilters()
                            .FirstOrDefaultAsync(d => d.Id == dosya.Id);
                        if (db != null)
                        {
                            db.DosyaYolu = finalTargetRel;
                            db.DosyaAdi = Path.GetFileName(finalTargetRel);
                            db.UpdatedAt = DateTime.UtcNow;
                            await uCtx.SaveChangesAsync();
                        }
                        await tx.CommitAsync();
                        aracCopied++;
                    }
                    catch
                    {
                        await tx.RollbackAsync();
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    aracFailed++;
                    errors.Add($"Araç Id={dosya.Id}: {ex.Message}");
                }
            }
        }

        _output.WriteLine($"  Kopyalanan: {aracCopied}");
        _output.WriteLine($"  Atlanan (zaten yeni): {aracSkipped}");
        _output.WriteLine($"  Başarısız: {aracFailed}");
        _output.WriteLine("");

        // ─── ÖZET ──────────────────────────────────────────────────
        _output.WriteLine("═══════════════════════════════════════════");
        _output.WriteLine("  EXECUTE TAMAMLANDI");
        _output.WriteLine("═══════════════════════════════════════════");
        _output.WriteLine($"Personel: Kopyalanan={personelCopied} Atlanan={personelSkipped} Başarısız={personelFailed}");
        _output.WriteLine($"Araç:     Kopyalanan={aracCopied} Atlanan={aracSkipped} Başarısız={aracFailed}");
        _output.WriteLine($"Toplam kopyalanan: {personelCopied + aracCopied}");
        _output.WriteLine($"Yeni dizinler: {_createdDirs.Count}");
        _output.WriteLine($"Hedef kök: {StorageRoot}\\Arsiv\\Sifreli\\");
        _output.WriteLine("");

        if (errors.Any())
        {
            _output.WriteLine("─── HATALAR ───");
            foreach (var e in errors)
                _output.WriteLine($"  {e}");
        }

        // Doğrulama
        var personelToplam = personelCopied + personelSkipped + personelFailed;
        var aracToplam = aracCopied + aracSkipped + aracFailed;
        Assert.Equal(90, personelToplam);
        Assert.Equal(87, aracToplam);
        Assert.Empty(errors);
    }
}
