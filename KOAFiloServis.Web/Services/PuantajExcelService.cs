using System.Globalization;
using KOAFiloServis.Shared.Entities;
using KOAFiloServis.Web.Data;
using KOAFiloServis.Web.Services.Calculation;
using KOAFiloServis.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KOAFiloServis.Web.Services;

/// <summary>
/// Excel puantaj import + engine + cari eşleştirme + snapshot.
/// </summary>
public class PuantajExcelService
{
    private readonly IDbContextFactory<ApplicationDbContext> _factory;
    private readonly IMaasSnapshotService? _snapshotService;

    public PuantajExcelService(IDbContextFactory<ApplicationDbContext> factory, IMaasSnapshotService? snapshotService = null)
    {
        _factory = factory;
        _snapshotService = snapshotService;
    }

    public class PuantajImportSonuc
    {
        public bool Basarili { get; set; }
        public int ToplamSatir { get; set; }
        public int Kaydedilen { get; set; }
        public int Atlanan { get; set; }
        public int Hata { get; set; }
        public List<string> Hatalar { get; set; } = new();
        public List<PuantajOnizlemeSatir> Onizleme { get; set; } = new();
    }

    public class PuantajOnizlemeSatir
    {
        public string? Kurum { get; set; }
        public string? Guzergah { get; set; }
        public string? Yon { get; set; }
        public string? Sofor { get; set; }
        public string? Plaka { get; set; }
        public decimal BirimFiyat { get; set; }
        public int Sefer { get; set; }
        public decimal Toplam { get; set; }
        public decimal Kesinti { get; set; }
        public decimal Net { get; set; }
        public string? EslesenCari { get; set; }
        public string? EslesmeTipi { get; set; } // Tam / Lower / Fuzzy / Yeni
    }

    /// <summary>Excel dosyasını parse edip önizleme döndürür. Kayıt YAPMAZ.</summary>
    public async Task<PuantajImportSonuc> PreviewAsync(byte[] fileBytes, int firmaId)
    {
        var sonuc = new PuantajImportSonuc();
        var rows = ReadExcelRows(fileBytes);
        sonuc.ToplamSatir = rows.Count - 1;

        await using var context = await _factory.CreateDbContextAsync();
        var cariler = await context.Cariler.AsNoTracking().Where(c => c.FirmaId == firmaId && !c.IsDeleted).ToListAsync();

        for (int i = 1; i < Math.Min(rows.Count, 6); i++) // ilk 5 veri satırı
        {
            try
            {
                var input = ParsePuantajRow(rows[i]);
                var engineSonuc = PuantajEngine.Hesapla(input);
                var (cari, tip) = EsleCari(cariler, input.Kurum);

                sonuc.Onizleme.Add(new PuantajOnizlemeSatir
                {
                    Kurum = input.Kurum, Guzergah = input.Guzergah, Yon = input.Yon, Sofor = input.Sofor, Plaka = input.Plaka,
                    BirimFiyat = input.BirimFiyat, Sefer = engineSonuc.Sefer, Toplam = engineSonuc.Toplam,
                    Kesinti = engineSonuc.Kesinti, Net = engineSonuc.Net,
                    EslesenCari = cari?.Unvan, EslesmeTipi = tip
                });
            }
            catch (Exception ex) { sonuc.Hatalar.Add($"Satır {i + 1}: {ex.Message}"); sonuc.Hata++; }
        }
        return sonuc;
    }

