using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using MKFiloServis.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MKFiloServis.Web.Services;

/// <summary>
/// Dinamik banka Excel/CSV import servisi.
/// Kullanıcı tanımlı kolon mapping ile çalışır.
/// SHA256 hash + duplicate + preview + snapshot entegrasyonlu.
/// </summary>
public class BankaImportService
{
    private readonly IDbContextFactory<ApplicationDbContext> _factory;
    private readonly IMaasSnapshotService? _snapshotService;

    public BankaImportService(IDbContextFactory<ApplicationDbContext> factory, IMaasSnapshotService? snapshotService = null)
    {
        _factory = factory;
        _snapshotService = snapshotService;
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
        var hash = ComputeSha256(fileBytes);

        using var ms = new MemoryStream(fileBytes);
        var satirlar = await DosyaOkuAsync(ms, fileName);
        var sonuc = new BankaImportSonuc { DosyaHash = hash, Basarili = true };

        var startIndex = map.BaslikVarMi ? 1 + map.AtlanacakSatir : map.AtlanacakSatir;
        var previewRows = Math.Min(5, satirlar.Length - startIndex);

        for (int i = startIndex; i < startIndex + previewRows; i++)
        {
            try
            {
                var hareket = ParseSatir(satirlar[i], map, firmaId);
                if (hareket != null) sonuc.Onizleme.Add(hareket);
            }
            catch (Exception ex)
            {
                sonuc.Hatalar.Add($"Satır {i + 1}: {ex.Message}");
                sonuc.Hata++;
            }
        }

        sonuc.AlinanSatir = Math.Max(0, satirlar.Length - startIndex);
        return sonuc;
    }

    /// <summary>
    /// Tam import: parse + hash kontrol + duplicate kontrol + kaydet + snapshot güncelle.
    /// </summary>
    public async Task<BankaImportSonuc> ImportAsync(byte[] fileBytes, string fileName, BankaKolonMapping map, int firmaId)
    {
        MapDogrula(map);
        var hash = ComputeSha256(fileBytes);
        var sonuc = new BankaImportSonuc { DosyaHash = hash };

        // ── Hash kontrol: aynı dosya daha önce yüklendi mi? ──
        await using var context = await _factory.CreateDbContextAsync();
        var hashVarMi = await context.FinansHareketler
            .AsNoTracking()
            .AnyAsync(x => x.DosyaHash == hash && x.FirmaId == firmaId && !x.IsDeleted);

        if (hashVarMi)
        {
            sonuc.Hatalar.Add($"Bu dosya daha önce yüklenmiş (Hash: {hash[..12]}...).");
            return sonuc;
        }

        using var ms = new MemoryStream(fileBytes);
        var satirlar = await DosyaOkuAsync(ms, fileName);
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
            .Select(x => new { x.Tarih, x.ReferansNo, x.Tutar })
            .ToListAsync();

        for (int i = startIndex; i < satirlar.Length; i++)
        {
            var line = satirlar[i].Trim();
            if (string.IsNullOrWhiteSpace(line)) continue;

            try
            {
                var hareket = ParseSatir(line, map, firmaId);
                if (hareket == null) continue;

                hareket.DosyaHash = hash;

                // ── Duplicate kontrol: ReferansNo + Tarih + Tutar ──
                if (!string.IsNullOrWhiteSpace(hareket.ReferansNo))
                {
                    var dup = mevcutTarihReferanslar.Any(x =>
                        x.Tarih.Date == hareket.Tarih.Date &&
                        x.ReferansNo == hareket.ReferansNo &&
                        x.Tutar == hareket.Tutar);

                    if (dup)
                    {
                        sonuc.Atlanan++;
                        continue;
                    }
                }

                kaydedilecekler.Add(hareket);
            }
            catch (Exception ex)
            {
                sonuc.Hatalar.Add($"Satır {i + 1}: {ex.Message}");
                sonuc.Hata++;
            }
        }

        // ── Toplu kaydet ──
        if (kaydedilecekler.Any())
        {
            context.FinansHareketler.AddRange(kaydedilecekler);
            await context.SaveChangesAsync();
            sonuc.Kaydedilen = kaydedilecekler.Count;
            sonuc.Kaydedilenler = kaydedilecekler;
            sonuc.Basarili = true;

            // Import tamam

            // ── Snapshot güncelle (varsa) ──
            if (_snapshotService != null)
            {
                try
                {
                    var now = DateTime.Now;
                    var varMi = await _snapshotService.VarMiAsync(now.Year, now.Month, firmaId);
                    if (varMi)
                    {
                        await _snapshotService.GuncelleAsync(now.Year, now.Month, firmaId,
                            new List<(int, string, string?, string?, string?, decimal, decimal, decimal, decimal, decimal, decimal, decimal, decimal)>());
                    }
                }
                catch (Exception ex) { Console.WriteLine($"[BankaImport] Snapshot güncelleme hatası (önemsiz): {ex.Message}"); }
            }
        }

        return sonuc;
    }

