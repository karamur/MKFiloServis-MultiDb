using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Web.Services;

/// <summary>
/// Banka/Kasa hareket içeri aktarma servisi.
/// <para>
/// İçeri aktarılan Excel/CSV/PDF satırları kullanıcı eşleştirmesini onaylayana
/// kadar veritabanına yazılmaz. Stage kayıtları per-circuit (Scoped) tutulur,
/// kullanıcı "Yansıt" dediğinde seçili satırlar tek bir transaction içinde
/// <see cref="BankaKasaHareket"/> olarak yazılır.
/// </para>
/// </summary>
public interface IBankaHareketImportService
{
    Task<BankaHareketImportSonuc> DosyadanYukleAsync(string dosyaAdi, byte[] icerik);
    IReadOnlyList<BankaHareketImportSatir> MevcutSatirlar { get; }
    void Temizle();
    void SatirGuncelle(Guid id, Action<BankaHareketImportSatir> update);
    Task<BankaHareketImportYansitSonuc> YansitAsync(int bankaHesapId, IEnumerable<Guid> satirIds);
}

public class BankaHareketImportSatir
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int SiraNo { get; set; }
    public DateTime? Tarih { get; set; }
    public string? Aciklama { get; set; }
    public string? BelgeNo { get; set; }
    public decimal? Tutar { get; set; }
    public HareketTipi HareketTipi { get; set; }
    /// <summary>Eşleşen Cari (opsiyonel).</summary>
    public int? CariId { get; set; }
    /// <summary>Eşleşen Araç (opsiyonel).</summary>
    public int? AracId { get; set; }
    public string? MuhasebeHesapKodu { get; set; }
    public string? KostMerkeziKodu { get; set; }
    public bool Secili { get; set; } = true;
    public string? HataMesaji { get; set; }
    public string? KaynakSatirOzeti { get; set; }
}

public class BankaHareketImportSonuc
{
    public int OkunanSatir { get; set; }
    public int GecerliSatir { get; set; }
    public List<string> Uyarilar { get; set; } = new();
    public string? Hata { get; set; }
    public string KaynakTipi { get; set; } = string.Empty; // Excel/CSV/PDF
}

public class BankaHareketImportYansitSonuc
{
    public int YazilanKayit { get; set; }
    public List<string> Hatalar { get; set; } = new();
    public bool Basarili => Hatalar.Count == 0;
}

