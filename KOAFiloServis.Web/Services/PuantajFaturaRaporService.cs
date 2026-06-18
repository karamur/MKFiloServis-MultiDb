using KOAFiloServis.Shared.Entities;
using KOAFiloServis.Web.Data;
using KOAFiloServis.Web.Models;
using KOAFiloServis.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KOAFiloServis.Web.Services;

/// <summary>
/// Puantaj sonrası fatura hazırlık için READONLY rapor servisi.
/// PuantajKayit tablosunu PRIMARY kaynak olarak kullanır.
/// Hiçbir kayıt oluşturmaz, güncellemez, silmez. Sadece SELECT.
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
        var query = BuildBaseQuery(db, request);

        var gruplu = await query
            .GroupBy(_ => 1)
            .Select(g => new PuantajFaturaOzetDto
            {
                ToplamKayit = g.Count(),
                FaturaKesilen = g.Count(x => x.GelirFaturaKesildi || x.GiderFaturaAlindi),
                ToplamSefer = (int)g.Sum(x => x.Gun),
                ToplamGelir = g.Sum(x => x.ToplamGelir),
                ToplamGider = g.Sum(x => x.ToplamGider),
                ToplamKdv = g.Sum(x => x.GelirKdvTutari + x.GiderKdv20Tutari + x.GiderKdv10Tutari),
                ToplamKesinti = g.Sum(x => x.GelirKesinti + x.GiderKesinti),
            })
            .FirstOrDefaultAsync(ct);

        if (gruplu == null) return new PuantajFaturaOzetDto();

        gruplu.FaturaKesilmeyen = gruplu.ToplamKayit - gruplu.FaturaKesilen;
        gruplu.NetGelir = gruplu.ToplamGelir - gruplu.ToplamKdv - gruplu.ToplamKesinti;
        gruplu.NetGider = gruplu.ToplamGider;
        gruplu.KarZarar = gruplu.NetGelir - gruplu.NetGider;

        return gruplu;
    }

    // ══════════════════════════════════════════════
    // SATIRLAR (Sayfalamalı)
    // ══════════════════════════════════════════════

    public async Task<List<PuantajFaturaSatirDto>> GetSatirlarAsync(PuantajFaturaRaporRequest request, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var query = db.PuantajKayitlar
            .Where(x => x.Yil == request.Yil && x.Ay == request.Ay && !x.IsDeleted)
            .Where(x => x.OnayDurum == PuantajOnayDurum.Onaylandi)
            .AsQueryable();

        query = ApplyFilters(query, request);

        if (request.Yon == PuantajFaturaYonu.Gelir)
            query = query.Where(x => x.ToplamGelir > 0 || x.BirimGelir > 0);
        else if (request.Yon == PuantajFaturaYonu.Gider)
            query = query.Where(x => x.ToplamGider > 0 || x.BirimGider > 0);

        if (!string.IsNullOrWhiteSpace(request.Arama))
        {
            var arama = request.Arama.Trim().ToUpperInvariant();
            query = query.Where(x =>
                (x.Plaka != null && x.Plaka.ToUpper().Contains(arama)) ||
                (x.SoforAdi != null && x.SoforAdi.ToUpper().Contains(arama)) ||
                (x.KurumAdi != null && x.KurumAdi.ToUpper().Contains(arama)) ||
                (x.FaturaKesiciAdi != null && x.FaturaKesiciAdi.ToUpper().Contains(arama)));
        }

        var pageSize = Math.Min(request.PageSize, MaxPageSize);
        var skip = (Math.Max(1, request.Page) - 1) * pageSize;

        var kayitlar = await query
            .Include(x => x.Kurum)
            .Include(x => x.Guzergah)
            .Include(x => x.Arac)
            .Include(x => x.Sofor)
            .Include(x => x.FaturaKesiciCari)
            .Include(x => x.OdemeYapilacakCari)
            .AsNoTracking()
            .OrderBy(x => x.SiraNo)
            .ThenBy(x => x.Id)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(ct);

        return kayitlar.Select(MapToSatirDto).ToList();
    }

    // ══════════════════════════════════════════════
    // AĞAÇ (Hiyerarşik gruplu)
    // ══════════════════════════════════════════════

    public async Task<List<PuantajFaturaAgacNodeDto>> GetAgacAsync(PuantajFaturaRaporRequest request, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var query = db.PuantajKayitlar
            .Where(x => x.Yil == request.Yil && x.Ay == request.Ay && !x.IsDeleted)
            .Where(x => x.OnayDurum == PuantajOnayDurum.Onaylandi)
            .AsQueryable();

        query = ApplyFilters(query, request);

        if (request.Yon == PuantajFaturaYonu.Gelir)
            query = query.Where(x => x.ToplamGelir > 0 || x.BirimGelir > 0);
        else if (request.Yon == PuantajFaturaYonu.Gider)
            query = query.Where(x => x.ToplamGider > 0 || x.BirimGider > 0);

        var tumKayitlar = await query
            .Include(x => x.Kurum)
            .Include(x => x.FaturaKesiciCari)
            .Include(x => x.OdemeYapilacakCari)
            .Include(x => x.Guzergah)
            .Include(x => x.Arac)
            .Include(x => x.Sofor)
            .AsNoTracking()
            .OrderBy(x => x.SiraNo)
            .ToListAsync(ct);

        var satirlar = tumKayitlar.Select(MapToSatirDto).ToList();

        return request.Agac switch
        {
            PuantajFaturaAgacYapisi.KurumAracGuzergah => BuildAgac(satirlar,
                s => s.KurumAdi ?? $"Kurum #{s.KurumId}",
                s => s.Plaka ?? $"Arac #{s.AracId}",
                s => s.GuzergahAdi ?? "-",
                request.Yon),

            PuantajFaturaAgacYapisi.KurumGuzergahArac => BuildAgac(satirlar,
                s => s.KurumAdi ?? $"Kurum #{s.KurumId}",
                s => s.GuzergahAdi ?? "-",
                s => s.Plaka ?? $"Arac #{s.AracId}",
                request.Yon),

            PuantajFaturaAgacYapisi.TedarikciAracGuzergah => BuildAgac(satirlar,
                s => s.TedarikciUnvan ?? s.CariUnvan ?? $"Cari #{s.CariId}",
                s => s.Plaka ?? $"Arac #{s.AracId}",
                s => s.GuzergahAdi ?? "-",
                request.Yon),

            _ => BuildAgac(satirlar, // Varsayılan: CariAracGuzergah
                s => s.CariUnvan ?? $"Cari #{s.CariId}",
                s => s.Plaka ?? $"Arac #{s.AracId}",
                s => s.GuzergahAdi ?? "-",
                request.Yon),
        };
    }

    // ══════════════════════════════════════════════
    // COUNT
    // ══════════════════════════════════════════════

    public async Task<int> GetCountAsync(PuantajFaturaRaporRequest request, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await BuildBaseQuery(db, request).CountAsync(ct);
    }

    // ══════════════════════════════════════════════
    // PRIVATE HELPERS
    // ══════════════════════════════════════════════

    private static IQueryable<PuantajKayit> BuildBaseQuery(ApplicationDbContext db, PuantajFaturaRaporRequest request)
    {
        var query = db.PuantajKayitlar
            .Where(x => x.Yil == request.Yil && x.Ay == request.Ay && !x.IsDeleted)
            .Where(x => x.OnayDurum == PuantajOnayDurum.Onaylandi);

        return ApplyFilters(query, request);
    }

    private static IQueryable<PuantajKayit> ApplyFilters(IQueryable<PuantajKayit> query, PuantajFaturaRaporRequest request)
    {
        if (request.KurumId.HasValue)
            query = query.Where(x => x.KurumId == request.KurumId.Value);

        if (request.CariId.HasValue)
            query = query.Where(x =>
                x.FaturaKesiciCariId == request.CariId.Value ||
                x.OdemeYapilacakCariId == request.CariId.Value);

        if (request.AracId.HasValue)
            query = query.Where(x => x.AracId == request.AracId.Value);

        if (request.GuzergahId.HasValue)
            query = query.Where(x => x.GuzergahId == request.GuzergahId.Value);

        return query;
    }

    private static PuantajFaturaSatirDto MapToSatirDto(PuantajKayit k)
    {
        return new PuantajFaturaSatirDto
        {
            KayitId = k.Id,
            Kaynak = "PuantajKayit",

            KurumId = k.KurumId,
            KurumAdi = k.Kurum?.KurumAdi ?? k.KurumAdi,

            CariId = k.FaturaKesiciCariId ?? k.OdemeYapilacakCariId,
            CariUnvan = k.FaturaKesiciCari?.Unvan ?? k.OdemeYapilacakCari?.Unvan ?? k.FaturaKesiciAdi,
            Telefon = k.FaturaKesiciTelefon ?? k.FaturaKesiciCari?.Telefon ?? k.OdemeYapilacakCari?.Telefon,

            TedarikciId = k.KaynakTipi == PlanlamaKaynakTipi.Tedarikci ? k.OdemeYapilacakCariId : null,
            TedarikciUnvan = k.KaynakTipi == PlanlamaKaynakTipi.Tedarikci
                ? k.OdemeYapilacakCari?.Unvan ?? k.FaturaKesiciAdi
                : null,

            AracId = k.AracId,
            Plaka = k.Arac?.AktifPlaka ?? k.Plaka,

            SoforId = k.SoforId,
            SoforAdi = k.Sofor != null ? $"{k.Sofor.Ad} {k.Sofor.Soyad}" : k.SoforAdi,

            GuzergahId = k.GuzergahId,
            GuzergahAdi = k.Guzergah?.GuzergahAdi ?? k.GuzergahAdi,

            SlotAdi = k.SlotAdi,
            YonTipi = k.Yon.ToString(),

            BirimGelir = k.BirimGelir,
            ToplamGelir = k.ToplamGelir,
            BirimGider = k.BirimGider,
            ToplamGider = k.ToplamGider,

            KdvTutar = k.GelirKdvTutari + k.GiderKdv20Tutari + k.GiderKdv10Tutari,
            Kdv10Tutar = k.GiderKdv10Tutari,
            Kdv20Tutar = k.GiderKdv20Tutari + k.GelirKdvTutari,
            KesintiTutar = k.GelirKesinti + k.GiderKesinti,

            TahsilEdilecek = k.Alinacak,
            Odenecek = k.Odenecek,

            ToplamSefer = (int)k.Gun,
            Gun = k.Gun,

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
                            SeviyeAdi = "Seviye2",
                            Etiket = g2.Key,
                            Satirlar = satirList,
                            SatirSayisi = satirList.Count,
                            ToplamSefer = satirList.Sum(s => s.ToplamSefer),
                            ToplamGelir = satirList.Sum(s => s.ToplamGelir),
                            ToplamGider = satirList.Sum(s => s.ToplamGider),
                            ToplamKdv = satirList.Sum(s => s.KdvTutar),
                            ToplamKesinti = satirList.Sum(s => s.KesintiTutar),
                            NetTutar = yon == PuantajFaturaYonu.Gelir
                                ? satirList.Sum(s => s.TahsilEdilecek)
                                : satirList.Sum(s => s.Odenecek),
                        };
                    })
                    .ToList();

                var tumSatirlar = g1.ToList();
                return new PuantajFaturaAgacNodeDto
                {
                    SeviyeAdi = "Seviye1",
                    Etiket = g1.Key,
                    Cocuklar = cocuklar,
                    SatirSayisi = tumSatirlar.Count,
                    ToplamSefer = tumSatirlar.Sum(s => s.ToplamSefer),
                    ToplamGelir = tumSatirlar.Sum(s => s.ToplamGelir),
                    ToplamGider = tumSatirlar.Sum(s => s.ToplamGider),
                    ToplamKdv = tumSatirlar.Sum(s => s.KdvTutar),
                    ToplamKesinti = tumSatirlar.Sum(s => s.KesintiTutar),
                };
            })
            .ToList();
    }
}
