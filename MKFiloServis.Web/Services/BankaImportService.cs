using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using MKFiloServis.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic.FileIO;

namespace MKFiloServis.Web.Services;

/// <summary>
/// Dinamik banka Excel/CSV import servisi.
/// Kullanıcı tanımlı kolon mapping ile çalışır.
/// SHA256 hash, mükerrer kayıt kontrolü ve önizleme içerir.
/// </summary>
public class BankaImportService
{
    private readonly IDbContextFactory<ApplicationDbContext> _factory;
    private readonly IAktifFirmaProvider _aktifFirmaProvider;

    public BankaImportService(IDbContextFactory<ApplicationDbContext> factory, IAktifFirmaProvider aktifFirmaProvider,
        IMaasSnapshotService? snapshotService = null)
    {
        _factory = factory;
        _aktifFirmaProvider = aktifFirmaProvider;
    }

    /// <summary>
    /// Import sonucu — log + preview + kaydedilen kayıt sayısı.
    /// </summary>
    public class BankaImportSonuc
    {
        public bool Basarili { get; set; }
        public string? DosyaHash { get; set; }
        public int AlinanSatir { get; set; }
        public int Kaydedilen { get; set; }
        public int Atlanan { get; set; }
        public int Hata { get; set; }
        public List<string> Hatalar { get; set; } = new();
        public List<FinansHareket> Onizleme { get; set; } = new();
        public List<FinansHareket> Kaydedilenler { get; set; } = new();
    }

    /// <summary>
    /// İlk 5 satırı parse edip önizleme döndürür. Kayıt YAPMAZ.
    /// </summary>
    public async Task<BankaImportSonuc> PreviewAsync(byte[] fileBytes, string fileName, BankaKolonMapping map, int firmaId)
    {
        MapDogrula(map);
        await using var context = await _factory.CreateDbContextAsync();
        await ImportKapsaminiDogrulaAsync(context, map, firmaId);
        var hash = ComputeSha256(fileBytes);

        using var ms = new MemoryStream(fileBytes);
        var satirlar = await DosyaOkuAsync(ms, fileName, map.Ayrac, map.SayiAyraci);
        var sonuc = new BankaImportSonuc { DosyaHash = hash, Basarili = true };

        var startIndex = map.BaslikVarMi ? 1 + map.AtlanacakSatir : map.AtlanacakSatir;
        var previewRows = Math.Min(5, satirlar.Length - startIndex);

        for (int i = startIndex; i < startIndex + previewRows; i++)
        {
            if (satirlar[i].All(string.IsNullOrWhiteSpace)) continue;
            try
            {
                var hareket = ParseSatir(satirlar[i], map, firmaId);
                sonuc.Onizleme.Add(hareket);
            }
            catch (Exception ex)
            {
                sonuc.Hatalar.Add($"Satır {i + 1}: {ex.Message}");
                sonuc.Hata++;
            }
        }

        sonuc.AlinanSatir = Math.Max(0, satirlar.Length - startIndex);
        sonuc.Basarili = sonuc.Hata == 0 && sonuc.Onizleme.Count > 0;
        return sonuc;
    }

