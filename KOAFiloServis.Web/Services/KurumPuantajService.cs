using KOAFiloServis.Shared.Entities;
using KOAFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace KOAFiloServis.Web.Services;

public sealed class KurumPuantajService : IKurumPuantajService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;

    public KurumPuantajService(IDbContextFactory<ApplicationDbContext> dbFactory)
        => _dbFactory = dbFactory;

    // ── Kurum / Güzergah / Araç ───────────────────────────────────────────────

    public async Task<List<Kurum>> GetAktifKurumlarAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Kurumlar
            .Where(k => !k.IsDeleted && k.Aktif)
            .OrderBy(k => k.KurumAdi)
            .ToListAsync();
    }

    public async Task<List<Guzergah>> GetKurumGuzergahlariAsync(int kurumId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Guzergahlar
            .Include(g => g.VarsayilanArac)
            .Include(g => g.VarsayilanSofor)
            .Where(g => !g.IsDeleted && g.Aktif && g.KurumId == kurumId)
            .OrderBy(g => g.GuzergahAdi)
            .ToListAsync();
    }

    public async Task<List<AracGuzergahSatiri>> GetGuzergahAraclariAsync(int guzergahId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        // FiloGuzergahEslestirme tablosundan atanmış araçlar
        var eslestirmeler = await db.FiloGuzergahEslestirmeleri
            .Include(e => e.Arac)
            .Include(e => e.Sofor)
            .Where(e => e.GuzergahId == guzergahId && e.IsActive)
            .ToListAsync();

        var sonuc = eslestirmeler.Select(e => new AracGuzergahSatiri
        {
            AracId                = e.AracId,
            Plaka                 = e.Arac?.AktifPlaka ?? e.Arac?.Plaka ?? "-",
            SoforId               = e.SoforId,
            SoforAdi              = e.Sofor != null ? $"{e.Sofor.Ad} {e.Sofor.Soyad}" : null,
            KurumaKesilecekUcret  = e.KurumaKesilecekUcret,
            TaseronaOdenenUcret   = e.TaseronaOdenenUcret,
            EslestirmeId          = e.Id
        }).ToList();

        // Eşleştirmede yoksa güzergahın varsayılan aracını ekle
        if (!sonuc.Any())
        {
            var guzergah = await db.Guzergahlar
                .Include(g => g.VarsayilanArac)
                .Include(g => g.VarsayilanSofor)
                .FirstOrDefaultAsync(g => g.Id == guzergahId && !g.IsDeleted);

            if (guzergah?.VarsayilanArac != null)
            {
                sonuc.Add(new AracGuzergahSatiri
                {
                    AracId               = guzergah.VarsayilanArac.Id,
                    Plaka                = guzergah.VarsayilanArac.AktifPlaka ?? guzergah.VarsayilanArac.Plaka,
                    SoforId              = guzergah.VarsayilanSoforId,
                    SoforAdi             = guzergah.VarsayilanSofor != null
                                               ? $"{guzergah.VarsayilanSofor.Ad} {guzergah.VarsayilanSofor.Soyad}"
                                               : null,
                    KurumaKesilecekUcret = guzergah.BirimFiyat,
                    TaseronaOdenenUcret  = 0,
                    EslestirmeId         = null
                });
            }
        }

        return sonuc.OrderBy(s => s.Plaka).ToList();
    }

    // ── PuantajKayit CRUD ─────────────────────────────────────────────────────

    public async Task<List<PuantajKayit>> GetPuantajlarAsync(int yil, int ay, int kurumId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        // KurumId → Güzergahların KurumId alanından filtrele
        var guzergahIds = await db.Guzergahlar
            .Where(g => !g.IsDeleted && g.KurumId == kurumId)
            .Select(g => g.Id)
            .ToListAsync();

        return await db.PuantajKayitlar
            .Include(p => p.Guzergah)
            .Include(p => p.Arac)
            .Include(p => p.Sofor)
            .Where(p => !p.IsDeleted
                        && p.Yil == yil
                        && p.Ay == ay
                        && p.GuzergahId != null
                        && guzergahIds.Contains(p.GuzergahId!.Value))
            .OrderBy(p => p.Guzergah!.GuzergahAdi)
            .ThenBy(p => p.Plaka)
            .ToListAsync();
    }

    public async Task<PuantajKayit?> GetPuantajByIdAsync(int id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.PuantajKayitlar
            .Include(p => p.Guzergah)
            .Include(p => p.Arac)
            .Include(p => p.Sofor)
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
    }

    public async Task<PuantajKayit> SavePuantajAsync(PuantajKayit kayit)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        // Upsert: Güzergah + Araç + Yıl + Ay + Slot kombinasyonu unique
        var mevcut = await db.PuantajKayitlar
            .FirstOrDefaultAsync(p =>
                !p.IsDeleted &&
                p.GuzergahId == kayit.GuzergahId &&
                p.AracId     == kayit.AracId &&
                p.Yil        == kayit.Yil &&
                p.Ay         == kayit.Ay &&
                p.Slot       == kayit.Slot);

        if (mevcut == null)
        {
            db.PuantajKayitlar.Add(kayit);
        }
        else
        {
            // Gün değerlerini kopyala
            for (int g = 1; g <= 31; g++)
                mevcut.SetGunDeger(g, kayit.GetGunDeger(g));

            mevcut.SoforId      = kayit.SoforId;
            mevcut.SoforAdi     = kayit.SoforAdi;
            mevcut.Plaka        = kayit.Plaka;
            mevcut.Slot         = kayit.Slot;
            mevcut.Yon          = kayit.Yon;
            mevcut.Gun          = kayit.Gun;
            mevcut.SeferSayisi  = kayit.SeferSayisi;
            mevcut.KurumId      = kayit.KurumId;
            mevcut.IsverenFirmaId = kayit.IsverenFirmaId;
            mevcut.UpdatedAt    = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();
        return mevcut ?? kayit;
    }

    public async Task TopluSavePuantajAsync(IEnumerable<PuantajKayit> kayitlar)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync();

        foreach (var kayit in kayitlar)
        {
            var mevcut = await db.PuantajKayitlar
                .FirstOrDefaultAsync(p =>
                    !p.IsDeleted &&
                    p.GuzergahId == kayit.GuzergahId &&
                    p.AracId     == kayit.AracId &&
                    p.Yil        == kayit.Yil &&
                    p.Ay         == kayit.Ay &&
                    p.Slot       == kayit.Slot);

            if (mevcut == null)
            {
                db.PuantajKayitlar.Add(kayit);
            }
            else
            {
                for (int g = 1; g <= 31; g++)
                    mevcut.SetGunDeger(g, kayit.GetGunDeger(g));

                mevcut.SoforId      = kayit.SoforId;
                mevcut.SoforAdi     = kayit.SoforAdi;
                mevcut.Plaka        = kayit.Plaka;
                mevcut.Slot         = kayit.Slot;
                mevcut.Yon          = kayit.Yon;
                mevcut.Gun          = kayit.Gun;
                mevcut.SeferSayisi  = kayit.SeferSayisi;
                mevcut.KurumId      = kayit.KurumId;
                mevcut.IsverenFirmaId = kayit.IsverenFirmaId;
                mevcut.UpdatedAt    = DateTime.UtcNow;
            }
        }

        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }

    public async Task DeletePuantajAsync(int id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var kayit = await db.PuantajKayitlar.FindAsync(id);
        if (kayit == null) return;
        kayit.IsDeleted  = true;
        kayit.UpdatedAt  = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    // ── Şablon oluşturma ─────────────────────────────────────────────────────

    public async Task<List<PuantajKayit>> SablonOlusturAsync(int kurumId, int yil, int ay)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var guzergahlar = await db.Guzergahlar
            .Include(g => g.VarsayilanArac)
            .Include(g => g.VarsayilanSofor)
            .Where(g => !g.IsDeleted && g.Aktif && g.KurumId == kurumId)
            .ToListAsync();

        var guzergahIds = guzergahlar.Select(g => g.Id).ToList();

        // Mevcut kayıtları al
        var mevcutlar = await db.PuantajKayitlar
            .Where(p => !p.IsDeleted && p.Yil == yil && p.Ay == ay
                        && p.GuzergahId != null && guzergahIds.Contains(p.GuzergahId!.Value))
            .ToListAsync();

        var sonuc = new List<PuantajKayit>(mevcutlar);

        foreach (var guzergah in guzergahlar)
        {
            // FiloGuzergahEslestirme'den araçlar
            var eslestirmeler = await db.FiloGuzergahEslestirmeleri
                .Include(e => e.Arac)
                .Include(e => e.Sofor)
                .Where(e => e.GuzergahId == guzergah.Id && e.IsActive)
                .ToListAsync();

            if (!eslestirmeler.Any() && guzergah.VarsayilanArac != null)
            {
                // Varsayılan araçla, her slot için satır
                foreach (SeferSlot slot in Enum.GetValues<SeferSlot>())
                {
                    EkleEksikSatir(sonuc, mevcutlar, guzergah, guzergah.VarsayilanArac.Id,
                        guzergah.VarsayilanArac.AktifPlaka ?? guzergah.VarsayilanArac.Plaka,
                        guzergah.VarsayilanSoforId,
                        guzergah.VarsayilanSofor != null ? $"{guzergah.VarsayilanSofor.Ad} {guzergah.VarsayilanSofor.Soyad}" : null,
                        yil, ay, slot);
                }
            }
            else
            {
                foreach (var e in eslestirmeler)
                {
                    var soforAdi = e.Sofor != null ? $"{e.Sofor.Ad} {e.Sofor.Soyad}" : null;
                    foreach (SeferSlot slot in Enum.GetValues<SeferSlot>())
                    {
                        EkleEksikSatir(sonuc, mevcutlar, guzergah, e.AracId,
                            e.Arac?.AktifPlaka ?? e.Arac?.Plaka,
                            e.SoforId, soforAdi, yil, ay, slot);
                    }
                }
            }
        }

        return sonuc.OrderBy(p => p.GuzergahId).ThenBy(p => p.Plaka).ThenBy(p => p.Slot).ToList();
    }

    private static void EkleEksikSatir(
        List<PuantajKayit> sonuc,
        List<PuantajKayit> mevcutlar,
        Guzergah guzergah,
        int aracId, string? plaka,
        int? soforId, string? soforAdi,
        int yil, int ay,
        SeferSlot slot = SeferSlot.Sabah)
    {
        var varMi = mevcutlar.Any(p => p.GuzergahId == guzergah.Id && p.AracId == aracId && p.Slot == slot);
        if (varMi) return;

        sonuc.Add(new PuantajKayit
        {
            GuzergahId  = guzergah.Id,
            Guzergah    = guzergah,
            AracId      = aracId,
            Plaka       = plaka ?? string.Empty,
            SoforId     = soforId,
            SoforAdi    = soforAdi,
            Yil         = yil,
            Ay          = ay,
            Slot        = slot,
            Yon         = guzergah.SeferTipi switch
            {
                SeferTipi.Sabah      => PuantajYon.Sabah,
                SeferTipi.Aksam      => PuantajYon.Aksam,
                SeferTipi.SabahAksam => PuantajYon.SabahAksam,
                _                    => PuantajYon.SabahAksam
            },
            SeferSayisi = 1,
            Gun         = 0
        });
    }
}
