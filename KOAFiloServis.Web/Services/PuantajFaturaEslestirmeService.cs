using KOAFiloServis.Shared.Entities;
using KOAFiloServis.Web.Data;
using KOAFiloServis.Web.Models;
using KOAFiloServis.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KOAFiloServis.Web.Services;

/// <summary>
/// PuantajKayit (B1) ↔ Fatura eşleştirme ve fark raporu servisi.
/// Otomatik eşleşme: Cari + Yön + Dönem + Tutar.
/// Manuel eşleştirme: Kullanıcı bağlar.
/// Fark raporu: Eşleşmeyen/farklı kayıtları listeler.
/// </summary>
public class PuantajFaturaEslestirmeService : IPuantajFaturaEslestirmeService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private const decimal TamEslesmeEsik = 0.01m;  // %1
    private const decimal YakinEslesmeEsik = 0.05m; // %5

    public PuantajFaturaEslestirmeService(IDbContextFactory<ApplicationDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<PuantajFaturaEslesmeRaporu> EslesmeAnaliziYapAsync(int yil, int ay, int? kurumId = null, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var ilkGun = new DateTime(yil, ay, 1);
        var sonGun = ilkGun.AddMonths(1).AddDays(-1);

        // ── PuantajKayit (B1) ──
        var pkQuery = db.PuantajKayitlar
            .Where(x => x.Yil == yil && x.Ay == ay && x.OnayDurum == PuantajOnayDurum.Onaylandi && !x.IsDeleted);
        if (kurumId.HasValue)
            pkQuery = pkQuery.Where(x => x.KurumId == kurumId.Value);

        var pkList = await pkQuery.Include(x => x.Kurum).Include(x => x.FaturaKesiciCari)
            .Include(x => x.OdemeYapilacakCari).AsNoTracking().ToListAsync(ct);

        // ── PuantajIstisna ──
        var pkIds = pkList.Select(p => p.Id).ToList();
        var istisnalar = await db.PuantajIstisnalar
            .Where(i => pkIds.Contains(i.PuantajKayitId) && !i.IsDeleted)
            .AsNoTracking().ToListAsync(ct);
        var istisnaLookup = istisnalar.GroupBy(i => i.PuantajKayitId).ToDictionary(g => g.Key, g => g.ToList());

        // ── Fatura ──
        var faturaQuery = db.Faturalar
            .Where(x => x.FaturaTarihi >= ilkGun && x.FaturaTarihi <= sonGun && !x.IsDeleted && x.Durum != FaturaDurum.IptalEdildi);
        if (kurumId.HasValue)
            faturaQuery = faturaQuery.Where(x => x.FaturaKalemleri.Any());

        var faturaList = await faturaQuery.Include(x => x.Cari).AsNoTracking().ToListAsync(ct);

        // ── Eşleştirme ──
        var rapor = new PuantajFaturaEslesmeRaporu { Yil = yil, Ay = ay, ToplamPuantajKayit = pkList.Count, ToplamFatura = faturaList.Count };
        var eslesenFaturaIds = new HashSet<int>();

        foreach (var pk in pkList)
        {
            // Zaten eşleşmiş mi? (GelirFaturaId veya GiderFaturaId)
            if (pk.GelirFaturaId.HasValue || pk.GiderFaturaId.HasValue)
            {
                rapor.ManuelEslesen++;
                if (pk.GelirFaturaId.HasValue) eslesenFaturaIds.Add(pk.GelirFaturaId.Value);
                if (pk.GiderFaturaId.HasValue) eslesenFaturaIds.Add(pk.GiderFaturaId.Value);
                continue;
            }

            // Eşleşme ara
            var pkCariId = pk.FaturaKesiciCariId ?? pk.OdemeYapilacakCariId;
            var pkTutar = pk.Alinacak > 0 ? pk.Alinacak : pk.Odenecek;

            if (pkCariId == null || pkTutar == 0)
            {
                rapor.EslesmeyenPuantaj++;
                rapor.Farklar.Add(new PuantajFaturaFarkDto { PuantajKayitId = pk.Id, FarkTipi = PuantajFaturaFarkTipi.PuantajVarFaturaYok, FarkAciklamasi = "Puantajda cari veya tutar yok", PKPlaka = pk.Arac?.AktifPlaka ?? pk.Plaka, PKGuzergah = pk.Guzergah?.GuzergahAdi, PKCari = pk.FaturaKesiciCari?.Unvan, PKTutar = pkTutar });
                continue;
            }

            // Aynı cari + aynı yöndeki faturaları ara
            var pkYon = pk.ToplamGelir > 0 ? FaturaYonu.Giden : FaturaYonu.Gelen;
            var adayFaturalar = faturaList
                .Where(f => f.CariId == pkCariId && f.FaturaYonu == pkYon && !eslesenFaturaIds.Contains(f.Id))
                .ToList();

            if (!adayFaturalar.Any())
            {
                rapor.EslesmeyenPuantaj++;
                rapor.Farklar.Add(new PuantajFaturaFarkDto { PuantajKayitId = pk.Id, FarkTipi = PuantajFaturaFarkTipi.PuantajVarFaturaYok, FarkAciklamasi = "Eşleşen fatura bulunamadı", PKPlaka = pk.Arac?.AktifPlaka ?? pk.Plaka, PKGuzergah = pk.Guzergah?.GuzergahAdi, PKCari = pk.FaturaKesiciCari?.Unvan ?? pk.OdemeYapilacakCari?.Unvan, PKTutar = pkTutar, PKKdv = pk.GelirKdvTutari + pk.GelirKdv20Tutari + pk.GelirKdv10Tutari + pk.GiderKdv20Tutari + pk.GiderKdv10Tutari, PKKesinti = pk.GelirKesinti + pk.GiderKesinti, PKSefer = (int)pk.Gun });
                continue;
            }

            // En yakın tutarlı faturayı bul
            Fatura? enYakin = null;
            decimal enYakinFark = decimal.MaxValue;
            foreach (var f in adayFaturalar)
            {
                var fark = Math.Abs(pkTutar - f.GenelToplam);
                if (fark < enYakinFark)
                {
                    enYakinFark = fark;
                    enYakin = f;
                }
            }

            if (enYakin == null)
            {
                rapor.EslesmeyenPuantaj++;
                continue;
            }

            var farkYuzde = pkTutar > 0 ? enYakinFark / pkTutar : 1;

            if (farkYuzde <= TamEslesmeEsik)
            {
                // KDV ve kesinti farkı kontrolü
                var pkKdv = pk.GelirKdvTutari + pk.GelirKdv20Tutari + pk.GelirKdv10Tutari + pk.GiderKdv20Tutari + pk.GiderKdv10Tutari;
                var kdvFark = Math.Abs(pkKdv - enYakin.KdvTutar);
                var kdvFarkYuzde = pkKdv > 0 ? kdvFark / pkKdv : 0;
                var kesintiFark = Math.Abs((pk.GelirKesinti + pk.GiderKesinti) - 0); // Fatura'da kesinti alanı yok

                var farkTipi = PuantajFaturaFarkTipi.TamEslesen;
                var farkAciklama = "Tam eşleşme";
                if (kdvFarkYuzde > YakinEslesmeEsik) { farkTipi = PuantajFaturaFarkTipi.KdvFarki; farkAciklama = $"KDV farkı — PK:{pkKdv:N2} FT:{enYakin.KdvTutar:N2}"; }
                else if (kesintiFark > 10) { farkTipi = PuantajFaturaFarkTipi.KesintiFarki; farkAciklama = $"Kesinti farkı — PK:{pk.GelirKesinti + pk.GiderKesinti:N2}"; }

                rapor.TamEslesen++;
                eslesenFaturaIds.Add(enYakin.Id);
                rapor.Farklar.Add(new PuantajFaturaFarkDto { PuantajKayitId = pk.Id, FaturaId = enYakin.Id, FarkTipi = farkTipi, FarkAciklamasi = farkAciklama, PKPlaka = pk.Arac?.AktifPlaka ?? pk.Plaka, PKGuzergah = pk.Guzergah?.GuzergahAdi, PKCari = pk.FaturaKesiciCari?.Unvan, PKTutar = pkTutar, PKKdv = pk.GelirKdvTutari + pk.GelirKdv20Tutari + pk.GelirKdv10Tutari + pk.GiderKdv20Tutari + pk.GiderKdv10Tutari, PKKesinti = pk.GelirKesinti + pk.GiderKesinti, PKSefer = (int)pk.Gun, FaturaNo = enYakin.FaturaNo, FaturaTarihi = enYakin.FaturaTarihi, FCari = enYakin.Cari?.Unvan, FTutar = enYakin.GenelToplam, FKdv = enYakin.KdvTutar, FarkTutar = enYakinFark, FarkYuzde = farkYuzde * 100 });
            }
            else if (farkYuzde <= YakinEslesmeEsik)
            {
                rapor.YakinEslesen++;
                eslesenFaturaIds.Add(enYakin.Id);
                rapor.Farklar.Add(new PuantajFaturaFarkDto { PuantajKayitId = pk.Id, FaturaId = enYakin.Id, FarkTipi = PuantajFaturaFarkTipi.TutarFarki, FarkAciklamasi = $"Yakın eşleşme — fark %{farkYuzde * 100:N1}", PKPlaka = pk.Arac?.AktifPlaka ?? pk.Plaka, PKGuzergah = pk.Guzergah?.GuzergahAdi, PKCari = pk.FaturaKesiciCari?.Unvan, PKTutar = pkTutar, PKKdv = pk.GelirKdvTutari + pk.GelirKdv20Tutari + pk.GelirKdv10Tutari, PKKesinti = pk.GelirKesinti + pk.GiderKesinti, PKSefer = (int)pk.Gun, FaturaNo = enYakin.FaturaNo, FaturaTarihi = enYakin.FaturaTarihi, FCari = enYakin.Cari?.Unvan, FTutar = enYakin.GenelToplam, FKdv = enYakin.KdvTutar, FarkTutar = enYakinFark, FarkYuzde = farkYuzde * 100 });
            }
            else
            {
                rapor.EslesmeyenPuantaj++;
                rapor.Farklar.Add(new PuantajFaturaFarkDto { PuantajKayitId = pk.Id, FaturaId = enYakin.Id, FarkTipi = PuantajFaturaFarkTipi.TutarFarki, FarkAciklamasi = $"Tutar farkı çok yüksek — fark %{farkYuzde * 100:N1}", PKPlaka = pk.Arac?.AktifPlaka ?? pk.Plaka, PKGuzergah = pk.Guzergah?.GuzergahAdi, PKCari = pk.FaturaKesiciCari?.Unvan, PKTutar = pkTutar, PKKdv = pk.GelirKdvTutari + pk.GelirKdv20Tutari + pk.GelirKdv10Tutari, PKKesinti = pk.GelirKesinti + pk.GiderKesinti, PKSefer = (int)pk.Gun, FaturaNo = enYakin.FaturaNo, FaturaTarihi = enYakin.FaturaTarihi, FCari = enYakin.Cari?.Unvan, FTutar = enYakin.GenelToplam, FKdv = enYakin.KdvTutar, FarkTutar = enYakinFark, FarkYuzde = farkYuzde * 100 });
            }
        }

        // Eşleşmeyen faturalar
        rapor.EslesmeyenFatura = faturaList.Count(f => !eslesenFaturaIds.Contains(f.Id));
        foreach (var f in faturaList.Where(f => !eslesenFaturaIds.Contains(f.Id)))
        {
            rapor.Farklar.Add(new PuantajFaturaFarkDto { FaturaId = f.Id, FarkTipi = PuantajFaturaFarkTipi.FaturaVarPuantajYok, FarkAciklamasi = "Faturanın puantaj karşılığı bulunamadı", FaturaNo = f.FaturaNo, FaturaTarihi = f.FaturaTarihi, FCari = f.Cari?.Unvan, FTutar = f.GenelToplam, FKdv = f.KdvTutar });
        }

        // ── İstisna bilgilerini FarkDto'lara ekle ──
        foreach (var fark in rapor.Farklar)
        {
            if (fark.PuantajKayitId.HasValue && istisnaLookup.TryGetValue(fark.PuantajKayitId.Value, out var pkIstisnalar))
            {
                fark.IstisnaSayisi = pkIstisnalar.Count;
                fark.CezaTutar = pkIstisnalar.Where(i => i.KararTipi == KararTipi.Ceza).Sum(i => i.Tutar);
                fark.MasrafTutar = pkIstisnalar.Where(i => i.KararTipi == KararTipi.Masraf).Sum(i => i.Tutar);
                fark.IstisnaOzeti = string.Join("; ", pkIstisnalar.Select(i => $"{i.IstisnaTipi}:{i.KararTipi}"));
            }
        }

        return rapor;
    }

    public async Task<bool> ManuelEslestirAsync(int puantajKayitId, int faturaId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var pk = await db.PuantajKayitlar.Where(x => x.Id == puantajKayitId && !x.IsDeleted).FirstOrDefaultAsync(ct);
        if (pk == null) return false;

        var fatura = await db.Faturalar.Where(x => x.Id == faturaId && !x.IsDeleted).FirstOrDefaultAsync(ct);
        if (fatura == null) return false;

        // Yönüne göre bağla
        if (fatura.FaturaYonu == FaturaYonu.Giden)
        {
            pk.GelirFaturaId = faturaId;
            pk.GelirFaturaKesildi = true;
            pk.GelirFaturaNo = fatura.FaturaNo;
            pk.GelirFaturaTarihi = fatura.FaturaTarihi;
        }
        else
        {
            pk.GiderFaturaId = faturaId;
            pk.GiderFaturaAlindi = true;
            pk.GiderFaturaNo = fatura.FaturaNo;
            pk.GiderFaturaTarihi = fatura.FaturaTarihi;
        }

        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> EslesmeKaldirAsync(int puantajKayitId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var pk = await db.PuantajKayitlar.Where(x => x.Id == puantajKayitId && !x.IsDeleted).FirstOrDefaultAsync(ct);
        if (pk == null) return false;

        pk.GelirFaturaId = null;
        pk.GelirFaturaKesildi = false;
        pk.GelirFaturaNo = null;
        pk.GelirFaturaTarihi = null;
        pk.GiderFaturaId = null;
        pk.GiderFaturaAlindi = false;
        pk.GiderFaturaNo = null;
        pk.GiderFaturaTarihi = null;

        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<List<PuantajFaturaFarkDto>> FarkRaporuGetirAsync(int yil, int ay, int? kurumId = null, CancellationToken ct = default)
    {
        var rapor = await EslesmeAnaliziYapAsync(yil, ay, kurumId, ct);
        return rapor.Farklar
            .Where(f => f.FarkTipi != PuantajFaturaFarkTipi.TamEslesen)
            .OrderBy(f => f.FarkTipi)
            .ThenByDescending(f => f.FarkTutar)
            .ToList();
    }

    public async Task<PuantajFaturaEslesmeRaporu> TopluOtoEslestirAsync(int yil, int ay, int? kurumId = null, CancellationToken ct = default)
    {
        var rapor = await EslesmeAnaliziYapAsync(yil, ay, kurumId, ct);

        // Tam eşleşmeleri otomatik kaydet
        var tamEslesmeler = rapor.Farklar
            .Where(f => f.FarkTipi == PuantajFaturaFarkTipi.TamEslesen
                && f.PuantajKayitId.HasValue && f.FaturaId.HasValue)
            .ToList();

        if (tamEslesmeler.Any())
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            foreach (var f in tamEslesmeler)
            {
                var pk = await db.PuantajKayitlar
                    .Where(x => x.Id == f.PuantajKayitId!.Value && !x.IsDeleted)
                    .FirstOrDefaultAsync(ct);
                var fatura = await db.Faturalar
                    .Where(x => x.Id == f.FaturaId!.Value && !x.IsDeleted)
                    .FirstOrDefaultAsync(ct);

                if (pk == null || fatura == null) continue;

                if (fatura.FaturaYonu == FaturaYonu.Giden)
                {
                    pk.GelirFaturaId = fatura.Id;
                    pk.GelirFaturaKesildi = true;
                    pk.GelirFaturaNo = fatura.FaturaNo;
                    pk.GelirFaturaTarihi = fatura.FaturaTarihi;
                }
                else
                {
                    pk.GiderFaturaId = fatura.Id;
                    pk.GiderFaturaAlindi = true;
                    pk.GiderFaturaNo = fatura.FaturaNo;
                    pk.GiderFaturaTarihi = fatura.FaturaTarihi;
                }
            }
            await db.SaveChangesAsync(ct);
        }

        return rapor;
    }

    public async Task<byte[]> ExportFarkRaporuExcelAsync(int yil, int ay, int? kurumId = null, CancellationToken ct = default)
    {
        var farklar = await FarkRaporuGetirAsync(yil, ay, kurumId, ct);

        using var wb = new ClosedXML.Excel.XLWorkbook();
        var ws = wb.Worksheets.Add("Fark Raporu");

        // Başlık
        ws.Cell(1, 1).Value = $"Puantaj ↔ Fatura Fark Raporu — {yil}/{ay:00}";
        ws.Range(1, 1, 1, 15).Merge().Style.Font.Bold = true;

        // Kolon başlıkları
        var headers = new[] { "DURUM", "AÇIKLAMA", "PLAKA", "GÜZERGAH", "PUANTAJ CARİ",
            "PUANTAJ TUTAR", "PUANTAJ KDV", "PUANTAJ SEFER", "FATURA NO", "FATURA TUTAR", "FARK %",
            "İSTİSNA SAYISI", "CEZA TUTAR", "MASRAF TUTAR", "İSTİSNA ÖZETİ" };
        for (int c = 0; c < headers.Length; c++)
        {
            ws.Cell(3, c + 1).Value = headers[c];
            ws.Cell(3, c + 1).Style.Font.Bold = true;
            ws.Cell(3, c + 1).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;
        }

        int row = 4;
        foreach (var f in farklar)
        {
            ws.Cell(row, 1).Value = f.FarkTipi.ToString();
            ws.Cell(row, 2).Value = f.FarkAciklamasi;
            ws.Cell(row, 3).Value = f.PKPlaka;
            ws.Cell(row, 4).Value = f.PKGuzergah;
            ws.Cell(row, 5).Value = f.PKCari;
            ws.Cell(row, 6).Value = (double)f.PKTutar;
            ws.Cell(row, 7).Value = (double)f.PKKdv;
            ws.Cell(row, 8).Value = f.PKSefer;
            ws.Cell(row, 9).Value = f.FaturaNo;
            ws.Cell(row, 10).Value = (double)f.FTutar;
            ws.Cell(row, 11).Value = (double)f.FarkYuzde;
            ws.Cell(row, 12).Value = f.IstisnaSayisi;
            ws.Cell(row, 13).Value = (double)f.CezaTutar;
            ws.Cell(row, 14).Value = (double)f.MasrafTutar;
            ws.Cell(row, 15).Value = f.IstisnaOzeti;

            if (f.FarkTipi == PuantajFaturaFarkTipi.PuantajVarFaturaYok)
                ws.Row(row).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightYellow;
            else if (f.FarkTipi == PuantajFaturaFarkTipi.FaturaVarPuantajYok)
                ws.Row(row).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightSalmon;

            row++;
        }

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }
}
