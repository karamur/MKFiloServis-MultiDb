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
        var rawRows = ReadExcelRows(fileBytes);
        var rows = BuildCanonicalRows(rawRows);
        sonuc.ToplamSatir = Math.Max(rows.Count - 1, 0);

        if (rows.Count < 2)
        {
            AddError(sonuc, null, "HEADER", "EMPTY_FILE", "Dosya boş veya sadece başlık satırı var.", null, "Şablona uygun en az bir veri satırı ekleyin.");
            FinalizePreviewSummary(sonuc);
            return sonuc;
        }

        await using var context = await _factory.CreateDbContextAsync();
        var cariler = await context.Cariler.AsNoTracking().Where(c => c.FirmaId == firmaId && !c.IsDeleted).ToListAsync();
        var guzergahlar = await context.Guzergahlar.AsNoTracking().Where(g => g.FirmaId == firmaId && !g.IsDeleted).ToListAsync();

        for (int i = 1; i < Math.Min(rows.Count, 6); i++) // ilk 5 veri satırı
        {
            try
            {
                var input = ParsePuantajRow(rows[i]);
                var guzergah = EsleGuzergah(guzergahlar, input.Guzergah);

                if (string.IsNullOrWhiteSpace(input.Yon))
                    input.Yon = InferYonFromGuzergah(guzergah);
                input.Yon = NormalizeYonLabel(input.Yon);

                if (input.BirimFiyat <= 0 && guzergah != null)
                    input.BirimFiyat = guzergah.GelirFiyat > 0 ? guzergah.GelirFiyat : guzergah.GiderFiyat;

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

                if (string.IsNullOrWhiteSpace(input.Yon))
                {
                    AddWarning(sonuc, i + 1, "YON", "YON_DERIVE_FAILED", "YÖN alanı belirlenemedi, 'Ek Sefer' olarak işaretlendi.", null, "YÖN bilgisini doğrulayın.");
                    input.Yon = "Ek Sefer";
                }

                if (input.BirimFiyat <= 0)
                {
                    AddWarning(sonuc, i + 1, "BIRIMFIYAT", "PRICE_DERIVE_FAILED", "Birim fiyat belirlenemedi veya 0 kaldı.", null, "Güzergah fiyatlarını kontrol edin.");
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
        var rawRows = ReadExcelRows(fileBytes);
        var rows = BuildCanonicalRows(rawRows);
        sonuc.ToplamSatir = Math.Max(rows.Count - 1, 0);

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
                var (cari, _) = EsleCari(cariler, input.Kurum);
                var guzergah = EsleGuzergah(guzergahlar, input.Guzergah);
                var sofor = EsleSofor(soforler, input.Sofor);

                if (string.IsNullOrWhiteSpace(input.Yon))
                    input.Yon = InferYonFromGuzergah(guzergah);
                input.Yon = NormalizeYonLabel(input.Yon);
                if (string.IsNullOrWhiteSpace(input.Yon))
                    input.Yon = "Ek Sefer";

                if (input.BirimFiyat <= 0 && guzergah != null)
                    input.BirimFiyat = guzergah.GelirFiyat > 0 ? guzergah.GelirFiyat : guzergah.GiderFiyat;

                _ = EsleArac(araclar, input.Plaka);
                var engineSonuc = PuantajEngine.Hesapla(input);

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

    private static readonly Dictionary<string, string[]> CanonicalAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["KURUM"] = new[] { "kurum", "firma", "musteri", "müşteri" },
        ["GUZERGAH"] = new[] { "guzergah", "güzergah", "route" },
        ["YON"] = new[] { "yon", "yön", "istikamet" },
        ["SOFOR"] = new[] { "sofor", "şoför", "surucu", "sürücü" },
        ["PLAKA"] = new[] { "plaka", "aracplaka", "araçplaka", "arac plaka", "araç plaka" },
        ["BIRIMFIYAT"] = new[] { "birimfiyat", "birim fiyat", "fiyat", "gelir", "ucret", "ücret" },
        ["KESINTI"] = new[] { "kesinti", "kersinti", "kesi̇nti̇" }
    };

    private static List<List<string>> BuildCanonicalRows(List<List<string>> sourceRows)
    {
        if (sourceRows.Count == 0)
            return sourceRows;

        var header = sourceRows[0];
        var normalizedHeaderMap = header
            .Select((value, index) => new { Key = NormalizeHeader(value), Index = index })
            .Where(x => !string.IsNullOrWhiteSpace(x.Key))
            .GroupBy(x => x.Key)
            .ToDictionary(g => g.Key, g => g.First().Index, StringComparer.OrdinalIgnoreCase);

        var dayColumns = header
            .Select((value, index) => new { Raw = value?.Trim(), Index = index })
            .Select(x => new { x.Index, Day = int.TryParse(x.Raw, out var day) ? day : 0 })
            .Where(x => x.Day is >= 1 and <= 31)
            .OrderBy(x => x.Day)
            .ToList();

        var canonical = new List<List<string>>
        {
            new()
            {
                "KURUM", "GÜZERGAH", "YÖN", "ŞOFÖR", "PLAKA", "BIRIMFIYAT",
                "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15", "16", "17", "18", "19", "20", "21", "22", "23", "24", "25", "26", "27", "28", "29", "30", "31",
                "KESINTI"
            }
        };

        for (var rowIndex = 1; rowIndex < sourceRows.Count; rowIndex++)
        {
            var row = sourceRows[rowIndex];
            var kurum = GetCellByAliases(row, normalizedHeaderMap, CanonicalAliases["KURUM"]);
            var guzergah = GetCellByAliases(row, normalizedHeaderMap, CanonicalAliases["GUZERGAH"]);
            var yon = GetCellByAliases(row, normalizedHeaderMap, CanonicalAliases["YON"]);
            var sofor = GetCellByAliases(row, normalizedHeaderMap, CanonicalAliases["SOFOR"]);
            var plaka = GetCellByAliases(row, normalizedHeaderMap, CanonicalAliases["PLAKA"]);
            var birimFiyat = GetCellByAliases(row, normalizedHeaderMap, CanonicalAliases["BIRIMFIYAT"]);
            var kesinti = GetCellByAliases(row, normalizedHeaderMap, CanonicalAliases["KESINTI"]);

            var dayValues = new string[31];
            foreach (var dayColumn in dayColumns)
            {
                var value = dayColumn.Index < row.Count ? row[dayColumn.Index] : string.Empty;
                dayValues[dayColumn.Day - 1] = value?.Trim() ?? string.Empty;
            }

            var hasMeaningfulValue =
                !string.IsNullOrWhiteSpace(kurum) ||
                !string.IsNullOrWhiteSpace(guzergah) ||
                !string.IsNullOrWhiteSpace(sofor) ||
                !string.IsNullOrWhiteSpace(plaka) ||
                !string.IsNullOrWhiteSpace(birimFiyat) ||
                dayValues.Any(v => !string.IsNullOrWhiteSpace(v));

            if (!hasMeaningfulValue)
                continue;

            canonical.Add(new List<string>
            {
                kurum,
                guzergah,
                yon,
                sofor,
                plaka,
                birimFiyat,
                dayValues[0], dayValues[1], dayValues[2], dayValues[3], dayValues[4], dayValues[5], dayValues[6], dayValues[7], dayValues[8], dayValues[9],
                dayValues[10], dayValues[11], dayValues[12], dayValues[13], dayValues[14], dayValues[15], dayValues[16], dayValues[17], dayValues[18], dayValues[19],
                dayValues[20], dayValues[21], dayValues[22], dayValues[23], dayValues[24], dayValues[25], dayValues[26], dayValues[27], dayValues[28], dayValues[29], dayValues[30],
                kesinti
            });
        }

        return canonical;
    }

    private static string GetCellByAliases(IReadOnlyList<string> row, IReadOnlyDictionary<string, int> headerMap, IEnumerable<string> aliases)
    {
        foreach (var alias in aliases)
        {
            if (!headerMap.TryGetValue(NormalizeHeader(alias), out var index))
                continue;

            if (index < row.Count)
                return row[index]?.Trim() ?? string.Empty;
        }

        return string.Empty;
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
        // Kanonik kolonlar: KURUM, GÜZERGAH, YÖN, ŞOFÖR, PLAKA, BIRIMFIYAT, 1..31, KESINTI
        const int gunStart = 6;
        const int gunCount = 31;

        for (var d = gunStart; d < gunStart + gunCount && d < cols.Count; d++)
            gunler.Add(cols[d]);

        var birimFiyat = ParseDecimalSmart(cols.ElementAtOrDefault(5));
        var kesinti = ParseDecimalSmart(cols.ElementAtOrDefault(gunStart + gunCount));

        return new PuantajInput
        {
            Kurum = cols.ElementAtOrDefault(0),
            Guzergah = cols.ElementAtOrDefault(1),
            Yon = cols.ElementAtOrDefault(2),
            Sofor = cols.ElementAtOrDefault(3),
            Plaka = cols.ElementAtOrDefault(4),
            BirimFiyat = birimFiyat,
            Kesinti = kesinti,
            Gunler = gunler
        };
    }

    private static decimal ParseDecimalSmart(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return 0;

        var text = value.Trim();

        if (decimal.TryParse(text, NumberStyles.Any, new CultureInfo("tr-TR"), out var trResult))
            return trResult;

        if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var invResult))
            return invResult;

        var normalized = text.Replace(".", string.Empty).Replace(',', '.');
        return decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out var normalizedResult)
            ? normalizedResult
            : 0;
    }

    private static string NormalizeYonLabel(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var raw = value.Trim();
        var normalized = NormalizeHeader(raw);

        return normalized switch
        {
            "s" or "sabah" => "Sabah",
            "a" or "aksam" => "Akşam",
            "sa" or "as" or "sabahaksam" or "gidisdonus" => "Sabah+Akşam",
            "m" or "mesai" => "Mesai",
            "e" or "eksefer" => "Ek Sefer",
            _ => raw.Equals("S-A", StringComparison.OrdinalIgnoreCase) || raw.Equals("A-S", StringComparison.OrdinalIgnoreCase)
                ? "Sabah+Akşam"
                : "Ek Sefer"
        };
    }

    private static string InferYonFromGuzergah(Guzergah? guzergah)
    {
        if (guzergah == null)
            return string.Empty;

        return guzergah.SeferTipi switch
        {
            SeferTipi.Sabah => "Sabah",
            SeferTipi.Aksam => "Akşam",
            SeferTipi.SabahAksam => "Sabah+Akşam",
            SeferTipi.Mesai => "Mesai",
            _ => "Ek Sefer"
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


