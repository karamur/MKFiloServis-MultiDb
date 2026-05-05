using Microsoft.EntityFrameworkCore;
using KOAFiloServis.Web.Data;
using KOAFiloServis.Shared.Entities;
using KOAFiloServis.Web.Services.Interfaces;
using System.Text.Json;

namespace KOAFiloServis.Web.Services;

public class LastikService : ILastikService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public LastikService(IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    // ================================================================
    //  DEPO
    // ================================================================

    public async Task<List<LastikDepo>> GetDepoListAsync()
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        return await ctx.LastikDepolar
            .AsNoTracking()
            .Where(d => !d.IsDeleted)
            .OrderBy(d => d.DepoAdi)
            .ToListAsync();
    }

    public async Task<LastikDepo?> GetDepoByIdAsync(int id)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        return await ctx.LastikDepolar
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);
    }

    public async Task<LastikDepo> CreateDepoAsync(LastikDepo depo)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        ctx.LastikDepolar.Add(depo);
        await ctx.SaveChangesAsync();
        return depo;
    }

    public async Task<LastikDepo> UpdateDepoAsync(LastikDepo depo)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        depo.UpdatedAt = DateTime.UtcNow;
        ctx.LastikDepolar.Update(depo);
        await ctx.SaveChangesAsync();
        return depo;
    }

    public async Task DeleteDepoAsync(int id)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        var depo = await ctx.LastikDepolar.FindAsync(id);
        if (depo != null)
        {
            depo.IsDeleted = true;
            depo.UpdatedAt = DateTime.UtcNow;
            await ctx.SaveChangesAsync();
        }
    }

    // ================================================================
    //  STOK
    // ================================================================

    public async Task<List<LastikStok>> GetStokListAsync(int? depoId = null, bool? aktif = true)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        var query = ctx.LastikStoklar
            .AsNoTracking()
            .Include(s => s.Depo)
            .Include(s => s.Arac)
            .Where(s => !s.IsDeleted);

        if (depoId.HasValue)
            query = query.Where(s => s.DepoId == depoId.Value);

        if (aktif.HasValue)
            query = query.Where(s => s.Aktif == aktif.Value);

        return await query
            .OrderBy(s => s.Depo != null ? s.Depo.DepoAdi : "")
            .ThenBy(s => s.Marka)
            .ThenBy(s => s.Ebat)
            .ToListAsync();
    }

    public async Task<LastikStok?> GetStokByIdAsync(int id)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        return await ctx.LastikStoklar
            .AsNoTracking()
            .Include(s => s.Depo)
            .Include(s => s.Arac)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);
    }

    public async Task<LastikStok> CreateStokAsync(LastikStok stok)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        ctx.LastikStoklar.Add(stok);
        await ctx.SaveChangesAsync();
        return stok;
    }

    public async Task<List<LastikStok>> CreateStokToplualAsync(LastikStok sablon, int adet)
    {
        if (adet < 1) adet = 1;
        if (adet > 20) adet = 20;

        await using var ctx = await _contextFactory.CreateDbContextAsync();
        var liste = new List<LastikStok>(adet);
        for (int i = 0; i < adet; i++)
        {
            var yeni = new LastikStok
            {
                SirketId = sablon.SirketId,
                DepoId = sablon.DepoId,
                AracId = sablon.AracId,
                YedekMi = sablon.YedekMi,
                Marka = sablon.Marka,
                Ebat = sablon.Ebat,
                Sezon = sablon.Sezon,
                SeriNo = sablon.SeriNo,
                Durum = sablon.Durum,
                Aktif = sablon.Aktif,
                Notlar = sablon.Notlar
            };
            ctx.LastikStoklar.Add(yeni);
            liste.Add(yeni);
        }
        await ctx.SaveChangesAsync();
        return liste;
    }

    public async Task<LastikStok> UpdateStokAsync(LastikStok stok)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        stok.UpdatedAt = DateTime.UtcNow;
        ctx.LastikStoklar.Update(stok);
        await ctx.SaveChangesAsync();
        return stok;
    }

    public async Task DeleteStokAsync(int id)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        var stok = await ctx.LastikStoklar.FindAsync(id);
        if (stok != null)
        {
            stok.IsDeleted = true;
            stok.UpdatedAt = DateTime.UtcNow;
            await ctx.SaveChangesAsync();
        }
    }

    public async Task PasifAlAsync(int id)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        var stok = await ctx.LastikStoklar.FindAsync(id);
        if (stok != null)
        {
            stok.Aktif = false;
            stok.Durum = LastikDurum.Hurda;
            stok.UpdatedAt = DateTime.UtcNow;
            await ctx.SaveChangesAsync();
        }
    }

    // ================================================================
    //  DEĞİŞİM
    // ================================================================

    public async Task<List<LastikDegisim>> GetDegisimListAsync(int? aracId = null, DateTime? baslangic = null, DateTime? bitis = null)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        var query = ctx.LastikDegisimler
            .AsNoTracking()
            .Include(d => d.Arac)
            .Include(d => d.TakilanStok).ThenInclude(s => s!.Depo)
            .Include(d => d.SokulenStok).ThenInclude(s => s!.Depo)
            .Include(d => d.HedefDepo)
            .Include(d => d.KaynakDepo)
            .Where(d => !d.IsDeleted);

        if (aracId.HasValue)
            query = query.Where(d => d.AracId == aracId.Value);
        if (baslangic.HasValue)
            query = query.Where(d => d.DegisimTarihi >= baslangic.Value);
        if (bitis.HasValue)
            query = query.Where(d => d.DegisimTarihi <= bitis.Value);

        return await query
            .OrderByDescending(d => d.DegisimTarihi)
            .ToListAsync();
    }

    public async Task<LastikDegisim?> GetDegisimByIdAsync(int id)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        return await ctx.LastikDegisimler
            .AsNoTracking()
            .Include(d => d.Arac)
            .Include(d => d.TakilanStok).ThenInclude(s => s!.Depo)
            .Include(d => d.SokulenStok).ThenInclude(s => s!.Depo)
            .Include(d => d.HedefDepo)
            .Include(d => d.KaynakDepo)
            .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);
    }

    public async Task<LastikDegisim> CreateDegisimAsync(LastikDegisim degisim)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        ctx.LastikDegisimler.Add(degisim);
        await ctx.SaveChangesAsync();
        return degisim;
    }

    public async Task<LastikDegisim> UpdateDegisimAsync(LastikDegisim degisim)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        degisim.UpdatedAt = DateTime.UtcNow;
        ctx.LastikDegisimler.Update(degisim);
        await ctx.SaveChangesAsync();
        return degisim;
    }

    public async Task DeleteDegisimAsync(int id)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        var degisim = await ctx.LastikDegisimler.FindAsync(id);
        if (degisim != null)
        {
            degisim.IsDeleted = true;
            degisim.UpdatedAt = DateTime.UtcNow;
            await ctx.SaveChangesAsync();
        }
    }

    public async Task<List<LastikAracDonemOzet>> GetAracDonemOzetListAsync(DateTime? baslangic = null, DateTime? bitis = null)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();

        var araclar = await ctx.Araclar
            .AsNoTracking()
            .Where(a => !a.IsDeleted)
            .Select(a => new
            {
                a.Id,
                a.AktifPlaka,
                a.Marka,
                a.Model,
                a.ModelYili
            })
            .OrderBy(a => a.AktifPlaka)
            .ToListAsync();

        var degisimQuery = ctx.LastikDegisimler
            .AsNoTracking()
            .Where(d => !d.IsDeleted);

        if (baslangic.HasValue)
            degisimQuery = degisimQuery.Where(d => d.DegisimTarihi >= baslangic.Value);

        if (bitis.HasValue)
            degisimQuery = degisimQuery.Where(d => d.DegisimTarihi <= bitis.Value);

        var donemDegisimSayilari = await degisimQuery
            .GroupBy(d => d.AracId)
            .Select(g => new { AracId = g.Key, Sayi = g.Count() })
            .ToDictionaryAsync(x => x.AracId, x => x.Sayi);

        var takiliLastikler = await ctx.LastikStoklar
            .AsNoTracking()
            .Where(s => !s.IsDeleted && s.Aktif && s.AracId != null)
            .Select(s => new LastikAracaTakiliSatir
            {
                AracId = s.AracId!.Value,
                Marka = s.Marka,
                Ebat = s.Ebat,
                Sezon = s.Sezon
            })
            .ToListAsync();

        var takiliByArac = takiliLastikler
            .GroupBy(x => x.AracId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var sonuc = new List<LastikAracDonemOzet>(araclar.Count);

        foreach (var a in araclar)
        {
            var donemDegisimSayisi = donemDegisimSayilari.TryGetValue(a.Id, out var sayi) ? sayi : 0;
            var aracaTakili = takiliByArac.TryGetValue(a.Id, out var liste) ? liste : new List<LastikAracaTakiliSatir>();

            var takiliSayisi = aracaTakili.Count;
            var dortLastikAyni = takiliSayisi == 4 && aracaTakili
                .Select(x => $"{x.Marka}|{x.Ebat}|{(int)x.Sezon}")
                .Distinct()
                .Count() == 1;

            var takiliOzet = takiliSayisi == 0
                ? "Takili lastik kaydi yok"
                : string.Join(" | ", aracaTakili
                    .GroupBy(x => new { x.Marka, x.Ebat, x.Sezon })
                    .Select(g => $"{g.Key.Marka} {g.Key.Ebat} {GetSezonText(g.Key.Sezon)} x{g.Count()}"));

            sonuc.Add(new LastikAracDonemOzet
            {
                AracId = a.Id,
                Plaka = a.AktifPlaka ?? "-",
                AracBilgisi = $"{a.Marka} {a.Model} {a.ModelYili}".Trim(),
                DonemDegisimSayisi = donemDegisimSayisi,
                DonemdeDegisti = donemDegisimSayisi > 0,
                TakiliLastikSayisi = takiliSayisi,
                DortLastikAyniMi = dortLastikAyni,
                TakiliLastikOzeti = takiliOzet
            });
        }

        return sonuc;
    }

    public async Task<LastikAracDetay?> GetAracDetayAsync(int aracId, DateTime? baslangic = null, DateTime? bitis = null)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();

        var arac = await ctx.Araclar
            .AsNoTracking()
            .Include(a => a.PlakaGecmisi)
            .FirstOrDefaultAsync(a => a.Id == aracId && !a.IsDeleted);

        if (arac == null)
            return null;

        var takiliLastikler = await ctx.LastikStoklar
            .AsNoTracking()
            .Include(s => s.Depo)
            .Where(s => !s.IsDeleted && s.Aktif && s.AracId == aracId)
            .OrderBy(s => s.Marka)
            .ThenBy(s => s.Ebat)
            .ToListAsync();

        var hareketQuery = ctx.LastikDegisimler
            .AsNoTracking()
            .Include(d => d.TakilanStok)
            .Include(d => d.SokulenStok)
            .Include(d => d.KaynakDepo)
            .Include(d => d.HedefDepo)
            .Where(d => !d.IsDeleted && d.AracId == aracId);

        if (baslangic.HasValue)
            hareketQuery = hareketQuery.Where(d => d.DegisimTarihi >= baslangic.Value);

        if (bitis.HasValue)
            hareketQuery = hareketQuery.Where(d => d.DegisimTarihi <= bitis.Value);

        var hareketKayitlari = await hareketQuery
            .OrderByDescending(d => d.DegisimTarihi)
            .ToListAsync();

        var hareketler = hareketKayitlari
            .Select(d => new LastikAracHareketSatiri
            {
                DegisimId = d.Id,
                Tarih = d.DegisimTarihi,
                DegisimTipi = d.DegisimTipi,
                KmDurumu = d.KmDurumu,
                TakilanAciklama = FormatTakilanHareket(d),
                SokulenAciklama = FormatSokulenHareket(d),
                YapilanYer = d.YapilanYer ?? string.Empty,
                Ucret = d.Ucret
            })
            .ToList();

        var plakaGecmisi = arac.PlakaGecmisi
            .Where(p => !p.IsDeleted)
            .OrderByDescending(p => p.GirisTarihi)
            .Select(p => new LastikAracPlakaSatiri
            {
                Plaka = p.Plaka,
                GirisTarihi = p.GirisTarihi,
                CikisTarihi = p.CikisTarihi,
                Aktif = p.Aktif
            })
            .ToList();

        return new LastikAracDetay
        {
            AracId = arac.Id,
            Plaka = arac.AktifPlaka ?? "-",
            AracBilgisi = $"{arac.Marka} {arac.Model} {arac.ModelYili}".Trim(),
            PlakaGecmisi = plakaGecmisi,
            TakiliLastikler = takiliLastikler,
            Hareketler = hareketler
        };
    }

    public async Task<List<LastikPlakaEnvanteri>> GetPlakaBazliEnvanterAsync()
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();

        var araclar = await ctx.Araclar
            .AsNoTracking()
            .Where(a => !a.IsDeleted)
            .Select(a => new
            {
                a.Id,
                a.AktifPlaka,
                a.Marka,
                a.Model,
                a.ModelYili
            })
            .OrderBy(a => a.AktifPlaka)
            .ToListAsync();

        var stoklar = await ctx.LastikStoklar
            .AsNoTracking()
            .Include(s => s.Depo)
            .Where(s => !s.IsDeleted && s.Aktif && s.AracId != null)
            .ToListAsync();

        var stokByArac = stoklar.GroupBy(s => s.AracId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        var sonuc = new List<LastikPlakaEnvanteri>(araclar.Count);
        foreach (var a in araclar)
        {
            if (!stokByArac.TryGetValue(a.Id, out var liste))
                continue;

            var satirlar = liste
                .OrderBy(s => s.YedekMi)
                .ThenBy(s => s.Sezon)
                .ThenBy(s => s.Marka)
                .Select(s => new LastikPlakaEnvanterSatiri
                {
                    StokId = s.Id,
                    Marka = s.Marka,
                    Ebat = s.Ebat,
                    Sezon = s.Sezon,
                    Durum = s.Durum,
                    SeriNo = s.SeriNo,
                    Yedek = s.YedekMi,
                    Takili = !s.YedekMi,
                    DepoAdi = s.Depo?.DepoAdi
                })
                .ToList();

            sonuc.Add(new LastikPlakaEnvanteri
            {
                AracId = a.Id,
                Plaka = a.AktifPlaka ?? "-",
                AracBilgisi = $"{a.Marka} {a.Model} {a.ModelYili}".Trim(),
                Lastikler = satirlar,
                TakiliSayisi = satirlar.Count(x => x.Takili),
                YedekSayisi = satirlar.Count(x => x.Yedek),
                YazVar = satirlar.Any(x => x.Sezon == LastikSezon.YazLastigi || x.Sezon == LastikSezon.DortMevsim),
                KisVar = satirlar.Any(x => x.Sezon == LastikSezon.KisLastigi || x.Sezon == LastikSezon.DortMevsim)
            });
        }

        return sonuc;
    }

    public async Task<List<LastikEksikSezonSatiri>> GetEksikSezonRaporuAsync()
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();

        var araclar = await ctx.Araclar
            .AsNoTracking()
            .Where(a => !a.IsDeleted)
            .Select(a => new
            {
                a.Id,
                a.AktifPlaka,
                a.Marka,
                a.Model,
                a.ModelYili
            })
            .OrderBy(a => a.AktifPlaka)
            .ToListAsync();

        var stoklar = await ctx.LastikStoklar
            .AsNoTracking()
            .Where(s => !s.IsDeleted && s.Aktif && s.AracId != null)
            .Select(s => new { s.AracId, s.Sezon })
            .ToListAsync();

        var stokByArac = stoklar.GroupBy(s => s.AracId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        var sonuc = new List<LastikEksikSezonSatiri>();
        foreach (var a in araclar)
        {
            stokByArac.TryGetValue(a.Id, out var liste);
            liste ??= new();

            var yazVar = liste.Any(x => x.Sezon == LastikSezon.YazLastigi || x.Sezon == LastikSezon.DortMevsim);
            var kisVar = liste.Any(x => x.Sezon == LastikSezon.KisLastigi || x.Sezon == LastikSezon.DortMevsim);

            if (!yazVar || !kisVar)
            {
                sonuc.Add(new LastikEksikSezonSatiri
                {
                    AracId = a.Id,
                    Plaka = a.AktifPlaka ?? "-",
                    AracBilgisi = $"{a.Marka} {a.Model} {a.ModelYili}".Trim(),
                    YazEksik = !yazVar,
                    KisEksik = !kisVar,
                    ToplamLastikSayisi = liste.Count
                });
            }
        }
        return sonuc;
    }

    private static string FormatTakilanHareket(LastikDegisim degisim)
    {
        var payload = ParseDegisimPayload(degisim.Notlar);
        if (payload?.Satirlar != null && payload.Satirlar.Count > 0)
        {
            var satirlar = payload.Satirlar
                .Select(s => FormatSatirTakilan(s))
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();

            if (satirlar.Count > 0)
                return string.Join(" || ", satirlar);
        }

        return FormatLastikHareket(degisim.TakilanStok, degisim.KaynakDepo?.DepoAdi, degisim.TakilanPozisyon);
    }

    private static string FormatSokulenHareket(LastikDegisim degisim)
    {
        var payload = ParseDegisimPayload(degisim.Notlar);
        if (payload?.Satirlar != null && payload.Satirlar.Count > 0)
        {
            var satirlar = payload.Satirlar
                .Select(s => FormatSatirSokulen(s))
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();

            if (satirlar.Count > 0)
                return string.Join(" || ", satirlar);
        }

        return FormatLastikHareket(degisim.SokulenStok, degisim.HedefDepo?.DepoAdi, degisim.SokulenPozisyon);
    }

    private static string FormatSatirTakilan(LastikDegisimNotSatiri satir)
    {
        if (string.IsNullOrWhiteSpace(satir.TakilanEtiket))
            return string.Empty;

        var pozisyon = string.IsNullOrWhiteSpace(satir.Pozisyon) ? "Pozisyon?" : satir.Pozisyon;
        var depo = string.IsNullOrWhiteSpace(satir.KaynakDepoAdi) ? string.Empty : $" | {satir.KaynakDepoAdi}";
        return $"{pozisyon}: {satir.TakilanEtiket}{depo}";
    }

    private static string FormatSatirSokulen(LastikDegisimNotSatiri satir)
    {
        if (string.IsNullOrWhiteSpace(satir.SokulenEtiket))
            return string.Empty;

        var pozisyon = string.IsNullOrWhiteSpace(satir.Pozisyon) ? "Pozisyon?" : satir.Pozisyon;
        var depo = string.IsNullOrWhiteSpace(satir.HedefDepoAdi) ? string.Empty : $" | {satir.HedefDepoAdi}";
        return $"{pozisyon}: {satir.SokulenEtiket}{depo}";
    }

    private static LastikDegisimNotPayload? ParseDegisimPayload(string? notlar)
    {
        if (string.IsNullOrWhiteSpace(notlar))
            return null;

        try
        {
            return JsonSerializer.Deserialize<LastikDegisimNotPayload>(notlar);
        }
        catch
        {
            return null;
        }
    }

    private static string FormatLastikHareket(LastikStok? stok, string? depoAdi, string? pozisyon)
    {
        if (stok == null)
            return "-";

        var lastik = $"{stok.Marka} {stok.Ebat} {GetSezonText(stok.Sezon)}".Trim();
        var depo = string.IsNullOrWhiteSpace(depoAdi) ? string.Empty : $" | {depoAdi}";
        var poz = string.IsNullOrWhiteSpace(pozisyon) ? string.Empty : $" | {pozisyon}";
        return $"{lastik}{depo}{poz}";
    }

    private static string GetSezonText(LastikSezon sezon) => sezon switch
    {
        LastikSezon.YazLastigi => "Yaz",
        LastikSezon.KisLastigi => "Kis",
        LastikSezon.DortMevsim => "4 Mevsim",
        _ => sezon.ToString()
    };
}

internal sealed class LastikAracaTakiliSatir
{
    public int AracId { get; set; }
    public string? Marka { get; set; }
    public string Ebat { get; set; } = string.Empty;
    public LastikSezon Sezon { get; set; }
}

internal sealed class LastikDegisimNotPayload
{
    public string? GirisTipi { get; set; }
    public int Adet { get; set; }
    public string? UserNot { get; set; }
    public List<LastikDegisimNotSatiri> Satirlar { get; set; } = new();
}

internal sealed class LastikDegisimNotSatiri
{
    public string? Pozisyon { get; set; }

    public int? TakilanStokId { get; set; }
    public string? TakilanEtiket { get; set; }
    public int? KaynakDepoId { get; set; }
    public string? KaynakDepoAdi { get; set; }

    public int? SokulenStokId { get; set; }
    public string? SokulenEtiket { get; set; }
    public int? HedefDepoId { get; set; }
    public string? HedefDepoAdi { get; set; }
}
