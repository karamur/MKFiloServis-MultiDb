using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using System.IO.Compression;
using System.Text;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Web.Services;

public interface ITasimaTedarikciService
{
    // Tedarikçi CRUD
    Task<List<TasimaTedarikci>> GetAllAsync(bool sadeceAktif = false);
    Task<TasimaTedarikci?> GetAsync(int id);
    Task<TasimaTedarikci> CreateAsync(TasimaTedarikci tedarikci);
    Task<TasimaTedarikci> UpdateAsync(TasimaTedarikci tedarikci);
    Task<TasimaTedarikci> CreateFromCariAsync(int cariId, bool updateIfExists = true);
    Task<TasimaTedarikci> CreateFromPersonelAsync(int personelId, bool updateIfExists = true);
    Task DeleteAsync(int id);
    Task<string> GenerateTedarikciKoduAsync();

    // İş (Tedarikçi-Güzergah eşleşmesi) CRUD
    Task<List<TasimaTedarikciIs>> GetIslerAsync(int? tedarikciId = null);
    Task<TasimaTedarikciIs?> GetIsAsync(int id);
    Task<TasimaTedarikciIs> CreateIsAsync(TasimaTedarikciIs tedarikciIs);
    Task<TasimaTedarikciIs> UpdateIsAsync(TasimaTedarikciIs tedarikciIs);
    Task DeleteIsAsync(int id);

    // Tedarikçiye bağlı personel/araç (mevcut Sofor/Arac kayıtları üzerinden)
    Task<List<Sofor>> GetTedarikciPersonelleriAsync(int tedarikciId);
    Task<List<Arac>> GetTedarikciAraclariAsync(int tedarikciId);

    // Tedarikçi firma evrak CRUD
    Task<List<TedarikciEvrak>> GetTedarikciEvraklariAsync(int tedarikciId);
    Task<List<TedarikciEvrak>> GetAllTedarikciEvraklariAsync();
    Task<TedarikciEvrak> CreateTedarikciEvrakAsync(TedarikciEvrak evrak);
    Task<TedarikciEvrak> UpdateTedarikciEvrakAsync(TedarikciEvrak evrak);
    Task DeleteTedarikciEvrakAsync(int evrakId);

    // Tedarikçi evrak dosya işlemleri
    Task<TedarikciEvrakDosya> UploadTedarikciEvrakDosyaAsync(int evrakId, IBrowserFile file);
    Task<byte[]> GetTedarikciEvrakDosyaAsync(int dosyaId);
    Task DeleteTedarikciEvrakDosyaAsync(int dosyaId);
    Task<byte[]> GetTedarikciEvraklariZipAsync(int tedarikciId, IEnumerable<int>? evrakIds = null);
}

