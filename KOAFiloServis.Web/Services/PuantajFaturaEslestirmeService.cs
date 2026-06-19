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
                rapor.TamEslesen++;
                eslesenFaturaIds.Add(enYakin.Id);
                // Otomatik bağla (opsiyonel — sadece raporlama)
                rapor.Farklar.Add(new PuantajFaturaFarkDto { PuantajKayitId = pk.Id, FaturaId = enYakin.Id, FarkTipi = PuantajFaturaFarkTipi.TamEslesen, FarkAciklamasi = "Tam eşleşme", PKPlaka = pk.Arac?.AktifPlaka ?? pk.Plaka, PKGuzergah = pk.Guzergah?.GuzergahAdi, PKCari = pk.FaturaKesiciCari?.Unvan, PKTutar = pkTutar, PKKdv = pk.GelirKdvTutari + pk.GelirKdv20Tutari + pk.GelirKdv10Tutari, PKKesinti = pk.GelirKesinti + pk.GiderKesinti, PKSefer = (int)pk.Gun, FaturaNo = enYakin.FaturaNo, FaturaTarihi = enYakin.FaturaTarihi, FCari = enYakin.Cari?.Unvan, FTutar = enYakin.GenelToplam, FKdv = enYakin.KdvTutar, FarkTutar = enYakinFark, FarkYuzde = farkYuzde * 100 });
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
}
