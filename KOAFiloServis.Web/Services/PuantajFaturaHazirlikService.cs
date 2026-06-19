using KOAFiloServis.Shared.Entities;
using KOAFiloServis.Web.Data;
using KOAFiloServis.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KOAFiloServis.Web.Services;

/// <summary>
/// PuantajKayit (B1) → PuantajFaturaHazirlik + PuantajFaturaHazirlikSatir dönüşümü.
/// Manuel düzeltme, onay ve faturaya bağlama işlemlerini yönetir.
/// </summary>
public class PuantajFaturaHazirlikService : IPuantajFaturaHazirlikService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;

    public PuantajFaturaHazirlikService(IDbContextFactory<ApplicationDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    // ══════════════════════════════════════════════
    // CRUD
    // ══════════════════════════════════════════════

    public async Task<PuantajFaturaHazirlik?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await db.PuantajFaturaHazirliklar
            .Include(x => x.Satirlar)
            .Where(x => x.Id == id && !x.IsDeleted)
            .AsNoTracking()
            .FirstOrDefaultAsync(ct);
    }

    public async Task<List<PuantajFaturaHazirlik>> GetByDonemAsync(int yil, int ay, int? kurumId = null, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var query = db.PuantajFaturaHazirliklar
            .Where(x => x.Yil == yil && x.Ay == ay && !x.IsDeleted);

        if (kurumId.HasValue)
            query = query.Where(x => x.KurumId == kurumId.Value);

        return await query.OrderByDescending(x => x.CreatedAt)
            .AsNoTracking().ToListAsync(ct);
    }

    public async Task<PuantajFaturaHazirlik> CreateAsync(PuantajFaturaHazirlik hazirlik, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        hazirlik.CreatedAt = DateTime.UtcNow;
        hazirlik.Durum = PuantajFaturaHazirlikDurum.Taslak;

        // PuantajKayit'tan satırları oluştur
        var pkQuery = db.PuantajKayitlar
            .Where(x => x.Yil == hazirlik.Yil && x.Ay == hazirlik.Ay
                && x.OnayDurum == PuantajOnayDurum.Onaylandi
                && !x.IsDeleted);

        if (hazirlik.KurumId.HasValue)
            pkQuery = pkQuery.Where(x => x.KurumId == hazirlik.KurumId.Value);

        // Yön filtresi
        if (hazirlik.FaturaYonu == PuantajFaturaYonu.Gelir)
            pkQuery = pkQuery.Where(x => x.ToplamGelir > 0);
        else if (hazirlik.FaturaYonu == PuantajFaturaYonu.Gider)
            pkQuery = pkQuery.Where(x => x.ToplamGider > 0);

        var pkKayitlar = await pkQuery
            .Include(x => x.Kurum).Include(x => x.Guzergah).Include(x => x.Arac)
            .Include(x => x.Sofor).Include(x => x.FaturaKesiciCari).Include(x => x.OdemeYapilacakCari)
            .AsNoTracking()
            .ToListAsync(ct);

        foreach (var pk in pkKayitlar)
        {
            var satir = MapPuantajKayitToSatir(pk, hazirlik.FaturaYonu);
            satir.HazirlikId = hazirlik.Id;
            hazirlik.Satirlar.Add(satir);
        }

        // PuantajIstisna kayıtlarını yükle ve satırlara yansıt
        var pkIds = pkKayitlar.Select(p => p.Id).ToList();
        var istisnalar = await db.PuantajIstisnalar
            .Where(i => pkIds.Contains(i.PuantajKayitId) && !i.IsDeleted)
            .ToListAsync(ct);

        foreach (var satir in hazirlik.Satirlar)
        {
            var satirIstisnalar = istisnalar.Where(i => i.PuantajKayitId == satir.PuantajKayitId).ToList();
            if (!satirIstisnalar.Any()) continue;

            satir.EkGelir = satirIstisnalar
                .Where(i => i.IstisnaTipi == IstisnaTipi.EkSefer && i.KararTipi == KararTipi.Masraf && hazirlik.FaturaYonu == PuantajFaturaYonu.Gelir)
                .Sum(i => i.Tutar);
            satir.EkGider = satirIstisnalar
                .Where(i => i.IstisnaTipi == IstisnaTipi.EkSefer && i.KararTipi == KararTipi.Masraf && hazirlik.FaturaYonu == PuantajFaturaYonu.Gider)
                .Sum(i => i.Tutar);
            satir.CezaTutar = satirIstisnalar
                .Where(i => i.KararTipi == KararTipi.Ceza).Sum(i => i.Tutar);
            satir.MasrafTutar = satirIstisnalar
                .Where(i => i.KararTipi == KararTipi.Masraf && i.IstisnaTipi != IstisnaTipi.EkSefer)
                .Sum(i => i.Tutar);

            // Toplamlara yansıt
            satir.TahsilEdilecek += satir.EkGelir - satir.CezaTutar;
            satir.Odenecek += satir.EkGider + satir.MasrafTutar;
        }

        // Özeti hesapla
        HesaplaOzet(hazirlik);

        db.PuantajFaturaHazirliklar.Add(hazirlik);
        await db.SaveChangesAsync(ct);
        return hazirlik;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var hazirlik = await db.PuantajFaturaHazirliklar
            .Include(x => x.Satirlar)
            .Where(x => x.Id == id && !x.IsDeleted)
            .FirstOrDefaultAsync(ct);

        if (hazirlik == null) return false;

        // Sadece Taslak durumunda silinebilir
        if (hazirlik.Durum != PuantajFaturaHazirlikDurum.Taslak)
            throw new InvalidOperationException("Sadece taslak hazırlıklar silinebilir.");

        hazirlik.IsDeleted = true;
        hazirlik.DeletedAt = DateTime.UtcNow;
        foreach (var satir in hazirlik.Satirlar)
        {
            satir.IsDeleted = true;
            satir.DeletedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(ct);
        return true;
    }

    // ══════════════════════════════════════════════
    // SATIR YÖNETİMİ
    // ══════════════════════════════════════════════

    public async Task<List<PuantajFaturaHazirlikSatir>> GetSatirlarAsync(int hazirlikId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await db.PuantajFaturaHazirlikSatirlar
            .Where(x => x.HazirlikId == hazirlikId && !x.IsDeleted)
            .OrderBy(x => x.Id)
            .AsNoTracking().ToListAsync(ct);
    }

    public async Task<PuantajFaturaHazirlikSatir> ManuelSatirEkleAsync(int hazirlikId, PuantajFaturaHazirlikSatir satir, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var hazirlik = await db.PuantajFaturaHazirliklar
            .Where(x => x.Id == hazirlikId && !x.IsDeleted && x.Durum == PuantajFaturaHazirlikDurum.Taslak)
            .FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException("Hazırlık bulunamadı veya onaylanmış.");

        satir.HazirlikId = hazirlikId;
        satir.FirmaId = hazirlik.FirmaId;
        satir.ManuelDuzeltmeMi = true;
        satir.PuantajKayitId = null;
        satir.CreatedAt = DateTime.UtcNow;

        db.PuantajFaturaHazirlikSatirlar.Add(satir);
        await db.SaveChangesAsync(ct);

        await HesaplaToplamlarAsync(hazirlikId, ct);
        return satir;
    }

    public async Task<bool> SatirSilAsync(int satirId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var satir = await db.PuantajFaturaHazirlikSatirlar
            .Where(x => x.Id == satirId && !x.IsDeleted)
            .FirstOrDefaultAsync(ct);

        if (satir == null) return false;

        // Bağlı olduğu hazırlık onaylı değilse silinebilir
        var hazirlik = await db.PuantajFaturaHazirliklar
            .Where(x => x.Id == satir.HazirlikId && x.Durum == PuantajFaturaHazirlikDurum.Taslak)
            .FirstOrDefaultAsync(ct);

        if (hazirlik == null)
            throw new InvalidOperationException("Onaylanmış hazırlıktan satır silinemez.");

        satir.IsDeleted = true;
        satir.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        await HesaplaToplamlarAsync(satir.HazirlikId, ct);
        return true;
    }

    public async Task<bool> SatirGuncelleAsync(PuantajFaturaHazirlikSatir satir, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var existing = await db.PuantajFaturaHazirlikSatirlar
            .Where(x => x.Id == satir.Id && !x.IsDeleted)
            .FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException("Satır bulunamadı.");

        var hazirlik = await db.PuantajFaturaHazirliklar
            .Where(x => x.Id == existing.HazirlikId && x.Durum == PuantajFaturaHazirlikDurum.Taslak)
            .FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException("Onaylanmış hazırlıkta güncelleme yapılamaz.");

        // Finansal alanları güncelle
        existing.BirimGelir = satir.BirimGelir;
        existing.ToplamGelir = satir.ToplamGelir;
        existing.BirimGider = satir.BirimGider;
        existing.ToplamGider = satir.ToplamGider;
        existing.Kdv10Tutar = satir.Kdv10Tutar;
        existing.Kdv20Tutar = satir.Kdv20Tutar;
        existing.KesintiTutar = satir.KesintiTutar;
        existing.TahsilEdilecek = satir.TahsilEdilecek;
        existing.Odenecek = satir.Odenecek;
        existing.Gun01 = satir.Gun01; existing.Gun02 = satir.Gun02; existing.Gun03 = satir.Gun03;
        existing.Gun04 = satir.Gun04; existing.Gun05 = satir.Gun05; existing.Gun06 = satir.Gun06;
        existing.Gun07 = satir.Gun07; existing.Gun08 = satir.Gun08; existing.Gun09 = satir.Gun09;
        existing.Gun10 = satir.Gun10; existing.Gun11 = satir.Gun11; existing.Gun12 = satir.Gun12;
        existing.Gun13 = satir.Gun13; existing.Gun14 = satir.Gun14; existing.Gun15 = satir.Gun15;
        existing.Gun16 = satir.Gun16; existing.Gun17 = satir.Gun17; existing.Gun18 = satir.Gun18;
        existing.Gun19 = satir.Gun19; existing.Gun20 = satir.Gun20; existing.Gun21 = satir.Gun21;
        existing.Gun22 = satir.Gun22; existing.Gun23 = satir.Gun23; existing.Gun24 = satir.Gun24;
        existing.Gun25 = satir.Gun25; existing.Gun26 = satir.Gun26; existing.Gun27 = satir.Gun27;
        existing.Gun28 = satir.Gun28; existing.Gun29 = satir.Gun29; existing.Gun30 = satir.Gun30;
        existing.Gun31 = satir.Gun31;
        existing.ToplamGun = satir.Gun01 + satir.Gun02 + satir.Gun03 + satir.Gun04 + satir.Gun05
            + satir.Gun06 + satir.Gun07 + satir.Gun08 + satir.Gun09 + satir.Gun10
            + satir.Gun11 + satir.Gun12 + satir.Gun13 + satir.Gun14 + satir.Gun15
            + satir.Gun16 + satir.Gun17 + satir.Gun18 + satir.Gun19 + satir.Gun20
            + satir.Gun21 + satir.Gun22 + satir.Gun23 + satir.Gun24 + satir.Gun25
            + satir.Gun26 + satir.Gun27 + satir.Gun28 + satir.Gun29 + satir.Gun30 + satir.Gun31;
        existing.ToplamSefer = existing.ToplamGun;
        existing.DuzeltmeAciklamasi = satir.DuzeltmeAciklamasi;
        existing.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        await HesaplaToplamlarAsync(existing.HazirlikId, ct);
        return true;
    }

    // ══════════════════════════════════════════════
    // WORKFLOW
    // ══════════════════════════════════════════════

    public async Task<PuantajFaturaHazirlik> OnaylaAsync(int id, string kullanici, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var hazirlik = await db.PuantajFaturaHazirliklar
            .Where(x => x.Id == id && !x.IsDeleted && x.Durum == PuantajFaturaHazirlikDurum.Taslak)
            .FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException("Hazırlık bulunamadı veya zaten onaylanmış.");

        hazirlik.Durum = PuantajFaturaHazirlikDurum.Onaylandi;
        hazirlik.OnaylayanKullanici = kullanici;
        hazirlik.OnayTarihi = DateTime.UtcNow;
        hazirlik.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return hazirlik;
    }

    public async Task<PuantajFaturaHazirlik> FaturalastiAsync(int id, int? faturaId = null, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var hazirlik = await db.PuantajFaturaHazirliklar
            .Include(x => x.Satirlar)
            .Where(x => x.Id == id && !x.IsDeleted && x.Durum == PuantajFaturaHazirlikDurum.Onaylandi)
            .FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException("Hazırlık bulunamadı veya onaylanmamış.");

        hazirlik.Durum = PuantajFaturaHazirlikDurum.Faturalasti;
        hazirlik.FaturaId = faturaId;
        hazirlik.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return hazirlik;
    }

    // ══════════════════════════════════════════════
    // HESAPLAMA
    // ══════════════════════════════════════════════

    public async Task HesaplaToplamlarAsync(int hazirlikId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var hazirlik = await db.PuantajFaturaHazirliklar
            .Include(x => x.Satirlar)
            .Where(x => x.Id == hazirlikId && !x.IsDeleted)
            .FirstOrDefaultAsync(ct);

        if (hazirlik == null) return;

        HesaplaOzet(hazirlik);
        await db.SaveChangesAsync(ct);
    }

    // ══════════════════════════════════════════════
    // HELPER
    // ══════════════════════════════════════════════

    private static void HesaplaOzet(PuantajFaturaHazirlik h)
    {
        var satirlar = h.Satirlar.Where(s => !s.IsDeleted).ToList();
        h.SatirSayisi = satirlar.Count;
        h.ToplamSefer = satirlar.Sum(s => s.ToplamSefer);
        h.ToplamGelir = satirlar.Sum(s => s.ToplamGelir + s.EkGelir);
        h.ToplamGider = satirlar.Sum(s => s.ToplamGider + s.EkGider + s.MasrafTutar);
        h.ToplamKdv = satirlar.Sum(s => s.Kdv10Tutar + s.Kdv20Tutar);
        h.ToplamKesinti = satirlar.Sum(s => s.KesintiTutar + s.CezaTutar);

        if (h.FaturaYonu == PuantajFaturaYonu.Gelir)
            h.NetTutar = h.ToplamGelir - h.ToplamKdv - h.ToplamKesinti;
        else
            h.NetTutar = h.ToplamGider - h.ToplamKdv - h.ToplamKesinti;
    }

    private static PuantajFaturaHazirlikSatir MapPuantajKayitToSatir(PuantajKayit k, PuantajFaturaYonu yon)
    {
        return new PuantajFaturaHazirlikSatir
        {
            FirmaId = null, // PuantajKayit'ta FirmaId yok, hazirlik uzerinden inherit edilir
            PuantajKayitId = k.Id,
            KurumId = k.KurumId,
            CariId = yon == PuantajFaturaYonu.Gelir ? k.FaturaKesiciCariId : k.OdemeYapilacakCariId,
            AracId = k.AracId,
            GuzergahId = k.GuzergahId,
            SoforId = k.SoforId,
            Plaka = k.Arac?.AktifPlaka ?? k.Plaka,
            SoforAdi = k.Sofor != null ? $"{k.Sofor.Ad} {k.Sofor.Soyad}" : k.SoforAdi,
            Telefon = k.FaturaKesiciTelefon ?? k.FaturaKesiciCari?.Telefon ?? k.OdemeYapilacakCari?.Telefon,
            CariUnvan = k.FaturaKesiciCari?.Unvan ?? k.OdemeYapilacakCari?.Unvan ?? k.FaturaKesiciAdi,
            GuzergahAdi = k.Guzergah?.GuzergahAdi ?? k.GuzergahAdi,

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
            ToplamGun = (int)k.Gun,
            ToplamSefer = (int)k.Gun,

            BirimGelir = k.BirimGelir,
            ToplamGelir = k.ToplamGelir,
            BirimGider = k.BirimGider,
            ToplamGider = k.ToplamGider,
            Kdv10Tutar = k.GelirKdv10Tutari + k.GiderKdv10Tutari,
            Kdv20Tutar = k.GelirKdv20Tutari + k.GiderKdv20Tutari + k.GelirKdvTutari,
            KesintiTutar = k.GelirKesinti + k.GiderKesinti,
            TahsilEdilecek = k.Alinacak,
            Odenecek = k.Odenecek,

            // İstisna özetleri — varsayılan 0, CreateAsync'te istisnalardan doldurulur
            EkGelir = 0,
            EkGider = 0,
            CezaTutar = 0,
            MasrafTutar = 0,
        };
    }
}
