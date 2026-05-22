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

    public async Task<List<PuantajKayit>> GetPuantajlarAsync(int yil, int ay, int? kurumId = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        // KurumId → Güzergahların KurumId alanından filtrele (null = tum kurumlar)
        var guzergahQuery = db.Guzergahlar.Where(g => !g.IsDeleted);
        if (kurumId.HasValue && kurumId.Value > 0)
            guzergahQuery = guzergahQuery.Where(g => g.KurumId == kurumId.Value);
        var guzergahIds = await guzergahQuery.Select(g => g.Id).ToListAsync();

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
            mevcut.KaynakTipi   = kayit.KaynakTipi;
            mevcut.FinansYonu   = kayit.FinansYonu;
            mevcut.KurumId      = kayit.KurumId;
            mevcut.IsverenFirmaId = kayit.IsverenFirmaId;
            mevcut.BelgeNo      = kayit.BelgeNo;
            mevcut.TransferDurum = kayit.TransferDurum;
            mevcut.UpdatedAt    = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();
        return mevcut ?? kayit;
    }

    public async Task TopluSavePuantajAsync(IEnumerable<PuantajKayit> kayitlar)
    {
        var kayitList = kayitlar.ToList();
        if (!kayitList.Any()) return;

        await using var db = await _dbFactory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync();

        // Ayni donemdeki tum mevcut kayitlari TEK sorguda yukle
        var yil = kayitList[0].Yil;
        var ay = kayitList[0].Ay;
        var mevcutKayitlar = await db.PuantajKayitlar
            .Where(p => !p.IsDeleted && p.Yil == yil && p.Ay == ay)
            .ToListAsync();

        foreach (var kayit in kayitList)
        {
            var mevcut = mevcutKayitlar.FirstOrDefault(m =>
                m.GuzergahId == kayit.GuzergahId &&
                m.AracId     == kayit.AracId &&
                m.Slot       == kayit.Slot);

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
                mevcut.KaynakTipi   = kayit.KaynakTipi;
                mevcut.FinansYonu   = kayit.FinansYonu;
                mevcut.KurumId      = kayit.KurumId;
                mevcut.IsverenFirmaId = kayit.IsverenFirmaId;
                mevcut.BelgeNo      = kayit.BelgeNo;
                mevcut.TransferDurum = kayit.TransferDurum;
                mevcut.UpdatedAt    = DateTime.UtcNow;
            }
        }

        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }

    // ── Çakışma Kontrolü ─────────────────────────────────────────────────────

    public async Task<ConflictResult> CheckKayitConflictsAsync(PuantajKayit kayit)
        => await CheckConflictsAsync(new List<PuantajKayit> { kayit });

    public async Task<ConflictResult> CheckConflictsAsync(List<PuantajKayit> kayitlar)
    {
        var result = new ConflictResult();
        if (kayitlar.Count == 0) return result;

        var yil = kayitlar[0].Yil;
        var ay = kayitlar[0].Ay;

        await using var db = await _dbFactory.CreateDbContextAsync();

        // Aynı dönemdeki tüm kayıtları al (çakışma karşılaştırması için)
        var mevcutTumKayitlar = await db.PuantajKayitlar
            .Where(p => !p.IsDeleted && p.Yil == yil && p.Ay == ay)
            .ToListAsync();

        // Tüm kayıtların (mevcut + yeni) birleşik listesi
        var tumKayitlar = mevcutTumKayitlar
            .Where(m => !kayitlar.Any(k => k.Id == m.Id))
            .Concat(kayitlar)
            .ToList();

        // Guzergah kapasite haritası (Kapasite kuralı için)
        var guzergahIds = tumKayitlar
            .Select(k => k.GuzergahId).Where(id => id.HasValue).Select(id => id!.Value)
            .Distinct().ToList();
        var kapasiteMap = await db.Guzergahlar
            .Where(g => guzergahIds.Contains(g.Id) && g.PersonelSayisi > 0)
            .ToDictionaryAsync(g => g.Id, g => g.PersonelSayisi);

        foreach (var kayit in kayitlar)
        {
            if (kayit.GuzergahId == null || kayit.AracId == null) continue;

            for (int gun = 1; gun <= 31; gun++)
            {
                var gunDeger = kayit.GetGunDeger(gun);
                if (gunDeger == 0) continue;

                // Kural 1: Aynı gün + aynı güzergah + aynı slot → tek araç
                var ayniGuzergahSlot = mevcutTumKayitlar
                    .Where(p => p.Id != kayit.Id
                        && p.GuzergahId == kayit.GuzergahId
                        && p.Slot == kayit.Slot
                        && p.AracId != kayit.AracId
                        && p.GetGunDeger(gun) > 0)
                    .ToList();

                foreach (var cakisan in ayniGuzergahSlot)
                {
                    result.Conflicts.Add(new ConflictItem
                    {
                        Severity = ConflictSeverity.Blocking,
                        Kural = "Tek Arac",
                        Mesaj = $"Gun {gun}: {kayit.Slot} slotunda '{cakisan.Guzergah?.GuzergahAdi ?? cakisan.GuzergahAdi}' guzergahinda zaten {cakisan.Plaka ?? cakisan.Arac?.Plaka} araci atanmis.",
                        Gun = gun,
                        Slot = kayit.Slot,
                        EtkilenenKayitId = cakisan.Id,
                        EtkilenenAciklama = $"{cakisan.Plaka} / {cakisan.SoforAdi}"
                    });
                }

                // Kural 2: Aynı gün + aynı slot → aynı araç tek güzergah
                var ayniAracSlot = mevcutTumKayitlar
                    .Where(p => p.Id != kayit.Id
                        && p.AracId == kayit.AracId
                        && p.Slot == kayit.Slot
                        && p.GuzergahId != kayit.GuzergahId
                        && p.GetGunDeger(gun) > 0)
                    .ToList();

                foreach (var cakisan in ayniAracSlot)
                {
                    result.Conflicts.Add(new ConflictItem
                    {
                        Severity = ConflictSeverity.Blocking,
                        Kural = "Tek Guzergah",
                        Mesaj = $"Gun {gun}: {kayit.Slot} slotunda '{cakisan.Plaka}' araci zaten '{cakisan.Guzergah?.GuzergahAdi ?? cakisan.GuzergahAdi}' guzergahinda gorevli.",
                        Gun = gun,
                        Slot = kayit.Slot,
                        EtkilenenKayitId = cakisan.Id,
                        EtkilenenAciklama = cakisan.Guzergah?.GuzergahAdi ?? cakisan.GuzergahAdi
                    });
                }

                // Kural 5: Kapasite — güzergah başına slot başına araç sayısı PersonelSayisi'ni aşamaz
                if (kayit.GuzergahId.HasValue && kapasiteMap.TryGetValue(kayit.GuzergahId.Value, out var kapasite) && kapasite > 0)
                {
                    var ayniGuzergahSlotAracSayisi = tumKayitlar
                        .Count(x => x.Id != kayit.Id && x.GuzergahId == kayit.GuzergahId
                                    && x.Slot == kayit.Slot && x.GetGunDeger(gun) > 0);
                    if (ayniGuzergahSlotAracSayisi >= kapasite)
                    {
                        result.Conflicts.Add(new ConflictItem
                        {
                            Severity = ConflictSeverity.Blocking,
                            Kural = "Kapasite",
                            Mesaj = $"Bu guzergahin kapasitesi ({kapasite} arac) dolu. Gun {gun}, Slot {kayit.Slot}.",
                            Gun = gun,
                            Slot = kayit.Slot
                        });
                    }
                }

                // Kural 4: Tedarikçi izolasyonu - kendi araç + tedarikçi şoför (Warning)
                if (kayit.KaynakTipi == PlanlamaKaynakTipi.Kendi && kayit.SoforOdemeTipi != SoforOdemeTipi.Ozmal)
                {
                    result.Conflicts.Add(new ConflictItem
                    {
                        Severity = ConflictSeverity.Warning,
                        Kural = "Izolasyon",
                        Mesaj = $"Kendi aracla tedarikci sofor ({kayit.SoforOdemeTipi}) eslesmesi onay gerektirir.",
                        Gun = 0,
                        Slot = kayit.Slot
                    });
                }

                // Kural 3: Aynı gün + aynı slot → aynı şoför farklı görev (Warning)
                if (kayit.SoforId.HasValue)
                {
                    var ayniSofor = mevcutTumKayitlar
                        .Where(p => p.Id != kayit.Id
                            && p.SoforId == kayit.SoforId
                            && p.Slot == kayit.Slot
                            && p.GetGunDeger(gun) > 0)
                        .ToList();

                    foreach (var cakisan in ayniSofor)
                    {
                        if (!result.Conflicts.Any(c => c.EtkilenenKayitId == cakisan.Id && c.Kural == "Tek Sofor"))
                        {
                            result.Conflicts.Add(new ConflictItem
                            {
                                Severity = ConflictSeverity.Warning,
                                Kural = "Tek Sofor",
                                Mesaj = $"Gun {gun}: {kayit.Slot} slotunda '{cakisan.SoforAdi}' soforu birden fazla gorevde.",
                                Gun = gun,
                                Slot = kayit.Slot,
                                EtkilenenKayitId = cakisan.Id,
                                EtkilenenAciklama = $"{cakisan.Guzergah?.GuzergahAdi ?? cakisan.GuzergahAdi} / {cakisan.Plaka}"
                            });
                        }
                    }
                }
            }
        }

        return result;
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

        // GuzergahSefer satırlarını yükle (öncelikli araç/şoför kaynağı)
        var seferler = await db.GuzergahSeferleri
            .Where(s => guzergahIds.Contains(s.GuzergahId) && s.AracId.HasValue)
            .Include(s => s.Arac)
            .OrderBy(s => s.GuzergahId).ThenBy(s => s.Sira)
            .ToListAsync();
        var seferMap = seferler.GroupBy(s => s.GuzergahId).ToDictionary(g => g.Key, g => g.ToList());

        // Mevcut kayıtları al
        var mevcutlar = await db.PuantajKayitlar
            .Where(p => !p.IsDeleted && p.Yil == yil && p.Ay == ay
                        && p.GuzergahId != null && guzergahIds.Contains(p.GuzergahId!.Value))
            .ToListAsync();

        var sonuc = new List<PuantajKayit>(mevcutlar);

        foreach (var guzergah in guzergahlar)
        {
            // Öncelik 1: GuzergahSefer satırlarından araç/şoför ata
            if (seferMap.TryGetValue(guzergah.Id, out var gSeferler) && gSeferler.Any())
            {
                foreach (var sefer in gSeferler)
                {
                    var slotlar = SeferTipindenSlotlara(sefer.SeferTipi);
                    foreach (var slot in slotlar)
                    {
                        EkleEksikSatir(sonuc, mevcutlar, guzergah, sefer.AracId!.Value,
                            sefer.Arac?.AktifPlaka ?? sefer.Arac?.Plaka, null, sefer.SoforAd, yil, ay, slot);
                    }
                }
                continue;
            }

            // Öncelik 2: FiloGuzergahEslestirme'den araçlar
            var eslestirmeler = await db.FiloGuzergahEslestirmeleri
                .Include(e => e.Arac)
                .Include(e => e.Sofor)
                .Where(e => e.GuzergahId == guzergah.Id && e.IsActive)
                .ToListAsync();

            if (!eslestirmeler.Any() && guzergah.VarsayilanArac != null)
            {
                // Varsayılan araçla, sadece güzergahın SeferTipi'ne uygun slotlar
                foreach (var slot in SeferTipindenSlotlara(guzergah.SeferTipi))
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
                    foreach (var slot in SeferTipindenSlotlara(guzergah.SeferTipi))
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

    private static SeferSlot[] SeferTipindenSlotlara(SeferTipi tip) => tip switch
    {
        SeferTipi.Sabah => new[] { SeferSlot.Sabah },
        SeferTipi.Aksam => new[] { SeferSlot.Aksam },
        SeferTipi.SabahAksam => new[] { SeferSlot.Sabah, SeferSlot.Aksam },
        SeferTipi.Saatlik => new[] { SeferSlot.Sabah, SeferSlot.Aksam, SeferSlot.Mesai },
        _ => new[] { SeferSlot.Sabah }
    };

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