    /// <summary>
    /// CSV başlık satırını okuyup kolon adlarını döndürür (TahminEt için).
    /// </summary>
    public async Task<string[]> ReadHeadersAsync(byte[] fileBytes, string fileName)
    {
        using var ms = new MemoryStream(fileBytes);
        var satirlar = await DosyaOkuAsync(ms, fileName);
        if (satirlar.Length == 0) return Array.Empty<string>();

        var firstLine = satirlar[0].Trim();
        return firstLine.Split(';', ',', '\t').Select(c => c.Trim().Trim('"')).ToArray();
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
        await using var context = await _factory.CreateDbContextAsync();
        if (map.Id > 0)
        {
            var existing = await context.BankaKolonMappingler.FindAsync(map.Id);
            if (existing != null)
            {
                existing.Ad = map.Ad; existing.TarihKolon = map.TarihKolon; existing.AciklamaKolon = map.AciklamaKolon;
                existing.TutarKolon = map.TutarKolon; existing.BorcAlacakKolon = map.BorcAlacakKolon; existing.ReferansKolon = map.ReferansKolon;
                existing.DosyaTipi = map.DosyaTipi; existing.Ayrac = map.Ayrac; existing.TarihFormati = map.TarihFormati;
                existing.SayiAyraci = map.SayiAyraci; existing.BorcGostergesi = map.BorcGostergesi; existing.AlacakGostergesi = map.AlacakGostergesi;
                existing.BaslikVarMi = map.BaslikVarMi; existing.AtlanacakSatir = map.AtlanacakSatir; existing.Varsayilan = map.Varsayilan;
                existing.UpdatedAt = DateTime.UtcNow;
                await context.SaveChangesAsync();
                return existing;
            }
        }
        context.BankaKolonMappingler.Add(map);
        await context.SaveChangesAsync();
        return map;
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
        await using var context = await _factory.CreateDbContextAsync();
        await context.BankaKolonMappingler.Where(x => x.Id == id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.IsDeleted, true).SetProperty(x => x.DeletedAt, DateTime.UtcNow));
    }

    #region Private Helpers

    private static void MapDogrula(BankaKolonMapping map)
    {
        if (map.TarihKolon <= 0) throw new InvalidOperationException("Tarih kolonu zorunludur.");
        if (map.TutarKolon <= 0) throw new InvalidOperationException("Tutar kolonu zorunludur.");
    }

    private static FinansHareket? ParseSatir(string line, BankaKolonMapping map, int firmaId)
    {
        var cols = line.Split(map.Ayrac);
        if (cols.Length < Math.Max(map.TarihKolon, Math.Max(map.TutarKolon, map.AciklamaKolon > 0 ? map.AciklamaKolon : 1)))
            return null;

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

    private static async Task<string[]> DosyaOkuAsync(Stream stream, string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        if (ext == ".csv" || ext == ".txt")
        {
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            return (await reader.ReadToEndAsync()).Split('\n', StringSplitOptions.RemoveEmptyEntries);
        }
        if (ext == ".xlsx" || ext == ".xls")
        {
            using var workbook = new ClosedXML.Excel.XLWorkbook(stream);
            var ws = workbook.Worksheet(1);
            return ws.RowsUsed().Select(row => string.Join(";", row.Cells().Select(c => c.GetString()))).ToArray();
        }
        throw new InvalidOperationException($"Desteklenmeyen dosya türü: {ext}. CSV veya Excel (.xlsx) kullanın.");
    }

    private static string Temizle(string[] cols, int index)
        => (index <= 0 || index > cols.Length) ? string.Empty : cols[index - 1].Trim().Trim('"', '\'');

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
        var val = Temizle(cols, map.BorcAlacakKolon).ToUpperInvariant().Trim();

        // Negatif tutar → alacak
        if (val == "-" || val.StartsWith("-")) return false;

        // Borç göstergeleri: B, BORÇ, GİDEN, D, DEBIT
        if (val is "B" or "BORÇ" or "BORC" or "GİDEN" or "GIDEN" or "D" or "DEBIT") return true;
        // Alacak göstergeleri: A, ALACAK, GELEN, C, CREDIT
        if (val is "A" or "ALACAK" or "GELEN" or "C" or "CREDIT") return false;

        // Kullanıcının özel göstergeleri
        var borcIndicators = (map.BorcGostergesi ?? "B").ToUpperInvariant().Split('|');
        var alacakIndicators = (map.AlacakGostergesi ?? "A").ToUpperInvariant().Split('|');
        foreach (var b in borcIndicators) if (val.Contains(b.Trim())) return true;
        foreach (var a in alacakIndicators) if (val.Contains(a.Trim())) return false;

        return true;
    }

    private static string ComputeSha256(byte[] data)
    {
        var hashBytes = SHA256.HashData(data);
        return Convert.ToHexStringLower(hashBytes);
    }

    #endregion
}


