using ClosedXML.Excel;
using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using MKFiloServis.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MKFiloServis.Web.Services;

public class GuzergahService : IGuzergahService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ICacheService _cache;
    private readonly NumaraSerisiService _numaraSerisi;
    private readonly IAktifFirmaProvider _aktifFirmaProvider;
    private readonly GuzergahSeferService _seferService;

    public GuzergahService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        ICacheService cache,
        NumaraSerisiService numaraSerisi,
        IAktifFirmaProvider aktifFirmaProvider,
        GuzergahSeferService seferService)
    {
        _contextFactory = contextFactory;
        _cache = cache;
        _numaraSerisi = numaraSerisi;
        _aktifFirmaProvider = aktifFirmaProvider;
        _seferService = seferService;
    }

    public Task<List<Guzergah>> GetAllAsync() =>
        _cache.GetOrSetAsync(CacheKeys.GuzergahListesi, async () =>
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Guzergahlar
                .AsNoTracking()
                .Include(g => g.Cari)
                .Include(g => g.Firma)
                .Include(g => g.Kurum)
                .Include(g => g.VarsayilanArac)
                .Include(g => g.VarsayilanSofor)
                .Where(g => g.Cari == null || !g.Cari.IsDeleted)
                .OrderBy(g => g.GuzergahAdi)
                .ToListAsync();
        }, CacheDurations.Long);

    public Task<List<Guzergah>> GetActiveAsync() =>
        _cache.GetOrSetAsync(CacheKeys.GuzergahAktif, async () =>
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Guzergahlar
                .AsNoTracking()
                .Include(g => g.Cari)
                .Include(g => g.Firma)
                .Where(g => g.Aktif)
                .Where(g => g.Cari == null || !g.Cari.IsDeleted)
                .OrderBy(g => g.GuzergahAdi)
                .ToListAsync();
        }, CacheDurations.Long);

    public async Task<List<Guzergah>> GetByCariIdAsync(int cariId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Guzergahlar
            .AsNoTracking()
            .Where(g => g.CariId == cariId)
            .OrderBy(g => g.GuzergahAdi)
            .ToListAsync();
    }

    public async Task<List<Guzergah>> GetByFirmaIdAsync(int firmaId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Guzergahlar
            .AsNoTracking()
            .Include(g => g.VarsayilanArac)
            .Include(g => g.VarsayilanSofor)
            .Where(g => g.FirmaId == firmaId && g.Aktif)
            .OrderBy(g => g.GuzergahAdi)
            .ToListAsync();
    }

    public async Task<Guzergah?> GetByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Guzergahlar
            .AsNoTracking()
            .Include(g => g.Cari)
            .Include(g => g.Firma)
            .Include(g => g.Kurum)
            .Include(g => g.VarsayilanArac)
            .Include(g => g.VarsayilanSofor)
            .FirstOrDefaultAsync(g => g.Id == id);
    }

    public async Task<Guzergah> CreateAsync(Guzergah guzergah)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        // Tenant/firma görünürlüğü için create anında FirmaId'yi netleştir.
        if (!guzergah.FirmaId.HasValue || guzergah.FirmaId.Value <= 0)
        {
            var aktifFirmaId = _aktifFirmaProvider.AktifFirmaId ?? _aktifFirmaProvider.Mevcut.FirmaId;
            if (aktifFirmaId > 0)
            {
                guzergah.FirmaId = aktifFirmaId;
            }
        }

        context.Guzergahlar.Add(guzergah);
        await context.SaveChangesAsync();

        // Yazım doğrulaması: kayıt gerçekten DB'ye geçti mi?
        var persisted = await context.Guzergahlar
            .AsNoTracking()
            .AnyAsync(g => g.Id == guzergah.Id);

        if (!persisted)
            throw new InvalidOperationException("Güzergah kaydı doğrulanamadı. Kayıt veritabanına yansımadı.");

        await _cache.RemoveByPrefixAsync(CacheKeys.GuzergahPrefix);
        return guzergah;
    }

    public async Task<Guzergah> AddAsync(Guzergah guzergah)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await CreateAsync(guzergah);
    }

    public async Task<Guzergah> UpdateAsync(Guzergah guzergah)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var existing = await context.Guzergahlar.FindAsync(guzergah.Id);
        if (existing == null)
            throw new InvalidOperationException($"Güzergah bulunamadı. Id: {guzergah.Id}");

        // Normal alanları güncelle (CariId/KurumId hariç — onlar ExecuteUpdate ile garantilenecek)
        existing.GuzergahKodu = guzergah.GuzergahKodu;
        existing.GuzergahAdi = guzergah.GuzergahAdi;
        existing.BaslangicNoktasi = guzergah.BaslangicNoktasi;
        existing.BitisNoktasi = guzergah.BitisNoktasi;
        existing.BaslangicLatitude = guzergah.BaslangicLatitude;
        existing.BaslangicLongitude = guzergah.BaslangicLongitude;
        existing.BitisLatitude = guzergah.BitisLatitude;
        existing.BitisLongitude = guzergah.BitisLongitude;
        existing.RotaRengi = guzergah.RotaRengi;
        existing.Mesafe = guzergah.Mesafe;
        existing.TahminiSure = guzergah.TahminiSure;
        existing.GelirFiyat = guzergah.GelirFiyat;
        existing.GiderFiyat = guzergah.GiderFiyat;
        existing.SeferTipi = guzergah.SeferTipi;
        existing.PersonelSayisi = guzergah.PersonelSayisi;
        existing.KapasiteAdi = guzergah.KapasiteAdi;
        existing.FirmaId = guzergah.FirmaId > 0 ? guzergah.FirmaId : null;
        existing.VarsayilanAracId = guzergah.VarsayilanAracId;
        existing.VarsayilanSoforId = guzergah.VarsayilanSoforId;
        existing.Notlar = guzergah.Notlar;
        existing.PuantajCarpani = guzergah.PuantajCarpani;
        existing.Aktif = guzergah.Aktif;
        existing.IsDeleted = guzergah.IsDeleted;
        existing.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        // ── CariId / KurumId garantili direkt DB yazma ──
        // UI modelinden 0/null gelme ihtimaline karşı tracking bypass edilir.
        int? hedefCariId = guzergah.CariId > 0 ? guzergah.CariId : null;
        int? hedefKurumId = guzergah.KurumId > 0 ? guzergah.KurumId : null;

        var updated = await context.Guzergahlar
            .IgnoreQueryFilters()
            .Where(x => x.Id == guzergah.Id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.CariId, hedefCariId ?? existing.CariId)
                .SetProperty(x => x.KurumId, hedefKurumId ?? existing.KurumId)
                .SetProperty(x => x.UpdatedAt, DateTime.UtcNow));

        if (updated != 1)
            throw new InvalidOperationException(
                $"Güzergah Cari/Kurum FK update başarısız. GuzergahId={guzergah.Id}, UpdatedRows={updated}");

        // DB'den doğrula
        var kontrol = await context.Guzergahlar
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == guzergah.Id);

        if (kontrol == null)
            throw new InvalidOperationException($"Güzergah doğrulama kaydı bulunamadı. Id={guzergah.Id}");

        var beklenenCari = hedefCariId ?? existing.CariId;
        if (kontrol.CariId != beklenenCari)
            throw new InvalidOperationException(
                $"Güzergah CariId DB'ye yazılamadı. GuzergahId={guzergah.Id}, Beklenen={beklenenCari}, DB={kontrol.CariId}");

        if (kontrol.KurumId != (hedefKurumId ?? existing.KurumId))
            throw new InvalidOperationException(
                $"Güzergah KurumId DB'ye yazılamadı. GuzergahId={guzergah.Id}, Beklenen={hedefKurumId}, DB={kontrol.KurumId}");

        await _cache.RemoveByPrefixAsync(CacheKeys.GuzergahPrefix);
        return kontrol;
    }

    /// <summary>
    /// Güzergah ana kayıt + seferleri TEK transaction içinde günceller.
    /// Partial save riskini ortadan kaldırır.
    /// </summary>
    public async Task UpdateWithSeferlerAsync(Guzergah guzergah, List<GuzergahSefer> seferler)
    {
        await using var strategyDb = await _contextFactory.CreateDbContextAsync();
        var strategy = strategyDb.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            await using var tx = await context.Database.BeginTransactionAsync();

            try
            {
                var now = DateTime.UtcNow;

                // 1. Güncelle ana güzergah kaydı
                var existing = await context.Guzergahlar.FindAsync(guzergah.Id);
                if (existing == null)
                    throw new InvalidOperationException($"Güzergah bulunamadı. Id: {guzergah.Id}");

                existing.GuzergahKodu = guzergah.GuzergahKodu;
                existing.GuzergahAdi = guzergah.GuzergahAdi;
                existing.BaslangicNoktasi = guzergah.BaslangicNoktasi;
                existing.BitisNoktasi = guzergah.BitisNoktasi;
                existing.BaslangicLatitude = guzergah.BaslangicLatitude;
                existing.BaslangicLongitude = guzergah.BaslangicLongitude;
                existing.BitisLatitude = guzergah.BitisLatitude;
                existing.BitisLongitude = guzergah.BitisLongitude;
                existing.RotaRengi = guzergah.RotaRengi;
                existing.Mesafe = guzergah.Mesafe;
                existing.TahminiSure = guzergah.TahminiSure;
                existing.GelirFiyat = guzergah.GelirFiyat;
                existing.GiderFiyat = guzergah.GiderFiyat;
                existing.SeferTipi = guzergah.SeferTipi;
                existing.PersonelSayisi = guzergah.PersonelSayisi;
                existing.KapasiteAdi = guzergah.KapasiteAdi;
                existing.FirmaId = guzergah.FirmaId > 0 ? guzergah.FirmaId : null;
                existing.VarsayilanAracId = guzergah.VarsayilanAracId;
                existing.VarsayilanSoforId = guzergah.VarsayilanSoforId;
                existing.Notlar = guzergah.Notlar;
                existing.PuantajCarpani = guzergah.PuantajCarpani;
                existing.Aktif = guzergah.Aktif;
                existing.IsDeleted = guzergah.IsDeleted;
                existing.UpdatedAt = now;

                int? hedefCariId = guzergah.CariId > 0 ? guzergah.CariId : null;
                int? hedefKurumId = guzergah.KurumId > 0 ? guzergah.KurumId : null;
                existing.CariId = hedefCariId ?? existing.CariId;
                existing.KurumId = hedefKurumId ?? existing.KurumId;

                await context.SaveChangesAsync();

                // CariId / KurumId garantili direkt DB yazma (eski UpdateAsync pattern)
                await context.Guzergahlar
                    .IgnoreQueryFilters()
                    .Where(x => x.Id == guzergah.Id)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(x => x.CariId, hedefCariId ?? existing.CariId)
                        .SetProperty(x => x.KurumId, hedefKurumId ?? existing.KurumId)
                        .SetProperty(x => x.UpdatedAt, now));

                // 2. Replace seferler (aynı context + transaction içinde)
                await _seferService.ReplaceAllInCurrentDbAsync(context, guzergah.Id, seferler, now);

                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        });

        await _cache.RemoveByPrefixAsync(CacheKeys.GuzergahPrefix);
    }

    public async Task DeleteAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var guzergah = await context.Guzergahlar.FindAsync(id);
        if (guzergah != null)
        {
            guzergah.IsDeleted = true;
            guzergah.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
            await _cache.RemoveByPrefixAsync(CacheKeys.GuzergahPrefix);
        }
    }

    public async Task<string> GenerateNextKodAsync()
    {
        // Kural 15: Atomik numara üretimi (global)
        var nextNumber = await _numaraSerisi.GenerateNextAsync("GZR", 0, "GLOBAL");
        return $"GZR-{nextNumber:D4}";
    }

    public async Task<string> GenerateGuzergahKoduAsync(int firmaId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var firma = await context.Firmalar
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == firmaId);
        var firmaKisaltma = firma?.FirmaAdi?.Length >= 3
            ? firma.FirmaAdi.Substring(0, 3).ToUpperInvariant()
            : "GZR";

        // Kural 15: FirmaId bazlı atomik numara
        var sayi = await _numaraSerisi.GenerateNextAsync(firmaKisaltma, firmaId, "GLOBAL");
        return $"{firmaKisaltma}-{sayi:D3}";
    }

    #region Doğrulama Metodları

    public async Task<bool> FaturaKalemdenGuzergahVarMiAsync(int faturaKalemId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Guzergahlar
            .AsNoTracking()
            .AnyAsync(g => g.FaturaKalemId == faturaKalemId && !g.IsDeleted);
    }

    public async Task<Guzergah?> GetByFaturaKalemIdAsync(int faturaKalemId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Guzergahlar
            .AsNoTracking()
            .Include(g => g.Firma)
            .FirstOrDefaultAsync(g => g.FaturaKalemId == faturaKalemId && !g.IsDeleted);
    }

    public async Task<bool> BenzersizGuzergahMiAsync(int firmaId, string guzergahAdi, int? haricId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var normalized = guzergahAdi.Trim();

        var query = context.Guzergahlar
            .AsNoTracking()
            .Where(g => g.FirmaId == firmaId && !g.IsDeleted
                        && g.GuzergahAdi.Trim().ToLower() == normalized.ToLowerInvariant());

        if (haricId.HasValue)
            query = query.Where(g => g.Id != haricId.Value);

        return !await query.AnyAsync();
    }

    #endregion

    #region Excel Import

    public async Task<GuzergahImportSonuc> ImportFromExcelAsync(Stream excelStream, int firmaId)
    {
        var sonuc = new GuzergahImportSonuc();
        var satirlar = new List<GuzergahImportSatir>();

        using var workbook = new ClosedXML.Excel.XLWorkbook(excelStream);
        var ws = workbook.Worksheet(1);
        var rows = ws.RowsUsed().ToList();

        if (rows.Count < 2)
        {
            sonuc.Satirlar.Add(new GuzergahImportSatir { SatirNo = 0, Basarili = false, Mesaj = "Excel dosyası boş veya sadece başlık içeriyor." });
            return sonuc;
        }

        // Başlık satırını atla
        var dataRows = rows.Skip(1).ToList();
        sonuc = new GuzergahImportSonuc { ToplamSatir = dataRows.Count };

        await using var context = await _contextFactory.CreateDbContextAsync();

        foreach (var row in dataRows)
        {
            var satirNo = row.RowNumber();
            try
            {
                var guzergahAdi = GetCell(row, 2)?.Trim();
                var cariIdStr = GetCell(row, 3)?.Trim();

                if (string.IsNullOrWhiteSpace(guzergahAdi))
                {
                    satirlar.Add(new GuzergahImportSatir { SatirNo = satirNo, Basarili = false, Mesaj = "Güzergah adı zorunlu" });
                    continue;
                }

                if (!int.TryParse(cariIdStr, out var cariId) || cariId <= 0)
                {
                    satirlar.Add(new GuzergahImportSatir { SatirNo = satirNo, GuzergahAdi = guzergahAdi, Basarili = false, Mesaj = "Geçerli bir CariId gerekli" });
                    continue;
                }

                // Cari var mı kontrol et
                var cariVar = await context.Cariler.AnyAsync(c => c.Id == cariId && !c.IsDeleted);
                if (!cariVar)
                {
                    satirlar.Add(new GuzergahImportSatir { SatirNo = satirNo, GuzergahAdi = guzergahAdi, Basarili = false, Mesaj = $"CariId={cariId} bulunamadı" });
                    continue;
                }

                // Aynı kod var mı?
                var kod = GetCell(row, 1)?.Trim();
                if (!string.IsNullOrWhiteSpace(kod))
                {
                    var kodVar = await context.Guzergahlar.AnyAsync(g => g.GuzergahKodu == kod && g.FirmaId == firmaId && !g.IsDeleted);
                    if (kodVar)
                    {
                        satirlar.Add(new GuzergahImportSatir { SatirNo = satirNo, GuzergahKodu = kod, GuzergahAdi = guzergahAdi, Basarili = false, Mesaj = $"'{kod}' kodlu güzergah zaten mevcut" });
                        continue;
                    }
                }

                // Kod boşsa otomatik üret
                if (string.IsNullOrWhiteSpace(kod))
                {
                    kod = await GenerateGuzergahKoduAsync(firmaId);
                }

                // BirimFiyat (Gelir)
                decimal birimFiyat = 0;
                var birimFiyatStr = GetCell(row, 4)?.Trim();
                if (!string.IsNullOrWhiteSpace(birimFiyatStr))
                    decimal.TryParse(birimFiyatStr.Replace(".", ","), out birimFiyat);

                // GiderFiyat
                decimal giderFiyat = 0;
                var giderFiyatStr = GetCell(row, 5)?.Trim();
                if (!string.IsNullOrWhiteSpace(giderFiyatStr))
                    decimal.TryParse(giderFiyatStr.Replace(".", ","), out giderFiyat);

                // SeferTipi
                var seferTipi = SeferTipi.SabahAksam;
                var seferTipiStr = GetCell(row, 6)?.Trim();
                if (!string.IsNullOrWhiteSpace(seferTipiStr))
                {
                    if (int.TryParse(seferTipiStr, out var st) && Enum.IsDefined(typeof(SeferTipi), st))
                        seferTipi = (SeferTipi)st;
                    else
                        seferTipi = seferTipiStr.ToLowerInvariant() switch
                        {
                            "sabah" => SeferTipi.Sabah,
                            "akşam" or "aksam" => SeferTipi.Aksam,
                            "sabah-akşam" or "sabah aksam" or "sabahaksam" => SeferTipi.SabahAksam,
                            "saatlik" => SeferTipi.Saatlik,
                            "mesai" => SeferTipi.Mesai,
                            "vardiya" => SeferTipi.Vardiya,
                            _ => SeferTipi.SabahAksam
                        };
                }

                // KurumId (opsiyonel)
                int? kurumId = null;
                var kurumIdStr = GetCell(row, 7)?.Trim();
                if (int.TryParse(kurumIdStr, out var kid) && kid > 0)
                    kurumId = kid;

                // PersonelSayisi
                int personelSayisi = 0;
                var psStr = GetCell(row, 10)?.Trim();
                if (!string.IsNullOrWhiteSpace(psStr))
                    int.TryParse(psStr, out personelSayisi);

                // Mesafe
                decimal? mesafe = null;
                var mesafeStr = GetCell(row, 11)?.Trim();
                if (!string.IsNullOrWhiteSpace(mesafeStr) && decimal.TryParse(mesafeStr.Replace(".", ","), out var m))
                    mesafe = m;

                // TahminiSure
                int? tahminiSure = null;
                var tsStr = GetCell(row, 12)?.Trim();
                if (!string.IsNullOrWhiteSpace(tsStr) && int.TryParse(tsStr, out var ts))
                    tahminiSure = ts;

                // Aktif
                var aktifStr = GetCell(row, 14)?.Trim()?.ToLowerInvariant();
                bool aktif = aktifStr != "hayır" && aktifStr != "hayir" && aktifStr != "false" && aktifStr != "0";

                var guzergah = new Guzergah
                {
                    GuzergahKodu = kod ?? "",
                    GuzergahAdi = guzergahAdi,
                    CariId = cariId,
                    FirmaId = firmaId,
                    BirimFiyat = birimFiyat,
                    GiderFiyat = giderFiyat,
                    SeferTipi = seferTipi,
                    KurumId = kurumId,
                    BaslangicNoktasi = GetCell(row, 8)?.Trim(),
                    BitisNoktasi = GetCell(row, 9)?.Trim(),
                    PersonelSayisi = personelSayisi,
                    Mesafe = mesafe,
                    TahminiSure = tahminiSure,
                    Notlar = GetCell(row, 13)?.Trim(),
                    Aktif = aktif,
                    PuantajCarpani = 1.0m
                };

                context.Guzergahlar.Add(guzergah);
                await context.SaveChangesAsync();

                satirlar.Add(new GuzergahImportSatir
                {
                    SatirNo = satirNo,
                    GuzergahKodu = guzergah.GuzergahKodu,
                    GuzergahAdi = guzergah.GuzergahAdi,
                    Basarili = true,
                    Mesaj = "✅ Eklendi"
                });
            }
            catch (Exception ex)
            {
                satirlar.Add(new GuzergahImportSatir { SatirNo = satirNo, Basarili = false, Mesaj = $"Hata: {ex.Message}" });
            }
        }

        // Cache temizle
        await _cache.RemoveByPrefixAsync(CacheKeys.GuzergahPrefix);

        return new GuzergahImportSonuc
        {
            ToplamSatir = dataRows.Count,
            Basarili = satirlar.Count(s => s.Basarili),
            Atlandi = satirlar.Count(s => !string.IsNullOrEmpty(s.Mesaj) && !s.Basarili && s.Mesaj.Contains("zaten mevcut")),
            Hatali = satirlar.Count(s => !s.Basarili),
            Satirlar = satirlar
        };
    }

    private static string? GetCell(ClosedXML.Excel.IXLRow row, int col)
    {
        try { return row.Cell(col).GetString(); }
        catch { return null; }
    }

    #endregion
}