    /// <summary>
    /// Tam import: kapsam, hash ve mükerrer kontrolü; tüm satırlar geçerliyse tek SaveChanges ile kaydet.
    /// </summary>
    public async Task<BankaImportSonuc> ImportAsync(byte[] fileBytes, string fileName, BankaKolonMapping map, int firmaId)
    {
        MapDogrula(map);
        var hash = ComputeSha256(fileBytes);
        var sonuc = new BankaImportSonuc { DosyaHash = hash };

        // ── Hash kontrol: aynı dosya daha önce yüklendi mi? ──
        await using var context = await _factory.CreateDbContextAsync();
        await ImportKapsaminiDogrulaAsync(context, map, firmaId);
        var hashVarMi = await context.FinansHareketler
            .AsNoTracking()
            .AnyAsync(x => x.DosyaHash == hash && x.FirmaId == firmaId && !x.IsDeleted);

        if (hashVarMi)
        {
            sonuc.Hatalar.Add($"Bu dosya daha önce yüklenmiş (Hash: {hash[..12]}...).");
            return sonuc;
        }

        using var ms = new MemoryStream(fileBytes);
        var satirlar = await DosyaOkuAsync(ms, fileName, map.Ayrac, map.SayiAyraci);
        if (satirlar.Length == 0)
        {
            sonuc.Hatalar.Add("Dosya boş.");
            return sonuc;
        }

        var startIndex = map.BaslikVarMi ? 1 + map.AtlanacakSatir : map.AtlanacakSatir;
        sonuc.AlinanSatir = Math.Max(0, satirlar.Length - startIndex);

        var kaydedilecekler = new List<FinansHareket>();
        var mevcutTarihReferanslar = await context.FinansHareketler
            .AsNoTracking()
            .Where(x => x.FirmaId == firmaId && !x.IsDeleted)
            .Select(x => new { x.Tarih, x.ReferansNo, x.Tutar, x.BorcMu })
            .ToListAsync();
        var referanslar = mevcutTarihReferanslar
            .Where(x => !string.IsNullOrWhiteSpace(x.ReferansNo))
            .Select(x => (x.Tarih.Date, x.ReferansNo!.Trim(), x.Tutar, x.BorcMu))
            .ToHashSet();

        for (int i = startIndex; i < satirlar.Length; i++)
        {
            var line = satirlar[i];
            if (line.All(string.IsNullOrWhiteSpace)) continue;

            try
            {
                var hareket = ParseSatir(line, map, firmaId);

                hareket.DosyaHash = hash;

                // Mevcut DB kayıtları ve bu dosyada daha önce kabul edilen satırlar.
                if (!string.IsNullOrWhiteSpace(hareket.ReferansNo))
                {
                    var referans = hareket.ReferansNo.Trim();
                    if (!referanslar.Add((hareket.Tarih.Date, referans, hareket.Tutar, hareket.BorcMu)))
                    {
                        sonuc.Atlanan++;
                        continue;
                    }

                    hareket.IthalatTekillikAnahtari = OlusturIthalatTekillikAnahtari(
                        firmaId, hareket.Tarih.Date, referans, hareket.Tutar, hareket.BorcMu);
                }

                kaydedilecekler.Add(hareket);
            }
            catch (Exception ex)
            {
                sonuc.Hatalar.Add($"Satır {i + 1}: {ex.Message}");
                sonuc.Hata++;
            }
        }

        if (sonuc.Hata > 0)
        {
            sonuc.Hatalar.Add("Dosyada hatalı satırlar bulundu. Hiçbir kayıt eklenmedi; dosyayı düzeltip yeniden yükleyin.");
            return sonuc;
        }

        // ── Toplu kaydet ──
        if (kaydedilecekler.Any())
        {
            context.FinansHareketler.AddRange(kaydedilecekler);
            await context.SaveChangesAsync();
            sonuc.Kaydedilen = kaydedilecekler.Count;
            sonuc.Kaydedilenler = kaydedilecekler;
            sonuc.Basarili = true;

        }
        else
            sonuc.Hatalar.Add("İçe aktarılabilecek yeni kayıt bulunamadı.");

        return sonuc;
    }

    private static string OlusturIthalatTekillikAnahtari(
        int firmaId, DateTime tarih, string referansNo, decimal tutar, bool borcMu)
    {
        var canonical = JsonSerializer.Serialize(new
        {
            FirmaId = firmaId,
            Tarih = tarih.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ReferansNo = referansNo,
            Tutar = tutar.ToString("G29", CultureInfo.InvariantCulture),
            BorcMu = borcMu
        });
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }

