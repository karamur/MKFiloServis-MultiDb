using System.Globalization;
using System.Text;
using KOAFiloServis.Shared.Entities;
using KOAFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace KOAFiloServis.Web.Services;

/// <summary>
/// Dinamik banka Excel/CSV import servisi.
/// Kullanıcı tanımlı kolon mapping ile çalışır.
/// </summary>
public class BankaImportService
{
    private readonly IDbContextFactory<ApplicationDbContext> _factory;

    public BankaImportService(IDbContextFactory<ApplicationDbContext> factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Dosyayı parse edip FinansHareket listesi döndürür.
    /// </summary>
    public async Task<List<FinansHareket>> ParseAsync(Stream fileStream, string fileName, BankaKolonMapping map, int firmaId)
    {
        MapDogrula(map);

        var satirlar = await DosyaOkuAsync(fileStream, fileName);
        if (satirlar.Length == 0)
            throw new InvalidOperationException("Dosya boş.");

        var startIndex = map.BaslikVarMi ? 1 + map.AtlanacakSatir : map.AtlanacakSatir;
        var liste = new List<FinansHareket>();

        for (int i = startIndex; i < satirlar.Length; i++)
        {
            var line = satirlar[i].Trim();
            if (string.IsNullOrWhiteSpace(line)) continue;

            var cols = line.Split(map.Ayrac);
            if (cols.Length < Math.Max(map.TarihKolon, Math.Max(map.TutarKolon, map.AciklamaKolon)))
                continue;

            var tarih = ParseTarih(Temizle(cols, map.TarihKolon), map.TarihFormati);
            var aciklama = Temizle(cols, map.AciklamaKolon);
            var tutar = ParseTutar(Temizle(cols, map.TutarKolon), map.SayiAyraci);
            var borcMu = ParseBorcAlacak(cols, map);
            var referansNo = map.ReferansKolon > 0 ? Temizle(cols, map.ReferansKolon) : null;

            liste.Add(new FinansHareket
            {
                FirmaId = firmaId,
                Tarih = tarih,
                Tip = "Banka",
                Tutar = tutar,
                BorcMu = borcMu,
                Aciklama = aciklama,
                ReferansNo = referansNo,
                CreatedAt = DateTime.UtcNow
            });
        }

        return liste;
    }

    /// <summary>
    /// CSV/Ekstre başlık satırındaki kolon adlarını analiz edip otomatik tahmin yapar.
    /// Smart detection: "Tarih", "Açıklama", "Tutar" gibi anahtar kelimeler arar.
    /// </summary>
    public BankaKolonMapping TahminEt(string[] headerColumns)
    {
        var map = new BankaKolonMapping
        {
            TarihKolon = 0,
            AciklamaKolon = 0,
            TutarKolon = 0,
            BaslikVarMi = true,
            Ayrac = ";",
            SayiAyraci = ",",
            TarihFormati = "dd.MM.yyyy"
        };

        for (int i = 0; i < headerColumns.Length; i++)
        {
            var h = headerColumns[i].Trim().ToLowerInvariant();

            if (map.TarihKolon == 0 && (h.Contains("tarih") || h.Contains("date") || h.Contains("işlem") || h.Contains("valör")))
                map.TarihKolon = i + 1;

            if (map.AciklamaKolon == 0 && (h.Contains("açıklama") || h.Contains("aciklama") || h.Contains("description") || h.Contains("işlem")))
                map.AciklamaKolon = i + 1;

            if (map.TutarKolon == 0 && (h.Contains("tutar") || h.Contains("miktar") || h.Contains("amount") || h.Contains("işlem tutarı")))
                map.TutarKolon = i + 1;

            if (map.BorcAlacakKolon == 0 && (h.Contains("borç") || h.Contains("alacak") || h.Contains("b/a") || h.Contains("işaret") || h.Contains("d/c")))
                map.BorcAlacakKolon = i + 1;

            if (map.ReferansKolon == 0 && (h.Contains("referans") || h.Contains("işlem no") || h.Contains("dekont") || h.Contains("ref")))
                map.ReferansKolon = i + 1;
        }

        // Fallback: bulunamayanlar için sıralı tahmin
        if (map.TarihKolon == 0) map.TarihKolon = 1;
        if (map.AciklamaKolon == 0) map.AciklamaKolon = 2;
        if (map.TutarKolon == 0) map.TutarKolon = 3;

        return map;
    }

    /// <summary>
    /// Mapping kaydet / güncelle.
    /// </summary>
    public async Task<BankaKolonMapping> KaydetAsync(BankaKolonMapping map)
    {
        await using var context = await _factory.CreateDbContextAsync();

        if (map.Id > 0)
        {
            var existing = await context.BankaKolonMappingler.FindAsync(map.Id);
            if (existing != null)
            {
                existing.Ad = map.Ad;
                existing.TarihKolon = map.TarihKolon;
                existing.AciklamaKolon = map.AciklamaKolon;
                existing.TutarKolon = map.TutarKolon;
                existing.BorcAlacakKolon = map.BorcAlacakKolon;
                existing.ReferansKolon = map.ReferansKolon;
                existing.DosyaTipi = map.DosyaTipi;
                existing.Ayrac = map.Ayrac;
                existing.TarihFormati = map.TarihFormati;
                existing.SayiAyraci = map.SayiAyraci;
                existing.BorcGostergesi = map.BorcGostergesi;
                existing.AlacakGostergesi = map.AlacakGostergesi;
                existing.BaslikVarMi = map.BaslikVarMi;
                existing.AtlanacakSatir = map.AtlanacakSatir;
                existing.Varsayilan = map.Varsayilan;
                existing.UpdatedAt = DateTime.UtcNow;
                await context.SaveChangesAsync();
                return existing;
            }
        }

        context.BankaKolonMappingler.Add(map);
        await context.SaveChangesAsync();
        return map;
    }

    /// <summary>
    /// Firma için kayıtlı mappingleri listeler.
    /// </summary>
    public async Task<List<BankaKolonMapping>> GetMappingsAsync(int firmaId)
    {
        await using var context = await _factory.CreateDbContextAsync();
        return await context.BankaKolonMappingler
            .AsNoTracking()
            .Where(x => x.FirmaId == firmaId && !x.IsDeleted)
            .OrderByDescending(x => x.Varsayilan)
            .ThenBy(x => x.Ad)
            .ToListAsync();
    }

    /// <summary>
    /// Mapping sil.
    /// </summary>
    public async Task SilAsync(int id)
    {
        await using var context = await _factory.CreateDbContextAsync();
        await context.BankaKolonMappingler
            .Where(x => x.Id == id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.IsDeleted, true)
                .SetProperty(x => x.DeletedAt, DateTime.UtcNow));
    }