    /// <summary>Tam import: parse + engine + cari + db + snapshot.</summary>
    public async Task<PuantajImportSonuc> ImportAsync(byte[] fileBytes, int yil, int ay, int firmaId)
    {
        var sonuc = new PuantajImportSonuc();
        var rows = ReadExcelRows(fileBytes);
        sonuc.ToplamSatir = rows.Count - 1;
        if (rows.Count < 2) { sonuc.Hatalar.Add("Dosya boş veya sadece başlık var."); return sonuc; }

        await using var context = await _factory.CreateDbContextAsync();
        var cariler = await context.Cariler.AsNoTracking().Where(c => c.FirmaId == firmaId && !c.IsDeleted).ToListAsync();
        var guzergahlar = await context.Guzergahlar.AsNoTracking().Where(g => g.FirmaId == firmaId && !g.IsDeleted).ToListAsync();
        var soforler = await context.Soforler.AsNoTracking().Where(s => !s.IsDeleted).ToListAsync();
        var araclar = await context.Araclar.AsNoTracking().Where(a => !a.IsDeleted).ToListAsync();

        // Mükerrer engelleme: mevcut hakediş kayıtları
        var mevcutHakedisler = await context.HakedisPuantajlar.AsNoTracking()
            .Where(h => h.Yil == yil && h.Ay == ay && h.FirmaId == firmaId && !h.IsDeleted)
            .Select(h => new { h.GuzergahId, h.SoforId })
            .ToListAsync();

        var yeniler = new List<HakedisPuantaj>();

        for (int i = 1; i < rows.Count; i++)
        {
            try
            {
                var input = ParsePuantajRow(rows[i]);
                var engineSonuc = PuantajEngine.Hesapla(input);
                var (cari, _) = EsleCari(cariler, input.Kurum);
                var guzergah = EsleGuzergah(guzergahlar, input.Guzergah);
                var sofor = EsleSofor(soforler, input.Sofor);
                var arac = EsleArac(araclar, input.Plaka);

                if (guzergah == null) { sonuc.Hatalar.Add($"Satır {i + 1}: Güzergah bulunamadı '{input.Guzergah}'"); sonuc.Hata++; continue; }
                if (sofor == null) { sonuc.Hatalar.Add($"Satır {i + 1}: Şoför bulunamadı '{input.Sofor}'"); sonuc.Hata++; continue; }

                // Duplicate kontrol: aynı güzergah+şoför bu ay zaten kayıtlı mı?
                if (mevcutHakedisler.Any(h => h.GuzergahId == guzergah.Id && h.SoforId == sofor.Id))
                { sonuc.Atlanan++; continue; }

                var hakedis = new HakedisPuantaj
                {
                    FirmaId = firmaId, Yil = yil, Ay = ay,
                    GuzergahId = guzergah.Id, SoforId = sofor.Id, AracId = arac?.Id ?? 0,
                    CariId = cari?.Id ?? 0,
                    GunlukSeferSayisi = engineSonuc.Sefer,
                    ToplamSefer = engineSonuc.Sefer,
                    BirimFiyat = engineSonuc.BirimFiyat,
                    GelirBirimFiyat = engineSonuc.BirimFiyat,
                    GiderBirimFiyat = engineSonuc.BirimFiyat,
                    GelirToplam = engineSonuc.Toplam,
                    GiderToplam = engineSonuc.Toplam,
                    ToplamKesinti = engineSonuc.Kesinti,
                    OdenecekTutar = engineSonuc.Net,
                    TahsilEdilecekTutar = engineSonuc.Toplam,
                    CreatedAt = DateTime.UtcNow
                };
                yeniler.Add(hakedis);
            }
            catch (Exception ex) { sonuc.Hatalar.Add($"Satır {i + 1}: {ex.Message}"); sonuc.Hata++; }
        }

        if (yeniler.Any())
        {
            context.HakedisPuantajlar.AddRange(yeniler);
            await context.SaveChangesAsync();
            sonuc.Kaydedilen = yeniler.Count;
            sonuc.Basarili = true;
        }

        return sonuc;
    }

    #region Private Helpers

    private static List<List<string>> ReadExcelRows(byte[] fileBytes)
    {
        using var ms = new MemoryStream(fileBytes);
        using var workbook = new ClosedXML.Excel.XLWorkbook(ms);
        var ws = workbook.Worksheet(1);
        return ws.RowsUsed().Select(row => row.Cells().Select(c => c.GetString().Trim()).ToList()).ToList();
    }