public class TasimaTedarikciService : ITasimaTedarikciService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ISecureFileService _secureFileService;

    public TasimaTedarikciService(IDbContextFactory<ApplicationDbContext> contextFactory, ISecureFileService secureFileService)
    {
        _contextFactory = contextFactory;
        _secureFileService = secureFileService;
    }

    public async Task<List<TasimaTedarikci>> GetAllAsync(bool sadeceAktif = false)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.TasimaTedarikciler
            .Include(t => t.Cari)
            .AsQueryable();

        if (sadeceAktif)
            query = query.Where(t => t.Aktif);

        return await query.OrderBy(t => t.Unvan).ToListAsync();
    }

    public async Task<TasimaTedarikci?> GetAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.TasimaTedarikciler
            .Include(t => t.Cari)
            .Include(t => t.Personeller)
            .Include(t => t.Araclar)
            .Include(t => t.Isler)
                .ThenInclude(i => i.Guzergah)
            .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);
    }

    public async Task<TasimaTedarikci> CreateAsync(TasimaTedarikci tedarikci)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        if (string.IsNullOrWhiteSpace(tedarikci.TedarikciKodu))
            tedarikci.TedarikciKodu = await GenerateTedarikciKoduAsync();

        tedarikci.CreatedAt = DateTime.UtcNow;
        context.TasimaTedarikciler.Add(tedarikci);
        await context.SaveChangesAsync();
        return tedarikci;
    }

    public async Task<TasimaTedarikci> UpdateAsync(TasimaTedarikci tedarikci)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        tedarikci.UpdatedAt = DateTime.UtcNow;
        context.TasimaTedarikciler.Update(tedarikci);
        await context.SaveChangesAsync();
        return tedarikci;
    }

    public async Task<TasimaTedarikci> CreateFromCariAsync(int cariId, bool updateIfExists = true)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var cari = await context.Cariler
            .FirstOrDefaultAsync(c => c.Id == cariId && !c.IsDeleted);

        if (cari is null)
            throw new InvalidOperationException("Kopyalanacak cari bulunamadı.");

        var existing = await context.TasimaTedarikciler
            .FirstOrDefaultAsync(t => t.CariId == cariId && !t.IsDeleted);

        if (existing is not null)
        {
            if (updateIfExists)
            {
                MapFromCari(existing, cari);
                existing.UpdatedAt = DateTime.UtcNow;
                await context.SaveChangesAsync();
            }

            return existing;
        }

        var yeniTedarikci = new TasimaTedarikci
        {
            TedarikciKodu = await GenerateTedarikciKoduAsync(),
            Aktif = cari.Aktif,
            CariId = cari.Id,
            CreatedAt = DateTime.UtcNow
        };

        MapFromCari(yeniTedarikci, cari);
        context.TasimaTedarikciler.Add(yeniTedarikci);
        await context.SaveChangesAsync();

        return yeniTedarikci;
    }

    public async Task<TasimaTedarikci> CreateFromPersonelAsync(int personelId, bool updateIfExists = true)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var personel = await context.Soforler
            .FirstOrDefaultAsync(s => s.Id == personelId && !s.IsDeleted);

        if (personel is null)
            throw new InvalidOperationException("Kopyalanacak personel bulunamadı.");

        var existing = await context.TasimaTedarikciler
            .FirstOrDefaultAsync(t => t.Notlar != null && t.Notlar.Contains($"PersonelId:{personelId}") && !t.IsDeleted);

        if (existing is not null)
        {
            if (updateIfExists)
            {
                MapFromPersonel(existing, personel);
                existing.UpdatedAt = DateTime.UtcNow;
                await context.SaveChangesAsync();
            }
            return existing;
        }

        var yeniTedarikci = new TasimaTedarikci
        {
            TedarikciKodu = await GenerateTedarikciKoduAsync(),
            Aktif = personel.Aktif,
            CreatedAt = DateTime.UtcNow
        };
        MapFromPersonel(yeniTedarikci, personel);
        yeniTedarikci.Notlar = (yeniTedarikci.Notlar ?? "") + $" [PersonelId:{personelId}]";
        context.TasimaTedarikciler.Add(yeniTedarikci);
        await context.SaveChangesAsync();
        return yeniTedarikci;
    }

    private static void MapFromPersonel(TasimaTedarikci hedef, Sofor kaynak)
    {
        hedef.Unvan = $"{kaynak.Ad} {kaynak.Soyad}".Trim();
        hedef.YetkiliKisi = $"{kaynak.Ad} {kaynak.Soyad}".Trim();
        hedef.Telefon = kaynak.Telefon;
        hedef.Email = kaynak.Email;
        hedef.Adres = kaynak.Adres;
    }

    public async Task DeleteAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var entity = await context.TasimaTedarikciler.FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);
        if (entity is null) return;
        entity.IsDeleted = true;
        entity.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
    }

    public async Task<string> GenerateTedarikciKoduAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var sayi = await context.TasimaTedarikciler.IgnoreQueryFilters().CountAsync();
        return $"TT{(sayi + 1):D4}";
    }

    public async Task<List<TasimaTedarikciIs>> GetIslerAsync(int? tedarikciId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.TasimaTedarikciIsler
            .Include(i => i.TasimaTedarikci)
            .Include(i => i.Guzergah)
            .Include(i => i.Arac)
            .Include(i => i.Sofor)
            .Where(i => !i.IsDeleted)
            .AsQueryable();

        if (tedarikciId.HasValue)
            query = query.Where(i => i.TasimaTedarikciId == tedarikciId.Value);

        return await query.OrderByDescending(i => i.BaslangicTarihi).ToListAsync();
    }

    public async Task<TasimaTedarikciIs?> GetIsAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.TasimaTedarikciIsler
            .Include(i => i.TasimaTedarikci)
            .Include(i => i.Guzergah)
            .Include(i => i.Arac)
            .Include(i => i.Sofor)
            .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted);
    }

    public async Task<TasimaTedarikciIs> CreateIsAsync(TasimaTedarikciIs tedarikciIs)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        tedarikciIs.CreatedAt = DateTime.UtcNow;
        context.TasimaTedarikciIsler.Add(tedarikciIs);
        await context.SaveChangesAsync();
        return tedarikciIs;
    }

    public async Task<TasimaTedarikciIs> UpdateIsAsync(TasimaTedarikciIs tedarikciIs)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        tedarikciIs.UpdatedAt = DateTime.UtcNow;
        context.TasimaTedarikciIsler.Update(tedarikciIs);
        await context.SaveChangesAsync();
        return tedarikciIs;
    }

    public async Task DeleteIsAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var entity = await context.TasimaTedarikciIsler.FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted);
        if (entity is null) return;
        entity.IsDeleted = true;
        entity.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
    }

    public async Task<List<Sofor>> GetTedarikciPersonelleriAsync(int tedarikciId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Soforler
            .Where(s => s.TasimaTedarikciId == tedarikciId && !s.IsDeleted)
            .OrderBy(s => s.Ad).ThenBy(s => s.Soyad)
            .ToListAsync();
    }

    public async Task<List<Arac>> GetTedarikciAraclariAsync(int tedarikciId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Araclar
            .Where(a => a.TasimaTedarikciId == tedarikciId && !a.IsDeleted)
            .OrderBy(a => a.AktifPlaka)
            .ToListAsync();
    }

    #region Tedarikçi Evrak İşlemleri

    public async Task<List<TedarikciEvrak>> GetTedarikciEvraklariAsync(int tedarikciId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.TedarikciEvraklari
            .Include(e => e.Dosyalar.Where(d => !d.IsDeleted))
            .Where(e => e.TasimaTedarikciId == tedarikciId && !e.IsDeleted)
            .OrderBy(e => e.EvrakKategorisi)
            .ThenByDescending(e => e.BitisTarihi)
            .ToListAsync();
    }

    public async Task<List<TedarikciEvrak>> GetAllTedarikciEvraklariAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.TedarikciEvraklari
            .Include(e => e.Dosyalar.Where(d => !d.IsDeleted))
            .Include(e => e.TasimaTedarikci)
            .Where(e => !e.IsDeleted)
            .OrderBy(e => e.TasimaTedarikci!.Unvan)
            .ThenBy(e => e.EvrakKategorisi)
            .ThenByDescending(e => e.BitisTarihi)
            .ToListAsync();
    }

    public async Task<TedarikciEvrak> CreateTedarikciEvrakAsync(TedarikciEvrak evrak)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        if (evrak.BaslangicTarihi.HasValue)
            evrak.BaslangicTarihi = DateTime.SpecifyKind(evrak.BaslangicTarihi.Value, DateTimeKind.Utc);
        if (evrak.BitisTarihi.HasValue)
            evrak.BitisTarihi = DateTime.SpecifyKind(evrak.BitisTarihi.Value, DateTimeKind.Utc);

        evrak.CreatedAt = DateTime.UtcNow;
        context.TedarikciEvraklari.Add(evrak);
        await context.SaveChangesAsync();
        return evrak;
    }

    public async Task<TedarikciEvrak> UpdateTedarikciEvrakAsync(TedarikciEvrak evrak)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        if (evrak.BaslangicTarihi.HasValue)
            evrak.BaslangicTarihi = DateTime.SpecifyKind(evrak.BaslangicTarihi.Value, DateTimeKind.Utc);
        if (evrak.BitisTarihi.HasValue)
            evrak.BitisTarihi = DateTime.SpecifyKind(evrak.BitisTarihi.Value, DateTimeKind.Utc);

        evrak.UpdatedAt = DateTime.UtcNow;
        context.TedarikciEvraklari.Update(evrak);
        await context.SaveChangesAsync();
        return evrak;
    }

    public async Task DeleteTedarikciEvrakAsync(int evrakId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var evrak = await context.TedarikciEvraklari
            .Include(e => e.Dosyalar.Where(d => !d.IsDeleted))
            .FirstOrDefaultAsync(e => e.Id == evrakId && !e.IsDeleted);

        if (evrak != null)
        {
            foreach (var dosya in evrak.Dosyalar)
            {
                await _secureFileService.DeleteAsync(dosya.DosyaYolu);
                dosya.IsDeleted = true;
            }

            evrak.IsDeleted = true;
            await context.SaveChangesAsync();
        }
    }

    public async Task<TedarikciEvrakDosya> UploadTedarikciEvrakDosyaAsync(int evrakId, IBrowserFile file)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var evrak = await context.TedarikciEvraklari.FirstOrDefaultAsync(e => e.Id == evrakId && !e.IsDeleted)
            ?? throw new Exception("Evrak bulunamadı");

        await using var stream = file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024);
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);

        string? storedPath = null;
        try
        {
            storedPath = await _secureFileService.SaveEncryptedAsync(
                $"tedarikci-evraklar/{evrakId}",
                file.Name,
                ms.ToArray());

            var dosya = new TedarikciEvrakDosya
            {
                TedarikciEvrakId = evrakId,
                DosyaAdi = file.Name,
                DosyaYolu = storedPath,
                DosyaTipi = Path.GetExtension(file.Name).TrimStart('.').ToLower(),
                DosyaBoyutu = file.Size,
                CreatedAt = DateTime.UtcNow
            };

            context.TedarikciEvrakDosyalari.Add(dosya);
            await context.SaveChangesAsync();
            return dosya;
        }
        catch
        {
            if (!string.IsNullOrWhiteSpace(storedPath))
            {
                try { await _secureFileService.DeleteAsync(storedPath); } catch { }
            }

            throw;
        }
    }

    public async Task<byte[]> GetTedarikciEvrakDosyaAsync(int dosyaId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var dosya = await context.TedarikciEvrakDosyalari
            .Include(d => d.TedarikciEvrak)
            .FirstOrDefaultAsync(d => d.Id == dosyaId && !d.IsDeleted && d.TedarikciEvrak != null && !d.TedarikciEvrak.IsDeleted)
            ?? throw new Exception("Dosya bulunamadı");

        return await _secureFileService.ReadDecryptedAsync(dosya.DosyaYolu)
            ?? throw new Exception("Dosya diskte bulunamadı");
    }

    public async Task DeleteTedarikciEvrakDosyaAsync(int dosyaId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var dosya = await context.TedarikciEvrakDosyalari
            .Include(d => d.TedarikciEvrak)
            .FirstOrDefaultAsync(d => d.Id == dosyaId && !d.IsDeleted && d.TedarikciEvrak != null && !d.TedarikciEvrak.IsDeleted);
        if (dosya != null)
        {
            await _secureFileService.DeleteAsync(dosya.DosyaYolu);
            dosya.IsDeleted = true;
            await context.SaveChangesAsync();
        }
    }

    public async Task<byte[]> GetTedarikciEvraklariZipAsync(int tedarikciId, IEnumerable<int>? evrakIds = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var tedarikci = await context.TasimaTedarikciler
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tedarikciId && !t.IsDeleted);

        if (tedarikci is null)
            throw new InvalidOperationException("Tedarikçi bulunamadı.");

        var evrakIdSet = evrakIds?
            .Where(id => id > 0)
            .Distinct()
            .ToHashSet() ?? new HashSet<int>();

        var evrakQuery = context.TedarikciEvraklari
            .AsNoTracking()
            .Include(e => e.Dosyalar.Where(d => !d.IsDeleted))
            .Where(e => e.TasimaTedarikciId == tedarikciId && !e.IsDeleted)
            .AsQueryable();

        if (evrakIdSet.Count > 0)
            evrakQuery = evrakQuery.Where(e => evrakIdSet.Contains(e.Id));

        var evraklar = await evrakQuery
            .OrderBy(e => e.EvrakKategorisi)
            .ThenBy(e => e.BitisTarihi)
            .ThenBy(e => e.EvrakAdi)
            .ToListAsync();

        using var zipMs = new MemoryStream();
        using var archive = new ZipArchive(zipMs, ZipArchiveMode.Create, leaveOpen: true);
        var usedEntryNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var eklenenDosyaSayisi = 0;

        foreach (var evrak in evraklar)
        {
            if (evrak.Dosyalar == null || evrak.Dosyalar.Count == 0)
                continue;

            var kategori = SanitizePathSegment(evrak.EvrakKategorisi, "Kategori");
            var evrakAdi = SanitizePathSegment(evrak.EvrakAdi, $"Evrak_{evrak.Id}");

            foreach (var dosya in evrak.Dosyalar.OrderBy(d => d.DosyaAdi))
            {
                try
                {
                    var rawBytes = await _secureFileService.ReadDecryptedAsync(dosya.DosyaYolu);
                    if (rawBytes is null || rawBytes.Length == 0)
                        continue;

                    var fileName = SanitizePathSegment(dosya.DosyaAdi, $"dosya_{dosya.Id}");
                    var entryName = $"{kategori}/{evrakAdi}/{fileName}";

                    if (!usedEntryNames.Add(entryName))
                    {
                        var name = Path.GetFileNameWithoutExtension(fileName);
                        var ext = Path.GetExtension(fileName);
                        var i = 2;
                        while (!usedEntryNames.Add(entryName))
                        {
                            entryName = $"{kategori}/{evrakAdi}/{name}_{i}{ext}";
                            i++;
                        }
                    }

                    var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
                    await using var entryStream = entry.Open();
                    await entryStream.WriteAsync(rawBytes);
                    eklenenDosyaSayisi++;
                }
                catch
                {
                    // Tek bir bozuk dosya tüm tedarikçi ZIP indirmesini durdurmamalı.
                    continue;
                }
            }
        }

        if (eklenenDosyaSayisi == 0)
            return Array.Empty<byte>();

        var ozet = new StringBuilder();
        ozet.AppendLine($"Tedarikçi: {tedarikci.Unvan}");
        ozet.AppendLine($"Tarih: {DateTime.Now:dd.MM.yyyy HH:mm}");
        ozet.AppendLine($"Toplam Evrak Kaydı: {evraklar.Count}");
        ozet.AppendLine();

        foreach (var grup in evraklar.GroupBy(e => e.EvrakKategorisi).OrderBy(g => g.Key))
        {
            ozet.AppendLine($"[{grup.Key}] ({grup.Count()} kayıt)");
            foreach (var evrak in grup.OrderBy(e => e.BitisTarihi).ThenBy(e => e.EvrakAdi))
            {
                var bitis = evrak.BitisTarihi?.ToString("dd.MM.yyyy") ?? "-";
                var dosyaSayisi = evrak.Dosyalar?.Count ?? 0;
                ozet.AppendLine($"- {evrak.EvrakAdi ?? "(İsimsiz)"} | Bitiş: {bitis} | Dosya: {dosyaSayisi}");
            }

            ozet.AppendLine();
        }

        var ozetEntry = archive.CreateEntry("00_Ozet.txt", CompressionLevel.NoCompression);
        await using (var ozetStream = ozetEntry.Open())
        await using (var writer = new StreamWriter(ozetStream, Encoding.UTF8))
        {
            await writer.WriteAsync(ozet.ToString());
        }

        zipMs.Position = 0;
        return zipMs.ToArray();
    }

    #endregion

    private static string SanitizePathSegment(string? value, string fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
            return fallback;

        var invalids = Path.GetInvalidFileNameChars();
        var cleaned = new string(value.Trim().Select(c => invalids.Contains(c) ? '_' : c).ToArray());
        return string.IsNullOrWhiteSpace(cleaned) ? fallback : cleaned;
    }

    private static void MapFromCari(TasimaTedarikci hedef, Cari kaynak)
    {
        hedef.Unvan = kaynak.Unvan;
        hedef.YetkiliKisi = kaynak.YetkiliKisi;
        hedef.Telefon = kaynak.Telefon;
        hedef.Telefon2 = kaynak.Telefon2;
        hedef.Email = kaynak.Email;
        hedef.Adres = kaynak.Adres;
        hedef.Il = kaynak.Il;
        hedef.Ilce = kaynak.Ilce;
        hedef.VergiDairesi = kaynak.VergiDairesi;
        hedef.VergiNo = kaynak.VergiNo;
        hedef.Notlar = kaynak.Notlar;
        hedef.SozlesmeNo = kaynak.SozlesmeNo;
        hedef.SozlesmeBaslangicTarihi = kaynak.SozlesmeBaslangicTarihi;
        hedef.SozlesmeBitisTarihi = kaynak.SozlesmeBitisTarihi;
    }
}