    #region Private Helpers

    private static void MapDogrula(BankaKolonMapping map)
    {
        if (map.TarihKolon <= 0)
            throw new InvalidOperationException("Tarih kolonu zorunludur.");
        if (map.TutarKolon <= 0)
            throw new InvalidOperationException("Tutar kolonu zorunludur.");
        if (map.AciklamaKolon <= 0)
            throw new InvalidOperationException("Açıklama kolonu zorunludur.");
    }

    private static async Task<string[]> DosyaOkuAsync(Stream stream, string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();

        if (ext == ".csv" || ext == ".txt")
        {
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            var content = await reader.ReadToEndAsync();
            return content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        }

        if (ext == ".xlsx" || ext == ".xls")
        {
            using var workbook = new ClosedXML.Excel.XLWorkbook(stream);
            var ws = workbook.Worksheet(1);
            var rows = new List<string>();
            foreach (var row in ws.RowsUsed())
            {
                var cells = row.Cells().Select(c => c.GetString()).ToList();
                rows.Add(string.Join(";", cells));
            }
            return rows.ToArray();
        }

        throw new InvalidOperationException($"Desteklenmeyen dosya türü: {ext}. CSV veya Excel (.xlsx) kullanın.");
    }

    private static string Temizle(string[] cols, int index)
    {
        if (index <= 0 || index > cols.Length) return string.Empty;
        return cols[index - 1].Trim().Trim('"', '\'');
    }

    private static DateTime ParseTarih(string value, string format)
    {
        var clean = value.Replace("\"", "").Trim();
        if (DateTime.TryParseExact(clean, format.Split('|'), CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            return DateTime.SpecifyKind(dt, DateTimeKind.Utc);
        if (DateTime.TryParse(clean, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt2))
            return DateTime.SpecifyKind(dt2, DateTimeKind.Utc);
        if (DateTime.TryParse(clean, new CultureInfo("tr-TR"), DateTimeStyles.None, out var dt3))
            return DateTime.SpecifyKind(dt3, DateTimeKind.Utc);
        throw new InvalidOperationException($"Tarih ayrıştırılamadı: '{value}' (Format: {format})");
    }

    private static decimal ParseTutar(string value, string sayiAyraci)
    {
        var clean = value.Replace("\"", "").Replace(" ", "").Trim();
        if (sayiAyraci == ".") clean = clean.Replace(",", "");   // 1.234,56 → 1234.56
        else clean = clean.Replace(".", "").Replace(",", ".");   // 1.234,56 → 1234.56
        if (decimal.TryParse(clean, NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
            return d;
        throw new InvalidOperationException($"Tutar ayrıştırılamadı: '{value}'");
    }

    private static bool ParseBorcAlacak(string[] cols, BankaKolonMapping map)
    {
        if (map.BorcAlacakKolon <= 0)
            return true; // varsayılan: borç

        var val = Temizle(cols, map.BorcAlacakKolon).ToUpperInvariant();

        var borcIndicators = (map.BorcGostergesi ?? "B").ToUpperInvariant().Split('|');
        var alacakIndicators = (map.AlacakGostergesi ?? "A").ToUpperInvariant().Split('|');

        foreach (var b in borcIndicators)
            if (val.Contains(b.Trim())) return true;
        foreach (var a in alacakIndicators)
            if (val.Contains(a.Trim())) return false;

        // Fallback: negatif tutar = alacak
        if (val == "-" || val.StartsWith("-")) return false;

        return true; // varsayılan borç
    }

    #endregion
}
