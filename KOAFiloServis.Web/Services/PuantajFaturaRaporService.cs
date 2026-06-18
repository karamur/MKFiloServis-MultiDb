using KOAFiloServis.Shared.Entities;
using KOAFiloServis.Web.Data;
using KOAFiloServis.Web.Models;
using KOAFiloServis.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KOAFiloServis.Web.Services;

/// <summary>
/// Puantaj sonrası fatura hazırlık için READONLY rapor servisi.
/// PRIMARY kaynak: PuantajKayit (KurumId, FaturaKesiciCariId, Gun01-31 direkt)
/// SECONDARY kaynak: HakedisPuantaj (Guzergah uzerinden KurumId, GunlukSeferSayisi)
/// İki kaynak merge edilir. Hiçbir kayıt oluşturmaz, güncellemez, silmez.
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

        var pkOzet = await BuildPuantajKayitQuery(db, request)
            .GroupBy(_ => 1)
            .Select(g => new { Count = g.Count(), Gelir = g.Sum(x => x.ToplamGelir), Gider = g.Sum(x => x.ToplamGider), Kdv = g.Sum(x => x.GelirKdvTutari + x.GiderKdv20Tutari + x.GiderKdv10Tutari), Kesinti = g.Sum(x => x.GelirKesinti + x.GiderKesinti), Sefer = (int)g.Sum(x => x.Gun), FaturaKesilen = g.Count(x => x.GelirFaturaKesildi || x.GiderFaturaAlindi) })
            .FirstOrDefaultAsync(ct);

        var hpOzet = await BuildHakedisPuantajQuery(db, request)
            .GroupBy(_ => 1)
            .Select(g => new { Count = g.Count(), Gelir = g.Sum(x => x.GelirToplam), Gider = g.Sum(x => x.GiderToplam), Kdv = g.Sum(x => x.GelirKdvTutari + x.KdvTutari), Kesinti = g.Sum(x => x.ToplamKesinti), Sefer = g.Sum(x => x.ToplamSefer), FaturaKesilen = g.Count(x => x.Durum == HakedisDurumu.Faturalasti || x.Durum == HakedisDurumu.Odendi) })
            .FirstOrDefaultAsync(ct);

        var ozet = new PuantajFaturaOzetDto
        {
            ToplamKayit = (pkOzet?.Count ?? 0) + (hpOzet?.Count ?? 0),
            FaturaKesilen = (pkOzet?.FaturaKesilen ?? 0) + (hpOzet?.FaturaKesilen ?? 0),
            ToplamSefer = (pkOzet?.Sefer ?? 0) + (hpOzet?.Sefer ?? 0),
            ToplamGelir = (pkOzet?.Gelir ?? 0) + (hpOzet?.Gelir ?? 0),
            ToplamGider = (pkOzet?.Gider ?? 0) + (hpOzet?.Gider ?? 0),
            ToplamKdv = (pkOzet?.Kdv ?? 0) + (hpOzet?.Kdv ?? 0),
            ToplamKesinti = (pkOzet?.Kesinti ?? 0) + (hpOzet?.Kesinti ?? 0),
        };
        ozet.FaturaKesilmeyen = ozet.ToplamKayit - ozet.FaturaKesilen;
        ozet.NetGelir = ozet.ToplamGelir - ozet.ToplamKdv - ozet.ToplamKesinti;
        ozet.NetGider = ozet.ToplamGider;
        ozet.KarZarar = ozet.NetGelir - ozet.NetGider;

        return ozet;
    }

    // ══════════════════════════════════════════════
    // SATIRLAR (Sayfalamalı, iki kaynak merge)
    // ══════════════════════════════════════════════

    public async Task<List<PuantajFaturaSatirDto>> GetSatirlarAsync(PuantajFaturaRaporRequest request, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        // PuantajKayit kaynaklı satırlar
        var pkQuery = BuildPuantajKayitQuery(db, request);
        pkQuery = ApplyYonFilter(pkQuery, request);
        pkQuery = ApplyAramaFilter(pkQuery, request);

        // HakedisPuantaj kaynaklı satırlar
        var hpQuery = BuildHakedisPuantajQuery(db, request);
        hpQuery = ApplyYonFilterHp(hpQuery, request);
        // HakedisPuantaj'da arama: GuzergahAdi veya Plaka uzerinden
        if (!string.IsNullOrWhiteSpace(request.Arama))
        {
            var arama = request.Arama.Trim().ToUpperInvariant();
            hpQuery = hpQuery.Where(x =>
                (x.Arac != null && x.Arac.AktifPlaka != null && x.Arac.AktifPlaka.ToUpper().Contains(arama)) ||
                (x.Sofor != null && (x.Sofor.Ad + " " + x.Sofor.Soyad).ToUpper().Contains(arama)) ||
                (x.Guzergah != null && x.Guzergah.GuzergahAdi.ToUpper().Contains(arama)));
        }

        var pageSize = Math.Min(request.PageSize, MaxPageSize);
        var skip = (Math.Max(1, request.Page) - 1) * pageSize;

        // İki kaynaktan veri çek
        var pkKayitlar = await pkQuery
            .Include(x => x.Kurum).Include(x => x.Guzergah).Include(x => x.Arac)
            .Include(x => x.Sofor).Include(x => x.FaturaKesiciCari).Include(x => x.OdemeYapilacakCari)
            .AsNoTracking().OrderBy(x => x.SiraNo).ThenBy(x => x.Id)
            .ToListAsync(ct);

        var hpKayitlar = await hpQuery
            .Include(x => x.Guzergah).ThenInclude(g => g!.Kurum)
            .Include(x => x.Arac).Include(x => x.Sofor).Include(x => x.Cari)
            .Include(x => x.Detaylar)
            .AsNoTracking().OrderBy(x => x.Id)
            .ToListAsync(ct);

        // Merge + sayfalama
        var merged = pkKayitlar.Select(MapPuantajKayitToDto)
            .Concat(hpKayitlar.Select(MapHakedisPuantajToDto))
            .OrderBy(x => x.KurumAdi).ThenBy(x => x.Plaka).ThenBy(x => x.GuzergahAdi)
            .Skip(skip).Take(pageSize)
            .ToList();

        return merged;
    }

    // ══════════════════════════════════════════════
    // AĞAÇ (Hiyerarşik gruplu, iki kaynak merge)
    // ══════════════════════════════════════════════

    public async Task<List<PuantajFaturaAgacNodeDto>> GetAgacAsync(PuantajFaturaRaporRequest request, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        // PuantajKayit
        var pkQuery = BuildPuantajKayitQuery(db, request);
        pkQuery = ApplyYonFilter(pkQuery, request);
        var pkKayitlar = await pkQuery
            .Include(x => x.Kurum).Include(x => x.FaturaKesiciCari).Include(x => x.OdemeYapilacakCari)
            .Include(x => x.Guzergah).Include(x => x.Arac).Include(x => x.Sofor)
            .AsNoTracking().OrderBy(x => x.SiraNo)
            .ToListAsync(ct);

        // HakedisPuantaj
        var hpQuery = BuildHakedisPuantajQuery(db, request);
        hpQuery = ApplyYonFilterHp(hpQuery, request);
        var hpKayitlar = await hpQuery
            .Include(x => x.Guzergah).ThenInclude(g => g!.Kurum)
            .Include(x => x.Arac).Include(x => x.Sofor).Include(x => x.Cari)
            .AsNoTracking().OrderBy(x => x.Id)
            .ToListAsync(ct);

        var satirlar = pkKayitlar.Select(MapPuantajKayitToDto)
            .Concat(hpKayitlar.Select(MapHakedisPuantajToDto))
            .ToList();

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
    // COUNT (iki kaynak)
    // ══════════════════════════════════════════════

    public async Task<int> GetCountAsync(PuantajFaturaRaporRequest request, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var pkCount = await BuildPuantajKayitQuery(db, request).CountAsync(ct);
        var hpCount = await BuildHakedisPuantajQuery(db, request).CountAsync(ct);
        return pkCount + hpCount;
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

    private static IQueryable<HakedisPuantaj> BuildHakedisPuantajQuery(ApplicationDbContext db, PuantajFaturaRaporRequest request)
    {
        var query = db.HakedisPuantajlar
            .Where(x => x.Yil == request.Yil && x.Ay == request.Ay && !x.IsDeleted)
            .Where(x => x.Durum == HakedisDurumu.Onaylandi || x.Durum == HakedisDurumu.Faturalasti || x.Durum == HakedisDurumu.Odendi);

        if (request.KurumId.HasValue)
            query = query.Where(x => x.Guzergah != null && x.Guzergah.KurumId == request.KurumId.Value);
        if (request.CariId.HasValue)
            query = query.Where(x => x.CariId == request.CariId.Value);
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

    private static IQueryable<HakedisPuantaj> ApplyYonFilterHp(IQueryable<HakedisPuantaj> query, PuantajFaturaRaporRequest request)
    {
        return request.Yon switch
        {
            PuantajFaturaYonu.Gelir => query.Where(x => x.GelirToplam > 0 || x.GelirBirimFiyat > 0),
            PuantajFaturaYonu.Gider => query.Where(x => x.GiderToplam > 0 || x.GiderBirimFiyat > 0),
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
    // MAPPER: HakedisPuantaj → DTO
    // ══════════════════════════════════════════════

    private static PuantajFaturaSatirDto MapHakedisPuantajToDto(HakedisPuantaj h)
    {
        // Günlük değerleri Detaylar'dan al
        var gunDegerleri = new int[31];
        if (h.Detaylar != null)
        {
            foreach (var detay in h.Detaylar)
            {
                if (detay.Gun >= 1 && detay.Gun <= 31)
                    gunDegerleri[detay.Gun - 1] = detay.SeferSayisi;
            }
        }

        var kdvTutar = h.GelirKdvTutari + h.KdvTutari;
        var faturaKesildi = h.Durum == HakedisDurumu.Faturalasti || h.Durum == HakedisDurumu.Odendi;

        return new PuantajFaturaSatirDto
        {
            KayitId = h.Id, Kaynak = "HakedisPuantaj",
            KurumId = h.Guzergah?.KurumId,
            KurumAdi = h.Guzergah?.Kurum?.KurumAdi,
            CariId = h.CariId,
            CariUnvan = h.Cari?.Unvan,
            Telefon = h.Cari?.Telefon,
            // HakedisPuantaj'da KaynakTipi yok — CariTipi'ne bakılır
            TedarikciId = h.Cari?.CariTipi == CariTipi.Tedarikci || h.Cari?.CariTipi == CariTipi.MusteriTedarikci ? h.CariId : null,
            TedarikciUnvan = h.Cari?.CariTipi == CariTipi.Tedarikci || h.Cari?.CariTipi == CariTipi.MusteriTedarikci ? h.Cari?.Unvan : null,
            AracId = h.AracId, Plaka = h.Arac?.AktifPlaka,
            SoforId = h.SoforId,
            SoforAdi = h.Sofor != null ? $"{h.Sofor.Ad} {h.Sofor.Soyad}" : null,
            GuzergahId = h.GuzergahId, GuzergahAdi = h.Guzergah?.GuzergahAdi,
            YonTipi = h.YonTipi.ToString(),
            BirimGelir = h.GelirBirimFiyat, ToplamGelir = h.GelirToplam,
            BirimGider = h.GiderBirimFiyat, ToplamGider = h.GiderToplam,
            KdvTutar = kdvTutar,
            // HakedisPuantaj'da KDV/10 ve KDV/20 ayrımı yok — tek KdvOrani
            Kdv10Tutar = h.KdvOrani == 10 ? kdvTutar : 0,
            Kdv20Tutar = h.KdvOrani == 20 ? kdvTutar : 0,
            KesintiTutar = h.ToplamKesinti,
            TahsilEdilecek = h.TahsilEdilecekTutar, Odenecek = h.OdenecekTutar,
            ToplamSefer = h.ToplamSefer, Gun = h.ToplamSefer,
            Gun01 = gunDegerleri[0], Gun02 = gunDegerleri[1], Gun03 = gunDegerleri[2],
            Gun04 = gunDegerleri[3], Gun05 = gunDegerleri[4], Gun06 = gunDegerleri[5],
            Gun07 = gunDegerleri[6], Gun08 = gunDegerleri[7], Gun09 = gunDegerleri[8],
            Gun10 = gunDegerleri[9], Gun11 = gunDegerleri[10], Gun12 = gunDegerleri[11],
            Gun13 = gunDegerleri[12], Gun14 = gunDegerleri[13], Gun15 = gunDegerleri[14],
            Gun16 = gunDegerleri[15], Gun17 = gunDegerleri[16], Gun18 = gunDegerleri[17],
            Gun19 = gunDegerleri[18], Gun20 = gunDegerleri[19], Gun21 = gunDegerleri[20],
            Gun22 = gunDegerleri[21], Gun23 = gunDegerleri[22], Gun24 = gunDegerleri[23],
            Gun25 = gunDegerleri[24], Gun26 = gunDegerleri[25], Gun27 = gunDegerleri[26],
            Gun28 = gunDegerleri[27], Gun29 = gunDegerleri[28], Gun30 = gunDegerleri[29],
            Gun31 = gunDegerleri[30],
            FaturaKesildi = faturaKesildi,
            FaturaNo = h.GelirFatura?.FaturaNo ?? h.GiderFatura?.FaturaNo ?? h.Fatura?.FaturaNo,
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
}