public class BankaHareketImportService : IBankaHareketImportService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IBankaKasaHareketService _hareketService;
    private readonly List<BankaHareketImportSatir> _stage = new();

    public BankaHareketImportService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IBankaKasaHareketService hareketService)
    {
        _contextFactory = contextFactory;
        _hareketService = hareketService;
    }

    public IReadOnlyList<BankaHareketImportSatir> MevcutSatirlar => _stage;

    public void Temizle() => _stage.Clear();

    public void SatirGuncelle(Guid id, Action<BankaHareketImportSatir> update)
    {
        var satir = _stage.FirstOrDefault(s => s.Id == id);
        if (satir != null) update(satir);
    }

    public async Task<BankaHareketImportSonuc> DosyadanYukleAsync(string dosyaAdi, byte[] icerik)
    {
        var sonuc = new BankaHareketImportSonuc();
        var ext = Path.GetExtension(dosyaAdi).ToLowerInvariant();
        _stage.Clear();

        try
        {
            List<BankaHareketImportSatir> satirlar;
            switch (ext)
            {
                case ".xlsx":
                case ".xls":
                    satirlar = ParseExcel(icerik, sonuc);
                    sonuc.KaynakTipi = "Excel";
                    break;
                case ".csv":
                    satirlar = ParseCsv(icerik, sonuc);
                    sonuc.KaynakTipi = "CSV";
                    break;
                case ".pdf":
                    satirlar = ParsePdf(icerik, sonuc);
                    sonuc.KaynakTipi = "PDF";
                    break;
                default:
                    sonuc.Hata = $"Desteklenmeyen dosya türü: {ext}";
                    return sonuc;
            }

            // Sıra ve geçerlilik bilgisi
            var i = 1;
            foreach (var s in satirlar)
            {
                s.SiraNo = i++;
                if (s.Tutar == null || s.Tutar == 0) s.HataMesaji = "Tutar okunamadı";
                else if (s.Tarih == null) s.HataMesaji = "Tarih okunamadı";
            }

            _stage.AddRange(satirlar);
            sonuc.OkunanSatir = satirlar.Count;
            sonuc.GecerliSatir = satirlar.Count(s => s.HataMesaji == null);
        }
        catch (Exception ex)
        {
            sonuc.Hata = $"Dosya işlenirken hata: {ex.Message}";
        }

        // Cari otomatik eşleştirme (Açıklama içinden ünvana göre)
        await OtoCariEslestirAsync();
        return sonuc;
    }

    private async Task OtoCariEslestirAsync()
    {
        if (_stage.Count == 0) return;
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        var cariler = await ctx.Cariler.AsNoTracking()
            .Where(c => !c.IsDeleted)
            .Select(c => new { c.Id, c.Unvan })
            .ToListAsync();

        foreach (var s in _stage)
        {
            if (string.IsNullOrWhiteSpace(s.Aciklama)) continue;
            var acUpper = s.Aciklama.ToUpperInvariant();
            var match = cariler.FirstOrDefault(c =>
                !string.IsNullOrWhiteSpace(c.Unvan) &&
                acUpper.Contains(c.Unvan.ToUpperInvariant()));
            if (match != null) s.CariId = match.Id;
        }
    }

    public async Task<BankaHareketImportYansitSonuc> YansitAsync(int bankaHesapId, IEnumerable<Guid> satirIds)
    {
        var sonuc = new BankaHareketImportYansitSonuc();
        if (bankaHesapId <= 0)
        {
            sonuc.Hatalar.Add("Hedef banka/kasa hesabı seçilmedi.");
            return sonuc;
        }

        var seciliSatirlar = _stage
            .Where(s => satirIds.Contains(s.Id) && s.HataMesaji == null)
            .ToList();

        if (seciliSatirlar.Count == 0)
        {
            sonuc.Hatalar.Add("Yansıtılacak (geçerli ve seçili) satır bulunamadı.");
            return sonuc;
        }

        foreach (var s in seciliSatirlar)
        {
            try
            {
                var hareket = new BankaKasaHareket
                {
                    BankaHesapId = bankaHesapId,
                    IslemTarihi = DateTime.SpecifyKind(s.Tarih!.Value.Date, DateTimeKind.Utc),
                    HareketTipi = s.HareketTipi,
                    Tutar = Math.Abs(s.Tutar!.Value),
                    Aciklama = s.Aciklama,
                    BelgeNo = s.BelgeNo,
                    CariId = s.CariId,
                    AracId = s.AracId,
                    MuhasebeHesapKodu = s.MuhasebeHesapKodu,
                    KostMerkeziKodu = s.KostMerkeziKodu,
                    IslemKaynak = IslemKaynak.Manuel
                };
                await _hareketService.CreateAsync(hareket);
                sonuc.YazilanKayit++;
                _stage.Remove(s);
            }
            catch (Exception ex)
            {
                sonuc.Hatalar.Add($"#{s.SiraNo} {s.Aciklama}: {ex.Message}");
            }
        }

        return sonuc;
    }

    // ============================ PARSERS ============================

    private static List<BankaHareketImportSatir> ParseExcel(byte[] icerik, BankaHareketImportSonuc sonuc)
    {
        var liste = new List<BankaHareketImportSatir>();
        using var ms = new MemoryStream(icerik);
        using var wb = new XLWorkbook(ms);
        var ws = wb.Worksheets.First();
        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;
        var lastCol = ws.Row(1).LastCellUsed()?.Address.ColumnNumber ?? 0;

        var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int c = 1; c <= lastCol; c++)
        {
            var h = ws.Cell(1, c).GetString()?.Trim();
            if (!string.IsNullOrWhiteSpace(h) && !headers.ContainsKey(h))
                headers[h] = c;
        }

        int? Find(params string[] alternatifler)
        {
            foreach (var a in alternatifler)
            {
                foreach (var kv in headers)
                {
                    if (kv.Key.Replace(" ", "").Equals(a.Replace(" ", ""), StringComparison.OrdinalIgnoreCase))
                        return kv.Value;
                }
            }
            return null;
        }

        var tarihCol = Find("Tarih", "İşlem Tarihi", "Islem Tarihi", "Date");
        var aciklamaCol = Find("Açıklama", "Aciklama", "Description", "İşlem Açıklaması");
        var belgeCol = Find("Belge No", "Dekont", "Referans", "Reference");
        var tutarCol = Find("Tutar", "Amount");
        var girisCol = Find("Giriş", "Giris", "Alacak", "Credit");
        var cikisCol = Find("Çıkış", "Cikis", "Borç", "Borc", "Debit");

        if (tarihCol == null || (tutarCol == null && girisCol == null && cikisCol == null))
        {
            sonuc.Uyarilar.Add("Beklenen kolonlar (Tarih, Tutar/Giriş/Çıkış) bulunamadı. Lütfen başlık satırını kontrol edin.");
        }

        for (int r = 2; r <= lastRow; r++)
        {
            var satir = new BankaHareketImportSatir();
            if (tarihCol.HasValue) satir.Tarih = ReadDate(ws.Cell(r, tarihCol.Value));
            if (aciklamaCol.HasValue) satir.Aciklama = ws.Cell(r, aciklamaCol.Value).GetString()?.Trim();
            if (belgeCol.HasValue) satir.BelgeNo = ws.Cell(r, belgeCol.Value).GetString()?.Trim();

            decimal? giris = girisCol.HasValue ? ReadDecimal(ws.Cell(r, girisCol.Value)) : null;
            decimal? cikis = cikisCol.HasValue ? ReadDecimal(ws.Cell(r, cikisCol.Value)) : null;
            decimal? tutar = tutarCol.HasValue ? ReadDecimal(ws.Cell(r, tutarCol.Value)) : null;

            if (giris.HasValue && giris.Value > 0)
            {
                satir.Tutar = giris.Value;
                satir.HareketTipi = HareketTipi.Giris;
            }
            else if (cikis.HasValue && cikis.Value > 0)
            {
                satir.Tutar = cikis.Value;
                satir.HareketTipi = HareketTipi.Cikis;
            }
            else if (tutar.HasValue && tutar.Value != 0)
            {
                satir.Tutar = Math.Abs(tutar.Value);
                satir.HareketTipi = tutar.Value >= 0 ? HareketTipi.Giris : HareketTipi.Cikis;
            }

            // Boş satırı atla
            if (satir.Tarih == null && satir.Tutar == null && string.IsNullOrWhiteSpace(satir.Aciklama))
                continue;

            satir.KaynakSatirOzeti = $"Satır {r}";
            liste.Add(satir);
        }
        return liste;
    }

    private static List<BankaHareketImportSatir> ParseCsv(byte[] icerik, BankaHareketImportSonuc sonuc)
    {
        var liste = new List<BankaHareketImportSatir>();
        var text = DecodeText(icerik);
        var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length < 2) return liste;

        var sep = DetectSeparator(lines[0]);
        var headers = lines[0].Split(sep).Select(h => h.Trim().Trim('"')).ToList();

        int IndexOf(params string[] names)
        {
            foreach (var n in names)
            {
                var idx = headers.FindIndex(h => h.Replace(" ", "").Equals(n.Replace(" ", ""), StringComparison.OrdinalIgnoreCase));
                if (idx >= 0) return idx;
            }
            return -1;
        }

        var tarihIdx = IndexOf("Tarih", "Date", "İşlem Tarihi", "Islem Tarihi");
        var aciklamaIdx = IndexOf("Aciklama", "Açıklama", "Description");
        var belgeIdx = IndexOf("Belge No", "Referans", "Reference", "Dekont");
        var tutarIdx = IndexOf("Tutar", "Amount");
        var girisIdx = IndexOf("Giris", "Giriş", "Alacak", "Credit");
        var cikisIdx = IndexOf("Cikis", "Çıkış", "Borc", "Borç", "Debit");

        for (int i = 1; i < lines.Length; i++)
        {
            var parts = SplitCsv(lines[i], sep);
            if (parts.Count == 0) continue;

            var satir = new BankaHareketImportSatir();
            if (tarihIdx >= 0 && tarihIdx < parts.Count) satir.Tarih = TryParseDate(parts[tarihIdx]);
            if (aciklamaIdx >= 0 && aciklamaIdx < parts.Count) satir.Aciklama = parts[aciklamaIdx];
            if (belgeIdx >= 0 && belgeIdx < parts.Count) satir.BelgeNo = parts[belgeIdx];

            decimal? giris = girisIdx >= 0 && girisIdx < parts.Count ? TryParseDecimal(parts[girisIdx]) : null;
            decimal? cikis = cikisIdx >= 0 && cikisIdx < parts.Count ? TryParseDecimal(parts[cikisIdx]) : null;
            decimal? tutar = tutarIdx >= 0 && tutarIdx < parts.Count ? TryParseDecimal(parts[tutarIdx]) : null;

            if (giris.HasValue && giris.Value > 0)
            {
                satir.Tutar = giris.Value; satir.HareketTipi = HareketTipi.Giris;
            }
            else if (cikis.HasValue && cikis.Value > 0)
            {
                satir.Tutar = cikis.Value; satir.HareketTipi = HareketTipi.Cikis;
            }
            else if (tutar.HasValue && tutar.Value != 0)
            {
                satir.Tutar = Math.Abs(tutar.Value);
                satir.HareketTipi = tutar.Value >= 0 ? HareketTipi.Giris : HareketTipi.Cikis;
            }

            satir.KaynakSatirOzeti = $"CSV satır {i + 1}";
            liste.Add(satir);
        }

        if (liste.Count == 0)
            sonuc.Uyarilar.Add("CSV içinden veri satırı çıkarılamadı.");

        return liste;
    }

    private static List<BankaHareketImportSatir> ParsePdf(byte[] icerik, BankaHareketImportSonuc sonuc)
    {
        var liste = new List<BankaHareketImportSatir>();
        using var pdf = PdfDocument.Open(icerik);
        var sb = new StringBuilder();
        foreach (Page page in pdf.GetPages())
        {
            sb.AppendLine(page.Text);
        }
        var text = sb.ToString();
        var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

        // Tarih + tutar yakalayan basit satır parse. Format: dd.MM.yyyy ... 1.234,56
        var tarihRegex = new System.Text.RegularExpressions.Regex(@"\b(\d{2}[./-]\d{2}[./-]\d{2,4})\b");
        var tutarRegex = new System.Text.RegularExpressions.Regex(@"-?\d{1,3}(?:[.,]\d{3})*[.,]\d{2}");

        int siraNo = 0;
        foreach (var raw in lines)
        {
            var line = raw.Trim();
            if (line.Length < 10) continue;

            var tMatch = tarihRegex.Match(line);
            var amounts = tutarRegex.Matches(line);
            if (!tMatch.Success || amounts.Count == 0) continue;

            var tarih = TryParseDate(tMatch.Value);
            if (tarih == null) continue;

            // En sondaki tutarı al (genellikle bakiye, ondan önceki tutar hareket)
            decimal? tutar = null;
            if (amounts.Count >= 2)
            {
                tutar = TryParseDecimal(amounts[amounts.Count - 2].Value);
            }
            else
            {
                tutar = TryParseDecimal(amounts[0].Value);
            }
            if (tutar == null) continue;

            var aciklama = line
                .Replace(tMatch.Value, "")
                .Trim();
            foreach (System.Text.RegularExpressions.Match a in amounts)
                aciklama = aciklama.Replace(a.Value, "").Trim();

            siraNo++;
            liste.Add(new BankaHareketImportSatir
            {
                Tarih = tarih,
                Aciklama = aciklama,
                Tutar = Math.Abs(tutar.Value),
                HareketTipi = tutar.Value >= 0 ? HareketTipi.Giris : HareketTipi.Cikis,
                KaynakSatirOzeti = $"PDF satır {siraNo}"
            });
        }

        if (liste.Count == 0)
            sonuc.Uyarilar.Add("PDF içeriğinden tarih+tutar barındıran satır okunamadı. Eşleştirmeden önce satırları manuel olarak düzenleyin.");

        return liste;
    }

    // ============================ HELPERS ============================

    private static DateTime? ReadDate(IXLCell cell)
    {
        if (cell.IsEmpty()) return null;
        if (cell.DataType == XLDataType.DateTime) return cell.GetDateTime();
        if (cell.DataType == XLDataType.Number) return DateTime.FromOADate(cell.GetDouble());
        return TryParseDate(cell.GetString());
    }

    private static decimal? ReadDecimal(IXLCell cell)
    {
        if (cell.IsEmpty()) return null;
        if (cell.DataType == XLDataType.Number) return (decimal)cell.GetDouble();
        return TryParseDecimal(cell.GetString());
    }

    private static DateTime? TryParseDate(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var s = raw.Trim();
        string[] formats = { "dd.MM.yyyy", "dd/MM/yyyy", "dd-MM-yyyy", "yyyy-MM-dd", "dd.MM.yy", "d.M.yyyy" };
        if (DateTime.TryParseExact(s, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)) return d;
        if (DateTime.TryParse(s, new CultureInfo("tr-TR"), DateTimeStyles.None, out d)) return d;
        return DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out d) ? d : null;
    }

    private static decimal? TryParseDecimal(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var s = raw.Trim().Replace(" ", "").Replace("TL", "").Replace("₺", "");
        // Türkçe: 1.234,56 -> 1234.56
        if (s.Contains(',') && s.Contains('.'))
            s = s.Replace(".", "").Replace(",", ".");
        else if (s.Contains(','))
            s = s.Replace(",", ".");
        return decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out var v) ? v : null;
    }

    private static char DetectSeparator(string header)
    {
        if (header.Count(c => c == ';') > header.Count(c => c == ',')) return ';';
        if (header.Contains('\t')) return '\t';
        return ',';
    }

    private static List<string> SplitCsv(string line, char sep)
    {
        var result = new List<string>();
        var sb = new StringBuilder();
        bool insideQuotes = false;
        foreach (var ch in line)
        {
            if (ch == '"') { insideQuotes = !insideQuotes; continue; }
            if (ch == sep && !insideQuotes) { result.Add(sb.ToString().Trim()); sb.Clear(); continue; }
            sb.Append(ch);
        }
        result.Add(sb.ToString().Trim());
        return result;
    }

    private static string DecodeText(byte[] icerik)
    {
        // UTF-8 / UTF-8 BOM / Windows-1254 fallback
        if (icerik.Length >= 3 && icerik[0] == 0xEF && icerik[1] == 0xBB && icerik[2] == 0xBF)
            return Encoding.UTF8.GetString(icerik, 3, icerik.Length - 3);
        try
        {
            return Encoding.UTF8.GetString(icerik);
        }
        catch
        {
            try
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                return Encoding.GetEncoding(1254).GetString(icerik);
            }
            catch
            {
                return Encoding.Default.GetString(icerik);
            }
        }
    }
}