    /// <summary>
    /// CSV başlık satırını okuyup kolon adlarını döndürür (TahminEt için).
    /// </summary>
    public async Task<string[]> ReadHeadersAsync(byte[] fileBytes, string fileName, string? ayrac = null)
    {
        // Ayraç belirtilmediyse yalnız ilk mantıksal kaydın en çok kolon üreten adayı seçilir.
        if (ayrac is null && Path.GetExtension(fileName).ToLowerInvariant() is ".csv" or ".txt")
        {
            using var reader = new StreamReader(new MemoryStream(fileBytes), Encoding.UTF8, true);
            var content = await reader.ReadToEndAsync();
            string[]? header = null;
            foreach (var candidate in new[] { ";", ",", "\t" })
            {
                try
                {
                    using var parser = CreateCsvParser(content, candidate);
                    var fields = parser.EndOfData ? Array.Empty<string>() : parser.ReadFields()!;
                    if (header is null || fields.Length > header.Length) header = fields;
                }
                catch (MalformedLineException) { /* Diğer ayraç adayını dene. */ }
            }
            return header ?? throw new InvalidOperationException("CSV başlık satırı ayrıştırılamadı; kolon ayracını belirtin.");
        }
        using var ms = new MemoryStream(fileBytes);
        var satirlar = await DosyaOkuAsync(ms, fileName, ayrac ?? ";");
        if (satirlar.Length == 0) return Array.Empty<string>();
        return satirlar[0];
    }

    public BankaKolonMapping TahminEt(string[] headerColumns)
    {
        var map = new BankaKolonMapping
        {
            TarihKolon = 0, AciklamaKolon = 0, TutarKolon = 0,
            BaslikVarMi = true, Ayrac = ";", SayiAyraci = ",", TarihFormati = "dd.MM.yyyy"
        };

        for (int i = 0; i < headerColumns.Length; i++)
        {
            var h = headerColumns[i].Trim().ToLowerInvariant();
            if (map.TarihKolon == 0 && (h.Contains("tarih") || h.Contains("date") || h.Contains("valör"))) map.TarihKolon = i + 1;
            if (map.AciklamaKolon == 0 && (h.Contains("açıklama") || h.Contains("aciklama") || h.Contains("description"))) map.AciklamaKolon = i + 1;
            if (map.TutarKolon == 0 && (h.Contains("tutar") || h.Contains("miktar") || h.Contains("amount"))) map.TutarKolon = i + 1;
            if (map.BorcAlacakKolon == 0 && (h.Contains("borç") || h.Contains("alacak") || h.Contains("b/a") || h.Contains("işaret") || h.Contains("d/c"))) map.BorcAlacakKolon = i + 1;
            if (map.ReferansKolon == 0 && (h.Contains("referans") || h.Contains("işlem no") || h.Contains("dekont") || h.Contains("ref"))) map.ReferansKolon = i + 1;
        }

        if (map.TarihKolon == 0) map.TarihKolon = 1;
        if (map.AciklamaKolon == 0) map.AciklamaKolon = 2;
        if (map.TutarKolon == 0) map.TutarKolon = 3;

        return map;
    }

    public async Task<BankaKolonMapping> KaydetAsync(BankaKolonMapping map)
    {
        MapDogrula(map);
        if (map.Id < 0)
            throw new InvalidOperationException("Geçersiz banka kolon eşleme kimliği.");

        var firmaId = GetWriteFirmaId();
        await using var context = await _factory.CreateDbContextAsync();
        if (map.Id > 0)
        {
            var existing = await context.BankaKolonMappingler.AsTracking()
                .FirstOrDefaultAsync(x => x.Id == map.Id && x.FirmaId == firmaId && !x.IsDeleted);
            if (existing is null)
                throw new InvalidOperationException("Banka kolon eşleme şablonu bulunamadı veya firma kapsamında erişilebilir değil.");

            CopyMappingFields(existing, map);
            existing.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
            return existing;
        }
        if (map.FirmaId.HasValue && map.FirmaId.Value != 0 && map.FirmaId.Value != firmaId)
            throw new InvalidOperationException("Yeni banka şablonu yalnız seçili firma için oluşturulabilir.");
        if (!await context.Firmalar.AnyAsync(x => x.Id == firmaId && !x.IsDeleted))
            throw new InvalidOperationException("Seçili firma bulunamadı veya silinmiş.");

        var yeni = new BankaKolonMapping { FirmaId = firmaId };
        CopyMappingFields(yeni, map);
        context.BankaKolonMappingler.Add(yeni);
        await context.SaveChangesAsync();
        return yeni;
    }