    private static PuantajInput ParsePuantajRow(List<string> cols)
    {
        var gunler = new List<string>();
        // Excel kolonları: KURUM, GÜZERGAH, YÖN, ŞOFÖR, PLAKA, BIRIMFIYAT, 1,2,3...31, KESINTI
        // Gün kolonları 6'dan başlar (index 5), 31 gün
        int gunStart = 5;
        for (int d = gunStart; d < gunStart + 31 && d < cols.Count; d++)
            gunler.Add(cols[d]);

        decimal birimFiyat = 0, kesinti = 0;
        int fiyatIdx = gunStart + 31;
        if (cols.Count > fiyatIdx) decimal.TryParse(cols[fiyatIdx], NumberStyles.Any, CultureInfo.InvariantCulture, out birimFiyat);
        if (cols.Count > fiyatIdx + 1) decimal.TryParse(cols[fiyatIdx + 1], NumberStyles.Any, CultureInfo.InvariantCulture, out kesinti);

        return new PuantajInput
        {
            Kurum = cols.ElementAtOrDefault(0), Guzergah = cols.ElementAtOrDefault(1),
            Yon = cols.ElementAtOrDefault(2), Sofor = cols.ElementAtOrDefault(3),
            Plaka = cols.ElementAtOrDefault(4), BirimFiyat = birimFiyat, Kesinti = kesinti, Gunler = gunler
        };
    }

    private static (Cari?, string) EsleCari(List<Cari> cariler, string? ad)
    {
        if (string.IsNullOrWhiteSpace(ad)) return (null, "Boş");
        var normalized = ad.Trim();

        // 1. Tam eşleşme
        var exact = cariler.FirstOrDefault(c => c.Unvan == normalized);
        if (exact != null) return (exact, "Tam");

        // 2. Büyük/küçük harf ignore
        var lower = cariler.FirstOrDefault(c => c.Unvan.Equals(normalized, StringComparison.OrdinalIgnoreCase));
        if (lower != null) return (lower, "Lower");

        // 3. Fuzzy match (Levenshtein)
        var best = cariler
            .Select(c => (Cari: c, Score: SimilarityHelper.Similarity(c.Unvan, normalized)))
            .OrderByDescending(x => x.Score)
            .First();

        if (best.Score > 0.8) return (best.Cari, $"Fuzzy ({best.Score:P0})");

        // 4. Bulunamadı → null (çağıran oluştursun)
        return (null, "Yeni");
    }

    private static Guzergah? EsleGuzergah(List<Guzergah> guzergahlar, string? ad)
    {
        if (string.IsNullOrWhiteSpace(ad)) return null;
        var normalized = ad.Trim();
        return guzergahlar.FirstOrDefault(g => g.GuzergahAdi != null && g.GuzergahAdi.Trim().Equals(normalized, StringComparison.OrdinalIgnoreCase))
            ?? guzergahlar.FirstOrDefault(g => g.GuzergahAdi != null && g.GuzergahAdi.Trim().StartsWith(normalized, StringComparison.OrdinalIgnoreCase));
    }

    private static Sofor? EsleSofor(List<Sofor> soforler, string? ad)
    {
        if (string.IsNullOrWhiteSpace(ad)) return null;
        var parts = ad.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 2)
        {
            var adMatch = soforler.FirstOrDefault(s => s.Ad.Equals(parts[0], StringComparison.OrdinalIgnoreCase) && s.Soyad.Equals(parts[1], StringComparison.OrdinalIgnoreCase));
            if (adMatch != null) return adMatch;
        }
        return soforler.FirstOrDefault(s => s.TamAd.Equals(ad.Trim(), StringComparison.OrdinalIgnoreCase))
            ?? soforler.FirstOrDefault(s => s.TamAd.StartsWith(ad.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    private static Arac? EsleArac(List<Arac> araclar, string? plaka)
    {
        if (string.IsNullOrWhiteSpace(plaka)) return null;
        var normalized = plaka.Trim().Replace(" ", "").ToUpperInvariant();
        return araclar.FirstOrDefault(a => a.AktifPlaka != null && a.AktifPlaka.Replace(" ", "").ToUpperInvariant() == normalized);
    }

    #endregion
}
