using ClosedXML.Excel;
using KOAFiloServis.Shared.Entities;
using KOAFiloServis.Web.Data;
using KOAFiloServis.Web.Models;
using KOAFiloServis.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KOAFiloServis.Web.Services;

/// <summary>
/// Puantaj sonrası fatura hazırlık için READONLY rapor servisi.
/// TEK kaynak: PuantajKayit (OperasyonKaydi → PuantajEngineService → PuantajKayit)
/// Tüm alanlar B1'den karşılanır — KDV %10+%20 ayrımı, KurumId, FaturaKesiciCariId, Gun01-31.
/// Hiçbir kayıt oluşturmaz, güncellemez, silmez.
/// </summary>
public class PuantajFaturaRaporService : IPuantajFaturaRaporService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private const int MaxPageSize = 500;

    public PuantajFaturaRaporService(IDbContextFactory<ApplicationDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    // ══════════════════════════════════════════════
    // ÖZET
    // ══════════════════════════════════════════════

    public async Task<PuantajFaturaOzetDto> GetOzetAsync(PuantajFaturaRaporRequest request, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var ozetData = await BuildPuantajKayitQuery(db, request)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Count = g.Count(),
                Gelir = g.Sum(x => x.ToplamGelir),
                Gider = g.Sum(x => x.ToplamGider),
                Kdv = g.Sum(x => x.GelirKdvTutari + x.GiderKdv20Tutari + x.GiderKdv10Tutari),
                Kesinti = g.Sum(x => x.GelirKesinti + x.GiderKesinti),
                Sefer = (int)g.Sum(x => x.Gun),
                FaturaKesilen = g.Count(x => x.GelirFaturaKesildi || x.GiderFaturaAlindi)
            })
            .FirstOrDefaultAsync(ct);

        var ozet = new PuantajFaturaOzetDto
        {
            ToplamKayit = ozetData?.Count ?? 0,
            FaturaKesilen = ozetData?.FaturaKesilen ?? 0,
            ToplamSefer = ozetData?.Sefer ?? 0,
            ToplamGelir = ozetData?.Gelir ?? 0,
            ToplamGider = ozetData?.Gider ?? 0,
            ToplamKdv = ozetData?.Kdv ?? 0,
            ToplamKesinti = ozetData?.Kesinti ?? 0,
        };
        ozet.FaturaKesilmeyen = ozet.ToplamKayit - ozet.FaturaKesilen;
        ozet.NetGelir = ozet.ToplamGelir - ozet.ToplamKdv - ozet.ToplamKesinti;
        ozet.NetGider = ozet.ToplamGider;
        ozet.KarZarar = ozet.NetGelir - ozet.NetGider;

        return ozet;
    }

    // ══════════════════════════════════════════════
    // SATIRLAR (Sayfalamalı, tek kaynak — PuantajKayit)
    // ══════════════════════════════════════════════

    public async Task<List<PuantajFaturaSatirDto>> GetSatirlarAsync(PuantajFaturaRaporRequest request, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var query = BuildPuantajKayitQuery(db, request);
        query = ApplyYonFilter(query, request);
        query = ApplyAramaFilter(query, request);

        var pageSize = Math.Min(request.PageSize, MaxPageSize);
        var skip = (Math.Max(1, request.Page) - 1) * pageSize;

        var kayitlar = await query
            .Include(x => x.Kurum).Include(x => x.Guzergah).Include(x => x.Arac)
            .Include(x => x.Sofor).Include(x => x.FaturaKesiciCari).Include(x => x.OdemeYapilacakCari)
            .AsNoTracking().OrderBy(x => x.SiraNo).ThenBy(x => x.Id)
            .Skip(skip).Take(pageSize)
            .ToListAsync(ct);

        return kayitlar.Select(MapPuantajKayitToDto).ToList();
    }

    // ══════════════════════════════════════════════
    // AĞAÇ (Hiyerarşik gruplu, tek kaynak — PuantajKayit)
    // ══════════════════════════════════════════════

    public async Task<List<PuantajFaturaAgacNodeDto>> GetAgacAsync(PuantajFaturaRaporRequest request, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var query = BuildPuantajKayitQuery(db, request);
        query = ApplyYonFilter(query, request);
        var kayitlar = await query
            .Include(x => x.Kurum).Include(x => x.FaturaKesiciCari).Include(x => x.OdemeYapilacakCari)
            .Include(x => x.Guzergah).Include(x => x.Arac).Include(x => x.Sofor)
            .AsNoTracking().OrderBy(x => x.SiraNo)
            .ToListAsync(ct);

        var satirlar = kayitlar.Select(MapPuantajKayitToDto).ToList();

        return request.Agac switch
        {
            PuantajFaturaAgacYapisi.KurumAracGuzergah => BuildAgac(satirlar,
                s => s.KurumAdi ?? $"Kurum #{s.KurumId}",
                s => s.Plaka ?? $"Arac #{s.AracId}",
                s => s.GuzergahAdi ?? "-", request.Yon),

            PuantajFaturaAgacYapisi.KurumGuzergahArac => BuildAgac(satirlar,
                s => s.KurumAdi ?? $"Kurum #{s.KurumId}",
                s => s.GuzergahAdi ?? "-",
                s => s.Plaka ?? $"Arac #{s.AracId}", request.Yon),

            PuantajFaturaAgacYapisi.TedarikciAracGuzergah => BuildAgac(satirlar,
                s => s.TedarikciUnvan ?? s.CariUnvan ?? $"Cari #{s.CariId}",
                s => s.Plaka ?? $"Arac #{s.AracId}",
                s => s.GuzergahAdi ?? "-", request.Yon),

            _ => BuildAgac(satirlar,
                s => s.CariUnvan ?? $"Cari #{s.CariId}",
                s => s.Plaka ?? $"Arac #{s.AracId}",
                s => s.GuzergahAdi ?? "-", request.Yon),
        };
    }

    // ══════════════════════════════════════════════
    // COUNT (tek kaynak — PuantajKayit)
    // ══════════════════════════════════════════════

    public async Task<int> GetCountAsync(PuantajFaturaRaporRequest request, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await BuildPuantajKayitQuery(db, request).CountAsync(ct);
    }

    // ══════════════════════════════════════════════
    // QUERY BUILDERS
    // ══════════════════════════════════════════════

    private static IQueryable<PuantajKayit> BuildPuantajKayitQuery(ApplicationDbContext db, PuantajFaturaRaporRequest request)
    {
        var query = db.PuantajKayitlar
            .Where(x => x.Yil == request.Yil && x.Ay == request.Ay && !x.IsDeleted)
            .Where(x => x.OnayDurum == PuantajOnayDurum.Onaylandi);

        if (request.KurumId.HasValue)
            query = query.Where(x => x.KurumId == request.KurumId.Value);
        if (request.CariId.HasValue)
            query = query.Where(x => x.FaturaKesiciCariId == request.CariId.Value || x.OdemeYapilacakCariId == request.CariId.Value);
        if (request.AracId.HasValue)
            query = query.Where(x => x.AracId == request.AracId.Value);
        if (request.GuzergahId.HasValue)
            query = query.Where(x => x.GuzergahId == request.GuzergahId.Value);

        return query;
    }

    // ══════════════════════════════════════════════
    // YÖN / ARAMA FİLTRELERİ
    // ══════════════════════════════════════════════

    private static IQueryable<PuantajKayit> ApplyYonFilter(IQueryable<PuantajKayit> query, PuantajFaturaRaporRequest request)
    {
        return request.Yon switch
        {
            PuantajFaturaYonu.Gelir => query.Where(x => x.ToplamGelir > 0 || x.BirimGelir > 0),
            PuantajFaturaYonu.Gider => query.Where(x => x.ToplamGider > 0 || x.BirimGider > 0),
            _ => query
        };
    }

    private static IQueryable<PuantajKayit> ApplyAramaFilter(IQueryable<PuantajKayit> query, PuantajFaturaRaporRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Arama)) return query;
        var arama = request.Arama.Trim().ToUpperInvariant();
        return query.Where(x =>
            (x.Plaka != null && x.Plaka.ToUpper().Contains(arama)) ||
            (x.SoforAdi != null && x.SoforAdi.ToUpper().Contains(arama)) ||
            (x.KurumAdi != null && x.KurumAdi.ToUpper().Contains(arama)) ||
            (x.FaturaKesiciAdi != null && x.FaturaKesiciAdi.ToUpper().Contains(arama)));
    }

    // ══════════════════════════════════════════════
    // MAPPER: PuantajKayit → DTO
    // ══════════════════════════════════════════════

    private static PuantajFaturaSatirDto MapPuantajKayitToDto(PuantajKayit k)
    {
        return new PuantajFaturaSatirDto
        {
            KayitId = k.Id, Kaynak = "PuantajKayit",
            KurumId = k.KurumId, KurumAdi = k.Kurum?.KurumAdi ?? k.KurumAdi,
            CariId = k.FaturaKesiciCariId ?? k.OdemeYapilacakCariId,
            CariUnvan = k.FaturaKesiciCari?.Unvan ?? k.OdemeYapilacakCari?.Unvan ?? k.FaturaKesiciAdi,
            Telefon = k.FaturaKesiciTelefon ?? k.FaturaKesiciCari?.Telefon ?? k.OdemeYapilacakCari?.Telefon,
            TedarikciId = k.KaynakTipi == PlanlamaKaynakTipi.Tedarikci ? k.OdemeYapilacakCariId : null,
            TedarikciUnvan = k.KaynakTipi == PlanlamaKaynakTipi.Tedarikci ? k.OdemeYapilacakCari?.Unvan ?? k.FaturaKesiciAdi : null,
            AracId = k.AracId, Plaka = k.Arac?.AktifPlaka ?? k.Plaka,
            SoforId = k.SoforId, SoforAdi = k.Sofor != null ? $"{k.Sofor.Ad} {k.Sofor.Soyad}" : k.SoforAdi,
            GuzergahId = k.GuzergahId, GuzergahAdi = k.Guzergah?.GuzergahAdi ?? k.GuzergahAdi,
            SlotAdi = k.SlotAdi, YonTipi = k.Yon.ToString(),
            BirimGelir = k.BirimGelir, ToplamGelir = k.ToplamGelir,
            BirimGider = k.BirimGider, ToplamGider = k.ToplamGider,
            KdvTutar = k.GelirKdvTutari + k.GiderKdv20Tutari + k.GiderKdv10Tutari,
            Kdv10Tutar = k.GiderKdv10Tutari, Kdv20Tutar = k.GiderKdv20Tutari + k.GelirKdvTutari,
            KesintiTutar = k.GelirKesinti + k.GiderKesinti,
            TahsilEdilecek = k.Alinacak, Odenecek = k.Odenecek,
            ToplamSefer = (int)k.Gun, Gun = k.Gun,
            Gun01 = k.GetGunDeger(1), Gun02 = k.GetGunDeger(2), Gun03 = k.GetGunDeger(3),
            Gun04 = k.GetGunDeger(4), Gun05 = k.GetGunDeger(5), Gun06 = k.GetGunDeger(6),
            Gun07 = k.GetGunDeger(7), Gun08 = k.GetGunDeger(8), Gun09 = k.GetGunDeger(9),
            Gun10 = k.GetGunDeger(10), Gun11 = k.GetGunDeger(11), Gun12 = k.GetGunDeger(12),
            Gun13 = k.GetGunDeger(13), Gun14 = k.GetGunDeger(14), Gun15 = k.GetGunDeger(15),
            Gun16 = k.GetGunDeger(16), Gun17 = k.GetGunDeger(17), Gun18 = k.GetGunDeger(18),
            Gun19 = k.GetGunDeger(19), Gun20 = k.GetGunDeger(20), Gun21 = k.GetGunDeger(21),
            Gun22 = k.GetGunDeger(22), Gun23 = k.GetGunDeger(23), Gun24 = k.GetGunDeger(24),
            Gun25 = k.GetGunDeger(25), Gun26 = k.GetGunDeger(26), Gun27 = k.GetGunDeger(27),
            Gun28 = k.GetGunDeger(28), Gun29 = k.GetGunDeger(29), Gun30 = k.GetGunDeger(30),
            Gun31 = k.GetGunDeger(31),
            FaturaKesildi = k.GelirFaturaKesildi || k.GiderFaturaAlindi,
            FaturaNo = k.GelirFaturaNo ?? k.GiderFaturaNo,
            FaturaTarihi = k.GelirFaturaTarihi ?? k.GiderFaturaTarihi,
        };
    }

    // ══════════════════════════════════════════════
    // AĞAÇ BUILDER
    // ══════════════════════════════════════════════

    private static List<PuantajFaturaAgacNodeDto> BuildAgac(
        List<PuantajFaturaSatirDto> satirlar,
        Func<PuantajFaturaSatirDto, string> seviye1Key,
        Func<PuantajFaturaSatirDto, string> seviye2Key,
        Func<PuantajFaturaSatirDto, string> seviye3Key,
        PuantajFaturaYonu yon)
    {
        return satirlar
            .GroupBy(seviye1Key)
            .Select(g1 =>
            {
                var cocuklar = g1
                    .GroupBy(seviye2Key)
                    .Select(g2 =>
                    {
                        var satirList = g2.ToList();
                        return new PuantajFaturaAgacNodeDto
                        {
                            SeviyeAdi = "Seviye2", Etiket = g2.Key,
                            Satirlar = satirList, SatirSayisi = satirList.Count,
                            ToplamSefer = satirList.Sum(s => s.ToplamSefer),
                            ToplamGelir = satirList.Sum(s => s.ToplamGelir),
                            ToplamGider = satirList.Sum(s => s.ToplamGider),
                            ToplamKdv = satirList.Sum(s => s.KdvTutar),
                            ToplamKesinti = satirList.Sum(s => s.KesintiTutar),
                            NetTutar = yon == PuantajFaturaYonu.Gelir
                                ? satirList.Sum(s => s.TahsilEdilecek)
                                : satirList.Sum(s => s.Odenecek),
                        };
                    }).ToList();

                var tumSatirlar = g1.ToList();
                return new PuantajFaturaAgacNodeDto
                {
                    SeviyeAdi = "Seviye1", Etiket = g1.Key,
                    Cocuklar = cocuklar, SatirSayisi = tumSatirlar.Count,
                    ToplamSefer = tumSatirlar.Sum(s => s.ToplamSefer),
                    ToplamGelir = tumSatirlar.Sum(s => s.ToplamGelir),
                    ToplamGider = tumSatirlar.Sum(s => s.ToplamGider),
                    ToplamKdv = tumSatirlar.Sum(s => s.KdvTutar),
                    ToplamKesinti = tumSatirlar.Sum(s => s.KesintiTutar),
                };
            }).ToList();
    }

    // ══════════════════════════════════════════════
    // EXCEL EXPORT
    // ══════════════════════════════════════════════

    public async Task<byte[]> ExportExcelAsync(PuantajFaturaRaporRequest request, CancellationToken ct = default)
    {
        request.Page = 1;
        request.PageSize = MaxPageSize;
        var satirlar = await GetSatirlarAsync(request, ct);
        var ozet = await GetOzetAsync(request, ct);

        using var wb = new XLWorkbook();

        // ── Sayfa 1: Fatura Hazırlık Listesi ──
        var ws1 = wb.Worksheets.Add("Fatura Hazırlık Listesi");
        var headerStyle = wb.Style;
        WriteSheet1(ws1, satirlar, ozet);

        // ── Sayfa 2: Kurum Bazlı Özet ──
        var ws2 = wb.Worksheets.Add("Kurum Bazlı Özet");
        WriteGroupedSheet(ws2, satirlar, s => s.KurumAdi ?? "-", "Kurum");

        // ── Sayfa 3: Araç Bazlı Özet ──
        var ws3 = wb.Worksheets.Add("Araç Bazlı Özet");
        WriteGroupedSheet(ws3, satirlar, s => s.Plaka ?? "-", "Araç");

        // ── Sayfa 4: Güzergah Bazlı Özet ──
        var ws4 = wb.Worksheets.Add("Güzergah Bazlı Özet");
        WriteGroupedSheet(ws4, satirlar, s => s.GuzergahAdi ?? "-", "Güzergah");

        // ── Sayfa 5: Tedarikçi Bazlı Ödenecek ──
        var ws5 = wb.Worksheets.Add("Tedarikçi Bazlı Ödenecek");
        var tedarikciSatirlar = satirlar.Where(s => !string.IsNullOrEmpty(s.TedarikciUnvan) || s.Odenecek > 0).ToList();
        WriteGroupedSheet(ws5, tedarikciSatirlar, s => s.TedarikciUnvan ?? s.CariUnvan ?? "-", "Tedarikçi");

        // ── Sayfa 6: Özet ──
        var ws6 = wb.Worksheets.Add("Genel Özet");
        WriteSummarySheet(ws6, ozet);

        // Kolon genişliklerini otomatik ayarla (ilk 5 sayfa)
        foreach (var ws in new[] { ws1, ws2, ws3, ws4, ws5 })
            ws.Columns().AdjustToContents(1, Math.Min(ws.LastColumnUsed()?.ColumnNumber() ?? 30, 30));

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    private static void WriteSheet1(IXLWorksheet ws, List<PuantajFaturaSatirDto> satirlar, PuantajFaturaOzetDto ozet)
    {
        int row = 1;
        // Başlık
        ws.Cell(row, 1).Value = "Fatura Hazırlık Listesi";
        ws.Range(row, 1, row, 17).Merge().Style.Font.Bold = true;
        row += 2;

        // Özet satırı
        ws.Cell(row, 1).Value = $"Toplam Kayıt: {ozet.ToplamKayit} | Sefer: {ozet.ToplamSefer} | Gelir: {ozet.ToplamGelir:N2} | Gider: {ozet.ToplamGider:N2} | KDV: {ozet.ToplamKdv:N2} | Kesinti: {ozet.ToplamKesinti:N2} | Net: {ozet.NetGelir:N2}";
        ws.Range(row, 1, row, 12).Merge();
        row += 2;

        // Kolon başlıkları
        var headers = new[] { "S.NO", "GÜZERGAH", "GELİR", "GİDER", "YÖN", "PLAKA", "ŞOFÖR", "CARİ", "TELEFON",
            "TOPLAM SEFER", "KDV", "KESİNTİ", "ÖDENECEK/TAHSİL", "KAYNAK", "FATURA NO" };
        for (int c = 0; c < headers.Length; c++)
        {
            ws.Cell(row, c + 1).Value = headers[c];
            ws.Cell(row, c + 1).Style.Font.Bold = true;
            ws.Cell(row, c + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }
        row++;

        // Satırlar
        int sira = 1;
        foreach (var s in satirlar)
        {
            ws.Cell(row, 1).Value = sira++;
            ws.Cell(row, 2).Value = s.GuzergahAdi;
            ws.Cell(row, 3).Value = (double)s.ToplamGelir;
            ws.Cell(row, 4).Value = (double)s.ToplamGider;
            ws.Cell(row, 5).Value = s.YonTipi;
            ws.Cell(row, 6).Value = s.Plaka;
            ws.Cell(row, 7).Value = s.SoforAdi;
            ws.Cell(row, 8).Value = s.CariUnvan;
            ws.Cell(row, 9).Value = s.Telefon;
            ws.Cell(row, 10).Value = s.ToplamSefer;
            ws.Cell(row, 11).Value = (double)s.KdvTutar;
            ws.Cell(row, 12).Value = (double)s.KesintiTutar;
            ws.Cell(row, 13).Value = (double)(s.TahsilEdilecek > 0 ? s.TahsilEdilecek : s.Odenecek);
            ws.Cell(row, 14).Value = s.Kaynak;
            ws.Cell(row, 15).Value = s.FaturaNo;

            if (s.FaturaKesildi)
                ws.Row(row).Style.Fill.BackgroundColor = XLColor.LightGreen;
            row++;
        }

        // Sayı formatları
        ws.Column(3).Style.NumberFormat.Format = "#,##0.00";
        ws.Column(4).Style.NumberFormat.Format = "#,##0.00";
        ws.Column(11).Style.NumberFormat.Format = "#,##0.00";
        ws.Column(12).Style.NumberFormat.Format = "#,##0.00";
        ws.Column(13).Style.NumberFormat.Format = "#,##0.00";
    }

    private static void WriteGroupedSheet(IXLWorksheet ws, List<PuantajFaturaSatirDto> satirlar,
        Func<PuantajFaturaSatirDto, string> keySelector, string groupName)
    {
        int row = 1;
        ws.Cell(row, 1).Value = $"{groupName} Bazlı Özet";
        ws.Range(row, 1, row, 8).Merge().Style.Font.Bold = true;
        row += 2;

        var headers = new[] { groupName, "TOPLAM SEFER", "GELİR", "GİDER", "KDV", "KESİNTİ", "NET", "KAYIT SAYISI" };
        for (int c = 0; c < headers.Length; c++)
        {
            ws.Cell(row, c + 1).Value = headers[c];
            ws.Cell(row, c + 1).Style.Font.Bold = true;
            ws.Cell(row, c + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }
        row++;

        foreach (var g in satirlar.GroupBy(keySelector).OrderByDescending(g => g.Sum(s => s.ToplamGelir)))
        {
            var list = g.ToList();
            ws.Cell(row, 1).Value = g.Key;
            ws.Cell(row, 2).Value = list.Sum(s => s.ToplamSefer);
            ws.Cell(row, 3).Value = (double)list.Sum(s => s.ToplamGelir);
            ws.Cell(row, 4).Value = (double)list.Sum(s => s.ToplamGider);
            ws.Cell(row, 5).Value = (double)list.Sum(s => s.KdvTutar);
            ws.Cell(row, 6).Value = (double)list.Sum(s => s.KesintiTutar);
            ws.Cell(row, 7).Value = (double)list.Sum(s => s.TahsilEdilecek > 0 ? s.TahsilEdilecek : s.Odenecek);
            ws.Cell(row, 8).Value = list.Count;
            row++;
        }

        ws.Column(3).Style.NumberFormat.Format = "#,##0.00";
        ws.Column(4).Style.NumberFormat.Format = "#,##0.00";
        ws.Column(5).Style.NumberFormat.Format = "#,##0.00";
        ws.Column(6).Style.NumberFormat.Format = "#,##0.00";
        ws.Column(7).Style.NumberFormat.Format = "#,##0.00";
    }

    private static void WriteSummarySheet(IXLWorksheet ws, PuantajFaturaOzetDto ozet)
    {
        int row = 1;
        ws.Cell(row, 1).Value = "Genel Özet";
        ws.Range(row, 1, row, 2).Merge().Style.Font.Bold = true;
        row += 2;

        var rows = new Dictionary<string, object>
        {
            ["Toplam Kayıt"] = ozet.ToplamKayit,
            ["Fatura Kesilen"] = ozet.FaturaKesilen,
            ["Fatura Kesilmeyen"] = ozet.FaturaKesilmeyen,
            ["Toplam Sefer"] = ozet.ToplamSefer,
            ["Toplam Gelir"] = ozet.ToplamGelir.ToString("N2"),
            ["Toplam Gider"] = ozet.ToplamGider.ToString("N2"),
            ["Toplam KDV"] = ozet.ToplamKdv.ToString("N2"),
            ["Toplam Kesinti"] = ozet.ToplamKesinti.ToString("N2"),
            ["Net Gelir"] = ozet.NetGelir.ToString("N2"),
            ["Net Gider"] = ozet.NetGider.ToString("N2"),
            ["Kar / Zarar"] = ozet.KarZarar.ToString("N2"),
        };

        foreach (var kv in rows)
        {
            ws.Cell(row, 1).Value = kv.Key;
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 2).Value = kv.Value.ToString();
            if (kv.Key == "Kar / Zarar" && ozet.KarZarar < 0)
                ws.Cell(row, 2).Style.Font.FontColor = XLColor.Red;
            row++;
        }
    }
}