    public async Task<List<BankaKolonMapping>> GetMappingsAsync(int firmaId)
    {
        await using var context = await _factory.CreateDbContextAsync();
        return await context.BankaKolonMappingler.AsNoTracking()
            .Where(x => x.FirmaId == firmaId && !x.IsDeleted)
            .OrderByDescending(x => x.Varsayilan).ThenBy(x => x.Ad).ToListAsync();
    }

    public async Task SilAsync(int id)
    {
        var firmaId = GetWriteFirmaId();
        await using var context = await _factory.CreateDbContextAsync();
        var existing = await context.BankaKolonMappingler.AsTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.FirmaId == firmaId && !x.IsDeleted);
        if (existing is null)
            throw new InvalidOperationException("Banka kolon eşleme şablonu bulunamadı veya firma kapsamında erişilebilir değil.");

        existing.IsDeleted = true;
        existing.DeletedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
    }

    #region Private Helpers

    private async Task ImportKapsaminiDogrulaAsync(ApplicationDbContext context, BankaKolonMapping map, int firmaId)
    {
        var aktifFirmaId = GetWriteFirmaId();
        if (firmaId != aktifFirmaId ||
            (map.FirmaId is not null and not 0 && map.FirmaId != aktifFirmaId))
            throw new InvalidOperationException("Banka dosyası yalnız seçili firmanın şablonuyla seçili firmaya aktarılabilir.");
        if (!await context.Firmalar.AnyAsync(x => x.Id == aktifFirmaId && !x.IsDeleted))
            throw new InvalidOperationException("Seçili firma bulunamadı veya silinmiş.");
        if (map.Id < 0 || (map.Id > 0 && !await context.BankaKolonMappingler.AnyAsync(
                x => x.Id == map.Id && x.FirmaId == aktifFirmaId && !x.IsDeleted)))
            throw new InvalidOperationException("Banka kolon eşleme şablonu bulunamadı veya firma kapsamında erişilebilir değil.");
    }

    private int GetWriteFirmaId()
    {
        var firmaId = _aktifFirmaProvider.AktifFirmaId;
        if (_aktifFirmaProvider.TumFirmalar || firmaId is not > 0)
            throw new InvalidOperationException("Banka işlemi için tek bir firma seçilmelidir.");
        return firmaId.Value;
    }

    private static void CopyMappingFields(BankaKolonMapping target, BankaKolonMapping source)
    {
        target.Ad = source.Ad; target.TarihKolon = source.TarihKolon; target.AciklamaKolon = source.AciklamaKolon;
        target.TutarKolon = source.TutarKolon; target.BorcAlacakKolon = source.BorcAlacakKolon; target.ReferansKolon = source.ReferansKolon;
        target.DosyaTipi = source.DosyaTipi; target.Ayrac = source.Ayrac; target.TarihFormati = source.TarihFormati;
        target.SayiAyraci = source.SayiAyraci; target.BorcGostergesi = source.BorcGostergesi; target.AlacakGostergesi = source.AlacakGostergesi;
        target.BaslikVarMi = source.BaslikVarMi; target.AtlanacakSatir = source.AtlanacakSatir; target.Varsayilan = source.Varsayilan;
    }

    private static void MapDogrula(BankaKolonMapping map)
    {
        if (map.TarihKolon <= 0) throw new InvalidOperationException("Tarih kolonu zorunludur.");
        if (map.TutarKolon <= 0) throw new InvalidOperationException("Tutar kolonu zorunludur.");
        if (map.AciklamaKolon < 0 || map.BorcAlacakKolon < 0 || map.ReferansKolon < 0)
            throw new InvalidOperationException("İsteğe bağlı kolon numaraları negatif olamaz.");
        if (map.AtlanacakSatir < 0 || map.AtlanacakSatir == int.MaxValue)
            throw new InvalidOperationException("Atlanacak satır sayısı geçersiz.");
        if (string.IsNullOrEmpty(map.Ayrac) || string.IsNullOrWhiteSpace(map.TarihFormati))
            throw new InvalidOperationException("Kolon ayracı ve tarih formatı zorunludur.");
        if (map.Ayrac.IndexOfAny(new[] { '\r', '\n', '"' }) >= 0)
            throw new InvalidOperationException("Kolon ayracı satır sonu veya çift tırnak içeremez.");
        if (map.BorcAlacakKolon > 0 &&
            GetDirectionIndicators(true, map.BorcGostergesi).Overlaps(
                GetDirectionIndicators(false, map.AlacakGostergesi)))
            throw new InvalidOperationException("Borç ve alacak göstergeleri aynı değeri içeremez. Şablondaki göstergeleri düzeltin.");
    }

    private static FinansHareket ParseSatir(string[] cols, BankaKolonMapping map, int firmaId)
    {
        var sonKolon = new[] { map.TarihKolon, map.TutarKolon, map.AciklamaKolon, map.BorcAlacakKolon, map.ReferansKolon }.Max();
        if (cols.Length < sonKolon)
            throw new InvalidOperationException($"Satırda {sonKolon} kolon bekleniyor; {cols.Length} kolon bulundu.");

        var tarih = ParseTarih(Temizle(cols, map.TarihKolon), map.TarihFormati);
        var aciklama = map.AciklamaKolon > 0 ? Temizle(cols, map.AciklamaKolon) : "";
        var tutar = ParseTutar(Temizle(cols, map.TutarKolon), map.SayiAyraci);
        var borcMu = ParseBorcAlacak(cols, map);
        var referansNo = map.ReferansKolon > 0 ? Temizle(cols, map.ReferansKolon) : null;

        return new FinansHareket
        {
            FirmaId = firmaId, Tarih = tarih, Tip = "Banka",
            Tutar = tutar, BorcMu = borcMu, Aciklama = aciklama, ReferansNo = referansNo,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static TextFieldParser CreateCsvParser(string content, string ayrac)
    {
        var parser = new TextFieldParser(new StringReader(content))
        {
            TextFieldType = FieldType.Delimited,
            HasFieldsEnclosedInQuotes = true,
            TrimWhiteSpace = true
        };
        parser.SetDelimiters(ayrac);
        return parser;
    }

    private static async Task<string[][]> DosyaOkuAsync(Stream stream, string fileName, string ayrac, string sayiAyraci = ",")
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        if (ext == ".csv" || ext == ".txt")
        {
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            using var parser = CreateCsvParser(await reader.ReadToEndAsync(), ayrac);
            var rows = new List<string[]>();
            try
            {
                while (!parser.EndOfData)
                    rows.Add(parser.ReadFields()!);
            }
            catch (MalformedLineException ex)
            {
                throw new InvalidOperationException($"CSV satırı {ex.LineNumber} ayrıştırılamadı. Tırnak ve ayraç kullanımını kontrol edin.", ex);
            }
            return rows.ToArray();
        }
        if (ext == ".xlsx")
        {
            using var workbook = new ClosedXML.Excel.XLWorkbook(stream);
            var ws = workbook.Worksheet(1);
            var lastRow = ws.LastRowUsed()?.RowNumber() ?? 0;
            var lastColumn = ws.LastColumnUsed()?.ColumnNumber() ?? 0;
            var rows = new List<string[]>();
            var numberCulture = sayiAyraci == "." ? CultureInfo.InvariantCulture : CultureInfo.GetCultureInfo("tr-TR");
            for (var row = 1; row <= lastRow; row++)
            {
                var fields = new string[lastColumn];
                for (var column = 1; column <= lastColumn; column++)
                {
                    var cell = ws.Cell(row, column);
                    fields[column - 1] = cell.DataType switch
                    {
                        ClosedXML.Excel.XLDataType.DateTime => cell.GetDateTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                        ClosedXML.Excel.XLDataType.Number => cell.GetValue<decimal>().ToString(numberCulture),
                        _ => cell.GetString()
                    };
                }
                rows.Add(fields);
            }
            return rows.ToArray();
        }
        if (ext == ".xls")
            throw new InvalidOperationException("Eski Excel (.xls) biçimi desteklenmiyor. Dosyayı .xlsx veya CSV olarak kaydedin.");
        throw new InvalidOperationException($"Desteklenmeyen dosya türü: {ext}. CSV veya Excel (.xlsx) kullanın.");
    }

    private static string Temizle(string[] cols, int index)
        => (index <= 0 || index > cols.Length) ? string.Empty : cols[index - 1].Trim();

    private static DateTime ParseTarih(string value, string format)
    {
        var clean = value.Replace("\"", "").Trim();
        if (DateTime.TryParseExact(clean, format.Split('|'), CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt)) return DateTime.SpecifyKind(dt, DateTimeKind.Utc);
        if (DateTime.TryParse(clean, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt2)) return DateTime.SpecifyKind(dt2, DateTimeKind.Utc);
        if (DateTime.TryParse(clean, new CultureInfo("tr-TR"), DateTimeStyles.None, out var dt3)) return DateTime.SpecifyKind(dt3, DateTimeKind.Utc);
        throw new InvalidOperationException($"Tarih ayrıştırılamadı: '{value}'");
    }

    private static decimal ParseTutar(string value, string sayiAyraci)
    {
        var clean = value.Replace("\"", "").Replace(" ", "").Trim();
        if (sayiAyraci == ".") clean = clean.Replace(",", "");
        else clean = clean.Replace(".", "").Replace(",", ".");
        if (decimal.TryParse(clean, NumberStyles.Any, CultureInfo.InvariantCulture, out var d)) return d;
        throw new InvalidOperationException($"Tutar ayrıştırılamadı: '{value}'");
    }

    private static bool ParseBorcAlacak(string[] cols, BankaKolonMapping map)
    {
        if (map.BorcAlacakKolon <= 0) return true;
        var val = Temizle(cols, map.BorcAlacakKolon);
        if (string.IsNullOrWhiteSpace(val))
            throw new InvalidOperationException("Borç/alacak kolonu boş. İşlem yönünü belirtin.");

        // Tam değer eşleşmesi: açıklama içindeki bir harf işlem yönü sayılmaz.
        var borcMu = GetDirectionIndicators(true, map.BorcGostergesi).Contains(val);
        var alacakMi = GetDirectionIndicators(false, map.AlacakGostergesi).Contains(val);
        if (borcMu == alacakMi)
            throw new InvalidOperationException("Borç/alacak göstergesi tanınmıyor veya çelişkili. Şablondaki göstergeleri kontrol edin.");
        return borcMu;
    }

    private static HashSet<string> GetDirectionIndicators(bool borc, string? custom)
    {
        var indicators = new HashSet<string>(borc
            ? new[] { "B", "BORÇ", "BORC", "GİDEN", "GIDEN", "D", "DEBIT" }
            : new[] { "A", "ALACAK", "GELEN", "C", "CREDIT", "-" }, StringComparer.OrdinalIgnoreCase);
        if (custom is not null)
            foreach (var value in custom.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                indicators.Add(value);
        return indicators;
    }

    private static string ComputeSha256(byte[] data)
    {
        var hashBytes = SHA256.HashData(data);
        return Convert.ToHexStringLower(hashBytes);
    }

    #endregion
}


