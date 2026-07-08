using System.Globalization;
using System.Text;
using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using MKFiloServis.Web.Services.Calculation;
using MKFiloServis.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MKFiloServis.Web.Services;

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
        public List<PuantajDogrulamaMesaji> Errors { get; set; } = new();
        public List<PuantajDogrulamaMesaji> Warnings { get; set; } = new();
        public PuantajOnizlemeOzet Ozet { get; set; } = new();
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
        public string DiffStatus { get; set; } = "Yeni";
    }

    public class PuantajDogrulamaMesaji
    {
        public int? RowNumber { get; set; }
        public string ColumnName { get; set; } = string.Empty;
        public string ErrorCode { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? RawValue { get; set; }
        public string? SuggestedAction { get; set; }
    }

    public class PuantajOnizlemeOzet
    {
        public int Toplam { get; set; }
        public int Gecerli { get; set; }
        public int Hatali { get; set; }
        public int Uyarili { get; set; }
    }

    /// <summary>Excel dosyasını parse edip önizleme döndürür. Kayıt YAPMAZ.</summary>
    public async Task<PuantajImportSonuc> PreviewAsync(byte[] fileBytes, int firmaId)
    {
        var sonuc = new PuantajImportSonuc();
        var rows = ReadExcelRows(fileBytes);
        sonuc.ToplamSatir = Math.Max(rows.Count - 1, 0);

        if (!TryValidateRequiredColumns(rows, sonuc))
        {
            FinalizePreviewSummary(sonuc);
            return sonuc;
        }

        await using var context = await _factory.CreateDbContextAsync();
        var cariler = await context.Cariler.AsNoTracking().Where(c => c.FirmaId == firmaId && !c.IsDeleted).ToListAsync();

        for (int i = 1; i < Math.Min(rows.Count, 6); i++) // ilk 5 veri satırı
        {
            try
            {
                var input = ParsePuantajRow(rows[i]);
                var engineSonuc = PuantajEngine.Hesapla(input);
                var (cari, tip) = EsleCari(cariler, input.Kurum);

                if (tip.StartsWith("Fuzzy", StringComparison.OrdinalIgnoreCase))
                {
                    AddWarning(sonuc, i + 1, "Kurum", "FUZZY_MATCH", $"Kurum fuzzy eşleşti: '{input.Kurum}'", input.Kurum, "Kurum bilgisini kontrol edin.");
                }
                else if (cari == null)
                {
                    AddWarning(sonuc, i + 1, "Kurum", "CARI_NOT_FOUND_PREVIEW", $"Kurum için eşleşen cari bulunamadı: '{input.Kurum}'", input.Kurum, "Import öncesi cari eşleşmesini kontrol edin.");
                }

                sonuc.Onizleme.Add(new PuantajOnizlemeSatir
                {
                    Kurum = input.Kurum, Guzergah = input.Guzergah, Yon = input.Yon, Sofor = input.Sofor, Plaka = input.Plaka,
                    BirimFiyat = input.BirimFiyat, Sefer = engineSonuc.Sefer, Toplam = engineSonuc.Toplam,
                    Kesinti = engineSonuc.Kesinti, Net = engineSonuc.Net,
                    EslesenCari = cari?.Unvan, EslesmeTipi = tip,
                    DiffStatus = "Yeni"
                });
            }
            catch (Exception ex)
            {
                AddError(sonuc, i + 1, "Genel", "ROW_PARSE_ERROR", $"Satır işlenemedi: {ex.Message}", null, "İlgili satırdaki alanları kontrol ederek tekrar deneyin.");
            }
        }

        FinalizePreviewSummary(sonuc);
        return sonuc;
    }

    /// <summary>Tam import: parse + engine + cari + db + snapshot.</summary>
    public async Task<PuantajImportSonuc> ImportAsync(byte[] fileBytes, int yil, int ay, int firmaId)
    {
        var sonuc = new PuantajImportSonuc();
        var rows = ReadExcelRows(fileBytes);
        sonuc.ToplamSatir = Math.Max(rows.Count - 1, 0);

        if (!TryValidateRequiredColumns(rows, sonuc))
        {
            FinalizeImportSummary(sonuc);
            return sonuc;
        }

        if (rows.Count < 2)
        {
            AddError(sonuc, null, "HEADER", "EMPTY_FILE", "Dosya boş veya sadece başlık satırı var.", null, "Şablona uygun en az bir veri satırı ekleyin.");
            FinalizeImportSummary(sonuc);
            return sonuc;
        }

        await using var context = await _factory.CreateDbContextAsync();
        var cariler = await context.Cariler.AsNoTracking().Where(c => c.FirmaId == firmaId && !c.IsDeleted).ToListAsync();
        var guzergahlar = await context.Guzergahlar.AsNoTracking().Where(g => g.FirmaId == firmaId && !g.IsDeleted).ToListAsync();
        var soforler = await context.Soforler.AsNoTracking().Where(s => !s.IsDeleted).ToListAsync();
        var araclar = await context.Araclar.AsNoTracking().Where(a => !a.IsDeleted).ToListAsync();

        // Mükerrer engelleme: aynı dönem/kurum için mevcut yeni model hakediş kayıtları
        var mevcutReferanslar = await context.Hakedisler.AsNoTracking()
            .Where(h => h.Yil == yil && h.Ay == ay && h.FirmaId == firmaId && h.Tip == HakedisTipi.Kurum && !h.IsDeleted)
            .Select(h => h.ReferansId)
            .ToListAsync();

        var mevcutReferansSet = mevcutReferanslar.ToHashSet();
        var yenilerByReferans = new Dictionary<int, Hakedis>();

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

                if (guzergah == null)
                {
                    AddError(sonuc, i + 1, "Guzergah", "ROUTE_NOT_FOUND", $"Güzergah bulunamadı: '{input.Guzergah}'", input.Guzergah, "Güzergah adını kontrol edin.");
                    continue;
                }

                if (sofor == null)
                {
                    AddError(sonuc, i + 1, "Sofor", "DRIVER_NOT_FOUND", $"Şoför bulunamadı: '{input.Sofor}'", input.Sofor, "Şoför bilgisini kontrol edin.");
                    continue;
                }

                if (cari == null)
                {
                    AddError(sonuc, i + 1, "Kurum", "CARI_NOT_FOUND", $"Kurum cari eşleşmesi bulunamadı: '{input.Kurum}'", input.Kurum, "Kurum-cari eşleşmesini kontrol edin.");
                    continue;
                }

                // Duplicate kontrol: aynı kurum için bu ay zaten yeni model hakediş varsa satırı atla
                if (mevcutReferansSet.Contains(cari.Id))
                {
                    sonuc.Atlanan++;
                    AddWarning(sonuc, i + 1, "Kurum", "DUPLICATE_REFERENCE_SKIPPED", $"Aynı dönem için kurum kaydı mevcut olduğundan satır atlandı: '{input.Kurum}'", input.Kurum, "Mükerrer kayıtları kontrol edin.");
                    continue;
                }

                if (!yenilerByReferans.TryGetValue(cari.Id, out var hakedis))
                {
                    hakedis = new Hakedis
                    {
                        FirmaId = firmaId,
                        Yil = yil,
                        Ay = ay,
                        Tip = HakedisTipi.Kurum,
                        ReferansId = cari.Id,
                        Durum = HakedisDurum.Taslak,
                        ToplamSeferSayisi = 0,
                        BirimFiyat = 0,
                        Tutar = 0,
                        KdvOran = 20,
                        KdvTutar = 0,
                        GenelToplam = 0,
                        CreatedAt = DateTime.UtcNow,
                        GenerationParams = "{\"Kaynak\":\"PuantajExcelImport\"}"
                    };
                    yenilerByReferans[cari.Id] = hakedis;
                }

                hakedis.ToplamSeferSayisi += engineSonuc.Sefer;
                hakedis.Tutar += engineSonuc.Toplam;
                hakedis.BirimFiyat = hakedis.ToplamSeferSayisi > 0
                    ? hakedis.Tutar / hakedis.ToplamSeferSayisi
                    : engineSonuc.BirimFiyat;
                hakedis.KdvTutar = hakedis.Tutar * hakedis.KdvOran / 100m;
                hakedis.GenelToplam = hakedis.Tutar + hakedis.KdvTutar;
            }
            catch (Exception ex)
            {
                AddError(sonuc, i + 1, "Genel", "ROW_IMPORT_ERROR", $"Satır import edilemedi: {ex.Message}", null, "Satır verisini kontrol ederek tekrar deneyin.");
            }
        }

        if (yenilerByReferans.Any())
        {
            var yeniler = yenilerByReferans.Values.ToList();
            context.Hakedisler.AddRange(yeniler);
            await context.SaveChangesAsync();
            sonuc.Kaydedilen = yeniler.Count;
        }

        FinalizeImportSummary(sonuc);
        return sonuc;
    }

    #region Private Helpers

    private static readonly (string DisplayName, string[] Aliases)[] RequiredColumns =
    {
        ("KURUM", new[] { "kurum", "musteri", "müşteri" }),
        ("GÜZERGAH", new[] { "guzergah", "güzergah", "route" }),
        ("YÖN", new[] { "yon", "yön", "istikamet" }),
        ("ŞOFÖR", new[] { "sofor", "şoför", "surucu", "sürücü" }),
        ("PLAKA", new[] { "plaka", "aracplaka", "araçplaka", "arac plaka", "araç plaka" }),
        ("BIRIMFIYAT", new[] { "birimfiyat", "birim fiyat", "fiyat" })
    };

    private static bool TryValidateRequiredColumns(List<List<string>> rows, PuantajImportSonuc sonuc)
    {
        if (rows.Count == 0)
        {
            AddError(sonuc, null, "HEADER", "MISSING_REQUIRED_COLUMN", "Zorunlu kolon eksik: Başlık satırı bulunamadı.", null, "Excel dosyasının ilk satırına başlıkları ekleyin.");
            return false;
        }

        var missingColumns = ValidateRequiredColumns(rows[0]);
        if (missingColumns.Count == 0)
            return true;

        foreach (var missingColumn in missingColumns)
        {
            AddError(sonuc, null, missingColumn, "MISSING_REQUIRED_COLUMN", $"Zorunlu kolon eksik: {missingColumn}", null, "Şablon dosyadaki başlık adlarını kullanın.");
        }

        sonuc.Basarili = false;
        return false;
    }

    private static void AddError(PuantajImportSonuc sonuc, int? rowNumber, string columnName, string errorCode, string message, string? rawValue, string? suggestedAction)
    {
        sonuc.Hatalar.Add($"[{errorCode}] {message}");
        sonuc.Errors.Add(new PuantajDogrulamaMesaji
        {
            RowNumber = rowNumber,
            ColumnName = columnName,
            ErrorCode = errorCode,
            Message = message,
            RawValue = rawValue,
            SuggestedAction = suggestedAction
        });
        sonuc.Hata++;
    }

    private static void AddWarning(PuantajImportSonuc sonuc, int? rowNumber, string columnName, string errorCode, string message, string? rawValue, string? suggestedAction)
    {
        sonuc.Warnings.Add(new PuantajDogrulamaMesaji
        {
            RowNumber = rowNumber,
            ColumnName = columnName,
            ErrorCode = errorCode,
            Message = message,
            RawValue = rawValue,
            SuggestedAction = suggestedAction
        });
    }

    private static List<string> ValidateRequiredColumns(IReadOnlyList<string> headerColumns)
    {
        var normalizedHeaders = headerColumns
            .Select(NormalizeHeader)
            .Where(h => !string.IsNullOrWhiteSpace(h))
            .ToHashSet();

        var missing = new List<string>();
        foreach (var column in RequiredColumns)
        {
            var hasColumn = column.Aliases.Any(alias => normalizedHeaders.Contains(NormalizeHeader(alias)));
            if (!hasColumn)
                missing.Add(column.DisplayName);
        }

        return missing;
    }

    public static string BuildErrorsCsv(IEnumerable<PuantajDogrulamaMesaji> messages)
    {
        var sb = new StringBuilder();
        sb.AppendLine("SatirNo,Alan,HataKodu,HataMesaji,Oneri");

        foreach (var message in messages)
        {
            var row = message.RowNumber?.ToString() ?? string.Empty;
            sb.AppendLine(string.Join(",",
                EscapeCsv(row),
                EscapeCsv(message.ColumnName),
                EscapeCsv(message.ErrorCode),
                EscapeCsv(message.Message),
                EscapeCsv(message.SuggestedAction ?? string.Empty)));
        }

        return sb.ToString();
    }

    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        var escaped = value.Replace("\"", "\"\"");
        return $"\"{escaped}\"";
    }

    private static void FinalizePreviewSummary(PuantajImportSonuc sonuc)
    {
        sonuc.Ozet.Toplam = sonuc.ToplamSatir;
        sonuc.Ozet.Gecerli = sonuc.Onizleme.Count;
        sonuc.Ozet.Hatali = sonuc.Hata;
        sonuc.Ozet.Uyarili = sonuc.Warnings.Count;
        sonuc.Basarili = sonuc.Hata == 0;
    }

    private static void FinalizeImportSummary(PuantajImportSonuc sonuc)
    {
        sonuc.Ozet.Toplam = sonuc.ToplamSatir;
        sonuc.Ozet.Gecerli = Math.Max(sonuc.ToplamSatir - sonuc.Hata - sonuc.Atlanan, 0);
        sonuc.Ozet.Hatali = sonuc.Hata;
        sonuc.Ozet.Uyarili = sonuc.Warnings.Count;
        sonuc.Basarili = sonuc.Hata == 0;
    }

    private static string NormalizeHeader(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var normalized = value.Trim().ToLowerInvariant();
        var deaccented = string.Concat(
            normalized
                .Normalize(NormalizationForm.FormD)
                .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark));

        return new string(deaccented.Where(char.IsLetterOrDigit).ToArray());
    }

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


