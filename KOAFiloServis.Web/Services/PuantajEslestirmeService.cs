using KOAFiloServis.Shared.Entities;
using KOAFiloServis.Web.Data;
using KOAFiloServis.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KOAFiloServis.Web.Services;

public class PuantajEslestirmeService : IPuantajEslestirmeService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public PuantajEslestirmeService(IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    // ============== Firma + Araç + Şoför ==============
    public async Task<List<FirmaAracSoforEslestirme>> GetAracSoforEslestirmeleriAsync(int firmaId, bool sadeceAktif = false)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        var q = ctx.FirmaAracSoforEslestirmeleri
            .Include(e => e.KurumCari)
            .Include(e => e.Arac)
            .Include(e => e.Sofor)
            .Where(e => e.FirmaId == firmaId);
        if (sadeceAktif) q = q.Where(e => e.Aktif);
        return await q.OrderBy(e => e.KurumCari!.Unvan)
            .ThenBy(e => e.Arac!.AktifPlaka)
            .ToListAsync();
    }

    public async Task<FirmaAracSoforEslestirme?> GetAracSoforEslestirmeByIdAsync(int id)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        return await ctx.FirmaAracSoforEslestirmeleri
            .Include(e => e.KurumCari).Include(e => e.Arac).Include(e => e.Sofor)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<FirmaAracSoforEslestirme> CreateAracSoforEslestirmeAsync(FirmaAracSoforEslestirme model)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        ctx.FirmaAracSoforEslestirmeleri.Add(model);
        await ctx.SaveChangesAsync();
        return model;
    }

    public async Task<FirmaAracSoforEslestirme> UpdateAracSoforEslestirmeAsync(FirmaAracSoforEslestirme model)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        var ex = await ctx.FirmaAracSoforEslestirmeleri.FindAsync(model.Id);
        if (ex == null) return model;
        ex.KurumCariId = model.KurumCariId;
        ex.AracId = model.AracId;
        ex.SoforId = model.SoforId;
        ex.BaslangicTarihi = model.BaslangicTarihi;
        ex.BitisTarihi = model.BitisTarihi;
        ex.VarsayilanBirimUcret = model.VarsayilanBirimUcret;
        ex.Aktif = model.Aktif;
        ex.Notlar = model.Notlar;
        ex.UpdatedAt = DateTime.UtcNow;
        await ctx.SaveChangesAsync();
        return ex;
    }

    public async Task DeleteAracSoforEslestirmeAsync(int id)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        var ex = await ctx.FirmaAracSoforEslestirmeleri.FindAsync(id);
        if (ex == null) return;
        ex.IsDeleted = true;
        ex.UpdatedAt = DateTime.UtcNow;
        await ctx.SaveChangesAsync();
    }

    public async Task<List<Arac>> GetEslesmeyenAraclarAsync(int firmaId)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        var eslesmisAracIds = await ctx.FirmaAracSoforEslestirmeleri
            .Where(e => e.FirmaId == firmaId && e.Aktif)
            .Select(e => e.AracId).Distinct().ToListAsync();
        return await ctx.Araclar
            .Where(a => !a.IsDeleted && !eslesmisAracIds.Contains(a.Id))
            .OrderBy(a => a.AktifPlaka).ToListAsync();
    }

    public async Task<List<Cari>> GetEslesmeyenKurumlarAsync(int firmaId)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        var eslesmisCariIds = await ctx.FirmaAracSoforEslestirmeleri
            .Where(e => e.FirmaId == firmaId && e.Aktif)
            .Select(e => e.KurumCariId).Distinct().ToListAsync();
        return await ctx.Cariler
            .Where(c => !c.IsDeleted
                && (c.CariTipi == CariTipi.Musteri || c.CariTipi == CariTipi.MusteriTedarikci)
                && !eslesmisCariIds.Contains(c.Id))
            .OrderBy(c => c.Unvan).ToListAsync();
    }

    public async Task<int> TopluAracSoforEslestirAsync(
        int firmaId,
        List<(int AracId, int SoforId)> aracSoforListesi,
        List<int> kurumCariIdleri,
        decimal varsayilanBirimUcret)
    {
        if (aracSoforListesi == null || aracSoforListesi.Count == 0
            || kurumCariIdleri == null || kurumCariIdleri.Count == 0)
            return 0;

        await using var ctx = await _contextFactory.CreateDbContextAsync();
        var yeni = new List<FirmaAracSoforEslestirme>();
        foreach (var (aracId, soforId) in aracSoforListesi)
        {
            foreach (var kurumId in kurumCariIdleri)
            {
                bool varMi = await ctx.FirmaAracSoforEslestirmeleri.AnyAsync(e =>
                    e.FirmaId == firmaId && e.KurumCariId == kurumId &&
                    e.AracId == aracId && e.SoforId == soforId);
                if (varMi) continue;
                yeni.Add(new FirmaAracSoforEslestirme
                {
                    FirmaId = firmaId,
                    KurumCariId = kurumId,
                    AracId = aracId,
                    SoforId = soforId,
                    VarsayilanBirimUcret = varsayilanBirimUcret,
                    Aktif = true,
                    BaslangicTarihi = DateTime.Today
                });
            }
        }
        if (yeni.Count > 0)
        {
            await ctx.FirmaAracSoforEslestirmeleri.AddRangeAsync(yeni);
            await ctx.SaveChangesAsync();
        }
        return yeni.Count;
    }

    // ============== Firma + Güzergah ==============
    public async Task<List<FirmaGuzergahEslestirme>> GetGuzergahEslestirmeleriAsync(int firmaId, bool sadeceAktif = false)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        var q = ctx.FirmaGuzergahEslestirmeleri
            .Include(e => e.KurumCari)
            .Include(e => e.Guzergah)
            .Where(e => e.FirmaId == firmaId);
        if (sadeceAktif) q = q.Where(e => e.Aktif);
        return await q.OrderBy(e => e.KurumCari!.Unvan)
            .ThenBy(e => e.Guzergah!.GuzergahAdi).ToListAsync();
    }

    public async Task<FirmaGuzergahEslestirme?> GetGuzergahEslestirmeByIdAsync(int id)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        return await ctx.FirmaGuzergahEslestirmeleri
            .Include(e => e.KurumCari).Include(e => e.Guzergah)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<FirmaGuzergahEslestirme> CreateGuzergahEslestirmeAsync(FirmaGuzergahEslestirme model)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        ctx.FirmaGuzergahEslestirmeleri.Add(model);
        await ctx.SaveChangesAsync();
        return model;
    }

    public async Task<FirmaGuzergahEslestirme> UpdateGuzergahEslestirmeAsync(FirmaGuzergahEslestirme model)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        var ex = await ctx.FirmaGuzergahEslestirmeleri.FindAsync(model.Id);
        if (ex == null) return model;
        ex.KurumCariId = model.KurumCariId;
        ex.GuzergahId = model.GuzergahId;
        ex.BaslangicTarihi = model.BaslangicTarihi;
        ex.BitisTarihi = model.BitisTarihi;
        ex.SeferUcreti = model.SeferUcreti;
        ex.KdvOrani = model.KdvOrani;
        ex.Aktif = model.Aktif;
        ex.Notlar = model.Notlar;
        ex.UpdatedAt = DateTime.UtcNow;
        await ctx.SaveChangesAsync();
        return ex;
    }

    public async Task DeleteGuzergahEslestirmeAsync(int id)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        var ex = await ctx.FirmaGuzergahEslestirmeleri.FindAsync(id);
        if (ex == null) return;
        ex.IsDeleted = true;
        ex.UpdatedAt = DateTime.UtcNow;
        await ctx.SaveChangesAsync();
    }

    public async Task<List<Guzergah>> GetEslesmeyenGuzergahlarAsync(int firmaId)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        var eslesmisIds = await ctx.FirmaGuzergahEslestirmeleri
            .Where(e => e.FirmaId == firmaId && e.Aktif)
            .Select(e => e.GuzergahId).Distinct().ToListAsync();
        return await ctx.Guzergahlar
            .Where(g => !g.IsDeleted && g.Aktif && !eslesmisIds.Contains(g.Id))
            .OrderBy(g => g.GuzergahAdi).ToListAsync();
    }

    public async Task<List<Cari>> GetGuzergahsiZKurumlarAsync(int firmaId)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        var eslesmisCariIds = await ctx.FirmaGuzergahEslestirmeleri
            .Where(e => e.FirmaId == firmaId && e.Aktif)
            .Select(e => e.KurumCariId).Distinct().ToListAsync();
        return await ctx.Cariler
            .Where(c => !c.IsDeleted
                && (c.CariTipi == CariTipi.Musteri || c.CariTipi == CariTipi.MusteriTedarikci)
                && !eslesmisCariIds.Contains(c.Id))
            .OrderBy(c => c.Unvan).ToListAsync();
    }

    public async Task<int> TopluGuzergahEslestirAsync(
        int firmaId,
        List<int> guzergahIdleri,
        List<int> kurumCariIdleri,
        decimal seferUcreti,
        int kdvOrani = 20)
    {
        if (guzergahIdleri == null || guzergahIdleri.Count == 0
            || kurumCariIdleri == null || kurumCariIdleri.Count == 0)
            return 0;

        await using var ctx = await _contextFactory.CreateDbContextAsync();
        var yeni = new List<FirmaGuzergahEslestirme>();
        foreach (var gId in guzergahIdleri)
        {
            foreach (var cId in kurumCariIdleri)
            {
                bool varMi = await ctx.FirmaGuzergahEslestirmeleri.AnyAsync(e =>
                    e.FirmaId == firmaId && e.KurumCariId == cId && e.GuzergahId == gId);
                if (varMi) continue;
                yeni.Add(new FirmaGuzergahEslestirme
                {
                    FirmaId = firmaId,
                    KurumCariId = cId,
                    GuzergahId = gId,
                    SeferUcreti = seferUcreti,
                    KdvOrani = kdvOrani,
                    Aktif = true,
                    BaslangicTarihi = DateTime.Today
                });
            }
        }
        if (yeni.Count > 0)
        {
            await ctx.FirmaGuzergahEslestirmeleri.AddRangeAsync(yeni);
            await ctx.SaveChangesAsync();
        }
        return yeni.Count;
    }

    // ============== Mutabakat (gerçek FiloGunlukPuantaj verilerinden) ==============
    public async Task<List<CariFaturaMutabakatRow>> GetCariMutabakatAsync(int firmaId, DateTime baslangic, DateTime bitis)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();

        // Günlük puantaj kayıtlarından kuruma tahakkuk eden gerçek tutarlar
        // KurumFirmaId üzerinden Firma -> CariId eşlemesi yapamadığımız için (Firma'nın CariId'si yok),
        // FirmaGuzergahEslestirme tablosundan KurumFirmaId -> KurumCariId map'i çıkarıyoruz.
        var firmaToCari = await ctx.FirmaGuzergahEslestirmeleri
            .Where(e => e.FirmaId == firmaId)
            .GroupBy(e => e.KurumCariId)
            .Select(g => new { CariId = g.Key, FirmaIds = g.Select(x => x.FirmaId).Distinct().ToList() })
            .ToListAsync();

        // Önce tüm tahakkuk satırlarını al, sonra cariId'ye agrega et
        var puantajlar = await ctx.FiloGunlukPuantajlar
            .Include(p => p.KurumFirma)
            .Where(p => p.FirmaId == firmaId
                && p.Tarih >= baslangic && p.Tarih <= bitis
                && !p.IsDeleted)
            .Select(p => new
            {
                p.KurumFirmaId,
                KurumAd = p.KurumFirma!.FirmaAdi,
                p.TahakkukEdenKurumUcreti,
                p.KurumFaturaKesildiMi
            })
            .ToListAsync();

        // KurumFirmaId -> Cari eşleşmesi: Cariler tablosunda Unvan eşleşmesi ile bul
        var tumCariler = await ctx.Cariler.AsNoTracking().ToListAsync();
        var cariByUnvan = tumCariler
            .GroupBy(c => c.Unvan.Trim().ToLower())
            .ToDictionary(g => g.Key, g => g.First());

        var tahakkukByCari = new Dictionary<int, (string Unvan, decimal Tutar, int Sayi, int FaturaliSayi)>();
        foreach (var grp in puantajlar.GroupBy(p => p.KurumFirmaId))
        {
            string ad = grp.First().KurumAd ?? "(?)";
            int? cariId = null;
            if (cariByUnvan.TryGetValue(ad.Trim().ToLower(), out var c)) cariId = c.Id;
            if (!cariId.HasValue) continue;

            var tutar = grp.Sum(x => x.TahakkukEdenKurumUcreti);
            var sayi = grp.Count();
            var faturali = grp.Count(x => x.KurumFaturaKesildiMi);
            if (tahakkukByCari.TryGetValue(cariId.Value, out var existing))
                tahakkukByCari[cariId.Value] = (existing.Unvan, existing.Tutar + tutar, existing.Sayi + sayi, existing.FaturaliSayi + faturali);
            else
                tahakkukByCari[cariId.Value] = (c!.Unvan, tutar, sayi, faturali);
        }

        // Kesilen (Giden) faturalar
        var faturalar = await ctx.Faturalar
            .Include(f => f.Cari)
            .Where(f => f.FaturaYonu == FaturaYonu.Giden
                && f.FaturaTarihi >= baslangic && f.FaturaTarihi <= bitis
                && (firmaId == 0 || f.SirketId == null || f.SirketId == firmaId))
            .Select(f => new { f.CariId, Unvan = f.Cari!.Unvan, f.GenelToplam })
            .ToListAsync();

        var faturaDict = faturalar
            .GroupBy(f => f.CariId)
            .ToDictionary(g => g.Key, g => new
            {
                Unvan = g.First().Unvan,
                Tutar = g.Sum(f => f.GenelToplam),
                Sayi = g.Count()
            });

        var allCariIds = tahakkukByCari.Keys.Union(faturaDict.Keys).ToList();
        var sonuc = new List<CariFaturaMutabakatRow>();
        foreach (var cariId in allCariIds)
        {
            tahakkukByCari.TryGetValue(cariId, out var tk);
            faturaDict.TryGetValue(cariId, out var fk);
            sonuc.Add(new CariFaturaMutabakatRow
            {
                CariId = cariId,
                CariUnvan = tk.Unvan ?? fk?.Unvan ?? "(?)",
                TahakkukEdenTutar = tk.Tutar,
                KesilenFaturaTutari = fk?.Tutar ?? 0,
                PuantajSayisi = tk.Sayi,
                FaturaSayisi = fk?.Sayi ?? 0
            });
        }
        return sonuc.OrderByDescending(r => Math.Abs(r.Fark)).ToList();
    }

    public async Task<List<TasimaTedarikciMutabakatRow>> GetTasimaTedarikciMutabakatAsync(int firmaId, DateTime baslangic, DateTime bitis)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();

        // Tahakkuk: günlük puantajdaki TahakkukEdenTaseronUcreti — araç -> TasimaTedarikciId üzerinden
        var puantajlar = await ctx.FiloGunlukPuantajlar
            .Include(p => p.Arac)
            .Where(p => p.FirmaId == firmaId
                && p.Tarih >= baslangic && p.Tarih <= bitis
                && !p.IsDeleted
                && p.Arac != null && p.Arac.TasimaTedarikciId != null
                && p.TahakkukEdenTaseronUcreti > 0)
            .Select(p => new
            {
                TedarikciId = p.Arac!.TasimaTedarikciId!.Value,
                p.TahakkukEdenTaseronUcreti,
                p.TaseronOdemeYapildiMi
            })
            .ToListAsync();

        var tedarikciler = await ctx.TasimaTedarikciler
            .Where(t => !t.IsDeleted)
            .Select(t => new { t.Id, t.Unvan, t.CariId })
            .ToListAsync();
        var tedDict = tedarikciler.ToDictionary(t => t.Id);

        var tahakkukByTed = puantajlar
            .GroupBy(p => p.TedarikciId)
            .Where(g => tedDict.ContainsKey(g.Key))
            .ToDictionary(g => g.Key, g => new
            {
                Tutar = g.Sum(x => x.TahakkukEdenTaseronUcreti),
                Sayi = g.Count(),
                OdenenSayi = g.Count(x => x.TaseronOdemeYapildiMi)
            });

        // Gelen faturalar: tedarikçinin Cari'si üzerinden
        var tedarikciCariIdleri = tedarikciler.Where(t => t.CariId.HasValue).Select(t => t.CariId!.Value).ToList();
        var faturalar = await ctx.Faturalar
            .Where(f => f.FaturaYonu == FaturaYonu.Gelen
                && f.FaturaTarihi >= baslangic && f.FaturaTarihi <= bitis
                && tedarikciCariIdleri.Contains(f.CariId))
            .Select(f => new { f.CariId, f.GenelToplam })
            .ToListAsync();

        var faturaByCari = faturalar
            .GroupBy(f => f.CariId)
            .ToDictionary(g => g.Key, g => new { Tutar = g.Sum(f => f.GenelToplam), Sayi = g.Count() });

        var sonuc = new List<TasimaTedarikciMutabakatRow>();
        foreach (var t in tedarikciler)
        {
            tahakkukByTed.TryGetValue(t.Id, out var tk);
            decimal faturaTutar = 0; int faturaSayi = 0;
            if (t.CariId.HasValue && faturaByCari.TryGetValue(t.CariId.Value, out var fk))
            {
                faturaTutar = fk.Tutar;
                faturaSayi = fk.Sayi;
            }
            decimal tahakkuk = tk?.Tutar ?? 0;
            if (tahakkuk == 0 && faturaTutar == 0) continue;
            sonuc.Add(new TasimaTedarikciMutabakatRow
            {
                TedarikciId = t.Id,
                TedarikciUnvan = t.Unvan,
                TahakkukEdenTutar = tahakkuk,
                GelenFaturaTutari = faturaTutar,
                PuantajSayisi = tk?.Sayi ?? 0,
                FaturaSayisi = faturaSayi
            });
        }
        return sonuc.OrderByDescending(r => Math.Abs(r.Fark)).ToList();
    }

    private static int HesaplaIsGunu(DateTime baslangic, DateTime bitis)
    {
        int gun = 0;
        for (var d = baslangic.Date; d <= bitis.Date; d = d.AddDays(1))
        {
            if (d.DayOfWeek != DayOfWeek.Sunday) gun++;
        }
        return gun;
    }

    // ============== Drill-down Detay ==============
    public async Task<MutabakatDetay> GetCariMutabakatDetayAsync(int firmaId, int cariId, DateTime baslangic, DateTime bitis)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        var cari = await ctx.Cariler.AsNoTracking().FirstOrDefaultAsync(c => c.Id == cariId);
        var detay = new MutabakatDetay { Baslik = cari?.Unvan ?? "(?)" };
        if (cari == null) return detay;

        // KurumFirmaId(ler)i: Firma.FirmaAdi == Cari.Unvan eşlemesi
        var firmaIds = await ctx.Firmalar
            .Where(f => f.FirmaAdi.ToLower() == cari.Unvan.ToLower())
            .Select(f => f.Id).ToListAsync();

        var puantajlar = await ctx.FiloGunlukPuantajlar
            .Include(p => p.Arac).Include(p => p.Sofor).Include(p => p.Guzergah)
            .Where(p => p.FirmaId == firmaId
                && p.Tarih >= baslangic && p.Tarih <= bitis
                && !p.IsDeleted
                && firmaIds.Contains(p.KurumFirmaId)
                && p.TahakkukEdenKurumUcreti > 0)
            .OrderBy(p => p.Tarih)
            .ToListAsync();

        detay.Puantajlar = puantajlar.Select(p => new MutabakatDetayPuantaj
        {
            Id = p.Id,
            Tarih = p.Tarih,
            AracPlaka = p.Arac?.AktifPlaka ?? "",
            Sofor = p.Sofor != null ? $"{p.Sofor.Ad} {p.Sofor.Soyad}" : "",
            Guzergah = p.Guzergah?.GuzergahAdi ?? "",
            SeferSayisi = p.SeferSayisi,
            Tutar = p.TahakkukEdenKurumUcreti,
            Faturalandi = p.KurumFaturaKesildiMi,
            FaturaId = p.KurumFaturaId
        }).ToList();

        var faturalar = await ctx.Faturalar
            .Where(f => f.CariId == cariId && f.FaturaYonu == FaturaYonu.Giden
                && f.FaturaTarihi >= baslangic && f.FaturaTarihi <= bitis
                && (firmaId == 0 || f.SirketId == null || f.SirketId == firmaId))
            .OrderBy(f => f.FaturaTarihi)
            .ToListAsync();

        var bagliFaturaIds = puantajlar.Where(p => p.KurumFaturaId.HasValue).Select(p => p.KurumFaturaId!.Value).ToHashSet();
        detay.Faturalar = faturalar.Select(f => new MutabakatDetayFatura
        {
            Id = f.Id,
            Tarih = f.FaturaTarihi,
            FaturaNo = f.FaturaNo ?? "",
            Tutar = f.GenelToplam,
            PuantajaBagli = bagliFaturaIds.Contains(f.Id)
        }).ToList();
        return detay;
    }

    public async Task<MutabakatDetay> GetTasimaTedarikciMutabakatDetayAsync(int firmaId, int tedarikciId, DateTime baslangic, DateTime bitis)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        var ted = await ctx.TasimaTedarikciler.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tedarikciId);
        var detay = new MutabakatDetay { Baslik = ted?.Unvan ?? "(?)" };
        if (ted == null) return detay;

        var puantajlar = await ctx.FiloGunlukPuantajlar
            .Include(p => p.Arac).Include(p => p.Sofor).Include(p => p.Guzergah)
            .Where(p => p.FirmaId == firmaId
                && p.Tarih >= baslangic && p.Tarih <= bitis
                && !p.IsDeleted
                && p.Arac != null && p.Arac.TasimaTedarikciId == tedarikciId
                && p.TahakkukEdenTaseronUcreti > 0)
            .OrderBy(p => p.Tarih)
            .ToListAsync();

        detay.Puantajlar = puantajlar.Select(p => new MutabakatDetayPuantaj
        {
            Id = p.Id,
            Tarih = p.Tarih,
            AracPlaka = p.Arac?.AktifPlaka ?? "",
            Sofor = p.Sofor != null ? $"{p.Sofor.Ad} {p.Sofor.Soyad}" : "",
            Guzergah = p.Guzergah?.GuzergahAdi ?? "",
            SeferSayisi = p.SeferSayisi,
            Tutar = p.TahakkukEdenTaseronUcreti,
            Faturalandi = p.TaseronOdemeYapildiMi,
            FaturaId = p.TedarikciOdemeFaturaId
        }).ToList();

        if (ted.CariId.HasValue)
        {
            var faturalar = await ctx.Faturalar
                .Where(f => f.CariId == ted.CariId.Value && f.FaturaYonu == FaturaYonu.Gelen
                    && f.FaturaTarihi >= baslangic && f.FaturaTarihi <= bitis)
                .OrderBy(f => f.FaturaTarihi)
                .ToListAsync();
            var bagliFaturaIds = puantajlar.Where(p => p.TedarikciOdemeFaturaId.HasValue)
                .Select(p => p.TedarikciOdemeFaturaId!.Value).ToHashSet();
            detay.Faturalar = faturalar.Select(f => new MutabakatDetayFatura
            {
                Id = f.Id,
                Tarih = f.FaturaTarihi,
                FaturaNo = f.FaturaNo ?? "",
                Tutar = f.GenelToplam,
                PuantajaBagli = bagliFaturaIds.Contains(f.Id)
            }).ToList();
        }
        return detay;
    }
}
