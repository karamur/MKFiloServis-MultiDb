using System.Text.Json;
using KOAFiloServis.Shared.Entities;
using KOAFiloServis.Web.Data;
using KOAFiloServis.Web.Helpers;
using Microsoft.EntityFrameworkCore;

namespace KOAFiloServis.Web.Services;

/// <summary>
/// Eski personel ve araç evraklarını yeni arşiv dizin yapısına taşır.
/// Copy-only stratejisi: eski dosyalar silinmez, şifreli olarak yeni konuma kopyalanır, DB path güncellenir.
/// </summary>
public class ArchiveMigrationService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ISecureFileService _secureFileService;
    private readonly ILogger<ArchiveMigrationService> _logger;

    public ArchiveMigrationService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        ISecureFileService secureFileService,
        ILogger<ArchiveMigrationService> logger)
    {
        _contextFactory = contextFactory;
        _secureFileService = secureFileService;
        _logger = logger;
    }

    // ── Modeller ────────────────────────────────────────────────────

    public class MigrationReport
    {
        public DateTime GeneratedAt { get; set; } = DateTime.Now;
        public bool IsDryRun { get; set; }
        public PersonelStats Personel { get; set; } = new();
        public AracStats Arac { get; set; } = new();
        public List<MigrationEntry> Entries { get; set; } = new();
    }

    public class PersonelStats
    {
        public int Total { get; set; }
        public int AlreadyInNewPath { get; set; }
        public int Migrated { get; set; }
        public int MissingSource { get; set; }
        public int Failed { get; set; }
    }

    public class AracStats
    {
        public int Total { get; set; }
        public int AlreadyInNewPath { get; set; }
        public int Migrated { get; set; }
        public int MissingSource { get; set; }
        public int Failed { get; set; }
    }

    public class MigrationEntry
    {
        public string Type { get; set; } = "";
        public int RecordId { get; set; }
        public string? SourcePath { get; set; }
        public string? TargetPath { get; set; }
        public string Status { get; set; } = ""; // Skipped | Pending | Copied | Missing | Failed
        public string? Error { get; set; }
        public string? DisplayName { get; set; }
    }

    // ── Public API ──────────────────────────────────────────────────

    public async Task<MigrationReport> AnalyzeAsync(CancellationToken ct = default)
    {
        var report = new MigrationReport { IsDryRun = true };
        await AnalyzePersonelAsync(report, ct);
        await AnalyzeAracAsync(report, ct);
        return report;
    }

    public async Task<MigrationReport> ExecuteAsync(bool dryRun, CancellationToken ct = default)
    {
        var report = new MigrationReport { IsDryRun = dryRun };
        await MigratePersonelAsync(report, dryRun, ct);
        await MigrateAracAsync(report, dryRun, ct);
        return report;
    }

    // ── Normalize helper ────────────────────────────────────────────

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

    private static string BuildPersonelTargetPath(PersonelOzlukEvrak evrak)
    {
        var ad = evrak.Sofor?.Ad ?? "PERSONEL";
        var soyad = evrak.Sofor?.Soyad ?? evrak.SoforId.ToString();
        var firma = evrak.Sofor?.Firma?.FirmaAdi;
        var klasor = AppStoragePaths.BuildPersonelArsivKlasoru(ad, soyad, firma);
        var evrakAdi = evrak.EvrakTanim?.EvrakAdi ?? "Evrak";
        var uzanti = Path.GetExtension((evrak.DosyaYolu ?? ".pdf").Replace(".enc", ""));
        if (string.IsNullOrEmpty(uzanti)) uzanti = ".pdf";
        var ts = (evrak.CreatedAt == default ? DateTime.Now : evrak.CreatedAt).ToString("yyyyMMdd_HHmmss");
        var dosyaAdi = $"{Norm(ad, true)}{Norm(soyad, true)}{Norm(evrakAdi, true)}_{ts}{uzanti}.enc";
        return $"{AppStoragePaths.PersonelEvrakRelativeRoot}/{klasor}/{dosyaAdi}";
    }

    private static string BuildAracTargetPath(AracEvrakDosya dosya)
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
        return $"{AppStoragePaths.AracEvrakRelativeRoot}/{klasor}/{dosyaAdi}";
    }

    // ── Personel ────────────────────────────────────────────────────

    private async Task AnalyzePersonelAsync(MigrationReport report, CancellationToken ct)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct);
        var evraklar = await ctx.PersonelOzlukEvraklar
            .IgnoreQueryFilters()
            .Include(e => e.Sofor).ThenInclude(s => s.Firma)
            .Include(e => e.EvrakTanim)
            .Where(e => !string.IsNullOrWhiteSpace(e.DosyaYolu))
            .ToListAsync(ct);

        report.Personel.Total = evraklar.Count;
        foreach (var evrak in evraklar)
            AnalyzePersonelEntry(evrak, report);
    }

    private void AnalyzePersonelEntry(PersonelOzlukEvrak evrak, MigrationReport report)
    {
        var entry = new MigrationEntry
        {
            Type = "Personel",
            RecordId = evrak.Id,
            SourcePath = evrak.DosyaYolu,
            DisplayName = $"{evrak.Sofor?.TamAd} - {evrak.EvrakTanim?.EvrakAdi}"
        };

        var n = (evrak.DosyaYolu ?? "").Replace('\\', '/').TrimStart('/');
        if (n.StartsWith(AppStoragePaths.PersonelEvrakRelativeRoot + "/", StringComparison.OrdinalIgnoreCase))
        {
            entry.Status = "Skipped";
            entry.TargetPath = evrak.DosyaYolu;
            report.Personel.AlreadyInNewPath++;
            report.Entries.Add(entry);
            return;
        }

        if (!_secureFileService.ExistsAsync(evrak.DosyaYolu).Result)
        {
            entry.Status = "Missing";
            entry.Error = "Kaynak dosya bulunamadı.";
            report.Personel.MissingSource++;
            report.Entries.Add(entry);
            return;
        }

        entry.TargetPath = BuildPersonelTargetPath(evrak);
        entry.Status = "Pending";
        report.Entries.Add(entry);
    }

    private async Task MigratePersonelAsync(MigrationReport report, bool dryRun, CancellationToken ct)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct);
        var evraklar = await ctx.PersonelOzlukEvraklar
            .IgnoreQueryFilters()
            .Include(e => e.Sofor).ThenInclude(s => s.Firma)
            .Include(e => e.EvrakTanim)
            .Where(e => !string.IsNullOrWhiteSpace(e.DosyaYolu))
            .ToListAsync(ct);

        report.Personel.Total = evraklar.Count;

        foreach (var evrak in evraklar)
        {
            ct.ThrowIfCancellationRequested();
            var entry = new MigrationEntry
            {
                Type = "Personel", RecordId = evrak.Id,
                SourcePath = evrak.DosyaYolu,
                DisplayName = $"{evrak.Sofor?.TamAd} - {evrak.EvrakTanim?.EvrakAdi}"
            };

            var n = (evrak.DosyaYolu ?? "").Replace('\\', '/').TrimStart('/');
            if (n.StartsWith(AppStoragePaths.PersonelEvrakRelativeRoot + "/", StringComparison.OrdinalIgnoreCase))
            {
                entry.Status = "Skipped"; entry.TargetPath = evrak.DosyaYolu;
                report.Personel.AlreadyInNewPath++; report.Entries.Add(entry); continue;
            }

            if (!await _secureFileService.ExistsAsync(evrak.DosyaYolu, ct))
            {
                entry.Status = "Missing"; entry.Error = "Kaynak dosya bulunamadı.";
                report.Personel.MissingSource++; report.Entries.Add(entry); continue;
            }

            entry.TargetPath = BuildPersonelTargetPath(evrak);
            if (dryRun) { entry.Status = "Pending"; report.Entries.Add(entry); continue; }

            try
            {
                var targetDir = Path.GetDirectoryName(entry.TargetPath)!.Replace('\\', '/');
                var targetName = Path.GetFileName(entry.TargetPath);
                var newPath = await _secureFileService.CopyEncryptedAsync(evrak.DosyaYolu!, targetDir, targetName, ct);

                await using var uCtx = await _contextFactory.CreateDbContextAsync(ct);
                await using var tx = await uCtx.Database.BeginTransactionAsync(ct);
                try
                {
                    var db = await uCtx.PersonelOzlukEvraklar.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == evrak.Id, ct);
                    if (db != null) { db.DosyaYolu = newPath; db.DosyaAdi = Path.GetFileName(newPath); db.UpdatedAt = DateTime.UtcNow; }
                    await uCtx.SaveChangesAsync(ct);
                    await tx.CommitAsync(ct);
                    entry.Status = "Copied"; entry.TargetPath = newPath;
                    report.Personel.Migrated++;
                }
                catch { await tx.RollbackAsync(ct); throw; }
            }
            catch (Exception ex)
            {
                entry.Status = "Failed"; entry.Error = ex.Message;
                report.Personel.Failed++;
                _logger.LogError(ex, "Personel evrak taşıma hatası Id={Id}", evrak.Id);
            }
            report.Entries.Add(entry);
        }
    }

    // ── Araç ────────────────────────────────────────────────────────

    private async Task AnalyzeAracAsync(MigrationReport report, CancellationToken ct)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct);
        var evraklar = await ctx.AracEvrakDosyalari
            .IgnoreQueryFilters()
            .Include(d => d.AracEvrak).ThenInclude(e => e.Arac).ThenInclude(a => a.Firma)
            .Where(d => !string.IsNullOrWhiteSpace(d.DosyaYolu))
            .ToListAsync(ct);

        report.Arac.Total = evraklar.Count;
        foreach (var dosya in evraklar)
            AnalyzeAracEntry(dosya, report);
    }

    private void AnalyzeAracEntry(AracEvrakDosya dosya, MigrationReport report)
    {
        var entry = new MigrationEntry
        {
            Type = "Arac", RecordId = dosya.Id, SourcePath = dosya.DosyaYolu,
            DisplayName = $"{(dosya.AracEvrak?.Arac?.AktifPlaka ?? "?")} - {dosya.AracEvrak?.EvrakKategorisi}"
        };

        var n = (dosya.DosyaYolu ?? "").Replace('\\', '/').TrimStart('/');
        if (n.StartsWith(AppStoragePaths.AracEvrakRelativeRoot + "/", StringComparison.OrdinalIgnoreCase))
        {
            entry.Status = "Skipped"; entry.TargetPath = dosya.DosyaYolu;
            report.Arac.AlreadyInNewPath++; report.Entries.Add(entry); return;
        }

        if (!_secureFileService.ExistsAsync(dosya.DosyaYolu).Result)
        {
            entry.Status = "Missing"; entry.Error = "Kaynak dosya bulunamadı.";
            report.Arac.MissingSource++; report.Entries.Add(entry); return;
        }

        entry.TargetPath = BuildAracTargetPath(dosya);
        entry.Status = "Pending";
        report.Entries.Add(entry);
    }

    private async Task MigrateAracAsync(MigrationReport report, bool dryRun, CancellationToken ct)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct);
        var evraklar = await ctx.AracEvrakDosyalari
            .IgnoreQueryFilters()
            .Include(d => d.AracEvrak).ThenInclude(e => e.Arac).ThenInclude(a => a.Firma)
            .Where(d => !string.IsNullOrWhiteSpace(d.DosyaYolu))
            .ToListAsync(ct);

        report.Arac.Total = evraklar.Count;

        foreach (var dosya in evraklar)
        {
            ct.ThrowIfCancellationRequested();
            var entry = new MigrationEntry
            {
                Type = "Arac", RecordId = dosya.Id, SourcePath = dosya.DosyaYolu,
                DisplayName = $"{(dosya.AracEvrak?.Arac?.AktifPlaka ?? "?")} - {dosya.AracEvrak?.EvrakKategorisi}"
            };

            var n = (dosya.DosyaYolu ?? "").Replace('\\', '/').TrimStart('/');
            if (n.StartsWith(AppStoragePaths.AracEvrakRelativeRoot + "/", StringComparison.OrdinalIgnoreCase))
            {
                entry.Status = "Skipped"; entry.TargetPath = dosya.DosyaYolu;
                report.Arac.AlreadyInNewPath++; report.Entries.Add(entry); continue;
            }

            if (!await _secureFileService.ExistsAsync(dosya.DosyaYolu, ct))
            {
                entry.Status = "Missing"; entry.Error = "Kaynak dosya bulunamadı.";
                report.Arac.MissingSource++; report.Entries.Add(entry); continue;
            }

            entry.TargetPath = BuildAracTargetPath(dosya);
            if (dryRun) { entry.Status = "Pending"; report.Entries.Add(entry); continue; }

            try
            {
                var targetDir = Path.GetDirectoryName(entry.TargetPath)!.Replace('\\', '/');
                var targetName = Path.GetFileName(entry.TargetPath);
                var newPath = await _secureFileService.CopyEncryptedAsync(dosya.DosyaYolu!, targetDir, targetName, ct);

                await using var uCtx = await _contextFactory.CreateDbContextAsync(ct);
                await using var tx = await uCtx.Database.BeginTransactionAsync(ct);
                try
                {
                    var db = await uCtx.AracEvrakDosyalari.IgnoreQueryFilters().FirstOrDefaultAsync(d => d.Id == dosya.Id, ct);
                    if (db != null) { db.DosyaYolu = newPath; db.DosyaAdi = Path.GetFileName(newPath); db.UpdatedAt = DateTime.UtcNow; }
                    await uCtx.SaveChangesAsync(ct);
                    await tx.CommitAsync(ct);
                    entry.Status = "Copied"; entry.TargetPath = newPath;
                    report.Arac.Migrated++;
                }
                catch { await tx.RollbackAsync(ct); throw; }
            }
            catch (Exception ex)
            {
                entry.Status = "Failed"; entry.Error = ex.Message;
                report.Arac.Failed++;
                _logger.LogError(ex, "Araç evrak taşıma hatası Id={Id}", dosya.Id);
            }
            report.Entries.Add(entry);
        }
    }

    // ── Rapor ───────────────────────────────────────────────────────

    public async Task<string> SaveReportAsync(MigrationReport report, CancellationToken ct = default)
    {
        var reportDir = Path.Combine(AppStoragePaths.DefaultStorageRoot, "Arsiv", "MigrationReports");
        Directory.CreateDirectory(reportDir);
        var fileName = $"archive_migration_{DateTime.Now:yyyyMMdd_HHmmss}.json";
        var filePath = Path.Combine(reportDir, fileName);
        var json = JsonSerializer.Serialize(report, new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });
        await File.WriteAllTextAsync(filePath, json, ct);
        _logger.LogInformation("Migration raporu kaydedildi: {Path}", filePath);
        return filePath;
    }
}
