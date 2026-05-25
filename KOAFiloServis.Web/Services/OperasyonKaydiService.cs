using KOAFiloServis.Shared.Entities;
using KOAFiloServis.Web.Data;
using KOAFiloServis.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KOAFiloServis.Web.Services;

/// <summary>
/// OperasyonKaydi veri erişim katmanı.
/// Validation → OperasyonKaydiValidator, Domain kuralları → OperasyonKaydiBusinessRules.
/// </summary>
public sealed class OperasyonKaydiService : IOperasyonKaydiService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly OperasyonKaydiBusinessRules _rules;

    public OperasyonKaydiService(IDbContextFactory<ApplicationDbContext> dbFactory,
                                 OperasyonKaydiBusinessRules rules)
    {
        _dbFactory = dbFactory;
        _rules = rules;
    }

    // ── Sorgulama ──────────────────────────────────────────────────────────

    public async Task<List<OperasyonKaydi>> GetByDateRangeAsync(DateTime baslangic, DateTime bitis, int? kurumId = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var query = db.OperasyonKayitlari
            .Include(o => o.Guzergah)
            .Include(o => o.Arac)
            .Include(o => o.Sofor)
            .Where(o => !o.IsDeleted && o.Tarih >= baslangic && o.Tarih <= bitis);

        if (kurumId.HasValue && kurumId.Value > 0)
        {
            var guzergahIds = await db.Guzergahlar
                .Where(g => !g.IsDeleted && g.KurumId == kurumId.Value)
                .Select(g => g.Id)
                .ToListAsync();
            query = query.Where(o => guzergahIds.Contains(o.GuzergahId));
        }

        return await query
            .OrderBy(o => o.Tarih)
            .ThenBy(o => o.Guzergah!.GuzergahAdi)
            .ToListAsync();
    }

    public async Task<List<OperasyonKaydi>> GetByAracGuzergahAsync(int aracId, int guzergahId, int yil, int ay)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.OperasyonKayitlari
            .Include(o => o.Guzergah)
            .Include(o => o.Arac)
            .Include(o => o.Sofor)
            .Where(o => !o.IsDeleted
                        && o.AracId == aracId
                        && o.GuzergahId == guzergahId
                        && o.Tarih.Year == yil
                        && o.Tarih.Month == ay)
            .OrderBy(o => o.Tarih)
            .ToListAsync();
    }

    public async Task<List<OperasyonKaydi>> GetByDonemAsync(int yil, int ay, int? kurumId = null)
    {
        var baslangic = new DateTime(yil, ay, 1);
        var bitis = baslangic.AddMonths(1).AddDays(-1);
        return await GetByDateRangeAsync(baslangic, bitis, kurumId);
    }

    public async Task<OperasyonKaydi?> GetByIdAsync(int id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.OperasyonKayitlari
            .Include(o => o.Guzergah)
            .Include(o => o.Arac)
            .Include(o => o.Sofor)
            .Include(o => o.PuantajKayit)
            .FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted);
    }

    // ── CRUD ────────────────────────────────────────────────────────────────

    public async Task<OperasyonKaydi> SaveAsync(OperasyonKaydi kayit)
    {
        // 1. Validation
        var validationErrors = OperasyonKaydiValidator.Validate(kayit);
        if (validationErrors.Any())
            throw new InvalidOperationException(string.Join("; ", validationErrors));

        // 2. Domain rules
        var ruleErrors = OperasyonKaydiBusinessRules.CheckOperationalRules(kayit);
        if (ruleErrors.Any())
            throw new InvalidOperationException(string.Join("; ", ruleErrors));

        await using var db = await _dbFactory.CreateDbContextAsync();

        // 3. Conflict check (DB-dependent domain rule)
        var conflictErrors = await _rules.CheckConflictsAsync(kayit);
        if (conflictErrors.Any())
            throw new InvalidOperationException(string.Join("; ", conflictErrors));

        // 4. Persist
        var mevcut = await db.OperasyonKayitlari
            .FirstOrDefaultAsync(o =>
                !o.IsDeleted &&
                o.Tarih == kayit.Tarih &&
                o.GuzergahId == kayit.GuzergahId &&
                o.AracId == kayit.AracId &&
                o.Slot == kayit.Slot);

        if (mevcut == null)
        {
            db.OperasyonKayitlari.Add(kayit);
        }
        else
        {
            ApplyUpdateFields(mevcut, kayit);
            kayit = mevcut;
        }

        await db.SaveChangesAsync();
        return kayit;
    }

    public async Task TopluSaveAsync(IEnumerable<OperasyonKaydi> kayitlar)
    {
        var list = kayitlar.ToList();
        if (!list.Any()) return;

        // 1. Validation
        var validationErrors = OperasyonKaydiValidator.ValidateToplu(list);
        if (validationErrors.Any())
            throw new InvalidOperationException(string.Join("; ", validationErrors));

        await using var db = await _dbFactory.CreateDbContextAsync();

        // 2. Data access
        var tarihler = list.Select(k => k.Tarih).Distinct().ToList();
        var minTarih = tarihler.Min();
        var maxTarih = tarihler.Max();

        var mevcutlar = await db.OperasyonKayitlari
            .Where(o => !o.IsDeleted && o.Tarih >= minTarih && o.Tarih <= maxTarih)
            .ToListAsync();

        foreach (var kayit in list)
        {
            var mevcut = mevcutlar.FirstOrDefault(m =>
                m.Tarih == kayit.Tarih &&
                m.GuzergahId == kayit.GuzergahId &&
                m.AracId == kayit.AracId &&
                m.Slot == kayit.Slot);

            if (mevcut == null)
            {
                db.OperasyonKayitlari.Add(kayit);
            }
            else
            {
                ApplyUpdateFields(mevcut, kayit);
            }
        }

        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id, string? deletedBy = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var kayit = await db.OperasyonKayitlari.FindAsync(id);
        if (kayit == null) return;

        kayit.IsDeleted = true;
        kayit.DeletedAt = DateTime.UtcNow;
        kayit.DeletedBy = deletedBy;
        kayit.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    // ── Şablon ──────────────────────────────────────────────────────────────

    public async Task<List<OperasyonKaydi>> SablonOlusturAsync(int kurumId, int yil, int ay)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var guzergahlar = await db.Guzergahlar
            .Include(g => g.VarsayilanArac)
            .Include(g => g.VarsayilanSofor)
            .Where(g => !g.IsDeleted && g.Aktif && g.KurumId == kurumId)
            .ToListAsync();

        var guzergahIds = guzergahlar.Select(g => g.Id).ToList();

        var eslestirmeler = await db.FiloGuzergahEslestirmeleri
            .Include(e => e.Arac)
            .Include(e => e.Sofor)
            .Where(e => guzergahIds.Contains(e.GuzergahId) && e.IsActive)
            .ToListAsync();

        var gunSayisi = DateTime.DaysInMonth(yil, ay);
        var baslangic = new DateTime(yil, ay, 1);
        var bitis = new DateTime(yil, ay, gunSayisi);

        var mevcutlar = await db.OperasyonKayitlari
            .Where(o => !o.IsDeleted && o.Tarih >= baslangic && o.Tarih <= bitis
                        && guzergahIds.Contains(o.GuzergahId))
            .ToListAsync();

        var sonuc = new List<OperasyonKaydi>(mevcutlar);

        foreach (var guzergah in guzergahlar)
        {
            var guzergahEslestirmeler = eslestirmeler
                .Where(e => e.GuzergahId == guzergah.Id).ToList();

            List<(int aracId, string? plaka, int? soforId, string? soforAdi)> aracList;

            if (guzergahEslestirmeler.Any())
            {
                aracList = guzergahEslestirmeler
                    .Select(e => (
                        e.AracId,
                        e.Arac?.AktifPlaka ?? e.Arac?.Plaka,
                        (int?)e.SoforId,
                        e.Sofor != null ? $"{e.Sofor.Ad} {e.Sofor.Soyad}" : null
                    )).ToList();
            }
            else if (guzergah.VarsayilanArac != null)
            {
                aracList = new List<(int, string?, int?, string?)>
                {
                    (guzergah.VarsayilanArac.Id,
                     guzergah.VarsayilanArac.AktifPlaka ?? guzergah.VarsayilanArac.Plaka,
                     guzergah.VarsayilanSoforId,
                     guzergah.VarsayilanSofor != null
                         ? $"{guzergah.VarsayilanSofor.Ad} {guzergah.VarsayilanSofor.Soyad}"
                         : null)
                };
            }
            else
            {
                continue;
            }

            var slotlar = guzergah.SeferTipi switch
            {
                SeferTipi.Sabah => new[] { SeferSlot.Sabah },
                SeferTipi.Aksam => new[] { SeferSlot.Aksam },
                SeferTipi.SabahAksam => new[] { SeferSlot.Sabah, SeferSlot.Aksam },
                SeferTipi.Mesai => new[] { SeferSlot.Mesai },
                _ => new[] { SeferSlot.Sabah }
            };

            foreach (var (aracId, plaka, soforId, soforAdi) in aracList)
            {
                foreach (var slot in slotlar)
                {
                    for (int gun = 1; gun <= gunSayisi; gun++)
                    {
                        var tarih = new DateTime(yil, ay, gun);
                        if (tarih.DayOfWeek == DayOfWeek.Sunday)
                            continue;

                        var zatenVar = mevcutlar.Any(m =>
                            m.Tarih == tarih && m.GuzergahId == guzergah.Id
                            && m.AracId == aracId && m.Slot == slot);

                        if (zatenVar) continue;

                        sonuc.Add(new OperasyonKaydi
                        {
                            Tarih = tarih,
                            GuzergahId = guzergah.Id,
                            Guzergah = guzergah,
                            AracId = aracId,
                            SoforId = soforId,
                            Slot = slot,
                            Yon = slot == SeferSlot.Sabah ? PuantajYon.Sabah
                                : slot == SeferSlot.Aksam ? PuantajYon.Aksam
                                : PuantajYon.SabahAksam,
                            KurumId = kurumId,
                            SeferSayisi = 1,
                            PuantajCarpani = 1.0m,
                            OperasyonDurumu = OperasyonDurumu.Gitti,
                            KaynakTipi = PlanlamaKaynakTipi.Kendi,
                            FinansYonu = PlanlamaFinansYonu.Giden,
                            SoforOdemeTipi = SoforOdemeTipi.Ozmal,
                            Kaynak = PuantajKaynak.Manuel,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }
            }
        }

        return sonuc.OrderBy(o => o.Tarih).ThenBy(o => o.GuzergahId).ToList();
    }

    // ── Migrasyon ───────────────────────────────────────────────────────────

    public async Task<int> ImportFromPuantajKayitAsync(int yil, int ay, int? kurumId = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var guzergahIds = kurumId.HasValue
            ? await db.Guzergahlar
                .Where(g => !g.IsDeleted && g.KurumId == kurumId.Value)
                .Select(g => g.Id)
                .ToListAsync()
            : null;

        var puantajKayitlari = await db.PuantajKayitlar
            .Where(p => !p.IsDeleted && p.Yil == yil && p.Ay == ay
                        && p.GuzergahId != null && p.AracId != null
                        && (guzergahIds == null || guzergahIds.Contains(p.GuzergahId!.Value)))
            .ToListAsync();

        var mevcutOps = await db.OperasyonKayitlari
            .Where(o => !o.IsDeleted && o.Tarih.Year == yil && o.Tarih.Month == ay)
            .ToListAsync();

        int eklenen = 0;

        foreach (var pk in puantajKayitlari)
        {
            for (int gun = 1; gun <= 31; gun++)
            {
                var gunDeger = pk.GetGunDeger(gun);
                if (gunDeger <= 0) continue;

                try
                {
                    var tarih = new DateTime(yil, ay, gun);
                    var mevcut = mevcutOps.FirstOrDefault(o =>
                        o.Tarih == tarih && o.GuzergahId == pk.GuzergahId
                        && o.AracId == pk.AracId && o.Slot == pk.Slot);

                    if (mevcut != null) continue;

                    db.OperasyonKayitlari.Add(new OperasyonKaydi
                    {
                        Tarih = tarih,
                        GuzergahId = pk.GuzergahId!.Value,
                        AracId = pk.AracId!.Value,
                        SoforId = pk.SoforId,
                        Slot = pk.Slot,
                        SlotAdi = pk.SlotAdi,
                        Yon = pk.Yon,
                        KurumId = pk.KurumId ?? pk.KurumCariId,
                        IsverenFirmaId = pk.IsverenFirmaId,
                        SeferSayisi = gunDeger,
                        PuantajCarpani = 1.0m,
                        OperasyonDurumu = OperasyonDurumu.Gitti,
                        KaynakTipi = pk.KaynakTipi,
                        FinansYonu = pk.FinansYonu,
                        SoforOdemeTipi = pk.SoforOdemeTipi,
                        OdemeYapilacakCariId = pk.OdemeYapilacakCariId,
                        FaturaKesiciCariId = pk.FaturaKesiciCariId,
                        BelgeNo = pk.BelgeNo,
                        TransferDurum = pk.TransferDurum,
                        Kaynak = pk.Kaynak,
                        ExcelImportId = pk.ExcelImportId,
                        ExcelSatirNo = pk.ExcelSatirNo,
                        Notlar = pk.Notlar,
                        CreatedAt = DateTime.UtcNow
                    });

                    mevcutOps.Add(new OperasyonKaydi
                    {
                        Tarih = tarih, GuzergahId = pk.GuzergahId!.Value,
                        AracId = pk.AracId!.Value, Slot = pk.Slot
                    });
                    eklenen++;
                }
                catch
                {
                    // Geçersiz tarih (31 Şubat vb.) - atla
                }
            }
        }

        await db.SaveChangesAsync();
        return eklenen;
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static void ApplyUpdateFields(OperasyonKaydi mevcut, OperasyonKaydi kayit)
    {
        mevcut.SoforId = kayit.SoforId;
        mevcut.SlotAdi = kayit.SlotAdi;
        mevcut.Yon = kayit.Yon;
        mevcut.KurumId = kayit.KurumId;
        mevcut.IsverenFirmaId = kayit.IsverenFirmaId;
        mevcut.SeferSayisi = kayit.SeferSayisi;
        mevcut.PuantajCarpani = kayit.PuantajCarpani;
        mevcut.OperasyonDurumu = kayit.OperasyonDurumu;
        mevcut.KaynakTipi = kayit.KaynakTipi;
        mevcut.FinansYonu = kayit.FinansYonu;
        mevcut.SoforOdemeTipi = kayit.SoforOdemeTipi;
        mevcut.OdemeYapilacakCariId = kayit.OdemeYapilacakCariId;
        mevcut.FaturaKesiciCariId = kayit.FaturaKesiciCariId;
        mevcut.BelgeNo = kayit.BelgeNo;
        mevcut.TransferDurum = kayit.TransferDurum;
        mevcut.Notlar = kayit.Notlar;
        mevcut.UpdatedAt = DateTime.UtcNow;
        mevcut.UpdatedBy = kayit.UpdatedBy;

        if (mevcut.Islendi)
        {
            mevcut.Islendi = false;
            mevcut.IslenmeTarihi = null;
        }
    }
}
